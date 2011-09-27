The Forecasting Client is a stand-alone .NET wrapper library around the [REST Forecasting API v3] (http://www.lokad.com/programmers-guide-forecasting-api-v3.ashx). A Java port is also available.

## Sample usage (C#)
	// Initialization with the API key of Lokad
	var client = new ForecastingClient(myApiKey);

	// Create a new dataset (no effect if it exists already)
	var container = new Dataset { Name = "mydata", Period = "week", Horizon = 4 };
	client.InsertDataset(container); 

	// Update or Insert time-series
	var series = new TimeSerie[] { /* snipped */ };
	client.UpsertTimeSeries("mydata", series, false); // merge=false

	// Wait until forecasts are ready, and then download forecasts
	var forecasts = client.GetForecasts("mydata", series);
	
## Resources

* [GettingStarted] (https://github.com/Lokad/lokad-sdk/wiki/GettingStarted) - a sample tutorial in C# for the forecasting client.
* [GettingStartedJava] (https://github.com/Lokad/lokad-sdk/wiki/GettingStartedJava) - a sample tutorial in Java for the forecasting client.
* C# / .NET 3.5 ([browse source] (https://github.com/Lokad/lokad-sdk/tree/forecasting-client/dot-net)).

## Apps using the forecasting client
* Lokad Salescast - [product info] (http://www.lokad.com/salescast-sales-forecasting-software.ashx)
* Lokad Callcalc - [product info] (http://www.lokad.com/call-center-calculator-software.ashx)
* Lokad Shelfcheck - [product info] (http://www.lokad.com/shelfcheck-on-shelf-availability-optimization.ashx)
