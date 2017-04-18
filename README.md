# TentacleSoftware.XmlRpc

http://xmlrpc.scripting.com/spec.html

## TODO:

We're not ready for you, yet!

- Move exploratory code in Test project to XmlRpcMiddleware
- Serialize method response and write to response
- Consolidate Tokenizer and method/parameter deserializer (we don't need two state machines)
- Change tokenizer/deserializer to read from stream async (now possible because we won't need to yield return due to consolidation above)
- Return error response as per spec