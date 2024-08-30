' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    ''' <summary>
    ''' Tests for lambdas converted to expression trees.
    ''' </summary>
    Public Class CodeGenExprLambda
        Inherits BasicTestBase

#Region "Literals"

        <Fact>
        Public Sub Literals()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim exprTree1 As Expression(Of Func(Of Integer)) = Function() 3
        Console.WriteLine(exprtree1.Dump)
        Dim exprTree2 As Expression(Of Func(Of Integer, Integer)) = Function(x) 3 + x
        Console.WriteLine(exprtree2.Dump)
    End Sub
End Module]]></file>
                             <%= _exprTesting %>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe.WithOverflowChecks(True),
                 expectedOutput:=<![CDATA[
Lambda(
  body {
    Constant(
      3
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`1[System.Int32]
)

Lambda(
  Parameter(
    x
    type: System.Int32
  )
  body {
    AddChecked(
      Constant(
        3
        type: System.Int32
      )
      Parameter(
        x
        type: System.Int32
      )
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`2[System.Int32,System.Int32]
)
]]>).VerifyDiagnostics()
        End Sub

#End Region

#Region "Unary Operations"

        <Fact>
        Public Sub TestUnaryOperator_Unchecked_PlusMinus()
            TestUnaryOperator_AllTypes_PlusMinus(False, result:=ExpTreeTestResources.UncheckedUnaryPlusMinusNot)
        End Sub

        <Fact>
        Public Sub TestUnaryOperator_Checked_PlusMinusNot()
            TestUnaryOperator_AllTypes_PlusMinus(True, result:=ExpTreeTestResources.CheckedUnaryPlusMinusNot)
        End Sub

        <Fact>
        Public Sub UserDefinedIsTrueIsFalse_Unchecked()
            Dim file = <file name="expr.vb"><%= ExpTreeTestResources.TestUnary_UDO_IsTrueIsFalse %></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedAndUncheckedIsTrueIsFalse, checked:=False)
        End Sub

        <Fact>
        Public Sub UserDefinedIsTrueIsFalse_Checked()
            Dim file = <file name="expr.vb"><%= ExpTreeTestResources.TestUnary_UDO_IsTrueIsFalse %></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedAndUncheckedIsTrueIsFalse, checked:=True)
        End Sub

        <Fact>
        Public Sub UserDefinedPlusMinusNot_Unchecked()
            Dim file = <file name="expr.vb"><%= ExpTreeTestResources.TestUnary_UDO_PlusMinusNot %></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedAndUncheckedUdoUnaryPlusMinusNot1, checked:=False)
        End Sub

        <Fact>
        Public Sub UserDefinedPlusMinusNot_Checked()
            Dim file = <file name="expr.vb"><%= ExpTreeTestResources.TestUnary_UDO_PlusMinusNot %></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedAndUncheckedUdoUnaryPlusMinusNot1, checked:=True)
        End Sub

#Region "Implementation"

        Public Sub TestUnaryOperator_AllTypes_IsIsNot(checked As Boolean, result As String)
            Dim ops() As String = {"Is", "IsNot"}

            Dim tests As New List(Of ExpressionTreeTest)
            For Each op In ops
                TestUnaryOperator_AllTypesWithNullableAndEnum_InIsNot(checked, op, tests)
            Next

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestUnaryOperator_AllTypes_PlusMinus(checked As Boolean, result As String)
            Dim ops() As String = {"+", "-", "Not"}

            Dim tests As New List(Of ExpressionTreeTest)
            For Each op In ops
                TestUnaryOperator_AllTypesWithNullableAndEnum_PlusMinus(checked, op, tests)
            Next

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

