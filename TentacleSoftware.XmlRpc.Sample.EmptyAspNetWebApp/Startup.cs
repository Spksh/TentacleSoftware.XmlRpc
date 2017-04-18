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
            app.Map("/api", app2 =>
            {
                app2.Use(new XmlRpcMiddleware()
                    .Add<SampleResponder>());
            });
        }
    }
}