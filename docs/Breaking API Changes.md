API Breaking Changes
====

# Version 1.1.0 

### Removed VisualBasicCommandLineParser.ctor 
During a toolset update we noticed the constructor on `VisualBasicCommandLineParser` was `public`.  This in turn made many of the `protected` members of `CommandLineParser` a part of the API surface as it gave external customers an inheritance path.  

It was never the intent for these members to be a part of the supported API surface.  Creation of the parsers is meant to be done via the `Default` singleton properties.  There seems to be little risk that we broke any customers here and hence we decided to remove this API.  

PR: https://github.com/dotnet/roslyn/pull/4169

### Changed Simplifier methods to throw ArgumentNullExceptions 
Changed Simplifier.ReduceAsync, Simplifier.ExpandAsync, and Simplifier.Expand methods to throw ArgumentNullExceptions if any non-optional, nullable arguments are passed in.  Previously the user would get a NullReferenceException for synchronous methods and an AggregateException containing a NullReferenceException for asynchronous methods.

PR: https://github.com/dotnet/roslyn/pull/5144

# Version 1.3.0

### Treat a method marked with both public and private flags as private

The scenario is loading an assembly where some methods, fields or nested types have accessibility flags set to 7 (all three bits set), which mean public AND private.
After the fix, such flags are loaded to mean private.
The compat change is we’re trading a compile-time success and runtime failure (native compiler) against a compile-time error (restoring the behavior of v1.2).

Details below:

- The native compiler successfully compiles the method and field case (those only yield runtime error System.TypeLoadException: Invalid Field Access Flags) and reported an accessibility error on the nested type.
- The 1.2 compiler generated errors:
```
error BC30390: 'C.Private Overloads Sub M()' is not accessible in this context because it is 'Private'.
error BC30389: 'C.F' is not accessible in this context because it is 'Private'.
error BC30389: 'C.C2' is not accessible in this context because it is 'Protected Friend'.
error BC30390: 'C2.Private Overloads Sub M2()' is not accessible in this context because it is 'Private'.
```
- The 1.3 compiler crashes.
- After fix, the same errors as 1.2 are generated again.

PR: https://github.com/dotnet/roslyn/pull/11547

### Don't emit bad DateTimeConstant, and load bad BadTimeConstant as default value instead

The change affects compatibility in two ways:

- When loading an invalid DateTimeConstant(-1), the compiler will use default(DateTime) instead, whereas the native compiler would produce code that fails to execute.
- DateTimeConstant(-1) will still count when we check that you don’t specify two default values. The compiler will produce an error, instead of succeeding (and producing IL with two attributes).

PR: https://github.com/dotnet/roslyn/pull/11536

# Version 4.1.0

### Can no longer inherit from CompletionService and CompletionServiceWithProviders

The constructors of Microsoft.CodeAnalysis.Completion and Microsoft.CodeAnalysis.Completion.CompletionServiceWithProviders are now internal.
Roslyn does not support implementing completion for arbitrary languages.

# Version 4.2.0

### Can no longer inherit from QuickInfoService

The constructors of Microsoft.CodeAnalysis.QuickInfoService are now internal.
Roslyn does not support implementing completion for arbitrary languages.

### `Microsoft.CodeAnalysis.CodeStyle.NotificationOption` is now immutable

All property setters now throw an exception.

# Version 4.4.0

`Workspace.OnWorkspaceFailed` is no longer called when an error occurs while reading source file content from disk.

The `Workspace` and `DocumentId` parameters of `TextLoader.LoadTextAndVersionAsync(Workspace, DocumentId, CancellationToken)` are deprecated.
The method now receives `null` `Workspace` and `DocumentId`.

# Version 4.5.0

`SymbolDisplayFormat.CSharpErrorMessageFormat` and `CSharpShortErrorMessageFormat` now include parameter names by default if used on a standalone `IParameterSymbol`.
For example, parameter `p` in `void M(ref int p)` was previously formatted as `"ref int"` and now it is formatted as `"ref int p"`.

# Version 4.7.0

### `SymbolDisplayFormat` includes parameter name when invoked on `IParameterSymbol`

All `SymbolDisplayFormat`s (predefined and user-created) now include parameter names by default if used on a standalone `IParameterSymbol` for consistency with predefined formats (see the breaking change for version 4.5.0 above).

### Changed `IncrementalStepRunReason` when a modified input produced a new output

`IncrementalGeneratorRunStep.Outputs` previously contained `IncrementalStepRunReason.Modified` as `Reason`
when the input to the step was modified in a way that produced a new output.
Now the reason will be reported more accurately as `IncrementalStepRunReason.New`.

# Version 4.8.0

### Changed `Assembly.Location` behavior in non-Windows

The value of `Assembly.Location` previously held the location on disk where an analyzer or source generator was loaded from. This could be either the original location or the shadow copy location. In 4.8 this will be `""` in certain cases when running on non Windows platforms. This is due the compiler server loading assemblies using `AssemblyLoadContext.LoadFromStream` instead of loading from disk. 

This could already happen in other load scenarios but this change moves it into mainline build scenarios. 

### Deprecation warning for SyntaxNode serialization

The ability to serialize/deserialize a SyntaxNode to/from a Stream has been deprecated. The code for this still exists in Roslyn, but attempting to call the APIs to perform these functions will result in 'Obsolete' warnings being reported. A future version of Roslyn will remove this functionality entirely. This functionality could only work for a host that wrote out the nodes to a stream, and later read it back in within the same process instance. It could not be used to communicate across processes, or for persisting nodes to disk to be read in at a later time by a new host sessions. This functionality originally existed for the days when Roslyn was hosted in 32bit processes with limited address space. That is no longer a mainline supported scenario. Clients can get similar functionality by persisting the text of the node, and parsing it back out when needed.

PR: https://github.com/dotnet/roslyn/pull/70365

# Version 4.9.0

### Obsoletion and removal of SyntaxNode serialization.

Continuation of the deprecation that happened in 4.8.0 (see information above).  In 4.9.0 this functionality is now entirely removed, and will issue both an obsoletion error, and will throw at runtime if the APIs are used.

PR: https://github.com/dotnet/roslyn/pull/70277

### Changes in `Microsoft.CodeAnalysis.Emit.EmitBaseline.CreateInitialBaseline` method

A new required parameter `Compilation` has been added. Existing overloads without this parameter no longer work and throw `NotSupportedException`.

### Changes in `Microsoft.CodeAnalysis.Emit.SemanticEdit` constructors

The value of `preserveLocalVariables` passed to the constructors is no longer used.
