' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class UnaryOperators
        Inherits BasicTestBase

        <Fact>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        Public Sub Test1()

            Dim source =
<compilation name="UnaryOperator1">
    <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        Dim Bo As Boolean
        Dim SB As SByte
        Dim By As Byte
        Dim Sh As Short
        Dim US As UShort
        Dim [In] As Integer
        Dim UI As UInteger
        Dim Lo As Long
        Dim UL As ULong
        Dim De As Decimal
        Dim Si As Single
        Dim [Do] As Double
        Dim St As String
        Dim Ob As Object
        Dim Tc As System.TypeCode

        Bo = False
        SB = -1
        By = 2
        Sh = -3
        US = 4
        [In] = -5
        UI = 6
        Lo = -7
        UL = 8
        De = -9D
        Si = 10
        [Do] = -11
        St = "12"
        Ob = -13

        System.Console.WriteLine("Unary Minus")
        PrintResult(-Nothing)
        PrintResult(-Bo)
        PrintResult(-SB)
        PrintResult(-By)
        PrintResult(-Sh)
        PrintResult(-US)
        PrintResult(-[In])
        PrintResult(-UI)
        PrintResult(-Lo)
        PrintResult(-UL)
        PrintResult(-De)
        PrintResult(-Si)
        PrintResult(-[Do])
        PrintResult(-St)
        PrintResult(-Ob)
        PrintResult(-Tc)

        PrintResult(-True)
        PrintResult(-False)
        PrintResult(-System.SByte.MaxValue)
        PrintResult(-System.Byte.MaxValue)
        PrintResult(-14S)
        PrintResult(-15US)
        PrintResult(-16I)
        PrintResult(-17UI)
        PrintResult(-19L)
        PrintResult(-20UL)
        PrintResult(-System.Single.MaxValue)
        PrintResult(-22.0)
        PrintResult(-23D)
        PrintResult(-"24")

        System.Console.WriteLine("")
        System.Console.WriteLine("Unary Plus")
        PrintResult(+Nothing)
        PrintResult(+Bo)
        PrintResult(+SB)
        PrintResult(+By)
        PrintResult(+Sh)
        PrintResult(+US)
        PrintResult(+[In])
        PrintResult(+UI)
        PrintResult(+Lo)
        PrintResult(+UL)
        PrintResult(+De)
        PrintResult(+Si)
        PrintResult(+[Do])
        PrintResult(+St)
        PrintResult(+Ob)
        PrintResult(+Tc)

        PrintResult(+True)
        PrintResult(+False)
        PrintResult(+System.SByte.MaxValue)
        PrintResult(+System.Byte.MaxValue)
        PrintResult(+14S)
        PrintResult(+15US)
        PrintResult(+16I)
        PrintResult(+17UI)
        PrintResult(+19L)
        PrintResult(+20UL)
        PrintResult(+System.Single.MaxValue)
        PrintResult(+22.0)
        PrintResult(+23D)
        PrintResult(+"24")

        System.Console.WriteLine("")
        System.Console.WriteLine("Logical Not")
        PrintResult(Not Nothing)
        PrintResult(Not Bo)
        PrintResult(Not SB)
        PrintResult(Not By)
        PrintResult(Not Sh)
        PrintResult(Not US)
        PrintResult(Not [In])
        PrintResult(Not UI)
        PrintResult(Not Lo)
        PrintResult(Not UL)
        PrintResult(Not De)
        PrintResult(Not Si)
        PrintResult(Not [Do])
        PrintResult(Not St)
        PrintResult(Not Ob)
        PrintResult(Not Tc)

        PrintResult(Not True)
        PrintResult(Not False)
        PrintResult(Not System.SByte.MaxValue)
        PrintResult(Not System.Byte.MaxValue)
        PrintResult(Not 14S)
        PrintResult(Not 15US)
        PrintResult(Not 16I)
        PrintResult(Not 17UI)
        PrintResult(Not 19L)
        PrintResult(Not 20UL)
        PrintResult(Not 21.0F)
        PrintResult(Not 22.0)
        PrintResult(Not 23D)
        PrintResult(Not "24")

    End Sub

    Sub PrintResult(val As Boolean)
        System.Console.WriteLine("Boolean: {0}", val)
    End Sub
    Sub PrintResult(val As SByte)
        System.Console.WriteLine("SByte: {0}", val)
    End Sub
    Sub PrintResult(val As Byte)
        System.Console.WriteLine("Byte: {0}", val)
    End Sub
    Sub PrintResult(val As Short)
        System.Console.WriteLine("Short: {0}", val)
    End Sub
    Sub PrintResult(val As UShort)
        System.Console.WriteLine("UShort: {0}", val)
    End Sub
    Sub PrintResult(val As Integer)
        System.Console.WriteLine("Integer: {0}", val)
    End Sub
    Sub PrintResult(val As UInteger)
        System.Console.WriteLine("UInteger: {0}", val)
    End Sub
    Sub PrintResult(val As Long)
        System.Console.WriteLine("Long: {0}", val)
    End Sub
    Sub PrintResult(val As ULong)
        System.Console.WriteLine("ULong: {0}", val)
    End Sub
    Sub PrintResult(val As Decimal)
        System.Console.WriteLine("Decimal: {0}", val)
    End Sub
    Sub PrintResult(val As Single)
        System.Console.WriteLine("Single: {0}", val.ToString(System.Globalization.CultureInfo.InvariantCulture))
    End Sub
    Sub PrintResult(val As Double)
        System.Console.WriteLine("Double: {0}", val)
    End Sub
    'Sub PrintResult(val As Date)
    '    System.Console.WriteLine("Date: {0}", val)
    'End Sub
    Sub PrintResult(val As Char)
        System.Console.WriteLine("Char: {0}", val)
    End Sub
    Sub PrintResult(val As String)
        System.Console.WriteLine("String: ")
        System.Console.WriteLine(val)
    End Sub
    Sub PrintResult(val As Object)
        System.Console.WriteLine("Object: {0}", val)
    End Sub
    Sub PrintResult(val As System.TypeCode)
        System.Console.WriteLine("TypeCode: {0}", val)
    End Sub
