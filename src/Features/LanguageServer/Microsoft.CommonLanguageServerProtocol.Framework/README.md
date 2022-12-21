# Common Language Server Protocol Framework (CLaSP)

CLaSP was created to ease the creation and maintenance of [Language Server](https://microsoft.github.io/language-server-protocol/) implementations by sharing our hard-won knowledge, while leaving you the flexiblity to create the server you need.

## Getting Started with CLaSP

You can find a [simple example implementation of a CLaSP-based LanguageServer here](https://github.com/dotnet/roslyn/tree/main/src/Features/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.Example).

To get started with CLaSP you may follow the following steps

1. Create a PackageReference to `Microsoft.CommonLanguageServerProtocol.Framework` in your LSP server project.
1. Implement the following classes:
    1. `ILspLogger`. This allows CLaSP to log information however you would like.
    1. `ILspServices`. This interface will serve as a wrapper around whatever DI system you choose to use to make sure that your services implemented elsewhere are available to CLaSP.
    1. `AbstractLanguageServer`. This is the core of your Language Server implementation, which will manage your lifecycle and host the rest of the components.
        1. Ensure that `Initialize` is called from the constructor to start the server.
        1. `ConstructLspServices` will be called once to construct your implementation of `ILspServices`. When this exits all your services should be registerd, including your IMethodHandlers. The following Services are mandatory for proper function:
            - An `ILspLogger`.
            - An `IRequestContextFactory`.
            - An `InitializeHandler` and `InitializedHandler`, either the ones included in CLaSP or your own implementations. If you use the included handlers you will need an `IInitializeManager` (which handles your Client and Server Capabilities) too.
            - The `AbstractLanguageServer` itself.
    1. `IRequestContextFactory<TRequestContext>`. Constructs the RequestContext for each request. To maintain good performance you need to minimize the work being done in `CreateRequestContextAsync` since it blocks the queue from receiving mutating requests.
        1. A RequestContext type. This is an object which will be passed in on every request your Handlers handle. It is a useful place to keep things like Loggers, Service providers, and most importantly DocumentSnapshots.
1. Now implement any Method handlers you need for your Language Server to properly function, such as [textDocument/didOpen](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_didOpen) using the `INotificationHandler` and `IRequestHandler` interfaces (and the `ITextDocumeentIdentifierhandler<TRequest,TTextDocumentIdentifier>` interface if the request relates to a specific document), being sure to include them in your `ILspServices` object (as constructed by `ConstructLspServices`) as `IMethodHandler` services. This automatically registers them on the JsonRpc object.
    1. `MutatesSolutionState` should be `true` for Handlers like `textDocument/didChange` which change solution state because this affects the queuing behavior required in order to ensure that the correct document version is being operated on.
    1. `HandleNotificationAsync` and `HandleRequestAsync`. These implement your actual handler behavior but it's very important that if they required access to the current state of a TextDocument that this information be gathered by `IRequestContextFactory` and put on the `RequestContext` object rather than retrieved here. If you fail to follow this stipulation you may run into document sync issues because requests which mutate document state are not guareneets to happen in a particular order. This means that document state might change while your `HandleRequestAsync` request is executing, but the `IRequestContextFactory` is guarenteed to operate in a thread-safe maner.
