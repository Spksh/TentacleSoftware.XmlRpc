using System;

namespace TentacleSoftware.XmlRpc.Core
{
    [AttributeUsage(AttributeTargets.Property)]
    public class XmlRpcMemberAttribute : Attribute
    {
        public string Name { get; set; }

        public XmlRpcMemberAttribute(string name)
        {
            Name = name;
        }
    }
}
