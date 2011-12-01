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
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
            request.AllowAutoRedirect = false;

            if (String.IsNullOrEmpty(content))
            {
                request.ServicePoint.Expect100Continue = false;
            }
            else
            {
                request.ServicePoint.Expect100Continue = true;
                request.ContentType = "application/xml";

                var bytes = Encoding.ASCII.GetBytes(content);

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

            using (var response = RetryPolicy(request.GetResponse))
            {
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
                using (output)
                {
                    if (typeof(TResult) == typeof(String))
                    {
                        var reader = new StreamReader(output);
                        var resultString = reader.ReadToEnd();
                        var result = (object)XElement.Parse(resultString).Value;
                        return (TResult) result;
                    }
                    else
                    {
                        var reader = XmlReader.Create(output);
                        var serializer = new XmlSerializer(typeof(TResult), "");
                        var result = serializer.Deserialize(reader);
                        return (TResult)result;
                    }
                }
            }
        }

        /// <summary>Ad-hoc retry policy for transient errors.</summary>
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
                    var statusCode = HttpStatusCode.InternalServerError;
                    if (ex.Response != null)
                    {
                        statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    }

                    // Do not retry in the following cases
                    switch (statusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            throw new UnauthorizedAccessException(ex.Message);
                        case HttpStatusCode.BadRequest:
                            throw new ArgumentException(ErrorCodes.OutOfRangeInput);
                        case HttpStatusCode.NotFound:
                            throw new InvalidOperationException(ErrorCodes.DatasetNotFound);
                    }
                    
                    if (i < maxAttempts)
                    {
                        // increasing sleep delay pattern
                        Thread.Sleep((i + 1) * 1000);
                    }
                    else
                    {
                        // after 'maxAttempts' we give up
                        switch(statusCode)
                        {
                            case HttpStatusCode.PreconditionFailed:
                                throw new InvalidOperationException(ErrorCodes.InvalidDatasetState);
                            case HttpStatusCode.InternalServerError:
                                throw new InvalidOperationException(ErrorCodes.ServiceFailure);
                            default:
                                throw;
                        }
                    }
                }
            }

            throw new ApplicationException("Retry policy is broken.");
        }
    }
}