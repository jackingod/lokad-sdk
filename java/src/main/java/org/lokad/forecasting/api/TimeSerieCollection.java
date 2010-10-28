package org.lokad.forecasting.api;

/**
 * @see IForecastingApi#UpsertTimeSeries(String, String, TimeSerie[], Boolean)
 */
public class TimeSerieCollection {
	public TimeSerie[] TimeSeries;

	public String ContinuationToken;

	public String ErrorCode;
}
