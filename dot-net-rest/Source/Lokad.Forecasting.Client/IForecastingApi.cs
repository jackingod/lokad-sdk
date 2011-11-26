#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System;

namespace Lokad.Forecasting.Client
{
    /// <summary>Helper for Forecasting API v3.</summary>
    public static class Constants
    {
        /// <summary>Root namespace.</summary>
        public const string Namespace = "http://schemas.lokad.com/";

        /// <summary>Compound methods of Forecasting API are nearly
        /// all upper bounded to collection of 100 items at most.</summary>
        public const int SeriesSliceLength = 100;
    }

    /// <summary>Forecasting API v3.</summary>
    public interface IForecastingApi
    {
        /// <summary>
        /// Insert a dataset in a Lokad account. Once inserted, datasets
        /// are immutable. They cannot be updated, but they can be deleted
        /// and re-inserted.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="dataset">
        /// Dataset name must match the regex <c>[A-Za-z0-9]{1,32}</c>.
        /// </param>
        /// <returns>
        /// An error code if the call fails. Otherwise, the returned value will be
        /// null or empty. The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </returns>
        /// <remarks>
        /// <para>Datasets represent the elementary data containers offered by Lokad
        /// to store the historical data. A dataset defines also the forecasts
        /// to be performed on the data through the <c>Period</c> representing
        /// the aggregation level of the forecasts and the <c>Horizon</c>, the
        /// number of periods to be forecasted ahead.
        /// </para>
        /// <para>Datasets are identified according to their names.
        /// If a given name is not associated to any existing dataset,
        /// then the dataset get inserted, and ignored otherwise.
        /// A Lokad account can contain an arbitrarily large number of
        /// datasets.
        /// </para>
        /// <para>
        /// The accepted values for the <c>Period</c> are (lower-case): 
        /// <ul>
        ///   <li>quarterhour</li>
        ///	  <li>halfhour</li>
        ///   <li>hour</li>
        ///   <li>day</li>
        ///   <li>week</li>
        ///   <li>month</li>
        /// </ul>
        /// </para>
        /// <para>The horizon is a value comprised between 1 and 100 for
        /// dayly, weekly and monthly forecasts. In case of hourly, half-hourly
        /// and quarter-hourly forecasts, the horizon should is a value
        /// comprised between 1 and 10000.
        /// </para>
        /// </remarks>
        string InsertDataset(string identity, Dataset dataset);

        /// <summary>
        /// Incremental enumeration of the datasets contained in a Lokad account.
        /// Datasets being deleted are not included in this enumeration.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="continuationToken">Optional continuation token. If null 
        /// or empty, the enumeration starts from the beginning.</param>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// <para>A collection of datasets. If <c>DatasetCollection.ContinuationToken</c>
        /// is not null or empty, then this method should be called again passing
        /// the continuation token as argument to retrieve the other datasets
        /// contained in the account.
        /// </para>
        /// <para>The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </para>
        /// </returns>
        DatasetCollection ListDatasets(string identity, string continuationToken);

        /// <summary>
        /// Asynchronous deletion of a dataset contained in a Lokad account.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="datasetName">Names of the dataset being deleted.</param>
        /// <remarks>
        /// Deletion is not immediate. It might take a few minutes before the dataset 
        /// is deleted and the corresponding name available again.
        /// </remarks>
        /// <returns>
        /// An error code if the call fails. Otherwise, the returned value will be
        /// null or empty. The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </returns>
        string DeleteDataset(string identity, string datasetName);

        /// <summary>
        /// Update or insert decorated time-series in a dataset
        /// attached to a Lokad account.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="datasetName">Targeted dataset.</param>
        /// <param name="timeSeries">
        /// Set of time-series. No more than 100 time-series can
        /// be passed as argument. The web request as a whole cannot
        /// weight more than 4MB.
        /// </param>
        /// <param name="enableMerge">
        /// If <c>true</c>, the input time-series are merged
        /// with the existing ones (according the merge rules).
        /// If <c>false</c>, the existing time-series are overwritten
        /// by the values passed as input.
        /// </param>
        /// <remarks>
        /// <para>Further constraints apply the time-series content. 
        /// <ul>
        ///   <li>The maximal number of tags per serie or per event is 100.</li>
        ///   <li>The maximal number of events per serie is 100.</li>
        ///   <li>The maximal number of time-values per serie is 100,000.</li>
        /// </ul>
        /// Then, for each time-serie, all tags are expected to be distinct if any.
        /// For each event, all tags are expected to be distinct too. Each event
        /// needs to be associated to at least one tag.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An error code if the call fails. Otherwise, the returned value will be
        /// null or empty. The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>DatasetNotFound</li>
        ///   <li>InvalidDatasetState</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </returns>
        string UpsertTimeSeries(string identity, string datasetName, TimeSerie[] timeSeries, bool enableMerge);

