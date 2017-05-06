using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace TentacleSoftware.XmlRpc.Owin
{
    public class HttpPostFilterMiddleware
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
                context.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                context.Response.ReasonPhrase = "Method not allowed";

                return;
            }

            await Next(environment);
        }
    }
}
