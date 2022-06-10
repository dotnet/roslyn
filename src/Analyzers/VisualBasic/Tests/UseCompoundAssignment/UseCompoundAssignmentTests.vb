' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseCompoundAssignment
    Public Class UseCompoundAssignmentTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(Workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseCompoundAssignmentDiagnosticAnalyzer(), New VisualBasicUseCompoundAssignmentCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestAddExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a + 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestSubtractExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a - 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a -= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestMultiplyExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a * 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a *= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestDivideExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a / 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a /= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestConcatenateExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as string)
        a [||]= a & 10
    end sub
end class",
"public class C
    sub M(a as string)
        a &= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestExponentiationExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a ^ 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a ^= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestLeftShiftExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a << 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a <<= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestRightShiftExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a >> 10
    end sub
end class",
"public class C
    sub M(a as integer)
        a >>= 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestField() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    private a as integer

    sub M()
        a [||]= a + 10
    end sub
end class",
"public class C
    private a as integer

    sub M()
        a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestFieldWithThis() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    private a as integer

    sub M()
        me.a [||]= me.a + 10
    end sub
end class",
"public class C
    private a as integer

    sub M()
        me.a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestStaticFieldThroughType() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    shared private a as integer

    sub M()
        C.a [||]= C.a + 10
    end sub
end class",
"public class C
    shared private a as integer

    sub M()
        C.a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestStaticFieldThroughNamespaceAndType() As Task
            Await TestInRegularAndScriptAsync(
"namespace NS
    public class C
        shared private a as integer

        sub M()
            NS.C.a [||]= NS.C.a + 10
        end sub
    end class
end namespace",
"namespace NS
    public class C
        shared private a as integer

        sub M()
            NS.C.a += 10
        end sub
    end class
end namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestThroughBase() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    public a as integer
end class

public class D
    inherits C
    sub M()
        mybase.a [||]= mybase.a + 10
    end sub
end class",
"public class C
    public a as integer
end class

public class D
    inherits C
    sub M()
        mybase.a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestMultiAccess() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    public a as integer
end class

public class D
    private c as C

    sub M()
        me.c.a [||]= me.c.a + 10
    end sub
end class",
"public class C
    public a as integer
end class

public class D
    private c as C

    sub M()
        me.c.a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestOnTopLevelProp() As Task
            Await TestInRegularAndScript1Async(
"public class C
    public property a as integer
    end property

    sub M()
        a [||]= a + 10
    end sub
end class",
"public class C
    public property a as integer
    end property

    sub M()
        a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestNotOnNestedProp1() As Task
            Await TestMissingAsync(
"
public class A
    public x as integer
end class
public class C
    public property a as A
    end property

    sub M()
        a.x [||]= a.x + 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestNotOnUnboundSymbol() As Task
            Await TestMissingAsync(
"public class C
    sub M()
        a [||]= a + 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestNotWithSideEffects() As Task
            Await TestMissingAsync(
"public class C
    private i as integer

    function Goo() as C
        return me
    end function

    sub M()
        me.Goo().i [||]= me.Goo().i + 10
    end sub
end class")
        End Function

        <WorkItem(35870, "https://github.com/dotnet/roslyn/issues/35870")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestRightExpressionOnNextLine() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= a +
            10
    end sub
end class",
"public class C
    sub M(a as integer)
        a += 10
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        // before
        a [||]= a + 10 // after
    end sub
end class",
"public class C
    sub M(a as integer)
        // before
        a += 10 // after
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestFixAll() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer, b as integer)
        a {|FixAllInDocument:|}= a + 10
        b = b - a
    end sub
end class",
"public class C
    sub M(a as integer, b as integer)
        a += 10
        b -= a
    end sub
end class")
        End Function

        <WorkItem(38137, "https://github.com/dotnet/roslyn/issues/38137")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestParenthesizedExpression() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= (a + 10)
    end sub
end class",
"public class C
    sub M(a as integer)
        a += 10
    end sub
end class")
        End Function

        <WorkItem(38137, "https://github.com/dotnet/roslyn/issues/38137")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)>
        Public Async Function TestParenthesizedExpressionTrailingTrivia() As Task
            Await TestInRegularAndScriptAsync(
"public class C
    sub M(a as integer)
        a [||]= (a + 10) ' trailing
    end sub
end class",
"public class C
    sub M(a as integer)
        a += 10 ' trailing
    end sub
end class")
        End Function
    End Class
End Namespace
