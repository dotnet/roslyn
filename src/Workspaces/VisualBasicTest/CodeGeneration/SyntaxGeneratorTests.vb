' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Editting
    Public Class SyntaxGeneratorTests
        Private ReadOnly _g As SyntaxGenerator = SyntaxGenerator.GetGenerator(New AdhocWorkspace(), LanguageNames.VisualBasic)

        Private ReadOnly _emptyCompilation As VisualBasicCompilation = VisualBasicCompilation.Create("empty", references:={TestReferences.NetFx.v4_0_30319.mscorlib, TestReferences.NetFx.v4_0_30319.System})

        Private _ienumerableInt As INamedTypeSymbol

        Public Sub New()
            Me._ienumerableInt = _emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_emptyCompilation.GetSpecialType(SpecialType.System_Int32))
        End Sub

        Public Function Compile(code As String) As Compilation
            code = code.Replace(vbLf, vbCrLf)
            Return VisualBasicCompilation.Create("test").AddReferences(TestReferences.NetFx.v4_0_30319.mscorlib).AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code))
        End Function

        Public Function CompileRaw(code As String) As Compilation
            Return VisualBasicCompilation.Create("test").AddReferences(TestReferences.NetFx.v4_0_30319.mscorlib).AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code))
        End Function

        Private Sub VerifySyntax(Of TSyntax As SyntaxNode)(type As SyntaxNode, expectedText As String)
            Assert.IsAssignableFrom(GetType(TSyntax), type)
            Dim normalized = type.NormalizeWhitespace().ToFullString()
            Dim fixedExpectations = expectedText.Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
            Assert.Equal(fixedExpectations, normalized)
        End Sub

        Private Sub VerifySyntaxRaw(Of TSyntax As SyntaxNode)(type As SyntaxNode, expectedText As String)
            Assert.IsAssignableFrom(GetType(TSyntax), type)
            Dim text = type.ToFullString()
            Assert.Equal(expectedText, text)
        End Sub

        Private Function ParseCompilationUnit(text As String) As CompilationUnitSyntax
            Dim fixedText = text.Replace(vbLf, vbCrLf)
            Return SyntaxFactory.ParseCompilationUnit(fixedText)
        End Function

