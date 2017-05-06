namespace TentacleSoftware.XmlRpc.Core
{
    public class XmlRpcFault
    {
        [XmlRpcMember("faultCode")]
        public int FaultCode { get; set; }

        [XmlRpcMember("faultString")]
        public string FaultString { get; set; }
    }
}