End Module
    </file>
</compilation>

            Dim expected = <![CDATA[
Unary Minus
Integer: 0
Short: 0
SByte: 1
Short: -2
Short: 3
Integer: -4
Integer: 5
Long: -6
Long: 7
Decimal: -8
Decimal: 9
Single: -10
Double: 11
Double: -12
Object: 13
Integer: 0
Short: 1
Short: 0
SByte: -127
Short: -255
Short: -14
Integer: -15
Integer: -16
Long: -17
Long: -19
Decimal: -20
Single: -3.402823E+38
Double: -22
Decimal: -23
Double: -24

Unary Plus
Integer: 0
Short: 0
SByte: -1
Byte: 2
Short: -3
UShort: 4
Integer: -5
UInteger: 6
Long: -7
ULong: 8
Decimal: -9
Single: 10
Double: -11
Double: 12
Object: -13
Integer: 0
Short: -1
Short: 0
SByte: 127
Byte: 255
Short: 14
UShort: 15
Integer: 16
UInteger: 17
Long: 19
ULong: 20
Single: 3.402823E+38
Double: 22
Decimal: 23
Double: 24

Logical Not
Integer: -1
Boolean: True
SByte: 0
Byte: 253
Short: 2
UShort: 65531
Integer: 4
UInteger: 4294967289
Long: 6
ULong: 18446744073709551607
Long: 8
Long: -11
Long: 10
Long: -13
Object: 12
TypeCode: -1
Boolean: False
Boolean: True
SByte: -128
Byte: 0
Short: -15
UShort: 65520
Integer: -17
UInteger: 4294967278
Long: -20
ULong: 18446744073709551595
Long: -22
Long: -23
Long: -24
Long: -25
]]>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            Assert.True(c1.Options.CheckOverflow)

            CompileAndVerify(c1, expected)

            c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOverflowChecks(False))
            Assert.False(c1.Options.CheckOverflow)

            CompileAndVerify(c1, expected)
        End Sub


        <Fact>
        Public Sub Test2()


            Dim compilationDef =
<compilation name="Test2">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim Da As Date
        Dim Ch As Char
        Dim Ob As Object
        Dim result As Object

        result = -Da
        result = -Ch
        result = -Ob
        result = +Da
        result = +Ch
        result = +Ob
        result = Not Da
        result = Not Ch
        result = Not Ob

        result = --Da
    End Sub

End Module
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30487: Operator '-' is not defined for type 'Date'.
        result = -Da
                 ~~~
BC30487: Operator '-' is not defined for type 'Char'.
        result = -Ch
                 ~~~
BC30038: Option Strict On prohibits operands of type Object for operator '-'.
        result = -Ob
                  ~~
BC42104: Variable 'Ob' is used before it has been assigned a value. A null reference exception could result at runtime.
        result = -Ob
                  ~~
