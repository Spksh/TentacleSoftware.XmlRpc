using System.Diagnostics;
using Microsoft.Owin;
using Owin;
using TentacleSoftware.XmlRpc.Core;
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
                //  Handle any unhandled exceptions in the pipeline by responding with an appropriate XML-RPC fault struct, as per XML-RPC spec.
                //  If your XML-RPC handler throws XmlRpcExceptions, we'll transform those into fault a struct with the specified FaultCode and FaultString.
                //  If your XML-RPC handler throws other exceptions, we'll respond with a fault struct of { FaultCode = 500, FaultString = "Internal Server Error" }.
                app2.Use(new XmlRpcFaultMiddleware()
                    // Do some logging
                    .OnFaulted((s, e) =>
                    {
                        XmlRpcException fault = e as XmlRpcException;

                        if (fault != null)
                        {
                            Trace.WriteLine($"FAULT: {fault.Code} {fault.Message}");

                            if (fault.InnerException != null)
                            {
                                Trace.WriteLine($"ERROR: {fault.InnerException.Message}");
                            }

                            Trace.WriteLine(fault.StackTrace);
                        }
                        else
                        {
                            Trace.WriteLine($"ERROR: {e.Source} {e.Message}");
                            Trace.WriteLine(e.StackTrace);
                        }
                    }));

                // Make sure we only accept POSTs and text/xml, as per XML-RPC spec
                // This is a separate middleware because, depending on how conformant your clients are, you might want to do more or less filtering
                app2.Use(new XmlRpcRequestFilterMiddleware());

                // Start up our global XML-RPC listener
                // Attach SampleResponder
                app2.Use(new XmlRpcMiddleware()
                    .Add<SampleResponder>());
            });
        }
    }
}