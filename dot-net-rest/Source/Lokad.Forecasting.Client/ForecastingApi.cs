#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.Linq;
using System.Xml.Linq;

namespace Lokad.Forecasting.Client
{
    public class ForecastingApi : IForecastingApi
    {
        private readonly string _endpoint;

        public ForecastingApi(string endpoint)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException("endpoint");
            _endpoint = endpoint;
        }

        public string InsertDataset(string identity, Dataset dataset)
        {
            var url = String.Format(@"{0}/datasets", _endpoint);

            var document = new XElement("Dataset",
                new XElement("Name", dataset.Name),
                new XElement("Period", dataset.Period),
                new XElement("Horizon", dataset.Horizon));

            var request = LokadRequest.Create(identity);

            return request.GetResponse<string>(url, document.ToString(), HttpMethod.Put);
        }

        public DatasetCollection ListDatasets(string identity, string continuationToken)
        {
            var url = String.Format(@"{0}/datasets/{1}", _endpoint, continuationToken);
            var request = LokadRequest.Create(identity);

            return request.GetResponse<DatasetCollection>(url, String.Empty, HttpMethod.Get);
        }

        public string DeleteDataset(string identity, string datasetName)
        {
            var url = String.Format(@"{0}/datasets/{1}", _endpoint, datasetName);
            var request = LokadRequest.Create(identity);

            return request.GetResponse<string>(url, String.Empty, HttpMethod.Delete);
        }

        public string UpsertTimeSeries(string identity, string datasetName, TimeSerie[] timeSeries, bool enableMerge)
        {
            var url = _endpoint + "/series/" + datasetName + (enableMerge ? "?merge=true" : String.Empty);

            var request = LokadRequest.Create(identity);

            var content1 = new XElement("TimeSeries");
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
                    }

                    if (serie.Events != null && serie.Events.Any())
                    {
                        var events = new XElement("Events");
                        foreach (var timeEvent in serie.Events)
                        {
                            var e = new XElement("Event",
                                                 new XElement("Time", timeEvent.Time),
                                                 new XElement("KnownSince", timeEvent.KnownSince));

                            if (!timeEvent.Tags.Any()) continue;
                            var eventTags = new XElement("Tags");
                            foreach (var tag in timeEvent.Tags)
                            {
                                e.Add(new XElement("string",tag));
                            }
                            e.Add(eventTags);
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
                    content1.Add(timeserie);
                }
            }

            return request.GetResponse<string>(url, content1.ToString(), HttpMethod.Put);
        }

        public TimeSerieCollection ListTimeSeries(string identity, string datasetName, string continuationToken)
        {
            var url = _endpoint + "/series/" + datasetName + (String.IsNullOrEmpty(continuationToken)
                          ? String.Empty
                          : "/" + continuationToken);
            var request = LokadRequest.Create(identity);

            return request.GetResponse<TimeSerieCollection>(url, String.Empty, HttpMethod.Get);
        }

        public string DeleteTimeSeries(string identity, string datasetName, string[] serieNames)
        {
            var url = _endpoint + "/series/" + datasetName + "?n=" + String.Join(";", serieNames);
            var request = LokadRequest.Create(identity);

            return request.GetResponse<string>(url, String.Empty, HttpMethod.Delete);
        }

        public ForecastStatus GetForecastStatus(string identity, string datasetName)
        {
            var url = _endpoint + "/status/" + datasetName;
            var request = LokadRequest.Create(identity);

            return request.GetResponse<ForecastStatus>(url, String.Empty, HttpMethod.Get);
        }

        public ForecastCollection GetForecasts(string identity, string datasetName, string[] serieNames)
        {
            var url = _endpoint + "/forecasts/" + datasetName + "?n=" + String.Join(";", serieNames);
            var request = LokadRequest.Create(identity);

            return request.GetResponse<ForecastCollection>(url, String.Empty, HttpMethod.Get);
        }

        
    }
}