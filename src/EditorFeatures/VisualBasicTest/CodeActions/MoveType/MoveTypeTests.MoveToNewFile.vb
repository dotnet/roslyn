' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    <Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
    Partial Public Class MoveTypeTests
        Inherits BasicMoveTypeTestsBase

        <WpfFact>
        Public Async Function TestMissing_OnMatchingFileName() As Task
            Dim code =
"
[||]Class test1
End Class
"

            Await TestMissingInRegularAndScriptAsync(code)
        End Function

        <WpfFact>
        Public Async Function TestMissing_Nested_OnMatchingFileName_Simple() As Task
            Dim code =
"
Class Outer
    [||]Class test1
    End Class
End Class
"

            Await TestMissingInRegularAndScriptAsync(code)
        End Function

        <WpfFact>
        Public Async Function MultipleTypesInFileWithNoContainerNamespace() As Task
            Dim code =
"
[||]Class Class1
End Class

Class Class2
End Class
"
            Dim codeAfterMove =
"
Class Class2
End Class
"
            Dim expectedDocumentName = "Class1.vb"

            Dim destinationDocumentText =
"Class Class1
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WpfFact>
        Public Async Function MoveNestedTypeToNewFile_Simple() As Task
            Dim code =
"
Public Class Class1
    Class Class2[||]
    End Class
End Class
"
            Dim codeAfterMove =
"
Partial Public Class Class1
End Class
"
            Dim expectedDocumentName = "Class2.vb"

            Dim destinationDocumentText =
"
Partial Public Class Class1
    Class Class2
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WpfFact>
        Public Async Function MoveNestedTypeToNewFile_Simple_DottedName() As Task
            Dim code =
"
Public Class Class1
    Class Class2[||]
    End Class
End Class
"
            Dim codeAfterMove =
"
Partial Public Class Class1
End Class
"
            Dim expectedDocumentName = "Class1.Class2.vb"

            Dim destinationDocumentText =
"
Partial Public Class Class1
    Class Class2
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText, index:=1)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/14484")>
        <WpfFact>
        Public Async Function MoveNestedTypeToNewFile_RemoveComments() As Task
            Dim code =
"
''' Outer comment
Public Class Class1
    ''' Inner comment
    Class Class2[||]
    End Class
End Class
"
            Dim codeAfterMove =
"
''' Outer comment
Partial Public Class Class1
End Class
"
            Dim expectedDocumentName = "Class1.Class2.vb"

            Dim destinationDocumentText =
"
Partial Public Class Class1
    ''' Inner comment
    Class Class2
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText,
                index:=1)
        End Function

        <WpfFact>
        Public Async Function TestImports() As Task
            Dim code =
"
' Used only by inner
Imports System

' Not used
Imports System.Collections

Class Outer
    [||]Class Inner
        Sub M(d as DateTime)
        End Sub
    End Class
End Class
"
            Dim codeAfterMove =
"
' Used only by inner

' Not used
Imports System.Collections

Partial Class Outer
End Class
"
            Dim expectedDocumentName = "Inner.vb"

            Dim destinationDocumentText =
"
' Used only by inner
Imports System

' Not used

Partial Class Outer
    Class Inner
        Sub M(d as DateTime)
        End Sub
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/16282")>
        <WpfFact>
        Public Async Function TestTypeInheritance() As Task
            Dim code =
"
Class Outer
    Inherits Something
    Implements ISomething

    [||]Class Inner
        Inherits Other
        Implements IOther

        Sub M(d as DateTime)
        End Sub
    End Class
End Class
"
            Dim codeAfterMove =
"
Partial Class Outer
    Inherits Something
    Implements ISomething
End Class
"
            Dim expectedDocumentName = "Inner.vb"

            Dim destinationDocumentText =
"
Partial Class Outer
    Class Inner
        Inherits Other
        Implements IOther

        Sub M(d as DateTime)
        End Sub
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/21456")>
        <WpfFact>
        Public Async Function TestLeadingBlankLines1() As Task
            Dim code =
"' Banner Text
imports System

[||]class Class1
    sub Foo()
        Console.WriteLine()
    end sub
end class

class Class2
    sub Foo()
        Console.WriteLine()
    end sub
end class
"
            Dim codeAfterMove = "' Banner Text
imports System

class Class2
    sub Foo()
        Console.WriteLine()
    end sub
end class
"

            Dim expectedDocumentName = "Class1.vb"
            Dim destinationDocumentText = "' Banner Text
imports System

class Class1
    sub Foo()
        Console.WriteLine()
    end sub
end class
"

            Await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/21456")>
        <WpfFact>
        Public Async Function TestLeadingBlankLines2() As Task
            Dim code =
"' Banner Text
imports System

class Class1
    sub Foo()
        Console.WriteLine()
    end sub
end class

[||]class Class2
    sub Foo()
        Console.WriteLine()
    end sub
end class
"
            Dim codeAfterMove = "' Banner Text
imports System

class Class1
    sub Foo()
        Console.WriteLine()
    end sub
end class
"

            Dim expectedDocumentName = "Class2.vb"
            Dim destinationDocumentText = "' Banner Text
imports System

class Class2
    sub Foo()
        Console.WriteLine()
    end sub
end class
"

            Await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function
    End Class
End Namespace
