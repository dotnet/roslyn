# Source Generator / Workspace API Proposal

In Visual Studio 16.8, we shipped only limited source generator support in the Workspace APIs; any caller requesting a Compilation would get a Compilation that contained the correct generated SyntaxTrees, but there was no way to know where the trees came from, nor interact with the generated documents in any other way. This proposal describes how we are going to shore these APIs up.

## SourceGeneratedDocument Type

A new type will be introduced, `SourceGeneratedDocument`, that inherits from `Document`. `GetSyntaxTreeAsync`, `GetSyntaxRootAsync`, and `GetTextAsync` (and the corresponding synchronous and Try methods) will return the generated text or trees. The trees will be the same instances as would be found in the `Compilation.SyntaxTrees`.

The `Id` of the `Document` will be generated dynamically by the workspace. As much as possible, the Id will be persistent between snapshots as much as is practical. When a generator produces a document, it provides a "hint name" which is intended to be used for the IDE to allow for 'permanence' between runs. `DocumentId`s are defined by a GUID in the implementation, so to ensure the DocumentId can be stable, we will generate the GUID as a hash of the hint name, the generator name, and any other pertinent information. Although the implementation may do extra caching behind the scenes where it won't have to recompute hashes in this way, this will ensure the `DocumentId` stays stable even if the caches fail to avoid any surprises.

`SourceGeneratedDocument` will have some additional members:

1. `HintName`: the exact hint name that was provided by the generator.
1. `SourceGenerator`: the ISourceGenerator instance that produced this document.

For now, any _mutation_ of a SourceGeneratedDocument will not be supported. Calls to the following members will throw `NotSupportedException`:

1. `WithText`
2. `WithSyntaxRoot`
3. `WithName`
4. `WithFolders`
5. `WithFilePath`

Down the road, I hope that we might offer an extension point for a source generator to participate in WithText and WithSyntaxRoot. In many cases the generated source may contain expressions that are verbatim C# (think a .cshtml file having a C# section), and so we could have WithText or WithSyntaxRoot apply the resulting edit back to the document that the verbatim C# came from. The design of this is out of scope here.

## APIs on `Project`

### Existing API: `Documents`

Note the Documents collection will _not_ change, and will only include regular documents. It cannot include source generated documents because since it is a property access, there is no opportunity to make this async, nor have any way to express cancellation. Since a generator could produce any number of documents, there's simply no way to make this answer quickly without fundamentally taking a different approach to the API.

### New API: `ImmutableArray<SourceGeneratedDocument> GetSourceGeneratedDocumentsAsync(CancelationToken)`

This will run generators if they have not already ran, and then return a `SourceGeneratedDocument` for each generated document.

The implementation of this will run `GetCompilationAsync` if the documents have not been generated; if the Compilation had already been generated, we would have cached the generated document information and so this would be cheap. We will hold onto the list of documents strongly (the tree will be a recoverable tree), so even if the `Compilation` is GC'ed we won't have to recompute this part a second time.

### New API: `SourceGeneratedDocument? GetSourceGeneratedDocumentAsync(DocumentId, CancellationToken)`

Fetches a single document by ID; equivalent to calling the API and filtering down to the right document.

### Existing API: `GetDocument(SyntaxTree)`

No changes, it will potentially return a `SourceGeneratedDocument` now. Callers _may_ have to check whether it's a generated document before they try to offer a fix or refactoring on the document.

This API is the hardest API to figure out what to do in this entire spec. If it doesn't return generated documents, many features would break, since it's very common to assume that if a syntax tree exists in a `Compilation`, that there must be a `Document` that matches it. However, some features might _not_ want to see a generated document returned here because then they're going to try to modify that, and that won't make sense either. An audit of the code fixes in Roslyn discovered that for many of the places that would need an update to _ignore_ a generated document would also need an update to deal with the null reference that would come back from `GetDocument`.

The other odd thing here is whether this function needs to be asynchronous or not. The current belief is no, because the only way you can have gotten a SyntaxTree to pass to it from the same `Solution` is either from:

1. The `Compilation` or a symbol that came from it, which means generators have ran.
2. You inspected some `SourceGeneratedDocument` and got it's tree, which means that generator has already ran.

The only surprise you might run into is taking a `SyntaxTree` from an earlier `Solution`, passing it to a newer `Solution`, and getting a document that no longer exists because in the later `Solution` is no longer generating this document. However, passing a `SyntaxTree` between snapshots like that is questionable in the first place. Any code that is working with multiple snapshots and knew that a Document between the snapshots had the same tree could have done something like this, but that code is potentially broken _anyways_ with source generators since now one tree can change due to another tree changing.

## APIs on `Workspace`

### Existing APIs: `OpenDocument(DocumentId)`/`CloseDocument(DocumentId)`

This API today is used by any feature that wants to tell the host to open a file. This will accept the DocumentId of a generated document and work properly.

### Existing APIs: `IsDocumentOpen(DocumentId)` / `GetOpenDocumentIds(...)`
These will behave no differently than before.

### Existing APIs: `OnDocumentOpened`/`OnDocumentClosed`
These APIs today associate a Workspace and a SourceTextContainer. Besides allowing APIs like `GetOpenDocumentInCurrentContextWithChanges` to work, this also ensures that a change to the text buffer updates the Workspace automatically. For generated documents, it will wire up the first part (assocating the container) but will not update the Workspace contents when the buffer changes. This is because the updates flow in the _other_ direction, and for now that updating is being managed by a Visual Studio-layer component. Further refactoring may move the core updating into the Workspace layer directly, but not for now.

## APIs for Fetching Documents

### Existing API: `GetOpenDocumentInCurrentContextWithChanges(ITextSnapshot)`

Because we want source generated documents to have the same IDE affordances as any other open file, this API will still work but return a `SourceGeneratedDocument` in that case. Some special handling is required though due to asynchrony. If a user has a generated document open, we may have an async process running in the background that may be trying to refresh this open generated document. The call to `GetOpenDocumentInCurrentContextWithChanges` however will "freeze" the generated document to match the `ITextSnapshot`, so any calls to `GetSourceGeneratedDocumentsAsync()` will return that exact same text, even if that content is out of sync with what would be generated given that project state.

This does mean that in this case, the contents of this document won't match if you compared the document to Workspace.CurrentSolution, got the generated document by ID, and then asked for it's text. This however is OK: that can _always_ be the case for any caller to `GetOpenDocumentInCurrentContextWithChanges` since it always forks to match the `ITextSnapshot`. We're just making the window where this can happen to be bigger than usual.