' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.MoveType
    Partial Public Class MoveTypeTests
        Inherits BasicMoveTypeTestsBase

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function TestMissing_OnMatchingFileName() As Task
            Dim code =
"
[||]Class test1
End Class
"

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
        Public Async Function TestMissing_Nested_OnMatchingFileName_Simple() As Task
            Dim code =
"
Class Outer
    [||]Class test1
    End Class
End Class
"

            Await TestMissingAsync(code)
    End Function

    <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
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
"
Class Class1
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
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
Public Partial Class Class1
End Class
"
            Dim expectedDocumentName = "Class2.vb"

            Dim destinationDocumentText =
"
Public Partial Class Class1
    Class Class2
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
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
Public Partial Class Class1
End Class
"
            Dim expectedDocumentName = "Class1.Class2.vb"

            Dim destinationDocumentText =
"
Public Partial Class Class1
    Class Class2

    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText, index:=1)
        End Function

        <WorkItem(14484, "https://github.com/dotnet/roslyn/issues/14484")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
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
Public Partial Class Class1
End Class
"
            Dim expectedDocumentName = "Class1.Class2.vb"

            Dim destinationDocumentText =
"
Public Partial Class Class1
    ''' Inner comment
    Class Class2
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText,
                index:=1, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)>
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

Partial Class Outer
    Class Inner
        Sub M(d as DateTime)
        End Sub
    End Class
End Class
"
            Await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText)
        End Function
    End Class
End Namespace