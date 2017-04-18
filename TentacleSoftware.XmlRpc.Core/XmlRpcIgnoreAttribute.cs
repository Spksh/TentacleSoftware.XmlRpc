using System;

namespace TentacleSoftware.XmlRpc.Core
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class XmlRpcIgnoreAttribute : Attribute
    {
    }
}
