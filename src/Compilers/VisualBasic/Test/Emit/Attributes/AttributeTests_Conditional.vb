' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class AttributeTests_Conditional
        Inherits BasicTestBase

#Region "Conditional Attribute Type tests"

#Region "Common Helpers"
        Private Shared ReadOnly s_commonTestSource_ConditionalAttrDefs As String = <![CDATA[
Imports System
Imports System.Diagnostics

' Applied conditional attribute

<Conditional("cond1")> _
Public Class PreservedAppliedAttribute
    Inherits Attribute
End Class

<Conditional("cond2")> _
Public Class OmittedAppliedAttribute
    Inherits Attribute
End Class


' Inherited conditional attribute

<Conditional("cond3_dummy")> _
Public Class BasePreservedInheritedAttribute
    Inherits Attribute
End Class
<Conditional("cond3")> _
Public Class PreservedInheritedAttribute
    Inherits BasePreservedInheritedAttribute
End Class

<Conditional("cond4")> _
Public Class BaseOmittedInheritedAttribute
    Inherits Attribute
End Class
<Conditional("cond5")> _
Public Class OmittedInheritedAttribute
    Inherits BaseOmittedInheritedAttribute
End Class

' Multiple conditional attributes

<Conditional("cond6"), Conditional("cond7"), Conditional("cond8")> _
Public Class PreservedMultipleAttribute
    Inherits Attribute
End Class

<Conditional("cond9")> _
Public Class BaseOmittedMultipleAttribute
    Inherits Attribute
End Class
<Conditional("cond10"), Conditional("cond11")> _
Public Class OmittedMultipleAttribute
    Inherits BaseOmittedMultipleAttribute
End Class


' Partially preserved applied conditional attribute
' This attribute has its conditional constant defined midway through the source file. Hence it is conditionally emitted in metadata only for some symbols.

<Conditional("condForPartiallyPreservedAppliedAttribute")> _
Public Class PartiallyPreservedAppliedAttribute
    Inherits Attribute
End Class
]]>.Value

        Private Shared ReadOnly s_commonTestSource_ConditionalAttributesApplied As String = <![CDATA[
<PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
Public MustInherit Class Z
    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    Public Function m(<PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> param1 As Integer) _
        As <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> Integer

#Const condForPartiallyPreservedAppliedAttribute = True

        Return 0
    End Function

    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    Public f As Integer

    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    Public Property p1() As <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> Integer
        <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
        Get
            Return m_p1
        End Get

#Const condForPartiallyPreservedAppliedAttribute = False

        <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
        Set(value As Integer)
            m_p1 = value
        End Set
    End Property
    Private m_p1 As Integer

    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    Public MustOverride ReadOnly Property p2() As <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> Integer

    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    Public MustOverride Property p3() As <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> Integer

    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    Public Event e As Action
End Class

#Const condForPartiallyPreservedAppliedAttribute = "TrueAgain"

<PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
Public Enum E
    <PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
    A = 1
End Enum

<PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute, PartiallyPreservedAppliedAttribute> _
Public Structure S
End Structure

Public Class Test
    Public Shared Sub Main()
    End Sub
End Class
]]>.Value
        Private ReadOnly _commonValidatorForCondAttrType As Func(Of Boolean, Action(Of ModuleSymbol)) =
            Function(isFromSource As Boolean) _
                Sub(m As ModuleSymbol)

                    ' Each Tuple indicates: <Attributes, hasPartiallyPreservedAppliedAttribute>
                    ' PartiallyPreservedAppliedAttribute has its conditional constant defined midway through the source file.
                    ' Hence this attribute is emitted in metadata only for some symbols.
                    Dim attributesArrayBuilder = ArrayBuilder(Of Tuple(Of ImmutableArray(Of VisualBasicAttributeData), Boolean)).GetInstance()

                    Dim classZ = m.GlobalNamespace.GetTypeMember("Z")
                    attributesArrayBuilder.Add(Tuple.Create(classZ.GetAttributes(), False))

                    Dim methodM = classZ.GetMember(Of MethodSymbol)("m")
                    attributesArrayBuilder.Add(Tuple.Create(methodM.GetAttributes(), False))
                    attributesArrayBuilder.Add(Tuple.Create(methodM.GetReturnTypeAttributes(), False))
                    Dim param1 = methodM.Parameters(0)
                    attributesArrayBuilder.Add(Tuple.Create(param1.GetAttributes(), False))

                    Dim fieldF = classZ.GetMember(Of FieldSymbol)("f")
                    attributesArrayBuilder.Add(Tuple.Create(fieldF.GetAttributes(), True))

                    Dim propP1 = classZ.GetMember(Of PropertySymbol)("p1")
                    attributesArrayBuilder.Add(Tuple.Create(propP1.GetAttributes(), True))
                    Dim propGetMethod = propP1.GetMethod
                    attributesArrayBuilder.Add(Tuple.Create(propGetMethod.GetAttributes(), True))
                    attributesArrayBuilder.Add(Tuple.Create(propGetMethod.GetReturnTypeAttributes(), True))
                    Dim propSetMethod = propP1.SetMethod
                    attributesArrayBuilder.Add(Tuple.Create(propSetMethod.GetAttributes(), False))
                    Assert.Equal(0, propSetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, propSetMethod.Parameters(0).GetAttributes().Length)

                    Dim propP2 = classZ.GetMember(Of PropertySymbol)("p2")
                    attributesArrayBuilder.Add(Tuple.Create(propP2.GetAttributes(), False))
                    propGetMethod = propP2.GetMethod
                    Assert.Equal(0, propGetMethod.GetAttributes().Length)
                    attributesArrayBuilder.Add(Tuple.Create(propGetMethod.GetReturnTypeAttributes(), False))

                    Dim propP3 = classZ.GetMember(Of PropertySymbol)("p3")
                    attributesArrayBuilder.Add(Tuple.Create(propP3.GetAttributes(), False))
                    propGetMethod = propP3.GetMethod
                    Assert.Equal(0, propGetMethod.GetAttributes().Length)
                    attributesArrayBuilder.Add(Tuple.Create(propGetMethod.GetReturnTypeAttributes(), False))
                    propSetMethod = propP3.SetMethod
                    Assert.Equal(0, propSetMethod.GetAttributes().Length)
                    Assert.Equal(0, propSetMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, propSetMethod.Parameters(0).GetAttributes().Length)

                    Dim eventE = classZ.GetMember(Of EventSymbol)("e")
                    attributesArrayBuilder.Add(Tuple.Create(eventE.GetAttributes(), False))
                    If isFromSource Then
                        Assert.Equal(0, eventE.AssociatedField.GetAttributes().Length)
                        Assert.Equal(0, eventE.AddMethod.GetAttributes().Length)
                        Assert.Equal(0, eventE.RemoveMethod.GetAttributes().Length)
                    Else
                        AssertEx.Equal({"CompilerGeneratedAttribute"}, GetAttributeNames(eventE.AddMethod.GetAttributes()))
                        AssertEx.Equal({"CompilerGeneratedAttribute"}, GetAttributeNames(eventE.RemoveMethod.GetAttributes()))
                    End If
                    Assert.Equal(0, eventE.AddMethod.GetReturnTypeAttributes().Length)
                    Assert.Equal(0, eventE.RemoveMethod.GetReturnTypeAttributes().Length)

                    Dim enumE = m.GlobalNamespace.GetTypeMember("E")
                    attributesArrayBuilder.Add(Tuple.Create(enumE.GetAttributes(), True))

                    Dim fieldA = enumE.GetMember(Of FieldSymbol)("A")
                    attributesArrayBuilder.Add(Tuple.Create(fieldA.GetAttributes(), True))

                    Dim structS = m.GlobalNamespace.GetTypeMember("S")
                    attributesArrayBuilder.Add(Tuple.Create(structS.GetAttributes(), True))

                    For Each tup In attributesArrayBuilder
                        ' PreservedAppliedAttribute and OmittedAppliedAttribute have applied conditional attributes, such that
                        ' (a) PreservedAppliedAttribute is conditionally applied to symbols
                        ' (b) OmittedAppliedAttribute is conditionally NOT applied to symbols

                        ' PreservedInheritedAttribute and OmittedInheritedAttribute have inherited conditional attributes, such that
                        ' (a) PreservedInheritedAttribute is conditionally applied to symbols
                        ' (b) OmittedInheritedAttribute is conditionally NOT applied to symbols

                        ' PreservedMultipleAttribute and OmittedMultipleAttribute have multiple applied/inherited conditional attributes, such that
                        ' (a) PreservedMultipleAttribute is conditionally applied to symbols
                        ' (b) OmittedMultipleAttribute is conditionally NOT applied to symbols

                        ' PartiallyPreservedAppliedAttribute has its conditional constant defined midway through the source file.
                        ' Hence this attribute is emitted in metadata only for some symbols.

                        Dim attributesArray As ImmutableArray(Of VisualBasicAttributeData) = tup.Item1
                        Dim actualAttributeNames = GetAttributeNames(attributesArray)

                        If isFromSource Then
                            ' All attributes should be present for source symbols
                            AssertEx.SetEqual({"PreservedAppliedAttribute",
                                               "OmittedAppliedAttribute",
                                               "PreservedInheritedAttribute",
                                               "OmittedInheritedAttribute",
                                               "PreservedMultipleAttribute",
                                               "OmittedMultipleAttribute",
                                               "PartiallyPreservedAppliedAttribute"}, actualAttributeNames)
                        Else
                            Dim hasPartiallyPreservedAppliedAttribute = tup.Item2

                            Dim expectedAttributeNames As String()

                            If Not hasPartiallyPreservedAppliedAttribute Then
                                ' Only PreservedAppliedAttribute, PreservedInheritedAttribute, PreservedMultipleAttribute should be emitted in metadata
                                expectedAttributeNames = {"PreservedAppliedAttribute",
                                                          "PreservedInheritedAttribute",
                                                          "PreservedMultipleAttribute"}
                            Else
                                ' PartiallyPreservedAppliedAttribute must also be emitted in metadata
                                expectedAttributeNames = {"PreservedAppliedAttribute",
                                                          "PreservedInheritedAttribute",
                                                          "PreservedMultipleAttribute",
                                                          "PartiallyPreservedAppliedAttribute"}
                            End If

                            AssertEx.SetEqual(expectedAttributeNames, actualAttributeNames)
                        End If
                    Next

                    attributesArrayBuilder.Free()
                End Sub

        Private Sub TestConditionAttributeType_SameSource(condDefs As String, preprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object)))
            ' Same source file
            Debug.Assert(Not preprocessorSymbols.IsDefault)
            Dim parseOpts = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols)
            Dim testSource As String = condDefs & s_commonTestSource_ConditionalAttrDefs & s_commonTestSource_ConditionalAttributesApplied
            Dim compilation = CreateCompilationWithMscorlib40({Parse(testSource, parseOpts)}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, sourceSymbolValidator:=_commonValidatorForCondAttrType(True), symbolValidator:=_commonValidatorForCondAttrType(False), expectedOutput:="")
        End Sub

        Private Sub TestConditionAttributeType_SameSource(condDefs As String)
            TestConditionAttributeType_SameSource(condDefs, ImmutableArray.Create(Of KeyValuePair(Of String, Object))())
        End Sub

        Private Sub TestConditionAttributeType_DifferentSource(condDefsSrcFile1 As String, condDefsSrcFile2 As String)
            TestConditionAttributeType_DifferentSource(condDefsSrcFile1, ImmutableArray.Create(Of KeyValuePair(Of String, Object))(), condDefsSrcFile2, ImmutableArray.Create(Of KeyValuePair(Of String, Object))())
        End Sub

        Private Sub TestConditionAttributeType_DifferentSource(condDefsSrcFile1 As String,
                                                               preprocessorSymbolsSrcFile1 As ImmutableArray(Of KeyValuePair(Of String, Object)),
                                                               condDefsSrcFile2 As String,
                                                               preprocessorSymbolsSrcFile2 As ImmutableArray(Of KeyValuePair(Of String, Object)))
            Dim source1 As String = condDefsSrcFile1 & s_commonTestSource_ConditionalAttrDefs
            Dim source2 As String = condDefsSrcFile2 & <![CDATA[
Imports System
Imports System.Diagnostics
]]>.Value & s_commonTestSource_ConditionalAttributesApplied

            Debug.Assert(Not preprocessorSymbolsSrcFile1.IsDefault)
            Dim parseOpts1 = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbolsSrcFile1)
            Debug.Assert(Not preprocessorSymbolsSrcFile2.IsDefault)
            Dim parseOpts2 = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbolsSrcFile2)

            ' Different source files, same compilation
            Dim comp = CreateCompilationWithMscorlib40({Parse(source1, parseOpts1), Parse(source2, parseOpts2)}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(comp, sourceSymbolValidator:=_commonValidatorForCondAttrType(True), symbolValidator:=_commonValidatorForCondAttrType(False), expectedOutput:="")

            ' Different source files, different compilation
            Dim comp1 = CreateCompilationWithMscorlib40({Parse(source1, parseOpts1)}, options:=TestOptions.ReleaseDll)
            Dim comp2 = VisualBasicCompilation.Create("comp2", {Parse(source2, parseOpts2)}, {MscorlibRef, New VisualBasicCompilationReference(comp1)}, options:=TestOptions.ReleaseExe)
            CompileAndVerify(comp2, sourceSymbolValidator:=_commonValidatorForCondAttrType(True), symbolValidator:=_commonValidatorForCondAttrType(False), expectedOutput:="")
        End Sub
#End Region

#Region "Tests"
        <Fact>
        Public Sub TestConditionAttributeType_01_SourceDefines()
            Dim conditionalDefs As String = <![CDATA[
#Const cond1 = 1
#Const cond3 = ""
#Const cond6 = True
 ]]>.Value

            TestConditionAttributeType_SameSource(conditionalDefs)

            Dim conditionalDefsDummy As String = <![CDATA[
#Const cond2 = 1
#Const cond5 = ""
#Const cond7 = True
 ]]>.Value
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, conditionalDefs)
        End Sub

        <Fact>
        Public Sub TestConditionAttributeType_01_CommandLineDefines()
            Dim preprocessorSymbols = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond1", 1),
                                                                                               New KeyValuePair(Of String, Object)("cond3", ""),
                                                                                               New KeyValuePair(Of String, Object)("cond6", True))
            TestConditionAttributeType_SameSource(condDefs:="", preprocessorSymbols:=preprocessorSymbols)

            Dim preprocessorSymbolsDummy = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond2", 1),
                                                                                                    New KeyValuePair(Of String, Object)("cond5", ""),
                                                                                                    New KeyValuePair(Of String, Object)("cond7", True))
            TestConditionAttributeType_DifferentSource(condDefsSrcFile1:="", preprocessorSymbolsSrcFile1:=preprocessorSymbolsDummy, condDefsSrcFile2:="", preprocessorSymbolsSrcFile2:=preprocessorSymbols)
        End Sub

        <Fact>
        Public Sub TestConditionAttributeType_02_SourceDefines()
            Dim conditionalDefs As String = <![CDATA[
#Const cond1 = 1
#Const cond2 = 1.0  ' Decimal type value is not considered for CC constants.
#Const cond3 = ""
#Const cond4 = True     ' Conditional attributes are not inherited from base type.
#Const cond5 = 1
#Const cond5 = Nothing  ' The last definition holds for CC constants.
#Const cond6 = True
#Const cond7 = 0  ' One of the conditional symbol is zero, but other conditional symbols for the attribute type are defined.
#Const cond8 = 2 ' Multiple conditional symbols defined.
 ]]>.Value

            TestConditionAttributeType_SameSource(conditionalDefs)

            Dim conditionalDefsDummy As String = <![CDATA[
#Const cond2 = 1
#Const cond3_dummy = 0
#Const cond5 = ""
#Const cond7 = True
#Const cond8 = True
#Const cond9 = True
#Const cond10 = True
#Const cond11 = True
 ]]>.Value
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, conditionalDefs)
        End Sub

        <Fact>
        Public Sub TestConditionAttributeType_02_CommandLineDefines()
            ' Mix and match source and command line defines.
            Dim preprocessorSymbols = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond1", 1),
                                                                                               New KeyValuePair(Of String, Object)("cond2", 1.0),
                                                                                               New KeyValuePair(Of String, Object)("cond3", ""),
                                                                                               New KeyValuePair(Of String, Object)("cond4", True),
                                                                                               New KeyValuePair(Of String, Object)("cond5", 1))
            Dim conditionalDefs As String = <![CDATA[
#Const cond5 = Nothing  ' Source definition for CC constants overrides command line /define definitions.
#Const cond6 = True ' Mix match source and command line defines.
#Const cond7 = 0  ' One of the conditional symbol is zero, but other conditional symbols for the attribute type are defined.
#Const cond8 = 2 ' Multiple conditional symbols defined.
 ]]>.Value

            TestConditionAttributeType_SameSource(conditionalDefs, preprocessorSymbols:=preprocessorSymbols)

            ' Mix and match source and command line defines.
            Dim preprocessorSymbolsDummy = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond2", 1),
                                                                                                    New KeyValuePair(Of String, Object)("cond3_dummy", 1))
            Dim conditionalDefsDummy As String = <![CDATA[
#Const cond5 = ""
#Const cond7 = True
#Const cond8 = True
#Const cond9 = True
#Const cond10 = True
#Const cond11 = True
 ]]>.Value
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, preprocessorSymbolsDummy, conditionalDefs, preprocessorSymbols)
        End Sub

        <Fact>
        Public Sub TestNestedTypeMember()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Diagnostics

