using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TentacleSoftware.XmlRpc.Core;
using TentacleSoftware.XmlRpc.Owin;
using TentacleSoftware.XmlRpc.Sample;

namespace TentacleSoftware.XmlRpc.Test
{
    [TestFixture]
    public class DeserializeFixture
    {
        [Test, TestCaseSource(typeof(MethodCallTestData), nameof(MethodCallTestData.TestCases))]
        public void Deserialize(string xml)
        {
            XmlRpcMiddleware middleware = new XmlRpcMiddleware()
                .Add<SampleResponder>();

            foreach (var xmlRpcMethod in middleware.Methods)
            {
                Console.WriteLine(xmlRpcMethod.Key);
                Console.WriteLine(xmlRpcMethod.Value.ReturnType);
                Console.WriteLine(string.Join(", ", xmlRpcMethod.Value.ParameterTypes.ToList()));
                Console.WriteLine("------------------");
            }

            Stack<ElementType> elements = new Stack<ElementType>();
            ElementType currentElement = ElementType.Root;

            XmlRpcMethod method = null;
            Type[] parameterTypes = new Type[0];
            object[] parameters = new object[0];
            int index = 0;

            object currentValue = null;

            Type currentType = null;
            Stack<ValueContext> values = new Stack<ValueContext>();

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                using (IEnumerator<Token> enumerator = stream.Tokenize().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Console.WriteLine($"{enumerator.Current.NodeType} <{enumerator.Current.ElementType}> {enumerator.Current.Value}");

                        Token node = enumerator.Current;

                        switch (currentElement)
                        {
                            case ElementType.Root:

                                if (node.ElementType == ElementType.MethodCall)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.MethodCall;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.MethodCall}");
                                }

                                break;

                            case ElementType.MethodCall:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == ElementType.MethodName)
                                {
                                    elements.Push(currentElement);
                                    currentElement = node.ElementType;
                                }

                                else if (node.ElementType == ElementType.Params)
                                {
                                    elements.Push(currentElement);
                                    currentElement = node.ElementType;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.MethodName}, {ElementType.Params} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.MethodName:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == ElementType.MethodName && node.NodeType == NodeType.Text)
                                {
                                    // Do we have a method bound to this methodName?
                                    if (!middleware.Methods.TryGetValue(node.Value, out method))
                                    {
                                        throw new InvalidOperationException();
                                    }

                                    // No need to pop stack, we'll read the closing </methodName> next anyway
                                    parameterTypes = method.ParameterTypes;
                                    parameters = new object[parameterTypes.Length];
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {NodeType.Text} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Params:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();

                                    // Close out params; do we have enough?
                                    // We always increase our index by 1 at the end of a </param>, so if we have all params our index == parameters.length
                                    if (index != parameters.Length)
                                    {
                                        throw new ArgumentOutOfRangeException($"Received {index} parameters. Expected {parameterTypes.Length} parameters for this methodCall, ");
                                    }
                                }

