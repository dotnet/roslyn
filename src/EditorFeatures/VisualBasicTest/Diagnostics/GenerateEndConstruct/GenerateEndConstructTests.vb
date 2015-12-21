' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEndConstruct
    Public Class GenerateEndConstructTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateEndConstructCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestIf() As Task
            Dim text = <MethodBody>
If True Then[||]
</MethodBody>

            Dim expected = <MethodBody>
If True Then

End If</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestUsing() As Task
            Dim text = <MethodBody>
Using (foo)[||]
</MethodBody>

            Dim expected = <MethodBody>
Using (foo)

End Using</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestStructure() As Task
            Dim text = <File>
Structure Foo[||]</File>

            Dim expected = StringFromLines("Structure Foo", "End Structure", "")

            Await TestAsync(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestModule() As Task
            Dim text = <File>
Module Foo[||]
</File>

            Dim expected = StringFromLines("Module Foo", "End Module", "")

            Await TestAsync(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestNamespace() As Task
            Dim text = <File>
Namespace Foo[||]
</File>

            Dim expected = StringFromLines("Namespace Foo", "End Namespace", "")

            Await TestAsync(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestClass() As Task
            Dim text = <File>
Class Foo[||]
</File>

            Dim expected = StringFromLines("Class Foo", "End Class", "")

            Await TestAsync(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestInterface() As Task
            Dim text = <File>
Interface Foo[||]
</File>

            Dim expected = StringFromLines("Interface Foo", "End Interface", "")

            Await TestAsync(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestEnum() As Task
            Dim text = <File>
Enum Foo[||]
</File>

            Dim expected = StringFromLines("Enum Foo", "End Enum", "")

            Await TestAsync(text.ConvertTestSourceTag(), expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestWhile() As Task
            Dim text = <MethodBody>
While True[||]</MethodBody>

            Dim expected = <MethodBody>
While True

End While</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestWith() As Task
            Dim text = <MethodBody>
With True[||]</MethodBody>

            Dim expected = <MethodBody>
With True

End With</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestSyncLock() As Task
            Dim text = <MethodBody>
SyncLock Me[||]</MethodBody>

            Dim expected = <MethodBody>
SyncLock Me

End SyncLock</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestDoLoop() As Task
            Dim text = <MethodBody>
Do While True[||]</MethodBody>

            Dim expected = <MethodBody>
Do While True

Loop</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestForNext() As Task
            Dim text = <MethodBody>
For x = 1 to 3[||]</MethodBody>

            Dim expected = <MethodBody>
For x = 1 to 3

Next</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestForEachNext() As Task
            Dim text = <MethodBody>
For Each x in {}[||]</MethodBody>

            Dim expected = <MethodBody>
For Each x in {}

Next</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestEndTry() As Task
            Dim text = <MethodBody>
Try[||]</MethodBody>

            Dim expected = <MethodBody>
Try

End Try</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestEndTryCatch() As Task
            Dim text = <MethodBody>
Try[||]
Catch</MethodBody>

            Dim expected = <MethodBody>
Try
Catch

End Try</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestEndTryCatchFinally() As Task
            Dim text = <MethodBody>
Try[||]
Catch
Finally</MethodBody>

            Dim expected = <MethodBody>
Try
Catch
Finally

End Try</MethodBody>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestProperty() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestReadOnlyProperty() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestWriteOnlyProperty() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestWriteOnlyPropertyFromSet() As Task
            Dim text = <File>
Class C
    WriteOnly Property P As Integer
        Set[||]
End Class</File>

            Dim expected = <File>
Class C
    WriteOnly Property P As Integer
        Set
        End Set
    End Property
End Class</File>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestInvInsideEndsEnum() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestMissingEndSub() As Task
            Dim text = <File>
Class C
    Sub Bar()[||]
End Class</File>

            Dim expected = <File>
Class C
    Sub Bar()

    End Sub
End Class</File>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestMissingEndFunction() As Task
            Dim text = <File>
Class C
    Function Bar() as Integer[||]
End Class</File>

            Dim expected = <File>
Class C
    Function Bar() as Integer

    End Function
End Class</File>

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <WorkItem(576176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestFormatWrappedBlock() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <WorkItem(578253)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestDoNotWrapCLass() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

        <WorkItem(578260)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestNotOnLambda() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n End Sub \n Function foo() \n Dim op = Sub[||](c) \n Dim kl = Sub(g) \n End Sub \n End Function \n End Module"))
        End Function

        <WorkItem(578271)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
        Public Async Function TestNamespaceThatEndsAtFile() As Task
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

            Await TestAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag(), compareTokens:=False)
        End Function

    End Class
End Namespace
