package temp;
import java.io.DataInputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;

import org.lokad.forecasting.api.Dataset;
import org.lokad.forecasting.api.DatasetCollection;

import com.thoughtworks.xstream.XStream;
import com.thoughtworks.xstream.io.xml.DomDriver;


public class Program {

	private final static String DsName = "timsonSimpleDataset1";
	
//	public static void main(String[] args) {
//		// usingJersey();
//		//usingHttpNative();
//		putDataset();
//		//deleteDataset();
//		System.out.println("Done.");
//	}
	
	
	
	private static void putDataset() {
		URL url;
		try {
			url = new URL(
					"http://sandbox-api.lokad.com/rest/forecasting3/datasets");
			HttpURLConnection connection = (HttpURLConnection) url.openConnection();
			connection.setDoInput(true);
			connection.setDoOutput(true);
			connection.setRequestMethod("PUT");
			connection.setRequestProperty("Content-Type", "application/xml");
			connection.setRequestProperty("Authorization",
					"Basic YXV0aC13aXRoLWtleUBsb2thZC5jb206d0lMOW9RRUF0b3V2ZFJBY0VHV0YzOHVCeFAvRXVwV2RRek1uWkNjPQ==");

			OutputStream outputStream = connection.getOutputStream();
			
			Dataset ds = new Dataset();
			ds.Name = DsName;
			ds.Horizon = 1;
			ds.Period = "week";
			
			XStream xstream = new XStream(new DomDriver()); // does not require XPP3 library
			xstream.alias("Dataset", Dataset.class);
			xstream.toXML(ds, outputStream);
						
			InputStream inputStream = connection.getInputStream();
		
			XStream xs2 = new XStream(new DomDriver()); // does not require XPP3 library
			xs2.alias("ErrorCode", String.class);
			String errorCode = (String) xs2.fromXML(inputStream);
			System.out.println(errorCode);
			
			DataInputStream dis = new DataInputStream(inputStream);
			String s = dis.readLine();
			System.out.println(s);

			int code = connection.getResponseCode();
			System.out.println(code);
			connection.disconnect();
		} catch (Exception e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
	
	private static void deleteDataset() {
		URL url;
		try {
			url = new URL(
					"http://sandbox-api.lokad.com/rest/forecasting3/datasets/" + DsName);
			HttpURLConnection connection = (HttpURLConnection) url.openConnection();
			connection.setDoInput(true);
			connection.setRequestMethod("DELETE");
			connection.setRequestProperty("Authorization",
					"Basic YXV0aC13aXRoLWtleUBsb2thZC5jb206d0lMOW9RRUF0b3V2ZFJBY0VHV0YzOHVCeFAvRXVwV2RRek1uWkNjPQ==");

			InputStream inputStream = connection.getInputStream();

			DataInputStream dis = new DataInputStream(inputStream);
			String s = dis.readLine();
			System.out.println(s);

			int code = connection.getResponseCode();
			System.out.println(code);
			connection.disconnect();
		} catch (Exception e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

	private static void usingHttpNative() {
		URL url;
		try {
			url = new URL(
					"http://sandbox-api.lokad.com/rest/forecasting3/datasets");
//			"http://xstream.codehaus.org/tutorial.html");			
			HttpURLConnection connection = (HttpURLConnection) url.openConnection();
			connection.setDoInput(true);
//			connection.setInstanceFollowRedirects(false);
			connection.setRequestMethod("GET");
//			connection.setRequestProperty("Content-Type", "application/xml");
			connection.setRequestProperty("Authorization",
					"Basic YXV0aC13aXRoLWtleUBsb2thZC5jb206d0lMOW9RRUF0b3V2ZFJBY0VHV0YzOHVCeFAvRXVwV2RRek1uWkNjPQ==");

			//Object o = connection.getOutputStream();
			//o.toString();
			InputStream o = connection.getInputStream();
			
			getData(o);
			
			DataInputStream dis = new DataInputStream(o);
			String s = dis.readLine();
			System.out.println(s);
//			OutputStream os = connection.getOutputStream();
//			String out = new String(os.toByteArray(), "UTF-8")
//			System.out.println(os.toString());

			int code = connection.getResponseCode();
			System.out.println(code);
			connection.disconnect();
		} catch (Exception e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}

	private static void getData(InputStream inputStream) {
		XStream xstream = new XStream(new DomDriver()); // does not require XPP3 library
		xstream.alias("DatasetCollection", DatasetCollection.class);
		xstream.alias("Dataset", Dataset.class);
		
		DatasetCollection o = (DatasetCollection) xstream.fromXML(inputStream);
		printDataset(o);
	}

	private static void printDataset(DatasetCollection o) {
		for (Dataset ds : o.Datasets) {
			System.out.println("Name: " + ds.Name);
			System.out.println("Horizon: " + ds.Horizon);
			System.out.println("Period: " + ds.Period);
		}
	}
//
//	private static void usingJersey() {
//		DefaultClientConfig clientConfig = new DefaultClientConfig();
//		Client client = Client.create(clientConfig);
//
//		WebResource resource = client.resource("http://sandbox-api.lokad.com/rest/forecasting3/datasets");
//		// resource.
//
//		ClientResponse response = resource.type("application/xml").put(
//				ClientResponse.class, "<customer>...</customer.");
//		System.out.println(response);
//
//		System.out.println("hello world!");
//	}
}
