' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InvertLogical

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertLogical
    Partial Public Class InvertLogicalTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertLogicalCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)>
        Public Async Function InvertConditional1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 [||]orelse b < 20
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 AndAlso b >= 20)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)>
        Public Async Function InvertConditional2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 [||]andalso b >= 20)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 OrElse b < 20
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 [||]andalso
                  b >= 20)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 OrElse
                  b < 20
    end sub
end class")
        End Function
    End Class
End Namespace
