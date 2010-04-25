#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;

namespace Lokad.Forecasting.Client
{
	/// <summary>Decorator around the Forecasting API v3.</summary>
	/// <remarks>Implement a low level retry policy to deal with
	/// transient network or service errors.</remarks>
	public class ForecastingApi : IForecastingApi, IDisposable
	{
		readonly ChannelFactory<IForecastingApi> _factory;
		readonly IForecastingApi _channel;

		public ForecastingApi(string endPoint)
		{
			var binding = new BasicHttpBinding();
			var address = new EndpointAddress(endPoint);

			_factory = new ChannelFactory<IForecastingApi>(binding, address);
			_channel = _factory.CreateChannel(); 
		}

		public void Dispose()
		{
			_factory.Close();
		}

		public string InsertDataset(string identity, Dataset dataset)
		{
			return RetryPolicy(() => _channel.InsertDataset(identity, dataset));
		}

		public DatasetCollection ListDatasets(string identity, string continuationToken)
		{
			return RetryPolicy(() => _channel.ListDatasets(identity, continuationToken));
		}

		public string DeleteDataset(string identity, string datasetName)
		{
			return RetryPolicy(() => _channel.DeleteDataset(identity, datasetName));
		}

		public string UpsertTimeSeries(string identity, string datasetName, TimeSerie[] timeSeries, bool enableMerge)
		{
			// HACK: removing time-zone info to avoid datetime shift caused by serialization/deserizalization
			timeSeries = timeSeries.Select(ts => 
				new TimeSerie
             	{
             		Name = ts.Name,
					Tags = ts.Tags,
					Events = ts.Events != null ? ts.Events.Select(e => 
						new EventValue
                       	{
                       		Tags = e.Tags,
							Time = DateTime.SpecifyKind(e.Time, DateTimeKind.Unspecified),
							KnownSince = DateTime.SpecifyKind(e.KnownSince, DateTimeKind.Unspecified),
                       	}).ToArray() : null,
					Values = ts.Values != null ? ts.Values.Select(tv =>
						new TimeValue
						{
							Time = DateTime.SpecifyKind(tv.Time, DateTimeKind.Unspecified),
							Value = tv.Value
						}).ToArray() : null
             	}).ToArray();

			return RetryPolicy(() => _channel.UpsertTimeSeries(identity, datasetName, timeSeries, enableMerge));
		}

		public TimeSerieCollection ListTimeSeries(string identity, string datasetName, string continuationToken)
		{
			return RetryPolicy(() => _channel.ListTimeSeries(identity, datasetName, continuationToken));
		}

		public string DeleteTimeSeries(string identity, string datasetName, string[] serieNames)
		{
			return RetryPolicy(() => _channel.DeleteTimeSeries(identity, datasetName, serieNames));
		}

		public ForecastStatus GetForecastStatus(string identity, string datasetName)
		{
			return RetryPolicy(() => _channel.GetForecastStatus(identity, datasetName));
		}

		public ForecastCollection GetForecasts(string identity, string datasetName, string[] serieNames)
		{
			return RetryPolicy(() => _channel.GetForecasts(identity, datasetName, serieNames));
		}

		/// <summary>Ad-hoc retry policy for transcient network errors.</summary>
		static T RetryPolicy<T>(Func<T> webRequest)
		{
			const int maxAttempts = 10;

			for(int i = 0; i < maxAttempts + 1; i++)
			{
				try
				{
					// if the request completes, we don't try again
					return webRequest();
				}
				catch (WebException)
				{
					if (i < maxAttempts)
					{
						// increasing sleep delay pattern
						Thread.Sleep((i + 1)*1000);
					}
					else
					{
						// after 'maxAttempts' we give up
						throw;
					}
				}
			}

			throw new ApplicationException("Retry policy is broken.");
		}
	}
}
