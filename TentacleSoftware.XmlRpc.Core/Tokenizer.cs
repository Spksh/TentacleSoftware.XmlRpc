using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace TentacleSoftware.XmlRpc.Core
{
    public static class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(this object target)
        {
            if (target == null)
            {
                yield break;
            }
            
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
                        object propertyValue = getMethod.Invoke(target, null);

                        // Don't try to serialize a null value
                        // We won't include this <member> at all
                        if (propertyValue == null)
                        {
                            continue;
                        }

                        yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Member };

                        XmlRpcMemberAttribute xmlRpcMember = property.GetCustomAttribute<XmlRpcMemberAttribute>();

                        // In death, a member of Project Struct has a name
                        yield return new Token { NodeType = NodeType.Element, ElementType = ElementType.Name };
                        yield return new Token { NodeType = NodeType.Text, Value = xmlRpcMember != null ? xmlRpcMember.Name : property.Name };
                        yield return new Token { NodeType = NodeType.EndElement, ElementType = ElementType.Name };

                        // Wrap value in appropriate tokens
                        foreach (Token value in Tokenize(propertyValue))
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
