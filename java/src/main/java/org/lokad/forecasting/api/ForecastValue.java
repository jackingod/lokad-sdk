package org.lokad.forecasting.api;

import java.util.Calendar;

/**
 * @see IForecastingApi#GetForecasts(String, String, String[])
 */
public class ForecastValue {
	/**
	 * Time.
	 */
	public Calendar Time;

	/**
	 * Value.
	 */
	public double Value;

	/**
	 * Percentage expressed as a value between 0 and 1.
	 */
	public double Accuracy;
}
