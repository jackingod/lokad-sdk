using System;
using System.IO;
using System.IO.Compression;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace Lokad.Forecasting.Client
{
    // [vermorel] Implement the symmetric of HTTP compression, but for HTTP requests.
    // Response decompression is disabled, as it will be handled separatly.

    /// <summary>Enable GZip compression for WCF.</summary>
    /// <remarks>
    /// Based on the code found at http://msdn.microsoft.com/en-us/library/ms751458.aspx
    /// </remarks>
    public sealed class GZipMessageEncodingBindingElement : MessageEncodingBindingElement 
    {
        MessageEncodingBindingElement _element;

        /// <remarks></remarks>
        public GZipMessageEncodingBindingElement(MessageEncodingBindingElement element)
        {
            _element = element;
        }

        /// <summary>Called by WCF to get the factory that creates the message encoder.</summary>
        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new GZipMessageEncoderFactory(_element.CreateMessageEncoderFactory());
        }

        /// <remarks></remarks>
        public override MessageVersion MessageVersion
        {
            get { return _element.MessageVersion; }
            set { _element.MessageVersion = value; }
        }

        /// <remarks></remarks>
        public override BindingElement Clone()
        {
            return new GZipMessageEncodingBindingElement(_element);
        }

        /// <remarks></remarks>
        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(XmlDictionaryReaderQuotas))
            {
                return _element.GetProperty<T>(context);
            }

            return base.GetProperty<T>(context);
        }

        /// <remarks></remarks>
        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        /// <remarks></remarks>
        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        /// <remarks></remarks>
        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }
    }

    /// <summary>Used to create the custom encoder (GZipMessageEncoder).</summary>
    internal class GZipMessageEncoderFactory : MessageEncoderFactory
    {
        readonly MessageEncoder _encoder;

        //The GZip encoder wraps an inner encoder
        //We require a factory to be passed in that will create this inner encoder
        public GZipMessageEncoderFactory(MessageEncoderFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            _encoder = new GZipMessageEncoder(factory.Encoder);
        }

        //The service framework uses this property to obtain an encoder from this encoder factory
        public override MessageEncoder Encoder
        {
            get { return _encoder; }
        }

        public override MessageVersion MessageVersion
        {
            get { return _encoder.MessageVersion; }
        }

        /// <summary>
        /// Wraps an inner encoder that actually converts a WCF Message into textual XML, binary XML 
        /// or some other format. This implementation then compresses the requests, and leaves
        /// untouched the responses.
        /// </summary>
        class GZipMessageEncoder : MessageEncoder
        {
            readonly MessageEncoder _encoder;

            internal GZipMessageEncoder(MessageEncoder encoder)
            {
                if (encoder == null)
                    throw new ArgumentNullException("encoder");
                _encoder = encoder;
            }

            public override string ContentType
            {
                get { return "text/xml; charset=utf-8"; } // used by for WCF
            }

            public override string MediaType
            {
                get { return _encoder.MediaType; }
            }

            // SOAP version to use - we delegate to the inner encoder for this
            public override MessageVersion MessageVersion
            {
                get { return _encoder.MessageVersion; }
            }

            #region CompressBuffer
            /// <summary>Helper method to compress an array of bytes.</summary>
            static ArraySegment<byte> CompressBuffer(ArraySegment<byte> buffer, BufferManager bufferManager, int messageOffset)
            {
                byte[] bufferedBytes;
                int totalLength;
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(buffer.Array, 0, messageOffset);

                    using (var gzStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        gzStream.Write(buffer.Array, messageOffset, buffer.Count);
                    }

                    var compressedBytes = memoryStream.ToArray();
                    totalLength = compressedBytes.Length;
                    bufferedBytes = bufferManager.TakeBuffer(compressedBytes.Length);

                    Array.Copy(compressedBytes, 0, bufferedBytes, 0, compressedBytes.Length);

                    bufferManager.ReturnBuffer(buffer.Array);
                }

                var byteArray = new ArraySegment<byte>(bufferedBytes, messageOffset, totalLength);
                return byteArray;
            }
            #endregion

            #region DecompressBuffer is not used
            /// <summary>Helper method to decompress an array of bytes.</summary>
            static ArraySegment<byte> DecompressBuffer(ArraySegment<byte> buffer, BufferManager bufferManager)
            {
                var memoryStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count - buffer.Offset);
                var decompressedStream = new MemoryStream();
                const int blockSize = 1024;
                var tempBuffer = bufferManager.TakeBuffer(blockSize);

                using (var gzStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    while (true)
                    {
                        int bytesRead = gzStream.Read(tempBuffer, 0, blockSize);
                        if (bytesRead == 0)
                            break;
                        decompressedStream.Write(tempBuffer, 0, bytesRead);
                    }
                }
                bufferManager.ReturnBuffer(tempBuffer);

                var decompressedBytes = decompressedStream.ToArray();
                var bufferManagerBuffer = bufferManager.TakeBuffer(decompressedBytes.Length + buffer.Offset);
                Array.Copy(buffer.Array, 0, bufferManagerBuffer, 0, buffer.Offset);
                Array.Copy(decompressedBytes, 0, bufferManagerBuffer, buffer.Offset, decompressedBytes.Length);

                var byteArray = new ArraySegment<byte>(bufferManagerBuffer, buffer.Offset, decompressedBytes.Length);
                bufferManager.ReturnBuffer(buffer.Array);

                return byteArray;
            }
            #endregion

            /// <summary>One of the two main entry points into the encoder. 
            /// Called by WCF to decode a buffered byte array into a Message.</summary>
            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                // No decompression for returned message.
                return _encoder.ReadMessage(buffer, bufferManager, contentType);

                ////Decompress the buffer
                //var decompressedBuffer = DecompressBuffer(buffer, bufferManager);

                ////Use the inner encoder to decode the decompressed buffer
                //var returnMessage = _encoder.ReadMessage(decompressedBuffer, bufferManager);
                //returnMessage.Properties.Encoder = this;
                //return returnMessage;
            }

            /// <summary>One of the two main entry points into the encoder.
            /// Called by WCF to encode a Message into a buffered byte array.</summary>
            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                //Use the inner encoder to encode a Message into a buffered byte array
                var buffer = _encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                //Compress the resulting byte array
                return CompressBuffer(buffer, bufferManager, messageOffset);
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                return _encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                throw new NotSupportedException();
            }
        }
    }

    /// <summary>Applies the <c>Content-Encoding: gzip</c> HTTP header to the WCF
    /// requests.</summary>
    public class GZipHeaderRequestBehavior : IEndpointBehavior
    {
        /// <summary>Does nothing.</summary>
        public void AddBindingParameters(ServiceEndpoint serviceEndpoint, BindingParameterCollection bindingParameters) { }

        /// <summary>Adds the <c>GZipHeaderRequestInspector</c>.</summary>
        public void ApplyClientBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime behavior)
        {
            behavior.MessageInspectors.Add(new GZipHeaderRequestInspector());
        }

        /// <summary>Does nothing.</summary>
        public void ApplyDispatchBehavior(ServiceEndpoint serviceEndpoint, EndpointDispatcher endpointDispatcher) { }

        /// <summary>Does nothing.</summary>
        public void Validate(ServiceEndpoint serviceEndpoint) { }
    }
    /// <summary>Utility class used to inserted the <c>Content-Encoding: gzip</c> HTTP header.</summary>
    class GZipHeaderRequestInspector : IClientMessageInspector
    {
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var httpRequestMessage = new HttpRequestMessageProperty();
            // HACK: for some reason, the SOAPAction header is not properly inserted with the current config
            //httpRequestMessage.Headers.Add("SOAPAction", request.Headers.Action); 
            // adding the custom 'gzip' header
            httpRequestMessage.Headers.Add("Content-Encoding", "gzip");
            request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // do nothing
        }
    }
}
