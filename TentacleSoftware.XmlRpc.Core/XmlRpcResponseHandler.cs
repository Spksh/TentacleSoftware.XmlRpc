using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TentacleSoftware.XmlRpc.Core
{
    public class XmlRpcResponseHandler
    {
        public async Task RespondWith(object response, Stream output)
        {
            using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Async = true, Encoding = Encoding.UTF8, Indent = true })) // TODO: Drop indent
            {
                await writer.WriteStartDocumentAsync();

                await writer.WriteStartElementAsync(null, "methodResponse", null);

                if (response is XmlRpcFault)
                {
                    await writer.WriteStartElementAsync(null, "fault", null);
                }
                else
                {
                    await writer.WriteStartElementAsync(null, "params", null);
                    await writer.WriteStartElementAsync(null, "param", null);
                }

                foreach (Token token in response.Tokenize())
                {
                    if (token.NodeType == NodeType.EndElement)
                    {
                        await writer.WriteEndElementAsync();
                    }
                    else if (token.NodeType == NodeType.Text)
                    {
                        await writer.WriteStringAsync(token.Value);
                    }
                    else if (token.ElementType == ElementType.Int)
                    {
                        await writer.WriteStartElementAsync(null, "i4", null);
                    }
                    else if (token.ElementType == ElementType.DateTimeIso8601)
                    {
                        await writer.WriteStartElementAsync(null, "dateTime.iso8601", null);
                    }
                    else if (token.NodeType == NodeType.Element)
                    {
                        await writer.WriteStartElementAsync(null, token.ElementType.ToString().ToLowerInvariant(), null);
                    }
                }

                await writer.WriteEndElementAsync();

                if (response is XmlRpcFault)
                {
                    await writer.WriteEndElementAsync();
                }
                else
                {
                    await writer.WriteEndElementAsync();
                    await writer.WriteEndElementAsync();
                }

                await writer.WriteEndDocumentAsync();
                await writer.FlushAsync();
            }
        }
    }
}
