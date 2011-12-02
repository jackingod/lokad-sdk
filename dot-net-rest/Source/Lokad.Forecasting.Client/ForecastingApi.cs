#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace Lokad.Forecasting.Client
{
    /// <summary>
    /// <summary>Decorator around the Forecasting API v3. REST endpoint</summary>
    /// </summary>
    public class ForecastingApi : IForecastingApi
    {
        private readonly string _endpoint;
        private readonly bool _compressRequest;

        public ForecastingApi(string endpoint, bool compressRequest = true, int timeoutMs = 100000, int readWriteTimeoutMs = 300000)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException("endpoint");
            _endpoint = endpoint;
            _compressRequest = compressRequest;
            TimeoutMs = timeoutMs;
            ReadWriteTimeoutMs = readWriteTimeoutMs;

            ServicePointManager.DefaultConnectionLimit = Math.Max(16, ServicePointManager.DefaultConnectionLimit);
            ServicePointManager.MaxServicePoints = Math.Max(16, ServicePointManager.MaxServicePoints);
            // Expect100Continue is chosen per request, so we do not set it here
        }

        public int TimeoutMs { get; set; }
        public int ReadWriteTimeoutMs { get; set; }
        public string Endpoint
        {
            get { return _endpoint; }
        }

        public string InsertDataset(string identity, Dataset dataset)
        {
            var url = String.Format(@"{0}/datasets", _endpoint);

            var document = new XElement("Dataset",
                new XElement("Name", dataset.Name),
                new XElement("Period", dataset.Period),
                new XElement("Horizon", dataset.Horizon));

            return LokadRequest.Put(identity, url, document.ToString(), _compressRequest);
        }

        public DatasetCollection ListDatasets(string identity, string continuationToken)
        {
            var url = String.Format(@"{0}/datasets/{1}", _endpoint, continuationToken);
            return LokadRequest.Get<DatasetCollection>(identity, url, TimeoutMs, ReadWriteTimeoutMs);
        }

        public string DeleteDataset(string identity, string datasetName)
        {
            var url = String.Format(@"{0}/datasets/{1}", _endpoint, datasetName);
            return LokadRequest.Delete(identity, url, TimeoutMs, ReadWriteTimeoutMs);
        }

        public string UpsertTimeSeries(string identity, string datasetName, TimeSerie[] timeSeries, bool enableMerge)
        {
            var url = _endpoint + "/series/" + datasetName + (enableMerge ? "?merge=true" : String.Empty);

            var content = new XElement("TimeSeries");
            if (timeSeries.Any())
            {
                foreach (var serie in timeSeries)
                {
                    var timeserie = new XElement("TimeSerie", new XElement("Name", serie.Name));
                    if (serie.Tags != null && serie.Tags.Any())
                    {
                        var tags = new XElement("Tags");
                        foreach (var tag in serie.Tags)
                        {
                            tags.Add(new XElement("string",tag));
                        }
                        timeserie.Add(tags);
                    }

                    if (serie.Events != null && serie.Events.Any())
                    {
                        var events = new XElement("Events");
                        foreach (var timeEvent in serie.Events)
                        {
                            var e = new XElement("EventValue",
                                                 new XElement("Time", timeEvent.Time),
                                                 new XElement("KnownSince", timeEvent.KnownSince));

                            if (!timeEvent.Tags.Any()) continue;
                            var eventTags = new XElement("Tags");
                            foreach (var tag in timeEvent.Tags)
                            {
                                eventTags.Add(new XElement("string", tag));
                            }
                            e.Add(eventTags);
                            events.Add(e);
                        }
                        timeserie.Add(events);
                    }

                    if (serie.Values.Any())
                    {
                        var values = new XElement("Values");

                        foreach (var timeValue in serie.Values)
                        {
                            values.Add(new XElement("TimeValue", 
                                                new XElement("Time", timeValue.Time),
                                                new XElement("Value", timeValue.Value)));
                        }

                        timeserie.Add(values);
                    }
                    content.Add(timeserie);
                }
            }

            return LokadRequest.Put(identity, url, content.ToString(), _compressRequest, TimeoutMs, ReadWriteTimeoutMs);
        }

        public TimeSerieCollection ListTimeSeries(string identity, string datasetName, string continuationToken)
        {
            var url = _endpoint + "/series/" + datasetName + (String.IsNullOrEmpty(continuationToken)
                          ? String.Empty
                          : "/" + continuationToken);
            return LokadRequest.Get<TimeSerieCollection>(identity, url, TimeoutMs, ReadWriteTimeoutMs);
        }

        public string DeleteTimeSeries(string identity, string datasetName, string[] serieNames)
        {
            var url = _endpoint + "/series/" + datasetName + "?n=" + String.Join(";", serieNames);
            return LokadRequest.Delete(identity, url, TimeoutMs, ReadWriteTimeoutMs);
        }

        public ForecastStatus GetForecastStatus(string identity, string datasetName)
        {
            var url = _endpoint + "/status/" + datasetName;
            return LokadRequest.Get<ForecastStatus>(identity, url, TimeoutMs, ReadWriteTimeoutMs);
        }

        public ForecastCollection GetForecasts(string identity, string datasetName, string[] serieNames)
        {
            var url = _endpoint + "/forecasts/" + datasetName + "?n=" + String.Join(";", serieNames);
            return LokadRequest.Get<ForecastCollection>(identity, url, TimeoutMs, ReadWriteTimeoutMs);
        }
    }
}