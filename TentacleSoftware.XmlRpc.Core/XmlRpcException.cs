using System;

namespace TentacleSoftware.XmlRpc.Core
{
    public class XmlRpcException : Exception
    {
        public int Code { get; set; }

        public XmlRpcException()
        {
        }

        public XmlRpcException(int code, string message) : base(message)
        {
            Code = code;
        }

        public XmlRpcException(int code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}
