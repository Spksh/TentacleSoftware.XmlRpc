using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TentacleSoftware.XmlRpc.Core
{
    public class XmlRpcResponseHandler
    {
        public async Task RespondWith(object response, Stream output)
        {
            using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Async = true, Encoding = new UTF8Encoding(/* Do not write BOM to stream */ false), Indent = true }))
            {
                // We could be writing async for each and every element
                // However, we incur some overhead for each await and Microsoft seems to recommend a threshold of 50 ms for async operations:
                // - https://blogs.msdn.microsoft.com/windowsappdev/2012/03/20/keeping-apps-fast-and-fluid-with-asynchrony-in-the-windows-runtime/
                // - http://blog.stephencleary.com/2013/04/ui-guidelines-for-async.html
                //
                // So, we'll only write potentially large value elements async:
                // - base64 (because the value is probably a large binary blob)
                // - string (because the value could be long)
                await writer.WriteXmlRpcResponseAsync(response);
                await writer.FlushAsync();
            }
        }
    }

    public static class XmlWriterExtensions
    {
        public static async Task WriteXmlRpcResponseAsync(this XmlWriter writer, object response)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("methodResponse");

            if (response is XmlRpcFault)
            {
                writer.WriteStartElement("fault");
            }
            else
            {
                writer.WriteStartElement("params");
                writer.WriteStartElement("param");
            }

            await writer.WriteXmlRpcObjectAsync(response);

            // Close our open <fault> or <params><param> elements
            writer.WriteEndDocument();
        }

        public static async Task WriteXmlRpcObjectAsync(this XmlWriter writer, object target)
        {
            if (target == null)
            {
                return;
            }

            // Built-In Types Table (C# Reference)
            // https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx
            if (target is bool)
            {
                writer.WriteXmlRpcValue("boolean", Convert.ToString(target));
            }
            else if (target is byte)
            {
                // Unsigned
                throw new ArgumentException("target is byte, use sbyte instead", nameof(target));
            }
            else if (target is sbyte)
            {
                // Signed 8-bit
                writer.WriteXmlRpcValue("i4", Convert.ToString(Convert.ToInt32(target)));
            }
            else if (target is char)
            {
                // Unicode 16-bit
                writer.WriteXmlRpcValue("string", Convert.ToString(target));
            }
            else if (target is decimal)
            {
                // Data loss, use double
                throw new ArgumentException("target is decimal, conversion to double may result in loss of precision", nameof(target));
            }
            else if (target is double)
            {
                writer.WriteXmlRpcValue("double", Convert.ToString(Convert.ToDouble(target), CultureInfo.InvariantCulture));
            }
            else if (target is float)
            {
                // 32-bit, convert to double
                writer.WriteXmlRpcValue("double", Convert.ToString(Convert.ToDouble(target), CultureInfo.InvariantCulture));
            }
            else if (target is int)
            {
                writer.WriteXmlRpcValue("i4", Convert.ToString(Convert.ToInt32(target)));
            }
            else if (target is uint)
            {
                // Unsigned
                throw new ArgumentException("target is uint, use signed 32-bit integer instead", nameof(target));
            }
            else if (target is long)
            {
                // Signed 64-bit
                throw new ArgumentException("target is long, use signed 32-bit integer instead", nameof(target));
            }
            else if (target is ulong)
            {
                // Unsigned
                throw new ArgumentException("target is ulong, use signed 32-bit integer instead", nameof(target));
            }
            else if (target is short)
            {
                // Signed 16-bit
                writer.WriteXmlRpcValue("i4", Convert.ToString(Convert.ToInt32(target)));
            }
            else if (target is ushort)
            {
                // Unsigned 16-bit
                throw new ArgumentException("target is ushort, use short instead", nameof(target));
            }
            else if (target is string)
            {
                await writer.WriteXmlRpcValueAsync("string", Convert.ToString(target));

            }
            else if (target is DateTime)
            {
                writer.WriteXmlRpcValue("dateTime.iso8601", ((DateTime)target).ToString("s", CultureInfo.InvariantCulture));
            }
            else if (target is byte[])
            {
                // Base64
                await writer.WriteXmlRpcValueAsync("base64", Convert.ToBase64String((byte[])target));
            }
            else if (target is IEnumerable)
            {
                writer.WriteStartElement("value");
                writer.WriteStartElement("array");
                writer.WriteStartElement("data");

                foreach (object item in (IEnumerable)target)
                {
                    await writer.WriteXmlRpcObjectAsync(item);
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartElement("value");
                writer.WriteStartElement("struct");

                foreach (PropertyInfo property in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    MethodInfo getMethod = property.GetGetMethod();

                    if (getMethod != null && property.GetCustomAttribute<XmlRpcIgnoreAttribute>() == null)
                    {
                        object propertyValue = getMethod.Invoke(target, null);

                        // Don't try to serialize a null value
                        // We won't include this <member> at all
                        if (propertyValue == null)
                        {
                            continue;
                        }

                        writer.WriteStartElement("member");

                        XmlRpcMemberAttribute xmlRpcMember = property.GetCustomAttribute<XmlRpcMemberAttribute>();

                        // In death, a member of Project Struct has a name
                        writer.WriteStartElement("name");
                        writer.WriteString(xmlRpcMember != null ? xmlRpcMember.Name : property.Name);
                        writer.WriteEndElement();

                        await writer.WriteXmlRpcObjectAsync(propertyValue);

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        public static void WriteXmlRpcValue(this XmlWriter writer, string type, string value)
        {
            writer.WriteStartElement("value");
            writer.WriteStartElement(type);
            writer.WriteString(value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        public static async Task WriteXmlRpcValueAsync(this XmlWriter writer, string type, string value)
        {
            writer.WriteStartElement("value");
            writer.WriteStartElement(type);
            await writer.WriteStringAsync(value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