#Region "Expression(Of Func(Of Type, Type)) <= Function(x) <op> x"

        Private Sub TestUnaryOperator_AllTypesWithNullableAndEnum_PlusMinus(checked As Boolean, operation As String, list As List(Of ExpressionTreeTest))
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("SByte", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("Byte", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("Short", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("UShort", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("Integer", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("UInteger", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("Long", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus("ULong", operation, list)

            TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus("Boolean", operation, list)
            TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus("Single", operation, list)
            TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus("Double", operation, list)
            TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus("Decimal", operation, list)
            'TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus("Date", operation, list)

            TestUnaryOperator_AddTestsForType_PlusMinus("String", operation, list)
            TestUnaryOperator_AddTestsForType_PlusMinus("Object", operation, list)
        End Sub

        Private Sub TestUnaryOperator_AddTestsForTypeAndEnum_PlusMinus(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus(type, operation, list)
            TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus(GetEnumTypeName(type), operation, list)
        End Sub

        Private Sub TestUnaryOperator_AddTestsForTypeAndNullable_PlusMinus(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestUnaryOperator_AddTestsForType_PlusMinus(type, operation, list)
            TestUnaryOperator_AddTestsForType_PlusMinus(type + "?", operation, list)
        End Sub

        Private Sub TestUnaryOperator_AddTestsForType_PlusMinus(type1 As String, operation As String, list As List(Of ExpressionTreeTest))
            Dim descr1 = String.Format("-=-=-=-=-=-=-=-=- {0} {{0}} => {{0}} -=-=-=-=-=-=-=-=-", operation)

            list.Add(New ExpressionTreeTest With {.Description = String.Format(descr1, type1),
                                                  .ExpressionTypeArgument =
                                                            String.Format("Func(Of {0}, {0})", type1),
                                                  .Lambda = String.Format("Function(x) {0} x ", operation)})
        End Sub

#End Region

#Region "Expression(Of Func(Of Type, Type)) <= Function(x) x <op> Nothing"

        Private Sub TestUnaryOperator_AllTypesWithNullableAndEnum_InIsNot(checked As Boolean, operation As String, list As List(Of ExpressionTreeTest))
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("SByte?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("Byte?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("Short?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("UShort?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("Integer?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("UInteger?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("Long?", operation, list)
            TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot("ULong?", operation, list)

            TestUnaryOperator_AddTestsForType_IsIsNot("Boolean?", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot("Single?", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot("Double?", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot("Decimal?", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot("Date?", operation, list)

            TestUnaryOperator_AddTestsForType_IsIsNot("String", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot("Object", operation, list)
        End Sub

        Private Sub TestUnaryOperator_AddTestsForTypeAndEnum_IsIsNot(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestUnaryOperator_AddTestsForType_IsIsNot(type, operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot(GetEnumTypeName(type), operation, list)
        End Sub

        Private Sub TestUnaryOperator_AddTestsForType_IsIsNot(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestUnaryOperator_AddTestsForType_IsIsNot(type, "Object", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot(type, "Boolean", operation, list)
            TestUnaryOperator_AddTestsForType_IsIsNot(type, "Boolean?", operation, list)
        End Sub

        Private Sub TestUnaryOperator_AddTestsForType_IsIsNot(type1 As String, type2 As String, operation As String, list As List(Of ExpressionTreeTest))
            Dim descr1 = String.Format("-=-=-=-=-=-=-=-=- {{0}} {0} Nothing => {{1}} -=-=-=-=-=-=-=-=-", operation)

            list.Add(New ExpressionTreeTest With {.Description = String.Format(descr1, type1, type2),
                                                  .ExpressionTypeArgument =
                                                            String.Format("Func(Of {0}, {1})", type1, type2),
                                                  .Lambda = String.Format("Function(x) x {0} Nothing", operation)})
        End Sub

#End Region

#End Region

#End Region

#Region "Binary Operations"

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_Arithmetic()
            TestBinaryOperator_AllTypes_NumericOperations(False, result:=ExpTreeTestResources.UncheckedArithmeticBinaryOperators)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_Arithmetic()
            TestBinaryOperator_AllTypes_NumericOperations(True, result:=ExpTreeTestResources.CheckedArithmeticBinaryOperators)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_AndOrXor()
            TestBinaryOperator_AllTypes_AndOrXor(False, result:=ExpTreeTestResources.UncheckedAndOrXor)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_AndOrXor()
            TestBinaryOperator_AllTypes_AndOrXor(True, result:=ExpTreeTestResources.CheckedAndOrXor)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_ShortCircuit()
            TestBinaryOperator_AllTypes_ShortCircuit(False, result:=ExpTreeTestResources.UncheckedShortCircuit)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_ShortCircuit()
            TestBinaryOperator_AllTypes_ShortCircuit(True, result:=ExpTreeTestResources.CheckedShortCircuit)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_Comparisons()
            TestBinaryOperator_AllTypes_Comparisons(False, result:=ExpTreeTestResources.UncheckedComparisonOperators)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_Comparisons()
            TestBinaryOperator_AllTypes_Comparisons(True, result:=ExpTreeTestResources.CheckedComparisonOperators)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_IsIsNot()
            TestBinaryOperator_AllTypes_IsIsNot(False, result:=ExpTreeTestResources.CheckedAndUncheckedIsIsNot)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_IsIsNot()
            TestBinaryOperator_AllTypes_IsIsNot(True, result:=ExpTreeTestResources.CheckedAndUncheckedIsIsNot)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_IsIsNot_Nothing()
            TestUnaryOperator_AllTypes_IsIsNot(False, result:=ExpTreeTestResources.CheckedAndUncheckedIsIsNotNothing)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_IsIsNot_Nothing()
            TestUnaryOperator_AllTypes_IsIsNot(True, result:=ExpTreeTestResources.CheckedAndUncheckedIsIsNotNothing)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_Concatenate()
            TestBinaryOperator_ConcatenatePlus(False, result:=ExpTreeTestResources.UncheckedConcatenate)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_Concatenate()
            TestBinaryOperator_ConcatenatePlus(True, result:=ExpTreeTestResources.CheckedConcatenate)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub TestBinaryOperator_Unchecked_Like()
            TestBinaryOperator_Like(False, result:=ExpTreeTestResources.UncheckedLike)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub TestBinaryOperator_Checked_Like()
            TestBinaryOperator_Like(True, result:=ExpTreeTestResources.CheckedLike)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_WithDate()
            TestBinaryOperator_DateAsOperand(False, result:=ExpTreeTestResources.CheckedAndUncheckedWithDate)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_WithDate()
            TestBinaryOperator_DateAsOperand(True, result:=ExpTreeTestResources.CheckedAndUncheckedWithDate)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Unchecked_UserDefinedBinaryOperator()
            TestBinaryOperator_UserDefinedBinaryOperator(False, result:=ExpTreeTestResources.UncheckedUserDefinedBinaryOperators)
        End Sub

        <Fact>
        Public Sub TestBinaryOperator_Checked_UserDefinedBinaryOperator()
            TestBinaryOperator_UserDefinedBinaryOperator(True, result:=ExpTreeTestResources.CheckedUserDefinedBinaryOperators)
        End Sub

#Region "Implementation"

        Private Sub TestBinaryOperator_AllTypes_NumericOperations(checked As Boolean, result As String)
            TestBinaryOperator_AllTypesAndSomeOperations(checked, {"+", "-", "*", "/", "\", "^", "Mod", "<<", ">>"}, result)
        End Sub

        Private Sub TestBinaryOperator_AllTypes_AndOrXor(checked As Boolean, result As String)
            TestBinaryOperator_AllTypesAndSomeOperations(checked, {"And", "Or", "Xor"}, result)
        End Sub

        Public Sub TestBinaryOperator_AllTypes_ShortCircuit(checked As Boolean, result As String)
            TestBinaryOperator_AllTypesAndSomeOperations(checked, {"AndAlso", "OrElse"}, result)
        End Sub

        Private Sub TestBinaryOperator_AllTypesAndSomeOperations(checked As Boolean, ops() As String, result As String)
            Dim tests As New List(Of ExpressionTreeTest)
            For Each op In ops
                TestBinaryOperator_AllTypesWithNullableAndEnum(checked, op, tests)
            Next
            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestBinaryOperator_AllTypes_Comparisons(checked As Boolean, result As String)
            Dim ops() As String = {"=", "<>", "<", "<=", ">=", ">"}

            Dim tests As New List(Of ExpressionTreeTest)
            For Each op In ops
                TestBinaryOperator_AllTypesWithNullableAndEnum(checked, op, tests)
            Next
            For Each op In ops
                TestBinaryOperator_AllTypesWithNullableAndEnum_Bool(checked, op, tests)
            Next

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestBinaryOperator_AllTypes_IsIsNot(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)
            TestBinaryOperator_AddTestsForType_BoolObject("String", "Is", tests)
            TestBinaryOperator_AddTestsForType_BoolObject("String", "IsNot", tests)
            TestBinaryOperator_AddTestsForType_BoolObject("Object", "Is", tests)
            TestBinaryOperator_AddTestsForType_BoolObject("Object", "IsNot", tests)
            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestBinaryOperator_ConcatenatePlus(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            For Each type1 In _allTypes
                TestBinaryOperator_AddTestsForType(type1, "String", "String", "&", tests)
                TestBinaryOperator_AddTestsForType(type1, "String", "String", "+", tests)
                TestBinaryOperator_AddTestsForType("String", type1, "String", "&", tests)
                TestBinaryOperator_AddTestsForType("String", type1, "String", "+", tests)
            Next
            TestBinaryOperator_AddTestsForType("Object", "Object", "Object", "&", tests)

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestBinaryOperator_Like(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            Dim index As Integer = 0
            For Each type1 In _allTypes
                Select Case index Mod 4
                    Case 0, 3
                        TestBinaryOperator_AddTestsForType(type1, "String", "Boolean", "Like", tests)
                        TestBinaryOperator_AddTestsForType("Object", type1, "Boolean", "Like", tests)
                    Case Else
                        TestBinaryOperator_AddTestsForType("String", type1, "Boolean", "Like", tests)
                        TestBinaryOperator_AddTestsForType(type1, "Object", "Boolean", "Like", tests)
                End Select
                index += 1
            Next
            TestBinaryOperator_AddTestsForType("Object", "Object", "Object", "Like", tests)

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestBinaryOperator_DateAsOperand(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            Dim flag = True
            For Each op In {">", "<", "<=", ">=", "+", "-"}
                If op <> "-" Then
                    If flag Then
                        TestBinaryOperator_AddTestsForType("String", "Date", "Object", op, tests)
                        TestBinaryOperator_AddTestsForType("Date?", "String", "Object", op, tests)
                        TestBinaryOperator_AddTestsForType("Date", "Object", "Object", op, tests)
                        TestBinaryOperator_AddTestsForType("Object", "Date?", "Object", op, tests)
                    Else
                        TestBinaryOperator_AddTestsForType("Date", "String", "Object", op, tests)
                        TestBinaryOperator_AddTestsForType("String", "Date?", "Object", op, tests)
                        TestBinaryOperator_AddTestsForType("Object", "Date", "Object", op, tests)
                        TestBinaryOperator_AddTestsForType("Date?", "Object", "Object", op, tests)
                    End If
                Else
                    TestBinaryOperator_AddTestsForType("Date", "Date", "TimeSpan", op, tests)
                    TestBinaryOperator_AddTestsForType("Date", "Date?", "TimeSpan", op, tests)
                    TestBinaryOperator_AddTestsForType("Date?", "Date", "TimeSpan?", op, tests)
                    TestBinaryOperator_AddTestsForType("Date?", "Date?", "TimeSpan?", op, tests)
                    TestBinaryOperator_AddTestsForType("Date?", "Date?", "TimeSpan", op, tests)
                End If

                TestBinaryOperator_AddTestsForType("Date", "Date", "Object", op, tests)
                TestBinaryOperator_AddTestsForType("Date", "Date?", "Object", op, tests)
                TestBinaryOperator_AddTestsForType("Date?", "Date", "Object", op, tests)
                TestBinaryOperator_AddTestsForType("Date?", "Date?", "Object", op, tests)

                flag = Not flag
            Next

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

#Region "User Defined Binary Operator"

        Public Sub TestBinaryOperator_UserDefinedBinaryOperator(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            Dim index As Integer = 0
            For Each suffix1 In {"", "?"}
                For Each suffix2 In {"", "?"}
                    Dim type As String = "UserDefinedBinaryOperator" & index

                    For Each op In {"+", "-", "*", "/", "\", "Mod", "^", "=", "<>", "<", ">", "<=", ">=", "Like", "&", "And", "Or", "Xor"}
                        TestBinaryOperator_AddTestsForTypeWithNullable(type, op, tests)
                    Next

                    For Each op In {"<<", ">>"}
                        TestBinaryOperator_AddTestsForType(type, "Integer", type + "?", op, tests)
                        TestBinaryOperator_AddTestsForType(type + "?", "Integer", type, op, tests)
                    Next

                    index += 1
                Next
            Next

            tests(0).Prefix = <![CDATA[]]>
            index = 0
            For Each suffix1 In {"", "?"}
                For Each suffix2 In {"", "?"}
                    tests(0).Prefix.Value = tests(0).Prefix.Value & vbLf &
                        String.Format(ExpTreeTestResources.UserDefinedBinaryOperators,
                                      suffix1, suffix2, index)
                    index += 1
                Next
            Next

            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

#End Region

#Region "Expression(Of Func(Of Type[?], Type[?], Type[?])) <= Function(x, y) x <op> y"

        Private Sub TestBinaryOperator_AllTypesWithNullableAndEnum(checked As Boolean, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("SByte", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("Byte", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("Short", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("UShort", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("Integer", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("UInteger", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("Long", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum("ULong", operation, list)

            TestBinaryOperator_AddTestsForTypeWithNullable("Boolean", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable("Single", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable("Double", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable("Decimal", operation, list)

            TestBinaryOperator_AddTestsForType("String", operation, list)
            TestBinaryOperator_AddTestsForType("Object", operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForTypeWithNullable(type As String, operation As String, list As List(Of ExpressionTreeTest))
            Dim nullable As String = type + "?"
            TestBinaryOperator_AddTestsForType(type, type, type, operation, list)
            TestBinaryOperator_AddTestsForType(type, type, nullable, operation, list)
            TestBinaryOperator_AddTestsForType(nullable, type, type, operation, list)
            TestBinaryOperator_AddTestsForType(type, nullable, nullable, operation, list)
            TestBinaryOperator_AddTestsForType(nullable, nullable, nullable, operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForType(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForType(type, type, type, operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForTypeWithNullableAndEnum(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForTypeWithNullable(type, operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable(GetEnumTypeName(type), operation, list)
        End Sub
#End Region

#Region "Expression(Of Func(Of Type[?], Type[?], Boolean[?])) <= Function(x, y) x <op> y"

        Private Sub TestBinaryOperator_AllTypesWithNullableAndEnum_Bool(checked As Boolean, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("SByte", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("Byte", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("Short", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("UShort", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("Integer", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("UInteger", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("Long", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool("ULong", operation, list)

            TestBinaryOperator_AddTestsForTypeWithNullable_Bool("Boolean", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable_Bool("Single", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable_Bool("Double", operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable_Bool("Decimal", operation, list)

            TestBinaryOperator_AddTestsForType_Bool("String", operation, list)
            TestBinaryOperator_AddTestsForType_Bool("Object", operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForTypeWithNullableAndEnum_Bool(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForTypeWithNullable_Bool(type, operation, list)
            TestBinaryOperator_AddTestsForTypeWithNullable_Bool(GetEnumTypeName(type), operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForTypeWithNullable_Bool(type As String, operation As String, list As List(Of ExpressionTreeTest))
            Dim nullable As String = type + "?"
            TestBinaryOperator_AddTestsForType(type, type, "Boolean", operation, list)
            TestBinaryOperator_AddTestsForType(type, type, "Boolean?", operation, list)
            TestBinaryOperator_AddTestsForType(type, nullable, "Boolean", operation, list)
            TestBinaryOperator_AddTestsForType(type, nullable, "Boolean?", operation, list)
            TestBinaryOperator_AddTestsForType(nullable, type, "Boolean", operation, list)
            TestBinaryOperator_AddTestsForType(nullable, type, "Boolean?", operation, list)
            TestBinaryOperator_AddTestsForType(nullable, nullable, "Boolean", operation, list)
            TestBinaryOperator_AddTestsForType(nullable, nullable, "Boolean?", operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForType_Bool(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForType(type, type, "Boolean", operation, list)
            TestBinaryOperator_AddTestsForType(type, type, "Boolean?", operation, list)
        End Sub

#End Region

#Region "Expression(Of Func(Of Type, Type, {Bool|Bool?|Object})) <= Function(x, y) x <op> y"

        Private Sub TestBinaryOperator_AddTestsForType_BoolObject(type As String, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForType_BoolObject(type, type, operation, list)
        End Sub

        Private Sub TestBinaryOperator_AddTestsForType_BoolObject(type1 As String, type2 As String, operation As String, list As List(Of ExpressionTreeTest))
            TestBinaryOperator_AddTestsForType(type1, type2, "Object", operation, list)
            TestBinaryOperator_AddTestsForType(type1, type2, "Boolean", operation, list)
            TestBinaryOperator_AddTestsForType(type1, type2, "Boolean?", operation, list)
        End Sub

#End Region

        Private Sub TestBinaryOperator_AddTestsForType(type1 As String, type2 As String, type3 As String, operation As String, list As List(Of ExpressionTreeTest))
            Dim descr1 = String.Format("-=-=-=-=-=-=-=-=- {{0}} {0} {{1}} => {{2}} -=-=-=-=-=-=-=-=-", operation)

            list.Add(New ExpressionTreeTest With {.Description = String.Format(descr1, type1, type2, type3),
                                                  .ExpressionTypeArgument =
                                                            String.Format("Func(Of {0}, {1}, {2})", type1, type2, type3),
                                                  .Lambda = String.Format("Function(x, y) x {0} y", operation)})
        End Sub

#End Region

#End Region

#Region "Conversions"

        <Fact>
        Public Sub NothingLiteralConversions_Unchecked()
            TestConversion_NothingLiteral(False, ExpTreeTestResources.CheckedAndUncheckedNothingConversions)
        End Sub

        <Fact>
        Public Sub NothingLiteralConversions_Checked()
            TestConversion_NothingLiteral(True, ExpTreeTestResources.CheckedAndUncheckedNothingConversions)
        End Sub

        <Fact>
        Public Sub NothingLiteralConversionsDate_CheckedUnchecked()

            Dim source = <compilation>
                             <%= _exprTesting %>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Public Class TestClass
    Public Sub Test()
        Console.WriteLine("-=-=-=-=-=-=-=-=- Nothing -> Date -=-=-=-=-=-=-=-=-")
        Dim exprtree1 As Expression(Of Func(Of Date)) = Function() Nothing
        Console.WriteLine(exprtree1.Dump)
        Console.WriteLine("-=-=-=-=-=-=-=-=- CType(Nothing, Date) -> Date -=-=-=-=-=-=-=-=-")
        Dim exprtree2 As Expression(Of Func(Of Date)) = Function() CType(Nothing, Date)
        Console.WriteLine(exprtree2.Dump)
        Console.WriteLine("-=-=-=-=-=-=-=-=- DirectCast(Nothing, Date) -> Date -=-=-=-=-=-=-=-=-")
        Dim exprtree3 As Expression(Of Func(Of Date)) = Function() DirectCast(Nothing, Date)
        Console.WriteLine(exprtree3.Dump)
        Console.WriteLine("-=-=-=-=-=-=-=-=- Nothing -> Date? -=-=-=-=-=-=-=-=-")
        Dim exprtree4 As Expression(Of Func(Of Date?)) = Function() Nothing
        Console.WriteLine(exprtree4.Dump)
        Console.WriteLine("-=-=-=-=-=-=-=-=- CType(Nothing, Date?) -> Date? -=-=-=-=-=-=-=-=-")
        Dim exprtree5 As Expression(Of Func(Of Date?)) = Function() CType(Nothing, Date?)
        Console.WriteLine(exprtree5.Dump)
        Console.WriteLine("-=-=-=-=-=-=-=-=- DirectCast(Nothing, Date?) -> Date? -=-=-=-=-=-=-=-=-")
        Dim exprtree6 As Expression(Of Func(Of Date?)) = Function() DirectCast(Nothing, Date?)
        Console.Write(exprtree6.Dump.TrimEnd)

    End Sub
End Class

Module Form1
    Sub Main()
        Dim inst As New TestClass()
        inst.Test()
    End Sub
End Module
]]></file>
                         </compilation>

            Dim expected = <![CDATA[-=-=-=-=-=-=-=-=- Nothing -> Date -=-=-=-=-=-=-=-=-
Lambda(
  body {
    Constant(
      1/1/0001 12:00:00 AM
      type: System.DateTime
    )
  }
  return type: System.DateTime
  type: System.Func`1[System.DateTime]
)

-=-=-=-=-=-=-=-=- CType(Nothing, Date) -> Date -=-=-=-=-=-=-=-=-
Lambda(
  body {
    Constant(
      1/1/0001 12:00:00 AM
      type: System.DateTime
    )
  }
  return type: System.DateTime
  type: System.Func`1[System.DateTime]
)

-=-=-=-=-=-=-=-=- DirectCast(Nothing, Date) -> Date -=-=-=-=-=-=-=-=-
Lambda(
  body {
    Constant(
      1/1/0001 12:00:00 AM
      type: System.DateTime
    )
  }
  return type: System.DateTime
  type: System.Func`1[System.DateTime]
)

-=-=-=-=-=-=-=-=- Nothing -> Date? -=-=-=-=-=-=-=-=-
Lambda(
  body {
    Constant(
      null
      type: System.Nullable`1[System.DateTime]
    )
  }
  return type: System.Nullable`1[System.DateTime]
  type: System.Func`1[System.Nullable`1[System.DateTime]]
)

-=-=-=-=-=-=-=-=- CType(Nothing, Date?) -> Date? -=-=-=-=-=-=-=-=-
Lambda(
  body {
    Constant(
      null
      type: System.Nullable`1[System.DateTime]
    )
  }
  return type: System.Nullable`1[System.DateTime]
  type: System.Func`1[System.Nullable`1[System.DateTime]]
)

-=-=-=-=-=-=-=-=- DirectCast(Nothing, Date?) -> Date? -=-=-=-=-=-=-=-=-
Lambda(
  body {
    Constant(
      null
      type: System.Nullable`1[System.DateTime]
    )
  }
  return type: System.Nullable`1[System.DateTime]
  type: System.Func`1[System.Nullable`1[System.DateTime]]
)
]]>

            CompileAndVerify(source,
                          references:={Net40.References.SystemCore},
                          options:=TestOptions.ReleaseExe.WithOverflowChecks(True),
                          expectedOutput:=expected
            )

            CompileAndVerify(source,
                        references:={Net40.References.SystemCore},
                        options:=TestOptions.ReleaseExe.WithOverflowChecks(False),
                        expectedOutput:=expected
            )

        End Sub

        <Fact>
        Public Sub TypeParameterConversions_Unchecked()
            TestConversion_TypeParameters(False, ExpTreeTestResources.CheckedAndUncheckedTypeParameters)
        End Sub

        <Fact>
        Public Sub TypeParameterConversions_Checked()
            TestConversion_TypeParameters(True, ExpTreeTestResources.CheckedAndUncheckedTypeParameters)
        End Sub

        <Fact>
        Public Sub TypeConversions_Unchecked_Std_DirectTrySpecialized()
            TestConversion_TypeMatrix_Standard_DirectTrySpecialized(False, ExpTreeTestResources.UncheckedDirectTrySpecificConversions)
        End Sub

        <Fact>
        Public Sub TypeConversions_Checked_Std_DirectTrySpecialized()
            TestConversion_TypeMatrix_Standard_DirectTrySpecialized(True, ExpTreeTestResources.CheckedDirectTrySpecificConversions)
        End Sub

        <Fact>
        Public Sub TypeConversions_Unchecked_Std_ImplicitAndCType()
            TestConversion_TypeMatrix_Standard_ImplicitAndCType(False, ExpTreeTestResources.UncheckedCTypeAndImplicitConversionsEven, True)
            TestConversion_TypeMatrix_Standard_ImplicitAndCType(False, ExpTreeTestResources.UncheckedCTypeAndImplicitConversionsOdd, False)
        End Sub

        <Fact>
        Public Sub TypeConversions_Checked_Std_ImplicitAndCType()
            TestConversion_TypeMatrix_Standard_ImplicitAndCType(True, ExpTreeTestResources.CheckedCTypeAndImplicitConversionsEven, True)
            TestConversion_TypeMatrix_Standard_ImplicitAndCType(True, ExpTreeTestResources.CheckedCTypeAndImplicitConversionsOdd, False)
        End Sub

        <Fact>
        Public Sub TypeConversions_Unchecked_UserTypes()
            TestConversion_TypeMatrix_UserTypes(False, ExpTreeTestResources.CheckedAndUncheckedUserTypeConversions)
        End Sub

        <Fact>
        Public Sub TypeConversions_Checked_UserTypes()
            TestConversion_TypeMatrix_UserTypes(True, ExpTreeTestResources.CheckedAndUncheckedUserTypeConversions)
        End Sub

        <Fact>
        Public Sub TypeConversions_Unchecked_Narrowing_UserDefinedConversions()
            TestConversion_UserDefinedTypes_Narrowing(False, ExpTreeTestResources.CheckedAndUncheckedNarrowingUDC)
        End Sub

        <Fact>
        Public Sub TypeConversions_Checked_Narrowing_UserDefinedConversions()
            TestConversion_UserDefinedTypes_Narrowing(True, ExpTreeTestResources.CheckedAndUncheckedNarrowingUDC)
        End Sub

        <Fact>
        Public Sub TypeConversions_Unchecked_Widening_UserDefinedConversions()
            TestConversion_UserDefinedTypes_Widening(False, ExpTreeTestResources.CheckedAndUncheckedWideningUDC)
        End Sub

        <Fact>
        Public Sub TypeConversions_Checked_Widening_UserDefinedConversions()
            TestConversion_UserDefinedTypes_Widening(True, ExpTreeTestResources.CheckedAndUncheckedWideningUDC)
        End Sub

        <Fact>
        Public Sub ExpressionTrees_UDC_NullableAndConversion()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Public Structure Type01
    Public Shared Widening Operator CType(x As Integer) As Type01?
        Return Nothing
    End Operator
    Public Shared Widening Operator CType(x As Double?) As Type01
        Return Nothing
    End Operator
End Structure

Public Structure Type02
    Public Shared Widening Operator CType(x As Type02) As Integer?
        Return Nothing
    End Operator
End Structure

Public Structure Type03
    Public Shared Widening Operator CType(x As Type03?) As Double
        Return Nothing
    End Operator
End Structure



Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of SByte, Type01)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Single, Type01)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of SByte?, Type01)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Single?, Type01)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of SByte?, Type01?)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Single, Type01?)))).Dump)

        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Type02, SByte)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Type03, Single)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Type02, SByte?)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Type03, Single?)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Type02?, SByte?)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x, Expression(Of Func(Of Type03, Single)))).Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    x
    type: System.SByte
  )
  body {
    Convert(
      Convert(
        Convert(
          Parameter(
            x
            type: System.SByte
          )
          type: System.Double
        )
        method: System.Nullable`1[System.Double] op_Implicit(Double) in System.Nullable`1[System.Double]
        type: System.Nullable`1[System.Double]
      )
      method: Type01 op_Implicit(System.Nullable`1[System.Double]) in Type01
      type: Type01
    )
  }
  return type: Type01
  type: System.Func`2[System.SByte,Type01]
)

Lambda(
  Parameter(
    x
    type: System.Single
  )
  body {
    Convert(
      Convert(
        Convert(
          Parameter(
            x
            type: System.Single
          )
          type: System.Double
        )
        method: System.Nullable`1[System.Double] op_Implicit(Double) in System.Nullable`1[System.Double]
        type: System.Nullable`1[System.Double]
      )
      method: Type01 op_Implicit(System.Nullable`1[System.Double]) in Type01
      type: Type01
    )
  }
  return type: Type01
  type: System.Func`2[System.Single,Type01]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[System.SByte]
  )
  body {
    Convert(
      Convert(
        Parameter(
          x
          type: System.Nullable`1[System.SByte]
        )
        Lifted
        LiftedToNull
        type: System.Nullable`1[System.Double]
      )
      method: Type01 op_Implicit(System.Nullable`1[System.Double]) in Type01
      type: Type01
    )
  }
  return type: Type01
  type: System.Func`2[System.Nullable`1[System.SByte],Type01]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[System.Single]
  )
  body {
    Convert(
      Convert(
        Parameter(
          x
          type: System.Nullable`1[System.Single]
        )
        Lifted
        LiftedToNull
        type: System.Nullable`1[System.Double]
      )
      method: Type01 op_Implicit(System.Nullable`1[System.Double]) in Type01
      type: Type01
    )
  }
  return type: Type01
  type: System.Func`2[System.Nullable`1[System.Single],Type01]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[System.SByte]
  )
  body {
    Convert(
      Convert(
        Convert(
          Parameter(
            x
            type: System.Nullable`1[System.SByte]
          )
          Lifted
          LiftedToNull
          type: System.Nullable`1[System.Double]
        )
        method: Type01 op_Implicit(System.Nullable`1[System.Double]) in Type01
        type: Type01
      )
      Lifted
      LiftedToNull
      type: System.Nullable`1[Type01]
    )
  }
  return type: System.Nullable`1[Type01]
  type: System.Func`2[System.Nullable`1[System.SByte],System.Nullable`1[Type01]]
)

Lambda(
  Parameter(
    x
    type: System.Single
  )
  body {
    Convert(
      Convert(
        Convert(
          Convert(
            Parameter(
              x
              type: System.Single
            )
            type: System.Double
          )
          method: System.Nullable`1[System.Double] op_Implicit(Double) in System.Nullable`1[System.Double]
          type: System.Nullable`1[System.Double]
        )
        method: Type01 op_Implicit(System.Nullable`1[System.Double]) in Type01
        type: Type01
      )
      Lifted
      LiftedToNull
      type: System.Nullable`1[Type01]
    )
  }
  return type: System.Nullable`1[Type01]
  type: System.Func`2[System.Single,System.Nullable`1[Type01]]
)

Lambda(
  Parameter(
    x
    type: Type02
  )
  body {
    ConvertChecked(
      ConvertChecked(
        ConvertChecked(
          Parameter(
            x
            type: Type02
          )
          method: System.Nullable`1[System.Int32] op_Implicit(Type02) in Type02
          type: System.Nullable`1[System.Int32]
        )
        method: Int32 op_Explicit(System.Nullable`1[System.Int32]) in System.Nullable`1[System.Int32]
        type: System.Int32
      )
      type: System.SByte
    )
  }
  return type: System.SByte
  type: System.Func`2[Type02,System.SByte]
)

Lambda(
  Parameter(
    x
    type: Type03
  )
  body {
    Convert(
      Convert(
        Convert(
          Parameter(
            x
            type: Type03
          )
          Lifted
          LiftedToNull
          type: System.Nullable`1[Type03]
        )
        method: Double op_Implicit(System.Nullable`1[Type03]) in Type03
        type: System.Double
      )
      type: System.Single
    )
  }
  return type: System.Single
  type: System.Func`2[Type03,System.Single]
)

Lambda(
  Parameter(
    x
    type: Type02
  )
  body {
    ConvertChecked(
      ConvertChecked(
        Parameter(
          x
          type: Type02
        )
        method: System.Nullable`1[System.Int32] op_Implicit(Type02) in Type02
        type: System.Nullable`1[System.Int32]
      )
      Lifted
      LiftedToNull
      type: System.Nullable`1[System.SByte]
    )
  }
  return type: System.Nullable`1[System.SByte]
  type: System.Func`2[Type02,System.Nullable`1[System.SByte]]
)

Lambda(
  Parameter(
    x
    type: Type03
  )
  body {
    Convert(
      Convert(
        Convert(
          Convert(
            Parameter(
              x
              type: Type03
            )
            Lifted
            LiftedToNull
            type: System.Nullable`1[Type03]
          )
          method: Double op_Implicit(System.Nullable`1[Type03]) in Type03
          type: System.Double
        )
        type: System.Single
      )
      method: System.Nullable`1[System.Single] op_Implicit(Single) in System.Nullable`1[System.Single]
      type: System.Nullable`1[System.Single]
    )
  }
  return type: System.Nullable`1[System.Single]
  type: System.Func`2[Type03,System.Nullable`1[System.Single]]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[Type02]
  )
  body {
    ConvertChecked(
      ConvertChecked(
        Convert(
          Parameter(
            x
            type: System.Nullable`1[Type02]
          )
          Lifted
          type: Type02
        )
        method: System.Nullable`1[System.Int32] op_Implicit(Type02) in Type02
        type: System.Nullable`1[System.Int32]
      )
      Lifted
      LiftedToNull
      type: System.Nullable`1[System.SByte]
    )
  }
  return type: System.Nullable`1[System.SByte]
  type: System.Func`2[System.Nullable`1[Type02],System.Nullable`1[System.SByte]]
)

Lambda(
  Parameter(
    x
    type: Type03
  )
  body {
    Convert(
      Convert(
        Convert(
          Parameter(
            x
            type: Type03
          )
          Lifted
          LiftedToNull
          type: System.Nullable`1[Type03]
        )
        method: Double op_Implicit(System.Nullable`1[Type03]) in Type03
        type: System.Double
      )
      type: System.Single
    )
  }
  return type: System.Single
  type: System.Func`2[Type03,System.Single]
)
]]>)
        End Sub

#Region "Conversions: User Defined Types"

        Public Sub TestConversion_UserDefinedTypes_Narrowing(checked As Boolean, result As String)
            'Dim tests As New List(Of ExpressionTreeTest)
            'TestConversion_UserDefinedTypes_ClassClass(True, tests)
            'TestConversion_UserDefinedTypes_ClassStruct(True, tests)
            'TestConversion_UserDefinedTypes_StructClass(True, tests)
            'TestConversion_UserDefinedTypes_StructStruct(True, tests)
            'TestExpressionTrees(checked, result, tests.ToArray())

            Dim source = <compilation>
                             <file name="a.vb">
                                 <%= ExpTreeTestResources.TestConversion_Narrowing_UDC %>
                             </file>
                             <%= _exprTesting %>
                         </compilation>

            CompileAndVerify(source,
                             references:={Net40.References.SystemCore},
                             options:=TestOptions.ReleaseExe.WithOverflowChecks(checked),
                             expectedOutput:=result.Trim
            ).VerifyDiagnostics()
        End Sub

        Public Sub TestConversion_UserDefinedTypes_Widening(checked As Boolean, result As String)
            'Dim tests As New List(Of ExpressionTreeTest)
            'TestConversion_UserDefinedTypes_ClassClass(False, tests)
            'TestConversion_UserDefinedTypes_ClassStruct(False, tests)
            'TestConversion_UserDefinedTypes_StructClass(False, tests)
            'TestConversion_UserDefinedTypes_StructStruct(False, tests)
            'TestExpressionTrees(checked, result, tests.ToArray())

            Dim source = <compilation>
                             <file name="a.vb">
                                 <%= ExpTreeTestResources.TestConversion_Widening_UDC %>
                             </file>
                             <%= _exprTesting %>
                         </compilation>

            CompileAndVerify(source,
                             references:={Net40.References.SystemCore},
                             options:=TestOptions.ReleaseExe.WithOverflowChecks(checked),
                             expectedOutput:=result.Trim
            ).VerifyDiagnostics()
        End Sub

#Region "Generation"

        Private Class OperatorDescriptor
            Public TypeFrom As String
            Public TypeFromLifted As Boolean = False
            Public TypeTo As String
            Public TypeToLifted As Boolean = False
            Public IsWidenning As Boolean

            Public ReadOnly Property Keyword As String
                Get
                    Return If(IsWidenning, "Widening", "Narrowing")
                End Get
            End Property

            Public ReadOnly Property Code As String
                Get
                    Dim builder As New StringBuilder
                    builder.AppendFormat("  Public Shared {2} Operator CType(x As {0}{3}) As {1}{4}",
                                         TypeFrom, TypeTo, Keyword,
                                         If(TypeFromLifted, "?", ""),
                                         If(TypeToLifted, "?", "")).AppendLine()
                    builder.AppendLine("    Return Nothing")
                    builder.AppendLine("  End Operator")
                    Return builder.ToString()
                End Get
            End Property

            Public Function AssignTypes(typeFrom As String, typeTo As String) As String
                Me.TypeFrom = typeFrom
                Me.TypeTo = typeTo
                Return String.Format("{1}{3} -> {2}{4}", Keyword, Me.TypeFrom, Me.TypeTo,
                                     If(TypeFromLifted, "?", ""), If(TypeToLifted, "?", ""))
            End Function
        End Class

        Private Class TypeDescriptor
            Public Type As String = ""
            Public IsStructure As Boolean
            Private ReadOnly _operators As New List(Of OperatorDescriptor)

            Public Function WithOperators(ParamArray ops() As OperatorDescriptor) As TypeDescriptor
                _operators.Clear()
                _operators.AddRange(ops)
                Return Me
            End Function

            Public ReadOnly Property Keyword As String
                Get
                    Return If(IsStructure, "Structure", "Class")
                End Get
            End Property

            Public ReadOnly Property Code As String
                Get
                    Dim builder As New StringBuilder
                    builder.AppendFormat("Public {0} {1}", Keyword, Type)
                    builder.AppendLine()
                    For Each op In _operators
                        builder.Append(op.Code)
                    Next
                    builder.AppendFormat("End {0}", Keyword)
                    builder.AppendLine()
                    Return builder.ToString()
                End Get
            End Property

            Public Function AssignTypes(type As String, typeFrom As String, typeTo As String) As String
                Dim builder As New StringBuilder
                Me.Type = type
                builder.Append(Keyword).Append(" ").Append(Me.Type).Append("{")
                For i = 0 To _operators.Count - 1
                    builder.Append(If(i > 0, ", ", "")).Append(_operators(i).AssignTypes(typeFrom, typeTo))
                Next
                builder.Append("}")
                Return builder.ToString()
            End Function
        End Class

        Private Sub TestConversion_UserDefinedTypes_StructStruct(isNarrowing As Boolean, list As List(Of ExpressionTreeTest))
            Dim type1 As New TypeDescriptor With {.IsStructure = True}
            Dim type2 As New TypeDescriptor With {.IsStructure = True}

            Dim ops = {New OperatorDescriptor With {.IsWidenning = Not isNarrowing},
                       New OperatorDescriptor With {.IsWidenning = Not isNarrowing, .TypeToLifted = True},
                       New OperatorDescriptor With {.IsWidenning = Not isNarrowing, .TypeFromLifted = True},
                       New OperatorDescriptor With {.IsWidenning = Not isNarrowing, .TypeFromLifted = True, .TypeToLifted = True}}

            For i = 0 To ops.Count - 1
                TestConversion_UserDefinedTypes_OneOp(isNarrowing, type1, type2, ops(i), list)
                For j = i + 1 To ops.Count - 1
                    TestConversion_UserDefinedTypes_TwoOps(isNarrowing, type1, type2, ops(i), ops(j), list)
                Next
            Next
        End Sub

        Private Sub TestConversion_UserDefinedTypes_ClassStruct(isNarrowing As Boolean, list As List(Of ExpressionTreeTest))
            TestConversion_UserDefinedTypes_TwoOps(isNarrowing,
                                                   New TypeDescriptor With {.IsStructure = False},
                                                   New TypeDescriptor With {.IsStructure = True},
                                                   New OperatorDescriptor With {.IsWidenning = Not isNarrowing},
                                                   New OperatorDescriptor With {.IsWidenning = Not isNarrowing, .TypeToLifted = True},
                                                   list)
        End Sub

        Private Sub TestConversion_UserDefinedTypes_StructClass(isNarrowing As Boolean, list As List(Of ExpressionTreeTest))
            TestConversion_UserDefinedTypes_TwoOps(isNarrowing,
                                                   New TypeDescriptor With {.IsStructure = True},
                                                   New TypeDescriptor With {.IsStructure = False},
                                                   New OperatorDescriptor With {.IsWidenning = Not isNarrowing},
                                                   New OperatorDescriptor With {.IsWidenning = Not isNarrowing, .TypeFromLifted = True},
                                                   list)
        End Sub

        Private Sub TestConversion_UserDefinedTypes_ClassClass(isNarrowing As Boolean, list As List(Of ExpressionTreeTest))
            TestConversion_UserDefinedTypes_OneOp(isNarrowing,
                                                  New TypeDescriptor With {.IsStructure = False},
                                                  New TypeDescriptor With {.IsStructure = False},
                                                  New OperatorDescriptor With {.IsWidenning = Not isNarrowing},
                                                  list)
        End Sub

        Private Sub TestConversion_UserDefinedTypes_TwoOps(isNarrowing As Boolean,
                                                           type1 As TypeDescriptor, type2 As TypeDescriptor,
                                                           op1 As OperatorDescriptor, op2 As OperatorDescriptor,
                                                           list As List(Of ExpressionTreeTest))

            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(op1), type2.WithOperators(), isNarrowing, list)
            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(), type2.WithOperators(op1), isNarrowing, list)

            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(op2), type2.WithOperators(), isNarrowing, list)
            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(), type2.WithOperators(op2), isNarrowing, list)

            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(op1, op2), type2.WithOperators(), isNarrowing, list)
            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(op1), type2.WithOperators(op2), isNarrowing, list)
            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(), type2.WithOperators(op1, op2), isNarrowing, list)
        End Sub

        Private Sub TestConversion_UserDefinedTypes_OneOp(isNarrowing As Boolean,
                                                          type1 As TypeDescriptor, type2 As TypeDescriptor,
                                                          op As OperatorDescriptor, list As List(Of ExpressionTreeTest))

            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(op), type2.WithOperators(), isNarrowing, list)
            TestConversion_UserDefinedTypes_TypeType(type1.WithOperators(), type2.WithOperators(op), isNarrowing, list)
        End Sub

        Private Sub TestConversion_UserDefinedTypes_TypeType(type1 As TypeDescriptor, type2 As TypeDescriptor, isNarrowing As Boolean, list As List(Of ExpressionTreeTest))
            Dim index = list.Count

            Dim typeFrom = "From" + index.ToString()
            Dim typeTo = "To" + index.ToString()

            Dim typeFromDescr = type1.AssignTypes(typeFrom, typeFrom, typeTo)
            Dim typeToDescr = type2.AssignTypes(typeTo, typeFrom, typeTo)

            For Each typeFromInstance In If(type1.IsStructure, {typeFrom, typeFrom & "?"}, {typeFrom})
                For Each typeToInstance In If(type2.IsStructure, {typeTo, typeTo & "?"}, {typeTo})
                    TestConversion_UserDefinedTypes_OneTest(typeFromDescr, typeToDescr, typeFromInstance, typeToInstance, isNarrowing, list)
                Next
            Next

            list(index).Prefix = <![CDATA[]]>
            list(index).Prefix.Value = type1.Code & type2.Code
        End Sub

        Private Sub TestConversion_UserDefinedTypes_OneTest(typeFromDescr As String, typeToDescr As String,
                                                            typeFrom As String, typeTo As String,
                                                            isNarrowing As Boolean, list As List(Of ExpressionTreeTest))

            Dim description = If(isNarrowing,
                                 String.Format("-=-=-=-=-=-=-=-=- CType({0}, {1}) for {2} and {3} -=-=-=-=-=-=-=-=-",
                                               typeFrom, typeTo, typeFromDescr, typeToDescr),
                                 String.Format("-=-=-=-=-=-=-=-=- implicit {0} -> {1} for {2} and {3} -=-=-=-=-=-=-=-=-",
                                               typeFrom, typeTo, typeFromDescr, typeToDescr))

            Dim opPattern = If(isNarrowing, ("CType({0}, " & typeTo & ")"), "{0}")

            list.Add(New ExpressionTreeTest With {.Description = description,
                                                  .ExpressionTypeArgument = String.Format("Func(Of {0}, {1})", typeFrom, typeTo),
                                                  .Lambda = String.Format("Function(x As {0}) {1}", typeFrom, String.Format(opPattern, "x", typeTo))})
        End Sub

#End Region

#End Region

#Region "Conversions: Type Matrix"

        Public Sub TestConversion_TypeMatrix_UserTypes(checked As Boolean, result As String)
            '            Dim tests As New List(Of ExpressionTreeTest)
            '            For Each type1 In {"Object", "String", "Struct1", "Struct1?", "Clazz1", "Clazz2"}
            '                For Each type2 In {"Object", "String", "Struct1", "Struct1?", "Clazz1", "Clazz2"}
            '                    TestConversion_TypeMatrix_Implicit(type1, type2, tests)
            '                    TestConversion_TypeMatrix_CType(type1, type2, tests)
            '                    TestConversion_TypeMatrix_DirectCast(type1, type2, tests)
            '                    TestConversion_TypeMatrix_TryCast(type1, type2, tests)
            '                    TestConversion_TypeMatrix_Specific(type1, type2, tests)
            '                Next
            '            Next
            '            tests(0).Prefix = <![CDATA[
            'Public Class Clazz1
            'End Class
            'Public Class Clazz2
            '    Inherits Clazz1
            'End Class
            'Public Structure Struct1
            'End Structure
            ']]>
            '            tests(0).Prefix.Value = tests(0).Prefix.Value & vbLf & EnumDeclarations.Value
            '            TestExpressionTrees(checked, result, tests.ToArray())

            Dim source = <compilation>
                             <file name="a.vb">
                                 <%= ExpTreeTestResources.TestConversion_TypeMatrix_UserTypes %>
                             </file>
                             <%= _exprTesting %>
                         </compilation>

            CompileAndVerify(source,
                             references:={Net40.References.SystemCore},
                             options:=TestOptions.ReleaseExe.WithOverflowChecks(checked),
                             expectedOutput:=result.Trim
            ).VerifyDiagnostics(Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"))
        End Sub

        Public Sub TestConversion_TypeMatrix_Standard_ImplicitAndCType(checked As Boolean, result As String, even As Boolean)
            Dim tests As New List(Of ExpressionTreeTest)

            ' Primitive types
            For Each type1 In _allTypes
                Dim i As Integer = 0 ' Generate only calls to even or odd types from type2
                For Each type2 In _allTypes
                    i += 1
                    If (i Mod 2) = If(even, 0, 1) Then
                        Dim isFloatingType = (type2 = "Single") OrElse (type2 = "Double")
                        Dim isEven = ((i >> 1) Mod 2) = 0

                        If isEven OrElse isFloatingType Then
                            TestConversion_TypeMatrix_Implicit(type1, type2, tests)
                        End If
                        If Not isEven OrElse isFloatingType Then
                            TestConversion_TypeMatrix_CType(type1, type2, tests)
                        End If
                    End If
                Next
            Next

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Public Sub TestConversion_TypeMatrix_Standard_DirectTrySpecialized(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            ' Primitive types
            For Each type1 In _allTypes
                For Each type2 In _allTypes
                    TestConversion_TypeMatrix_DirectCast(type1, type2, tests)
                    TestConversion_TypeMatrix_TryCast(type1, type2, tests)
                    TestConversion_TypeMatrix_Specific(type1, type2, tests)
                Next
            Next

            tests(0).Prefix = s_enumDeclarations
            TestExpressionTrees(checked, result, tests.ToArray(),
                                diagnostics:={
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x"),
                                    Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "x")})
        End Sub

        Private Function IsDateVersusPrimitive(type1 As String, type2 As String) As Boolean
            Return type1 <> type2 AndAlso
                   ((type2 = "Date" OrElse type2 = "Date?") AndAlso IsPrimitiveStructType(type1) OrElse
                    (type1 = "Date" OrElse type1 = "Date?") AndAlso IsPrimitiveStructType(type2))
        End Function

        Private Sub TestConversion_TypeMatrix_Implicit(type1 As String, type2 As String, list As List(Of ExpressionTreeTest))
            If IsDateVersusPrimitive(type1, type2) Then
                Return
            End If

            TestConversion_TwoTypesAndExpression(type1, type2, "{0}", list)
        End Sub

        Private Sub TestConversion_TypeMatrix_CType(type1 As String, type2 As String, list As List(Of ExpressionTreeTest))
            If IsDateVersusPrimitive(type1, type2) Then
                Return
            End If

            TestConversion_TwoTypesAndExpression(type1, type2, "CType({0}, {1})", list)
        End Sub

        Private Sub TestConversion_TypeMatrix_DirectCast(type1 As String, type2 As String, list As List(Of ExpressionTreeTest))
            If IsPrimitiveStructType(type1) Then
                If Not IsPrimitiveStructType(type1) OrElse (type1 = type2 AndAlso type1 <> "Single" AndAlso type1 <> "Double") Then
                    TestConversion_TwoTypesAndExpression(type1, type2, "DirectCast({0}, {1})", list)
                End If

            ElseIf type1 = "String" Then
                If Not IsPrimitiveStructType(type2) Then
                    TestConversion_TwoTypesAndExpression(type1, type2, "DirectCast({0}, {1})", list)
                End If

            Else
                TestConversion_TwoTypesAndExpression(type1, type2, "DirectCast({0}, {1})", list)
            End If
        End Sub

        Private Sub TestConversion_TypeMatrix_TryCast(type1 As String, type2 As String, list As List(Of ExpressionTreeTest))
            Select Case type2
                Case "Struct1", "Struct1?"
lNothing:
                ' Nothing

                Case "String"
                    If IsPrimitiveStructType(type1) Then
                        GoTo lNothing
                    End If

                Case Else
                    If IsPrimitiveStructType(type2) Then
                        GoTo lNothing
                    End If

                    TestConversion_TwoTypesAndExpression(type1, type2, "TryCast({0}, {1})", list)
            End Select
        End Sub

        Private Sub TestConversion_TypeMatrix_Specific(type1 As String, type2 As String, list As List(Of ExpressionTreeTest))
            Select Case type2
                Case "Boolean"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CBool({0})", list)

                Case "Byte"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CByte({0})", list)

                Case "Char"
                    TestConversion_TwoTypesAndExpression(type1, type2, "CChar({0})", list)

                Case "Date"
                    If IsPrimitiveStructType(type1) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CDate({0})", list)

                Case "Double"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CDbl({0})", list)

                Case "Decimal"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CDec({0})", list)

                Case "Integer"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CInt({0})", list)

                Case "Long"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CLng({0})", list)

                Case "Object"
                    TestConversion_TwoTypesAndExpression(type1, type2, "CObj({0})", list)

                Case "Short"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CShort({0})", list)

                Case "Single"
                    If type1.StartsWith("Date", StringComparison.Ordinal) Then
                        GoTo lNothing
                    End If
                    TestConversion_TwoTypesAndExpression(type1, type2, "CSng({0})", list)

                Case "String"
                    TestConversion_TwoTypesAndExpression(type1, type2, "CStr({0})", list)

                Case Else
lNothing:
                    ' Nothing
            End Select
        End Sub

#End Region

#Region "Conversions: Type Parameters"

        Public Sub TestConversion_TypeParameters(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            Dim typeParameters = "(Of T, O As Clazz1, S As Structure, D As {Class, O})"
            Dim typeArguments = "(Of Object, Clazz1, Struct1, Clazz2)"

            ' Type Parameters <--> Type Parameter
            TestConversion_TypeParameters("O", "D", tests)
            TestConversion_TypeParameters("D", "O", tests)

            ' Type parameter -> Type
            For Each type1 In {"T", "S", "S?", "O", "D"}
                TestConversion_TypeParameters(type1, "Object", tests)
            Next
            TestConversion_TypeParameters("O", "Clazz1", tests)
            TestConversion_TwoTypesAndExpression("O", "Clazz2", "TryCast({0}, {1})", tests)
            TestConversion_TypeParameters("D", "Clazz1", tests)
            TestConversion_TwoTypesAndExpression("D", "Clazz2", "TryCast({0}, {1})", tests)

            ' Type -> Type parameter 
            For Each type1 In {"T", "S", "S?", "O", "D"}
                TestConversion_TypeParameters(type1, "Object", tests)
            Next
            TestConversion_TypeParameters("Object", "T", tests, False)
            TestConversion_TypeParameters("Object", "O", tests)
            TestConversion_TypeParameters("Clazz1", "O", tests)
            TestConversion_TwoTypesAndExpression("Clazz2", "O", "TryCast({0}, {1})", tests)
            TestConversion_TypeParameters("Object", "D", tests)
            TestConversion_TypeParameters("Clazz1", "D", tests)
            TestConversion_TwoTypesAndExpression("Clazz2", "D", "TryCast({0}, {1})", tests)

            ' Nothing -> Type parameter 
            For Each type1 In {"T", "S", "S?", "O", "D"}
                TestConversion_TwoTypesAndExpression("Object", type1, "Nothing", tests)
                TestConversion_TwoTypesAndExpression("Object", type1, "CType(Nothing, {1})", tests)
                TestConversion_TwoTypesAndExpression("Object", type1, "DirectCast(Nothing, {1})", tests)
                If type1 = "O" OrElse type1 = "D" Then
                    TestConversion_TwoTypesAndExpression("Object", type1, "TryCast(Nothing, {1})", tests)
                End If
            Next

            tests(0).Prefix = <![CDATA[
Public Class Clazz1
End Class
Public Class Clazz2
    Inherits Clazz1
End Class
Public Structure Struct1
End Structure
]]>

            tests(0).Prefix.Value = tests(0).Prefix.Value & vbLf & s_enumDeclarations.Value

            TestExpressionTrees(checked, result, tests.ToArray(), typeParameters, typeArguments)
        End Sub

        Private Sub TestConversion_TypeParameters(type1 As String, type2 As String, list As List(Of ExpressionTreeTest), Optional useTryCast As Boolean = True)
            TestConversion_TwoTypesAndExpression(type1, type2, "{0}", list)
            TestConversion_TwoTypesAndExpression(type1, type2, "CType({0}, {1})", list)
            TestConversion_TwoTypesAndExpression(type1, type2, "DirectCast({0}, {1})", list)
            If useTryCast Then
                TestConversion_TwoTypesAndExpression(type1, type2, "TryCast({0}, {1})", list)
            End If
        End Sub

        Private Sub TestConversion_TwoTypesAndExpression(type1 As String, type2 As String, pattern As String, list As List(Of ExpressionTreeTest))
            list.Add(New ExpressionTreeTest With {.Description =
                                                            String.Format("-=-=-=-=-=-=-=-=- {0} -> {1} -=-=-=-=-=-=-=-=-",
                                                                          String.Format(pattern, type1, type2), type2),
                                                  .ExpressionTypeArgument =
                                                            String.Format("Func(Of {0}, {1})", type1, type2),
                                                  .Lambda = String.Format("Function(x As {0}) {1}",
                                                                          type1, String.Format(pattern, "x", type2))})
        End Sub

#End Region

#Region "Conversions: Nothing Literal"

        Public Sub TestConversion_NothingLiteral(checked As Boolean, result As String)
            Dim tests As New List(Of ExpressionTreeTest)

            For Each type1 In _allTypesWithoutDate.Concat({"Clazz1", "Struct1", "Clazz2(Of Clazz1, String)", "Struct2(Of Struct1)"})
                TestConversion_NothingLiteral(type1, "Nothing", tests)
                TestConversion_NothingLiteral(type1, "CType(Nothing, " + type1 + ")", tests)
                TestConversion_NothingLiteral(type1, "DirectCast(Nothing, " + type1 + ")", tests)
            Next

            For Each type1 In {"String", "Object", "Clazz1", "Clazz2(Of Clazz1, String)"}
                TestConversion_NothingLiteral(type1, "TryCast(Nothing, " + type1 + ")", tests)
            Next

            tests(0).Prefix = <![CDATA[
Class Clazz1
End Class

Class Clazz2(Of T, U)
End Class

Structure Struct1
End Structure

Structure Struct2(Of T)
End Structure
]]>

            tests(0).Prefix.Value = tests(0).Prefix.Value & vbLf & s_enumDeclarations.Value

            TestExpressionTrees(checked, result, tests.ToArray())
        End Sub

        Private Sub TestConversion_NothingLiteral(type As String, nothingLiteral As String, list As List(Of ExpressionTreeTest))
            Dim descr1 = String.Format("-=-=-=-=-=-=-=-=- {0} -> {{0}} -=-=-=-=-=-=-=-=-", nothingLiteral)

            list.Add(New ExpressionTreeTest With {.Description = String.Format(descr1, type),
                                                  .ExpressionTypeArgument =
                                                            String.Format("Func(Of {0})", type),
                                                  .Lambda = String.Format("Function() {0}", nothingLiteral)})
        End Sub

#End Region

#End Region

#Region "Operators Test Utils"

        Private Sub VerifyExpressionTreesDiagnostics(sourceFile As XElement, diagnostics As XElement,
                                                     Optional checked As Boolean = True,
                                                     Optional optimize As Boolean = True,
                                                     Optional addXmlReferences As Boolean = False)

            Dim compilation =
                CompilationUtils.CreateEmptyCompilationWithReferences(
                    <compilation>
                        <%= sourceFile %>
                        <%= _exprTesting %>
                        <%= _queryTesting %>
                    </compilation>,
                    references:=If(addXmlReferences, DefaultVbReferences.Concat(Net40XmlReferences), DefaultVbReferences),
                    options:=If(optimize, TestOptions.ReleaseDll, TestOptions.DebugDll).WithOverflowChecks(checked))

            CompilationUtils.AssertTheseDiagnostics(compilation, diagnostics)
        End Sub

        Private Function TestExpressionTreesVerifier(sourceFile As XElement, result As String,
                                                     Optional checked As Boolean = True,
                                                     Optional optimize As Boolean = True,
                                                     Optional latestReferences As Boolean = False,
                                                     Optional addXmlReferences As Boolean = False,
                                                     Optional verify As Verification = Nothing) As CompilationVerifier

            Debug.Assert(Not latestReferences OrElse Not addXmlReferences) ' NYI

            Return CompileAndVerify(
                <compilation>
                    <%= sourceFile %>
                    <%= _exprTesting %>
                    <%= _queryTesting %>
                </compilation>,
                options:=If(optimize, TestOptions.ReleaseExe, TestOptions.DebugExe).WithOverflowChecks(checked),
                expectedOutput:=If(result IsNot Nothing, result.Trim, Nothing),
                references:=If(addXmlReferences, Net40XmlReferences, {}),
                useLatestFramework:=latestReferences,
                verify:=verify)
        End Function

        Private Sub TestExpressionTrees(sourceFile As XElement, result As String,
                                        Optional checked As Boolean = True,
                                        Optional optimize As Boolean = True,
                                        Optional latestReferences As Boolean = False,
                                        Optional addXmlReferences As Boolean = False,
                                        Optional diagnostics() As DiagnosticDescription = Nothing,
                                        Optional verify As Verification = Nothing)

            TestExpressionTreesVerifier(sourceFile, result, checked, optimize, latestReferences, addXmlReferences, verify).VerifyDiagnostics(If(diagnostics, {}))
        End Sub

        Private Sub TestExpressionTrees(sourceFile As XElement, result As XCData,
                                        Optional checked As Boolean = True,
                                        Optional optimize As Boolean = True,
                                        Optional latestReferences As Boolean = False,
                                        Optional addXmlReferences As Boolean = False,
                                        Optional diagnostics() As DiagnosticDescription = Nothing)
            TestExpressionTrees(sourceFile, TestHelpers.NormalizeNewLines(result), checked, optimize, latestReferences, addXmlReferences, diagnostics)
        End Sub

        Private Class ExpressionTreeTest
            Public Description As String
            Public ExpressionTypeArgument As String
            Public Lambda As String
            Public Prefix As XCData
        End Class

        Private Sub TestExpressionTrees(checked As Boolean, result As String, tests() As ExpressionTreeTest,
                                        Optional typeParameters As String = "",
                                        Optional typeArguments As String = "",
                                        Optional diagnostics() As DiagnosticDescription = Nothing)

            Dim prefixbuilder As New StringBuilder
            Dim testbuilder As New StringBuilder

            Dim procIndex As Integer = 0
            Dim count As Integer = 0

            For Each tst In tests
                count += 1

                Debug.Assert(tst.Description IsNot Nothing)
                Debug.Assert(tst.ExpressionTypeArgument IsNot Nothing)
                Debug.Assert(tst.Lambda IsNot Nothing)

                If tst.Prefix IsNot Nothing Then
                    prefixbuilder.AppendLine()
                    prefixbuilder.AppendLine(tst.Prefix.Value)
                End If

                testbuilder.AppendLine(String.Format("Console.WriteLine(""{0}"")", tst.Description))
                testbuilder.AppendLine(String.Format("Dim exprtree{0} As Expression(Of {1}) = {2}", count, tst.ExpressionTypeArgument, tst.Lambda))
                testbuilder.AppendLine(String.Format("Console.WriteLine(exprtree{0}.Dump)", count))
            Next

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

{0}
Public Class TestClass{2}
    Public Sub Test()
        {1}
    End Sub
End Class

Module Form1
    Sub Main()
        Dim inst As New TestClass{3}()
        inst.Test()
    End Sub
End Module
]]></file>
                             <%= _exprTesting %>
                         </compilation>

            source...<file>.Value = String.Format(source...<file>.Value,
                                                  prefixbuilder.ToString(),
                                                  testbuilder.ToString(),
                                                  typeParameters,
                                                  typeArguments)
            Dim src = source...<file>.Value

            CompileAndVerify(source,
                             references:={Net40.References.SystemCore},
                             options:=TestOptions.ReleaseExe.WithOverflowChecks(checked),
                             expectedOutput:=result.Trim
            ).VerifyDiagnostics(If(diagnostics, {}))
        End Sub

        Private Shared ReadOnly s_enumDeclarations As XCData = <![CDATA[
Public Enum E_Byte As Byte : Dummy : End Enum
Public Enum E_SByte As SByte : Dummy : End Enum
Public Enum E_UShort As UShort : Dummy : End Enum
Public Enum E_Short As Short : Dummy : End Enum
Public Enum E_UInteger As UInteger : Dummy : End Enum
Public Enum E_Integer As Integer : Dummy : End Enum
Public Enum E_ULong As ULong : Dummy : End Enum
Public Enum E_Long As Long : Dummy : End Enum
]]>

        Private ReadOnly _allTypes() As String =
                {
                    "SByte", "SByte?", GetEnumTypeName("SByte"), GetEnumTypeName("SByte") + "?",
                    "Byte", "Byte?", GetEnumTypeName("Byte"), GetEnumTypeName("Byte") + "?",
                    "Short", "Short?", GetEnumTypeName("Short"), GetEnumTypeName("Short") + "?",
                    "UShort", "UShort?", GetEnumTypeName("UShort"), GetEnumTypeName("UShort") + "?",
                    "Integer", "Integer?", GetEnumTypeName("Integer"), GetEnumTypeName("Integer") + "?",
                    "UInteger", "UInteger?", GetEnumTypeName("UInteger"), GetEnumTypeName("UInteger") + "?",
                    "Long", "Long?", GetEnumTypeName("Long"), GetEnumTypeName("Long") + "?",
                    "Boolean", "Boolean?",
                    "Single", "Single?",
                    "Double", "Double?",
                    "Decimal", "Decimal?",
                    "Date", "Date?",
                    "String",
                    "Object"
                }

        Private ReadOnly _allTypesWithoutDate() As String =
                {
                    "SByte", "SByte?", GetEnumTypeName("SByte"), GetEnumTypeName("SByte") + "?",
                    "Byte", "Byte?", GetEnumTypeName("Byte"), GetEnumTypeName("Byte") + "?",
                    "Short", "Short?", GetEnumTypeName("Short"), GetEnumTypeName("Short") + "?",
                    "UShort", "UShort?", GetEnumTypeName("UShort"), GetEnumTypeName("UShort") + "?",
                    "Integer", "Integer?", GetEnumTypeName("Integer"), GetEnumTypeName("Integer") + "?",
                    "UInteger", "UInteger?", GetEnumTypeName("UInteger"), GetEnumTypeName("UInteger") + "?",
                    "Long", "Long?", GetEnumTypeName("Long"), GetEnumTypeName("Long") + "?",
                    "Boolean", "Boolean?",
                    "Single", "Single?",
                    "Double", "Double?",
                    "Decimal", "Decimal?",
                    "String",
                    "Object"
                }

        Private Shared Function IsPrimitiveStructType(type As String) As Boolean
            Select Case type
                Case "SByte", "Byte", "SByte?", "Byte?", "E_SByte", "E_Byte", "E_SByte?", "E_Byte?",
                     "Short", "UShort", "Short?", "UShort?", "E_Short", "E_UShort", "E_Short?", "E_UShort?",
                     "Integer", "UInteger", "Integer?", "UInteger?", "E_Integer", "E_UInteger", "E_Integer?", "E_UInteger?",
                     "Long", "ULong", "Long?", "ULong?", "E_Long", "E_ULong", "E_Long?", "E_ULong?",
                     "Boolean", "Boolean?", "Decimal", "Decimal?", "Date", "Date?", "Double", "Double?", "Single", "Single?"
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Function GetEnumTypeName(type As String) As String
            Return "E_" & type
        End Function

#End Region

#Region "Lambdas"

        <Fact>
        <WorkItem(530883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530883")>
        Public Sub ExpressionTreeParameterWithLambdaArgumentAndTypeInference()
            Dim source = <compilation>
                             <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System.Linq.Expressions
Friend Module LambdaParam05mod
Function Moo(Of T)(ByVal x As Expression(Of T)) As Integer
  Return 5
End Function

Sub LambdaParam05()
 Dim ret = Moo(Function(v, w, x, y, z) z)
End Sub
End Module

]]></file>
                         </compilation>

            CompileAndVerify(source, references:={Net40.References.SystemCore}).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72456")>
        Public Sub UndeclaredClassInLambdaFunction()
            Dim source =
<compilation>
    <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Module Program
    Public Function CreateExpression() As Expression(Of Func(Of Object))
        Return Function() (New UndeclaredClass() With {.Name = "testName"})
    End Function
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, {LinqAssemblyRef})
            AssertTheseDiagnostics(compilation.GetDiagnostics(),
<expected>
BC30002: Type 'UndeclaredClass' is not defined.
        Return Function() (New UndeclaredClass() With {.Name = "testName"})
                               ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(577271, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577271")>
        Public Sub Bug577271()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict On 
Imports System
Imports System.Linq.Expressions
Imports System.Linq.Enumerable
Imports System.Xml.Linq
Imports System.Xml

Module Program
    Sub Main()
        Dim x As Expression(Of Func(Of XElement)) = Function() <e a=<%= Sub() Return %>/>
        Console.WriteLine(x)
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36675: Statement lambdas cannot be converted to expression trees.
        Dim x As Expression(Of Func(Of XElement)) = Function() &lt;e a=&lt;%= Sub() Return %&gt;/&gt;
                                                                        ~~~~~~~~~~~~
</errors>, addXmlReferences:=True)
        End Sub

        <WorkItem(577272, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577272")>
        <Fact>
        Public Sub Bug577272()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System.Linq.Expressions
Imports System

<Serializable()> 
Public Structure AStruct
    Public x As Byte
    Public y As Byte
    Public Overrides Function ToString() As String
        Return x & "," & y
    End Function
End Structure

Module Module1
    Sub Main
        Dim NOT_USED As Expression(Of Func(Of AStruct)) = Function() New AStruct() With {.y = 255}
        Console.WriteLine(NOT_USED.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  body {
    MemberInit(
      NewExpression(
        New(
          <.ctor>(
          )
          type: AStruct
        )
      )
      bindings:
        MemberAssignment(
          member: Byte y
          expression: {
            Constant(
              255
              type: System.Byte
            )
          }
        )
      type: AStruct
    )
  }
  return type: AStruct
  type: System.Func`1[AStruct]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion1()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Friend Module Program
    Sub Main()
        Dim ret as Expression(Of Func(Of Func(Of Object, Object, Object, Object))) = Function() Function(v, w, z) z
        Console.WriteLine(ret.Dump)
    End Sub
End Module

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  body {
    Lambda(
      Parameter(
        v
        type: System.Object
      )
      Parameter(
        w
        type: System.Object
      )
      Parameter(
        z
        type: System.Object
      )
      body {
        Parameter(
          z
          type: System.Object
        )
      }
      return type: System.Object
      type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
    )
  }
  return type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
  type: System.Func`1[System.Func`4[System.Object,System.Object,System.Object,System.Object]]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion2()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Friend Module Program
    Sub Main()
        Dim ret As Func(Of Expression(Of Func(Of Object, Object, Object, Object))) = Function() Function(v, w, z) z
        Console.WriteLine(ret().Dump)
    End Sub
End Module

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    v
    type: System.Object
  )
  Parameter(
    w
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Parameter(
      z
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion3()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Private Function Goo(p As Expression(Of Func(Of Object, Object))) As Expression(Of Func(Of Object, Object))
        Return p
    End Function

    Public Sub Main()
        Dim ret As Expression(Of Func(Of Object, Object, Object, Object)) = Function(x, y, z) Goo(Function() Function(w) w)
        Console.WriteLine(ret.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                a0
                type: System.Object
              )
              body {
                Invoke(
                  Lambda(
                    body {
                      Convert(
                        Lambda(
                          Parameter(
                            w
                            type: System.Object
                          )
                          body {
                            Parameter(
                              w
                              type: System.Object
                            )
                          }
                          return type: System.Object
                          type: VB$AnonymousDelegate_1`2[System.Object,System.Object]
                        )
                        type: System.Object
                      )
                    }
                    return type: System.Object
                    type: VB$AnonymousDelegate_0`1[System.Object]
                  )
                  (
                  )
                  type: System.Object
                )
              }
              return type: System.Object
              type: System.Func`2[System.Object,System.Object]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]
          )
        )
        type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion4()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Private Function Goo(p As Expression(Of Func(Of Object, Object))) As Expression(Of Func(Of Object, Object))
        Return p
    End Function

    Public Sub Main()
        Dim ret As Expression(Of Func(Of Object, Object, Object, Object)) = Function(x, y, z) Goo(Function(w) w)
        Console.WriteLine(ret.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                w
                type: System.Object
              )
              body {
                Parameter(
                  w
                  type: System.Object
                )
              }
              return type: System.Object
              type: System.Func`2[System.Object,System.Object]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]
          )
        )
        type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion5()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Private Function Goo(p As Expression(Of Func(Of Object, Func(Of Object, Object)))) As Func(Of Object, Object)
        Return p.Compile.Invoke(Nothing)
    End Function

    Private Function Bar(p As Object) As Expression(Of Func(Of Object, Object))
        Return p
    End Function

    Public Sub Main()
        Dim ret As Expression(Of Func(Of Object, Object, Object, Object)) = Function(x, y, z) Goo(Function() AddressOf Bar)
        Console.WriteLine(ret.Dump)
    End Sub
End Module

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Func`2[System.Object,System.Object] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                a0
                type: System.Object
              )
              body {
                Invoke(
                  Lambda(
                    body {
                      Convert(
                        Call(
                          <NULL>
                          method: System.Delegate CreateDelegate(System.Type, System.Object, System.Reflection.MethodInfo, Boolean) in System.Delegate (
                            Constant(
                              System.Func`2[System.Object,System.Object]
                              type: System.Type
                            )
                            Constant(
                              null
                              type: System.Object
                            )
                            Constant(
                              System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]] Bar(System.Object)
                              type: System.Reflection.MethodInfo
                            )
                            Constant(
                              False
                              type: System.Boolean
                            )
                          )
                          type: System.Delegate
                        )
                        type: System.Func`2[System.Object,System.Object]
                      )
                    }
                    return type: System.Func`2[System.Object,System.Object]
                    type: VB$AnonymousDelegate_0`1[System.Func`2[System.Object,System.Object]]
                  )
                  (
                  )
                  type: System.Func`2[System.Object,System.Object]
                )
              }
              return type: System.Func`2[System.Object,System.Object]
              type: System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]
          )
        )
        type: System.Func`2[System.Object,System.Object]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion5_45()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Private Function Goo(p As Expression(Of Func(Of Object, Func(Of Object, Object)))) As Func(Of Object, Object)
        Return p.Compile.Invoke(Nothing)
    End Function

    Private Function Bar(p As Object) As Expression(Of Func(Of Object, Object))
        Return p
    End Function

    Public Sub Main()
        Dim ret As Expression(Of Func(Of Object, Object, Object, Object)) = Function(x, y, z) Goo(Function() AddressOf Bar)
        Console.WriteLine(ret.Dump)
    End Sub
End Module

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Func`2[System.Object,System.Object] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                a0
                type: System.Object
              )
              body {
                Invoke(
                  Lambda(
                    body {
                      Convert(
                        Call(
                          Constant(
                            System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]] Bar(System.Object)
                            type: System.Reflection.MethodInfo
                          )
                          method: System.Delegate CreateDelegate(System.Type, System.Object) in System.Reflection.MethodInfo (
                            Constant(
                              System.Func`2[System.Object,System.Object]
                              type: System.Type
                            )
                            Constant(
                              null
                              type: System.Object
                            )
                          )
                          type: System.Delegate
                        )
                        type: System.Func`2[System.Object,System.Object]
                      )
                    }
                    return type: System.Func`2[System.Object,System.Object]
                    type: VB$AnonymousDelegate_0`1[System.Func`2[System.Object,System.Object]]
                  )
                  (
                  )
                  type: System.Func`2[System.Object,System.Object]
                )
              }
              return type: System.Func`2[System.Object,System.Object]
              type: System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]
          )
        )
        type: System.Func`2[System.Object,System.Object]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>, latestReferences:=True)
        End Sub

        <Fact>
        Public Sub LambdaConversion6()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Private Function Goo(p As Expression(Of Func(Of Object, Func(Of Object, Object)))) As Func(Of Object, Object)
        Return p.Compile.Invoke(Nothing)
    End Function

    Public Sub Main()
        Dim ret1 = DirectCast(Function(x, y, z) Goo(Function() Function(w) w), Expression(Of Func(Of Object, Object, Object, Object)))
        Console.WriteLine(ret1.Dump)
        Dim ret2 = TryCast(Function(x, y, z) Goo(Function() Function(w) w), Expression(Of Func(Of Object, Object, Object, Object)))
        Console.WriteLine(ret2.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Func`2[System.Object,System.Object] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                a0
                type: System.Object
              )
              body {
                Invoke(
                  Lambda(
                    body {
                      Lambda(
                        Parameter(
                          w
                          type: System.Object
                        )
                        body {
                          Parameter(
                            w
                            type: System.Object
                          )
                        }
                        return type: System.Object
                        type: System.Func`2[System.Object,System.Object]
                      )
                    }
                    return type: System.Func`2[System.Object,System.Object]
                    type: VB$AnonymousDelegate_0`1[System.Func`2[System.Object,System.Object]]
                  )
                  (
                  )
                  type: System.Func`2[System.Object,System.Object]
                )
              }
              return type: System.Func`2[System.Object,System.Object]
              type: System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]
          )
        )
        type: System.Func`2[System.Object,System.Object]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)

Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Func`2[System.Object,System.Object] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                a0
                type: System.Object
              )
              body {
                Invoke(
                  Lambda(
                    body {
                      Lambda(
                        Parameter(
                          w
                          type: System.Object
                        )
                        body {
                          Parameter(
                            w
                            type: System.Object
                          )
                        }
                        return type: System.Object
                        type: System.Func`2[System.Object,System.Object]
                      )
                    }
                    return type: System.Func`2[System.Object,System.Object]
                    type: VB$AnonymousDelegate_0`1[System.Func`2[System.Object,System.Object]]
                  )
                  (
                  )
                  type: System.Func`2[System.Object,System.Object]
                )
              }
              return type: System.Func`2[System.Object,System.Object]
              type: System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Func`2[System.Object,System.Object]]]
          )
        )
        type: System.Func`2[System.Object,System.Object]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub LambdaConversion7()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Private Function Goo(p As Expression(Of Func(Of Object, Object))) As Expression(Of Func(Of Object, Object))
        Return p
    End Function

    Public Sub Main()
        Dim a As Integer = 1
        Dim b As Double = 2
        Dim ret As Expression(Of Func(Of Object, Object, Object, Object)) = Function(x, y, z) Goo(Function(w) a + b + w)
        Console.WriteLine(ret.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  Parameter(
    y
    type: System.Object
  )
  Parameter(
    z
    type: System.Object
  )
  body {
    Convert(
      Call(
        <NULL>
        method: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]] Goo(System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]) in Form1 (
          Quote(
            Lambda(
              Parameter(
                w
                type: System.Object
              )
              body {
                Add(
                  Convert(
                    Add(
                      Convert(
                        MemberAccess(
                          Constant(
                            Form1+_Closure$__1-0
                            type: Form1+_Closure$__1-0
                          )
                          -> $VB$Local_a
                          type: System.Int32
                        )
                        type: System.Double
                      )
                      MemberAccess(
                        Constant(
                          Form1+_Closure$__1-0
                          type: Form1+_Closure$__1-0
                        )
                        -> $VB$Local_b
                        type: System.Double
                      )
                      type: System.Double
                    )
                    type: System.Object
                  )
                  Parameter(
                    w
                    type: System.Object
                  )
                  method: System.Object AddObject(System.Object, System.Object) in Microsoft.VisualBasic.CompilerServices.Operators
                  type: System.Object
                )
              }
              return type: System.Object
              type: System.Func`2[System.Object,System.Object]
            )
            type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]
          )
        )
        type: System.Linq.Expressions.Expression`1[System.Func`2[System.Object,System.Object]]
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`4[System.Object,System.Object,System.Object,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation01()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of C0)
    End Sub

End Module

Class C0
    Public Function Process() As Boolean
        Return False
    End Function

    Public Sub ProcessSub()
    End Sub
End Class

Class C1(Of T As {New, C0})
    Public ProcessMethodSub As MethodInfo = RegisterMethod(Sub(c) c.ProcessSub())
    Public ProcessMethod As MethodInfo = RegisterMethod(Function(c) c.Process)

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of T))) As MethodInfo
        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(new T())

        Return Nothing
    End Function

End Class


]]></file>

            TestExpressionTrees(file,
            <![CDATA[
c => c.ProcessSub()
c => c.Process()
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Relaxation02()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process(x As Integer) As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub(x As Integer)
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(Sub(c As String) C0.ProcessSub(c))
    Public ProcessMethod As MethodInfo = RegisterMethod(Function(c) C0.Process(c))

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of String))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(1)

        Return Nothing
    End Function

End Class


]]></file>

            TestExpressionTrees(file,
            <![CDATA[
c => ProcessSub(ConvertChecked(c))
c => Process(ConvertChecked(c))
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation03()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process() As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub()
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(Sub(c As String) C0.ProcessSub())
    Public ProcessMethod As MethodInfo = RegisterMethod(Function(c As String) C0.Process())

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of String))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()("qq")

        Return Nothing
    End Function

End Class

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
c => ProcessSub()
c => Process()
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation04()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process() As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub()
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(Sub() C0.ProcessSub())
    Public ProcessMethod As MethodInfo = RegisterMethod(Function() C0.Process())

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of Integer))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(1)

        Return Nothing
    End Function

End Class


]]></file>

            TestExpressionTrees(file,
            <![CDATA[
a0 => Invoke(() => ProcessSub())
a0 => Invoke(() => Process())
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation05()
            Dim file = <file name="expr.vb"><![CDATA[

Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process(arg As Action(Of Integer)) As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub(arg As Action(Of Integer))
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(Sub(t As Integer) C0.ProcessSub(Sub(tt As Integer) C0.ProcessSub(Nothing)))
    Public ProcessMethod As MethodInfo = RegisterMethod(Function(t As Integer) C0.Process(Function(tt As Integer) C0.Process(Nothing)))

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of Integer))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(Nothing)

        Return Nothing
    End Function

End Class

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
t => ProcessSub(tt => ProcessSub(null))
t => Process(tt => Process(null))
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation05ET()
            Dim file = <file name="expr.vb"><![CDATA[

Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process(arg As Expression(Of Action(Of Integer))) As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub(arg As Expression(Of Action(Of Integer)))
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(Sub(t As Integer) C0.ProcessSub(Sub(tt As Integer) C0.ProcessSub(Nothing)))
    Public ProcessMethod As MethodInfo = RegisterMethod(Function(t As Integer) C0.Process(Function(tt As Integer) C0.Process(Nothing)))

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of Integer))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(Nothing)

        Return Nothing
    End Function

End Class

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
t => ProcessSub(tt => ProcessSub(null))
t => Process(tt => Process(null))
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation06()
            Dim file = <file name="expr.vb"><![CDATA[

Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process(arg As Action(Of Integer)) As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub(arg As Action(Of Integer))
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(DirectCast(
                                                           Sub(t As Integer) C0.ProcessSub(Sub(tt As Integer) C0.ProcessSub(Nothing)),
                                                           Expression(Of Action(Of Integer))))

    Public ProcessMethod As MethodInfo = RegisterMethod(DirectCast(
                                                        Function(t As Integer) C0.Process(Function(tt As Integer) C0.Process(Nothing)),
                                                        Expression(Of Action(Of Integer))))

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of Integer))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(Nothing)

        Return Nothing
    End Function

End Class

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
t => ProcessSub(tt => ProcessSub(null))
t => Process(tt => Process(null))
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation07()
            Dim file = <file name="expr.vb"><![CDATA[

Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
    End Sub

End Module

Class C0
    Public Shared Function Process(arg As Action(Of Integer)) As Boolean
        Return False
    End Function

    Public Shared Sub ProcessSub(arg As Action(Of Integer))
    End Sub
End Class

Class C1(Of T)
    Public ProcessMethodSub As MethodInfo = RegisterMethod(TryCast(
                                                           Sub(t As Integer) C0.ProcessSub(Sub(tt As Integer) C0.ProcessSub(Nothing)),
                                                           Expression(Of Action(Of Integer))))

    Public ProcessMethod As MethodInfo = RegisterMethod(TryCast(
                                                        Function(t As Integer) C0.Process(Function(tt As Integer) C0.Process(Nothing)),
                                                        Expression(Of Action(Of Integer))))

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action(Of Integer))) As MethodInfo

        System.Console.WriteLine(methodLambdaExpression.ToString())

        methodLambdaExpression.Compile()(Nothing)

        Return Nothing
    End Function

End Class

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
t => ProcessSub(tt => ProcessSub(null))
t => Process(tt => Process(null))
]]>)
        End Sub

        <Fact>
        Public Sub Relaxation08()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Reflection

Module Module1

    Sub Main()
        Dim o As New C1(Of String)
        o.Test()
    End Sub

End Module


Class C1(Of T)

    Sub Test()
        Dim anonymousDelegate = Function(x As Integer) x
        RegisterMethod(Sub() M1(anonymousDelegate))
    End Sub

    Shared Sub M1(y As Action(Of Integer))
    End Sub

    Public Function RegisterMethod(methodLambdaExpression As Expression(Of Action)) As MethodInfo
        System.Console.WriteLine(methodLambdaExpression.ToString())
        methodLambdaExpression.Compile()()
        Return Nothing
    End Function

End Class

]]></file>

            TestExpressionTrees(file,
            <![CDATA[
() => M1(a0 => Invoke(value(C1`1+_Closure$__1-0[System.String]).$VB$Local_anonymousDelegate, a0))
]]>)
        End Sub

#End Region

#Region "Xml Literals"

        <Fact>
        Public Sub XmlLiteralsInExprLambda01()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions
Imports System.Linq.Enumerable
Imports System.Xml.Linq
Imports <xmlns="http://roslyn/default1">
Imports <xmlns:r1="http://roslyn">
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">

Module Form1
    Private Sub F(p As Expression(Of Func(Of Object)))
        System.Console.WriteLine(p.Dump)
    End Sub

    Public Sub Main()
        F(Function() <!-- comment -->)
        F(Function() <?xml version="1.0"?><!-- A --><?p?><x><!-- B --><?q?></x><?r?><!-- C-->)
        F(Function() <x a="1"><!-- B --><?q?></x>)
        F(Function() <x a="1"/>)
        F(Function() <x xmlns="http://roslyn/default2" a="2"/>)
        F(Function() <p:x xmlns:p="http://roslyn/p4" a="4"/>)
        F(Function() <x xmlns="http://roslyn/default5" xmlns:p="http://roslyn/p5" p:a="5"/>)
        F(Function() <x a="0"/>)
        F(Function() <p:x xmlns:p="http://roslyn/p3" a="3"/>)

        Dim x = <a xmlns:r2="http://roslyn" r1:b="a.b"><b>1</b><r2:c>2</r2:c><c d="c.d">3</c><b>4</b><b/></a>
        F(Function() <a xmlns:r2="http://roslyn" r1:b="a.b"><b>1</b><r2:c>2</r2:c><c d="c.d">3</c><b>4</b><b/></a>)
        F(Function() x.<b>)
        F(Function() x.<c>)
        F(Function() x.<r1:c>)
        F(Function() x.@r1:b)
        F(Function() x.@xmlns:r2)

        Dim f1 As XElement = <x xmlns="http://roslyn/p" a="a1" p:b="b1"/>
        F(Function() <x xmlns="http://roslyn/p" a="a1" p:b="b1"/>)
        F(Function() <p:x a="a1" q:b="b1"/>)
        F(Function() f1.@<a>)
        F(Function() f1.@<p:a>)
        F(Function() f1.@<q:a>)
        F(Function() f1.@<b>)
        F(Function() f1.@<p:b>)
        F(Function() f1.@<q:b>)

        F(Function() <x/>.<xmlns>.Count())
        F(Function() <x/>.@xmlns)
        F(Function() <x xmlns="http://roslyn/default"/>.@xmlns)
        F(Function() <x xmlns:p="http://roslyn/p"/>.@<xmlns:p>)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, ExpTreeTestResources.XmlLiteralsInExprLambda01_Result, addXmlReferences:=True)
        End Sub

        <Fact>
        Public Sub XmlLiteralsInExprLambda02()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off
Imports System
Imports System.Collections.Generic
Imports System.Linq.Expressions
Imports System.Xml.Linq
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q">
Imports <xmlns="http://roslyn/default">
Imports <xmlns:qq="">
Imports <xmlns:p1="http://roslyn/1">
Imports <xmlns:p2="http://roslyn/2">

Module Form1
    Private Sub F(p As Expression(Of Func(Of Object)))
        System.Console.WriteLine(p.Dump)
    End Sub

    Function FF(x As XElement) As XElement
        Return x
    End Function

    Public Sub Main()
        F(Function() DirectCast(<a><b c="1"/><b c="2"/></a>.<b>, IEnumerable(Of XElement)))
        F(Function() DirectCast(<a><b><c>1</c></b><b><c>2</c></b></a>.<b>, IEnumerable(Of XElement)))

        Dim P1 = <a><b c="1"/><b c="2"/></a>
        F(Function() P1.<b>.@c)
        Dim P2 As IEnumerable(Of XElement) = P1.<b>
        F(Function() P2.@c)
        Dim P3 = <a><b><c>1</c></b><b><c>2</c></b></a>
        F(Function() P3.<b>.<c>)
        Dim P4 As IEnumerable(Of XElement) = P1.<b>
        F(Function() P4.<c>)

        F(Function() <a><c>1</c><b><q:c>2</q:c><p3:c xmlns:p3="http://roslyn/p">3</p3:c></b><b><c>4</c><p5:c xmlns:p5="http://roslyn/p">5</p5:c></b></a>)
        Dim P5 = <a><c>1</c><b><q:c>2</q:c><p3:c xmlns:p3="http://roslyn/p">3</p3:c></b><b><c>4</c><p5:c xmlns:p5="http://roslyn/p">5</p5:c></b></a>
        F(Function() P5...<c>)
        F(Function() P5...<b>...<p:c>)

        F(Function() <pa:a xmlns:pa="http://roslyn"><pb:b xmlns:pb="http://roslyn"/><pa:c/><p:d/></pa:a>)
        Dim P6 = <pa:a xmlns:pa="http://roslyn"><pb:b xmlns:pb="http://roslyn"/><pa:c/><p:d/></pa:a>
        F(Function() P6.<p:b>)
        F(Function() P6.<p:c>)
        F(Function() P6.<p:d>)
        F(Function() DirectCast(AddressOf <x a="b"/>.@a.ToString, Func(Of String)))

        F(Function() <p:x><y/><qq:z/></p:x>)

        Dim P7 As Object = <p:y/>
        F(Function() <x><%= <p:y1/> %><%= <p:y2/> %><%= <q:y3/> %></x>)
        F(Function() <x><%= P7 %></x>)
        F(Function() <p:x><%= <<%= <q:y/> %>/> %></p:x>)
        F(Function() <p:x><%= (Function() <y/>)() %></p:x>)

        F(Function() <a><b><%= FF(<c><p1:d/></c>) %></b><b><%= FF(<c><p1:d/><p2:d/></c>) %></b></a>)
        F(Function() <a><b xmlns:p1="http://roslyn/3"><%= FF(<c><p1:d/><p2:d/></c>) %></b><b xmlns:p2="http://roslyn/4"><%= FF(<c><p1:d/><p2:d/></c>) %></b></a>)
        F(Function() <a xmlns:p1="http://roslyn/3"><b xmlns:p2="http://roslyn/4"><%= FF(<c><p1:d/><p2:d/></c>) %></b></a>)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, ExpTreeTestResources.XmlLiteralsInExprLambda02_Result, addXmlReferences:=True)
        End Sub

        <Fact>
        Public Sub XmlLiteralsInExprLambda03()
            Dim file = <file name="expr.vb"><![CDATA[
imports System
Imports System.Xml.Linq
Imports System.Linq.Expressions
Imports <xmlns:p="http://roslyn/p">
Imports <xmlns:q="http://roslyn/q1">
Imports <xmlns:r="http://roslyn/r">
Imports <xmlns:s="http://roslyn/r">

Module Form1
    Private Sub F(p As Expression(Of Func(Of Object)))
        System.Console.WriteLine(p.Dump)
    End Sub

    Public Sub Main()
        F(Function() <x><y><p:z q:a="b" r:c="d"/><%= N.F %></y></x>)

        F(Function() <x <%= <p:y/> %>/>)
        F(Function() <<%= <p:y/> %>/>)
        F(Function() <?xml version="1.0"?><%= <p:y/> %>)

        Dim F1 = <q:y/>
        F(Function() <p:x><%= F1 %></p:x>)
        F(Function() <p:x><%= F1 %></p:x>)

        Dim F2 As String = "x"
        F(Function() <<%= F2 %> <%= F2 %>="..."/>)

        F(Function() <x a1="b1"/>)
        F(Function() <x a2=<%= "b2" %>/>)
        F(Function() <x a3=<%= 3 %>/>)
        F(Function() <x a4=<%= Nothing %>/>)

        F(Function() <r:h xmlns:r="http://roslyn"/>)
        F(Function() <w w=<%= F2 %>/>)
        F(Function() <x><%= F2 %></x>)
        Dim F3 As XElement = <g/>
        F(Function() <y><%= F3 %></y>)
        Dim F4 As XElement = <y><%= F3 %></y>
        F(Function() <z><%= F2 %><%= F3 %><%= F4 %></z>)
    End Sub
End Module

Module N
    Public F As Object = <p:z q:a="b" s:c="d"/>
End Module
]]></file>

            TestExpressionTrees(file, ExpTreeTestResources.XmlLiteralsInExprLambda03_Result, addXmlReferences:=True)
        End Sub

        <WorkItem(545738, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545738")>
        <Fact>
        Public Sub Bug_14377b()
            ' Expression Trees: Xml literals NYI
            Dim file = <file name="a.vb"><![CDATA[
imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim val As String = "val"
        apCompareLambdaToExpression(Function() <e1><%= val %></e1>, Function() <e1><%= val %></e1>)
    End Sub
    Public Sub apCompareLambdaToExpression(Of T)(ByVal func As Func(Of T), ByVal expr As Expression(Of Func(Of T)))
        func()
        Console.WriteLine(expr.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, <![CDATA[
Lambda(
  body {
    New(
      Void .ctor(System.Xml.Linq.XName, System.Object)(
        Call(
          <NULL>
          method: System.Xml.Linq.XName Get(System.String, System.String) in System.Xml.Linq.XName (
            Constant(
              e1
              type: System.String
            )
            Constant(
              
              type: System.String
            )
          )
          type: System.Xml.Linq.XName
        )
        Convert(
          MemberAccess(
            Constant(
              Module1+_Closure$__0-0
              type: Module1+_Closure$__0-0
            )
            -> $VB$Local_val
            type: System.String
          )
          type: System.Object
        )
      )
      type: System.Xml.Linq.XElement
    )
  }
  return type: System.Xml.Linq.XElement
  type: System.Func`1[System.Xml.Linq.XElement]
)]]>, addXmlReferences:=True)
        End Sub

#End Region

#Region "Miscellaneous"

        <Fact>
        Public Sub ExprTree_LegacyTests01()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1

    Class Class1
    End Class
    Class Class2 : Inherits Class1
    End Class
    Delegate Sub DeleSub1(ByVal x As Integer)
    Sub Sub1(ByVal x As Long)
    End Sub
    Delegate Function DeleFunc1(ByVal x As Class2) As Integer
    Delegate Function DeleFunc2() As Decimal
    Function Func1(ByVal x As Class1) As UShort
        Return Nothing
    End Function
    Function Func2() As Boolean
        Return Nothing
    End Function
    Function Func3(Optional ByVal x As Integer = 42) As Long
        Return Nothing
    End Function
    Function Goo(ByVal ds As DeleSub1) As Boolean
        Return Nothing
    End Function

    Sub Main()
        Dim l1 As Expression(Of Func(Of DeleSub1)) = Function() CType(AddressOf Sub1, DeleSub1)
        Console.WriteLine(l1.Dump)
        Dim l2 As Expression(Of Func(Of DeleFunc1)) = Function() New DeleFunc1(AddressOf Func1)
        Console.WriteLine(l2.Dump)
        Dim l3 As Expression(Of Func(Of Boolean)) = Function() Goo(AddressOf Func2)
        Console.WriteLine(l3.Dump)
        Dim l4 As Expression(Of Func(Of DeleFunc2)) = Function() CType(AddressOf Func3, DeleFunc2)
        Console.WriteLine(l4.Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  body {
    Lambda(
      Parameter(
        a0
        type: System.Int32
      )
      body {
        Call(
          <NULL>
          method: Void Sub1(Int64) in Form1 (
            ConvertChecked(
              Parameter(
                a0
                type: System.Int32
              )
              type: System.Int64
            )
          )
          type: System.Void
        )
      }
      return type: System.Void
      type: Form1+DeleSub1
    )
  }
  return type: Form1+DeleSub1
  type: System.Func`1[Form1+DeleSub1]
)

Lambda(
  body {
    Lambda(
      Parameter(
        a0
        type: Form1+Class2
      )
      body {
        ConvertChecked(
          Call(
            <NULL>
            method: UInt16 Func1(Class1) in Form1 (
              Convert(
                Parameter(
                  a0
                  type: Form1+Class2
                )
                type: Form1+Class1
              )
            )
            type: System.UInt16
          )
          type: System.Int32
        )
      }
      return type: System.Int32
      type: Form1+DeleFunc1
    )
  }
  return type: Form1+DeleFunc1
  type: System.Func`1[Form1+DeleFunc1]
)

Lambda(
  body {
    Call(
      <NULL>
      method: Boolean Goo(DeleSub1) in Form1 (
        Lambda(
          Parameter(
            a0
            type: System.Int32
          )
          body {
            Call(
              <NULL>
              method: Boolean Func2() in Form1 (
              )
              type: System.Boolean
            )
          }
          return type: System.Void
          type: Form1+DeleSub1
        )
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`1[System.Boolean]
)

Lambda(
  body {
    Lambda(
      body {
        Convert(
          Call(
            <NULL>
            method: Int64 Func3(Int32) in Form1 (
              Constant(
                42
                type: System.Int32
              )
            )
            type: System.Int64
          )
          method: System.Decimal op_Implicit(Int64) in System.Decimal
          type: System.Decimal
        )
      }
      return type: System.Decimal
      type: Form1+DeleFunc2
    )
  }
  return type: Form1+DeleFunc2
  type: System.Func`1[Form1+DeleFunc2]
)]]>)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests02_v40()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System

Module Form1

    Delegate Sub DeleSub(ByVal x As Integer)
    Sub Sub1(ByVal x As Integer)
    End Sub
    Delegate Function DeleFunc(ByVal x As DeleSub) As Boolean
    Class Class1
        Function Func1(ByVal x As DeleSub) As Boolean
            Return Nothing
        End Function
    End Class
    <Runtime.CompilerServices.Extension()>
    Function ExtensionMethod1(ByVal target As Class1, ByVal x As DeleSub) As Boolean
        Return Nothing
    End Function

    Sub Main()
        'Dim l1 As Expression(Of Func(Of DeleSub1)) = Function() CType(AddressOf Sub1, DeleSub1)
        'Console.WriteLine(l1.Dump)

        Dim queryObj As New QueryHelper(Of String)
        Dim c1 As New Class1
        Dim scenario1 = From s In queryObj Where New DeleSub(AddressOf Sub1) Is Nothing Select s
        Dim scenario3 = From s In queryObj Where CType(AddressOf c1.Func1, DeleFunc) Is Nothing Select s
        Dim d1 As New DeleSub(AddressOf Sub1)
        Dim d2 As New DeleFunc(AddressOf c1.Func1)
        Dim scenario4 = From s In queryObj Where d2(d1) Select s
        Dim scenario5 = From s In queryObj Where d2.Invoke(d1) Select s
        Dim callback As AsyncCallback = Nothing
        Dim scenario6 = From s In queryObj Where d2.BeginInvoke(d1, callback, Nothing).IsCompleted Select s
        Dim d3 = CType(AddressOf c1.ExtensionMethod1, DeleFunc)
        Dim scenario7 = From s In queryObj Where CType(AddressOf c1.ExtensionMethod1, DeleFunc) Is Nothing Select s
        Dim scenario8 = From s In queryObj Where d3(d1) Select s
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.ExprTree_LegacyTests02_v40_Result)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests02_v45()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System

Module Form1

    Delegate Sub DeleSub(ByVal x As Integer)
    Sub Sub1(ByVal x As Integer)
    End Sub
    Delegate Function DeleFunc(ByVal x As DeleSub) As Boolean
    Class Class1
        Function Func1(ByVal x As DeleSub) As Boolean
            Return Nothing
        End Function
    End Class
    <Runtime.CompilerServices.Extension()>
    Function ExtensionMethod1(ByVal target As Class1, ByVal x As DeleSub) As Boolean
        Return Nothing
    End Function

    Sub Main()
        Dim queryObj As New QueryHelper(Of String)
        Dim c1 As New Class1
        Dim scenario1 = From s In queryObj Where New DeleSub(AddressOf Sub1) Is Nothing Select s
        Dim scenario3 = From s In queryObj Where CType(AddressOf c1.Func1, DeleFunc) Is Nothing Select s
        Dim d1 As New DeleSub(AddressOf Sub1)
        Dim d2 As New DeleFunc(AddressOf c1.Func1)
        Dim scenario4 = From s In queryObj Where d2(d1) Select s
        Dim scenario5 = From s In queryObj Where d2.Invoke(d1) Select s
        Dim callback As AsyncCallback = Nothing
        Dim scenario6 = From s In queryObj Where d2.BeginInvoke(d1, callback, Nothing).IsCompleted Select s
        Dim d3 = CType(AddressOf c1.ExtensionMethod1, DeleFunc)
        Dim scenario7 = From s In queryObj Where CType(AddressOf c1.ExtensionMethod1, DeleFunc) Is Nothing Select s
        Dim scenario8 = From s In queryObj Where d3(d1) Select s
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.ExprTree_LegacyTests02_v45_Result, latestReferences:=True)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests03()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 

Module Form1

    Dim queryObj As New QueryHelper(Of String)
    Class Class1
        Const x As Integer = -3
        ReadOnly y As ULong = 3
        Function GetX() As Object
            Return From s In queryObj Where x Select s
        End Function
        Function GetY() As Object
            Return From s In queryObj Where y Select s
        End Function
        Function GooToTheX() As Object
            Return From s In queryObj Where Goo(x) Select s
        End Function
        Function GooToTheY() As Object
            Return From s In queryObj Where Goo(y) Select s
        End Function
        Function GooToTheLocal() As Object
            Dim t As Integer = 1
            Return From s In queryObj Where Goo(t) Select s
        End Function
        Function GooToTheLocalAndCopyBack() As Object
            Dim t As String = "1"
            Return From s In queryObj Where Goo(t) Select s
        End Function
    End Class
    Function Goo(ByRef x As Integer) As Boolean
        x = 42
        Return Nothing
    End Function

    Sub Main()
        Dim c1 As New Class1
        c1.GetX()
        c1.GetY()
        c1.GooToTheX()
        c1.GooToTheY()
        c1.GooToTheLocal()
        c1.GooToTheLocalAndCopyBack()
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Constant(
      True
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Convert(
      MemberAccess(
        Constant(
          Form1+Class1
          type: Form1+Class1
        )
        -> y
        type: System.UInt64
      )
      method: Boolean ToBoolean(UInt64) in System.Convert
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Call(
      <NULL>
      method: Boolean Goo(Int32 ByRef) in Form1 (
        Constant(
          -3
          type: System.Int32
        )
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Call(
      <NULL>
      method: Boolean Goo(Int32 ByRef) in Form1 (
        ConvertChecked(
          MemberAccess(
            Constant(
              Form1+Class1
              type: Form1+Class1
            )
            -> y
            type: System.UInt64
          )
          type: System.Int32
        )
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Call(
      <NULL>
      method: Boolean Goo(Int32 ByRef) in Form1 (
        MemberAccess(
          Constant(
            Form1+Class1+_Closure$__7-0
            type: Form1+Class1+_Closure$__7-0
          )
          -> $VB$Local_t
          type: System.Int32
        )
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Call(
      <NULL>
      method: Boolean Goo(Int32 ByRef) in Form1 (
        ConvertChecked(
          MemberAccess(
            Constant(
              Form1+Class1+_Closure$__8-0
              type: Form1+Class1+_Closure$__8-0
            )
            -> $VB$Local_t
            type: System.String
          )
          method: Int32 ToInteger(System.String) in Microsoft.VisualBasic.CompilerServices.Conversions
          type: System.Int32
        )
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)]]>)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests04()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Imports System

Module Form1

    Dim queryObj As New QueryHelper(Of String)
    Class Class1
    End Class
    Class Class2(Of T)
        Shared Sub Scenario4()
            Dim scenario4 = From s In queryObj Where GetType(Class2(Of T)) Is t3 Select s
        End Sub
        Shared Sub Scenario6()
            Dim scenario6 = From s In queryObj Where GetType(T) Is t1 Select s
        End Sub
    End Class
    Structure Struct1
        Dim a
    End Structure
    Enum Enum1
        a
    End Enum

    Sub Main()
        Dim t1 = GetType(Integer)
        Dim t2 = GetType(Class1)
        Dim t3 = GetType(Nullable(Of IntPtr))

        Dim scenario1 = From s In queryObj Where GetType(Integer) Is t1 Select s
        Dim scenario2 = From s In queryObj Where GetType(Class1) Is t2 Select s
        Dim scenario3 = From s In queryObj Where GetType(Nullable(Of IntPtr)) Is t3 Select s
        Class2(Of String).Scenario4()
        Dim scenario5 = From s In queryObj Where GetType(System.Void) Is t3 Select s
        Class2(Of Integer).Scenario6()
        Dim scenario7 = From s In queryObj Where GetType(Struct1) Is t3 Select s
        Dim scenario8 = From s In queryObj Where GetType(Enum1) Is t3 Select s
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          System.Int32
          type: System.Type
        )
        type: System.Object
      )
      Convert(
        MemberAccess(
          Constant(
            Form1+_Closure$__6-0
            type: Form1+_Closure$__6-0
          )
          -> $VB$Local_t1
          type: System.Type
        )
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          Form1+Class1
          type: System.Type
        )
        type: System.Object
      )
      Convert(
        MemberAccess(
          Constant(
            Form1+_Closure$__6-0
            type: Form1+_Closure$__6-0
          )
          -> $VB$Local_t2
          type: System.Type
        )
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          System.Nullable`1[System.IntPtr]
          type: System.Type
        )
        type: System.Object
      )
      Convert(
        MemberAccess(
          Constant(
            Form1+_Closure$__6-0
            type: Form1+_Closure$__6-0
          )
          -> $VB$Local_t3
          type: System.Type
        )
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          Form1+Class2`1[System.String]
          type: System.Type
        )
        type: System.Object
      )
      MemberAccess(
        Constant(
          Form1+Class2`1+_Closure$__1-0[System.String]
          type: Form1+Class2`1+_Closure$__1-0[System.String]
        )
        -> $VB$Local_t3
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          System.Void
          type: System.Type
        )
        type: System.Object
      )
      Convert(
        MemberAccess(
          Constant(
            Form1+_Closure$__6-0
            type: Form1+_Closure$__6-0
          )
          -> $VB$Local_t3
          type: System.Type
        )
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          System.Int32
          type: System.Type
        )
        type: System.Object
      )
      MemberAccess(
        Constant(
          Form1+Class2`1+_Closure$__2-0[System.Int32]
          type: Form1+Class2`1+_Closure$__2-0[System.Int32]
        )
        -> $VB$Local_t1
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          Form1+Struct1
          type: System.Type
        )
        type: System.Object
      )
      Convert(
        MemberAccess(
          Constant(
            Form1+_Closure$__6-0
            type: Form1+_Closure$__6-0
          )
          -> $VB$Local_t3
          type: System.Type
        )
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Equal(
      Convert(
        Constant(
          Form1+Enum1
          type: System.Type
        )
        type: System.Object
      )
      Convert(
        MemberAccess(
          Constant(
            Form1+_Closure$__6-0
            type: Form1+_Closure$__6-0
          )
          -> $VB$Local_t3
          type: System.Type
        )
        type: System.Object
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)]]>, diagnostics:={
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "t3").WithArguments("t3"),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "t1").WithArguments("t1")})
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests05()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Imports System
Imports System.Linq.Expressions

Module Form1

    Dim queryObj As New QueryHelper(Of String)
    Class Class1
        Private x
        Function GetMyClass() As Object
            Return From s In queryObj Where MyClass.Bar() Select s
        End Function
        Function GetMe() As Object
            Return From s In queryObj Where Me.x Select s
        End Function
        Function GetMyClassV() As Object
            Return From s In queryObj Where MyClass.VBar() Select s
        End Function
        Function Bar() As Integer
            Return Nothing
        End Function
        Overridable Function VBar() As Integer
            Return Nothing
        End Function
        Function GetMyClassAsExpression() As Expression(Of Func(Of Integer))
            Return Function() MyClass.Bar()
        End Function
        Function GetMyClassAsExpressionV() As Expression(Of Func(Of Integer))
            Return Function() MyClass.VBar()
        End Function
        Function GetMyClassAddressOfAsExpressionV() As Expression(Of Func(Of Func(Of Integer)))
            Return Function() AddressOf MyClass.VBar
        End Function
    End Class
    Class Class2 : Inherits Class1
        Overloads Function Bar() As Integer
            Return Nothing
        End Function
        Function GetMyBase() As Object
            Return From s In queryObj Where MyBase.Bar() Select s
        End Function
        Function GetMyBaseV() As Object
            Return From s In queryObj Where MyBase.VBar() Select s
        End Function
        Function GetMyBaseAsExpression() As Expression(Of Func(Of Integer))
            Return Function() MyBase.Bar()
        End Function
        Function GetMyBaseAsExpressionV() As Expression(Of Func(Of Integer))
            Return Function() MyBase.VBar()
        End Function
        Function GetMyBaseAddressOfAsExpressionV() As Expression(Of Func(Of Func(Of Integer)))
            Return Function() AddressOf MyBase.VBar
        End Function
    End Class

    Sub Main()
        Dim c1 As Class1 = New Class2
        Dim c2 As New Class2
        c1.GetMyClass()
        c1.GetMyClassV()
        c1.GetMe()
        c2.GetMyBase()
        c2.GetMyBaseV()

        Dim l1 = c2.GetMyBaseAsExpression
        Console.WriteLine(l1.Dump)
        Dim l2 = c2.GetMyBaseAsExpressionV
        Console.WriteLine(l2.Dump)
        Dim l3 = c1.GetMyClassAsExpression
        Console.WriteLine(l3.Dump)
        Dim l4 = c1.GetMyClassAsExpressionV
        Console.WriteLine(l4.Dump)
        Dim l5 = c1.GetMyClassAddressOfAsExpressionV
        Console.WriteLine(l5.Dump)
        Dim l6 = c2.GetMyBaseAddressOfAsExpressionV
        Console.WriteLine(l6.Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Convert(
      Call(
        Constant(
          Form1+Class2
          type: Form1+Class1
        )
        method: Int32 $VB$ClosureStub_Bar_MyClass() in Form1+Class1 (
        )
        type: System.Int32
      )
      method: Boolean ToBoolean(Int32) in System.Convert
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Convert(
      Call(
        Constant(
          Form1+Class2
          type: Form1+Class1
        )
        method: Int32 $VB$ClosureStub_VBar_MyClass() in Form1+Class1 (
        )
        type: System.Int32
      )
      method: Boolean ToBoolean(Int32) in System.Convert
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Convert(
      MemberAccess(
        Constant(
          Form1+Class2
          type: Form1+Class1
        )
        -> x
        type: System.Object
      )
      method: Boolean ToBoolean(System.Object) in Microsoft.VisualBasic.CompilerServices.Conversions
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Convert(
      Call(
        Constant(
          Form1+Class2
          type: Form1+Class2
        )
        method: Int32 $VB$ClosureStub_Bar_MyBase() in Form1+Class2 (
        )
        type: System.Int32
      )
      method: Boolean ToBoolean(Int32) in System.Convert
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Convert(
      Call(
        Constant(
          Form1+Class2
          type: Form1+Class2
        )
        method: Int32 $VB$ClosureStub_VBar_MyBase() in Form1+Class2 (
        )
        type: System.Int32
      )
      method: Boolean ToBoolean(Int32) in System.Convert
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)

Lambda(
  body {
    Call(
      Constant(
        Form1+Class2
        type: Form1+Class2
      )
      method: Int32 $VB$ClosureStub_Bar_MyBase() in Form1+Class2 (
      )
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`1[System.Int32]
)

Lambda(
  body {
    Call(
      Constant(
        Form1+Class2
        type: Form1+Class2
      )
      method: Int32 $VB$ClosureStub_VBar_MyBase() in Form1+Class2 (
      )
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`1[System.Int32]
)

Lambda(
  body {
    Call(
      Constant(
        Form1+Class2
        type: Form1+Class1
      )
      method: Int32 $VB$ClosureStub_Bar_MyClass() in Form1+Class1 (
      )
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`1[System.Int32]
)

Lambda(
  body {
    Call(
      Constant(
        Form1+Class2
        type: Form1+Class1
      )
      method: Int32 $VB$ClosureStub_VBar_MyClass() in Form1+Class1 (
      )
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`1[System.Int32]
)

Lambda(
  body {
    Convert(
      Call(
        <NULL>
        method: System.Delegate CreateDelegate(System.Type, System.Object, System.Reflection.MethodInfo, Boolean) in System.Delegate (
          Constant(
            System.Func`1[System.Int32]
            type: System.Type
          )
          Convert(
            Constant(
              Form1+Class2
              type: Form1+Class1
            )
            type: System.Object
          )
          Constant(
            Int32 $VB$ClosureStub_VBar_MyClass()
            type: System.Reflection.MethodInfo
          )
          Constant(
            False
            type: System.Boolean
          )
        )
        type: System.Delegate
      )
      type: System.Func`1[System.Int32]
    )
  }
  return type: System.Func`1[System.Int32]
  type: System.Func`1[System.Func`1[System.Int32]]
)

Lambda(
  body {
    Convert(
      Call(
        <NULL>
        method: System.Delegate CreateDelegate(System.Type, System.Object, System.Reflection.MethodInfo, Boolean) in System.Delegate (
          Constant(
            System.Func`1[System.Int32]
            type: System.Type
          )
          Convert(
            Constant(
              Form1+Class2
              type: Form1+Class2
            )
            type: System.Object
          )
          Constant(
            Int32 $VB$ClosureStub_VBar_MyBase()
            type: System.Reflection.MethodInfo
          )
          Constant(
            False
            type: System.Boolean
          )
        )
        type: System.Delegate
      )
      type: System.Func`1[System.Int32]
    )
  }
  return type: System.Func`1[System.Int32]
  type: System.Func`1[System.Func`1[System.Int32]]
)]]>)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests06()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Module Form1
    Dim queryObj As New QueryHelper(Of String)
    Function Goo(ByVal x As Integer) As String
        Return Nothing
    End Function
    Sub Main()
        Dim scenario3 = From var1 In queryObj, var2 In queryObj From var3 In queryObj Select expr3 = Goo(var1) & var3
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    var1
    type: System.String
  )
  Parameter(
    var2
    type: System.String
  )
  body {
    New(
      Void .ctor(System.String, System.String)(
        Parameter(
          var1
          type: System.String
        )
        Parameter(
          var2
          type: System.String
        )
      )
      members: {
        System.String var1
        System.String var2
      }
      type: VB$AnonymousType_0`2[System.String,System.String]
    )
  }
  return type: VB$AnonymousType_0`2[System.String,System.String]
  type: System.Func`3[System.String,System.String,VB$AnonymousType_0`2[System.String,System.String]]
)

Lambda(
  Parameter(
    $VB$It1
    type: VB$AnonymousType_0`2[System.String,System.String]
  )
  Parameter(
    var3
    type: System.String
  )
  body {
    Call(
      <NULL>
      method: System.String Concat(System.String, System.String) in System.String (
        Call(
          <NULL>
          method: System.String Goo(Int32) in Form1 (
            ConvertChecked(
              MemberAccess(
                Parameter(
                  $VB$It1
                  type: VB$AnonymousType_0`2[System.String,System.String]
                )
                -> var1
                type: System.String
              )
              method: Int32 ToInteger(System.String) in Microsoft.VisualBasic.CompilerServices.Conversions
              type: System.Int32
            )
          )
          type: System.String
        )
        Parameter(
          var3
          type: System.String
        )
      )
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`3[VB$AnonymousType_0`2[System.String,System.String],System.String,System.String]
)
]]>)
        End Sub

        <WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        <Fact>
        Public Sub ExprTree_LegacyTests06_IL()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Imports System
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Form1
    Dim queryObj As New QueryHelper(Of String)
    Function Goo(ByVal x As Integer) As String
        Return Nothing
    End Function
    Sub Main()
        Dim scenario3 = From var1 In queryObj, var2 In queryObj From var3 In queryObj Select expr3 = Goo(var1) & var3
    End Sub
End Module
]]></file>
            TestExpressionTreesVerifier(file, Nothing).VerifyIL("Form1.Main",
            <![CDATA[
{
  // Code size      484 (0x1e4)
  .maxstack  15
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
  System.Linq.Expressions.ParameterExpression V_1)
  IL_0000:  ldsfld     "Form1.queryObj As QueryHelper(Of String)"
  IL_0005:  ldtoken    "String"
  IL_000a:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000f:  ldstr      "var1"
  IL_0014:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0019:  stloc.0
  IL_001a:  ldnull
  IL_001b:  ldtoken    "Form1.queryObj As QueryHelper(Of String)"
  IL_0020:  call       "Function System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle) As System.Reflection.FieldInfo"
  IL_0025:  call       "Function System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo) As System.Linq.Expressions.MemberExpression"
  IL_002a:  ldtoken    "System.Collections.Generic.IEnumerable(Of String)"
  IL_002f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0034:  call       "Function System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type) As System.Linq.Expressions.UnaryExpression"
  IL_0039:  ldc.i4.1
  IL_003a:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_003f:  dup
  IL_0040:  ldc.i4.0
  IL_0041:  ldloc.0
  IL_0042:  stelem.ref
  IL_0043:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of String, System.Collections.Generic.IEnumerable(Of String)))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of String, System.Collections.Generic.IEnumerable(Of String)))"
  IL_0048:  ldtoken    "String"
  IL_004d:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0052:  ldstr      "var1"
  IL_0057:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_005c:  stloc.0
  IL_005d:  ldtoken    "String"
  IL_0062:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0067:  ldstr      "var2"
  IL_006c:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0071:  stloc.1
  IL_0072:  ldtoken    "Sub VB$AnonymousType_0(Of String, String)..ctor(String, String)"
  IL_0077:  ldtoken    "VB$AnonymousType_0(Of String, String)"
  IL_007c:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle) As System.Reflection.MethodBase"
  IL_0081:  castclass  "System.Reflection.ConstructorInfo"
  IL_0086:  ldc.i4.2
  IL_0087:  newarr     "System.Linq.Expressions.Expression"
  IL_008c:  dup
  IL_008d:  ldc.i4.0
  IL_008e:  ldloc.0
  IL_008f:  stelem.ref
  IL_0090:  dup
  IL_0091:  ldc.i4.1
  IL_0092:  ldloc.1
  IL_0093:  stelem.ref
  IL_0094:  ldc.i4.2
  IL_0095:  newarr     "System.Reflection.MemberInfo"
  IL_009a:  dup
  IL_009b:  ldc.i4.0
  IL_009c:  ldtoken    "Function VB$AnonymousType_0(Of String, String).get_var1() As String"
  IL_00a1:  ldtoken    "VB$AnonymousType_0(Of String, String)"
  IL_00a6:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle) As System.Reflection.MethodBase"
  IL_00ab:  castclass  "System.Reflection.MethodInfo"
  IL_00b0:  stelem.ref
  IL_00b1:  dup
  IL_00b2:  ldc.i4.1
  IL_00b3:  ldtoken    "Function VB$AnonymousType_0(Of String, String).get_var2() As String"
  IL_00b8:  ldtoken    "VB$AnonymousType_0(Of String, String)"
  IL_00bd:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle) As System.Reflection.MethodBase"
  IL_00c2:  castclass  "System.Reflection.MethodInfo"
  IL_00c7:  stelem.ref
  IL_00c8:  call       "Function System.Linq.Expressions.Expression.New(System.Reflection.ConstructorInfo, System.Collections.Generic.IEnumerable(Of System.Linq.Expressions.Expression), ParamArray System.Reflection.MemberInfo()) As System.Linq.Expressions.NewExpression"
  IL_00cd:  ldc.i4.2
  IL_00ce:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_00d3:  dup
  IL_00d4:  ldc.i4.0
  IL_00d5:  ldloc.0
  IL_00d6:  stelem.ref
  IL_00d7:  dup
  IL_00d8:  ldc.i4.1
  IL_00d9:  ldloc.1
  IL_00da:  stelem.ref
  IL_00db:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of String, String, <anonymous type: Key var1 As String, Key var2 As String>))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of String, String, <anonymous type: Key var1 As String, Key var2 As String>))"
  IL_00e0:  call       "Function ExpressionTreeHelpers.SelectMany(Of String, String, <anonymous type: Key var1 As String, Key var2 As String>)(QueryHelper(Of String), System.Linq.Expressions.Expression(Of System.Func(Of String, System.Collections.Generic.IEnumerable(Of String))), System.Linq.Expressions.Expression(Of System.Func(Of String, String, <anonymous type: Key var1 As String, Key var2 As String>))) As QueryHelper(Of <anonymous type: Key var1 As String, Key var2 As String>)"
  IL_00e5:  ldtoken    "VB$AnonymousType_0(Of String, String)"
  IL_00ea:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_00ef:  ldstr      "$VB$It"
  IL_00f4:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_00f9:  stloc.1
  IL_00fa:  ldnull
  IL_00fb:  ldtoken    "Form1.queryObj As QueryHelper(Of String)"
  IL_0100:  call       "Function System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle) As System.Reflection.FieldInfo"
  IL_0105:  call       "Function System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo) As System.Linq.Expressions.MemberExpression"
  IL_010a:  ldtoken    "System.Collections.Generic.IEnumerable(Of String)"
  IL_010f:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0114:  call       "Function System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type) As System.Linq.Expressions.UnaryExpression"
  IL_0119:  ldc.i4.1
  IL_011a:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_011f:  dup
  IL_0120:  ldc.i4.0
  IL_0121:  ldloc.1
  IL_0122:  stelem.ref
  IL_0123:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of <anonymous type: Key var1 As String, Key var2 As String>, System.Collections.Generic.IEnumerable(Of String)))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of <anonymous type: Key var1 As String, Key var2 As String>, System.Collections.Generic.IEnumerable(Of String)))"
  IL_0128:  ldtoken    "VB$AnonymousType_0(Of String, String)"
  IL_012d:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0132:  ldstr      "$VB$It1"
  IL_0137:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_013c:  stloc.1
  IL_013d:  ldtoken    "String"
  IL_0142:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0147:  ldstr      "var3"
  IL_014c:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0151:  stloc.0
  IL_0152:  ldnull
  IL_0153:  ldtoken    "Function String.Concat(String, String) As String"
  IL_0158:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase"
  IL_015d:  castclass  "System.Reflection.MethodInfo"
  IL_0162:  ldc.i4.2
  IL_0163:  newarr     "System.Linq.Expressions.Expression"
  IL_0168:  dup
  IL_0169:  ldc.i4.0
  IL_016a:  ldnull
  IL_016b:  ldtoken    "Function Form1.Goo(Integer) As String"
  IL_0170:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase"
  IL_0175:  castclass  "System.Reflection.MethodInfo"
  IL_017a:  ldc.i4.1
  IL_017b:  newarr     "System.Linq.Expressions.Expression"
  IL_0180:  dup
  IL_0181:  ldc.i4.0
  IL_0182:  ldloc.1
  IL_0183:  ldtoken    "Function VB$AnonymousType_0(Of String, String).get_var1() As String"
  IL_0188:  ldtoken    "VB$AnonymousType_0(Of String, String)"
  IL_018d:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle) As System.Reflection.MethodBase"
  IL_0192:  castclass  "System.Reflection.MethodInfo"
  IL_0197:  call       "Function System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression, System.Reflection.MethodInfo) As System.Linq.Expressions.MemberExpression"
  IL_019c:  ldtoken    "Integer"
  IL_01a1:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_01a6:  ldtoken    "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(String) As Integer"
  IL_01ab:  call       "Function System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle) As System.Reflection.MethodBase"
  IL_01b0:  castclass  "System.Reflection.MethodInfo"
  IL_01b5:  call       "Function System.Linq.Expressions.Expression.ConvertChecked(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo) As System.Linq.Expressions.UnaryExpression"
  IL_01ba:  stelem.ref
  IL_01bb:  call       "Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression"
  IL_01c0:  stelem.ref
  IL_01c1:  dup
  IL_01c2:  ldc.i4.1
  IL_01c3:  ldloc.0
  IL_01c4:  stelem.ref
  IL_01c5:  call       "Function System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, ParamArray System.Linq.Expressions.Expression()) As System.Linq.Expressions.MethodCallExpression"
  IL_01ca:  ldc.i4.2
  IL_01cb:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_01d0:  dup
  IL_01d1:  ldc.i4.0
  IL_01d2:  ldloc.1
  IL_01d3:  stelem.ref
  IL_01d4:  dup
  IL_01d5:  ldc.i4.1
  IL_01d6:  ldloc.0
  IL_01d7:  stelem.ref
  IL_01d8:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of <anonymous type: Key var1 As String, Key var2 As String>, String, String))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of <anonymous type: Key var1 As String, Key var2 As String>, String, String))"
  IL_01dd:  call       "Function ExpressionTreeHelpers.SelectMany(Of <anonymous type: Key var1 As String, Key var2 As String>, String, String)(QueryHelper(Of <anonymous type: Key var1 As String, Key var2 As String>), System.Linq.Expressions.Expression(Of System.Func(Of <anonymous type: Key var1 As String, Key var2 As String>, System.Collections.Generic.IEnumerable(Of String))), System.Linq.Expressions.Expression(Of System.Func(Of <anonymous type: Key var1 As String, Key var2 As String>, String, String))) As QueryHelper(Of String)"
  IL_01e2:  pop
  IL_01e3:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests07()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Imports System
