# Common Language Server Protocol Framework (CLaSP)

CLaSP was created to ease the creation and maintenance of [Language Server](https://microsoft.github.io/language-server-protocol/) implementations by sharing our hard-won knowledge, while leaving you the flexiblity to create the server you need.

## A note on support

Currently CLaSP is not recommended or supported for use outside of specific teams/projects. We hope to make it more broadly available in the future.

## Getting Started with CLaSP

You can find a [simple example implementation of a CLaSP-based LanguageServer here](https://github.com/dotnet/roslyn/tree/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example).

To get started with CLaSP you may follow the following steps

1. Create a PackageReference to `Microsoft.CommonLanguageServerProtocol.Framework` in your LSP server project.
1. Implement the following classes:
    1. `ILspLogger`. This allows CLaSP to log information however you would like.
    1. [`ILspServices`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleLspServices.cs). This interface will serve as a wrapper around whatever DI system you choose to use to make sure that your services implemented elsewhere are available to CLaSP.
        1. We recommend making `SupportsGetRegisteredServices` return `false` and `GetRegisteredServices` throw `NotImplementedException`.
    1. [`AbstractLanguageServer`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleLanguageServer.cs). This is the core of your Language Server implementation, which will manage your lifecycle and host the rest of the components.
        1. [Ensure that `Initialize` is called](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleLanguageServer.cs#:~:text=Initialize) (preferably from the constructor) to start the server.
        1. [`ConstructLspServices`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleLanguageServer.cs#:~:text=ConstructLspServices) will be called once to construct your implementation of `ILspServices`. When this exits all your services should be registerd, including your `IMethodHandlers`. The following Services are mandatory for proper function:
            - An `ILspLogger`.
            - An `IRequestContextFactory`.
            - An `InitializeHandler` and `InitializedHandler`, either the ones included in CLaSP or your own implementations. If you use the included handlers you will need an `IInitializeManager` (which handles your Client and Server Capabilities) too.
            - [The `AbstractLanguageServer` itself](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleLanguageServer.cs#L28).
    1. [`IRequestContextFactory<TRequestContext>`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleRequestContextFactory.cs). Constructs the RequestContext for each request. To maintain good performance you need to minimize the work being done in `CreateRequestContextAsync` since it blocks the queue from receiving mutating requests.
        1. [A RequestContext type](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleRequestContext.cs). This is an object which will be passed in on every request your Handlers handle. It is a useful place to keep things like Loggers, Service providers, and most importantly DocumentSnapshots.
1. Now implement any Method handlers you need for your Language Server to properly function, such as [textDocument/didOpen](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_didOpen) using the `INotificationHandler` and `IRequestHandler` interfaces (and the `ITextDocumeentIdentifierhandler<TRequest,TTextDocumentIdentifier>` interface if the request relates to a specific document), being sure to include them in your `ILspServices` object (as constructed by `ConstructLspServices`) [as `IMethodHandler` services](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example/ExampleLanguageServer.cs#:~:text=AddHandlers). This automatically registers them on the JsonRpc object.
    1. `MutatesSolutionState` should be `true` for Handlers like `textDocument/didChange` which change solution state because this affects the queuing behavior required in order to ensure that the correct document version is being operated on.
    1. `HandleNotificationAsync` and `HandleRequestAsync`. These implement your actual handler behavior but it's very important that if they require access to the current state of a TextDocument that this information be gathered by `IRequestContextFactory` and put on the `RequestContext` object rather than retrieved here. If you fail to follow this stipulation you may run into document sync issues because requests which mutate document state are not guaranteed to happen in a particular order. This means that document state might change while your `HandleRequestAsync` request is executing, but the `IRequestContextFactory` is guaranteed to operate in a thread-safe manner.

## More complex examples

- [Roslyn](https://github.com/dotnet/roslyn/tree/main/src/Features/LanguageServer/Protocol) is the original implementor of CLaSP.
  - [`AbstractLanguageServer`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Protocol/RoslynLanguageServer.cs). Note that Roslyn overrides `ConstructRequestExecutionQueue`, allowing it to change the override some default behavior.
  - [`ILSPLogger`](https://github.com/dotnet/roslyn/blob/main/src/VisualStudio/Core/Def/LanguageClient/LogHubLspLogger.cs). Logs to LogHub.
  - [`ILSPServices`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Protocol/LspServices/LspServices.cs). Note: Roslyns `ILSPServices` implementation is a product of their specific history and needs and ends up being a combination of MEF and explicitly constructed services. We don't recommend using it to guide your creation of an `ILSPServices` implementation unless you have similarly complicated needs.
  - [`IRequestContextFactory`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Protocol/Handler/RequestContextFactory.cs). Provides a good example of how to get the TextDocumentIdentifier (URI) off of the `requestParam` object.
    - [RequestContext](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Protocol/Handler/RequestContext.cs). Of particular interest here is the [retrieval of information about the document](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Protocol/Handler/RequestContext.cs#:~:text=GetLspDocumentInfoAsync) (if any). Since `RequestContext.CreateAsync` is called by `IRequestContextFactory.CreateRequestContextAsync` we maintain synchronization safety.
  - [`IRequestHandler` or `INotificationHandler`](https://github.com/dotnet/roslyn/blob/main/src/Features/LanguageServer/Protocol/Handler/DocumentChanges/DidOpenHandler.cs). Note that this example mutates solution state and has document context.
- [Razor](https://github.com/dotnet/razor/tree/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer) has simpler requirements than Roslyn in some ways (such as `ILSPServices`) but relies on multiple other language servers (C#, HTML) for information on its contained languages.
  - [`AbstractLanguageServer`](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/RazorLanguageServer.cs).
    - [`ConstructLspServices`](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/RazorLanguageServer.cs#:~:text=ConstructLspServices). Razor has a simple implementation of `ILSPServices` which wraps `Microsoft.Extensions.DependencyInjection` and explicitly adds each service that it depends on.
  - [`ILSPLogger`](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/LspLogger.cs#:~:text=class%20LSPLogger). Razor's `ILSPLogger` implementation simply notifies the client so that it can do whatever is needed.
  - [`ILSPServices`](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/LspServices.cs). A simple wrapper which may serve as a good reference for implementations.
  - [`IRequestContextFactory`](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/RazorRequestContextFactory.cs). Note again the [retrieval of document information](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/RazorRequestContextFactory.cs#:~:text=documentContextFactory).
  - [`IRequestHandler` or `INotificationHandler`](https://github.com/dotnet/razor/blob/main/src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/DocumentSynchronization/RazorDidChangeTextDocumentEndpoint.cs). Note that this example mutates solution state and has document context.
