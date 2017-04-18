using System;

namespace TentacleSoftware.XmlRpc.Core
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class XmlRpcParameterAttribute : Attribute
    {
        public bool IsNullable { get; set; }
    }
}
