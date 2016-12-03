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
Imports System.Reflection.Metadata

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

        <Fact>
        <WorkItem(14844, "https://github.com/dotnet/roslyn/issues/14844")>
        Public Sub AttributesOnTypeConstraints()
            Dim src = <compilation>
                          <file src="a.vb">
                              <![CDATA[
Public Interface I1(Of T)
End Interface

Public Interface I2(Of T As I1(Of (a as Integer, b as Integer)))
End Interface
Public Interface I3(Of T As I1(Of (c as Integer, d as Integer)))
End Interface
]]>
                          </file>
                      </compilation>

            Dim validator =
            Sub(assembly As PEAssembly)
                Dim reader = assembly.ManifestModule.MetadataReader

                Dim verifyTupleConstraint =
                Sub(def As TypeDefinition, tupleNames As String())
                    Dim typeParams = def.GetGenericParameters()
                    Assert.Equal(1, typeParams.Count)
                    Dim typeParam = reader.GetGenericParameter(typeParams(0))
                    Dim constraintHandles = typeParam.GetConstraints()
                    Assert.Equal(1, constraintHandles.Count)
                    Dim constraint = reader.GetGenericParameterConstraint(constraintHandles(0))

                    Dim Attributes = constraint.GetCustomAttributes()
                    Assert.Equal(1, Attributes.Count)
                    Dim attr = reader.GetCustomAttribute(Attributes.Single())

                    ' Verify that the attribute contains an array of matching tuple names
                    Dim argsReader = reader.GetBlobReader(attr.Value)
                    ' Prolog
                    Assert.Equal(1, argsReader.ReadUInt16())
                    ' Array size
                    Assert.Equal(tupleNames.Length, argsReader.ReadInt32())

                    For Each name In tupleNames
                        Assert.Equal(name, argsReader.ReadSerializedString())
                    Next
                End Sub

                For Each typeHandle In reader.TypeDefinitions
                    Dim def = reader.GetTypeDefinition(typeHandle)
                    Dim name = reader.GetString(def.Name)
                    Select Case name
                        Case "I1`1"
                        Case "<Module>"
                            Continue For

                        Case "I2`1"
                            verifyTupleConstraint(def, {"a", "b"})
                            Exit For

                        Case "I3`1"
                            verifyTupleConstraint(def, {"c", "d"})
                            Exit For

                        Case Else
                            Throw TestExceptionUtilities.UnexpectedValue(name)

                    End Select

                Next
            End Sub

            Dim symbolValidator =
                Sub(m As ModuleSymbol)
                    Dim verifyTupleImpls =
                    Sub(t As NamedTypeSymbol, tupleNames As String())
                        Dim typeParam = t.TypeParameters.Single()
                        Dim constraint = DirectCast(typeParam.ConstraintTypes.Single(), NamedTypeSymbol)
                        Dim typeArg = constraint.TypeArguments.Single()
                        Assert.True(typeArg.IsTupleType)
                        Assert.Equal(tupleNames, typeArg.TupleElementNames)
                    End Sub

                    For Each t In m.GlobalNamespace.GetTypeMembers()
                        Select Case t.Name
                            Case "I1"
                            Case "<Module>"
                                Continue For

                            Case "I2"
                                verifyTupleImpls(t, {"a", "b"})
                                Exit For

                            Case "I3"
                                verifyTupleImpls(t, {"c", "d"})
                                Exit For

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(t.Name)
                        End Select
                    Next
                End Sub

            CompileAndVerify(src,
                             additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef},
                             validator:=validator,
                             symbolValidator:=symbolValidator)
        End Sub

        <Fact>
        <WorkItem(14844, "https://github.com/dotnet/roslyn/issues/14844")>
        Public Sub AttributesOnInterfaceImplementations()
            Dim src = <compilation>
                          <file name="a.vb">
                              <![CDATA[
Public Interface I1(Of T)
End Interface

Public Interface I2
    Inherits I1(Of (a as Integer, b as Integer))
End Interface
Public Interface I3
    Inherits I1(Of (c as Integer, d as Integer))
End Interface
]]>

                          </file>
                      </compilation>

            Dim validator =
            Sub(assembly As PEAssembly)
                Dim reader = assembly.ManifestModule.MetadataReader

                Dim verifyTupleImpls =
                Sub(def As TypeDefinition, tupleNames As String())
                    Dim interfaceImpls = def.GetInterfaceImplementations()
                    Assert.Equal(1, interfaceImpls.Count)
                    Dim interfaceImpl = reader.GetInterfaceImplementation(interfaceImpls.Single())

                    Dim attributes = interfaceImpl.GetCustomAttributes()
                    Assert.Equal(1, attributes.Count)
                    Dim attr = reader.GetCustomAttribute(attributes.Single())

                    ' Verify that the attribute contains an array of matching tuple names
                    Dim argsReader = reader.GetBlobReader(attr.Value)
                    ' Prolog
                    Assert.Equal(1, argsReader.ReadUInt16())
                    ' Array size
                    Assert.Equal(tupleNames.Length, argsReader.ReadInt32())

                    For Each name In tupleNames
                        Assert.Equal(name, argsReader.ReadSerializedString())
                    Next
                End Sub

                For Each typeHandle In reader.TypeDefinitions
                    Dim def = reader.GetTypeDefinition(typeHandle)
                    Dim name = reader.GetString(def.Name)

                    Select Case name
                        Case "I1`1"
                        Case "<Module>"
                            Continue For

                        Case "I2"
                            verifyTupleImpls(def, {"a", "b"})
                            Exit Select

                        Case "I3"
                            verifyTupleImpls(def, {"c", "d"})
                            Exit Select

                        Case Else
                            Throw TestExceptionUtilities.UnexpectedValue(name)

                    End Select
                Next
            End Sub

            Dim symbolValidator =
                Sub(m As ModuleSymbol)
                    Dim VerifyTupleImpls =
                    Sub(t As NamedTypeSymbol, tupleNames As String())
                        Dim interfaceImpl = t.Interfaces.Single()
                        Dim typeArg = interfaceImpl.TypeArguments.Single()
                        Assert.True(typeArg.IsTupleType)
                        Assert.Equal(tupleNames, typeArg.TupleElementNames)
                    End Sub

                    For Each t In m.GlobalNamespace.GetTypeMembers()
                        Select Case t.Name
                            Case "I1"
                            Case "<Module>"
                                Continue For

                            Case "I2"
                                VerifyTupleImpls(t, {"a", "b"})
                                Exit Select

                            Case "I3"
                                VerifyTupleImpls(t, {"c", "d"})
                                Exit Select

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(t.Name)
                        End Select
                    Next
                End Sub

            CompileAndVerify(src,
                additionalRefs:={ValueTupleRef, SystemRuntimeFacadeRef},
                validator:=validator,
                symbolValidator:=symbolValidator)
        End Sub
    End Class
End Namespace
