' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay.VisualBasicAddDebuggerDisplayCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddDebuggerDisplay

    <Trait(Traits.Feature, Traits.Features.CodeActionsAddDebuggerDisplay)>
    Public NotInheritable Class AddDebuggerDisplayTests
        <Fact>
        Public Async Function OfferedOnEmptyClass() As Task
            Await VerifyVB.VerifyRefactoringAsync("
[||]Class C
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function OfferedOnEmptyStruct() As Task
            Await VerifyVB.VerifyRefactoringAsync("
[||]Structure Foo
End Structure", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Structure Foo
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Structure")
        End Function

        <Fact>
        Public Async Function NotOfferedOnModule() As Task
            Dim code = "
[||]Module Foo
End Module"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function NotOfferedOnInterfaceWithToString() As Task
            Dim code = "
[||]Interface IFoo
    Function ToString() As String
End Interface"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function NotOfferedOnEnum() As Task
            Dim code = "
[||]Enum Foo
    None
End Enum"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function NotOfferedOnDelegate() As Task
            Dim code = "
[||]Delegate Sub Foo()"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function NotOfferedOnUnrelatedClassMembers() As Task
            Dim code = "
Class C
    [||]Public ReadOnly Property Foo As Integer
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function OfferedOnToString() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Class C
    Public Overrides Function [||]ToString() As String
        Return ""Foo""
    End Function
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Public Overrides Function ToString() As String
        Return ""Foo""
    End Function

    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function OfferedOnShadowedToString() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Class C
    Public Shadows Function [||]ToString() As String
        Return ""Foo""
    End Function
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Public Shadows Function ToString() As String
        Return ""Foo""
    End Function

    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function NotOfferedOnWrongOverloadOfToString() As Task
            Dim code = "
Class A
    Public Overridable Function ToString(Optional bar As Integer = 0) As String
        Return ""Foo""
    End Function
End Class

Class B
    Inherits A

    Public Overrides Function [||]ToString(Optional bar As Integer = 0) As String
        Return ""Bar""
    End Function
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function OfferedOnExistingDebuggerDisplayMethod() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Class C
    Private Function [||]GetDebuggerDisplay() As String
        Return ""Foo""
    End Function
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ""Foo""
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function NotOfferedOnWrongOverloadOfDebuggerDisplayMethod() As Task
            Dim code = "
Class C
    Private Function [||]GetDebuggerDisplay(Optional bar As Integer = 0) As String
        Return ""Foo""
    End Function
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function NamespaceImportIsNotDuplicated() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Imports System.Diagnostics

[||]Class C
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function NamespaceImportIsSorted() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Imports System.Xml

[||]Class C
End Class", "
Imports System.Diagnostics
Imports System.Xml

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function NotOfferedWhenAlreadySpecified() As Task
            Dim code = "
<System.Diagnostics.DebuggerDisplay(""Foo"")>
[||]Class C
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function NotOfferedWhenAlreadySpecifiedWithSuffix() As Task
            Dim code = "
<System.Diagnostics.DebuggerDisplayAttribute(""Foo"")>
[||]Class C
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function OfferedWhenAttributeWithTheSameNameIsSpecified() As Task
            Await VerifyVB.VerifyRefactoringAsync("
<{|BC30002:BrokenCode.DebuggerDisplay|}(""Foo"")>
[||]Class C
End Class", "
Imports System.Diagnostics

<{|BC30002:BrokenCode.DebuggerDisplay|}(""Foo"")>
<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function OfferedWhenAttributeWithTheSameNameIsSpecifiedWithSuffix() As Task
            Await VerifyVB.VerifyRefactoringAsync("
<{|BC30002:BrokenCode.DebuggerDisplayAttribute|}(""Foo"")>
[||]Class C
End Class", "
Imports System.Diagnostics

<{|BC30002:BrokenCode.DebuggerDisplayAttribute|}(""Foo"")>
<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function AliasedTypeIsRecognized() As Task
            Dim code = "
Imports DD = System.Diagnostics.DebuggerDisplayAttribute

<DD(""Foo"")>
[||]Class C
End Class"

            Await VerifyVB.VerifyRefactoringAsync(code, code)
        End Function

        <Fact>
        Public Async Function OfferedWhenBaseClassHasDebuggerDisplay() As Task
            Await VerifyVB.VerifyRefactoringAsync("
Imports System.Diagnostics

<DebuggerDisplay(""Foo"")>
Class A
End Class

[||]Class B
    Inherits A
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""Foo"")>
Class A
End Class

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class B
    Inherits A

    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function ExistingDebuggerDisplayMethodIsUsedEvenWhenPublicSharedNonString() As Task
            Await VerifyVB.VerifyRefactoringAsync("
[||]Class C
    Public Shared Function GetDebuggerDisplay() As Object
        Return ""Foo""
    End Function
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Public Shared Function GetDebuggerDisplay() As Object
        Return ""Foo""
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function ExistingDebuggerDisplayMethodWithParameterIsNotUsed() As Task
            Await VerifyVB.VerifyRefactoringAsync("
[||]Class C
    Private Function GetDebuggerDisplay(Optional foo As Integer = 0) As String
        Return ""Foo""
    End Function
End Class", "
Imports System.Diagnostics

<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay(Optional foo As Integer = 0) As String
        Return ""Foo""
    End Function

    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function
    End Class
End Namespace
