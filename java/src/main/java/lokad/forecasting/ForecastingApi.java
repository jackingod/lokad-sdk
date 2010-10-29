package lokad.forecasting;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.Collection;
import java.util.Iterator;




import com.thoughtworks.xstream.XStream;
import com.thoughtworks.xstream.io.xml.DomDriver;


public class ForecastingApi implements IForecastingApi {

	private final String endPoint;

	public ForecastingApi(String endPoint) {
		this.endPoint = endPoint;
	}

	private String getErrorCode(InputStream inputStream) {
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3
		xstream.alias("ErrorCode", String.class);
		String errorCode = (String) xstream.fromXML(inputStream);
		return errorCode;
	}
	
	static String join(Collection<String> s, String delimiter) {
		StringBuilder builder = new StringBuilder();
		Iterator<String> iter = s.iterator();
		while (iter.hasNext()) {
			builder.append(iter.next());
			if (!iter.hasNext()) {
				break;
			}
			builder.append(delimiter);
		}
		return builder.toString();
	}

	static String join(String[] ss, String delimiter) {
		if (ss.length == 0)
			return null;
		StringBuilder builder = new StringBuilder();
		for (int i = 0; i < ss.length - 1; i++) {
			builder.append(ss[i]);
			builder.append(delimiter);
		}
		builder.append(ss[ss.length - 1]);
		return builder.toString();
	}

	@Override
	public String InsertDataset(String identity, Dataset dataset) throws IOException {
		String errorCode = "";

		URL url = new URL(endPoint + "/datasets");
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setDoOutput(true);
		connection.setRequestMethod("PUT");
		connection.setRequestProperty("Content-Type", "application/xml");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		OutputStream outputStream = connection.getOutputStream();
		//
		XStream xstream = new XStream(new DomDriver());
		xstream.alias("Dataset", Dataset.class);
		xstream.toXML(dataset, outputStream);
		//
		int responseCode = connection.getResponseCode();
		errorCode = getErrorCode(connection.getInputStream());

		connection.disconnect();

		return errorCode;
	}

	@Override
	public DatasetCollection ListDatasets(String identity, String continuationToken) throws IOException {
		DatasetCollection datasets = null;
		String u = endPoint + "/datasets"
				+ ((continuationToken != null && continuationToken.length() > 0) ? "/" + continuationToken : "");
		URL url = new URL(u);
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setRequestMethod("GET");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		//
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3
														// library
		xstream.alias("DatasetCollection", DatasetCollection.class);
		xstream.alias("Dataset", Dataset.class);

		//
		int responseCode = connection.getResponseCode();
		InputStream inputStream = connection.getInputStream();
		datasets = (DatasetCollection) xstream.fromXML(inputStream);

		connection.disconnect();

		return datasets;
	}

	@Override
	public String DeleteDataset(String identity, String datasetName) throws IOException {
		String errorCode = "";
		URL url = new URL(endPoint + "/datasets/" + datasetName);
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setRequestMethod("DELETE");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		//
		int responseCode = connection.getResponseCode();
		errorCode = getErrorCode(connection.getInputStream());

		connection.disconnect();

		return errorCode;
	}

	@Override
	public String UpsertTimeSeries(String identity, String datasetName, TimeSerie[] timeSeries, Boolean enableMerge) throws IOException {
		String errorCode = "";
		String suffix = "/series/" + datasetName + "?merge=" + ((enableMerge) ? "true" : "false");
		URL url = new URL(endPoint + suffix);
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setDoOutput(true);
		connection.setRequestMethod("PUT");
		connection.setRequestProperty("Content-Type", "application/xml");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		OutputStream outputStream = connection.getOutputStream();
		//
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3
														// library
		xstream.alias("TimeSeries", TimeSerie[].class);
		xstream.alias("TimeSerie", TimeSerie.class);
		xstream.alias("TimeValue", TimeValue.class);
		xstream.alias("EventValue", EventValue.class);
		xstream.registerConverter(new DateConverter());
		xstream.toXML(timeSeries, outputStream);

		//
		int responseCode = connection.getResponseCode();
		errorCode = getErrorCode(connection.getInputStream());

		connection.disconnect();

		return errorCode;
	}

	@Override
	public TimeSerieCollection ListTimeSeries(String identity, String datasetName, String continuationToken) throws IOException {
		TimeSerieCollection timeSerieCollection = null;
		String u = endPoint + "/series/" + datasetName
				+ ((continuationToken != null && continuationToken.length() > 0) ? "/" + continuationToken : "");
		URL url = new URL(u);
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setRequestMethod("GET");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		//
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3
														// library
		xstream.alias("TimeSerieCollection", TimeSerieCollection.class);
		xstream.alias("TimeSerie", TimeSerie.class);
		xstream.alias("TimeValue", TimeValue.class);
		xstream.alias("EventValue", EventValue.class);
		xstream.registerConverter(new DateConverter());

		//
		int responseCode = connection.getResponseCode();
		InputStream inputStream = connection.getInputStream();
		timeSerieCollection = (TimeSerieCollection) xstream.fromXML(inputStream);

		connection.disconnect();

		return timeSerieCollection;
	}

	@Override
	public String DeleteTimeSeries(String identity, String datasetName, String[] serieNames) throws IOException {
		String errorCode = "";

		String series = join(serieNames, ";");
		StringBuilder sb = new StringBuilder(endPoint).append("/series/").append(datasetName).append("?n=")
				.append(series);

		URL url = new URL(sb.toString());
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setRequestMethod("DELETE");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		//
		int responseCode = connection.getResponseCode();
		errorCode = getErrorCode(connection.getInputStream());

		connection.disconnect();

		return errorCode;
	}

	@Override
	public ForecastStatus GetForecastStatus(String identity, String datasetName) throws IOException {
		ForecastStatus status = null;
		String u = endPoint + "/status/" + datasetName;
		URL url = new URL(u);
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setRequestMethod("GET");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		//
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3
														// library
		xstream.alias("ForecastStatus", ForecastStatus.class);
		//
		int responseCode = connection.getResponseCode();
		InputStream inputStream = connection.getInputStream();
		status = (ForecastStatus) xstream.fromXML(inputStream);

		connection.disconnect();

		return status;
	}

	@Override
	public ForecastCollection GetForecasts(String identity, String datasetName, String[] serieNames) throws IOException {
		ForecastCollection forecasts = null;
		String series = join(serieNames, ";");
		StringBuilder sb = new StringBuilder(endPoint).append("/forecasts/").append(datasetName).append("?n=")
				.append(series);
		URL url = new URL(sb.toString());
		HttpURLConnection connection = (HttpURLConnection) url.openConnection();
		connection.setDoInput(true);
		connection.setRequestMethod("GET");
		connection.setRequestProperty("Authorization", "Basic " + identity);

		//
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3
														// library
		xstream.alias("ForecastCollection", ForecastCollection.class);
		xstream.alias("ForecastSerie", ForecastSerie.class);
		xstream.alias("ForecastValue", ForecastValue.class);
		xstream.alias("TimeSerieCollection", TimeSerieCollection.class);
		xstream.alias("TimeSerie", TimeSerie.class);
		xstream.alias("TimeValue", TimeValue.class);
		xstream.registerConverter(new DateConverter());
		//
		int responseCode = connection.getResponseCode();
		InputStream inputStream = connection.getInputStream();
		forecasts = (ForecastCollection) xstream.fromXML(inputStream);

		connection.disconnect();

		return forecasts;
	}
}
