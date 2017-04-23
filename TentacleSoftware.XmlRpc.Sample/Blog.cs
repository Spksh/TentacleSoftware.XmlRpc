using System.Collections.Generic;
using TentacleSoftware.XmlRpc.Core;

namespace TentacleSoftware.XmlRpc.Sample
{
    public class Blog
    {
        [XmlRpcMember("blogid")]
        public string BlogId { get; set; }

        [XmlRpcMember("url")]
        public string Url { get; set; }

        [XmlRpcMember("blogName")]
        public string BlogName { get; set; }

        [XmlRpcIgnore]
        public bool IsAdmin { get; set; }

        [XmlRpcMember("xmlrpc")]
        public string XmlRpc { get; set; }

        [XmlRpcMember("users")]
        public List<User> Users { get; set; }
    }
}
