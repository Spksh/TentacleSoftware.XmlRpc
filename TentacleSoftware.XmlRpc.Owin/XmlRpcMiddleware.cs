using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Owin;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Owin
{
    public class XmlRpcMiddleware
    {
        public Func<IDictionary<string, object>, Task> Next { get; set; }

        public Dictionary<string, XmlRpcMethod> Methods = new Dictionary<string, XmlRpcMethod>();

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
        /// <param name="factory"></param>
        /// <returns></returns>
        public XmlRpcMiddleware Add<TResponder>(Func<TResponder> factory) where TResponder : class
        {
            Type type = typeof(TResponder);

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<XmlRpcIgnoreAttribute>() != null)
                {
                    continue;
                }

                XmlRpcMethod xmlRpcMethod = new XmlRpcMethod(factory, method);

                List<XmlRpcMethodAttribute> xmlRpcMethodNames = method.GetCustomAttributes<XmlRpcMethodAttribute>().ToList();

                if (xmlRpcMethodNames.Any())
                {
                    foreach (XmlRpcMethodAttribute methodName in xmlRpcMethodNames)
                    {
                        Methods.Add(methodName.Name, xmlRpcMethod);
                    }
                }
                else
                {
                    Methods.Add($"{type.Name}.{method.Name}", xmlRpcMethod);
                }
            }

            return this;
        }

        public void Initialize(Func<IDictionary<string, object>, Task> next)
        {
            Next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            await context.Response.WriteAsync("XML-RPC World");

            // This is the end of the pipeline
            //await Next(environment);
        }
    }
}
