package temp;

import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.GregorianCalendar;

import com.thoughtworks.xstream.converters.ConversionException;
import com.thoughtworks.xstream.converters.Converter;
import com.thoughtworks.xstream.converters.MarshallingContext;
import com.thoughtworks.xstream.converters.UnmarshallingContext;
import com.thoughtworks.xstream.io.HierarchicalStreamReader;
import com.thoughtworks.xstream.io.HierarchicalStreamWriter;

public class DateConverter implements Converter {


    public boolean canConvert(Class clazz) {
            return Calendar.class.isAssignableFrom(clazz);
    }

    public void marshal(Object value, HierarchicalStreamWriter writer,
                    MarshallingContext context) {
            Calendar calendar = (Calendar) value;
            Date date = calendar.getTime();
    		DateFormat formatter = new SimpleDateFormat("yyyy-MM-dd hh:mm:ss");
    		String dateString = formatter.format(date);
    		dateString = dateString.replace(' ', 'T');
            writer.setValue(dateString);
    }

    public Object unmarshal(HierarchicalStreamReader reader,
                    UnmarshallingContext context) {
    		String dateString = reader.getValue();
    		dateString = dateString.replace('T', ' ');
    		DateFormat formatter = new SimpleDateFormat("yyyy-MM-dd hh:mm:ss");
            GregorianCalendar calendar = new GregorianCalendar();
            try {
                    calendar.setTime(formatter.parse(dateString));
            } catch (ParseException e) {
                    throw new ConversionException(e.getMessage(), e);
            }
            return calendar;
    }
}
