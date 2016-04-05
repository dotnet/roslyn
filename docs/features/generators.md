Source Generators
=================

Summary
-------
Source generators provide a mechanism through which source code can be generated at compile time
and added to the compilation. The additional source can be based on the content of the compilation,
enabling some meta-programming scenarios.

Scenarios
---------
* Generate `BoundNode` classes from record definitions.
* Implement `System.ComponentModel.INotifyPropertyChanged`.
* Support code contracts defined through attributes.
* Generate types from structured data similar to F# Type Providers.

General
-------
Source generators are implementations of `Microsoft.CodeAnalysis.SourceGenerator`.
```
    public abstract class SourceGenerator
    {
        public abstract void Execute(SourceGeneratorContext context);
    }
```
`SourceGenerator` implementations are defined in external assemblies passed to the compiler
using the same `-analyzer:` option used for diagnostic analyzers. An assembly can
contain a mix of diagnostic analyzers and source generators.
Since generators are loaded from external assemblies, a generator cannot be used to build
the assembly in which it is defined.

`SourceGenerator` has a single `Execute` method that is called by the host -- either the IDE
or the command-line compiler. `Execute` provides
access to the `Compilation` and allows adding source and reporting diagnostics.
```
    public abstract class SourceGeneratorContext
    {
        public abstract Compilation Compilation { get; }
        public abstract void ReportDiagnostic(Diagnostic diagnostic);
        public abstract void AddCompilationUnit(string name, SyntaxTree tree);
    }
```
Generators add source to the compilation using `context.AddCompilationUnit()`.
Source can be added to the compilation but not replaced or rewritten. The `replace` keyword allows redefining methods.

The command-line compiler persists the generated source to support scenarios that require
files on disk (e.g.: navigating to error locations; debugging and setting breakpoints in generated code).

Generated source is persisted to a `GeneratedFiles/{GeneratorAssemblyName}` subfolder within the
`CommandLineArguments.OutputDirectory` using the `name` argument to `AddCompilationUnit`, and an extension based on the language.
For instance, on Windows a call to ```AddCompilationUnit("MyCode", ...);```
from `MyGenerator.dll` for a C# project would be persisted as `obj/debug/GeneratedFiles/MyGenerator.dll/MyCode.cs`.

The `name` must be a valid file name, must be unique across all files produced by the generator for the compilation,
and should be deterministic. (The content of the generated source should be deterministic as well. Both requirements
are necessary to ensure builds are deterministic.) By convention, `name` should be the namespace-qualified
type name of the type modified or generated.

`build clean` should be modified to delete the `GeneratedFiles/` directory.

Execution
---------
Source generators are executed by the command-line compilers and the IDE. The generators
are obtained from the `AnalyzerReference.GetSourceGenerators` for each analyzer reference
specified on the command-line or in the project. `GetSourceGenerators` uses reflection to find types that
inherit from `SourceGenerator` and instantiates those types.
```
    public abstract class AnalyzerReference
    {
        ...
        public abstract ImmutableArray<SourceGenerator> GetSourceGenerators(string language);
    }

```
A public `GeneratedSource` extension method on `Compilation` executes each generator in a collection of generators
and returns the collection of `SyntaxTrees` and `Diagnostics`.
(`GenerateSource` is called by the command-line compilers and IDE.)
If `writeToDisk` is true, the generated source is persisted to `outputPath`. Regardless of whether the tree is persisted
to disk, `SyntaxTree.FilePath` is set.
```
    public static class SourceGeneratorExtensions
    {
        public static ImmutableArray<SyntaxTree> GenerateSource(
            this Compilation compilation,
            ImmutableArray<SourceGenerator> generators,
            string outputPath,
            bool writeToDisk,
            out ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken);
    }
```
The compilers and IDE add `SyntaxTrees` returned by `GenerateSource` to the `Compilation` to
generate a new `Compilation` that is compiled and passed to any diagnostic analyzers.
Diagnostics from `GenerateSource` are reported to the user.
In the command-line compilers, the compile will be aborted if the diagnostics include errors.
In the IDE, diagnostics are included in the Errors list and errors do not prevent subsequent binding or analysis.

Exceptions thrown from generators are caught by the `GenerateSource` and reported as errors.

Generators may be executed in parallel by `GenerateSource` using the same policy that is used for
concurrent execution of `DiagnosticAnalyzers`.

Modifying Types
---------------
To add members to an existing class, the generated source will define a `partial class`.
In C# this means the original class definition must be defined as `partial`.

To redefine members in generated source, there are new language keywords: `replace` and `original`.
`replace` is a declaration modifier applied to the redefined method, property, or event.
`original` is a reference to the member that is replaced in a `replace` member.

