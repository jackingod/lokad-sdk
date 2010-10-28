package lokad.forecasting;

/**
 * @see IForecastingApi#GetForecasts(String, String, String[])
 */
public class ForecastSerie {
	/**
	 * Name of the time-serie being forecasted.
	 */
	public String Name;

	/**
	 * Forecasted time-values (with accuracy measurements).
	 */
	public ForecastValue[] Values;
}
