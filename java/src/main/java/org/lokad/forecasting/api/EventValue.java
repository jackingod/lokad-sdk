package org.lokad.forecasting.api;

import java.util.Calendar;

/**
 * @see IForecastingApi#UpsertTimeSeries(String, String, TimeSerie[], Boolean)
 */
public class EventValue {
	public String[] Tags;

	/**
	 * Starting date of the event.
	 */
	public Calendar Time;

	/**
	 * Original discovery date of the event.
	 */
	public Calendar KnownSince;
}
