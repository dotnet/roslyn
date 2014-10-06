Imports System.Globalization
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.CodeGeneration
    Public Class SyntaxGeneratorTests
        Private ReadOnly g As SyntaxGenerator = SyntaxGenerator.GetGenerator(New CustomWorkspace(), LanguageNames.VisualBasic)

        Private ReadOnly emptyCompilation As VBCompilation = VBCompilation.Create("empty", references:={TestReferences.NetFx.v4_0_30319.mscorlib})

        Private ienumerableInt As INamedTypeSymbol

        Public Sub New()
            Me.ienumerableInt = emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(emptyCompilation.GetSpecialType(SpecialType.System_Int32))
        End Sub

        Private Sub VerifySyntax(Of TSyntax As SyntaxNode)(type As SyntaxNode, expectedText As String)
            Assert.IsAssignableFrom(GetType(TSyntax), type)
            Dim normalized = type.NormalizeWhitespace().ToString()
            Dim fixedExpectations = expectedText.Replace(vbLf, vbCrLf)
            Assert.Equal(fixedExpectations, normalized)
        End Sub

        <Fact>
        Public Sub TestLiteralExpressions()
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0), "0")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(1), "1")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(-1), "-1")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Integer.MinValue), "Global.System.Int32.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Integer.MaxValue), "Global.System.Int32.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0L), "0L")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(1L), "1L")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(-1L), "-1L")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Long.MinValue), "Global.System.Int64.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Long.MaxValue), "Global.System.Int64.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0UL), "0UL")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(1UL), "1UL")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(ULong.MinValue), "0UL")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(ULong.MaxValue), "Global.System.UInt64.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0.0F), "0F")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(1.0F), "1F")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(-1.0F), "-1F")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Single.MinValue), "Global.System.Single.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Single.MaxValue), "Global.System.Single.MaxValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Single.Epsilon), "Global.System.Single.Epsilon")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Single.NaN), "Global.System.Single.NaN")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Single.NegativeInfinity), "Global.System.Single.NegativeInfinity")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Single.PositiveInfinity), "Global.System.Single.PositiveInfinity")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0.0), "0R")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(1.0), "1R")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(-1.0), "-1R")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Double.MinValue), "Global.System.Double.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Double.MaxValue), "Global.System.Double.MaxValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Double.Epsilon), "Global.System.Double.Epsilon")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Double.NaN), "Global.System.Double.NaN")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Double.NegativeInfinity), "Global.System.Double.NegativeInfinity")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Double.PositiveInfinity), "Global.System.Double.PositiveInfinity")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0D), "0D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(0.00D), "0.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("1.00", CultureInfo.InvariantCulture)), "1.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("-1.00", CultureInfo.InvariantCulture)), "-1.00D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("1.0000000000", CultureInfo.InvariantCulture)), "1.0000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("0.000000", CultureInfo.InvariantCulture)), "0.000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("0.0000000", CultureInfo.InvariantCulture)), "0.0000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(1000000000D), "1000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(123456789.123456789D), "123456789.123456789D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("1E-28", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000001D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("0E-28", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("1E-29", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(Decimal.Parse("-1E-29", NumberStyles.Any, CultureInfo.InvariantCulture)), "0.0000000000000000000000000000D")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Decimal.MinValue), "Global.System.Decimal.MinValue")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.LiteralExpression(Decimal.MaxValue), "Global.System.Decimal.MaxValue")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression("c"c), """c""c")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression("str"), """str""")

            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(True), "True")
            VerifySyntax(Of LiteralExpressionSyntax)(g.LiteralExpression(False), "False")
        End Sub

        <Fact>
        Public Sub TestNameExpressions()
            VerifySyntax(Of IdentifierNameSyntax)(g.IdentifierName("x"), "x")
            VerifySyntax(Of QualifiedNameSyntax)(g.QualifiedName(g.IdentifierName("x"), g.IdentifierName("y")), "x.y")
            VerifySyntax(Of QualifiedNameSyntax)(g.DottedName("x.y"), "x.y")

            VerifySyntax(Of GenericNameSyntax)(g.GenericName("x", g.IdentifierName("y")), "x(Of y)")
            VerifySyntax(Of GenericNameSyntax)(g.GenericName("x", g.IdentifierName("y"), g.IdentifierName("z")), "x(Of y, z)")

            ' convert identifer name into generic name
            VerifySyntax(Of GenericNameSyntax)(g.WithTypeArguments(g.IdentifierName("x"), g.IdentifierName("y")), "x(Of y)")

            ' convert qualified name into qualified generic name
            VerifySyntax(Of QualifiedNameSyntax)(g.WithTypeArguments(g.DottedName("x.y"), g.IdentifierName("z")), "x.y(Of z)")

            ' convert member access expression into generic member access expression
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.WithTypeArguments(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x.y(Of z)")

            ' convert existing generic name into a different generic name
            Dim gname = g.WithTypeArguments(g.IdentifierName("x"), g.IdentifierName("y"))
            VerifySyntax(Of GenericNameSyntax)(gname, "x(Of y)")
            VerifySyntax(Of GenericNameSyntax)(g.WithTypeArguments(gname, g.IdentifierName("z")), "x(Of z)")
        End Sub

        <Fact>
        Public Sub TestTypeExpressions()
            ' these are all type syntax too
            VerifySyntax(Of TypeSyntax)(g.IdentifierName("x"), "x")
            VerifySyntax(Of TypeSyntax)(g.QualifiedName(g.IdentifierName("x"), g.IdentifierName("y")), "x.y")
            VerifySyntax(Of TypeSyntax)(g.DottedName("x.y"), "x.y")
            VerifySyntax(Of TypeSyntax)(g.GenericName("x", g.IdentifierName("y")), "x(Of y)")
            VerifySyntax(Of TypeSyntax)(g.GenericName("x", g.IdentifierName("y"), g.IdentifierName("z")), "x(Of y, z)")

            VerifySyntax(Of TypeSyntax)(g.ArrayTypeExpression(g.IdentifierName("x")), "x()")
            VerifySyntax(Of TypeSyntax)(g.ArrayTypeExpression(g.ArrayTypeExpression(g.IdentifierName("x"))), "x()()")
            VerifySyntax(Of TypeSyntax)(g.NullableTypeExpression(g.IdentifierName("x")), "x?")
            VerifySyntax(Of TypeSyntax)(g.NullableTypeExpression(g.NullableTypeExpression(g.IdentifierName("x"))), "x?")
        End Sub

        <Fact>
        Public Sub TestSpecialTypeExpression()
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Byte), "Byte")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_SByte), "SByte")

            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Int16), "Short")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_UInt16), "UShort")

            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Int32), "Integer")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_UInt32), "UInteger")

            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Int64), "Long")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_UInt64), "ULong")

            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Single), "Single")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Double), "Double")

            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Char), "Char")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_String), "String")

            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Object), "Object")
            VerifySyntax(Of TypeSyntax)(g.TypeExpression(SpecialType.System_Decimal), "Decimal")
        End Sub

        <Fact>
        Public Sub TestSymbolTypeExpressions()
            Dim genericType = emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            VerifySyntax(Of QualifiedNameSyntax)(g.TypeExpression(genericType), "Global.System.Collections.Generic.IEnumerable(Of T)")

            Dim arrayType = emptyCompilation.CreateArrayTypeSymbol(emptyCompilation.GetSpecialType(SpecialType.System_Int32))
            VerifySyntax(Of ArrayTypeSyntax)(g.TypeExpression(arrayType), "System.Int32()")
        End Sub

        <Fact>
        Public Sub TestMathAndLogicExpressions()
            VerifySyntax(Of UnaryExpressionSyntax)(g.NegateExpression(g.IdentifierName("x")), "-(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) + (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.SubtractExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) - (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.MultiplyExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) * (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.DivideExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) / (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.ModuloExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) Mod (y)")

            VerifySyntax(Of UnaryExpressionSyntax)(g.BitwiseNotExpression(g.IdentifierName("x")), "Not(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.BitwiseAndExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) And (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.BitwiseOrExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) Or (y)")

            VerifySyntax(Of UnaryExpressionSyntax)(g.LogicalNotExpression(g.IdentifierName("x")), "Not(x)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.LogicalAndExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) AndAlso (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.LogicalOrExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) OrElse (y)")
        End Sub

        <Fact>
        Public Sub TestEqualityAndInequalityExpressions()
            VerifySyntax(Of BinaryExpressionSyntax)(g.ReferenceEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) Is (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.ValueEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) = (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(g.ReferenceNotEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) IsNot (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.ValueNotEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) <> (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(g.LessThanExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) < (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.LessThanOrEqualExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) <= (y)")

            VerifySyntax(Of BinaryExpressionSyntax)(g.GreaterThanExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) > (y)")
            VerifySyntax(Of BinaryExpressionSyntax)(g.GreaterThanOrEqualExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) >= (y)")
        End Sub

        <Fact>
        Public Sub TestConditionalExpressions()
            VerifySyntax(Of BinaryConditionalExpressionSyntax)(g.CoalesceExpression(g.IdentifierName("x"), g.IdentifierName("y")), "If(x, y)")
            VerifySyntax(Of TernaryConditionalExpressionSyntax)(g.ConditionalExpression(g.IdentifierName("x"), g.IdentifierName("y"), g.IdentifierName("z")), "If(x, y, z)")
        End Sub

        <Fact>
        Public Sub TestMemberAccessExpressions()
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), "x.y")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.IdentifierName("x"), "y"), "x.y")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x.y.z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x(y).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x(y).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "((x) + (y)).z")
            VerifySyntax(Of MemberAccessExpressionSyntax)(g.MemberAccessExpression(g.NegateExpression(g.IdentifierName("x")), g.IdentifierName("y")), "(-(x)).y")
        End Sub

        <Fact>
        Public Sub TestObjectCreationExpressions()
            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                g.ObjectCreationExpression(g.IdentifierName("x")),
                "New x()")

            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                g.ObjectCreationExpression(g.IdentifierName("x"), g.IdentifierName("y")),
                "New x(y)")

            Dim intType = emptyCompilation.GetSpecialType(SpecialType.System_Int32)
            Dim listType = emptyCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
            Dim listOfIntType = listType.Construct(intType)

            VerifySyntax(Of ObjectCreationExpressionSyntax)(
                g.ObjectCreationExpression(listOfIntType, g.IdentifierName("y")),
                "New Global.System.Collections.Generic.List(Of System.Int32)(y)")
        End Sub

        <Fact>
        Public Sub TestElementAccessExpressions()
            VerifySyntax(Of InvocationExpressionSyntax)(
                g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")),
                "x(y)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y"), g.IdentifierName("z")),
                "x(y, z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                g.ElementAccessExpression(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "x.y(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                g.ElementAccessExpression(g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "x(y)(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                g.ElementAccessExpression(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "x(y)(z)")

            VerifySyntax(Of InvocationExpressionSyntax)(
                g.ElementAccessExpression(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "((x) + (y))(z)")
        End Sub

        <Fact>
        Public Sub TestCastAndConvertExpressions()
            VerifySyntax(Of DirectCastExpressionSyntax)(g.CastExpression(g.IdentifierName("x"), g.IdentifierName("y")), "DirectCast(y, x)")
            VerifySyntax(Of CTypeExpressionSyntax)(g.ConvertExpression(g.IdentifierName("x"), g.IdentifierName("y")), "CType(y, x)")
        End Sub

        <Fact>
        Public Sub TestIsAndAsExpressions()
            VerifySyntax(Of TypeOfExpressionSyntax)(g.IsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "TypeOf(x) Is y")
            VerifySyntax(Of TryCastExpressionSyntax)(g.AsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "TryCast(x, y)")
        End Sub

        <Fact>
        Public Sub TestInvocationExpressions()
            ' without explicit arguments
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.IdentifierName("x")), "x()")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y")), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y"), g.IdentifierName("z")), "x(y, z)")

            ' using explicit arguments
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.IdentifierName("x"), g.Argument(g.IdentifierName("y"))), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.IdentifierName("x"), g.Argument(RefKind.Ref, g.IdentifierName("y"))), "x(y)")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.IdentifierName("x"), g.Argument(RefKind.Out, g.IdentifierName("y"))), "x(y)")

            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "x.y()")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "x(y)()")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "x(y)()")
            VerifySyntax(Of InvocationExpressionSyntax)(g.InvocationExpression(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "((x) + (y))()")
        End Sub

        <Fact>
        Public Sub TestAssignmentStatement()
            VerifySyntax(Of AssignmentStatementSyntax)(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y")), "x = y")
        End Sub

        <Fact>
        Public Sub TestExpressionStatement()
            VerifySyntax(Of ExpressionStatementSyntax)(g.ExpressionStatement(g.IdentifierName("x")), "x")
            VerifySyntax(Of ExpressionStatementSyntax)(g.ExpressionStatement(g.InvocationExpression(g.IdentifierName("x"))), "x()")
        End Sub

        <Fact>
        Public Sub TestLocalDeclarationStatements()
            VerifySyntax(Of LocalDeclarationStatementSyntax)(g.LocalDeclarationStatement(g.IdentifierName("x"), "y"), "Dim y As x")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(g.LocalDeclarationStatement(g.IdentifierName("x"), "y", g.IdentifierName("z")), "Dim y As x = z")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(g.LocalDeclarationStatement("y", g.IdentifierName("z")), "Dim y = z")

            VerifySyntax(Of LocalDeclarationStatementSyntax)(g.LocalDeclarationStatement(g.IdentifierName("x"), "y", isConst:=True), "Const y As x")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(g.LocalDeclarationStatement(g.IdentifierName("x"), "y", g.IdentifierName("z"), isConst:=True), "Const y As x = z")
            VerifySyntax(Of LocalDeclarationStatementSyntax)(g.LocalDeclarationStatement(DirectCast(Nothing, SyntaxNode), "y", g.IdentifierName("z"), isConst:=True), "Const y = z")
        End Sub

        <Fact>
        Public Sub TestReturnStatements()
            VerifySyntax(Of ReturnStatementSyntax)(g.ReturnStatement(), "Return")
            VerifySyntax(Of ReturnStatementSyntax)(g.ReturnStatement(g.IdentifierName("x")), "Return x")
        End Sub

        <Fact>
        Public Sub TestThrowStatements()
            VerifySyntax(Of ThrowStatementSyntax)(g.ThrowStatement(), "Throw")
            VerifySyntax(Of ThrowStatementSyntax)(g.ThrowStatement(g.IdentifierName("x")), "Throw x")
        End Sub

        <Fact>
        Public Sub TestIfStatements()
            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"), New SyntaxNode() {}),
<x>If x Then
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"), Nothing),
<x>If x Then
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"), New SyntaxNode() {}, New SyntaxNode() {}),
<x>If x Then
Else
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"),
                    {g.IdentifierName("y")}),
<x>If x Then
    y
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"),
                    {g.IdentifierName("y")},
                    {g.IdentifierName("z")}),
<x>If x Then
    y
Else
    z
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"),
                    {g.IdentifierName("y")},
                    {g.IfStatement(g.IdentifierName("p"), {g.IdentifierName("q")})}),
<x>If x Then
    y
ElseIf p Then
    q
End If</x>.Value)

            VerifySyntax(Of MultiLineIfBlockSyntax)(
                g.IfStatement(g.IdentifierName("x"),
                    {g.IdentifierName("y")},
                    g.IfStatement(g.IdentifierName("p"),
                        {g.IdentifierName("q")},
                        {g.IdentifierName("z")})),
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
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        {g.IdentifierName("z")})),
<x>Select x
    Case y
        z
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(
                        {g.IdentifierName("y"), g.IdentifierName("p"), g.IdentifierName("q")},
                        {g.IdentifierName("z")})),
<x>Select x
    Case y, p, q
        z
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        {g.IdentifierName("z")}),
                    g.SwitchSection(g.IdentifierName("a"),
                        {g.IdentifierName("b")})),
<x>Select x
    Case y
        z
    Case a
        b
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        {g.IdentifierName("z")}),
                    g.DefaultSwitchSection(
                        {g.IdentifierName("b")})),
<x>Select x
    Case y
        z
    Case Else
        b
End Select</x>.Value)

            VerifySyntax(Of SelectBlockSyntax)(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        {g.ExitSwitchStatement()})),
<x>Select x
    Case y
        Exit Select
End Select</x>.Value)
        End Sub

        <Fact>
        Public Sub TestUsingStatements()
            VerifySyntax(Of UsingBlockSyntax)(
                g.UsingStatement(g.IdentifierName("x"), {g.IdentifierName("y")}),
<x>Using x
    y
End Using</x>.Value)

            VerifySyntax(Of UsingBlockSyntax)(
                g.UsingStatement("x", g.IdentifierName("y"), {g.IdentifierName("z")}),
<x>Using x = y
    z
End Using</x>.Value)

            VerifySyntax(Of UsingBlockSyntax)(
                g.UsingStatement(g.IdentifierName("x"), "y", g.IdentifierName("z"), {g.IdentifierName("q")}),
<x>Using y As x = z
    q
End Using</x>.Value)
        End Sub

        <Fact>
        Public Sub TestTryCatchStatements()

            VerifySyntax(Of TryBlockSyntax)(
                g.TryCatchStatement(
                    {g.IdentifierName("x")},
                    g.CatchClause(g.IdentifierName("y"), "z",
                        {g.IdentifierName("a")})),
<x>Try
    x
Catch z As y
    a
End Try</x>.Value)

            VerifySyntax(Of TryBlockSyntax)(
                g.TryCatchStatement(
                    {g.IdentifierName("s")},
                    g.CatchClause(g.IdentifierName("x"), "y",
                        {g.IdentifierName("z")}),
                    g.CatchClause(g.IdentifierName("a"), "b",
                        {g.IdentifierName("c")})),
<x>Try
    s
Catch y As x
    z
Catch b As a
    c
End Try</x>.Value)

            VerifySyntax(Of TryBlockSyntax)(
                g.TryCatchStatement(
                    {g.IdentifierName("s")},
                    {g.CatchClause(g.IdentifierName("x"), "y",
                        {g.IdentifierName("z")})},
                    {g.IdentifierName("a")}),
<x>Try
    s
Catch y As x
    z
Finally
    a
End Try</x>.Value)

            VerifySyntax(Of TryBlockSyntax)(
                g.TryFinallyStatement(
                    {g.IdentifierName("x")},
                    {g.IdentifierName("a")}),
<x>Try
    x
Finally
    a
End Try</x>.Value)

        End Sub

        <Fact>
        Public Sub TestWhileStatements()
            VerifySyntax(Of WhileBlockSyntax)(
                g.WhileStatement(g.IdentifierName("x"), {g.IdentifierName("y")}),
<x>While x
    y
End While</x>.Value)

            VerifySyntax(Of WhileBlockSyntax)(
                g.WhileStatement(g.IdentifierName("x"), Nothing),
<x>While x
End While</x>.Value)
        End Sub

        <Fact>
        Public Sub TestLambdaExpressions()
            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression("x", g.IdentifierName("y")),
                <x>Function(x) y</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression({g.LambdaParameter("x"), g.LambdaParameter("y")}, g.IdentifierName("z")),
                <x>Function(x, y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression(New SyntaxNode() {}, g.IdentifierName("y")),
                <x>Function() y</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression("x", g.IdentifierName("y")),
                <x>Sub(x) y</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression({g.LambdaParameter("x"), g.LambdaParameter("y")}, g.IdentifierName("z")),
                <x>Sub(x, y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression(New SyntaxNode() {}, g.IdentifierName("y")),
                <x>Sub() y</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression("x", {g.ReturnStatement(g.IdentifierName("y"))}),
<x>Function(x)
    Return y
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression({g.LambdaParameter("x"), g.LambdaParameter("y")}, {g.ReturnStatement(g.IdentifierName("z"))}),
<x>Function(x, y)
    Return z
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression(New SyntaxNode() {}, {g.ReturnStatement(g.IdentifierName("y"))}),
<x>Function()
    Return y
End Function</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression("x", {g.IdentifierName("y")}),
<x>Sub(x)
    y
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression({g.LambdaParameter("x"), g.LambdaParameter("y")}, {g.IdentifierName("z")}),
<x>Sub(x, y)
    z
End Sub</x>.Value)

            VerifySyntax(Of MultiLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression(New SyntaxNode() {}, {g.IdentifierName("y")}),
<x>Sub()
    y
End Sub</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression({g.LambdaParameter("x", g.IdentifierName("y"))}, g.IdentifierName("z")),
                <x>Function(x As y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.ValueReturningLambdaExpression({g.LambdaParameter("x", g.IdentifierName("y")), g.LambdaParameter("a", g.IdentifierName("b"))}, g.IdentifierName("z")),
                <x>Function(x As y, a As b) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression({g.LambdaParameter("x", g.IdentifierName("y"))}, g.IdentifierName("z")),
                <x>Sub(x As y) z</x>.Value)

            VerifySyntax(Of SingleLineLambdaExpressionSyntax)(
                g.VoidReturningLambdaExpression({g.LambdaParameter("x", g.IdentifierName("y")), g.LambdaParameter("a", g.IdentifierName("b"))}, g.IdentifierName("z")),
                <x>Sub(x As y, a As b) z</x>.Value)
        End Sub

        <Fact>
        Public Sub TestFieldDeclarations()
            VerifySyntax(Of FieldDeclarationSyntax)(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32)),
                <x>Dim fld As Integer</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), initializer:=g.LiteralExpression(0)),
                <x>Dim fld As Integer = 0</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), accessibility:=Accessibility.Public),
                <x>Public fld As Integer</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), modifiers:=DeclarationModifiers.Static Or DeclarationModifiers.ReadOnly),
                <x>Shared ReadOnly fld As Integer</x>.Value)
        End Sub

        <Fact>
        Public Sub TestMethodDeclarations()
            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m"),
<x>Sub m()
End Sub</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m", typeParameters:={"x", "y"}),
<x>Sub m(Of x, y)()
End Sub</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m", returnType:=g.IdentifierName("x")),
<x>Function m() As x
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m", returnType:=g.IdentifierName("x"), statements:={g.ReturnStatement(g.IdentifierName("y"))}),
<x>Function m() As x
    Return y
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m", parameters:={g.ParameterDeclaration("z", g.IdentifierName("y"))}, returnType:=g.IdentifierName("x")),
<x>Function m(z As y) As x
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m", parameters:={g.ParameterDeclaration("z", g.IdentifierName("y"), g.IdentifierName("a"))}, returnType:=g.IdentifierName("x")),
<x>Function m(Optional z As y = a) As x
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.MethodDeclaration("m", returnType:=g.IdentifierName("x"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.None),
<x>Public Function m() As x
End Function</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                g.MethodDeclaration("m", returnType:=g.IdentifierName("x"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Abstract),
<x>Public MustInherit Function m() As x</x>.Value)
        End Sub

        <Fact>
        Public Sub TestPropertyDeclarations()
            VerifySyntax(Of PropertyStatementSyntax)(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.ReadOnly),
<x>MustInherit ReadOnly Property p As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly),
<x>ReadOnly Property p As x
    Get
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
<x>MustInherit Property p As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly, getterStatements:={g.IdentifierName("y")}),
<x>ReadOnly Property p As x
    Get
        y
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.PropertyDeclaration("p", g.IdentifierName("x"), setterStatements:={g.IdentifierName("y")}),
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
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract + DeclarationModifiers.ReadOnly),
<x>Default MustInherit ReadOnly Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
<x>Default MustInherit Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly),
<x>Default ReadOnly Property Item(z As y) As x
    Get
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"), modifiers:=DeclarationModifiers.ReadOnly,
                    getterStatements:={g.IdentifierName("a")}),
<x>Default ReadOnly Property Item(z As y) As x
    Get
        a
    End Get
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"), modifiers:=DeclarationModifiers.None),
<x>Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"),
                    setterStatements:={g.IdentifierName("a")}),
<x>Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
        a
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"),
                    getterStatements:={g.IdentifierName("a")}, setterStatements:={g.IdentifierName("b")}),
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
        Public Sub TestConstructorDeclaration()
            VerifySyntax(Of ConstructorBlockSyntax)(
                g.ConstructorDeclaration("c"),
<x>Sub New()
End Sub</x>.Value)

            VerifySyntax(Of ConstructorBlockSyntax)(
                g.ConstructorDeclaration("c", accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Static),
<x>Public Shared Sub New()
End Sub</x>.Value)

            VerifySyntax(Of ConstructorBlockSyntax)(
                g.ConstructorDeclaration("c", parameters:={g.ParameterDeclaration("p", g.IdentifierName("t"))}),
<x>Sub New(p As t)
End Sub</x>.Value)

            VerifySyntax(Of ConstructorBlockSyntax)(
                g.ConstructorDeclaration("c",
                    parameters:={g.ParameterDeclaration("p", g.IdentifierName("t"))},
                    baseConstructorArguments:={g.IdentifierName("p")}),
<x>Sub New(p As t)
    MyBase.New(p)
End Sub</x>.Value)
        End Sub

        <Fact>
        Public Sub TestClassDeclarations()
            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c"),
<x>Class c
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", typeParameters:={"x", "y"}),
<x>Class c(Of x, y)
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", accessibility:=Accessibility.Public),
<x>Public Class c
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", baseType:=g.IdentifierName("x")),
<x>Class c
    Inherits x

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", interfaceTypes:={g.IdentifierName("x")}),
<x>Class c
    Implements x

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", baseType:=g.IdentifierName("x"), interfaceTypes:={g.IdentifierName("y"), g.IdentifierName("z")}),
<x>Class c
    Inherits x
    Implements y, z

End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", interfaceTypes:={}),
<x>Class c
End Class</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.ClassDeclaration("c", members:={g.FieldDeclaration("y", type:=g.IdentifierName("x"))}),
<x>Class c

    Dim y As x
End Class</x>.Value)

        End Sub

        <Fact>
        Public Sub TestStructDeclarations()
            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s"),
<x>Structure s
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", typeParameters:={"x", "y"}),
<x>Structure s(Of x, y)
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Partial),
<x>Public Partial Structure s
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", interfaceTypes:={g.IdentifierName("x")}),
<x>Structure s
    Implements x

End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", interfaceTypes:={g.IdentifierName("x"), g.IdentifierName("y")}),
<x>Structure s
    Implements x, y

End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", interfaceTypes:={}),
<x>Structure s
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", members:={g.FieldDeclaration("y", g.IdentifierName("x"))}),
<x>Structure s

    Dim y As x
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s", members:={g.MethodDeclaration("m", returnType:=g.IdentifierName("t"))}),
<x>Structure s

    Function m() As t
    End Function
End Structure</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.StructDeclaration("s",
                    members:={g.ConstructorDeclaration(accessibility:=Accessibility.NotApplicable, modifiers:=DeclarationModifiers.None)}),
<x>Structure s

    Sub New()
    End Sub
End Structure</x>.Value)
        End Sub

        <Fact>
        Public Sub TestInterfaceDeclarations()
            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i"),
<x>Interface i
End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", typeParameters:={"x", "y"}),
<x>Interface i(Of x, y)
End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", interfaceTypes:={g.IdentifierName("a")}),
<x>Interface i
    Inherits a

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", interfaceTypes:={g.IdentifierName("a"), g.IdentifierName("b")}),
<x>Interface i
    Inherits a, b

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", interfaceTypes:={}),
<x>Interface i
End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", members:={g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Sealed)}),
<x>Interface i

    Function m() As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", members:={g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.Sealed)}),
<x>Interface i

    Property p As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", members:={g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility:=Accessibility.Public, modifiers:=DeclarationModifiers.ReadOnly)}),
<x>Interface i

    ReadOnly Property p As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", members:={g.IndexerDeclaration({g.ParameterDeclaration("y", g.IdentifierName("x"))}, g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.Sealed)}),
<x>Interface i

    Default Property Item(y As x) As t

End Interface</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.InterfaceDeclaration("i", members:={g.IndexerDeclaration({g.ParameterDeclaration("y", g.IdentifierName("x"))}, g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.ReadOnly)}),
<x>Interface i

    Default ReadOnly Property Item(y As x) As t

End Interface</x>.Value)
        End Sub

        <Fact>
        Public Sub TestEnumDeclarations()
            VerifySyntax(Of EnumBlockSyntax)(
                g.EnumDeclaration("e"),
<x>Enum e
End Enum</x>.Value)

            VerifySyntax(Of EnumBlockSyntax)(
                g.EnumDeclaration("e", members:={g.EnumMember("a"), g.EnumMember("b"), g.EnumMember("c")}),
<x>Enum e
    a
    b
    c
End Enum</x>.Value)

            VerifySyntax(Of EnumBlockSyntax)(
                g.EnumDeclaration("e", members:={g.IdentifierName("a"), g.EnumMember("b"), g.IdentifierName("c")}),
<x>Enum e
    a
    b
    c
End Enum</x>.Value)

            VerifySyntax(Of EnumBlockSyntax)(
                g.EnumDeclaration("e", members:={g.EnumMember("a", g.LiteralExpression(0)), g.EnumMember("b"), g.EnumMember("c", g.LiteralExpression(5))}),
<x>Enum e
    a = 0
    b
    c = 5
End Enum</x>.Value)
        End Sub

        <Fact>
        Public Sub TestNamespaceImportDeclarations()
            VerifySyntax(Of ImportsStatementSyntax)(
                g.NamespaceImportDeclaration(g.IdentifierName("n")),
<x>Imports n</x>.Value)

            VerifySyntax(Of ImportsStatementSyntax)(
                g.NamespaceImportDeclaration("n"),
<x>Imports n</x>.Value)

            VerifySyntax(Of ImportsStatementSyntax)(
                g.NamespaceImportDeclaration("n.m"),
<x>Imports n.m</x>.Value)
        End Sub

        <Fact>
        Public Sub TestNamespaceDeclarations()
            VerifySyntax(Of NamespaceBlockSyntax)(
                g.NamespaceDeclaration("n"),
<x>Namespace n
End Namespace</x>.Value)

            VerifySyntax(Of NamespaceBlockSyntax)(
                g.NamespaceDeclaration("n.m"),
<x>Namespace n.m
End Namespace</x>.Value)

            VerifySyntax(Of NamespaceBlockSyntax)(
                g.NamespaceDeclaration("n",
                    g.NamespaceImportDeclaration("m")),
<x>Namespace n

    Imports m
End Namespace</x>.Value)

            VerifySyntax(Of NamespaceBlockSyntax)(
                g.NamespaceDeclaration("n",
                    g.ClassDeclaration("c"),
                    g.NamespaceImportDeclaration("m")),
<x>Namespace n

    Imports m

    Class c
    End Class
End Namespace</x>.Value)
        End Sub


        <Fact>
        Public Sub TestCompilationUnits()
            VerifySyntax(Of CompilationUnitSyntax)(
                g.CompilationUnit(),
                "")

            VerifySyntax(Of CompilationUnitSyntax)(
                g.CompilationUnit(
                    g.NamespaceDeclaration("n")),
<x>Namespace n
End Namespace
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                g.CompilationUnit(
                    g.NamespaceImportDeclaration("n")),
<x>Imports n
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                g.CompilationUnit(
                    g.ClassDeclaration("c"),
                    g.NamespaceImportDeclaration("m")),
<x>Imports m

Class c
End Class
</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                g.CompilationUnit(
                    g.NamespaceImportDeclaration("n"),
                    g.NamespaceDeclaration("n",
                        g.NamespaceImportDeclaration("m"),
                        g.ClassDeclaration("c"))),
<x>Imports n

Namespace n

    Imports m

    Class c
    End Class
End Namespace
</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAttributeDeclarations()
            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute(g.IdentifierName("a")),
                "<a>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a"),
                "<a>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a.b"),
                "<a.b>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a", {}),
                "<a()>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a", {g.IdentifierName("x")}),
                "<a(x)>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a", {g.AttributeArgument(g.IdentifierName("x"))}),
                "<a(x)>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a", {g.AttributeArgument("x", g.IdentifierName("y"))}),
                "<a(x:=y)>")

            VerifySyntax(Of AttributeListSyntax)(
                g.Attribute("a", {g.IdentifierName("x"), g.IdentifierName("y")}),
                "<a(x, y)>")
        End Sub

        <Fact>
        Public Sub TestAddAttributes()
            VerifySyntax(Of FieldDeclarationSyntax)(
                g.AddAttributes(
                    g.FieldDeclaration("y", g.IdentifierName("x")),
                    g.Attribute("a")),
<x>&lt;a&gt;
Dim y As x</x>.Value)

            VerifySyntax(Of FieldDeclarationSyntax)(
                g.AddAttributes(
                    g.AddAttributes(
                        g.FieldDeclaration("y", g.IdentifierName("x")),
                        g.Attribute("a")),
                    g.Attribute("b")),
<x>&lt;a&gt;
&lt;b&gt;
Dim y As x</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                g.AddAttributes(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    g.Attribute("a")),
<x>&lt;a&gt;
MustInherit Function m() As t</x>.Value)

            VerifySyntax(Of MethodStatementSyntax)(
                g.AddReturnAttributes(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    g.Attribute("a")),
<x>MustInherit Function m() As &lt;a&gt; t</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.AddAttributes(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    g.Attribute("a")),
<x>&lt;a&gt;
Function m() As t
End Function</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.AddReturnAttributes(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), modifiers:=DeclarationModifiers.None),
                    g.Attribute("a")),
<x>Function m() As &lt;a&gt; t
End Function</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                g.AddAttributes(
                    g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
                    g.Attribute("a")),
<x>&lt;a&gt;
MustInherit Property p As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.AddAttributes(
                    g.PropertyDeclaration("p", g.IdentifierName("x")),
                    g.Attribute("a")),
<x>&lt;a&gt;
Property p As x
    Get
    End Get

    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyStatementSyntax)(
                g.AddAttributes(
                    g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x"), modifiers:=DeclarationModifiers.Abstract),
                    g.Attribute("a")),
<x>&lt;a&gt;
Default MustInherit Property Item(z As y) As x</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.AddAttributes(
                    g.IndexerDeclaration({g.ParameterDeclaration("z", g.IdentifierName("y"))}, g.IdentifierName("x")),
                    g.Attribute("a")),
<x>&lt;a&gt;
Default Property Item(z As y) As x
    Get
    End Get

    Set(value As x)
    End Set
End Property</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.AddAttributes(
                    g.ClassDeclaration("c"),
                    g.Attribute("a")),
<x>&lt;a&gt;
Class c
End Class</x>.Value)

            VerifySyntax(Of ParameterSyntax)(
                g.AddAttributes(
                    g.ParameterDeclaration("p", g.IdentifierName("t")),
                    g.Attribute("a")),
<x>&lt;a&gt; p As t</x>.Value)

            VerifySyntax(Of CompilationUnitSyntax)(
                g.AddAttributes(
                    g.CompilationUnit(g.NamespaceDeclaration("n")),
                    g.Attribute("a")),
<x>&lt;Assembly:a&gt;
Namespace n
End Namespace
</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAsPublicInterfaceImplementation()
            VerifySyntax(Of MethodBlockBaseSyntax)(
                g.AsPublicInterfaceImplementation(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), modifiers:=DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
<x>Public Function m() As t Implements i.m
End Function</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.AsPublicInterfaceImplementation(
                    g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
<x>Public Property p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.AsPublicInterfaceImplementation(
                    g.IndexerDeclaration({g.ParameterDeclaration("p", g.IdentifierName("a"))}, g.IdentifierName("t"), Accessibility.Internal, DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
<x>Default Public Property Item(p As a) As t Implements i.Item
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAsPrivateInterfaceImplementation()
            VerifySyntax(Of MethodBlockBaseSyntax)(
                g.AsPrivateInterfaceImplementation(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
<x>Private Function i_m() As t Implements i.m
End Function</x>.Value)

            VerifySyntax(Of MethodBlockBaseSyntax)(
                g.AsPrivateInterfaceImplementation(
                    g.MethodDeclaration("m", returnType:=g.IdentifierName("t"), accessibility:=Accessibility.Private, modifiers:=DeclarationModifiers.Abstract),
                    g.TypeExpression(Me.ienumerableInt)),
<x>Private Function IEnumerable_Int32_m() As t Implements Global.System.Collections.Generic.IEnumerable(Of System.Int32).m
End Function</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.AsPrivateInterfaceImplementation(
                    g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility:=Accessibility.Internal, modifiers:=DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
<x>Private Property i_p As t Implements i.p
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)

            VerifySyntax(Of PropertyBlockSyntax)(
                g.AsPrivateInterfaceImplementation(
                    g.IndexerDeclaration({g.ParameterDeclaration("p", g.IdentifierName("a"))}, g.IdentifierName("t"), Accessibility.Protected, DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
<x>Private Property i_Item(p As a) As t Implements i.Item
    Get
    End Get

    Set(value As t)
    End Set
End Property</x>.Value)
        End Sub

        <Fact>
        Public Sub TestWithTypeParameters()
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a"),
<x>MustInherit Sub m(Of a)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers:=DeclarationModifiers.None),
                    "a"),
<x>Sub m(Of a)()
End Sub</x>.Value)

            ' assigning no type parameters is legal
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract)),
<x>MustInherit Sub m()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers:=DeclarationModifiers.None)),
<x>Sub m()
End Sub</x>.Value)

            ' removing type parameters
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeParameters(g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a")),
<x>MustInherit Sub m()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeParameters(g.WithTypeParameters(
                    g.MethodDeclaration("m"),
                    "a")),
<x>Sub m()
End Sub</x>.Value)

            ' multiple type parameters
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract),
                    "a", "b"),
<x>MustInherit Sub m(Of a, b)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeParameters(
                    g.MethodDeclaration("m"),
                    "a", "b"),
<x>Sub m(Of a, b)()
End Sub</x>.Value)

            VerifySyntax(Of ClassBlockSyntax)(
                g.WithTypeParameters(
                    g.ClassDeclaration("c"),
                    "a", "b"),
<x>Class c(Of a, b)
End Class</x>.Value)

            VerifySyntax(Of StructureBlockSyntax)(
                g.WithTypeParameters(
                    g.StructDeclaration("s"),
                    "a", "b"),
<x>Structure s(Of a, b)
End Structure</x>.Value)

            VerifySyntax(Of InterfaceBlockSyntax)(
                g.WithTypeParameters(
                    g.InterfaceDeclaration("i"),
                    "a", "b"),
<x>Interface i(Of a, b)
End Interface</x>.Value)

        End Sub

        <Fact>
        Public Sub TestWithTypeConstraint()
            ' single type constraint
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b")),
<x>MustInherit Sub m(Of a As b)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m"), "a"),
                    "a", g.IdentifierName("b")),
<x>Sub m(Of a As b)()
End Sub</x>.Value)

            ' multiple type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")),
<x>MustInherit Sub m(Of a As {b, c})()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m"), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")),
<x>Sub m(Of a As {b, c})()
End Sub</x>.Value)

            ' no type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a"),
<x>MustInherit Sub m(Of a)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m"), "a"),
                    "a"),
<x>Sub m(Of a)()
End Sub</x>.Value)

            ' removed type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")), "a"),
<x>MustInherit Sub m(Of a)()</x>.Value)

            VerifySyntax(Of MethodBlockSyntax)(
                g.WithTypeConstraint(g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m"), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")), "a"),
<x>Sub m(Of a)()
End Sub</x>.Value)

            ' multipe type parameters with constraints
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeConstraint(
                        g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a", "x"),
                        "a", g.IdentifierName("b"), g.IdentifierName("c")),
                    "x", g.IdentifierName("y")),
<x>MustInherit Sub m(Of a As {b, c}, x As y)()</x>.Value)

            ' with constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.Constructor),
<x>MustInherit Sub m(Of a As New)()</x>.Value)

            ' with reference constraint
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType),
<x>MustInherit Sub m(Of a As Class)()</x>.Value)

            ' with value type constraint
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType),
<x>MustInherit Sub m(Of a As Structure)()</x>.Value)

            ' with reference constraint and constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType Or SpecialTypeConstraintKind.Constructor),
<x>MustInherit Sub m(Of a As {Class, New})()</x>.Value)

            ' with value type constraint and constructor constraint
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType Or SpecialTypeConstraintKind.Constructor),
<x>MustInherit Sub m(Of a As {Structure, New})()</x>.Value)

            ' with reference constraint and type constraints
            VerifySyntax(Of MethodStatementSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers:=DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType, g.IdentifierName("b"), g.IdentifierName("c")),
<x>MustInherit Sub m(Of a As {Class, b, c})()</x>.Value)

            ' class declarations
            VerifySyntax(Of ClassBlockSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.ClassDeclaration("c"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
<x>Class c(Of a As x, b)
End Class</x>.Value)

            ' structure declarations
            VerifySyntax(Of StructureBlockSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.StructDeclaration("s"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
<x>Structure s(Of a As x, b)
End Structure</x>.Value)

            ' interface delcarations
            VerifySyntax(Of InterfaceBlockSyntax)(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.InterfaceDeclaration("i"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
<x>Interface i(Of a As x, b)
End Interface</x>.Value)

        End Sub

    End Class
End Namespace
