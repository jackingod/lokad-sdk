package lokad.forecasting;
import static org.junit.Assert.assertTrue;

import java.util.Calendar;
import java.util.List;
import java.util.Random;

import lokad.forecasting.Dataset;
import lokad.forecasting.EventValue;
import lokad.forecasting.ForecastSerie;
import lokad.forecasting.ForecastingClient;
import lokad.forecasting.TimeSerie;
import lokad.forecasting.TimeValue;

import org.junit.Before;
import org.junit.Ignore;
import org.junit.Test;

public class ForecastingClientHardTest {

	private ForecastingClient client;
	private final String endPoint = "http://sandbox-api.lokad.com/rest/forecasting3";
	private final String identity = "wIL9oQEAtouvdRAcEGWF38uBxP/EupWdQzMnZCc=";
	private final String DsName = "timsonSimpleDataset"; 
	
	@Before
	public void setUp() throws Exception {
		client = new ForecastingClient(identity, endPoint);
	}

	@Test
	public void testClient100() throws InterruptedException {
		testClient(2, 100);
	}
	@Test
	public void testClient1000() throws InterruptedException {
		testClient(5, 1000);
	}
	@Test
	public void testClient10000() throws InterruptedException {
		testClient(5, 10000);
	}
	@Test
	public void testClientRand10000() throws InterruptedException {
		Random r = new Random();
		testClient(r.nextInt(1000), r.nextInt(1000));
	}	
	
	private void testClient(int timeSeriesCount, int timeSerieSize) throws InterruptedException {
		String datasetName = String.format("%s%dc%d", DsName, timeSeriesCount, timeSerieSize); 
		Dataset dataset = new Dataset();
		dataset.Name = datasetName;
		dataset.Horizon = 10;
		dataset.Period = "month";
		
		// insert dataset
		client.InsertDataset(dataset);

		// insert timeSerie
		Random rand = new Random();
		TimeSerie[] timeSeries = new TimeSerie[timeSeriesCount];
		for (int ti = 0; ti < timeSeries.length; ti++) {
			TimeSerie t = new TimeSerie();
			timeSeries[ti] = t;
			t.Name = "Test" + String.valueOf(ti);

			// events and tags
			t.Events = new EventValue[50];
			t.Tags = new String[t.Events.length];
			for (int i = 0; i < t.Events.length; i++) {
				t.Events[i] = new EventValue();
				t.Events[i].KnownSince = Calendar.getInstance();
				t.Events[i].Time = Calendar.getInstance();
				t.Events[i].Tags = new String[] { "Tag" + String.valueOf(i) };
				t.Tags[i] = "Tag" + String.valueOf(i);
			}

			//values
			t.Values = new TimeValue[timeSerieSize];
			for (int i = 0; i < t.Values.length; i++) {
				t.Values[i] = new TimeValue();
				t.Values[i].Time = Calendar.getInstance();
				t.Values[i].Time.add(Calendar.DATE, i - t.Values.length);
				t.Values[i].Value = rand.nextGaussian();
			}
		}
		client.UpsertTimeSeries(datasetName, timeSeries, false);
		
		// list time series
		List<TimeSerie> timeSerieCollection = client.ListTimeSeries(datasetName);
		assertTrue(timeSerieCollection.size() == timeSeries.length);

		// TriggerForecastCompute
		boolean status = false;
		while (!status) {
			Thread.sleep(5 * 1000);
			status = client.TriggerForecastCompute(datasetName);
		}
	
		String[] serieNames = new String[timeSeries.length];
		for (int i = 0; i < timeSeries.length; i++) {
			serieNames[i] = timeSeries[i].Name;
		}

		// get forecasts
		ForecastSerie[] forecasts = client.GetForecasts(datasetName, serieNames);
		assertTrue(String.format("%s : timeSeries.length != forecasts.length (%d != %d)", datasetName, timeSeries.length, forecasts.length), 
				timeSeries.length == forecasts.length);
		
		// delete dataset
		client.DeleteDataset(datasetName);
	}	
}
