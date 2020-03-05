## Prerequisites
* [Visual Studio 2015](https://www.visualstudio.com/downloads)
* [.NET Compiler Platform SDK](https://aka.ms/roslynsdktemplates)

## Introduction
Today, the Visual Basic and C# compilers are black boxes - text goes in and bytes come out - with no transparency into the intermediate phases of the compilation pipeline. With the **.NET Compiler Platform** (formerly known as "Roslyn"), tools and developers can leverage the exact same data structures and algorithms the compiler uses to analyze and understand code with confidence that that information is accurate and complete.

In this walkthrough we'll explore the **Syntax API**. The **Syntax API** exposes the parsers, the syntax trees themselves, and utilities for reasoning about and constructing them.

## Understanding Syntax Trees
The **Syntax API** exposes the syntax trees the compilers use to understand Visual Basic and C# programs. They are produced by the same parser that runs when a project is built or a developer hits F5. The syntax trees have full-fidelity with the language; every bit of information in a code file is represented in the tree, including things like comments or whitespace. Writing a syntax tree to text will reproduce the exact original text that was parsed. The syntax trees are also immutable; once created a syntax tree can never be changed. This means consumers of the trees can analyze the trees on multiple threads, without locks or other concurrency measures, with the security that the data will never change under.

The four primary building blocks of syntax trees are:

* The **SyntaxTree** class, an instance of which represents an entire parse tree. **SyntaxTree** is an abstract class which has language-specific derivatives. To parse syntax in a particular language you will need to use the parse methods on the **CSharpSyntaxTree** (or **VisualBasicSyntaxTree**) class.
* The **SyntaxNode** class, instances of which represent syntactic constructs such as declarations, statements, clauses, and expressions.
* The **SyntaxToken** structure, which represents an individual keyword, identifier, operator, or punctuation.
* And lastly the **SyntaxTrivia** structure, which represents syntactically insignificant bits of information such as the whitespace between tokens, preprocessing directives, and comments.

Trivia, tokens, and nodes are composed hierarchically to form a tree that completely represents everything in a fragment of Visual Basic or C# code. For example, were you to examine the following C# source file using the **Syntax Visualizer** (In Visual Studio, choose **View -> Other Windows -> Syntax Visualizer**) it tree view would look like this:

**SyntaxNode**: Blue | **SyntaxToken**: Green | **SyntaxTrivia**: Red
![C# Code File](images/walkthrough-csharp-syntax-figure1.png)

By navigating this tree structure you can find any statement, expression, token, or bit of whitespace in a code file!

## Traversing Trees
### Manual Traversal
The following steps use **Edit and Continue** to demonstrate how to parse C# source text and find a parameter declaration contained in the source.

#### Example - Manually traversing the tree
1) Create a new C# **Stand-Alone Code Analysis Tool** project.
  * In Visual Studio, choose **File -> New -> Project...** to display the New Project dialog.
  * Under **Visual C# -> Extensibility**, choose **Stand-Alone Code Analysis Tool**.
  * Name your project "**GettingStartedCS**" and click OK. 

2) Add the following using directives to your Program.cs file:
```C#
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

3) Enter the following code into your **Main** method:
```C#
SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections;
using System.Linq;
using System.Text;
 
namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}");
 
var root = (CompilationUnitSyntax)tree.GetRoot();
```

4) Move your cursor to the line containing the **closing brace** of your **Main** method and set a breakpoint there.
  * In Visual Studio, choose **Debug -> Toggle Breakpoint**.

5) Run the program.
  * In Visual Studio, choose **Debug -> Start Debugging**.

6) Inspect the root variable in the debugger by hovering over it and expanding the datatip.
  * Note that its **Usings** property is a collection with four elements; one for each using directive in the parsed text.
  * Note that the **KindText** of the root node is **CompilationUnit**.
  * Note that the **Members** collection of the **CompilationUnitSyntax** node has one element.

7) Insert the following statement at the end of the Main method to store the first member of the root **CompilationUnitSyntax** variable into a new variable:
```C#
            var firstMember = root.Members[0];
```

8) Set this statement as the next statement to be executed and execute it.
  * Right-click this line and choose **Set Next Statement**.
  * In Visual Studio, choose **Debug -> Step Over**, to execute this statement and initialize the new variable.
  * You will need to repeat this process for each of the following steps as we introduce new variables and inspect them with the debugger.

9) Hover over the **firstMember** variable and expand the datatips to inspect it. 
  * Note that its **KindText** is **NamespaceDeclaration**.
  * Note that its run-time type is **NamespaceDeclarationSyntax**. 

10) Cast this node to **NamespaceDeclarationSyntax** and store it in a new variable:
```C#
            var helloWorldDeclaration = (NamespaceDeclarationSyntax)firstMember;
