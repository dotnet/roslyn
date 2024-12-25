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
            Await TestInRegularAndScript1Async(
"[|friend|] protected class C
end class
",
"protected friend class C
end class
")
        End Function

        <Fact>
        public async Function TestStruct() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"[|friend|] protected structure C

end structure",
"protected friend structure C

end structure")
        End Function

        <Fact>
        Public Async Function TestInterface() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"[|friend|] protected interface C
end interface",
"protected friend interface C
end interface")
        End Function

        <Fact>
        Public Async Function TestEnum() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"[|friend|] protected enum C
end enum",
"protected friend enum C
end enum")
        End Function

        <Fact>
        Public Async Function TestDelegate() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"[|friend|] protected delegate sub D()",
"protected friend delegate sub D()")
        End Function

        <Fact>
        Public Async Function TestMethodStatement() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"class C
    [|mustoverride|] protected sub M()
end class",
"class C
    protected mustoverride sub M()
end class")
        End Function

        <Fact>
        Public Async Function TestMethodBlock() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestField() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"class C
    [|friend|] protected dim a as integer
end class",
"class C
    protected friend dim a as integer
end class")
        End Function

        <Fact>
        Public Async Function TestConstructor() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestPropertyStatement() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"class C
    [|readonly|] protected property P as integer
end class",
"class C
    protected readonly property P as integer
end class")
        End Function

        <Fact>
        Public Async Function TestPropertyBlock() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestAccessor() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestPropertyEvent() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestFieldEvent() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
"class C
    [|friend|] protected event E as Action
end class",
"class C
    protected friend event E as Action
end class")
        End Function

        <Fact>
        Public Async Function TestOperator() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestConversionOperator() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestFixAll1() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestFixAll2() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
        Public Async Function TestTrivia1() As Threading.Tasks.Task
            Await TestInRegularAndScript1Async(
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
            Await TestInRegularAndScript1Async(
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