Imports System.Linq.Expressions

Module Form1
    Class Class1
        Shared Widening Operator CType(ByVal x As Double) As Class1
            Return Nothing
        End Operator
        Shared Widening Operator CType(ByVal x As Class1) As ULong?
            Return Nothing
        End Operator
    End Class
    Interface Interface1
    End Interface
    Class Class2 : Inherits Class1 : Implements Interface1
    End Class
    Structure Struct1 : Implements Interface1
        Dim a As Integer
        Shared Widening Operator CType(ByVal x As Struct1) As ULong
            Return Nothing
        End Operator
        Shared Operator <>(ByVal x As Struct1, ByVal y As Struct1) As Boolean
            Return x.a <> y.a
        End Operator
        Shared Operator =(ByVal x As Struct1, ByVal y As Struct1) As Boolean
            Return x.a = y.a
        End Operator
    End Structure
    Dim n1 As Double
    Dim n2? As ULong
    Dim n3? As Decimal
    Dim n4? As UInteger
    Dim c1 As Class1 = Nothing
    Dim s1 As New Struct1
    Dim s2? As Struct1 = New Struct1
    Dim i1 As Interface1 = Nothing
    Dim c2 As New Class2

    Sub F(Of T)(p As Expression(Of Func(Of T)))
        Console.WriteLine(p.Dump)
    End Sub

    Sub Main()
        F(Function() If(c1, n1))
        F(Function() If(c1, c1))
        F(Function() If(c1, n2))
        F(Function() If(n2, n1))
        F(Function() If(n2, n2))
        F(Function() If(n2, n3))
        F(Function() If(n2, s2))
        F(Function() If(s2, s1))
        F(Function() If(s2, s2))
        F(Function() If(s2, n1))
        F(Function() If(s2, n2))
        F(Function() If(i1, c2))
        F(Function() If(c2, i1))
        F(Function() If(c2, c1))
        F(Function() If(c1, c2))
        F(Function() If(i1, s1))
        F(Function() If(s2, i1))
        F(Function() If(n2, n4))
        F(Function() If(n4, n2))
        F(Function() If(CType(n2, Boolean?), False))
        'F(Function() If(n3, 3.3))
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.ExprTree_LegacyTests07_Result)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests07_Decimal()

            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off
