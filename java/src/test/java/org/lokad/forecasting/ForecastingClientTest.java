package org.lokad.forecasting;
import static org.junit.Assert.*;

import java.util.Calendar;
import java.util.List;

import org.junit.After;
import org.junit.Before;
import org.junit.Ignore;
import org.junit.Test;
import org.lokad.forecasting.api.Dataset;
import org.lokad.forecasting.api.DatasetCollection;
import org.lokad.forecasting.api.EventValue;
import org.lokad.forecasting.api.ForecastCollection;
import org.lokad.forecasting.api.ForecastSerie;
import org.lokad.forecasting.api.ForecastStatus;
import org.lokad.forecasting.api.TimeSerie;
import org.lokad.forecasting.api.TimeSerieCollection;
import org.lokad.forecasting.api.TimeValue;
import org.lokad.forecasting.client.ForecastingApi;
import org.lokad.forecasting.client.ForecastingClient;


public class ForecastingClientTest {

	private ForecastingClient client;
	private final String endPoint = "http://sandbox-api.lokad.com/rest/forecasting3";
	private final String identity = "wIL9oQEAtouvdRAcEGWF38uBxP/EupWdQzMnZCc=";
	private final String DsName = "timsonSimpleDataset"; 
	
	@Before
	public void setUp() throws Exception {
		client = new ForecastingClient(identity, endPoint);
	}

	@After
	public void tearDown() throws Exception {
	}

	@Test
	public void testInsertDataset() {
		Dataset dataset = new Dataset();
		dataset.Name = DsName;
		dataset.Horizon = 1;
		dataset.Period = "week";
		
		client.InsertDataset(dataset);
	}

	@Test
	public void testListDatasets() {
		List<Dataset> list = client.ListDatasets();
		assertTrue(list.size() > 0);
	}

	@Test
	public void testDeleteDataset() {
		Dataset dataset = new Dataset();
		dataset.Name = DsName;
		dataset.Horizon = 1;
		dataset.Period = "week";
		
		client.InsertDataset(dataset);
		client.DeleteDataset(dataset.Name);
	}

	@Test
	@Ignore
	public void testDeleteDatasetAndWait() {
		fail("Not yet implemented");
	}

	@Test
	@Ignore
	public void testUpsertTimeSeries() {
		fail("Not yet implemented");
	}

	@Test
	@Ignore
	public void testListTimeSeries() {
		List<TimeSerie> list = client.ListTimeSeries(DsName);		
	}

	@Test
	@Ignore
	public void testDeleteTimeSeries() {
		fail("Not yet implemented");
	}

	@Test
	@Ignore
	public void testTriggerForecastCompute() {
		fail("Not yet implemented");
	}

	@Test
	@Ignore
	public void testGetForecasts() throws InterruptedException {
		client.GetForecasts(DsName, new String[] { });
	}
	
	@Test
	public void testClient() throws InterruptedException {
		String datasetName = DsName + "2"; 
		Dataset dataset = new Dataset();
		dataset.Name = datasetName;
		dataset.Horizon = 1;
		dataset.Period = "week";
		
		// insert dataset
		client.InsertDataset(dataset);

		// list datasets
		List<Dataset> collection = client.ListDatasets();		
		assertTrue("DatasetCollection is empty", collection.size() > 0);

		// insert timeSerie
		TimeSerie[] timeSeries = new TimeSerie[3];
		for (int ti = 0; ti < timeSeries.length; ti++) {
			TimeSerie t = new TimeSerie();
			timeSeries[ti] = t;
			
			t.Name = "Lilo" + String.valueOf(ti);
			t.Events = new EventValue[5];
			t.Tags = new String[5];
			t.Values = new TimeValue[5];
			for (int i = 0; i < t.Values.length; i++) {
				t.Events[i] = new EventValue();
				t.Events[i].KnownSince = Calendar.getInstance();
				t.Events[i].Time = Calendar.getInstance();
				t.Events[i].Tags = new String[] { "Tag" + String.valueOf(i) };

				t.Tags[i] = "Tag" + String.valueOf(i);

				t.Values[i] = new TimeValue();
				t.Values[i].Time = Calendar.getInstance();
				t.Values[i].Time.add(Calendar.DATE, i - t.Values.length);
				t.Values[i].Value = 1.0 / (i+1);
			}
		}
		client.UpsertTimeSeries(datasetName, timeSeries, false);
		
		// get status
		boolean status = client.TriggerForecastCompute(datasetName);
	
		String[] serieNames = new String[timeSeries.length];
		for (int i = 0; i < timeSeries.length; i++) {
			serieNames[i] = timeSeries[i].Name;
		}
		
		// list time series
		List<TimeSerie> timeSerieCollection = client.ListTimeSeries(datasetName);
		assertTrue(timeSerieCollection.size() == timeSeries.length);

		// get forecasts
		ForecastSerie[] forecasts = client.GetForecasts(datasetName, serieNames);

		// delete time series
		client.DeleteTimeSeries(datasetName, serieNames);
		
		// delete dataset
		client.DeleteDataset(datasetName);
	}	
}
