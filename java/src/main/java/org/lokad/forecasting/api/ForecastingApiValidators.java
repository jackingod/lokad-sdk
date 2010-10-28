package org.lokad.forecasting.api;

import java.util.regex.Pattern;

/**
 * Helpers for the Forecasting API.
 */
public final class ForecastingApiValidators {
	public static final Pattern DefaultNamePattern = Pattern.compile("^[a-zA-Z0-9]{1,32}$");

	private final static String[] HighPeriods = new String[] { PeriodCodes.QuarterHour, PeriodCodes.HalfHour,
			PeriodCodes.Hour };
	private final static String[] AllPeriods = new String[] { PeriodCodes.QuarterHour, PeriodCodes.HalfHour,
			PeriodCodes.Hour, PeriodCodes.Day, PeriodCodes.Week, PeriodCodes.Month };

	public static boolean IsValidApiName(String name) {
		boolean valid = true;
		valid &= name != null && !name.isEmpty();
		valid &= DefaultNamePattern.matcher(name).find();
		return valid;
	}

	private static boolean stringArrayContains(String[] array, String str) {
		for (String string : array) {
			if (string.equals(str))
				return true;
		}
		return false;
	}

	public static void Validate(Dataset dataset) {
		if (null == dataset) {
			throw new NullPointerException("dataset");
		}

		// 'Name' validation
		if (!IsValidApiName(dataset.Name)) {
			throw new IllegalArgumentException("Dataset name is not valid.");
		}

		// 'Period' validation
		if (null == dataset.Period) {
			throw new IllegalArgumentException("Dataset period cannot be null.");
		}

		if (!stringArrayContains(AllPeriods, dataset.Period)) {
			throw new IllegalArgumentException("Dataset period is not valid.");
		}

		// 'Horizon' validation
		if (stringArrayContains(HighPeriods, dataset.Period)) {
			// high-frequency horizons
			if (dataset.Horizon <= 0 || dataset.Horizon > 10000) {
				throw new IllegalArgumentException("Horizon should be comprised between 1 and 10000.");
			}
		} else {
			// low frequency horizons
			if (dataset.Horizon <= 0 || dataset.Horizon > 100) {
				throw new IllegalArgumentException("Horizon should be comprised between 1 and 100.");
			}
		}
	}

	public static void Validate(TimeSerie timeSerie) {
		if (null == timeSerie) {
			throw new NullPointerException("timeSerie");
		}

		// 'Name' validation
		if (!IsValidApiName(timeSerie.Name)) {
			throw new IllegalArgumentException("TimeSerie name is not valid.");
		}

		// 'Tags' validation
		if (timeSerie.Tags != null) {
			if (timeSerie.Tags.length > 100) {
				throw new IllegalArgumentException("No more than 100 tags per serie.");
			}

			for (String tag : timeSerie.Tags) {
				if (!IsValidApiName(tag))
					throw new IllegalArgumentException(String.format("%s is not a valid tag.", tag));
			}

			for (int i = 0; i < timeSerie.Tags.length - 1; i++) {
				for (int j = i + 1; j < timeSerie.Tags.length; j++) {
					if (timeSerie.Tags[i].equals(timeSerie.Tags[j])) {
						throw new IllegalArgumentException("All tags should be distinct within a TimeSerie.");
					}
				}
			}
		}

		// 'Events' validation
		if (timeSerie.Events != null) {
			if (timeSerie.Events.length > 100) {
				throw new IllegalArgumentException("No more than 100 events per serie.");
			}

			for (EventValue e : timeSerie.Events) {
				if (e.Tags.length == 0 || e.Tags.length > 100) {
					throw new IllegalArgumentException("There should be 1 to 100 tags per event.");
				}

				for (String tag : e.Tags) {
					if (!IsValidApiName(tag))
						throw new IllegalArgumentException(String.format("%s is not a valid tag.", tag));
				}

				for (int i = 0; i < e.Tags.length - 1; i++) {
					for (int j = i + 1; j < e.Tags.length; j++) {
						if (timeSerie.Tags[i].equals(timeSerie.Tags[j])) {
							throw new IllegalArgumentException("All tags should be distinct within a TimeSerie.");
						}
					}
				}
			}
		}

		// 'Values' validation
		if (timeSerie.Values != null) {
			if (timeSerie.Values.length > 65536) {
				throw new IllegalArgumentException("Maximal number of time-values is 64k.");
			}

			// Time-values should be strictly ordered
			for (int i = 0; i < timeSerie.Values.length - 1; i++) {
				if (timeSerie.Values[i + 1].Time.compareTo(timeSerie.Values[i].Time) <= 0) {
					throw new IllegalArgumentException("Time-serie is not properly ordered.");
				}
			}
		}
	}
}
