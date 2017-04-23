using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Owin
{
    public class XmlRpcMiddleware
    {
        public Func<IDictionary<string, object>, Task> Next { get; set; }

        public XmlRpcRequestHandler Handler { get; set; }

        public XmlRpcMiddleware()
        {
            Handler = new XmlRpcRequestHandler();
        }

        public XmlRpcMiddleware(XmlRpcRequestHandler handler)
        {
            Handler = handler;
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
            Handler.Add(instanceFactory);

            return this;
        }

        public void Initialize(Func<IDictionary<string, object>, Task> next)
        {
            Next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            // TODO: Check HTTP verb
            // TODO: Catch exceptions, respond with failure struct
            // TODO: HTTP headers, XML content type
            await Handler.Respond(context.Request.Body, context.Response.Body);
            
            // TODO: If we sent a response, this is the end of the pipeline. Do we have other cases where we want to pass through to Next()?
            // await Next(environment);
        }
    }
}
