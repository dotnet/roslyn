' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertToInterpolatedString
    Public Class ConvertConcatenationToInterpolatedStringTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingOnSimpleString() As Task
            Await TestMissingAsync(
"
Public Class C
    Sub M()
        dim v = [||]""string""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithStringOnLeft() As Task
            Await TestAsync(
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
            Await TestAsync(
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
            Await TestAsync(
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
            Await TestAsync(
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
            Await TestAsync(
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
End Class", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestWithComplexExpressions() As Task
            Await TestAsync(
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
            Await TestAsync(
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
            Await TestAsync(
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
            Await TestAsync(
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
            Await TestMissingAsync(
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
    End Class
End Namespace