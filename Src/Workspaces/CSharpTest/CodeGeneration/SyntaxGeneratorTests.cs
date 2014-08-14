using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGeneration
{
    public class SyntaxGeneratorTests
    {
        private readonly SyntaxGenerator g = SyntaxGenerator.GetGenerator(new CustomWorkspace(), LanguageNames.CSharp);

        private readonly CSharpCompilation emptyCompilation = CSharpCompilation.Create("empty",
                references: new MetadataReference[] { new MetadataFileReference(typeof(int).Assembly.Location) });

        private readonly INamedTypeSymbol ienumerableInt;

        public SyntaxGeneratorTests()
        {
            this.ienumerableInt = emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(emptyCompilation.GetSpecialType(SpecialType.System_Int32));
        }

        private void VerifySyntax<TSyntax>(SyntaxNode type, string expectedText) where TSyntax : SyntaxNode
        {
            Assert.IsAssignableFrom(typeof(TSyntax), type);
            var normalized = type.NormalizeWhitespace().ToString();
            Assert.Equal(expectedText, normalized);
        }

        [Fact]
        public void TestLiteralExpressions()
        {
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0), "0");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1), "1");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(-1), "-1");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(int.MinValue), "global::System.Int32.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(int.MaxValue), "global::System.Int32.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0L), "0L");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1L), "1L");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(-1L), "-1L");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(long.MinValue), "global::System.Int64.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(long.MaxValue), "global::System.Int64.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0UL), "0UL");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1UL), "1UL");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(ulong.MinValue), "0UL");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(ulong.MaxValue), "global::System.UInt64.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0.0f), "0F");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1.0f), "1F");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(-1.0f), "-1F");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(float.MinValue), "global::System.Single.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(float.MaxValue), "global::System.Single.MaxValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(float.Epsilon), "global::System.Single.Epsilon");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(float.NaN), "global::System.Single.NaN");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(float.NegativeInfinity), "global::System.Single.NegativeInfinity");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(float.PositiveInfinity), "global::System.Single.PositiveInfinity");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0.0), "0D");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1.0), "1D");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(-1.0), "-1D");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(double.MinValue), "global::System.Double.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(double.MaxValue), "global::System.Double.MaxValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(double.Epsilon), "global::System.Double.Epsilon");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(double.NaN), "global::System.Double.NaN");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(double.NegativeInfinity), "global::System.Double.NegativeInfinity");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(double.PositiveInfinity), "global::System.Double.PositiveInfinity");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0m), "0M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0.00m), "0.00M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1.00m), "1.00M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(-1.00m), "-1.00M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1.0000000000m), "1.0000000000M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0.000000m), "0.000000M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0.0000000m), "0.0000000M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1000000000m), "1000000000M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(123456789.123456789m), "123456789.123456789M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1E-28m), "0.0000000000000000000000000001M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(0E-28m), "0.0000000000000000000000000000M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(1E-29m), "0.0000000000000000000000000000M");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(-1E-29m), "0.0000000000000000000000000000M");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(decimal.MinValue), "global::System.Decimal.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(g.LiteralExpression(decimal.MaxValue), "global::System.Decimal.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression('c'), "'c'");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression("str"), "\"str\"");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression("s\"t\"r"), "\"s\\\"t\\\"r\"");

            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(true), "true");
            VerifySyntax<LiteralExpressionSyntax>(g.LiteralExpression(false), "false");
        }

        [Fact]
        public void TestNameExpressions()
        {
            VerifySyntax<IdentifierNameSyntax>(g.IdentifierName("x"), "x");
            VerifySyntax<QualifiedNameSyntax>(g.QualifiedName(g.IdentifierName("x"), g.IdentifierName("y")), "x.y");
            VerifySyntax<QualifiedNameSyntax>(g.DottedName("x.y"), "x.y");

            VerifySyntax<GenericNameSyntax>(g.GenericName("x", g.IdentifierName("y")), "x<y>");
            VerifySyntax<GenericNameSyntax>(g.GenericName("x", g.IdentifierName("y"), g.IdentifierName("z")), "x<y, z>");

            // convert identifer name into generic name
            VerifySyntax<GenericNameSyntax>(g.WithTypeArguments(g.IdentifierName("x"), g.IdentifierName("y")), "x<y>");

            // convert qualified name into qualified generic name
            VerifySyntax<QualifiedNameSyntax>(g.WithTypeArguments(g.DottedName("x.y"), g.IdentifierName("z")), "x.y<z>");

            // convert member access expression into generic member access expression
            VerifySyntax<MemberAccessExpressionSyntax>(g.WithTypeArguments(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x.y<z>");

            // convert existing generic name into a different generic name
            var gname = g.WithTypeArguments(g.IdentifierName("x"), g.IdentifierName("y"));
            VerifySyntax<GenericNameSyntax>(gname, "x<y>");
            VerifySyntax<GenericNameSyntax>(g.WithTypeArguments(gname, g.IdentifierName("z")), "x<z>");
        }

        [Fact]
        public void TestTypeExpressions()
        {
            // these are all type syntax too
            VerifySyntax<TypeSyntax>(g.IdentifierName("x"), "x");
            VerifySyntax<TypeSyntax>(g.QualifiedName(g.IdentifierName("x"), g.IdentifierName("y")), "x.y");
            VerifySyntax<TypeSyntax>(g.DottedName("x.y"), "x.y");
            VerifySyntax<TypeSyntax>(g.GenericName("x", g.IdentifierName("y")), "x<y>");
            VerifySyntax<TypeSyntax>(g.GenericName("x", g.IdentifierName("y"), g.IdentifierName("z")), "x<y, z>");

            VerifySyntax<TypeSyntax>(g.ArrayTypeExpression(g.IdentifierName("x")), "x[]");
            VerifySyntax<TypeSyntax>(g.ArrayTypeExpression(g.ArrayTypeExpression(g.IdentifierName("x"))), "x[][]");
            VerifySyntax<TypeSyntax>(g.NullableTypeExpression(g.IdentifierName("x")), "x?");
            VerifySyntax<TypeSyntax>(g.NullableTypeExpression(g.NullableTypeExpression(g.IdentifierName("x"))), "x?");
        }

        [Fact]
        public void TestSpecialTypeExpression()
        {
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Byte), "byte");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_SByte), "sbyte");

            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Int16), "short");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_UInt16), "ushort");

            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Int32), "int");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_UInt32), "uint");

            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Int64), "long");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_UInt64), "ulong");

            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Single), "float");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Double), "double");

            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Char), "char");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_String), "string");

            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Object), "object");
            VerifySyntax<TypeSyntax>(g.TypeExpression(SpecialType.System_Decimal), "decimal");
        }

        [Fact]
        public void TestSymbolTypeExpressions()
        {
            var genericType = emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            VerifySyntax<QualifiedNameSyntax>(g.TypeExpression(genericType), "global::System.Collections.Generic.IEnumerable<T>");

            var arrayType = emptyCompilation.CreateArrayTypeSymbol(emptyCompilation.GetSpecialType(SpecialType.System_Int32));
            VerifySyntax<ArrayTypeSyntax>(g.TypeExpression(arrayType), "System.Int32[]");
        }

        [Fact]
        public void TestMathAndLogicExpressions()
        {
            VerifySyntax<PrefixUnaryExpressionSyntax>(g.NegateExpression(g.IdentifierName("x")), "-(x)");
            VerifySyntax<BinaryExpressionSyntax>(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) + (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.SubtractExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) - (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.MultiplyExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) * (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.DivideExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) / (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.ModuloExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) % (y)");

            VerifySyntax<PrefixUnaryExpressionSyntax>(g.BitwiseNotExpression(g.IdentifierName("x")), "~(x)");
            VerifySyntax<BinaryExpressionSyntax>(g.BitwiseAndExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) & (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.BitwiseOrExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) | (y)");

            VerifySyntax<PrefixUnaryExpressionSyntax>(g.LogicalNotExpression(g.IdentifierName("x")), "!(x)");
            VerifySyntax<BinaryExpressionSyntax>(g.LogicalAndExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) && (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.LogicalOrExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) || (y)");
        }

        [Fact]
        public void TestEqualityAndInequalityExpressions()
        {
            VerifySyntax<BinaryExpressionSyntax>(g.ReferenceEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) == (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.ValueEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) == (y)");

            VerifySyntax<BinaryExpressionSyntax>(g.ReferenceNotEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) != (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.ValueNotEqualsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) != (y)");

            VerifySyntax<BinaryExpressionSyntax>(g.LessThanExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) < (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.LessThanOrEqualExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) <= (y)");

            VerifySyntax<BinaryExpressionSyntax>(g.GreaterThanExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) > (y)");
            VerifySyntax<BinaryExpressionSyntax>(g.GreaterThanOrEqualExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) >= (y)");
        }

        [Fact]
        public void TestConditionalExpressions()
        {
            VerifySyntax<BinaryExpressionSyntax>(g.CoalesceExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) ?? (y)");
            VerifySyntax<ConditionalExpressionSyntax>(g.ConditionalExpression(g.IdentifierName("x"), g.IdentifierName("y"), g.IdentifierName("z")), "(x) ? (y) : (z)");
        }

        [Fact]
        public void TestMemberAccessExpressions()
        {
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), "x.y");
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.IdentifierName("x"), "y"), "x.y");
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x.y.z");
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x(y).z");
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "x[y].z");
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")), "((x) + (y)).z");
            VerifySyntax<MemberAccessExpressionSyntax>(g.MemberAccessExpression(g.NegateExpression(g.IdentifierName("x")), g.IdentifierName("y")), "(-(x)).y");
        }

        [Fact]
        public void TestObjectCreationExpressions()
        {
            VerifySyntax<ObjectCreationExpressionSyntax>(
                g.ObjectCreationExpression(g.IdentifierName("x")),
                "new x()");

            VerifySyntax<ObjectCreationExpressionSyntax>(
                g.ObjectCreationExpression(g.IdentifierName("x"), g.IdentifierName("y")),
                "new x(y)");

            var intType = emptyCompilation.GetSpecialType(SpecialType.System_Int32);
            var listType = emptyCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var listOfIntType = listType.Construct(intType);

            VerifySyntax<ObjectCreationExpressionSyntax>(
                g.ObjectCreationExpression(listOfIntType, g.IdentifierName("y")),
                "new global::System.Collections.Generic.List<System.Int32>(y)");  // should this be 'int' or if not shouldn't it have global::?
        }

        [Fact]
        public void TestElementAccessExpressions()
        {
            VerifySyntax<ElementAccessExpressionSyntax>(
                g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")),
                "x[y]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y"), g.IdentifierName("z")),
                "x[y, z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                g.ElementAccessExpression(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "x.y[z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                g.ElementAccessExpression(g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "x[y][z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                g.ElementAccessExpression(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "x(y)[z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                g.ElementAccessExpression(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y")), g.IdentifierName("z")),
                "((x) + (y))[z]");
        }

        [Fact]
        public void TestCastAndConvertExpressions()
        {
            VerifySyntax<CastExpressionSyntax>(g.CastExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x)(y)");
            VerifySyntax<CastExpressionSyntax>(g.ConvertExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x)(y)");
        }

        [Fact]
        public void TestIsAndAsExpressions()
        {
            VerifySyntax<BinaryExpressionSyntax>(g.IsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) is y");
            VerifySyntax<BinaryExpressionSyntax>(g.AsExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) as y");
        }

        [Fact]
        public void TestInvocationExpressions()
        {
            // without explicit arguments
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.IdentifierName("x")), "x()");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y")), "x(y)");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y"), g.IdentifierName("z")), "x(y, z)");

            // using explicit arguments
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.IdentifierName("x"), g.Argument(g.IdentifierName("y"))), "x(y)");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.IdentifierName("x"), g.Argument(RefKind.Ref, g.IdentifierName("y"))), "x(ref y)");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.IdentifierName("x"), g.Argument(RefKind.Out, g.IdentifierName("y"))), "x(out y)");

            // auto parenthesizing
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.MemberAccessExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "x.y()");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.ElementAccessExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "x[y]()");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.InvocationExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "x(y)()");
            VerifySyntax<InvocationExpressionSyntax>(g.InvocationExpression(g.AddExpression(g.IdentifierName("x"), g.IdentifierName("y"))), "((x) + (y))()");
        }

        [Fact]
        public void TestAssignmentStatement()
        {
            VerifySyntax<BinaryExpressionSyntax>(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y")), "x = (y)");
        }

        [Fact]
        public void TestExpressionStatement()
        {
            VerifySyntax<ExpressionStatementSyntax>(g.ExpressionStatement(g.IdentifierName("x")), "x;");
            VerifySyntax<ExpressionStatementSyntax>(g.ExpressionStatement(g.InvocationExpression(g.IdentifierName("x"))), "x();");
        }

        [Fact]
        public void TestLocalDeclarationStatements()
        {
            VerifySyntax<LocalDeclarationStatementSyntax>(g.LocalDeclarationStatement(g.IdentifierName("x"), "y"), "x y;");
            VerifySyntax<LocalDeclarationStatementSyntax>(g.LocalDeclarationStatement(g.IdentifierName("x"), "y", g.IdentifierName("z")), "x y = z;");

            VerifySyntax<LocalDeclarationStatementSyntax>(g.LocalDeclarationStatement(g.IdentifierName("x"), "y", isConst: true), "const x y;");
            VerifySyntax<LocalDeclarationStatementSyntax>(g.LocalDeclarationStatement(g.IdentifierName("x"), "y", g.IdentifierName("z"), isConst: true), "const x y = z;");

            VerifySyntax<LocalDeclarationStatementSyntax>(g.LocalDeclarationStatement("y", g.IdentifierName("z")), "var y = z;");
        }

        [Fact]
        public void TestReturnStatements()
        {
            VerifySyntax<ReturnStatementSyntax>(g.ReturnStatement(), "return;");
            VerifySyntax<ReturnStatementSyntax>(g.ReturnStatement(g.IdentifierName("x")), "return x;");
        }

        [Fact]
        public void TestThrowStatements()
        {
            VerifySyntax<ThrowStatementSyntax>(g.ThrowStatement(), "throw;");
            VerifySyntax<ThrowStatementSyntax>(g.ThrowStatement(g.IdentifierName("x")), "throw x;");
        }

        [Fact]
        public void TestIfStatements()
        {
            VerifySyntax<IfStatementSyntax>(
                g.IfStatement(g.IdentifierName("x"), new SyntaxNode[] { }),
                "if (x)\r\n{\r\n}");

            VerifySyntax<IfStatementSyntax>(
                g.IfStatement(g.IdentifierName("x"), new SyntaxNode[] { }, new SyntaxNode[] { }),
                "if (x)\r\n{\r\n}\r\nelse\r\n{\r\n}");

            VerifySyntax<IfStatementSyntax>(
                g.IfStatement(g.IdentifierName("x"),
                    new SyntaxNode[] { g.IdentifierName("y") }),
                "if (x)\r\n{\r\n    y;\r\n}");

            VerifySyntax<IfStatementSyntax>(
                g.IfStatement(g.IdentifierName("x"),
                    new SyntaxNode[] { g.IdentifierName("y") },
                    new SyntaxNode[] { g.IdentifierName("z") }),
                "if (x)\r\n{\r\n    y;\r\n}\r\nelse\r\n{\r\n    z;\r\n}");

            VerifySyntax<IfStatementSyntax>(
                g.IfStatement(g.IdentifierName("x"),
                    new SyntaxNode[] { g.IdentifierName("y") },
                    g.IfStatement(g.IdentifierName("p"), new SyntaxNode[] { g.IdentifierName("q") })),
                "if (x)\r\n{\r\n    y;\r\n}\r\nelse if (p)\r\n{\r\n    q;\r\n}");

            VerifySyntax<IfStatementSyntax>(
                g.IfStatement(g.IdentifierName("x"),
                    new SyntaxNode[] { g.IdentifierName("y") },
                    g.IfStatement(g.IdentifierName("p"), new SyntaxNode[] { g.IdentifierName("q") }, g.IdentifierName("z"))),
                "if (x)\r\n{\r\n    y;\r\n}\r\nelse if (p)\r\n{\r\n    q;\r\n}\r\nelse\r\n{\r\n    z;\r\n}");
        }

        [Fact]
        public void TestSwitchStatements()
        {
            VerifySyntax<SwitchStatementSyntax>(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        new[] { g.IdentifierName("z") })),
                "switch (x)\r\n{\r\n    case y:\r\n        z;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(
                        new[] { g.IdentifierName("y"), g.IdentifierName("p"), g.IdentifierName("q") },
                        new[] { g.IdentifierName("z") })),
                "switch (x)\r\n{\r\n    case y:\r\n    case p:\r\n    case q:\r\n        z;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        new[] { g.IdentifierName("z") }),
                    g.SwitchSection(g.IdentifierName("a"),
                        new[] { g.IdentifierName("b") })),
                "switch (x)\r\n{\r\n    case y:\r\n        z;\r\n    case a:\r\n        b;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        new[] { g.IdentifierName("z") }),
                    g.DefaultSwitchSection(
                        new[] { g.IdentifierName("b") })),
                "switch (x)\r\n{\r\n    case y:\r\n        z;\r\n    default:\r\n        b;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                g.SwitchStatement(g.IdentifierName("x"),
                    g.SwitchSection(g.IdentifierName("y"),
                        new[] { g.ExitSwitchStatement() })),
                "switch (x)\r\n{\r\n    case y:\r\n        break;\r\n}");
        }

        [Fact]
        public void TestUsingStatements()
        {
            VerifySyntax<UsingStatementSyntax>(
                g.UsingStatement(g.IdentifierName("x"), new[] { g.IdentifierName("y") }),
                "using (x)\r\n{\r\n    y;\r\n}");

            VerifySyntax<UsingStatementSyntax>(
                g.UsingStatement("x", g.IdentifierName("y"), new[] { g.IdentifierName("z") }),
                "using (var x = y)\r\n{\r\n    z;\r\n}");

            VerifySyntax<UsingStatementSyntax>(
                g.UsingStatement(g.IdentifierName("x"), "y", g.IdentifierName("z"), new[] { g.IdentifierName("q") }),
                "using (x y = z)\r\n{\r\n    q;\r\n}");
        }

        [Fact]
        public void TestTryCatchStatements()
        {
            VerifySyntax<TryStatementSyntax>(
                g.TryCatchStatement(
                    new[] { g.IdentifierName("x") },
                    g.CatchClause(g.IdentifierName("y"), "z",
                        new[] { g.IdentifierName("a") })),
                "try\r\n{\r\n    x;\r\n}\r\ncatch (y z)\r\n{\r\n    a;\r\n}");

            VerifySyntax<TryStatementSyntax>(
                g.TryCatchStatement(
                    new[] { g.IdentifierName("s") },
                    g.CatchClause(g.IdentifierName("x"), "y",
                        new[] { g.IdentifierName("z") }),
                    g.CatchClause(g.IdentifierName("a"), "b",
                        new[] { g.IdentifierName("c") })),
                "try\r\n{\r\n    s;\r\n}\r\ncatch (x y)\r\n{\r\n    z;\r\n}\r\ncatch (a b)\r\n{\r\n    c;\r\n}");

            VerifySyntax<TryStatementSyntax>(
                g.TryCatchStatement(
                    new[] { g.IdentifierName("s") },
                    new[] { g.CatchClause(g.IdentifierName("x"), "y", new[] { g.IdentifierName("z") }) },
                    new[] { g.IdentifierName("a") }),
                "try\r\n{\r\n    s;\r\n}\r\ncatch (x y)\r\n{\r\n    z;\r\n}\r\nfinally\r\n{\r\n    a;\r\n}");

            VerifySyntax<TryStatementSyntax>(
                g.TryFinallyStatement(
                    new[] { g.IdentifierName("x") },
                    new[] { g.IdentifierName("a") }),
                "try\r\n{\r\n    x;\r\n}\r\nfinally\r\n{\r\n    a;\r\n}");
        }

        [Fact]
        public void TestWhileStatements()
        {
            VerifySyntax<WhileStatementSyntax>(
                g.WhileStatement(g.IdentifierName("x"),
                    new[] { g.IdentifierName("y") }),
                "while (x)\r\n{\r\n    y;\r\n}");

            VerifySyntax<WhileStatementSyntax>(
                g.WhileStatement(g.IdentifierName("x"), null),
                "while (x)\r\n{\r\n}");
        }

        [Fact]
        public void TestLambdaExpressions()
        {
            VerifySyntax<SimpleLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression("x", g.IdentifierName("y")),
                "x => y");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression(new[] { g.LambdaParameter("x"), g.LambdaParameter("y") }, g.IdentifierName("z")),
                "(x, y) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression(new SyntaxNode[] { }, g.IdentifierName("y")),
                "() => y");

            VerifySyntax<SimpleLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression("x", g.IdentifierName("y")),
                "x => y");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression(new[] { g.LambdaParameter("x"), g.LambdaParameter("y") }, g.IdentifierName("z")),
                "(x, y) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression(new SyntaxNode[] { }, g.IdentifierName("y")),
                "() => y");

            VerifySyntax<SimpleLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression("x", new[] { g.ReturnStatement(g.IdentifierName("y")) }),
                "x =>\r\n{\r\n    return y;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression(new[] { g.LambdaParameter("x"), g.LambdaParameter("y") }, new[] { g.ReturnStatement(g.IdentifierName("z")) }),
                "(x, y) =>\r\n{\r\n    return z;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression(new SyntaxNode[] { }, new[] { g.ReturnStatement(g.IdentifierName("y")) }),
                "() =>\r\n{\r\n    return y;\r\n}");

            VerifySyntax<SimpleLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression("x", new[] { g.IdentifierName("y") }),
                "x =>\r\n{\r\n    y;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression(new[] { g.LambdaParameter("x"), g.LambdaParameter("y") }, new[] { g.IdentifierName("z") }),
                "(x, y) =>\r\n{\r\n    z;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression(new SyntaxNode[] { }, new[] { g.IdentifierName("y") }),
                "() =>\r\n{\r\n    y;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression(new[] { g.LambdaParameter("x", g.IdentifierName("y")) }, g.IdentifierName("z")),
                "(y x) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.ValueReturningLambdaExpression(new[] { g.LambdaParameter("x", g.IdentifierName("y")), g.LambdaParameter("a", g.IdentifierName("b")) }, g.IdentifierName("z")),
                "(y x, b a) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression(new[] { g.LambdaParameter("x", g.IdentifierName("y")) }, g.IdentifierName("z")),
                "(y x) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                g.VoidReturningLambdaExpression(new[] { g.LambdaParameter("x", g.IdentifierName("y")), g.LambdaParameter("a", g.IdentifierName("b")) }, g.IdentifierName("z")),
                "(y x, b a) => z");
        }

        [Fact]
        public void TestFieldDeclarations()
        {
            VerifySyntax<FieldDeclarationSyntax>(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32)),
                "int fld;");

            VerifySyntax<FieldDeclarationSyntax>(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), initializer: g.LiteralExpression(0)),
                "int fld = 0;");

            VerifySyntax<FieldDeclarationSyntax>(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), accessibility: Accessibility.Public),
                "public int fld;");

            VerifySyntax<FieldDeclarationSyntax>(
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), accessibility: Accessibility.NotApplicable, modifiers: SymbolModifiers.Static | SymbolModifiers.ReadOnly),
                "static readonly int fld;");
        }

        [Fact]
        public void TestMethodDeclarations()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m"),
                "void m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", typeParameters: new[] { "x", "y" }),
                "void m<x, y>()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", returnType: g.IdentifierName("x")),
                "x m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", returnType: g.IdentifierName("x"), statements: new[] { g.IdentifierName("y") }),
                "x m()\r\n{\r\n    y;\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", parameters: new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, returnType: g.IdentifierName("x")),
                "x m(y z)\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", parameters: new[] { g.ParameterDeclaration("z", g.IdentifierName("y"), g.IdentifierName("a")) }, returnType: g.IdentifierName("x")),
                "x m(y z = a)\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", returnType: g.IdentifierName("x"), accessibility: Accessibility.Public),
                "public x m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.MethodDeclaration("m", returnType: g.IdentifierName("x"), accessibility: Accessibility.Public, modifiers: SymbolModifiers.Abstract),
                "public abstract x m();");
        }

        [Fact]
        public void TestConstructorDeclaration()
        {           
            VerifySyntax<ConstructorDeclarationSyntax>(
                g.ConstructorDeclaration(),
                "ctor()\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                g.ConstructorDeclaration("c"),
                "c()\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                g.ConstructorDeclaration("c", accessibility: Accessibility.Public, modifiers: SymbolModifiers.Static),
                "public static c()\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                g.ConstructorDeclaration("c", new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) }),
                "c(t p)\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                g.ConstructorDeclaration("c", 
                    parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) },
                    baseConstructorArguments: new[] { g.IdentifierName("p") }),
                "c(t p): base (p)\r\n{\r\n}");
        }

        [Fact]
        public void TestPropertyDeclarations()
        {
            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: SymbolModifiers.Abstract | SymbolModifiers.ReadOnly),
                "abstract x p\r\n{\r\n    get;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: SymbolModifiers.ReadOnly),
                "x p\r\n{\r\n    get\r\n    {\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: SymbolModifiers.Abstract),
                "abstract x p\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: SymbolModifiers.ReadOnly, getterStatements: new[] { g.IdentifierName("y") }),
                "x p\r\n{\r\n    get\r\n    {\r\n        y;\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), setterStatements: new[] { g.IdentifierName("y") }),
                "x p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n        y;\r\n    }\r\n}");
        }

        [Fact]
        public void TestIndexerDeclarations()
        {
            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: SymbolModifiers.Abstract | SymbolModifiers.ReadOnly),
                "abstract x this[y z]\r\n{\r\n    get;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: SymbolModifiers.Abstract),
                "abstract x this[y z]\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: SymbolModifiers.ReadOnly),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: SymbolModifiers.ReadOnly,
                    getterStatements: new[] { g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x")),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"),
                    setterStatements: new[] { g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"),
                    getterStatements: new[] { g.IdentifierName("a") }, setterStatements: new[] { g.IdentifierName("b") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n        a;\r\n    }\r\n\r\n    set\r\n    {\r\n        b;\r\n    }\r\n}");
        }

        [Fact]
        public void TestAsPublicInterfaceImplementation()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.AsPublicInterfaceImplementation(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), modifiers: SymbolModifiers.Abstract),
                    g.IdentifierName("i")),
                "public t m()\r\n{\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.AsPublicInterfaceImplementation(
                    g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: SymbolModifiers.Abstract),
                    g.IdentifierName("i")),
                "public t p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.AsPublicInterfaceImplementation(
                    g.IndexerDeclaration(parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("a")) }, type: g.IdentifierName("t"), accessibility: Accessibility.Internal, modifiers: SymbolModifiers.Abstract),
                    g.IdentifierName("i")),
                "public t this[a p]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestAsPrivateInterfaceImplementation()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: SymbolModifiers.Abstract),
                    g.IdentifierName("i")),
                "t i.m()\r\n{\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Internal, modifiers: SymbolModifiers.Abstract),
                    g.IdentifierName("i")),
                "t i.p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.IndexerDeclaration(parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("a")) }, type: g.IdentifierName("t"), accessibility: Accessibility.Protected, modifiers: SymbolModifiers.Abstract),
                    g.IdentifierName("i")),
                "t i.this[a p]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestClassDeclarations()
        {
            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c"),
                "class c\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", typeParameters: new[] { "x", "y" }),
                "class c<x, y>\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", baseType: g.IdentifierName("x")),
                "class c : x\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", interfaceTypes: new[] { g.IdentifierName("x") }),
                "class c : x\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", baseType: g.IdentifierName("x"), interfaceTypes: new[] { g.IdentifierName("y") }),
                "class c : x, y\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", interfaceTypes: new SyntaxNode[] { }),
                "class c\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", members: new[] { g.FieldDeclaration("y", type: g.IdentifierName("x")) }),
                "class c\r\n{\r\n    x y;\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", members: new[] { g.MethodDeclaration("m", returnType: g.IdentifierName("t")) }),
                "class c\r\n{\r\n    t m()\r\n    {\r\n    }\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("c", members: new[] { g.ConstructorDeclaration() }),
                "class c\r\n{\r\n    c()\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestStructDeclarations()
        {
            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s"),
                "struct s\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", typeParameters: new[] { "x", "y" }),
                "struct s<x, y>\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", interfaceTypes: new[] { g.IdentifierName("x") }),
                "struct s : x\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", interfaceTypes: new[] { g.IdentifierName("x"), g.IdentifierName("y") }),
                "struct s : x, y\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", interfaceTypes: new SyntaxNode[] { }),
                "struct s\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", members: new[] { g.FieldDeclaration("y", g.IdentifierName("x")) }),
                "struct s\r\n{\r\n    x y;\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", members: new[] { g.MethodDeclaration("m", returnType: g.IdentifierName("t")) }),
                "struct s\r\n{\r\n    t m()\r\n    {\r\n    }\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.StructDeclaration("s", members: new[] { g.ConstructorDeclaration("xxx") }),
                "struct s\r\n{\r\n    s()\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestInterfaceDeclarations()
        {
            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i"),
                "interface i\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", typeParameters: new[] { "x", "y" }),
                "interface i<x, y>\r\n{\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", interfaceTypes: new[] { g.IdentifierName("a") }),
                    "interface i : a\r\n{\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", interfaceTypes: new[] { g.IdentifierName("a"), g.IdentifierName("b") }),
                    "interface i : a, b\r\n{\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", interfaceTypes: new SyntaxNode[] { }),
                    "interface i\r\n{\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", members: new[] { g.MethodDeclaration("m", returnType: g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: SymbolModifiers.Sealed) }),
                    "interface i\r\n{\r\n    t m();\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", members: new[] { g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: SymbolModifiers.Sealed) }),
                    "interface i\r\n{\r\n    t p\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", members: new[] { g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: SymbolModifiers.ReadOnly) }),
                    "interface i\r\n{\r\n    t p\r\n    {\r\n        get;\r\n    }\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", members: new[] { g.IndexerDeclaration(new[] { g.ParameterDeclaration("y", g.IdentifierName("x")) }, g.IdentifierName("t"), Accessibility.Public, SymbolModifiers.Sealed) }),
                    "interface i\r\n{\r\n    t this[x y]\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");

                VerifySyntax<InterfaceDeclarationSyntax>(
                    g.InterfaceDeclaration("i", members: new[] { g.IndexerDeclaration(new[] { g.ParameterDeclaration("y", g.IdentifierName("x")) }, g.IdentifierName("t"), Accessibility.Public, SymbolModifiers.ReadOnly) }),
                    "interface i\r\n{\r\n    t this[x y]\r\n    {\r\n        get;\r\n    }\r\n}");
            }

        [Fact]
        public void TestEnumDeclarations()
        {
            VerifySyntax<EnumDeclarationSyntax>(
                g.EnumDeclaration("e"),
                "enum e\r\n{\r\n}");

            VerifySyntax<EnumDeclarationSyntax>(
                g.EnumDeclaration("e", members: new[] { g.EnumMember("a"), g.EnumMember("b"), g.EnumMember("c") }),
                "enum e\r\n{\r\n    a,\r\n    b,\r\n    c\r\n}");

            VerifySyntax<EnumDeclarationSyntax>(
                g.EnumDeclaration("e", members: new[] { g.IdentifierName("a"), g.EnumMember("b"), g.IdentifierName("c") }),
                "enum e\r\n{\r\n    a,\r\n    b,\r\n    c\r\n}");

            VerifySyntax<EnumDeclarationSyntax>(
                g.EnumDeclaration("e", members: new[] { g.EnumMember("a", g.LiteralExpression(0)), g.EnumMember("b"), g.EnumMember("c", g.LiteralExpression(5)) }),
                "enum e\r\n{\r\n    a = 0,\r\n    b,\r\n    c = 5\r\n}");
        }

        [Fact]
        public void TestNamespaceImportDeclarations()
        {
            VerifySyntax<UsingDirectiveSyntax>(
                g.NamespaceImportDeclaration(g.IdentifierName("n")),
                "using n;");

            VerifySyntax<UsingDirectiveSyntax>(
                g.NamespaceImportDeclaration("n"),
                "using n;");

            VerifySyntax<UsingDirectiveSyntax>(
                g.NamespaceImportDeclaration("n.m"),
                "using n.m;");
        }

        [Fact]
        public void TestNamespaceDeclarations()
        {
            VerifySyntax<NamespaceDeclarationSyntax>(
                g.NamespaceDeclaration("n"),
                "namespace n\r\n{\r\n}");

            VerifySyntax<NamespaceDeclarationSyntax>(
                g.NamespaceDeclaration("n.m"),
                "namespace n.m\r\n{\r\n}");

            VerifySyntax<NamespaceDeclarationSyntax>(
                g.NamespaceDeclaration("n",
                    g.NamespaceImportDeclaration("m")),
                "namespace n\r\n{\r\n    using m;\r\n}");

            VerifySyntax<NamespaceDeclarationSyntax>(
                g.NamespaceDeclaration("n",
                    g.ClassDeclaration("c"),
                    g.NamespaceImportDeclaration("m")),
                "namespace n\r\n{\r\n    using m;\r\n\r\n    class c\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestCompilationUnits()
        {
            VerifySyntax<CompilationUnitSyntax>(
                g.CompilationUnit(),
                "");

            VerifySyntax<CompilationUnitSyntax>(
                g.CompilationUnit(
                    g.NamespaceDeclaration("n")),
                "namespace n\r\n{\r\n}");

            VerifySyntax<CompilationUnitSyntax>(
                g.CompilationUnit(
                    g.NamespaceImportDeclaration("n")),
                "using n;");

            VerifySyntax<CompilationUnitSyntax>(
                g.CompilationUnit(
                    g.ClassDeclaration("c"),
                    g.NamespaceImportDeclaration("m")),
                "using m;\r\n\r\nclass c\r\n{\r\n}");

            VerifySyntax<CompilationUnitSyntax>(
                g.CompilationUnit(
                    g.NamespaceImportDeclaration("n"),
                    g.NamespaceDeclaration("n",
                        g.NamespaceImportDeclaration("m"),
                        g.ClassDeclaration("c"))),
                "using n;\r\n\r\nnamespace n\r\n{\r\n    using m;\r\n\r\n    class c\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestAttributeDeclarations()
        {
            VerifySyntax<AttributeListSyntax>(
                g.Attribute(g.IdentifierName("a")),
                "[a]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a"),
                "[a]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a.b"),
                "[a.b]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a", new SyntaxNode[] { }),
                "[a()]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a", new[] { g.IdentifierName("x") }),
                "[a(x)]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a", new[] { g.AttributeArgument(g.IdentifierName("x")) }),
                "[a(x)]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a", new[] { g.AttributeArgument("x", g.IdentifierName("y")) }),
                "[a(x = y)]");

            VerifySyntax<AttributeListSyntax>(
                g.Attribute("a", new[] { g.IdentifierName("x"), g.IdentifierName("y") }),
                "[a(x, y)]");
        }

        [Fact]
        public void TestAddAttributes()
        {
            VerifySyntax<FieldDeclarationSyntax>(
                g.AddAttributes(
                    g.FieldDeclaration("y", g.IdentifierName("x")),
                    g.Attribute("a")),
                "[a]\r\nx y;");

            VerifySyntax<FieldDeclarationSyntax>(
                g.AddAttributes(
                    g.AddAttributes(
                        g.FieldDeclaration("y", g.IdentifierName("x")),
                        g.Attribute("a")),
                    g.Attribute("b")),
                "[a]\r\n[b]\r\nx y;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.AddAttributes(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), modifiers: SymbolModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract t m();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.AddReturnAttributes(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), modifiers: SymbolModifiers.Abstract),
                    g.Attribute("a")),
                "[return: a]\r\nabstract t m();");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.AddAttributes(
                    g.PropertyDeclaration("p", g.IdentifierName("x"), accessibility: Accessibility.NotApplicable, modifiers: SymbolModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract x p\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.AddAttributes(
                    g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: SymbolModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract x this[y z]\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.AddAttributes(
                    g.ClassDeclaration("c"),
                    g.Attribute("a")),
                "[a]\r\nclass c\r\n{\r\n}");

            VerifySyntax<ParameterSyntax>(
                g.AddAttributes(
                    g.ParameterDeclaration("p", g.IdentifierName("t")),
                    g.Attribute("a")),
                "[a]\r\nt p");

            VerifySyntax<CompilationUnitSyntax>(
                g.AddAttributes(
                    g.CompilationUnit(g.NamespaceDeclaration("n")),
                    g.Attribute("a")),
                "[assembly: a]\r\nnamespace n\r\n{\r\n}");
        }

        [Fact]
        public void TestWithTypeParameters()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract),
                    "a"),
            "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract)),
            "abstract void m();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract),
                    "a", "b"),
            "abstract void m<a, b>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract),
                    "a", "b")),
            "abstract void m();");

            VerifySyntax<ClassDeclarationSyntax>(
                g.WithTypeParameters(
                    g.ClassDeclaration("c"),
                    "a", "b"),
            "class c<a, b>\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.WithTypeParameters(
                    g.StructDeclaration("s"),
                    "a", "b"),
            "struct s<a, b>\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.WithTypeParameters(
                    g.InterfaceDeclaration("i"),
                    "a", "b"),
            "interface i<a, b>\r\n{\r\n}");
        }

        [Fact]
        public void TestWithTypeConstraint()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b")),
                "abstract void m<a>()where a : b;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")),
                "abstract void m<a>()where a : b, c;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a"),
                "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")), "a"),
                "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeConstraint(
                        g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a", "x"),
                        "a", g.IdentifierName("b"), g.IdentifierName("c")),
                    "x", g.IdentifierName("y")),
                "abstract void m<a, x>()where a : b, c where x : y;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.Constructor),
                "abstract void m<a>()where a : new ();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType),
                "abstract void m<a>()where a : class;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType),
                "abstract void m<a>()where a : struct;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType | SpecialTypeConstraintKind.Constructor),
                "abstract void m<a>()where a : class, new ();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType | SpecialTypeConstraintKind.ValueType),
                "abstract void m<a>()where a : class;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: SymbolModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType, g.IdentifierName("b"), g.IdentifierName("c")),
                "abstract void m<a>()where a : class, b, c;");

            // type declarations
            VerifySyntax<ClassDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.ClassDeclaration("c"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
            "class c<a, b>\r\n    where a : x\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.StructDeclaration("s"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
            "struct s<a, b>\r\n    where a : x\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.InterfaceDeclaration("i"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
            "interface i<a, b>\r\n    where a : x\r\n{\r\n}");
        }
    }
}