#Region "Expressions & Statements"
        <Fact>
        Public Sub TestLiteralExpressions()
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0), "0")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(1), "1")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(-1), "-1")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Integer.MinValue), "Global.System.Int32.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Integer.MaxValue), "Global.System.Int32.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0L), "0L")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(1L), "1L")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(-1L), "-1L")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Long.MinValue), "Global.System.Int64.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Long.MaxValue), "Global.System.Int64.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0UL), "0UL")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(1UL), "1UL")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(ULong.MinValue), "0UL")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(ULong.MaxValue), "Global.System.UInt64.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0.0F), "0F")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(1.0F), "1F")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(-1.0F), "-1F")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Single.MinValue), "Global.System.Single.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Single.MaxValue), "Global.System.Single.MaxValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Single.Epsilon), "Global.System.Single.Epsilon")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Single.NaN), "Global.System.Single.NaN")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Single.NegativeInfinity), "Global.System.Single.NegativeInfinity")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Single.PositiveInfinity), "Global.System.Single.PositiveInfinity")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0.0), "0R")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(1.0), "1R")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(-1.0), "-1R")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Double.MinValue), "Global.System.Double.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Double.MaxValue), "Global.System.Double.MaxValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Double.Epsilon), "Global.System.Double.Epsilon")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Double.NaN), "Global.System.Double.NaN")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Double.NegativeInfinity), "Global.System.Double.NegativeInfinity")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Double.PositiveInfinity), "Global.System.Double.PositiveInfinity")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0D), "0D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(0.00D), "0.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("1.00", CultureInfo.InvariantCulture)), "1.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("-1.00", CultureInfo.InvariantCulture)), "-1.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("1.0000000000", CultureInfo.InvariantCulture)), "1.0000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("0.000000", CultureInfo.InvariantCulture)), "0.000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("0.0000000", CultureInfo.InvariantCulture)), "0.0000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(1000000000D), "1000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(123456789.123456789D), "123456789.123456789D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("1E-28", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000001D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("0E-28", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("1E-29", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(Decimal.Parse("-1E-29", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Decimal.MinValue), "Global.System.Decimal.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.LiteralExpression(Decimal.MaxValue), "Global.System.Decimal.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression("c"c), """c""c")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression("str"), """str""")

            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(True), "True")
            VerifySyntax(Of LiteralExpressionSyntax)(_g.LiteralExpression(False), "False")
        End Sub

        <Fact>
        Public Sub TestAttributeData()
            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
End Class
", "<MyAttribute>")), "<Global.MyAttribute>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Object)
  End Sub
End Class
", "<MyAttribute(Nothing)>")), "<Global.MyAttribute(Nothing)>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Integer)
  End Sub
End Class
", "<MyAttribute(123)>")), "<Global.MyAttribute(123)>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Double)
  End Sub
End Class
", "<MyAttribute(12.3)>")), "<Global.MyAttribute(12.3)>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As String)
  End Sub
End Class
", "<MyAttribute(""value"")>")), "<Global.MyAttribute(""value"")>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
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

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(value As Type)
  End Sub
End Class
", "<MyAttribute(GetType(MyAttribute))>")), "<Global.MyAttribute(GetType(Global.MyAttribute))>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute
  Public Sub New(values as Integer())
  End Sub
End Class
", "<MyAttribute({1, 2, 3})>")), "<Global.MyAttribute({1, 2, 3})>")

            VerifySyntax(Of AttributeListSyntax)(_g.Attribute(GetAttributeData("
Imports System
Public Class MyAttribute
  Inherits Attribute 
  Public Property Value As Integer
End Class
", "<MyAttribute(Value := 123)>")), "<Global.MyAttribute(Value:=123)>")

        End Sub

        Private Function GetAttributeData(decl As String, use As String) As AttributeData
            Dim code = decl & vbCrLf & use & vbCrLf & "Public Class C " & vbCrLf & "End Class" & vbCrLf
            Dim compilation = CompileRaw(code)
            Dim typeC = DirectCast(compilation.GlobalNamespace.GetMembers("C").First, INamedTypeSymbol)
            Return typeC.GetAttributes().First()
        End Function

        <Fact>
        Public Sub TestNameExpressions()
            VerifySyntax(Of IdentifierNameSyntax)(_g.IdentifierName("x"), "x")
            VerifySyntax(Of QualifiedNameSyntax)(_g.QualifiedName(_g.IdentifierName("x"), _g.IdentifierName("y")), "x.y")
            VerifySyntax(Of QualifiedNameSyntax)(_g.DottedName("x.y"), "x.y")

            VerifySyntax(Of GenericNameSyntax)(_g.GenericName("x", _g.IdentifierName("y")), "x(Of y)")
            VerifySyntax(Of GenericNameSyntax)(_g.GenericName("x", _g.IdentifierName("y"), _g.IdentifierName("z")), "x(Of y, z)")

            ' convert identifier name into generic name
            VerifySyntax(Of GenericNameSyntax)(_g.WithTypeArguments(_g.IdentifierName("x"), _g.IdentifierName("y")), "x(Of y)")

            ' convert qualified name into qualified generic name
            VerifySyntax(Of QualifiedNameSyntax)(_g.WithTypeArguments(_g.DottedName("x.y"), _g.IdentifierName("z")), "x.y(Of z)")

            ' convert member access expression into generic member access expression
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.WithTypeArguments(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x.y(Of z)")

            ' convert existing generic name into a different generic name
            Dim gname = _g.WithTypeArguments(_g.IdentifierName("x"), _g.IdentifierName("y"))
            VerifySyntax(Of GenericNameSyntax)(gname, "x(Of y)")
            VerifySyntax(Of GenericNameSyntax)(_g.WithTypeArguments(gname, _g.IdentifierName("z")), "x(Of z)")
        End Sub

        <Fact>
        Public Sub TestTypeExpressions()
            ' these are all type syntax too
            VerifySyntax(Of TypeSyntax)(_g.IdentifierName("x"), "x")
            VerifySyntax(Of TypeSyntax)(_g.QualifiedName(_g.IdentifierName("x"), _g.IdentifierName("y")), "x.y")
            VerifySyntax(Of TypeSyntax)(_g.DottedName("x.y"), "x.y")
            VerifySyntax(Of TypeSyntax)(_g.GenericName("x", _g.IdentifierName("y")), "x(Of y)")
            VerifySyntax(Of TypeSyntax)(_g.GenericName("x", _g.IdentifierName("y"), _g.IdentifierName("z")), "x(Of y, z)")

            VerifySyntax(Of TypeSyntax)(_g.ArrayTypeExpression(_g.IdentifierName("x")), "x()")
            VerifySyntax(Of TypeSyntax)(_g.ArrayTypeExpression(_g.ArrayTypeExpression(_g.IdentifierName("x"))), "x()()")
            VerifySyntax(Of TypeSyntax)(_g.NullableTypeExpression(_g.IdentifierName("x")), "x?")
            VerifySyntax(Of TypeSyntax)(_g.NullableTypeExpression(_g.NullableTypeExpression(_g.IdentifierName("x"))), "x?")
        End Sub

        <Fact>
        Public Sub TestSpecialTypeExpression()
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Byte), "Byte")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_SByte), "SByte")

            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Int16), "Short")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_UInt16), "UShort")

            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Int32), "Integer")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_UInt32), "UInteger")

            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Int64), "Long")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_UInt64), "ULong")

            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Single), "Single")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Double), "Double")

            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Char), "Char")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_String), "String")

            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Object), "Object")
            VerifySyntax(Of TypeSyntax)(_g.TypeExpression(SpecialType.System_Decimal), "Decimal")
        End Sub

        <Fact>
        Public Sub TestSymbolTypeExpressions()
            Dim genericType = _emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            VerifySyntax(Of QualifiedNameSyntax)(_g.TypeExpression(genericType), "Global.System.Collections.Generic.IEnumerable(Of T)")

            Dim arrayType = _emptyCompilation.CreateArrayTypeSymbol(_emptyCompilation.GetSpecialType(SpecialType.System_Int32))
            VerifySyntax(Of ArrayTypeSyntax)(_g.TypeExpression(arrayType), "System.Int32()")
        End Sub

        <Fact>
        Public Sub TestMathAndLogicExpressions()
            VerifySyntax(Of UnaryExpressionSyntax)(_g.NegateExpression(_g.IdentifierName("x")), "-(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) + (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.SubtractExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) - (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.MultiplyExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) * (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.DivideExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) / (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.ModuloExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) Mod (y)")

            VerifySyntax(Of UnaryExpressionSyntax)(_g.BitwiseNotExpression(_g.IdentifierName("x")), "Not(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.BitwiseAndExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) And (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.BitwiseOrExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) Or (y)")

            VerifySyntax(Of UnaryExpressionSyntax)(_g.LogicalNotExpression(_g.IdentifierName("x")), "Not(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.LogicalAndExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) AndAlso (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.LogicalOrExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) OrElse (y)")
        End Sub

        <Fact>
        Public Sub TestEqualityAndInequalityExpressions()
            VerifySyntax(Of BinaryExpressionSyntax)(_g.ReferenceEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) Is (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.ValueEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) = (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(_g.ReferenceNotEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) IsNot (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.ValueNotEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) <> (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(_g.LessThanExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) < (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.LessThanOrEqualExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) <= (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(_g.GreaterThanExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) > (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(_g.GreaterThanOrEqualExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) >= (y)")
        End Sub

        <Fact>
        Public Sub TestConditionalExpressions()
            VerifySyntax(Of BinaryConditionalExpressionSyntax)(_g.CoalesceExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "If(x, y)")
            VerifySyntax(Of TernaryConditionalExpressionSyntax)(_g.ConditionalExpression(_g.IdentifierName("x"), _g.IdentifierName("y"), _g.IdentifierName("z")), "If(x, y, z)")
        End Sub

        <Fact>
        Public Sub TestMemberAccessExpressions()
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "x.y")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.IdentifierName("x"), "y"), "x.y")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x.y.z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x(y).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x(y).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "((x) + (y)).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(_g.MemberAccessExpression(_g.NegateExpression(_g.IdentifierName("x")), _g.IdentifierName("y")), "(-(x)).y")
        End Sub

        <Fact>
        Public Sub TestArrayCreationExpressions()
            VerifySyntax(Of ArrayCreationExpressionSyntax)(
                _g.ArrayCreationExpression(_g.IdentifierName("x"), _g.LiteralExpression(10)),
                "New x(10) {}")

            VerifySyntax(Of ArrayCreationExpressionSyntax)(
                _g.ArrayCreationExpression(_g.IdentifierName("x"), {_g.IdentifierName("y"), _g.IdentifierName("z")}),
                "New x() {y, z}")
        End Sub

        <Fact>
        Public Sub TestObjectCreationExpressions()
            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                _g.ObjectCreationExpression(_g.IdentifierName("x")),
                "New x()")

            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                _g.ObjectCreationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")),
                "New x(y)")

            Dim intType = _emptyCompilation.GetSpecialType(SpecialType.System_Int32)
            Dim listType = _emptyCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
            Dim listOfIntType = listType.Construct(intType)

            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                _g.ObjectCreationExpression(listOfIntType, _g.IdentifierName("y")),
                "New Global.System.Collections.Generic.List(Of System.Int32)(y)")
        End Sub

        <Fact>
        Public Sub TestElementAccessExpressions()
            VerifySyntax(Of InvocationExpressionSyntax)(
                _g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")),
                "x(y)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                _g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y"), _g.IdentifierName("z")),
                "x(y, z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                _g.ElementAccessExpression(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "x.y(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                _g.ElementAccessExpression(_g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "x(y)(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                _g.ElementAccessExpression(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "x(y)(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                _g.ElementAccessExpression(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "((x) + (y))(z)")
        End Sub

        <Fact>
        Public Sub TestCastAndConvertExpressions()
            VerifySyntax(Of DirectCastExpressionSyntax)(_g.CastExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "DirectCast(y, x)")
            VerifySyntax(Of CTypeExpressionSyntax)(_g.ConvertExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "CType(y, x)")
        End Sub

        <Fact>
        Public Sub TestIsAndAsExpressions()
            VerifySyntax(Of TypeOfExpressionSyntax)(_g.IsTypeExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "TypeOf(x) Is y")
            VerifySyntax(Of TryCastExpressionSyntax)(_g.TryCastExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "TryCast(x, y)")
            VerifySyntax(Of GetTypeExpressionSyntax)(_g.TypeOfExpression(_g.IdentifierName("x")), "GetType(x)")
        End Sub

        <Fact>
        Public Sub TestInvocationExpressions()
            ' without explicit arguments
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.IdentifierName("x")), "x()")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y"), _g.IdentifierName("z")), "x(y, z)")

            ' using explicit arguments
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.IdentifierName("x"), _g.Argument(_g.IdentifierName("y"))), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.IdentifierName("x"), _g.Argument(RefKind.Ref, _g.IdentifierName("y"))), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.IdentifierName("x"), _g.Argument(RefKind.Out, _g.IdentifierName("y"))), "x(y)")

            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "x.y()")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "x(y)()")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "x(y)()")
            VerifySyntax(Of InvocationExpressionSyntax)(_g.InvocationExpression(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "((x) + (y))()")
        End Sub

        <Fact>
        Public Sub TestAssignmentStatement()
            VerifySyntax(Of AssignmentStatementSyntax)(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y")), "x = y")
        End Sub

        <Fact>
        Public Sub TestExpressionStatement()
            VerifySyntax(Of ExpressionStatementSyntax)(_g.ExpressionStatement(_g.IdentifierName("x")), "x")
            VerifySyntax(Of ExpressionStatementSyntax)(_g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("x"))), "x()")
        End Sub

        <Fact>
        Public Sub TestLocalDeclarationStatements()
            VerifySyntax(Of LocalDeclarationStatementSyntax)(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y"), "Dim y As x")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y", _g.IdentifierName("z")), "Dim y As x = z")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(_g.LocalDeclarationStatement("y", _g.IdentifierName("z")), "Dim y = z")

            VerifySyntax(Of LocalDeclarationStatementSyntax)(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y", isConst:=True), "Const y As x")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y", _g.IdentifierName("z"), isConst:=True), "Const y As x = z")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(_g.LocalDeclarationStatement(DirectCast(Nothing, SyntaxNode), "y", _g.IdentifierName("z"), isConst:=True), "Const y = z")
        End Sub

        <Fact>
        Public Sub TestAwaitExpressions()
            VerifySyntax(Of AwaitExpressionSyntax)(_g.AwaitExpression(_g.IdentifierName("x")), "Await x")
        End Sub

        <Fact>
        Public Sub TestReturnStatements()
            VerifySyntax(Of ReturnStatementSyntax)(_g.ReturnStatement(), "Return")
            VerifySyntax(Of ReturnStatementSyntax)(_g.ReturnStatement(_g.IdentifierName("x")), "Return x")
        End Sub

        <Fact>
        Public Sub TestThrowStatements()
            VerifySyntax(Of ThrowStatementSyntax)(_g.ThrowStatement(), "Throw")
            VerifySyntax(Of ThrowStatementSyntax)(_g.ThrowStatement(_g.IdentifierName("x")), "Throw x")
        End Sub

        <Fact>
        Public Sub TestIfStatements()
            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"), New SyntaxNode() {}),
<x>If x Then
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"), Nothing),
<x>If x Then
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"), New SyntaxNode() {}, New SyntaxNode() {}),
<x>If x Then
Else
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"),
                    {_g.IdentifierName("y")}),
<x>If x Then
    y
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"),
                    {_g.IdentifierName("y")},
                    {_g.IdentifierName("z")}),
<x>If x Then
    y
Else
    z
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"),
                    {_g.IdentifierName("y")},
                    {_g.IfStatement(_g.IdentifierName("p"), {_g.IdentifierName("q")})}),
<x>If x Then
    y
ElseIf p Then
    q
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                _g.IfStatement(_g.IdentifierName("x"),
                    {_g.IdentifierName("y")},
                    _g.IfStatement(_g.IdentifierName("p"),
                        {_g.IdentifierName("q")},
                        {_g.IdentifierName("z")})),
<x>If x Then
    y
ElseIf p Then
    q
Else
    z
End If</x>.Value)

        End Sub

        <Fact>
        Public Sub TestSwitchStatements()
            Dim x = 10

            VerifySyntax(Of SelectBlockSyntax)(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        {_g.IdentifierName("z")})),
<x>Select x
    Case y
        z
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(
                        {_g.IdentifierName("y"), _g.IdentifierName("p"), _g.IdentifierName("q")},
                        {_g.IdentifierName("z")})),
<x>Select x
    Case y, p, q
        z
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        {_g.IdentifierName("z")}),
                    _g.SwitchSection(_g.IdentifierName("a"),
                        {_g.IdentifierName("b")})),
<x>Select x
    Case y
        z
    Case a
        b
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        {_g.IdentifierName("z")}),
                    _g.DefaultSwitchSection(
                        {_g.IdentifierName("b")})),
<x>Select x
    Case y
        z
    Case Else
        b
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        {_g.ExitSwitchStatement()})),
<x>Select x
    Case y
        Exit Select
End Select</x>.Value)
        End Sub

        <Fact>
        Public Sub TestUsingStatements()
            VerifySyntax(Of UsingBlockSyntax)(
                _g.UsingStatement(_g.IdentifierName("x"), {_g.IdentifierName("y")}),
<x>Using x
    y
End Using</x>.Value)

            VerifySyntax(Of UsingBlockSyntax)(
                _g.UsingStatement("x", _g.IdentifierName("y"), {_g.IdentifierName("z")}),
<x>Using x = y
    z
End Using</x>.Value)

            VerifySyntax(Of UsingBlockSyntax)(
                _g.UsingStatement(_g.IdentifierName("x"), "y", _g.IdentifierName("z"), {_g.IdentifierName("q")}),
<x>Using y As x = z
    q
End Using</x>.Value)
        End Sub

        <Fact>
        Public Sub TestTryCatchStatements()

            VerifySyntax(Of TryBlockSyntax)(
                _g.TryCatchStatement(
                    {_g.IdentifierName("x")},
                    _g.CatchClause(_g.IdentifierName("y"), "z",
                        {_g.IdentifierName("a")})),
<x>Try
    x
Catch z As y
    a
End Try</x>.Value)

            VerifySyntax(Of TryBlockSyntax)(
                _g.TryCatchStatement(
                    {_g.IdentifierName("s")},
                    _g.CatchClause(_g.IdentifierName("x"), "y",
                        {_g.IdentifierName("z")}),
                    _g.CatchClause(_g.IdentifierName("a"), "b",
                        {_g.IdentifierName("c")})),
<x>Try
    s
Catch y As x
    z
Catch b As a
    c
End Try</x>.Value)

            VerifySyntax(Of TryBlockSyntax)(
                _g.TryCatchStatement(
                    {_g.IdentifierName("s")},
                    {_g.CatchClause(_g.IdentifierName("x"), "y",
                        {_g.IdentifierName("z")})},
                    {_g.IdentifierName("a")}),
<x>Try
    s
Catch y As x
    z
Finally
    a
End Try</x>.Value)

            VerifySyntax(Of TryBlockSyntax)(
                _g.TryFinallyStatement(
                    {_g.IdentifierName("x")},
                    {_g.IdentifierName("a")}),
<x>Try
    x
Finally
    a
End Try</x>.Value)

        End Sub

        <Fact>
        Public Sub TestWhileStatements()
            VerifySyntax(Of WhileBlockSyntax)(
                _g.WhileStatement(_g.IdentifierName("x"), {_g.IdentifierName("y")}),
<x>While x
    y
End While</x>.Value)

            VerifySyntax(Of WhileBlockSyntax)(
                _g.WhileStatement(_g.IdentifierName("x"), Nothing),
<x>While x
End While</x>.Value)
        End Sub

        <Fact>
        Public Sub TestLambdaExpressions()
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression("x", _g.IdentifierName("y")),
                <x>Function(x) y</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression({_g.LambdaParameter("x"), _g.LambdaParameter("y")}, _g.IdentifierName("z")),
                <x>Function(x, y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression(New SyntaxNode() {}, _g.IdentifierName("y")),
                <x>Function() y</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression("x", _g.IdentifierName("y")),
                <x>Sub(x) y</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression({_g.LambdaParameter("x"), _g.LambdaParameter("y")}, _g.IdentifierName("z")),
                <x>Sub(x, y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression(New SyntaxNode() {}, _g.IdentifierName("y")),
                <x>Sub() y</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression("x", {_g.ReturnStatement(_g.IdentifierName("y"))}),
<x>Function(x)
    Return y
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression({_g.LambdaParameter("x"), _g.LambdaParameter("y")}, {_g.ReturnStatement(_g.IdentifierName("z"))}),
<x>Function(x, y)
    Return z
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression(New SyntaxNode() {}, {_g.ReturnStatement(_g.IdentifierName("y"))}),
<x>Function()
    Return y
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression("x", {_g.IdentifierName("y")}),
<x>Sub(x)
    y
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression({_g.LambdaParameter("x"), _g.LambdaParameter("y")}, {_g.IdentifierName("z")}),
<x>Sub(x, y)
    z
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression(New SyntaxNode() {}, {_g.IdentifierName("y")}),
<x>Sub()
    y
End Sub</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression({_g.LambdaParameter("x", _g.IdentifierName("y"))}, _g.IdentifierName("z")),
                <x>Function(x As y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.ValueReturningLambdaExpression({_g.LambdaParameter("x", _g.IdentifierName("y")), _g.LambdaParameter("a", _g.IdentifierName("b"))}, _g.IdentifierName("z")),
                <x>Function(x As y, a As b) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression({_g.LambdaParameter("x", _g.IdentifierName("y"))}, _g.IdentifierName("z")),
                <x>Sub(x As y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.VoidReturningLambdaExpression({_g.LambdaParameter("x", _g.IdentifierName("y")), _g.LambdaParameter("a", _g.IdentifierName("b"))}, _g.IdentifierName("z")),
                <x>Sub(x As y, a As b) z</x>.Value)
        End Sub
#End Region

#Region "Declarations"
        <Fact>
        Public Sub TestFieldDeclarations()
            VerifySyntax(Of FieldDeclarationSyntax)(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32)),
                <x>Dim fld As Integer</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32), initializer:=_g.LiteralExpression(0)),
                <x>Dim fld As Integer = 0</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32), accessibility:=Accessibility.Public),
                <x>Public fld As Integer</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32), modifiers:=DeclarationModifiers.Static Or DeclarationModifiers.ReadOnly),
                <x>Shared ReadOnly fld As Integer</x>.Value)
        End Sub

        <Fact>
        Public Sub TestMethodDeclarations()
            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m"),
<x>Sub m()
End Sub</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", typeParameters:={"x", "y"}),
<x>Sub m(Of x, y)()
End Sub</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", returnType:=_g.IdentifierName("x")),
<x>Function m() As x
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", returnType:=_g.IdentifierName("x"), statements:={_g.ReturnStatement(_g.IdentifierName("y"))}),
<x>Function m() As x
    Return y
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", parameters:={_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, returnType:=_g.IdentifierName("x")),
<x>Function m(z As y) As x
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", parameters:={_g.ParameterDeclaration("z", _g.IdentifierName("y"), _g.IdentifierName("a"))}, returnType:=_g.IdentifierName("x")),
<x>Function m(Optional z As y = a) As x
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", returnType:=_g.IdentifierName("x"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.None),
<x>Public Function m() As x
End Function</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                _g.MethodDeclaration("m", returnType:=_g.IdentifierName("x"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Abstract),
<x>Public MustInherit Function m() As x</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration("m", accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Partial),
<x>Private Partial Sub m()
End Sub</x>.Value)
        End Sub

        <Fact>
        Public Sub MethodDeclarationCanRoundTrip()
            Dim tree = VisualBasicSyntaxTree.ParseText(
<x>
Public Sub Test()
End Sub</x>.Value)
            Dim compilation = VisualBasicCompilation.Create("AssemblyName", syntaxTrees:={tree})
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().First()
            Dim symbol = CType(model.GetDeclaredSymbol(node), IMethodSymbol)
            VerifySyntax(Of MethodBlockSyntax)(
                _g.MethodDeclaration(symbol),
<x>Public Sub Test()
End Sub</x>.Value)
        End Sub

        <Fact>
        Public Sub TestPropertyDeclarations()
            VerifySyntax(Of PropertyStatementSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.ReadOnly),
<x>MustInherit ReadOnly Property p As x</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.WriteOnly),
<x>MustInherit WriteOnly Property p As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly),
<x>ReadOnly Property p As x
    Get
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly),
<x>WriteOnly Property p As x
    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
<x>MustInherit Property p As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly, getAccessorStatements:={_g.IdentifierName("y")}),
<x>ReadOnly Property p As x
    Get
        y
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly, setAccessorStatements:={_g.IdentifierName("y")}),
<x>WriteOnly Property p As x
    Set(value As x)
        y
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), setAccessorStatements:={_g.IdentifierName("y")}),
<x>Property p As x
    Get
    End Get

    Set(value As x)
        y
    End Set
End Property</x>.Value)
        End Sub

        <Fact>
        Public Sub TestIndexerDeclarations()
            VerifySyntax(Of PropertyStatementSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.ReadOnly),
<x>Default MustInherit ReadOnly Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.WriteOnly),
<x>Default MustInherit WriteOnly Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
<x>Default MustInherit Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly),
<x>Default ReadOnly Property Item(z As y) As x
    Get
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly),
<x>Default WriteOnly Property Item(z As y) As x
    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly,
                    getAccessorStatements:={_g.IdentifierName("a")}),
<x>Default ReadOnly Property Item(z As y) As x
    Get
        a
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.WriteOnly,
                    setAccessorStatements:={_g.IdentifierName("a")}),
<x>Default WriteOnly Property Item(z As y) As x
    Set(value As x)
        a
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.None),
<x>Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"),
                    setAccessorStatements:={_g.IdentifierName("a")}),
<x>Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
        a
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"),
                    getAccessorStatements:={_g.IdentifierName("a")}, setAccessorStatements:={_g.IdentifierName("b")}),
<x>Default Property Item(z As y) As x
    Get
        a
    End Get

    Set(value As x)
        b
    End Set
End Property</x>.Value)

        End Sub

        <Fact>
        Public Sub TestEventDeclarations()
            VerifySyntax(Of EventStatementSyntax)(
                _g.EventDeclaration("ev", _g.IdentifierName("t")),
<x>Event ev As t</x>.Value)

            VerifySyntax(Of EventStatementSyntax)(
                _g.EventDeclaration("ev", _g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Static),
<x>Public Shared Event ev As t</x>.Value)

            VerifySyntax(Of EventBlockSyntax)(
                _g.CustomEventDeclaration("ev", _g.IdentifierName("t")),
<x>Custom Event ev As t
    AddHandler(value As t)
    End AddHandler

    RemoveHandler(value As t)
    End RemoveHandler

    RaiseEvent()
    End RaiseEvent
End Event</x>.Value)

            Dim params = {_g.ParameterDeclaration("sender", _g.TypeExpression(SpecialType.System_Object)), _g.ParameterDeclaration("args", _g.IdentifierName("EventArgs"))}
            VerifySyntax(Of EventBlockSyntax)(
                _g.CustomEventDeclaration("ev", _g.IdentifierName("t"), parameters:=params),
<x>Custom Event ev As t
    AddHandler(value As t)
    End AddHandler

    RemoveHandler(value As t)
    End RemoveHandler

    RaiseEvent(sender As Object, args As EventArgs)
    End RaiseEvent
End Event</x>.Value)

        End Sub

        <Fact>
        Public Sub TestConstructorDeclaration()
            VerifySyntax(Of ConstructorBlockSyntax)(
                _g.ConstructorDeclaration("c"),
<x>Sub New()
End Sub</x>.Value)

            VerifySyntax(Of ConstructorBlockSyntax)(
                _g.ConstructorDeclaration("c", accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Static),
<x>Public Shared Sub New()
End Sub</x>.Value)

            VerifySyntax(Of ConstructorBlockSyntax)(
                _g.ConstructorDeclaration("c", parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))}),
<x>Sub New(p As t)
End Sub</x>.Value)

            VerifySyntax(Of ConstructorBlockSyntax)(
                _g.ConstructorDeclaration("c",
                    parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))},
                    baseConstructorArguments:={_g.IdentifierName("p")}),
<x>Sub New(p As t)
    MyBase.New(p)
End Sub</x>.Value)
        End Sub

        <Fact>
        Public Sub TestClassDeclarations()
            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c"),
<x>Class c
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", typeParameters:={"x", "y"}),
<x>Class c(Of x, y)
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", accessibility:=Accessibility.Public),
<x>Public Class c
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", baseType:=_g.IdentifierName("x")),
<x>Class c
    Inherits x

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", interfaceTypes:={_g.IdentifierName("x")}),
<x>Class c
    Implements x

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", baseType:=_g.IdentifierName("x"), interfaceTypes:={_g.IdentifierName("y"), _g.IdentifierName("z")}),
<x>Class c
    Inherits x
    Implements y, z

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", interfaceTypes:={}),
<x>Class c
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("c", members:={_g.FieldDeclaration("y", type:=_g.IdentifierName("x"))}),
<x>Class c

    Dim y As x
End Class</x>.Value)

        End Sub

        <Fact>
        Public Sub TestStructDeclarations()
            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s"),
<x>Structure s
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", typeParameters:={"x", "y"}),
<x>Structure s(Of x, y)
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Partial),
<x>Public Partial Structure s
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", interfaceTypes:={_g.IdentifierName("x")}),
<x>Structure s
    Implements x

End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", interfaceTypes:={_g.IdentifierName("x"), _g.IdentifierName("y")}),
<x>Structure s
    Implements x, y

End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", interfaceTypes:={}),
<x>Structure s
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", members:={_g.FieldDeclaration("y", _g.IdentifierName("x"))}),
<x>Structure s

    Dim y As x
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s", members:={_g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"))}),
<x>Structure s

    Function m() As t
    End Function
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.StructDeclaration("s",
                    members:={_g.ConstructorDeclaration(accessibility:=Accessibility.NotApplicable, modifiers:=DeclarationModifiers.None)}),
<x>Structure s

    Sub New()
    End Sub
End Structure</x>.Value)
        End Sub

        <Fact>
        Public Sub TestInterfaceDeclarations()
            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i"),
<x>Interface i
End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", typeParameters:={"x", "y"}),
<x>Interface i(Of x, y)
End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", interfaceTypes:={_g.IdentifierName("a")}),
<x>Interface i
    Inherits a

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", interfaceTypes:={_g.IdentifierName("a"), _g.IdentifierName("b")}),
<x>Interface i
    Inherits a, b

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", interfaceTypes:={}),
<x>Interface i
End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", members:={_g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Sealed)}),
<x>Interface i

    Function m() As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", members:={_g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Sealed)}),
<x>Interface i

    Property p As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", members:={_g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.ReadOnly)}),
<x>Interface i

    ReadOnly Property p As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", members:={_g.IndexerDeclaration({_g.ParameterDeclaration("y", _g.IdentifierName("x"))}, _g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.Sealed)}),
<x>Interface i

    Default Property Item(y As x) As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.InterfaceDeclaration("i", members:={_g.IndexerDeclaration({_g.ParameterDeclaration("y", _g.IdentifierName("x"))}, _g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.ReadOnly)}),
<x>Interface i

    Default ReadOnly Property Item(y As x) As t

End Interface</x>.Value)
        End Sub

        <Fact>
        Public Sub TestEnumDeclarations()
            VerifySyntax(Of EnumBlockSyntax)(
                _g.EnumDeclaration("e"),
<x>Enum e
End Enum</x>.Value)

            VerifySyntax(Of EnumBlockSyntax)(
                _g.EnumDeclaration("e", members:={_g.EnumMember("a"), _g.EnumMember("b"), _g.EnumMember("c")}),
<x>Enum e
    a
    b
    c
End Enum</x>.Value)

            VerifySyntax(Of EnumBlockSyntax)(
                _g.EnumDeclaration("e", members:={_g.IdentifierName("a"), _g.EnumMember("b"), _g.IdentifierName("c")}),
<x>Enum e
    a
    b
    c
End Enum</x>.Value)

            VerifySyntax(Of EnumBlockSyntax)(
                _g.EnumDeclaration("e", members:={_g.EnumMember("a", _g.LiteralExpression(0)), _g.EnumMember("b"), _g.EnumMember("c", _g.LiteralExpression(5))}),
<x>Enum e
    a = 0
    b
    c = 5
End Enum</x>.Value)
        End Sub

        <Fact>
        Public Sub TestDelegateDeclarations()
            VerifySyntax(Of DelegateStatementSyntax)(
                _g.DelegateDeclaration("d"),
<x>Delegate Sub d()</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.DelegateDeclaration("d", parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))}),
<x>Delegate Sub d(p As t)</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.DelegateDeclaration("d", returnType:=_g.IdentifierName("t")),
<x>Delegate Function d() As t</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.DelegateDeclaration("d", parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))}, returnType:=_g.IdentifierName("t")),
<x>Delegate Function d(p As t) As t</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.DelegateDeclaration("d", accessibility:=Accessibility.Public),
<x>Public Delegate Sub d()</x>.Value)

            ' ignores modifiers
            VerifySyntax(Of DelegateStatementSyntax)(
                _g.DelegateDeclaration("d", modifiers:=DeclarationModifiers.Static),
<x>Delegate Sub d()</x>.Value)
        End Sub

        <Fact>
        Public Sub TestNamespaceImportDeclarations()
            VerifySyntax(Of ImportsStatementSyntax)(
                _g.NamespaceImportDeclaration(_g.IdentifierName("n")),
<x>Imports n</x>.Value)

            VerifySyntax(Of ImportsStatementSyntax)(
                _g.NamespaceImportDeclaration("n"),
<x>Imports n</x>.Value)

            VerifySyntax(Of ImportsStatementSyntax)(
                _g.NamespaceImportDeclaration("n.m"),
<x>Imports n.m</x>.Value)
        End Sub

        <Fact>
        Public Sub TestNamespaceDeclarations()
            VerifySyntax(Of NamespaceBlockSyntax)(
                _g.NamespaceDeclaration("n"),
<x>Namespace n
End Namespace</x>.Value)

            VerifySyntax(Of NamespaceBlockSyntax)(
                _g.NamespaceDeclaration("n.m"),
<x>Namespace n.m
End Namespace</x>.Value)

            VerifySyntax(Of NamespaceBlockSyntax)(
                _g.NamespaceDeclaration("n",
                    _g.NamespaceImportDeclaration("m")),
<x>Namespace n

    Imports m
End Namespace</x>.Value)

            VerifySyntax(Of NamespaceBlockSyntax)(
                _g.NamespaceDeclaration("n",
                    _g.ClassDeclaration("c"),
                    _g.NamespaceImportDeclaration("m")),
<x>Namespace n

    Imports m

    Class c
    End Class
End Namespace</x>.Value)
        End Sub


        <Fact>
        Public Sub TestCompilationUnits()
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.CompilationUnit(),
                "")

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.CompilationUnit(
                    _g.NamespaceDeclaration("n")),
<x>Namespace n
End Namespace
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.CompilationUnit(
                    _g.NamespaceImportDeclaration("n")),
<x>Imports n
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.CompilationUnit(
                    _g.ClassDeclaration("c"),
                    _g.NamespaceImportDeclaration("m")),
<x>Imports m

Class c
End Class
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.CompilationUnit(
                    _g.NamespaceImportDeclaration("n"),
                    _g.NamespaceDeclaration("n",
                        _g.NamespaceImportDeclaration("m"),
                        _g.ClassDeclaration("c"))),
<x>Imports n

Namespace n

    Imports m

    Class c
    End Class
End Namespace
</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAsPublicInterfaceImplementation()
            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPublicInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
<x>Public Function m() As t Implements i.m
End Function</x>.Value)

            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPublicInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    _g.IdentifierName("i")),
<x>Public Function m() As t Implements i.m
End Function</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AsPublicInterfaceImplementation(
                    _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
<x>Public Property p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AsPublicInterfaceImplementation(
                    _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.None),
                    _g.IdentifierName("i")),
<x>Public Property p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AsPublicInterfaceImplementation(
                    _g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("a"))}, _g.IdentifierName("t"), Accessibility.Internal, DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
<x>Default Public Property Item(p As a) As t Implements i.Item
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            ' convert private method to public 
            Dim pim = _g.AsPrivateInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t")),
                    _g.IdentifierName("i"))

            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPublicInterfaceImplementation(pim, _g.IdentifierName("i2")),
<x>Public Function m() As t Implements i2.m
End Function</x>.Value)

            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPublicInterfaceImplementation(pim, _g.IdentifierName("i2"), "m2"),
<x>Public Function m2() As t Implements i2.m2
End Function</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAsPrivateInterfaceImplementation()
            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPrivateInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
<x>Private Function i_m() As t Implements i.m
End Function</x>.Value)

            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPrivateInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    _g.TypeExpression(Me._ienumerableInt)),
<x>Private Function IEnumerable_Int32_m() As t Implements Global.System.Collections.Generic.IEnumerable(Of System.Int32).m
End Function</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AsPrivateInterfaceImplementation(
                    _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Internal, modifiers:=DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
<x>Private Property i_p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AsPrivateInterfaceImplementation(
                    _g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("a"))}, _g.IdentifierName("t"), Accessibility.Protected, DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
<x>Private Property i_Item(p As a) As t Implements i.Item
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            ' convert public method to private
            Dim pim = _g.AsPublicInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t")),
                    _g.IdentifierName("i"))

            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPrivateInterfaceImplementation(pim, _g.IdentifierName("i2")),
<x>Private Function i2_m() As t Implements i2.m
End Function</x>.Value)

            VerifySyntax(Of MethodBlockBaseSyntax)(
                _g.AsPrivateInterfaceImplementation(pim, _g.IdentifierName("i2"), "m2"),
<x>Private Function i2_m2() As t Implements i2.m2
End Function</x>.Value)
        End Sub

        <Fact>
        Public Sub TestWithTypeParameters()
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a"),
<x>MustInherit Sub m(Of a)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers:=DeclarationModifiers.None),
                    "a"),
<x>Sub m(Of a)()
End Sub</x>.Value)

            ' assigning no type parameters is legal
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)),
<x>MustInherit Sub m()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers:=DeclarationModifiers.None)),
<x>Sub m()
End Sub</x>.Value)

            ' removing type parameters
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeParameters(_g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a")),
<x>MustInherit Sub m()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeParameters(_g.WithTypeParameters(
                    _g.MethodDeclaration("m"),
                    "a")),
<x>Sub m()
End Sub</x>.Value)

            ' multiple type parameters
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a", "b"),
<x>MustInherit Sub m(Of a, b)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m"),
                    "a", "b"),
<x>Sub m(Of a, b)()
End Sub</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.WithTypeParameters(
                    _g.ClassDeclaration("c"),
                    "a", "b"),
<x>Class c(Of a, b)
End Class</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.WithTypeParameters(
                    _g.StructDeclaration("s"),
                    "a", "b"),
<x>Structure s(Of a, b)
End Structure</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.WithTypeParameters(
                    _g.InterfaceDeclaration("i"),
                    "a", "b"),
<x>Interface i(Of a, b)
End Interface</x>.Value)

        End Sub

        <Fact>
        Public Sub TestWithTypeConstraint()
            ' single type constraint
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", _g.IdentifierName("b")),
<x>MustInherit Sub m(Of a As b)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m"), "a"),
                    "a", _g.IdentifierName("b")),
<x>Sub m(Of a As b)()
End Sub</x>.Value)

            ' multiple type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", _g.IdentifierName("b"), _g.IdentifierName("c")),
<x>MustInherit Sub m(Of a As {b, c})()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m"), "a"),
                    "a", _g.IdentifierName("b"), _g.IdentifierName("c")),
<x>Sub m(Of a As {b, c})()
End Sub</x>.Value)

            ' no type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a"),
<x>MustInherit Sub m(Of a)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m"), "a"),
                    "a"),
<x>Sub m(Of a)()
End Sub</x>.Value)

            ' removed type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(_g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", _g.IdentifierName("b"), _g.IdentifierName("c")), "a"),
<x>MustInherit Sub m(Of a)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithTypeConstraint(_g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m"), "a"),
                    "a", _g.IdentifierName("b"), _g.IdentifierName("c")), "a"),
<x>Sub m(Of a)()
End Sub</x>.Value)

            ' multiple type parameters with constraints
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeConstraint(
                        _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a", "x"),
                        "a", _g.IdentifierName("b"), _g.IdentifierName("c")),
                    "x", _g.IdentifierName("y")),
<x>MustInherit Sub m(Of a As {b, c}, x As y)()</x>.Value)

            ' with constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.Constructor),
<x>MustInherit Sub m(Of a As New)()</x>.Value)

            ' with reference constraint
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType),
<x>MustInherit Sub m(Of a As Class)()</x>.Value)

            ' with value type constraint
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType),
<x>MustInherit Sub m(Of a As Structure)()</x>.Value)

            ' with reference constraint and constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType Or SpecialTypeConstraintKind.Constructor),
<x>MustInherit Sub m(Of a As {Class, New})()</x>.Value)

            ' with value type constraint and constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType Or SpecialTypeConstraintKind.Constructor),
<x>MustInherit Sub m(Of a As {Structure, New})()</x>.Value)

            ' with reference constraint and type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType, _g.IdentifierName("b"), _g.IdentifierName("c")),
<x>MustInherit Sub m(Of a As {Class, b, c})()</x>.Value)

            ' class declarations
            VerifySyntax(Of ClassBlockSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.ClassDeclaration("c"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
<x>Class c(Of a As x, b)
End Class</x>.Value)

            ' structure declarations
            VerifySyntax(Of StructureBlockSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.StructDeclaration("s"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
<x>Structure s(Of a As x, b)
End Structure</x>.Value)

            ' interface declarations
            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.InterfaceDeclaration("i"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
<x>Interface i(Of a As x, b)
End Interface</x>.Value)

        End Sub

        <Fact>
        Public Sub TestAttributeDeclarations()
            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute(_g.IdentifierName("a")),
                "<a>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a"),
                "<a>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a.b"),
                "<a.b>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a", {}),
                "<a()>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a", {_g.IdentifierName("x")}),
                "<a(x)>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a", {_g.AttributeArgument(_g.IdentifierName("x"))}),
                "<a(x)>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a", {_g.AttributeArgument("x", _g.IdentifierName("y"))}),
                "<a(x:=y)>")

            VerifySyntax(Of AttributeListSyntax)(
                _g.Attribute("a", {_g.IdentifierName("x"), _g.IdentifierName("y")}),
                "<a(x, y)>")
        End Sub

        <Fact>
        Public Sub TestAddAttributes()
            VerifySyntax(Of FieldDeclarationSyntax)(
                _g.AddAttributes(
                    _g.FieldDeclaration("y", _g.IdentifierName("x")),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Dim y As x</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                _g.AddAttributes(
                    _g.AddAttributes(
                        _g.FieldDeclaration("y", _g.IdentifierName("x")),
                        _g.Attribute("a")),
                    _g.Attribute("b")),
<x>&lt;a&gt;
&lt;b&gt;
Dim y As x</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                _g.AddAttributes(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
<x>&lt;a&gt;
MustInherit Function m() As t</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                _g.AddReturnAttributes(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
<x>MustInherit Function m() As &lt;a&gt; t</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.AddAttributes(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Function m() As t
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.AddReturnAttributes(
                    _g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    _g.Attribute("a")),
<x>Function m() As &lt;a&gt; t
End Function</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                _g.AddAttributes(
                    _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
<x>&lt;a&gt;
MustInherit Property p As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AddAttributes(
                    _g.PropertyDeclaration("p", _g.IdentifierName("x")),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Property p As x
    Get
    End Get

    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                _g.AddAttributes(
                    _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Default MustInherit Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                _g.AddAttributes(
                    _g.IndexerDeclaration({_g.ParameterDeclaration("z", _g.IdentifierName("y"))}, _g.IdentifierName("x")),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.AddAttributes(
                    _g.ClassDeclaration("c"),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Class c
End Class</x>.Value)

            VerifySyntax(Of ParameterSyntax)(
                _g.AddAttributes(
                    _g.ParameterDeclaration("p", _g.IdentifierName("t")),
                    _g.Attribute("a")),
<x>&lt;a&gt; p As t</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.AddAttributes(
                    _g.CompilationUnit(_g.NamespaceDeclaration("n")),
                    _g.Attribute("a")),
<x>&lt;Assembly:a&gt;
Namespace n
End Namespace
</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.AddAttributes(
                    _g.DelegateDeclaration("d"),
                    _g.Attribute("a")),
<x>&lt;a&gt;
Delegate Sub d()</x>.Value)

        End Sub

        <Fact>
        <WorkItem(5066, "https://github.com/dotnet/roslyn/issues/5066")>
        Public Sub TestAddAttributesOnAccessors()
            Dim prop = _g.PropertyDeclaration("P", _g.IdentifierName("T"))

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

            CheckAddRemoveAttribute(_g.GetAccessor(prop, DeclarationKind.GetAccessor))
            CheckAddRemoveAttribute(_g.GetAccessor(prop, DeclarationKind.SetAccessor))
            CheckAddRemoveAttribute(_g.GetAccessor(evnt, DeclarationKind.AddAccessor))
            CheckAddRemoveAttribute(_g.GetAccessor(evnt, DeclarationKind.RemoveAccessor))
            CheckAddRemoveAttribute(_g.GetAccessor(evnt, DeclarationKind.RaiseAccessor))
        End Sub

        Private Sub CheckAddRemoveAttribute(declaration As SyntaxNode)
            Dim initialAttributes = _g.GetAttributes(declaration)
            Assert.Equal(0, initialAttributes.Count)

            Dim withAttribute = _g.AddAttributes(declaration, _g.Attribute("a"))
            Dim attrsAdded = _g.GetAttributes(withAttribute)
            Assert.Equal(1, attrsAdded.Count)

            Dim withoutAttribute = _g.RemoveNode(withAttribute, attrsAdded(0))
            Dim attrsRemoved = _g.GetAttributes(withoutAttribute)
            Assert.Equal(0, attrsRemoved.Count)
        End Sub

        <Fact>
        Public Sub TestAddRemoveAttributesPreservesTrivia()
            Dim cls = ParseCompilationUnit(
<x>' comment
Class C
End Class ' end</x>.Value).Members(0)

            Dim added = _g.AddAttributes(cls, _g.Attribute("a"))
            VerifySyntax(Of ClassBlockSyntax)(
                added,
<x>' comment
&lt;a&gt;
Class C
End Class ' end</x>.Value)

            Dim removed = _g.RemoveAllAttributes(added)
            VerifySyntax(Of ClassBlockSyntax)(
                removed,
<x>' comment
Class C
End Class ' end</x>.Value)

            Dim attrWithComment = _g.GetAttributes(added).First()
            VerifySyntax(Of AttributeListSyntax)(
                attrWithComment,
<x>' comment
&lt;a&gt;</x>.Value)

        End Sub

        <Fact>
        Public Sub TestInterfaceDeclarationWithEventSymbol()
            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.Declaration(_emptyCompilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged")),
<x>Public Interface INotifyPropertyChanged

    Event PropertyChanged As Global.System.ComponentModel.PropertyChangedEventHandler

End Interface</x>.Value)
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
            Dim newCu = _g.RemoveNode(cu, summary)
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
            Dim newCu = _g.ReplaceNode(cu, summary, summary2)
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
            Dim newCu = _g.InsertNodesAfter(cu, text, {text})
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
            Dim newCu = _g.InsertNodesBefore(cu, text, {text})
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
            Assert.Equal(DeclarationKind.CompilationUnit, _g.GetDeclarationKind(_g.CompilationUnit()))
            Assert.Equal(DeclarationKind.Class, _g.GetDeclarationKind(_g.ClassDeclaration("c")))
            Assert.Equal(DeclarationKind.Struct, _g.GetDeclarationKind(_g.StructDeclaration("s")))
            Assert.Equal(DeclarationKind.Interface, _g.GetDeclarationKind(_g.InterfaceDeclaration("i")))
            Assert.Equal(DeclarationKind.Enum, _g.GetDeclarationKind(_g.EnumDeclaration("e")))
            Assert.Equal(DeclarationKind.Delegate, _g.GetDeclarationKind(_g.DelegateDeclaration("d")))
            Assert.Equal(DeclarationKind.Method, _g.GetDeclarationKind(_g.MethodDeclaration("m")))
            Assert.Equal(DeclarationKind.Method, _g.GetDeclarationKind(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationKind.Constructor, _g.GetDeclarationKind(_g.ConstructorDeclaration()))
            Assert.Equal(DeclarationKind.Parameter, _g.GetDeclarationKind(_g.ParameterDeclaration("p")))
            Assert.Equal(DeclarationKind.Property, _g.GetDeclarationKind(_g.PropertyDeclaration("p", _g.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.Property, _g.GetDeclarationKind(_g.PropertyDeclaration("p", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationKind.Indexer, _g.GetDeclarationKind(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.Indexer, _g.GetDeclarationKind(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(_g.FieldDeclaration("f", _g.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.EnumMember, _g.GetDeclarationKind(_g.EnumMember("v")))
            Assert.Equal(DeclarationKind.Event, _g.GetDeclarationKind(_g.EventDeclaration("e", _g.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.CustomEvent, _g.GetDeclarationKind(_g.CustomEventDeclaration("ce", _g.IdentifierName("t"))))
            Assert.Equal(DeclarationKind.Namespace, _g.GetDeclarationKind(_g.NamespaceDeclaration("n")))
            Assert.Equal(DeclarationKind.NamespaceImport, _g.GetDeclarationKind(_g.NamespaceImportDeclaration("u")))
            Assert.Equal(DeclarationKind.Variable, _g.GetDeclarationKind(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")))
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(_g.Attribute("a")))
        End Sub

        <Fact>
        Public Sub TestGetName()
            Assert.Equal("c", _g.GetName(_g.ClassDeclaration("c")))
            Assert.Equal("s", _g.GetName(_g.StructDeclaration("s")))
            Assert.Equal("i", _g.GetName(_g.EnumDeclaration("i")))
            Assert.Equal("e", _g.GetName(_g.EnumDeclaration("e")))
            Assert.Equal("d", _g.GetName(_g.DelegateDeclaration("d")))
            Assert.Equal("m", _g.GetName(_g.MethodDeclaration("m")))
            Assert.Equal("m", _g.GetName(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal("", _g.GetName(_g.ConstructorDeclaration()))
            Assert.Equal("p", _g.GetName(_g.ParameterDeclaration("p")))
            Assert.Equal("p", _g.GetName(_g.PropertyDeclaration("p", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal("p", _g.GetName(_g.PropertyDeclaration("p", _g.IdentifierName("t"))))
            Assert.Equal("Item", _g.GetName(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"))))
            Assert.Equal("Item", _g.GetName(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal("f", _g.GetName(_g.FieldDeclaration("f", _g.IdentifierName("t"))))
            Assert.Equal("v", _g.GetName(_g.EnumMember("v")))
            Assert.Equal("ef", _g.GetName(_g.EventDeclaration("ef", _g.IdentifierName("t"))))
            Assert.Equal("ep", _g.GetName(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"))))
            Assert.Equal("n", _g.GetName(_g.NamespaceDeclaration("n")))
            Assert.Equal("u", _g.GetName(_g.NamespaceImportDeclaration("u")))
            Assert.Equal("loc", _g.GetName(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")))
            Assert.Equal("a", _g.GetName(_g.Attribute("a")))
        End Sub

        <Fact>
        Public Sub TestWithName()
            Assert.Equal("c", _g.GetName(_g.WithName(_g.ClassDeclaration("x"), "c")))
            Assert.Equal("s", _g.GetName(_g.WithName(_g.StructDeclaration("x"), "s")))
            Assert.Equal("i", _g.GetName(_g.WithName(_g.EnumDeclaration("x"), "i")))
            Assert.Equal("e", _g.GetName(_g.WithName(_g.EnumDeclaration("x"), "e")))
            Assert.Equal("d", _g.GetName(_g.WithName(_g.DelegateDeclaration("x"), "d")))
            Assert.Equal("m", _g.GetName(_g.WithName(_g.MethodDeclaration("x"), "m")))
            Assert.Equal("m", _g.GetName(_g.WithName(_g.MethodDeclaration("x", modifiers:=DeclarationModifiers.Abstract), "m")))
            Assert.Equal("", _g.GetName(_g.WithName(_g.ConstructorDeclaration(), ".ctor")))
            Assert.Equal("p", _g.GetName(_g.WithName(_g.ParameterDeclaration("x"), "p")))
            Assert.Equal("p", _g.GetName(_g.WithName(_g.PropertyDeclaration("x", _g.IdentifierName("t")), "p")))
            Assert.Equal("p", _g.GetName(_g.WithName(_g.PropertyDeclaration("x", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract), "p")))
            Assert.Equal("X", _g.GetName(_g.WithName(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t")), "X")))
            Assert.Equal("X", _g.GetName(_g.WithName(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract), "X")))
            Assert.Equal("f", _g.GetName(_g.WithName(_g.FieldDeclaration("x", _g.IdentifierName("t")), "f")))
            Assert.Equal("v", _g.GetName(_g.WithName(_g.EnumMember("x"), "v")))
            Assert.Equal("ef", _g.GetName(_g.WithName(_g.EventDeclaration("x", _g.IdentifierName("t")), "ef")))
            Assert.Equal("ep", _g.GetName(_g.WithName(_g.CustomEventDeclaration("x", _g.IdentifierName("t")), "ep")))
            Assert.Equal("n", _g.GetName(_g.WithName(_g.NamespaceDeclaration("x"), "n")))
            Assert.Equal("u", _g.GetName(_g.WithName(_g.NamespaceImportDeclaration("x"), "u")))
            Assert.Equal("loc", _g.GetName(_g.WithName(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "x"), "loc")))
            Assert.Equal("a", _g.GetName(_g.WithName(_g.Attribute("x"), "a")))
        End Sub

        <Fact>
        Public Sub TestGetAccessibility()
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.ClassDeclaration("c", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.StructDeclaration("s", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.EnumDeclaration("i", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.EnumDeclaration("e", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.DelegateDeclaration("d", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.MethodDeclaration("m", accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.ConstructorDeclaration(accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.ParameterDeclaration("p")))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.FieldDeclaration("f", _g.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.EnumMember("v")))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.EventDeclaration("ef", _g.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"), accessibility:=Accessibility.Internal)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.NamespaceDeclaration("n")))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.NamespaceImportDeclaration("u")))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.Attribute("a")))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(SyntaxFactory.TypeParameter("tp")))
        End Sub

        <Fact>
        Public Sub TestWithAccessibility()
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.ClassDeclaration("c", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.StructDeclaration("s", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.EnumDeclaration("i", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.EnumDeclaration("e", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.DelegateDeclaration("d", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.MethodDeclaration("m", accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.ConstructorDeclaration(accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.ParameterDeclaration("p"), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.FieldDeclaration("f", _g.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.EnumMember("v"), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.EventDeclaration("ef", _g.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"), accessibility:=Accessibility.Internal), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.NamespaceDeclaration("n"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.NamespaceImportDeclaration("u"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.Attribute("a"), Accessibility.Private)))
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(SyntaxFactory.TypeParameter("tp"), Accessibility.Private)))
        End Sub

        <Fact>
        Public Sub TestGetModifiers()
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.ClassDeclaration("c", modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Partial, _g.GetModifiers(_g.StructDeclaration("s", modifiers:=DeclarationModifiers.Partial)))
            Assert.Equal(DeclarationModifiers.[New], _g.GetModifiers(_g.EnumDeclaration("e", modifiers:=DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.[New], _g.GetModifiers(_g.DelegateDeclaration("d", modifiers:=DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.ConstructorDeclaration(modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.ParameterDeclaration("p")))
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.PropertyDeclaration("p", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Const, _g.GetModifiers(_g.FieldDeclaration("f", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Const)))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.EventDeclaration("ef", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"), modifiers:=DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.EnumMember("v")))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.NamespaceDeclaration("n")))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.NamespaceImportDeclaration("u")))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.Attribute("a")))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(SyntaxFactory.TypeParameter("tp")))
        End Sub

        <Fact>
        Public Sub TestWithModifiers()
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.WithModifiers(_g.ClassDeclaration("c"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Partial, _g.GetModifiers(_g.WithModifiers(_g.StructDeclaration("s"), DeclarationModifiers.Partial)))
            Assert.Equal(DeclarationModifiers.[New], _g.GetModifiers(_g.WithModifiers(_g.EnumDeclaration("e"), DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.[New], _g.GetModifiers(_g.WithModifiers(_g.DelegateDeclaration("d"), DeclarationModifiers.[New])))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.MethodDeclaration("m"), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.ConstructorDeclaration(), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.ParameterDeclaration("p"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.WithModifiers(_g.PropertyDeclaration("p", _g.IdentifierName("t")), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.WithModifiers(_g.IndexerDeclaration({_g.ParameterDeclaration("i")}, _g.IdentifierName("t")), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.Const, _g.GetModifiers(_g.WithModifiers(_g.FieldDeclaration("f", _g.IdentifierName("t")), DeclarationModifiers.Const)))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.EventDeclaration("ef", _g.IdentifierName("t")), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.CustomEventDeclaration("ep", _g.IdentifierName("t")), DeclarationModifiers.Static)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.EnumMember("v"), DeclarationModifiers.Partial)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.NamespaceDeclaration("n"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.NamespaceImportDeclaration("u"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.Attribute("a"), DeclarationModifiers.Abstract)))
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(SyntaxFactory.TypeParameter("tp"), DeclarationModifiers.Abstract)))
        End Sub

        <Fact>
        Public Sub TestGetType()
            Assert.Equal("t", _g.GetType(_g.MethodDeclaration("m", returnType:=_g.IdentifierName("t"))).ToString())
            Assert.Null(_g.GetType(_g.MethodDeclaration("m")))

            Assert.Equal("t", _g.GetType(_g.FieldDeclaration("f", _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("pt"))}, _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.ParameterDeclaration("p", _g.IdentifierName("t"))).ToString())

            Assert.Equal("t", _g.GetType(_g.EventDeclaration("ef", _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"))).ToString())

            Assert.Equal("t", _g.GetType(_g.DelegateDeclaration("t", returnType:=_g.IdentifierName("t"))).ToString())
            Assert.Null(_g.GetType(_g.DelegateDeclaration("d")))

            Assert.Equal("t", _g.GetType(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "v")).ToString())

            Assert.Null(_g.GetType(_g.ClassDeclaration("c")))
            Assert.Null(_g.GetType(_g.IdentifierName("x")))
        End Sub

        <Fact>
        Public Sub TestWithType()
            Assert.Equal("t", _g.GetType(_g.WithType(_g.MethodDeclaration("m", returnType:=_g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.MethodDeclaration("m"), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.FieldDeclaration("f", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.PropertyDeclaration("p", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("pt"))}, _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.ParameterDeclaration("p", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())

            Assert.Equal("t", _g.GetType(_g.WithType(_g.DelegateDeclaration("t", returnType:=_g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.DelegateDeclaration("t"), _g.IdentifierName("t"))).ToString())

            Assert.Equal("t", _g.GetType(_g.WithType(_g.EventDeclaration("ef", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())
            Assert.Equal("t", _g.GetType(_g.WithType(_g.CustomEventDeclaration("ep", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString())

            Assert.Equal("t", _g.GetType(_g.WithType(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "v"), _g.IdentifierName("t"))).ToString())
            Assert.Null(_g.GetType(_g.WithType(_g.ClassDeclaration("c"), _g.IdentifierName("t"))))
            Assert.Null(_g.GetType(_g.WithType(_g.IdentifierName("x"), _g.IdentifierName("t"))))
        End Sub

        <Fact>
        Public Sub TestWithTypeChangesSubFunction()
            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithType(_g.MethodDeclaration("m", returnType:=_g.IdentifierName("x")), Nothing),
<x>Sub m()
End Sub</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                _g.WithType(_g.MethodDeclaration("m"), _g.IdentifierName("x")),
<x>Function m() As x
End Function</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithType(_g.MethodDeclaration("m", returnType:=_g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract), Nothing),
<x>MustInherit Sub m()</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                _g.WithType(_g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), _g.IdentifierName("x")),
<x>MustInherit Function m() As x</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.WithType(_g.DelegateDeclaration("d", returnType:=_g.IdentifierName("x")), Nothing),
<x>Delegate Sub d()</x>.Value)

            VerifySyntax(Of DelegateStatementSyntax)(
                _g.WithType(_g.DelegateDeclaration("d"), _g.IdentifierName("x")),
<x>Delegate Function d() As x</x>.Value)

        End Sub

        <Fact>
        Public Sub TestGetParameters()
            Assert.Equal(0, _g.GetParameters(_g.MethodDeclaration("m")).Count)
            Assert.Equal(1, _g.GetParameters(_g.MethodDeclaration("m", parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
            Assert.Equal(2, _g.GetParameters(_g.MethodDeclaration("m", parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.ParameterDeclaration("p2", _g.IdentifierName("t2"))})).Count)

            Assert.Equal(0, _g.GetParameters(_g.ConstructorDeclaration()).Count)
            Assert.Equal(1, _g.GetParameters(_g.ConstructorDeclaration(parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
            Assert.Equal(2, _g.GetParameters(_g.ConstructorDeclaration(parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.ParameterDeclaration("p2", _g.IdentifierName("t2"))})).Count)

            Assert.Equal(0, _g.GetParameters(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).Count)

            Assert.Equal(1, _g.GetParameters(_g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("t"))}, _g.IdentifierName("t"))).Count)
            Assert.Equal(2, _g.GetParameters(_g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.ParameterDeclaration("p2", _g.IdentifierName("t2"))}, _g.IdentifierName("t"))).Count)

            Assert.Equal(0, _g.GetParameters(_g.ValueReturningLambdaExpression(_g.IdentifierName("expr"))).Count)
            Assert.Equal(1, _g.GetParameters(_g.ValueReturningLambdaExpression("p1", _g.IdentifierName("expr"))).Count)

            Assert.Equal(0, _g.GetParameters(_g.VoidReturningLambdaExpression(_g.IdentifierName("expr"))).Count)
            Assert.Equal(1, _g.GetParameters(_g.VoidReturningLambdaExpression("p1", _g.IdentifierName("expr"))).Count)

            Assert.Equal(0, _g.GetParameters(_g.DelegateDeclaration("d")).Count)
            Assert.Equal(1, _g.GetParameters(_g.DelegateDeclaration("d", parameters:={_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)

            Assert.Equal(0, _g.GetParameters(_g.ClassDeclaration("c")).Count)
            Assert.Equal(0, _g.GetParameters(_g.IdentifierName("x")).Count)
        End Sub

        <Fact>
        Public Sub TestAddParameters()
            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.MethodDeclaration("m"), {_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.ConstructorDeclaration(), {_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
            Assert.Equal(3, _g.GetParameters(_g.AddParameters(_g.IndexerDeclaration({_g.ParameterDeclaration("p", _g.IdentifierName("t"))}, _g.IdentifierName("t")), {_g.ParameterDeclaration("p2", _g.IdentifierName("t2")), _g.ParameterDeclaration("p3", _g.IdentifierName("t3"))})).Count)

            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.ValueReturningLambdaExpression(_g.IdentifierName("expr")), {_g.LambdaParameter("p")})).Count)
            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.VoidReturningLambdaExpression(_g.IdentifierName("expr")), {_g.LambdaParameter("p")})).Count)

            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.DelegateDeclaration("d"), {_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)

            Assert.Equal(0, _g.GetParameters(_g.AddParameters(_g.ClassDeclaration("c"), {_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
            Assert.Equal(0, _g.GetParameters(_g.AddParameters(_g.IdentifierName("x"), {_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
            Assert.Equal(0, _g.GetParameters(_g.AddParameters(_g.PropertyDeclaration("p", _g.IdentifierName("t")), {_g.ParameterDeclaration("p", _g.IdentifierName("t"))})).Count)
        End Sub

        <Fact>
        Public Sub TestGetExpression()
            ' initializers
            Assert.Equal("x", _g.GetExpression(_g.FieldDeclaration("f", _g.IdentifierName("t"), initializer:=_g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.ParameterDeclaration("p", _g.IdentifierName("t"), initializer:=_g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.LocalDeclarationStatement("loc", initializer:=_g.IdentifierName("x"))).ToString())

            ' lambda bodies
            Assert.Null(_g.GetExpression(_g.ValueReturningLambdaExpression("p", {_g.IdentifierName("x")})))
            Assert.Equal(1, _g.GetStatements(_g.ValueReturningLambdaExpression("p", {_g.IdentifierName("x")})).Count)
            Assert.Equal("x", _g.GetExpression(_g.ValueReturningLambdaExpression(_g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.VoidReturningLambdaExpression(_g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.ValueReturningLambdaExpression("p", _g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.VoidReturningLambdaExpression("p", _g.IdentifierName("x"))).ToString())

            Assert.Null(_g.GetExpression(_g.IdentifierName("e")))
        End Sub

        <Fact>
        Public Sub TestWithExpression()
            ' initializers
            Assert.Equal("x", _g.GetExpression(_g.WithExpression(_g.FieldDeclaration("f", _g.IdentifierName("t")), _g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.WithExpression(_g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.IdentifierName("x"))).ToString())
            Assert.Equal("x", _g.GetExpression(_g.WithExpression(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc"), _g.IdentifierName("x"))).ToString())

            ' lambda bodies
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression("p", {_g.IdentifierName("x")}), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression("p", {_g.IdentifierName("x")}), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression({_g.IdentifierName("x")}), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression({_g.IdentifierName("x")}), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression("p", _g.IdentifierName("x")), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression("p", _g.IdentifierName("x")), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression(_g.IdentifierName("x")), _g.IdentifierName("y"))).ToString())
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression(_g.IdentifierName("x")), _g.IdentifierName("y"))).ToString())

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.ValueReturningLambdaExpression({_g.IdentifierName("s")}), _g.IdentifierName("e")),
                <x>Function() e</x>.Value)

            Assert.Null(_g.GetExpression(_g.WithExpression(_g.IdentifierName("e"), _g.IdentifierName("x"))))
        End Sub

        <Fact>
        Public Sub TestWithExpression_LambdaChanges()
            ' multi line function changes to single line function
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.ValueReturningLambdaExpression({_g.IdentifierName("s")}), _g.IdentifierName("e")),
                <x>Function() e</x>.Value)

            ' multi line sub changes to single line sub
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.VoidReturningLambdaExpression({_g.IdentifierName("s")}), _g.IdentifierName("e")),
                <x>Sub() e</x>.Value)

            ' single line function changes to multi-line function with null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.ValueReturningLambdaExpression(_g.IdentifierName("e")), Nothing),
<x>Function()
End Function</x>.Value)

            ' single line sub changes to multi line sub with null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.VoidReturningLambdaExpression(_g.IdentifierName("e")), Nothing),
<x>Sub()
End Sub</x>.Value)

            ' multi line function no-op when assigned null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.ValueReturningLambdaExpression({_g.IdentifierName("s")}), Nothing),
<x>Function()
    s
End Function</x>.Value)

            ' multi line sub no-op when assigned null expression
            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithExpression(_g.VoidReturningLambdaExpression({_g.IdentifierName("s")}), Nothing),
<x>Sub()
    s
End Sub</x>.Value)

            Assert.Null(_g.GetExpression(_g.WithExpression(_g.IdentifierName("e"), _g.IdentifierName("x"))))
        End Sub

        <Fact>
        Public Sub TestGetStatements()
            Dim stmts = {_g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))), _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))}

            Assert.Equal(0, _g.GetStatements(_g.MethodDeclaration("m")).Count)
            Assert.Equal(2, _g.GetStatements(_g.MethodDeclaration("m", statements:=stmts)).Count)

            Assert.Equal(0, _g.GetStatements(_g.ConstructorDeclaration()).Count)
            Assert.Equal(2, _g.GetStatements(_g.ConstructorDeclaration(statements:=stmts)).Count)

            Assert.Equal(0, _g.GetStatements(_g.VoidReturningLambdaExpression(_g.IdentifierName("e"))).Count)
            Assert.Equal(0, _g.GetStatements(_g.VoidReturningLambdaExpression({})).Count)
            Assert.Equal(2, _g.GetStatements(_g.VoidReturningLambdaExpression(stmts)).Count)

            Assert.Equal(0, _g.GetStatements(_g.ValueReturningLambdaExpression(_g.IdentifierName("e"))).Count)
            Assert.Equal(0, _g.GetStatements(_g.ValueReturningLambdaExpression({})).Count)
            Assert.Equal(2, _g.GetStatements(_g.ValueReturningLambdaExpression(stmts)).Count)

            Assert.Equal(0, _g.GetStatements(_g.IdentifierName("x")).Count)
        End Sub

        <Fact>
        Public Sub TestWithStatements()
            Dim stmts = {_g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))), _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))}

            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.MethodDeclaration("m"), stmts)).Count)
            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.ConstructorDeclaration(), stmts)).Count)

            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.VoidReturningLambdaExpression({}), stmts)).Count)
            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.ValueReturningLambdaExpression({}), stmts)).Count)

            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.VoidReturningLambdaExpression(_g.IdentifierName("e")), stmts)).Count)
            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.ValueReturningLambdaExpression(_g.IdentifierName("e")), stmts)).Count)

            Assert.Equal(0, _g.GetStatements(_g.WithStatements(_g.IdentifierName("x"), stmts)).Count)
        End Sub

        <Fact>
        Public Sub TestWithStatements_LambdaChanges()
            Dim stmts = {_g.ExpressionStatement(_g.IdentifierName("x")), _g.ExpressionStatement(_g.IdentifierName("y"))}

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.VoidReturningLambdaExpression({}), stmts),
<x>Sub()
    x
    y
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.ValueReturningLambdaExpression({}), stmts),
<x>Function()
    x
    y
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.VoidReturningLambdaExpression(_g.IdentifierName("e")), stmts),
<x>Sub()
    x
    y
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.ValueReturningLambdaExpression(_g.IdentifierName("e")), stmts),
<x>Function()
    x
    y
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.VoidReturningLambdaExpression(stmts), {}),
<x>Sub()
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.ValueReturningLambdaExpression(stmts), {}),
<x>Function()
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.VoidReturningLambdaExpression(_g.IdentifierName("e")), {}),
<x>Sub()
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                _g.WithStatements(_g.ValueReturningLambdaExpression(_g.IdentifierName("e")), {}),
<x>Function()
End Function</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAccessorDeclarations()
            Dim _g = Me._g
            Dim prop = _g.PropertyDeclaration("p", _g.IdentifierName("T"))

            Assert.Equal(2, _g.GetAccessors(prop).Count)

            ' get accessors from property
            Dim getAccessor = _g.GetAccessor(prop, DeclarationKind.GetAccessor)
            Assert.NotNull(getAccessor)
            VerifySyntax(Of AccessorBlockSyntax)(getAccessor,
<x>Get
End Get</x>.Value)

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
<x>Get
End Get</x>.Value)

            ' change accessors
            Dim newProp = _g.ReplaceNode(prop, getAccessor, _g.WithAccessibility(getAccessor, Accessibility.Public))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.GetAccessor(newProp, DeclarationKind.GetAccessor)))

            newProp = _g.ReplaceNode(prop, setAccessor, _g.WithAccessibility(setAccessor, Accessibility.Public))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.GetAccessor(newProp, DeclarationKind.SetAccessor)))
        End Sub

        <Fact>
        Public Sub TestGetAccessorStatements()
            Dim stmts = {_g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))), _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))}

            Dim p = _g.ParameterDeclaration("p", _g.IdentifierName("t"))

            ' get-accessor
            Assert.Equal(0, _g.GetGetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).Count)
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"), getAccessorStatements:=stmts)).Count)

            Assert.Equal(0, _g.GetGetAccessorStatements(_g.IndexerDeclaration({p}, _g.IdentifierName("t"))).Count)
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.IndexerDeclaration({p}, _g.IdentifierName("t"), getAccessorStatements:=stmts)).Count)

            Assert.Equal(0, _g.GetGetAccessorStatements(_g.IdentifierName("x")).Count)

            ' set-accessor
            Assert.Equal(0, _g.GetSetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).Count)
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"), setAccessorStatements:=stmts)).Count)

            Assert.Equal(0, _g.GetSetAccessorStatements(_g.IndexerDeclaration({p}, _g.IdentifierName("t"))).Count)
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.IndexerDeclaration({p}, _g.IdentifierName("t"), setAccessorStatements:=stmts)).Count)

            Assert.Equal(0, _g.GetSetAccessorStatements(_g.IdentifierName("x")).Count)
        End Sub

        <Fact>
        Public Sub TestWithAccessorStatements()
            Dim stmts = {_g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))), _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))}

            Dim p = _g.ParameterDeclaration("p", _g.IdentifierName("t"))

            ' get-accessor
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.WithGetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t")), stmts)).Count)
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.WithGetAccessorStatements(_g.IndexerDeclaration({p}, _g.IdentifierName("t")), stmts)).Count)
            Assert.Equal(0, _g.GetGetAccessorStatements(_g.WithGetAccessorStatements(_g.IdentifierName("x"), stmts)).Count)

            ' set-accessor
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.WithSetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t")), stmts)).Count)
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.WithSetAccessorStatements(_g.IndexerDeclaration({p}, _g.IdentifierName("t")), stmts)).Count)
            Assert.Equal(0, _g.GetSetAccessorStatements(_g.WithSetAccessorStatements(_g.IdentifierName("x"), stmts)).Count)
        End Sub

        Private Sub AssertNamesEqual(expectedNames As String(), actualNodes As IReadOnlyList(Of SyntaxNode))
            Dim actualNames = actualNodes.Select(Function(n) _g.GetName(n)).ToArray()
            Assert.Equal(expectedNames.Length, actualNames.Length)
            Dim expected = String.Join(", ", expectedNames)
            Dim actual = String.Join(", ", actualNames)
            Assert.Equal(expected, actual)
        End Sub

        Private Sub AssertMemberNamesEqual(expectedNames As String(), declaration As SyntaxNode)
            AssertNamesEqual(expectedNames, _g.GetMembers(declaration))
        End Sub

        Private Sub AssertMemberNamesEqual(expectedName As String, declaration As SyntaxNode)
            AssertMemberNamesEqual({expectedName}, declaration)
        End Sub

        <Fact>
        Public Sub TestGetMembers()
            AssertMemberNamesEqual("m", _g.ClassDeclaration("c", members:={_g.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", _g.StructDeclaration("s", members:={_g.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", _g.InterfaceDeclaration("i", members:={_g.MethodDeclaration("m")}))
            AssertMemberNamesEqual("v", _g.EnumDeclaration("e", members:={_g.EnumMember("v")}))
            AssertMemberNamesEqual("c", _g.NamespaceDeclaration("n", declarations:={_g.ClassDeclaration("c")}))
            AssertMemberNamesEqual("c", _g.CompilationUnit(declarations:={_g.ClassDeclaration("c")}))
        End Sub

        <Fact>
        Public Sub TestAddMembers()
            AssertMemberNamesEqual("m", _g.AddMembers(_g.ClassDeclaration("d"), {_g.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", _g.AddMembers(_g.StructDeclaration("s"), {_g.MethodDeclaration("m")}))
            AssertMemberNamesEqual("m", _g.AddMembers(_g.InterfaceDeclaration("i"), {_g.MethodDeclaration("m")}))
            AssertMemberNamesEqual("v", _g.AddMembers(_g.EnumDeclaration("e"), {_g.EnumMember("v")}))
            AssertMemberNamesEqual("n2", _g.AddMembers(_g.NamespaceDeclaration("n"), {_g.NamespaceDeclaration("n2")}))
            AssertMemberNamesEqual("n", _g.AddMembers(_g.CompilationUnit(), {_g.NamespaceDeclaration("n")}))

            AssertMemberNamesEqual({"m", "m2"}, _g.AddMembers(_g.ClassDeclaration("d", members:={_g.MethodDeclaration("m")}), {_g.MethodDeclaration("m2")}))
            AssertMemberNamesEqual({"m", "m2"}, _g.AddMembers(_g.StructDeclaration("s", members:={_g.MethodDeclaration("m")}), {_g.MethodDeclaration("m2")}))
            AssertMemberNamesEqual({"m", "m2"}, _g.AddMembers(_g.InterfaceDeclaration("i", members:={_g.MethodDeclaration("m")}), {_g.MethodDeclaration("m2")}))
            AssertMemberNamesEqual({"v", "v2"}, _g.AddMembers(_g.EnumDeclaration("i", members:={_g.EnumMember("v")}), {_g.EnumMember("v2")}))
            AssertMemberNamesEqual({"n1", "n2"}, _g.AddMembers(_g.NamespaceDeclaration("n", {_g.NamespaceDeclaration("n1")}), {_g.NamespaceDeclaration("n2")}))
            AssertMemberNamesEqual({"n1", "n2"}, _g.AddMembers(_g.CompilationUnit(declarations:={_g.NamespaceDeclaration("n1")}), {_g.NamespaceDeclaration("n2")}))
        End Sub

        <Fact>
        Public Sub TestRemoveMembers()
            TestRemoveAllMembers(_g.ClassDeclaration("d", members:={_g.MethodDeclaration("m")}))
            TestRemoveAllMembers(_g.StructDeclaration("s", members:={_g.MethodDeclaration("m")}))
            TestRemoveAllMembers(_g.InterfaceDeclaration("i", members:={_g.MethodDeclaration("m")}))
            TestRemoveAllMembers(_g.EnumDeclaration("i", members:={_g.EnumMember("v")}))
            TestRemoveAllMembers(_g.AddMembers(_g.NamespaceDeclaration("n", {_g.NamespaceDeclaration("n1")})))
            TestRemoveAllMembers(_g.AddMembers(_g.CompilationUnit(declarations:={_g.NamespaceDeclaration("n1")})))
        End Sub

        Private Sub TestRemoveAllMembers(declaration As SyntaxNode)
            Assert.Equal(0, _g.GetMembers(_g.RemoveNodes(declaration, _g.GetMembers(declaration))).Count)
        End Sub

        Private Sub TestRemoveMember(declaration As SyntaxNode, name As String, remainingNames As String())
            Dim newDecl = _g.RemoveNode(declaration, _g.GetMembers(declaration).First(Function(m) _g.GetName(m) = name))
            AssertMemberNamesEqual(remainingNames, newDecl)
        End Sub

        <Fact>
        Public Sub TestGetBaseAndInterfaceTypes()
            Dim classBI = SyntaxFactory.ParseCompilationUnit(
<x>Class C
    Inherits B
    Implements I
End Class</x>.Value).Members(0)

            Dim baseListBI = _g.GetBaseAndInterfaceTypes(classBI)
            Assert.NotNull(baseListBI)
            Assert.Equal(2, baseListBI.Count)
            Assert.Equal("B", baseListBI(0).ToString())
            Assert.Equal("I", baseListBI(1).ToString())

            Dim ifaceI = SyntaxFactory.ParseCompilationUnit(
<x>Interface I
    Inherits X
    Inherits Y
End Class</x>.Value).Members(0)

            Dim baseListXY = _g.GetBaseAndInterfaceTypes(ifaceI)
            Assert.NotNull(baseListXY)
            Assert.Equal(2, baseListXY.Count)
            Assert.Equal("X", baseListXY(0).ToString())
            Assert.Equal("Y", baseListXY(1).ToString())

            Dim classN = SyntaxFactory.ParseCompilationUnit(
<x>Class C
End Class</x>.Value).Members(0)

            Dim baseListN = _g.GetBaseAndInterfaceTypes(classN)
            Assert.NotNull(baseListN)
            Assert.Equal(0, baseListN.Count)
        End Sub

        <Fact>
        Public Sub TestRemoveBaseAndInterfaceTypes()
            Dim classC = SyntaxFactory.ParseCompilationUnit(
<x>Class C
    Inherits A
    Implements X, Y

End Class</x>.Value).Members(0)

            Dim baseList = _g.GetBaseAndInterfaceTypes(classC)
            Assert.Equal(3, baseList.Count)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(classC, baseList(0)),
<x>Class C
    Implements X, Y

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(classC, baseList(1)),
<x>Class C
    Inherits A
    Implements Y

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(classC, baseList(2)),
<x>Class C
    Inherits A
    Implements X

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(classC, {baseList(1), baseList(2)}),
<x>Class C
    Inherits A

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(classC, baseList),
<x>Class C
End Class</x>.Value)

        End Sub

        <Fact>
        Public Sub TestAddBaseType()
            Dim classC = SyntaxFactory.ParseCompilationUnit(
<x>Class C
End Class</x>.Value).Members(0)

            Dim classCB = SyntaxFactory.ParseCompilationUnit(
<x>Class C
    Inherits B

End Class</x>.Value).Members(0)

            Dim structS = SyntaxFactory.ParseCompilationUnit(
<x>Structure S
End Structure</x>.Value).Members(0)

            Dim ifaceI = SyntaxFactory.ParseCompilationUnit(
<x>Interface I
End Interface</x>.Value).Members(0)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.AddBaseType(classC, _g.IdentifierName("T")),
<x>Class C
    Inherits T

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.AddBaseType(classCB, _g.IdentifierName("T")),
<x>Class C
    Inherits T

End Class</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.AddBaseType(structS, _g.IdentifierName("T")),
<x>Structure S
End Structure</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.AddBaseType(ifaceI, _g.IdentifierName("T")),
<x>Interface I
End Interface</x>.Value)

        End Sub

        <Fact>
        Public Sub TestAddInterfaceType()
            Dim classC = SyntaxFactory.ParseCompilationUnit(
<x>Class C
End Class</x>.Value).Members(0)

            Dim classCB = SyntaxFactory.ParseCompilationUnit(
<x>Class C
    Inherits B

End Class</x>.Value).Members(0)

            Dim classCI = SyntaxFactory.ParseCompilationUnit(
<x>Class C
    Implements I

End Class</x>.Value).Members(0)

            Dim structS = SyntaxFactory.ParseCompilationUnit(
<x>Structure S
End Structure</x>.Value).Members(0)

            Dim ifaceI = SyntaxFactory.ParseCompilationUnit(
<x>Interface I
End Interface</x>.Value).Members(0)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.AddInterfaceType(classC, _g.IdentifierName("T")),
<x>Class C
    Implements T

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.AddInterfaceType(classCB, _g.IdentifierName("T")),
<x>Class C
    Inherits B
    Implements T

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.AddInterfaceType(classCI, _g.IdentifierName("T")),
<x>Class C
    Implements I, T

End Class</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                _g.AddInterfaceType(structS, _g.IdentifierName("T")),
<x>Structure S
    Implements T

End Structure</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                _g.AddInterfaceType(ifaceI, _g.IdentifierName("T")),
<x>Interface I
    Inherits T

End Interface</x>.Value)

        End Sub

        <Fact>
        <WorkItem(5097, "https://github.com/dotnet/roslyn/issues/5097")>
        Public Sub TestAddInterfaceWithEOLs()
            Dim classC = SyntaxFactory.ParseCompilationUnit("
Public Class C
End Class").Members(0)

            VerifySyntaxRaw(Of ClassBlockSyntax)(
                _g.AddInterfaceType(classC, _g.IdentifierName("X")), "
Public Class C
ImplementsXEnd Class")

            Dim interfaceI = SyntaxFactory.ParseCompilationUnit("
Public Interface I
End Interface").Members(0)

            VerifySyntaxRaw(Of InterfaceBlockSyntax)(
                _g.AddInterfaceType(interfaceI, _g.IdentifierName("X")), "
Public Interface I
InheritsXEnd Interface")

            Dim classCX = SyntaxFactory.ParseCompilationUnit("
Public Class C
    Implements X
End Class").Members(0)

            VerifySyntaxRaw(Of ClassBlockSyntax)(
                _g.AddInterfaceType(classCX, _g.IdentifierName("Y")), "
Public Class C
    Implements X,Y
End Class")

            Dim interfaceIX = SyntaxFactory.ParseCompilationUnit("
Public Interface I
    Inherits X
End Interface").Members(0)

            VerifySyntaxRaw(Of InterfaceBlockSyntax)(
                _g.AddInterfaceType(interfaceIX, _g.IdentifierName("Y")), "
Public Interface I
    Inherits X,Y
End Interface")

            Dim classCXY = SyntaxFactory.ParseCompilationUnit("
Public Class C
    Implements X
    Implements Y
End Class").Members(0)

            VerifySyntaxRaw(Of ClassBlockSyntax)(
                _g.AddInterfaceType(classCXY, _g.IdentifierName("Z")), "
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
                _g.AddInterfaceType(interfaceIXY, _g.IdentifierName("Z")), "
Public Interface I
    Inherits X
    Inherits Y
InheritsZEnd Interface")

        End Sub

        <Fact>
        Public Sub TestMultiFieldMembers()
            Dim comp = Compile(
<x>' Comment
Public Class C
    Public Shared X, Y, Z As Integer
End Class</x>.Value)

            Dim symbolC = DirectCast(comp.GlobalNamespace.GetMembers("C").First(), INamedTypeSymbol)
            Dim symbolX = DirectCast(symbolC.GetMembers("X").First(), IFieldSymbol)
            Dim symbolY = DirectCast(symbolC.GetMembers("Y").First(), IFieldSymbol)
            Dim symbolZ = DirectCast(symbolC.GetMembers("Z").First(), IFieldSymbol)

            Dim declC = _g.GetDeclaration(symbolC.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())
            Dim declX = _g.GetDeclaration(symbolX.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())
            Dim declY = _g.GetDeclaration(symbolY.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())
            Dim declZ = _g.GetDeclaration(symbolZ.DeclaringSyntaxReferences.Select(Function(x) x.GetSyntax()).First())

            Assert.Equal(SyntaxKind.ModifiedIdentifier, declX.Kind)
            Assert.Equal(SyntaxKind.ModifiedIdentifier, declY.Kind)
            Assert.Equal(SyntaxKind.ModifiedIdentifier, declZ.Kind)

            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(declX))
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(declY))
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(declZ))

            Assert.NotNull(_g.GetType(declX))
            Assert.Equal("Integer", _g.GetType(declX).ToString())
            Assert.Equal("X", _g.GetName(declX))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(declX))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(declX))

            Assert.NotNull(_g.GetType(declY))
            Assert.Equal("Integer", _g.GetType(declY).ToString())
            Assert.Equal("Y", _g.GetName(declY))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(declY))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(declY))

            Assert.NotNull(_g.GetType(declZ))
            Assert.Equal("Integer", _g.GetType(declZ).ToString())
            Assert.Equal("Z", _g.GetName(declZ))
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(declZ))
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(declZ))

            Dim xTypedT = _g.WithType(declX, _g.IdentifierName("T"))
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xTypedT))
            Assert.Equal(SyntaxKind.FieldDeclaration, xTypedT.Kind)
            Assert.Equal("T", _g.GetType(xTypedT).ToString())

            Dim xNamedQ = _g.WithName(declX, "Q")
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xNamedQ))
            Assert.Equal(SyntaxKind.FieldDeclaration, xNamedQ.Kind)
            Assert.Equal("Q", _g.GetName(xNamedQ).ToString())

            Dim xInitialized = _g.WithExpression(declX, _g.IdentifierName("e"))
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xInitialized))
            Assert.Equal(SyntaxKind.FieldDeclaration, xInitialized.Kind)
            Assert.Equal("e", _g.GetExpression(xInitialized).ToString())

            Dim xPrivate = _g.WithAccessibility(declX, Accessibility.Private)
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xPrivate))
            Assert.Equal(SyntaxKind.FieldDeclaration, xPrivate.Kind)
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(xPrivate))

            Dim xReadOnly = _g.WithModifiers(declX, DeclarationModifiers.ReadOnly)
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xReadOnly))
            Assert.Equal(SyntaxKind.FieldDeclaration, xReadOnly.Kind)
            Assert.Equal(DeclarationModifiers.ReadOnly, _g.GetModifiers(xReadOnly))

            Dim xAttributed = _g.AddAttributes(declX, _g.Attribute("A"))
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xAttributed))
            Assert.Equal(SyntaxKind.FieldDeclaration, xAttributed.Kind)
            Assert.Equal(1, _g.GetAttributes(xAttributed).Count)
            Assert.Equal("<A>", _g.GetAttributes(xAttributed)(0).ToString())

            Dim membersC = _g.GetMembers(declC)
            Assert.Equal(3, membersC.Count)
            Assert.Equal(declX, membersC(0))
            Assert.Equal(declY, membersC(1))
            Assert.Equal(declZ, membersC(2))

            ' create new class from existing members, now appear as separate declarations
            VerifySyntax(Of ClassBlockSyntax)(
                _g.ClassDeclaration("C", members:={declX, declY}),
<x>Class C

    Public Shared X As Integer

    Public Shared Y As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertMembers(declC, 0, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Dim A As T

    Public Shared X, Y, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertMembers(declC, 1, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Public Shared X As Integer

    Dim A As T

    Public Shared Y, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertMembers(declC, 2, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Public Shared X, Y As Integer

    Dim A As T

    Public Shared Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertMembers(declC, 3, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Public Shared X, Y, Z As Integer

    Dim A As T
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declX, _g.WithType(declX, _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Public Shared X As T

    Public Shared Y, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declX, _g.WithExpression(declX, _g.IdentifierName("e"))),
<x>' Comment
Public Class C

    Public Shared X As Integer = e

    Public Shared Y, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declX, _g.WithName(declX, "Q")),
<x>' Comment
Public Class C

    Public Shared Q, Y, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declY, _g.WithType(declY, _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Public Shared X As Integer

    Public Shared Y As T

    Public Shared Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declZ, _g.WithType(declZ, _g.IdentifierName("T"))),
<x>' Comment
Public Class C

    Public Shared X, Y As Integer

    Public Shared Z As T
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declX, declZ),
<x>' Comment
Public Class C

    Public Shared Z, Y, Z As Integer
End Class</x>.Value)

            ' Removing 
            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(declC, declX),
<x>' Comment
Public Class C

    Public Shared Y, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(declC, declY),
<x>' Comment
Public Class C

    Public Shared X, Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(declC, declZ),
<x>' Comment
Public Class C

    Public Shared X, Y As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declX, declY}),
<x>' Comment
Public Class C

    Public Shared Z As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declX, declZ}),
<x>' Comment
Public Class C

    Public Shared Y As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declY, declZ}),
<x>' Comment
Public Class C

    Public Shared X As Integer
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declX, declY, declZ}),
<x>' Comment
Public Class C
End Class</x>.Value)

        End Sub

        <Fact>
        Public Sub TestMultiAttributes()
            Dim comp = Compile(
<x>' Comment
&lt;X, Y, Z&gt;
Public Class C
End Class</x>.Value)

            Dim symbolC = DirectCast(comp.GlobalNamespace.GetMembers("C").First(), INamedTypeSymbol)
            Dim declC = _g.GetDeclaration(symbolC.DeclaringSyntaxReferences.First().GetSyntax())

            Dim attrs = _g.GetAttributes(declC)
            Assert.Equal(3, attrs.Count)
            Dim declX = attrs(0)
            Dim declY = attrs(1)
            Dim declZ = attrs(2)
            Assert.Equal(SyntaxKind.Attribute, declX.Kind)
            Assert.Equal(SyntaxKind.Attribute, declY.Kind)
            Assert.Equal(SyntaxKind.Attribute, declZ.Kind)
            Assert.Equal("X", _g.GetName(declX))
            Assert.Equal("Y", _g.GetName(declY))
            Assert.Equal("Z", _g.GetName(declZ))

            Dim xNamedQ = _g.WithName(declX, "Q")
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(xNamedQ))
            Assert.Equal(SyntaxKind.AttributeList, xNamedQ.Kind)
            Assert.Equal("<Q>", xNamedQ.ToString())

            Dim xWithArg = _g.AddAttributeArguments(declX, {_g.AttributeArgument(_g.IdentifierName("e"))})
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(xWithArg))
            Assert.Equal(SyntaxKind.AttributeList, xWithArg.Kind)
            Assert.Equal("<X(e)>", xWithArg.ToString())

            ' inserting
            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertAttributes(declC, 0, _g.Attribute("A")),
<x>' Comment
&lt;A&gt;
&lt;X, Y, Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertAttributes(declC, 1, _g.Attribute("A")),
<x>' Comment
&lt;X&gt;
&lt;A&gt;
&lt;Y, Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertAttributes(declC, 2, _g.Attribute("A")),
<x>' Comment
&lt;X, Y&gt;
&lt;A&gt;
&lt;Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.InsertAttributes(declC, 3, _g.Attribute("A")),
<x>' Comment
&lt;X, Y, Z&gt;
&lt;A&gt;
Public Class C
End Class</x>.Value)

            ' replacing
            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declX, _g.Attribute("A")),
<x>' Comment
&lt;A, Y, Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.ReplaceNode(declC, declX, _g.InsertAttributeArguments(declX, 0, {_g.AttributeArgument(_g.IdentifierName("e"))})),
<x>' Comment
&lt;X(e), Y, Z&gt;
Public Class C
End Class</x>.Value)

            ' removing
            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(declC, declX),
<x>' Comment
&lt;Y, Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(declC, declY),
<x>' Comment
&lt;X, Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNode(declC, declZ),
<x>' Comment
&lt;X, Y&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declX, declY}),
<x>' Comment
&lt;Z&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declX, declZ}),
<x>' Comment
&lt;Y&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declY, declZ}),
<x>' Comment
&lt;X&gt;
Public Class C
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                _g.RemoveNodes(declC, {declX, declY, declZ}),
<x>' Comment
Public Class C
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestMultiImports()
            Dim comp = Compile(
<x>' Comment
Imports X, Y, Z
</x>.Value)

            Dim declCU = comp.SyntaxTrees.First().GetRoot()

            Assert.Equal(SyntaxKind.CompilationUnit, declCU.Kind)
            Dim imps = _g.GetNamespaceImports(declCU)
            Assert.Equal(3, imps.Count)
            Dim declX = imps(0)
            Dim declY = imps(1)
            Dim declZ = imps(2)

            Dim xRenamedQ = _g.WithName(declX, "Q")
            Assert.Equal(DeclarationKind.NamespaceImport, _g.GetDeclarationKind(xRenamedQ))
            Assert.Equal(SyntaxKind.ImportsStatement, xRenamedQ.Kind)
            Assert.Equal("Imports Q", xRenamedQ.ToString())

            ' inserting
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.InsertNamespaceImports(declCU, 0, _g.NamespaceImportDeclaration("N")),
<x>' Comment
Imports N
Imports X, Y, Z
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.InsertNamespaceImports(declCU, 1, _g.NamespaceImportDeclaration("N")),
<x>' Comment
Imports X
Imports N
Imports Y, Z
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.InsertNamespaceImports(declCU, 2, _g.NamespaceImportDeclaration("N")),
<x>' Comment
Imports X, Y
Imports N
Imports Z
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                _g.InsertNamespaceImports(declCU, 3, _g.NamespaceImportDeclaration("N")),
<x>' Comment
Imports X, Y, Z
Imports N
</x>.Value)

            ' Replacing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.ReplaceNode(declCU, declX, _g.NamespaceImportDeclaration("N")),
<x>' Comment
Imports N, Y, Z
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNode(declCU, declX),
<x>' Comment
Imports Y, Z
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNode(declCU, declY),
<x>' Comment
Imports X, Z
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNode(declCU, declZ),
<x>' Comment
Imports X, Y
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNodes(declCU, {declX, declY}),
<x>' Comment
Imports Z
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNodes(declCU, {declX, declZ}),
<x>' Comment
Imports Y
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNodes(declCU, {declY, declZ}),
<x>' Comment
Imports X
</x>.Value)

            ' Removing
            VerifySyntax(Of CompilationUnitSyntax)(
                _g.RemoveNodes(declCU, {declX, declY, declZ}),
<x>' Comment
</x>.Value)

        End Sub
#End Region

    End Class
End Namespace
