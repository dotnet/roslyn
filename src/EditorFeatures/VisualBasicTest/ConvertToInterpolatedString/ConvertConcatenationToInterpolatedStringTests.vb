' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertToInterpolatedString
    Public Class ConvertConcatenationToInterpolatedStringTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingOnSimpleString() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = [||]""string""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithStringOnLeft() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = [||]""string"" & 1
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""string{1}""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestRightSideOfString() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""string""[||] & 1
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""string{1}""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithStringOnRight() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 1 & [||]""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1}string""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithComplexExpressionOnLeft() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 1 + 2 & [||]""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1 + 2}string""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithTrivia1() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 1 + 2 & [||]""string"" ' trailing trivia
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1 + 2}string"" ' trailing trivia
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithComplexExpressions() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 1 + 2 & [||]""string"" & 3 & 4
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1 + 2}string{3}{4}""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithEscapes1() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""\r"" & 2 & [||]""string"" & 3 & ""\n""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""\r{2}string{3}\n""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithEscapes2() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""\\r"" & 2 & [||]""string"" & 3 & ""\\n""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""\\r{2}string{3}\\n""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithOverloadedOperator() As Task
            Await TestInRegularAndScriptAsync(
"
public class D
    public shared operator&(D d, string s) as boolean
    end operator
    public shared operator&(string s, D d) as boolean
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = 1 & [||]""string"" & d
    End Sub
End Class",
"
public class D
    public shared operator&(D d, string s) as boolean
    end operator
    public shared operator&(string s, D d) as boolean
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = $""{1}string"" & d
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithOverloadedOperator2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
public class D
    public shared operator&(D d, string s) as boolean
    end operator
    public shared operator&(string s, D d) as boolean
    end operator
end class

Public Class C
    Sub M()
        dim d as D = nothing
        dim v = d & [||]""string"" & 1
    End Sub
End Class")
        End Function

        <WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithMultipleStringConcatinations() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""A"" & 1 & [||]""B"" & ""C""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""A{1}BC""
    End Sub
End Class")
        End Function


        <WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithMultipleStringConcatinations2() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""A"" & [||]""B"" & ""C"" & 1
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""ABC{1}""
    End Sub
End Class")
        End Function


        <WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithMultipleStringConcatinations3() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""A"" & 1 & [||]""B"" & ""C"" & 2 & ""D"" & ""E"" & ""F"" & 3  
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""A{1}BC{2}DEF{3}""
    End Sub
End Class")
        End Function

        <WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithStringLiteralWithBraces() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 1 & [||]""{string}""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1}{{string}}""
    End Sub
End Class")
        End Function

        <WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithStringLiteralWithDoubleBraces() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 1 & [||]""{{string}}""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{1}{{{{string}}}}""
    End Sub
End Class")
        End Function

        <WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithMultipleStringLiteralsWithBraces() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = ""{"" & 1 & [||]""}""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{{{1}}}""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingWithSelectionOnEntireToBeInterpolatedString() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = [|""string"" & 1|]
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingWithSelectionOnPartOfToBeInterpolatedString() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = [|""string"" & 1|] & ""string""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingWithSelectionExceedingToBeInterpolatedString() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        [|dim v = ""string"" & 1|]
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithCaretBeforeNonStringToken() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = [||]3 & ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithCaretAfterNonStringToken() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 3[||] & ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithCaretBeforeAmpersandToken() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 3 [||]& ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithCaretAfterAmpersandToken() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 3 &[||] ""string"" & 1 & ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithCaretBeforeLastAmpersandToken() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 3 & ""string"" & 1 [||]& ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithCaretAfterLastAmpersandToken() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        dim v = 3 & ""string"" & 1 &[||] ""string""
    End Sub
End Class",
"
Public Class C
    Sub M()
        dim v = $""{3}string{1}string""
    End Sub
End Class")
        End Function

        <WorkItem(37324, "https://github.com/dotnet/roslyn/issues/37324")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestConcatenationWithChar() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim world = ""world""
        Dim str = hello [||]& "" ""c & world
    End Sub
End Class",
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim world = ""world""
        Dim str = $""{hello} {world}""
    End Sub
End Class")
        End Function

        <WorkItem(37324, "https://github.com/dotnet/roslyn/issues/37324")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestConcatenationWithCharAfterStringLiteral() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Private Sub M()
        Dim world = ""world""
        Dim str = ""hello"" [||]& "" ""c & world
    End Sub
End Class",
"
Public Class C
    Private Sub M()
        Dim world = ""world""
        Dim str = $""hello {world}""
    End Sub
End Class")
        End Function

        <WorkItem(37324, "https://github.com/dotnet/roslyn/issues/37324")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestConcatenationWithCharBeforeStringLiteral() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim str = hello [||]& "" ""c & ""world""
    End Sub
End Class",
"
Public Class C
    Private Sub M()
        Dim hello = ""hello""
        Dim str = $""{hello} world""
    End Sub
End Class")
        End Function
    End Class
End Namespace
