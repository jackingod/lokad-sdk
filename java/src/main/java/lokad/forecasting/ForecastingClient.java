package lokad.forecasting;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;




/**
 * High-level client for the Forecasting API v3.
 * <p>
 * Compared to {@link ForecastingApi}, the client abstracts away paging and
 * continuation tokens. It also validates the inputs.
 * </p>
 */
public class ForecastingClient {

	private final static String lokadUserName = "auth-with-key@lokad.com";
	
	/**
	 * Initial value is based on the Forecasting API limitations, yet, this
	 * value can be lowered if timeouts are encountered.
	 */
	int _seriesSliceLength = 100;

	/**
	 * Same than {@link Constants#SeriesSliceLength} but for larger series.
	 */
	int _midSeriesSliceLength = 10;

	private final String _identity;
	private final IForecastingApi _forecastingApi;

	// const string ProductionEndpoint =
	// "http://api.lokad.com/forecasting3.svc";
	// const string SandboxEndpoint =
	// "http://sandbox-api.lokad.com/forecasting3.svc";

	// / <summary>Create a new client to access a Lokad account.</summary>
	// / <param name="identity">Authentication key to access the Lokad
	// account.</param>
	// / <remarks>The URL endpoint of the service is inferred from the
	// key.</remarks>
	// public ForecastingClient(String identity)
	// {
	// // Validating the format of the key.
	// byte[] bytes;
	// // try
	// // {
	// // bytes = Convert.FromBase64String(identity);
	// // }
	// // catch (FormatException)
	// // {
	// // throw new ArgumentException("Not a valid key.", "identity");
	// // }
	//
	// if(bytes.length <= 8)
	// {
	// throw new IllegalArgumentException("Key is too short." + "identity");
	// }
	//
	// // Extracting the server code from the key (to auto-plug the URL)
	//
	// var body = bytes.Skip(4).ToArray();
	//
	// // body[0] contains the API version
	// // body[1] contains the server (was 1 = Production, 2 = Sandbox, but not
	// used anymore)
	//
	// // By default, we plug the production.
	// var endpoint = body[1] != 2 ? ProductionEndpoint : SandboxEndpoint;
	//
	// _identity = identity;
	// _forecastingApi = new ForecastingApi(endpoint);
	// }

	/**
	 * Create a new client to access a Lokad account.
	 * <p>
	 * The URL endpoint of the service is inferred from the key.
	 * </p>
	 * 
	 * @param identity
	 *            Authentication key to access the Lokad account.
	 * @param endpoint
	 */
	public ForecastingClient(String identity, String endpoint) {
		String authString = lokadUserName + ":" + identity;
		_identity = Base64.encode(authString);
		_forecastingApi = new ForecastingApi(endpoint);
	}

	/**
	 * Access to the underlying client implementation.
	 * 
	 * @param identity
	 * @param forecastingApi
	 */
	public ForecastingClient(String identity, IForecastingApi forecastingApi) {
		_identity = identity;
		_forecastingApi = forecastingApi;
	}

	/**
	 * Insert a dataset into the Lokad account.
	 * 
	 * @param dataset
	 *            Dataset to be inserted.
	 * @throws IOException if an error occurred working with server.
	 * @throws IllegalArgumentException
	 *             Thrown if the dataset is not compliant with the Forecasting
	 *             API restrictions.
	 * @throws NullPointerException
	 *             Thrown if the argument is null.
	 * @throws IllegalStateException
	 *             Thrown if don't have the correct access rights, if the
	 *             service happens to be down at the time.
	 * @see IForecastingApi#InsertDataset(String, Dataset)
	 */
	public void InsertDataset(Dataset dataset) throws IOException {
		ForecastingApiValidators.Validate(dataset);
		String errorCode = _forecastingApi.InsertDataset(_identity, dataset);
		WrapAndThrow(errorCode);
	}

	/**
	 * Lazy list iterating over the datasets available in the Lokad account.
	 * 
	 * @return Lazy enumeration, network requests happen as the set gets
	 *         enumerated.
	 * @throws IOException if an error occurred working with server.
	 * @throws Thrown
	 *             if the access rights are incorrect, or if the service is down
	 *             at the moment.
	 */
	public List<Dataset> ListDatasets() throws IOException {
		DatasetCollection datasets = null;
		List<Dataset> collection = new ArrayList<Dataset>();

		do {
			datasets = _forecastingApi.ListDatasets(_identity, datasets != null ? datasets.ContinuationToken : null);

			WrapAndThrow(datasets.ErrorCode);

			for (Dataset dataset : datasets.Datasets) {
				collection.add(dataset);
			}

		} while (!isStringBlank(datasets.ContinuationToken));

		return collection;
	}

