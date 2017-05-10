# TentacleSoftware.XmlRpc

`TentacleSoftware.XmlRpc` is an async-aware streaming server implementation of the XML-RPC specification that runs in a `Microsoft.Owin` context with no other dependencies.

http://xmlrpc.scripting.com/spec.html

## Usage

Define your XML-RPC responder in a simple class and decorate its methods.

```csharp
public class SampleResponder
{
    [XmlRpcMethod("blogger.getUsersBlogs")]
    public List<Blog> GetUsersBlogs(string appKey, string username, string password)
    {
        return new List<Blog>
        {
            new Blog
            {
                BlogId = "1",
                BlogName = "My Blog 1",
                Url = "http://myblog1.com",
            }
        };
    }
}    
```

Define your XML-RPC structs in simple classes with simple properties.

```csharp
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

    [XmlRpcMember("users")]
    public List<User> Users { get; set; }
}
```

Add a mapping to your OwinStartup class for `XmlRpcMiddleware`. Map as many XML-RPC responder classes as you like; all your XML-RPC methods will be accessible via the specified path. 

```csharp
using System.Diagnostics;
using Microsoft.Owin;
using Owin;
using TentacleSoftware.XmlRpc.Core;
using TentacleSoftware.XmlRpc.Owin;

[assembly: OwinStartup(typeof(TentacleSoftware.XmlRpc.Sample.EmptyAspNetWebApp.Startup))]
namespace TentacleSoftware.XmlRpc.Sample.EmptyAspNetWebApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Branch to new pipeline
            // Our XmlRpcMiddleware will "listen" on this URL
            app.Map("/api", app2 =>
            {
                // Attach SampleResponder
                app2.Use(new XmlRpcMiddleware()
                    .Add<SampleResponder>());
            });
        }
    }
}
```

## XML-RPC responders

`XmlRpcMiddleware` supports an arbitrary number of responders. You can have a single class that contains all the methods you want to expose via XML-RPC, or multiple classes with single responsibilities. 

```csharp
app2.Use(new XmlRpcMiddleware()
    .Add<SampleResponder>())
    .Add<SampleResponder2>())
    .Add<SampleResponder3>())
    .Add<SampleResponder4>());  
```

The `Add(...)` method has three overloads:

- `Add<TResponder>()` Add a type of XML-RPC responder class from which a new instance will be instantiated for each request
- `Add<TResponder>(TResponder instance)` Add a single instance of an XML-RPC responder class that will be returned for all requests
- `Add<TResponder>(Func<TResponder> instanceFactory)` Add a factory that generates a new instance of an XML-RPC responder class for each request

`XmlRpcMiddleware` stores each method from each added class in an internal `Dictionary` keyed by `ClassName`.`MethodName` or the name given to the `XmlRpcMethodAttribute` decorating the method. If a method is decorated with more than one `XmlRpcMethodAttribute`, it is added to the `Dictionary` under each name. The name provided in the client's `<methodRequest>` is looked up in this `Dictionary`.

- `params` signatures are supported as `<array>` values
- `Task<T>` return values are awaited
- `void` and `Task` return values are not supported, as per the XML-RPC spec

You can decorate a method with `XmlRpcIgnoreAttribute` if you want `XmlRpcMiddleware` to ignore it when populating its `Dictionary` of available methods.

## Structs

Serialization and deserialization of `<struct>`s is performed using reflection of the public properties of the specific class.

- Your struct class must have a parameterless constructor
- All deserialized members of the XML-RPC struct must have a matching public property with a public setter method in your struct class
- All serialized members of your struct class must be defined as public properties with public getter methods
- Mapping XML-RPC struct member names to class property names is case sensitive
- We will serialize arbitrarily deep object graphs, so you'll generate a response timeout or stack overflow exception if your graph has a reference loop
- You can use `XmlRpcMemberAttribute` to specify the member name of the property in the XML-RPC struct
- You can use `XmlRpcIgnoreAttribute` to instruct the deserialization and serialization engines to ignore the specified property

We only support types defined in the XML-RPC spec; most importantly, that means we only support `<i4>` and `<int>`. We do not the extensions to XML-RPC that define `<i8>`. We'll do our best to convert types into their XML-RPC equivalents as long as doing so won't result in loss of data or precision.

- `string`
- `bool`
- `int`
- `double`
- `DateTime`
- `byte[]`
- `char` --> `string`
- `sbyte` --> `int`
- `short` --> `int`
- `float` --> `double`

`byte[]` values will be serialized to and deserialized from `<base64>`.

## Arrays

We support deserialization of `<array>` elements as `T[]` or as generic collections that implement `IList` and contain a single concrete type (e.g. `List<T>`) only. The collection class must have a parameterless constructor. 

If `<array>` members are `<struct>`s, the struct class must be a concrete class with a parameterless constructor and must contain public properties with public setter methods. We don't support collections of a base class (e.g. `object`) or collections of an interface.

Properties and parameters of `T[]` are created internally as a `List<T>` and then converted to a `T[]` when we've deserialized the entire collection and thus know the length of the required `array`.

## Fault handling and request filtering

Optionally, you can add `XmlRpcFaultMiddleware` to your pipeline before `XmlRpcMiddleware`.

This middleware will catch any unhandled exceptions in the pipeline and will respond to the client with an appropriate XML-RPC `<fault>`, as per XML-RPC spec.

- If your XML-RPC handler throws an `XmlRpcException`, we'll transform it into `<fault>` with the specified `FaultCode` and `FaultString`.
- If your XML-RPC handler throws other exceptions, we'll respond with a `<fault>` containing `FaultCode` = 500 and `FaultString` = "Internal Server Error".

Attach a handler to the `OnFaulted` event to pass the exception to another exception handler (e.g. your logger).

Additionally, you can add `XmlRpcRequestFilterMiddleware` to your pipeline to make sure we only accept POSTs and `text/xml`, as per XML-RPC spec. This is a separate middleware because, depending on how conformant your clients are, you might want to do more or less filtering.

```csharp
app2.Use(new XmlRpcFaultMiddleware()
    // Do some logging
    .OnFaulted((s, e) =>
    {
        XmlRpcException fault = e as XmlRpcException;

        if (fault != null)
        {
            Trace.WriteLine($"FAULT: {fault.Code} {fault.Message}");

            if (fault.InnerException != null)
            {
                Trace.WriteLine($"ERROR: {fault.InnerException.Message}");
            }

            Trace.WriteLine(fault.StackTrace);
        }
        else
        {
            Trace.WriteLine($"ERROR: {e.Source} {e.Message}");
            Trace.WriteLine(e.StackTrace);
        }
    }));

app2.Use(new XmlRpcRequestFilterMiddleware());

// Attach SampleResponder
app2.Use(new XmlRpcMiddleware()
    .Add<SampleResponder>());
```

## Client

`TentacleSoftware.XmlRpc` is an XML-RPC server and has no client capabilities. 

The `TentacleSoftware.XmlRpc.Sample.EmptyAspNetWebApp` project in this repository contains an example usage of the `jquery-xmlrpc` JavaScript XML-RPC client.
