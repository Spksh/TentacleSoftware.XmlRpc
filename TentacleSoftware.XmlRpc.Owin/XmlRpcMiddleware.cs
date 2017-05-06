using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Owin
{
    public class XmlRpcMiddleware
    {
        public Func<IDictionary<string, object>, Task> Next { get; set; }

        public XmlRpcRequestHandler RequestHandler { get; set; }

        public XmlRpcResponseHandler ResponseHandler { get; set; }

        public XmlRpcMiddleware()
        {
            RequestHandler = new XmlRpcRequestHandler();
            ResponseHandler = new XmlRpcResponseHandler();
        }

        public XmlRpcMiddleware(XmlRpcRequestHandler requestHandler, XmlRpcResponseHandler responseHandler)
        {
            RequestHandler = requestHandler;
            ResponseHandler = responseHandler;
        }

        /// <summary>
        /// Add XML-RPC responder of type TResponder that will be instantiated for each request.
        /// </summary>
        /// <typeparam name="TResponder"></typeparam>
        /// <returns></returns>
        public XmlRpcMiddleware Add<TResponder>() where TResponder : class, new()
        {
            return Add(() => new TResponder());
        }

        /// <summary>
        /// Add XML-RPC responder instance that will be returned for all requests.
        /// </summary>
        /// <typeparam name="TResponder"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public XmlRpcMiddleware Add<TResponder>(TResponder instance) where TResponder : class
        {
            return Add(() => instance);
        }

        /// <summary>
        /// Add XML-RPC responder factory that will be called for each request.
        /// </summary>
        /// <typeparam name="TResponder"></typeparam>
        /// <param name="instanceFactory"></param>
        /// <returns></returns>
        public XmlRpcMiddleware Add<TResponder>(Func<TResponder> instanceFactory) where TResponder : class
        {
            RequestHandler.Add(instanceFactory);

            return this;
        }

        public void Initialize(Func<IDictionary<string, object>, Task> next)
        {
            Next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            context.Response.StatusCode = (int) HttpStatusCode.OK;
            context.Response.ContentType = "text/xml";

            await ResponseHandler.RespondWith(
                await RequestHandler.RespondTo(context.Request.Body), 
                context.Response.Body
            );

            // We don't invoke Next() at all because we're the end of the pipeline
            // However, we still include the public property for Next to conform with OWIN conventions for middleware classes
        }
    }
}
