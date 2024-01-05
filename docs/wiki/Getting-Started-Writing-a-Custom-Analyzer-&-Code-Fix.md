Before Visual Studio 2015, it was difficult to create custom warnings that target C# or Visual Basic. However, since we shipped and open sourced the Diagnostics API in the .NET Compiler Platform ("Roslyn"), this once difficult task has become easy! Using our APIs, all you need to do is perform a bit of analysis to identify your issue and (optionally) provide a tree transformation as a code fix. Once you provide that information, we automatically do the heavy-lifting of running your analysis on a background thread, showing squiggly underlines in the editor, populating the Visual Studio Error List, creating "light bulb" suggestions and showing rich previews.

## Prerequisites
* [Visual Studio 2015](https://www.visualstudio.com/downloads)
* [.NET Compiler Platform SDK](https://aka.ms/roslynsdktemplates)
* [Getting Started C# Syntax Analysis](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Syntax-Analysis.md)
* [Getting Started C# Semantic Analysis](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md)
* [Getting Started C# Syntax Transformation](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Syntax-Transformation.md)

## Resources

### Documentation & Tools
* [Documentation](https://github.com/dotnet/roslyn/tree/main/docs/analyzers) - repo with documentation.
* [API Source Code/Documentation](http://sourceroslyn.io/) - allows you to browse the Roslyn source code online and easily navigate types.
* [RoslynQuoter](http://roslynquoter.azurewebsites.net/) - a tool that for a given C# program shows syntax tree API calls to construct its syntax tree
* [.NET Compiler Platform SDK](https://aka.ms/roslynsdktemplates) - provides the [Syntax Visualizer](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Syntax-Visualizer.md) which lets you inspect the syntax tree of any C# or VB code file that is open inside the Visual Studio IDE.

### Articles
* [C# and Visual Basic - Use Roslyn to Write a Live Code Analyzer for Your API](https://msdn.microsoft.com/en-us/magazine/dn879356.aspx?f=255&MSPPError=-2147217396) - MSDN article that walks you through how to write a custom analyzer.
* [C# - Adding a Code Fix to Your Roslyn Analyzer](https://msdn.microsoft.com/magazine/dn904670.aspx) - MSDN article that walks you through how to write a custom code fix.
* How To Write an Analyzer and Code Fix ([VB](https://github.com/dotnet/roslyn/blob/main/docs/wiki/How-To-Write-a-Visual-Basic-Analyzer-and-Code-Fix.md) | [C#](https://github.com/dotnet/roslyn/blob/main/docs/wiki/How-To-Write-a-C%23-Analyzer-and-Code-Fix.md))
* [Roslyn Diagnostic Analyzer Tutorial](https://blogs.msdn.microsoft.com/dotnet/2015/09/15/our-summer-internship-on-the-net-team/) - interns on the Roslyn team wrote an article about how they built a meta-analyzer to help you learn how to write an analyzer.

### Videos
* [The Power of Roslyn: Improving Your Productivity with Live Code Analyzers with Dustin Campbell (2016)](https://channel9.msdn.com/Events/dotnetConf/2016/The-Power-of-Roslyn-Improving-Your-Productivity-with-Live-Code-Analyzers) - introduces the in-progress IOperation API and SyntaxGenerator API while writing an analyzer & code fix for newing up zero-length arrays.
* [The Power of Roslyn: Improving Your Productivity with Live Code Analyzers with Kasey Uhlenhuth (2016)](https://www.youtube.com/watch?v=zxAKyiQ1XiM&index=36&list=PLM75ZaNQS_Fb7I6E9MDnMgwW1GGZIijf_) - introduces the in-progress IOperation API and SyntaxGenerator API while writing an analyzer & code fix for newing up zero-length arrays.
* [All Things Roslyn with Dustin Campbell (2015)](https://channel9.msdn.com/Events/FutureDecoded/Future-Decoded-2015-UK/22) - walks you through the tools needed to build a custom analyzer/code fix and then implements an analyzer and a fix for using collection initializers on Immutable Collections.
* [.NET Compiler Platform ("Roslyn"): Analyzers and the Rise of Code-Aware Libraries with Anthony D. Green (2015)](https://channel9.msdn.com/Events/dotnetConf/2015/NET-Compiler-Platform-Roslyn-Analyzers-and-the-Rise-of-Code-Aware-Libraries) - explains the power of custom code analyzers and walks through how to build an analyzer for identifying where you've used the keyword `var`. 
* [Improve Your Code Quality Using Live Code Analyzers with Alex Turner (2014)](https://channel9.msdn.com/Events/Visual-Studio/Connect-event-2014/714) - <5 min video that introduces the concept of live code analysis and analyzers.
