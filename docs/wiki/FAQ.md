# .NET Compiler Platform ("Roslyn") FAQ

This FAQ has good learning or getting-started questions in addition to frequent questions, all inspired by the great questions and answers from previous Roslyn CTPs.  In several cases, the community was helping each other without the team chiming in, so great job everyone!  You will find many good pointers by searching for keywords or phrases on this page.

Where there is code available, the answer to the question has one or more tags such as "FAQ(27)" in the text.  You can open the code files just named and search for the tag.  By not listing all the code in this document, the document is less likely to diverge from working code.  The samples/test project is always up to date with any API changes.


## Contents

* [Project / Cross-cutting Questions](#project-/-cross-cutting-questions)
    * [What docs are available on Roslyn](#what-docs-are-available-on-roslyn)
    * [Can I rewrite source code within the compiler pipeline](#can-i-rewrite-source-code-within-the-compiler-pipeline)
    * [Can I redistribute the Roslyn DLLs with my samples or code on my blog](#can-i-redistribute-the-roslyn-dlls-with-my-samples-or-code-on-my-blog)
    * [How do the Roslyn APIs relate to the VS Code Model and CodeDom](#how-do-the-roslyn-apis-relate-to-the-vs-code-model-and-codedom)
    * [Can you just open a Connect bug for me](#can-you-just-open-a-connect-bug-for-me)
* [GitHub Site](#github-site)
    * [Why are there several solution files?](#why-are-there-several-solution-files)
    * [What components can I run locally in Visual Studio?](#what-components-can-i-run-locally-in-visual-studio)
* [Getting Information Questions](#getting-information-questions)
    * [How do I get type info for a variable in a declaration, with inferred ('var') or explicit variable type](#how-do-i-get-type-info-for-a-variable-in-a-declaration-with-inferred-var-or-explicit-variable-type)
    * [How do I get all variables declared of a specified type that are available at a given code locations](#how-do-i-get-all-variables-declared-of-a-specified-type-that-are-available-at-a-given-code-locations)
    * [How do I get a completion list or accessible symbols at a code location](#how-do-i-get-a-completion-list-or-accessible-symbols-at-a-code-location)
    * [How do I get a completion list with members of an accessible type](#how-do-i-get-a-completion-list-with-members-of-an-accessible-type)
    * [How do I get caller/callee info](#how-do-i-get-caller/callee-info)
    * [How do I go from a Solution to Find All References on a symbol/type](#how-do-i-go-from-a-solution-to-find-all-references-on-a-symbol/type)
    * [How do I find all calls in a compilation into a particular namespace](#how-do-i-find-all-calls-in-a-compilation-into-a-particular-namespace)
    * [How do I get all symbols of an assembly (or all referenced assemblies)](#how-do-i-get-all-symbols-of-an-assembly-or-all-referenced-assemblies)
    * [How do I get the type of an expression node](#how-do-i-get-the-type-of-an-expression-node)
    * [How do I get type information of parameters and declared locals with common API](#how-do-i-get-type-information-of-parameters-and-declared-locals-with-common-api)
    * [How do I get the type information (TypeSymbol) from a semantic model for an identifier (or an IdentifierNameSyntax node)](#how-do-i-get-the-type-information-typesymbol-from-a-semantic-model-for-an-identifier-or-an-identifiernamesyntax-node)
    * [How do I compare syntax nodes (optionally ignoring attached trivia)](#how-do-i-compare-syntax-nodes-optionally-ignoring-attached-trivia)
    * [How are comments stored in the syntax tree (and how to use the Syntax Visualizer)](#how-are-comments-stored-in-the-syntax-tree-and-how-to-use-the-syntax-visualizer)
    * [What is structured trivia and how do I get at it](#what-is-structured-trivia-and-how-do-i-get-at-it)
    * [Is there a syntax tree visualization or tools to visually inspect a tree](#is-there-a-syntax-tree-visualization-or-tools-to-visually-inspect-a-tree)
    * [How do I tell if the type associated with a symbol is a known type  Do I have to construct the AssemblyQualifiedName myself](#how-do-i-tell-if-the-type-associated-with-a-symbol-is-a-known-type--do-i-have-to-construct-the-assemblyqualifiedname-myself)
    * [How do I tell if Symbols are the same](#how-do-i-tell-if-symbols-are-the-same)
    * [How can I test if a semantic model can provide information about a syntax node](#how-can-i-test-if-a-semantic-model-can-provide-information-about-a-syntax-node)
    * [How can I get the metadata token for an ISymbol](#how-can-i-get-the-metadata-token-for-an-isymbol)
    * [What's with identifier's (SyntaxTokens) having Value rather than Name, and how do Value, ValueText, and GetText relate](#what's-with-identifier's-syntaxtokens-having-value-rather-than-name,-and-how-do-value,-valuetext,-and-gettext-relate)
    * [Why use ChildNodesAndTokens rather than just Children](#why-use-childnodesandtokens-rather-than-just-children)
    * [How do I get line and column information to report errors](#how-do-i-get-line-and-column-information-to-report-errors)
    * [How do I find all syntactic sub trees of a particular kind](#how-do-i-find-all-syntactic-sub-trees-of-a-particular-kind)
    * [How do I get the fully qualified type name of a definition](#how-do-i-get-the-fully-qualified-type-name-of-a-definition)
    * [How do I determine which overload binds at a given call site](#how-do-i-determine-which-overload-binds-at-a-given-call-site)
    * [How can I tell if a TypeSymbol from an expression can be assigned to the TypeSymbol of a location](#how-can-i-tell-if-a-typesymbol-from-an-expression-can-be-assigned-to-the-typesymbol-of-a-location)
    * [How can I get a fully qualified type name for a local variable declaration, instead of just the text parsed in the SyntaxTree](#how-can-i-get-a-fully-qualified-type-name-for-a-local-variable-declaration,-instead-of-just-the-text-parsed-in-the-syntaxtree)
    * [How do I get the .NET Framework version](#how-do-i-get-the-.net-framework-version)
    * [How do I get a project's assembly symbol, references, and syntax trees for each document or item in the project](#how-do-i-get-a-project's-assembly-symbol,-references,-and-syntax-trees-for-each-document-or-item-in-the-project)
    * [How do I extract an annotation of a particular (sub) type](#how-do-i-extract-an-annotation-of-a-particular-sub-type)
    * [Why doesn't the Workspace API support more VS concepts (nested documents or non-code files)](#why-doesnt-the-workspace-api-support-more-vs-concepts-nested-documents-or-non-code-files)
    * [How do I get base type or implemented interface information and which members override or implement base members](#how-do-i-get-base-type-or-implemented-interface-information-and-which-members-override-or-implement-base-members)
    * [How do I use symbols to find and investigate attributes that have been applied to methods](#how-do-i-use-symbols-to-find-and-investigate-attributes-that-have-been-applied-to-methods)
* [Constructing and Updating Tree Questions](#constructing-and-updating-tree-questions)
    * [How do I add a method to a class](#how-do-i-add-a-method-to-a-class)
    * [How do I replace a sub expression, declaration, etc.](#how-do-i-replace-a-sub-expression,-declaration,-etc.)
    * [How do I change the name of a symbol at the declaration site and all reference sites](#how-do-i-change-the-name-of-a-symbol-at-the-declaration-site-and-all-reference-sites)
    * [Can I add custom information to syntax and symbols](#can-i-add-custom-information-to-syntax-and-symbols)
    * [How can I remove a statement with a SyntaxRewriter](#how-can-i-remove-a-statement-with-a-syntaxrewriter)
    * [How do I construct a pointer type or array type given another type](#how-do-i-construct-a-pointer-type-or-array-type-given-another-type)
    * [How can I remove #region and #endregion (structured trivia) with SyntaxRewriter](#how-can-i-remove-#region-and-#endregion-structured-trivia-with-syntaxrewriter)
    * [How can I add logging to all statements of a particular kind (for example, to log contents of variables)](#how-can-i-add-logging-to-all-statements-of-a-particular-kind-for-example,-to-log-contents-of-variables)
    * [How can I remove all comments from a file of code](#how-can-i-remove-all-comments-from-a-file-of-code)
* [Scripting, REPL, and Executing Code Questions](#scripting,-repl,-and-executing-code-questions)
    * [What happened to the REPL and hosting scripting APIs](#what-happened-to-the-repl-and-hosting-scripting-apis)
    * [How do the Roslyn APIs relate to LINQ Expression Trees or Expression Trees v2?  Is one better for meta-programming or implementing DSLs](#how-do-the-roslyn-apis-relate-to-linq-expression-trees-or-expression-trees-v2--is-one-better-for-meta-programming-or-implementing-dsls)
    * [How do I compile some code into a collectible type or DynamicMethod](#how-do-i-compile-some-code-into-a-collectible-type-or-dynamicmethod)
* [Miscellaneous Questions](#miscellaneous-questions)
    * [What is elastic trivia](#what-is-elastic-trivia)
    * [Why do some Syntax factories attach elastic trivia I didn't ask for](#why-do-some-syntax-factories-attach-elastic-trivia-i-didn't-ask-for)
    * [How do I format a tree or node to a textual representation](#how-do-i-format-a-tree-or-node-to-a-textual-representation)
    * [How can I use Roslyn in an MSBuild task and avoid metadata fetching and re-entrancy conflicts](#how-can-i-use-roslyn-in-an-msbuild-task-and-avoid-metadata-fetching-and-re-entrancy-conflicts)
    * [Is there an end-to-end example on compiling a program to IL (Emit APIs)](#is-there-an-end-to-end-example-on-compiling-a-program-to-il-emit-apis)
    * [How can I capture IL, debug info, and doc comment outputs from a Compilation](#how-can-i-capture-il,-debug-info,-and-doc-comment-outputs-from-a-compilation)
    * [Is there an object model chart or type inheritance diagram of Roslyn types](#is-there-an-object-model-chart-or-type-inheritance-diagram-of-roslyn-types)
    * [How to build on Windows 8](#how-to-build-on-windows-8)

## Project / Cross-cutting Questions

## What docs are available on Roslyn?
There are a few specs for features, design notes for language feature discussions, and full coverage of doc comments.  However, the team does not have current API docs.  See [Documentation].

### Can I rewrite source code within the compiler pipeline?
Roslyn does not provide a plug-in architecture throughout the compiler pipeline so that at each stage you can affect syntax parsed, semantic analysis, optimization algorithms, code emission, etc.  However, you can use a pre-build rule to analyze and generate different code that MSBuild then feeds to csc.exe or vbc.exe.  You can use Roslyn to parse code and semantically analyze it, and then rewrite the trees, change references, etc.  Then compile the result as a new compilation.

### Can I redistribute the Roslyn DLLs? 
Yes. The recommended way to redistribute the Roslyn DLLs is with the [Roslyn NuGet package](http://www.nuget.org/packages/Microsoft.CodeAnalysis).

### How do the Roslyn APIs relate to the VS Code Model and CodeDom?
The CodeDom targets programmatic code generation and compilation scenarios in ASP.NET on the server.  It was later co-opted for some tooling uses, adding modeling of existing code to the code generation functionality.

The VS Code Model tries to relieve ISVs from having to parse code in VS so that ISVs can provide code-oriented extensions to VS.  The VS Code Model represented code (and had meager code generation support) down to types, members, and parameters.  It did NOT go into functions to the statement level.

The Roslyn APIs fully model all C# and VB code and provide full code generation or updating capabilities.  In Visual Studio 2015, the VS Code Model APIs are implemented atop the Roslyn APIs.  The CodeDom is a separate technology built outside the C# and VB teams, and it does not need to be rewritten (though someone may want to move it to the new Roslyn APIs).

### Can you just open a Connect bug for me?
As Microsoft employees chatting with folks on forums or via email, we're happy to open internal bugs to save you effort.  If we say we'll open a bug, then you're guaranteed we opened and gave the issue every bit of weight that we would give any customer issue.  If you want to track the bug and be automatically notified of any changes to the bug, then we need you to open a Connect bug.

There are a couple of key reasons we ask you to open Connect bugs, rather than our doing it for you.  First, we try to avoid artificially driving up customer demand on our own features, so sometimes when reviewing Connect bugs, auditors ask us when a bug doesn’t appear to come from customers.  Second, the Connect bug keeps a line open to the customer reporting the issue in case we have follow up questions about repro info or scenario clarifications, etc.  

When an employee logs the Connect issue for you, any updates cause Connect to send mail to the employee, not the customer, which is a problem should the bug go to another team from the employee who opened the bug.  The customer info or contact may be lost.  That said, this isn't a hardcore policy, and there are targeted cases where we will open a Connect bug on behalf of a customer when we have a long-term partner relationship with them.  This isn't our general workflow.

We do want to make it super easy for customers who have given us feedback to log that feedback though, so we tend to offer to log the issue for you.  However, if you want to get update mail and track the bug, we ask you to log the Connect bug.  There is a [url:VS Feedback|http://visualstudiogallery.msdn.microsoft.com/f8a5aac8-0418-4f88-9d34-bdbe2c4cfe72] tool that is designed to make it even easier to report Connect issues.

## GitHub Site
### Why are there several solution files?
The main solution, Roslyn.slnx, contains the entire codebase consisting of the compilers, workspaces, and Visual Studio layers. Building Roslyn.slnx requires Visual Studio and a compatible version of the VS SDK. The Compilers.slnf solution filter contains only the compiler layer and can therefore be built without Visual Studio or the VS SDK.

### What components can I run locally in Visual Studio?
Starting with Visual Studio 2015 Update 1, all parts of Roslyn can be ran inside Visual Studio. Read our instructions for [Building on Windows](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md) for more information.

## Getting Information Questions

### How do I get type info for a variable in a declaration, with inferred (‘var’) or explicit variable type?
See the sample code answer tagged “FAQ(1)” ([installed location information|faq#codefiles]) to see how to get the type from the IdentifierNameSyntax modeling ‘var’ as an identifier (C#) or the type name as an identifier (VB).  See the sample code answer tagged “FAQ(2)” to see how to get the type from the VariableDeclaratorSyntax modeling an entire ‘var’ declaration (C#) or a ‘dim’ declaration with Option Infer on (VB).

### How do I get all variables declared of a specified type that are available at a given code locations?
See the sample code answer tagged “FAQ(4)” ([installed location information|faq#codefiles]) to see a few examples of how to how to get the names of a specified type, including how to filter for different kinds of members or scopes.  The SemanticModel.LookupSymbols API will give you all the Symbols that can be referenced by a simple name at a given location.  There are a few issues to be aware of.

LookupSymbols may return symbols with unutterable names in C#, such as for anonymous types.  You can check the Symbol property CanBeReferencedByName.

The results include both local symbols and wider scopes such as symbols for methods, fields, types, and namespaces.  If you only care about local variables, you can check the Kind of the returned Symbols, see the code example. 

LookupSymbols may return symbols that cannot be referenced, but they are technically in scope.  For example, since C# lifts lexicals to function scope, so you could find the symbol for a local declared later in the function but that is illegal semantically to reference before the declaration.

### How do I get a completion list or accessible symbols at a code location?
See the sample code answer tagged “FAQ(4)” ([installed location information|faq#codefiles]) to see a few examples of how to get accessible names at a location, including how to filter for different kinds of members or scopes.  This question is similar to “How do I get all variables declared of a specified type that are available at a given code locations?”, but note in that questions answer that truly getting what Visual Studio shows in a completion list has multiple issues that you need to deal with. 

### How do I get a completion list with members of an accessible type?
See the sample code answer tagged “FAQ(5)” ([installed location information|faq#codefiles]) to see how to get accessible members.

### How do I get caller/callee info?
See the sample code answer tagged “FAQ(6)” ([installed location information|faq#codefiles]) to see how to get caller/callee information.  Note, the sample is not necessarily complete; for example, the analyzed code could have assigned the function to a delegate variable and then invoked it, for which the sample does not account.

### How do I go from a Solution to Find All References on a symbol/type?
See the sample code answer tagged “FAQ(7)” ([installed location information|faq#codefiles]) to see how to get references. In a nutshell, you should use the Microsoft.CodeAnalysis.SymbolFinder API.

### How do I find all calls in a compilation into a particular namespace?
See the sample code answer tagged “FAQ(8)” ([installed location information|faq#codefiles]) to see how to find all calls into a particular namespace (or to functions from that namespace).

### How do I get all symbols of an assembly (or all referenced assemblies)?
See the sample code answers tagged “FAQ(9)” and “FAQ(10)” ([installed location information|faq#codefiles]).  The former shows walking all namespace symbols, then type symbols, and then for simplicity walking field and method definitions.  The latter shows a walker for all expressions to discovered referenced symbols.

### How do I get the type of an expression node?
See the sample code answer tagged “FAQ(3)” ([installed location information|faq#codefiles]) to see a few examples of getting an expression’s type information.

### How do I get type information of parameters and declared locals with common API?
See the sample code answer tagged “FAQ(3)” ([installed location information|faq#codefiles]) to see a few examples of getting variable type information.  You’ll note Roslyn models parameters and other locals differently, with LocalSymbol and ParameterSymbol, which makes sense since parameters can be ref/out, have default values, etc.

### How do I get the type information (TypeSymbol) from a semantic model for an identifier (or an IdentifierNameSyntax node)?
See the sample code answer tagged “FAQ(1)” ([installed location information|faq#codefiles]) to see how to get the type from the IdentifierNameSyntax modeling ‘var’ as an identifier.

### How do I compare syntax nodes (optionally ignoring attached trivia)?
See the sample code answer tagged “FAQ(11)” ([installed location information|faq#codefiles]) to see several aspects of comparing trees and nodes.

### How are comments stored in the syntax tree (and how to use the Syntax Visualizer)?
Roslyn trees store comments as trivia on tokens.  The heuristic used is to attach trivia to the token following it (that is, as leading trivia on the node containing the token).  However, if the trivia is after the last token on a line, it attaches to the preceding token (that is, as trailing trivia).

In the following example, the first comment attaches as LeadingTrivia to the ‘int’ token in the declaration following it.  You can also see the leading trivia on the LocalDeclarationStatementSyntax node containing the ‘int’ token.  The first comment trivia attaches to the first token following it even though ‘int’ is on a different line because there are no tokens on the line with the comment.

``` csharp
int method1 () {
  int a = 1;
  // comment1
  int b = 1; // comment 2
}
```

The second comment attaches as TrailingTrivia to the semi-colon, and you can also see it on the LocalDeclarationStatementSyntax node.

For an overview of the Syntax Visualizer tool that allows you to visually inspect pieces of code and understand how Roslyn constructs syntax trees, go [here|Syntax Visualizer].

See the sample code answer tagged “FAQ(29)” ([installed location information|faq#codefiles]) to see a little SyntaxWalker that collects the comments in code and distinguishes structured doc comments.

### What is structured trivia and how do I get at it?
Most trivia (such as WhitespaceTrivia or SingleLineCommentTrivia) simply distinguishes the kind of trivia with the Kind property.  All trivia have the same struct type, SyntaxTrivia.

Some trivia (such as directives or xml doc comments) have structured models with SyntaxNode derived types to represent them.  You can check whether a trivia has structure by checking the HasStructure property.  If a trivia has structure, you can call the GetStructure method on the trivia to get a SyntaxNode.  For example, if you have an IfDirective trivia, calling its GetStructure method returns a IfDirectiveSyntax node.  This node gives you access to the HashToken, IfKeyword, condition’s ExpressionSyntax node, etc.

### Is there a syntax tree visualization or tools to visually inspect a tree?
Yes there is! Look [here|Syntax Visualizer] for more details about the Syntax Visualizer tool.

### How do I tell if the type associated with a symbol is a known type?  Do I have to construct the AssemblyQualifiedName myself?
It is best to use Equals().  Symbols overload operator==, which works in many cases, but doesn’t work properly with the Common Symbol API (ISymbol).  The easiest way to compare a known type to a symbol’s type is to use Compilation.GetTypeByMetadataName to get the known type, and then compare with Equals().  See the sample code answer tagged “FAQ(12)” ([installed location information|faq#codefiles]) to see symbol comparison.

For a small, fixed set of very common types (for example, like Int32 and String) and a few special types of interest to the compiler, the property TypeSymbol.SpecialType lets you easily see if your known type is one of those special types.  See the sample code answers tagged “FAQ(1)” and “FAQ(3)” to see symbol comparisons to special types.

Comparing symbols from different compilations has undefined results.

### How do I tell if Symbols are the same?
It is best to use Equals().  Symbols overload operator==, which works in many cases, but doesn’t work properly with the Common Symbol API (ISymbol).  See the sample code answer tagged “FAQ(12)” ([installed location information|faq#codefiles]) to see symbol comparison.
There are no guarantees that APIs return the same Symbol object even when called on the exact same argument objects.

For a small, fixed set of very common types (for example, like Int32 and String) and a few special types of interest to the compiler, the property TypeSymbol.SpecialType lets you easily see if a type symbol is one of those special types.  See the sample code answers tagged “FAQ(1)” and “FAQ(3)” to see symbol comparisons to special types.

Comparing symbols from different compilations has undefined results.

### How can I test if a semantic model can provide information about a syntax node?
See the sample code answer tagged “FAQ(13)” ([installed location information|faq#codefiles]) to see several ways of checking if trees, nodes, and semantic models are related.

### How can I get the metadata token for an ISymbol?
This might be interesting for interoperating between Roslyn APIs and other tools such as ildasm.  We do not have a way to do this today.

### What's with identifier's (SyntaxTokens) having Value rather than Name, and how do Value, ValueText, and GetText relate?
When I consider "ParameterList.Parameters{"[0]"}.Identifier.Value", wouldn't Name be a better choice than Value?

Regarding .Value vs. .Name for identifiers, we use SyntaxToken to represent identifiers.  SyntaxToken also represents all significant pieces of text in source code, such as literals, punctuation, etc.  SyntaxTokens have no concept of a name, but do model text.  SyntaxToken.GetText() returns this text exactly as it was read when parsing the source code.  SyntaxToken.Value returns the object "value" of the text represented by the SyntaxToken.  SyntaxToken.ValueText returns the text representation of this value.

Consider "long @long = 1L".  SyntaxToken.Value and SyntaxToken.ValueText for the escaped identifier "@long" both return the string "long".  However, SyntaxToken.GetText() returns the string "@long".  Similarly, SyntaxToken.Value for the literal "1L" returns the boxed integer "1".  SyntaxToken.ValueText returns the string "1".  SyntaxToken.GetText() returns the text parsed from the code, "1L".

See the sample code answer tagged “FAQ(14)” ([installed location information|faq#codefiles]).

### Why use ChildNodesAndTokens rather than just Children?
There was a Children property originally.  The feedback from usability testing showed that a key problem people had using the syntax API was using this collection.  The names were changed to emphasize the distinction between nodes and tokens and to steer people away from using the SyntaxNodeOrToken type by default.

### How do I get line and column information to report errors?
See the sample code answers tagged “FAQ(16)” and “FAQ(17)” ([installed location information|faq#codefiles]) to see several examples of fetching position information from trees and nodes.

### How do I find all syntactic sub trees of a particular kind?
See the sample code answer tagged “FAQ(18)” ([installed location information|faq#codefiles]) to see a SyntaxWalker that visits specific kinds of nodes and tokens.  Several other samples use node.DescendentNodes().OfType<>() to fetch sub trees (for example, “FAQ(1)”).  Some samples show filtering with node.DescendentNodes().First(t => t.Kind == SyntaxKind…), such as “FAQ(11)”.

### How do I get the fully qualified type name of a definition?
See the sample code answer tagged “FAQ(19)” ([installed location information|faq#codefiles]) to see several examples of getting and comparing names, including open generic types vs. closed generic types.

### How do I determine which overload binds at a given call site?
See the sample code answer tagged “FAQ(20)” ([installed location information|faq#codefiles]) to see resolving overloads.

### How can I tell if a TypeSymbol from an expression can be assigned to the TypeSymbol of a location?
See the sample code answers tagged “FAQ(21)” and “FAQ(22)” ([installed location information|faq#codefiles]) to see several examples of determining what’s assignable and if so what sort of conversion would be needed if any.

### How can I get a fully qualified type name for a local variable declaration, instead of just the text parsed in the SyntaxTree?
See the sample code answer tagged “FAQ(19)” ([installed location information|faq#codefiles]).

### How do I get the .NET Framework version?
See the sample code answer tagged “FAQ(23)” ([installed location information|faq#codefiles]), but essentially you get the containing assembly of SpecialType.System_Object and get the assembly’s version.

### How do I get a project's assembly symbol, references, and syntax trees for each document or item in the project?
See the sample code answer tagged “FAQ(24)” ([installed location information|faq#codefiles]) to see how to do it for one reference and document, but you can easily convert the code to loop over all.

### How do I extract an annotation of a particular (sub) type?
See the sample code answer tagged “FAQ(25)” ([installed location information|faq#codefiles]) to see annotating certain tokens and then finding tokens with a particular type of annotation.  The sample doesn’t show it, but this works with nodes as well.

### Why doesn't the Workspace API support more VS concepts (nested documents or non-code files)?
The Workspace API represents the view of solutions, projects, and documents that the C# and VB language services actually consume.  The Workspace model includes C# and VB source files, references, compilation options, and other similar details.  However, the Workspace API does not represent every feature of the Visual Studio project system, such as modeling xaml, resource files, etc.  The Workspace model also works independently of Visual Studio.

To get all the project item information that Visual Studio has, use the MSBuild APIs which ship in the .NET Framework.  If your code executes within Visual Studio, you could also use the IVsHierarchy API to enumerate project items, which would expose access to nested documents that Visual Studio manages.  If you want to do semantic analysis of C# and VB files, you should use a workspace.

### How do I get base type or implemented interface information and which members override or implement base members?
See the sample code answers tagged “FAQ(37)” “FAQ(38)” ([installed location information|faq#codefiles]) to see fetching types by name, fetching base and implemented types, and inspecting members for information such as virtual, override, sealed, etc.

### How do I use symbols to find and investigate attributes that have been applied to methods?
See the sample code answer tagged in “FAQ(39)” ([installed location information|faq#codefiles]) to see some basic mechanisms for working with attributes.

## Constructing and Updating Tree Questions

### How do I add a method to a class?
See the sample code answer tagged “FAQ(26)” ([installed location information|faq#codefiles]).

### How do I replace a sub expression, declaration, etc.?
See the sample code answers tagged “FAQ(26)” ([installed location information|faq#codefiles]) for replacing a class declaration and “FAQ(27)” for replacing sub expressions.

Note, there may be multiple workspaces active in Visual Studio at any time if the Roslyn language service is loaded:

* If the Roslyn language service is loaded, there is a workspace modeling the active solution inside Visual Studio.
* If the Roslyn language service is loaded, there is a workspace modeling a solution and projects for any miscellaneous (see Misc Files Project) .cs and .vb files open in VS.
* There is a workspace modeling solution and projects for any miscellaneous .csx files open in VS.
* There is a workspace which models the submission chain in the Interactive window.

Because the Roslyn language service features (completion, code Issues, refactoring support, etc.) operate on a Workspace, the features work in all of the above workspace contexts.

### How do I change the name of a symbol at the declaration site and all reference sites?
See the sample code answer tagged “FAQ(28)” ([installed location information|faq#codefiles]) to see a renaming example.  Note, this sample doesn’t handle generic names, and depending on the type of thing you are renaming, you might create the SyntaxRewriter differently (for example, not visiting class declarations or constructor declarations).

### Can I add custom information to syntax and symbols?
See the sample code answer tagged “FAQ(25)” ([installed location information|faq#codefiles]) to see annotating certain nodes and then finding nodes with a particular kind of annotation.

There is no mechanism for Symbols.

### How can I remove a statement with a SyntaxRewriter?
See the sample code answer tagged “FAQ(30)” ([installed location information|faq#codefiles]) to see removing a statement and when you can simply return null from a visit method or when you need to return Syntax.EmptyStatement().

### How do I construct a pointer type or array type given another type?
See the sample code answer tagged “FAQ(31)” ([installed location information|faq#codefiles]).
w

### How can I remove #region and #endregion (structured trivia) with SyntaxRewriter?
See the sample code answers tagged “FAQ(32)” and “FAQ(33)” ([installed location information|faq#codefiles]) to see a few ways of removing these directives.

### How can I add logging to all statements of a particular kind (for example, to log contents of variables)?
See the sample code answer tagged “FAQ(34)” ([installed location information|faq#codefiles]).

### How can I remove all comments from a file of code?
See the sample code answers tagged “FAQ(32)” and “FAQ(33)” ([installed location information|faq#codefiles]) to see a few ways of removing #region directives, but you can change the code to look for SyntaxKind SingleLineComment or MutliLineComment.

## Scripting, REPL, and Executing Code Questions

### What happened to the REPL and hosting scripting APIs?
The C# Interactive Window is back in Visual Studio Update 1. Enjoy!

### How do the Roslyn APIs relate to LINQ Expression Trees or Expression Trees v2?  Is one better for meta-programming or implementing DSLs?
Expression Trees v2 are a semantic model with some shapes resembling syntax.  They do support some control flow, assignment statements, recursion, etc.  However, they do not model many things that the C# and VB languages have, for example, type definitions.  The Roslyn APIs will have full fidelity with C# and VB for syntax, semantic binding, code emission, etc.

While the DLR and ETs v2 may lend themselves to some meta-programming, their real focus was supporting dynamic languages on .NET and enabling applications to host those languages for application scripting purposes.

The Roslyn APIs will be much better suited and probably your only consideration if your goal is embedded Domain Specific Languages (DSLs).  Embedded DSLs are syntax extensions to a language for which you can supply core language code during compilation or prior to execution to give semantics to the new syntax.  See the [url:Dylan language|http://opendylan.org/books/dpg/db_329.html] for an example of DSL support in a language.  Otherwise, a DSL is just a formal language that is highly specific to a domain, such as SQL, as opposed to a general purpose language like C#.  The Roslyn APIs will certainly be less limiting since they will support everything languages like C# and VB can express.

## Miscellaneous Questions

### What is elastic trivia?
Elastic trivia is usually in a manually constructed syntax tree to represent flexible whitespace elements.  The trees returned by the parsers represent any whitespace literally as it was in the source code.  Elastic whitespace lets generated trees suggest whitespace elements and ensure tokens are not immediately adjacent to each other.  Formatters and other tree processing tools can freely substitute, lengthen, or change the elastic whitespace in any way without breaking fidelity with an original code source.

### Why do some Syntax factories attach elastic trivia I didn't ask for?
Elastic whitespace denotes suggested whitespace, but rendering a tree to text may use zero or more whitespace where the elastic whitespace is.  Some factories add elastic whitespace to help avoid accidently slamming two things together where you may require whitespace, or where a tool may want to apply options for how to format.  Of course, a renderer needs to be smart and put some whitespace between some tokens, but it could, say, eliminate all whitespace around an open parenthesis depending on formatting options.

This elastic trivia will report that it has zero length because it potentially could be left out when mapping the tree to text.

You can take the result of the factory and remove the elastic trivia if you need to do so (though this would be very uncommon), for example:

``` csharp
     Syntax.Token(SyntaxKind.SemicolonToken).WithLeadingTrivia().WithTrailingTrivia()
```

### How do I format a tree or node to a textual representation?
See the sample code answer tagged “FAQ(35)” ([installed location information|faq#codefiles]) to see several formatting examples.  This sample also includes showing some services on Document, such as SimplifyNames().  The sample “FAQ(26)” also shows SyntaxNode.Format().

### How can I use Roslyn in an MSBuild task and avoid metadata fetching and re-entrancy conflicts?
There is not a great answer for this currently.  Roslyn uses MSBuild internally to construct workspaces, so when you try to construct them from within a Task, there are re-entrancy conflict issues.  If it actually worked, there would also be an issue with a lot of duplicated effort and space.  The right way would be to have a base RoslynTask that derived from Task and that used the MSBuild information already present.  As a workaround for now, you can create a Roslyn Compilation from some of the info in the MSBuild Task and then use the compilation’s code information.  However, this still is not a complete solution since you likely want to affect the code being compiled, add files to the MSBuild targets, or remove them.

The following code is not in the samples/test project because it is not a great answer, but since the question comes up enough, this gives you some idea of what you need to do:

``` csharp
public class MyTask : Task {
    public override bool Execute() {
          var projectFileName = this.BuildEngine.ProjectFileOfTaskNode;
              var project = ProjectCollection.GlobalProjectCollection.
                            GetLoadedProjects(projectFileName).Single();
              var compilation = CSharpCompilation.Create(
                                    project.GetPropertyValue("AssemblyName"),
                                    syntaxTrees: project.GetItems("Compile").Select(
                                      c => SyntaxFactory.ParseCompilationUnit(
                                               c.EvaluatedInclude).SyntaxTree),
                                    references: project.GetItems("Reference")
                                                       .Select(          
                                      r => new MetadataFileReference
                                                   (r.EvaluatedInclude)));
             // Now work with compilation ...
    }
}
```

### Is there an end-to-end example on compiling a program to IL (Emit APIs)?
See next question, [#How can I capture IL, debug info, and doc comment outputs from a Compilation].

### How can I capture IL, debug info, and doc comment outputs from a Compilation?
See the sample code answer tagged “FAQ(34)” ([installed location information|faq#codefiles]).  This sample includes an Execute() method definition, which compiles and executes a binary.

### Is there an object model chart or type inheritance diagram of Roslyn types?
You can create a type inheritance diagram that you can zoom and search within.  You need Visual Studio 2010 Ultimate, and the instructions for creating the diagram are in this [post](http://social.msdn.microsoft.com/Forums/en-US/roslyn/thread/705b090b-58ac-4a94-b7b5-d1408205bc90).

### How to build on Windows 8

If you don't have Visual Studio installed, you may need to install the .NET Framework (for example, 4.6.2).

Then you may get an error `CSC error CS0041: Unexpected error writing debug information -- 'DLL 'Microsoft.DiaSymReader.Native.amd64.dll' failed: the specified module could not be found. (Exception from HRESULT is returned: 0x8007007E)` when building. This can be resolved by installing the [C runtime](https://support.microsoft.com/en-us/help/2999226/update-for-universal-c-runtime-in-windows) (universal CRT).