BC30487: Operator '+' is not defined for type 'Date'.
        result = +Da
                 ~~~
BC30487: Operator '+' is not defined for type 'Char'.
        result = +Ch
                 ~~~
BC30038: Option Strict On prohibits operands of type Object for operator '+'.
        result = +Ob
                  ~~
BC30487: Operator 'Not' is not defined for type 'Date'.
        result = Not Da
                 ~~~~~~
BC30487: Operator 'Not' is not defined for type 'Char'.
        result = Not Ch
                 ~~~~~~
BC30038: Option Strict On prohibits operands of type Object for operator 'Not'.
        result = Not Ob
                     ~~
BC30487: Operator '-' is not defined for type 'Date'.
        result = --Da
                  ~~~
</expected>)

        End Sub


        <Fact>
        Public Sub Test3()

            Dim c1 = VisualBasicCompilation.Create("Test3",
                syntaxTrees:={VisualBasicSyntaxTree.ParseText(
<text>
Option Strict Off

Module Module1

    Interface I1
    End Interface

    Function Main() As I1
        Dim result As Object

        result = -Nothing
    End Function

End Module</text>.Value
                        )},
                references:=Nothing, options:=TestOptions.ReleaseDll)


            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC31091: Import of type 'Object' from assembly or module 'Test3.dll' failed.
Module Module1
       ~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor' is not defined.
Module Module1
       ~~~~~~~
BC30002: Type 'System.Object' is not defined.
        Dim result As Object
                      ~~~~~~
BC30002: Type 'System.Int32' is not defined.
        result = -Nothing
                  ~~~~~~~
BC42105: Function 'Main' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function
    ~~~~~~~~~~~~
</expected>)

        End Sub


        <Fact>
        Public Sub Test4()

            Dim source =
<compilation name="UnaryOperator4">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()
        Dim Ob As Object

        Ob = -System.Byte.MinValue
        Ob = -System.Byte.MaxValue
        Ob = -System.SByte.MinValue
        Ob = -System.SByte.MaxValue
        Ob = -Int16.MinValue
        Ob = -Int16.MaxValue
        Ob = -UInt16.MaxValue
        Ob = -UInt16.MinValue
        Ob = -Int32.MinValue
        Ob = -Int32.MaxValue
        Ob = -UInt32.MaxValue
        Ob = -UInt32.MinValue
        Ob = -Int64.MinValue
        Ob = -Int64.MaxValue
        Ob = -UInt64.MaxValue
        Ob = -UInt64.MinValue
        Ob = -System.Decimal.MaxValue
        Ob = -System.Decimal.MinValue
        Ob = -System.Single.MaxValue
        Ob = -System.Single.MinValue
        Ob = -System.Double.MaxValue
        Ob = -System.Double.MinValue

    End Sub
End Module
    </file>
</compilation>

            Dim expected =
<expected>
BC30439: Constant expression not representable in type 'SByte'.
        Ob = -System.SByte.MinValue
             ~~~~~~~~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Short'.
        Ob = -Int16.MinValue
             ~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Integer'.
        Ob = -Int32.MinValue
             ~~~~~~~~~~~~~~~
BC30439: Constant expression not representable in type 'Long'.
        Ob = -Int64.MinValue
             ~~~~~~~~~~~~~~~
</expected>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Assert.True(c1.Options.CheckOverflow)

            CompilationUtils.AssertTheseDiagnostics(c1, expected)

            Dim c2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOverflowChecks(False))

            Assert.False(c2.Options.CheckOverflow)

            CompilationUtils.AssertTheseDiagnostics(c2, expected)

        End Sub



        <Fact>
        Public Sub Test5()

            Dim compilationDef =
<compilation name="UnaryOperator2">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()

        Dim Ob As Object = Nothing

        Ob = -Ob
        Ob = +Ob
        Ob = Not Ob

    End Sub

End Module
    </file>
</compilation>


            Dim expected =
<expected>
BC42019: Operands of type Object used for operator '-'; runtime errors could occur.
        Ob = -Ob
              ~~
BC42019: Operands of type Object used for operator '+'; runtime errors could occur.
        Ob = +Ob
              ~~
BC42019: Operands of type Object used for operator 'Not'; runtime errors could occur.
        Ob = Not Ob
                 ~~
