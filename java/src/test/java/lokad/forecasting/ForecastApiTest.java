package lokad.forecasting;

import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import java.io.IOException;
import java.util.Calendar;

import lokad.forecasting.Base64;
import lokad.forecasting.Dataset;
import lokad.forecasting.DatasetCollection;
import lokad.forecasting.EventValue;
import lokad.forecasting.ForecastCollection;
import lokad.forecasting.ForecastStatus;
import lokad.forecasting.ForecastingApi;
import lokad.forecasting.TimeSerie;
import lokad.forecasting.TimeSerieCollection;
import lokad.forecasting.TimeValue;

import org.junit.After;
import org.junit.Before;
import org.junit.Ignore;
import org.junit.Test;

public class ForecastApiTest {

	private final String endPoint = "http://sandbox-api.lokad.com/rest/forecasting3";
	private String identity;

	private final String DsName = "timsonSimpleDataset";

	@Before
	public void setUp() throws Exception {
		String apiKey = System.getProperty("apiKey");
		identity = Base64.encode("auth-with-key@lokad.com" + ":" + apiKey);
	}

	@After
	public void tearDown() throws Exception {
		ForecastingApi f = new ForecastingApi(endPoint);
		String errorCode = f.DeleteDataset(identity, DsName);
	}

	@Test
	public void testInsertDataset() throws IOException {
		Dataset dataset = new Dataset();
		dataset.Name = DsName;
		dataset.Horizon = 1;
		dataset.Period = "week";

		ForecastingApi f = new ForecastingApi(endPoint);
		String errorCode = f.InsertDataset(identity, dataset);

		assertTrue(errorCode, errorCode == null || errorCode.length() == 0);
	}

	@Test
	public void testListDatasets() throws IOException {
		ForecastingApi f = new ForecastingApi(endPoint);
		DatasetCollection collection = f.ListDatasets(identity, null);

		assertTrue(collection.ErrorCode, collection.ErrorCode == null || collection.ErrorCode.length() == 0);
		assertTrue("Collection is empty", collection.Datasets.length > 0);
	}

	@Test
	public void testDeleteDataset() throws IOException {
		ForecastingApi f = new ForecastingApi(endPoint);
		String errorCode = f.DeleteDataset(identity, DsName);
		assertTrue(errorCode, errorCode == null || errorCode.length() == 0);
	}

	@Test
	@Ignore
	public void testUpsertTimeSeries() {
		fail("Not implemented");
	}

	@Test
	public void testListTimeSeries() throws IOException {
		ForecastingApi f = new ForecastingApi(endPoint);
		TimeSerieCollection collection = f.ListTimeSeries(identity, "SCx311x6Month201010132311", "");

		assertTrue(collection.ErrorCode, collection.ErrorCode == null || collection.ErrorCode.length() == 0);
		assertTrue("Collection is empty", collection.TimeSeries.length > 0);
	}

	@Test
	@Ignore
	public void testDeleteTimeSeries() {
	}

	@Test
	public void testGetForecastStatus() throws IOException {
		ForecastingApi f = new ForecastingApi(endPoint);
		ForecastStatus status = f.GetForecastStatus(identity, "SCx311x6Month201010132311");
		assertTrue(status.ErrorCode, status.ErrorCode == null || status.ErrorCode.length() == 0);
	}

	@Test
	public void testGetForecasts() throws IOException {
		ForecastingApi f = new ForecastingApi(endPoint);
		ForecastCollection collection = f.GetForecasts(identity, "SCx311x6Month201010132311", new String[] { "90582",
				"113778" });
		assertTrue(collection.ErrorCode, collection.ErrorCode == null || collection.ErrorCode.length() == 0);
	}

	@Test
	public void testApi() throws IOException {
		ForecastingApi f = new ForecastingApi(endPoint);

		String datasetName = DsName + "2";
		Dataset dataset = new Dataset();
		dataset.Name = datasetName;
		dataset.Horizon = 1;
		dataset.Period = "week";

		// insert dataset
		String errorCode = f.InsertDataset(identity, dataset);
		assertTrue("InsertDataset returned: " + errorCode, errorCode == null || errorCode.length() == 0);

		// list datasets
		DatasetCollection collection = f.ListDatasets(identity, null);
		assertTrue("ListDatasets returned: " + collection.ErrorCode, collection.ErrorCode == null
				|| collection.ErrorCode.length() == 0);
		assertTrue("DatasetCollection is empty", collection.Datasets.length > 0);

		// insert timeSerie
		TimeSerie[] timeSeries = new TimeSerie[1];
		for (int ti = 0; ti < timeSeries.length; ti++) {
			TimeSerie t = new TimeSerie();
			timeSeries[ti] = t;

			t.Name = "Lilo";
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
				t.Values[i].Value = 1.0 / (i + 1);
			}
		}
		errorCode = f.UpsertTimeSeries(identity, datasetName, timeSeries, false);
		assertTrue("UpsertTimeSeries returned: " + errorCode, errorCode == null || errorCode.length() == 0);

		ForecastStatus status = f.GetForecastStatus(identity, datasetName);
		assertTrue("GetForecastStatus returned: " + status.ErrorCode,
				status.ErrorCode == null || status.ErrorCode.length() == 0);

		String[] serieNames = new String[timeSeries.length];
		for (int i = 0; i < timeSeries.length; i++) {
			serieNames[i] = timeSeries[i].Name;
		}
		ForecastCollection forecasts = f.GetForecasts(identity, datasetName, serieNames);
		assertTrue("GetForecasts returned: " + errorCode, errorCode == null || errorCode.length() == 0);

		// TODO InvalidDatasetState
		// errorCode = f.DeleteTimeSeries(identity, datasetName, serieNames);
		// assertTrue("DeleteTimeSeries returned: " + errorCode, errorCode ==
		// null || errorCode.length() == 0);

		errorCode = f.DeleteDataset(identity, datasetName);
		assertTrue("DeleteDataset returned: " + errorCode, errorCode == null || errorCode.length() == 0);
	}
}
