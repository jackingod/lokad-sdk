package org.lokad.forecasting.api;

/**
 * Gather error codes as returned by Forecasting API v3.
 */
public class ErrorCodes {
	/**
	 * Invalid authentication key.
	 */
	public final static String AuthenticationFailed = "AuthenticationFailed";

	/**
	 * Strings are expected to be in <code>^[a-zA-Z0-9]{1,32}$</code>, and
	 * arrays comes with length constraints too.
	 */
	public final static String OutOfRangeInput = "OutOfRangeInput";

	/**
	 * Target dataset cannot be found.
	 */
	public final static String DatasetNotFound = "DatasetNotFound";

	/**
	 * Target dataset is not in an appropriate state to be the target of the
	 * specified operation. If dataset is being deleted you should wait until
	 * completion.
	 */
	public final static String InvalidDatasetState = "InvalidDatasetState";

	/**
	 * Transient failure on the Lokad side. We suggest to retry later on.
	 */
	public final static String ServiceFailure = "ServiceFailure";
}