Option Explicit Off

Imports System
Imports System.Linq.Expressions

Module Form1

    Dim n3? As Decimal

    Sub F(Of T)(p As Expression(Of Func(Of T)))
        Console.WriteLine(p.Dump)
    End Sub

    Sub Main()
        F(Function() If(n3, 3.3))
    End Sub
End Module
]]></file>
            TestExpressionTrees(file,
            <![CDATA[Lambda(
  body {
    Coalesce(
      MemberAccess(
        <NULL>
        -> n3
        type: System.Nullable`1[System.Decimal]
      )
      Constant(
        3.3
        type: System.Double
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[System.Decimal]
          )
          body {
            Convert(
              Convert(
                Parameter(
                  CoalesceLHS
                  type: System.Nullable`1[System.Decimal]
                )
                method: System.Decimal op_Explicit(System.Nullable`1[System.Decimal]) in System.Nullable`1[System.Decimal]
                type: System.Decimal
              )
              method: Double op_Explicit(System.Decimal) in System.Decimal
              type: System.Double
            )
          }
          return type: System.Double
          type: System.Func`2[System.Nullable`1[System.Decimal],System.Double]
        )
      type: System.Double
    )
  }
  return type: System.Double
  type: System.Func`1[System.Double]
)]]>)

        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests08()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Imports System
Imports System.Linq
Imports System.Linq.Expressions

Module Form1
    Structure Struct1(Of T As Structure)
        Dim a As T
        Shared Operator -(ByVal y As Struct1(Of T), ByVal x As T) As T
            Return Nothing
        End Operator
    End Structure
    Class Class1
        Public field1 As Boolean
        Public field2 As Decimal
        Public ReadOnly Property Prop() As Boolean
            Get
                Return Nothing
            End Get
        End Property
        Default Public ReadOnly Property MyProperty(ByVal x As String) As Boolean
            Get
                Return Nothing
            End Get
        End Property
    End Class
    Class class2
        Public datetime2 As DateTime
        Default Public ReadOnly Property MyProperty2(ByVal x As String) As DateTime
            Get
                Return Nothing
            End Get
        End Property
        Public Property MyProperty3() As DateTime
            Get
                Return Nothing
            End Get
            Set(ByVal value As DateTime)
            End Set
        End Property
    End Class
    Function RefParam(ByRef p As DateTime) As Boolean
        Return Nothing
    End Function

    Dim queryObj As New QueryHelper(Of String)

    Dim c1 As New Class1
    Dim c2 As New class2
    Dim x1 As Short = 3
    Dim x2? As Integer = 3.2
    Dim s1 As New Struct1(Of Long)
    Dim s2? As Struct1(Of Decimal) = Nothing
    Dim i2 As SByte = -2
    Dim d3? As Decimal = 3.3

    Delegate Sub Goo()
    Function Bar(ByVal s As Goo) As Boolean
        Return Nothing
    End Function
    Sub SubRoutine()
    End Sub

    Sub F(Of T)(p As Expression(Of Func(Of T)))
        Console.WriteLine(p.Dump)
    End Sub

    Sub Main()
        F(Function() s1 - x1)
        F(Function() s1 - x2)
        F(Function() s2 - x1)
        F(Function() s2 - x2)

        F(Function() -d3)
        Dim a = Function() -d3 ' BUG
        F(Function() i2 Mod d3)

        Dim scenario1 = From s In queryObj Where c1.Prop Select s
        Dim scenario2 = From s In queryObj Where c1.Prop And c1.field2 Select s
        Dim scenario3 = From s In queryObj Where c1!Goo Select s
        Dim scenario4 = From s In queryObj Where c2!Day = c2.datetime2 Select s
        Dim scenario5 = From s In queryObj Where RefParam(c2.MyProperty3) Select s
        Dim scenario6 = From s In queryObj Where RefParam((c2.MyProperty3)) Select s

        Dim col = GetQueryCollection(1, 2, 3)
        Dim col1 = GetQueryCollection(1, 2, 3)
        Dim q1 = From i In col Where i > 2 Select i Group Join j In col1 On i Equals j Into Count(), Sum(i), Average(i)
        Dim q2 = From i In col Where i > 2 Select i Group Join j In col1 On i Equals j Into Count(), Sum(j)
        Dim q3 = From i In col Where i > 2 Select i Group Join j In col1 On i Equals j Into Count() From k In col Where k > 2 Select k

        Dim d As Goo = Nothing
        Dim scenario7 = From s In queryObj Where Bar(AddressOf SubRoutine) Select s
        Dim scenario8 = From s In queryObj Where Bar(d) Select s
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.ExprTree_LegacyTests08_Result)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests09()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Option Explicit Off

Imports System
Imports System.Linq.Expressions

Module Form1
    Structure Struct1
        Public Shared Operator =(ByVal x As Struct1, ByVal y As Struct1) As Boolean
            Return True
        End Operator
        Public Shared Operator <>(ByVal x As Struct1, ByVal y As Struct1) As Boolean
            Return False
        End Operator
    End Structure
    Class Class1
    End Class

    Class Class2 : Inherits Class1
    End Class

    Sub Goo1(Of T As Structure)(ByVal x As T?)
        F(Function() If(x, Nothing))
        F(Function() x is Nothing)
    End Sub
    'Sub Goo2(Of T As Structure)(ByVal x As T?)
    '    F(Function() If(x, CObj(3.3)))
    'End Sub

    Sub Bar(Of T As Class2)(ByVal y As T)
        Dim c1 As New Class1
        F(Function() If(y, c1))
        FF(Function() If(y, c1))
    End Sub

    Sub Moo(Of U As Class, V As U)(ByVal z1 As U)
        Dim z2 As V = Nothing
        F(Function() If(z1, z2))
    End Sub

    Sub F(Of T)(p As Expression(Of Func(Of T)))
        Console.WriteLine(p.Dump)
    End Sub
    Sub FF(Of T)(p As Func(Of T))
    End Sub

    Sub Main()
        Dim x? As Struct1 = New Struct1()
        Dim c2 As Class2 = New Class2()

        Goo1(Of Struct1)(Nothing)
        Goo1(x)
        'Goo2(Of Struct1)(Nothing)
        'Goo2(x)
        Bar(Of Class2)(Nothing)
        Bar(c2)
        Moo(Of Class2, Class2)(Nothing)
        Moo(Of Class2, Class2)(c2)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.ExprTree_LegacyTests09_Result)
        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests09_Decimal()

            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off
Option Explicit Off

Imports System
Imports System.Linq.Expressions

Module Form1
    Structure Struct1
        Public Shared Operator =(ByVal x As Struct1, ByVal y As Struct1) As Boolean
            Return True
        End Operator
        Public Shared Operator <>(ByVal x As Struct1, ByVal y As Struct1) As Boolean
            Return False
        End Operator
    End Structure

    Sub Goo2(Of T As Structure)(ByVal x As T?)
        F(Function() If(x, CObj(3.3)))
    End Sub

    Sub F(Of T)(p As Expression(Of Func(Of T)))
        Console.WriteLine(p.Dump)
    End Sub

    Sub Main()
        Dim x? As Struct1 = New Struct1()

        Goo2(Of Struct1)(Nothing)
        Goo2(x)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  body {
    Coalesce(
      MemberAccess(
        Constant(
          Form1+_Closure$__1-0`1[Form1+Struct1]
          type: Form1+_Closure$__1-0`1[Form1+Struct1]
        )
        -> $VB$Local_x
        type: System.Nullable`1[Form1+Struct1]
      )
      Convert(
        Constant(
          3.3
          type: System.Double
        )
        type: System.Object
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[Form1+Struct1]
          )
          body {
            Convert(
              Convert(
                Parameter(
                  CoalesceLHS
                  type: System.Nullable`1[Form1+Struct1]
                )
                method: Struct1 op_Explicit(System.Nullable`1[Form1+Struct1]) in System.Nullable`1[Form1+Struct1]
                type: Form1+Struct1
              )
              type: System.Object
            )
          }
          return type: System.Object
          type: System.Func`2[System.Nullable`1[Form1+Struct1],System.Object]
        )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Coalesce(
      MemberAccess(
        Constant(
          Form1+_Closure$__1-0`1[Form1+Struct1]
          type: Form1+_Closure$__1-0`1[Form1+Struct1]
        )
        -> $VB$Local_x
        type: System.Nullable`1[Form1+Struct1]
      )
      Convert(
        Constant(
          3.3
          type: System.Double
        )
        type: System.Object
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[Form1+Struct1]
          )
          body {
            Convert(
              Convert(
                Parameter(
                  CoalesceLHS
                  type: System.Nullable`1[Form1+Struct1]
                )
                method: Struct1 op_Explicit(System.Nullable`1[Form1+Struct1]) in System.Nullable`1[Form1+Struct1]
                type: Form1+Struct1
              )
              type: System.Object
            )
          }
          return type: System.Object
          type: System.Func`2[System.Nullable`1[Form1+Struct1],System.Object]
        )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)
]]>)

        End Sub

        <Fact>
        Public Sub ExprTree_LegacyTests10()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off
Option Explicit Off

Imports System
Imports System.Linq.Expressions

Module Form1
    Sub F(Of T)(p As Expression(Of Func(Of T)))
        Console.WriteLine(p.Dump)
    End Sub

    Structure Struct1
        Public a As Integer
    End Structure
    Public Function Goo(Of T As Structure)(ByVal x As T?) As Integer
        Return Nothing
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Sub Goo(ByVal x As Integer)
        Console.WriteLine("Printed From 'Sub Goo(ByVal x As Integer)'")
        Console.WriteLine()
    End Sub

    Sub Main()
        Dim q0 As Expression(Of Func(Of Integer)) = Function() Goo(Of Struct1)(Nothing)
        Console.WriteLine(q0.Dump)
        Console.WriteLine("Result: " + q0.Compile()().ToString())

        Dim q1 As Expression(Of Func(Of Func(Of Func(Of Object, Boolean)))) = Function() If(4 > 3, Function() Function(s) True, Function() Function() False)
        Console.WriteLine(q1.Dump)
        Console.WriteLine("Result: " + q1.Compile()()()(Nothing).ToString())

        Dim q2 As Expression(Of Func(Of Func(Of String))) = Function() If(4 > 3, Function() 1UI, Function() 5.0)
        Console.WriteLine(q2.Dump)
        Console.WriteLine("Result: " + q2.Compile()()().ToString())

        Dim q3 As Expression(Of Func(Of Func(Of Long))) = Function() If(4 > 3, Function() 1UI, Function() 1US)
        Console.WriteLine(q3.Dump)
        Console.WriteLine("Result: " + q3.Compile()()().ToString())

        Dim q4 As Expression(Of Func(Of Action(Of String))) = Function() If(4 > 3, Function() 1UI, Function() 5.0)
        Console.WriteLine(q4.Dump)
        Call q4.Compile()()("11")

        Dim q5 As Expression(Of Func(Of Action)) = Function() AddressOf 0.Goo
        Console.WriteLine(q5.Dump)
        Call q5.Compile()()()
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.ExprTree_LegacyTests10_Result)
        End Sub

        <Fact>
        Public Sub ExprTreeLiftedUserDefinedConversionsWithNullableResult()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Program
    Sub Main()
        Dim l1 As Expression(Of Func(Of Test1?, Test2, Test2)) = Function(x, y) If(x, y)
        Console.WriteLine(l1.Dump)
        Dim l2 As Expression(Of Func(Of Test1?, Test2?, Test2?)) = Function(x, y) If(x, y)
        Console.WriteLine(l2.Dump)
        Dim l1x As Expression(Of Func(Of Test3?, Test1, Test1)) = Function(x, y) If(x, y)
        Console.WriteLine(l1x.Dump)
        Dim l2x As Expression(Of Func(Of Test3?, Test1?, Test1?)) = Function(x, y) If(x, y)
        Console.WriteLine(l2x.Dump)
    End Sub
End Module

Structure Test1
    Public Shared Widening Operator CType(x As Test1) As Test2?
        Return Nothing
    End Operator
End Structure

Structure Test2
End Structure

Structure Test3
    Public Shared Widening Operator CType(x As Test3?) As Test1
        Return Nothing
    End Operator
End Structure
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Nullable`1[Test1]
  )
  Parameter(
    y
    type: Test2
  )
  body {
    Coalesce(
      Parameter(
        x
        type: System.Nullable`1[Test1]
      )
      Parameter(
        y
        type: Test2
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[Test1]
          )
          body {
            Convert(
              Convert(
                Convert(
                  Parameter(
                    CoalesceLHS
                    type: System.Nullable`1[Test1]
                  )
                  Lifted
                  type: Test1
                )
                method: System.Nullable`1[Test2] op_Implicit(Test1) in Test1
                type: System.Nullable`1[Test2]
              )
              Lifted
              type: Test2
            )
          }
          return type: Test2
          type: System.Func`2[System.Nullable`1[Test1],Test2]
        )
      type: Test2
    )
  }
  return type: Test2
  type: System.Func`3[System.Nullable`1[Test1],Test2,Test2]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[Test1]
  )
  Parameter(
    y
    type: System.Nullable`1[Test2]
  )
  body {
    Coalesce(
      Parameter(
        x
        type: System.Nullable`1[Test1]
      )
      Parameter(
        y
        type: System.Nullable`1[Test2]
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[Test1]
          )
          body {
            Convert(
              Convert(
                Parameter(
                  CoalesceLHS
                  type: System.Nullable`1[Test1]
                )
                Lifted
                type: Test1
              )
              method: System.Nullable`1[Test2] op_Implicit(Test1) in Test1
              type: System.Nullable`1[Test2]
            )
          }
          return type: System.Nullable`1[Test2]
          type: System.Func`2[System.Nullable`1[Test1],System.Nullable`1[Test2]]
        )
      type: System.Nullable`1[Test2]
    )
  }
  return type: System.Nullable`1[Test2]
  type: System.Func`3[System.Nullable`1[Test1],System.Nullable`1[Test2],System.Nullable`1[Test2]]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[Test3]
  )
  Parameter(
    y
    type: Test1
  )
  body {
    Coalesce(
      Parameter(
        x
        type: System.Nullable`1[Test3]
      )
      Parameter(
        y
        type: Test1
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[Test3]
          )
          body {
            Convert(
              Parameter(
                CoalesceLHS
                type: System.Nullable`1[Test3]
              )
              method: Test1 op_Implicit(System.Nullable`1[Test3]) in Test3
              type: Test1
            )
          }
          return type: Test1
          type: System.Func`2[System.Nullable`1[Test3],Test1]
        )
      type: Test1
    )
  }
  return type: Test1
  type: System.Func`3[System.Nullable`1[Test3],Test1,Test1]
)

Lambda(
  Parameter(
    x
    type: System.Nullable`1[Test3]
  )
  Parameter(
    y
    type: System.Nullable`1[Test1]
  )
  body {
    Coalesce(
      Parameter(
        x
        type: System.Nullable`1[Test3]
      )
      Parameter(
        y
        type: System.Nullable`1[Test1]
      )
      conversion:
        Lambda(
          Parameter(
            CoalesceLHS
            type: System.Nullable`1[Test3]
          )
          body {
            Convert(
              Convert(
                Parameter(
                  CoalesceLHS
                  type: System.Nullable`1[Test3]
                )
                method: Test1 op_Implicit(System.Nullable`1[Test3]) in Test3
                type: Test1
              )
              Lifted
              LiftedToNull
              type: System.Nullable`1[Test1]
            )
          }
          return type: System.Nullable`1[Test1]
          type: System.Func`2[System.Nullable`1[Test3],System.Nullable`1[Test1]]
        )
      type: System.Nullable`1[Test1]
    )
  }
  return type: System.Nullable`1[Test1]
  type: System.Func`3[System.Nullable`1[Test3],System.Nullable`1[Test1],System.Nullable`1[Test1]]
)]]>)
        End Sub

        <Fact>
        Public Sub ExprTreeMiscellaneous_A()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Call (New Clazz(Of C)()).Main()
    End Sub
End Module

Class C
    Public Property P As Object
End Class

Class Clazz(Of T As {C, New})
    Sub Main()
        Console.WriteLine((DirectCast(Function() Me.ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() New T(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() New T() With {.P = New T()}, Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() Nothing, Expression(Of Func(Of T)))).Dump)
        Console.WriteLine((DirectCast(Function() Sub() AddHandler Me.EV, Nothing, Expression(Of Func(Of Action)))).Dump)
        Console.WriteLine((DirectCast(Function() Sub() RemoveHandler Me.EV, Nothing, Expression(Of Func(Of Action)))).Dump)
        Console.WriteLine((DirectCast(Function(x) (TypeOf Me Is Object) = (TypeOf x Is String), Expression(Of Func(Of String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x) Me(x), Expression(Of Func(Of String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x) (New Clazz(Of T))(x), Expression(Of Func(Of String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x("aaa"), Expression(Of Func(Of Clazz(Of T), Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x) x.P, Expression(Of Func(Of C, Object)))).Dump)
    End Sub

    Event EV(i As Integer)
    Default Public Property IND(s As String) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
End Class
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedMiscellaneousA)
        End Sub

        <Fact>
        Public Sub ExprTreeNothingIsNothing()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Dim l1 As Expression(Of Func(Of Object)) = Function() Nothing Is Nothing
        Console.WriteLine(l1.Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  body {
    Convert(
      Equal(
        Constant(
          null
          type: System.Object
        )
        Constant(
          null
          type: System.Object
        )
        type: System.Boolean
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub CheckedCoalesceWithNullableBoolean()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Dim ret1n As Expression(Of Func(Of Boolean?, Object)) = Function(x) If(x, True)
        Console.WriteLine(ret1n.Dump)
        Dim ret1na As Expression(Of Func(Of Boolean?, Boolean)) = Function(x) x
        Console.WriteLine(ret1na.Dump)
        Dim ret2n As Expression(Of Func(Of Boolean?, Boolean?, Object)) = Function(x, y) If(x AndAlso y, True, False)
        Console.WriteLine(ret2n.Dump)
        Dim ret2na As Expression(Of Func(Of Boolean?, Boolean?, Object)) = Function(x, y) If(CType(x AndAlso y, Boolean), True, False)
        Console.WriteLine(ret2na.Dump)
        Dim ret2nb As Expression(Of Func(Of Boolean?, Object)) = Function(x) If(x, True, False)
        Console.WriteLine(ret2nb.Dump)
        Dim ret3n As Expression(Of Func(Of Boolean?, Boolean, Object)) = Function(x, y) If(x AndAlso y, True, False)
        Console.WriteLine(ret3n.Dump)
        Dim ret3na As Expression(Of Func(Of Boolean, Boolean?, Object)) = Function(x, y) If(x AndAlso y, True, False)
        Console.WriteLine(ret3na.Dump)
        Dim ret3nb As Expression(Of Func(Of Boolean, Boolean, Object)) = Function(x, y) If(x AndAlso y, True, False)
        Console.WriteLine(ret3nb.Dump)
        Dim ret4n As Expression(Of Func(Of Integer, Object)) = Function(x) If("const", x)
        Console.WriteLine(ret4n.Dump)
        Dim ret5n As Expression(Of Func(Of Integer?, Object)) = Function(x) If(x, "const")
        Console.WriteLine(ret5n.Dump)
        Dim ret6n As Expression(Of Func(Of Boolean?, Object)) = Function(x) If(x, "const")
        Console.WriteLine(ret6n.Dump)
        Dim ret7n As Expression(Of Func(Of Boolean?, String)) = Function(x) If(x, "const")
        Console.WriteLine(ret7n.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, ExpTreeTestResources.CheckedCoalesceWithNullableBoolean)
        End Sub

        <Fact>
        Public Sub ExprTreeWithCollectionInitializer()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions
Imports System.Collections
Imports System.Collections.Generic

Structure Custom
    Implements IEnumerable
    Public Shared list As New List(Of String)()
    Public Overloads Sub Add(p As Object)
    End Sub
    Public Overloads Sub Add(p As String, p2 As Object)
    End Sub
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure

Class Clazz
    Public Property P() As List(Of Object)
    Public F() As Custom
End Class

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) New List(Of String)() From {x, "a", y.ToString()}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New List(Of Object)() From {{{x}}, {"a"}}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Custom() From {{{x}}, {x, {"a"}}}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New List(Of List(Of String))() From {New List(Of String)() From {"Hello", " "}, New List(Of String)() From {"World!"}}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New List(Of Action) From {Sub() Console.Write("hello"), Sub() Console.Write("world")}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz() With {.F = {New Custom() From {{{x}}, {x, {"a"}}}, New Custom() From {{{x}}}, New Custom()},
                                                                      .P = New List(Of Object) From {New List(Of Action) From {Sub() Console.Write("hello"), Sub() Console.Write("world")}}}, 
                                                                                                                                    Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) {x, y}, Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedCollectionInitializers)
        End Sub

        <Fact>
        Public Sub ArrayCreationAndInitialization()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(11) {}).Length), Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(11) {}).LongLength), Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(2) {x, y, ""})(x)), Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(2) {x, y, ""})(x)), Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(1, 2) {})(x, 0)), Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedArrayInitializers)
        End Sub

        <Fact>
        Public Sub SimpleObjectCreation()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Public Structure SSS
    Public Sub New(i As Object)
    End Sub
End Structure

Public Class Clazz
    Public Sub New()
    End Sub
    Public Sub New(i As Object)
    End Sub
End Class

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) New SSS(New SSS()), Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(New Clazz()), Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Int32
  )
  Parameter(
    y
    type: System.String
  )
  body {
    Convert(
      New(
        Void .ctor(System.Object)(
          Convert(
            New(
              <.ctor>(
              )
              type: SSS
            )
            type: System.Object
          )
        )
        type: SSS
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`3[System.Int32,System.String,System.Object]
)

Lambda(
  Parameter(
    x
    type: System.Int32
  )
  Parameter(
    y
    type: System.String
  )
  body {
    Convert(
      New(
        Void .ctor(System.Object)(
          Convert(
            New(
              Void .ctor()(
              )
              type: Clazz
            )
            type: System.Object
          )
        )
        type: Clazz
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`3[System.Int32,System.String,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub ObjectCreationInitializers()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Public Structure SSS
    Public ReadOnly I As Integer
    Public F As Object
    Public Sub New(i As Integer)
        Me.I = i
    End Sub
End Structure

Public Class Clazz
    Public Shared Property P0 As Object
    Public Property P1 As Object
    Public Property P2 As Object
    Public Property P3 As SSS
    Public Shared F0 As Object
    Public F1 As Object
    Public F2 As Object
    Public F3 As SSS
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
    End Sub
End Class

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.F1 = New Clazz(y)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.P1 = New Clazz(y)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.F3 = New SSS(1)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.P3 = New SSS(1)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.F1 = Nothing, .F2 = .F0}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.P1 = Nothing, .P2 = .P0}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz() With {.F1 = Sub() Console.WriteLine()}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz() With {.P1 = Sub() Console.WriteLine()}, Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module
]]></file>
            TestExpressionTrees(file, ExpTreeTestResources.CheckedObjectInitializers)
        End Sub

        <Fact>
        Public Sub ObjectCreationInitializers_BC36534a()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Public Structure SSS
    Public F As Object
    Public Property P As Object
    Public Sub New(i As Integer)
    End Sub
End Structure

Public Class Clazz
    Public Property P1 As Object
    Public Property P2 As Object
    Public F1 As Object
    Public F2 As Object
    Public Sub New(i As Integer)
    End Sub
End Class

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) New SSS(x) With {.F = New SSS(1)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New SSS(x) With {.P = New SSS(1)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module

]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36534: Expression cannot be converted into an expression tree.
        Console.WriteLine((DirectCast(Function(x, y) New SSS(x) With {.F = New SSS(1)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
                                                                ~~~~~~~~~~~~~~~~~~~~~~
BC36534: Expression cannot be converted into an expression tree.
        Console.WriteLine((DirectCast(Function(x, y) New SSS(x) With {.P = New SSS(1)}, Expression(Of Func(Of Integer, String, Object)))).Dump)
                                                                ~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub ObjectCreationInitializers_BC36534b()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Public Structure SSS
    Public F As Object
    Public Property P As Object
    Public Sub New(i As Integer)
    End Sub
End Structure

Public Class Clazz
    Public Property P1 As Object
    Public Property P2 As Object
    Public F1 As Object
    Public F2 As Object
    Public Sub New(i As Integer)
    End Sub
End Class

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.P1 = Nothing, .P2 = .P1}, Expression(Of Func(Of Integer, String, Object)))).Dump)
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.F1 = Nothing, .F2 = .F1}, Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module

]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36534: Expression cannot be converted into an expression tree.
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.P1 = Nothing, .P2 = .P1}, Expression(Of Func(Of Integer, String, Object)))).Dump)
                                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36534: Expression cannot be converted into an expression tree.
        Console.WriteLine((DirectCast(Function(x, y) New Clazz(x) With {.F1 = Nothing, .F2 = .F1}, Expression(Of Func(Of Integer, String, Object)))).Dump)
                                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousObjectCreationExpression()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Dim ret1 As Expression(Of Func(Of Integer, String, Object)) = Function(x, y) New With {.A = x, .B = y}.B + x
        Console.WriteLine(ret1.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Int32
  )
  Parameter(
    y
    type: System.String
  )
  body {
    Convert(
      Add(
        Convert(
          MemberAccess(
            New(
              Void .ctor(Int32, System.String)(
                Parameter(
                  x
                  type: System.Int32
                )
                Parameter(
                  y
                  type: System.String
                )
              )
              members: {
                Int32 A
                System.String B
              }
              type: VB$AnonymousType_0`2[System.Int32,System.String]
            )
            -> B
            type: System.String
          )
          method: Double ToDouble(System.String) in Microsoft.VisualBasic.CompilerServices.Conversions
          type: System.Double
        )
        Convert(
          Parameter(
            x
            type: System.Int32
          )
          type: System.Double
        )
        type: System.Double
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`3[System.Int32,System.String,System.Object]
)
]]>)
        End Sub

        <Fact>
        Public Sub CheckedCoalesceWithUserDefinedConversion()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Structure Str1
    Public F As Boolean?
    Public Shared Narrowing Operator CType(s As Str1) As String
        Return ""
    End Operator
    Public Shared Narrowing Operator CType(s As Str1) As Str2
        Return Nothing
    End Operator
End Structure

Structure Str2
    Public F As Boolean?
    Public Shared Narrowing Operator CType(s As Str2?) As String
        Return ""
    End Operator
End Structure

Module Form1
    Sub Main()
        Dim ret1 As Expression(Of Func(Of Str1?, Object)) = Function(x) If(x, True)
        Console.WriteLine(ret1.Dump)
        Dim ret2 As Expression(Of Func(Of Str1?, Str2?)) = Function(x) If(x, Nothing)
        Console.WriteLine(ret2.Dump)
        Dim ret3 As Expression(Of Func(Of Str1?, Str2)) = Function(x) If(x, Nothing)
        Console.WriteLine(ret3.Dump)
        Dim ret4 As Expression(Of Func(Of Str2?, Object)) = Function(x) If(x, True)
        Console.WriteLine(ret4.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, ExpTreeTestResources.CheckedCoalesceWithUserDefinedConversions)
        End Sub

        <Fact>
        Public Sub CheckedExpressionInCoalesceWitSideEffects()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Structure str
    Public F As Boolean?
End Structure

Module Form1

    Function A(p As Object) As str()
        Return New str(5) {}
    End Function

    Sub Main()
        Dim ret As Expression(Of Func(Of Object, Object)) = Function(x) If(A(x)(1).F, True)
        Console.WriteLine(ret.Dump)
    End Sub
End Module
]]></file>

            TestExpressionTrees(file,
            <![CDATA[
Lambda(
  Parameter(
    x
    type: System.Object
  )
  body {
    Convert(
      Coalesce(
        MemberAccess(
          ArrayIndex(
            Call(
              <NULL>
              method: str[] A(System.Object) in Form1 (
                Parameter(
                  x
                  type: System.Object
                )
              )
              type: str[]
            )
            Constant(
              1
              type: System.Int32
            )
            type: str
          )
          -> F
          type: System.Nullable`1[System.Boolean]
        )
        Constant(
          True
          type: System.Boolean
        )
        type: System.Boolean
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`2[System.Object,System.Object]
)
]]>)
        End Sub

        <WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        <Fact>
        Public Sub ExprTreeIL()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim exprTree As Expression(Of Func(Of Integer, Integer, Integer)) = Function(x, y) x + y
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
  System.Linq.Expressions.ParameterExpression V_1)
  IL_0000:  ldtoken    "Integer"
  IL_0005:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_000a:  ldstr      "x"
  IL_000f:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0014:  stloc.0
  IL_0015:  ldtoken    "Integer"
  IL_001a:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_001f:  ldstr      "y"
  IL_0024:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0029:  stloc.1
  IL_002a:  ldloc.0
  IL_002b:  ldloc.1
  IL_002c:  call       "Function System.Linq.Expressions.Expression.AddChecked(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression) As System.Linq.Expressions.BinaryExpression"
  IL_0031:  ldc.i4.2
  IL_0032:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_0037:  dup
  IL_0038:  ldc.i4.0
  IL_0039:  ldloc.0
  IL_003a:  stelem.ref
  IL_003b:  dup
  IL_003c:  ldc.i4.1
  IL_003d:  ldloc.1
  IL_003e:  stelem.ref
  IL_003f:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of Integer, Integer, Integer))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of Integer, Integer, Integer))"
  IL_0044:  pop
  IL_0045:  ret
}]]>)
        End Sub

        <Fact>
        Public Sub MissingHelpers()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim exprTree As Expression(Of Func(Of Integer, Integer, Integer)) = Function(x, y) x + y
    End Sub
End Module
    </file>
    <file name="b.vb">
Namespace System.Linq.Expressions
    Public Class Expression

    End Class
    Public Class Expression(Of T)
        Inherits Expression
        Public Function Lambda(Of U)(e As Expression, ParamArray p As ParameterExpression()) As Expression(Of U)
            Return Nothing
        End Function
    End Class

    Public Class ParameterExpression
        Inherits Expression
    End Class
End Namespace        
    </file>
</compilation>)

            Using stream = New MemoryStream()
                Dim emitResult = compilation.Emit(stream)
                Assert.False(emitResult.Success)
                CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics, <expected>
BC30456: 'Lambda' is not a member of 'Expression'.
        Dim exprTree As Expression(Of Func(Of Integer, Integer, Integer)) = Function(x, y) x + y
                                                                            ~~~~~~~~~~~~~~~~~~~~
BC30456: 'Parameter' is not a member of 'Expression'.
        Dim exprTree As Expression(Of Func(Of Integer, Integer, Integer)) = Function(x, y) x + y
                                                                            ~~~~~~~~~~~~~~~~~~~~
BC30456: 'Parameter' is not a member of 'Expression'.
        Dim exprTree As Expression(Of Func(Of Integer, Integer, Integer)) = Function(x, y) x + y
                                                                            ~~~~~~~~~~~~~~~~~~~~
BC30456: 'AddChecked' is not a member of 'Expression'.
        Dim exprTree As Expression(Of Func(Of Integer, Integer, Integer)) = Function(x, y) x + y
                                                                                           ~~~~~
                                                                           </expected>)
            End Using
        End Sub

        <Fact>
        Public Sub LocalVariableAccess()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq.Expressions

Module Module1

    Sub Main()
        Dim y As Integer = 12
        Dim exprTree1 As Expression(Of Func(Of Integer, Integer)) = Function(x) x + y
        Console.WriteLine(exprtree1.Dump)
   End Sub

End Module
]]></file>
                             <%= _exprTesting %>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:=<![CDATA[
Lambda(
  Parameter(
    x
    type: System.Int32
  )
  body {
    AddChecked(
      Parameter(
        x
        type: System.Int32
      )
      MemberAccess(
        Constant(
          Module1+_Closure$__0-0
          type: Module1+_Closure$__0-0
        )
        -> $VB$Local_y
        type: System.Int32
      )
      type: System.Int32
    )
  }
  return type: System.Int32
  type: System.Func`2[System.Int32,System.Int32]
)]]>).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub LocalVariableAccessInGeneric()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim y As Integer = 7
        Dim exprTree As Expression(Of Func(Of Decimal, String)) = (New Gen(Of Long, String, Decimal)()).F("hello")
        Console.WriteLine(exprTree.Dump)
    End Sub
