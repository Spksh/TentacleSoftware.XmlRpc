using Microsoft.Owin;
using Owin;
using TentacleSoftware.XmlRpc.Owin;

[assembly: OwinStartup(typeof(TentacleSoftware.XmlRpc.Sample.EmptyAspNetWebApp.Startup))]
namespace TentacleSoftware.XmlRpc.Sample.EmptyAspNetWebApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Branch to new pipeline
            // Our XmlRpcMiddleware will "listen" on this URL
            app.Map("/api", app2 =>
            {
                // Handle any unhandled exceptions in the pipeline by responding with an appropriate XML-RPC <fault> struct
                //  - If your XML-RPC handler throws XmlRpcExceptions, we'll transform those into <fault> structs with the specified FaultCode and FaultString
                //  - If your XML-RPC handler throws other exceptions, we'll respond with a <fault> struct of { FaultCode = 500, FaultString = "Internal Server Error" }
                // This doesn't swallow the exception; we throw again after sending the <fault> so your application logging mechanism still has a chance to play
                app2.Use(new XmlRpcFaultMiddleware());

                // Make sure we only accept POSTs
                // This is a separate middleware in case you want to accept other verbs
                app2.Use(new HttpPostFilterMiddleware());

                // Start up our global XML-RPC listener
                // Attach SampleResponder
                app2.Use(new XmlRpcMiddleware()
                    .Add<SampleResponder>());
            });
        }
    }
}