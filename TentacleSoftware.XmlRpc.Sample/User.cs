using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Sample
{
    public class User
    {
        [XmlRpcMember("userId")]
        public int UserId { get; set; }

        [XmlRpcMember("name")]
        public string Name { get; set; }
    }
}