End Module

Public Class Gen(Of U, V, W)
    Public Function F(val As V) As Expression(Of Func(Of W, V))
        Dim y As V = val
        Return Function(x) y
    End Function
End Class
]]></file>
                             <%= _exprTesting %>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:=<![CDATA[
Lambda(
  Parameter(
    x
    type: System.Decimal
  )
  body {
    MemberAccess(
      Constant(
        Gen`3+_Closure$__1-0[System.Int64,System.String,System.Decimal]
        type: Gen`3+_Closure$__1-0[System.Int64,System.String,System.Decimal]
      )
      -> $VB$Local_y
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.Decimal,System.String]
)]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(651996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651996")>
        <Fact>
        Public Sub LocalVariableAccessIL()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim y As Integer = 7
        Dim exprTree As Expression(Of Func(Of Integer, Integer)) = Function(x) x + y
    End Sub
End Module
    </file>
</compilation>)

            c.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       88 (0x58)
  .maxstack  5
  .locals init (Module1._Closure$__0-0 V_0, //$VB$Closure_0
                System.Linq.Expressions.ParameterExpression V_1)
  IL_0000:  newobj     "Sub Module1._Closure$__0-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.7
  IL_0008:  stfld      "Module1._Closure$__0-0.$VB$Local_y As Integer"
  IL_000d:  ldtoken    "Integer"
  IL_0012:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_0017:  ldstr      "x"
  IL_001c:  call       "Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression"
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  ldloc.0
  IL_0024:  ldtoken    "Module1._Closure$__0-0"
  IL_0029:  call       "Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type"
  IL_002e:  call       "Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression"
  IL_0033:  ldtoken    "Module1._Closure$__0-0.$VB$Local_y As Integer"
  IL_0038:  call       "Function System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle) As System.Reflection.FieldInfo"
  IL_003d:  call       "Function System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo) As System.Linq.Expressions.MemberExpression"
  IL_0042:  call       "Function System.Linq.Expressions.Expression.AddChecked(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression) As System.Linq.Expressions.BinaryExpression"
  IL_0047:  ldc.i4.1
  IL_0048:  newarr     "System.Linq.Expressions.ParameterExpression"
  IL_004d:  dup
  IL_004e:  ldc.i4.0
  IL_004f:  ldloc.1
  IL_0050:  stelem.ref
  IL_0051:  call       "Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of Integer, Integer))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of Integer, Integer))"
  IL_0056:  pop
  IL_0057:  ret
}
    ]]>)
        End Sub

        <Fact>
        Public Sub TypeInference()
            Dim source = <compilation>
                             <%= _exprTesting %>
                             <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq.Expressions