        /// <summary>
        /// Incremental enumeration of the time-series contained
        /// in a Lokad account.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="datasetName">Targeted dataset.</param>
        /// <param name="continuationToken">Optional continuation token. If null 
        /// or empty, the enumeration starts from the beginning.</param>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// <para>A collection of time-series. If <c>TimeSerieCollection.ContinuationToken</c>
        /// is not null or empty, then this method should be called again passing
        /// the continuation token as argument to retrieve the other datasets
        /// contained in the account.
        /// </para>
        /// <para>The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>DatasetNotFound</li>
        ///   <li>InvalidDatasetState</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </para>
        /// </returns>
        TimeSerieCollection ListTimeSeries(string identity, string datasetName, string continuationToken);

        /// <summary>
        /// Delete time-series from the specified datasets in a Lokad account.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="datasetName">Targeted dataset.</param>
        /// <param name="serieNames">
        /// Series to be deleted. No more than 100 series
        /// could be deleted at once.
        /// </param>
        /// <remarks>
        /// </remarks>
        /// <returns>
        /// An error code if the call fails. Otherwise, the returned value will be 
        /// null or empty. The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>DatasetNotFound</li>
        ///   <li>InvalidDatasetState</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </returns>
        string DeleteTimeSeries(string identity, string datasetName, string[] serieNames);

        /// <summary>
        /// Call this method once, just after finishing uploading
        /// you updated series. The first call to this method triggers the 
        /// forecast computation. Then, routinely call this method every
        /// minute or so to detect when the forecasts are available
        /// to be retrieved.
        /// </summary>
        /// <param name="identity">Authentication keys.</param>
        /// <param name="datasetName">Targeted dataset.</param>
        /// <remarks>
        /// <para>
        /// The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>DatasetNotFound</li>
        ///   <li>InvalidDatasetState</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </para>
        /// </remarks>
        /// <returns>
        /// The status of the forecast computation. When forecasts
        /// are indicated as ready, the method <c>GetForecasts</c>
        /// can be called.
        /// 
        /// The following error codes can be returned (if any) through
        /// the value of <c>ForecastStatus.ErrorCode</c>:
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>DatasetNotFound</li>
        ///   <li>InvalidDatasetState</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </returns>
        ForecastStatus GetForecastStatus(string identity, string datasetName);

        /// <summary>
        /// Retrieves the forecasts from a dataset of a Lokad account.
        /// </summary>
        /// <param name="identity">Authentication key.</param>
        /// <param name="datasetName">Targeted dataset.</param>
        /// <param name="serieNames">
        /// Name of the forecasted series. No more than 100 forecasted
        /// series could be retrieved at once. If a serie cannot be found,
        /// it will be ignored, and no corresponding forecast will
        /// be returned.
        /// </param>
        /// <remarks>
        /// <para>The size of the response should not exceed 4MB, otherwise
        /// the call will fail. If response exceed 4MB, we suggest to retrieve
        /// fewer series at once.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A collection of forecasts matching the serie names passed
        /// as argument. The following error codes can be returned (if any):
        /// <ul>
        ///   <li>AuthenticationFailed</li>
        ///   <li>OutOfRangeInput</li>
        ///   <li>DatasetNotFound</li>
        ///   <li>InvalidDatasetState</li>
        ///   <li>ServiceFailure</li>
        /// </ul>
        /// </returns>
        ForecastCollection GetForecasts(string identity, string datasetName, string[] serieNames);

		/// <summary>
		/// Endpoint URL to which this API is connected
		/// </summary>
    	string Endpoint { get; }
    }

    /// <summary>Gather error codes as returned by Forecasting API v3.</summary>
    public static class ErrorCodes
    {
        /// <summary>Invalid authentication key.</summary>
        public const string AuthenticationFailed = "AuthenticationFailed";

        /// <summary>Strings are expected to be in <c>^[a-zA-Z0-9]{1,32}$</c>,
        /// and arrays comes with length constraints too.</summary>
        public const string OutOfRangeInput = "OutOfRangeInput";

        /// <summary>Target dataset cannot be found.</summary>
        public const string DatasetNotFound = "DatasetNotFound";

