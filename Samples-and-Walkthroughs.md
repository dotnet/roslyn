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
* [Getting Started - Semantic Analysis (CSharp).pdf](../blob/master/docs/samples/csharp-semantic.pdf) or [Word docx](../blob/master/docs/samples/csharp-semantic.docx)
* [Getting Started - Semantic Analysis (VB).pdf](../blob/master/docs/samples/vb-semantic.pdf) or [Word docx](../blob/master/docs/samples/vb-semantic.docx)
* [Getting Started - Syntax Analysis (CSharp).pdf](../blob/master/docs/samples/csharp-syntax.pdf) or [Word docx](../blob/master/docs/samples/csharp-syntax.docx)
* [Getting Started - Syntax Analysis (VB).pdf](../blob/master/docs/samples/vb-syntax.pdf) or [Word docx](../blob/master/docs/samples/vb-syntax.docx)
* [Getting Started - Syntax Transformation (CSharp).pdf](../blob/master/docs/samples/csharp-syntax-transform.pdf) or [Word docx](../blob/master/docs/samples/csharp-syntax-transform.docx)
* [Getting Started - Syntax Transformation (VB).pdf](../blob/master/docs/samples/vb-syntax-transform.pdf) or [Word docx](../blob/master/docs/samples/vb-syntax-transform.docx)

## Diagnostics and Code Fixes
* [How To Write a Diagnostic and Code Fix (CSharp).pdf](../blob/master/docs/samples/csharp-diag.pdf) or [Word docx](../blob/master/docs/samples/csharp-diag.docx)
* [How To Write a Diagnostic and Code Fix (VB).pdf](../blob/master/docs/samples/vb-diag.pdf) or [Word docx](../blob/master/docs/samples/vb-diag.docx)