                                else if (node.ElementType == ElementType.Param)
                                {
                                    elements.Push(currentElement);
                                    currentElement = node.ElementType;

                                    // A new open param tag
                                    // Are we expecting another param for this methodCall?
                                    if (index >= parameters.Length)
                                    {
                                        throw new ArgumentOutOfRangeException($"Received too many parameters. Expected {parameterTypes.Length} parameters for methodCall.");
                                    }
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Param} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Param:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == ElementType.Value)
                                {
                                    elements.Push(currentElement);
                                    currentElement = node.ElementType;

                                    // We use Reflection to create new instances of complex types (e.g. for a Struct or an Array), and to do that we need a Type
                                    // We update this currentType as we progress through the object graph; this is just the root value for this parameter
                                    currentType = parameterTypes[index];
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Value} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Value:

                                if (node.NodeType == NodeType.EndElement)
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

                                else if (node.ElementType == ElementType.Int)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Int;
                                }

                                else if (node.ElementType == ElementType.Boolean)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Boolean;
                                }

                                else if (node.ElementType == ElementType.String)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.String;
                                }

                                else if (node.ElementType == ElementType.Double)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Double;
                                }

                                else if (node.ElementType == ElementType.DateTimeIso8601)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.DateTimeIso8601;
                                }

                                else if (node.ElementType == ElementType.Base64)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Base64;
                                }

                                else if (node.ElementType == ElementType.Array)
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

                                else if (node.ElementType == ElementType.Struct)
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

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Int}, {ElementType.Boolean}, {ElementType.String}, {ElementType.Double}, {ElementType.DateTimeIso8601}, {ElementType.Base64}, {ElementType.Array}, {ElementType.Struct}, or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Int:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    currentValue = Convert.ToInt32(node.Value);
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Boolean:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    currentValue = Convert.ToBoolean(node.Value);
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.String:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    currentValue = node.Value;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Double:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    currentValue = Convert.ToDouble(node.Value);
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.DateTimeIso8601:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    currentValue = DateTime.ParseExact(node.Value, "s", CultureInfo.InvariantCulture);
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Base64:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    currentValue = Convert.FromBase64String(node.Value);
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Array:

                                if (node.NodeType == NodeType.EndElement)
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

                                else if (node.ElementType == ElementType.Data)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Data;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Data} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Struct:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();

                                    // We've finished building our struct, so pop it off the stack
                                    // Closing </param> or </member> will shove this into a method parameter or an object property
                                    currentValue = values.Pop().Value;
                                }

                                else if (node.ElementType == ElementType.Member)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Member;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Member} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Data:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == ElementType.Value)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Value;

                                    // We're starting a new <array> member
                                    // What's the expected type for this collection?
                                    currentType = values.Peek().CollectedType;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Value} or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Member:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == ElementType.Name)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Name;
                                }

                                else if (node.ElementType == ElementType.Value)
                                {
                                    elements.Push(currentElement);
                                    currentElement = ElementType.Value;
                                }

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected {ElementType.Name}, {ElementType.Value}  or {NodeType.EndElement}");
                                }

                                break;

                            case ElementType.Name:

                                if (node.NodeType == NodeType.EndElement)
                                {
                                    currentElement = elements.Pop();
                                }

                                else if (node.ElementType == currentElement && node.NodeType == NodeType.Text)
                                {
                                    string name = node.Value;

                                    if (string.IsNullOrWhiteSpace(name))
                                    {
                                        throw new ArgumentOutOfRangeException($"{node.ElementType} is empty");
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

                                else
                                {
                                    throw new ArgumentOutOfRangeException($"<{node.NodeType}, {node.ElementType}> is unexpected. Expected <{NodeType.Text}, {currentElement}> or {NodeType.EndElement}");
                                }

                                break;
                        }
                    }
                }

                foreach (object parameter in parameters)
                {
                    Console.WriteLine($"{parameter} as {parameter.GetType()}");

                    IEnumerable items = parameter as IEnumerable;

                    if (items != null)
                    {
                        foreach (object item in items)
                        {
                            Console.WriteLine(" - " + item);

                            Blog blog = item as Blog;

                            if (blog != null)
                            {
                                Console.WriteLine("  - " + blog.BlogId);
                                Console.WriteLine("  - " + blog.BlogName);
                                Console.WriteLine("  - " + blog.Url);

                                if (blog.Users != null)
                                {
                                    foreach (User user in blog.Users)
                                    {
                                        Console.WriteLine("   - " + user.UserId);
                                        Console.WriteLine("   - " + user.Name);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("  - None");
                                }
                            }

                        }
                    }

                    Blog blog2 = parameter as Blog;

                    if (blog2 != null)
                    {
                        Console.WriteLine("  - " + blog2.BlogId);
                        Console.WriteLine("  - " + blog2.BlogName);
                        Console.WriteLine("  - " + blog2.Url);

                        if (blog2.Users != null)
                        {
                            foreach (User user in blog2.Users)
                            {
                                Console.WriteLine("   - " + user.UserId);
                                Console.WriteLine("   - " + user.Name);
                            }
                        }
                        else
                        {
                            Console.WriteLine("  - None");
                        }
                    }
                }

                Console.WriteLine("------------------------");

                method.Invoke(parameters);
            }
        }
    }
}
