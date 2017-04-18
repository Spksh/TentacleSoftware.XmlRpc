using System;
using System.Linq;
using System.Reflection;

namespace TentacleSoftware.XmlRpc.Core
{
    public class XmlRpcMethod
    {
        private readonly Func<object> _instanceFactory;
        private readonly MethodInfo _method;

        public Type ReturnType { get; private set; }

        public Type[] ParameterTypes { get; private set; }

        public XmlRpcMethod(Func<object> instanceFactory, MethodInfo method)
        {
            _instanceFactory = instanceFactory;
            _method = method;

            ReturnType = method.ReturnType;
            ParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        }

        public object Invoke(params object[] objects)
        {
            return _method.Invoke(_instanceFactory(), objects);
        }
    }
}
