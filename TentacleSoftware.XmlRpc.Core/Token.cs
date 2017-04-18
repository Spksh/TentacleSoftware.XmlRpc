namespace TentacleSoftware.XmlRpc.Core
{
    public struct Token
    {
        public NodeType NodeType { get; set; }

        public ElementType ElementType { get; set; }

        public string Value { get; set; }

        public Token(NodeType node, ElementType element, string value)
        {
            NodeType = node;
            ElementType = element;
            Value = value;
        }

        public static Token ValueToken = new Token { NodeType = NodeType.Element, ElementType = ElementType.Value };
    }
}
