using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace TentacleSoftware.XmlRpc.Core
{
    public static class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(this Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { Async = false }))
            {
                if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
                {
                    // Require that the first node be an XmlDeclaration
                    // This will explode anyway if the XML stream is malformed
                    // ...but we want to skip the first XmlDeclaration node, so we'll just consume it silently
                    throw new InvalidOperationException();
                }

                Stack<ElementType> elements = new Stack<ElementType>();
                ElementType currentElement = ElementType.Root;

                while (reader.Read())
                {
                    switch (currentElement)
                    {
                        case ElementType.Root:

                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "methodCall")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.MethodCall;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "methodResponse")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.MethodResponse;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.MethodCall:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "methodName")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.MethodName;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "params")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Params;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.MethodResponse:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "params")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Params;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Params:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "param")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Param;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Param:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Value;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Value:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                // Assume values with no type are strings; this is valid as per XML-RPC spec
                                // However, we won't have a child node that contains the value, so we have to extract it here and fake it for consistency with the rest of the pipeline
                                // currentValue remains ElementType.Value so that, when we loop next and find the end element, we're closing off the value node correctly
                                yield return new Token { ElementType = ElementType.String };
                                yield return new Token { NodeType = NodeType.Text, ElementType = ElementType.String, Value = reader.Value };
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.String };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "string")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.String;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "array")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Array;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "struct")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Struct;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && (reader.Name == "i4" || reader.Name == "int"))
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Int;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "boolean")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Boolean;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "double")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Double;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "dateTime.iso8601")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.DateTimeIso8601;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "base64")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Base64;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Array:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "data")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Data;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Data:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Value;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Struct:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "member")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Member;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Member:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "name")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Name;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Value;

                                yield return new Token { ElementType = currentElement };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;

                        case ElementType.Name:
                        case ElementType.MethodName:
                        case ElementType.String:
                        case ElementType.Int:
                        case ElementType.Boolean:
                        case ElementType.Double:
                        case ElementType.DateTimeIso8601:
                        case ElementType.Base64:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                yield return new Token { NodeType = NodeType.EndElement, ElementType = currentElement };

                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                // Text nodes are the end of a branch, and don't have a closing tag
                                // Therefore, we don't throw them into the stack
                                yield return new Token { NodeType = NodeType.Text, ElementType = currentElement, Value = reader.Value };
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new InvalidOperationException();
                            }

                            break;
                    }
                }
            }
        }

        public static IEnumerable<Token> Tokenize(this object target)
        {
            // https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx
            if (target is bool)
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Boolean };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(target) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Boolean };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is byte)
            {
                // Unsigned
                throw new ArgumentException("target is byte, use sbyte instead", nameof(target));
            }
            else if (target is sbyte)
            {
                // Signed 8-bit
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Int };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(Convert.ToInt32(target)) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Int };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is char)
            {
                // Unicode 16-bit
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.String };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(target) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.String };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is decimal)
            {
                // Data loss, use double
                throw new ArgumentException("target is decimal, conversion to double may result in loss of precision", nameof(target));
            }
            else if (target is double)
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Double };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(target) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Double };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is float)
            {
                // 32-bit, convert to double
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Double };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(Convert.ToDouble(target), CultureInfo.InvariantCulture) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Double };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is int)
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Int };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(target) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Int };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
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
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Int };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(Convert.ToInt32(target)) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Int };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is ushort)
            {
                // Unsigned 16-bit
                throw new ArgumentException("target is ushort, use short instead", nameof(target));
            }
            else if (target is string)
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.String };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToString(target) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.String };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is DateTime)
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.DateTimeIso8601 };
                yield return new Token { NodeType = NodeType.Text, Value = ((DateTime)target).ToString("s", CultureInfo.InvariantCulture) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.DateTimeIso8601 };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is byte[])
            {
                // Base64
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Base64 };
                yield return new Token { NodeType = NodeType.Text, Value = Convert.ToBase64String((byte[])target) };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Base64 };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else if (target is IEnumerable)
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Array };
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Data };

                foreach (object item in (IEnumerable)target)
                {
                    foreach (Token member in item.Tokenize())
                    {
                        yield return member;
                    }
                }

                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Data };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Array };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
            else
            {
                yield return Token.ValueToken;
                yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Struct };

                foreach (PropertyInfo property in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    MethodInfo getMethod = property.GetGetMethod();

                    if (getMethod != null && property.GetCustomAttribute<XmlRpcIgnoreAttribute>() == null)
                    {
                        yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Member };

                        XmlRpcMemberAttribute xmlRpcMember = property.GetCustomAttribute<XmlRpcMemberAttribute>();

                        // In death, a member of Project Struct has a name
                        yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Name };
                        yield return new Token { NodeType = NodeType.Text, Value = xmlRpcMember != null ? xmlRpcMember.Name : property.Name };
                        yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Name };

                        // Wrap value in appropriate tokens
                        foreach (Token value in Tokenize(getMethod.Invoke(target, null)))
                        {
                            yield return value;
                        }

                        yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Member };
                    }
                }

                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Struct };
                yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Value };
            }
        }
    }
}