```

11) Execute this statement and examine the **helloWorldDeclaration** variable.
  * Note that like the **CompilationUnitSyntax**, **NamespaceDeclarationSyntax** also has a **Members** collection.

12) Examine the **Members** collection.
  * Note that it contains a single member. Examine it.
    * Note that its **KindText** is **ClassDeclaration.**
    * Note that its run-time type is **ClassDeclarationSyntax**.

13) Cast this node to **ClassDeclarationSyntax** and store it in a new variable:
```C#
            var programDeclaration = (ClassDeclarationSyntax)helloWorldDeclaration.Members[0];
```

14) Execute this statement.

15) Locate the **Main** declaration in the **programDeclaration.Members** collection and store it in a new variable:
```C#
            var mainDeclaration = (MethodDeclarationSyntax)programDeclaration.Members[0];
```

16) Execute this statement and examine the members of the **MethodDeclarationSyntax** object.
  * Note the **ReturnType**, and **Identifier** properties.
  * Note the **Body** property.
  * Note the **ParameterList** property; examine it.
    * Note that it contains both the open and close parentheses of the parameter list in addition to the list of parameters themselves.
    * Note that the parameters are stored as a **SeparatedSyntaxList**<**ParameterSyntax**>.

17) Store the first parameter of the **Main** declaration in a variable. 
```C#
            var argsParameter = mainDeclaration.ParameterList.Parameters[0];
```

18) Execute this statement and examine the **argsParameter** variable.
  * Examine the **Identifier** property; note that it is of the structure type **SyntaxToken**.
  * Examine the properties of the **Identifier** **SyntaxToken**; note that the text of the identifier can be found in the **ValueText** property.

19) Stop the program.
  * In Visual Studio, choose **Debug -> Stop Debugging**.

20) Your program should look like this now:
```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
 
namespace GettingStartedCS
{
    class Program
    {
        static void Main(string[] args)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections;
using System.Linq;
using System.Text;
 
namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}");
 
            var root = (CompilationUnitSyntax)tree.GetRoot();
 
            var firstMember = root.Members[0];
 
            var helloWorldDeclaration = (NamespaceDeclarationSyntax)firstMember;
 
            var programDeclaration = (ClassDeclarationSyntax)helloWorldDeclaration.Members[0];
 
            var mainDeclaration = (MethodDeclarationSyntax)programDeclaration.Members[0];
 
            var argsParameter = mainDeclaration.ParameterList.Parameters[0];
 
        }
    }
}
```

### Query Methods
In addition to traversing trees using the properties of the **SyntaxNode** derived classes you can also explore the syntax tree using the query methods defined on **SyntaxNode**. These methods should be immediately familiar to anyone familiar with XPath. You can use these methods with LINQ to quickly find things in a tree. 

#### Example - Using query methods
1) Using IntelliSense, examine the members of the **SyntaxNode** class through the root variable.
  * Note query methods such as **DescendantNodes**, **AncestorsAndSelf**, and **ChildNodes**.

2) Add the following statements to the end of the Main method. The first statement uses a LINQ expression and the **DescendantNodes** method to locate the same parameter as in the previous example:
```C#
var firstParameters = from methodDeclaration in root.DescendantNodes()
                                                    .OfType<MethodDeclarationSyntax>()
                      where methodDeclaration.Identifier.ValueText == "Main"
                      select methodDeclaration.ParameterList.Parameters.First();
 
var argsParameter2 = firstParameters.Single();
```

3) Start debugging the program.

4) Open the **Immediate Window**.
  * In Visual Studio, choose **Debug -> Windows -> Immediate**.

5) Using the Immediate window, type the expression **argsParameter == argsParameter2** and press enter to evaluate it. 
  * Note that the LINQ expression found the same parameter as manually navigating the tree.

6) Stop the program.

### SyntaxWalkers
Often you'll want to find all nodes of a specific type in a syntax tree, for example, every property declaration in a file. By extending the **CSharpSyntaxWalker** class and overriding the **VisitPropertyDeclaration** method you can process every property declaration in a syntax tree without knowing its structure beforehand. **CSharpSyntaxWalker** is a specific kind of **SyntaxVisitor** which recursively visits a node and each of its children.

#### Example - Implementing a SyntaxWalker
This example shows how to implement a **CSharpSyntaxWalker** which examines an entire syntax tree and collects any **using** directives it finds which aren't importing a **System** namespace.

1) Create a new C# **Stand-Alone Code Analysis Tool** project; name it "**UsingCollectorCS**".

2) Add the following using directives to your **Program.cs** file:
```C#
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