</expected>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))
            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)
            CompilationUtils.AssertTheseDiagnostics(compilation, expected)

        End Sub

        <WorkItem(544620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544620")>
        <Fact()>
        Public Sub Bug13088()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program

    Public Const Z1 As Integer = +2147483648 'BIND:"Public Const Z1 As Integer = +2147483648"
    Public Const Z2 As Integer = (-(-2147483648)) 'BIND1:"Public Const Z2 As Integer = (-(-2147483648))"

    Sub Main()
    End Sub
End Module
    ]]></file>
    </compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_ExpressionOverflow1, "+2147483648").WithArguments("Integer"),
                                           Diagnostic(ERRID.ERR_ExpressionOverflow1, "(-(-2147483648))").WithArguments("Integer"))

            Dim symbol = compilation.GlobalNamespace.GetTypeMembers("Program").Single.GetMembers("Z1").Single
            Assert.False(DirectCast(symbol, FieldSymbol).HasConstantValue)

            symbol = compilation.GlobalNamespace.GetTypeMembers("Program").Single.GetMembers("Z2").Single
            Assert.False(DirectCast(symbol, FieldSymbol).HasConstantValue)
        End Sub

        <Fact()>
        Public Sub IntrinsicSymbols()
            Dim operators() As UnaryOperatorKind =
            {
            UnaryOperatorKind.Plus,
            UnaryOperatorKind.Minus,
            UnaryOperatorKind.Not
            }

            Dim opTokens = (From op In operators Select SyntaxFacts.GetText(OverloadResolution.GetOperatorTokenKind(op))).ToArray()

            Dim typeNames() As String =
                {
                "System.Object",
                "System.String",
                "System.Double",
                "System.SByte",
                "System.Int16",
                "System.Int32",
                "System.Int64",
                "System.Decimal",
                "System.Single",
                "System.Byte",
                "System.UInt16",
                "System.UInt32",
                "System.UInt64",
                "System.Boolean",
                "System.Char",
                "System.DateTime",
                "System.TypeCode",
                "System.StringComparison",
                "System.Guid"
                }

            Dim builder As New System.Text.StringBuilder
            Dim n As Integer = 0

            For Each arg1 In typeNames
                n += 1
                builder.AppendFormat(
"Sub Test{1}(x1 as {0}, x2 as {0}?)" & vbCrLf, arg1, n)

                Dim k As Integer = 0
                For Each opToken In opTokens
                    builder.AppendFormat(
"    Dim z{0}_1 = {1} x1" & vbCrLf &
"    Dim z{0}_2 = {1} x2" & vbCrLf &
"    If {1} x1" & vbCrLf &
"    End If" & vbCrLf &
"    If {1} x2" & vbCrLf &
"    End If" & vbCrLf,
                                         k, opToken)
                    k += 1
                Next

                builder.Append(
"End Sub" & vbCrLf)
            Next

            Dim source =
<compilation>
    <file name="a.vb">
