' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ConvertIfToSwitch
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)>
    Public Class ConvertIfToSwitchFixAllTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertIfToSwitchCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function ConvertIfToSwitchStatement_FixAllInDocument() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        {|FixAllInDocument:|}If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub

    Sub M2(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub

    Sub M2(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ConvertIfToSwitchStatement_FixAllInProject() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
Class C
    Sub M(i As Integer)
        {|FixAllInProject:|}If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
        <Document>
Class C2
    Sub M2(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Class C3
    Sub M3(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
    </Project>
</Workspace>",
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class
        </Document>
        <Document>
Class C2
    Sub M2(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Class C3
    Sub M3(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact>
        Public Async Function ConvertIfToSwitchStatement_FixAllInSolution() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
Class C
    Sub M(i As Integer)
        {|FixAllInSolution:|}If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
        <Document>
Class C2
    Sub M2(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Class C3
    Sub M3(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class
        </Document>
    </Project>
</Workspace>",
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class
        </Document>
        <Document>
Class C2
    Sub M2(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
Class C3
    Sub M3(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact>
        Public Async Function ConvertIfToSwitchStatement_FixAllInContainingMember() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        {|FixAllInContainingMember:|}If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub

    Sub M2(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub

    Sub M2(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ConvertIfToSwitchStatement_FixAllInContainingType() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        {|FixAllInContainingType:|}If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub

    Sub M2(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class

Class C2
    Sub M3(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub

    Sub M2(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class

Class C2
    Sub M3(i As Integer)
        If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class")
        End Function
    End Class
End Namespace
