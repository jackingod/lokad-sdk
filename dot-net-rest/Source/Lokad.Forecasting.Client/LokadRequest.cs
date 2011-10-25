#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Lokad.Forecasting.Client
{
    /// <summary>
    /// 
    /// </summary>
    internal class LokadRequest
    {
        private string _identity;
        private bool _compressRequest;

        public static LokadRequest Create(string identity, bool compressRequest = false)
        {
            return new LokadRequest
                       {
                           _identity = Convert.ToBase64String(Encoding.ASCII.GetBytes("auth-with-key@lokad.com:" + identity)),
                           _compressRequest = compressRequest
                       };
        }

        public TResult GetResponse<TResult>(string url, string content, string method) where TResult : class 
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            
            request.Method = method;
            request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + _identity);
            
            if (!String.IsNullOrEmpty(content))
            {
                request.ContentType = "application/xml";
                
                var bytes = Encoding.ASCII.GetBytes(content);
                
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");

                if (_compressRequest)
                {
                    request.Headers.Add(HttpRequestHeader.ContentEncoding, "gzip");

                    // compress content
                    var ms = new MemoryStream();
                    var compressor = new GZipStream(ms, CompressionMode.Compress, true);
                    compressor.Write(bytes, 0, bytes.Length);
                    compressor.Close();

                    request.ContentLength = ms.Length;

                    ms.Position = 0;

                    // write data to request stream
                    var stream = request.GetRequestStream();
                    stream.Write(ms.ToArray(), 0, (int) ms.Length);
                    stream.Close();
                }
                else
                {
                    request.ContentLength = bytes.Length;

                    var requestStream = request.GetRequestStream();
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();
                }
            }
            else
            {
                request.ContentLength = 0;
            }

            var response = RetryPolicy(() => request.GetResponse());

            var output = response.GetResponseStream();
            
            // accept compressed response stream
            var contentEncoding = response.Headers[HttpResponseHeader.ContentEncoding];
            if (!string.IsNullOrEmpty(contentEncoding))
            {
                if (contentEncoding.IndexOf("gzip", StringComparison.InvariantCultureIgnoreCase)>-1)
                {
                    output = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress, true);
                }
            }
            
            if (typeof(TResult) == typeof(String))
            {
                var reader = new StreamReader(output);
                var result = (object) XElement.Parse(reader.ReadToEnd()).Value;
                return (TResult) result;
            }

            using (output)
            {
                var reader = XmlReader.Create(output);

                var serializer = new XmlSerializer(typeof(TResult), "");
                var result = serializer.Deserialize(reader);
                return (TResult) result;
            }
        }

        /// <summary>Ad-hoc retry policy for transient network errors.</summary>
        private static T RetryPolicy<T>(Func<T> webRequest)
        {
            const int maxAttempts = 10;

            for (var i = 0; i < maxAttempts + 1; i++)
            {
                try
                {
                    // if the request completes, we don't try again
                    return webRequest();
                }
                catch (WebException ex)
                {
                    var statusCode =HttpStatusCode.InternalServerError;
                    if (ex.Response != null)
                    {
                        statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    }
                    
                    if (i < maxAttempts
                        && statusCode != HttpStatusCode.Unauthorized
                        && statusCode != HttpStatusCode.BadRequest
                        && statusCode != HttpStatusCode.InternalServerError)
                    {
                        // increasing sleep delay pattern
                        Thread.Sleep((i + 1) * 1000);
                    }
                    else
                    {
                        // after 'maxAttempts' we give up
                        throw;
                    }
                }
            }

            throw new ApplicationException("Retry policy is broken.");
        }
    }
}