Module Module1
    Sub Goo(Of A, B, C)(q As Expression(Of Func(Of A, B, C)))
        Console.WriteLine("Infer A={0}", GetType(A))
        Console.WriteLine("Infer B={0}", GetType(B))
        Console.WriteLine("Infer C={0}", GetType(C))
    End Sub

    Sub Main()
        Goo(Function(x As Decimal, y As String) 4)
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:=<![CDATA[
Infer A=System.Decimal
Infer B=System.String
Infer C=System.Int32
]]>).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub QueryWhereSelect()
            Dim source = <compilation>
                             <%= _exprTesting %>
                             <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq.Expressions

Class QueryAble
    Public Function [Select](x As Expression(Of Func(Of Integer, Integer))) As Queryable
        System.Console.Write("Select ")
        'TODO: Check expression tree
        Return Me
    End Function

    Public Function Where(x As Expression(Of Func(Of Integer, Boolean))) As Queryable
        System.Console.Write("Where ")
        'TODO: Check expression tree
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim r = From a In q Where a > 0 Select a
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:=<![CDATA[Where Select]]>).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub QueryGroupBy()
            Dim source = <compilation>
                             <%= _exprTesting %>
                             <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq.Expressions

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Expression(Of Func(Of T, S))) As QueryAble(Of S)
        Console.Write("Select;")
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Expression(Of Func(Of T, K)), item As Expression(Of Func(Of T, I)), into As Expression(Of Func(Of K, QueryAble(Of I), R))) As QueryAble(Of R)
        System.Console.Write("GroupBy 1;")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Expression(Of Func(Of T, K)), into As Expression(Of Func(Of K, QueryAble(Of T), R))) As QueryAble(Of R)
        System.Console.Write("GroupBy 2;")
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class


