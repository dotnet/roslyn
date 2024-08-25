' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Editing
    <[UseExportProvider]>
    Public Class SyntaxGeneratorTests
        Private _g As SyntaxGenerator

        Private ReadOnly _emptyCompilation As VisualBasicCompilation = VisualBasicCompilation.Create("empty", references:={NetFramework.mscorlib, NetFramework.System})

        Private ReadOnly _ienumerableInt As INamedTypeSymbol

        Public Sub New()
            Me._ienumerableInt = _emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_emptyCompilation.GetSpecialType(SpecialType.System_Int32))
        End Sub

        Private ReadOnly Property Generator As SyntaxGenerator
            Get
                If _g Is Nothing Then
                    _g = SyntaxGenerator.GetGenerator(New AdhocWorkspace(), LanguageNames.VisualBasic)
                End If

                Return _g
            End Get
        End Property

        Public Shared Function Compile(code As String) As Compilation
            Return VisualBasicCompilation.Create("test").AddReferences(NetFramework.mscorlib).AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code))
        End Function

        Private Shared Sub VerifySyntax(Of TSyntax As SyntaxNode)(type As SyntaxNode, expectedText As String)
            Assert.IsAssignableFrom(GetType(TSyntax), type)
            Dim normalized = type.NormalizeWhitespace().ToFullString()
            Dim fixedExpectations = expectedText.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
            AssertEx.Equal(fixedExpectations, normalized)
        End Sub

        Private Shared Sub VerifySyntaxRaw(Of TSyntax As SyntaxNode)(type As SyntaxNode, expectedText As String)
            Assert.IsAssignableFrom(GetType(TSyntax), type)
            Dim text = type.ToFullString()
            Assert.Equal(expectedText, text)
        End Sub

        Private Shared Function ParseCompilationUnit(text As String) As CompilationUnitSyntax
            Dim fixedText = text.Replace(vbLf, vbCrLf)
            Return SyntaxFactory.ParseCompilationUnit(fixedText)
        End Function

