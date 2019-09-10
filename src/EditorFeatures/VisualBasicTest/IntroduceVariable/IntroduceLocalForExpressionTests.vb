' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.IntroduceVariable
    Partial Public Class IntroduceLocalForExpressionTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicIntroduceLocalForExpressionCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function IntroduceLocal() As Task
            Await TestInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        N()[||]
    end sub

    function N() as string
    end function
end class",
"
imports System

class C
    sub M()
        Dim {|Rename:v|} = N()
    end sub

    function N() as string
    end function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function IntroduceLocal_TrailingBlankLine() As Task
            Await TestInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        N()[||]

    end sub

    function N() as string
    end function
end class",
"
imports System

class C
    sub M()
        Dim {|Rename:v|} = N()

    end sub

    function N() as string
    end function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function IntroduceLocal_Selection() As Task
            Await TestInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        [|N()|]
    end sub

    function N() as string
    end function
end class",
"
imports System

class C
    sub M()
        Dim {|Rename:v|} = N()
    end sub

    function N() as string
    end function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function IntroduceLocal_Space() As Task
            Await TestInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        N() [||]
    end sub

    function N() as string
    end function
end class",
"
imports System

class C
    sub M()
        Dim {|Rename:v|} = N() 
    end sub

    function N() as string
    end function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function IntroduceLocal_LeadingTrivia() As Task
            Await TestInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        ' Comment
        N()[||]
    end sub

    function N() as string
    end function
end class",
"
imports System

class C
    sub M()
        ' Comment
        Dim {|Rename:v|} = N()
    end sub

    function N() as string
    end function
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function MissingOnVoidCall() As Task
            Await TestMissingInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        Console.WriteLine()[||]
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceLocalForExpression)>
        Public Async Function MissingOnDeclaration() As Task
            Await TestMissingInRegularAndScriptAsync(
"
imports System

class C
    sub M()
        dim x = N()[||]
    end sub

    function N() as string
    end function
end class")
        End Function
    End Class
End Namespace
