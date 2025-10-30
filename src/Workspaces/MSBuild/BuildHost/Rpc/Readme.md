# Purpose

This folder implements a custom RPC system between the hosting application and the build host processes. This is implemented only because as of this writing there isn't an RPC system we can use that is compatible with the .NET Source Build environment, so a small one is implemented here that has just enough of what we need. If we're able to use one of the existing solutions out there (StreamJsonRpc, gRPC) we should delete this in favor of those.

# Protocol

The protocol only supports sending a request from the client to the server, and the server sends a reply. Standard out/in is used to communicate with the child process. The request and response are sent as JSON formatted message without newlines, so the message in each direction comprises of a single line. A request consists of a numeric ID which is used to match it up with the response since requests can be overlapped. The ID is just incremented, and may be reused by the client once it's gotten the response for the previous ID. The request also consists of a method name (which matches the method being invoked by .NET Reflection on the server) and the list of parameters.

The server has one or more target objects registered with it. Each targeted object is given a numeric ID, starting with zero. So the initial BuildHost object is target 0, and then any projects loaded (which have their own set of methods) are given target 1, target 2, etc.

The server side takes the method and parameters and invokes methods via reflection. The methods must be public instance methods. Since the communication mechanism is stdin/out and the process is running as the same user as the invoking application, the client/server is not considered to be a security boundary in any way.

The Invoke method on the client takes a CancellationToken, but it can only be cancelled before it actually goes across the wire. Once a request is sent, there is no cancelling it. Any method on the server that takes a CancellationToken as the last argument will get CancellationToken.None filled in for it.