	/**
	 * Deletes the dataset specified as arguments from the Lokad account.
	 * 
	 * @param datasetName
	 *            Name of the target dataset.
	 * @throws IOException if an error occurred working with server.
	 * @throws NullPointerException
	 *             Thrown if the argument <code>datasetNames</code> is null.
	 * @throws IllegalArgumentException
	 *             Thrown is the dataset names are not compliant with the
	 *             Forecasting API, or if the names are not distinct.
	 * @throws IllegalStateException
	 *             Thrown if the access rights are incorrect, or if the service
	 *             is not available at the time.
	 */
	public void DeleteDataset(String datasetName) throws IOException {
		if (!ForecastingApiValidators.IsValidApiName(datasetName)) {
			throw new IllegalArgumentException(String.format("%s is not a valid dataset name.", datasetName));
		}

		String errorCode = _forecastingApi.DeleteDataset(_identity, datasetName);
		WrapAndThrow(errorCode);
	}

	/**
	 * Deletes the dataset specified as argument from the Lokad account, and
	 * waits until the deletion is effective.
	 * <p>
	 * This method will connect every 30s to check whether the dataset has been
	 * deleted until the dataset is finally flagged as "not found".
	 * </p>
	 * 
	 * @param datasetName
	 *            Name of the target dataset.
	 * @throws InterruptedException
	 * @throws IOException if an error occurred working with server.
	 * @throws NullPointerException
	 *             Thrown if the argument <code>datasetNames</code> is null.
	 * @throws IllegalArgumentException
	 *             Thrown is the dataset names are not compliant with the
	 *             Forecasting API, or if the names are not distinct.
	 * @throws IllegalStateException
	 *             Thrown if the access rights are incorrect, or if the service
	 *             is not available at the time.
	 */
	public void DeleteDatasetAndWait(String datasetName) throws InterruptedException, IOException {
		DeleteDataset(datasetName);

		TimeSerieCollection collection;
		while (true) {
			collection = _forecastingApi.ListTimeSeries(_identity, datasetName, null);

			if (ErrorCodes.DatasetNotFound.equals(collection.ErrorCode)) {
				break;
			}

			Thread.sleep(30000);
		}
	}

	/**
	 * Update or inserts time-series into the specified dataset.
	 * <p>
	 * The implementation takes care of enforcing the capacity limitations of
	 * the API. In particular, the array of series get splitted as needed into
	 * small sets before being pushed toward the Forecasting API.
	 * 
	 * @param datasetName
	 *            Targeted dataset.
	 * @param timeSeries
	 *            Series updated or inserted.
	 * @param enableMerge
	 *            Indicate whether existing series will be merged with inputs,
	 *            or if existing series will be overwritten by inputs.
	 * @throws IOException if an error occurred working with server.
	 * @throws NullPointerException
	 *             Thrown if one of the argument is null.
	 * @throws IllegalArgumentException
	 *             Throw if one of the argument is not compliant with the
	 *             Forecasting API specification.
	 * @throws IllegalStateException
	 *             Thrown if the access rights are incorrect, or if the service
	 *             is not available at the time.
	 * @see IForecastingApi#UpsertTimeSeries(String, String, TimeSerie[],
	 *      Boolean)
	 */
	public void UpsertTimeSeries(String datasetName, TimeSerie[] timeSeries, boolean enableMerge) throws IOException {
		// TODO catching potential network timeouts

		UpsertTimeSeriesInternal(datasetName, timeSeries, enableMerge);
	}

