
## Prerequisites
* [Visual Studio 2015](https://www.visualstudio.com/downloads)
* [.NET Compiler Platform SDK](https://aka.ms/roslynsdktemplates)
* [Getting Started VB Syntax Analysis](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-VB-Syntax-Analysis.md)

## Introduction
Today, the Visual Basic and C# compilers are black boxes - text goes in and bytes come out - with no transparency into the intermediate phases of the compilation pipeline. With the **.NET Compiler Platform** (formerly known as "Roslyn"), tools and developers can leverage the exact same data structures and algorithms the compiler uses to analyze and understand code with confidence that that information is accurate and complete.

In this walkthrough we'll explore the **Symbol** and **Binding APIs**. The **Syntax API** exposes the parsers, the syntax trees themselves, and utilities for reasoning about and constructing them.

## Understanding Compilations and Symbols
 The **Syntax API** allows you to look at the _structure_ of a program. However, often you'll want richer information about the semantics or _meaning_ of a program. And while a loose code file or snippet of VB or C# code can be syntactically analyzed in isolation it's not very meaningful to ask questions such as "what's the type of this variable" in a vacuum. The meaning of a type name may be dependent on assembly references, namespace imports, or other code files. That's where the **Compilation** class comes in.

A **Compilation** is analogous to a single project as seen by the compiler and represents everything needed to compile a Visual Basic or C# program such as assembly references, compiler options, and the set of source files to be compiled. With this context you can reason about the meaning of code. **Compilations** allow you to find **Symbols** - entities such as types, namespaces, members, and variables which names and other expressions refer to. The process of associating names and expressions with **Symbols** is called **Binding**.

Like **SyntaxTree**, **Compilation** is an abstract class with language-specific derivatives. When creating an instance of Compilation you must invoke a factory method on the **VisualBasicCompilation** (or **CSharpCompilation**) class.

#### Example - Creating a compilation
This example shows how to create a **Compilation** by adding assembly references and source files. Like the syntax trees, everything in the Symbols API and the Binding API is immutable.

1) Create a new Visual Basic **Stand-Alone Code Analysis Tool** project.
  * In Visual Studio, choose **File -> New -> Project...** to display the New Project dialog.
  * Under **Visual Basic -> Extensibility**, choose **Stand-Alone Code Analysis Tool**.
  * Name your project "**SemanticsVB**" and click OK. 

2) Replace the contents of your **Module1.vb** with the following:
```VB.NET
Option Strict Off

Module Module1
 
    Sub Main()
 
        Dim tree = VisualBasicSyntaxTree.ParseText(
"Imports System
Imports System.Collections.Generic
Imports System.Text
 
Namespace HelloWorld
    Class Class1
        Shared Sub Main(args As String())
            Console.WriteLine(""Hello, World!"")
        End Sub
    End Class
End Namespace")
 
        Dim root As CompilationUnitSyntax = tree.GetRoot()
    End Sub
 
End Module
```

  * Some readers may run with **Option Strict** turned **On** by default at the project level. Turning **Option Strict** **Off** in this walkthrough simplifies many of the examples by removing much of the casting required.

3) Next, add this code to the end of your **Main** method to construct a **Compilation** object:
```VB.NET
        Dim compilation As Compilation =
                VisualBasicCompilation.Create("HelloWorld").
                                       AddReferences(MetadataReference.CreateFromAssembly(
                                                         GetType(Object).Assembly)).
                                       AddSyntaxTrees(tree)
```

4) Move your cursor to the line containing the **End Sub** of your **Main** method and set a breakpoint there.
  * In Visual Studio, choose **Debug -> Toggle Breakpoint**.

5) Run the program.
  * In Visual Studio, choose **Debug -> Start Debugging**.

6)  Hover over the compilation variable and expand the datatips to inspect the **Compilation** object in the debugger.

## The SemanticModel
Once you have a **Compilation** you can ask it for a **SemanticModel** for any **SyntaxTree** contained in that **Compilation**. **SemanticModels** can be queried to answer questions like "What names are in scope at this location?" "What members are accessible from this method?" "What variables are used in this block of text?" and "What does this name/expression refer to?"

#### Example - Binding a name
This example shows how to obtain a **SemanticModel** object for our HelloWorld **SyntaxTree**. Once the model is obtained, the name in the first **Imports** statement is bound to retrieve a **Symbol** for the **System** namespace.

1) Add this code to the end of your Main method. The code gets a **SemanticModel** for the HelloWorld **SyntaxTree** and stores it in a new variable:
```VB.NET
        Dim model = compilation.GetSemanticModel(tree)
```

2) Set this statement as the next statement to be executed and execute it.
  * Right-click this line and choose **Set Next Statement**.
  * In Visual Studio, choose **Debug -> Step Over**, to execute this statement and initialize the new variable.
  * You will need to repeat this process for each of the following steps as we introduce new variables and inspect them with the debugger.

3) Now add this code to bind the **Name** of the "**Imports System**" statement using the **SemanticModel.GetSymbolInfo** method:
```VB.NET
        Dim firstImport As SimpleImportsClauseSyntax =
                root.Imports(0).ImportsClauses(0)
 
        Dim nameInfo = model.GetSymbolInfo(firstImport.Name)
```