Class Module1
<%= New System.Xml.Linq.XCData(builder.ToString()) %>
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithOverflowChecks(True))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim nodes = (From node In tree.GetRoot().DescendantNodes()
                         Select node = TryCast(node, UnaryExpressionSyntax)
                         Where node IsNot Nothing).ToArray()

            n = 0
            For Each name In typeNames
                Dim type = compilation.GetTypeByMetadataName(name)
                For Each op In operators
                    TestIntrinsicSymbol(
                        op,
                        type,
                        compilation,
                        semanticModel,
                        nodes(n),
                        nodes(n + 1),
                        nodes(n + 2),
                        nodes(n + 3))
                    n += 4
                Next
            Next

            Assert.Equal(n, nodes.Length)

        End Sub

        Private Sub TestIntrinsicSymbol(
            op As UnaryOperatorKind,
            type As TypeSymbol,
            compilation As VisualBasicCompilation,
            semanticModel As SemanticModel,
            node1 As UnaryExpressionSyntax,
            node2 As UnaryExpressionSyntax,
            node3 As UnaryExpressionSyntax,
            node4 As UnaryExpressionSyntax
        )
            Dim info1 As SymbolInfo = semanticModel.GetSymbolInfo(node1)
            Assert.Equal(CandidateReason.None, info1.CandidateReason)
            Assert.Equal(0, info1.CandidateSymbols.Length)

            Dim symbol1 = DirectCast(info1.Symbol, MethodSymbol)
            Dim symbol2 = semanticModel.GetSymbolInfo(node2).Symbol
            Dim symbol3 = DirectCast(semanticModel.GetSymbolInfo(node3).Symbol, MethodSymbol)
            Dim symbol4 = semanticModel.GetSymbolInfo(node4).Symbol

            Assert.Equal(symbol1, symbol3)

            If symbol1 IsNot Nothing Then
                Assert.NotSame(symbol1, symbol3)
                Assert.Equal(symbol1.GetHashCode(), symbol3.GetHashCode())

                Assert.Equal(symbol1.Parameters(0), symbol3.Parameters(0))
                Assert.Equal(symbol1.Parameters(0).GetHashCode(), symbol3.Parameters(0).GetHashCode())
            End If

            Assert.Equal(symbol2, symbol4)

            Dim special As SpecialType = type.GetEnumUnderlyingTypeOrSelf().SpecialType

            Dim resultType As SpecialType = OverloadResolution.ResolveNotLiftedIntrinsicUnaryOperator(op, special)

            If resultType = SpecialType.None Then
                Assert.Null(symbol1)
                Assert.Null(symbol2)
                Assert.Null(symbol3)
                Assert.Null(symbol4)
                Return
            End If

            Assert.NotNull(symbol1)

            Dim containerName As String = compilation.GetSpecialType(resultType).ToTestDisplayString()
            Dim returnName As String = containerName

            If op = UnaryOperatorKind.Not AndAlso type.IsEnumType() Then
                containerName = type.ToTestDisplayString()
                returnName = containerName
            End If

            Assert.Equal(String.Format("Function {0}.{1}(value As {0}) As {2}",
                                       containerName,
                                       OverloadResolution.TryGetOperatorName(op),
                                       returnName),
                         symbol1.ToTestDisplayString())

            Assert.Equal(MethodKind.BuiltinOperator, symbol1.MethodKind)
            Assert.True(symbol1.IsImplicitlyDeclared)

            Assert.Equal(op = UnaryOperatorKind.Minus AndAlso symbol1.ContainingType.IsIntegralType(),
                         symbol1.IsCheckedBuiltin)

            Assert.False(symbol1.IsGenericMethod)
            Assert.False(symbol1.IsExtensionMethod)
            Assert.False(symbol1.IsExternalMethod)
            Assert.False(symbol1.CanBeReferencedByName)
            Assert.Null(symbol1.DeclaringCompilation)
            Assert.Equal(symbol1.Name, symbol1.MetadataName)
            Assert.Same(symbol1.ContainingSymbol, symbol1.Parameters(0).Type)
            Assert.Equal(0, symbol1.Locations.Length)
            Assert.Null(symbol1.GetDocumentationCommentId)
            Assert.Equal("", symbol1.GetDocumentationCommentXml)

            Assert.True(symbol1.HasSpecialName)
            Assert.True(symbol1.IsShared)
            Assert.Equal(Accessibility.Public, symbol1.DeclaredAccessibility)
            Assert.False(symbol1.IsOverloads)
            Assert.False(symbol1.IsOverrides)
            Assert.False(symbol1.IsOverridable)
            Assert.False(symbol1.IsMustOverride)
            Assert.False(symbol1.IsNotOverridable)
            Assert.Equal(1, symbol1.ParameterCount)
            Assert.Equal(0, symbol1.Parameters(0).Ordinal)

            Dim otherSymbol = DirectCast(semanticModel.GetSymbolInfo(node1).Symbol, MethodSymbol)
            Assert.Equal(symbol1, otherSymbol)

            If type.IsValueType Then
                Assert.Equal(symbol1, symbol2)
                Return
            End If

            Assert.Null(symbol2)
        End Sub

        <Fact()>
        Public Sub CheckedIntrinsicSymbols()


            Dim source =
<compilation>
    <file name="a.vb">
Class Module1
    Sub Test(x as Integer)
        Dim z1 = -x
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithOverflowChecks(False))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 = (From node In tree.GetRoot().DescendantNodes()
                         Select node = TryCast(node, UnaryExpressionSyntax)
                         Where node IsNot Nothing).Single()

            Dim symbol1 = DirectCast(semanticModel.GetSymbolInfo(node1).Symbol, MethodSymbol)
            Assert.False(symbol1.IsCheckedBuiltin)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOverflowChecks(True))
            semanticModel = compilation.GetSemanticModel(tree)

            Dim symbol2 = DirectCast(semanticModel.GetSymbolInfo(node1).Symbol, MethodSymbol)
            Assert.True(symbol2.IsCheckedBuiltin)

            Assert.NotEqual(symbol1, symbol2)
        End Sub

    End Class

End Namespace
