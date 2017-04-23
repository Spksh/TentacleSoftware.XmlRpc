﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TentacleSoftware.XmlRpc.Core
{
    public class XmlRpcRequestHandler
    {
        public IDictionary<string, XmlRpcMethod> Methods { get; set; }

        public XmlRpcRequestHandler()
        {
            Methods = new Dictionary<string, XmlRpcMethod>();
        }

        public XmlRpcRequestHandler(IDictionary<string, XmlRpcMethod> methods)
        {
            Methods = methods;
        }

        /// <summary>
        /// Add XML-RPC responder of type TResponder that will be instantiated for each request.
        /// </summary>
        /// <typeparam name="TResponder"></typeparam>
        /// <returns></returns>
        public XmlRpcRequestHandler Add<TResponder>() where TResponder : class, new()
        {
            return Add(() => new TResponder());
        }

        /// <summary>
        /// Add XML-RPC responder instance that will be returned for all requests.
        /// </summary>
        /// <typeparam name="TResponder"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public XmlRpcRequestHandler Add<TResponder>(TResponder instance) where TResponder : class
        {
            return Add(() => instance);
        }

        /// <summary>
        /// Add XML-RPC responder factory that will be called for each request.
        /// </summary>
        /// <typeparam name="TResponder"></typeparam>
        /// <param name="instanceFactory"></param>
        /// <returns></returns>
        public XmlRpcRequestHandler Add<TResponder>(Func<TResponder> instanceFactory) where TResponder : class
        {
            Type type = typeof(TResponder);

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<XmlRpcIgnoreAttribute>() != null)
                {
                    continue;
                }

                if (method.ReturnType == typeof(void))
                {
                    throw new ArgumentException($"Return type '{method.ReturnType}' for {type.Name}.{method.Name} is not valid for XML-RPC");
                }

                XmlRpcMethod xmlRpcMethod = new XmlRpcMethod(instanceFactory, method);

                List<XmlRpcMethodAttribute> xmlRpcMethodNames = method.GetCustomAttributes<XmlRpcMethodAttribute>().ToList();

                if (xmlRpcMethodNames.Any())
                {
                    foreach (XmlRpcMethodAttribute methodName in xmlRpcMethodNames)
                    {
                        Methods.Add(methodName.Name, xmlRpcMethod);
                    }
                }
                else
                {
                    Methods.Add($"{type.Name}.{method.Name}", xmlRpcMethod);
                }
            }

            return this;
        }

        public async Task Respond(Stream input, Stream output)
        {
            XmlRpcMethod method = null;
            object[] parameters = new object[0];

            using (XmlReader reader = XmlReader.Create(input, new XmlReaderSettings { Async = true }))
            {
                if (!await reader.ReadAsync() || reader.NodeType != XmlNodeType.XmlDeclaration)
                {
                    // Require that the first node be an XmlDeclaration
                    // This will explode anyway if the XML stream is malformed
                    // ...but we want to skip the first XmlDeclaration node, so we'll just consume it silently
                    throw new InvalidOperationException(); // TODO: Write fault? Or handle in another OWIN module?
                }

                Stack<ElementType> elements = new Stack<ElementType>();
                ElementType currentElement = ElementType.Root;

                Type[] parameterTypes = new Type[0];
                int index = 0;

                object currentValue = null;

                Type currentType = null;
                Stack<ValueContext> values = new Stack<ValueContext>();

                while (await reader.ReadAsync())
                {
                    switch (currentElement)
                    {
                        case ElementType.Root:

                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "methodCall")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.MethodCall;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.MethodCall}");
                            }

                            break;

                        case ElementType.MethodCall:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "methodName")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.MethodName;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "params")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Params;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.MethodName}, {ElementType.Params} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.MethodName:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                // Do we have a method bound to this methodName?
                                if (!Methods.TryGetValue(reader.Value, out method))
                                {
                                    throw new InvalidOperationException();
                                }

                                // No need to pop stack, we'll read the closing </methodName> next anyway
                                parameterTypes = method.ParameterTypes;
                                parameters = new object[parameterTypes.Length];
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {NodeType.Text} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Params:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();

                                // Close out params; do we have enough?
                                // We always increase our index by 1 at the end of a </param>, so if we have all params our index == parameters.length
                                if (index != parameters.Length)
                                {
                                    throw new ArgumentOutOfRangeException($"Received {index} parameters. Expected {parameterTypes.Length} parameters for this methodCall, ");
                                }
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "param")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Param;

                                // A new open param tag
                                // Are we expecting another param for this methodCall?
                                if (index >= parameters.Length)
                                {
                                    throw new ArgumentOutOfRangeException($"Received too many parameters. Expected {parameterTypes.Length} parameters for methodCall.");
                                }
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Param} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Param:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Value;

                                // We use Reflection to create new instances of complex types (e.g. for a Struct or an Array), and to do that we need a Type
                                // We update this currentType as we progress through the object graph; this is just the root value for this parameter
                                currentType = parameterTypes[index];
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Value} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Value:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();

                                // We've completed a value, now what do we do with it?
                                // Well, that depends on its parent element...

                                // This is a method parameter we'll pass directly
                                if (currentElement == ElementType.Param)
                                {
                                    // Make sure it's of the expected type
                                    if (currentValue.GetType() != parameterTypes[index])
                                    {
                                        throw new ArgumentOutOfRangeException($"Parameter of type {currentValue.GetType()} does not match expected type {parameterTypes[index]} at index {index}");
                                    }

                                    parameters[index] = currentValue;
                                    currentValue = null;

                                    // ...and then bump our index
                                    // We compare this to expected parameter array length when we close out </params> later
                                    index++;
                                }
                                // This is a member of a collection, so we'll add it to the collection
                                else if (currentElement == ElementType.Data)
                                {
                                    // Find our array in the stack, but don't pop it
                                    // We're expecting more members
                                    IList collection = values.Peek().Value as IList;

                                    if (collection == null)
                                    {
                                        throw new ArgumentNullException($"Parameter of type {values.Peek().Value.GetType()} does not implement IList");
                                    }

                                    // We may have an empty collection
                                    if (currentValue == null)
                                    {
                                        continue;
                                    }

                                    collection.Add(currentValue);
                                    currentValue = null;
                                }
                                // This is a property of a struct
                                else if (currentElement == ElementType.Member)
                                {
                                    // Find our struct in the stack, but don't pop it
                                    // We're expecting more members
                                    ValueContext value = values.Peek();

                                    if (value.Set == null)
                                    {
                                        throw new ArgumentNullException($"No setter in {values.Peek().Value.GetType()} for {currentValue.GetType()}");
                                    }

                                    // Set property value
                                    value.Set.Invoke(value.Value, new[] { currentValue });

                                    // Clear property setter in case we get a parsing error and don't find a <name> for the next <member>
                                    value.Set = null;

                                    currentValue = null;
                                }
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                // Assume values with no type are strings; this is valid as per XML-RPC spec
                                // However, we won't have a child node that contains the value, so we have to extract it here
                                // currentElement remains ElementType.Value so that, when we loop next and find the end element, we're closing off the value node correctly
                                currentValue = reader.Value;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && (reader.Name == "i4" || reader.Name == "int"))
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Int;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "boolean")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Boolean;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "string")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.String;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "double")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Double;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "dateTime.iso8601")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.DateTimeIso8601;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "base64")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Base64;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "array")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Array;

                                // Make sure we've got a class we can cast to IList
                                if (currentType == null || !typeof(IList).IsAssignableFrom(currentType))
                                {
                                    throw new ArgumentOutOfRangeException($"Parameter of type {currentType} does not implement IList.");
                                }

                                // Create new instance of our collection and add it to the stack
                                // Subsequent runs through </value> will append to this collection
                                values.Push(new ValueContext(currentType));
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "struct")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Struct;

                                // This *will* explode if we're trying to populate a non-generic List and we're using structs
                                if (currentType == null || currentType == typeof(object))
                                {
                                    throw new ArgumentOutOfRangeException($"Cannot create instance of Struct from {currentType}. Struct parameters must be strongly-typed.");
                                }

                                // Create new instance of our expected object type and add it to the stack
                                // Subsequent runs through </value> will call set on properties for this instance
                                values.Push(new ValueContext(currentType));
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException(
                                    $"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Int}, {ElementType.Boolean}, {ElementType.String}, {ElementType.Double}, {ElementType.DateTimeIso8601}, {ElementType.Base64}, {ElementType.Array}, {ElementType.Struct}, or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Int:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                currentValue = Convert.ToInt32(reader.Value);
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Boolean:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                currentValue = Convert.ToBoolean(reader.Value);
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.String:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                currentValue = reader.Value;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Double:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                currentValue = Convert.ToDouble(reader.Value);
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.DateTimeIso8601:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                currentValue = DateTime.ParseExact(reader.Value, "s", CultureInfo.InvariantCulture);
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Base64:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                currentValue = Convert.FromBase64String(reader.Value);
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Array:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();

                                // We've finished building our array, so pop it off the stack
                                // Closing </param> or </member> will shove this into a method parameter or an object property
                                ValueContext collection = values.Pop();

                                // Are we expecting a real T[] or a generic collection?
                                if (collection.Type.IsArray)
                                {
                                    // We back T[] with List<T> so we can call IList.Add() without throwing fixed size exceptions
                                    // We'll convert our backing store to a T[]
                                    IList value = (IList)collection.Value;

                                    // Create new array of correct size
                                    IList array = (IList)Activator.CreateInstance(collection.Type, value.Count);

                                    for (int i = 0; i < value.Count; i++)
                                    {
                                        array[i] = value[i];
                                    }

                                    currentValue = array;
                                }
                                else
                                {
                                    // Generic collection, just pass through the value directly
                                    currentValue = collection.Value;
                                }
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "data")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Data;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Data} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Struct:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();

                                // We've finished building our struct, so pop it off the stack
                                // Closing </param> or </member> will shove this into a method parameter or an object property
                                currentValue = values.Pop().Value;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "member")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Member;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Member} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Data:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Value;

                                // We're starting a new <array> member
                                // What's the expected type for this collection?
                                currentType = values.Peek().CollectedType;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Value} or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Member:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "name")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Name;
                            }

                            else if (reader.NodeType == XmlNodeType.Element && reader.Name == "value")
                            {
                                elements.Push(currentElement);
                                currentElement = ElementType.Value;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected {ElementType.Name}, {ElementType.Value}  or {NodeType.EndElement}");
                            }

                            break;

                        case ElementType.Name:

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                currentElement = elements.Pop();
                            }

                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                string name = reader.Value;

                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    throw new ArgumentOutOfRangeException($"{reader.Name} is empty");
                                }

                                ValueContext context = values.Peek();
                                PropertyInfo property;

                                if (!context.Properties.TryGetValue(name, out property))
                                {
                                    throw new ArgumentOutOfRangeException($"{context.Value.GetType()} has no matching public property for '{name}'");
                                }

                                MethodInfo setMethod = property.GetSetMethod();

                                if (setMethod == null)
                                {
                                    throw new ArgumentOutOfRangeException($"{context.Value.GetType()} has no public setter for property '{name}'");
                                }

                                // Store setter so we can use it later when </value> closes
                                context.Set = setMethod;

                                // What's the expected type of this property?
                                currentType = property.PropertyType;
                            }

                            else if (reader.NodeType != XmlNodeType.Whitespace)
                            {
                                throw new ArgumentOutOfRangeException($"<{reader.NodeType}, {reader.Name}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                            }

                            break;
                    }
                }
            }

            object result = method.Invoke(parameters);

            if (result == null)
            {
                throw new ArgumentException($"Returned value for {method.DeclaringType}.{method.Name} is null. Expected {method.ReturnType}");
            }

            using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Async = true, Encoding = Encoding.UTF8, Indent = true })) // TODO: Drop indent
            {
                await writer.WriteStartDocumentAsync();

                await writer.WriteStartElementAsync(null, "methodResponse", null);
                await writer.WriteStartElementAsync(null, "params", null);
                await writer.WriteStartElementAsync(null, "param", null);

                foreach (Token token in result.Tokenize())
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
                await writer.WriteEndElementAsync();
                await writer.WriteEndElementAsync();

                await writer.WriteEndDocumentAsync();
            }
        }
    }
}