3) Enter the following code into your **Main** method:
```C#
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
 
namespace TopLevel
{
    using Microsoft;
    using System.ComponentModel;
 
    namespace Child1
    {
        using Microsoft.Win32;
        using System.Runtime.InteropServices;
 
        class Foo { }
    }
 
    namespace Child2
    {
        using System.CodeDom;
        using Microsoft.CSharp;
 
        class Bar { }
    }
}");
 
            var root = (CompilationUnitSyntax)tree.GetRoot();
```

4) Note that this source text contains **using** directives scattered across four different locations: the file-level, in the top-level namespace, and in the two nested namespaces.

5) Add a new class file to the project.
  * In Visual Studio, choose **Project -> Add New Item...** 
  * In the "Add New Item" dialog type **UsingCollector.cs** as the filename.

6) Add the following using directives to the top of the UsingCollector.cs file 
```C#
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

7) Make the new **UsingCollector** class in this file extend the **CSharpSyntaxWalker** class:
```C#
    class UsingCollector : CSharpSyntaxWalker
```

8) Declare a public read-only field in the **UsingCollector** class; we'll use this variable to store the **UsingDirectiveSyntax** nodes we find:
```C#
        public readonly List<UsingDirectiveSyntax> Usings = new List<UsingDirectiveSyntax>();
```

9) Override the **VisitUsingDirective** method:
```C#
        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            
        }
```

10) Using IntelliSense, examine the **UsingDirectiveSyntax** class through the **node** parameter of this method.
  * Note the **Name** property of type **NameSyntax**; this stores the name of the namespace being imported.

11) Replace the code in the **VisitUsingDirective** method with the following to conditionally add the found **node** to the **Usings** collection if **Name** doesn't refer to the **System** namespace or any of its descendant namespaces:
```C#
            if (node.Name.ToString() != "System" &&
                !node.Name.ToString().StartsWith("System."))
            {
                this.Usings.Add(node);
            }
```

12) The **UsingCollector.cs** file should now look like this:
```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
 
namespace UsingCollectorCS
{
    class UsingCollector : CSharpSyntaxWalker
    {
        public readonly List<UsingDirectiveSyntax> Usings = new List<UsingDirectiveSyntax>();

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name.ToString() != "System" &&
                !node.Name.ToString().StartsWith("System."))
            {
                this.Usings.Add(node);
            }
        }
    }
}
```

13) Return to the **Program.cs** file.

14) Add the following code to the end of the **Main** method to create an instance of the **UsingCollector**, use that instance to visit the root of the parsed tree, and iterate over the **UsingDirectiveSyntax** nodes collected and print their names to the **Console**:
```C#
            var collector = new UsingCollector();
            collector.Visit(root);
 
            foreach (var directive in collector.Usings)
            {
                Console.WriteLine(directive.Name);
            }
```

15) Your **Program.cs** file should now look like this:
```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
 
namespace UsingCollectorCS
{
    class Program
    {
        static void Main(string[] args)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
 
namespace TopLevel
{
    using Microsoft;
    using System.ComponentModel;
 
    namespace Child1
    {
        using Microsoft.Win32;
        using System.Runtime.InteropServices;
 
        class Foo { }
    }
 
    namespace Child2
    {
        using System.CodeDom;
        using Microsoft.CSharp;
 
        class Bar { }
    }
}");
 
            var root = (CompilationUnitSyntax)tree.GetRoot();
 
            var collector = new UsingCollector();
            collector.Visit(root);
 
            foreach (var directive in collector.Usings)
            {
                Console.WriteLine(directive.Name);
            }
        }
    }
}
```

16) Press **Ctrl+F5** to run the program without debugging it. You should see the following output:

```
Microsoft.CodeAnalysis
Microsoft.CodeAnalysis.CSharp
Microsoft
Microsoft.Win32
Microsoft.CSharp
Press any key to continue . . .
```

17) Observe that the walker has located all non-**System** namespace **using** directives in all four places.

18) Congratulations! You've just used the **Syntax API** to locate specific kinds of C# statements and declarations in C# source code.