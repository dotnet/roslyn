' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.OrderModifiers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.OrderModifiers
    <Trait(Traits.Feature, Traits.Features.CodeActionsOrderModifiers)>
    Public Class OrderModifiersTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicOrderModifiersDiagnosticAnalyzer(),
                    New VisualBasicOrderModifiersCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestClass() As Task
            Await TestInRegularAndScriptAsync(
"[|friend|] protected class C
end class
",
"protected friend class C
end class
")
        End Function

        <Fact>
        Public Async Function TestStruct() As Task
            Await TestInRegularAndScriptAsync(
"[|friend|] protected structure C

end structure",
"protected friend structure C

end structure")
        End Function

        <Fact>
        Public Async Function TestInterface() As Task
            Await TestInRegularAndScriptAsync(
"[|friend|] protected interface C
end interface",
"protected friend interface C
end interface")
        End Function

        <Fact>
        Public Async Function TestEnum() As Task
            Await TestInRegularAndScriptAsync(
"[|friend|] protected enum C
end enum",
"protected friend enum C
end enum")
        End Function

        <Fact>
        Public Async Function TestDelegate() As Task
            Await TestInRegularAndScriptAsync(
"[|friend|] protected delegate sub D()",
"protected friend delegate sub D()")
        End Function

        <Fact>
        Public Async Function TestMethodStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|mustoverride|] protected sub M()
end class",
"class C
    protected mustoverride sub M()
end class")
        End Function

        <Fact>
        Public Async Function TestMethodBlock() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|friend|] protected sub M()
    end sub
end class",
"class C
    protected friend sub M()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestField() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|friend|] protected dim a as integer
end class",
"class C
    protected friend dim a as integer
end class")
        End Function

        <Fact>
        Public Async Function TestConstructor() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|friend|] protected sub new()
    end sub
end class",
"class C
    protected friend sub new()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestPropertyStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|readonly|] protected property P as integer
end class",
"class C
    protected readonly property P as integer
end class")
        End Function

        <Fact>
        Public Async Function TestPropertyBlock() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|readonly|] protected property P as integer
        get
        end get
    end property
end class",
"class C
    protected readonly property P as integer
        get
        end get
    end property
end class")
        End Function

        <Fact>
        Public Async Function TestAccessor() As Task
            Await TestInRegularAndScriptAsync(
"class C
    public property P as integer
        [|friend|] protected get
        end get
    end property
end class
",
"class C
    public property P as integer
        protected friend get
        end get
    end property
end class
")
        End Function

        <Fact>
        Public Async Function TestPropertyEvent() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|friend|] protected custom event E as Action 
    end event
end class",
"class C
    protected friend custom event E as Action 
    end event
end class")
        End Function

        <Fact>
        Public Async Function TestFieldEvent() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|friend|] protected event E as Action
end class",
"class C
    protected friend event E as Action
end class")
        End Function

        <Fact>
        Public Async Function TestOperator() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|shared|] public operator +(c1 as integer, c2 as integer) as integer
    end operator
end class
",
"class C
    public shared operator +(c1 as integer, c2 as integer) as integer
    end operator
end class
")
        End Function

        <Fact>
        Public Async Function TestConversionOperator() As Task
            Await TestInRegularAndScriptAsync(
"class C
    [|shared|] public widening operator CType(x as integer) as boolean
    end operator
end class",
"class C
    public shared widening operator CType(x as integer) as boolean
    end operator
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"{|FixAllInDocument:friend|} protected class C
    friend protected class Nested
    end class
end class",
"protected friend class C
    protected friend class Nested
    end class
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScriptAsync(
"friend protected class C
    {|FixAllInDocument:friend|} protected class Nested
    end class
end class
",
"protected friend class C
    protected friend class Nested
    end class
end class
")
        End Function

        <Fact>
        Public Async Function TestTrivia1() As Task
            Await TestInRegularAndScriptAsync(
"
''' Doc comment
[|friend|] protected class C
end class
",
"
''' Doc comment
protected friend class C
end class
")
        End Function

        <Fact>
        Public Async Function TestTrivia3() As Task
            Await TestInRegularAndScriptAsync(
"
#if true
[|friend|] protected class C
end class
#end if
",
"
#if true
protected friend class C
end class
#end if
")
        End Function
    End Class
End Namespace
