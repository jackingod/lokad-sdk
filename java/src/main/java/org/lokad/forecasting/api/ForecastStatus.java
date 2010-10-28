package org.lokad.forecasting.api;

/**
 * @see IForecastingApi#GetForecastStatus(String, String)
 */
public class ForecastStatus {

	public boolean ForecastsReady;

	/**
	 * @see {@link ErrorCodes}
	 */
	public String ErrorCode;
}