<Conditional(Outer.Nested.ConstStr)> _
<Outer> _
Class Outer
	Inherits Attribute
	Public Class Nested
		Public Const ConstStr As String = "str"
	End Class
End Class
 ]]>
    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

#End Region

#End Region

#Region "Conditional Method tests"

#Region "Common Helpers"
        Private Shared ReadOnly s_commonTestSource_ConditionalMethodDefs As String = <![CDATA[
    Imports System
    Imports System.Diagnostics

    Public Class BaseZ
        <Conditional("cond3_base")> _
        Public Overridable Sub PreservedCalls_InheritedConditional_Method()
            System.Console.WriteLine("BaseZ.PreservedCalls_InheritedConditional_Method")
        End Sub

        <Conditional("cond4_base")> _
        Public Overridable Sub OmittedCalls_InheritedConditional_Method()
            System.Console.WriteLine("BaseZ.OmittedCalls_InheritedConditional_Method")
        End Sub
    End Class

    Public Interface I
        ' Conditional attributes are ignored for interface methods, but respected for implementing methods.
        <Conditional("dummy")>
        Sub PartiallyPreservedCalls_Interface_Method()
    End Interface

    Public Class Z
        Inherits BaseZ
        Implements I

        <Conditional("cond1")> _
        Public Sub PreservedCalls_AppliedConditional_Method()
            System.Console.WriteLine("Z.PreservedCalls_AppliedConditional_Method")
        End Sub

        <Conditional("cond2")> _
        Public Sub OmittedCalls_AppliedConditional_Method()
            System.Console.WriteLine("Z.OmittedCalls_AppliedConditional_Method")
        End Sub

        ' Conditional symbols are not inherited by overriding methods in VB
        <Conditional("cond3")> _
        Public Overrides Sub PreservedCalls_InheritedConditional_Method()
            System.Console.WriteLine("Z.PreservedCalls_InheritedConditional_Method")
        End Sub

    #Const cond4_base = "Conditional symbols are not inherited by overriding methods in VB"
        <Conditional("cond4")> _
        Public Overrides Sub OmittedCalls_InheritedConditional_Method()
            System.Console.WriteLine("Z.OmittedCalls_InheritedConditional_Method")
        End Sub

        <Conditional("cond5"), Conditional("cond6")> _
        Public Sub PreservedCalls_MultipleConditional_Method()
            System.Console.WriteLine("Z.PreservedCalls_MultipleConditional_Method")
        End Sub

        <Conditional("cond7"), Conditional("cond8")> _
        Public Sub OmittedCalls_MultipleConditional_Method()
            System.Console.WriteLine("Z.OmittedCalls_MultipleConditional_Method")
        End Sub

        ' Conditional attributes are ignored for interface methods, but respected for implementing methods.
        <Conditional("cond9")>
        Public Sub PartiallyPreservedCalls_Interface_Method() Implements I.PartiallyPreservedCalls_Interface_Method
            System.Console.WriteLine("Z.PartiallyPreservedCalls_Interface_Method")
        End Sub

        ' Conditional attributes are ignored for functions
        <Conditional("cond10")>
        Public Function PreservedCalls_Function() As Integer
            System.Console.WriteLine("Z.PreservedCalls_Function")
            Return 0
        End Function

        <Conditional(""), Conditional(Nothing)> _
        Public Sub OmittedCalls_AlwaysFalseConditional_Method()
            System.Console.WriteLine("Z.OmittedCalls_AlwaysFalseConditional_Method")
        End Sub

        <Conditional("condForPartiallyPreservedAppliedAttribute")>
        Public Sub PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method(i As Integer)
            System.Console.WriteLine("Z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method" & i)
        End Sub
    End Class
    ]]>.Value

        Private Shared ReadOnly s_commonTestSource_ConditionalMethodCalls As String = <![CDATA[
    Module Module1
        Public Sub Main()
            Dim z = New Z()
            z.PreservedCalls_AppliedConditional_Method()
            z.OmittedCalls_AppliedConditional_Method()
            z.PreservedCalls_InheritedConditional_Method()
            z.OmittedCalls_InheritedConditional_Method()
            z.PreservedCalls_MultipleConditional_Method()
            z.OmittedCalls_MultipleConditional_Method()
            z.OmittedCalls_AlwaysFalseConditional_Method()
            z.PartiallyPreservedCalls_Interface_Method() ' Omitted
            DirectCast(z, I).PartiallyPreservedCalls_Interface_Method() ' Preserved
            Console.WriteLine(z.PreservedCalls_Function())

            ' Second and fourth calls are preserved, first, third and fifth calls are omitted.
            z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method(1)
    # Const condForPartiallyPreservedAppliedAttribute = True
            z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method(2)
    # Const condForPartiallyPreservedAppliedAttribute = 0
            z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method(3)
    # Const condForPartiallyPreservedAppliedAttribute = "TrueAgain"
            z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method(4)
    # Const condForPartiallyPreservedAppliedAttribute = Nothing
            z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method(5)
        End Sub
    End Module
    ]]>.Value

        Private Shared ReadOnly s_commonExpectedOutput_ConditionalMethodsTest As String =
            "Z.PreservedCalls_AppliedConditional_Method" & Environment.NewLine &
            "Z.PreservedCalls_InheritedConditional_Method" & Environment.NewLine &
            "Z.PreservedCalls_MultipleConditional_Method" & Environment.NewLine &
            "Z.PartiallyPreservedCalls_Interface_Method" & Environment.NewLine &
            "Z.PreservedCalls_Function" & Environment.NewLine &
            "0" & Environment.NewLine &
            "Z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method2" & Environment.NewLine &
            "Z.PartiallyPreservedCalls_PartiallyPreservedAppliedAttribute_Method4" & Environment.NewLine

        Private Sub TestConditionalMethod_SameSource(condDefs As String)
            TestConditionalMethod_SameSource(condDefs, ImmutableArray.Create(Of KeyValuePair(Of String, Object))())
        End Sub

        Private Sub TestConditionalMethod_SameSource(condDefs As String, preprocessorSymbols As ImmutableArray(Of KeyValuePair(Of String, Object)))
            ' Same source file
            Debug.Assert(Not preprocessorSymbols.IsDefault)
            Dim parseOpts = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols)
            Dim testSource As String = condDefs & s_commonTestSource_ConditionalMethodDefs & s_commonTestSource_ConditionalMethodCalls
            Dim comp = VisualBasicCompilation.Create(GetUniqueName(), {Parse(testSource, parseOpts)}, {MscorlibRef, SystemCoreRef, MsvbRef})
            CompileAndVerify(comp, expectedOutput:=s_commonExpectedOutput_ConditionalMethodsTest)
        End Sub

        Private Sub TestConditionalMethod_DifferentSource(condDefsSrcFile1 As String, condDefsSrcFile2 As String)
            TestConditionalMethod_DifferentSource(condDefsSrcFile1, ImmutableArray.Create(Of KeyValuePair(Of String, Object))(), condDefsSrcFile2, ImmutableArray.Create(Of KeyValuePair(Of String, Object))())
        End Sub

        Private Sub TestConditionalMethod_DifferentSource(condDefsSrcFile1 As String,
                                                          preprocessorSymbolsSrcFile1 As ImmutableArray(Of KeyValuePair(Of String, Object)),
                                                          condDefsSrcFile2 As String,
                                                          preprocessorSymbolsSrcFile2 As ImmutableArray(Of KeyValuePair(Of String, Object)))
            Dim source1 As String = condDefsSrcFile1 & s_commonTestSource_ConditionalMethodDefs
            Dim source2 As String = condDefsSrcFile2 & <![CDATA[
Imports System
Imports System.Diagnostics
]]>.Value & s_commonTestSource_ConditionalMethodCalls

            Debug.Assert(Not preprocessorSymbolsSrcFile1.IsDefault)
            Dim parseOpts1 = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbolsSrcFile1)
            Debug.Assert(Not preprocessorSymbolsSrcFile2.IsDefault)
            Dim parseOpts2 = VisualBasicParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbolsSrcFile2)

            ' Different source files, same compilation
            Dim comp = VisualBasicCompilation.Create(GetUniqueName(), {Parse(source1, parseOpts1), Parse(source2, parseOpts2)}, {MscorlibRef, MsvbRef}, TestOptions.ReleaseExe)
            CompileAndVerify(comp, expectedOutput:=s_commonExpectedOutput_ConditionalMethodsTest)

            ' Different source files, different compilation
            Dim comp1 = VisualBasicCompilation.Create(GetUniqueName(), {Parse(source1, parseOpts1)}, {MscorlibRef, MsvbRef}, TestOptions.ReleaseDll)
            Dim comp2 = VisualBasicCompilation.Create(GetUniqueName(), {Parse(source2, parseOpts2)}, {MscorlibRef, MsvbRef, comp1.ToMetadataReference()}, TestOptions.ReleaseExe)
            CompileAndVerify(comp2, expectedOutput:=s_commonExpectedOutput_ConditionalMethodsTest)
        End Sub
