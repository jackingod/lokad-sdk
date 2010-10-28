package org.lokad.forecasting.api;

/**
 * Helper for Forecasting API v3.
 */
public final class Constants {
	/**
	 * Root namespace.
	 */
	public final static String Namespace = "http://schemas.lokad.com/";

	/**
	 * Compound methods of Forecasting API are nearly all upper bounded to
	 * collection of 100 items at most.
	 */
	public final static int SeriesSliceLength = 100;
}
