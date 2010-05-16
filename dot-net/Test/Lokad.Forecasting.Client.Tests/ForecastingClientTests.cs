#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion
using System;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;

namespace Lokad.Forecasting.Client.Tests
{
	[TestFixture]
	public class ForecastingClientTests
	{
		[Test]
		public void InsertDatasets_IsForwarded()
		{
			string identity = "mockid";

			var dataset = GetDataset();

			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.InsertDataset(identity, dataset))
					.Return(string.Empty);
			}

			using (mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				client.InsertDataset(dataset);
			}
		}

        [Test]
        public void PruneIntermediateZeroes_WorksWithEmptyValues()
        {
            var ts = new TimeSerie();
            var pruned = ForecastingClient.PruneIntermediateZeroes(ts);
            Assert.IsNotNull(pruned, "#A00");
            Assert.IsNull(pruned.Values, "#A01");
        }

	    [Test]
        public void PruneIntermediateZeroes_WorksWithSmallSeries()
        {
            var ts = new TimeSerie
            {
                Values = new[]
                {
                    new TimeValue {Time = DateTime.UtcNow, Value = 0.0},
                    new TimeValue { Time = DateTime.UtcNow.AddHours(1), Value = 0.0}
                }
            };

            var pruned = ForecastingClient.PruneIntermediateZeroes(ts);
            Assert.IsNotNull(pruned, "#A00");
            Assert.IsNotNull(pruned.Values, "#A01");
            Assert.AreEqual(2, pruned.Values.Length, "#A02");
        }

        [Test]
        public void PruneIntermediateZeroes_WorksWithMinySeries()
        {
            var ts = new TimeSerie
            {
                Values = new[]
                {
                    new TimeValue {Time = new DateTime(2001,1,1), Value = 0.0},
                    new TimeValue {Time = new DateTime(2001,1,2), Value = 0.0},
                    new TimeValue { Time = new DateTime(2001,1,3), Value = 0.0}
                }
            };

            var pruned = ForecastingClient.PruneIntermediateZeroes(ts);
            Assert.IsNotNull(pruned, "#A00");
            Assert.IsNotNull(pruned.Values, "#A01");
            Assert.AreEqual(2, pruned.Values.Length, "#A02");
            Assert.AreEqual(ts.Values[0].Time, pruned.Values[0].Time, "#A03");
            Assert.AreEqual(ts.Values[2].Time, pruned.Values[1].Time, "#A04");
        }

		[Test]
		public void ListDatasets_IsForwarded()
		{
			string identity = "mockid", token = "mytoken";
			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.ListDatasets(identity, null))
					.Return(new DatasetCollection
					{
						Datasets = new Dataset[0],
						ContinuationToken = token
					});

				// 2nd call comes with continuation token
				Expect.Call(api.ListDatasets(identity, token))
					.Return(new DatasetCollection
					{
						Datasets = new Dataset[0],
						ContinuationToken = null
					});
			}

			using(mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				var result = client.ListDatasets();

				Assert.AreEqual(0, result.Count(), "#A00");
			}
		}

		[Test]
		public void DeleteDatasets_IsForwarded()
		{
			string identity = "mockid";

			var dataset = GetDataset();

			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.DeleteDataset(identity, dataset.Name))
					.Return(string.Empty);
			}

			using (mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				client.DeleteDataset(dataset.Name);
			}
		}

		[Test]
		public void UpsertTimeSeries_IsForwarded()
		{
			string identity = "mockid";

			var timeSeries = GetTimeSeries(180);

			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.UpsertTimeSeries(identity, null, null, false))
					.Return(string.Empty)
					.IgnoreArguments()
					.Repeat.Twice(); //  180 series => two requests
			}

			using (mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				client.UpsertTimeSeries("mockdataset", timeSeries, false);
			}	
		}

		[Test]
		public void ListTimeSeries_IsForwarded()
		{
			string identity = "mockid", token = "mytoken", datasetName = "mydataset";
			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.ListTimeSeries(identity, datasetName, null))
					.Return(new TimeSerieCollection
					{
						TimeSeries = new TimeSerie[0],
						ContinuationToken = token
					});

				// 2nd call comes with continuation token
				Expect.Call(api.ListTimeSeries(identity, datasetName, token))
					.Return(new TimeSerieCollection
					{
						TimeSeries = new TimeSerie[0],
						ContinuationToken = null
					});
			}

			using (mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				var result = client.ListTimeSeries(datasetName);

				Assert.AreEqual(0, result.Count(), "#A00");
			}
		}

		[Test]
		public void DeleteTimeSeries_IsForwarded()
		{
			string identity = "mockid", datasetName = "mydataset";

			var timeSeries = GetTimeSeries(180);

			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.DeleteTimeSeries(identity, datasetName, null))
					.Return(string.Empty)
					.IgnoreArguments()
					.Repeat.Twice(); //  180 time-series => two requests
			}

			using (mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				client.DeleteTimeSeries(datasetName, timeSeries.Select(d => d.Name).ToArray());
			}
		}

		[Test]
		public void GetForecasts_IsForwarded()
		{
			string identity = "mockid", datasetName = "mydataset";

			var timeSeries = GetTimeSeries(180);

			IForecastingApi api;

			var mocks = new MockRepository();
			using (mocks.Record())
			{
				api = mocks.StrictMock<IForecastingApi>();

				Expect.Call(api.GetForecastStatus(identity, datasetName))
					.Return(new ForecastStatus
					        	{
					        		ForecastsReady = true
					        	});

				Expect.Call(api.GetForecasts(identity, datasetName, null))
					.Return(new ForecastCollection
					        	{
					        		Series = new [] { new ForecastSerie { Name = timeSeries[0].Name }}
					        	})
					.IgnoreArguments(); //  180 time-series => two requests

				Expect.Call(api.GetForecasts(identity, datasetName, null))
					.Return(new ForecastCollection
					{
						Series = new[] { new ForecastSerie { Name = timeSeries[1].Name } }
					})
					.IgnoreArguments(); 
			}

			using (mocks.Playback())
			{
				var client = new ForecastingClient(identity, api);
				var forecasts = client.GetForecasts(datasetName, timeSeries.Select(d => d.Name).ToArray());
				Assert.IsNotNull(forecasts, "#A00");
				Assert.AreEqual(2, forecasts.Length, "#A01");
				Assert.AreEqual(timeSeries[0].Name, forecasts[0].Name, "#A02");
				Assert.AreEqual(timeSeries[1].Name, forecasts[1].Name, "#A03");
			}
		}

		Dataset GetDataset()
		{
			return new Dataset
			{
				Name = "d",
				Horizon = 1,
				Period = PeriodCodes.Week
			};
		}

		TimeSerie[] GetTimeSeries(int count)
		{
			var array = new TimeSerie[count];
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = new TimeSerie
				           	{
				           		Name = "t" + i,
				           		Values = new[] {new TimeValue {Time = new DateTime(2001, 1, 1), Value = 1.0}}
				           	};
			}
			return array;
		}
	}
}
