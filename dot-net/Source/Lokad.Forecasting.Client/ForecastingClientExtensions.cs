#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System.Linq;

namespace Lokad.Forecasting.Client
{
    /// <summary>Helpers for the <see cref="ForecastingClient"/>.</summary>
    public static class ForecastingClientExtensions
    {
        /// <summary>Upsert a single time-series.</summary>
        /// <remarks>Merge is set to <c>false</c>.</remarks>
        public static void UpsertTimeSeries(
            this ForecastingClient client, string datasetName, TimeSerie timeSerie)
        {
            client.UpsertTimeSeries(datasetName, new[] {timeSerie}, false);
        }

        /// <summary>Upsert a single time-series.</summary>
        /// <remarks>Merge is set to <c>false</c>.</remarks>
        public static void UpsertTimeSeries(
            this ForecastingClient client, Dataset dataset, TimeSerie timeSerie)
        {
            client.UpsertTimeSeries(dataset.Name, new[] { timeSerie }, false);
        }

        /// <summary>Upsert multiple time-series.</summary>
        public static void UpsertTimeSeries(
            this ForecastingClient client, Dataset dataset, TimeSerie[] timeSeries, bool merge)
        {
            client.UpsertTimeSeries(dataset.Name, timeSeries, merge);
        }

        /// <summary>Gets a single forecasted series.</summary>
        public static ForecastSerie GetForecast(
            this ForecastingClient client, string datasetName, TimeSerie timeSerie)
        {
            return client.GetForecasts(datasetName, new[] { timeSerie.Name }).FirstOrDefault();
        }

        /// <summary>Gets a single forecasted series.</summary>
        public static ForecastSerie GetForecast(
            this ForecastingClient client, Dataset dataset, TimeSerie timeSerie)
        {
            return client.GetForecasts(dataset.Name, new []{timeSerie.Name}).FirstOrDefault();
        }

        /// <summary>Get forecasts.</summary>
        public static ForecastSerie[] GetForecasts(
            this ForecastingClient client, string datasetName, TimeSerie[] timeSeries)
        {
            return client.GetForecasts(datasetName, timeSeries.Select(ts => ts.Name).ToArray());
        }

        /// <summary>Get forecasts.</summary>
        public static ForecastSerie[] GetForecasts(
            this ForecastingClient client, Dataset dataset, TimeSerie[] timeSeries)
        {
            return client.GetForecasts(dataset.Name, timeSeries.Select(ts => ts.Name).ToArray());
        }
    }
}
