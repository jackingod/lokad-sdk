package lokad.forecasting;

/**
 * @see IForecastingApi#ListDatasets(String, String)
 */
public class DatasetCollection
{
	public Dataset[] Datasets;

	public String ContinuationToken;

	public String ErrorCode;
}
