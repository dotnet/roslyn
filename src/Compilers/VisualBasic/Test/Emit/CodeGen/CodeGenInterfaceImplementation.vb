' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenInterfaceImplementationTests
        Inherits BasicTestBase

        <WorkItem(540794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540794")>
        <Fact>
        Public Sub TestInterfaceMembersSignature()
            Dim source =
<compilation>
    <file name="a.vb">
Interface IFace
    Sub SubRoutine()
    Function Func() As Integer
    Property Prop As Integer
End Interface
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedSignatures:=
            {
                Signature("IFace", "SubRoutine", ".method public newslot strict abstract virtual instance System.Void SubRoutine() cil managed"),
                Signature("IFace", "Func", ".method public newslot strict abstract virtual instance System.Int32 Func() cil managed"),
                Signature("IFace", "get_Prop", ".method public newslot strict specialname abstract virtual instance System.Int32 get_Prop() cil managed"),
                Signature("IFace", "set_Prop", ".method public newslot strict specialname abstract virtual instance System.Void set_Prop(System.Int32 Value) cil managed"),
                Signature("IFace", "Prop", ".property readwrite instance System.Int32 Prop")
            })

            verifier.VerifyDiagnostics()
        End Sub

        <WorkItem(540794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540794")>
        <WorkItem(540805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540805")>
        <WorkItem(540861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540861")>
        <WorkItem(540807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540807")>
        <Fact>
        Public Sub TestNestedInterface()
            Dim source =
<compilation>
    <file name="a.vb">
Class Class2
    Implements Class2.InterfaceDerived

    Interface InterfaceDerived
        Inherits Class2.InterfaceBase
        Sub AbcDef()
    End Interface
    Public Interface InterfaceBase
        Property Bar As Integer
    End Interface

    Public Property Bar As Integer Implements InterfaceDerived.Bar
        Get
            Return 1
        End Get
        Set(value As Integer)

        End Set
    End Property

    Public Sub AbcDef() Implements InterfaceDerived.AbcDef

    End Sub
End Class
Class Class1
    Implements Class1.Interface1

    Public Interface Interface1
        Sub Foo()
    End Interface

    Public Sub Foo() Implements Interface1.Foo

    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedSignatures:=
            {
                Signature("Class2+InterfaceBase", "get_Bar", ".method public newslot strict specialname abstract virtual instance System.Int32 get_Bar() cil managed"),
                Signature("Class2+InterfaceBase", "set_Bar", ".method public newslot strict specialname abstract virtual instance System.Void set_Bar(System.Int32 Value) cil managed"),
                Signature("Class2+InterfaceBase", "Bar", ".property readwrite instance System.Int32 Bar"),
                Signature("Class2+InterfaceDerived", "AbcDef", ".method public newslot strict abstract virtual instance System.Void AbcDef() cil managed"),
                Signature("Class2", "get_Bar", ".method public newslot strict specialname virtual final instance System.Int32 get_Bar() cil managed"),
                Signature("Class2", "set_Bar", ".method public newslot strict specialname virtual final instance System.Void set_Bar(System.Int32 value) cil managed"),
                Signature("Class2", "AbcDef", ".method public newslot strict virtual final instance System.Void AbcDef() cil managed"),
                Signature("Class2", "Bar", ".property readwrite instance System.Int32 Bar"),
                Signature("Class1+Interface1", "Foo", ".method public newslot strict abstract virtual instance System.Void Foo() cil managed"),
                Signature("Class1", "Foo", ".method public newslot strict virtual final instance System.Void Foo() cil managed")
            })

            verifier.VerifyDiagnostics()
        End Sub

        <WorkItem(543426, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543426")>
        <Fact()>
        Public Sub TestExplicitlyImplementInterfaceNestedInGenericType()
            Dim source =
<compilation>
    <file name="a.vb">
Class Outer(Of T)
    Interface IInner
        Sub M(t As T)
    End Interface
    Class Inner
        Implements IInner

        Private Sub M(t As T) Implements Outer(Of T).IInner.M
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedSignatures:=
            {
                Signature("Outer`1+IInner", "M", ".method public newslot strict abstract virtual instance System.Void M(T t) cil managed"),
                Signature("Outer`1+Inner", "M", ".method private newslot strict virtual final instance System.Void M(T t) cil managed")
            })

            verifier.VerifyDiagnostics()
        End Sub

    End Class
End Namespace

