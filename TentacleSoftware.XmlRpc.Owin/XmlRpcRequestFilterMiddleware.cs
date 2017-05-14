using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace TentacleSoftware.XmlRpc.Owin
{
    /// <summary>
    /// Make sure we only accept POSTs and text/xml, as per XML-RPC spec.
    /// This is a separate middleware because, depending on how conformant your clients are, you might want to do more or less filtering.
    /// </summary>
    public class XmlRpcRequestFilterMiddleware
    {
        public Func<IDictionary<string, object>, Task> Next { get; set; }

        public void Initialize(Func<IDictionary<string, object>, Task> next)
        {
            Next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            if (context.Request.Method != HttpMethod.Post.Method)
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                context.Response.ReasonPhrase = "Method not allowed";

                return;
            }

            if (new ContentType(context.Request.ContentType).MediaType != MediaTypeNames.Text.Xml)
            {
                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                context.Response.ReasonPhrase = "Unsupported Media Type";

                return;
            }

            await Next(environment);
        }
    }
}
