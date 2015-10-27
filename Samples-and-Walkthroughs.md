# Samples

The samples below are available in the Git sources, under the [Samples folder](https://github.com/dotnet/roslyn/tree/master/src/Samples): 
* **APISampleUnitTests** - A collection of unit tests that show how various APIs can be used. Many of these methods are referenced in the [FAQ](https://github.com/dotnet/roslyn/wiki/FAQ). 
* **AsyncPackage** - A set of diagnostics and code fixes that help you use the await/async keywords correctly. 
* **ConsoleClassifier** - A simple console application that prints colored source code to the console. 
* **ConvertToAutoProperty** - A code refactoring to change a simple property with a trivial getter and setter into an auto property. 
* **FormatSolution** - A console application that formats all C# and VB source files in a solution. 
* **ImplementNotifyPropertyChanged** - A code refactoring to make selected properties of a class support PropertyChanged events. Select the properties to be updated in the editor, press Alt+. to show the refactoring lightbulb menu, and choose "Apply INotifyPropertyChanged pattern." 
* **MakeConst** - A diagnostic (user defined compiler warning), that indicates when local variables can be made const, and a quick fix to make the variable into a const. The Diagnostic walkthrough below explores this sample in detail.

You can also explore the [source repository](https://github.com/dotnet/roslyn/tree/master/src) to see how the Compiler and Workspaces layers make use of the APIs: 
* The [Diagnostics folder in the sources](https://github.com/dotnet/roslyn/tree/master/src/Diagnostics) has many examples of real-world Diagnostics (mostly re-implemented FxCop rules) and Code Fixes. Just drill down to the leaf-node code files in the CSharp or VisualBasic subfolders.

# Walkthroughs
Before working through the walkthroughs below, you should first familiarize yourself with the [Roslyn Overview](https://github.com/dotnet/roslyn/wiki/Roslyn-Overview), which sets up some of the key concepts.

## Getting Started
* Getting Started - Syntax Analysis ([VB](https://github.com/dotnet/roslyn/wiki/Getting-Started-VB-Syntax-Analysis) | [C#](https://github.com/dotnet/roslyn/wiki/Getting-Started-C%23-Syntax-Analysis))
* Getting Started - Semantic Analysis ([VB](https://github.com/dotnet/roslyn/wiki/Getting-Started-VB-Semantic-Analysis) | [C#](https://github.com/dotnet/roslyn/wiki/Getting-Started-C%23-Semantic-Analysis))
* Getting Started - Syntax Transformation ([VB](https://github.com/dotnet/roslyn/wiki/Getting-Started-VB-Syntax-Transformation) | [C#](https://github.com/dotnet/roslyn/wiki/Getting-Started-C%23-Syntax-Transformation))

## Analyzers and Code Fixes
* How To Write an Analyzer and Code Fix ([VB](https://github.com/dotnet/roslyn/wiki/How-To-Write-a-Visual-Basic-Analyzer-and-Code-Fix) | [C#](https://github.com/dotnet/roslyn/wiki/How-To-Write-a-C%23-Analyzer-and-Code-Fix))