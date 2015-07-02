' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEndConstruct
    Public Class GenerateEndConstructTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateEndConstructCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestIf()
            Dim text = <MethodBody>
If True Then[||]
</MethodBody>

            Dim expected = <MethodBody>
If True Then

End If</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestUsing()
            Dim text = <MethodBody>
Using (foo)[||]
</MethodBody>

            Dim expected = <MethodBody>
Using (foo)

End Using</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestStructure()
            Dim text = <File>
Structure Foo[||]</File>

            Dim expected = StringFromLines("Structure Foo", "End Structure", "")

            Test(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestModule()
            Dim text = <File>
Module Foo[||]
</File>

            Dim expected = StringFromLines("Module Foo", "End Module", "")

            Test(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestNamespace()
            Dim text = <File>
Namespace Foo[||]
</File>

            Dim expected = StringFromLines("Namespace Foo", "End Namespace", "")

            Test(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestClass()
            Dim text = <File>
Class Foo[||]
</File>

            Dim expected = StringFromLines("Class Foo", "End Class", "")

            Test(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestInterface()
            Dim text = <File>
Interface Foo[||]
</File>

            Dim expected = StringFromLines("Interface Foo", "End Interface", "")

            Test(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestEnum()
            Dim text = <File>
Enum Foo[||]
</File>

            Dim expected = StringFromLines("Enum Foo", "End Enum", "")

            Test(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestWhile()
            Dim text = <MethodBody>
While True[||]</MethodBody>

            Dim expected = <MethodBody>
While True

End While</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestWith()
            Dim text = <MethodBody>
With True[||]</MethodBody>

            Dim expected = <MethodBody>
With True

End With</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestSyncLock()
            Dim text = <MethodBody>
SyncLock Me[||]</MethodBody>

            Dim expected = <MethodBody>
SyncLock Me

End SyncLock</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestDoLoop()
            Dim text = <MethodBody>
Do While True[||]</MethodBody>

            Dim expected = <MethodBody>
Do While True

Loop</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestForNext()
            Dim text = <MethodBody>
For x = 1 to 3[||]</MethodBody>

            Dim expected = <MethodBody>
For x = 1 to 3

Next</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestForEachNext()
            Dim text = <MethodBody>
For Each x in {}[||]</MethodBody>

            Dim expected = <MethodBody>
For Each x in {}

Next</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestEndTry()
            Dim text = <MethodBody>
Try[||]</MethodBody>

            Dim expected = <MethodBody>
Try

End Try</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestEndTryCatch()
            Dim text = <MethodBody>
Try[||]
Catch</MethodBody>

            Dim expected = <MethodBody>
Try
Catch

End Try</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestEndTryCatchFinally()
            Dim text = <MethodBody>
Try[||]
Catch
Finally</MethodBody>

            Dim expected = <MethodBody>
Try
Catch
Finally

End Try</MethodBody>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestProperty()
            Dim text = <File>
Class C
    Property P As Integer[||]
        Get
End Class</File>

            Dim expected = <File>
Class C
    Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestReadOnlyProperty()
            Dim text = <File>
Class C
    ReadOnly Property P As Integer[||]
        Get
End Class</File>

            Dim expected = <File>
Class C
    ReadOnly Property P As Integer
        Get
        End Get
    End Property
End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestWriteonlyProperty()
            Dim text = <File>
Class C
    WriteOnly Property P As Integer[||]
        Set
End Class</File>

            Dim expected = <File>
Class C
    WriteOnly Property P As Integer
        Set
        End Set
    End Property
End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub InvInsideEndsEnum()
            Dim text = <File>
Public Enum e[||]
    e1
Class Foo
End Class</File>

            Dim expected = <File>
Public Enum e
    e1
End Enum

Class Foo
End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub MissingEndSub()
            Dim text = <File>
Class C
    Sub Bar()[||]
End Class</File>

            Dim expected = <File>
Class C
    Sub Bar()

    End Sub
End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub MissingEndFunction()
            Dim text = <File>
Class C
    Function Bar() as Integer[||]
End Class</File>

            Dim expected = <File>
Class C
    Function Bar() as Integer

    End Function
End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <WorkItem(576176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub FormatWrappedBlock()
            Dim text = <File>
Class C
    Sub Main(args As String())
        While True[||]

        Dim x = 1
        Dim y = 2
        Dim z = 3
    End Sub

End Class</File>

            Dim expected = <File>
Class C
    Sub Main(args As String())
        While True

        End While

        Dim x = 1
        Dim y = 2
        Dim z = 3
    End Sub

End Class</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <WorkItem(578253)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub DoNotWrapCLass()
            Dim text = <File>
Class C[||]
        Function f1() As Integer
            Return 1
        End Function
 
    Module Program
        Sub Main(args As String())
 
        End Sub
    End Module</File>

            Dim expected = <File>
Class C

End Class
Function f1() As Integer
    Return 1
End Function

Module Program
    Sub Main(args As String())

    End Sub
End Module</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

        <WorkItem(578260)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub NotOnLambda()
            TestMissing(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n End Sub \n Function foo() \n Dim op = Sub[||](c) \n Dim kl = Sub(g) \n End Sub \n End Function \n End Module"))
        End Sub

        <WorkItem(578271)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Sub TestNamespaceThatEndsAtFile()
            Dim text = <File>
Namespace N[||]
    Interface I
        Module Program
        Sub Main(args As String())
        End Sub
        End Module
    End Interface</File>

            Dim expected = <File>
Namespace N

End Namespace
Interface I
    Module Program
    Sub Main(args As String())
    End Sub
    End Module
End Interface</File>

            Test(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Sub

    End Class
End Namespace
