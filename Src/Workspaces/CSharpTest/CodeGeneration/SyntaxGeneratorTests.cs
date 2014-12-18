using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGeneration
{
    public class SyntaxGeneratorTests
    {
        private readonly SyntaxGenerator g = SyntaxGenerator.GetGenerator(new CustomWorkspace(), LanguageNames.CSharp);

        private readonly CSharpCompilation emptyCompilation = CSharpCompilation.Create("empty",
                references: new[] { TestReferences.NetFx.v4_0_30319.mscorlib });

        private readonly INamedTypeSymbol ienumerableInt;

        public SyntaxGeneratorTests()
        {
            this.ienumerableInt = emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(emptyCompilation.GetSpecialType(SpecialType.System_Int32));
        }

        public Compilation Compile(string code)
        {
            return CSharpCompilation.Create("test")
                .AddReferences(TestReferences.NetFx.v4_0_30319.mscorlib)
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code));
        }

        private void VerifySyntax<TSyntax>(SyntaxNode node, string expectedText) where TSyntax : SyntaxNode
        {
            Assert.IsAssignableFrom(typeof(TSyntax), node);
            var normalized = node.NormalizeWhitespace().ToFullString();
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
            VerifySyntax<BinaryExpressionSyntax>(g.IsTypeExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) is y");
            VerifySyntax<BinaryExpressionSyntax>(g.TryCastExpression(g.IdentifierName("x"), g.IdentifierName("y")), "(x) as y");
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
            VerifySyntax<AssignmentExpressionSyntax>(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y")), "x = (y)");
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
                g.FieldDeclaration("fld", g.TypeExpression(SpecialType.System_Int32), accessibility: Accessibility.NotApplicable, modifiers: DeclarationModifiers.Static | DeclarationModifiers.ReadOnly),
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
                g.MethodDeclaration("m", returnType: g.IdentifierName("x"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Abstract),
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
                g.ConstructorDeclaration("c", accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Static),
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
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract | DeclarationModifiers.ReadOnly),
                "abstract x p\r\n{\r\n    get;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly),
                "x p\r\n{\r\n    get\r\n    {\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract),
                "abstract x p\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly, getAccessorStatements: new[] { g.IdentifierName("y") }),
                "x p\r\n{\r\n    get\r\n    {\r\n        y;\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.PropertyDeclaration("p", g.IdentifierName("x"), setAccessorStatements: new[] { g.IdentifierName("y") }),
                "x p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n        y;\r\n    }\r\n}");
        }

        [Fact]
        public void TestIndexerDeclarations()
        {
            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract | DeclarationModifiers.ReadOnly),
                "abstract x this[y z]\r\n{\r\n    get;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract),
                "abstract x this[y z]\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly,
                    getAccessorStatements: new[] { g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x")),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"),
                    setAccessorStatements: new[] { g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"),
                    getAccessorStatements: new[] { g.IdentifierName("a") }, setAccessorStatements: new[] { g.IdentifierName("b") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n        a;\r\n    }\r\n\r\n    set\r\n    {\r\n        b;\r\n    }\r\n}");
        }

        [Fact]
        public void TestEventFieldDeclarations()
        {
            VerifySyntax<EventFieldDeclarationSyntax>(
                g.EventDeclaration("ef", g.IdentifierName("t")),
                "event t ef;");

            VerifySyntax<EventFieldDeclarationSyntax>(
                g.EventDeclaration("ef", g.IdentifierName("t"), accessibility: Accessibility.Public),
                "public event t ef;");

            VerifySyntax<EventFieldDeclarationSyntax>(
                g.EventDeclaration("ef", g.IdentifierName("t"), modifiers: DeclarationModifiers.Static),
                "static event t ef;");
        }

        [Fact]
        public void TestEventPropertyDeclarations()
        {
            VerifySyntax<EventDeclarationSyntax>(
                g.CustomEventDeclaration("ep", g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                "abstract event t ep\r\n{\r\n    add;\r\n    remove;\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                g.CustomEventDeclaration("ep", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Abstract),
                "public abstract event t ep\r\n{\r\n    add;\r\n    remove;\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                g.CustomEventDeclaration("ep", g.IdentifierName("t")),
                "event t ep\r\n{\r\n    add\r\n    {\r\n    }\r\n\r\n    remove\r\n    {\r\n    }\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                g.CustomEventDeclaration("ep", g.IdentifierName("t"), addAccessorStatements: new[] { g.IdentifierName("s") }, removeAccessorStatements: new[] { g.IdentifierName("s2") }),
                "event t ep\r\n{\r\n    add\r\n    {\r\n        s;\r\n    }\r\n\r\n    remove\r\n    {\r\n        s2;\r\n    }\r\n}");
        }

        [Fact]
        public void TestAsPublicInterfaceImplementation()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.AsPublicInterfaceImplementation(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "public t m()\r\n{\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.AsPublicInterfaceImplementation(
                    g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "public t p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.AsPublicInterfaceImplementation(
                    g.IndexerDeclaration(parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("a")) }, type: g.IdentifierName("t"), accessibility: Accessibility.Internal, modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "public t this[a p]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestAsPrivateInterfaceImplementation()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "t i.m()\r\n{\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Internal, modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "t i.p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.IndexerDeclaration(parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("a")) }, type: g.IdentifierName("t"), accessibility: Accessibility.Protected, modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "t i.this[a p]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                g.AsPrivateInterfaceImplementation(
                    g.CustomEventDeclaration("ep", g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    g.IdentifierName("i")),
                "event t i.ep\r\n{\r\n    add\r\n    {\r\n    }\r\n\r\n    remove\r\n    {\r\n    }\r\n}");
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
                g.InterfaceDeclaration("i", members: new[] { g.MethodDeclaration("m", returnType: g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t m();\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t p\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.ReadOnly) }),
                "interface i\r\n{\r\n    t p\r\n    {\r\n        get;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.IndexerDeclaration(new[] { g.ParameterDeclaration("y", g.IdentifierName("x")) }, g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t this[x y]\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.IndexerDeclaration(new[] { g.ParameterDeclaration("y", g.IdentifierName("x")) }, g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.ReadOnly) }),
                "interface i\r\n{\r\n    t this[x y]\r\n    {\r\n        get;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.CustomEventDeclaration("ep", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Static) }),
                "interface i\r\n{\r\n    event t ep\r\n    {\r\n        add;\r\n        remove;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.EventDeclaration("ef", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Static) }),
                "interface i\r\n{\r\n    event t ef\r\n    {\r\n        add;\r\n        remove;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.InterfaceDeclaration("i", members: new[] { g.FieldDeclaration("f", g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t f\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");
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
        public void TestDelegateDeclarations()
        {
            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d"),
                "delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d", returnType: g.IdentifierName("t")),
                "delegate t d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d", returnType: g.IdentifierName("t"), parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("pt")) }),
                "delegate t d(pt p);");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d", accessibility: Accessibility.Public),
                "public delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d", accessibility: Accessibility.Public),
                "public delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d", modifiers: DeclarationModifiers.New),
                "new delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.DelegateDeclaration("d", typeParameters: new[] { "T", "S" }),
                "delegate void d<T, S>();");
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
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract t m();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.AddReturnAttributes(
                    g.MethodDeclaration("m", returnType: g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    g.Attribute("a")),
                "[return: a]\r\nabstract t m();");

            VerifySyntax<PropertyDeclarationSyntax>(
                g.AddAttributes(
                    g.PropertyDeclaration("p", g.IdentifierName("x"), accessibility: Accessibility.NotApplicable, modifiers: DeclarationModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract x p\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                g.AddAttributes(
                    g.IndexerDeclaration(new[] { g.ParameterDeclaration("z", g.IdentifierName("y")) }, g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract x this[y z]\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                g.AddAttributes(
                    g.CustomEventDeclaration("ep", g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    g.Attribute("a")),
                "[a]\r\nabstract event t ep\r\n{\r\n    add;\r\n    remove;\r\n}");

            VerifySyntax<EventFieldDeclarationSyntax>(
                g.AddAttributes(
                    g.EventDeclaration("ef", g.IdentifierName("t")),
                    g.Attribute("a")),
                "[a]\r\nevent t ef;");

            VerifySyntax<ClassDeclarationSyntax>(
                g.AddAttributes(
                    g.ClassDeclaration("c"),
                    g.Attribute("a")),
                "[a]\r\nclass c\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                g.AddAttributes(
                    g.StructDeclaration("s"),
                    g.Attribute("a")),
                "[a]\r\nstruct s\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                g.AddAttributes(
                    g.InterfaceDeclaration("i"),
                    g.Attribute("a")),
                "[a]\r\ninterface i\r\n{\r\n}");

            VerifySyntax<DelegateDeclarationSyntax>(
                g.AddAttributes(
                    g.DelegateDeclaration("d"),
                    g.Attribute("a")),
                "[a]\r\ndelegate void d();");

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
        public void TestAddRemoveAttributesPerservesTrivia()
        {
            var cls = SyntaxFactory.ParseCompilationUnit(@"// comment
public class C { } // end").Members[0];

            var added = g.AddAttributes(cls, g.Attribute("a"));
            VerifySyntax<ClassDeclarationSyntax>(added, "// comment\r\n[a]\r\npublic class C\r\n{\r\n} // end\r\n");

            var removed = g.RemoveAllAttributes(added);
            VerifySyntax<ClassDeclarationSyntax>(removed, "// comment\r\npublic class C\r\n{\r\n} // end\r\n");

            var attrWithComment = g.GetAttributes(added).First();
            VerifySyntax<AttributeListSyntax>(attrWithComment, "// comment\r\n[a]\r\n");

            // added attributes are stripped of trivia
            var added2 = g.AddAttributes(cls, attrWithComment);
            VerifySyntax<ClassDeclarationSyntax>(added2, "// comment\r\n[a]\r\npublic class C\r\n{\r\n} // end\r\n");
        }

        [Fact]
        public void TestWithTypeParameters()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract),
                    "a"),
            "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract)),
            "abstract void m();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract),
                    "a", "b"),
            "abstract void m<a, b>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeParameters(g.WithTypeParameters(
                    g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract),
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

            VerifySyntax<DelegateDeclarationSyntax>(
                g.WithTypeParameters(
                    g.DelegateDeclaration("d"),
                    "a", "b"),
            "delegate void d<a, b>();");
        }

        [Fact]
        public void TestWithTypeConstraint()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b")),
                "abstract void m<a>()where a : b;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")),
                "abstract void m<a>()where a : b, c;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a"),
                "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", g.IdentifierName("b"), g.IdentifierName("c")), "a"),
                "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeConstraint(
                        g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a", "x"),
                        "a", g.IdentifierName("b"), g.IdentifierName("c")),
                    "x", g.IdentifierName("y")),
                "abstract void m<a, x>()where a : b, c where x : y;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.Constructor),
                "abstract void m<a>()where a : new ();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType),
                "abstract void m<a>()where a : class;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType),
                "abstract void m<a>()where a : struct;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType | SpecialTypeConstraintKind.Constructor),
                "abstract void m<a>()where a : class, new ();");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType | SpecialTypeConstraintKind.ValueType),
                "abstract void m<a>()where a : class;");

            VerifySyntax<MethodDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
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

            VerifySyntax<DelegateDeclarationSyntax>(
                g.WithTypeConstraint(
                    g.WithTypeParameters(
                        g.DelegateDeclaration("d"),
                        "a", "b"),
                    "a", g.IdentifierName("x")),
            "delegate void d<a, b>()where a : x;");
        }

        private void AssertNamesEqual(string[] expectedNames, IEnumerable<SyntaxNode> actualNodes)
        {
            var actualNames = actualNodes.Select(n => g.GetName(n)).ToArray();
            var expected = string.Join(", ", expectedNames);
            var actual = string.Join(", ", actualNames);
            Assert.Equal(expected, actual);
        }

        private void AssertNamesEqual(string name, IEnumerable<SyntaxNode> actualNodes)
        {
            AssertNamesEqual(new[] { name }, actualNodes);
        }

        private void AssertMemberNamesEqual(string[] expectedNames, SyntaxNode declaration)
        {
            AssertNamesEqual(expectedNames, g.GetMembers(declaration));
        }

        private void AssertMemberNamesEqual(string expectedName, SyntaxNode declaration)
        {
            AssertNamesEqual(new[] { expectedName }, g.GetMembers(declaration));
        }

        [Fact]
        public void TestAddNamespaceImports()
        {
            AssertNamesEqual("x.y", g.GetNamespaceImports(g.AddNamespaceImports(g.CompilationUnit(), g.NamespaceImportDeclaration("x.y"))));
            AssertNamesEqual(new[] { "x.y", "z" }, g.GetNamespaceImports(g.AddNamespaceImports(g.CompilationUnit(), g.NamespaceImportDeclaration("x.y"), g.IdentifierName("z"))));
            AssertNamesEqual("", g.GetNamespaceImports(g.AddNamespaceImports(g.CompilationUnit(), g.MethodDeclaration("m"))));
            AssertNamesEqual(new[] { "x", "y.z" }, g.GetNamespaceImports(g.AddNamespaceImports(g.CompilationUnit(g.IdentifierName("x")), g.DottedName("y.z"))));
        }

        [Fact]
        public void TestRemoveNamespaceImports()
        {
            TestRemoveAllNamespaceImports(g.CompilationUnit(g.NamespaceImportDeclaration("x")));
            TestRemoveAllNamespaceImports(g.CompilationUnit(g.NamespaceImportDeclaration("x"), g.IdentifierName("y")));

            TestRemoveNamespaceImport(g.CompilationUnit(g.NamespaceImportDeclaration("x")), "x", new string[] { });
            TestRemoveNamespaceImport(g.CompilationUnit(g.NamespaceImportDeclaration("x"), g.IdentifierName("y")), "x", new[] { "y" });
            TestRemoveNamespaceImport(g.CompilationUnit(g.NamespaceImportDeclaration("x"), g.IdentifierName("y")), "y", new[] { "x" });
        }

        private void TestRemoveAllNamespaceImports(SyntaxNode declaration)
        {
            Assert.Equal(0, g.GetNamespaceImports(g.RemoveDeclarations(declaration, g.GetNamespaceImports(declaration))).Count);
        }

        private void TestRemoveNamespaceImport(SyntaxNode declaration, string name, string[] remainingNames)
        {
            var newDecl = g.RemoveDeclaration(declaration, g.GetNamespaceImports(declaration).First(m => g.GetName(m) == name));
            AssertNamesEqual(remainingNames, g.GetNamespaceImports(newDecl));
        }

        [Fact]
        public void TestAddMembers()
        {
            AssertMemberNamesEqual("m", g.AddMembers(g.ClassDeclaration("d"), new[] { g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", g.AddMembers(g.StructDeclaration("s"), new[] { g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", g.AddMembers(g.InterfaceDeclaration("i"), new[] { g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("v", g.AddMembers(g.EnumDeclaration("e"), new[] { g.EnumMember("v") }));
            AssertMemberNamesEqual("n2", g.AddMembers(g.NamespaceDeclaration("n"), new[] { g.NamespaceDeclaration("n2") }));
            AssertMemberNamesEqual("n", g.AddMembers(g.CompilationUnit(), new[] { g.NamespaceDeclaration("n") }));

            AssertMemberNamesEqual(new[] { "m", "m2" }, g.AddMembers(g.ClassDeclaration("d", members: new[] { g.MethodDeclaration("m") }), new[] { g.MethodDeclaration("m2") }));
            AssertMemberNamesEqual(new[] { "m", "m2" }, g.AddMembers(g.StructDeclaration("s", members: new[] { g.MethodDeclaration("m") }), new[] { g.MethodDeclaration("m2") }));
            AssertMemberNamesEqual(new[] { "m", "m2" }, g.AddMembers(g.InterfaceDeclaration("i", members: new[] { g.MethodDeclaration("m") }), new[] { g.MethodDeclaration("m2") }));
            AssertMemberNamesEqual(new[] { "v", "v2" }, g.AddMembers(g.EnumDeclaration("i", members: new[] { g.EnumMember("v") }), new[] { g.EnumMember("v2") }));
            AssertMemberNamesEqual(new[] { "n1", "n2" }, g.AddMembers(g.NamespaceDeclaration("n", new[] { g.NamespaceDeclaration("n1") }), new[] { g.NamespaceDeclaration("n2") }));
            AssertMemberNamesEqual(new[] { "n1", "n2" }, g.AddMembers(g.CompilationUnit(declarations: new[] { g.NamespaceDeclaration("n1") }), new[] { g.NamespaceDeclaration("n2") }));
        }

        [Fact]
        public void TestRemoveMembers()
        {
            // remove all members
            TestRemoveAllMembers(g.ClassDeclaration("c", members: new[] { g.MethodDeclaration("m") }));
            TestRemoveAllMembers(g.StructDeclaration("s", members: new[] { g.MethodDeclaration("m") }));
            TestRemoveAllMembers(g.InterfaceDeclaration("i", members: new[] { g.MethodDeclaration("m") }));
            TestRemoveAllMembers(g.EnumDeclaration("i", members: new[] { g.EnumMember("v") }));
            TestRemoveAllMembers(g.NamespaceDeclaration("n", new[] { g.NamespaceDeclaration("n") }));
            TestRemoveAllMembers(g.CompilationUnit(declarations: new[] { g.NamespaceDeclaration("n") }));

            TestRemoveMember(g.ClassDeclaration("c", members: new[] { g.MethodDeclaration("m1"), g.MethodDeclaration("m2") }), "m1", new[] { "m2" });
            TestRemoveMember(g.StructDeclaration("s", members: new[] { g.MethodDeclaration("m1"), g.MethodDeclaration("m2") }), "m1", new[] { "m2" });
        }

        private void TestRemoveAllMembers(SyntaxNode declaration)
        {
            Assert.Equal(0, g.GetMembers(g.RemoveDeclarations(declaration, g.GetMembers(declaration))).Count);
        }

        private void TestRemoveMember(SyntaxNode declaration, string name, string[] remainingNames)
        {
            var newDecl = g.RemoveDeclaration(declaration, g.GetMembers(declaration).First(m => g.GetName(m) == name));
            AssertMemberNamesEqual(remainingNames, newDecl);
        }

        [Fact]
        public void TestGetMembers()
        {
            AssertMemberNamesEqual("m", g.ClassDeclaration("c", members: new[] { g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", g.StructDeclaration("s", members: new[] { g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", g.InterfaceDeclaration("i", members: new[] { g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("v", g.EnumDeclaration("e", members: new[] { g.EnumMember("v") }));
            AssertMemberNamesEqual("c", g.NamespaceDeclaration("n", declarations: new[] { g.ClassDeclaration("c") }));
            AssertMemberNamesEqual("c", g.CompilationUnit(declarations: new[] { g.ClassDeclaration("c") }));
        }

        [Fact]
        public void TestGetDeclarationKind()
        {
            Assert.Equal(DeclarationKind.CompilationUnit, g.GetDeclarationKind(g.CompilationUnit()));
            Assert.Equal(DeclarationKind.Class, g.GetDeclarationKind(g.ClassDeclaration("c")));
            Assert.Equal(DeclarationKind.Struct, g.GetDeclarationKind(g.StructDeclaration("s")));
            Assert.Equal(DeclarationKind.Interface, g.GetDeclarationKind(g.InterfaceDeclaration("i")));
            Assert.Equal(DeclarationKind.Enum, g.GetDeclarationKind(g.EnumDeclaration("e")));
            Assert.Equal(DeclarationKind.Delegate, g.GetDeclarationKind(g.DelegateDeclaration("d")));
            Assert.Equal(DeclarationKind.Method, g.GetDeclarationKind(g.MethodDeclaration("m")));
            Assert.Equal(DeclarationKind.Constructor, g.GetDeclarationKind(g.ConstructorDeclaration()));
            Assert.Equal(DeclarationKind.Parameter, g.GetDeclarationKind(g.ParameterDeclaration("p")));
            Assert.Equal(DeclarationKind.Property, g.GetDeclarationKind(g.PropertyDeclaration("p", g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.Indexer, g.GetDeclarationKind(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(g.FieldDeclaration("f", g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.EnumMember, g.GetDeclarationKind(g.EnumMember("v")));
            Assert.Equal(DeclarationKind.Event, g.GetDeclarationKind(g.EventDeclaration("ef", g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.CustomEvent, g.GetDeclarationKind(g.CustomEventDeclaration("e", g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.Namespace, g.GetDeclarationKind(g.NamespaceDeclaration("n")));
            Assert.Equal(DeclarationKind.NamespaceImport, g.GetDeclarationKind(g.NamespaceImportDeclaration("u")));
            Assert.Equal(DeclarationKind.Variable, g.GetDeclarationKind(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc")));
            Assert.Equal(DeclarationKind.Attribute, g.GetDeclarationKind(g.Attribute("a")));
        }

        [Fact]
        public void TestGetName()
        {
            Assert.Equal("c", g.GetName(g.ClassDeclaration("c")));
            Assert.Equal("s", g.GetName(g.StructDeclaration("s")));
            Assert.Equal("i", g.GetName(g.EnumDeclaration("i")));
            Assert.Equal("e", g.GetName(g.EnumDeclaration("e")));
            Assert.Equal("d", g.GetName(g.DelegateDeclaration("d")));
            Assert.Equal("m", g.GetName(g.MethodDeclaration("m")));
            Assert.Equal("", g.GetName(g.ConstructorDeclaration()));
            Assert.Equal("p", g.GetName(g.ParameterDeclaration("p")));
            Assert.Equal("p", g.GetName(g.PropertyDeclaration("p", g.IdentifierName("t"))));
            Assert.Equal("", g.GetName(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t"))));
            Assert.Equal("f", g.GetName(g.FieldDeclaration("f", g.IdentifierName("t"))));
            Assert.Equal("v", g.GetName(g.EnumMember("v")));
            Assert.Equal("ef", g.GetName(g.EventDeclaration("ef", g.IdentifierName("t"))));
            Assert.Equal("ep", g.GetName(g.CustomEventDeclaration("ep", g.IdentifierName("t"))));
            Assert.Equal("n", g.GetName(g.NamespaceDeclaration("n")));
            Assert.Equal("u", g.GetName(g.NamespaceImportDeclaration("u")));
            Assert.Equal("loc", g.GetName(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc")));
            Assert.Equal("a", g.GetName(g.Attribute("a")));
        }

        [Fact]
        public void TestWithtName()
        {
            Assert.Equal("c", g.GetName(g.WithName(g.ClassDeclaration("x"), "c")));
            Assert.Equal("s", g.GetName(g.WithName(g.StructDeclaration("x"), "s")));
            Assert.Equal("i", g.GetName(g.WithName(g.EnumDeclaration("x"), "i")));
            Assert.Equal("e", g.GetName(g.WithName(g.EnumDeclaration("x"), "e")));
            Assert.Equal("d", g.GetName(g.WithName(g.DelegateDeclaration("x"), "d")));
            Assert.Equal("m", g.GetName(g.WithName(g.MethodDeclaration("x"), "m")));
            Assert.Equal("", g.GetName(g.WithName(g.ConstructorDeclaration(), ".ctor")));
            Assert.Equal("p", g.GetName(g.WithName(g.ParameterDeclaration("x"), "p")));
            Assert.Equal("p", g.GetName(g.WithName(g.PropertyDeclaration("x", g.IdentifierName("t")), "p")));
            Assert.Equal("", g.GetName(g.WithName(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t")), "this")));
            Assert.Equal("f", g.GetName(g.WithName(g.FieldDeclaration("x", g.IdentifierName("t")), "f")));
            Assert.Equal("v", g.GetName(g.WithName(g.EnumMember("x"), "v")));
            Assert.Equal("ef", g.GetName(g.WithName(g.EventDeclaration("x", g.IdentifierName("t")), "ef")));
            Assert.Equal("ep", g.GetName(g.WithName(g.CustomEventDeclaration("x", g.IdentifierName("t")), "ep")));
            Assert.Equal("n", g.GetName(g.WithName(g.NamespaceDeclaration("x"), "n")));
            Assert.Equal("u", g.GetName(g.WithName(g.NamespaceImportDeclaration("x"), "u")));
            Assert.Equal("loc", g.GetName(g.WithName(g.LocalDeclarationStatement(g.IdentifierName("t"), "x"), "loc")));
            Assert.Equal("a", g.GetName(g.WithName(g.Attribute("x"), "a")));
        }

        [Fact]
        public void TestGetAccessibility()
        {
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.ClassDeclaration("c", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.StructDeclaration("s", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.EnumDeclaration("i", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.EnumDeclaration("e", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.DelegateDeclaration("d", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.MethodDeclaration("m", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.ConstructorDeclaration(accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.ParameterDeclaration("p")));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.FieldDeclaration("f", g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.EnumMember("v")));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.EventDeclaration("ef", g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, g.GetAccessibility(g.CustomEventDeclaration("ep", g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.NamespaceDeclaration("n")));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.NamespaceImportDeclaration("u")));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc")));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.Attribute("a")));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(SyntaxFactory.TypeParameter("tp")));
        }

        [Fact]
        public void TestWithAccessibilty()
        {
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.ClassDeclaration("c", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.StructDeclaration("s", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.EnumDeclaration("i", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.EnumDeclaration("e", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.DelegateDeclaration("d", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.MethodDeclaration("m", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.ConstructorDeclaration(accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(g.ParameterDeclaration("p"), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.PropertyDeclaration("p", g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.FieldDeclaration("f", g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(g.EnumMember("v"), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.EventDeclaration("ef", g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, g.GetAccessibility(g.WithAccessibility(g.CustomEventDeclaration("ep", g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(g.NamespaceDeclaration("n"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(g.NamespaceImportDeclaration("u"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(g.Attribute("a"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, g.GetAccessibility(g.WithAccessibility(SyntaxFactory.TypeParameter("tp"), Accessibility.Private)));
        }

        [Fact]
        public void TestGetModifiers()
        {
            Assert.Equal(DeclarationModifiers.Abstract, g.GetModifiers(g.ClassDeclaration("c", modifiers: DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Partial, g.GetModifiers(g.StructDeclaration("s", modifiers: DeclarationModifiers.Partial)));
            Assert.Equal(DeclarationModifiers.New, g.GetModifiers(g.EnumDeclaration("e", modifiers: DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.New, g.GetModifiers(g.DelegateDeclaration("d", modifiers: DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.MethodDeclaration("m", modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.ConstructorDeclaration(modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.ParameterDeclaration("p")));
            Assert.Equal(DeclarationModifiers.Abstract, g.GetModifiers(g.PropertyDeclaration("p", g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Abstract, g.GetModifiers(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Const, g.GetModifiers(g.FieldDeclaration("f", g.IdentifierName("t"), modifiers: DeclarationModifiers.Const)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.EventDeclaration("ef", g.IdentifierName("t"), modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.CustomEventDeclaration("ep", g.IdentifierName("t"), modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.EnumMember("v")));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.NamespaceDeclaration("n")));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.NamespaceImportDeclaration("u")));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc")));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.Attribute("a")));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(SyntaxFactory.TypeParameter("tp")));
        }

        [Fact]
        public void TestWithModifiers()
        {
            Assert.Equal(DeclarationModifiers.Abstract, g.GetModifiers(g.WithModifiers(g.ClassDeclaration("c"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Partial, g.GetModifiers(g.WithModifiers(g.StructDeclaration("s"), DeclarationModifiers.Partial)));
            Assert.Equal(DeclarationModifiers.New, g.GetModifiers(g.WithModifiers(g.EnumDeclaration("e"), DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.New, g.GetModifiers(g.WithModifiers(g.DelegateDeclaration("d"), DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.WithModifiers(g.MethodDeclaration("m"), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.WithModifiers(g.ConstructorDeclaration(), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(g.ParameterDeclaration("p"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Abstract, g.GetModifiers(g.WithModifiers(g.PropertyDeclaration("p", g.IdentifierName("t")), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Abstract, g.GetModifiers(g.WithModifiers(g.IndexerDeclaration(new[] { g.ParameterDeclaration("i") }, g.IdentifierName("t")), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Const, g.GetModifiers(g.WithModifiers(g.FieldDeclaration("f", g.IdentifierName("t")), DeclarationModifiers.Const)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.WithModifiers(g.EventDeclaration("ef", g.IdentifierName("t")), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(g.WithModifiers(g.CustomEventDeclaration("ep", g.IdentifierName("t")), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(g.EnumMember("v"), DeclarationModifiers.Partial)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(g.NamespaceDeclaration("n"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(g.NamespaceImportDeclaration("u"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(g.Attribute("a"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, g.GetModifiers(g.WithModifiers(SyntaxFactory.TypeParameter("tp"), DeclarationModifiers.Abstract)));
        }

        [Fact]
        public void TestGetType()
        {
            Assert.Equal("t", g.GetType(g.MethodDeclaration("m", returnType: g.IdentifierName("t"))).ToString());
            Assert.Null(g.GetType(g.MethodDeclaration("m")));

            Assert.Equal("t", g.GetType(g.FieldDeclaration("f", g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.PropertyDeclaration("p", g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.IndexerDeclaration(new[] { g.ParameterDeclaration("p", g.IdentifierName("pt")) }, g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.ParameterDeclaration("p", g.IdentifierName("t"))).ToString());

            Assert.Equal("t", g.GetType(g.EventDeclaration("ef", g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.CustomEventDeclaration("ep", g.IdentifierName("t"))).ToString());

            Assert.Equal("t", g.GetType(g.DelegateDeclaration("t", returnType: g.IdentifierName("t"))).ToString());
            Assert.Null(g.GetType(g.DelegateDeclaration("d")));

            Assert.Equal("t", g.GetType(g.LocalDeclarationStatement(g.IdentifierName("t"), "v")).ToString());

            Assert.Null(g.GetType(g.ClassDeclaration("c")));
            Assert.Null(g.GetType(g.IdentifierName("x")));
        }

        [Fact]
        public void TestWithType()
        {
            Assert.Equal("t", g.GetType(g.WithType(g.MethodDeclaration("m", returnType: g.IdentifierName("x")), g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.WithType(g.FieldDeclaration("f", g.IdentifierName("x")), g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.WithType(g.PropertyDeclaration("p", g.IdentifierName("x")), g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.WithType(g.IndexerDeclaration(new[] { g.ParameterDeclaration("p", g.IdentifierName("pt")) }, g.IdentifierName("x")), g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.WithType(g.ParameterDeclaration("p", g.IdentifierName("x")), g.IdentifierName("t"))).ToString());

            Assert.Equal("t", g.GetType(g.WithType(g.DelegateDeclaration("t"), g.IdentifierName("t"))).ToString());

            Assert.Equal("t", g.GetType(g.WithType(g.EventDeclaration("ef", g.IdentifierName("x")), g.IdentifierName("t"))).ToString());
            Assert.Equal("t", g.GetType(g.WithType(g.CustomEventDeclaration("ep", g.IdentifierName("x")), g.IdentifierName("t"))).ToString());

            Assert.Equal("t", g.GetType(g.WithType(g.LocalDeclarationStatement(g.IdentifierName("x"), "v"), g.IdentifierName("t"))).ToString());
            Assert.Null(g.GetType(g.WithType(g.ClassDeclaration("c"), g.IdentifierName("t"))));
            Assert.Null(g.GetType(g.WithType(g.IdentifierName("x"), g.IdentifierName("t"))));
        }

        [Fact]
        public void TestGetParameters()
        {
            Assert.Equal(0, g.GetParameters(g.MethodDeclaration("m")).Count);
            Assert.Equal(1, g.GetParameters(g.MethodDeclaration("m", parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);
            Assert.Equal(2, g.GetParameters(g.MethodDeclaration("m", parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("t")), g.ParameterDeclaration("p2", g.IdentifierName("t2")) })).Count);

            Assert.Equal(0, g.GetParameters(g.ConstructorDeclaration()).Count);
            Assert.Equal(1, g.GetParameters(g.ConstructorDeclaration(parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);
            Assert.Equal(2, g.GetParameters(g.ConstructorDeclaration(parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("t")), g.ParameterDeclaration("p2", g.IdentifierName("t2")) })).Count);

            Assert.Equal(1, g.GetParameters(g.IndexerDeclaration(new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) }, g.IdentifierName("t"))).Count);
            Assert.Equal(2, g.GetParameters(g.IndexerDeclaration(new[] { g.ParameterDeclaration("p", g.IdentifierName("t")), g.ParameterDeclaration("p2", g.IdentifierName("t2")) }, g.IdentifierName("t"))).Count);

            Assert.Equal(0, g.GetParameters(g.ValueReturningLambdaExpression(g.IdentifierName("expr"))).Count);
            Assert.Equal(1, g.GetParameters(g.ValueReturningLambdaExpression("p1", g.IdentifierName("expr"))).Count);

            Assert.Equal(0, g.GetParameters(g.VoidReturningLambdaExpression(g.IdentifierName("expr"))).Count);
            Assert.Equal(1, g.GetParameters(g.VoidReturningLambdaExpression("p1", g.IdentifierName("expr"))).Count);

            Assert.Equal(0, g.GetParameters(g.DelegateDeclaration("d")).Count);
            Assert.Equal(1, g.GetParameters(g.DelegateDeclaration("d", parameters: new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);

            Assert.Equal(0, g.GetParameters(g.ClassDeclaration("c")).Count);
            Assert.Equal(0, g.GetParameters(g.IdentifierName("x")).Count);
        }

        [Fact]
        public void TestAddParameters()
        {
            Assert.Equal(1, g.GetParameters(g.AddParameters(g.MethodDeclaration("m"), new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);
            Assert.Equal(1, g.GetParameters(g.AddParameters(g.ConstructorDeclaration(), new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);
            Assert.Equal(3, g.GetParameters(g.AddParameters(g.IndexerDeclaration(new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) }, g.IdentifierName("t")), new[] { g.ParameterDeclaration("p2", g.IdentifierName("t2")), g.ParameterDeclaration("p3", g.IdentifierName("t3")) })).Count);

            Assert.Equal(1, g.GetParameters(g.AddParameters(g.ValueReturningLambdaExpression(g.IdentifierName("expr")), new[] { g.LambdaParameter("p") })).Count);
            Assert.Equal(1, g.GetParameters(g.AddParameters(g.VoidReturningLambdaExpression(g.IdentifierName("expr")), new[] { g.LambdaParameter("p") })).Count);

            Assert.Equal(1, g.GetParameters(g.AddParameters(g.DelegateDeclaration("d"), new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);

            Assert.Equal(0, g.GetParameters(g.AddParameters(g.ClassDeclaration("c"), new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);
            Assert.Equal(0, g.GetParameters(g.AddParameters(g.IdentifierName("x"), new[] { g.ParameterDeclaration("p", g.IdentifierName("t")) })).Count);
        }

        [Fact]
        public void TestGetExpression()
        {
            // initializers
            Assert.Equal("x", g.GetExpression(g.FieldDeclaration("f", g.IdentifierName("t"), initializer: g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.ParameterDeclaration("p", g.IdentifierName("t"), initializer: g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.LocalDeclarationStatement("loc", initializer: g.IdentifierName("x"))).ToString());

            // lambda bodies
            Assert.Null(g.GetExpression(g.ValueReturningLambdaExpression("p", new[] { g.IdentifierName("x") })));
            Assert.Equal(1, g.GetStatements(g.ValueReturningLambdaExpression("p", new[] { g.IdentifierName("x") })).Count);
            Assert.Equal("x", g.GetExpression(g.ValueReturningLambdaExpression(g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.VoidReturningLambdaExpression(g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.ValueReturningLambdaExpression("p", g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.VoidReturningLambdaExpression("p", g.IdentifierName("x"))).ToString());

            Assert.Null(g.GetExpression(g.IdentifierName("e")));
        }

        [Fact]
        public void TestWithExpression()
        {
            // initializers
            Assert.Equal("x", g.GetExpression(g.WithExpression(g.FieldDeclaration("f", g.IdentifierName("t")), g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.WithExpression(g.ParameterDeclaration("p", g.IdentifierName("t")), g.IdentifierName("x"))).ToString());
            Assert.Equal("x", g.GetExpression(g.WithExpression(g.LocalDeclarationStatement(g.IdentifierName("t"), "loc"), g.IdentifierName("x"))).ToString());

            // lambda bodies
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.ValueReturningLambdaExpression("p", new[] { g.IdentifierName("x") }), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.VoidReturningLambdaExpression("p", new[] { g.IdentifierName("x") }), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.ValueReturningLambdaExpression(new[] { g.IdentifierName("x") }), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.VoidReturningLambdaExpression(new[] { g.IdentifierName("x") }), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.ValueReturningLambdaExpression("p", g.IdentifierName("x")), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.VoidReturningLambdaExpression("p", g.IdentifierName("x")), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.ValueReturningLambdaExpression(g.IdentifierName("x")), g.IdentifierName("y"))).ToString());
            Assert.Equal("y", g.GetExpression(g.WithExpression(g.VoidReturningLambdaExpression(g.IdentifierName("x")), g.IdentifierName("y"))).ToString());

            Assert.Null(g.GetExpression(g.WithExpression(g.IdentifierName("e"), g.IdentifierName("x"))));
        }

        [Fact]
        public void TestGetStatements()
        {
            var stmts = new[]
            {
                // x = y;
                g.ExpressionStatement(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y"))),

                // fn(arg);
                g.ExpressionStatement(g.InvocationExpression(g.IdentifierName("fn"), g.IdentifierName("arg")))
            };

            Assert.Equal(0, g.GetStatements(g.MethodDeclaration("m")).Count);
            Assert.Equal(2, g.GetStatements(g.MethodDeclaration("m", statements: stmts)).Count);

            Assert.Equal(0, g.GetStatements(g.ConstructorDeclaration()).Count);
            Assert.Equal(2, g.GetStatements(g.ConstructorDeclaration(statements: stmts)).Count);

            Assert.Equal(0, g.GetStatements(g.VoidReturningLambdaExpression(new SyntaxNode[] { })).Count);
            Assert.Equal(2, g.GetStatements(g.VoidReturningLambdaExpression(stmts)).Count);

            Assert.Equal(0, g.GetStatements(g.ValueReturningLambdaExpression(new SyntaxNode[] { })).Count);
            Assert.Equal(2, g.GetStatements(g.ValueReturningLambdaExpression(stmts)).Count);

            Assert.Equal(0, g.GetStatements(g.IdentifierName("x")).Count);
        }

        [Fact]
        public void TestWithStatements()
        {
            var stmts = new[]
            {
                // x = y;
                g.ExpressionStatement(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y"))),

                // fn(arg);
                g.ExpressionStatement(g.InvocationExpression(g.IdentifierName("fn"), g.IdentifierName("arg")))
            };

            Assert.Equal(2, g.GetStatements(g.WithStatements(g.MethodDeclaration("m"), stmts)).Count);
            Assert.Equal(2, g.GetStatements(g.WithStatements(g.ConstructorDeclaration(), stmts)).Count);
            Assert.Equal(2, g.GetStatements(g.WithStatements(g.VoidReturningLambdaExpression(new SyntaxNode[] { }), stmts)).Count);
            Assert.Equal(2, g.GetStatements(g.WithStatements(g.ValueReturningLambdaExpression(new SyntaxNode[] { }), stmts)).Count);

            Assert.Equal(0, g.GetStatements(g.WithStatements(g.IdentifierName("x"), stmts)).Count);
        }

        [Fact]
        public void TestGetAccessorStatements()
        {
            var stmts = new[]
            {
                // x = y;
                g.ExpressionStatement(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y"))),

                // fn(arg);
                g.ExpressionStatement(g.InvocationExpression(g.IdentifierName("fn"), g.IdentifierName("arg")))
            };

            var p = g.ParameterDeclaration("p", g.IdentifierName("t"));

            // get-accessor
            Assert.Equal(0, g.GetGetAccessorStatements(g.PropertyDeclaration("p", g.IdentifierName("t"))).Count);
            Assert.Equal(2, g.GetGetAccessorStatements(g.PropertyDeclaration("p", g.IdentifierName("t"), getAccessorStatements: stmts)).Count);

            Assert.Equal(0, g.GetGetAccessorStatements(g.IndexerDeclaration(new[] { p }, g.IdentifierName("t"))).Count);
            Assert.Equal(2, g.GetGetAccessorStatements(g.IndexerDeclaration(new[] { p }, g.IdentifierName("t"), getAccessorStatements: stmts)).Count);

            Assert.Equal(0, g.GetGetAccessorStatements(g.IdentifierName("x")).Count);

            // set-accessor
            Assert.Equal(0, g.GetSetAccessorStatements(g.PropertyDeclaration("p", g.IdentifierName("t"))).Count);
            Assert.Equal(2, g.GetSetAccessorStatements(g.PropertyDeclaration("p", g.IdentifierName("t"), setAccessorStatements: stmts)).Count);

            Assert.Equal(0, g.GetSetAccessorStatements(g.IndexerDeclaration(new[] { p }, g.IdentifierName("t"))).Count);
            Assert.Equal(2, g.GetSetAccessorStatements(g.IndexerDeclaration(new[] { p }, g.IdentifierName("t"), setAccessorStatements: stmts)).Count);

            Assert.Equal(0, g.GetSetAccessorStatements(g.IdentifierName("x")).Count);
        }

        [Fact]
        public void TestWithAccessorStatements()
        {
            var stmts = new[]
            {
                // x = y;
                g.ExpressionStatement(g.AssignmentStatement(g.IdentifierName("x"), g.IdentifierName("y"))),

                // fn(arg);
                g.ExpressionStatement(g.InvocationExpression(g.IdentifierName("fn"), g.IdentifierName("arg")))
            };

            var p = g.ParameterDeclaration("p", g.IdentifierName("t"));

            // get-accessor
            Assert.Equal(2, g.GetGetAccessorStatements(g.WithGetAccessorStatements(g.PropertyDeclaration("p", g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(2, g.GetGetAccessorStatements(g.WithGetAccessorStatements(g.IndexerDeclaration(new[] { p }, g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(0, g.GetGetAccessorStatements(g.WithGetAccessorStatements(g.IdentifierName("x"), stmts)).Count);

            // set-accessor
            Assert.Equal(2, g.GetSetAccessorStatements(g.WithSetAccessorStatements(g.PropertyDeclaration("p", g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(2, g.GetSetAccessorStatements(g.WithSetAccessorStatements(g.IndexerDeclaration(new[] { p }, g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(0, g.GetSetAccessorStatements(g.WithSetAccessorStatements(g.IdentifierName("x"), stmts)).Count);
        }

        [Fact]
        public void TestMultiFieldDeclarations()
        {
            var comp = Compile(
@"public class C
{
   public static int X, Y, Z;
}");

            var symbolC = (INamedTypeSymbol)comp.GlobalNamespace.GetMembers("C").First();
            var symbolX = (IFieldSymbol)symbolC.GetMembers("X").First();
            var symbolY = (IFieldSymbol)symbolC.GetMembers("Y").First();
            var symbolZ = (IFieldSymbol)symbolC.GetMembers("Z").First();

            var declC = g.GetDeclaration(symbolC.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());
            var declX = g.GetDeclaration(symbolX.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());
            var declY = g.GetDeclaration(symbolY.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());
            var declZ = g.GetDeclaration(symbolZ.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());

            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(declX));
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(declY));
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(declZ));

            Assert.NotNull(g.GetType(declX));
            Assert.Equal("int", g.GetType(declX).ToString());
            Assert.Equal("X", g.GetName(declX));
            Assert.Equal(Accessibility.Public, g.GetAccessibility(declX));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(declX));

            Assert.NotNull(g.GetType(declY));
            Assert.Equal("int", g.GetType(declY).ToString());
            Assert.Equal("Y", g.GetName(declY));
            Assert.Equal(Accessibility.Public, g.GetAccessibility(declY));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(declY));

            Assert.NotNull(g.GetType(declZ));
            Assert.Equal("int", g.GetType(declZ).ToString());
            Assert.Equal("Z", g.GetName(declZ));
            Assert.Equal(Accessibility.Public, g.GetAccessibility(declZ));
            Assert.Equal(DeclarationModifiers.Static, g.GetModifiers(declZ));

            var xTypedT = g.WithType(declX, g.IdentifierName("T"));
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(xTypedT));
            Assert.Equal(SyntaxKind.FieldDeclaration, xTypedT.CSharpKind());
            Assert.Equal("T", g.GetType(xTypedT).ToString());

            var xNamedQ = g.WithName(declX, "Q");
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(xNamedQ));
            Assert.Equal(SyntaxKind.FieldDeclaration, xNamedQ.CSharpKind());
            Assert.Equal("Q", g.GetName(xNamedQ).ToString());

            var xInitialized = g.WithExpression(declX, g.IdentifierName("e"));
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(xInitialized));
            Assert.Equal(SyntaxKind.FieldDeclaration, xInitialized.CSharpKind());
            Assert.Equal("e", g.GetExpression(xInitialized).ToString());

            var xPrivate = g.WithAccessibility(declX, Accessibility.Private);
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(xPrivate));
            Assert.Equal(SyntaxKind.FieldDeclaration, xPrivate.CSharpKind());
            Assert.Equal(Accessibility.Private, g.GetAccessibility(xPrivate));

            var xReadOnly = g.WithModifiers(declX, DeclarationModifiers.ReadOnly);
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(xReadOnly));
            Assert.Equal(SyntaxKind.FieldDeclaration, xReadOnly.CSharpKind());
            Assert.Equal(DeclarationModifiers.ReadOnly, g.GetModifiers(xReadOnly));

            var xAttributed = g.AddAttributes(declX, g.Attribute("A"));
            Assert.Equal(DeclarationKind.Field, g.GetDeclarationKind(xAttributed));
            Assert.Equal(SyntaxKind.FieldDeclaration, xAttributed.CSharpKind());
            Assert.Equal(1, g.GetAttributes(xAttributed).Count);
            Assert.Equal("[A]", g.GetAttributes(xAttributed)[0].ToString());

            var membersC = g.GetMembers(declC);
            Assert.Equal(3, membersC.Count);
            Assert.Equal(declX, membersC[0]);
            Assert.Equal(declY, membersC[1]);
            Assert.Equal(declZ, membersC[2]);

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertMembers(declC, 0, g.FieldDeclaration("A", g.IdentifierName("T"))),
@"public class C
{
    T A;
    public static int X, Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertMembers(declC, 1, g.FieldDeclaration("A", g.IdentifierName("T"))),
@"public class C
{
    public static int X;
    T A;
    public static int Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertMembers(declC, 2, g.FieldDeclaration("A", g.IdentifierName("T"))),
@"public class C
{
    public static int X, Y;
    T A;
    public static int Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertMembers(declC, 3, g.FieldDeclaration("A", g.IdentifierName("T"))),
@"public class C
{
    public static int X, Y, Z;
    T A;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ClassDeclaration("C", members: new[] { declX, declY }),
@"class C
{
    public static int X;
    public static int Y;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declX, xTypedT),
@"public class C
{
    public static T X;
    public static int Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declY, g.WithType(declY, g.IdentifierName("T"))),
@"public class C
{
    public static int X;
    public static T Y;
    public static int Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declZ, g.WithType(declZ, g.IdentifierName("T"))),
@"public class C
{
    public static int X, Y;
    public static T Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declX, g.WithAccessibility(declX, Accessibility.Private)),
@"public class C
{
    private static int X;
    public static int Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declX, g.WithModifiers(declX, DeclarationModifiers.None)),
@"public class C
{
    public int X;
    public static int Y, Z;
}");
            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declX, g.WithName(declX, "Q")),
@"public class C
{
    public static int Q, Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, declX, g.WithExpression(declX, g.IdentifierName("e"))),
@"public class C
{
    public static int X = e, Y, Z;
}");
        }

        [Fact]
        public void TestMultiAttributeDeclarations()
        {
            var comp = Compile(
@"[X, Y, Z]
public class C
{
}");
            var symbolC = comp.GlobalNamespace.GetMembers("C").First();
            var declC = symbolC.DeclaringSyntaxReferences.First().GetSyntax();
            var attrs = g.GetAttributes(declC);

            var attrX = attrs[0];
            var attrY = attrs[1];
            var attrZ = attrs[2];

            Assert.Equal(3, attrs.Count);
            Assert.Equal("X", g.GetName(attrX));
            Assert.Equal("Y", g.GetName(attrY));
            Assert.Equal("Z", g.GetName(attrZ));

            var xNamedQ = g.WithName(attrX, "Q");
            Assert.Equal(DeclarationKind.Attribute, g.GetDeclarationKind(xNamedQ));
            Assert.Equal(SyntaxKind.AttributeList, xNamedQ.CSharpKind());
            Assert.Equal("[Q]", xNamedQ.ToString());

            var xWithArg = g.AddAttributeArguments(attrX, new[] { g.AttributeArgument(g.IdentifierName("e")) });
            Assert.Equal(DeclarationKind.Attribute, g.GetDeclarationKind(xWithArg));
            Assert.Equal(SyntaxKind.AttributeList, xWithArg.CSharpKind());
            Assert.Equal("[X(e)]", xWithArg.ToString());

            // Inserting new attributes
            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertAttributes(declC, 0, g.Attribute("A")),
@"[A]
[X, Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertAttributes(declC, 1, g.Attribute("A")),
@"[X]
[A]
[Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertAttributes(declC, 2, g.Attribute("A")),
@"[X, Y]
[A]
[Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.InsertAttributes(declC, 3, g.Attribute("A")),
@"[X, Y, Z]
[A]
public class C
{
}");

            // Removing attributes
            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrX }),
@"[Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrY }),
@"[X, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrZ }),
@"[X, Y]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrX, attrY }),
@"[Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrX, attrZ }),
@"[Y]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrY, attrZ }),
@"[X]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.RemoveDeclarations(declC, new[] { attrX, attrY, attrZ }),
@"public class C
{
}");

            // Replacing attributes
            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, attrX, g.Attribute("A")),
@"[A, Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, attrY, g.Attribute("A")),
@"[X, A, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, attrZ, g.Attribute("A")),
@"[X, Y, A]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                g.ReplaceDeclaration(declC, attrX, g.AddAttributeArguments(attrX, new[] { g.AttributeArgument(g.IdentifierName("e")) })),
@"[X(e), Y, Z]
public class C
{
}");
        }

        [Fact]
        public void TestMultiReturnAttributeDeclarations()
        {
            var comp = Compile(
@"public class C
{
    [return: X, Y, Z]
    public void M()
    {
    }
}");
            var symbolC = comp.GlobalNamespace.GetMembers("C").First();
            var declC = symbolC.DeclaringSyntaxReferences.First().GetSyntax();
            var declM = g.GetMembers(declC).First();

            Assert.Equal(0, g.GetAttributes(declM).Count);

            var attrs = g.GetReturnAttributes(declM);
            Assert.Equal(3, attrs.Count);
            var attrX = attrs[0];
            var attrY = attrs[1];
            var attrZ = attrs[2];

            Assert.Equal("X", g.GetName(attrX));
            Assert.Equal("Y", g.GetName(attrY));
            Assert.Equal("Z", g.GetName(attrZ));

            var xNamedQ = g.WithName(attrX, "Q");
            Assert.Equal(DeclarationKind.Attribute, g.GetDeclarationKind(xNamedQ));
            Assert.Equal(SyntaxKind.AttributeList, xNamedQ.CSharpKind());
            Assert.Equal("[Q]", xNamedQ.ToString());

            var xWithArg = g.AddAttributeArguments(attrX, new[] { g.AttributeArgument(g.IdentifierName("e")) });
            Assert.Equal(DeclarationKind.Attribute, g.GetDeclarationKind(xWithArg));
            Assert.Equal(SyntaxKind.AttributeList, xWithArg.CSharpKind());
            Assert.Equal("[X(e)]", xWithArg.ToString());

            // Inserting new attributes
            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 0, g.Attribute("A")),
@"[return: A]
[return: X, Y, Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 1, g.Attribute("A")),
@"[return: X]
[return: A]
[return: Y, Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 2, g.Attribute("A")),
@"[return: X, Y]
[return: A]
[return: Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 3, g.Attribute("A")),
@"[return: X, Y, Z]
[return: A]
public void M()
{
}");

            // replacing
            VerifySyntax<MethodDeclarationSyntax>(
                g.ReplaceDeclaration(declM, attrX, g.Attribute("Q")),
@"[return: Q, Y, Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.ReplaceDeclaration(declM, attrX, g.AddAttributeArguments(attrX, new[] { g.AttributeArgument(g.IdentifierName("e")) })),
@"[return: X(e), Y, Z]
public void M()
{
}");
        }

        [Fact]
        public void TestMixedAttributeDeclarations()
        {
            var comp = Compile(
@"public class C
{
    [X]
    [return: A]
    [Y, Z]
    [return: B, C, D]
    [P]
    public void M()
    {
    }
}");
            var symbolC = comp.GlobalNamespace.GetMembers("C").First();
            var declC = symbolC.DeclaringSyntaxReferences.First().GetSyntax();
            var declM = g.GetMembers(declC).First();

            var attrs = g.GetAttributes(declM);
            Assert.Equal(4, attrs.Count);

            var attrX = attrs[0];
            Assert.Equal("X", g.GetName(attrX));
            Assert.Equal(SyntaxKind.AttributeList, attrX.CSharpKind());

            var attrY = attrs[1];
            Assert.Equal("Y", g.GetName(attrY));
            Assert.Equal(SyntaxKind.Attribute, attrY.CSharpKind());

            var attrZ = attrs[2];
            Assert.Equal("Z", g.GetName(attrZ));
            Assert.Equal(SyntaxKind.Attribute, attrZ.CSharpKind());

            var attrP = attrs[3];
            Assert.Equal("P", g.GetName(attrP));
            Assert.Equal(SyntaxKind.AttributeList, attrP.CSharpKind());

            var rattrs = g.GetReturnAttributes(declM);
            Assert.Equal(4, rattrs.Count);

            var attrA = rattrs[0];
            Assert.Equal("A", g.GetName(attrA));
            Assert.Equal(SyntaxKind.AttributeList, attrA.CSharpKind());

            var attrB = rattrs[1];
            Assert.Equal("B", g.GetName(attrB));
            Assert.Equal(SyntaxKind.Attribute, attrB.CSharpKind());

            var attrC = rattrs[2];
            Assert.Equal("C", g.GetName(attrC));
            Assert.Equal(SyntaxKind.Attribute, attrC.CSharpKind());

            var attrD = rattrs[3];
            Assert.Equal("D", g.GetName(attrD));
            Assert.Equal(SyntaxKind.Attribute, attrD.CSharpKind());

            // inserting
            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertAttributes(declM, 0, g.Attribute("Q")),
@"[Q]
[X]
[return: A]
[Y, Z]
[return: B, C, D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertAttributes(declM, 1, g.Attribute("Q")),
@"[X]
[return: A]
[Q]
[Y, Z]
[return: B, C, D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertAttributes(declM, 2, g.Attribute("Q")),
@"[X]
[return: A]
[Y]
[Q]
[Z]
[return: B, C, D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertAttributes(declM, 3, g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: B, C, D]
[Q]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertAttributes(declM, 4, g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: B, C, D]
[P]
[Q]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 0, g.Attribute("Q")),
@"[X]
[return: Q]
[return: A]
[Y, Z]
[return: B, C, D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 1, g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: Q]
[return: B, C, D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 2, g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: B]
[return: Q]
[return: C, D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 3, g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: B, C]
[return: Q]
[return: D]
[P]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                g.InsertReturnAttributes(declM, 4, g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: B, C, D]
[P]
[return: Q]
public void M()
{
}");
        }
    }
}
