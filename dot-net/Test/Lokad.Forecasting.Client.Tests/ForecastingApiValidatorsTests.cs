#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion
using System;
using NUnit.Framework;

namespace Lokad.Forecasting.Client.Tests
{
	[TestFixture]
	public class ForecastingApiValidatorsTests
	{
		[Test]
		public void IsValidApiName()
		{
			Assert.IsTrue("abC210".IsValidApiName(), "#A00");
			Assert.IsFalse("a_bc2010".IsValidApiName(), "#A01");
			Assert.IsFalse("abc ".IsValidApiName(), "#A02");

			string myNull = null;
			Assert.IsFalse(myNull.IsValidApiName(), "#A03");
			Assert.IsFalse(string.Empty.IsValidApiName(), "#A04");

			Assert.IsTrue(Guid.NewGuid().ToString("N").IsValidApiName(), "#A05");
		}

		[Test]
		public void IsValidDataset0()
		{
			var dataset = new Dataset
			{
				Name = "validname",
				Period = PeriodCodes.Week,
				Horizon = 1
			};

			dataset.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidDataset1()
		{
			var dataset = new Dataset
			               	{
			               		Name = "invalid-name",
			               		Period = "Week",
			               		Horizon = 1
			               	};

			dataset.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidDataset2()
		{
			var dataset = new Dataset
			{
				Name = "validname",
				Period = "invalid",
				Horizon = 1
			};

			dataset.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void IsValidDataset3()
		{
			var dataset = new Dataset
			{
				Name = "validname",
				Period = "Week",
				Horizon = 0
			};

			dataset.Validate();
		}

		[Test]
		public void IsValidTimeSerie0()
		{
			var timeSerie = new TimeSerie
			{
				Name = "validname",
			};

			timeSerie.Validate();
		}

		[Test]
		public void IsValidTimeSerie0Bis()
		{
			var timeSerie = new TimeSerie
			{
				Name = "validname",
				Tags = new string[0],
				Events = new EventValue[0],
				Values = new TimeValue[0],
			};

			timeSerie.Validate();
		}

		[Test]
		public void IsValidTimeSerie1()
		{
			var timeSerie = new TimeSerie
			{
				Name = "validname",
				Tags = new [] { "validtag" },
				Events = new [] { new EventValue
				                  	{
										KnownSince = new DateTime(2001,1,1), 
										Time = new DateTime(2001,1,1),
										Tags = new [] { "validtag" }
				                  	}},
				Values = new[]{ new TimeValue { Time = new DateTime(2001,1,1), Value = 13 }}
			};

			timeSerie.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidTimeSerie2()
		{
			var timeSerie = new TimeSerie
			{
				Name = "invalid-name",
			};

			timeSerie.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidTimeSerie2Bis()
		{
			var timeSerie = new TimeSerie
			{
				Name = null,
			};

			timeSerie.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidTimeSerie3()
		{
			var timeSerie = new TimeSerie
			{
				Name = "validname",
				Tags = new[] { "invalid-tag" },
			};

			timeSerie.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidTimeSerie4()
		{
			var timeSerie = new TimeSerie
			{
				Name = "validname",
				Events = new[] { new EventValue
				                  	{
										KnownSince = new DateTime(2001,1,1), 
										Time = new DateTime(2001,1,1),
										Tags = new [] { "invalid-tag" }
				                  	}},
			};

			timeSerie.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidTimeSerie6()
		{
			var tags = new string[150];
			for (int i = 0; i < tags.Length; i++) tags[i] = "t" + i;

			var timeSerie = new TimeSerie
			{
				Name = "validname",
				Tags = tags
			};

			timeSerie.Validate();
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void IsValidTimeSerie7()
		{
			var timeSerie = new TimeSerie
			{
				Name = "validname",
				Tags = new[]{ "duplicatetag", "duplicatetag"}
			};

			timeSerie.Validate();
		}
	}
}
