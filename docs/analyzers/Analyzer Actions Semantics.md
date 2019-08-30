**Definitions:**

 - **Session:** For batch compilation, a single compilation. For a hosted environment, an arbitrary duration potentially encompassing many compilations. In particular, for VS, a session shares the lifetime and scope of an analyzer reference, which may correspond to a loaded project, a loaded solution, or the VS process.
 - **Analyzer:** An instance of a type derived from `DiagnosticAnalyzer`.
 - **Action:** A method instance registered via a Register\*\*\*Action method of an instance of a \*\*\*Context class, to be automatically applied as appropriate. Every action is associated with an analyzer and has a kind (which is one of compilation start, syntax tree, symbol, code block start, syntax node, code block, code block end, semantic model, compilation, compilation end).
 - **Operation block:** A single unit of executable code containing operation nodes, e.g. a method body, parameter default value, field initializer, etc. Operation blocks are primarily used for semantic analysis of executable code using the `IOperation` APIs. 
 - **Code block:** A single unit of executable code containing syntax nodes, e.g. a method body, parameter default value, field initializer, etc. Code blocks are primarily used for syntax and/or semantic analysis of executable code using the language specific Syntax APIs. However, it is recommended that analyzers use Operation block and `IOperation` API for semantic analysis of executable code for improved performance and simpler, language-agnostic implementation.
 - **Symbol members:** List of zero or member symbols for a given `ISymbol`. For a namespace or type symbol, these are the members returned by [INamespaceOrTypeSymbol.GetMembers](http://source.roslyn.io/#Microsoft.CodeAnalysis/Symbols/INamespaceOrTypeSymbol.cs,23) API, which include nested symbols. Method, field, event and property symbols have no member symbols.

**Axioms:**
 - Two different analyzers are assumed not to share state. No actions of a single analyzer execute concurrently, unless they explicitly configure concurrent execution in the `DiagnosticAnalyzer.Initialize` method by invoking `AnalysisContext.EnableConcurrentExecution`. Actions of different analyzers may execute concurrently. (This is likely to be refined as we develop more sophisticated concurrency strategies, but it will remain true that actions will generally not require locks to avoid races.)
 - The context used to register an action affects the lifetime of the registration but does not otherwise affect the semantics of applying the action.
 - If an analyzer has multiple applicable actions of any given kind, all of those actions are applied in an arbitrary order.

**Ordering of actions:**

The `DiagnosticAnalyzer.Initialize` method of an analyzer is invoked before any actions of an analyzer can be applied. The actions registered by `Initialize` methods form the initial set of actions used for each compilation. `Initialize` method may be invoked multiple times on an analyzer instance in a session, but is guaranteed to be invoked only once for any given compilation.

The following rules related to action invocation apply per compilation or per any environmental change that invalidates prior analysis (e.g. changes to analyzer options or changes to non-compiled documents used by analyzers). For purposes of this specification, such environmental changes act as if they create a new compilation.

There are no ordering constraints for the invocations of any actions, except those introduced by start and end actions (compilation start, compilation end, symbol start, symbol end, operation block start, operation block end, code bock start, and code block end actions). Invocations of actions of one kind may be interleaved with invocations of actions of another kind. A given host environment might appear to provide a predictable order—for example, invoking syntax tree actions before semantic model actions, or symbol actions before code block actions, but depending on such an ordering is not safe.

 - A compilation start action is invoked once, before any of the analyzer’s other kinds of actions are invoked. Actions registered by a compilation start action apply to that compilation only.
 - A syntax tree action is invoked once per source document. A syntax tree action can be expected to be invoked as early as possible after a document is parsed, but this is not guaranteed.
 - A symbol action is invoked once per complete semantic processing of a declaration of a type or type member, provided that symbol has a kind matching one of the kinds supplied when the action was registered.
 - A symbol start action is invoked exactly once for every symbol of registered `SymbolKind`, before any of the analyzer’s other kinds of actions are invoked on the symbol or the symbol's members. A symbol start action can register following actions:
    - Actions for syntax and operation nodes within the symbol and its members: Operation action, operation block action, operation block start action, syntax node action, code block action and code block start action.
    - Symbol end action for the symbol, after all other applicable actions of an analyzer have been invoked on the symbol and its members.
 - An operation block start action is invoked once per operation block, before any operation node actions applicable to nodes within the block. Actions registered by a operation block start action apply to that operation block only.
 - An operation action is invoked once per operation node, provided that the operation node has a kind matching one of the kinds supplied when the action was registered.
 - An operation block action is invoked once per operation block.
 - An operation block end action is invoked once per operation block, after invoking any operation node actions applicable to nodes within the block and any operation block actions for the block.
 - A code block start action is invoked once per code block, before any syntax node actions applicable to nodes within the block. Actions registered by a code block start action apply to that code block only.
 - A syntax node action is invoked once per syntax node, provided that the syntax node has a kind matching one of the kinds supplied when the action was registered.
 - A code block action is invoked once per code block.
 - A code block end action is invoked once per code block, after invoking any syntax node actions applicable to nodes within the block and any code block actions for the block.
 - A semantic model action is invoked once per source document.
 - A compilation action is invoked once.
 - A compilation end action is invoked once, after all other applicable actions of an analyzer have been invoked.
 
Summarizing the ordering rules:
- For a given compilation, compilation start actions run first and compilation end actions run last.
- For a given symbol, symbol start actions run before operation/syntax/symbol actions on a symbol and its members, and these operation/syntax/symbol actions run before the symbol end actions.
- For a given operation block, operation block start actions run before operation node actions, and operation node actions run before the operation block end actions.
- For a given code block, code block start actions run before syntax node actions, and syntax node actions run before code block end actions.
- Otherwise, there are no ordering guarantees for action invocation.

**Implementation freedom:**

If an implementation determines that a given action will have equivalent effects in two compilations within a session, it can omit invoking the action for the second compilation and instead retain the effects of invoking it for the first compilation. For example, in an IDE, edits to a source document will not necessarily cause actions to be invoked for unrelated symbols etc. defined in other documents.

A host environment may delay invocation of actions arbitrarily and indefinitely. For example, an IDE might delay invoking some actions until a significant pause in editing activity, during which time several compilations might be initiated. Some actions might not execute at all for the interim compilations, because they will be cancelled when the next compilation begins. In such cases, it is possible for a compilation start action to run without its corresponding compilation end actions ever running for a given compilation, and likewise a code block start action can run without its corresponding code block end actions ever running. Therefore, end actions cannot be relied upon to free resources acquired by start actions.