	void UpsertTimeSeriesInternal(String datasetName, TimeSerie[] timeSeries, boolean enableMerge) throws IOException {
		List<String> serieNames = new ArrayList<String>();
		for (TimeSerie timeSerie : timeSeries) {
			serieNames.add(timeSerie.Name);
		}
		ValidateSerieNames(datasetName, serieNames.toArray(new String[] {}));

		for (TimeSerie ts : timeSeries) {
			ForecastingApiValidators.Validate(ts);
		}

		// TODO
		// Heuristic: intermediate zeroes can be pruned
		// timeSeries = timeSeries.Select(serie =>
		// PruneIntermediateZeroes(serie)).ToArray();

		// When uploading series toward Lokad, requests should not weight more
		// than 4MB
		// Instead of trying to figure out complex corner situation, we just
		// deal with
		// series of varying sizes separately.

		// Splitting series according to their respective sizes

		List<TimeSerie> veryLargeSeries = new ArrayList<TimeSerie>();
		List<TimeSerie> largeSeries = new ArrayList<TimeSerie>();
		List<TimeSerie> smallSeries = new ArrayList<TimeSerie>();

		for (TimeSerie timeSerie : timeSeries) {
			if (timeSerie.Values != null && timeSerie.Values.length > 10000)
				veryLargeSeries.add(timeSerie);
			else if (timeSerie.Values != null && timeSerie.Values.length > 1000)
				largeSeries.add(timeSerie);
			else
				smallSeries.add(timeSerie);
		}

		// very large series are uploaded 1 by 1
		for (int i = 0; i < veryLargeSeries.size(); i++) {
			String errorCode = _forecastingApi.UpsertTimeSeries(_identity, datasetName,
					new TimeSerie[] { veryLargeSeries.get(i) }, enableMerge);

			WrapAndThrow(errorCode);
		}

		// large series are uploaded 10 by 10
		for (int i = 0; i < largeSeries.size(); i += _midSeriesSliceLength) {
			// No 'Slice()' method available
			int toIndex = (i + _midSeriesSliceLength < largeSeries.size()) ? i + _midSeriesSliceLength : largeSeries
					.size();
			List<TimeSerie> list = largeSeries.subList(i, toIndex);
			String errorCode = _forecastingApi.UpsertTimeSeries(_identity, datasetName,
					list.toArray(new TimeSerie[] {}), enableMerge);

			WrapAndThrow(errorCode);
		}

		// small series are uploaded 100 by 100
		for (int i = 0; i < smallSeries.size(); i += _seriesSliceLength) {
			// No 'Slice()' method available
			int toIndex = (i + _seriesSliceLength < smallSeries.size()) ? i + _seriesSliceLength : smallSeries.size();
			List<TimeSerie> list = smallSeries.subList(i, toIndex);
			String errorCode = _forecastingApi.UpsertTimeSeries(_identity, datasetName,
					list.toArray(new TimeSerie[] {}), enableMerge);

			WrapAndThrow(errorCode);
		}
	}

	// private static TimeSerie PruneIntermediateZeroes(TimeSerie timeSerie)
	// {
	// if(timeSerie.Values == null || timeSerie.Values.length < 2)
	// {
	// return timeSerie;
	// }
	//
	// TimeValue[] values = timeSerie.Values;
	//
	// return new TimeSerie
	// {
	// Name = timeSerie.Name,
	// Events = timeSerie.Events,
	// Tags = timeSerie.Tags,
	// Values =
	// values.Take(1)
	// .Union(values.Skip(1).Take(values.Length - 2).Where(tv => tv.Value !=
	// 0.0))
	// .Union(values.Skip(values.Length - 1)).
	// ToArray()
	// };
	// }

	/**
	 * List time-series in the specified dataset name.
	 * 
	 * @param datasetName
	 *            Enumerated dataset.
	 * @return A lazy enumeration of the time-series contained in the dataset is
	 *         returned. Network calls are made as the series get enumerated.
	 * @throws IOException if an error occurred working with server.
	 * @throws NullPointerException
	 *             Thrown if the argument is null.
	 * @throws IllegalArgumentException
	 *             Thrown is the dataset name is not compliant with Forecasting
	 *             API specification.
	 * @throws IllegalStateException
	 *             Thrown if the access rigths are incorrect, or if the dataset
	 *             does not exists, or if the service is down.
	 * @see IForecastingApi#ListTimeSeries(String, String, String)
	 */
	public List<TimeSerie> ListTimeSeries(String datasetName) throws IOException {
		TimeSerieCollection timeSeries = null;
		List<TimeSerie> collection = new ArrayList<TimeSerie>();

		do {
			timeSeries = _forecastingApi.ListTimeSeries(_identity, datasetName,
					timeSeries != null ? timeSeries.ContinuationToken : null);

			WrapAndThrow(timeSeries.ErrorCode);

			for (TimeSerie timeSerie : timeSeries.TimeSeries) {
				collection.add(timeSerie);
			}
		} while (!isStringBlank(timeSeries.ContinuationToken));

		return collection;
	}