#Region "Expressions & Statements"
        <Fact>
        Public Sub TestLiteralExpressions()
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0), "0")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(1), "1")
            VerifySyntax(Of UnaryExpressionSyntax)(Generator.LiteralExpression(-1), "-1")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Integer.MinValue), "Global.System.Int32.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Integer.MaxValue), "Global.System.Int32.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0L), "0L")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(1L), "1L")
            VerifySyntax(Of UnaryExpressionSyntax)(Generator.LiteralExpression(-1L), "-1L")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Long.MinValue), "Global.System.Int64.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Long.MaxValue), "Global.System.Int64.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0UL), "0UL")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(1UL), "1UL")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(ULong.MinValue), "0UL")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(ULong.MaxValue), "Global.System.UInt64.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0.0F), "0F")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(1.0F), "1F")
            VerifySyntax(Of UnaryExpressionSyntax)(Generator.LiteralExpression(-1.0F), "-1F")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Single.MinValue), "Global.System.Single.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Single.MaxValue), "Global.System.Single.MaxValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Single.Epsilon), "Global.System.Single.Epsilon")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Single.NaN), "Global.System.Single.NaN")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Single.NegativeInfinity), "Global.System.Single.NegativeInfinity")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Single.PositiveInfinity), "Global.System.Single.PositiveInfinity")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0.0), "0R")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(1.0), "1R")
            VerifySyntax(Of UnaryExpressionSyntax)(Generator.LiteralExpression(-1.0), "-1R")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Double.MinValue), "Global.System.Double.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Double.MaxValue), "Global.System.Double.MaxValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Double.Epsilon), "Global.System.Double.Epsilon")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Double.NaN), "Global.System.Double.NaN")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Double.NegativeInfinity), "Global.System.Double.NegativeInfinity")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Double.PositiveInfinity), "Global.System.Double.PositiveInfinity")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0D), "0D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(0.00D), "0.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("1.00", CultureInfo.InvariantCulture)), "1.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("-1.00", CultureInfo.InvariantCulture)), "-1.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("1.0000000000", CultureInfo.InvariantCulture)), "1.0000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("0.000000", CultureInfo.InvariantCulture)), "0.000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("0.0000000", CultureInfo.InvariantCulture)), "0.0000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(1000000000D), "1000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(123456789.123456789D), "123456789.123456789D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("1E-28", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000001D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("0E-28", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("1E-29", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(Decimal.Parse("-1E-29", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Decimal.MinValue), "Global.System.Decimal.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.LiteralExpression(Decimal.MaxValue), "Global.System.Decimal.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression("c"c), """c""c")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression("str"), """str""")

            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(True), "True")
            VerifySyntax(Of LiteralExpressionSyntax)(Generator.LiteralExpression(False), "False")
        End Sub

        <Fact>
        Public Sub TestAttributeData()
            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
End Class
", "<MyAttribute>")), "<Global.MyAttribute>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Object)
  End Sub
End Class
", "<MyAttribute(Nothing)>")), "<Global.MyAttribute(Nothing)>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Integer)
  End Sub
End Class
", "<MyAttribute(123)>")), "<Global.MyAttribute(123)>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Double)
  End Sub
End Class
", "<MyAttribute(12.3)>")), "<Global.MyAttribute(12.3)>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As String)
  End Sub
End Class
", "<MyAttribute(""value"")>")), "<Global.MyAttribute(""value"")>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Enum E
    A
End Enum

Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As E)
  End Sub
End Class
", "<MyAttribute(E.A)>")), "<Global.MyAttribute(Global.E.A)>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Type)
  End Sub
End Class
", "<MyAttribute(GetType(MyAttribute))>")), "<Global.MyAttribute(GetType(Global.MyAttribute))>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(values as Integer())
  End Sub
End Class
", "<MyAttribute({1, 2, 3})>")), "<Global.MyAttribute({1, 2, 3})>")

            VerifySyntax(Of AttributeListSyntax)(Generator.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Property Value As Integer
End Class
", "<MyAttribute(Value := 123)>")), "<Global.MyAttribute(Value:=123)>")

        End Sub

        Private Shared Function GetAttributeData(decl As String, use As String) As AttributeData
            Dim code = decl & vbCrLf & use & vbCrLf & "Public Class C " & vbCrLf & "End Class" & vbCrLf
            Dim compilation = Compile(code)
            Dim typeC = DirectCast(compilation.GlobalNamespace.GetMembers("C").First, INamedTypeSymbol)
            Return typeC.GetAttributes().First()
        End Function

        <Fact>
        Public Sub TestNameExpressions()
            VerifySyntax(Of IdentifierNameSyntax)(Generator.IdentifierName("x"), "x")
            VerifySyntax(Of QualifiedNameSyntax)(Generator.QualifiedName(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "x.y")
            VerifySyntax(Of QualifiedNameSyntax)(Generator.DottedName("x.y"), "x.y")

            VerifySyntax(Of GenericNameSyntax)(Generator.GenericName("x", Generator.IdentifierName("y")), "x(Of y)")
            VerifySyntax(Of GenericNameSyntax)(Generator.GenericName("x", Generator.IdentifierName("y"), Generator.IdentifierName("z")), "x(Of y, z)")

            ' convert identifier name into generic name
            VerifySyntax(Of GenericNameSyntax)(Generator.WithTypeArguments(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "x(Of y)")

            ' convert qualified name into qualified generic name
            VerifySyntax(Of QualifiedNameSyntax)(Generator.WithTypeArguments(Generator.DottedName("x.y"), Generator.IdentifierName("z")), "x.y(Of z)")

            ' convert member access expression into generic member access expression
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.WithTypeArguments(Generator.MemberAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")), "x.y(Of z)")

            ' convert existing generic name into a different generic name
            Dim gname = Generator.WithTypeArguments(Generator.IdentifierName("x"), Generator.IdentifierName("y"))
            VerifySyntax(Of GenericNameSyntax)(gname, "x(Of y)")
            VerifySyntax(Of GenericNameSyntax)(Generator.WithTypeArguments(gname, Generator.IdentifierName("z")), "x(Of z)")
        End Sub

        <Fact>
        Public Sub TestTypeExpressions()
            ' these are all type syntax too
            VerifySyntax(Of TypeSyntax)(Generator.IdentifierName("x"), "x")
            VerifySyntax(Of TypeSyntax)(Generator.QualifiedName(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "x.y")
            VerifySyntax(Of TypeSyntax)(Generator.DottedName("x.y"), "x.y")
            VerifySyntax(Of TypeSyntax)(Generator.GenericName("x", Generator.IdentifierName("y")), "x(Of y)")
            VerifySyntax(Of TypeSyntax)(Generator.GenericName("x", Generator.IdentifierName("y"), Generator.IdentifierName("z")), "x(Of y, z)")

            VerifySyntax(Of TypeSyntax)(Generator.ArrayTypeExpression(Generator.IdentifierName("x")), "x()")
            VerifySyntax(Of TypeSyntax)(Generator.ArrayTypeExpression(Generator.ArrayTypeExpression(Generator.IdentifierName("x"))), "x()()")
            VerifySyntax(Of TypeSyntax)(Generator.NullableTypeExpression(Generator.IdentifierName("x")), "x?")
            VerifySyntax(Of TypeSyntax)(Generator.NullableTypeExpression(Generator.NullableTypeExpression(Generator.IdentifierName("x"))), "x?")

            Dim intType = _emptyCompilation.GetSpecialType(SpecialType.System_Int32)
            VerifySyntax(Of TupleElementSyntax)(Generator.TupleElementExpression(Generator.IdentifierName("x")), "x")
            VerifySyntax(Of TupleElementSyntax)(Generator.TupleElementExpression(Generator.IdentifierName("x"), "y"), "y As x")
            VerifySyntax(Of TupleElementSyntax)(Generator.TupleElementExpression(intType), "System.Int32")
            VerifySyntax(Of TupleElementSyntax)(Generator.TupleElementExpression(intType, "y"), "y As System.Int32")
            VerifySyntax(Of TypeSyntax)(Generator.TupleTypeExpression(Generator.TupleElementExpression(Generator.IdentifierName("x")), Generator.TupleElementExpression(Generator.IdentifierName("y"))), "(x, y)")
            VerifySyntax(Of TypeSyntax)(Generator.TupleTypeExpression(New ITypeSymbol() {intType, intType}), "(System.Int32, System.Int32)")
            VerifySyntax(Of TypeSyntax)(Generator.TupleTypeExpression(New ITypeSymbol() {intType, intType}, New String() {"x", "y"}), "(x As System.Int32, y As System.Int32)")
        End Sub

        <Fact>
        Public Sub TestSpecialTypeExpression()
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Byte), "Byte")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_SByte), "SByte")

            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Int16), "Short")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_UInt16), "UShort")

            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Int32), "Integer")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_UInt32), "UInteger")

            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Int64), "Long")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_UInt64), "ULong")

            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Single), "Single")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Double), "Double")

            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Char), "Char")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_String), "String")

            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Object), "Object")
            VerifySyntax(Of TypeSyntax)(Generator.TypeExpression(SpecialType.System_Decimal), "Decimal")
        End Sub

        <Fact>
        Public Sub TestSymbolTypeExpressions()
            Dim genericType = _emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            VerifySyntax(Of QualifiedNameSyntax)(Generator.TypeExpression(genericType), "Global.System.Collections.Generic.IEnumerable(Of T)")

            Dim arrayType = _emptyCompilation.CreateArrayTypeSymbol(_emptyCompilation.GetSpecialType(SpecialType.System_Int32))
            VerifySyntax(Of ArrayTypeSyntax)(Generator.TypeExpression(arrayType), "System.Int32()")
        End Sub

        <Fact>
        Public Sub TestMathAndLogicExpressions()
            VerifySyntax(Of UnaryExpressionSyntax)(Generator.NegateExpression(Generator.IdentifierName("x")), "-(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.AddExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) + (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.SubtractExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) - (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.MultiplyExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) * (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.DivideExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) / (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.ModuloExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) Mod (y)")

            VerifySyntax(Of UnaryExpressionSyntax)(Generator.BitwiseNotExpression(Generator.IdentifierName("x")), "Not(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.BitwiseAndExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) And (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.BitwiseOrExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) Or (y)")

            VerifySyntax(Of UnaryExpressionSyntax)(Generator.LogicalNotExpression(Generator.IdentifierName("x")), "Not(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.LogicalAndExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) AndAlso (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.LogicalOrExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) OrElse (y)")
        End Sub

        <Fact>
        Public Sub TestEqualityAndInequalityExpressions()
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.ReferenceEqualsExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) Is (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.ValueEqualsExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) = (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(Generator.ReferenceNotEqualsExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) IsNot (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.ValueNotEqualsExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) <> (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(Generator.LessThanExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) < (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.LessThanOrEqualExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) <= (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(Generator.GreaterThanExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) > (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(Generator.GreaterThanOrEqualExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "(x) >= (y)")
        End Sub

        <Fact>
        Public Sub TestConditionalExpressions()
            VerifySyntax(Of BinaryConditionalExpressionSyntax)(Generator.CoalesceExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "If(x, y)")
            VerifySyntax(Of TernaryConditionalExpressionSyntax)(Generator.ConditionalExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"), Generator.IdentifierName("z")), "If(x, y, z)")
        End Sub

        <Fact>
        Public Sub TestMemberAccessExpressions()
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "x.y")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.IdentifierName("x"), "y"), "x.y")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.MemberAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")), "x.y.z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")), "x(y).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.ElementAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")), "x(y).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.AddExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")), "((x) + (y)).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(Generator.MemberAccessExpression(Generator.NegateExpression(Generator.IdentifierName("x")), Generator.IdentifierName("y")), "(-(x)).y")
        End Sub

        <Fact>
        Public Sub TestArrayCreationExpressions()
            VerifySyntax(Of ArrayCreationExpressionSyntax)(
                Generator.ArrayCreationExpression(Generator.IdentifierName("x"), Generator.LiteralExpression(10)),
                "New x(10) {}")

            VerifySyntax(Of ArrayCreationExpressionSyntax)(
                Generator.ArrayCreationExpression(Generator.IdentifierName("x"), {Generator.IdentifierName("y"), Generator.IdentifierName("z")}),
                "New x() {y, z}")
        End Sub

        <Fact>
        Public Sub TestObjectCreationExpressions()
            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                Generator.ObjectCreationExpression(Generator.IdentifierName("x")),
                "New x()")

            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                Generator.ObjectCreationExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")),
                "New x(y)")

            Dim intType = _emptyCompilation.GetSpecialType(SpecialType.System_Int32)
            Dim listType = _emptyCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
            Dim listOfIntType = listType.Construct(intType)

            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                Generator.ObjectCreationExpression(listOfIntType, Generator.IdentifierName("y")),
                "New Global.System.Collections.Generic.List(Of System.Int32)(y)")
        End Sub

        <Fact>
        Public Sub TestElementAccessExpressions()
            VerifySyntax(Of InvocationExpressionSyntax)(
                Generator.ElementAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")),
                "x(y)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                Generator.ElementAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"), Generator.IdentifierName("z")),
                "x(y, z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                Generator.ElementAccessExpression(Generator.MemberAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")),
                "x.y(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                Generator.ElementAccessExpression(Generator.ElementAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")),
                "x(y)(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                Generator.ElementAccessExpression(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")),
                "x(y)(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                Generator.ElementAccessExpression(Generator.AddExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), Generator.IdentifierName("z")),
                "((x) + (y))(z)")
        End Sub

        <Fact>
        Public Sub TestCastAndConvertExpressions()
            VerifySyntax(Of DirectCastExpressionSyntax)(Generator.CastExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "DirectCast(y, x)")
            VerifySyntax(Of CTypeExpressionSyntax)(Generator.ConvertExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "CType(y, x)")
        End Sub

        <Fact>
        Public Sub TestIsAndAsExpressions()
            VerifySyntax(Of TypeOfExpressionSyntax)(Generator.IsTypeExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "TypeOf(x) Is y")
            VerifySyntax(Of TryCastExpressionSyntax)(Generator.TryCastExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "TryCast(x, y)")
            VerifySyntax(Of GetTypeExpressionSyntax)(Generator.TypeOfExpression(Generator.IdentifierName("x")), "GetType(x)")
        End Sub

        <Fact>
        Public Sub TestInvocationExpressions()
            ' without explicit arguments
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.IdentifierName("x")), "x()")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"), Generator.IdentifierName("z")), "x(y, z)")

            ' using explicit arguments
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.Argument(Generator.IdentifierName("y"))), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.Argument(RefKind.Ref, Generator.IdentifierName("y"))), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.Argument(RefKind.Out, Generator.IdentifierName("y"))), "x(y)")

            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.MemberAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), "x.y()")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.ElementAccessExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), "x(y)()")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.InvocationExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), "x(y)()")
            VerifySyntax(Of InvocationExpressionSyntax)(Generator.InvocationExpression(Generator.AddExpression(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), "((x) + (y))()")
        End Sub

        <Fact>
        Public Sub TestAssignmentStatement()
            VerifySyntax(Of AssignmentStatementSyntax)(Generator.AssignmentStatement(Generator.IdentifierName("x"), Generator.IdentifierName("y")), "x = y")
        End Sub

        <Fact>
        Public Sub TestExpressionStatement()
            VerifySyntax(Of ExpressionStatementSyntax)(Generator.ExpressionStatement(Generator.IdentifierName("x")), "x")
            VerifySyntax(Of ExpressionStatementSyntax)(Generator.ExpressionStatement(Generator.InvocationExpression(Generator.IdentifierName("x"))), "x()")
        End Sub

        <Fact>
        Public Sub TestLocalDeclarationStatements()
            VerifySyntax(Of LocalDeclarationStatementSyntax)(Generator.LocalDeclarationStatement(Generator.IdentifierName("x"), "y"), "Dim y As x")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(Generator.LocalDeclarationStatement(Generator.IdentifierName("x"), "y", Generator.IdentifierName("z")), "Dim y As x = z")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(Generator.LocalDeclarationStatement("y", Generator.IdentifierName("z")), "Dim y = z")

            VerifySyntax(Of LocalDeclarationStatementSyntax)(Generator.LocalDeclarationStatement(Generator.IdentifierName("x"), "y", isConst:=True), "Const y As x")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(Generator.LocalDeclarationStatement(Generator.IdentifierName("x"), "y", Generator.IdentifierName("z"), isConst:=True), "Const y As x = z")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(Generator.LocalDeclarationStatement(DirectCast(Nothing, SyntaxNode), "y", Generator.IdentifierName("z"), isConst:=True), "Const y = z")
        End Sub

        <Fact>
        Public Sub TestAwaitExpressions()
            VerifySyntax(Of AwaitExpressionSyntax)(Generator.AwaitExpression(Generator.IdentifierName("x")), "Await x")
        End Sub

        <Fact>
        Public Sub TestNameOfExpressions()
            VerifySyntax(Of NameOfExpressionSyntax)(Generator.NameOfExpression(Generator.IdentifierName("x")), "NameOf(x)")
        End Sub

        <Fact>
        Public Sub TestTupleExpression()
            VerifySyntax(Of TupleExpressionSyntax)(Generator.TupleExpression(
                {Generator.IdentifierName("x"), Generator.IdentifierName("y")}), "(x, y)")
            VerifySyntax(Of TupleExpressionSyntax)(Generator.TupleExpression(
                {Generator.Argument("goo", RefKind.None, Generator.IdentifierName("x")),
                 Generator.Argument("bar", RefKind.None, Generator.IdentifierName("y"))}), "(goo:=x, bar:=y)")
        End Sub

        <Fact>
        Public Sub TestReturnStatements()
            VerifySyntax(Of ReturnStatementSyntax)(Generator.ReturnStatement(), "Return")
            VerifySyntax(Of ReturnStatementSyntax)(Generator.ReturnStatement(Generator.IdentifierName("x")), "Return x")
        End Sub

        <Fact>
        Public Sub TestYieldReturnStatements()
            VerifySyntax(Of YieldStatementSyntax)(Generator.YieldReturnStatement(Generator.LiteralExpression(1)), "Yield 1")
            VerifySyntax(Of YieldStatementSyntax)(Generator.YieldReturnStatement(Generator.IdentifierName("x")), "Yield x")
        End Sub

        <Fact>
        Public Sub TestThrowStatements()
            VerifySyntax(Of ThrowStatementSyntax)(Generator.ThrowStatement(), "Throw")
            VerifySyntax(Of ThrowStatementSyntax)(Generator.ThrowStatement(Generator.IdentifierName("x")), "Throw x")
        End Sub

        <Fact>
        Public Sub TestIfStatements()
            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"), New SyntaxNode() {}),
"If x Then
End If")

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"), Nothing),
"If x Then
End If")

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"), New SyntaxNode() {}, New SyntaxNode() {}),
"If x Then
Else
End If")

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"),
                    {Generator.IdentifierName("y")}),
"If x Then
    y
End If")

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"),
                    {Generator.IdentifierName("y")},
                    {Generator.IdentifierName("z")}),
"If x Then
    y
Else
    z
End If")

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"),
                    {Generator.IdentifierName("y")},
                    {Generator.IfStatement(Generator.IdentifierName("p"), {Generator.IdentifierName("q")})}),
"If x Then
    y
ElseIf p Then
    q
End If")

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                Generator.IfStatement(Generator.IdentifierName("x"),
                    {Generator.IdentifierName("y")},
                    Generator.IfStatement(Generator.IdentifierName("p"),
                        {Generator.IdentifierName("q")},
                        {Generator.IdentifierName("z")})),
"If x Then
    y
ElseIf p Then
    q
Else
    z
End If")

        End Sub

        <Fact>
        Public Sub TestSwitchStatements()
            VerifySyntax(Of SelectBlockSyntax)(
                Generator.SwitchStatement(Generator.IdentifierName("x"),
                    Generator.SwitchSection(Generator.IdentifierName("y"),
                        {Generator.IdentifierName("z")})),
"Select x
    Case y
        z
End Select")

            VerifySyntax(Of SelectBlockSyntax)(
                Generator.SwitchStatement(Generator.IdentifierName("x"),
                    Generator.SwitchSection(
                        {Generator.IdentifierName("y"), Generator.IdentifierName("p"), Generator.IdentifierName("q")},
                        {Generator.IdentifierName("z")})),
"Select x
    Case y, p, q
        z
End Select")

            VerifySyntax(Of SelectBlockSyntax)(
                Generator.SwitchStatement(Generator.IdentifierName("x"),
                    Generator.SwitchSection(Generator.IdentifierName("y"),
                        {Generator.IdentifierName("z")}),
                    Generator.SwitchSection(Generator.IdentifierName("a"),
                        {Generator.IdentifierName("b")})),
"Select x
    Case y
        z
    Case a
        b
End Select")

            VerifySyntax(Of SelectBlockSyntax)(
                Generator.SwitchStatement(Generator.IdentifierName("x"),
                    Generator.SwitchSection(Generator.IdentifierName("y"),
                        {Generator.IdentifierName("z")}),
                    Generator.DefaultSwitchSection(
                        {Generator.IdentifierName("b")})),
"Select x
    Case y
        z
    Case Else
        b
End Select")

            VerifySyntax(Of SelectBlockSyntax)(
                Generator.SwitchStatement(Generator.IdentifierName("x"),
                    Generator.SwitchSection(Generator.IdentifierName("y"),
                        {Generator.ExitSwitchStatement()})),
"Select x
    Case y
        Exit Select
End Select")
        End Sub

        <Fact>
        Public Sub TestUsingStatements()
            VerifySyntax(Of UsingBlockSyntax)(
                Generator.UsingStatement(Generator.IdentifierName("x"), {Generator.IdentifierName("y")}),
"Using x
    y
End Using")

            VerifySyntax(Of UsingBlockSyntax)(
                Generator.UsingStatement("x", Generator.IdentifierName("y"), {Generator.IdentifierName("z")}),
"Using x = y
    z
End Using")

            VerifySyntax(Of UsingBlockSyntax)(
                Generator.UsingStatement(Generator.IdentifierName("x"), "y", Generator.IdentifierName("z"), {Generator.IdentifierName("q")}),
"Using y As x = z
    q
End Using")
        End Sub

        <Fact>
        Public Sub TestLockStatements()
            VerifySyntax(Of SyncLockBlockSyntax)(
                Generator.LockStatement(Generator.IdentifierName("x"), {Generator.IdentifierName("y")}),
"SyncLock x
    y
End SyncLock")
        End Sub

        <Fact>
        Public Sub TestTryCatchStatements()

            VerifySyntax(Of TryBlockSyntax)(
                Generator.TryCatchStatement(
                    {Generator.IdentifierName("x")},
                    Generator.CatchClause(Generator.IdentifierName("y"), "z",
                        {Generator.IdentifierName("a")})),
"Try
    x
Catch z As y
    a
End Try")

            VerifySyntax(Of TryBlockSyntax)(
                Generator.TryCatchStatement(
                    {Generator.IdentifierName("s")},
                    Generator.CatchClause(Generator.IdentifierName("x"), "y",
                        {Generator.IdentifierName("z")}),
                    Generator.CatchClause(Generator.IdentifierName("a"), "b",
                        {Generator.IdentifierName("c")})),
"Try
    s
Catch y As x
    z
Catch b As a
    c
End Try")

            VerifySyntax(Of TryBlockSyntax)(
                Generator.TryCatchStatement(
                    {Generator.IdentifierName("s")},
                    {Generator.CatchClause(Generator.IdentifierName("x"), "y",
                        {Generator.IdentifierName("z")})},
                    {Generator.IdentifierName("a")}),
"Try
    s
Catch y As x
    z
Finally
    a
End Try")

            VerifySyntax(Of TryBlockSyntax)(
                Generator.TryFinallyStatement(
                    {Generator.IdentifierName("x")},
                    {Generator.IdentifierName("a")}),
"Try
    x
Finally
    a
End Try")

        End Sub

        <Fact>
        Public Sub TestWhileStatements()
            VerifySyntax(Of WhileBlockSyntax)(
                Generator.WhileStatement(Generator.IdentifierName("x"), {Generator.IdentifierName("y")}),
"While x
    y
End While")

            VerifySyntax(Of WhileBlockSyntax)(
                Generator.WhileStatement(Generator.IdentifierName("x"), Nothing),
"While x
End While")
        End Sub

        <Fact>
        Public Sub TestLambdaExpressions()
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression("x", Generator.IdentifierName("y")),
                "Function(x) y")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression({Generator.LambdaParameter("x"), Generator.LambdaParameter("y")}, Generator.IdentifierName("z")),
                "Function(x, y) z")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression(New SyntaxNode() {}, Generator.IdentifierName("y")),
                "Function() y")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression("x", Generator.IdentifierName("y")),
                "Sub(x) y")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression({Generator.LambdaParameter("x"), Generator.LambdaParameter("y")}, Generator.IdentifierName("z")),
                "Sub(x, y) z")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression(New SyntaxNode() {}, Generator.IdentifierName("y")),
                "Sub() y")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression("x", {Generator.ReturnStatement(Generator.IdentifierName("y"))}),
"Function(x)
    Return y
End Function")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression({Generator.LambdaParameter("x"), Generator.LambdaParameter("y")}, {Generator.ReturnStatement(Generator.IdentifierName("z"))}),
"Function(x, y)
    Return z
End Function")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression(New SyntaxNode() {}, {Generator.ReturnStatement(Generator.IdentifierName("y"))}),
"Function()
    Return y
End Function")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression("x", {Generator.IdentifierName("y")}),
"Sub(x)
    y
End Sub")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression({Generator.LambdaParameter("x"), Generator.LambdaParameter("y")}, {Generator.IdentifierName("z")}),
"Sub(x, y)
    z
End Sub")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression(New SyntaxNode() {}, {Generator.IdentifierName("y")}),
"Sub()
    y
End Sub")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression({Generator.LambdaParameter("x", Generator.IdentifierName("y"))}, Generator.IdentifierName("z")),
                "Function(x As y) z")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.ValueReturningLambdaExpression({Generator.LambdaParameter("x", Generator.IdentifierName("y")), Generator.LambdaParameter("a", Generator.IdentifierName("b"))}, Generator.IdentifierName("z")),
                "Function(x As y, a As b) z")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression({Generator.LambdaParameter("x", Generator.IdentifierName("y"))}, Generator.IdentifierName("z")),
                "Sub(x As y) z")

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.VoidReturningLambdaExpression({Generator.LambdaParameter("x", Generator.IdentifierName("y")), Generator.LambdaParameter("a", Generator.IdentifierName("b"))}, Generator.IdentifierName("z")),
                "Sub(x As y, a As b) z")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31720")>
        Public Sub TestGetAttributeOnMethodBodies()
            Dim compilation = Compile("
Imports System
<AttributeUsage(System.AttributeTargets.All)>
Public Class MyAttribute
  Inherits Attribute
End Class

Public Class C
    <MyAttribute>
    Sub New()
    End Sub

    <MyAttribute>
    Sub M1()
    End Sub

    <MyAttribute>
    Function M1() As String
        Return Nothing
    End Sub
End Class
")

            Dim syntaxTree = compilation.SyntaxTrees(0)
            Dim declarations = syntaxTree.GetRoot().DescendantNodes().OfType(Of MethodBlockBaseSyntax)

            For Each decl In declarations
                Assert.Equal("<MyAttribute>", Generator.GetAttributes(decl).Single().ToString())
            Next
        End Sub
#End Region

#Region "Declarations"
        <Fact>
        Public Sub TestFieldDeclarations()
            VerifySyntax(Of FieldDeclarationSyntax)(
                Generator.FieldDeclaration("fld", Generator.TypeExpression(SpecialType.System_Int32)),
                "Dim fld As Integer")

            VerifySyntax(Of FieldDeclarationSyntax)(
                Generator.FieldDeclaration("fld", Generator.TypeExpression(SpecialType.System_Int32), initializer:=Generator.LiteralExpression(0)),
                "Dim fld As Integer = 0")

            VerifySyntax(Of FieldDeclarationSyntax)(
                Generator.FieldDeclaration("fld", Generator.TypeExpression(SpecialType.System_Int32), accessibility:=Accessibility.Public),
                "Public fld As Integer")

            VerifySyntax(Of FieldDeclarationSyntax)(
                Generator.FieldDeclaration("fld", Generator.TypeExpression(SpecialType.System_Int32), modifiers:=DeclarationModifiers.Static Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.WithEvents),
                "Shared ReadOnly WithEvents fld As Integer")
        End Sub

        <Fact>
        Public Sub TestMethodDeclarations()
            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m"),
"Sub m()
End Sub")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", typeParameters:={"x", "y"}),
"Sub m(Of x, y)()
End Sub")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x")),
"Function m() As x
End Function")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x"), statements:={Generator.ReturnStatement(Generator.IdentifierName("y"))}),
"Function m() As x
    Return y
End Function")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", parameters:={Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, returnType:=Generator.IdentifierName("x")),
"Function m(z As y) As x
End Function")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", parameters:={Generator.ParameterDeclaration("z", Generator.IdentifierName("y"), Generator.IdentifierName("a"))}, returnType:=Generator.IdentifierName("x")),
"Function m(Optional z As y = a) As x
End Function")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.None),
"Public Function m() As x
End Function")

            VerifySyntax(Of MethodStatementSyntax)(
                Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Abstract),
"Public MustOverride Function m() As x")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration("m", accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Partial),
"Partial Private Sub m()
End Sub")

        End Sub

        <Fact>
        Public Sub TestSealedDeclarationModifier()
            Dim md = Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Sealed)
            Assert.Equal(DeclarationModifiers.Sealed, Generator.GetModifiers(md))
            VerifySyntax(Of MethodBlockSyntax)(
                md,
"NotOverridable Sub m()
End Sub")

            Dim md2 = Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Sealed + DeclarationModifiers.Override)
            Assert.Equal(DeclarationModifiers.Sealed + DeclarationModifiers.Override, Generator.GetModifiers(md2))
            VerifySyntax(Of MethodBlockSyntax)(
                md2,
"NotOverridable Overrides Sub m()
End Sub")

            Dim cd = Generator.ClassDeclaration("c", modifiers:=DeclarationModifiers.Sealed)
            Assert.Equal(DeclarationModifiers.Sealed, Generator.GetModifiers(cd))
            VerifySyntax(Of ClassBlockSyntax)(
                cd,
"NotInheritable Class c
End Class")

        End Sub

        <Fact>
        Public Sub TestAbstractDeclarationModifier()
            Dim md = Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(md))
            VerifySyntax(Of MethodStatementSyntax)(
                md,
"MustOverride Sub m()")

            Dim cd = Generator.ClassDeclaration("c", modifiers:=DeclarationModifiers.Abstract)
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(cd))
            VerifySyntax(Of ClassBlockSyntax)(
                cd,
"MustInherit Class c
End Class")

        End Sub

        <Fact>
        Public Sub TestOperatorDeclaration()
            Dim parameterTypes = {
                _emptyCompilation.GetSpecialType(SpecialType.System_Int32),
                _emptyCompilation.GetSpecialType(SpecialType.System_String)
                }

            Dim parameters = parameterTypes.Select(Function(t, i) Generator.ParameterDeclaration("p" & i, Generator.TypeExpression(t))).ToList()
            Dim returnType = Generator.TypeExpression(SpecialType.System_Boolean)

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Addition, parameters, returnType),
"Operator +(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.BitwiseAnd, parameters, returnType),
"Operator And(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.BitwiseOr, parameters, returnType),
"Operator Or(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Division, parameters, returnType),
"Operator /(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Equality, parameters, returnType),
"Operator =(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.ExclusiveOr, parameters, returnType),
"Operator Xor(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.False, parameters, returnType),
"Operator IsFalse(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.GreaterThan, parameters, returnType),
"Operator>(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.GreaterThanOrEqual, parameters, returnType),
"Operator >=(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Inequality, parameters, returnType),
"Operator <>(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.LeftShift, parameters, returnType),
"Operator <<(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.LessThan, parameters, returnType),
"Operator <(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.LessThanOrEqual, parameters, returnType),
"Operator <=(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.LogicalNot, parameters, returnType),
"Operator Not(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Modulus, parameters, returnType),
"Operator Mod(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Multiply, parameters, returnType),
"Operator *(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.RightShift, parameters, returnType),
"Operator >>(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.Subtraction, parameters, returnType),
"Operator -(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.True, parameters, returnType),
"Operator IsTrue(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.UnaryNegation, parameters, returnType),
"Operator -(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.UnaryPlus, parameters, returnType),
"Operator +(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            ' Conversion operators

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.ImplicitConversion, parameters, returnType),
"Widening Operator CType(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(OperatorKind.ExplicitConversion, parameters, returnType),
"Narrowing Operator CType(p0 As System.Int32, p1 As System.String) As Boolean
End Operator")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65833")>
        Public Sub TestConversionOperatorDeclaration()
            Dim gcHandleType = _emptyCompilation.GetTypeByMetadataName(GetType(GCHandle).FullName)
            Dim Conversion = gcHandleType.GetMembers().OfType(Of IMethodSymbol)().Single(
                Function(m) m.Name = WellKnownMemberNames.ExplicitConversionName AndAlso m.Parameters(0).Type.Equals(gcHandleType))

            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.Declaration(Conversion),
"Public Shared Narrowing Operator CType(value As Global.System.Runtime.InteropServices.GCHandle) As Global.System.IntPtr
End Operator")

            Dim doubleType = _emptyCompilation.GetSpecialType(SpecialType.System_Decimal)
            Conversion = doubleType.GetMembers().OfType(Of IMethodSymbol)().Single(
                Function(m) m.Name = WellKnownMemberNames.ImplicitConversionName AndAlso m.Parameters(0).Type.Equals(_emptyCompilation.GetSpecialType(SpecialType.System_Byte)))
            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.Declaration(Conversion),
"Public Shared Widening Operator CType(value As System.Byte) As System.Decimal
End Operator")
        End Sub

        <Fact>
        Public Sub TestOperatorDeclarationSymbolOverload()
            Dim tree = VisualBasicSyntaxTree.ParseText(
"
Public Class C
    Shared Operator +(c1 As C, c2 As C) As C
    End Operator
End Class")
            Dim compilation = VisualBasicCompilation.Create("AssemblyName", syntaxTrees:={tree})

            Dim additionOperatorSymbol = DirectCast(compilation.GetTypeByMetadataName("C").GetMembers(WellKnownMemberNames.AdditionOperatorName).Single(), IMethodSymbol)
            VerifySyntax(Of OperatorBlockSyntax)(
                Generator.OperatorDeclaration(additionOperatorSymbol),
"Public Shared Operator +(c1 As Global.C, c2 As Global.C) As Global.C
End Operator")
        End Sub

        <Fact>
        Public Sub MethodDeclarationCanRoundTrip()
            Dim tree = VisualBasicSyntaxTree.ParseText(
"
Public Sub Test()
End Sub")
            Dim compilation = VisualBasicCompilation.Create("AssemblyName", syntaxTrees:={tree})
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().First()
            Dim symbol = CType(model.GetDeclaredSymbol(node), IMethodSymbol)
            VerifySyntax(Of MethodBlockSyntax)(
                Generator.MethodDeclaration(symbol),
"Public Sub Test()
End Sub")
        End Sub

        <Fact>
        Public Sub TestPropertyDeclarations()
            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.ReadOnly),
"MustOverride ReadOnly Property p As x")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.WriteOnly),
"MustOverride WriteOnly Property p As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly),
"ReadOnly Property p As x
    Get
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly),
"WriteOnly Property p As x
    Set(value As x)
    End Set
End Property")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
"MustOverride Property p As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly, getAccessorStatements:={Generator.IdentifierName("y")}),
"ReadOnly Property p As x
    Get
        y
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly, setAccessorStatements:={Generator.IdentifierName("y")}),
"WriteOnly Property p As x
    Set(value As x)
        y
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), setAccessorStatements:={Generator.IdentifierName("y")}),
"Property p As x
    Get
    End Get

    Set(value As x)
        y
    End Set
End Property")
        End Sub

        <Fact>
        Public Sub TestAccessorDeclarations2()
            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.WithAccessorDeclarations(Generator.PropertyDeclaration("p", Generator.IdentifierName("x"))),
                "Property p As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.WithAccessorDeclarations(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("x")),
                    Generator.GetAccessorDeclaration(Accessibility.NotApplicable, {Generator.ReturnStatement()})),
"ReadOnly Property p As x
    Get
        Return
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.WithAccessorDeclarations(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("x")),
                    Generator.GetAccessorDeclaration(Accessibility.NotApplicable, {Generator.ReturnStatement()}),
                    Generator.SetAccessorDeclaration(Accessibility.NotApplicable, {Generator.ReturnStatement()})),
"Property p As x
    Get
        Return
    End Get

    Set
        Return
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.WithAccessorDeclarations(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("x")),
                    Generator.GetAccessorDeclaration(Accessibility.Protected, {Generator.ReturnStatement()})),
"ReadOnly Property p As x
    Protected Get
        Return
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.WithAccessorDeclarations(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("x")),
                    Generator.SetAccessorDeclaration(Accessibility.Protected, {Generator.ReturnStatement()})),
"WriteOnly Property p As x
    Protected Set
        Return
    End Set
End Property")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.WithAccessorDeclarations(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}, Generator.IdentifierName("x"))),
                "Default Property Item(p As t) As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.WithAccessorDeclarations(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}, Generator.IdentifierName("x")),
                    Generator.GetAccessorDeclaration(Accessibility.Protected, {Generator.ReturnStatement()})),
"Default ReadOnly Property Item(p As t) As x
    Protected Get
        Return
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.WithAccessorDeclarations(
                    Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}, Generator.IdentifierName("x")),
                    Generator.SetAccessorDeclaration(Accessibility.Protected, {Generator.ReturnStatement()})),
"Default WriteOnly Property Item(p As t) As x
    Protected Set
        Return
    End Set
End Property")
        End Sub

        <Fact>
        Public Sub TestIndexerDeclarations()
            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.ReadOnly),
"Default MustOverride ReadOnly Property Item(z As y) As x")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.WriteOnly),
"Default MustOverride WriteOnly Property Item(z As y) As x")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
"Default MustOverride Property Item(z As y) As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly),
"Default ReadOnly Property Item(z As y) As x
    Get
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly),
"Default WriteOnly Property Item(z As y) As x
    Set(value As x)
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly,
                    getAccessorStatements:={Generator.IdentifierName("a")}),
"Default ReadOnly Property Item(z As y) As x
    Get
        a
    End Get
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly,
                    setAccessorStatements:={Generator.IdentifierName("a")}),
"Default WriteOnly Property Item(z As y) As x
    Set(value As x)
        a
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.None),
"Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"),
                    setAccessorStatements:={Generator.IdentifierName("a")}),
"Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
        a
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"),
                    getAccessorStatements:={Generator.IdentifierName("a")}, setAccessorStatements:={Generator.IdentifierName("b")}),
"Default Property Item(z As y) As x
    Get
        a
    End Get

    Set(value As x)
        b
    End Set
End Property")

        End Sub

        <Fact>
        Public Sub TestEventDeclarations()
            VerifySyntax(Of EventStatementSyntax)(
                Generator.EventDeclaration("ev", Generator.IdentifierName("t")),
"Event ev As t")

            VerifySyntax(Of EventStatementSyntax)(
                Generator.EventDeclaration("ev", Generator.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Static),
"Public Shared Event ev As t")

            VerifySyntax(Of EventBlockSyntax)(
                Generator.CustomEventDeclaration("ev", Generator.IdentifierName("t")),
"Custom Event ev As t
    AddHandler(value As t)
    End AddHandler

    RemoveHandler(value As t)
    End RemoveHandler

    RaiseEvent()
    End RaiseEvent
End Event")

            Dim params = {Generator.ParameterDeclaration("sender", Generator.TypeExpression(SpecialType.System_Object)), Generator.ParameterDeclaration("args", Generator.IdentifierName("EventArgs"))}
            VerifySyntax(Of EventBlockSyntax)(
                Generator.CustomEventDeclaration("ev", Generator.IdentifierName("t"), parameters:=params),
"Custom Event ev As t
    AddHandler(value As t)
    End AddHandler

    RemoveHandler(value As t)
    End RemoveHandler

    RaiseEvent(sender As Object, args As EventArgs)
    End RaiseEvent
End Event")

        End Sub

        <Fact>
        Public Sub TestConstructorDeclaration()
            VerifySyntax(Of ConstructorBlockSyntax)(
                Generator.ConstructorDeclaration("c"),
"Sub New()
End Sub")

            VerifySyntax(Of ConstructorBlockSyntax)(
                Generator.ConstructorDeclaration("c", accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Static),
"Public Shared Sub New()
End Sub")

            VerifySyntax(Of ConstructorBlockSyntax)(
                Generator.ConstructorDeclaration("c", parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}),
"Sub New(p As t)
End Sub")

            VerifySyntax(Of ConstructorBlockSyntax)(
                Generator.ConstructorDeclaration("c",
                    parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))},
                    baseConstructorArguments:={Generator.IdentifierName("p")}),
"Sub New(p As t)
    MyBase.New(p)
End Sub")
        End Sub

        <Fact>
        Public Sub TestClassDeclarations()
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c"),
"Class c
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", typeParameters:={"x", "y"}),
"Class c(Of x, y)
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", accessibility:=Accessibility.Public),
"Public Class c
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", baseType:=Generator.IdentifierName("x")),
"Class c
    Inherits x

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", interfaceTypes:={Generator.IdentifierName("x")}),
"Class c
    Implements x

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", baseType:=Generator.IdentifierName("x"), interfaceTypes:={Generator.IdentifierName("y"), Generator.IdentifierName("z")}),
"Class c
    Inherits x
    Implements y, z

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", interfaceTypes:={}),
"Class c
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("c", members:={Generator.FieldDeclaration("y", type:=Generator.IdentifierName("x"))}),
"Class c

    Dim y As x
End Class")

        End Sub

        <Fact>
        Public Sub TestStructDeclarations()
            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s"),
"Structure s
End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", typeParameters:={"x", "y"}),
"Structure s(Of x, y)
End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Partial),
"Partial Public Structure s
End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", interfaceTypes:={Generator.IdentifierName("x")}),
"Structure s
    Implements x

End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", interfaceTypes:={Generator.IdentifierName("x"), Generator.IdentifierName("y")}),
"Structure s
    Implements x, y

End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", interfaceTypes:={}),
"Structure s
End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", members:={Generator.FieldDeclaration("y", Generator.IdentifierName("x"))}),
"Structure s

    Dim y As x
End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s", members:={Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"))}),
"Structure s

    Function m() As t
    End Function
End Structure")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.StructDeclaration("s",
                    members:={Generator.ConstructorDeclaration(accessibility:=Accessibility.NotApplicable, modifiers:=DeclarationModifiers.None)}),
"Structure s

    Sub New()
    End Sub
End Structure")
        End Sub

        <Fact>
        Public Sub TestInterfaceDeclarations()
            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i"),
"Interface i
End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", typeParameters:={"x", "y"}),
"Interface i(Of x, y)
End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", interfaceTypes:={Generator.IdentifierName("a")}),
"Interface i
    Inherits a

End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", interfaceTypes:={Generator.IdentifierName("a"), Generator.IdentifierName("b")}),
"Interface i
    Inherits a, b

End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", interfaceTypes:={}),
"Interface i
End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", members:={Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Sealed)}),
"Interface i

    Function m() As t

End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", members:={Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Sealed)}),
"Interface i

    Property p As t

End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", members:={Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.ReadOnly)}),
"Interface i

    ReadOnly Property p As t

End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", members:={Generator.IndexerDeclaration({Generator.ParameterDeclaration("y", Generator.IdentifierName("x"))}, Generator.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.Sealed)}),
"Interface i

    Default Property Item(y As x) As t

End Interface")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.InterfaceDeclaration("i", members:={Generator.IndexerDeclaration({Generator.ParameterDeclaration("y", Generator.IdentifierName("x"))}, Generator.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.ReadOnly)}),
"Interface i

    Default ReadOnly Property Item(y As x) As t

End Interface")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66377")>
        Public Sub TestInterfaceVariance()
            Dim compilation = Compile("
interface I(of in X, out Y)
end interface
                ")

            Dim symbol = compilation.GlobalNamespace.GetMembers("I").Single()

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.Declaration(symbol),
"Friend Interface I(Of In X, Out Y)
End Interface")
        End Sub

        <Fact>
        Public Sub TestEnumDeclarations()
            VerifySyntax(Of EnumBlockSyntax)(
                Generator.EnumDeclaration("e"),
"Enum e
End Enum")

            VerifySyntax(Of EnumBlockSyntax)(
                Generator.EnumDeclaration("e", members:={Generator.EnumMember("a"), Generator.EnumMember("b"), Generator.EnumMember("c")}),
"Enum e
    a
    b
    c
End Enum")

            VerifySyntax(Of EnumBlockSyntax)(
                Generator.EnumDeclaration("e", members:={Generator.IdentifierName("a"), Generator.EnumMember("b"), Generator.IdentifierName("c")}),
"Enum e
    a
    b
    c
End Enum")

            VerifySyntax(Of EnumBlockSyntax)(
                Generator.EnumDeclaration("e", members:={Generator.EnumMember("a", Generator.LiteralExpression(0)), Generator.EnumMember("b"), Generator.EnumMember("c", Generator.LiteralExpression(5))}),
"Enum e
    a = 0
    b
    c = 5
End Enum")
        End Sub

        <Fact>
        Public Sub TestDelegateDeclarations()
            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.DelegateDeclaration("d"),
"Delegate Sub d()")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.DelegateDeclaration("d", parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}),
"Delegate Sub d(p As t)")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.DelegateDeclaration("d", returnType:=Generator.IdentifierName("t")),
"Delegate Function d() As t")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.DelegateDeclaration("d", parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}, returnType:=Generator.IdentifierName("t")),
"Delegate Function d(p As t) As t")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.DelegateDeclaration("d", accessibility:=Accessibility.Public),
"Public Delegate Sub d()")

            ' ignores modifiers
            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.DelegateDeclaration("d", modifiers:=DeclarationModifiers.Static),
"Delegate Sub d()")
        End Sub

        <Fact>
        Public Sub TestNamespaceImportDeclarations()
            VerifySyntax(Of ImportsStatementSyntax)(
                Generator.NamespaceImportDeclaration(Generator.IdentifierName("n")),
"Imports n")

            VerifySyntax(Of ImportsStatementSyntax)(
                Generator.NamespaceImportDeclaration("n"),
"Imports n")

            VerifySyntax(Of ImportsStatementSyntax)(
                Generator.NamespaceImportDeclaration("n.m"),
"Imports n.m")
        End Sub

        <Fact>
        Public Sub TestNamespaceDeclarations()
            VerifySyntax(Of NamespaceBlockSyntax)(
                Generator.NamespaceDeclaration("n"),
"Namespace n
End Namespace")

            VerifySyntax(Of NamespaceBlockSyntax)(
                Generator.NamespaceDeclaration("n.m"),
"Namespace n.m
End Namespace")

            VerifySyntax(Of NamespaceBlockSyntax)(
                Generator.NamespaceDeclaration("n",
                    Generator.NamespaceImportDeclaration("m")),
"Namespace n

    Imports m
End Namespace")

            VerifySyntax(Of NamespaceBlockSyntax)(
                Generator.NamespaceDeclaration("n",
                    Generator.ClassDeclaration("c"),
                    Generator.NamespaceImportDeclaration("m")),
"Namespace n

    Imports m

    Class c
    End Class
End Namespace")
        End Sub

        <Fact>
        Public Sub TestCompilationUnits()
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.CompilationUnit(),
                "")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.CompilationUnit(
                    Generator.NamespaceDeclaration("n")),
"Namespace n
End Namespace
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.CompilationUnit(
                    Generator.NamespaceImportDeclaration("n")),
"Imports n
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.CompilationUnit(
                    Generator.ClassDeclaration("c"),
                    Generator.NamespaceImportDeclaration("m")),
"Imports m

Class c
End Class
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.CompilationUnit(
                    Generator.NamespaceImportDeclaration("n"),
                    Generator.NamespaceDeclaration("n",
                        Generator.NamespaceImportDeclaration("m"),
                        Generator.ClassDeclaration("c"))),
"Imports n

Namespace n

    Imports m

    Class c
    End Class
End Namespace
")
        End Sub

        <Fact>
        Public Sub TestAsPublicInterfaceImplementation()
            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPublicInterfaceImplementation(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    Generator.IdentifierName("i")),
"Public Function m() As t Implements i.m
End Function")

            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPublicInterfaceImplementation(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    Generator.IdentifierName("i")),
"Public Function m() As t Implements i.m
End Function")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AsPublicInterfaceImplementation(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    Generator.IdentifierName("i")),
"Public Property p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AsPublicInterfaceImplementation(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.None),
                    Generator.IdentifierName("i")),
"Public Property p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AsPublicInterfaceImplementation(
                    Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("a"))}, Generator.IdentifierName("t"), Accessibility.Internal, DeclarationModifiers.Abstract),
                    Generator.IdentifierName("i")),
"Default Public Property Item(p As a) As t Implements i.Item
    Get
    End Get

    Set(value As t)
    End Set
End Property")

            ' convert private method to public
            Dim pim = Generator.AsPrivateInterfaceImplementation(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t")),
                    Generator.IdentifierName("i"))

            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPublicInterfaceImplementation(pim, Generator.IdentifierName("i2")),
"Public Function m() As t Implements i2.m
End Function")

            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPublicInterfaceImplementation(pim, Generator.IdentifierName("i2"), "m2"),
"Public Function m2() As t Implements i2.m2
End Function")
        End Sub

        <Fact>
        Public Sub TestAsPrivateInterfaceImplementation()
            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPrivateInterfaceImplementation(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    Generator.IdentifierName("i")),
"Private Function i_m() As t Implements i.m
End Function")

            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPrivateInterfaceImplementation(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    Generator.TypeExpression(Me._ienumerableInt)),
"Private Function IEnumerable_Int32_m() As t Implements Global.System.Collections.Generic.IEnumerable(Of System.Int32).m
End Function")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AsPrivateInterfaceImplementation(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal, modifiers:=DeclarationModifiers.Abstract),
                    Generator.IdentifierName("i")),
"Private Property i_p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AsPrivateInterfaceImplementation(
                    Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("a"))}, Generator.IdentifierName("t"), Accessibility.Protected, DeclarationModifiers.Abstract),
                    Generator.IdentifierName("i")),
"Private Property i_Item(p As a) As t Implements i.Item
    Get
    End Get

    Set(value As t)
    End Set
End Property")

            ' convert public method to private
            Dim pim = Generator.AsPublicInterfaceImplementation(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t")),
                    Generator.IdentifierName("i"))

            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPrivateInterfaceImplementation(pim, Generator.IdentifierName("i2")),
"Private Function i2_m() As t Implements i2.m
End Function")

            VerifySyntax(Of MethodBlockBaseSyntax)(
                Generator.AsPrivateInterfaceImplementation(pim, Generator.IdentifierName("i2"), "m2"),
"Private Function i2_m2() As t Implements i2.m2
End Function")
        End Sub

        <Fact>
        Public Sub TestWithTypeParameters()
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a"),
"MustOverride Sub m(Of a)()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.None),
                    "a"),
"Sub m(Of a)()
End Sub")

            ' assigning no type parameters is legal
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)),
"MustOverride Sub m()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.None)),
"Sub m()
End Sub")

            ' removing type parameters
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeParameters(Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a")),
"MustOverride Sub m()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeParameters(Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m"),
                    "a")),
"Sub m()
End Sub")

            ' multiple type parameters
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a", "b"),
"MustOverride Sub m(Of a, b)()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeParameters(
                    Generator.MethodDeclaration("m"),
                    "a", "b"),
"Sub m(Of a, b)()
End Sub")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.WithTypeParameters(
                    Generator.ClassDeclaration("c"),
                    "a", "b"),
"Class c(Of a, b)
End Class")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.WithTypeParameters(
                    Generator.StructDeclaration("s"),
                    "a", "b"),
"Structure s(Of a, b)
End Structure")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.WithTypeParameters(
                    Generator.InterfaceDeclaration("i"),
                    "a", "b"),
"Interface i(Of a, b)
End Interface")

        End Sub

        <Fact>
        Public Sub TestWithTypeConstraint()
            ' single type constraint
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", Generator.IdentifierName("b")),
"MustOverride Sub m(Of a As b)()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m"), "a"),
                    "a", Generator.IdentifierName("b")),
"Sub m(Of a As b)()
End Sub")

            ' multiple type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", Generator.IdentifierName("b"), Generator.IdentifierName("c")),
"MustOverride Sub m(Of a As {b, c})()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m"), "a"),
                    "a", Generator.IdentifierName("b"), Generator.IdentifierName("c")),
"Sub m(Of a As {b, c})()
End Sub")

            ' no type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a"),
"MustOverride Sub m(Of a)()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m"), "a"),
                    "a"),
"Sub m(Of a)()
End Sub")

            ' removed type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", Generator.IdentifierName("b"), Generator.IdentifierName("c")), "a"),
"MustOverride Sub m(Of a)()")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithTypeConstraint(Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m"), "a"),
                    "a", Generator.IdentifierName("b"), Generator.IdentifierName("c")), "a"),
"Sub m(Of a)()
End Sub")

            ' multiple type parameters with constraints
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeConstraint(
                        Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a", "x"),
                        "a", Generator.IdentifierName("b"), Generator.IdentifierName("c")),
                    "x", Generator.IdentifierName("y")),
"MustOverride Sub m(Of a As {b, c}, x As y)()")

            ' with constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.Constructor),
"MustOverride Sub m(Of a As New)()")

            ' with reference constraint
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType),
"MustOverride Sub m(Of a As Class)()")

            ' with value type constraint
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType),
"MustOverride Sub m(Of a As Structure)()")

            ' with reference constraint and constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType Or SpecialTypeConstraintKind.Constructor),
"MustOverride Sub m(Of a As {Class, New})()")

            ' with value type constraint and constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType Or SpecialTypeConstraintKind.Constructor),
"MustOverride Sub m(Of a As {Structure, New})()")

            ' with reference constraint and type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType, Generator.IdentifierName("b"), Generator.IdentifierName("c")),
"MustOverride Sub m(Of a As {Class, b, c})()")

            ' class declarations
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(
                        Generator.ClassDeclaration("c"),
                        "a", "b"),
                    "a", Generator.IdentifierName("x")),
"Class c(Of a As x, b)
End Class")

            ' structure declarations
            VerifySyntax(Of StructureBlockSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(
                        Generator.StructDeclaration("s"),
                        "a", "b"),
                    "a", Generator.IdentifierName("x")),
"Structure s(Of a As x, b)
End Structure")

            ' interface declarations
            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.WithTypeConstraint(
                    Generator.WithTypeParameters(
                        Generator.InterfaceDeclaration("i"),
                        "a", "b"),
                    "a", Generator.IdentifierName("x")),
"Interface i(Of a As x, b)
End Interface")

        End Sub

        <Fact>
        Public Sub TestAttributeDeclarations()
            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute(Generator.IdentifierName("a")),
                "<a>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a"),
                "<a>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a.b"),
                "<a.b>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a", {}),
                "<a()>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a", {Generator.IdentifierName("x")}),
                "<a(x)>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a", {Generator.AttributeArgument(Generator.IdentifierName("x"))}),
                "<a(x)>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a", {Generator.AttributeArgument("x", Generator.IdentifierName("y"))}),
                "<a(x:=y)>")

            VerifySyntax(Of AttributeListSyntax)(
                Generator.Attribute("a", {Generator.IdentifierName("x"), Generator.IdentifierName("y")}),
                "<a(x, y)>")
        End Sub

        <Fact>
        Public Sub TestAddAttributes()
            VerifySyntax(Of FieldDeclarationSyntax)(
                Generator.AddAttributes(
                    Generator.FieldDeclaration("y", Generator.IdentifierName("x")),
                    Generator.Attribute("a")),
"<a>
Dim y As x")

            VerifySyntax(Of FieldDeclarationSyntax)(
                Generator.AddAttributes(
                    Generator.AddAttributes(
                        Generator.FieldDeclaration("y", Generator.IdentifierName("x")),
                        Generator.Attribute("a")),
                    Generator.Attribute("b")),
"<a>
<b>
Dim y As x")

            VerifySyntax(Of MethodStatementSyntax)(
                Generator.AddAttributes(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    Generator.Attribute("a")),
"<a>
MustOverride Function m() As t")

            VerifySyntax(Of MethodStatementSyntax)(
                Generator.AddReturnAttributes(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    Generator.Attribute("a")),
"MustOverride Function m() As <a> t")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.AddAttributes(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    Generator.Attribute("a")),
"<a>
Function m() As t
End Function")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.AddReturnAttributes(
                    Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    Generator.Attribute("a")),
"Function m() As <a> t
End Function")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.AddAttributes(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
                    Generator.Attribute("a")),
"<a>
MustOverride Property p As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AddAttributes(
                    Generator.PropertyDeclaration("p", Generator.IdentifierName("x")),
                    Generator.Attribute("a")),
"<a>
Property p As x
    Get
    End Get

    Set(value As x)
    End Set
End Property")

            VerifySyntax(Of PropertyStatementSyntax)(
                Generator.AddAttributes(
                    Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
                    Generator.Attribute("a")),
"<a>
Default MustOverride Property Item(z As y) As x")

            VerifySyntax(Of PropertyBlockSyntax)(
                Generator.AddAttributes(
                    Generator.IndexerDeclaration({Generator.ParameterDeclaration("z", Generator.IdentifierName("y"))}, Generator.IdentifierName("x")),
                    Generator.Attribute("a")),
"<a>
Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
    End Set
End Property")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.AddAttributes(
                    Generator.ClassDeclaration("c"),
                    Generator.Attribute("a")),
"<a>
Class c
End Class")

            VerifySyntax(Of ParameterSyntax)(
                Generator.AddAttributes(
                    Generator.ParameterDeclaration("p", Generator.IdentifierName("t")),
                    Generator.Attribute("a")),
"<a> p As t")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.AddAttributes(
                    Generator.CompilationUnit(Generator.NamespaceDeclaration("n")),
                    Generator.Attribute("a")),
"<Assembly:a>
Namespace n
End Namespace
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.AddAttributes(
                    Generator.AddAttributes(
                        Generator.CompilationUnit(Generator.NamespaceDeclaration("n")),
                        Generator.Attribute("a")),
                    Generator.Attribute("b")),
"<Assembly:a>
<Assembly:b>
Namespace n
End Namespace
")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.AddAttributes(
                    Generator.DelegateDeclaration("d"),
                    Generator.Attribute("a")),
"<a>
Delegate Sub d()")

        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5066")>
        Public Sub TestAddAttributesOnAccessors()
            Dim prop = Generator.PropertyDeclaration("P", Generator.IdentifierName("T"))

            Dim evnt = DirectCast(SyntaxFactory.ParseCompilationUnit("
Class C
  Custom Event MyEvent As MyDelegate
      AddHandler(ByVal value As MyDelegate)
      End AddHandler

      RemoveHandler(ByVal value As MyDelegate)
      End RemoveHandler

      RaiseEvent(ByVal message As String)
      End RaiseEvent
  End Event
End Class
").Members(0), ClassBlockSyntax).Members(0)

            CheckAddRemoveAttribute(Generator.GetAccessor(prop, DeclarationKind.GetAccessor))
            CheckAddRemoveAttribute(Generator.GetAccessor(prop, DeclarationKind.SetAccessor))
            CheckAddRemoveAttribute(Generator.GetAccessor(evnt, DeclarationKind.AddAccessor))
            CheckAddRemoveAttribute(Generator.GetAccessor(evnt, DeclarationKind.RemoveAccessor))
            CheckAddRemoveAttribute(Generator.GetAccessor(evnt, DeclarationKind.RaiseAccessor))
        End Sub

        Private Sub CheckAddRemoveAttribute(declaration As SyntaxNode)
            Dim initialAttributes = Generator.GetAttributes(declaration)
            Assert.Equal(0, initialAttributes.Count)

            Dim withAttribute = Generator.AddAttributes(declaration, Generator.Attribute("a"))
            Dim attrsAdded = Generator.GetAttributes(withAttribute)
            Assert.Equal(1, attrsAdded.Count)

            Dim withoutAttribute = Generator.RemoveNode(withAttribute, attrsAdded(0))
            Dim attrsRemoved = Generator.GetAttributes(withoutAttribute)
            Assert.Equal(0, attrsRemoved.Count)
        End Sub

        <Fact>
        Public Sub TestAddRemoveAttributesPreservesTrivia()
            Dim cls = ParseCompilationUnit(
"' comment
Class C
End Class ' end").Members(0)

            Dim added = Generator.AddAttributes(cls, Generator.Attribute("a"))
            VerifySyntax(Of ClassBlockSyntax)(
                added,
"' comment
<a>
Class C
End Class ' end")

            Dim removed = Generator.RemoveAllAttributes(added)
            VerifySyntax(Of ClassBlockSyntax)(
                removed,
"' comment
Class C
End Class ' end")

            Dim attrWithComment = Generator.GetAttributes(added).First()
            VerifySyntax(Of AttributeListSyntax)(
                attrWithComment,
"' comment
<a>")

        End Sub

        <Fact>
        Public Sub TestInterfaceDeclarationWithEventSymbol()
            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.Declaration(_emptyCompilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged")),
"Public Interface INotifyPropertyChanged

    Event PropertyChanged As Global.System.ComponentModel.PropertyChangedEventHandler

End Interface")
        End Sub

        <Fact>
        Public Sub TestEnumDeclarationFromSymbol()
            VerifySyntax(Of EnumBlockSyntax)(Generator.Declaration(_emptyCompilation.GetTypeByMetadataName("System.DateTimeKind")),
"Public Enum DateTimeKind
    Unspecified = 0
    Utc = 1
    Local = 2
End Enum")
        End Sub

        <Fact>
        Public Sub TestEnumWithUnderlyingTypeFromSymbol()
            VerifySyntax(Of EnumBlockSyntax)(Generator.Declaration(_emptyCompilation.GetTypeByMetadataName("System.Security.SecurityRuleSet")),
"Public Enum SecurityRuleSet As Byte
    None = CByte(0)
    Level1 = CByte(1)
    Level2 = CByte(2)
End Enum")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66381")>
        Public Sub TestDelegateDeclarationFromSymbol()
            Dim compilation = _emptyCompilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree("Public Delegate Sub D()"))
            Dim type = compilation.GetTypeByMetadataName("D")
            VerifySyntax(Of DelegateStatementSyntax)(Generator.Declaration(type), "Public Delegate Sub D()")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65835")>
        Public Sub TestMethodDeclarationFromSymbol()
            Dim compilation = _emptyCompilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(
"Class C
    Public Sub M(ParamArray arr() As Integer)
    End Sub
End Class"))

            Dim type = compilation.GetTypeByMetadataName("C")
            Dim method = type.GetMembers("M").Single()

            VerifySyntax(Of MethodBlockSyntax)(Generator.Declaration(method),
"Public Sub M(ParamArray arr As System.Int32())
End Sub")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66379")>
        Public Sub TestPropertyDeclarationFromSymbol1()
            Dim compilation = _emptyCompilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(
"Class C
    Public Property Prop As Integer
        Get
        End Get

        Protected Set
        End Set
    End Property
End Class"))

            Dim type = compilation.GetTypeByMetadataName("C")
            Dim method = type.GetMembers("Prop").Single()

            VerifySyntax(Of PropertyBlockSyntax)(Generator.Declaration(method),
"Public Property Prop As System.Int32
    Get
    End Get

    Protected Set
    End Set
End Property")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66379")>
        Public Sub TestPropertyDeclarationFromSymbol2()
            Dim compilation = _emptyCompilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(
"Class C
    Public Property Prop As Integer
        Protected Get
        End Get

        Set
        End Set
    End Property
End Class"))

            Dim type = compilation.GetTypeByMetadataName("C")
            Dim method = type.GetMembers("Prop").Single()

            VerifySyntax(Of PropertyBlockSyntax)(Generator.Declaration(method),
"Public Property Prop As System.Int32
    Protected Get
    End Get

    Set
    End Set
End Property")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66374")>
        Public Sub TestDestructor1()
            Dim compilation = _emptyCompilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(
"Class C
    Protected Overrides Sub Finalize()
    End Sub
End Class"))

            Dim type = compilation.GetTypeByMetadataName("C")
            Dim method = type.GetMembers(WellKnownMemberNames.DestructorName).Single()

            VerifySyntax(Of MethodBlockSyntax)(Generator.Declaration(method),
"Protected Overrides Sub Finalize()
End Sub")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69376")>
        Public Sub TestConstantDecimalFieldDeclarationFromMetadata()
            Dim compilation = _emptyCompilation.
                WithOptions(New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).
                AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(
"Class C
    Public Const F As Decimal = 8675309000000 
End Class"))
            Dim reference = compilation.EmitToPortableExecutableReference()

            compilation = _emptyCompilation.AddReferences(reference)

            Dim type = compilation.GetTypeByMetadataName("C")
            Dim field = type.GetMembers("F").Single()

            VerifySyntax(Of FieldDeclarationSyntax)(Generator.Declaration(field),
                "Public Const F As System.Decimal = 8675309000000D")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69380")>
        Public Sub TestConstantFieldDeclarationSpecialTypes()
            Dim field = _emptyCompilation.GetSpecialType(SpecialType.System_UInt32).GetMembers(NameOf(UInt32.MaxValue)).Single()

            VerifySyntax(Of FieldDeclarationSyntax)(Generator.Declaration(field),
                "Public Const MaxValue As System.UInt32 = 4294967295UI")
        End Sub

#End Region

#Region "Add/Insert/Remove/Get/Set members & elements"

        <Fact>
        Public Sub TestRemoveNodeInTrivia()
            Dim code = "
'''<summary> ... </summary>
Public Class C
End Class
"
            Dim cu = SyntaxFactory.ParseCompilationUnit(code)
            Dim cls = cu.Members(0)
            Dim summary = cls.DescendantNodes(descendIntoTrivia:=True).OfType(Of XmlElementSyntax)().First()
            Dim newCu = Generator.RemoveNode(cu, summary)
            VerifySyntaxRaw(Of CompilationUnitSyntax)(
                newCu,
                "

Public Class C
End Class
")
        End Sub

        <Fact>
        Public Sub TestReplaceNodeInTrivia()
            Dim code = "
'''<summary> ... </summary>
Public Class C
End Class
"
            Dim cu = SyntaxFactory.ParseCompilationUnit(code)
            Dim cls = cu.Members(0)
            Dim summary = cls.DescendantNodes(descendIntoTrivia:=True).OfType(Of XmlElementSyntax)().First()
            Dim summary2 = summary.WithContent(Nothing)
            Dim newCu = Generator.ReplaceNode(cu, summary, summary2)
            VerifySyntaxRaw(Of CompilationUnitSyntax)(
                newCu,
                "
'''<summary></summary>
Public Class C
End Class
")
        End Sub

        <Fact>
        Public Sub TestInsertNodeAfterInTrivia()
            Dim code = "
'''<summary> ... </summary>
Public Class C
End Class
"
            Dim cu = SyntaxFactory.ParseCompilationUnit(code)
            Dim cls = cu.Members(0)
            Dim text = cls.DescendantNodes(descendIntoTrivia:=True).OfType(Of XmlTextSyntax)().First()
            Dim newCu = Generator.InsertNodesAfter(cu, text, {text})
            VerifySyntaxRaw(Of CompilationUnitSyntax)(
                newCu,
                "
'''<summary> ...  ... </summary>
Public Class C
End Class
")
        End Sub

        <Fact>
        Public Sub TestInsertNodeBeforeInTrivia()
            Dim code = "
'''<summary> ... </summary>
Public Class C
End Class
"
            Dim cu = SyntaxFactory.ParseCompilationUnit(code)
            Dim cls = cu.Members(0)
            Dim text = cls.DescendantNodes(descendIntoTrivia:=True).OfType(Of XmlTextSyntax)().First()
            Dim newCu = Generator.InsertNodesBefore(cu, text, {text})
            VerifySyntaxRaw(Of CompilationUnitSyntax)(
                newCu,
                "
'''<summary> ...  ... </summary>
Public Class C
End Class
")
        End Sub

        <Fact>
        Public Sub TestDeclarationKind()
            Assert.Equal(DeclarationKind.CompilationUnit, Generator.GetDeclarationKind(Generator.CompilationUnit()))
            Assert.Equal(DeclarationKind.Class, Generator.GetDeclarationKind(Generator.ClassDeclaration("c")))
            Assert.Equal(DeclarationKind.Struct, Generator.GetDeclarationKind(Generator.StructDeclaration("s")))
            Assert.Equal(DeclarationKind.Interface, Generator.GetDeclarationKind(Generator.InterfaceDeclaration("i")))
            Assert.Equal(DeclarationKind.Enum, Generator.GetDeclarationKind(Generator.EnumDeclaration("e")))
            Assert.Equal(DeclarationKind.Delegate, Generator.GetDeclarationKind(Generator.DelegateDeclaration("d")))
            Assert.Equal(DeclarationKind.Method, Generator.GetDeclarationKind(Generator.MethodDeclaration("m")))
            Assert.Equal(DeclarationKind.Method, Generator.GetDeclarationKind(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationKind.Constructor, Generator.GetDeclarationKind(Generator.ConstructorDeclaration()))
            Assert.Equal(DeclarationKind.Parameter, Generator.GetDeclarationKind(Generator.ParameterDeclaration("p")))
            Assert.Equal(DeclarationKind.Property, Generator.GetDeclarationKind(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.Property, Generator.GetDeclarationKind(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationKind.Indexer, Generator.GetDeclarationKind(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.Indexer, Generator.GetDeclarationKind(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(Generator.FieldDeclaration("f", Generator.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.EnumMember, Generator.GetDeclarationKind(Generator.EnumMember("v")))
            Assert.Equal(DeclarationKind.Event, Generator.GetDeclarationKind(Generator.EventDeclaration("e", Generator.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.CustomEvent, Generator.GetDeclarationKind(Generator.CustomEventDeclaration("ce", Generator.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.Namespace, Generator.GetDeclarationKind(Generator.NamespaceDeclaration("n")))
            Assert.Equal(DeclarationKind.NamespaceImport, Generator.GetDeclarationKind(Generator.NamespaceImportDeclaration("u")))
            Assert.Equal(DeclarationKind.Variable, Generator.GetDeclarationKind(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc")))
            Assert.Equal(DeclarationKind.Attribute, Generator.GetDeclarationKind(Generator.Attribute("a")))
        End Sub

        <Fact>
        Public Sub TestGetName()
            Assert.Equal("c", Generator.GetName(Generator.ClassDeclaration("c")))
            Assert.Equal("s", Generator.GetName(Generator.StructDeclaration("s")))
            Assert.Equal("i", Generator.GetName(Generator.EnumDeclaration("i")))
            Assert.Equal("e", Generator.GetName(Generator.EnumDeclaration("e")))
            Assert.Equal("d", Generator.GetName(Generator.DelegateDeclaration("d")))
            Assert.Equal("m", Generator.GetName(Generator.MethodDeclaration("m")))
            Assert.Equal("m", Generator.GetName(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal("", Generator.GetName(Generator.ConstructorDeclaration()))
            Assert.Equal("p", Generator.GetName(Generator.ParameterDeclaration("p")))
            Assert.Equal("p", Generator.GetName(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal("p", Generator.GetName(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"))))
            Assert.Equal("Item", Generator.GetName(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"))))
            Assert.Equal("Item", Generator.GetName(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal("f", Generator.GetName(Generator.FieldDeclaration("f", Generator.IdentifierName("t"))))
            Assert.Equal("v", Generator.GetName(Generator.EnumMember("v")))
            Assert.Equal("ef", Generator.GetName(Generator.EventDeclaration("ef", Generator.IdentifierName("t"))))
            Assert.Equal("ep", Generator.GetName(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("t"))))
            Assert.Equal("n", Generator.GetName(Generator.NamespaceDeclaration("n")))
            Assert.Equal("u", Generator.GetName(Generator.NamespaceImportDeclaration("u")))
            Assert.Equal("loc", Generator.GetName(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc")))
            Assert.Equal("a", Generator.GetName(Generator.Attribute("a")))
        End Sub

        <Fact>
        Public Sub TestWithName()
            Assert.Equal("c", Generator.GetName(Generator.WithName(Generator.ClassDeclaration("x"), "c")))
            Assert.Equal("s", Generator.GetName(Generator.WithName(Generator.StructDeclaration("x"), "s")))
            Assert.Equal("i", Generator.GetName(Generator.WithName(Generator.EnumDeclaration("x"), "i")))
            Assert.Equal("e", Generator.GetName(Generator.WithName(Generator.EnumDeclaration("x"), "e")))
            Assert.Equal("d", Generator.GetName(Generator.WithName(Generator.DelegateDeclaration("x"), "d")))
            Assert.Equal("m", Generator.GetName(Generator.WithName(Generator.MethodDeclaration("x"), "m")))
            Assert.Equal("m", Generator.GetName(Generator.WithName(Generator.MethodDeclaration("x", modifiers:=DeclarationModifiers.Abstract), "m")))
            Assert.Equal("", Generator.GetName(Generator.WithName(Generator.ConstructorDeclaration(), ".ctor")))
            Assert.Equal("p", Generator.GetName(Generator.WithName(Generator.ParameterDeclaration("x"), "p")))
            Assert.Equal("p", Generator.GetName(Generator.WithName(Generator.PropertyDeclaration("x", Generator.IdentifierName("t")), "p")))
            Assert.Equal("p", Generator.GetName(Generator.WithName(Generator.PropertyDeclaration("x", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract), "p")))
            Assert.Equal("X", Generator.GetName(Generator.WithName(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t")), "X")))
            Assert.Equal("X", Generator.GetName(Generator.WithName(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract), "X")))
            Assert.Equal("f", Generator.GetName(Generator.WithName(Generator.FieldDeclaration("x", Generator.IdentifierName("t")), "f")))
            Assert.Equal("v", Generator.GetName(Generator.WithName(Generator.EnumMember("x"), "v")))
            Assert.Equal("ef", Generator.GetName(Generator.WithName(Generator.EventDeclaration("x", Generator.IdentifierName("t")), "ef")))
            Assert.Equal("ep", Generator.GetName(Generator.WithName(Generator.CustomEventDeclaration("x", Generator.IdentifierName("t")), "ep")))
            Assert.Equal("n", Generator.GetName(Generator.WithName(Generator.NamespaceDeclaration("x"), "n")))
            Assert.Equal("u", Generator.GetName(Generator.WithName(Generator.NamespaceImportDeclaration("x"), "u")))
            Assert.Equal("loc", Generator.GetName(Generator.WithName(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "x"), "loc")))
            Assert.Equal("a", Generator.GetName(Generator.WithName(Generator.Attribute("x"), "a")))
        End Sub

        <Fact>
        Public Sub TestGetAccessibility()
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.ClassDeclaration("c", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.StructDeclaration("s", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.InterfaceDeclaration("i", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.EnumDeclaration("e", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.DelegateDeclaration("d", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.MethodDeclaration("m", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.ConstructorDeclaration(accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.ParameterDeclaration("p")))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.FieldDeclaration("f", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.EnumMember("v")))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.EventDeclaration("ef", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.NamespaceDeclaration("n")))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.NamespaceImportDeclaration("u")))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc")))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.Attribute("a")))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(SyntaxFactory.TypeParameter("tp")))

            Dim m = SyntaxFactory.ModuleBlock(
                SyntaxFactory.ModuleStatement("module2").
                              WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))))
            Assert.Equal(Accessibility.Public, Generator.GetAccessibility(m))
        End Sub

        <Fact>
        Public Sub TestWithAccessibility()
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.ClassDeclaration("c", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.StructDeclaration("s", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.EnumDeclaration("i", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.EnumDeclaration("e", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.DelegateDeclaration("d", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.MethodDeclaration("m", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.ConstructorDeclaration(accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(Generator.ParameterDeclaration("p"), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.FieldDeclaration("f", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(Generator.EnumMember("v"), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.EventDeclaration("ef", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(Generator.WithAccessibility(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(Generator.NamespaceDeclaration("n"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(Generator.NamespaceImportDeclaration("u"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(Generator.Attribute("a"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, Generator.GetAccessibility(Generator.WithAccessibility(SyntaxFactory.TypeParameter("tp"), Accessibility.Private)))

            Dim m = SyntaxFactory.ModuleBlock(
                SyntaxFactory.ModuleStatement("module2").
                              WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))))
            Assert.Equal(Accessibility.Internal, Generator.GetAccessibility(Generator.WithAccessibility(m, Accessibility.Internal)))
        End Sub

        <Fact>
        Public Sub TestGetModifiers()
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(Generator.ClassDeclaration("c", modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Partial, Generator.GetModifiers(Generator.StructDeclaration("s", modifiers:=DeclarationModifiers.Partial)))
            Assert.Equal(DeclarationModifiers.[New], Generator.GetModifiers(Generator.EnumDeclaration("e", modifiers:=DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.[New], Generator.GetModifiers(Generator.DelegateDeclaration("d", modifiers:=DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.ConstructorDeclaration(modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.ParameterDeclaration("p")))
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Const, Generator.GetModifiers(Generator.FieldDeclaration("f", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Const)))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.EventDeclaration("ef", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("t"), modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.EnumMember("v")))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.NamespaceDeclaration("n")))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.NamespaceImportDeclaration("u")))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc")))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.Attribute("a")))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(SyntaxFactory.TypeParameter("tp")))
        End Sub

        <Fact>
        Public Sub TestWithModifiers()
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(Generator.WithModifiers(Generator.ClassDeclaration("c"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Partial, Generator.GetModifiers(Generator.WithModifiers(Generator.StructDeclaration("s"), DeclarationModifiers.Partial)))
            Assert.Equal(DeclarationModifiers.[New], Generator.GetModifiers(Generator.WithModifiers(Generator.EnumDeclaration("e"), DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.[New], Generator.GetModifiers(Generator.WithModifiers(Generator.DelegateDeclaration("d"), DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.WithModifiers(Generator.MethodDeclaration("m"), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.WithModifiers(Generator.ConstructorDeclaration(), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(Generator.ParameterDeclaration("p"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(Generator.WithModifiers(Generator.PropertyDeclaration("p", Generator.IdentifierName("t")), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Abstract, Generator.GetModifiers(Generator.WithModifiers(Generator.IndexerDeclaration({Generator.ParameterDeclaration("i")}, Generator.IdentifierName("t")), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Const, Generator.GetModifiers(Generator.WithModifiers(Generator.FieldDeclaration("f", Generator.IdentifierName("t")), DeclarationModifiers.Const)))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.WithModifiers(Generator.EventDeclaration("ef", Generator.IdentifierName("t")), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(Generator.WithModifiers(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("t")), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(Generator.EnumMember("v"), DeclarationModifiers.Partial)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(Generator.NamespaceDeclaration("n"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(Generator.NamespaceImportDeclaration("u"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(Generator.Attribute("a"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, Generator.GetModifiers(Generator.WithModifiers(SyntaxFactory.TypeParameter("tp"), DeclarationModifiers.Abstract)))
        End Sub

        <Fact>
        Public Sub TestWithModifiers_Sealed_Class()
            Dim classBlock = DirectCast(Generator.ClassDeclaration("C"), ClassBlockSyntax)
            Dim classBlockWithModifiers = Generator.WithModifiers(classBlock, DeclarationModifiers.Sealed)
            VerifySyntax(Of ClassBlockSyntax)(classBlockWithModifiers, "NotInheritable Class C
End Class")

            Dim classStatement = classBlock.ClassStatement
            Dim classStatementWithModifiers = Generator.WithModifiers(classStatement, DeclarationModifiers.Sealed)
            VerifySyntax(Of ClassStatementSyntax)(classStatementWithModifiers, "NotInheritable Class C")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23410")>
        Public Sub TestWithModifiers_Sealed_Member()
            Dim classBlock = DirectCast(Generator.ClassDeclaration("C"), ClassBlockSyntax)
            classBlock = DirectCast(Generator.AddMembers(classBlock, Generator.WithModifiers(Generator.MethodDeclaration("Goo"), DeclarationModifiers.Sealed)), ClassBlockSyntax)
            VerifySyntax(Of ClassBlockSyntax)(classBlock, "Class C

    NotOverridable Sub Goo()
    End Sub
End Class")
        End Sub

        <Fact>
        Public Sub TestGetType()
            Assert.Equal("t", Generator.GetType(Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("t"))).ToString())
            Assert.Null(Generator.GetType(Generator.MethodDeclaration("m")))

            Assert.Equal("t", Generator.GetType(Generator.FieldDeclaration("f", Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("pt"))}, Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))).ToString())

            Assert.Equal("t", Generator.GetType(Generator.EventDeclaration("ef", Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("t"))).ToString())

            Assert.Equal("t", Generator.GetType(Generator.DelegateDeclaration("t", returnType:=Generator.IdentifierName("t"))).ToString())
            Assert.Null(Generator.GetType(Generator.DelegateDeclaration("d")))

            Assert.Equal("t", Generator.GetType(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "v")).ToString())

            Assert.Null(Generator.GetType(Generator.ClassDeclaration("c")))
            Assert.Null(Generator.GetType(Generator.IdentifierName("x")))
        End Sub

        <Fact>
        Public Sub TestWithType()
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.MethodDeclaration("m"), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.FieldDeclaration("f", Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.PropertyDeclaration("p", Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("pt"))}, Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.ParameterDeclaration("p", Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())

            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.DelegateDeclaration("t", returnType:=Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.DelegateDeclaration("t"), Generator.IdentifierName("t"))).ToString())

            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.EventDeclaration("ef", Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())
            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.CustomEventDeclaration("ep", Generator.IdentifierName("x")), Generator.IdentifierName("t"))).ToString())

            Assert.Equal("t", Generator.GetType(Generator.WithType(Generator.LocalDeclarationStatement(Generator.IdentifierName("x"), "v"), Generator.IdentifierName("t"))).ToString())
            Assert.Null(Generator.GetType(Generator.WithType(Generator.ClassDeclaration("c"), Generator.IdentifierName("t"))))
            Assert.Null(Generator.GetType(Generator.WithType(Generator.IdentifierName("x"), Generator.IdentifierName("t"))))
        End Sub

        <Fact>
        Public Sub TestWithTypeChangesSubFunction()
            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithType(Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x")), Nothing),
"Sub m()
End Sub")

            VerifySyntax(Of MethodBlockSyntax)(
                Generator.WithType(Generator.MethodDeclaration("m"), Generator.IdentifierName("x")),
"Function m() As x
End Function")

            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithType(Generator.MethodDeclaration("m", returnType:=Generator.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract), Nothing),
"MustOverride Sub m()")

            VerifySyntax(Of MethodStatementSyntax)(
                Generator.WithType(Generator.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), Generator.IdentifierName("x")),
"MustOverride Function m() As x")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.WithType(Generator.DelegateDeclaration("d", returnType:=Generator.IdentifierName("x")), Nothing),
"Delegate Sub d()")

            VerifySyntax(Of DelegateStatementSyntax)(
                Generator.WithType(Generator.DelegateDeclaration("d"), Generator.IdentifierName("x")),
"Delegate Function d() As x")

        End Sub

        <Fact>
        Public Sub TestGetParameters()
            Assert.Equal(0, Generator.GetParameters(Generator.MethodDeclaration("m")).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.MethodDeclaration("m", parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
            Assert.Equal(2, Generator.GetParameters(Generator.MethodDeclaration("m", parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t")), Generator.ParameterDeclaration("p2", Generator.IdentifierName("t2"))})).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.ConstructorDeclaration()).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.ConstructorDeclaration(parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
            Assert.Equal(2, Generator.GetParameters(Generator.ConstructorDeclaration(parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t")), Generator.ParameterDeclaration("p2", Generator.IdentifierName("t2"))})).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"))).Count)

            Assert.Equal(1, Generator.GetParameters(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}, Generator.IdentifierName("t"))).Count)
            Assert.Equal(2, Generator.GetParameters(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("t")), Generator.ParameterDeclaration("p2", Generator.IdentifierName("t2"))}, Generator.IdentifierName("t"))).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("expr"))).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.ValueReturningLambdaExpression("p1", Generator.IdentifierName("expr"))).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("expr"))).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.VoidReturningLambdaExpression("p1", Generator.IdentifierName("expr"))).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.DelegateDeclaration("d")).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.DelegateDeclaration("d", parameters:={Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.ClassDeclaration("c")).Count)
            Assert.Equal(0, Generator.GetParameters(Generator.IdentifierName("x")).Count)
        End Sub

        <Fact>
        Public Sub TestAddParameters()
            Assert.Equal(1, Generator.GetParameters(Generator.AddParameters(Generator.MethodDeclaration("m"), {Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.AddParameters(Generator.ConstructorDeclaration(), {Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
            Assert.Equal(3, Generator.GetParameters(Generator.AddParameters(Generator.IndexerDeclaration({Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))}, Generator.IdentifierName("t")), {Generator.ParameterDeclaration("p2", Generator.IdentifierName("t2")), Generator.ParameterDeclaration("p3", Generator.IdentifierName("t3"))})).Count)

            Assert.Equal(1, Generator.GetParameters(Generator.AddParameters(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("expr")), {Generator.LambdaParameter("p")})).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.AddParameters(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("expr")), {Generator.LambdaParameter("p")})).Count)

            Assert.Equal(1, Generator.GetParameters(Generator.AddParameters(Generator.DelegateDeclaration("d"), {Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)

            Assert.Equal(0, Generator.GetParameters(Generator.AddParameters(Generator.ClassDeclaration("c"), {Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
            Assert.Equal(0, Generator.GetParameters(Generator.AddParameters(Generator.IdentifierName("x"), {Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
            Assert.Equal(1, Generator.GetParameters(Generator.AddParameters(Generator.PropertyDeclaration("p", Generator.IdentifierName("t")), {Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))})).Count)
        End Sub

        <Fact>
        Public Sub TestGetExpression()
            ' initializers
            Assert.Equal("x", Generator.GetExpression(Generator.FieldDeclaration("f", Generator.IdentifierName("t"), initializer:=Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.ParameterDeclaration("p", Generator.IdentifierName("t"), initializer:=Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.LocalDeclarationStatement("loc", initializer:=Generator.IdentifierName("x"))).ToString())

            ' lambda bodies
            Assert.Null(Generator.GetExpression(Generator.ValueReturningLambdaExpression("p", {Generator.IdentifierName("x")})))
            Assert.Equal(1, Generator.GetStatements(Generator.ValueReturningLambdaExpression("p", {Generator.IdentifierName("x")})).Count)
            Assert.Equal("x", Generator.GetExpression(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.ValueReturningLambdaExpression("p", Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.VoidReturningLambdaExpression("p", Generator.IdentifierName("x"))).ToString())

            Assert.Null(Generator.GetExpression(Generator.IdentifierName("e")))
        End Sub

        <Fact>
        Public Sub TestWithExpression()
            ' initializers
            Assert.Equal("x", Generator.GetExpression(Generator.WithExpression(Generator.FieldDeclaration("f", Generator.IdentifierName("t")), Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.WithExpression(Generator.ParameterDeclaration("p", Generator.IdentifierName("t")), Generator.IdentifierName("x"))).ToString())
            Assert.Equal("x", Generator.GetExpression(Generator.WithExpression(Generator.LocalDeclarationStatement(Generator.IdentifierName("t"), "loc"), Generator.IdentifierName("x"))).ToString())

            ' lambda bodies
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.ValueReturningLambdaExpression("p", {Generator.IdentifierName("x")}), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.VoidReturningLambdaExpression("p", {Generator.IdentifierName("x")}), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.ValueReturningLambdaExpression({Generator.IdentifierName("x")}), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.VoidReturningLambdaExpression({Generator.IdentifierName("x")}), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.ValueReturningLambdaExpression("p", Generator.IdentifierName("x")), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.VoidReturningLambdaExpression("p", Generator.IdentifierName("x")), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("x")), Generator.IdentifierName("y"))).ToString())
            Assert.Equal("y", Generator.GetExpression(Generator.WithExpression(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("x")), Generator.IdentifierName("y"))).ToString())

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.ValueReturningLambdaExpression({Generator.IdentifierName("s")}), Generator.IdentifierName("e")),
                "Function() e")

            Assert.Null(Generator.GetExpression(Generator.WithExpression(Generator.IdentifierName("e"), Generator.IdentifierName("x"))))
        End Sub

        <Fact>
        Public Sub TestWithExpression_LambdaChanges()
            ' multi line function changes to single line function
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.ValueReturningLambdaExpression({Generator.IdentifierName("s")}), Generator.IdentifierName("e")),
                "Function() e")

            ' multi line sub changes to single line sub
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.VoidReturningLambdaExpression({Generator.IdentifierName("s")}), Generator.IdentifierName("e")),
                "Sub() e")

            ' single line function changes to multi-line function with null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("e")), Nothing),
"Function()
End Function")

            ' single line sub changes to multi line sub with null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("e")), Nothing),
"Sub()
End Sub")

            ' multi line function no-op when assigned null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.ValueReturningLambdaExpression({Generator.IdentifierName("s")}), Nothing),
"Function()
    s
End Function")

            ' multi line sub no-op when assigned null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithExpression(Generator.VoidReturningLambdaExpression({Generator.IdentifierName("s")}), Nothing),
"Sub()
    s
End Sub")

            Assert.Null(Generator.GetExpression(Generator.WithExpression(Generator.IdentifierName("e"), Generator.IdentifierName("x"))))
        End Sub

        <Fact>
        Public Sub TestGetStatements()
            Dim stmts = {Generator.ExpressionStatement(Generator.AssignmentStatement(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), Generator.ExpressionStatement(Generator.InvocationExpression(Generator.IdentifierName("fn"), Generator.IdentifierName("arg")))}

            Assert.Equal(0, Generator.GetStatements(Generator.MethodDeclaration("m")).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.MethodDeclaration("m", statements:=stmts)).Count)

            Assert.Equal(0, Generator.GetStatements(Generator.ConstructorDeclaration()).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.ConstructorDeclaration(statements:=stmts)).Count)

            Assert.Equal(0, Generator.GetStatements(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("e"))).Count)
            Assert.Equal(0, Generator.GetStatements(Generator.VoidReturningLambdaExpression({})).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.VoidReturningLambdaExpression(stmts)).Count)

            Assert.Equal(0, Generator.GetStatements(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("e"))).Count)
            Assert.Equal(0, Generator.GetStatements(Generator.ValueReturningLambdaExpression({})).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.ValueReturningLambdaExpression(stmts)).Count)

            Assert.Equal(0, Generator.GetStatements(Generator.IdentifierName("x")).Count)
        End Sub

        <Fact>
        Public Sub TestWithStatements()
            Dim stmts = {Generator.ExpressionStatement(Generator.AssignmentStatement(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), Generator.ExpressionStatement(Generator.InvocationExpression(Generator.IdentifierName("fn"), Generator.IdentifierName("arg")))}

            Assert.Equal(2, Generator.GetStatements(Generator.WithStatements(Generator.MethodDeclaration("m"), stmts)).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.WithStatements(Generator.ConstructorDeclaration(), stmts)).Count)

            Assert.Equal(2, Generator.GetStatements(Generator.WithStatements(Generator.VoidReturningLambdaExpression({}), stmts)).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.WithStatements(Generator.ValueReturningLambdaExpression({}), stmts)).Count)

            Assert.Equal(2, Generator.GetStatements(Generator.WithStatements(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("e")), stmts)).Count)
            Assert.Equal(2, Generator.GetStatements(Generator.WithStatements(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("e")), stmts)).Count)

            Assert.Equal(0, Generator.GetStatements(Generator.WithStatements(Generator.IdentifierName("x"), stmts)).Count)
        End Sub

        <Fact>
        Public Sub TestWithStatements_LambdaChanges()
            Dim stmts = {Generator.ExpressionStatement(Generator.IdentifierName("x")), Generator.ExpressionStatement(Generator.IdentifierName("y"))}

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.VoidReturningLambdaExpression({}), stmts),
"Sub()
    x
    y
End Sub")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.ValueReturningLambdaExpression({}), stmts),
"Function()
    x
    y
End Function")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("e")), stmts),
"Sub()
    x
    y
End Sub")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("e")), stmts),
"Function()
    x
    y
End Function")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.VoidReturningLambdaExpression(stmts), {}),
"Sub()
End Sub")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.ValueReturningLambdaExpression(stmts), {}),
"Function()
End Function")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.VoidReturningLambdaExpression(Generator.IdentifierName("e")), {}),
"Sub()
End Sub")

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                Generator.WithStatements(Generator.ValueReturningLambdaExpression(Generator.IdentifierName("e")), {}),
"Function()
End Function")
        End Sub

        <Fact>
        Public Sub TestAccessorDeclarations()
            Dim _g = Me.Generator
            Dim prop = _g.PropertyDeclaration("p", _g.IdentifierName("T"))

            Assert.Equal(2, _g.GetAccessors(prop).Count)

            ' get accessors from property
            Dim getAccessor = _g.GetAccessor(prop, DeclarationKind.GetAccessor)
            Assert.NotNull(getAccessor)
            VerifySyntax(Of AccessorBlockSyntax)(getAccessor,
"Get
End Get")

            Assert.NotNull(getAccessor)
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(getAccessor))

            ' get accessors from property
            Dim setAccessor = _g.GetAccessor(prop, DeclarationKind.SetAccessor)
            Assert.NotNull(setAccessor)
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(setAccessor))

            ' remove accessors
            Assert.Null(_g.GetAccessor(_g.RemoveNode(prop, getAccessor), DeclarationKind.GetAccessor))
            Assert.Null(_g.GetAccessor(_g.RemoveNode(prop, setAccessor), DeclarationKind.SetAccessor))

            ' change accessor accessibility
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.WithAccessibility(getAccessor, Accessibility.Public)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(setAccessor, Accessibility.Private)))

            ' change accessor statements
            Assert.Equal(0, _g.GetStatements(getAccessor).Count)
            Assert.Equal(0, _g.GetStatements(setAccessor).Count)

            Dim newGetAccessor = _g.WithStatements(getAccessor, Nothing)
            VerifySyntax(Of AccessorBlockSyntax)(newGetAccessor,
"Get
End Get")

            ' change accessors
            Dim newProp = _g.ReplaceNode(prop, getAccessor, _g.WithAccessibility(getAccessor, Accessibility.Public))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.GetAccessor(newProp, DeclarationKind.GetAccessor)))

            newProp = _g.ReplaceNode(prop, setAccessor, _g.WithAccessibility(setAccessor, Accessibility.Public))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.GetAccessor(newProp, DeclarationKind.SetAccessor)))
        End Sub

        <Fact>
        Public Sub TestGetAccessorStatements()
            Dim stmts = {Generator.ExpressionStatement(Generator.AssignmentStatement(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), Generator.ExpressionStatement(Generator.InvocationExpression(Generator.IdentifierName("fn"), Generator.IdentifierName("arg")))}

            Dim p = Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))

            ' get-accessor
            Assert.Equal(0, Generator.GetGetAccessorStatements(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"))).Count)
            Assert.Equal(2, Generator.GetGetAccessorStatements(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), getAccessorStatements:=stmts)).Count)

            Assert.Equal(0, Generator.GetGetAccessorStatements(Generator.IndexerDeclaration({p}, Generator.IdentifierName("t"))).Count)
            Assert.Equal(2, Generator.GetGetAccessorStatements(Generator.IndexerDeclaration({p}, Generator.IdentifierName("t"), getAccessorStatements:=stmts)).Count)

            Assert.Equal(0, Generator.GetGetAccessorStatements(Generator.IdentifierName("x")).Count)

            ' set-accessor
            Assert.Equal(0, Generator.GetSetAccessorStatements(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"))).Count)
            Assert.Equal(2, Generator.GetSetAccessorStatements(Generator.PropertyDeclaration("p", Generator.IdentifierName("t"), setAccessorStatements:=stmts)).Count)

            Assert.Equal(0, Generator.GetSetAccessorStatements(Generator.IndexerDeclaration({p}, Generator.IdentifierName("t"))).Count)
            Assert.Equal(2, Generator.GetSetAccessorStatements(Generator.IndexerDeclaration({p}, Generator.IdentifierName("t"), setAccessorStatements:=stmts)).Count)

            Assert.Equal(0, Generator.GetSetAccessorStatements(Generator.IdentifierName("x")).Count)
        End Sub

        <Fact>
        Public Sub TestWithAccessorStatements()
            Dim stmts = {Generator.ExpressionStatement(Generator.AssignmentStatement(Generator.IdentifierName("x"), Generator.IdentifierName("y"))), Generator.ExpressionStatement(Generator.InvocationExpression(Generator.IdentifierName("fn"), Generator.IdentifierName("arg")))}

            Dim p = Generator.ParameterDeclaration("p", Generator.IdentifierName("t"))

            ' get-accessor
            Assert.Equal(2, Generator.GetGetAccessorStatements(Generator.WithGetAccessorStatements(Generator.PropertyDeclaration("p", Generator.IdentifierName("t")), stmts)).Count)
            Assert.Equal(2, Generator.GetGetAccessorStatements(Generator.WithGetAccessorStatements(Generator.IndexerDeclaration({p}, Generator.IdentifierName("t")), stmts)).Count)
            Assert.Equal(0, Generator.GetGetAccessorStatements(Generator.WithGetAccessorStatements(Generator.IdentifierName("x"), stmts)).Count)

            ' set-accessor
            Assert.Equal(2, Generator.GetSetAccessorStatements(Generator.WithSetAccessorStatements(Generator.PropertyDeclaration("p", Generator.IdentifierName("t")), stmts)).Count)
            Assert.Equal(2, Generator.GetSetAccessorStatements(Generator.WithSetAccessorStatements(Generator.IndexerDeclaration({p}, Generator.IdentifierName("t")), stmts)).Count)
            Assert.Equal(0, Generator.GetSetAccessorStatements(Generator.WithSetAccessorStatements(Generator.IdentifierName("x"), stmts)).Count)
        End Sub

        Private Sub AssertNamesEqual(expectedNames As String(), actualNodes As IReadOnlyList(Of SyntaxNode))
            Dim actualNames = actualNodes.Select(Function(n) Generator.GetName(n)).ToArray()
            Assert.Equal(expectedNames.Length, actualNames.Length)
            Dim expected = String.Join(", ", expectedNames)
            Dim actual = String.Join(", ", actualNames)
            Assert.Equal(expected, actual)
        End Sub

        Private Sub AssertMemberNamesEqual(expectedNames As String(), declaration As SyntaxNode)
            AssertNamesEqual(expectedNames, Generator.GetMembers(declaration))
        End Sub

        Private Sub AssertMemberNamesEqual(expectedName As String, declaration As SyntaxNode)
            AssertMemberNamesEqual({expectedName}, declaration)
        End Sub

        <Fact>
        Public Sub TestGetMembers()
            AssertMemberNamesEqual("m", Generator.ClassDeclaration("c", members:={Generator.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", Generator.StructDeclaration("s", members:={Generator.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", Generator.InterfaceDeclaration("i", members:={Generator.MethodDeclaration("m")}))
            AssertMemberNamesEqual("v", Generator.EnumDeclaration("e", members:={Generator.EnumMember("v")}))
            AssertMemberNamesEqual("c", Generator.NamespaceDeclaration("n", declarations:={Generator.ClassDeclaration("c")}))
            AssertMemberNamesEqual("c", Generator.CompilationUnit(declarations:={Generator.ClassDeclaration("c")}))
        End Sub

        <Fact>
        Public Sub TestAddMembers()
            AssertMemberNamesEqual("m", Generator.AddMembers(Generator.ClassDeclaration("d"), {Generator.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", Generator.AddMembers(Generator.StructDeclaration("s"), {Generator.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", Generator.AddMembers(Generator.InterfaceDeclaration("i"), {Generator.MethodDeclaration("m")}))
            AssertMemberNamesEqual("v", Generator.AddMembers(Generator.EnumDeclaration("e"), {Generator.EnumMember("v")}))
            AssertMemberNamesEqual("n2", Generator.AddMembers(Generator.NamespaceDeclaration("n"), {Generator.NamespaceDeclaration("n2")}))
            AssertMemberNamesEqual("n", Generator.AddMembers(Generator.CompilationUnit(), {Generator.NamespaceDeclaration("n")}))

            AssertMemberNamesEqual({"m", "m2"}, Generator.AddMembers(Generator.ClassDeclaration("d", members:={Generator.MethodDeclaration("m")}), {Generator.MethodDeclaration("m2")}))
            AssertMemberNamesEqual({"m", "m2"}, Generator.AddMembers(Generator.StructDeclaration("s", members:={Generator.MethodDeclaration("m")}), {Generator.MethodDeclaration("m2")}))
            AssertMemberNamesEqual({"m", "m2"}, Generator.AddMembers(Generator.InterfaceDeclaration("i", members:={Generator.MethodDeclaration("m")}), {Generator.MethodDeclaration("m2")}))
            AssertMemberNamesEqual({"v", "v2"}, Generator.AddMembers(Generator.EnumDeclaration("i", members:={Generator.EnumMember("v")}), {Generator.EnumMember("v2")}))
            AssertMemberNamesEqual({"n1", "n2"}, Generator.AddMembers(Generator.NamespaceDeclaration("n", {Generator.NamespaceDeclaration("n1")}), {Generator.NamespaceDeclaration("n2")}))
            AssertMemberNamesEqual({"n1", "n2"}, Generator.AddMembers(Generator.CompilationUnit(declarations:={Generator.NamespaceDeclaration("n1")}), {Generator.NamespaceDeclaration("n2")}))
        End Sub

        <Fact>
        Public Sub TestRemoveMembers()
            TestRemoveAllMembers(Generator.ClassDeclaration("d", members:={Generator.MethodDeclaration("m")}))
            TestRemoveAllMembers(Generator.StructDeclaration("s", members:={Generator.MethodDeclaration("m")}))
            TestRemoveAllMembers(Generator.InterfaceDeclaration("i", members:={Generator.MethodDeclaration("m")}))
            TestRemoveAllMembers(Generator.EnumDeclaration("i", members:={Generator.EnumMember("v")}))
            TestRemoveAllMembers(Generator.AddMembers(Generator.NamespaceDeclaration("n", {Generator.NamespaceDeclaration("n1")})))
            TestRemoveAllMembers(Generator.AddMembers(Generator.CompilationUnit(declarations:={Generator.NamespaceDeclaration("n1")})))
        End Sub

        Private Sub TestRemoveAllMembers(declaration As SyntaxNode)
            Assert.Equal(0, Generator.GetMembers(Generator.RemoveNodes(declaration, Generator.GetMembers(declaration))).Count)
        End Sub

        <Fact>
        Public Sub TestGetBaseAndInterfaceTypes()
            Dim classBI = SyntaxFactory.ParseCompilationUnit(
"Class C
    Inherits B
    Implements I
End Class").Members(0)

            Dim baseListBI = Generator.GetBaseAndInterfaceTypes(classBI)
            Assert.NotNull(baseListBI)
            Assert.Equal(2, baseListBI.Count)
            Assert.Equal("B", baseListBI(0).ToString())
            Assert.Equal("I", baseListBI(1).ToString())

            Dim ifaceI = SyntaxFactory.ParseCompilationUnit(
"Interface I
    Inherits X
    Inherits Y
End Class").Members(0)

            Dim baseListXY = Generator.GetBaseAndInterfaceTypes(ifaceI)
            Assert.NotNull(baseListXY)
            Assert.Equal(2, baseListXY.Count)
            Assert.Equal("X", baseListXY(0).ToString())
            Assert.Equal("Y", baseListXY(1).ToString())

            Dim classN = SyntaxFactory.ParseCompilationUnit(
"Class C
End Class").Members(0)

            Dim baseListN = Generator.GetBaseAndInterfaceTypes(classN)
            Assert.NotNull(baseListN)
            Assert.Equal(0, baseListN.Count)
        End Sub

        <Fact>
        Public Sub TestRemoveBaseAndInterfaceTypes()
            Dim classC = SyntaxFactory.ParseCompilationUnit(
"Class C
    Inherits A
    Implements X, Y

End Class").Members(0)

            Dim baseList = Generator.GetBaseAndInterfaceTypes(classC)
            Assert.Equal(3, baseList.Count)

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(classC, baseList(0)),
"Class C
    Implements X, Y

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(classC, baseList(1)),
"Class C
    Inherits A
    Implements Y

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(classC, baseList(2)),
"Class C
    Inherits A
    Implements X

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(classC, {baseList(1), baseList(2)}),
"Class C
    Inherits A

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(classC, baseList),
"Class C
End Class")

        End Sub

        <Fact>
        Public Sub TestAddBaseType()
            Dim classC = SyntaxFactory.ParseCompilationUnit(
"Class C
End Class").Members(0)

            Dim classCB = SyntaxFactory.ParseCompilationUnit(
"Class C
    Inherits B

End Class").Members(0)

            Dim structS = SyntaxFactory.ParseCompilationUnit(
"Structure S
End Structure").Members(0)

            Dim ifaceI = SyntaxFactory.ParseCompilationUnit(
"Interface I
End Interface").Members(0)

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.AddBaseType(classC, Generator.IdentifierName("T")),
"Class C
    Inherits T

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.AddBaseType(classCB, Generator.IdentifierName("T")),
"Class C
    Inherits T

End Class")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.AddBaseType(structS, Generator.IdentifierName("T")),
"Structure S
End Structure")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.AddBaseType(ifaceI, Generator.IdentifierName("T")),
"Interface I
End Interface")

        End Sub

        <Fact>
        Public Sub TestAddInterfaceType()
            Dim classC = SyntaxFactory.ParseCompilationUnit(
"Class C
End Class").Members(0)

            Dim classCB = SyntaxFactory.ParseCompilationUnit(
"Class C
    Inherits B

End Class").Members(0)

            Dim classCI = SyntaxFactory.ParseCompilationUnit(
"Class C
    Implements I

End Class").Members(0)

            Dim structS = SyntaxFactory.ParseCompilationUnit(
"Structure S
End Structure").Members(0)

            Dim ifaceI = SyntaxFactory.ParseCompilationUnit(
"Interface I
End Interface").Members(0)

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.AddInterfaceType(classC, Generator.IdentifierName("T")),
"Class C
    Implements T

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.AddInterfaceType(classCB, Generator.IdentifierName("T")),
"Class C
    Inherits B
    Implements T

End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.AddInterfaceType(classCI, Generator.IdentifierName("T")),
"Class C
    Implements I, T

End Class")

            VerifySyntax(Of StructureBlockSyntax)(
                Generator.AddInterfaceType(structS, Generator.IdentifierName("T")),
"Structure S
    Implements T

End Structure")

            VerifySyntax(Of InterfaceBlockSyntax)(
                Generator.AddInterfaceType(ifaceI, Generator.IdentifierName("T")),
"Interface I
    Inherits T

End Interface")

        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5097")>
        Public Sub TestAddInterfaceWithEOLs()
            Dim classC = SyntaxFactory.ParseCompilationUnit("
Public Class C
End Class").Members(0)

            VerifySyntaxRaw(Of ClassBlockSyntax)(
                Generator.AddInterfaceType(classC, Generator.IdentifierName("X")), "
Public Class C
ImplementsXEnd Class")

            Dim interfaceI = SyntaxFactory.ParseCompilationUnit("
Public Interface I
End Interface").Members(0)

            VerifySyntaxRaw(Of InterfaceBlockSyntax)(
                Generator.AddInterfaceType(interfaceI, Generator.IdentifierName("X")), "
Public Interface I
InheritsXEnd Interface")

            Dim classCX = SyntaxFactory.ParseCompilationUnit("
Public Class C
    Implements X
End Class").Members(0)

            VerifySyntaxRaw(Of ClassBlockSyntax)(
                Generator.AddInterfaceType(classCX, Generator.IdentifierName("Y")), "
Public Class C
    Implements X,Y
End Class")

            Dim interfaceIX = SyntaxFactory.ParseCompilationUnit("
Public Interface I
    Inherits X
End Interface").Members(0)

            VerifySyntaxRaw(Of InterfaceBlockSyntax)(
                Generator.AddInterfaceType(interfaceIX, Generator.IdentifierName("Y")), "
Public Interface I
    Inherits X,Y
End Interface")

            Dim classCXY = SyntaxFactory.ParseCompilationUnit("
Public Class C
    Implements X
    Implements Y
End Class").Members(0)

            VerifySyntaxRaw(Of ClassBlockSyntax)(
                Generator.AddInterfaceType(classCXY, Generator.IdentifierName("Z")), "
Public Class C
    Implements X
    Implements Y
ImplementsZEnd Class")

            Dim interfaceIXY = SyntaxFactory.ParseCompilationUnit("
Public Interface I
    Inherits X
    Inherits Y
End Interface").Members(0)

            VerifySyntaxRaw(Of InterfaceBlockSyntax)(
                Generator.AddInterfaceType(interfaceIXY, Generator.IdentifierName("Z")), "
Public Interface I
    Inherits X
    Inherits Y
InheritsZEnd Interface")

        End Sub

        <Fact>
        Public Sub TestMultiFieldMembers()
            Dim comp = Compile(
"' Comment
Public Class C
    Public Shared X, Y, Z As Integer
End Class")

            Dim symbolC = DirectCast(comp.GlobalNamespace.GetMembers("C").First(), INamedTypeSymbol)
            Dim symbolX = DirectCast(symbolC.GetMembers("X").First(), IFieldSymbol)
            Dim symbolY = DirectCast(symbolC.GetMembers("Y").First(), IFieldSymbol)
            Dim symbolZ = DirectCast(symbolC.GetMembers("Z").First(), IFieldSymbol)

            Dim declC = Generator.GetDeclaration(symbolC.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())
            Dim declX = Generator.GetDeclaration(symbolX.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())
            Dim declY = Generator.GetDeclaration(symbolY.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())
            Dim declZ = Generator.GetDeclaration(symbolZ.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())

            Assert.Equal(SyntaxKind.ModifiedIdentifier, declX.Kind)
            Assert.Equal(SyntaxKind.ModifiedIdentifier, declY.Kind)
            Assert.Equal(SyntaxKind.ModifiedIdentifier, declZ.Kind)

            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(declX))
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(declY))
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(declZ))

            Assert.NotNull(Generator.GetType(declX))
            Assert.Equal("Integer", Generator.GetType(declX).ToString())
            Assert.Equal("X", Generator.GetName(declX))
            Assert.Equal(Accessibility.Public, Generator.GetAccessibility(declX))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(declX))

            Assert.NotNull(Generator.GetType(declY))
            Assert.Equal("Integer", Generator.GetType(declY).ToString())
            Assert.Equal("Y", Generator.GetName(declY))
            Assert.Equal(Accessibility.Public, Generator.GetAccessibility(declY))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(declY))

            Assert.NotNull(Generator.GetType(declZ))
            Assert.Equal("Integer", Generator.GetType(declZ).ToString())
            Assert.Equal("Z", Generator.GetName(declZ))
            Assert.Equal(Accessibility.Public, Generator.GetAccessibility(declZ))
            Assert.Equal(DeclarationModifiers.Static, Generator.GetModifiers(declZ))

            Dim xTypedT = Generator.WithType(declX, Generator.IdentifierName("T"))
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(xTypedT))
            Assert.Equal(SyntaxKind.FieldDeclaration, xTypedT.Kind)
            Assert.Equal("T", Generator.GetType(xTypedT).ToString())

            Dim xNamedQ = Generator.WithName(declX, "Q")
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(xNamedQ))
            Assert.Equal(SyntaxKind.FieldDeclaration, xNamedQ.Kind)
            Assert.Equal("Q", Generator.GetName(xNamedQ).ToString())

            Dim xInitialized = Generator.WithExpression(declX, Generator.IdentifierName("e"))
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(xInitialized))
            Assert.Equal(SyntaxKind.FieldDeclaration, xInitialized.Kind)
            Assert.Equal("e", Generator.GetExpression(xInitialized).ToString())

            Dim xPrivate = Generator.WithAccessibility(declX, Accessibility.Private)
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(xPrivate))
            Assert.Equal(SyntaxKind.FieldDeclaration, xPrivate.Kind)
            Assert.Equal(Accessibility.Private, Generator.GetAccessibility(xPrivate))

            Dim xReadOnly = Generator.WithModifiers(declX, DeclarationModifiers.ReadOnly)
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(xReadOnly))
            Assert.Equal(SyntaxKind.FieldDeclaration, xReadOnly.Kind)
            Assert.Equal(DeclarationModifiers.ReadOnly, Generator.GetModifiers(xReadOnly))

            Dim xAttributed = Generator.AddAttributes(declX, Generator.Attribute("A"))
            Assert.Equal(DeclarationKind.Field, Generator.GetDeclarationKind(xAttributed))
            Assert.Equal(SyntaxKind.FieldDeclaration, xAttributed.Kind)
            Assert.Equal(1, Generator.GetAttributes(xAttributed).Count)
            Assert.Equal("<A>", Generator.GetAttributes(xAttributed)(0).ToString())

            Dim membersC = Generator.GetMembers(declC)
            Assert.Equal(3, membersC.Count)
            Assert.Equal(declX, membersC(0))
            Assert.Equal(declY, membersC(1))
            Assert.Equal(declZ, membersC(2))

            ' create new class from existing members, now appear as separate declarations
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ClassDeclaration("C", members:={declX, declY}),
"Class C

    Public Shared X As Integer

    Public Shared Y As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertMembers(declC, 0, Generator.FieldDeclaration("A", Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Dim A As T

    Public Shared X, Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertMembers(declC, 1, Generator.FieldDeclaration("A", Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Public Shared X As Integer

    Dim A As T

    Public Shared Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertMembers(declC, 2, Generator.FieldDeclaration("A", Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Public Shared X, Y As Integer

    Dim A As T

    Public Shared Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertMembers(declC, 3, Generator.FieldDeclaration("A", Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Public Shared X, Y, Z As Integer

    Dim A As T
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX, Generator.WithType(declX, Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Public Shared X As T

    Public Shared Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX, Generator.WithExpression(declX, Generator.IdentifierName("e"))),
"' Comment
Public Class C

    Public Shared X As Integer = e

    Public Shared Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX, Generator.WithName(declX, "Q")),
"' Comment
Public Class C

    Public Shared Q, Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX.GetAncestorOrThis(Of ModifiedIdentifierSyntax), SyntaxFactory.ModifiedIdentifier("Q")),
"' Comment
Public Class C

    Public Shared Q, Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declY, Generator.WithType(declY, Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Public Shared X As Integer

    Public Shared Y As T

    Public Shared Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declZ, Generator.WithType(declZ, Generator.IdentifierName("T"))),
"' Comment
Public Class C

    Public Shared X, Y As Integer

    Public Shared Z As T
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX, declZ),
"' Comment
Public Class C

    Public Shared Z, Y, Z As Integer
End Class")

            ' Removing
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(declC, declX),
"' Comment
Public Class C

    Public Shared Y, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(declC, declY),
"' Comment
Public Class C

    Public Shared X, Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(declC, declZ),
"' Comment
Public Class C

    Public Shared X, Y As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declX, declY}),
"' Comment
Public Class C

    Public Shared Z As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declX, declZ}),
"' Comment
Public Class C

    Public Shared Y As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declY, declZ}),
"' Comment
Public Class C

    Public Shared X As Integer
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declX, declY, declZ}),
"' Comment
Public Class C
End Class")

        End Sub

        <Fact>
        Public Sub TestMultiAttributes()
            Dim comp = Compile(
"' Comment
<X, Y, Z>
Public Class C
End Class")

            Dim symbolC = DirectCast(comp.GlobalNamespace.GetMembers("C").First(), INamedTypeSymbol)
            Dim declC = Generator.GetDeclaration(symbolC.DeclaringSyntaxReferences.First().GetSyntax())

            Dim attrs = Generator.GetAttributes(declC)
            Assert.Equal(3, attrs.Count)
            Dim declX = attrs(0)
            Dim declY = attrs(1)
            Dim declZ = attrs(2)
            Assert.Equal(SyntaxKind.Attribute, declX.Kind)
            Assert.Equal(SyntaxKind.Attribute, declY.Kind)
            Assert.Equal(SyntaxKind.Attribute, declZ.Kind)
            Assert.Equal("X", Generator.GetName(declX))
            Assert.Equal("Y", Generator.GetName(declY))
            Assert.Equal("Z", Generator.GetName(declZ))

            Dim xNamedQ = Generator.WithName(declX, "Q")
            Assert.Equal(DeclarationKind.Attribute, Generator.GetDeclarationKind(xNamedQ))
            Assert.Equal(SyntaxKind.AttributeList, xNamedQ.Kind)
            Assert.Equal("<Q>", xNamedQ.ToString())

            Dim xWithArg = Generator.AddAttributeArguments(declX, {Generator.AttributeArgument(Generator.IdentifierName("e"))})
            Assert.Equal(DeclarationKind.Attribute, Generator.GetDeclarationKind(xWithArg))
            Assert.Equal(SyntaxKind.AttributeList, xWithArg.Kind)
            Assert.Equal("<X(e)>", xWithArg.ToString())

            ' inserting
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertAttributes(declC, 0, Generator.Attribute("A")),
"' Comment
<A>
<X, Y, Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertAttributes(declC, 1, Generator.Attribute("A")),
"' Comment
<X>
<A>
<Y, Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertAttributes(declC, 2, Generator.Attribute("A")),
"' Comment
<X, Y>
<A>
<Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.InsertAttributes(declC, 3, Generator.Attribute("A")),
"' Comment
<X, Y, Z>
<A>
Public Class C
End Class")

            ' replacing
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX, Generator.Attribute("A")),
"' Comment
<A, Y, Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.ReplaceNode(declC, declX, Generator.InsertAttributeArguments(declX, 0, {Generator.AttributeArgument(Generator.IdentifierName("e"))})),
"' Comment
<X(e), Y, Z>
Public Class C
End Class")

            ' removing
            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(declC, declX),
"' Comment
<Y, Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(declC, declY),
"' Comment
<X, Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNode(declC, declZ),
"' Comment
<X, Y>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declX, declY}),
"' Comment
<Z>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declX, declZ}),
"' Comment
<Y>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declY, declZ}),
"' Comment
<X>
Public Class C
End Class")

            VerifySyntax(Of ClassBlockSyntax)(
                Generator.RemoveNodes(declC, {declX, declY, declZ}),
"' Comment
Public Class C
End Class")
        End Sub

        <Fact>
        Public Sub TestMultiImports()
            Dim comp = Compile(
"' Comment
Imports X, Y, Z
")

            Dim declCU = comp.SyntaxTrees.First().GetRoot()

            Assert.Equal(SyntaxKind.CompilationUnit, declCU.Kind)
            Dim imps = Generator.GetNamespaceImports(declCU)
            Assert.Equal(3, imps.Count)
            Dim declX = imps(0)
            Dim declY = imps(1)
            Dim declZ = imps(2)

            Dim xRenamedQ = Generator.WithName(declX, "Q")
            Assert.Equal(DeclarationKind.NamespaceImport, Generator.GetDeclarationKind(xRenamedQ))
            Assert.Equal(SyntaxKind.ImportsStatement, xRenamedQ.Kind)
            Assert.Equal("Imports Q", xRenamedQ.ToString())

            ' inserting
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.InsertNamespaceImports(declCU, 0, Generator.NamespaceImportDeclaration("N")),
"' Comment
Imports N
Imports X, Y, Z
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.InsertNamespaceImports(declCU, 1, Generator.NamespaceImportDeclaration("N")),
"' Comment
Imports X
Imports N
Imports Y, Z
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.InsertNamespaceImports(declCU, 2, Generator.NamespaceImportDeclaration("N")),
"' Comment
Imports X, Y
Imports N
Imports Z
")

            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.InsertNamespaceImports(declCU, 3, Generator.NamespaceImportDeclaration("N")),
"' Comment
Imports X, Y, Z
Imports N
")

            ' Replacing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.ReplaceNode(declCU, declX, Generator.NamespaceImportDeclaration("N")),
"' Comment
Imports N, Y, Z
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNode(declCU, declX),
"' Comment
Imports Y, Z
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNode(declCU, declY),
"' Comment
Imports X, Z
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNode(declCU, declZ),
"' Comment
Imports X, Y
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNodes(declCU, {declX, declY}),
"' Comment
Imports Z
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNodes(declCU, {declX, declZ}),
"' Comment
Imports Y
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNodes(declCU, {declY, declZ}),
"' Comment
Imports X
")

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                Generator.RemoveNodes(declCU, {declX, declY, declZ}),
"' Comment
")

        End Sub
#End Region

    End Class
End Namespace
