' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class AttributeTests_Tuples
        Inherits BasicTestBase

        ReadOnly s_valueTupleRefs As MetadataReference() = New MetadataReference() {ValueTupleRef, SystemRuntimeFacadeRef}

        <Fact>
        Public Sub ExplicitTupleNamesAttribute()
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

<TupleElementNames({"a", "b"})>
Public Class C
    <TupleElementNames({Nothing, Nothing})>
    Public Field1 As ValueTuple(Of Integer, Integer)

    <TupleElementNames({"x", "y"})>
    Public ReadOnly Prop1 As ValueTuple(Of Integer, Integer)

    Public ReadOnly Property Prop2 As Integer
        <TupleElementNames({"x", "y"})>
        Get
            Return Nothing
        End Get
    End Property


    Public Function M(<TupleElementNames({Nothing})> x As ValueTuple) As <TupleElementNames({Nothing, Nothing})> ValueTuple(Of Integer, Integer)
        Return (0, 0)
    End Function

    Public Delegate Sub Delegate1(Of T)(sender As Object, <TupleElementNames({"x"})> args As ValueTuple(Of T))

    <TupleElementNames({"y"})>
    Public Custom Event Event1 As Delegate1(Of ValueTuple(Of Integer))
        AddHandler(value As Delegate1(Of ValueTuple(Of Integer)))

        End AddHandler
        RemoveHandler(value As Delegate1(Of ValueTuple(Of Integer)))

        End RemoveHandler
        RaiseEvent(sender As Object, args As ValueTuple(Of ValueTuple(Of Integer)))

        End RaiseEvent
    End Event

    <TupleElementNames({"a", "b"})>
    Default Public ReadOnly Property Item1(<TupleElementNames> t As (a As Integer, b As Integer)) As (a As Integer, b As Integer)
        Get
            Return t
        End Get
    End Property
End Class

<TupleElementNames({"a", "b"})>
Public Structure S
End Structure

]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
    <![CDATA[
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
<TupleElementNames({"a", "b"})>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    <TupleElementNames({Nothing, Nothing})>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    <TupleElementNames({"x", "y"})>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31445: Attribute 'TupleElementNamesAttribute' cannot be applied to 'Get' of 'Prop2' because the attribute is not valid on this declaration type.
        <TupleElementNames({"x", "y"})>
         ~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Public Function M(<TupleElementNames({Nothing})> x As ValueTuple) As <TupleElementNames({Nothing, Nothing})> ValueTuple(Of Integer, Integer)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Public Function M(<TupleElementNames({Nothing})> x As ValueTuple) As <TupleElementNames({Nothing, Nothing})> ValueTuple(Of Integer, Integer)
                                                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Public Delegate Sub Delegate1(Of T)(sender As Object, <TupleElementNames({"x"})> args As ValueTuple(Of T))
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'TupleElementNamesAttribute' cannot be applied to 'Event1' because the attribute is not valid on this declaration type.
    <TupleElementNames({"y"})>
     ~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    <TupleElementNames({"a", "b"})>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Default Public ReadOnly Property Item1(<TupleElementNames> t As (a As Integer, b As Integer)) As (a As Integer, b As Integer)
                                            ~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
<TupleElementNames({"a", "b"})>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    ]]>
</errors>)

        End Sub
    End Class
End Namespace