	/**
	 * Deletes the specified time-series from a dataset. Time-series that do not
	 * exist are ignored.
	 * 
	 * @param datasetName
	 *            Targeted dataset.
	 * @param serieNames
	 *            Series to be deleted.
	 * @throws IOException if an error occurred working with server.
	 * @throws NullPointerException
	 *             Thrown if one of the argument is null.
	 * @throws IllegalArgumentException
	 *             Thrown if the arguments are not compliant with the
	 *             Forecasting API specification.
	 * @throws IllegalStateException
	 *             Thrown if the access rights are not correct, of if dataset
	 *             does not exist, or if the service is down.
	 * @see IForecastingApi#DeleteTimeSeries(String, String, String[])
	 */
	public void DeleteTimeSeries(String datasetName, String[] serieNames) throws IOException {
		ValidateSerieNames(datasetName, serieNames);

		for (int i = 0; i < serieNames.length; i += _seriesSliceLength) {
			int toIndex = (i + _midSeriesSliceLength < serieNames.length) ? i + _midSeriesSliceLength
					: serieNames.length;
			List<String> list = new ArrayList<String>();
			for (int j = i; j < toIndex; j++) {
				list.add(serieNames[j]);
			}

			// No 'Slice()' method available
			String errorCode = _forecastingApi.DeleteTimeSeries(_identity, datasetName, list.toArray(new String[] {}));

			WrapAndThrow(errorCode);
		}
	}

	/**
	 * Trigger the forecast computation. No need to call this method (
	 * {@link #GetForecasts(String, String[])}) unless you want to avoid your
	 * <code>GetForecasts</code> call being blocked waiting.
	 * <p>
	 * This method can be called many time until the forecasts are finally
	 * available.
	 * </p>
	 * 
	 * @param datasetName
	 *            Targeted dataset.
	 * @return Indicates whether the forecasts are ready.
	 * @throws IOException if an error occurred working with server.
	 */
	public boolean TriggerForecastCompute(String datasetName) throws IOException {
		ForecastStatus status = _forecastingApi.GetForecastStatus(_identity, datasetName);

		WrapAndThrow(status.ErrorCode);

		return status.ForecastsReady;
	}

	/**
	 * Gets the forecasts from a specified datasets.
	 * <p>
	 * Call is blocking until the forecasts are ready and downloaded.
	 * </p>
	 * 
	 * @param datasetName
	 *            Targeted dataset.
	 * @param serieNames
	 *            Targeted series. Series that do not exists in the targeted
	 *            dataset are ignored.
	 * @return
	 * @throws InterruptedException
	 * @throws IOException if an error occurred working with server.
	 * @throws NullPointerException
	 *             Thrown if any of the argument is null.
	 * @throws IllegalArgumentException
	 *             Thrown if the arguments are not compliant with the
	 *             Forecasting API specification.
	 * @throws IllegalStateException
	 *             Thrown if the access rights are incorrect, if the dataset
	 *             does not exists, or if the service is down.
	 * @see IForecastingApi#GetForecastStatus(String, String)
	 * @see IForecastingApi#GetForecasts(String, String, String[])
	 */
	public ForecastSerie[] GetForecasts(String datasetName, String[] serieNames) throws InterruptedException, IOException {
		// TODO
		// catching potential network timeouts
		// return RetryPolicy(() => GetForecastsInternal(datasetName,
		// serieNames));
		return GetForecastsInternal(datasetName, serieNames);
	}

