' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertAnonymousTypeToTuple
    Partial Public Class ConvertAnonymousTypeToTupleTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicConvertAnonymousTypeToTupleDiagnosticAnalyzer(), New VisualBasicConvertAnonymousTypeToTupleCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function ConvertSingleAnonymousType() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function NotOnEmptyAnonymousType() As Task
            Await TestMissingInRegularAndScriptAsync("
class Test
    sub Method()
        dim t1 = [||]new with { }
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function NotOnSingleFieldAnonymousType() As Task
            Await TestMissingInRegularAndScriptAsync("
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1 }
    end sub
end class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function ConvertSingleAnonymousTypeWithInferredName() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { .a = 1, b }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = (a:=1, b)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function ConvertMultipleInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function ConvertMultipleInstancesAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function OnlyConvertMatchingTypesInSameMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, b }
        dim t3 = new with { .a = 4 }
        dim t4 = new with { .b = 5, .a = 6 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b)
        dim t3 = new with { .a = 4 }
        dim t4 = new with { .b = 5, .a = 6 }
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function TestFixAllInSingleMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = {|FixAllInDocument:|}new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, b }
        dim t3 = new with { .a = 4 }
        dim t4 = new with { .b = 5, .a = 6 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b)
        dim t3 = new with { .a = 4 }
        dim t4 = (b:=5, a:=6)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function TestFixAllAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = {|FixAllInDocument:|}new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function TestFixAllNestedTypes() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = {|FixAllInDocument:|}new with { .a = 1, .b = new with { .c = 1, .d = 2 } }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=(c:=1, d:=2))
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
            dim t1 = [||]new with { .a = 1, .b = directcast(new with { .a = 1, .b = directcast(nothing, object) }, object) }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
            dim t1 = (a:=1, b:=directcast((a:=1, b:=directcast(nothing, object)), object))
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function TestInLambda1() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim a as Action =
            sub()
                dim t2 = new with { .a = 3, .b = 4 }
            end sub
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim a as Action =
            sub()
                dim t2 = (a:=3, b:=4)
            end sub
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function TestInLambda2() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim t1 = new with { .a = 1, .b = 2 }
        dim a as Action =
            sub()
                dim t2 = [||]new with { .a = 3, .b = 4 }
            end sub
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim a as Action =
            sub()
                dim t2 = (a:=3, b:=4)
            end sub
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function TestIncomplete() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim t1 = [||]new with { .a = , .b = }
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim t1 = (a:= , b:= )
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function
    End Class
End Namespace
