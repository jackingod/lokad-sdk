using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Lokad.Forecasting.Client.Tests
{
    [TestFixture]
    public class ForecastingApiTests
    {
        // identity format "API KEY"
        private const string Identity = "32h7sAEATW0ohaw3OXstys/P45YqwzUIx6BRPCk=";
        private const string Endpoint = "http://api.lokad.com/rest/forecasting3";

        private IForecastingApi _forecastingApi;

        [SetUp]
        public void Setup()
        {
            // Current version of forecasting API v3 cannot process compressed requests.
            _forecastingApi = new ForecastingApi(Endpoint);
        }

        [Test]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void AuthenticateWithInvalidCredentials()
        {
            _forecastingApi.ListDatasets("aW52YWxpZGNyZWRlbnRpYWw=", null);
        }

        [Test]
        public void InsertValidDataset()
        {
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };
            var errorCode = _forecastingApi.InsertDataset(Identity, dataset);

            Assert.IsEmpty(errorCode);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        public void InsertDatasetWithWrongPeriod()
        {
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = "millennium"
                };

            var exception = Assert.Throws<ArgumentException>(() => _forecastingApi.InsertDataset(Identity, dataset));
            Assert.AreEqual(ErrorCodes.OutOfRangeInput, exception.Message);
        }

        [Test]
        public void InsertDatasetWithLongName()
        {
            var dataset = new Dataset
                {
                    Name = "Dataset".PadLeft(1024, 'd'),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };

            var exception = Assert.Throws<ArgumentException>(() => _forecastingApi.InsertDataset(Identity, dataset));
            Assert.AreEqual(ErrorCodes.OutOfRangeInput, exception.Message);
        }

        [Test]
        public void ListDatasetsTest()
        {
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var datasetCollection = _forecastingApi.ListDatasets(Identity, String.Empty);

            Assert.IsEmpty(datasetCollection.ErrorCode);
            Assert.IsTrue(0 < datasetCollection.Datasets.Length);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        public void DeleteDatasetTest()
        {
            // insert test dataset
            var datasetName = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff");
            var dataset = new Dataset
                {
                    Name = datasetName,
                    Horizon = 60,
                    Period = "week"
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            // delete test dataset
            var errorCode =_forecastingApi.DeleteDataset(Identity, datasetName);

            Assert.IsTrue(String.IsNullOrEmpty(errorCode));
        }

        [Test]
        public void UpsertTimeSeriesTest()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            var errorCode = _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            Assert.IsEmpty(errorCode);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        public void Data_round_trip()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            var errorCode = _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, true);

            Assert.IsEmpty(errorCode);

            var client = new ForecastingClient(Identity, _forecastingApi);
            var data = client.ListTimeSeries(dataset.Name);
            var loaded = data.ToDictionary(s => s.Name);

            foreach (var exp in timeseries)
            {
                var actual = loaded[exp.Name];
                CollectionAssert.AreEquivalent(exp.Tags, actual.Tags, "Tags equal for {0}", exp.Name);
                
                CollectionAssert.AreEqual(exp.Values.Select(s => s.ToString()).ToArray(), actual.Values.Select(s => s.ToString()).ToArray(), "Values are equal");
                CollectionAssert.AreEqual(exp.Events.Select(s => s.ToString()).ToArray(), actual.Events.Select(s => s.ToString()).ToArray(), "Events are equal");

                Assert.AreEqual(exp.Tau, actual.Tau);
                Assert.AreEqual(exp.Lambda, actual.Lambda);
            }

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        public void ListTimeSeriesTest()
        {
             // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeSeriesCollection = _forecastingApi.ListTimeSeries(Identity, dataset.Name, string.Empty);

            Assert.IsEmpty(timeSeriesCollection.ErrorCode);
            Assert.IsEmpty(timeSeriesCollection.TimeSeries);
        }

        [Test]
        public void DeleteTimeSeriesTest()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 60,
                    Period = PeriodCodes.Week
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var errorCode = _forecastingApi.DeleteTimeSeries(Identity, dataset.Name,
                timeseries.Take(10).Select(t => t.Name).ToArray());

            Assert.IsEmpty(errorCode);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        [Ignore("Causes a flow to run. Run it if really need to test Lokad service.")]
        public void GetForecastsStatusTest()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTest" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 10,
                    Period = PeriodCodes.Week
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var forecastStatus =  _forecastingApi.GetForecastStatus(Identity, dataset.Name);

            Assert.IsTrue(String.IsNullOrEmpty(forecastStatus.ErrorCode));

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        [Ignore("Long test. Run it if really need to test Lokad service.")]
        public void GetForecastsTest()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTestF" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 10,
                    Period = "week"
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var forecastStatus = _forecastingApi.GetForecastStatus(Identity, dataset.Name);
            // wait forecast
            while (!forecastStatus.ForecastsReady)
            {
                forecastStatus = _forecastingApi.GetForecastStatus(Identity, dataset.Name);
                Debug.WriteLine("Forecasts is not ready. Waiting...");
                Thread.Sleep(5000);
            }

            var forecastCollection =
                _forecastingApi.GetForecasts(
                    Identity,
                    dataset.Name,
                    timeseries.Take(10).Select(t => t.Name).ToArray());

            Assert.IsTrue(forecastStatus.ForecastsReady);
            Assert.IsTrue(String.IsNullOrEmpty(forecastCollection.ErrorCode));
            Assert.IsTrue(forecastCollection.Series.Length > 0);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        [Ignore("Long test. Run it if really need to test Lokad service.")]
        public void GetQuantilesTest()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTestQ" + DateTime.Now.ToString("yyyyMMddHHmmssfffff"),
                    Horizon = 10,
                    Period = "week"
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var quantileStatus = _forecastingApi.GetQuantileStatus(Identity, dataset.Name);
            // wait forecast
            while (!quantileStatus.ForecastsReady)
            {
                quantileStatus = _forecastingApi.GetQuantileStatus(Identity, dataset.Name);
                Debug.WriteLine("Quantiles is not ready. Waiting...");
                Thread.Sleep(5000);
            }

            var quantileCollection =
                _forecastingApi.GetQuantiles(
                    Identity,
                    dataset.Name,
                    timeseries.Take(10).Select(t => t.Name).ToArray());

            Assert.IsTrue(quantileStatus.ForecastsReady);
            Assert.IsTrue(String.IsNullOrEmpty(quantileCollection.ErrorCode));
            Assert.IsTrue(quantileCollection.Quantiles.Length > 0);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        [Test]
        [Ignore("Long test. Run it if really need to test Lokad service.")]
        public void GetForecastsAndQuantilesTest()
        {
            // insert test dataset
            var dataset = new Dataset
                {
                    Name = "SDKIntTestFQ" + DateTime.Now.ToString("yyyyMMddHHmmssffff"),
                    Horizon = 10,
                    Period = "week"
                };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var forecastStatus = _forecastingApi.GetForecastStatus(Identity, dataset.Name);
            var quantileStatus = _forecastingApi.GetQuantileStatus(Identity, dataset.Name);
            // wait forecast
            while (!quantileStatus.ForecastsReady || !forecastStatus.ForecastsReady)
            {
                forecastStatus = _forecastingApi.GetForecastStatus(Identity, dataset.Name);
                quantileStatus = _forecastingApi.GetQuantileStatus(Identity, dataset.Name);
                Debug.WriteLine("Not ready (F={0}, Q={1}). Waiting...", forecastStatus.ForecastsReady, quantileStatus.ForecastsReady);
                Thread.Sleep(5000);
            }

            var forecastCollection =
                _forecastingApi.GetForecasts(
                    Identity,
                    dataset.Name,
                    timeseries.Take(10).Select(t => t.Name).ToArray());

            Assert.IsTrue(forecastStatus.ForecastsReady);
            Assert.IsTrue(String.IsNullOrEmpty(forecastCollection.ErrorCode));
            Assert.IsTrue(forecastCollection.Series.Length > 0);

            var quantileCollection =
                _forecastingApi.GetQuantiles(
                    Identity,
                    dataset.Name,
                    timeseries.Take(10).Select(t => t.Name).ToArray());

            Assert.IsTrue(quantileStatus.ForecastsReady);
            Assert.IsTrue(String.IsNullOrEmpty(quantileCollection.ErrorCode));
            Assert.IsTrue(quantileCollection.Quantiles.Length > 0);

            _forecastingApi.DeleteDataset(Identity, dataset.Name);
        }

        private static TimeSerie[] GetTimeSeries(int count)
        {
            var array = new TimeSerie[count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new TimeSerie
                    {
                        Name = "t" + i,
                        Values = GetTimeValues(20, i, 0.3 * i),
                        Tags = new[] { "T" + i },
                        Events = new[]
                            {
                                new EventValue
                                    {
                                        Tags = new[] { "foo" + i },
                                        KnownSince = new DateTime(2001, 1, 1).AddDays(i),
                                        Time = new DateTime(2001, 1, 1).AddDays(i)
                                    },
                            }
                    };

                if (i % 4 > 0)
                {
                    array[i].Lambda = 14f;
                    array[i].Tau = 0.95f;
                }
            }
            return array;
        }

        private static TimeValue[] GetTimeValues(int count, int dayOffset, double phaseOffset)
        {
            var array = new TimeValue[count];
            var baseDay = new DateTime(2001, 1, 1).AddDays(dayOffset);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new TimeValue { Time = baseDay.AddDays(7*i), Value = 100 * (1 + Math.Sin(phaseOffset + (0.2 * i))) };
            }
            return array;
        }
    }
}