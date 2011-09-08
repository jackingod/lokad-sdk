#region (c)2010 Lokad - New BSD license
// Company: Lokad SAS, http://www.lokad.com/
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
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

        public static LokadRequest Create(string identity)
        {
            return new LokadRequest
                       {
                           _identity = Convert.ToBase64String(Encoding.ASCII.GetBytes("auth-with-key@lokad.com:" + identity))
                       };
        }

        public TResult GetResponse<TResult>(string url, string content, string method) where TResult : class 
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            
            request.Method = method;
            request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + _identity);
            request.ContentType = "application/xml";

            if (!String.IsNullOrEmpty(content))
            {
                var bytes = Encoding.ASCII.GetBytes(content);
                request.ContentLength = bytes.Length;
                var input = request.GetRequestStream();
                input.Write(bytes, 0, bytes.Length);
                input.Close();
            }
            else
            {
                request.ContentLength = 0;
            }

            var response = RetryPolicy(() => request.GetResponse());

            var output = response.GetResponseStream();

            if (typeof(TResult) == typeof(String))
            {
                var reader = new StreamReader(output);
                var result = (object) XElement.Parse(reader.ReadToEnd()).Value;
                return (TResult) result;
            }

            using (output)
            {
                var serializer = new XmlSerializer(typeof(TResult));
                return (TResult)serializer.Deserialize(output);
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