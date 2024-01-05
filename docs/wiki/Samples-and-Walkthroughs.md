# Samples

The samples below are available in the Git sources, under the [Samples folder](https://github.com/dotnet/roslyn-sdk/tree/main/samples/) in the `roslyn-sdk` repository: 
* **APISampleUnitTests** - A collection of unit tests that show how various APIs can be used. Many of these methods are referenced in the [FAQ](https://github.com/dotnet/roslyn/blob/main/docs/wiki/FAQ.md). 
* **AsyncPackage** - A set of diagnostics and code fixes that help you use the await/async keywords correctly. 
* **ConsoleClassifier** - A simple console application that prints colored source code to the console. 
* **ConvertToAutoProperty** - A code refactoring to change a simple property with a trivial getter and setter into an auto property. 
* **FormatSolution** - A console application that formats all C# and VB source files in a solution. 
* **ImplementNotifyPropertyChanged** - A code refactoring to make selected properties of a class support PropertyChanged events. Select the properties to be updated in the editor, press Alt+. to show the refactoring lightbulb menu, and choose "Apply INotifyPropertyChanged pattern." 
* **MakeConst** - A diagnostic (user defined compiler warning), that indicates when local variables can be made const, and a quick fix to make the variable into a const. The Diagnostic walkthrough below explores this sample in detail.

You can also explore the [source repository](https://github.com/dotnet/roslyn/tree/main/src) to see how the Compiler and Workspaces layers make use of the APIs: 
* The [Diagnostics folder in the sources](https://github.com/dotnet/roslyn/tree/main/src/Diagnostics) has many examples of real-world Diagnostics (mostly re-implemented FxCop rules) and Code Fixes. Just drill down to the leaf-node code files in the CSharp or VisualBasic subfolders.

# Walkthroughs
Before working through the walkthroughs below, you should first familiarize yourself with the [Roslyn Overview](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Roslyn-Overview.md), which sets up some of the key concepts.

## Getting Started
* Getting Started - Syntax Analysis ([VB](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-VB-Syntax-Analysis.md) | [C#](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Syntax-Analysis.md))
* Getting Started - Semantic Analysis ([VB](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-VB-Semantic-Analysis.md) | [C#](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md))
* Getting Started - Syntax Transformation ([VB](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-VB-Syntax-Transformation.md) | [C#](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Syntax-Transformation.md))
* Getting Started - Writing Custom Analyzers and Code Fixes ([C# & VB](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-Writing-a-Custom-Analyzer-&-Code-Fix.md))
