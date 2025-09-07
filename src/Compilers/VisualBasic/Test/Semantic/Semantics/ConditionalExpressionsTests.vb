' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ConditionalExpressionsTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestTernaryConditionalSimple()
            TestCondition("if(True, 1, 2)", expectedType:="System.Int32")
            TestCondition("if(True, ""a""c, GetChar)", expectedType:="System.Char")
            TestCondition("if(False, GetString, ""abc"")", expectedType:="System.String")
            TestCondition("if(False, GetUserNonGeneric, New C())", expectedType:="C")

            TestCondition("if(True, ""a""c, GetString)", expectedType:="System.String")
            TestCondition("if(GetDouble > 1, GetInt, GetSingle)", expectedType:="System.Single")
            TestCondition("if(nothing, GetInt, GetObject)", expectedType:="System.Object")
            TestCondition("if(nothing, GetObject, ""1""c)", expectedType:="System.Object")

            TestCondition("if(True, GetByte, GetShort)", expectedType:="System.Int16")
            TestCondition("if(True, 1, GetShort)", expectedType:="System.Int32")
            TestCondition("if(True, 1.1, GetSingle)", expectedType:="System.Double")

            TestCondition("if(nothing, GetString, 1.234)", strict:=OptionStrict.On, errors:=
<errors>
BC36913: Cannot infer a common type because more than one type is possible.
        System.Console.WriteLine(if(nothing, GetString, 1.234))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(nothing, GetString, 1.234)", expectedType:="System.Object", strict:=OptionStrict.Custom, errors:=
<errors>
BC42021: Cannot infer a common type because more than one type is possible; 'Object' assumed.
        System.Console.WriteLine(if(nothing, GetString, 1.234))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(nothing, GetString, 1.234)", expectedType:="System.Object", strict:=OptionStrict.Off)
        End Sub

        <Fact>
        Public Sub TestBinaryConditionalSimple()
            TestCondition("if(Nothing, 2)", expectedType:="System.Int32")
            TestCondition("if(Nothing, GetIntOpt)", expectedType:="System.Nullable(Of System.Int32)")
            TestCondition("if(""string"", #1/1/1#)", expectedType:="System.String", strict:=OptionStrict.Off)
            TestCondition("if(CType(Nothing, String), ""str-123"")", expectedType:="System.String")
            TestCondition("if(CType(Nothing, String), Nothing)", expectedType:="System.String")
            TestCondition("if(Nothing, Nothing)", expectedType:="System.Object")
            TestCondition("if(CType(CType(Nothing, String), Object), GetIntOpt)", expectedType:="System.Object")
            TestCondition("if(CType(CType(Nothing, String), Object), GetInt)", expectedType:="System.Object")

            TestCondition("if(GetIntOpt, 2)", expectedType:="System.Int32")
            TestCondition("if(GetCharOpt(), ""a""c)", expectedType:="System.Char")
            TestCondition("if(GetString, ""abc"")", expectedType:="System.String")
            TestCondition("if(GetUserNonGeneric, New C())", expectedType:="C")

            TestCondition("if(GetString, ""a""c)", expectedType:="System.String")
            TestCondition("if(GetIntOpt, GetSingle)", expectedType:="System.Single")
            TestCondition("if(GetIntOpt, GetObject)", expectedType:="System.Object")
            TestCondition("if(GetObject, ""1""c)", expectedType:="System.Object")

            TestCondition("if(GetByteOpt(), GetShort)", expectedType:="System.Int16")
            TestCondition("if(GetShortOpt, 1)", expectedType:="System.Int32")
            TestCondition("if(GetSingleOpt, 1.1)", expectedType:="System.Double")

            TestCondition("if(GetString, 1.234)", strict:=OptionStrict.On, errors:=
<errors>
BC36913: Cannot infer a common type because more than one type is possible.
        System.Console.WriteLine(if(GetString, 1.234))
                                 ~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(GetString, 1.234)", expectedType:="System.Object", strict:=OptionStrict.Custom)
            TestCondition("if(GetString, 1.234)", expectedType:="System.Object", strict:=OptionStrict.Off)
        End Sub

        <Fact>
        Public Sub TestTernaryConditionalGenerics()
            TestCondition("if(GetBoolean(), GetUserGeneric(Of T), GetUserGeneric(Of T))", expectedType:="D(Of T)")
            TestCondition("if(Nothing, GetTypeParameter(Of T), GetTypeParameter(Of T))", expectedType:="T")

            TestCondition("if(GetBoolean(), GetUserGeneric(Of T), GetUserGeneric(Of T))", expectedType:="D(Of T)")
            TestCondition("if(GetBoolean(), GetUserGeneric(Of T), Nothing)", expectedType:="D(Of T)")

            TestCondition("if(GetBoolean(), GetUserGeneric(Of T), ""abc"")", strict:=OptionStrict.On, errors:=
<errors>
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
        System.Console.WriteLine(if(GetBoolean(), GetUserGeneric(Of T), "abc"))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(GetBoolean(), GetUserGeneric(Of T), ""abc"")", expectedType:="System.Object", strict:=OptionStrict.Custom, errors:=
<errors>
BC42021: Cannot infer a common type; 'Object' assumed.
        System.Console.WriteLine(if(GetBoolean(), GetUserGeneric(Of T), "abc"))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(GetBoolean(), GetUserGeneric(Of T), ""abc"")", expectedType:="System.Object", strict:=OptionStrict.Off)
        End Sub

        <Fact>
        Public Sub TestBinaryConditionalGenerics()
            TestCondition("if(GetUserGeneric(Of T), GetUserGeneric(Of T))", expectedType:="D(Of T)")
            TestCondition("if(GetTypeParameter(Of T), GetTypeParameter(Of T))", expectedType:="T")

            TestCondition("if(GetUserGeneric(Of T), GetUserGeneric(Of T))", expectedType:="D(Of T)")
            TestCondition("if(GetUserGeneric(Of T), Nothing)", expectedType:="D(Of T)")
            TestCondition("if(Nothing, GetUserGeneric(Of T))", expectedType:="D(Of T)")

            TestCondition("if(GetUserGeneric(Of T), ""abc"")", strict:=OptionStrict.On, errors:=
<errors>
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
        System.Console.WriteLine(if(GetUserGeneric(Of T), "abc"))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(GetUserGeneric(Of T), ""abc"")", expectedType:="System.Object", strict:=OptionStrict.Custom, errors:=
<errors>
BC42021: Cannot infer a common type; 'Object' assumed.
        System.Console.WriteLine(if(GetUserGeneric(Of T), "abc"))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(GetUserGeneric(Of T), ""abc"")", expectedType:="System.Object", strict:=OptionStrict.Off)
        End Sub

        <Fact()>
        Public Sub TestTernaryConditionalVariantGenerics()
            TestCondition("if(Nothing, GetVariantInterface(Of String, Integer)(), GetVariantInterface(Of Object, Integer)())", expectedType:="I(Of System.String, System.Int32)")
            TestCondition("if(Nothing, GetVariantInterface(Of Integer, String)(), GetVariantInterface(Of Integer, Object)())", expectedType:="I(Of System.Int32, System.Object)")
        End Sub

        <Fact()>
        Public Sub TestBinaryConditionalVariantGenerics()
            TestCondition("if(GetVariantInterface(Of String, Integer)(), GetVariantInterface(Of Object, Integer)())", expectedType:="I(Of System.String, System.Int32)")
            TestCondition("if(GetVariantInterface(Of Integer, String)(), GetVariantInterface(Of Integer, Object)())", expectedType:="I(Of System.Int32, System.Object)")
        End Sub

        <Fact>
        Public Sub TestTernaryConditionalNothingAndNoType()

            TestCondition("if(True, New Object(), nothing)", expectedType:="System.Object")

            TestCondition("if(1 > 2, nothing, nothing)", expectedType:="System.Object")

            TestCondition("if(nothing, nothing, nothing)", expectedType:="System.Object", strict:=OptionStrict.Off)
            TestCondition("if(nothing, nothing, nothing)", expectedType:="System.Object", strict:=OptionStrict.Custom)
            TestCondition("if(nothing, nothing, nothing)", expectedType:="System.Object", strict:=OptionStrict.On)

            TestCondition("if(""yes"", nothing, nothing)", expectedType:="System.Object", strict:=OptionStrict.Off)
            TestCondition("if(""yes"", nothing, nothing)", expectedType:="System.Object", strict:=OptionStrict.Custom)
            TestCondition("if(""yes"", nothing, nothing)", strict:=OptionStrict.On, errors:=
<errors>
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Boolean'.
        System.Console.WriteLine(if("yes", nothing, nothing))
                                    ~~~~~
</errors>)

            TestCondition("if(1 > 2, nothing, 1)", expectedType:="System.Int32")
            TestCondition("if(1 > 2, nothing, 1.5)", expectedType:="System.Double")

            TestCondition("if(1 > 2, AddressOf Func1, 1.5)", errors:=
<errors>
BC30581: 'AddressOf' expression cannot be converted to 'Double' because 'Double' is not a delegate type.
        System.Console.WriteLine(if(1 > 2, AddressOf Func1, 1.5))
                                           ~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(1 > 2, 1.5, AddressOf Func1)", errors:=
<errors>
BC30581: 'AddressOf' expression cannot be converted to 'Double' because 'Double' is not a delegate type.
        System.Console.WriteLine(if(1 > 2, 1.5, AddressOf Func1))
                                                ~~~~~~~~~~~~~~~
</errors>)

            TestCondition("if(1 > 2, AddressOf Func1, AddressOf Func2)", strict:=OptionStrict.On, errors:=
<errors>
BC36911: Cannot infer a common type.
        System.Console.WriteLine(if(1 > 2, AddressOf Func1, AddressOf Func2))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact>
        Public Sub TestBinaryConditionalNothingAndNoType()

            TestCondition("if(New Object(), nothing)", expectedType:="System.Object")
            TestCondition("if(nothing, nothing)", expectedType:="System.Object")
            TestCondition("if(GetIntOpt, nothing)", expectedType:="System.Nullable(Of System.Int32)")

            TestCondition("if(nothing, CByte(1))", expectedType:="System.Byte")
            TestCondition("if(nothing, 1.5)", expectedType:="System.Double")
            TestCondition("if(GetShortOpt(), GetByte)", expectedType:="System.Int16")

            TestCondition("if(GetShortOpt(), GetDouble)", expectedType:="System.Double")
            TestCondition("if(GetShortOpt(), GetSingleOpt())", expectedType:="System.Nullable(Of System.Single)")

            TestCondition("if(AddressOf Func1, 1.5)", errors:=
<errors>
BC30581: 'AddressOf' expression cannot be converted to 'Double' because 'Double' is not a delegate type.
        System.Console.WriteLine(if(AddressOf Func1, 1.5))
                                    ~~~~~~~~~~~~~~~
</errors>)
            TestCondition("if(1.5, AddressOf Func1)", errors:=
<errors>
BC30581: 'AddressOf' expression cannot be converted to 'Double' because 'Double' is not a delegate type.
        System.Console.WriteLine(if(1.5, AddressOf Func1))
                                         ~~~~~~~~~~~~~~~
</errors>)

            TestCondition("if(AddressOf Func1, AddressOf Func2)", strict:=OptionStrict.On, errors:=
<errors>
BC36911: Cannot infer a common type.
        System.Console.WriteLine(if(AddressOf Func1, AddressOf Func2))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub TestTernaryConditionalLambdas()
            TestCondition("if(1 > 2, Function() 1, Function() 1.2)", expectedType:="Function <generated method>() As System.Double")
            TestCondition("if(1 > 2, AddressOf Func1, Function(x As Integer) x)", expectedType:="Function <generated method>(x As System.Int32) As System.Int32")
        End Sub

        <Fact()>
        Public Sub TestBinaryConditionalLambdas()
            TestCondition("if(Function() 1, Function() 1.2)", expectedType:="Function <generated method>() As System.Double")
            TestCondition("if(AddressOf Func1, Function(x As Integer) x)", expectedType:="Function <generated method>(x As System.Int32) As System.Int32")
        End Sub

        Private Sub TestCondition(text As String,
                                  Optional errors As XElement = Nothing,
                                  Optional expectedType As String = Nothing,
                                  Optional strict As OptionStrict = OptionStrict.On)
            Dim source =
<compilation name="TestInvalidNumberOfParametersInIfOperator">
    <file name="a.vb">
Imports System

Class C

    Sub Test(Of T, U)()
        Dim vT As T = Nothing
        Dim vU As U = Nothing
        System.Console.WriteLine({0})
    End Sub

    Function GetBoolean() As Boolean
        Return True
    End Function

    Function GetInt() As Integer
        Return 1
    End Function

    Function GetIntOpt() As Integer?
        Return Nothing
    End Function

    Function GetByte() As Byte
        Return 2
    End Function

    Function GetByteOpt() As Byte?
        Return 2
    End Function

    Function GetShort() As Short
        Return 3
    End Function

    Function GetShortOpt() As Short?
        Return 3
    End Function

    Function GetChar() As Char
        Return "C"c
    End Function

    Function GetCharOpt() As Char?
        Return "C"c
    End Function

    Function GetDouble() As Double
        Return 1.5
    End Function

    Function GetDoubleOpt() As Double?
        Return 1.5
    End Function

    Function GetSingle() As Single
        Return 1.6
    End Function

    Function GetSingleOpt() As Single?
        Return 1.6
    End Function

    Function GetString() As String
        Return "--str--"
    End Function

    Function GetObject() As Object
        Return New Object
    End Function

    Function GetUserNonGeneric() As C
        Return New C
    End Function

    Function GetUserGeneric(Of T)() As D(Of T)
        Return New D(Of T)
    End Function

    Function GetTypeParameter(Of T)() As T
        Return Nothing
    End Function

    Function GetVariantInterface(Of T, U)() As I(Of T, U)
        Return Nothing
    End Function

    Function Func1(p1 As Integer) As Integer
        Return Nothing
    End Function

    Function Func2(p1 As Integer, p2 As Integer) As Integer
        Return Nothing
    End Function
End Class

Class D(Of T)
End Class

Interface I(Of In T, Out U)
End Interface
    </file>
</compilation>

            source.<file>.Single().Value = String.Format(source.<file>.Single().Value, text)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(strict))

            If errors IsNot Nothing Then
                CompilationUtils.AssertTheseDiagnostics(compilation, errors)
            Else
                CompilationUtils.AssertNoErrors(compilation)
            End If

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.BinaryConditionalExpression).AsNode()
            If node Is Nothing Then
                node = tree.FindNodeOrTokenByKind(SyntaxKind.TernaryConditionalExpression).AsNode()
                Dim ifOp = DirectCast(node, TernaryConditionalExpressionSyntax)

                Assert.Equal("System.Boolean", CompilationUtils.GetSemanticInfoSummary(model, ifOp.Condition).ConvertedType.ToTestDisplayString())
                If expectedType IsNot Nothing Then
                    Assert.Equal(expectedType, CompilationUtils.GetSemanticInfoSummary(model, ifOp.WhenTrue).ConvertedType.ToTestDisplayString())
                    Assert.Equal(expectedType, CompilationUtils.GetSemanticInfoSummary(model, ifOp.WhenFalse).ConvertedType.ToTestDisplayString())
                    Assert.Equal(expectedType, CompilationUtils.GetSemanticInfoSummary(model, ifOp).Type.ToTestDisplayString())
                End If
            Else
                If expectedType IsNot Nothing Then
                    node = tree.FindNodeOrTokenByKind(SyntaxKind.BinaryConditionalExpression).AsNode()
                    Dim ifOp = DirectCast(node, BinaryConditionalExpressionSyntax)

                    ' not guaranteed :: Assert.Equal(expectedType, model.GetSemanticInfoInParent(ifOp.FirstExpression).Type.ToTestDisplayString())
                    ' not guaranteed :: Assert.Equal(expectedType, model.GetSemanticInfoInParent(ifOp.SecondExpression).Type.ToTestDisplayString())
                    Assert.Equal(expectedType, CompilationUtils.GetSemanticInfoSummary(model, ifOp).Type.ToTestDisplayString())
                End If
            End If
        End Sub

        <Fact>
        Public Sub TestInvalidIfOperators()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TestInvalidNumberOfParametersInIfOperator">
    <file name="a.vb">
Class CX

    Public F1 As Integer = If(True, 2, 3, 4, 5)

    Public F2 As Integer = If 1

    Public F3 As Integer = If(1,

    Public F4 As Integer = If(True, 2

    Public F5 As Integer = If(True, 2, 3, 4

    Public F6 As Integer = If(True, 2, 3  4)

    Public F7 As Integer = If(True, 2 3 4)

    Public F8 As Integer = If(TestExpression:=True, TruePart:=1, FalsePart:=2)

    Public F9 As Integer = If(Nothing, FalsePart:=1)

    Public F10 As Integer = If(, 1)
    Public F10_ As Integer = If(1, )

    Public F11 As Integer = If(True, , 1)
    Public F11_ As Integer = If(True, 1,)

    Public F12 As Integer = If(True, abc, 23)

    Public F13 As Integer = If(True, S, 23)

    Public F14 As Integer = If(

    Public F15 As Integer = If()

    Public F16 As Integer = If(True

    Public F17 As Integer = If(True)

    Public Sub S()
    End Sub

End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    BC33104: 'If' operator requires either two or three operands.
    Public F1 As Integer = If(True, 2, 3, 4, 5)
                                        ~~~~~~
BC30199: '(' expected.
    Public F2 As Integer = If 1
                              ~
BC30198: ')' expected.
    Public F3 As Integer = If(1,
                                ~
BC30201: Expression expected.
    Public F3 As Integer = If(1,
                                ~
BC30198: ')' expected.
    Public F4 As Integer = If(True, 2
                                     ~
BC33104: 'If' operator requires either two or three operands.
    Public F5 As Integer = If(True, 2, 3, 4
                                        ~~~
BC30198: ')' expected.
    Public F5 As Integer = If(True, 2, 3, 4
                                           ~
BC32017: Comma, ')', or a valid expression continuation expected.
    Public F6 As Integer = If(True, 2, 3  4)
                                          ~
BC32017: Comma, ')', or a valid expression continuation expected.
    Public F7 As Integer = If(True, 2 3 4)
                                      ~~~
BC33105: 'If' operands cannot be named arguments.
    Public F8 As Integer = If(TestExpression:=True, TruePart:=1, FalsePart:=2)
                              ~~~~~~~~~~~~~~~~
BC33105: 'If' operands cannot be named arguments.
    Public F8 As Integer = If(TestExpression:=True, TruePart:=1, FalsePart:=2)
                                                    ~~~~~~~~~~
BC33105: 'If' operands cannot be named arguments.
    Public F8 As Integer = If(TestExpression:=True, TruePart:=1, FalsePart:=2)
                                                                 ~~~~~~~~~~~
BC33105: 'If' operands cannot be named arguments.
    Public F9 As Integer = If(Nothing, FalsePart:=1)
                                       ~~~~~~~~~~~
BC30201: Expression expected.
    Public F10 As Integer = If(, 1)
                               ~
BC30201: Expression expected.
    Public F10_ As Integer = If(1, )
                                   ~
BC30201: Expression expected.
    Public F11 As Integer = If(True, , 1)
                                     ~
BC30201: Expression expected.
    Public F11_ As Integer = If(True, 1,)
                                        ~
BC30451: 'abc' is not declared. It may be inaccessible due to its protection level.
    Public F12 As Integer = If(True, abc, 23)
                                     ~~~
BC30491: Expression does not produce a value.
    Public F13 As Integer = If(True, S, 23)
                                     ~
BC30198: ')' expected.
    Public F14 As Integer = If(
                               ~
BC30201: Expression expected.
    Public F14 As Integer = If(
                               ~
BC33104: 'If' operator requires either two or three operands.
    Public F14 As Integer = If(
                               ~
BC33104: 'If' operator requires either two or three operands.
    Public F15 As Integer = If()
                               ~
BC30198: ')' expected.
    Public F16 As Integer = If(True
                                   ~
BC33104: 'If' operator requires either two or three operands.
    Public F16 As Integer = If(True
                                   ~
BC33104: 'If' operator requires either two or three operands.
    Public F17 As Integer = If(True)
                                   ~
</expected>)
        End Sub

        Private Sub TestInvalidTernaryIfOperatorsStrict(strict As OptionStrict, errs As XElement)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TestInvalidIfOperatorsStrict">
    <file name="a.vb">
Imports System

Class CX

    Public F1 As Integer = If(True, "", 23)
    Public F2 As Integer = If("error", 1, 2)

        Public Sub S1()
        End Sub
    Public F3 As Integer = If(True, S1(), (Nothing))

        Public Function FUNK1(x As Integer) As Integer
            Return 0
        End Function
        Public Function FUNK2(x As Integer, Optional y As Integer = 0) As Integer
            Return 0
        End Function
    Public F4 As Func(Of Integer, Integer) = If(True, Function(x As Integer) x, AddressOf FUNK1)
    Public F5 As Func(Of Integer, Integer) = If(True, Function(x As Integer) x, AddressOf FUNK2)
    Public F6 As Func(Of Integer, Integer) = If(True, AddressOf FUNK1, AddressOf FUNK2)

        Public G As CG(Of Boolean, DateTime, Integer, Short) = Nothing
    Public F7 As Object = If(G.P1, G.F2, G.P3)
    Public F8 As Object = If(G.F2, G.P3, G.F4)

End Class

Class CG(Of T1, T2, T3, T4)

    Public Sub TEST()
        Dim x1 As T1 = Nothing
        Dim x2 As T2 = Nothing
        Dim x3 As T3 = Nothing

        Dim v1 As T1 = If(x1, x2, x3)
        Dim v2 As T2 = If(True, x2, x3)
        Dim v3 As T3 = If(True, Nothing, x3)
    End Sub

    Public Property P1 As T1
    Public Function F2() As T2
        Return Nothing
    End Function
    Public Property P3 As T3
    Public Function F4() As T4
        Return Nothing
    End Function

End Class
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(strict))

            CompilationUtils.AssertTheseDiagnostics(compilation, errs)
        End Sub

        <Fact>
        Public Sub TestInvalidTernaryIfOperatorsStrictOn()
            TestInvalidTernaryIfOperatorsStrict(OptionStrict.On,
<expected><![CDATA[
BC36913: Cannot infer a common type because more than one type is possible.
    Public F1 As Integer = If(True, "", 23)
                           ~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Boolean'.
    Public F2 As Integer = If("error", 1, 2)
                              ~~~~~~~
BC30491: Expression does not produce a value.
    Public F3 As Integer = If(True, S1(), (Nothing))
                                    ~~~~
BC36911: Cannot infer a common type.
    Public F6 As Func(Of Integer, Integer) = If(True, AddressOf FUNK1, AddressOf FUNK2)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
    Public F7 As Object = If(G.P1, G.F2, G.P3)
                          ~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type 'Date' cannot be converted to 'Boolean'.
    Public F8 As Object = If(G.F2, G.P3, G.F4)
                             ~~~~
BC30311: Value of type 'T1' cannot be converted to 'Boolean'.
        Dim v1 As T1 = If(x1, x2, x3)
                          ~~
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
        Dim v2 As T2 = If(True, x2, x3)
                       ~~~~~~~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact>
        Public Sub TestInvalidTernaryIfOperatorsStrictCustom()
            TestInvalidTernaryIfOperatorsStrict(OptionStrict.Custom,
<expected><![CDATA[
BC42016: Implicit conversion from 'Object' to 'Integer'.
    Public F1 As Integer = If(True, "", 23)
                           ~~~~~~~~~~~~~~~~
BC42021: Cannot infer a common type because more than one type is possible; 'Object' assumed.
    Public F1 As Integer = If(True, "", 23)
                           ~~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'String' to 'Boolean'.
    Public F2 As Integer = If("error", 1, 2)
                              ~~~~~~~
BC30491: Expression does not produce a value.
    Public F3 As Integer = If(True, S1(), (Nothing))
                                    ~~~~
BC36911: Cannot infer a common type.
    Public F6 As Func(Of Integer, Integer) = If(True, AddressOf FUNK1, AddressOf FUNK2)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42021: Cannot infer a common type; 'Object' assumed.
    Public F7 As Object = If(G.P1, G.F2, G.P3)
                          ~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type 'Date' cannot be converted to 'Boolean'.
    Public F8 As Object = If(G.F2, G.P3, G.F4)
                             ~~~~
BC30311: Value of type 'T1' cannot be converted to 'Boolean'.
        Dim v1 As T1 = If(x1, x2, x3)
                          ~~
BC42016: Implicit conversion from 'Object' to 'T2'.
        Dim v2 As T2 = If(True, x2, x3)
                       ~~~~~~~~~~~~~~~~
BC42021: Cannot infer a common type; 'Object' assumed.
        Dim v2 As T2 = If(True, x2, x3)
                       ~~~~~~~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact>
        Public Sub TestInvalidTernaryIfOperatorsStrictOff()
            TestInvalidTernaryIfOperatorsStrict(OptionStrict.Off,
<expected><![CDATA[
BC30491: Expression does not produce a value.
    Public F3 As Integer = If(True, S1(), (Nothing))
                                    ~~~~
BC36911: Cannot infer a common type.
    Public F6 As Func(Of Integer, Integer) = If(True, AddressOf FUNK1, AddressOf FUNK2)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type 'Date' cannot be converted to 'Boolean'.
    Public F8 As Object = If(G.F2, G.P3, G.F4)
                             ~~~~
BC30311: Value of type 'T1' cannot be converted to 'Boolean'.
        Dim v1 As T1 = If(x1, x2, x3)
                          ~~
]]>
</expected>)
        End Sub

        Private Sub TestInvalidBinaryIfOperatorsStrict(strict As OptionStrict, errs As XElement)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TestInvalidIfOperatorsStrict">
    <file name="a.vb">
Imports System

Class CX

    Public Sub S1()
    End Sub

    Public Function FUNK1(x As Integer) As Integer
        Return 0
    End Function
    Public Function FUNK2(x As Integer, Optional y As Integer = 0) As Integer
        Return 0
    End Function

    Public G As CG(Of Boolean, DateTime, Integer?, Short) = Nothing

    Sub TEST()
        Dim F1 As Object = If("", 23)
        Dim F2 As Object = If(23, "")

        Dim F3 As Object = If(S1(), (Nothing))

        Dim F4 As Func(Of Integer, Integer) = If(Function(x As Integer) x, AddressOf FUNK1)
        Dim F5 As Func(Of Integer, Integer) = If(Function(x As Integer) x, AddressOf FUNK2)
        Dim F6 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
        Dim F7 As Func(Of Integer, Integer) = If(AddressOf FUNK1, AddressOf FUNK2)
        Dim F8 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)

        Dim F9 As Object = If(G.P1, G.F2)
        Dim F9_ As Object = If(G.P1, Nothing)
        Dim F9__ As Object = If(Nothing, G.P1)
        Dim F10 As Object = If(G.F2, G.P3)
        Dim F11 As Object = If(G.P3, G.F4)
        Dim F12 As Object = If(G.F4, G.P3)

        Dim F13 As Func(Of Integer, Integer) = If(AddressOf FUNK1, Function(x As Integer) x)
        Dim F14 As Func(Of Integer, Integer) = If(AddressOf FUNK2, Function(x As Integer) x)
    End Sub

End Class

Class CG(Of T1, T2, T3, T4)

    Public Sub TEST()
        Dim x1 As T1 = Nothing
        Dim x2 As T2 = Nothing
        Dim v1 As T1 = If(x1, x2)
   End Sub

    Public Property P1 As T1
    Public Function F2() As T2
        Return Nothing
    End Function
    Public Property P3 As T3
    Public Function F4() As T4
        Return Nothing
    End Function

End Class
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(strict))

            CompilationUtils.AssertTheseDiagnostics(compilation, errs)
        End Sub

        <Fact>
        Public Sub TestInvalidBinaryIfOperatorsStrictOn()
            TestInvalidBinaryIfOperatorsStrict(OptionStrict.On,
<expected><![CDATA[
BC36913: Cannot infer a common type because more than one type is possible.
        Dim F1 As Object = If("", 23)
                           ~~~~~~~~~~
BC36913: Cannot infer a common type because more than one type is possible.
        Dim F2 As Object = If(23, "")
                           ~~~~~~~~~~
BC30491: Expression does not produce a value.
        Dim F3 As Object = If(S1(), (Nothing))
                              ~~~~
BC36911: Cannot infer a common type.
        Dim F6 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36911: Cannot infer a common type.
        Dim F7 As Func(Of Integer, Integer) = If(AddressOf FUNK1, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36911: Cannot infer a common type.
        Dim F8 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
        Dim F9 As Object = If(G.P1, G.F2)
                           ~~~~~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F9_ As Object = If(G.P1, Nothing)
                               ~~~~
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
        Dim F10 As Object = If(G.F2, G.P3)
                            ~~~~~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F12 As Object = If(G.F4, G.P3)
                               ~~~~
BC36912: Cannot infer a common type, and Option Strict On does not allow 'Object' to be assumed.
        Dim v1 As T1 = If(x1, x2)
                       ~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact>
        Public Sub TestInvalidBinaryIfOperatorsStrictCustom()
            TestInvalidBinaryIfOperatorsStrict(OptionStrict.Custom,
<expected><![CDATA[
BC42021: Cannot infer a common type because more than one type is possible; 'Object' assumed.
        Dim F1 As Object = If("", 23)
                           ~~~~~~~~~~
BC42021: Cannot infer a common type because more than one type is possible; 'Object' assumed.
        Dim F2 As Object = If(23, "")
                           ~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F2 As Object = If(23, "")
                              ~~
BC30491: Expression does not produce a value.
        Dim F3 As Object = If(S1(), (Nothing))
                              ~~~~
BC36911: Cannot infer a common type.
        Dim F6 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36911: Cannot infer a common type.
        Dim F7 As Func(Of Integer, Integer) = If(AddressOf FUNK1, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36911: Cannot infer a common type.
        Dim F8 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42021: Cannot infer a common type; 'Object' assumed.
        Dim F9 As Object = If(G.P1, G.F2)
                           ~~~~~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F9 As Object = If(G.P1, G.F2)
                              ~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F9_ As Object = If(G.P1, Nothing)
                               ~~~~
BC42021: Cannot infer a common type; 'Object' assumed.
        Dim F10 As Object = If(G.F2, G.P3)
                            ~~~~~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F10 As Object = If(G.F2, G.P3)
                               ~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F12 As Object = If(G.F4, G.P3)
                               ~~~~
BC42016: Implicit conversion from 'Object' to 'T1'.
        Dim v1 As T1 = If(x1, x2)
                       ~~~~~~~~~~
BC42021: Cannot infer a common type; 'Object' assumed.
        Dim v1 As T1 = If(x1, x2)
                       ~~~~~~~~~~
]]>
</expected>)
        End Sub

        '<Fact(skip:="Temp variables in property/field initializers are not supported")>
        <Fact>
        Public Sub TestInvalidBinaryIfOperatorsStrictOff()
            TestInvalidBinaryIfOperatorsStrict(OptionStrict.Off,
<expected><![CDATA[
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F2 As Object = If(23, "")
                              ~~
BC30491: Expression does not produce a value.
        Dim F3 As Object = If(S1(), (Nothing))
                              ~~~~
BC36911: Cannot infer a common type.
        Dim F6 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36911: Cannot infer a common type.
        Dim F7 As Func(Of Integer, Integer) = If(AddressOf FUNK1, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36911: Cannot infer a common type.
        Dim F8 As Func(Of Integer, Integer) = If(Nothing, AddressOf FUNK2)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F9 As Object = If(G.P1, G.F2)
                              ~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F9_ As Object = If(G.P1, Nothing)
                               ~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F10 As Object = If(G.F2, G.P3)
                               ~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
        Dim F12 As Object = If(G.F4, G.P3)
                               ~~~~
        ]]>
</expected>)
        End Sub

        <Fact, WorkItem(544983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544983")>
        Public Sub Bug13187()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Strict On

Imports System


Module Module1

    Structure S
    End Structure

    Function Full() As S
        System.Console.WriteLine("Full")
        Return New S()
    End Function

    Function M(o As S?) As Integer

        Dim i = If(o, Full())

        Return 1
    End Function

    Sub Main()
        M(New S())
        System.Console.WriteLine("----")
        Dim x = If(New S?(Nothing), New S())
        System.Console.WriteLine("----")
        M(Nothing)
    End Sub

End Module
]]></file>
</compilation>, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
----
----
Full
]]>)

        End Sub

        <Fact, WorkItem(544983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544983")>
        Public Sub Bug13187_2()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System

Module Module1
    Sub Main(args As String())
        Test1()
        Test2()
        Test3()
        Test4()
    End Sub

    Function GetX() As Integer?
        Return 1
    End Function

    Function GetY() As Integer?
        System.Console.WriteLine("GetY")
        Return Nothing
    End Function

    Function GetZ() As Long?
        System.Console.WriteLine("GetZ")
        Return Nothing
    End Function

    Sub Test1()
        System.Console.WriteLine(If(GetX, GetY))
    End Sub

    Sub Test2()
        System.Console.WriteLine(If(GetX, GetZ))
    End Sub

    Sub Test3()
        Dim x as Integer = 4
        Dim z= If(New Integer?(x), GetY)
        System.Console.WriteLine(z)
    End Sub

    Sub Test4()
        Dim x as Integer = 5
        Dim z = If(New Integer?(x), GetZ)
        System.Console.WriteLine(z)
    End Sub
End Module
]]></file>
</compilation>, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
1
1
4
5
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (Integer? V_0,
  Integer? V_1)
  IL_0000:  call       "Function Module1.GetX() As Integer?"
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloca.s   V_1
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_0018
  IL_0011:  call       "Function Module1.GetY() As Integer?"
  IL_0016:  br.s       IL_0019
  IL_0018:  ldloc.0
  IL_0019:  box        "Integer?"
  IL_001e:  call       "Sub System.Console.WriteLine(Object)"
  IL_0023:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  .locals init (Integer? V_0,
  Integer? V_1)
  IL_0000:  call       "Function Module1.GetX() As Integer?"
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloca.s   V_1
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_0018
  IL_0011:  call       "Function Module1.GetZ() As Long?"
  IL_0016:  br.s       IL_0025
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_001f:  conv.i8
  IL_0020:  newobj     "Sub Long?..ctor(Long)"
  IL_0025:  box        "Long?"
  IL_002a:  call       "Sub System.Console.WriteLine(Object)"
  IL_002f:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.4
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0008:  box        "Integer?"
  IL_000d:  call       "Sub System.Console.WriteLine(Object)"
  IL_0012:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  conv.i8
  IL_0004:  newobj     "Sub Long?..ctor(Long)"
  IL_0009:  box        "Long?"
  IL_000e:  call       "Sub System.Console.WriteLine(Object)"
  IL_0013:  ret
}
]]>)

        End Sub

    End Class
End Namespace

