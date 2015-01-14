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
* [Getting Started - Semantic Analysis (CSharp).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822179) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822178)
* [Getting Started - Semantic Analysis (VB).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822181) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822180)
* [Getting Started - Syntax Analysis (CSharp).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822183) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822182)
* [Getting Started - Syntax Analysis (VB).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822185) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822184)
* [Getting Started - Syntax Transformation (CSharp).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822187) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822186)
* [Getting Started - Syntax Transformation (VB).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822189) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822188)

## Diagnostics and Code Fixes
* [How To Write a Diagnostic and Code Fix (CSharp).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822458) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822457)
* [How To Write a Diagnostic and Code Fix (VB).pdf](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822460) or [Word docx](http://www.codeplex.com/Download?ProjectName=roslyn&DownloadId=822459)
