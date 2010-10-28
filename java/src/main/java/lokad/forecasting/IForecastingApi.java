package lokad.forecasting;

/**
 * Forecasting API v3.
 */
public interface IForecastingApi {
	/**
	 * Insert a dataset in a Lokad account. Once inserted, datasets are
	 * immutable. They cannot be updated, but they can be deleted and
	 * re-inserted.
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param dataset
	 *            Dataset name must match the regex
	 *            <code>[A-Za-z0-9]{1,32}</code>.
	 *            <p>
	 *            Datasets represent the elementary data containers offered by
	 *            Lokad to store the historical data. A dataset defines also the
	 *            forecasts to be performed on the data through the
	 *            <code>Period</code> representing the agregation level of the
	 *            forecasts and the <code>Horizon</code>, the number of periods
	 *            to be forecasted ahead.
	 *            </p>
	 *            <p>
	 *            Datasets are identified according to their names. If a given
	 *            name is not associated to any existing dataset, then the
	 *            dataset get inserted, and ignored otherwise. A Lokad account
	 *            can contain an arbitrarily large number of datasets.
	 *            </p>
	 *            <p>
	 *            The accepted values for the <code>Period</code> are
	 *            (lower-case):
	 *            <ul>
	 *            <li>quarterhour</li>
	 *            <li>halfhour</li>
	 *            <li>hour</li>
	 *            <li>day</li>
	 *            <li>week</li>
	 *            <li>month</li>
	 *            </ul>
	 *            </p>
	 *            <p>
	 *            The horizon is a value comprised between 1 and 100 for dayly,
	 *            weekly and monthly forecasts. In case of hourly, half-hourly
	 *            and quarter-hourly forecasts, the horizon should is a value
	 *            comprised between 1 and 10000.
	 *            </p>
	 * @return An error code if the call fails. Otherwise, the returned value
	 *         will be null or empty. The following error codes can be returned
	 *         (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 */
	String InsertDataset(String identity, Dataset dataset);

	/**
	 * Incremental enumeration of the datasets contained in a Lokad account.
	 * Datasets being deleted are not included in this enumeration.
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param continuationToken
	 *            Optional continuation token. If null or empty, the enumeration
	 *            starts from the beginning.
	 * @return <p>
	 *         A collection of datasets. If
	 *         <code>DatasetCollection.ContinuationToken</code> is not null or
	 *         empty, then this method should be called again passing the
	 *         continuation token as argument to retrieve the other datasets
	 *         contained in the account.
	 *         </p>
	 *         <p>
	 *         The following error codes can be returned (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 *         </p>
	 */
	DatasetCollection ListDatasets(String identity, String continuationToken);

	/**
	 * Asynchronous deletion of a dataset contained in a Lokad account.
	 * <p>
	 * Deletion is not immediate. It might take a few minutes before the dataset
	 * is deleted and the corresponding name available again.
	 * </p>
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param datasetName
	 *            Names of the dataset being deleted.
	 * @return An error code if the call fails. Otherwise, the returned value
	 *         will be null or empty. The following error codes can be returned
	 *         (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 */
	String DeleteDataset(String identity, String datasetName);

	/**
	 * Update or insert decorated time-series in a dataset attached to a Lokad
	 * account.
	 * <p>
	 * Further constraints apply the time-series content.
	 * <ul>
	 * <li>The maximal number of tags per serie or per event is 100.</li>
	 * <li>The maximal number of events per serie is 100.</li>
	 * <li>The maximal number of time-values per serie is 100,000.</li>
	 * </ul>
	 * Then, for each time-serie, all tags are expected to be distinct if any.
	 * For each event, all tags are expeceted to be distinct too. Each event
	 * needs to be associated to at least one tag.
	 * </p>
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param datasetName
	 *            Targeted dataset.
	 * @param timeSeries
	 *            Set of time-series. No more than 100 time-series can be passed
	 *            as argument. The web request as a whole cannot weight more
	 *            than 4MB.
	 * @param enableMerge
	 *            If <code>true</code>, the input time-series are merged with
	 *            the existing ones (according the merge rules). If
	 *            <code>false</code>, the existing time-series are overwritten
	 *            by the values passed as input.
	 * @return An error code if the call fails. Otherwise, the returned value
	 *         will be null or empty. The following error codes can be returned
	 *         (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>DatasetNotFound</li>
	 *         <li>InvalidDatasetState</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 */
	String UpsertTimeSeries(String identity, String datasetName, TimeSerie[] timeSeries, Boolean enableMerge);

	/**
	 * Incremental enumeration of the time-series contained in a Lokad account.
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param datasetName
	 *            Targeted dataset.
	 * @param continuationToken
	 *            Optional continuation token. If null or empty, the enumeration
	 *            starts from the beginning.
	 * @return <p>
	 *         A collection of time-series. If
	 *         <code>TimeSerieCollection.ContinuationToken</code> is not null or
	 *         empty, then this method should be called again passing the
	 *         continuation token as argument to retrieve the other datasets
	 *         contained in the account.
	 *         </p>
	 *         <p>
	 *         The following error codes can be returned (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>DatasetNotFound</li>
	 *         <li>InvalidDatasetState</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 *         </p>
	 */
	TimeSerieCollection ListTimeSeries(String identity, String datasetName, String continuationToken);

	/**
	 * Delete time-series from the specified datasets in a Lokad account.
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param datasetName
	 *            Targeted dataset.
	 * @param serieNames
	 *            Series to be deleted. No more than 100 series could be deleted
	 *            at once.
	 * @return An error code if the call fails. Otherwise, the returned value
	 *         will be null or empty. The following error codes can be returned
	 *         (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>DatasetNotFound</li>
	 *         <li>InvalidDatasetState</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 */
	String DeleteTimeSeries(String identity, String datasetName, String[] serieNames);

	/**
	 * Call this method once, just after finishing uploading you updated series.
	 * The first call to this method triggers the forecast computation. Then,
	 * routinely call this method every minute or so to detect when the
	 * forecasts are available to be retrieved.
	 * 
	 * @param identity
	 *            Authentication keys.
	 * @param datasetName
	 *            Targeted dataset.
	 * @return The status of the forecast computation. When forecasts are
	 *         indicated as ready, the method <code>GetForecasts</code> can be
	 *         called. <b> The following error codes can be returned (if any)
	 *         through the value of <code>ForecastStatus.ErrorCode</code>:
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>DatasetNotFound</li>
	 *         <li>InvalidDatasetState</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 */
	ForecastStatus GetForecastStatus(String identity, String datasetName);

	/**
	 * Retrieves the forecasts from a dataset of a Lokad account.
	 * <p>
	 * The size of the response should not exceed 4MB, otherwise the call will
	 * fail. If response exceed 4MB, we suggest to retrieve fewer series at
	 * once.
	 * </p>
	 * 
	 * @param identity
	 *            Authentication key.
	 * @param datasetName
	 *            Targeted dataset.
	 * @param serieNames
	 *            Name of the forecasted series. No more than 100 forecasted
	 *            series could be retrieved at once. If a serie cannot be found,
	 *            it will be ignored, and no corresponding forecast will be
	 *            returned.
	 * @return A collection of forecasts matching the serie names passed as
	 *         argument. The following error codes can be returned (if any):
	 *         <ul>
	 *         <li>AuthenticationFailed</li>
	 *         <li>OutOfRangeInput</li>
	 *         <li>DatasetNotFound</li>
	 *         <li>InvalidDatasetState</li>
	 *         <li>ServiceFailure</li>
	 *         </ul>
	 */
	ForecastCollection GetForecasts(String identity, String datasetName, String[] serieNames);
}
