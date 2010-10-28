package lokad.forecasting;

/**
 * @see IForecastingApi#UpsertTimeSeries(String, String, TimeSerie[], Boolean)
 */
public class TimeSerie {
	public String Name;

	public String[] Tags;

	public EventValue[] Events;

	public TimeValue[] Values;
}
