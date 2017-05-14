using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Owin
{
    /// <summary>
    ///  Handle any unhandled exceptions in the pipeline by responding with an appropriate XML-RPC fault struct, as per XML-RPC spec.
    ///  This middleware swallows exceptions unless you subscribe to its Faulted event.
    ///  If your XML-RPC handler throws XmlRpcExceptions, we'll transform those into fault a struct with the specified FaultCode and FaultString.
    ///  If your XML-RPC handler throws other exceptions, we'll respond with a fault struct of { FaultCode = 500, FaultString = "Internal Server Error" }.
    /// </summary>
    public class XmlRpcFaultMiddleware
    {
        public Func<IDictionary<string, object>, Task> Next { get; set; }

        public XmlRpcResponseHandler ResponseHandler { get; set; }

        /// <summary>
        /// This event is raised after we've handled the exception and responded to the client with an acceptable XML-RPC fault struct.
        /// </summary>
        public EventHandler<Exception> Faulted;

        public XmlRpcFaultMiddleware()
        {
            ResponseHandler = new XmlRpcResponseHandler();
        }

        public XmlRpcFaultMiddleware(XmlRpcResponseHandler responseHandler)
        {
            ResponseHandler = responseHandler;
        }

        public void Initialize(Func<IDictionary<string, object>, Task> next)
        {
            Next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            try
            {
                await Next(environment);
            }
            catch (Exception error)
            {
                IOwinContext context = new OwinContext(environment);

                // All XML-RPC faults must return HTTP 200 OK
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "text/xml";

                // Enumerate InnerExceptions and AggregateExceptions to see if we've thrown an XmlRpcException anywhere
                XmlRpcException xmlRpcException = error.Enumerate().FirstOrDefault(e => e is XmlRpcException) as XmlRpcException;

                if (xmlRpcException != null)
                {
                    // Use the fault code and message for our <fault> struct
                    await ResponseHandler.RespondWith(new XmlRpcFault { FaultCode = xmlRpcException.Code, FaultString = xmlRpcException.Message }, context.Response.Body);
                }
                else
                {
                    // If we don't have an explicit XmlRpcException, just respond with generic HTTP 500 error to avoid exposing internals
                    await ResponseHandler.RespondWith(new XmlRpcFault { FaultCode = (int) HttpStatusCode.InternalServerError, FaultString = "Internal Server Error" }, context.Response.Body);
                }

                // Dear Global Unhandled Exception Handler, I have a present for you...
                OnFaulted(error);
            }
        }

        public XmlRpcFaultMiddleware OnFaulted(EventHandler<Exception> onFaulted)
        {
            Faulted += onFaulted;

            return this;
        }

        protected virtual void OnFaulted(Exception error)
        {
            EventHandler<Exception> faulted = Faulted;

            if (faulted != null)
            {
                faulted(this, error);
            }
        }
    }
}