Module Module1
    Sub Main()
        Dim q As New QueryAble(Of Integer)(4)

        Dim r = From a In q Group x = a By a Into Group Select a
        Dim s = From a In q Group By a Into Group Select a
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:="GroupBy 1;Select;GroupBy 2;Select;").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub QueryGroupJoin()
            Dim source = <compilation>
                             <%= _exprTesting %>
                             <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Linq.Expressions

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Expression(Of Func(Of T, S))) As QueryAble(Of S)
        Console.Write("Select;")
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Expression(Of Func(Of T, K)), innerKey As Expression(Of Func(Of I, K)), x As Expression(Of Func(Of T, QueryAble(Of I), R))) As QueryAble(Of R)
        System.Console.Write("GroupJoin;")
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class


Module Module1
    Sub Main()
        Dim q As New QueryAble(Of Integer)(4)

        Dim r = From a In q Group Join b In q On a Equals b Into Group
    End Sub
End Module]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:="GroupJoin;").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub OverloadResolutionDisambiguation1()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Linq.Expressions

Delegate Function MyFunc(Of U)() As U

Class X(Of T)
    Public Sub A(p As Func(Of T), q As Integer)
        Console.Write("A1 ")
    End Sub

    Public Sub A(p As Func(Of T), q As T)
        Console.Write("A2 ")
    End Sub

    Public Sub B(p As Func(Of T), q As T)
        Console.Write("B1 ")
    End Sub

    Public Sub B(p As Func(Of T), q As Integer)
        Console.Write("B2 ")
    End Sub

    Public Sub C(p As MyFunc(Of T), q As Integer)
        Console.Write("C1 ")
    End Sub

    Public Sub C(p As Func(Of T), q As T)
        Console.Write("C2 ")
    End Sub

    Public Sub D(p As MyFunc(Of T), q As T)
        Console.Write("D1 ")
    End Sub

    Public Sub D(p As Func(Of T), q As Integer)
        Console.Write("D2 ")
    End Sub

    Public Sub E(p As MyFunc(Of T), q As Integer)
        Console.Write("E1 ")
    End Sub

    Public Sub E(p As Expression(Of Func(Of T)), q As T)
        Console.Write("E2 ")
    End Sub

    Public Sub F(p As MyFunc(Of T), q As T)
        Console.Write("F1 ")
    End Sub

    Public Sub F(p As Expression(Of Func(Of T)), q As Integer)
        Console.Write("F2 ")
    End Sub

End Class

Module Module1

    Sub Main()
        Dim instance As New X(Of Integer)

        instance.A(Function() 1, 7)
        instance.B(Function() 1, 7)
        instance.C(Function() 1, 7)
        instance.D(Function() 1, 7)
        instance.E(Function() 1, 7)
        instance.F(Function() 1, 7)
    End Sub

End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:=<![CDATA[A1 B2 C1 D2 E1 F2]]>)
        End Sub

        <Fact>
        Public Sub OverloadResolutionDisambiguation2()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Delegate Function D1(x As Integer) As Integer
Delegate Function D2(y As Integer) As Long

Module Module1

    Sub Main()
        f(Nothing)
        f(Function(a) 4)
        g(Function(a) 4)
    End Sub

    Sub f(x As Expression(Of D1))
        Console.Write("f1 ")
    End Sub

    Sub f(x As Expression(Of D2))
        Console.Write("f2 ")
    End Sub

    Sub g(x As Expression(Of D1))
        Console.Write("g1 ")
    End Sub

    Sub g(x As D2)
        Console.Write("g2 ")
    End Sub
End Module]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:=<![CDATA[f1 f1 g1]]>)
        End Sub

        <WorkItem(545757, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545757")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Bug_14402()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        'System.Console.WriteLine("Dev10 #531876 regression test.")
        Dim s1_a As Boolean?
        Dim s1_b As Char? = "1"c
        Dim s1_c As Object = If(s1_a, s1_b)
        Dim expr As Expression(Of Func(Of Object)) = Function() If(s1_a, s1_b)
        System.Console.WriteLine(expr)
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:="() => (value(Form1+_Closure$__0-0).$VB$Local_s1_a ?? Convert(value(Form1+_Closure$__0-0).$VB$Local_s1_b))").VerifyDiagnostics()
        End Sub

        <WorkItem(531513, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531513")>
        <Fact>
        Public Sub Bug_18234()
            Dim file = <file name="a.vb"><![CDATA[
Option Strict Off 

Module Program
    Private d As Date
    Private v As Integer
    Private x As Decimal
    Sub Main(args As String())
        Dim queryObj As New QueryHelper(Of String)
        Dim scenario4 = From s In queryObj
                        Where Not TypeOf CObj(d) Is Double = (x + v) AndAlso True
                        Select s
    End Sub
End Module
]]></file>

            TestExpressionTrees(file, <![CDATA[
Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    AndAlso(
      Not(
        Equal(
          Convert(
            TypeIs(
              Convert(
                MemberAccess(
                  <NULL>
                  -> d
                  type: System.DateTime
                )
                type: System.Object
              )
              Type Operand: System.Double
              type: System.Boolean
            )
            method: System.Decimal ToDecimal(Boolean) in Microsoft.VisualBasic.CompilerServices.Conversions
            type: System.Decimal
          )
          Add(
            MemberAccess(
              <NULL>
              -> x
              type: System.Decimal
            )
            Convert(
              MemberAccess(
                <NULL>
                -> v
                type: System.Int32
              )
              method: System.Decimal op_Implicit(Int32) in System.Decimal
              type: System.Decimal
            )
            method: System.Decimal Add(System.Decimal, System.Decimal) in System.Decimal
            type: System.Decimal
          )
          method: Boolean op_Equality(System.Decimal, System.Decimal) in System.Decimal
          type: System.Boolean
        )
        type: System.Boolean
      )
      Constant(
        True
        type: System.Boolean
      )
      type: System.Boolean
    )
  }
  return type: System.Boolean
  type: System.Func`2[System.String,System.Boolean]
)

Lambda(
  Parameter(
    s
    type: System.String
  )
  body {
    Parameter(
      s
      type: System.String
    )
  }
  return type: System.String
  type: System.Func`2[System.String,System.String]
)
]]>)
        End Sub

        <WorkItem(545738, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545738")>
        <Fact>
        Public Sub Bug_14377a()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System.Linq.Expressions
Imports System
Module Module1
    Sub Main()
        Dim e As Expression(Of Func(Of Integer, Integer)) = Function(ByVal x) x + 1
        Dim f As Func(Of Integer, Integer) = Function(ByVal x) x + 1
        Console.WriteLine(f(9))
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:="10").VerifyDiagnostics()
        End Sub

        <WorkItem(547151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547151")>
        <Fact>
        Public Sub Bug_18156()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Friend Module ExprTreeAdd01mod
    Sub ExprTreeAdd01()
        Dim queryObj As New QueryHelper(Of String)
        Dim str As String = "abc"
        Dim b As Boolean = False
        Dim scenario1 = From s In queryObj Where str + b Select s
        Dim byteVal As Byte = 7
        Dim scenario2 = From s In queryObj Where str + byteVal Select s
        Dim c As Char = "c"
        Dim scenario3 = From s In queryObj Where c + str Select s
        Dim dt As DateTime = #1/1/2006#
        Dim scenario4 = From s In queryObj Where str + dt Select s
        Dim d As Decimal = 5.77
        Dim scenario5 = From s In queryObj Where d + str Select s
        Dim dbl As Double = 5.99
        Dim scenario6 = From s In queryObj Where str + dbl Select s
        Dim i As Integer = 5
        Dim scenario7 = From s In queryObj Where str + i Select s
        Dim lng As Long = 9999999
        Dim scenario8 = From s In queryObj Where str + lng Select s
        Dim sb As SByte = 9
        Dim scenario9 = From s In queryObj Where str + sb Select s
        Dim sh As Short = 5
        Dim scenario10 = From s In queryObj Where str + sh Select s
        Dim sng As Single = 5.5
        Dim scenario11 = From s In queryObj Where str + sng Select s
        Dim str2 As String = "string2"
        Dim scenario12 = From s In queryObj Where CBool(str + str2) Select s
        Dim ui As UInteger = 8
        Dim scenario13 = From s In queryObj Where str + ui Select s
        Dim ul As ULong = 555
        Dim scenario14 = From s In queryObj Where str + ul Select s
        Dim us As UShort = 7
        Dim scenario15 = From s In queryObj Where str + us Select s
        Dim obj As Object = New Object
        Dim scenario16 = From s In queryObj Where str + obj Select s
    End Sub
End Module
]]></file>
            VerifyExpressionTreesDiagnostics(file, <errors></errors>)
        End Sub

        <WorkItem(957927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957927")>
        <Fact>
        Public Sub Bug957927()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
imports System
imports System.Linq.Expressions

class Test

    shared Sub Main()
        System.Console.WriteLine(GetFunc(Of Integer)()().ToString())
    End Sub

	shared Function GetFunc(Of T)() As Func(Of Expression(Of Func(Of T,T)))
		Dim x = 10
		return Function()
			Dim y = x 
			return Function(m As T) m
			End Function
	End Function	
end class
                            ]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:="m => m").VerifyDiagnostics()
        End Sub

        <WorkItem(3906, "https://github.com/dotnet/roslyn/issues/3906")>
        <Fact>
        Public Sub GenericField01()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System

Public Class Module1
    Public Class S(Of T)
        Public x As T
    End Class

    Public Shared Function SomeFunc(Of A)(selector As System.Linq.Expressions.Expression(Of Func(Of Object, A))) As Object
        Return Nothing
    End Function

    Public Shared Sub CallIt(Of T)(p As T)
        Dim goodF As Func(Of Object, Object) = Function(xs) SomeFunc(Of S(Of T))(Function(e) New S(Of T)())
        Dim z1 = goodF(3)

        Dim badF As Func(Of Object, Object) = Function(xs)
                                                  Return SomeFunc(Of S(Of T))(Function(e) New S(Of T) With {.x = p})
                                              End Function
        Dim z2 = badF(3)
    End Sub

    Public Shared Sub Main()
        CallIt(Of Integer)(3)
    End Sub
End Class

                            ]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 expectedOutput:="").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ExpressionTrees_MyBaseMyClass()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Call (New Clazz(Of Object)()).Main()
        Call (New Derived()).Main()
    End Sub
End Module

Public Class Base
    Public F As Integer
    Public Property P1 As String
    Default Public Property P2(i As Integer) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
End Class

Public Class Derived
    Inherits Base

    Public Shadows F As Integer
    Public Shadows Property P1 As String
    Default Public Shadows Property P2(i As Integer) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property

    Sub Main()
        Console.WriteLine((DirectCast(Function() Me.F.ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyClass.F.ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyBase.F.ToString(), Expression(Of Func(Of Object)))).Dump)

        Console.WriteLine((DirectCast(Function() Me.P1.ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyClass.P1.ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyBase.P1.ToString(), Expression(Of Func(Of Object)))).Dump)

        Console.WriteLine((DirectCast(Function() Me.P2(1).ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyClass.P2(1).ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyBase.P2(1).ToString(), Expression(Of Func(Of Object)))).Dump)
    End Sub
End Class

Class Clazz(Of T As {Class, New})
    Sub Main()
        Console.WriteLine((DirectCast(Function() MyBase.ToString(), Expression(Of Func(Of Object)))).Dump)
        Console.WriteLine((DirectCast(Function() MyClass.ToString(), Expression(Of Func(Of Object)))).Dump)
    End Sub