4) Execute these statements and hover over the **nameInfo** variable and expand the datatip to inspect the **SymbolInfo** object returned.
* Note the **Symbol** property. This property returns the **Symbol** this expression refers to. For expressions which don't refer to anything (such as numeric literals) this property will be null.
  * Note that the **Symbol.Kind** property returns the value **SymbolKind.Namespace**.

5) Cast the symbol to a **NamespaceSymbol** instance and store it in a new variable:
```VB.NET
        Dim systemSymbol As INamespaceSymbol = nameInfo.Symbol
```

6) Execute this statement and examine the **systemSymbol** variable using the debugger datatips.
 
7) Stop the program.
  * In Visual Studio, choose **Debug -> Stop Debugging**.

8) Add the following code to enumerate the sub-namespaces of the **System** namespace and print their names to the **Console**:
```VB.NET
        For Each ns In systemSymbol.GetNamespaceMembers()
            Console.WriteLine(ns.Name)
        Next
```

9) Press **Ctrl+F5** to run the program. You should see the following output:
```
Collections
Configuration
Deployment
Diagnostics
Globalization
IO
Reflection
Resources
Runtime
Security
StubHelpers
Text
Threading
Press any key to continue . . .
```

#### Example - Binding an expression
The previous example showed how to bind name to find a **Symbol**. However, there are other expressions in a VB program that can be bound that aren't names. This example shows how binding works with other expression types - in this case a simple string literal.

1) Add the following code to locate the "**Hello, World!**" string literal in the **SyntaxTree** and store it in a variable (it should be the only **LiteralExpressionSyntax** in this example):
```VB.NET
        Dim helloWorldString = root.DescendantNodes().
                                    OfType(Of LiteralExpressionSyntax)().
                                    First()
```

2) Start debugging the program.

3) Add the following code to get the **TypeInfo** for this expression:
```VB.NET
        Dim literalInfo = model.GetTypeInfo(helloWorldString)
```

4) Execute this statement and examine the **literalInfo**.
  * Note that its **Type** property is not null and returns the **INamedTypeSymbol** for the **System.String** type because the string literal expression has a compile-time type of **System.String**

5) Stop the program.

6) Add the following code to enumerate the public methods of the **System.String** class which return strings and print their names to the **Console**:
```VB.NET
        Dim stringTypeSymbol As INamedTypeSymbol = literalInfo.Type

        Dim methodNames = From method In stringTypeSymbol.GetMembers().
                                                          OfType(Of IMethodSymbol)()
                          Where method.ReturnType.Equals(stringTypeSymbol) AndAlso
                                method.DeclaredAccessibility = Accessibility.Public
                          Select method.Name Distinct

        Console.Clear()
        For Each name In methodNames
            Console.WriteLine(name)
        Next
```

7) Press **Ctrl+F5** to run to run the program. You should see the following output:
```
Join
Substring
Trim
TrimStart
TrimEnd
Normalize
PadLeft
PadRight
ToLower
ToLowerInvariant
ToUpper
ToUpperInvariant
ToString
Insert
Replace
Remove
Format
Copy
Concat
Intern
IsInterned
Press any key to continue . . .
```

8) Your **Module1.vb** file should now look like this:
```VB.NET
Option Strict Off

Module Module1

    Sub Main()

        Dim tree = VisualBasicSyntaxTree.ParseText(
"Imports System
Imports System.Collections.Generic
Imports System.Text
 
Namespace HelloWorld
    Class Class1
        Shared Sub Main(args As String())
            Console.WriteLine(""Hello, World!"")
        End Sub
    End Class
End Namespace")

        Dim root As CompilationUnitSyntax = tree.GetRoot()

        Dim compilation As Compilation =
                VisualBasicCompilation.Create("HelloWorld").
                                       AddReferences(MetadataReference.CreateFromAssembly(
                                                         GetType(Object).Assembly)).
                                       AddSyntaxTrees(tree)

        Dim model = compilation.GetSemanticModel(tree)

        Dim firstImport As SimpleImportsClauseSyntax =
                root.Imports(0).ImportsClauses(0)

        Dim nameInfo = model.GetSymbolInfo(firstImport.Name)

        Dim systemSymbol As INamespaceSymbol = nameInfo.Symbol

        For Each ns In systemSymbol.GetNamespaceMembers()
            Console.WriteLine(ns.Name)
        Next

        Dim helloWorldString = root.DescendantNodes().
                                    OfType(Of LiteralExpressionSyntax)().
                                    First()

        Dim literalInfo = model.GetTypeInfo(helloWorldString)

        Dim stringTypeSymbol As INamedTypeSymbol = literalInfo.Type

        Dim methodNames = From method In stringTypeSymbol.GetMembers().
                                                          OfType(Of IMethodSymbol)()
                          Where method.ReturnType.Equals(stringTypeSymbol) AndAlso
                                method.DeclaredAccessibility = Accessibility.Public
                          Select method.Name Distinct

        Console.Clear()
        For Each name In methodNames
            Console.WriteLine(name)
        Next
    End Sub

End Module
```

9) Congratulations! You've just used the **Symbol** and **Binding APIs** to analyze the meaning of names and expressions in a VB program.