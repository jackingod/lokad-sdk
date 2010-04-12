#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lokad.Forecasting.Client
{
	/// <summary>Helpers for the Forecasting API.</summary>
	public static class ForecastingApiValidators
	{
		const string DefaultNamePattern = "^[a-zA-Z0-9]{1,32}$";

		readonly static string[] HighPeriods = new[] {PeriodCodes.QuarterHour, PeriodCodes.HalfHour, PeriodCodes.Hour};
		readonly static string[] AllPeriods = new[] { PeriodCodes.QuarterHour, PeriodCodes.HalfHour, PeriodCodes.Hour,
													  PeriodCodes.Day, PeriodCodes.Week, PeriodCodes.Month};

		public static bool IsValidApiName(this string name)
		{
			return !string.IsNullOrEmpty(name) && Regex.Match(name, DefaultNamePattern).Success;
		}

		public static void Validate(this Dataset dataset)
		{
			if(null == dataset)
			{
				throw new ArgumentNullException("dataset");
			}

			// 'Name' validation
			if(!dataset.Name.IsValidApiName())
			{
				throw new ArgumentException("Dataset name is not valid.", "dataset");
			}

			// 'Period' validation
			if(null == dataset.Period)
			{
				throw new ArgumentException("Dataset period cannot be null.", "dataset");
			}

			if(!AllPeriods.Contains(dataset.Period))
			{
				throw new ArgumentException("Dataset period is not valid.", "dataset");
			}

			// 'Horizon' validation
			if((HighPeriods).Contains(dataset.Period))
			{
				// high-frequency horizons
				if(dataset.Horizon <= 0 || dataset.Horizon > 10000)
				{
					throw new ArgumentOutOfRangeException("dataset", "Horizon should be comprised between 1 and 10000.");
				}
			}
			else
			{
				// low frequency horizons
				if (dataset.Horizon <= 0 || dataset.Horizon > 100)
				{
					throw new ArgumentOutOfRangeException("dataset", "Horizon should be comprised between 1 and 100.");
				}
			}
		}

		public static void Validate(this TimeSerie timeSerie)
		{
			if(null == timeSerie)
			{
				throw new ArgumentNullException("timeSerie");
			}

			// 'Name' validation
			if (!timeSerie.Name.IsValidApiName())
			{
				throw new ArgumentException("TimeSerie name is not valid.", "timeSerie");
			}

			// 'Tags' validation
			if(timeSerie.Tags != null)
			{
				if(timeSerie.Tags.Length > 100)
				{
					throw new ArgumentException("No more than 100 tags per serie.", "timeSerie");
				}

				foreach(var tag in timeSerie.Tags.Where(t => !t.IsValidApiName()))
				{
					throw new ArgumentException(String.Format("{0} is not a valid tag.", tag ?? "Null"), "timeSerie");
				}

				if(timeSerie.Tags.Distinct().Count() < timeSerie.Tags.Length)
				{
					throw new ArgumentException("All tags should be distinct within a TimeSerie.", "timeSerie");
				}
			}

			// 'Events' validation
			if(timeSerie.Events != null)
			{
				if (timeSerie.Events.Length > 100)
				{
					throw new ArgumentException("No more than 100 events per serie.", "timeSerie");
				}

				foreach(var e in timeSerie.Events)
				{
					if (e.Tags.Length == 0 || e.Tags.Length > 100)
					{
						throw new ArgumentException("There should be 1 to 100 tags per event.", "timeSerie");
					}

					foreach (var tag in e.Tags.Where(t => !t.IsValidApiName()))
					{
						throw new ArgumentException(String.Format("{0} is not a valid tag.", tag ?? "Null"), "timeSerie");
					}

					if(e.Tags.Distinct().Count() < e.Tags.Length)
					{
						throw new ArgumentException("All tags should be distinct within a TimeSerie.", "timeSerie");
					}
				}
			}

			// 'Values' validation
			if (timeSerie.Values != null)
			{
				// Time-values should be strictly ordered
				for (int i = 0; i < timeSerie.Values.Length - 1; i++)
				{
					if(timeSerie.Values[i+1].Time <= timeSerie.Values[i].Time)
					{
						throw new ArgumentException("Time-serie is not properly ordered.", "timeSerie");
					}
				}
			}
		}
	}
}