#End Region

#Region "Tests"
        <Fact>
        Public Sub TestConditionalMethod_01_SourceDefines()
            Dim conditionalDefs As String = <![CDATA[
#Const cond1 = 1
#Const cond3 = ""
#Const cond5 = True
 ]]>.Value

            TestConditionalMethod_SameSource(conditionalDefs)

            Dim conditionalDefsDummy As String = <![CDATA[
#Const cond2 = 1
#Const cond5 = ""
#Const cond7 = True
 ]]>.Value
            TestConditionalMethod_DifferentSource(conditionalDefsDummy, conditionalDefs)
        End Sub

        <Fact>
        Public Sub TestConditionalMethod_01_CommandLineDefines()
            Dim preprocessorSymbols = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond1", 1),
                                                                                               New KeyValuePair(Of String, Object)("cond3", ""),
                                                                                               New KeyValuePair(Of String, Object)("cond5", True))
            TestConditionalMethod_SameSource(condDefs:="", preprocessorSymbols:=preprocessorSymbols)

            Dim preprocessorSymbolsDummy = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond2", 1),
                                                                                                    New KeyValuePair(Of String, Object)("cond5", ""),
                                                                                                    New KeyValuePair(Of String, Object)("cond7", True))

            TestConditionalMethod_DifferentSource(condDefsSrcFile1:="", preprocessorSymbolsSrcFile1:=preprocessorSymbolsDummy, condDefsSrcFile2:="", preprocessorSymbolsSrcFile2:=preprocessorSymbols)
        End Sub

        <Fact>
        Public Sub TestConditionalMethod_02_SourceDefines()
            Dim conditionalDefs As String = <![CDATA[
#Const cond1 = 1
#Const cond2 = 1.0  ' Decimal type value is not considered for CC constants.
#Const cond3 = ""
#Const cond4_base = True     ' Conditional attributes are not inherited from base type.
#Const cond5 = 1
#Const cond5 = 0  ' One of the conditional symbol is zero, but other conditional symbols for the attribute type are defined.
#Const cond6 = True
#Const cond7 = 0  
#Const cond3 = True ' The last definition holds for CC constants.
 ]]>.Value

            TestConditionalMethod_SameSource(conditionalDefs)

            Dim conditionalDefsDummy As String = <![CDATA[
#Const cond2 = 1
#Const cond3_dummy = 0
#Const cond5 = ""
#Const cond7 = True
#Const cond8 = True
#Const cond9 = True
#Const cond10 = True
#Const cond11 = True
 ]]>.Value
            TestConditionalMethod_DifferentSource(conditionalDefsDummy, conditionalDefs)
        End Sub

        <Fact>
        Public Sub TestConditionalMethod_02_CommandLineDefines()
            ' Mix and match source and command line defines.
            Dim preprocessorSymbols = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond1", 1),
                                                                                               New KeyValuePair(Of String, Object)("cond2", 1.0),
                                                                                               New KeyValuePair(Of String, Object)("cond3", ""),
                                                                                               New KeyValuePair(Of String, Object)("cond4_base", True),
                                                                                               New KeyValuePair(Of String, Object)("cond5", 1))
            Dim conditionalDefs As String = <![CDATA[
#Const cond5 = 0  ' One of the conditional symbol is zero, but other conditional symbols for the attribute type are defined.
#Const cond6 = True
#Const cond7 = 0  
#Const cond3 = True ' The last definition holds for CC constants.
 ]]>.Value

            TestConditionalMethod_SameSource(conditionalDefs, preprocessorSymbols)

            ' Mix and match source and command line defines.
            Dim preprocessorSymbolsDummy = ImmutableArray.Create(Of KeyValuePair(Of String, Object))(New KeyValuePair(Of String, Object)("cond2", 1),
                                                                                                    New KeyValuePair(Of String, Object)("cond3_dummy", 0),
                                                                                                    New KeyValuePair(Of String, Object)("cond5", True))
            Dim conditionalDefsDummy As String = <![CDATA[
#Const cond7 = True
#Const cond8 = True
#Const cond9 = True
#Const cond10 = True
#Const cond11 = True
 ]]>.Value
            TestConditionalMethod_DifferentSource(conditionalDefsDummy, preprocessorSymbolsDummy, conditionalDefs, preprocessorSymbols)
        End Sub

        <WorkItem(546089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546089")>
        <Fact>
        Public Sub CaseInsensitivityTest()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System

Module Test
    &lt;System.Diagnostics.Conditional("VAR4")&gt; 
    Sub Sub1()
        Console.WriteLine("Sub1 Called")
    End Sub

    Sub Main()
#Const var4 = True
        Sub1()
    End Sub

End Module
                    </file>
                </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[Sub1 Called]]>)
        End Sub

        <WorkItem(546089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546089")>
        <Fact>
        Public Sub CaseInsensitivityTest_02()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System

Module Test
    &lt;System.Diagnostics.Conditional("VAR4")&gt; 
    Sub Sub1()
        Console.WriteLine("Sub1 Called")
    End Sub

#Const VAR4 = False
    Sub Main()
#Const var4 = True
        Sub1()
    End Sub

End Module
                    </file>
                </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[Sub1 Called]]>)
        End Sub

        <WorkItem(546094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546094")>
        <Fact>
        Public Sub ConditionalAttributeOnPropertySetter()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System

Class TestClass
    WriteOnly Property goo() As String
        &lt;Diagnostics.Conditional("N")&gt;
        Set(ByVal Value As String)
            Console.WriteLine("Property Called")
        End Set
    End Property
End Class

Module M1
    Sub Main()
        Dim t As New TestClass()
        t.goo = "abds"
    End Sub
End Module

                    </file>
                </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[Property Called]]>)
        End Sub
#End Region

        <WorkItem(1003274, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003274")>
        <Fact>
        Public Sub ConditionalAttributeInNetModule()
            Const source = "
Imports System.Diagnostics

Class C
    Sub M()
        N1()
        N2()
    End Sub

    <Conditional(""Defined"")>
    Sub N1()
    End Sub

    <Conditional(""Undefined"")>
    Sub N2()
    End Sub
End Class
"
            Dim parseOptions As New VisualBasicParseOptions(preprocessorSymbols:={New KeyValuePair(Of String, Object)("Defined", True)})
            Dim comp = CreateCompilationWithMscorlib40({VisualBasicSyntaxTree.ParseText(source, parseOptions)}, options:=TestOptions.ReleaseModule)
            CompileAndVerify(comp, verify:=Verification.Fails).VerifyIL("C.M", "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""Sub C.N1()""
  IL_0006:  ret
}
")
        End Sub

#End Region

    End Class
End Namespace
