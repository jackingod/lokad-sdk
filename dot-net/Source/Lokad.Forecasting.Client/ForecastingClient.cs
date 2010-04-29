#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lokad.Forecasting.Client
{
	/// <summary>High-level client for the Forecasting API v3.</summary>
	/// <remarks>Compared to <see cref="ForecastingApi"/>, the client abstracts
	/// away paging and continuation tokens. It also validates the inputs.</remarks>
	public class ForecastingClient
	{
		/// <summary>Compound methods of Forecasting API are nearly
		/// all upper bounded to collection of 100 items at most.</summary>
		const int SliceLength = 100;

		readonly string _identity;
		readonly IForecastingApi _forecastingApi;

		const string ProductionEndpoint = "http://api.lokad.com/forecasting3.svc";
		const string SandboxEndpoint = "http://sandbox-api.lokad.com/forecasting3.svc";

		/// <summary>Create a new client to access a Lokad account.</summary>
		/// <param name="identity">Authentication key to access the Lokad account.</param>
		/// <remarks>The URL endpoint of the service is inferred from the key.</remarks>
		public ForecastingClient(string identity)
		{
			// Validating the format of the key.
			byte[] bytes;
			try
			{
				bytes = Convert.FromBase64String(identity);

			}
			catch (FormatException)
			{
				throw new ArgumentException("Not a valid key.", "identity");
			}

			if(bytes.Length <= 8)
			{
				throw new ArgumentException("Key is too short.", "identity");
			}

			// Extracting the server code from the key (to auto-plug the URL)

			var body = bytes.Skip(4).ToArray();

			// body[0] contains the API version
			// body[1] contains the server (1 = Production, 2 = Sandbox)

			// By default, we plug the production.
			var endpoint = body[1] != 2 ? ProductionEndpoint : SandboxEndpoint;

			_identity = identity;
			_forecastingApi = new ForecastingApi(endpoint);
		}

		/// <summary>Create a new client to access a Lokad account.</summary>
		/// <param name="identity">Authentication key to access the Lokad account.</param>
		/// <param name="endpoint"></param>
		/// <remarks>The URL endpoint of the service is inferred from the key.</remarks>
		public ForecastingClient(string identity, string endpoint)
		{
			_identity = identity;
			_forecastingApi = new ForecastingApi(endpoint);
		}

		/// <summary>Access to the underlying client implementation.</summary>
		public ForecastingClient(string identity, IForecastingApi forecastingApi)
		{
			_identity = identity;
			_forecastingApi = forecastingApi;
		}

		/// <summary>Insert a dataset into the Lokad account.</summary>
		/// <param name="dataset">Dataset to be inserted.</param>
		/// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
		/// <exception cref="ArgumentException">
		/// Thrown if the dataset is not compliant with 
		/// the Forecasting API restrictions.</exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if don't have the correct access rights,
		/// if the service happens to be down at the time.
		/// </exception>
		/// <seealso cref="IForecastingApi.InsertDataset"/>
		public void InsertDataset(Dataset dataset)
		{
			dataset.Validate();
			var errorCode = _forecastingApi.InsertDataset(_identity, dataset);
			WrapAndThrow(errorCode);
		}

		/// <summary>
		/// Lazy list iterating over the datasets available in the Lokad account.
		/// </summary>
		/// <returns>
		/// Lazy enumeration, network requests happen as the set gets enumerated.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the access rights are incorrect, or if the service is down
		/// at the moment.
		/// </exception>
		public IEnumerable<Dataset> ListDatasets()
		{
			DatasetCollection datasets = null;

			do
			{
				datasets = _forecastingApi.ListDatasets(_identity, 
					datasets != null ? datasets.ContinuationToken : null);

				WrapAndThrow(datasets.ErrorCode);

				foreach(var dataset in datasets.Datasets)
				{
					yield return dataset;
				}

			} while (!string.IsNullOrEmpty(datasets.ContinuationToken));
		}

		/// <summary>
		/// Deletes the datasets specified as arguments from the Lokad account.
		/// </summary>
		/// <param name="datasetName">Name of the target dataset.</param>
		/// <exception cref="ArgumentNullException">Thrown if the argument <c>datasetNames</c> is null.</exception>
		/// <exception cref="ArgumentException">
		/// Thrown is the dataset names are not compliant
		/// with the Forecasting API, or if the names are not distinct.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the access rights are incorrect, or if the service is not available
		/// at the time.</exception>
		public void DeleteDataset(string datasetName)
		{
			if(!datasetName.IsValidApiName())
			{
				throw new ArgumentException(
					string.Format("{0} is not a valid dataset name.",datasetName), "datasetName");
			}

			var errorCode = _forecastingApi.DeleteDataset(_identity, datasetName);
			WrapAndThrow(errorCode);
		}

		/// <summary>
		/// Update or inserts time-series into the specified dataset.
		/// </summary>
		/// <param name="datasetName">Targeted dataset.</param>
		/// <param name="timeSeries">Series updated or inserted.</param>
		/// <param name="enableMerge">
		/// Indicate whether existing series will be merged with inputs,
		/// or if existing series will be overwritten by inputs.
		/// </param>
		/// <remarks>
		/// The implementation takes care of enforcing the capacity limitations of the API.
		/// In particular, the array of series get splitted as needed into small sets
		/// before being pushed toward the Forecasting API.
		/// </remarks>
		/// <exception cref="ArgumentNullException">Thrown if one of the argument is null.</exception>
		/// <exception cref="ArgumentException">Throw if one of the argument is not compliant
		/// with the Forecasting API specification.</exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the access rights are incorrect, or if the service is not available
		/// at the time.</exception>
		/// <seealso cref="IForecastingApi.UpsertTimeSeries"/>
		public void UpsertTimeSeries(string datasetName, TimeSerie[] timeSeries, bool enableMerge)
		{
			ValidateSerieNames(datasetName, timeSeries.Select(t => t.Name).ToArray());

			// When uploading series toward Lokad, requests should not weight more than 4MB
			// Instead of trying to figure out complex corner situation, we just deal with
			// series of varying sizes separately.

			// Splitting series according to their respective sizes
			var veryLargeSeries = timeSeries.Where(t => t.Values != null && t.Values.Length > 10000).ToArray();
			var largeSeries = timeSeries.Where(t => t.Values != null && t.Values.Length <= 10000 && t.Values.Length > 1000).ToArray();
			var smallSeries = timeSeries.Where(t => t.Values == null || t.Values.Length < 1000).ToArray();

			// very large series are uploaded 1 by 1
			for (var i = 0; i < veryLargeSeries.Length; i++)
			{
				var errorCode =
					_forecastingApi.UpsertTimeSeries(_identity, datasetName, 
						new[]{veryLargeSeries[i]}, enableMerge);

				WrapAndThrow(errorCode);
			}

			// large series are uploaded 10 by 10
			for (var i = 0; i < largeSeries.Length; i += 10)
			{
				// No 'Slice()' method available 
				var errorCode =
					_forecastingApi.UpsertTimeSeries(_identity, datasetName,
						largeSeries.Skip(i).Take(10).ToArray(), enableMerge);

				WrapAndThrow(errorCode);
			}

			// small series are uploaded 100 by 100
			for (var i = 0; i < smallSeries.Length; i += SliceLength)
			{
				// No 'Slice()' method available 
				var errorCode =
					_forecastingApi.UpsertTimeSeries(_identity, datasetName,
						smallSeries.Skip(i).Take(SliceLength).ToArray(), enableMerge);

				WrapAndThrow(errorCode);
			}
		}

		/// <summary>
		/// List time-series in the specified dataset name.
		/// </summary>
		/// <param name="datasetName">Enumerated dataset.</param>
		/// <returns>
		/// A lazy enumeration of the time-series contained in the dataset is returned.
		/// Network calls are made as the series get enumerated.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
		/// <exception cref="ArgumentException">
		/// Thrown is the dataset name is not compliant with Forecasting API specification.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the access rigths are incorrect, or if the dataset does not exists,
		/// or if the service is down.</exception>
		/// <seealso cref="IForecastingApi.ListTimeSeries"/>
		public IEnumerable<TimeSerie> ListTimeSeries(string datasetName)
		{
			TimeSerieCollection timeSeries = null;

			do
			{
				timeSeries = _forecastingApi.ListTimeSeries(_identity, datasetName,
					timeSeries != null ? timeSeries.ContinuationToken : null);

				WrapAndThrow(timeSeries.ErrorCode);

				foreach (var timeSerie in timeSeries.TimeSeries)
				{
					yield return timeSerie;
				}

			} while (!string.IsNullOrEmpty(timeSeries.ContinuationToken));
		}

		/// <summary>
		/// Deletes the specified time-series from a dataset.
		/// Time-series that do not exist are ignored.
		/// </summary>
		/// <param name="datasetName">Targeted datasets.</param>
		/// <param name="serieNames">Series to be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown if one of the argument is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the arguments are not compliant
		/// with the Forecasting API specification.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the access rights are not correct,
		/// of if dataset does not exist, or if the service is down.</exception>
		/// <seealso cref="IForecastingApi.DeleteTimeSeries"/>
		public void DeleteTimeSeries(string datasetName, string[] serieNames)
		{
			ValidateSerieNames(datasetName, serieNames);

			for (var i = 0; i < serieNames.Length; i += SliceLength)
			{
				// No 'Slice()' method available 
				var errorCode =
					_forecastingApi.DeleteTimeSeries(_identity, datasetName,
						serieNames.Skip(i).Take(SliceLength).ToArray());

				WrapAndThrow(errorCode);
			}
		}

		/// <summary>
		/// Gets the forecasts from a specified datasets.
		/// </summary>
		/// <param name="datasetName">Targeted dataset.</param>
		/// <param name="serieNames">Targeted series. Series that do not
		/// exists in the targeted dataset are ignored.</param>
		/// <remark>Call is blocking until the forecasts are ready
		/// and downloaded.</remark>
		/// <exception cref="ArgumentNullException">Thrown if any of the argument is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the arguments are not compliant
		/// with the Forecasting API specification.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the access rights are incorrect,
		/// if the dataset does not exists, or if the service is down.</exception>
		/// <seealso cref="IForecastingApi.GetForecastStatus"/>
		/// <seealso cref="IForecastingApi.GetForecasts"/>
		public ForecastSerie[] GetForecasts(string datasetName, string[] serieNames)
		{
			ValidateSerieNames(datasetName, serieNames);

			ForecastStatus status = null;
			do
			{
				if(null != status)
				{
					Thread.Sleep(30 * 1000); // 30s sleep between checks while waiting for the forecasts
				}

				status = _forecastingApi.GetForecastStatus(_identity, datasetName);

				WrapAndThrow(status.ErrorCode);

			} while (!status.ForecastsReady);

			var forecasts = new Dictionary<string, ForecastSerie>(serieNames.Length);

			// A subtle situation may arise if the forecasts are so large that
			// they can't be retrieved in batches of 100 while still be compliant
			// with 4MB limitation.

			for (var i = 0; i < serieNames.Length; i += SliceLength)
			{
				// No 'Slice()' method available 
				var forecastCollection =
					_forecastingApi.GetForecasts(_identity, datasetName,
						serieNames.Skip(i).Take(SliceLength).ToArray());

				WrapAndThrow(forecastCollection.ErrorCode);

				foreach(var forecast in forecastCollection.Series)
				{
					forecasts.Add(forecast.Name, forecast);
				}
			}

			// Ordering the results before returning them.
			// (not necessary, but simplifies the debugging)
			return serieNames.Where(forecasts.ContainsKey).Select(n => forecasts[n]).ToArray();
		}

		static void ValidateSerieNames(string datasetName, string[] serieNames)
		{
			if (!datasetName.IsValidApiName())
			{
				throw new ArgumentException("Invalid dataset name.", "datasetName");
			}

			if (null == serieNames)
			{
				throw new ArgumentNullException("serieNames");
			}

			foreach (var name in serieNames.Where(n => !n.IsValidApiName()))
			{
				throw new ArgumentException(
					String.Format("{0} is not a valid serie name.", name), "serieNames");
			}

			if (serieNames.Distinct().Count() < serieNames.Length)
			{
				throw new ArgumentException("All serie names are expected to be distinct.", "serieNames");
			}
		}

		static void WrapAndThrow(string errorCode)
		{
			// If error code is null or empty, then just ignore.
			if(string.IsNullOrEmpty(errorCode)) return;

			switch (errorCode)
			{
				case ErrorCodes.AuthenticationFailed:
					throw new InvalidOperationException(errorCode);

				case ErrorCodes.DatasetNotFound:
					throw new InvalidOperationException(errorCode);

				// Not supposed to happen thanks to retry policy of ForecastingApi.
				// Might happen though, if deletion takes too long.
				case ErrorCodes.InvalidDatasetState:
					throw new InvalidOperationException(errorCode);

				// Not supposed to happen thanks to client side validation
				case ErrorCodes.OutOfRangeInput:
					throw new ArgumentException(errorCode);

				// Not supposed to happen thanks to retry policy of ForecastingApi.
				// Might happen though, if service stays down for too long.
				case ErrorCodes.ServiceFailure:
					throw new InvalidOperationException(errorCode);
			}
		}
	}
}