        /// <summary>Target dataset is not in an appropriate state to
        /// be the target of the specified operation. If dataset is 
        /// being deleted you should wait until completion.</summary>
        public const string InvalidDatasetState = "InvalidDatasetState";

        /// <summary>Transient failure on the Lokad side. We suggest to
        /// retry later on.</summary>
        public const string ServiceFailure = "ServiceFailure";
    }

    /// <summary>Gather period codes as supported by Forecasting API v3</summary>
    public static class PeriodCodes
    {
        /// <summary>Quarter-hour</summary>
        public const string QuarterHour = "quarterhour";

        /// <summary>Half-hour</summary>
        public const string HalfHour = "halfhour";

        /// <summary>Hour</summary>
        public const string Hour = "hour";

        /// <summary>Day</summary>
        public const string Day = "day";

        /// <summary>Week</summary>
        public const string Week = "week";

        /// <summary>Month</summary>
        public const string Month = "month";
    }

    /// <seealso cref="IForecastingApi.InsertDataset"/>
    public class Dataset
    {
        /// <remarks></remarks>
        public string Name { get; set; }

        /// <remarks></remarks>
        public string Period { get; set; }

        /// <remarks></remarks>
        public int Horizon { get; set; }
    }

    /// <seealso cref="IForecastingApi.ListDatasets"/>
    public class DatasetCollection
    {
        /// <remarks></remarks>
        public Dataset[] Datasets { get; set; }

        /// <remarks></remarks>
        public string ContinuationToken { get; set; }

        /// <remarks></remarks>
        public string ErrorCode { get; set; }
    }

    /// <seealso cref="IForecastingApi.UpsertTimeSeries"/>
    public class TimeSerie
    {
        /// <remarks></remarks>
        public string Name { get; set; }

        /// <remarks></remarks>
        public string[] Tags { get; set; }

        /// <remarks></remarks>
        public EventValue[] Events { get; set; }

        /// <remarks></remarks>
        public TimeValue[] Values { get; set; }
    }

    /// <seealso cref="IForecastingApi.UpsertTimeSeries"/>
    public class TimeSerieCollection
    {
        /// <remarks></remarks>
        public TimeSerie[] TimeSeries { get; set; }

        /// <remarks></remarks>
        public string ContinuationToken { get; set; }

        /// <see cref="ErrorCodes"/>
        public string ErrorCode { get; set; }
    }

    /// <seealso cref="IForecastingApi.UpsertTimeSeries"/>
    public class TimeValue
    {
        /// <summary>Time.</summary>
        public DateTime Time { get; set; }

        /// <summary>Value.</summary>
        public double Value { get; set; }

        public override string ToString()
        {
            return string.Format("({0}, {1})", Time, Value);
        }
    }

    /// <seealso cref="IForecastingApi.UpsertTimeSeries"/>
    public class EventValue
    {
        /// <remarks></remarks>
        public string[] Tags { get; set; }

        /// <summary>Starting date of the event.</summary>
        public DateTime Time { get; set; }

        /// <summary>Original discovery date of the event.</summary>
        public DateTime KnownSince { get; set; }

        public override string ToString()
        {
            return string.Format("Tags: {0}, Time: {1}, KnownSince: {2}", string.Join(",", Tags), Time, KnownSince);
        }
    }

    /// <seealso cref="IForecastingApi.GetForecastStatus"/>
    public class ForecastStatus
    {
        /// <remarks></remarks>
        public bool ForecastsReady { get; set; }

        /// <see cref="ErrorCodes"/>
        public string ErrorCode { get; set; }
    }

    /// <seealso cref="IForecastingApi.GetForecasts"/>
    public class ForecastCollection
    {
        /// <remarks></remarks>
        public ForecastSerie[] Series { get; set; }

        /// <see cref="ErrorCodes"/>
        public string ErrorCode { get; set; }
    }

    /// <seealso cref="IForecastingApi.GetForecasts"/>
    public class ForecastSerie
    {
        /// <summary>Name of the time-serie being forecasted.</summary>
        public string Name { get; set; }

        /// <summary>Forecasted time-values (with accuracy measurements). </summary>
        public ForecastValue[] Values { get; set; }
    }

    /// <seealso cref="IForecastingApi.GetForecasts"/>
    public class ForecastValue
    {
        /// <summary>Time.</summary>
        public DateTime Time { get; set; }

        /// <summary>Value.</summary>
        public double Value { get; set; }

        /// <summary>Percentage expressed as a value between 0 and 1.</summary>
        public double Accuracy { get; set; }
    }
}
