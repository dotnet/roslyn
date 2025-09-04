' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEndConstruct
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEndConstruct)>
    Public Class GenerateEndConstructTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New GenerateEndConstructCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestIf() As Task
            Dim text = <MethodBody>
If True Then[||]
</MethodBody>

            Dim expected = <MethodBody>
If True Then

End If</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestUsing() As Task
            Dim text = <MethodBody>
Using (goo)[||]
</MethodBody>

            Dim expected = <MethodBody>
Using (goo)

End Using</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestStructure() As Task
            Dim text = <File>
Structure Goo[||]</File>

            Dim expected = StringFromLines("", "Structure Goo", "End Structure", "")

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected)
        End Function

        <Fact>
        Public Async Function TestModule() As Task
            Dim text = <File>
Module Goo[||]
</File>

            Dim expected = StringFromLines("", "Module Goo", "", "End Module", "")

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected)
        End Function

        <Fact>
        Public Async Function TestNamespace() As Task
            Dim text = <File>
Namespace Goo[||]
</File>

            Dim expected = StringFromLines("", "Namespace Goo", "", "End Namespace", "")

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected)
        End Function

        <Fact>
        Public Async Function TestClass() As Task
            Dim text = <File>
Class Goo[||]
</File>

            Dim expected = StringFromLines("", "Class Goo", "", "End Class", "")

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected)
        End Function

        <Fact>
        Public Async Function TestInterface() As Task
            Dim text = <File>
Interface Goo[||]
</File>

            Dim expected = StringFromLines("", "Interface Goo", "", "End Interface", "")

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected)
        End Function

        <Fact>
        Public Async Function TestEnum() As Task
            Dim text = <File>
Enum Goo[||]
</File>

            Dim expected = StringFromLines("", "Enum Goo", "", "End Enum", "")

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected)
        End Function

        <Fact>
        Public Async Function TestWhile() As Task
            Dim text = <MethodBody>
While True[||]</MethodBody>

            Dim expected = <MethodBody>
While True

End While</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestWith() As Task
            Dim text = <MethodBody>
With True[||]</MethodBody>

            Dim expected = <MethodBody>
With True

End With</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestSyncLock() As Task
            Dim text = <MethodBody>
SyncLock Me[||]</MethodBody>

            Dim expected = <MethodBody>
SyncLock Me

End SyncLock</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestDoLoop() As Task
            Dim text = <MethodBody>
Do While True[||]</MethodBody>

            Dim expected = <MethodBody>
Do While True

Loop</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestForNext() As Task
            Dim text = <MethodBody>
For x = 1 to 3[||]</MethodBody>

            Dim expected = <MethodBody>
For x = 1 to 3

Next</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestForEachNext() As Task
            Dim text = <MethodBody>
For Each x in {}[||]</MethodBody>

            Dim expected = <MethodBody>
For Each x in {}

Next</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestEndTry() As Task
            Dim text = <MethodBody>
Try[||]</MethodBody>

            Dim expected = <MethodBody>
Try

End Try</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestEndTryCatch() As Task
            Dim text = <MethodBody>
Try[||]
Catch</MethodBody>

            Dim expected = <MethodBody>
Try
Catch

End Try</MethodBody>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
        Public Async Function TestInvInsideEndsEnum() As Task
            Dim text = <File>
Public Enum e[||]
    e1
Class Goo
End Class</File>

            Dim expected = <File>
Public Enum e
    e1
End Enum

Class Goo
End Class</File>

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576176")>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578253")>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578260")>
        Public Async Function TestNotOnLambda() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
    End Sub
    Function goo()
        Dim op = Sub[||](c)
                     Dim kl = Sub(g)
                              End Sub 
 End Function
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578271")>
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

            Await TestInRegularAndScriptAsync(text.ConvertTestSourceTag(), expected.ConvertTestSourceTag())
        End Function
    End Class
End Namespace