	private ForecastSerie[] GetForecastsInternal(String datasetName, String[] serieNames) throws InterruptedException, IOException {
		ValidateSerieNames(datasetName, serieNames);

		ForecastStatus status = null;
		do {
			if (null != status) {
				Thread.sleep(10 * 1000); // 10s sleep between checks while
											// waiting for the forecasts
			}

			status = _forecastingApi.GetForecastStatus(_identity, datasetName);

			WrapAndThrow(status.ErrorCode);

		} while (!status.ForecastsReady);

		// Dictionary<String, ForecastSerie> forecasts = new Hashtable<String,
		// ForecastSerie>(serieNames.length);
		List<ForecastSerie> forecasts = new ArrayList<ForecastSerie>();

		// A subtle situation may arise if the forecasts are so large that
		// they can't be retrieved in batches of 100 while still be compliant
		// with 4MB limitation.

		for (int i = 0; i < serieNames.length; i += _seriesSliceLength) {
			int toIndex = (i + _seriesSliceLength < serieNames.length) ? i + _seriesSliceLength : serieNames.length;
			List<String> list = new ArrayList<String>();
			for (int j = i; j < toIndex; j++) {
				list.add(serieNames[j]);
			}
			// No 'Slice()' method available
			ForecastCollection forecastCollection = _forecastingApi.GetForecasts(_identity, datasetName,
					list.toArray(new String[] {}));

			WrapAndThrow(forecastCollection.ErrorCode);

			for (ForecastSerie forecast : forecastCollection.Series) {
				// forecasts.put(forecast.Name, forecast);
				forecasts.add(forecast);
			}
		}

		// Ordering the results before returning them.
		// (not necessary, but simplifies the debugging)
		// return serieNames.Where(forecasts.ContainsKey).Select(n =>
		// forecasts[n]).ToArray();
		return forecasts.toArray(new ForecastSerie[] {});
	}

	static void ValidateSerieNames(String datasetName, String[] serieNames) {
		if (!ForecastingApiValidators.IsValidApiName(datasetName)) {
			throw new IllegalArgumentException("Invalid dataset name.");
		}

		if (null == serieNames) {
			throw new NullPointerException("serieNames");
		}

		for (String tag : serieNames) {
			if (!ForecastingApiValidators.IsValidApiName(tag))
				throw new IllegalArgumentException(String.format("%s is not a valid tag.", tag));
		}

		for (int i = 0; i < serieNames.length - 1; i++) {
			for (int j = i + 1; j < serieNames.length; j++) {
				if (serieNames[i].equals(serieNames[j])) {
					throw new IllegalArgumentException("All tags should be distinct within a TimeSerie.");
				}
			}
		}
	}

	static boolean isStringBlank(String s) {
		return s == null || s.length() == 0;
	}

	static void WrapAndThrow(String errorCode) {
		// If error code is null or empty, then just ignore.
		if (isStringBlank(errorCode))
			return;

		if (ErrorCodes.AuthenticationFailed.equals(errorCode)) {
			throw new IllegalStateException(errorCode);
		} else if (ErrorCodes.DatasetNotFound.equals(errorCode)) {
			throw new IllegalStateException(errorCode);
		} else if (ErrorCodes.InvalidDatasetState.equals(errorCode)) {
			// Not supposed to happen thanks to retry policy of ForecastingApi.
			// Might happen though, if deletion takes too long.
			throw new IllegalStateException(errorCode);
		} else if (ErrorCodes.OutOfRangeInput.equals(errorCode)) {
			// Not supposed to happen thanks to client side validation
			throw new IllegalArgumentException(errorCode);
		} else if (ErrorCodes.ServiceFailure.equals(errorCode)) {
			// Not supposed to happen thanks to retry policy of ForecastingApi.
			// Might happen though, if service stays down for too long.
			throw new IllegalStateException(errorCode);
		}
	}
	//
	// /// <summary>Ad-hoc retry policy for transcient network errors.</summary>
	// T RetryPolicy<T>(Func<T> webRequest)
	// {
	// const int maxAttempts = 10;
	//
	// for (int i = 0; i < maxAttempts + 1; i++)
	// {
	// try
	// {
	// // if the request completes, we don't try again
	// return webRequest();
	// }
	// catch (TimeoutException)
	// {
	// // after 'maxAttempts' we give up
	// if (i >= maxAttempts)
	// {
	// throw;
	// }
	//
	// // heuristic: time-outs are encountered with client with low bandwidth
	// // in such situation, we need to make smaller web requests, otherwise
	// // no request is going to succeed.
	//
	// if (i >= 3) // at 3 timeouts, we switch to the 'slow mode'.
	// {
	// _seriesSliceLength = 10;
	// _midSeriesSliceLength = 1;
	// }
	//
	// if (i >= 6) // at 6 timeouts, we switch to the 'extra slow mode'.
	// {
	// _seriesSliceLength = 1;
	// _midSeriesSliceLength = 1;
	// }
	// }
	// }
	//
	// throw new ApplicationException("Retry policy is broken.");
	// }
}
