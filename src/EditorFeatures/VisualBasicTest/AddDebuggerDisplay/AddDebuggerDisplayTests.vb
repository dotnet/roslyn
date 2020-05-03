' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddDebuggerDisplay

    <Trait(Traits.Feature, Traits.Features.CodeActionsAddDebuggerDisplay)>
    Public NotInheritable Class AddDebuggerDisplayTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicAddDebuggerDisplayCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function OfferedOnEmptyClass() As Task
            Await TestInRegularAndScriptAsync("
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
            Await TestInRegularAndScriptAsync("
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
        Public Async Function NotOfferedOnInterfaceWithToString() As Task
            Await TestMissingInRegularAndScriptAsync("
[||]Interface IFoo
    Function ToString() As String
End Interface")
        End Function

        <Fact>
        Public Async Function NotOfferedOnEnum() As Task
            Await TestMissingInRegularAndScriptAsync("
[||]Enum Foo
End Enum")
        End Function

        <Fact>
        Public Async Function NotOfferedOnDelegate() As Task
            Await TestMissingInRegularAndScriptAsync("
[||]Delegate Sub Foo()")
        End Function

        <Fact>
        Public Async Function NotOfferedOnUnrelatedClassMembers() As Task
            Await TestMissingInRegularAndScriptAsync("
Class C
    [||]Public ReadOnly Property Foo As Integer
End Class")
        End Function

        <Fact>
        Public Async Function OfferedOnToString() As Task
            Await TestInRegularAndScriptAsync("
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
            Await TestInRegularAndScriptAsync("
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
            Await TestMissingInRegularAndScriptAsync("
Class A
    Public Overrides Function ToString(Optional bar As Integer = 0) As String
        Return ""Foo""
    End Function
End Class

Class B
    Inherits A

    Public Overrides Function [||]ToString(Optional bar As Integer = 0) As String
        Return ""Bar""
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function OfferedOnExistingDebuggerDisplayMethod() As Task
            Await TestInRegularAndScriptAsync("
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
            Await TestMissingInRegularAndScriptAsync("
Class C
    Private Function [||]GetDebuggerDisplay(Optional bar As Integer = 0) As String
        Return ""Foo""
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function NamespaceImportIsNotDuplicated() As Task
            Await TestInRegularAndScriptAsync("
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
            Await TestInRegularAndScriptAsync("
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
            Await TestMissingInRegularAndScriptAsync("
<System.Diagnostics.DebuggerDisplay(""Foo"")>
[||]Class C
End Class")
        End Function

        <Fact>
        Public Async Function NotOfferedWhenAlreadySpecifiedWithSuffix() As Task
            Await TestMissingInRegularAndScriptAsync("
<System.Diagnostics.DebuggerDisplayAttribute(""Foo"")>
[||]Class C
End Class")
        End Function

        <Fact>
        Public Async Function OfferedWhenAttributeWithTheSameNameIsSpecified() As Task
            Await TestInRegularAndScriptAsync("
<BrokenCode.DebuggerDisplay(""Foo"")>
[||]Class C
End Class", "
Imports System.Diagnostics

<BrokenCode.DebuggerDisplay(""Foo"")>
<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function OfferedWhenAttributeWithTheSameNameIsSpecifiedWithSuffix() As Task
            Await TestInRegularAndScriptAsync("
<BrokenCode.DebuggerDisplayAttribute(""Foo"")>
[||]Class C
End Class", "
Imports System.Diagnostics

<BrokenCode.DebuggerDisplayAttribute(""Foo"")>
<DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")>
Class C
    Private Function GetDebuggerDisplay() As String
        Return ToString()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function AliasedTypeIsRecognized() As Task
            Await TestMissingInRegularAndScriptAsync("
Imports DD = System.Diagnostics.DebuggerDisplayAttribute

<DD(""Foo"")>
[||]Class C
End Class")
        End Function

        <Fact>
        Public Async Function OfferedWhenBaseClassHasDebuggerDisplay() As Task
            Await TestInRegularAndScriptAsync("
Imports System.Diagnostics

[DebuggerDisplay(""Foo"")]
Class A
End Class

[||]Class B
    Inherits A
End Class", "
Imports System.Diagnostics

[DebuggerDisplay(""Foo"")]
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
            Await TestInRegularAndScriptAsync("
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
            Await TestInRegularAndScriptAsync("
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