End Class
]]></file>

            TestExpressionTrees(file, <![CDATA[
Lambda(
  body {
    Convert(
      Call(
        Constant(
          Clazz`1[System.Object]
          type: Clazz`1[System.Object]
        )
        method: System.String $VB$ClosureStub_ToString_MyBase() in Clazz`1[System.Object] (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        Constant(
          Clazz`1[System.Object]
          type: Clazz`1[System.Object]
        )
        method: System.String $VB$ClosureStub_ToString_MyBase() in Clazz`1[System.Object] (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        MemberAccess(
          Constant(
            Derived
            type: Derived
          )
          -> F
          type: System.Int32
        )
        method: System.String ToString() in System.Int32 (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        MemberAccess(
          Constant(
            Derived
            type: Derived
          )
          -> F
          type: System.Int32
        )
        method: System.String ToString() in System.Int32 (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        MemberAccess(
          Constant(
            Derived
            type: Base
          )
          -> F
          type: System.Int32
        )
        method: System.String ToString() in System.Int32 (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        MemberAccess(
          Constant(
            Derived
            type: Derived
          )
          -> P1
          type: System.String
        )
        method: System.String ToString() in System.String (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        Call(
          Constant(
            Derived
            type: Derived
          )
          method: System.String $VB$ClosureStub_get_P1_MyClass() in Derived (
          )
          type: System.String
        )
        method: System.String ToString() in System.String (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        Call(
          Constant(
            Derived
            type: Derived
          )
          method: System.String $VB$ClosureStub_get_P1_MyBase() in Derived (
          )
          type: System.String
        )
        method: System.String ToString() in System.String (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        Call(
          Constant(
            Derived
            type: Derived
          )
          method: System.String get_P2(Int32) in Derived (
            Constant(
              1
              type: System.Int32
            )
          )
          type: System.String
        )
        method: System.String ToString() in System.String (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        Call(
          Constant(
            Derived
            type: Derived
          )
          method: System.String $VB$ClosureStub_get_P2_MyClass(Int32) in Derived (
            Constant(
              1
              type: System.Int32
            )
          )
          type: System.String
        )
        method: System.String ToString() in System.String (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)

Lambda(
  body {
    Convert(
      Call(
        Call(
          Constant(
            Derived
            type: Derived
          )
          method: System.String $VB$ClosureStub_get_P2_MyBase(Int32) in Derived (
            Constant(
              1
              type: System.Int32
            )
          )
          type: System.String
        )
        method: System.String ToString() in System.String (
        )
        type: System.String
      )
      type: System.Object
    )
  }
  return type: System.Object
  type: System.Func`1[System.Object]
)
]]>)
        End Sub

#End Region

#Region "Errors"

        <Fact>
        Public Sub ArrayCreationAndInitialization_BC36603()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(1, 2) {{x, y, ""}, {x, y, ""}})(x, 0)), Expression(Of Func(Of Integer, String, Object)))).Dump)
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36603: Multi-dimensional array cannot be converted to an expression tree.
        Console.WriteLine((DirectCast(Function(x, y) (Function(a) a)((New String(1, 2) {{x, y, ""}, {x, y, ""}})(x, 0)), Expression(Of Func(Of Integer, String, Object)))).Dump)
                                                                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(531526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531526")>
        <Fact>
        Public Sub ByRefParamsInExpressionLambdas_BC36538()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Program
    Delegate Function MyFunc(Of T)(ByRef x As T, ByVal y As T) As T
    Delegate Function MyFunc2(Of T)(ByRef x As T) As T
    Delegate Function MyFuncV(Of T)(ByVal x As T, ByVal y As T) As T
    Delegate Function MyFunc2V(Of T)(ByVal x As T) As T
 
    Sub Goo(Of T)(ByVal x As Expression(Of MyFunc(Of T)))
    End Sub
    Sub Goo2(Of T)(ByVal x As Expression(Of MyFunc2(Of T)))
    End Sub
    Sub Goo3(Of T)(ByVal x As Expression(Of MyFuncV(Of T)))
    End Sub
    Sub Goo4(Of T)(ByVal x As Expression(Of MyFunc2V(Of T)))
    End Sub
 
    Sub Main(args As String())
        'COMPILEERROR: BC36538, "Function(ByRef x As Double, y As Integer) 1.1"
        Goo(Function(ByRef x As Double, y As Integer) 1.1) 'Causes compile time error
        'COMPILEERROR: BC36538, "Function(ByRef x As Double) 1.1"
        Goo2(Function(ByRef x As Double) 1.1) 'Regression Scenario - Previously No compile time error 
        'COMPILEERROR: BC36538, "Function() 1.1"
        Goo(Function() 1.1)
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36538: References to 'ByRef' parameters cannot be converted to an expression tree.
        Goo(Function(ByRef x As Double, y As Integer) 1.1) 'Causes compile time error
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36538: References to 'ByRef' parameters cannot be converted to an expression tree.
        Goo2(Function(ByRef x As Double) 1.1) 'Regression Scenario - Previously No compile time error 
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36538: References to 'ByRef' parameters cannot be converted to an expression tree.
        Goo(Function() 1.1)
            ~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AnonymousObjectCreationExpression_BC36548()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Form1
    Sub Main()
        Dim ret0 As Expression(Of Func(Of Integer, String, Object)) = Function(x, y) New With {.A = x, .B = y, .C = .A + .B}.C
        Console.WriteLine(ret0.Dump)
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36548: Cannot convert anonymous type to an expression tree because a property of the type is used to initialize another property.
        Dim ret0 As Expression(Of Func(Of Integer, String, Object)) = Function(x, y) New With {.A = x, .B = y, .C = .A + .B}.C
                                                                                                                    ~~
BC36548: Cannot convert anonymous type to an expression tree because a property of the type is used to initialize another property.
        Dim ret0 As Expression(Of Func(Of Integer, String, Object)) = Function(x, y) New With {.A = x, .B = y, .C = .A + .B}.C
                                                                                                                         ~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiStatementLambda_BC36675a()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim l1 As Expression(Of Func(Of Object)) = Function() Function() Function()
                                                                             Return Nothing
                                                                         End Function
        Dim l2 As Expression(Of Func(Of Object)) = Function() Function() Sub()
                                                                             Console.WriteLine()
                                                                         End Sub
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36675: Statement lambdas cannot be converted to expression trees.
        Dim l1 As Expression(Of Func(Of Object)) = Function() Function() Function()
                                                                         ~~~~~~~~~~~
BC36675: Statement lambdas cannot be converted to expression trees.
        Dim l2 As Expression(Of Func(Of Object)) = Function() Function() Sub()
                                                                         ~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub MultiStatementLambda_BC36675b()
            Dim file = <file name="expr.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim l3 As Expression(Of Func(Of Object)) = Function() Sub() If True Then Console.WriteLine()
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36675: Statement lambdas cannot be converted to expression trees.
        Dim l3 As Expression(Of Func(Of Object)) = Function() Sub() If True Then Console.WriteLine()
                                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(545804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545804")>
        <Fact>
        Public Sub Bug_14469()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports System.Linq.Expressions

Module Module1
    Sub Main()
        Dim x15 As Expression(Of Action) = Sub() glob = Function()
                                                            Return Sub()

                                                                   End Sub
                                                        End Function
    End Sub
    Public glob As Object = Nothing
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36534: Expression cannot be converted into an expression tree.
        Dim x15 As Expression(Of Action) = Sub() glob = Function()
                                                 ~~~~~~~~~~~~~~~~~~
BC36675: Statement lambdas cannot be converted to expression trees.
        Dim x15 As Expression(Of Action) = Sub() glob = Function()
                                                        ~~~~~~~~~~~
BC36675: Statement lambdas cannot be converted to expression trees.
                                                            Return Sub()
                                                                   ~~~~~~
</errors>)
        End Sub

        <WorkItem(531420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531420")>
        <Fact>
        Public Sub ExprTreeLiftedUserDefinedOperatorsWithNullableResult_Binary_BC36534()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports Microsoft.VisualBasic
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim l2 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) x = y
        Console.WriteLine(l2.Dump)
        Dim l3 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) x + y
        Console.WriteLine(l3.Dump)
    End Sub
End Module

Structure Test1
    Public Shared Operator +(x As Test1, y As Test1) As Test1?
        Return Nothing
    End Operator
    Public Shared Operator =(x As Test1, y As Test1) As Test1?
        Return Nothing
    End Operator
    Public Shared Operator <>(x As Test1, y As Test1) As Test1?
        Return Nothing
    End Operator
End Structure
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36534: Expression cannot be converted into an expression tree.
        Dim l2 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) x = y
                                                                                ~~~~~
BC36534: Expression cannot be converted into an expression tree.
        Dim l3 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) x + y
                                                                                ~~~~~
</errors>)
        End Sub

        <WorkItem(531423, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531423")>
        <Fact>
        Public Sub ExprTreeUserDefinedAndAlsoOrElseWithNullableResult_BC36534()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports Microsoft.VisualBasic
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim l2 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) if(x AndAlso y, x, y)
        Console.WriteLine(l2.Dump)
        Dim l3 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) if(x OrElse y, x, y)
        Console.WriteLine(l3.Dump)
    End Sub
End Module

Structure Test1
    Public Shared Operator And(x As Test1, y As Test1) As Test1?
        Return Nothing
    End Operator
    Public Shared Operator Or(x As Test1, y As Test1) As Test1?
        Return Nothing
    End Operator
    Public Shared Operator IsTrue(x As Test1) As Boolean
        Return Nothing
    End Operator
    Public Shared Operator IsFalse(x As Test1) As Boolean
        Return Nothing
    End Operator
End Structure
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36534: Expression cannot be converted into an expression tree.
        Dim l2 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) if(x AndAlso y, x, y)
                                                                                   ~~~~~~~~~~~
BC36534: Expression cannot be converted into an expression tree.
        Dim l3 As Expression(Of Func(Of Test1?, Test1, Test1)) = Function(x, y) if(x OrElse y, x, y)
                                                                                   ~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(531424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531424")>
        <Fact>
        Public Sub ExprTreeUserDefinedUnaryWithNullableResult_BC36534()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports Microsoft.VisualBasic
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim l9 As Expression(Of Func(Of Test1?, Test1)) = Function(x) -x
        Console.WriteLine(l9.Dump)
        Dim l10 As Expression(Of Func(Of Test1?, Test1?)) = Function(x) -x
        Console.WriteLine(l10.Dump)
    End Sub
End Module

Structure Test1
    Public Shared Operator -(x As Test1) As Test1?
        Return Nothing
    End Operator
End Structure
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36534: Expression cannot be converted into an expression tree.
        Dim l9 As Expression(Of Func(Of Test1?, Test1)) = Function(x) -x
                                                                      ~~
BC36534: Expression cannot be converted into an expression tree.
        Dim l10 As Expression(Of Func(Of Test1?, Test1?)) = Function(x) -x
                                                                        ~~
</errors>)
        End Sub

        <Fact>
        Public Sub ExprTree_LateBinding_BC36604()
            Dim file = <file name="expr.vb"><![CDATA[
Option Strict Off 
Imports System
Imports Microsoft.VisualBasic
Imports System.Linq.Expressions
Imports System.Collections.Generic

Module Form1

    Dim queryObj As New QueryHelper(Of String)
    Public x
    Function Goo(ByVal x As Long)
        Return Nothing
    End Function
    Function Goo(ByVal x As Short)
        Return Nothing
    End Function

    Sub Main()
        Dim scenario1 = From s In queryObj Where x.Goo() Select s
        Dim scenario2 = From s In queryObj Where Goo(x) Select s
        Dim scenario3 = From s In queryObj Where CBool(Goo(x)) Select s
    End Sub
End Module
]]></file>

            VerifyExpressionTreesDiagnostics(file,
<errors>
BC36604: Late binding operations cannot be converted to an expression tree.
        Dim scenario1 = From s In queryObj Where x.Goo() Select s
                                                 ~~~~~~~
BC36604: Late binding operations cannot be converted to an expression tree.
        Dim scenario2 = From s In queryObj Where Goo(x) Select s
                                                 ~~~~~~
BC36604: Late binding operations cannot be converted to an expression tree.
        Dim scenario3 = From s In queryObj Where CBool(Goo(x)) Select s
                                                       ~~~~~~
</errors>)
        End Sub

        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact>
        Public Sub MissingMember_System_Type__GetTypeFromHandle()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Linq.Expressions
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Class MulticastDelegate
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
    Public Structure Nullable(Of T)
    End Structure
    Public Class [Type]
    End Class
    Public Interface IAsyncResult
    End Interface
    Public Class AsyncCallback
    End Class
End Namespace
Namespace System.Collections.Generic
    Public Interface IEnumerable(Of T)
    End Interface
End Namespace
Namespace System.Linq.Expressions
    Public Class Expression
        Public Shared Function [New](t As Type) As Expression
            Return Nothing
        End Function
        Public Shared Function Lambda(Of T)(e As Expression, args As Expression()) As Expression(Of T)
            Return Nothing
        End Function
    End Class
    Public Class Expression(Of T)
    End Class
    Public Class ParameterExpression
        Inherits Expression
    End Class
End Namespace
Delegate Function D() As C
Class C
    Shared E As Expression(Of D) = Function() New C()
End Class
]]></file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Type.GetTypeFromHandle' is not defined.
    Shared E As Expression(Of D) = Function() New C()
                                              ~~~~~~~
</errors>)
        End Sub

        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact>
        Public Sub MissingMember_System_Reflection_FieldInfo__GetFieldFromHandle()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Linq.Expressions
Imports System.Reflection
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Class MulticastDelegate
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
    Public Structure Nullable(Of T)
    End Structure
    Public Class [Type]
    End Class
    Public Class Array
    End Class
    Public Interface IAsyncResult
    End Interface
    Public Class AsyncCallback
    End Class
End Namespace
Namespace System.Collections.Generic
    Public Interface IEnumerable(Of T)
    End Interface
End Namespace
Namespace System.Linq.Expressions
    Public Class Expression
        Public Shared Function Field(e As Expression, f As FieldInfo) As Expression
            Return Nothing
        End Function
        Public Shared Function Lambda(Of T)(e As Expression, args As Expression()) As Expression(Of T)
            Return Nothing
        End Function
    End Class
    Public Class Expression(Of T)
    End Class
    Public Class ParameterExpression
        Inherits Expression
    End Class
End Namespace
Namespace System.Reflection
    Public Class FieldInfo
    End Class
End Namespace
Delegate Function D() As Object
Class A
    Shared F As Object = Nothing
    Shared G As Expression(Of D) = Function() F
End Class
Class B(Of T)
    Shared F As Object = Nothing
    Shared G As Expression(Of D) = Function() F
End Class
]]></file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.FieldInfo.GetFieldFromHandle' is not defined.
    Shared G As Expression(Of D) = Function() F
                                              ~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.FieldInfo.GetFieldFromHandle' is not defined.
    Shared G As Expression(Of D) = Function() F
                                              ~
</errors>)
        End Sub

        <WorkItem(797996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797996")>
        <Fact>
        Public Sub MissingMember_System_Reflection_MethodBase__GetMethodFromHandle()
            Dim compilation = CreateEmptyCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Linq.Expressions
Imports System.Reflection
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Class MulticastDelegate
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
    Public Structure Nullable(Of T)
    End Structure
    Public Class [Type]
        Public Shared Function GetTypeFromHandle(h As RuntimeTypeHandle) As [Type]
            Return Nothing
        End Function
    End Class
    Public Structure RuntimeTypeHandle
    End Structure
    Public Class Array
    End Class
    Public Interface IAsyncResult
    End Interface
    Public Class AsyncCallback
    End Class
End Namespace
Namespace System.Collections.Generic
    Public Interface IEnumerable(Of T)
    End Interface
End Namespace
Namespace System.Linq.Expressions
    Public Class Expression
        Public Shared Function [Call](e As Expression, m As MethodInfo, args As Expression()) As Expression
            Return Nothing
        End Function
        Public Shared Function Constant(o As Object, t As Type) As Expression
            Return Nothing
        End Function
        Public Shared Function Convert(e As Expression, t As Type) As Expression
            Return Nothing
        End Function
        Public Shared Function Lambda(Of T)(e As Expression, args As Expression()) As Expression(Of T)
            Return Nothing
        End Function
        Public Shared Function [New](c As ConstructorInfo, args As IEnumerable(Of Expression)) As Expression
            Return Nothing
        End Function
    End Class
    Public Class Expression(Of T)
    End Class
    Public Class ParameterExpression
        Inherits Expression
    End Class
End Namespace
Namespace System.Reflection
    Public Class ConstructorInfo
    End Class
    Public Class MethodInfo
    End Class
End Namespace
Delegate Function D() As Object
Class A
    Shared F As Expression(Of D) = Function() New A(Nothing)
    Shared G As Expression(Of D) = Function() M()
    Shared Function M() As Object
        Return Nothing
    End Function
    Sub New(o As Object)
    End Sub
End Class
Class B(Of T)
    Shared F As Expression(Of D) = Function() New A(Nothing)
    Shared G As Expression(Of D) = Function() M()
    Shared Function M() As Object
        Return Nothing
    End Function
    Sub New(o As Object)
    End Sub
End Class
]]></file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
    BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
    Shared F As Expression(Of D) = Function() New A(Nothing)
                                              ~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
    Shared G As Expression(Of D) = Function() M()
                                              ~~~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
    Shared F As Expression(Of D) = Function() New A(Nothing)
                                              ~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.MethodBase.GetMethodFromHandle' is not defined.
    Shared G As Expression(Of D) = Function() M()
                                              ~~~
</errors>)
        End Sub

#End Region

#Region "Expression Tree Test Helpers"

        Private ReadOnly _exprTesting As XElement = <file name="exprlambdatest.vb"><%= ExpTreeTestResources.ExprLambdaUtils %></file>

        Private ReadOnly _queryTesting As XElement = <file name="QueryHelper.vb"><%= ExpTreeTestResources.QueryHelper %></file>

#End Region

        <WorkItem(808608, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808608")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Bug808608_01()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Friend Module M

    Structure X
        Public x As Integer
        Public Shared Operator =(ByVal x As X, ByVal y As X) As Boolean
            Return x.x = y.x
        End Operator
        Public Shared Operator <>(ByVal x As X, ByVal y As X) As Boolean
            Return x.x <> y.x
        End Operator
    End Structure

    Sub Main()
        Goo1(Of X)(Nothing)
        Goo1(New X())
        Goo2(Of X)()
    End Sub

    Sub Goo1(Of T As Structure)(ByVal x As T?)
        ExprTest(Function() x Is Nothing)
        ExprTest(Function() x IsNot Nothing)
        ExprTest(Function() Nothing Is x)
        ExprTest(Function() Nothing IsNot x)
    End Sub

    Sub Goo2(Of T As Structure)()
        ExprTest(Function() CType(Nothing, X?))
        ExprTest(Function() CType(Nothing, X))
        ExprTest(Function() CType(Nothing, T?))
        ExprTest(Function() CType(Nothing, T))
    End Sub

    Public Sub ExprTest(Of T)(expr As Expression(Of Func(Of T)))
        Console.WriteLine(expr.ToString)
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x == null)
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x != null)
() => (null == value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => (null != value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x == null)
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x != null)
() => (null == value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => (null != value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => null
() => new X()
() => Convert(null)
() => default(X)
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(808608, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808608")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Bug808608_02()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Friend Module M

    Structure X
        Public x As Integer
    End Structure

    Sub Main()
        Goo1(Of X)(Nothing)
        Goo1(New X())
        Goo2(Of X)()
    End Sub

    Sub Goo1(Of T As Structure)(ByVal x As T?)
        ExprTest(Function() x Is Nothing)
        ExprTest(Function() x IsNot Nothing)
        ExprTest(Function() Nothing Is x)
        ExprTest(Function() Nothing IsNot x)
    End Sub

    Sub Goo2(Of T As Structure)()
        ExprTest(Function() CType(Nothing, X?))
        ExprTest(Function() CType(Nothing, X))
        ExprTest(Function() CType(Nothing, T?))
        ExprTest(Function() CType(Nothing, T))
    End Sub

    Public Sub ExprTest(Of T)(expr As Expression(Of Func(Of T)))
        Console.WriteLine(expr.ToString)
    End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x == null)
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x != null)
() => (null == value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => (null != value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x == null)
() => (value(M+_Closure$__2-0`1[M+X]).$VB$Local_x != null)
() => (null == value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => (null != value(M+_Closure$__2-0`1[M+X]).$VB$Local_x)
() => null
() => new X()
() => Convert(null)
() => default(X)
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(808651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808651")>
        <Fact>
        Public Sub Bug808651()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Friend Module M
 
Sub Main()
Dim str As String = "123"
ExprTest(Function() str & Nothing)
ExprTest(Function() Nothing & str)
End Sub
 
Public Sub ExprTest(Of T)(expr As Expression(Of Func(Of T)))
 Console.WriteLine(expr.ToString)
End Sub
End Module
]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
() => Concat(value(M+_Closure$__0-0).$VB$Local_str, null)
() => Concat(null, value(M+_Closure$__0-0).$VB$Local_str)
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(1190, "https://github.com/dotnet/roslyn/issues/1190")>
        <Fact>
        Public Sub CollectionInitializers()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq.Expressions
Imports System.Runtime.CompilerServices

Namespace ConsoleApplication31
    Module Program
        Sub Main()
            Try
                Dim e1 As Expression(Of Func(Of Stack(Of Integer))) = Function() New Stack(Of Integer) From {42}
                System.Console.WriteLine("e1 => {0}", e1.ToString())
            Catch
                System.Console.WriteLine("In catch")
            End Try

            Dim e2 As Expression(Of Func(Of MyStack(Of Integer))) = Function() New MyStack(Of Integer) From {42}
            System.Console.WriteLine("e2 => {0}", e2.ToString())
            System.Console.WriteLine(e2.Compile()().Pop())
        End Sub
    End Module

    Module StackExtensions
        <Extension()>
        Public Sub Add(Of T)(s As Stack(Of T), x As T)
            s.Push(x)
        End Sub
    End Module

    Class MyStack(Of T)
        Inherits System.Collections.Generic.Stack(Of T)

        Public Sub Add(x As T)
            Me.Push(x)
        End Sub
    End Class
End Namespace
                            ]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
In catch
e2 => () => new MyStack`1() {Void Add(Int32)(42)}
42
]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(4524, "https://github.com/dotnet/roslyn/issues/4524")>
        <Fact>
        Public Sub PropertyAssignment()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Module Module1

    Public Interface IAddress
        Property City As String
    End Interface

    Public Class Address
        Implements IAddress

        Public Property City As String Implements IAddress.City

        Public Field As String

        Public Sub Verify(expression As Expression(Of Action(Of Address)))
            Console.WriteLine(expression.ToString())
            expression.Compile()(Me)
        End Sub

    End Class

    Public Class Customer
        Public Property Address As IAddress

        Public Sub DoWork(newValue As String)
            Address.City = newValue
        End Sub
    End Class

    Public Function ItIs(Of TValue)(match As Expression(Of Func(Of TValue, Boolean))) As TValue
    End Function

    Sub Main()
        Dim a As New Address

        a.Verify(Sub(x) x.City = ItIs(Of String)(Function(s) String.IsNullOrEmpty(s)))

        a.Verify(Sub(x) x.City = "aa")

        System.Console.WriteLine(a.City)
    End Sub

End Module

                            ]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
x => x.set_City(ItIs(s => IsNullOrEmpty(s)))
x => x.set_City("aa")
aa
]]>).VerifyDiagnostics()

        End Sub

        <WorkItem(4524, "https://github.com/dotnet/roslyn/issues/4524")>
        <Fact>
        Public Sub PropertyAssignmentParameterized()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Module Module1

    Public Interface IAddress
        Property City(i As Integer) As String
    End Interface

    Public Class Address
        Implements IAddress

        Private c As String

        Public Property City(i As Integer) As String Implements IAddress.City
            Get
                Return c & i
            End Get
            Set(value As String)
                c = value & i
            End Set
        End Property

        Public Field As String

        Public Sub Verify(expression As Expression(Of Action(Of Address)))
            Console.WriteLine(expression.ToString())
            expression.Compile()(Me)
        End Sub

    End Class

    Public Class Customer
        Public Property Address As IAddress

        Public Sub DoWork(newValue As String)
            Address.City(0) = newValue
        End Sub
    End Class

    Public Function ItIs(Of TValue)(match As Expression(Of Func(Of TValue, Boolean))) As TValue
    End Function

    Sub Main()
        Dim a As New Address

        a.Verify(Sub(x) x.City(1) = ItIs(Of String)(Function(s) String.IsNullOrEmpty(s)))

        Dim i As Integer = 2
        a.Verify(Sub(x) x.City(i) = "aa")

        System.Console.WriteLine(a.City(3))
    End Sub

End Module

                            ]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
x => x.set_City(1, ItIs(s => IsNullOrEmpty(s)))
x => x.set_City(value(Module1+_Closure$__4-0).$VB$Local_i, "aa")
aa23
]]>).VerifyDiagnostics()

        End Sub

        <WorkItem(4524, "https://github.com/dotnet/roslyn/issues/4524")>
        <Fact>
        Public Sub PropertyAssignmentCompound()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions

Module Module1

    Public Interface IAddress
        Property City As String
    End Interface

    Public Class Address
        Implements IAddress

        Public Property City As String Implements IAddress.City

        Public Field As String

        Public Sub Verify(expression As Expression(Of Action(Of Address)))
            expression.Compile()(Me)
        End Sub

    End Class

    Public Class Customer
        Public Property Address As IAddress

        Public Sub DoWork(newValue As String)
            Address.City = newValue
        End Sub
    End Class

    Public Function ItIs(Of TValue)(match As Expression(Of Func(Of TValue, Boolean))) As TValue
    End Function

    Sub Main()
        Dim a As New Address

        a.Verify(Sub(x) x.City = ItIs(Of String)(Function(s) String.IsNullOrEmpty(s)))

        a.Verify(Sub(x) x.City += "qq")

        System.Console.WriteLine(a.City)
    End Sub

End Module

                            ]]></file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe)

            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_ExpressionTreeNotSupported, "x.City += ""qq""").WithLocation(39, 25)
            )

        End Sub

        <WorkItem(6416, "https://github.com/dotnet/roslyn/issues/6416")>
        <Fact>
        Public Sub CapturedMe001()

            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[

Imports System
Imports System.Linq.Expressions


Class Module1
    Public Shared Sub Main()
        Dim v = New Module1()
        v.test()
    End Sub

    Public ReadOnly Property P1 As Integer
        Get
            Return 42
        End Get
    End Property

    Public Sub test()
        Dim local = 0
        Dim f As Func(Of Expression(Of Func(Of Integer))) =
                Function()
                    System.Console.WriteLine(P1 + local)
                    Return Function() P1
                End Function

        System.Console.WriteLine(DirectCast(f().Body, MemberExpression).Expression)
    End Sub
End Class



                            ]]></file>
                         </compilation>

            CompileAndVerify(source,
                 references:={Net40.References.SystemCore},
                 options:=TestOptions.ReleaseExe,
                 expectedOutput:=<![CDATA[
42
value(Module1)
]]>).VerifyDiagnostics()

        End Sub

    End Class
End Namespace
