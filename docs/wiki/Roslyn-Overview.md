# .NET Compiler Platform ("Roslyn") Overview

[Download in PDF](https://docs.microsoft.com/en-us/dotnet/opbuildpdf/csharp/roslyn-sdk/toc.pdf?branch=live)

Contents

* [Introduction](#introduction)
* [Exposing the Compiler APIs](#exposing-the-compiler-apis)
    * [Compiler Pipeline Functional Areas](#compiler-pipeline-functional-areas)
    * [API Layers](#api-layers)
        * [Compiler APIs](#compiler-apis)
        * [Workspaces APIs](#workspaces-apis)
* [Working with Syntax](#working-with-syntax)
    * [Syntax Trees](#syntax-trees)
    * [Syntax Nodes](#syntax-nodes)
    * [Syntax Tokens](#syntax-tokens)
    * [Syntax Trivia](#syntax-trivia)
    * [Spans](#spans)
    * [Kinds](#kinds)
    * [Errors](#errors)
* [Working with Semantics](#working-with-semantics)
    * [Compilation](#compilation)
    * [Symbols](#symbols)
    * [Semantic Model](#semantic-model)
* [Working with a Workspace](#working-with-a-workspace)
    * [Workspace](#workspace)
    * [Solutions, Projects and Documents](#solutions-projects-documents)

# Summary

## Introduction

Traditionally, compilers are black boxes -- source code goes in one end, magic happens in the middle, and object files or assemblies come out the other end. As compilers perform their magic, they build up deep understanding of the code they are processing, but that knowledge is unavailable to anyone but the compiler implementation wizards.  The information is promptly forgotten after the translated output is produced.

For decades, this world view has served us well, but it is no longer sufficient. Increasingly we rely on integrated development environment (IDE) features such as IntelliSense, refactoring, intelligent rename, “Find all references,” and “Go to definition” to increase our productivity. We rely on code analysis tools to improve our code quality and code generators to aid in application construction. As these tools get smarter, they need access to more and more of the deep code knowledge that only compilers possess. This is the core mission of Roslyn: opening up the black boxes and allowing tools and end users to share in the wealth of information compilers have about our code. Instead of being opaque source-code-in and object-code-out translators, through Roslyn, compilers become platforms—APIs that you can use for code related tasks in your tools and applications.

The transition to compilers as platforms dramatically lowers the barrier to entry for creating code focused tools and applications. It creates many opportunities for innovation in areas such as meta-programming, code generation and transformation, interactive use of the C# and VB languages, and embedding of C# and VB in domain specific languages.

Roslyn SDK Preview includes the latest drafts of new language object models for code generation, analysis, and refactoring. We hope to include drafts of API support for scripting and interactive use of C# and Visual Basic in a future preview. This document provides a conceptual overview of Roslyn. Further details can be found in the walkthroughs and samples included in the SDK Preview.

## Exposing the Compiler APIs
### Compiler Pipeline Functional Areas

Roslyn exposes the C# and Visual Basic compiler’s code analysis to you as a consumer by providing an API layer that mirrors a traditional compiler pipeline.

![compiler pipeline](images/compiler-pipeline.png)

Each phase of this pipeline is now a separate component. First the parse phase, where source is tokenized and parsed into syntax that follows the language grammar. Second the declaration phase, where declarations from source and imported metadata are analyzed to form named symbols. Next the bind phase, where identifiers in the code are matched to symbols. Finally, the emit phase, where all the information built up by the compiler is emitted as an assembly.

![compiler pipeline api](images/compiler-pipeline-api.png)
 
Corresponding to each of those phases, an object model is surfaced that allows access to the information at that phase. The parsing phase is exposed as a syntax tree, the declaration phase as a hierarchical symbol table, the binding phase as a model that exposes the result of the compiler’s semantic analysis and the emit phase as an API that produces IL byte codes.

![compiler api lang service](images/compiler-pipeline-lang-svc.png)

Each compiler combines these components together as a single end-to-end whole.

To ensure that the public Compiler APIs are sufficient for building world-class IDE features, the language services that will be used to power the C# and VB experiences in Visual Studio vNext have been rebuilt using them. For instance, the code outlining and formatting features use the syntax trees, the Object Browser and navigation features use the symbol table, refactorings and Go to Definition use the semantic model, and Edit and Continue uses all of these, including the Emit API. These experiences may be previewed on Visual Studio 2013 through the “Roslyn” End-User Preview. This preview is required in order to build and test applications build on top of Roslyn SDK meant for integration into Visual Studio though Roslyn APIs can be used in your own applications independently of Visual Studio without requiring the End-User Preview.

### API Layers

Roslyn consists of two main layers of APIs – the Compiler APIs and Workspaces APIs.

![api layers](images/alex-api-layers.png)

#### Compiler APIs

The compiler layer contains the object models that correspond with information exposed at each phase of the compiler pipeline, both syntactic and semantic. The compiler layer also contains an immutable snapshot of a single invocation of a compiler, including assembly references, compiler options, and source code files. There are two distinct APIs that represent the C# language and the Visual Basic language. These two APIs are similar in shape but tailored for high-fidelity to each individual language. This layer has no dependencies on Visual Studio components.

#### Diagnostic APIs

As part of their analysis the compiler may produce a set of diagnostics covering everything from syntax, semantic, and definite assignment errors to various warnings and informational diagnostics. The Compiler API layer exposes diagnostics through an extensible API allowing for user-defined analyzers to be plugged into a Compilation and user-defined diagnostics, such as those produced by tools like StyleCop or FxCop, to be produced alongside compiler-defined diagnostics. Producing diagnostics in this way has the benefit of integrating naturally with tools such as MSBuild and Visual Studio which depend on diagnostics for experiences such as halting a build based on policy and showing live squiggles in the editor and suggesting code fixes.

#### Scripting APIs

As a part of the compiler layer, the team created hosting/scripting APIs for executing code snippets and accumulating a runtime execution context.  The REPL uses these APIs.

#### Workspaces APIs

The Workspaces layer contains the Workspace API, which is the starting point for doing code analysis and refactoring over entire solutions. It assists you in organizing all the information about the projects in a solution into single object model, offering you direct access to the compiler layer object models without needing to parse files, configure options or manage project to project dependencies.

In addition, the Workspaces layer surfaces a set of commonly used APIs used when implementing code analysis and refactoring tools that function within a host environment like the Visual Studio IDE, such as the Find All References, Formatting, and Code Generation APIs.

This layer has no dependencies on Visual Studio components.

## Working with Syntax

The most fundamental data structure exposed by the Compiler APIs is the syntax tree. These trees represent the lexical and syntactic structure of source code. They serve two important purposes:

1. To allow tools - such as an IDE, add-ins, code analysis tools, and refactorings - to see and process the syntactic structure of source code in a user’s project.
2. To enable tools - such as refactorings and an IDE - to create, modify, and rearrange source code in a natural manner without having use direct text edits. By creating and manipulating trees, tools can easily create and rearrange source code.

### Syntax Trees

Syntax trees are the primary structure used for compilation, code analysis, binding, refactoring, IDE features, and code generation. No part of the source code is understood without it first being identified and categorized into one of many well-known structural language elements. 

Syntax trees have three key attributes. The first attribute is that syntax trees hold all the source information in full fidelity. This means that the syntax tree contains every piece of information found in the source text, every grammatical construct, every lexical token, and everything else in between including whitespace, comments, and preprocessor directives. For example, each literal mentioned in the source is represented exactly as it was typed. The syntax trees also represent errors in source code when the program is incomplete or malformed, by representing skipped or missing tokens in the syntax tree.  

This enables the second attribute of syntax trees. A syntax tree obtained from the parser is completely round-trippable back to the text it was parsed from. From any syntax node, it is possible to get the text representation of the sub-tree rooted at that node. This means that syntax trees can be used as a way to construct and edit source text. By creating a tree you have by implication created the equivalent text, and by editing a syntax tree, making a new tree out of changes to an existing tree, you have effectively edited the text. 

The third attribute of syntax trees is that they are immutable and thread-safe.  This means that after a tree is obtained, it is a snapshot of the current state of the code, and never changes. This allows multiple users to interact with the same syntax tree at the same time in different threads without locking or duplication. Because the trees are immutable and no modifications can be made directly to a tree, factory methods help create and modify syntax trees by creating additional snapshots of the tree. The trees are efficient in the way they reuse underlying nodes, so the new version can be rebuilt fast and with little extra memory.

A syntax tree is literally a tree data structure, where non-terminal structural elements parent other elements. Each syntax tree is made up of nodes, tokens, and trivia.  

### Syntax Nodes

Syntax nodes are one of the primary elements of syntax trees. These nodes represent syntactic constructs such as declarations, statements, clauses, and expressions. Each category of syntax nodes is represented by a separate class derived from SyntaxNode. The set of node classes is not extensible. 

All syntax nodes are non-terminal nodes in the syntax tree, which means they always have other nodes and tokens as children. As a child of another node, each node has a parent node that can be accessed through the Parent property. Because nodes and trees are immutable, the parent of a node never changes. The root of the tree has a null parent.  

Each node has a ChildNodes method, which returns a list of child nodes in sequential order based on its position in the source text. This list does not contain tokens. Each node also has a collection of Descendant methods - such as DescendantNodes, DescendantTokens, or DescendantTrivia - that represent a list of all the nodes, tokens, or trivia that exist in the sub-tree rooted by that node.  

In addition, each syntax node subclass exposes all the same children through strongly typed properties. For example, a BinaryExpressionSyntax node class has three additional properties specific to binary operators: Left, OperatorToken, and Right. The type of Left and Right is ExpressionSyntax, and the type of OperatorToken is SyntaxToken.

Some syntax nodes have optional children. For example, an IfStatementSyntax has an optional ElseClauseSyntax. If the child is not present, the property returns null. 

### Syntax Tokens

Syntax tokens are the terminals of the language grammar, representing the smallest syntactic fragments of the code. They are never parents of other nodes or tokens. Syntax tokens consist of keywords, identifiers, literals, and punctuation. 

For efficiency purposes, the SyntaxToken type is a CLR value type. Therefore, unlike syntax nodes, there is only one structure for all kinds of tokens with a mix of properties that have meaning depending on the kind of token that is being represented.

For example, an integer literal token represents a numeric value. In addition to the raw source text the token spans, the literal token has a Value property that tells you the exact decoded integer value. This property is typed as Object because it may be one of many primitive types.

The ValueText property tells you the same information as the Value property; however this property is always typed as String. An identifier in C# source text may include Unicode escape characters, yet the syntax of the escape sequence itself is not considered part of the identifier name. So although the raw text spanned by the token does include the escape sequence, the ValueText property does not. Instead, it includes the Unicode characters identified by the escape.

### Syntax Trivia

Syntax trivia represent the parts of the source text that are largely insignificant for normal understanding of the code, such as whitespace, comments, and preprocessor directives. 

Because trivia are not part of the normal language syntax and can appear anywhere between any two tokens, they are not included in the syntax tree as a child of a node. Yet, because they are important when implementing a feature like refactoring and to maintain full fidelity with the source text, they do exist as part of the syntax tree.

You can access trivia by inspecting a token’s LeadingTrivia or TrailingTrivia collections. When source text is parsed, sequences of trivia are associated with tokens. In general, a token owns any trivia after it on the same line up to the next token. Any trivia after that line is associated with the following token. The first token in the source file gets all the initial trivia, and the last sequence of trivia in the file is tacked onto the end-of-file token, which otherwise has zero width.

Unlike syntax nodes and tokens, syntax trivia do not have parents. Yet, because they are part of the tree and each is associated with a single token, you may access the token it is associated with using the Token property.

Like syntax tokens, trivia are value types. The single SyntaxTrivia type is used to describe all kinds of trivia.

### Spans

Each node, token, or trivia knows its position within the source text and the number of characters it consists of. A text position is represented as a 32-bit integer, which is a zero-based Unicode character index. A TextSpan object is the beginning position and a count of characters, both represented as integers. If TextSpan has a zero length, it refers to a location between two characters.

Each node has two TextSpan properties: Span and FullSpan. 

The Span property is the text span from the start of the first token in the node’s sub-tree to the end of the last token. This span does not include any leading or trailing trivia.

The FullSpan property is the text span that includes the node’s normal span, plus the span of any leading or trailing trivia.

For example: 

``` csharp
      if (x > 3)
      {
||        // this is bad
          |throw new Exception("Not right.");|  // better exception?||
      }
```

The statement node inside the block has a span indicated by the single vertical bars (|). It includes the characters +throw new Exception(“Not right.”);+. The full span is indicated by the double vertical bars (||). It includes the same characters as the span and the characters associated with the leading and trailing trivia.

### Kinds

Each node, token, or trivia has a RawKind property, of type System.Int32, that identifies the exact syntax element represented. This value can be cast to a language-specific enumeration; each language, C# or VB, has a single SyntaxKind enumeration that lists all the possible nodes, tokens, and trivia elements in the grammar. This conversion can be done automatically by accessing the CSharpSyntaxKind() or VisualBasicSyntaxKind() extension methods.

The RawKind property allows for easy disambiguation of syntax node types that share the same node class. For tokens and trivia, this property is the only way to distinguish one type of element from another. 

For example, a single BinaryExpressionSyntax class has Left, OperatorToken, and Right as children. The Kind property distinguishes whether it is an AddExpression, SubtractExpression, or MultiplyExpression kind of syntax node.

### Errors

Even when the source text contains syntax errors, a full syntax tree that is round-trippable to the source is exposed. When the parser encounters code that does not conform to the defined syntax of the language, it uses one of two techniques to create a syntax tree.

First, if the parser expects a particular kind of token, but does not find it, it may insert a missing token into the syntax tree in the location that the token was expected. A missing token represents the actual token that was expected, but it has an empty span, and its IsMissing property returns true.

Second, the parser may skip tokens until it finds one where it can continue parsing. In this case, the skipped tokens that were skipped are attached as a trivia node with the kind SkippedTokens.

## Working with Semantics

Syntax trees represent the lexical and syntactic structure of source code. Although this information alone is enough to describe all the declarations and logic in the source, it is not enough information to identify what is being referenced.

For example, many types, fields, methods, and local variables with the same name may be spread throughout the source. Although each of these is uniquely different, determining which one an identifier actually refers to often requires a deep understanding of the language rules. 

There are program elements represented in source code, and programs can also refer to previously compiled libraries, packaged in assembly files. Although no source code is available for assemblies and therefore no syntax nodes or trees, programs can still refer to elements inside them.

In addition to a syntactic model of the source code, a semantic model encapsulates the language rules, giving you an easy way to make these distinctions.

### Compilation

A compilation is a representation of everything needed to compile a C# or Visual Basic program, which includes all the assembly references, compiler options, and source files. 

Because all this information is in one place, the elements contained in the source code can be described in more detail. The compilation represents each declared type, member, or variable as a symbol. The compilation contains a variety of methods that help you find and relate the symbols that have either been declared in the source code or imported as metadata from an assembly.

Similar to syntax trees, compilations are immutable. After you create a compilation, it cannot be changed by you or anyone else you might be sharing it with. However, you can create a new compilation from an existing compilation, specifying a change as you do so. For example, you might create a compilation that is the same in every way as an existing compilation, except it may include an additional source file or assembly reference.

### Symbols
A symbol represents a distinct element declared by the source code or imported from an assembly as metadata. Every namespace, type, method, property, field, event, parameter, or local variable is represented by a symbol. 

A variety of methods and properties on the Compilation type help you find symbols. For example, you can find a symbol for a declared type by its common metadata name. You can also access the entire symbol table as a tree of symbols rooted by the global namespace.

Symbols also contain additional information that the compiler determined from the source or metadata, such as other referenced symbols. Each kind of symbol is represented by a separate interface derived from ISymbol, each with its own methods and properties detailing the information the compiler has gathered. Many of these properties directly reference other symbols. For example, the ReturnType property of the IMethodSymbol class tells you the actual type symbol that the method declaration referenced.

Symbols present a common representation of namespaces, types, and members, between source code and metadata. For example, a method that was declared in source code and a method that was imported from metadata are both represented by an IMethodSymbol with the same properties.

Symbols are similar in concept to the CLR type system as represented by the System.Reflection API, yet they are richer in that they model more than just types. Namespaces, local variables, and labels are all symbols. In addition, symbols are a representation of language concepts, not CLR concepts. There is a lot of overlap, but there are many meaningful distinctions as well. For instance, an iterator method in C# or Visual Basic is a single symbol. However, when the iterator method is translated to CLR metadata, it is a type and multiple methods.

### Semantic Model

A semantic model represents all the semantic information for a single source file. You can use it to discover the following: 

* The symbols referenced at a specific location in source.
* The resultant type of any expression.
* All diagnostics, which are errors and warnings.
* How variables flow in and out of regions of source.
* The answers to more speculative questions.

## Working with a Workspace

The Workspaces layer is the starting point for doing code analysis and refactoring over entire solutions. Within this layer, the Workspace API assists you in organizing all the information about the projects in a solution into single object model, offering you direct access to compiler layer object models like source text, syntax trees, semantic models and compilations without needing to parse files, configure options or manage inter-project dependencies. 

Host environments, like an IDE, provide a workspace for you corresponding to the open solution. It is also possible to use this model outside of an IDE by simply loading a solution file.

### Workspace

A workspace is an active representation of your solution as a collection of projects, each with a collection of documents. A workspace is typically tied to a host environment that is constantly changing as a user types or manipulates properties. 

The workspace provides access to the current model of the solution. When a change in the host environment occurs, the workspace fires corresponding events, and the CurrentSolution property is updated. For example, when the user types in a text editor corresponding to one of the source documents, the workspace uses an event to signal that the overall model of the solution has changed and which document was modified. You can then react to those changes by analyzing the new model for correctness, highlighting areas of significance, or by making a suggestion for a code change. 

You can also create stand-alone workspaces that are disconnected from the host environment or used in an application that has no host environment.

### Solutions, Projects, Documents

Although a workspace may change every time a key is pressed, you can work with the model of the solution in isolation. 

A solution is an immutable model of the projects and documents. This means that the model can be shared without locking or duplication. After you obtain a solution instance from the Workspace’s CurrentSolution property, that instance will never change. However, like with syntax trees and compilations, you can modify solutions by constructing new instances based on existing solutions and specific changes. To get the workspace to reflect your changes, you must explicitly apply the changed solution back to the workspace.

A project is a part of the overall immutable solution model. It represents all the source code documents, parse and compilation options, and both assembly and project-to-project references. From a project, you can access the corresponding compilation without needing to determine project dependencies or parse any source files.

A document is also a part of the overall immutable solution model. A document represents a single source file from which you can access the text of the file, the syntax tree, and the semantic model.

The following diagram is a representation of how the Workspace relates to the host environment, tools, and how edits are made.

![workspace relations](images/workspace-obj-relations.png)

## Summary

Roslyn exposes a set of Compiler APIs and Workspaces APIs that provides rich information about your source code and that has full fidelity with the C# and Visual Basic languages.  The transition to compilers as a platform dramatically lowers the barrier to entry for creating code focused tools and applications. It creates many opportunities for innovation in areas such as meta-programming, code generation and transformation, interactive use of the C# and VB languages, and embedding of C# and VB in domain specific languages.  
