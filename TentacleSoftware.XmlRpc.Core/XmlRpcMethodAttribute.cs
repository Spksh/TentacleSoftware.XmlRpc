using System;

namespace TentacleSoftware.XmlRpc.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class XmlRpcMethodAttribute : Attribute
    {
        public string Name { get; set; }

        public XmlRpcMethodAttribute(string name)
        {
            Name = name;
        }
    }
}
