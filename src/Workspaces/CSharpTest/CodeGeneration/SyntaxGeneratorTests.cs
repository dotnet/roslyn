// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editting
{
    public class SyntaxGeneratorTests
    {
        private readonly Workspace _ws;
        private readonly SyntaxGenerator _g;

        private readonly CSharpCompilation _emptyCompilation = CSharpCompilation.Create("empty",
                references: new[] { TestReferences.NetFx.v4_0_30319.mscorlib, TestReferences.NetFx.v4_0_30319.System });

        private readonly INamedTypeSymbol _ienumerableInt;

        public SyntaxGeneratorTests()
        {
            _ws = new AdhocWorkspace();
            _g = SyntaxGenerator.GetGenerator(_ws, LanguageNames.CSharp);
            _ienumerableInt = _emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_emptyCompilation.GetSpecialType(SpecialType.System_Int32));
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

        private void VerifySyntaxRaw<TSyntax>(SyntaxNode node, string expectedText) where TSyntax : SyntaxNode
        {
            Assert.IsAssignableFrom(typeof(TSyntax), node);
            var normalized = node.ToFullString();
            Assert.Equal(expectedText, normalized);
        }

        #region Expressions and Statements
        [Fact]
        public void TestLiteralExpressions()
        {
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0), "0");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1), "1");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(-1), "-1");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(int.MinValue), "global::System.Int32.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(int.MaxValue), "global::System.Int32.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0L), "0L");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1L), "1L");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(-1L), "-1L");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(long.MinValue), "global::System.Int64.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(long.MaxValue), "global::System.Int64.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0UL), "0UL");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1UL), "1UL");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(ulong.MinValue), "0UL");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(ulong.MaxValue), "global::System.UInt64.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0.0f), "0F");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1.0f), "1F");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(-1.0f), "-1F");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(float.MinValue), "global::System.Single.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(float.MaxValue), "global::System.Single.MaxValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(float.Epsilon), "global::System.Single.Epsilon");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(float.NaN), "global::System.Single.NaN");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(float.NegativeInfinity), "global::System.Single.NegativeInfinity");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(float.PositiveInfinity), "global::System.Single.PositiveInfinity");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0.0), "0D");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1.0), "1D");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(-1.0), "-1D");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(double.MinValue), "global::System.Double.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(double.MaxValue), "global::System.Double.MaxValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(double.Epsilon), "global::System.Double.Epsilon");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(double.NaN), "global::System.Double.NaN");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(double.NegativeInfinity), "global::System.Double.NegativeInfinity");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(double.PositiveInfinity), "global::System.Double.PositiveInfinity");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0m), "0M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0.00m), "0.00M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1.00m), "1.00M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(-1.00m), "-1.00M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1.0000000000m), "1.0000000000M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0.000000m), "0.000000M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0.0000000m), "0.0000000M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1000000000m), "1000000000M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(123456789.123456789m), "123456789.123456789M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1E-28m), "0.0000000000000000000000000001M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(0E-28m), "0.0000000000000000000000000000M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(1E-29m), "0.0000000000000000000000000000M");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(-1E-29m), "0.0000000000000000000000000000M");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(decimal.MinValue), "global::System.Decimal.MinValue");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.LiteralExpression(decimal.MaxValue), "global::System.Decimal.MaxValue");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression('c'), "'c'");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression("str"), "\"str\"");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression("s\"t\"r"), "\"s\\\"t\\\"r\"");

            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(true), "true");
            VerifySyntax<LiteralExpressionSyntax>(_g.LiteralExpression(false), "false");
        }

        [Fact]
        public void TestAttributeData()
        {
            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { }",
@"[MyAttribute]")),
@"[global::MyAttribute]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public MyAttribute(object value) { } }",
@"[MyAttribute(null)]")),
@"[global::MyAttribute(null)]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public MyAttribute(int value) { } }",
@"[MyAttribute(123)]")),
@"[global::MyAttribute(123)]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public MyAttribute(double value) { } }",
@"[MyAttribute(12.3)]")),
@"[global::MyAttribute(12.3)]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public MyAttribute(string value) { } }",
@"[MyAttribute(""value"")]")),
@"[global::MyAttribute(""value"")]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public enum E { A, B, C }
public class MyAttribute : Attribute { public MyAttribute(E value) { } }",
@"[MyAttribute(E.A)]")),
@"[global::MyAttribute(global::E.A)]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public MyAttribute(Type value) { } }",
@"[MyAttribute(typeof(MyAttribute))]")),
@"[global::MyAttribute(typeof (global::MyAttribute))]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public MyAttribute(int[] values) { } }",
@"[MyAttribute(new [] {1, 2, 3})]")),
@"[global::MyAttribute(new[]{1, 2, 3})]");

            VerifySyntax<AttributeListSyntax>(_g.Attribute(GetAttributeData(
@"using System; 
public class MyAttribute : Attribute { public int Value {get; set;} }",
@"[MyAttribute(Value = 123)]")),
@"[global::MyAttribute(Value = 123)]");
        }

        private AttributeData GetAttributeData(string decl, string use)
        {
            var compilation = Compile(decl + "\r\n" + use + "\r\nclass C { }");
            var typeC = compilation.GlobalNamespace.GetMembers("C").First() as INamedTypeSymbol;
            return typeC.GetAttributes().First();
        }

        [Fact]
        public void TestNameExpressions()
        {
            VerifySyntax<IdentifierNameSyntax>(_g.IdentifierName("x"), "x");
            VerifySyntax<QualifiedNameSyntax>(_g.QualifiedName(_g.IdentifierName("x"), _g.IdentifierName("y")), "x.y");
            VerifySyntax<QualifiedNameSyntax>(_g.DottedName("x.y"), "x.y");

            VerifySyntax<GenericNameSyntax>(_g.GenericName("x", _g.IdentifierName("y")), "x<y>");
            VerifySyntax<GenericNameSyntax>(_g.GenericName("x", _g.IdentifierName("y"), _g.IdentifierName("z")), "x<y, z>");

            // convert identifier name into generic name
            VerifySyntax<GenericNameSyntax>(_g.WithTypeArguments(_g.IdentifierName("x"), _g.IdentifierName("y")), "x<y>");

            // convert qualified name into qualified generic name
            VerifySyntax<QualifiedNameSyntax>(_g.WithTypeArguments(_g.DottedName("x.y"), _g.IdentifierName("z")), "x.y<z>");

            // convert member access expression into generic member access expression
            VerifySyntax<MemberAccessExpressionSyntax>(_g.WithTypeArguments(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x.y<z>");

            // convert existing generic name into a different generic name
            var gname = _g.WithTypeArguments(_g.IdentifierName("x"), _g.IdentifierName("y"));
            VerifySyntax<GenericNameSyntax>(gname, "x<y>");
            VerifySyntax<GenericNameSyntax>(_g.WithTypeArguments(gname, _g.IdentifierName("z")), "x<z>");
        }

        [Fact]
        public void TestTypeExpressions()
        {
            // these are all type syntax too
            VerifySyntax<TypeSyntax>(_g.IdentifierName("x"), "x");
            VerifySyntax<TypeSyntax>(_g.QualifiedName(_g.IdentifierName("x"), _g.IdentifierName("y")), "x.y");
            VerifySyntax<TypeSyntax>(_g.DottedName("x.y"), "x.y");
            VerifySyntax<TypeSyntax>(_g.GenericName("x", _g.IdentifierName("y")), "x<y>");
            VerifySyntax<TypeSyntax>(_g.GenericName("x", _g.IdentifierName("y"), _g.IdentifierName("z")), "x<y, z>");

            VerifySyntax<TypeSyntax>(_g.ArrayTypeExpression(_g.IdentifierName("x")), "x[]");
            VerifySyntax<TypeSyntax>(_g.ArrayTypeExpression(_g.ArrayTypeExpression(_g.IdentifierName("x"))), "x[][]");
            VerifySyntax<TypeSyntax>(_g.NullableTypeExpression(_g.IdentifierName("x")), "x?");
            VerifySyntax<TypeSyntax>(_g.NullableTypeExpression(_g.NullableTypeExpression(_g.IdentifierName("x"))), "x?");
        }

        [Fact]
        public void TestSpecialTypeExpression()
        {
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Byte), "byte");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_SByte), "sbyte");

            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Int16), "short");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_UInt16), "ushort");

            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Int32), "int");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_UInt32), "uint");

            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Int64), "long");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_UInt64), "ulong");

            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Single), "float");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Double), "double");

            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Char), "char");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_String), "string");

            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Object), "object");
            VerifySyntax<TypeSyntax>(_g.TypeExpression(SpecialType.System_Decimal), "decimal");
        }

        [Fact]
        public void TestSymbolTypeExpressions()
        {
            var genericType = _emptyCompilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            VerifySyntax<QualifiedNameSyntax>(_g.TypeExpression(genericType), "global::System.Collections.Generic.IEnumerable<T>");

            var arrayType = _emptyCompilation.CreateArrayTypeSymbol(_emptyCompilation.GetSpecialType(SpecialType.System_Int32));
            VerifySyntax<ArrayTypeSyntax>(_g.TypeExpression(arrayType), "System.Int32[]");
        }

        [Fact]
        public void TestMathAndLogicExpressions()
        {
            VerifySyntax<PrefixUnaryExpressionSyntax>(_g.NegateExpression(_g.IdentifierName("x")), "-(x)");
            VerifySyntax<BinaryExpressionSyntax>(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) + (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.SubtractExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) - (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.MultiplyExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) * (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.DivideExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) / (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.ModuloExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) % (y)");

            VerifySyntax<PrefixUnaryExpressionSyntax>(_g.BitwiseNotExpression(_g.IdentifierName("x")), "~(x)");
            VerifySyntax<BinaryExpressionSyntax>(_g.BitwiseAndExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) & (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.BitwiseOrExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) | (y)");

            VerifySyntax<PrefixUnaryExpressionSyntax>(_g.LogicalNotExpression(_g.IdentifierName("x")), "!(x)");
            VerifySyntax<BinaryExpressionSyntax>(_g.LogicalAndExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) && (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.LogicalOrExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) || (y)");
        }

        [Fact]
        public void TestEqualityAndInequalityExpressions()
        {
            VerifySyntax<BinaryExpressionSyntax>(_g.ReferenceEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) == (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.ValueEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) == (y)");

            VerifySyntax<BinaryExpressionSyntax>(_g.ReferenceNotEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) != (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.ValueNotEqualsExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) != (y)");

            VerifySyntax<BinaryExpressionSyntax>(_g.LessThanExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) < (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.LessThanOrEqualExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) <= (y)");

            VerifySyntax<BinaryExpressionSyntax>(_g.GreaterThanExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) > (y)");
            VerifySyntax<BinaryExpressionSyntax>(_g.GreaterThanOrEqualExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) >= (y)");
        }

        [Fact]
        public void TestConditionalExpressions()
        {
            VerifySyntax<BinaryExpressionSyntax>(_g.CoalesceExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) ?? (y)");
            VerifySyntax<ConditionalExpressionSyntax>(_g.ConditionalExpression(_g.IdentifierName("x"), _g.IdentifierName("y"), _g.IdentifierName("z")), "(x) ? (y) : (z)");
        }

        [Fact]
        public void TestMemberAccessExpressions()
        {
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "x.y");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.IdentifierName("x"), "y"), "x.y");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x.y.z");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x(y).z");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "x[y].z");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")), "((x) + (y)).z");
            VerifySyntax<MemberAccessExpressionSyntax>(_g.MemberAccessExpression(_g.NegateExpression(_g.IdentifierName("x")), _g.IdentifierName("y")), "(-(x)).y");
        }

        [Fact]
        public void TestArrayCreationExpressions()
        {
            VerifySyntax<ArrayCreationExpressionSyntax>(
                _g.ArrayCreationExpression(_g.IdentifierName("x"), _g.LiteralExpression(10)),
                "new x[10]");

            VerifySyntax<ArrayCreationExpressionSyntax>(
                _g.ArrayCreationExpression(_g.IdentifierName("x"), new SyntaxNode[] { _g.IdentifierName("y"), _g.IdentifierName("z") }),
                "new x[]{y, z}");
        }

        [Fact]
        public void TestObjectCreationExpressions()
        {
            VerifySyntax<ObjectCreationExpressionSyntax>(
                _g.ObjectCreationExpression(_g.IdentifierName("x")),
                "new x()");

            VerifySyntax<ObjectCreationExpressionSyntax>(
                _g.ObjectCreationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")),
                "new x(y)");

            var intType = _emptyCompilation.GetSpecialType(SpecialType.System_Int32);
            var listType = _emptyCompilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var listOfIntType = listType.Construct(intType);

            VerifySyntax<ObjectCreationExpressionSyntax>(
                _g.ObjectCreationExpression(listOfIntType, _g.IdentifierName("y")),
                "new global::System.Collections.Generic.List<System.Int32>(y)");  // should this be 'int' or if not shouldn't it have global::?
        }

        [Fact]
        public void TestElementAccessExpressions()
        {
            VerifySyntax<ElementAccessExpressionSyntax>(
                _g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")),
                "x[y]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                _g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y"), _g.IdentifierName("z")),
                "x[y, z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                _g.ElementAccessExpression(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "x.y[z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                _g.ElementAccessExpression(_g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "x[y][z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                _g.ElementAccessExpression(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "x(y)[z]");

            VerifySyntax<ElementAccessExpressionSyntax>(
                _g.ElementAccessExpression(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), _g.IdentifierName("z")),
                "((x) + (y))[z]");
        }

        [Fact]
        public void TestCastAndConvertExpressions()
        {
            VerifySyntax<CastExpressionSyntax>(_g.CastExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x)(y)");
            VerifySyntax<CastExpressionSyntax>(_g.ConvertExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x)(y)");
        }

        [Fact]
        public void TestIsAndAsExpressions()
        {
            VerifySyntax<BinaryExpressionSyntax>(_g.IsTypeExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) is y");
            VerifySyntax<BinaryExpressionSyntax>(_g.TryCastExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "(x) as y");
            VerifySyntax<TypeOfExpressionSyntax>(_g.TypeOfExpression(_g.IdentifierName("x")), "typeof (x)");
        }

        [Fact]
        public void TestInvocationExpressions()
        {
            // without explicit arguments
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.IdentifierName("x")), "x()");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y")), "x(y)");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y"), _g.IdentifierName("z")), "x(y, z)");

            // using explicit arguments
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.IdentifierName("x"), _g.Argument(_g.IdentifierName("y"))), "x(y)");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.IdentifierName("x"), _g.Argument(RefKind.Ref, _g.IdentifierName("y"))), "x(ref y)");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.IdentifierName("x"), _g.Argument(RefKind.Out, _g.IdentifierName("y"))), "x(out y)");

            // auto parenthesizing
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.MemberAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "x.y()");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.ElementAccessExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "x[y]()");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.InvocationExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "x(y)()");
            VerifySyntax<InvocationExpressionSyntax>(_g.InvocationExpression(_g.AddExpression(_g.IdentifierName("x"), _g.IdentifierName("y"))), "((x) + (y))()");
        }

        [Fact]
        public void TestAssignmentStatement()
        {
            VerifySyntax<AssignmentExpressionSyntax>(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y")), "x = (y)");
        }

        [Fact]
        public void TestExpressionStatement()
        {
            VerifySyntax<ExpressionStatementSyntax>(_g.ExpressionStatement(_g.IdentifierName("x")), "x;");
            VerifySyntax<ExpressionStatementSyntax>(_g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("x"))), "x();");
        }

        [Fact]
        public void TestLocalDeclarationStatements()
        {
            VerifySyntax<LocalDeclarationStatementSyntax>(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y"), "x y;");
            VerifySyntax<LocalDeclarationStatementSyntax>(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y", _g.IdentifierName("z")), "x y = z;");

            VerifySyntax<LocalDeclarationStatementSyntax>(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y", isConst: true), "const x y;");
            VerifySyntax<LocalDeclarationStatementSyntax>(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "y", _g.IdentifierName("z"), isConst: true), "const x y = z;");

            VerifySyntax<LocalDeclarationStatementSyntax>(_g.LocalDeclarationStatement("y", _g.IdentifierName("z")), "var y = z;");
        }

        [Fact]
        public void TestAwaitExpressions()
        {
            VerifySyntax<AwaitExpressionSyntax>(_g.AwaitExpression(_g.IdentifierName("x")), "await x");
        }

        [Fact]
        public void TestReturnStatements()
        {
            VerifySyntax<ReturnStatementSyntax>(_g.ReturnStatement(), "return;");
            VerifySyntax<ReturnStatementSyntax>(_g.ReturnStatement(_g.IdentifierName("x")), "return x;");
        }

        [Fact]
        public void TestThrowStatements()
        {
            VerifySyntax<ThrowStatementSyntax>(_g.ThrowStatement(), "throw;");
            VerifySyntax<ThrowStatementSyntax>(_g.ThrowStatement(_g.IdentifierName("x")), "throw x;");
        }

        [Fact]
        public void TestIfStatements()
        {
            VerifySyntax<IfStatementSyntax>(
                _g.IfStatement(_g.IdentifierName("x"), new SyntaxNode[] { }),
                "if (x)\r\n{\r\n}");

            VerifySyntax<IfStatementSyntax>(
                _g.IfStatement(_g.IdentifierName("x"), new SyntaxNode[] { }, new SyntaxNode[] { }),
                "if (x)\r\n{\r\n}\r\nelse\r\n{\r\n}");

            VerifySyntax<IfStatementSyntax>(
                _g.IfStatement(_g.IdentifierName("x"),
                    new SyntaxNode[] { _g.IdentifierName("y") }),
                "if (x)\r\n{\r\n    y;\r\n}");

            VerifySyntax<IfStatementSyntax>(
                _g.IfStatement(_g.IdentifierName("x"),
                    new SyntaxNode[] { _g.IdentifierName("y") },
                    new SyntaxNode[] { _g.IdentifierName("z") }),
                "if (x)\r\n{\r\n    y;\r\n}\r\nelse\r\n{\r\n    z;\r\n}");

            VerifySyntax<IfStatementSyntax>(
                _g.IfStatement(_g.IdentifierName("x"),
                    new SyntaxNode[] { _g.IdentifierName("y") },
                    _g.IfStatement(_g.IdentifierName("p"), new SyntaxNode[] { _g.IdentifierName("q") })),
                "if (x)\r\n{\r\n    y;\r\n}\r\nelse if (p)\r\n{\r\n    q;\r\n}");

            VerifySyntax<IfStatementSyntax>(
                _g.IfStatement(_g.IdentifierName("x"),
                    new SyntaxNode[] { _g.IdentifierName("y") },
                    _g.IfStatement(_g.IdentifierName("p"), new SyntaxNode[] { _g.IdentifierName("q") }, _g.IdentifierName("z"))),
                "if (x)\r\n{\r\n    y;\r\n}\r\nelse if (p)\r\n{\r\n    q;\r\n}\r\nelse\r\n{\r\n    z;\r\n}");
        }

        [Fact]
        public void TestSwitchStatements()
        {
            VerifySyntax<SwitchStatementSyntax>(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        new[] { _g.IdentifierName("z") })),
                "switch (x)\r\n{\r\n    case y:\r\n        z;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(
                        new[] { _g.IdentifierName("y"), _g.IdentifierName("p"), _g.IdentifierName("q") },
                        new[] { _g.IdentifierName("z") })),
                "switch (x)\r\n{\r\n    case y:\r\n    case p:\r\n    case q:\r\n        z;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        new[] { _g.IdentifierName("z") }),
                    _g.SwitchSection(_g.IdentifierName("a"),
                        new[] { _g.IdentifierName("b") })),
                "switch (x)\r\n{\r\n    case y:\r\n        z;\r\n    case a:\r\n        b;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        new[] { _g.IdentifierName("z") }),
                    _g.DefaultSwitchSection(
                        new[] { _g.IdentifierName("b") })),
                "switch (x)\r\n{\r\n    case y:\r\n        z;\r\n    default:\r\n        b;\r\n}");

            VerifySyntax<SwitchStatementSyntax>(
                _g.SwitchStatement(_g.IdentifierName("x"),
                    _g.SwitchSection(_g.IdentifierName("y"),
                        new[] { _g.ExitSwitchStatement() })),
                "switch (x)\r\n{\r\n    case y:\r\n        break;\r\n}");
        }

        [Fact]
        public void TestUsingStatements()
        {
            VerifySyntax<UsingStatementSyntax>(
                _g.UsingStatement(_g.IdentifierName("x"), new[] { _g.IdentifierName("y") }),
                "using (x)\r\n{\r\n    y;\r\n}");

            VerifySyntax<UsingStatementSyntax>(
                _g.UsingStatement("x", _g.IdentifierName("y"), new[] { _g.IdentifierName("z") }),
                "using (var x = y)\r\n{\r\n    z;\r\n}");

            VerifySyntax<UsingStatementSyntax>(
                _g.UsingStatement(_g.IdentifierName("x"), "y", _g.IdentifierName("z"), new[] { _g.IdentifierName("q") }),
                "using (x y = z)\r\n{\r\n    q;\r\n}");
        }

        [Fact]
        public void TestTryCatchStatements()
        {
            VerifySyntax<TryStatementSyntax>(
                _g.TryCatchStatement(
                    new[] { _g.IdentifierName("x") },
                    _g.CatchClause(_g.IdentifierName("y"), "z",
                        new[] { _g.IdentifierName("a") })),
                "try\r\n{\r\n    x;\r\n}\r\ncatch (y z)\r\n{\r\n    a;\r\n}");

            VerifySyntax<TryStatementSyntax>(
                _g.TryCatchStatement(
                    new[] { _g.IdentifierName("s") },
                    _g.CatchClause(_g.IdentifierName("x"), "y",
                        new[] { _g.IdentifierName("z") }),
                    _g.CatchClause(_g.IdentifierName("a"), "b",
                        new[] { _g.IdentifierName("c") })),
                "try\r\n{\r\n    s;\r\n}\r\ncatch (x y)\r\n{\r\n    z;\r\n}\r\ncatch (a b)\r\n{\r\n    c;\r\n}");

            VerifySyntax<TryStatementSyntax>(
                _g.TryCatchStatement(
                    new[] { _g.IdentifierName("s") },
                    new[] { _g.CatchClause(_g.IdentifierName("x"), "y", new[] { _g.IdentifierName("z") }) },
                    new[] { _g.IdentifierName("a") }),
                "try\r\n{\r\n    s;\r\n}\r\ncatch (x y)\r\n{\r\n    z;\r\n}\r\nfinally\r\n{\r\n    a;\r\n}");

            VerifySyntax<TryStatementSyntax>(
                _g.TryFinallyStatement(
                    new[] { _g.IdentifierName("x") },
                    new[] { _g.IdentifierName("a") }),
                "try\r\n{\r\n    x;\r\n}\r\nfinally\r\n{\r\n    a;\r\n}");
        }

        [Fact]
        public void TestWhileStatements()
        {
            VerifySyntax<WhileStatementSyntax>(
                _g.WhileStatement(_g.IdentifierName("x"),
                    new[] { _g.IdentifierName("y") }),
                "while (x)\r\n{\r\n    y;\r\n}");

            VerifySyntax<WhileStatementSyntax>(
                _g.WhileStatement(_g.IdentifierName("x"), null),
                "while (x)\r\n{\r\n}");
        }

        [Fact]
        public void TestLambdaExpressions()
        {
            VerifySyntax<SimpleLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression("x", _g.IdentifierName("y")),
                "x => y");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression(new[] { _g.LambdaParameter("x"), _g.LambdaParameter("y") }, _g.IdentifierName("z")),
                "(x, y) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression(new SyntaxNode[] { }, _g.IdentifierName("y")),
                "() => y");

            VerifySyntax<SimpleLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression("x", _g.IdentifierName("y")),
                "x => y");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression(new[] { _g.LambdaParameter("x"), _g.LambdaParameter("y") }, _g.IdentifierName("z")),
                "(x, y) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression(new SyntaxNode[] { }, _g.IdentifierName("y")),
                "() => y");

            VerifySyntax<SimpleLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression("x", new[] { _g.ReturnStatement(_g.IdentifierName("y")) }),
                "x =>\r\n{\r\n    return y;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression(new[] { _g.LambdaParameter("x"), _g.LambdaParameter("y") }, new[] { _g.ReturnStatement(_g.IdentifierName("z")) }),
                "(x, y) =>\r\n{\r\n    return z;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression(new SyntaxNode[] { }, new[] { _g.ReturnStatement(_g.IdentifierName("y")) }),
                "() =>\r\n{\r\n    return y;\r\n}");

            VerifySyntax<SimpleLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression("x", new[] { _g.IdentifierName("y") }),
                "x =>\r\n{\r\n    y;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression(new[] { _g.LambdaParameter("x"), _g.LambdaParameter("y") }, new[] { _g.IdentifierName("z") }),
                "(x, y) =>\r\n{\r\n    z;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression(new SyntaxNode[] { }, new[] { _g.IdentifierName("y") }),
                "() =>\r\n{\r\n    y;\r\n}");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression(new[] { _g.LambdaParameter("x", _g.IdentifierName("y")) }, _g.IdentifierName("z")),
                "(y x) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.ValueReturningLambdaExpression(new[] { _g.LambdaParameter("x", _g.IdentifierName("y")), _g.LambdaParameter("a", _g.IdentifierName("b")) }, _g.IdentifierName("z")),
                "(y x, b a) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression(new[] { _g.LambdaParameter("x", _g.IdentifierName("y")) }, _g.IdentifierName("z")),
                "(y x) => z");

            VerifySyntax<ParenthesizedLambdaExpressionSyntax>(
                _g.VoidReturningLambdaExpression(new[] { _g.LambdaParameter("x", _g.IdentifierName("y")), _g.LambdaParameter("a", _g.IdentifierName("b")) }, _g.IdentifierName("z")),
                "(y x, b a) => z");
        }
        #endregion

        #region Declarations
        [Fact]
        public void TestFieldDeclarations()
        {
            VerifySyntax<FieldDeclarationSyntax>(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32)),
                "int fld;");

            VerifySyntax<FieldDeclarationSyntax>(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32), initializer: _g.LiteralExpression(0)),
                "int fld = 0;");

            VerifySyntax<FieldDeclarationSyntax>(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32), accessibility: Accessibility.Public),
                "public int fld;");

            VerifySyntax<FieldDeclarationSyntax>(
                _g.FieldDeclaration("fld", _g.TypeExpression(SpecialType.System_Int32), accessibility: Accessibility.NotApplicable, modifiers: DeclarationModifiers.Static | DeclarationModifiers.ReadOnly),
                "static readonly int fld;");
        }

        [Fact]
        public void TestMethodDeclarations()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m"),
                "void m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", typeParameters: new[] { "x", "y" }),
                "void m<x, y>()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", returnType: _g.IdentifierName("x")),
                "x m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", returnType: _g.IdentifierName("x"), statements: new[] { _g.IdentifierName("y") }),
                "x m()\r\n{\r\n    y;\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", parameters: new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, returnType: _g.IdentifierName("x")),
                "x m(y z)\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", parameters: new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y"), _g.IdentifierName("a")) }, returnType: _g.IdentifierName("x")),
                "x m(y z = a)\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", returnType: _g.IdentifierName("x"), accessibility: Accessibility.Public),
                "public x m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", returnType: _g.IdentifierName("x"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Abstract),
                "public abstract x m();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", modifiers: DeclarationModifiers.Partial),
                "partial void m();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.MethodDeclaration("m", modifiers: DeclarationModifiers.Partial, statements: new[] { _g.IdentifierName("y") }),
                "partial void m()\r\n{\r\n    y;\r\n}");
        }

        [Fact]
        public void TestConstructorDeclaration()
        {
            VerifySyntax<ConstructorDeclarationSyntax>(
                _g.ConstructorDeclaration(),
                "ctor()\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                _g.ConstructorDeclaration("c"),
                "c()\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                _g.ConstructorDeclaration("c", accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Static),
                "public static c()\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                _g.ConstructorDeclaration("c", new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) }),
                "c(t p)\r\n{\r\n}");

            VerifySyntax<ConstructorDeclarationSyntax>(
                _g.ConstructorDeclaration("c",
                    parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) },
                    baseConstructorArguments: new[] { _g.IdentifierName("p") }),
                "c(t p): base (p)\r\n{\r\n}");
        }

        [Fact]
        public void TestPropertyDeclarations()
        {
            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract | DeclarationModifiers.ReadOnly),
                "abstract x p\r\n{\r\n    get;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract | DeclarationModifiers.WriteOnly),
                "abstract x p\r\n{\r\n    set;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly),
                "x p\r\n{\r\n    get\r\n    {\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.WriteOnly),
                "x p\r\n{\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract),
                "abstract x p\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly, getAccessorStatements: new[] { _g.IdentifierName("y") }),
                "x p\r\n{\r\n    get\r\n    {\r\n        y;\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), modifiers: DeclarationModifiers.WriteOnly, setAccessorStatements: new[] { _g.IdentifierName("y") }),
                "x p\r\n{\r\n    set\r\n    {\r\n        y;\r\n    }\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.PropertyDeclaration("p", _g.IdentifierName("x"), setAccessorStatements: new[] { _g.IdentifierName("y") }),
                "x p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n        y;\r\n    }\r\n}");
        }

        [Fact]
        public void TestIndexerDeclarations()
        {
            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract | DeclarationModifiers.ReadOnly),
                "abstract x this[y z]\r\n{\r\n    get;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract | DeclarationModifiers.WriteOnly),
                "abstract x this[y z]\r\n{\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract),
                "abstract x this[y z]\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.WriteOnly),
                "x this[y z]\r\n{\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.ReadOnly,
                    getAccessorStatements: new[] { _g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.WriteOnly,
                    setAccessorStatements: new[] { _g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    set\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x")),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"),
                    setAccessorStatements: new[] { _g.IdentifierName("a") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n        a;\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"),
                    getAccessorStatements: new[] { _g.IdentifierName("a") }, setAccessorStatements: new[] { _g.IdentifierName("b") }),
                "x this[y z]\r\n{\r\n    get\r\n    {\r\n        a;\r\n    }\r\n\r\n    set\r\n    {\r\n        b;\r\n    }\r\n}");
        }

        [Fact]
        public void TestEventFieldDeclarations()
        {
            VerifySyntax<EventFieldDeclarationSyntax>(
                _g.EventDeclaration("ef", _g.IdentifierName("t")),
                "event t ef;");

            VerifySyntax<EventFieldDeclarationSyntax>(
                _g.EventDeclaration("ef", _g.IdentifierName("t"), accessibility: Accessibility.Public),
                "public event t ef;");

            VerifySyntax<EventFieldDeclarationSyntax>(
                _g.EventDeclaration("ef", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Static),
                "static event t ef;");
        }

        [Fact]
        public void TestEventPropertyDeclarations()
        {
            VerifySyntax<EventDeclarationSyntax>(
                _g.CustomEventDeclaration("ep", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                "abstract event t ep\r\n{\r\n    add;\r\n    remove;\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                _g.CustomEventDeclaration("ep", _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Abstract),
                "public abstract event t ep\r\n{\r\n    add;\r\n    remove;\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                _g.CustomEventDeclaration("ep", _g.IdentifierName("t")),
                "event t ep\r\n{\r\n    add\r\n    {\r\n    }\r\n\r\n    remove\r\n    {\r\n    }\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                _g.CustomEventDeclaration("ep", _g.IdentifierName("t"), addAccessorStatements: new[] { _g.IdentifierName("s") }, removeAccessorStatements: new[] { _g.IdentifierName("s2") }),
                "event t ep\r\n{\r\n    add\r\n    {\r\n        s;\r\n    }\r\n\r\n    remove\r\n    {\r\n        s2;\r\n    }\r\n}");
        }

        [Fact]
        public void TestAsPublicInterfaceImplementation()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                _g.AsPublicInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "public t m()\r\n{\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.AsPublicInterfaceImplementation(
                    _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "public t p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.AsPublicInterfaceImplementation(
                    _g.IndexerDeclaration(parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("a")) }, type: _g.IdentifierName("t"), accessibility: Accessibility.Internal, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "public t this[a p]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            // convert private to public
            var pim = _g.AsPrivateInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i"));

            VerifySyntax<MethodDeclarationSyntax>(
                _g.AsPublicInterfaceImplementation(pim, _g.IdentifierName("i2")),
                "public t m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.AsPublicInterfaceImplementation(pim, _g.IdentifierName("i2"), "m2"),
                "public t m2()\r\n{\r\n}");
        }

        [Fact]
        public void TestAsPrivateInterfaceImplementation()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                _g.AsPrivateInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "t i.m()\r\n{\r\n}");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.AsPrivateInterfaceImplementation(
                    _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility: Accessibility.Internal, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "t i.p\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.AsPrivateInterfaceImplementation(
                    _g.IndexerDeclaration(parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("a")) }, type: _g.IdentifierName("t"), accessibility: Accessibility.Protected, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "t i.this[a p]\r\n{\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                _g.AsPrivateInterfaceImplementation(
                    _g.CustomEventDeclaration("ep", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i")),
                "event t i.ep\r\n{\r\n    add\r\n    {\r\n    }\r\n\r\n    remove\r\n    {\r\n    }\r\n}");

            // convert public to private
            var pim = _g.AsPublicInterfaceImplementation(
                    _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), accessibility: Accessibility.Private, modifiers: DeclarationModifiers.Abstract),
                    _g.IdentifierName("i"));

            VerifySyntax<MethodDeclarationSyntax>(
                _g.AsPrivateInterfaceImplementation(pim, _g.IdentifierName("i2")),
                "t i2.m()\r\n{\r\n}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.AsPrivateInterfaceImplementation(pim, _g.IdentifierName("i2"), "m2"),
                "t i2.m2()\r\n{\r\n}");
        }

        [WorkItem(3928, "https://github.com/dotnet/roslyn/issues/3928")]
        [Fact]
        public void TestAsPrivateInterfaceImplementationRemovesConstraints()
        {
            var code = @"
public interface IFace
{
    void Method<T>() where T : class;
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var iface = cu.Members[0];
            var method = _g.GetMembers(iface)[0];

            var privateMethod = _g.AsPrivateInterfaceImplementation(method, _g.IdentifierName("IFace"));

            VerifySyntax<MethodDeclarationSyntax>(
                privateMethod,
                "void IFace.Method<T>()\r\n{\r\n}");
        }

        [Fact]
        public void TestClassDeclarations()
        {
            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c"),
                "class c\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", typeParameters: new[] { "x", "y" }),
                "class c<x, y>\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", baseType: _g.IdentifierName("x")),
                "class c : x\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", interfaceTypes: new[] { _g.IdentifierName("x") }),
                "class c : x\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", baseType: _g.IdentifierName("x"), interfaceTypes: new[] { _g.IdentifierName("y") }),
                "class c : x, y\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", interfaceTypes: new SyntaxNode[] { }),
                "class c\r\n{\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", members: new[] { _g.FieldDeclaration("y", type: _g.IdentifierName("x")) }),
                "class c\r\n{\r\n    x y;\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", members: new[] { _g.MethodDeclaration("m", returnType: _g.IdentifierName("t")) }),
                "class c\r\n{\r\n    t m()\r\n    {\r\n    }\r\n}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("c", members: new[] { _g.ConstructorDeclaration() }),
                "class c\r\n{\r\n    c()\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestStructDeclarations()
        {
            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s"),
                "struct s\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", typeParameters: new[] { "x", "y" }),
                "struct s<x, y>\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", interfaceTypes: new[] { _g.IdentifierName("x") }),
                "struct s : x\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", interfaceTypes: new[] { _g.IdentifierName("x"), _g.IdentifierName("y") }),
                "struct s : x, y\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", interfaceTypes: new SyntaxNode[] { }),
                "struct s\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", members: new[] { _g.FieldDeclaration("y", _g.IdentifierName("x")) }),
                "struct s\r\n{\r\n    x y;\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", members: new[] { _g.MethodDeclaration("m", returnType: _g.IdentifierName("t")) }),
                "struct s\r\n{\r\n    t m()\r\n    {\r\n    }\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.StructDeclaration("s", members: new[] { _g.ConstructorDeclaration("xxx") }),
                "struct s\r\n{\r\n    s()\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestInterfaceDeclarations()
        {
            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i"),
                "interface i\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", typeParameters: new[] { "x", "y" }),
                "interface i<x, y>\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", interfaceTypes: new[] { _g.IdentifierName("a") }),
                "interface i : a\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", interfaceTypes: new[] { _g.IdentifierName("a"), _g.IdentifierName("b") }),
                "interface i : a, b\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", interfaceTypes: new SyntaxNode[] { }),
                "interface i\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t m();\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t p\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.ReadOnly) }),
                "interface i\r\n{\r\n    t p\r\n    {\r\n        get;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("y", _g.IdentifierName("x")) }, _g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t this[x y]\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("y", _g.IdentifierName("x")) }, _g.IdentifierName("t"), Accessibility.Public, DeclarationModifiers.ReadOnly) }),
                "interface i\r\n{\r\n    t this[x y]\r\n    {\r\n        get;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.CustomEventDeclaration("ep", _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Static) }),
                "interface i\r\n{\r\n    event t ep\r\n    {\r\n        add;\r\n        remove;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.EventDeclaration("ef", _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Static) }),
                "interface i\r\n{\r\n    event t ef\r\n    {\r\n        add;\r\n        remove;\r\n    }\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.InterfaceDeclaration("i", members: new[] { _g.FieldDeclaration("f", _g.IdentifierName("t"), accessibility: Accessibility.Public, modifiers: DeclarationModifiers.Sealed) }),
                "interface i\r\n{\r\n    t f\r\n    {\r\n        get;\r\n        set;\r\n    }\r\n}");
        }

        [Fact]
        public void TestEnumDeclarations()
        {
            VerifySyntax<EnumDeclarationSyntax>(
                _g.EnumDeclaration("e"),
                "enum e\r\n{\r\n}");

            VerifySyntax<EnumDeclarationSyntax>(
                _g.EnumDeclaration("e", members: new[] { _g.EnumMember("a"), _g.EnumMember("b"), _g.EnumMember("c") }),
                "enum e\r\n{\r\n    a,\r\n    b,\r\n    c\r\n}");

            VerifySyntax<EnumDeclarationSyntax>(
                _g.EnumDeclaration("e", members: new[] { _g.IdentifierName("a"), _g.EnumMember("b"), _g.IdentifierName("c") }),
                "enum e\r\n{\r\n    a,\r\n    b,\r\n    c\r\n}");

            VerifySyntax<EnumDeclarationSyntax>(
                _g.EnumDeclaration("e", members: new[] { _g.EnumMember("a", _g.LiteralExpression(0)), _g.EnumMember("b"), _g.EnumMember("c", _g.LiteralExpression(5)) }),
                "enum e\r\n{\r\n    a = 0,\r\n    b,\r\n    c = 5\r\n}");
        }

        [Fact]
        public void TestDelegateDeclarations()
        {
            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d"),
                "delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d", returnType: _g.IdentifierName("t")),
                "delegate t d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d", returnType: _g.IdentifierName("t"), parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("pt")) }),
                "delegate t d(pt p);");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d", accessibility: Accessibility.Public),
                "public delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d", accessibility: Accessibility.Public),
                "public delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d", modifiers: DeclarationModifiers.New),
                "new delegate void d();");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.DelegateDeclaration("d", typeParameters: new[] { "T", "S" }),
                "delegate void d<T, S>();");
        }

        [Fact]
        public void TestNamespaceImportDeclarations()
        {
            VerifySyntax<UsingDirectiveSyntax>(
                _g.NamespaceImportDeclaration(_g.IdentifierName("n")),
                "using n;");

            VerifySyntax<UsingDirectiveSyntax>(
                _g.NamespaceImportDeclaration("n"),
                "using n;");

            VerifySyntax<UsingDirectiveSyntax>(
                _g.NamespaceImportDeclaration("n.m"),
                "using n.m;");
        }

        [Fact]
        public void TestNamespaceDeclarations()
        {
            VerifySyntax<NamespaceDeclarationSyntax>(
                _g.NamespaceDeclaration("n"),
                "namespace n\r\n{\r\n}");

            VerifySyntax<NamespaceDeclarationSyntax>(
                _g.NamespaceDeclaration("n.m"),
                "namespace n.m\r\n{\r\n}");

            VerifySyntax<NamespaceDeclarationSyntax>(
                _g.NamespaceDeclaration("n",
                    _g.NamespaceImportDeclaration("m")),
                "namespace n\r\n{\r\n    using m;\r\n}");

            VerifySyntax<NamespaceDeclarationSyntax>(
                _g.NamespaceDeclaration("n",
                    _g.ClassDeclaration("c"),
                    _g.NamespaceImportDeclaration("m")),
                "namespace n\r\n{\r\n    using m;\r\n\r\n    class c\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestCompilationUnits()
        {
            VerifySyntax<CompilationUnitSyntax>(
                _g.CompilationUnit(),
                "");

            VerifySyntax<CompilationUnitSyntax>(
                _g.CompilationUnit(
                    _g.NamespaceDeclaration("n")),
                "namespace n\r\n{\r\n}");

            VerifySyntax<CompilationUnitSyntax>(
                _g.CompilationUnit(
                    _g.NamespaceImportDeclaration("n")),
                "using n;");

            VerifySyntax<CompilationUnitSyntax>(
                _g.CompilationUnit(
                    _g.ClassDeclaration("c"),
                    _g.NamespaceImportDeclaration("m")),
                "using m;\r\n\r\nclass c\r\n{\r\n}");

            VerifySyntax<CompilationUnitSyntax>(
                _g.CompilationUnit(
                    _g.NamespaceImportDeclaration("n"),
                    _g.NamespaceDeclaration("n",
                        _g.NamespaceImportDeclaration("m"),
                        _g.ClassDeclaration("c"))),
                "using n;\r\n\r\nnamespace n\r\n{\r\n    using m;\r\n\r\n    class c\r\n    {\r\n    }\r\n}");
        }

        [Fact]
        public void TestAttributeDeclarations()
        {
            VerifySyntax<AttributeListSyntax>(
                _g.Attribute(_g.IdentifierName("a")),
                "[a]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a"),
                "[a]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a.b"),
                "[a.b]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a", new SyntaxNode[] { }),
                "[a()]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a", new[] { _g.IdentifierName("x") }),
                "[a(x)]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a", new[] { _g.AttributeArgument(_g.IdentifierName("x")) }),
                "[a(x)]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a", new[] { _g.AttributeArgument("x", _g.IdentifierName("y")) }),
                "[a(x = y)]");

            VerifySyntax<AttributeListSyntax>(
                _g.Attribute("a", new[] { _g.IdentifierName("x"), _g.IdentifierName("y") }),
                "[a(x, y)]");
        }

        [Fact]
        public void TestAddAttributes()
        {
            VerifySyntax<FieldDeclarationSyntax>(
                _g.AddAttributes(
                    _g.FieldDeclaration("y", _g.IdentifierName("x")),
                    _g.Attribute("a")),
                "[a]\r\nx y;");

            VerifySyntax<FieldDeclarationSyntax>(
                _g.AddAttributes(
                    _g.AddAttributes(
                        _g.FieldDeclaration("y", _g.IdentifierName("x")),
                        _g.Attribute("a")),
                    _g.Attribute("b")),
                "[a]\r\n[b]\r\nx y;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.AddAttributes(
                    _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
                "[a]\r\nabstract t m();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.AddReturnAttributes(
                    _g.MethodDeclaration("m", returnType: _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
                "[return: a]\r\nabstract t m();");

            VerifySyntax<PropertyDeclarationSyntax>(
                _g.AddAttributes(
                    _g.PropertyDeclaration("p", _g.IdentifierName("x"), accessibility: Accessibility.NotApplicable, modifiers: DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
                "[a]\r\nabstract x p\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<IndexerDeclarationSyntax>(
                _g.AddAttributes(
                    _g.IndexerDeclaration(new[] { _g.ParameterDeclaration("z", _g.IdentifierName("y")) }, _g.IdentifierName("x"), modifiers: DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
                "[a]\r\nabstract x this[y z]\r\n{\r\n    get;\r\n    set;\r\n}");

            VerifySyntax<EventDeclarationSyntax>(
                _g.AddAttributes(
                    _g.CustomEventDeclaration("ep", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract),
                    _g.Attribute("a")),
                "[a]\r\nabstract event t ep\r\n{\r\n    add;\r\n    remove;\r\n}");

            VerifySyntax<EventFieldDeclarationSyntax>(
                _g.AddAttributes(
                    _g.EventDeclaration("ef", _g.IdentifierName("t")),
                    _g.Attribute("a")),
                "[a]\r\nevent t ef;");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddAttributes(
                    _g.ClassDeclaration("c"),
                    _g.Attribute("a")),
                "[a]\r\nclass c\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.AddAttributes(
                    _g.StructDeclaration("s"),
                    _g.Attribute("a")),
                "[a]\r\nstruct s\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.AddAttributes(
                    _g.InterfaceDeclaration("i"),
                    _g.Attribute("a")),
                "[a]\r\ninterface i\r\n{\r\n}");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.AddAttributes(
                    _g.DelegateDeclaration("d"),
                    _g.Attribute("a")),
                "[a]\r\ndelegate void d();");

            VerifySyntax<ParameterSyntax>(
                _g.AddAttributes(
                    _g.ParameterDeclaration("p", _g.IdentifierName("t")),
                    _g.Attribute("a")),
                "[a] t p");

            VerifySyntax<CompilationUnitSyntax>(
                _g.AddAttributes(
                    _g.CompilationUnit(_g.NamespaceDeclaration("n")),
                    _g.Attribute("a")),
                "[assembly: a]\r\nnamespace n\r\n{\r\n}");
        }

        [Fact]
        [WorkItem(5066, "https://github.com/dotnet/roslyn/issues/5066")]
        public void TestAddAttributesToAccessors()
        {
            var prop = _g.PropertyDeclaration("P", _g.IdentifierName("T"));
            var evnt = _g.CustomEventDeclaration("E", _g.IdentifierName("T"));
            CheckAddRemoveAttribute(_g.GetAccessor(prop, DeclarationKind.GetAccessor));
            CheckAddRemoveAttribute(_g.GetAccessor(prop, DeclarationKind.SetAccessor));
            CheckAddRemoveAttribute(_g.GetAccessor(evnt, DeclarationKind.AddAccessor));
            CheckAddRemoveAttribute(_g.GetAccessor(evnt, DeclarationKind.RemoveAccessor));
        }

        private void CheckAddRemoveAttribute(SyntaxNode declaration)
        {
            var initialAttributes = _g.GetAttributes(declaration);
            Assert.Equal(0, initialAttributes.Count);

            var withAttribute = _g.AddAttributes(declaration, _g.Attribute("a"));
            var attrsAdded = _g.GetAttributes(withAttribute);
            Assert.Equal(1, attrsAdded.Count);

            var withoutAttribute = _g.RemoveNode(withAttribute, attrsAdded[0]);
            var attrsRemoved = _g.GetAttributes(withoutAttribute);
            Assert.Equal(0, attrsRemoved.Count);
        }

        [Fact]
        public void TestAddRemoveAttributesPerservesTrivia()
        {
            var cls = SyntaxFactory.ParseCompilationUnit(@"// comment
public class C { } // end").Members[0];

            var added = _g.AddAttributes(cls, _g.Attribute("a"));
            VerifySyntax<ClassDeclarationSyntax>(added, "// comment\r\n[a]\r\npublic class C\r\n{\r\n} // end\r\n");

            var removed = _g.RemoveAllAttributes(added);
            VerifySyntax<ClassDeclarationSyntax>(removed, "// comment\r\npublic class C\r\n{\r\n} // end\r\n");

            var attrWithComment = _g.GetAttributes(added).First();
            VerifySyntax<AttributeListSyntax>(attrWithComment, "// comment\r\n[a]");
        }

        [Fact]
        public void TestWithTypeParameters()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract),
                    "a"),
            "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract)),
            "abstract void m();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract),
                    "a", "b"),
            "abstract void m<a, b>();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeParameters(_g.WithTypeParameters(
                    _g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract),
                    "a", "b")),
            "abstract void m();");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.ClassDeclaration("c"),
                    "a", "b"),
            "class c<a, b>\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.StructDeclaration("s"),
                    "a", "b"),
            "struct s<a, b>\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.InterfaceDeclaration("i"),
                    "a", "b"),
            "interface i<a, b>\r\n{\r\n}");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.WithTypeParameters(
                    _g.DelegateDeclaration("d"),
                    "a", "b"),
            "delegate void d<a, b>();");
        }

        [Fact]
        public void TestWithTypeConstraint()
        {
            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", _g.IdentifierName("b")),
                "abstract void m<a>()where a : b;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", _g.IdentifierName("b"), _g.IdentifierName("c")),
                "abstract void m<a>()where a : b, c;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a"),
                "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(_g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", _g.IdentifierName("b"), _g.IdentifierName("c")), "a"),
                "abstract void m<a>();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeConstraint(
                        _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a", "x"),
                        "a", _g.IdentifierName("b"), _g.IdentifierName("c")),
                    "x", _g.IdentifierName("y")),
                "abstract void m<a, x>()where a : b, c where x : y;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.Constructor),
                "abstract void m<a>()where a : new ();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType),
                "abstract void m<a>()where a : class;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ValueType),
                "abstract void m<a>()where a : struct;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType | SpecialTypeConstraintKind.Constructor),
                "abstract void m<a>()where a : class, new ();");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType | SpecialTypeConstraintKind.ValueType),
                "abstract void m<a>()where a : class;");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Abstract), "a"),
                    "a", SpecialTypeConstraintKind.ReferenceType, _g.IdentifierName("b"), _g.IdentifierName("c")),
                "abstract void m<a>()where a : class, b, c;");

            // type declarations
            VerifySyntax<ClassDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.ClassDeclaration("c"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
            "class c<a, b>\r\n    where a : x\r\n{\r\n}");

            VerifySyntax<StructDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.StructDeclaration("s"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
            "struct s<a, b>\r\n    where a : x\r\n{\r\n}");

            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.InterfaceDeclaration("i"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
            "interface i<a, b>\r\n    where a : x\r\n{\r\n}");

            VerifySyntax<DelegateDeclarationSyntax>(
                _g.WithTypeConstraint(
                    _g.WithTypeParameters(
                        _g.DelegateDeclaration("d"),
                        "a", "b"),
                    "a", _g.IdentifierName("x")),
            "delegate void d<a, b>()where a : x;");
        }

        [Fact]
        public void TestInterfaceDeclarationWithEventFromSymbol()
        {
            VerifySyntax<InterfaceDeclarationSyntax>(
                _g.Declaration(_emptyCompilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged")),
@"public interface INotifyPropertyChanged
{
    event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged
    {
        add;
        remove;
    }
}");
        }
        #endregion

        #region Add/Insert/Remove/Get declarations & members/elements

        private void AssertNamesEqual(string[] expectedNames, IEnumerable<SyntaxNode> actualNodes)
        {
            var actualNames = actualNodes.Select(n => _g.GetName(n)).ToArray();
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
            AssertNamesEqual(expectedNames, _g.GetMembers(declaration));
        }

        private void AssertMemberNamesEqual(string expectedName, SyntaxNode declaration)
        {
            AssertNamesEqual(new[] { expectedName }, _g.GetMembers(declaration));
        }

        [Fact]
        public void TestAddNamespaceImports()
        {
            AssertNamesEqual("x.y", _g.GetNamespaceImports(_g.AddNamespaceImports(_g.CompilationUnit(), _g.NamespaceImportDeclaration("x.y"))));
            AssertNamesEqual(new[] { "x.y", "z" }, _g.GetNamespaceImports(_g.AddNamespaceImports(_g.CompilationUnit(), _g.NamespaceImportDeclaration("x.y"), _g.IdentifierName("z"))));
            AssertNamesEqual("", _g.GetNamespaceImports(_g.AddNamespaceImports(_g.CompilationUnit(), _g.MethodDeclaration("m"))));
            AssertNamesEqual(new[] { "x", "y.z" }, _g.GetNamespaceImports(_g.AddNamespaceImports(_g.CompilationUnit(_g.IdentifierName("x")), _g.DottedName("y.z"))));
        }

        [Fact]
        public void TestRemoveNamespaceImports()
        {
            TestRemoveAllNamespaceImports(_g.CompilationUnit(_g.NamespaceImportDeclaration("x")));
            TestRemoveAllNamespaceImports(_g.CompilationUnit(_g.NamespaceImportDeclaration("x"), _g.IdentifierName("y")));

            TestRemoveNamespaceImport(_g.CompilationUnit(_g.NamespaceImportDeclaration("x")), "x", new string[] { });
            TestRemoveNamespaceImport(_g.CompilationUnit(_g.NamespaceImportDeclaration("x"), _g.IdentifierName("y")), "x", new[] { "y" });
            TestRemoveNamespaceImport(_g.CompilationUnit(_g.NamespaceImportDeclaration("x"), _g.IdentifierName("y")), "y", new[] { "x" });
        }

        private void TestRemoveAllNamespaceImports(SyntaxNode declaration)
        {
            Assert.Equal(0, _g.GetNamespaceImports(_g.RemoveNodes(declaration, _g.GetNamespaceImports(declaration))).Count);
        }

        private void TestRemoveNamespaceImport(SyntaxNode declaration, string name, string[] remainingNames)
        {
            var newDecl = _g.RemoveNode(declaration, _g.GetNamespaceImports(declaration).First(m => _g.GetName(m) == name));
            AssertNamesEqual(remainingNames, _g.GetNamespaceImports(newDecl));
        }

        [Fact]
        public void TestRemoveNodeInTrivia()
        {
            var code = @"
///<summary> ... </summary>
public class C
{
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];
            var summary = cls.DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();

            var newCu = _g.RemoveNode(cu, summary);

            VerifySyntaxRaw<CompilationUnitSyntax>(
                newCu,
                @"

public class C
{
}");
        }

        [Fact]
        public void TestReplaceNodeInTrivia()
        {
            var code = @"
///<summary> ... </summary>
public class C
{
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];
            var summary = cls.DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();

            var summary2 = summary.WithContent(default(SyntaxList<XmlNodeSyntax>));

            var newCu = _g.ReplaceNode(cu, summary, summary2);

            VerifySyntaxRaw<CompilationUnitSyntax>(
                newCu, @"
///<summary></summary>
public class C
{
}");
        }

        [Fact]
        public void TestInsertAfterNodeInTrivia()
        {
            var code = @"
///<summary> ... </summary>
public class C
{
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];
            var text = cls.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();

            var newCu = _g.InsertNodesAfter(cu, text, new SyntaxNode[] { text });

            VerifySyntaxRaw<CompilationUnitSyntax>(
                newCu, @"
///<summary> ...  ... </summary>
public class C
{
}");
        }

        [Fact]
        public void TestInsertBeforeNodeInTrivia()
        {
            var code = @"
///<summary> ... </summary>
public class C
{
}";

            var cu = SyntaxFactory.ParseCompilationUnit(code);
            var cls = cu.Members[0];
            var text = cls.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();

            var newCu = _g.InsertNodesBefore(cu, text, new SyntaxNode[] { text });

            VerifySyntaxRaw<CompilationUnitSyntax>(
                newCu, @"
///<summary> ...  ... </summary>
public class C
{
}");
        }

        [Fact]
        public void TestAddMembers()
        {
            AssertMemberNamesEqual("m", _g.AddMembers(_g.ClassDeclaration("d"), new[] { _g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", _g.AddMembers(_g.StructDeclaration("s"), new[] { _g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", _g.AddMembers(_g.InterfaceDeclaration("i"), new[] { _g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("v", _g.AddMembers(_g.EnumDeclaration("e"), new[] { _g.EnumMember("v") }));
            AssertMemberNamesEqual("n2", _g.AddMembers(_g.NamespaceDeclaration("n"), new[] { _g.NamespaceDeclaration("n2") }));
            AssertMemberNamesEqual("n", _g.AddMembers(_g.CompilationUnit(), new[] { _g.NamespaceDeclaration("n") }));

            AssertMemberNamesEqual(new[] { "m", "m2" }, _g.AddMembers(_g.ClassDeclaration("d", members: new[] { _g.MethodDeclaration("m") }), new[] { _g.MethodDeclaration("m2") }));
            AssertMemberNamesEqual(new[] { "m", "m2" }, _g.AddMembers(_g.StructDeclaration("s", members: new[] { _g.MethodDeclaration("m") }), new[] { _g.MethodDeclaration("m2") }));
            AssertMemberNamesEqual(new[] { "m", "m2" }, _g.AddMembers(_g.InterfaceDeclaration("i", members: new[] { _g.MethodDeclaration("m") }), new[] { _g.MethodDeclaration("m2") }));
            AssertMemberNamesEqual(new[] { "v", "v2" }, _g.AddMembers(_g.EnumDeclaration("i", members: new[] { _g.EnumMember("v") }), new[] { _g.EnumMember("v2") }));
            AssertMemberNamesEqual(new[] { "n1", "n2" }, _g.AddMembers(_g.NamespaceDeclaration("n", new[] { _g.NamespaceDeclaration("n1") }), new[] { _g.NamespaceDeclaration("n2") }));
            AssertMemberNamesEqual(new[] { "n1", "n2" }, _g.AddMembers(_g.CompilationUnit(declarations: new[] { _g.NamespaceDeclaration("n1") }), new[] { _g.NamespaceDeclaration("n2") }));
        }

        [Fact]
        public void TestRemoveMembers()
        {
            // remove all members
            TestRemoveAllMembers(_g.ClassDeclaration("c", members: new[] { _g.MethodDeclaration("m") }));
            TestRemoveAllMembers(_g.StructDeclaration("s", members: new[] { _g.MethodDeclaration("m") }));
            TestRemoveAllMembers(_g.InterfaceDeclaration("i", members: new[] { _g.MethodDeclaration("m") }));
            TestRemoveAllMembers(_g.EnumDeclaration("i", members: new[] { _g.EnumMember("v") }));
            TestRemoveAllMembers(_g.NamespaceDeclaration("n", new[] { _g.NamespaceDeclaration("n") }));
            TestRemoveAllMembers(_g.CompilationUnit(declarations: new[] { _g.NamespaceDeclaration("n") }));

            TestRemoveMember(_g.ClassDeclaration("c", members: new[] { _g.MethodDeclaration("m1"), _g.MethodDeclaration("m2") }), "m1", new[] { "m2" });
            TestRemoveMember(_g.StructDeclaration("s", members: new[] { _g.MethodDeclaration("m1"), _g.MethodDeclaration("m2") }), "m1", new[] { "m2" });
        }

        private void TestRemoveAllMembers(SyntaxNode declaration)
        {
            Assert.Equal(0, _g.GetMembers(_g.RemoveNodes(declaration, _g.GetMembers(declaration))).Count);
        }

        private void TestRemoveMember(SyntaxNode declaration, string name, string[] remainingNames)
        {
            var newDecl = _g.RemoveNode(declaration, _g.GetMembers(declaration).First(m => _g.GetName(m) == name));
            AssertMemberNamesEqual(remainingNames, newDecl);
        }

        [Fact]
        public void TestGetMembers()
        {
            AssertMemberNamesEqual("m", _g.ClassDeclaration("c", members: new[] { _g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", _g.StructDeclaration("s", members: new[] { _g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("m", _g.InterfaceDeclaration("i", members: new[] { _g.MethodDeclaration("m") }));
            AssertMemberNamesEqual("v", _g.EnumDeclaration("e", members: new[] { _g.EnumMember("v") }));
            AssertMemberNamesEqual("c", _g.NamespaceDeclaration("n", declarations: new[] { _g.ClassDeclaration("c") }));
            AssertMemberNamesEqual("c", _g.CompilationUnit(declarations: new[] { _g.ClassDeclaration("c") }));
        }

        [Fact]
        public void TestGetDeclarationKind()
        {
            Assert.Equal(DeclarationKind.CompilationUnit, _g.GetDeclarationKind(_g.CompilationUnit()));
            Assert.Equal(DeclarationKind.Class, _g.GetDeclarationKind(_g.ClassDeclaration("c")));
            Assert.Equal(DeclarationKind.Struct, _g.GetDeclarationKind(_g.StructDeclaration("s")));
            Assert.Equal(DeclarationKind.Interface, _g.GetDeclarationKind(_g.InterfaceDeclaration("i")));
            Assert.Equal(DeclarationKind.Enum, _g.GetDeclarationKind(_g.EnumDeclaration("e")));
            Assert.Equal(DeclarationKind.Delegate, _g.GetDeclarationKind(_g.DelegateDeclaration("d")));
            Assert.Equal(DeclarationKind.Method, _g.GetDeclarationKind(_g.MethodDeclaration("m")));
            Assert.Equal(DeclarationKind.Constructor, _g.GetDeclarationKind(_g.ConstructorDeclaration()));
            Assert.Equal(DeclarationKind.Parameter, _g.GetDeclarationKind(_g.ParameterDeclaration("p")));
            Assert.Equal(DeclarationKind.Property, _g.GetDeclarationKind(_g.PropertyDeclaration("p", _g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.Indexer, _g.GetDeclarationKind(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(_g.FieldDeclaration("f", _g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.EnumMember, _g.GetDeclarationKind(_g.EnumMember("v")));
            Assert.Equal(DeclarationKind.Event, _g.GetDeclarationKind(_g.EventDeclaration("ef", _g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.CustomEvent, _g.GetDeclarationKind(_g.CustomEventDeclaration("e", _g.IdentifierName("t"))));
            Assert.Equal(DeclarationKind.Namespace, _g.GetDeclarationKind(_g.NamespaceDeclaration("n")));
            Assert.Equal(DeclarationKind.NamespaceImport, _g.GetDeclarationKind(_g.NamespaceImportDeclaration("u")));
            Assert.Equal(DeclarationKind.Variable, _g.GetDeclarationKind(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")));
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(_g.Attribute("a")));
        }

        [Fact]
        public void TestGetName()
        {
            Assert.Equal("c", _g.GetName(_g.ClassDeclaration("c")));
            Assert.Equal("s", _g.GetName(_g.StructDeclaration("s")));
            Assert.Equal("i", _g.GetName(_g.EnumDeclaration("i")));
            Assert.Equal("e", _g.GetName(_g.EnumDeclaration("e")));
            Assert.Equal("d", _g.GetName(_g.DelegateDeclaration("d")));
            Assert.Equal("m", _g.GetName(_g.MethodDeclaration("m")));
            Assert.Equal("", _g.GetName(_g.ConstructorDeclaration()));
            Assert.Equal("p", _g.GetName(_g.ParameterDeclaration("p")));
            Assert.Equal("p", _g.GetName(_g.PropertyDeclaration("p", _g.IdentifierName("t"))));
            Assert.Equal("", _g.GetName(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t"))));
            Assert.Equal("f", _g.GetName(_g.FieldDeclaration("f", _g.IdentifierName("t"))));
            Assert.Equal("v", _g.GetName(_g.EnumMember("v")));
            Assert.Equal("ef", _g.GetName(_g.EventDeclaration("ef", _g.IdentifierName("t"))));
            Assert.Equal("ep", _g.GetName(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"))));
            Assert.Equal("n", _g.GetName(_g.NamespaceDeclaration("n")));
            Assert.Equal("u", _g.GetName(_g.NamespaceImportDeclaration("u")));
            Assert.Equal("loc", _g.GetName(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")));
            Assert.Equal("a", _g.GetName(_g.Attribute("a")));
        }

        [Fact]
        public void TestWithName()
        {
            Assert.Equal("c", _g.GetName(_g.WithName(_g.ClassDeclaration("x"), "c")));
            Assert.Equal("s", _g.GetName(_g.WithName(_g.StructDeclaration("x"), "s")));
            Assert.Equal("i", _g.GetName(_g.WithName(_g.EnumDeclaration("x"), "i")));
            Assert.Equal("e", _g.GetName(_g.WithName(_g.EnumDeclaration("x"), "e")));
            Assert.Equal("d", _g.GetName(_g.WithName(_g.DelegateDeclaration("x"), "d")));
            Assert.Equal("m", _g.GetName(_g.WithName(_g.MethodDeclaration("x"), "m")));
            Assert.Equal("", _g.GetName(_g.WithName(_g.ConstructorDeclaration(), ".ctor")));
            Assert.Equal("p", _g.GetName(_g.WithName(_g.ParameterDeclaration("x"), "p")));
            Assert.Equal("p", _g.GetName(_g.WithName(_g.PropertyDeclaration("x", _g.IdentifierName("t")), "p")));
            Assert.Equal("", _g.GetName(_g.WithName(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t")), "this")));
            Assert.Equal("f", _g.GetName(_g.WithName(_g.FieldDeclaration("x", _g.IdentifierName("t")), "f")));
            Assert.Equal("v", _g.GetName(_g.WithName(_g.EnumMember("x"), "v")));
            Assert.Equal("ef", _g.GetName(_g.WithName(_g.EventDeclaration("x", _g.IdentifierName("t")), "ef")));
            Assert.Equal("ep", _g.GetName(_g.WithName(_g.CustomEventDeclaration("x", _g.IdentifierName("t")), "ep")));
            Assert.Equal("n", _g.GetName(_g.WithName(_g.NamespaceDeclaration("x"), "n")));
            Assert.Equal("u", _g.GetName(_g.WithName(_g.NamespaceImportDeclaration("x"), "u")));
            Assert.Equal("loc", _g.GetName(_g.WithName(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "x"), "loc")));
            Assert.Equal("a", _g.GetName(_g.WithName(_g.Attribute("x"), "a")));
        }

        [Fact]
        public void TestGetAccessibility()
        {
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.ClassDeclaration("c", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.StructDeclaration("s", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.EnumDeclaration("i", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.EnumDeclaration("e", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.DelegateDeclaration("d", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.MethodDeclaration("m", accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.ConstructorDeclaration(accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.ParameterDeclaration("p")));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.FieldDeclaration("f", _g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.EnumMember("v")));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.EventDeclaration("ef", _g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.Internal, _g.GetAccessibility(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"), accessibility: Accessibility.Internal)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.NamespaceDeclaration("n")));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.NamespaceImportDeclaration("u")));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.Attribute("a")));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(SyntaxFactory.TypeParameter("tp")));
        }

        [Fact]
        public void TestWithAccessibilty()
        {
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.ClassDeclaration("c", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.StructDeclaration("s", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.EnumDeclaration("i", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.EnumDeclaration("e", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.DelegateDeclaration("d", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.MethodDeclaration("m", accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.ConstructorDeclaration(accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.ParameterDeclaration("p"), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.PropertyDeclaration("p", _g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.FieldDeclaration("f", _g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.EnumMember("v"), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.EventDeclaration("ef", _g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"), accessibility: Accessibility.Internal), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.NamespaceDeclaration("n"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.NamespaceImportDeclaration("u"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(_g.Attribute("a"), Accessibility.Private)));
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(_g.WithAccessibility(SyntaxFactory.TypeParameter("tp"), Accessibility.Private)));
        }

        [Fact]
        public void TestGetModifiers()
        {
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.ClassDeclaration("c", modifiers: DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Partial, _g.GetModifiers(_g.StructDeclaration("s", modifiers: DeclarationModifiers.Partial)));
            Assert.Equal(DeclarationModifiers.New, _g.GetModifiers(_g.EnumDeclaration("e", modifiers: DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.New, _g.GetModifiers(_g.DelegateDeclaration("d", modifiers: DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.MethodDeclaration("m", modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.ConstructorDeclaration(modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.ParameterDeclaration("p")));
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.PropertyDeclaration("p", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t"), modifiers: DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Const, _g.GetModifiers(_g.FieldDeclaration("f", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Const)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.EventDeclaration("ef", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"), modifiers: DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.EnumMember("v")));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.NamespaceDeclaration("n")));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.NamespaceImportDeclaration("u")));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc")));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.Attribute("a")));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(SyntaxFactory.TypeParameter("tp")));
        }

        [Fact]
        public void TestWithModifiers()
        {
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.WithModifiers(_g.ClassDeclaration("c"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Partial, _g.GetModifiers(_g.WithModifiers(_g.StructDeclaration("s"), DeclarationModifiers.Partial)));
            Assert.Equal(DeclarationModifiers.New, _g.GetModifiers(_g.WithModifiers(_g.EnumDeclaration("e"), DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.New, _g.GetModifiers(_g.WithModifiers(_g.DelegateDeclaration("d"), DeclarationModifiers.New)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.MethodDeclaration("m"), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.ConstructorDeclaration(), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.ParameterDeclaration("p"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.WithModifiers(_g.PropertyDeclaration("p", _g.IdentifierName("t")), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Abstract, _g.GetModifiers(_g.WithModifiers(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("i") }, _g.IdentifierName("t")), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.Const, _g.GetModifiers(_g.WithModifiers(_g.FieldDeclaration("f", _g.IdentifierName("t")), DeclarationModifiers.Const)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.EventDeclaration("ef", _g.IdentifierName("t")), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(_g.WithModifiers(_g.CustomEventDeclaration("ep", _g.IdentifierName("t")), DeclarationModifiers.Static)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.EnumMember("v"), DeclarationModifiers.Partial)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.NamespaceDeclaration("n"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.NamespaceImportDeclaration("u"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(_g.Attribute("a"), DeclarationModifiers.Abstract)));
            Assert.Equal(DeclarationModifiers.None, _g.GetModifiers(_g.WithModifiers(SyntaxFactory.TypeParameter("tp"), DeclarationModifiers.Abstract)));
        }

        [Fact]
        public void TestGetType()
        {
            Assert.Equal("t", _g.GetType(_g.MethodDeclaration("m", returnType: _g.IdentifierName("t"))).ToString());
            Assert.Null(_g.GetType(_g.MethodDeclaration("m")));

            Assert.Equal("t", _g.GetType(_g.FieldDeclaration("f", _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("p", _g.IdentifierName("pt")) }, _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.ParameterDeclaration("p", _g.IdentifierName("t"))).ToString());

            Assert.Equal("t", _g.GetType(_g.EventDeclaration("ef", _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.CustomEventDeclaration("ep", _g.IdentifierName("t"))).ToString());

            Assert.Equal("t", _g.GetType(_g.DelegateDeclaration("t", returnType: _g.IdentifierName("t"))).ToString());
            Assert.Null(_g.GetType(_g.DelegateDeclaration("d")));

            Assert.Equal("t", _g.GetType(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "v")).ToString());

            Assert.Null(_g.GetType(_g.ClassDeclaration("c")));
            Assert.Null(_g.GetType(_g.IdentifierName("x")));
        }

        [Fact]
        public void TestWithType()
        {
            Assert.Equal("t", _g.GetType(_g.WithType(_g.MethodDeclaration("m", returnType: _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.WithType(_g.FieldDeclaration("f", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.WithType(_g.PropertyDeclaration("p", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.WithType(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("p", _g.IdentifierName("pt")) }, _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.WithType(_g.ParameterDeclaration("p", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());

            Assert.Equal("t", _g.GetType(_g.WithType(_g.DelegateDeclaration("t"), _g.IdentifierName("t"))).ToString());

            Assert.Equal("t", _g.GetType(_g.WithType(_g.EventDeclaration("ef", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());
            Assert.Equal("t", _g.GetType(_g.WithType(_g.CustomEventDeclaration("ep", _g.IdentifierName("x")), _g.IdentifierName("t"))).ToString());

            Assert.Equal("t", _g.GetType(_g.WithType(_g.LocalDeclarationStatement(_g.IdentifierName("x"), "v"), _g.IdentifierName("t"))).ToString());
            Assert.Null(_g.GetType(_g.WithType(_g.ClassDeclaration("c"), _g.IdentifierName("t"))));
            Assert.Null(_g.GetType(_g.WithType(_g.IdentifierName("x"), _g.IdentifierName("t"))));
        }

        [Fact]
        public void TestGetParameters()
        {
            Assert.Equal(0, _g.GetParameters(_g.MethodDeclaration("m")).Count);
            Assert.Equal(1, _g.GetParameters(_g.MethodDeclaration("m", parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);
            Assert.Equal(2, _g.GetParameters(_g.MethodDeclaration("m", parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.ParameterDeclaration("p2", _g.IdentifierName("t2")) })).Count);

            Assert.Equal(0, _g.GetParameters(_g.ConstructorDeclaration()).Count);
            Assert.Equal(1, _g.GetParameters(_g.ConstructorDeclaration(parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);
            Assert.Equal(2, _g.GetParameters(_g.ConstructorDeclaration(parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.ParameterDeclaration("p2", _g.IdentifierName("t2")) })).Count);

            Assert.Equal(1, _g.GetParameters(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) }, _g.IdentifierName("t"))).Count);
            Assert.Equal(2, _g.GetParameters(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.ParameterDeclaration("p2", _g.IdentifierName("t2")) }, _g.IdentifierName("t"))).Count);

            Assert.Equal(0, _g.GetParameters(_g.ValueReturningLambdaExpression(_g.IdentifierName("expr"))).Count);
            Assert.Equal(1, _g.GetParameters(_g.ValueReturningLambdaExpression("p1", _g.IdentifierName("expr"))).Count);

            Assert.Equal(0, _g.GetParameters(_g.VoidReturningLambdaExpression(_g.IdentifierName("expr"))).Count);
            Assert.Equal(1, _g.GetParameters(_g.VoidReturningLambdaExpression("p1", _g.IdentifierName("expr"))).Count);

            Assert.Equal(0, _g.GetParameters(_g.DelegateDeclaration("d")).Count);
            Assert.Equal(1, _g.GetParameters(_g.DelegateDeclaration("d", parameters: new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);

            Assert.Equal(0, _g.GetParameters(_g.ClassDeclaration("c")).Count);
            Assert.Equal(0, _g.GetParameters(_g.IdentifierName("x")).Count);
        }

        [Fact]
        public void TestAddParameters()
        {
            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.MethodDeclaration("m"), new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);
            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.ConstructorDeclaration(), new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);
            Assert.Equal(3, _g.GetParameters(_g.AddParameters(_g.IndexerDeclaration(new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) }, _g.IdentifierName("t")), new[] { _g.ParameterDeclaration("p2", _g.IdentifierName("t2")), _g.ParameterDeclaration("p3", _g.IdentifierName("t3")) })).Count);

            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.ValueReturningLambdaExpression(_g.IdentifierName("expr")), new[] { _g.LambdaParameter("p") })).Count);
            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.VoidReturningLambdaExpression(_g.IdentifierName("expr")), new[] { _g.LambdaParameter("p") })).Count);

            Assert.Equal(1, _g.GetParameters(_g.AddParameters(_g.DelegateDeclaration("d"), new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);

            Assert.Equal(0, _g.GetParameters(_g.AddParameters(_g.ClassDeclaration("c"), new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);
            Assert.Equal(0, _g.GetParameters(_g.AddParameters(_g.IdentifierName("x"), new[] { _g.ParameterDeclaration("p", _g.IdentifierName("t")) })).Count);
        }

        [Fact]
        public void TestGetExpression()
        {
            // initializers
            Assert.Equal("x", _g.GetExpression(_g.FieldDeclaration("f", _g.IdentifierName("t"), initializer: _g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.ParameterDeclaration("p", _g.IdentifierName("t"), initializer: _g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.LocalDeclarationStatement("loc", initializer: _g.IdentifierName("x"))).ToString());

            // lambda bodies
            Assert.Null(_g.GetExpression(_g.ValueReturningLambdaExpression("p", new[] { _g.IdentifierName("x") })));
            Assert.Equal(1, _g.GetStatements(_g.ValueReturningLambdaExpression("p", new[] { _g.IdentifierName("x") })).Count);
            Assert.Equal("x", _g.GetExpression(_g.ValueReturningLambdaExpression(_g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.VoidReturningLambdaExpression(_g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.ValueReturningLambdaExpression("p", _g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.VoidReturningLambdaExpression("p", _g.IdentifierName("x"))).ToString());

            Assert.Null(_g.GetExpression(_g.IdentifierName("e")));
        }

        [Fact]
        public void TestWithExpression()
        {
            // initializers
            Assert.Equal("x", _g.GetExpression(_g.WithExpression(_g.FieldDeclaration("f", _g.IdentifierName("t")), _g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.WithExpression(_g.ParameterDeclaration("p", _g.IdentifierName("t")), _g.IdentifierName("x"))).ToString());
            Assert.Equal("x", _g.GetExpression(_g.WithExpression(_g.LocalDeclarationStatement(_g.IdentifierName("t"), "loc"), _g.IdentifierName("x"))).ToString());

            // lambda bodies
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression("p", new[] { _g.IdentifierName("x") }), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression("p", new[] { _g.IdentifierName("x") }), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression(new[] { _g.IdentifierName("x") }), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression(new[] { _g.IdentifierName("x") }), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression("p", _g.IdentifierName("x")), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression("p", _g.IdentifierName("x")), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.ValueReturningLambdaExpression(_g.IdentifierName("x")), _g.IdentifierName("y"))).ToString());
            Assert.Equal("y", _g.GetExpression(_g.WithExpression(_g.VoidReturningLambdaExpression(_g.IdentifierName("x")), _g.IdentifierName("y"))).ToString());

            Assert.Null(_g.GetExpression(_g.WithExpression(_g.IdentifierName("e"), _g.IdentifierName("x"))));
        }

        [Fact]
        public void TestAccessorDeclarations()
        {
            var prop = _g.PropertyDeclaration("p", _g.IdentifierName("T"));

            Assert.Equal(2, _g.GetAccessors(prop).Count);

            // get accessors from property
            var getAccessor = _g.GetAccessor(prop, DeclarationKind.GetAccessor);
            Assert.NotNull(getAccessor);
            VerifySyntax<AccessorDeclarationSyntax>(getAccessor,
@"get
{
}");

            Assert.NotNull(getAccessor);
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(getAccessor));

            // get accessors from property
            var setAccessor = _g.GetAccessor(prop, DeclarationKind.SetAccessor);
            Assert.NotNull(setAccessor);
            Assert.Equal(Accessibility.NotApplicable, _g.GetAccessibility(setAccessor));

            // remove accessors
            Assert.Null(_g.GetAccessor(_g.RemoveNode(prop, getAccessor), DeclarationKind.GetAccessor));
            Assert.Null(_g.GetAccessor(_g.RemoveNode(prop, setAccessor), DeclarationKind.SetAccessor));

            // change accessor accessibility
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.WithAccessibility(getAccessor, Accessibility.Public)));
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(_g.WithAccessibility(setAccessor, Accessibility.Private)));

            // change accessor statements
            Assert.Equal(0, _g.GetStatements(getAccessor).Count);
            Assert.Equal(0, _g.GetStatements(setAccessor).Count);

            var newGetAccessor = _g.WithStatements(getAccessor, null);
            VerifySyntax<AccessorDeclarationSyntax>(newGetAccessor,
@"get;");

            var newNewGetAccessor = _g.WithStatements(newGetAccessor, new SyntaxNode[] { });
            VerifySyntax<AccessorDeclarationSyntax>(newNewGetAccessor,
@"get
{
}");

            // change accessors
            var newProp = _g.ReplaceNode(prop, getAccessor, _g.WithAccessibility(getAccessor, Accessibility.Public));
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.GetAccessor(newProp, DeclarationKind.GetAccessor)));

            newProp = _g.ReplaceNode(prop, setAccessor, _g.WithAccessibility(setAccessor, Accessibility.Public));
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(_g.GetAccessor(newProp, DeclarationKind.SetAccessor)));
        }

        [Fact]
        public void TestAccessorsOnSpecialProperties()
        {
            var root = SyntaxFactory.ParseCompilationUnit(
@"class C
{
   public int X { get; set; } = 100;
   public int Y => 300;
}");
            var x = _g.GetMembers(root.Members[0])[0];
            var y = _g.GetMembers(root.Members[0])[1];

            Assert.Equal(2, _g.GetAccessors(x).Count);
            Assert.Equal(0, _g.GetAccessors(y).Count);

            // adding accessors to expression value property will not succeed
            var y2 = _g.AddAccessors(y, new[] { _g.GetAccessor(x, DeclarationKind.GetAccessor) });
            Assert.NotNull(y2);
            Assert.Equal(0, _g.GetAccessors(y2).Count);
        }

        [Fact]
        public void TestAccessorsOnSpecialIndexers()
        {
            var root = SyntaxFactory.ParseCompilationUnit(
@"class C
{
   public int this[int p] { get { return p * 10; } set { } };
   public int this[int p] => p * 10;
}");
            var x = _g.GetMembers(root.Members[0])[0];
            var y = _g.GetMembers(root.Members[0])[1];

            Assert.Equal(2, _g.GetAccessors(x).Count);
            Assert.Equal(0, _g.GetAccessors(y).Count);

            // adding accessors to expression value indexer will not succeed
            var y2 = _g.AddAccessors(y, new[] { _g.GetAccessor(x, DeclarationKind.GetAccessor) });
            Assert.NotNull(y2);
            Assert.Equal(0, _g.GetAccessors(y2).Count);
        }

        [Fact]
        public void TestExpressionsOnSpecialProperties()
        {
            // you can get/set expression from both expression value property and initialized properties
            var root = SyntaxFactory.ParseCompilationUnit(
@"class C
{
   public int X { get; set; } = 100;
   public int Y => 300;
   public int Z { get; set; }
}");
            var x = _g.GetMembers(root.Members[0])[0];
            var y = _g.GetMembers(root.Members[0])[1];
            var z = _g.GetMembers(root.Members[0])[2];

            Assert.NotNull(_g.GetExpression(x));
            Assert.NotNull(_g.GetExpression(y));
            Assert.Null(_g.GetExpression(z));
            Assert.Equal("100", _g.GetExpression(x).ToString());
            Assert.Equal("300", _g.GetExpression(y).ToString());

            Assert.Equal("500", _g.GetExpression(_g.WithExpression(x, _g.LiteralExpression(500))).ToString());
            Assert.Equal("500", _g.GetExpression(_g.WithExpression(y, _g.LiteralExpression(500))).ToString());
            Assert.Equal("500", _g.GetExpression(_g.WithExpression(z, _g.LiteralExpression(500))).ToString());
        }

        [Fact]
        public void TestExpressionsOnSpecialIndexers()
        {
            // you can get/set expression from both expression value property and initialized properties
            var root = SyntaxFactory.ParseCompilationUnit(
@"class C
{
   public int this[int p] { get { return p * 10; } set { } };
   public int this[int p] => p * 10;
}");
            var x = _g.GetMembers(root.Members[0])[0];
            var y = _g.GetMembers(root.Members[0])[1];

            Assert.Null(_g.GetExpression(x));
            Assert.NotNull(_g.GetExpression(y));
            Assert.Equal("p * 10", _g.GetExpression(y).ToString());

            Assert.Null(_g.GetExpression(_g.WithExpression(x, _g.LiteralExpression(500))));
            Assert.Equal("500", _g.GetExpression(_g.WithExpression(y, _g.LiteralExpression(500))).ToString());
        }

        [Fact]
        public void TestGetStatements()
        {
            var stmts = new[]
            {
                // x = y;
                _g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))),

                // fn(arg);
                _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))
            };

            Assert.Equal(0, _g.GetStatements(_g.MethodDeclaration("m")).Count);
            Assert.Equal(2, _g.GetStatements(_g.MethodDeclaration("m", statements: stmts)).Count);

            Assert.Equal(0, _g.GetStatements(_g.ConstructorDeclaration()).Count);
            Assert.Equal(2, _g.GetStatements(_g.ConstructorDeclaration(statements: stmts)).Count);

            Assert.Equal(0, _g.GetStatements(_g.VoidReturningLambdaExpression(new SyntaxNode[] { })).Count);
            Assert.Equal(2, _g.GetStatements(_g.VoidReturningLambdaExpression(stmts)).Count);

            Assert.Equal(0, _g.GetStatements(_g.ValueReturningLambdaExpression(new SyntaxNode[] { })).Count);
            Assert.Equal(2, _g.GetStatements(_g.ValueReturningLambdaExpression(stmts)).Count);

            Assert.Equal(0, _g.GetStatements(_g.IdentifierName("x")).Count);
        }

        [Fact]
        public void TestWithStatements()
        {
            var stmts = new[]
            {
                // x = y;
                _g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))),

                // fn(arg);
                _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))
            };

            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.MethodDeclaration("m"), stmts)).Count);
            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.ConstructorDeclaration(), stmts)).Count);
            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.VoidReturningLambdaExpression(new SyntaxNode[] { }), stmts)).Count);
            Assert.Equal(2, _g.GetStatements(_g.WithStatements(_g.ValueReturningLambdaExpression(new SyntaxNode[] { }), stmts)).Count);

            Assert.Equal(0, _g.GetStatements(_g.WithStatements(_g.IdentifierName("x"), stmts)).Count);
        }

        [Fact]
        public void TestGetAccessorStatements()
        {
            var stmts = new[]
            {
                // x = y;
                _g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))),

                // fn(arg);
                _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))
            };

            var p = _g.ParameterDeclaration("p", _g.IdentifierName("t"));

            // get-accessor
            Assert.Equal(0, _g.GetGetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).Count);
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"), getAccessorStatements: stmts)).Count);

            Assert.Equal(0, _g.GetGetAccessorStatements(_g.IndexerDeclaration(new[] { p }, _g.IdentifierName("t"))).Count);
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.IndexerDeclaration(new[] { p }, _g.IdentifierName("t"), getAccessorStatements: stmts)).Count);

            Assert.Equal(0, _g.GetGetAccessorStatements(_g.IdentifierName("x")).Count);

            // set-accessor
            Assert.Equal(0, _g.GetSetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"))).Count);
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t"), setAccessorStatements: stmts)).Count);

            Assert.Equal(0, _g.GetSetAccessorStatements(_g.IndexerDeclaration(new[] { p }, _g.IdentifierName("t"))).Count);
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.IndexerDeclaration(new[] { p }, _g.IdentifierName("t"), setAccessorStatements: stmts)).Count);

            Assert.Equal(0, _g.GetSetAccessorStatements(_g.IdentifierName("x")).Count);
        }

        [Fact]
        public void TestWithAccessorStatements()
        {
            var stmts = new[]
            {
                // x = y;
                _g.ExpressionStatement(_g.AssignmentStatement(_g.IdentifierName("x"), _g.IdentifierName("y"))),

                // fn(arg);
                _g.ExpressionStatement(_g.InvocationExpression(_g.IdentifierName("fn"), _g.IdentifierName("arg")))
            };

            var p = _g.ParameterDeclaration("p", _g.IdentifierName("t"));

            // get-accessor
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.WithGetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(2, _g.GetGetAccessorStatements(_g.WithGetAccessorStatements(_g.IndexerDeclaration(new[] { p }, _g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(0, _g.GetGetAccessorStatements(_g.WithGetAccessorStatements(_g.IdentifierName("x"), stmts)).Count);

            // set-accessor
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.WithSetAccessorStatements(_g.PropertyDeclaration("p", _g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(2, _g.GetSetAccessorStatements(_g.WithSetAccessorStatements(_g.IndexerDeclaration(new[] { p }, _g.IdentifierName("t")), stmts)).Count);
            Assert.Equal(0, _g.GetSetAccessorStatements(_g.WithSetAccessorStatements(_g.IdentifierName("x"), stmts)).Count);
        }

        [Fact]
        public void TestGetBaseAndInterfaceTypes()
        {
            var classBI = SyntaxFactory.ParseCompilationUnit(
@"class C : B, I
{
}").Members[0];

            var baseListBI = _g.GetBaseAndInterfaceTypes(classBI);
            Assert.NotNull(baseListBI);
            Assert.Equal(2, baseListBI.Count);
            Assert.Equal("B", baseListBI[0].ToString());
            Assert.Equal("I", baseListBI[1].ToString());

            var classB = SyntaxFactory.ParseCompilationUnit(
@"class C : B
{
}").Members[0];

            var baseListB = _g.GetBaseAndInterfaceTypes(classB);
            Assert.NotNull(baseListB);
            Assert.Equal(1, baseListB.Count);
            Assert.Equal("B", baseListB[0].ToString());

            var classN = SyntaxFactory.ParseCompilationUnit(
@"class C
{
}").Members[0];

            var baseListN = _g.GetBaseAndInterfaceTypes(classN);
            Assert.NotNull(baseListN);
            Assert.Equal(0, baseListN.Count);
        }

        [Fact]
        public void TestRemoveBaseAndInterfaceTypes()
        {
            var classBI = SyntaxFactory.ParseCompilationUnit(
@"class C : B, I
{
}").Members[0];

            var baseListBI = _g.GetBaseAndInterfaceTypes(classBI);
            Assert.NotNull(baseListBI);

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNode(classBI, baseListBI[0]),
@"class C : I
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNode(classBI, baseListBI[1]),
@"class C : B
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(classBI, baseListBI),
@"class C
{
}");
        }

        [Fact]
        public void TestAddBaseType()
        {
            var classC = SyntaxFactory.ParseCompilationUnit(
@"class C
{
}").Members[0];

            var classCI = SyntaxFactory.ParseCompilationUnit(
@"class C : I
{
}").Members[0];

            var classCB = SyntaxFactory.ParseCompilationUnit(
@"class C : B
{
}").Members[0];

            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddBaseType(classC, _g.IdentifierName("T")),
@"class C : T
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddBaseType(classCI, _g.IdentifierName("T")),
@"class C : T, I
{
}");

            // TODO: find way to avoid this
            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddBaseType(classCB, _g.IdentifierName("T")),
@"class C : T, B
{
}");
        }

        [Fact]
        public void TestAddInterfaceTypes()
        {
            var classC = SyntaxFactory.ParseCompilationUnit(
@"class C
{
}").Members[0];

            var classCI = SyntaxFactory.ParseCompilationUnit(
@"class C : I
{
}").Members[0];

            var classCB = SyntaxFactory.ParseCompilationUnit(
@"class C : B
{
}").Members[0];

            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddInterfaceType(classC, _g.IdentifierName("T")),
@"class C : T
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddInterfaceType(classCI, _g.IdentifierName("T")),
@"class C : I, T
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.AddInterfaceType(classCB, _g.IdentifierName("T")),
@"class C : B, T
{
}");
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

            var declC = _g.GetDeclaration(symbolC.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());
            var declX = _g.GetDeclaration(symbolX.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());
            var declY = _g.GetDeclaration(symbolY.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());
            var declZ = _g.GetDeclaration(symbolZ.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).First());

            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(declX));
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(declY));
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(declZ));

            Assert.NotNull(_g.GetType(declX));
            Assert.Equal("int", _g.GetType(declX).ToString());
            Assert.Equal("X", _g.GetName(declX));
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(declX));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(declX));

            Assert.NotNull(_g.GetType(declY));
            Assert.Equal("int", _g.GetType(declY).ToString());
            Assert.Equal("Y", _g.GetName(declY));
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(declY));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(declY));

            Assert.NotNull(_g.GetType(declZ));
            Assert.Equal("int", _g.GetType(declZ).ToString());
            Assert.Equal("Z", _g.GetName(declZ));
            Assert.Equal(Accessibility.Public, _g.GetAccessibility(declZ));
            Assert.Equal(DeclarationModifiers.Static, _g.GetModifiers(declZ));

            var xTypedT = _g.WithType(declX, _g.IdentifierName("T"));
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xTypedT));
            Assert.Equal(SyntaxKind.FieldDeclaration, xTypedT.Kind());
            Assert.Equal("T", _g.GetType(xTypedT).ToString());

            var xNamedQ = _g.WithName(declX, "Q");
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xNamedQ));
            Assert.Equal(SyntaxKind.FieldDeclaration, xNamedQ.Kind());
            Assert.Equal("Q", _g.GetName(xNamedQ).ToString());

            var xInitialized = _g.WithExpression(declX, _g.IdentifierName("e"));
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xInitialized));
            Assert.Equal(SyntaxKind.FieldDeclaration, xInitialized.Kind());
            Assert.Equal("e", _g.GetExpression(xInitialized).ToString());

            var xPrivate = _g.WithAccessibility(declX, Accessibility.Private);
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xPrivate));
            Assert.Equal(SyntaxKind.FieldDeclaration, xPrivate.Kind());
            Assert.Equal(Accessibility.Private, _g.GetAccessibility(xPrivate));

            var xReadOnly = _g.WithModifiers(declX, DeclarationModifiers.ReadOnly);
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xReadOnly));
            Assert.Equal(SyntaxKind.FieldDeclaration, xReadOnly.Kind());
            Assert.Equal(DeclarationModifiers.ReadOnly, _g.GetModifiers(xReadOnly));

            var xAttributed = _g.AddAttributes(declX, _g.Attribute("A"));
            Assert.Equal(DeclarationKind.Field, _g.GetDeclarationKind(xAttributed));
            Assert.Equal(SyntaxKind.FieldDeclaration, xAttributed.Kind());
            Assert.Equal(1, _g.GetAttributes(xAttributed).Count);
            Assert.Equal("[A]", _g.GetAttributes(xAttributed)[0].ToString());

            var membersC = _g.GetMembers(declC);
            Assert.Equal(3, membersC.Count);
            Assert.Equal(declX, membersC[0]);
            Assert.Equal(declY, membersC[1]);
            Assert.Equal(declZ, membersC[2]);

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertMembers(declC, 0, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
@"public class C
{
    T A;
    public static int X, Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertMembers(declC, 1, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
@"public class C
{
    public static int X;
    T A;
    public static int Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertMembers(declC, 2, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
@"public class C
{
    public static int X, Y;
    T A;
    public static int Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertMembers(declC, 3, _g.FieldDeclaration("A", _g.IdentifierName("T"))),
@"public class C
{
    public static int X, Y, Z;
    T A;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ClassDeclaration("C", members: new[] { declX, declY }),
@"class C
{
    public static int X;
    public static int Y;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declX, xTypedT),
@"public class C
{
    public static T X;
    public static int Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declY, _g.WithType(declY, _g.IdentifierName("T"))),
@"public class C
{
    public static int X;
    public static T Y;
    public static int Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declZ, _g.WithType(declZ, _g.IdentifierName("T"))),
@"public class C
{
    public static int X, Y;
    public static T Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declX, _g.WithAccessibility(declX, Accessibility.Private)),
@"public class C
{
    private static int X;
    public static int Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declX, _g.WithModifiers(declX, DeclarationModifiers.None)),
@"public class C
{
    public int X;
    public static int Y, Z;
}");
            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declX, _g.WithName(declX, "Q")),
@"public class C
{
    public static int Q, Y, Z;
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, declX, _g.WithExpression(declX, _g.IdentifierName("e"))),
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
            var attrs = _g.GetAttributes(declC);

            var attrX = attrs[0];
            var attrY = attrs[1];
            var attrZ = attrs[2];

            Assert.Equal(3, attrs.Count);
            Assert.Equal("X", _g.GetName(attrX));
            Assert.Equal("Y", _g.GetName(attrY));
            Assert.Equal("Z", _g.GetName(attrZ));

            var xNamedQ = _g.WithName(attrX, "Q");
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(xNamedQ));
            Assert.Equal(SyntaxKind.AttributeList, xNamedQ.Kind());
            Assert.Equal("[Q]", xNamedQ.ToString());

            var xWithArg = _g.AddAttributeArguments(attrX, new[] { _g.AttributeArgument(_g.IdentifierName("e")) });
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(xWithArg));
            Assert.Equal(SyntaxKind.AttributeList, xWithArg.Kind());
            Assert.Equal("[X(e)]", xWithArg.ToString());

            // Inserting new attributes
            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertAttributes(declC, 0, _g.Attribute("A")),
@"[A]
[X, Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertAttributes(declC, 1, _g.Attribute("A")),
@"[X]
[A]
[Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertAttributes(declC, 2, _g.Attribute("A")),
@"[X, Y]
[A]
[Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.InsertAttributes(declC, 3, _g.Attribute("A")),
@"[X, Y, Z]
[A]
public class C
{
}");

            // Removing attributes
            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrX }),
@"[Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrY }),
@"[X, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrZ }),
@"[X, Y]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrX, attrY }),
@"[Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrX, attrZ }),
@"[Y]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrY, attrZ }),
@"[X]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.RemoveNodes(declC, new[] { attrX, attrY, attrZ }),
@"public class C
{
}");

            // Replacing attributes
            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, attrX, _g.Attribute("A")),
@"[A, Y, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, attrY, _g.Attribute("A")),
@"[X, A, Z]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, attrZ, _g.Attribute("A")),
@"[X, Y, A]
public class C
{
}");

            VerifySyntax<ClassDeclarationSyntax>(
                _g.ReplaceNode(declC, attrX, _g.AddAttributeArguments(attrX, new[] { _g.AttributeArgument(_g.IdentifierName("e")) })),
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
            var declM = _g.GetMembers(declC).First();

            Assert.Equal(0, _g.GetAttributes(declM).Count);

            var attrs = _g.GetReturnAttributes(declM);
            Assert.Equal(3, attrs.Count);
            var attrX = attrs[0];
            var attrY = attrs[1];
            var attrZ = attrs[2];

            Assert.Equal("X", _g.GetName(attrX));
            Assert.Equal("Y", _g.GetName(attrY));
            Assert.Equal("Z", _g.GetName(attrZ));

            var xNamedQ = _g.WithName(attrX, "Q");
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(xNamedQ));
            Assert.Equal(SyntaxKind.AttributeList, xNamedQ.Kind());
            Assert.Equal("[Q]", xNamedQ.ToString());

            var xWithArg = _g.AddAttributeArguments(attrX, new[] { _g.AttributeArgument(_g.IdentifierName("e")) });
            Assert.Equal(DeclarationKind.Attribute, _g.GetDeclarationKind(xWithArg));
            Assert.Equal(SyntaxKind.AttributeList, xWithArg.Kind());
            Assert.Equal("[X(e)]", xWithArg.ToString());

            // Inserting new attributes
            VerifySyntax<MethodDeclarationSyntax>(
                _g.InsertReturnAttributes(declM, 0, _g.Attribute("A")),
@"[return: A]
[return: X, Y, Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.InsertReturnAttributes(declM, 1, _g.Attribute("A")),
@"[return: X]
[return: A]
[return: Y, Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.InsertReturnAttributes(declM, 2, _g.Attribute("A")),
@"[return: X, Y]
[return: A]
[return: Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.InsertReturnAttributes(declM, 3, _g.Attribute("A")),
@"[return: X, Y, Z]
[return: A]
public void M()
{
}");

            // replacing
            VerifySyntax<MethodDeclarationSyntax>(
                _g.ReplaceNode(declM, attrX, _g.Attribute("Q")),
@"[return: Q, Y, Z]
public void M()
{
}");

            VerifySyntax<MethodDeclarationSyntax>(
                _g.ReplaceNode(declM, attrX, _g.AddAttributeArguments(attrX, new[] { _g.AttributeArgument(_g.IdentifierName("e")) })),
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
            var declM = _g.GetMembers(declC).First();

            var attrs = _g.GetAttributes(declM);
            Assert.Equal(4, attrs.Count);

            var attrX = attrs[0];
            Assert.Equal("X", _g.GetName(attrX));
            Assert.Equal(SyntaxKind.AttributeList, attrX.Kind());

            var attrY = attrs[1];
            Assert.Equal("Y", _g.GetName(attrY));
            Assert.Equal(SyntaxKind.Attribute, attrY.Kind());

            var attrZ = attrs[2];
            Assert.Equal("Z", _g.GetName(attrZ));
            Assert.Equal(SyntaxKind.Attribute, attrZ.Kind());

            var attrP = attrs[3];
            Assert.Equal("P", _g.GetName(attrP));
            Assert.Equal(SyntaxKind.AttributeList, attrP.Kind());

            var rattrs = _g.GetReturnAttributes(declM);
            Assert.Equal(4, rattrs.Count);

            var attrA = rattrs[0];
            Assert.Equal("A", _g.GetName(attrA));
            Assert.Equal(SyntaxKind.AttributeList, attrA.Kind());

            var attrB = rattrs[1];
            Assert.Equal("B", _g.GetName(attrB));
            Assert.Equal(SyntaxKind.Attribute, attrB.Kind());

            var attrC = rattrs[2];
            Assert.Equal("C", _g.GetName(attrC));
            Assert.Equal(SyntaxKind.Attribute, attrC.Kind());

            var attrD = rattrs[3];
            Assert.Equal("D", _g.GetName(attrD));
            Assert.Equal(SyntaxKind.Attribute, attrD.Kind());

            // inserting
            VerifySyntax<MethodDeclarationSyntax>(
                _g.InsertAttributes(declM, 0, _g.Attribute("Q")),
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
                _g.InsertAttributes(declM, 1, _g.Attribute("Q")),
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
                _g.InsertAttributes(declM, 2, _g.Attribute("Q")),
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
                _g.InsertAttributes(declM, 3, _g.Attribute("Q")),
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
                _g.InsertAttributes(declM, 4, _g.Attribute("Q")),
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
                _g.InsertReturnAttributes(declM, 0, _g.Attribute("Q")),
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
                _g.InsertReturnAttributes(declM, 1, _g.Attribute("Q")),
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
                _g.InsertReturnAttributes(declM, 2, _g.Attribute("Q")),
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
                _g.InsertReturnAttributes(declM, 3, _g.Attribute("Q")),
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
                _g.InsertReturnAttributes(declM, 4, _g.Attribute("Q")),
@"[X]
[return: A]
[Y, Z]
[return: B, C, D]
[return: Q]
[P]
public void M()
{
}");
        }

        [WorkItem(293, "https://github.com/dotnet/roslyn/issues/293")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IntroduceBaseList()
        {
            var text = @"
public class C
{
}
";
            var expected = @"
public class C : IDisposable
{
}
";

            var root = SyntaxFactory.ParseCompilationUnit(text);
            var decl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var newDecl = _g.AddInterfaceType(decl, _g.IdentifierName("IDisposable"));
            var newRoot = root.ReplaceNode(decl, newDecl);

            var elasticOnlyFormatted = (await Formatter.FormatAsync(newRoot, SyntaxAnnotation.ElasticAnnotation, _ws)).ToFullString();
            Assert.Equal(expected, elasticOnlyFormatted);
        }

        #endregion
    }
}
