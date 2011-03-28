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
        // identity format "auth-with-key@lokad.com:API KEY"
        private const string Identity = "";
        private const string Endpoint = "http://api.lokad.com/rest/forecasting3";

        private IForecastingApi _forecastingApi;

        [SetUp]
        public void Setup()
        {
            _forecastingApi = new ForecastingApi(Endpoint);
        }

        [Test]
        public void InsertValidDataset()
        {
            var dataset = new Dataset
                              {
                                  Name = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                                  Horizon = 60,
                                  Period = PeriodCodes.Week
                              };
            var errorCode = _forecastingApi.InsertDataset(Identity, dataset);

            Assert.IsEmpty(errorCode);
        }

        [Test]
        public void InsertDatasetWithWrongPeriod()
        {
            var dataset = new Dataset
            {
                Name = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Horizon = 60,
                Period = "millennium"
            };
            var errorCode = _forecastingApi.InsertDataset(Identity, dataset);

            Assert.AreEqual(ErrorCodes.OutOfRangeInput, errorCode);
        }

        [Test]
        public void InsertDatasetWithLongName()
        {
            var dataset = new Dataset
            {
                Name = "".PadLeft(33),
                Horizon = 60,
                Period = PeriodCodes.Week
            };
            var errorCode = _forecastingApi.InsertDataset(Identity, dataset);

            Assert.AreEqual(ErrorCodes.OutOfRangeInput, errorCode);
        }

        [Test]
        public void ListDatasetsTest()
        {
            var dataset = new Dataset
            {
                Name = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Horizon = 60,
                Period = PeriodCodes.Week
            };

            _forecastingApi.InsertDataset(Identity, dataset);

            var datasetCollection = _forecastingApi.ListDatasets(Identity, String.Empty);

            Assert.IsEmpty(datasetCollection.ErrorCode);
            Assert.IsTrue(0 < datasetCollection.Datasets.Length);
        }

        [Test]
        public void DeleteDatasetTest()
        {
            // insert test dataset
            var dataSetName = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var dataset = new Dataset
            {
                Name = dataSetName,
                Horizon = 60,
                Period = "week"
            };

            _forecastingApi.InsertDataset(Identity, dataset);

            // delete test dataset
            var errorCode =_forecastingApi.DeleteDataset(Identity, dataSetName);

            Assert.IsTrue(String.IsNullOrEmpty(errorCode));
        }

        [Test]
        public void UpsertTimeSeriesTest()
        {
            // insert test dataset
            var dataSetName = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var dataset = new Dataset
            {
                Name = dataSetName,
                Horizon = 60,
                Period = PeriodCodes.Week
            };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            var errorCode = _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            Assert.IsEmpty(errorCode);
        }

        [Test]
        public void ListTimeSeriesTest()
        {
             // insert test dataset
            var dataSetName = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var dataset = new Dataset
            {
                Name = dataSetName,
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
            var dataSetName = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var dataset = new Dataset
            {
                Name = dataSetName,
                Horizon = 60,
                Period = PeriodCodes.Week
            };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var errorCode = _forecastingApi.DeleteTimeSeries(Identity, dataset.Name,
                                                             timeseries.Take(10).Select(t => t.Name).ToArray());

            Assert.IsEmpty(errorCode);
        }

        [Test]
        public void GetForecastsStatusTest()
        {
            // insert test dataset
            var dataSetName = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var dataset = new Dataset
            {
                Name = dataSetName,
                Horizon = 10,
                Period = PeriodCodes.Week
            };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var forecastStatus =  _forecastingApi.GetForecastStatus(Identity, dataset.Name);
            
            Assert.IsTrue(String.IsNullOrEmpty(forecastStatus.ErrorCode));
        }

        [Test]
        [Ignore("Long test. Run it if really need to test Lokad service.")]
        public void GetForecastsTest()
        {
            // insert test dataset
            var dataSetName = "AYZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var dataset = new Dataset
            {
                Name = dataSetName,
                Horizon = 10,
                Period = "week"
            };

            _forecastingApi.InsertDataset(Identity, dataset);

            var timeseries = GetTimeSeries(100);

            _forecastingApi.UpsertTimeSeries(Identity, dataset.Name, timeseries, false);

            var forecastStatus =  _forecastingApi.GetForecastStatus(Identity, dataset.Name);
            // wait forecast
            while (!forecastStatus.ForecastsReady)
            {
                forecastStatus =  _forecastingApi.GetForecastStatus(Identity, dataset.Name);
                Debug.WriteLine("Forecasts is not ready. Waiting...");
                Thread.Sleep(5000);
            }

            var forecastCollection = 
                _forecastingApi.GetForecasts(Identity, 
                                             dataset.Name,
                                             timeseries.Take(10).Select(t => t.Name).ToArray());
            
            Assert.IsTrue(forecastStatus.ForecastsReady);
            Assert.IsTrue(String.IsNullOrEmpty(forecastCollection.ErrorCode));
            Assert.IsTrue(forecastCollection.Series.Length>0);
        }

        private static TimeSerie[] GetTimeSeries(int count)
        {
            var array = new TimeSerie[count];
            for (int i = 0; i < array.Length; i++)
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