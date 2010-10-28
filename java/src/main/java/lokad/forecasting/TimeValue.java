package lokad.forecasting;

import java.util.Calendar;

/**
 * @see IForecastingApi#UpsertTimeSeries(String, String, TimeSerie[], Boolean)
 */
public class TimeValue {
	/**
	 * Time.
	 */
	public Calendar Time;

	/**
	 * Value.
	 */
	public double Value;

	@Override
	public String toString() {
		return String.format("(%s, %f)", Time, Value);
	}
}