`replace` and `original` are contextual keywords: `replace` is a keyword only when used as a member modifier;
`original` is a keyword only when used within a `replace` method (similar to parser handling of `async` and `await`).
```
original.cs:
    partial class C
    {
        void F() { }
        int P { get; set; }
        object this[int index] { get { return null; } }
        event EventHandler E;
    }

replace.cs:    
    partial class C
    {
        replace void F() { original(); }
        replace int P
        {
            get { return original; }
            set { original += value; } // P.get and P.set
        }
        replace object this[int index]
        {
            get { return original[index]; }
        }
        replace event EventHandler E
        {
            add { original += value; }
            remove { original -= value; }
        }
    }
```
The following `class` and `struct` members can be replaced:
1. Static and instance methods, properties, and events
1. Explicit interface implementations of members
1. User defined operators
1. Static constructors
1. Instance constructors
1. Instance destructors
1. Extension methods

The default constructor can be added by a generator but not replaced.

The following must match when replacing a member:
1. Signature: name, accessibility, arity, return type, parameter number, parameter types and ref-ness
1. Parameter names and default values (to prevent changing the interpretation of call-sites)
1. Type parameters and constraints
1. Attributes on the member, parameters, and return type (including `this` for extension methods)
1. Set of accessors in properties and events
1. Explicit implementation of the member
1. Modifiers: `sealed`, `static`, `virtual`, `new`, and `override`.

If type parameter constraints are specified in the original method but absent in the `replace` method,
the original constraints are used (similar to constraints in overrides).

`abstract` and `extern` members cannot be replaced.
`partial` methods can be replaced although the `partial` modifier is not allowed on the `replace` method.
`async` need not match.

If there are multiple `replace` definitions for the same member, the compiler will report an error
that there are multiple definitions for the member.

_To support scenarios where multiple generators may replace the same method, it will
be necessary to determine an order for the chain of replacements. One possibility is
to use the order of the assemblies containing the generators in the `Compilation`. Another possibility is
to require the ambiguous `replace` members to have explicit ```[Order(...)]``` attributes that indicate the
relative order._

Code Generation
---------------
The replacing methods will have the signature in metadata of the original method,
including the `virtual` and `final` metadata attributes and `override` clause.

The original methods will be emitted with mangled names to avoid multiple definitions with the same
name and signature when the containing type is loaded from metadata.

The mangled name is `<M>v__I` where M is the original method name, qualified by namespace and
type name if an explicit interface implementation, and where I is an index since there may be multiple
overloads that are replaced. (Since the compiler disallows methods and property or event accessors
with the same signatures, the same name mangling can be used for methods and accessors.)
To ensure the mangled names are deterministic, the index is from the original overloads sorted by syntax location.
Since the original methods will have mangled names, the methods are not callable
from source and are therefore emitted as `private`.

In the EE and REPL, which evaluate expressions in the context of methods loaded from metadata (even for projects from source),
the `Binder` implementations for those scenarios will bind `original` to the method with the mangled name.
Since there may be several methods named `<M>v__I` for a given M, with distinct signatures,
the Binder compares method signatures to find the actual original method.

The original methods in metadata will not override any base class or interface members so original
methods will be emitted without `virtual` and with no `override` clause.

If the replacing method does not call the original method (in scenarios where methods are rewritten
completely), the original method will be unused. And since it has a mangled name, the method is not
callable. To avoid bloating the assembly, the compiler should drop those unused
uncallable methods in optimized builds.

Original property and events will be dropped from metadata although the original accessors will be emitted.
The EE and REPL will need to recognize that certain mangled names map to accessors rather than oridinary methods. 

CodeAnalysis API
----------------
`SyntaxKind` includes `ReplaceKeyword` and `OriginalKeyword`.

`DeclarationModifiers` includes a `Replace` member.

`IMethodSymbol`, `IPopertySymbol`, `IEventSymbol` include `Replaced` and `ReplacedBy` properties.

Member lookup in expressions in the `SemanticModel` returns the replacing definition.

The semantic model for `original` will return the replaced symbol.

IDE
---
The IDE deals with `Workspace`, `Solution`, `Project`, and `Document`.
Generated source is exposed as `Documents` added to the `Project`.

The generated source is updated in the `Project` on load and on explicit build. At other times, the generated source is potentially stale.

The generated source are files on disk although the IDE does not persist the generated source, only the command-line compiler does.
On load and on build, the IDE will invoke `GenerateSource()` with a `Compilation` from the original source and use
the `FilePath` from each generated `SyntaxTree` to update the `Solution` in the `Workspace` to point to the new set of source files. 

`Document.IsGenerated` indicates whether the source was generated. Generated source will be readonly in the IDE
and certain tools such as Rename treat generated source specially. 
