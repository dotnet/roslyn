' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InvertConditional

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertConditional
    Partial Public Class InvertConditionalTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertConditionalCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)>
        Public Async Function InvertConditional1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = [||]if(x, a, b)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = if(Not x, b, a)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)>
        Public Async Function InvertConditional2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = [||]if(not x, a, b)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = if(x, b, a)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = [||]if(x,
            a,
            b)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = if(Not x,
            b,
            a)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)>
        Public Async Function MissingOnBinaryIf() As Task
            Await TestMissingAsync(
"class C
    sub M(x as integer?, a as integer)
        dim c = [||]if(x, a)
    end sub
end class")
        End Function
    End Class
End Namespace
