using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Owin
{
    public class XmlRpcFaultMiddleware
    {
        public Func<IDictionary<string, object>, Task> Next { get; set; }

        public XmlRpcResponseHandler ResponseHandler { get; set; }

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

                XmlRpcException xmlRpcException = error as XmlRpcException;

                if (xmlRpcException != null)
                {
                    await ResponseHandler.RespondWith(new XmlRpcFault { FaultCode = xmlRpcException.Code, FaultString = xmlRpcException.Message }, context.Response.Body);
                }
                else
                {
                    // If we don't have an explicit XmlRpcException, just respond with generic HTTP 500 error to avoid exposing internals
                    await ResponseHandler.RespondWith(new XmlRpcFault { FaultCode = (int) HttpStatusCode.InternalServerError, FaultString = "Internal Server Error" }, context.Response.Body);
                }

                // Dear Global Unhandled Exception Handler, I have a present for you...
                throw;
            }
        }
    }
}
