There are two major categories of work that will happen here: active feature development and quality improvements. Some features are experimental. All submissions need to meet high quality and design expectations. Submissions adding new functionality require extra analysis and design, and remember to always start a design discussion before spending significant effort.

# Quality Improvements
* Performance-related changes, mostly around reducing allocations, as part of an effort to make typing in Visual Studio as pause-free as possible.
* Explore the use of an alternative PDB writer implementation that will allow more parallelism in the emit pipeline.
* Increase the test coverage provided by our open source test suites. 
* Remove the core compiler's dependency on the full .NET framework allowing the use of the Compilation data type on platforms like WinRT.

# API Readiness
* Flesh out the XML documentation comments on the public API.
* Confirm that the public API does appropriate argument validation.
* Ensure that the collection types used across the public API surface are rational. 

# Language Features
* Discuss proposed language features for [C#](CSharp Language Design Notes) and [Visual Basic](Visual Basic Design Notes)
* To see an overview of the features being discussed check out the [Language Feature Status](https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md)

# Diagnostics
* Refine the Diagnostics API, which provides live code analysis as you type.
* Improve the performance of running analyzers from the command-line and in Visual Studio.
* Write more of the FxCop rules as source-based diagnostic analyzers. There are 350+ FxCop rules – feedback is welcome to prioritize which of these should be implemented first.
* Develop flow analysis APIs and provide easy ways to write analyzers that can perform flow analysis.

# Interactive (Scripting and REPL)
* Finishing the language semantics for various expressions at the top level.
* Review and refine the design of the REPL window.
* Review and refine the design of the scripting API.

[Update 7/25/15] You can read our [Interactive Design Meeting notes](https://github.com/dotnet/roslyn/issues?q=label%3A%22Design+Notes%22+label%3AInteractive-Scripting) to learn more about what features we are planning for 1.1 and the scripting dialect.

# IDE Support
* Find All References performance
* Rename conflict-detection improvements