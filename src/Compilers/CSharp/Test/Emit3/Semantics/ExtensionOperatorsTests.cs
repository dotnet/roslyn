// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CSharp.UnitTests.UserDefinedCompoundAssignmentOperatorsTests;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.Extensions)]
    public class ExtensionOperatorsTests : CompilingTestBase
    {
        [Fact]
        public void Conversions_001_Declaration()
        {
            var src = $$$"""
static class Extensions
{
    extension(S1)
    {
        public static implicit operator int(S1 x) => 0;
    }

    extension(S2)
    {
        public static explicit operator int(S2 x) => 0;
    }
}

struct S1
{}

struct S2
{}

static class C1
{
    static void Test()
    {
        var s1 = new S1();
        var i1 = (int)s1;

        var s2 = new S2();
        var i2 = (int)s2;
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,41): error CS9282: This member is not allowed in an extension block
                //         public static implicit operator int(S1 x) => 0;
                Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "int").WithLocation(5, 41),
                // (10,41): error CS9282: This member is not allowed in an extension block
                //         public static explicit operator int(S2 x) => 0;
                Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "int").WithLocation(10, 41),
                // (25,18): error CS0030: Cannot convert type 'S1' to 'int'
                //         var i1 = (int)s1;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int)s1").WithArguments("S1", "int").WithLocation(25, 18),
                // (28,18): error CS0030: Cannot convert type 'S2' to 'int'
                //         var i2 = (int)s2;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int)s2").WithArguments("S2", "int").WithLocation(28, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_001_Declaration([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
        public static S1? operator {{{op}}}(S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
        public static S2 operator {{{op}}}(S1 x) => default;
        public static S1 operator {{{op}}}(S2 x) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
        public static void operator {{{op}}}(S1 x) {}
    }
}

static class Extensions4
{
    extension(S1)
    {
        static S1 operator {{{op}}}(S1 x) => default;
    }

    extension(S2)
    {
        public S2 operator {{{op}}}(S2 x) => default;
    }

    extension(C1)
    {
        public static S1 operator {{{op}}}(C1 x) => default;
    }
}

static class Extensions5
{
    extension(S1?)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
        public static S2 operator {{{op}}}(S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,36): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static S1? operator +(S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, op).WithLocation(6, 36),
                // (15,35): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static S1 operator +(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, op).WithLocation(15, 35),
                // (23,37): error CS0590: User-defined operators cannot return void
                //         public static void operator +(S1 x) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, op).WithLocation(23, 37),
                // (31,28): error CS0558: User-defined operator 'Extensions4.extension(S1).operator +(S1)' must be declared static and public
                //         static S1 operator +(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions4.extension(S1).operator " + op + "(S1)").WithLocation(31, 28),
                // (36,28): error CS0558: User-defined operator 'Extensions4.extension(S2).operator +(S2)' must be declared static and public
                //         public S2 operator +(S2 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions4.extension(S2).operator " + op + "(S2)").WithLocation(36, 28),
                // (41,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator +(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(41, 35),
                // (41,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator +(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(41, 37),
                // (49,35): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static S1 operator +(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, op).WithLocation(49, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_002_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
        public static S1? operator {{{op}}}(S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
        public static S2 operator {{{op}}}(S1 x) => default;
        public static S1 operator {{{op}}}(S2 x) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
        static S1 operator {{{op}}}(S1 x) => default;
    }

    extension(S2)
    {
        public S2 operator {{{op}}}(S2 x) => default;
    }

    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x) => default;
    }
}

static class Extensions4
{
    extension(S1?)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
        public static S1 operator {{{op}}}(S1? x) => default;
    }
}

static class Extensions5
{
    extension(S1?)
    {
        public static S1? operator {{{op}}}(S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (6,36): error CS9318: The parameter type for ++ or -- operator must be the extended type.
                //         public static S1? operator ++(S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionIncDecSignature, op).WithLocation(6, 36),
                // (14,35): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //         public static S2 operator ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(14, 35),
                // (15,35): error CS9318: The parameter type for ++ or -- operator must be the extended type.
                //         public static S1 operator ++(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionIncDecSignature, op).WithLocation(15, 35),
                // (23,28): error CS0558: User-defined operator 'Extensions3.extension(S1).operator ++(S1)' must be declared static and public
                //         static S1 operator ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions3.extension(S1).operator " + op + "(S1)").WithLocation(23, 28),
                // (28,28): error CS0558: User-defined operator 'Extensions3.extension(S2).operator ++(S2)' must be declared static and public
                //         public S2 operator ++(S2 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions3.extension(S2).operator " + op + "(S2)").WithLocation(28, 28),
                // (33,23): error CS0722: 'C1': static types cannot be used as return types
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C1").WithArguments("C1").WithLocation(33, 23),
                // (33,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(33, 35),
                // (33,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(33, 38),
                // (41,35): error CS9318: The parameter type for ++ or -- operator must be the extended type.
                //         public static S1 operator ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionIncDecSignature, op).WithLocation(41, 35),
                // (42,35): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //         public static S1 operator ++(S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(42, 35)
                );
        }

        [Fact]
        public void Unary_003_Declaration()
        {
            var src = """
static class Extensions1
{
    extension(S1)
    {
#line 100
        public static bool operator true(S1 x) => default;
        public static bool operator false(S1 x) => default;
        public static bool operator true(S1? x) => default;
        public static bool operator false(S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 200
        public static bool operator true(S1 x) => default;
    }

    extension(S1)
    {
#line 300
        public static bool operator false(S1 x) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
#line 400
        public static bool operator true(S1 x) => default;
    }

    extension(S2)
    {
        public static bool operator false(S2 x) => default;
    }
}

static class Extensions4
{
    extension(S1)
    {
#line 500
        public static S1 operator true(S1 x) => default;
    }
}

static class Extensions5
{
    extension(S1)
    {
#line 600
        public static bool operator true(S2 x) => default;
        public static bool operator false(S2 x) => default;
    }
}

static class Extensions6
{
    extension(S1)
    {
#line 700
        static bool operator true(S1 x) => default;
        public bool operator false(S1 x) => default;
    }

    extension(C1)
    {
#line 800
        public static bool operator true(C1 x) => default;
        public static bool operator false(C1 x) => default;
    }
}

static class Extensions7
{
    extension(S1?)
    {
#line 900
        public static bool operator true(S1 x) => default;
        public static bool operator false(S1 x) => default;
        public static bool operator true(S1? x) => default;
        public static bool operator false(S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (102,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(102, 37),
                // (103,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(103, 37),
                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator true(S1)' requires a matching operator 'false' to also be defined
                //         public static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "true").WithArguments("Extensions3.extension(S1).operator true(S1)", "false").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator false(S2)' requires a matching operator 'true' to also be defined
                //         public static bool operator false(S2 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "false").WithArguments("Extensions3.extension(S2).operator false(S2)", "true").WithLocation(405, 37),
                // (500,35): error CS0215: The return type of operator True or False must be bool
                //         public static S1 operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OpTFRetType, "true").WithLocation(500, 35),
                // (500,35): error CS0216: The operator 'Extensions4.extension(S1).operator true(S1)' requires a matching operator 'false' to also be defined
                //         public static S1 operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "true").WithArguments("Extensions4.extension(S1).operator true(S1)", "false").WithLocation(500, 35),
                // (600,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(600, 37),
                // (601,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(601, 37),
                // (700,30): error CS0558: User-defined operator 'Extensions6.extension(S1).operator true(S1)' must be declared static and public
                //         static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "true").WithArguments("Extensions6.extension(S1).operator true(S1)").WithLocation(700, 30),
                // (701,30): error CS0558: User-defined operator 'Extensions6.extension(S1).operator false(S1)' must be declared static and public
                //         public bool operator false(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "false").WithArguments("Extensions6.extension(S1).operator false(S1)").WithLocation(701, 30),
                // (800,37): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static bool operator true(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "true").WithLocation(800, 37),
                // (800,42): error CS0721: 'C1': static types cannot be used as parameters
                //         public static bool operator true(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 42),
                // (801,37): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static bool operator false(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "false").WithLocation(801, 37),
                // (801,43): error CS0721: 'C1': static types cannot be used as parameters
                //         public static bool operator false(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 43),
                // (900,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(900, 37),
                // (901,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(901, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_004_Declaration([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

public struct S1
{}
""";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var name = UnaryOperatorName(op);
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(S1)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        private static string UnaryOperatorName(string op, bool isChecked = false)
        {
            return OperatorFacts.UnaryOperatorNameFromSyntaxKind(SyntaxFactory.ParseToken(op).Kind(), isChecked: isChecked);
        }

        [Theory]
        [CombinatorialData]
        public void Unary_005_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var name = UnaryOperatorName(op);
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(S1)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        [Fact]
        public void Unary_006_Declaration()
        {
            var src = """
static class Extensions1
{
    extension(S1)
    {
        public static bool operator true(S1 x) => default;
        public static bool operator false(S1 x) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.TrueOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.TrueOperatorName + "(S1)", method.ToDisplayString());
                verifyMethod(method);

                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.FalseOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.FalseOperatorName + "(S1)", method.ToDisplayString());
                verifyMethod(method);
            }

            static void verifyMethod(MethodSymbol method)
            {
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Unary_007_Declaration([CombinatorialValues("+", "-", "!", "~", "++", "--")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        {{{modifier}}}
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,35): error CS0106: The modifier 'abstract' is not valid for this item
                //         public static S1 operator !(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_008_Declaration([CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        {{{modifier}}}
        public static bool operator true(S1 x) => default;

        {{{modifier}}}
        public static bool operator false(S1 x) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,37): error CS0106: The modifier 'abstract' is not valid for this item
                //         public static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "true").WithArguments(modifier).WithLocation(6, 37),
                // (9,37): error CS0106: The modifier 'abstract' is not valid for this item
                //         public static bool operator false(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "false").WithArguments(modifier).WithLocation(9, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_009_Declaration([CombinatorialValues("-", "++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S1 operator checked {{{op}}}(S1 x) => default;
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 100
        public static S1 operator checked {{{op}}}(S1 x) => default;
    }
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
        public static S1 operator checked {{{op}}}(S1 x) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (112,43): error CS9025: The operator 'Extensions3.extension(S1).operator checked ++(S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "(S1)").WithLocation(112, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_010_Consumption(bool fromMetadata, [CombinatorialValues("+", "-", "!", "~")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            return new S1 { F = x.F + 1 };
        }
    }
}

public {{{typeKind}}} S1
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = 101 };
        var s2 = {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s1.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:101:102").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator " + op + "(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:101:102").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = !s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(6, 18)
                );

            var opName = UnaryOperatorName(op);
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.{{{opName}}}(s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.{{{opName}}}();
        S1.{{{opName}}}(s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_UnaryPlus' and no accessible extension method 'op_UnaryPlus' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_UnaryPlus();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("S1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_UnaryPlus'
                //         S1.op_UnaryPlus(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("S1", opName).WithLocation(7, 12)
                );
        }

        [Fact]
        public void Unary_011_Consumption_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x) => throw null;
    }
}

public struct S2
{
    public static implicit operator int(S2 x)
    {
        System.Console.Write("operator2");
        return 0;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        int x = +s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("int.operator +(int)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Int32", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Unary_012_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x)
    {
        System.Console.Write("operator2");
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = +s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator +(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Unary_013_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator +(S1 x)
            {
                System.Console.Write("operator1");
                return x;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator +(S2 x) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = +s1;
            }
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator +(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Unary_014_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static I1 operator -(I1 x) => x;
}

public interface I3
{
    public static I3 operator -(I3 x) => x;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator -(I2 x) => x;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = -x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (32,17): error CS9342: Operator resolution is ambiguous between the following members: 'I1.operator -(I1)' and 'I3.operator -(I3)'
                //         var y = -x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("I1.operator -(I1)", "I3.operator -(I3)").WithLocation(32, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator -(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator -(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78830")]
        public void Unary_015_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator -(I2 x) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static I1 operator -(I1 x) => x;
        }

        extension(I3)
        {
            public static I3 operator -(I3 x) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = -x;
        }
    }
}
""";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (34,21): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(I1).operator -(I1)' and 'Extensions2.extension(I3).operator -(I3)'
                //             var y = -x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("NS1.Extensions2.extension(I1).operator -(I1)", "NS1.Extensions2.extension(I3).operator -(I3)").WithLocation(34, 21)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator -(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator -(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Theory]
        [CombinatorialData]
        public void Unary_016_Consumption_Lifted([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x)
        {
            System.Console.Write("operator1");
            return new S1 { F = x.F + 1 };
        }
    }
}

public struct S1
{
    public int F;
}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1? s1 = new S1() { F = 101 };
        var s2 = {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s2.Value.F);
        System.Console.Write(":");
        s1 = null;
        s2 = {{{op}}}s1;
        System.Console.Write(s2?.F ?? -1);
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "operator1:102:-1").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:102:-1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = !s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(6, 18),
                // (11,14): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s2 = !s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(11, 14)
                );
        }

        [Fact]
        public void Unary_017_Consumption_LiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator +(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = +s1;
        System.Console.Write(":");
        s1 = null;
        _ = +s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_018_Consumption_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static S1? operator +(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = +s1;
        Extensions1.op_UnaryPlus(s1);

        S1? s2 = new S1();
        _ = +s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (21,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "S1").WithLocation(21, 13)
                );
        }

        [Fact]
        public void Unary_019_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = +s1;
        Extensions1.op_UnaryPlus(s1);

        S1? s2 = new S1();
        _ = +s2;
        Extensions1.op_UnaryPlus(s2);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "S1").WithLocation(22, 13),
                // (26,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1?'
                //         _ = +s2;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s2").WithArguments("+", "S1?").WithLocation(26, 13),
                // (27,34): error CS1503: Argument 1: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_UnaryPlus(s2);
                Diagnostic(ErrorCode.ERR_BadArgType, "s2").WithArguments("1", "S1?", "S2").WithLocation(27, 34)
                );
        }

        [Fact]
        public void Unary_020_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : struct
    {
        public static S1<T> operator +(S1<T> x)
        {
            System.Console.Write(typeof(T).ToString());
            return x;
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        s1 = +s1;
        Extensions1.op_UnaryPlus(s1);

        S1<int>? s2 = new S1<int>();
        _ = (+s2).GetValueOrDefault();
        s2 = null;
        System.Console.Write(":");
        _ = (+s2).GetValueOrDefault();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32System.Int32:").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_020_Consumption_Generic_Worse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>)
    {
        public static S1<T> operator +(S1<T> x)
        {
            System.Console.Write("[S1<T>]");
            return x;
        }
    }

    extension<T>(S1<T>?)
    {
        public static S1<T>? operator +(S1<T>? x)
        {
            System.Console.Write("[S1<T>?]");
            return x;
        }
    }

    extension(S1<int>)
    {
        public static S1<int> operator +(S1<int> x)
        {
            System.Console.Write("[S1<int>]");
            return x;
        }
    }

    extension<T>(S2<T>)
    {
        public static S2<T> operator +(in S2<T> x) => throw null;

        public static S2<T> operator +(S2<T> x)
        {
            System.Console.Write("[S2<T>]");
            return x;
        }
    }

    extension(S2<int>)
    {
        public static S2<int> operator +(in S2<int> x)
        {
            System.Console.Write("[in S2<int>]");
            return x;
        }
    }
}

public struct S1<T>
{}

public struct S2<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        s11 = +s11;
        Extensions1.op_UnaryPlus(s11);

        System.Console.WriteLine();

        var s12 = new S1<byte>();
        s12 = +s12;
        Extensions1.op_UnaryPlus(s12);

        System.Console.WriteLine();

        var s21 = new S2<int>();
        s21 = +s21;
        Extensions1.op_UnaryPlus(s21);

        System.Console.WriteLine();

        var s22 = new S2<byte>();
        s22 = +s22;
        Extensions1.op_UnaryPlus(s22);

        System.Console.WriteLine();

        S1<int>? s13 = new S1<int>();
        s13 = +s13;
        s13 = null;
        s13 = +s13;

        System.Console.WriteLine();

        S1<byte>? s14 = new S1<byte>();
        s14 = +s14;
        s14 = null;
        s14 = +s14;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
[S1<int>][S1<int>]
[S1<T>][S1<T>]
[in S2<int>][in S2<int>]
[S2<T>][S2<T>]
[S1<int>]
[S1<T>?][S1<T>?]
").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_021_Consumption_Generic_ConstraintsViolation()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : class
    {
        public static S1<T> operator +(S1<T> x) => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        _ = +s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1<int>'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "S1<int>").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Unary_022_Consumption_OverloadResolutionPriority()
        {
            var src = $$$"""
using System.Runtime.CompilerServices;

public static class Extensions1
{
    extension(C1)
    {
        [OverloadResolutionPriority(1)]
        public static C1 operator +(C1 x)
        {
            System.Console.Write("C1");
            return x;
        }
    }
    extension(C2)
    {
        public static C2 operator +(C2 x)
        {
            System.Console.Write("C2");
            return x;
        }
    }
    extension(C3)
    {
        public static C3 operator +(C3 x)
        {
            System.Console.Write("C3");
            return x;
        }
    }
    extension(C4)
    {
        public static C4 operator +(C4 x)
        {
            System.Console.Write("C4");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;

public class C3;
public class C4 : C3;

class Program
{
    static void Main()
    {
        var c2 = new C2();
        _ = +c2;
        var c4 = new C4();
        _ = +c4;
    }
}
""";

            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1C4").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_023_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = -c1;

        checked
        {
            _ = -c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_023_Consumption_Checked_CheckedFormNotSupported()
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator +(C1 x) => throw null;
        public static C1 operator checked +(C1 x) => throw null;
    }
}

public class C1;
""";

            var comp1 = CreateCompilation(src1);
            comp1.VerifyEmitDiagnostics(
                // (6,35): error CS9023: User-defined operator '+' cannot be declared checked
                //         public static C1 operator checked +(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("+").WithLocation(6, 35),
                // (6,43): error CS0111: Type 'Extensions1' already defines a member called 'op_UnaryPlus' with the same parameter types
                //         public static C1 operator checked +(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "+").WithArguments("op_UnaryPlus", "Extensions1").WithLocation(6, 43)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator +(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = +c1;

        checked
        {
            _ = +c1;
        }
    }
}
""";

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Unary_024_Consumption_Checked([CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
        public static C1 operator checked -(C1 x)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public {{{typeKind}}} C1;
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = -c1;

        checked
        {
            _ = -c1;
        }
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "regularchecked").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "regularchecked").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = -c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "-c1").WithArguments("extensions", "14.0").WithLocation(6, 13),
                // (10,17): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //             _ = -c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "-c1").WithArguments("extensions", "14.0").WithLocation(10, 17)
                );
        }

        [Fact]
        public void Unary_025_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension(C1)
    {
        public static C1 operator checked -(C1 x)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = -c1;

        checked
        {
            _ = -c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_026_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            return x;
        }

        public static C1 operator checked -(C1 x)
        {
            return x;
        }
    }
}

public static class Extensions2
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = -c1;

        checked
        {
            _ = -c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
#if DEBUG
            comp.VerifyEmitDiagnostics(
                // (35,13): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions2.extension(C1).operator -(C1)' and 'Extensions1.extension(C1).operator -(C1)'
                //         _ = -c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions2.extension(C1).operator -(C1)", "Extensions1.extension(C1).operator -(C1)").WithLocation(35, 13),
                // (39,17): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked -(C1)' and 'Extensions2.extension(C1).operator -(C1)'
                //             _ = -c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(C1).operator checked -(C1)", "Extensions2.extension(C1).operator -(C1)").WithLocation(39, 17)
                );
#else
            comp.VerifyEmitDiagnostics(
                // (35,13): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator -(C1)' and 'Extensions2.extension(C1).operator -(C1)'
                //         _ = -c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(C1).operator -(C1)", "Extensions2.extension(C1).operator -(C1)").WithLocation(35, 13),
                // (39,17): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked -(C1)' and 'Extensions2.extension(C1).operator -(C1)'
                //             _ = -c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(C1).operator checked -(C1)", "Extensions2.extension(C1).operator -(C1)").WithLocation(39, 17)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().Last();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("Extensions1.extension(C1).operator checked -(C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(C1).operator -(C1)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        public void Unary_027_Consumption_CheckedLiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator -(S1 x) => throw null;
        public static S1 operator checked -(S1 x) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator -(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = -s1;
        System.Console.Write(":");

        checked
        {
            _ = -s1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_028_Consumption_OverloadResolutionPlusRegularVsChecked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            System.Console.Write("C1");
            return x;
        }
        public static C1 operator checked -(C1 x)
        {
            System.Console.Write("checkedC1");
            return x;
        }
    }
    extension(C2)
    {
        public static C2 operator -(C2 x)
        {
            System.Console.Write("C2");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;
public class C3 : C1;

class Program
{
    static void Main()
    {
        var c3 = new C3();
        _ = -c3;

        checked
        {
            _ = -c3;
        }

        var c2 = new C2();
        _ = -c2;

        checked
        {
            _ = -c2;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1checkedC1C2C2").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_029_Consumption()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1? operator +(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static void M1(S1? x) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
#line 21
        _ = +s1;
        S1 s2 = new S1();
        _ = +s2;

        System.Nullable<S1>.M1(s1);
        S1.M1(s1);
        S1.M1(s2);
    }
}

public static class Extensions2
{
    extension(S2)
    {
        public static S2? operator +(S2 x)
        {
            return x;
        }
    }
}

public struct S2
{}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (5,36): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static S1? operator +(S1? x)
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "+").WithLocation(5, 36),
                // (21,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1?'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "S1?").WithLocation(21, 13),
                // (25,9): error CS1929: 'S1?' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1).M1(S1?)' requires a receiver of type 'S1'
                //         System.Nullable<S1>.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "System.Nullable<S1>").WithArguments("S1?", "M1", "Extensions1.extension(S1).M1(S1?)", "S1").WithLocation(25, 9)
                );
        }

        [Fact]
        public void Unary_030_Consumption_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator +(object x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = +s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_031_Consumption_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator +(object x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        try
        {
            _ = +s1;
        }
        catch
        {
            System.Console.Write("exception");
        }
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "exception").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_032_Consumption_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator +(object x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}
class Program
{
    static void Main()
    {
        _ = +null;
        _ = +default;
        _ = +new();
        _ = +(() => 1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (19,13): error CS8310: Operator '+' cannot be applied to operand '<null>'
                //         _ = +null;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "+null").WithArguments("+", "<null>").WithLocation(19, 13),
                // (20,14): error CS8716: There is no target type for the default literal.
                //         _ = +default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(20, 14),
                // (21,14): error CS8754: There is no target type for 'new()'
                //         _ = +new();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(21, 14),
                // (22,13): error CS0023: Operator '+' cannot be applied to operand of type 'lambda expression'
                //         _ = +(() => 1);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+(() => 1)").WithArguments("+", "lambda expression").WithLocation(22, 13)
                );
        }

        [Fact]
        public void Unary_033_Consumption_BadReceiver()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(__arglist)
    {
        public static object operator +(object x)
        {
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = +s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static object operator +(object x)
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "+").WithLocation(5, 39),
                // (17,13): error CS0023: Operator '+' cannot be applied to operand of type 'object'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "object").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Unary_034_Consumption_Checked_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T1>(C1<T1>)
    {
        public static C1<T1> operator -(C1<T1> x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension<T2>(C1<T2>)
    {
        public static C1<T2> operator checked -(C1<T2> x)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1<T>;

class Program
{
    static void Main()
    {
        var c1 = new C1<int>();
        _ = -c1;

        checked
        {
            _ = -c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Unary_035_Consumption_True(bool fromMetadata, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator true(S1 x)
        {
            System.Console.Write("operator1");
            return x.F;
        }
        public static bool operator false(S1 x) => throw null;
    }
}

public {{{typeKind}}} S1
{
    public bool F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = true };
        if (s1)
        {
            System.Console.Write(":true:");
        }

        s1 = new S1() { F = false };
        if (s1)
        {}
        else
        {
            System.Console.Write(":false");
        }
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:true:operator1:false").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.IfStatementSyntax>().First().Condition;
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("s1", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:true:operator1:false").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if (s1)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1").WithArguments("extensions", "14.0").WithLocation(6, 13),
                // (12,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if (s1)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1").WithArguments("extensions", "14.0").WithLocation(12, 13)
                );

            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.op_True(s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.op_True();
        S1.op_True(s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_True' and no accessible extension method 'op_True' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_True();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "op_True").WithArguments("S1", "op_True").WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_True'
                //         S1.op_True(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "op_True").WithArguments("S1", "op_True").WithLocation(7, 12)
                );
        }

        [Fact]
        public void Unary_036_Consumption_True_ConversionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static bool operator true(S2 x) => throw null;
        public static bool operator false(S2 x) => throw null;
    }
}

public struct S2
{
    public static implicit operator bool(S2 x)
    {
        System.Console.Write("operator2");
        return true;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        if(s2)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_037_Consumption_True_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static bool operator true(S2 x) => throw null;
        public static bool operator false(S2 x) => throw null;
    }
}

public struct S2
{
    public static bool operator true(S2 x)
    {
        System.Console.Write("operator2");
        return true;
    }
    public static bool operator false(S2 x) => throw null;
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        if(s2)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_038_Consumption_True_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator true(S1 x) => throw null;
        public static bool operator false(S1 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static bool operator true(S1 x)
            {
                System.Console.Write("operator1");
                return true;
            }
            public static bool operator false(S1 x) => throw null;
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static bool operator true(S2 x) => throw null;
                public static bool operator false(S2 x) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                if(s1)
                {}
            }
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_039_Consumption_True_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static bool operator true(I1 x) => true;
    public static bool operator false(I1 x) => false;
}

public interface I3
{
    public static bool operator true(I3 x) => true;
    public static bool operator false(I3 x) => false;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2)
    {
        public static bool operator true(I2 x) => true;
        public static bool operator false(I2 x) => false;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        if(x)
        {}
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (35,12): error CS0029: Cannot implicitly convert type 'I2' to 'bool'
                //         if(x)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("I2", "bool").WithLocation(35, 12)
                );
        }

        [Fact]
        public void Unary_040_Consumption_True_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2)
    {
        public static bool operator true(I2 x) => true;
        public static bool operator false(I2 x) => false;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static bool operator true(I1 x) => true;
            public static bool operator false(I1 x) => false;
        }

        extension(I3)
        {
            public static bool operator true(I3 x) => true;
            public static bool operator false(I3 x) => false;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            if(x)
            {}
        }
    }
}
""";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (37,16): error CS0029: Cannot implicitly convert type 'I2' to 'bool'
                //             if(x)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("I2", "bool").WithLocation(37, 16)
                );
        }

        [Fact]
        public void Unary_041_Consumption_True_NoLiftedForm()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator true(S1 x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(S1 x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        if (s1)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,13): error CS0029: Cannot implicitly convert type 'S1?' to 'bool'
                //         if (s1)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1?", "bool").WithLocation(22, 13)
                );
        }

        [Fact]
        public void Unary_042_Consumption_True_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static bool operator true(S1? x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(S1? x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        if (s1)
        {}
        Extensions1.op_True(s1);

        S1? s2 = new S1();
        if (s2)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,13): error CS0029: Cannot implicitly convert type 'S1' to 'bool'
                //         if (s1)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1", "bool").WithLocation(22, 13)
                );
        }

        [Fact]
        public void Unary_043_Consumption_True()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator true(S1? x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(S1? x) => throw null;

        public static void M1(S1? x) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
#line 22
        if (s1)
        {}
        S1 s2 = new S1();
        if (s2)
        {}

        System.Nullable<S1>.M1(s1);
        S1.M1(s1);
        S1.M1(s2);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (5,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S1? x)
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(5, 37),
                // (10,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S1? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(10, 37),
                // (22,13): error CS0029: Cannot implicitly convert type 'S1?' to 'bool'
                //         if (s1)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1?", "bool").WithLocation(22, 13),
                // (28,9): error CS1929: 'S1?' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1).M1(S1?)' requires a receiver of type 'S1'
                //         System.Nullable<S1>.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "System.Nullable<S1>").WithArguments("S1?", "M1", "Extensions1.extension(S1).M1(S1?)", "S1").WithLocation(28, 9)
                );
        }

        [Fact]
        public void Unary_044_Consumption_True_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static bool operator true(object x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        if (s1)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_045_Consumption_True_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static bool operator true(object x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        try
        {
            if (s1)
            {}
        }
        catch
        {
            System.Console.Write("exception");
        }
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "exception").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Unary_046_Consumption_TupleComparison(bool fromMetadata)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator true(S1 x) => throw null;
        public static bool operator false(S1 x)
        {
            System.Console.Write("operator1");
            return true;
        }
    }
}

public struct S1
{
    public static S1 operator ==(S1 x, S1 y)
    {
        return x;
    }
    public static S1 operator !=(S1 x, S1 y) => throw null;

    public override bool Equals(object o) => throw null;
    public override int GetHashCode() => throw null;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        if ((s1, 1) == (s1, 1))
        {}
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if ((s1, 1) == (s1, 1))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "(s1, 1) == (s1, 1)").WithArguments("extensions", "14.0").WithLocation(6, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_047_Consumption_ExpressionTree([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator {{{op}}}(S1 x)
        {
            System.Console.Write("operator1");
            return x;
        }

        public void Test()
        {
            Expression<System.Func<S1, S1>> ex = (s1) => {{{op}}}s1;
            ex.Compile()(s1);
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => {{{op}}}s1;

        var s1 = new S1();
        ex.Compile()(s1);

        s1.Test();

        System.Console.Write(":");
        System.Console.Write(ex);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1:s1 => " + (op is "!" or "~" ? "Not(" : op) + "s1" + (op is "!" or "~" ? ")" : "")).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Unary_048_Consumption_Lifted_ExpressionTree([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator {{{op}}}(S1 x)
        {
            System.Console.Write("operator1");
            return x;
        }

        public void Test()
        {
            Expression<System.Func<S1?, S1?>> ex = (s1) => {{{op}}}s1;
            var d = ex.Compile();
            
            d(s1);
            System.Console.Write(":");
            d(null);
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1?, S1?>> ex = (s1) => {{{op}}}s1;

        var s1 = new S1();
        var d = ex.Compile();
            
        d(s1);
        System.Console.Write(":");
        d(null);

        System.Console.Write(":");
        s1.Test();

        System.Console.Write(":");
        System.Console.Write(ex);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1::operator1::s1 => " + (op is "!" or "~" ? "Not(" : op) + "s1" + (op is "!" or "~" ? ")" : "")).VerifyDiagnostics();
        }

        [Fact]
        public void Unary_049_Consumption_True_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static bool operator true(S1 x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(S1 x) => throw null;

        public void Test()
        {
            Expression<System.Func<S1, int>> ex = (s1) => s1 ? 1 : 0;
            ex.Compile()(s1);
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, int>> ex = (s1) => s1 ? 1 : 0;

        var s1 = new S1();
        ex.Compile()(s1);

        s1.Test();

        System.Console.Write(":");
        System.Console.Write(ex);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1:s1 => IIF(op_True(s1), 1, 0)").VerifyDiagnostics();
        }

        [Fact]
        public void Unary_050_Consumption_True_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static bool operator true(S1 x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(S1 x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1?, int>> ex = (s1) => s1 ? 1 : 0;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,56): error CS0029: Cannot implicitly convert type 'S1?' to 'bool'
                //         Expression<System.Func<S1?, int>> ex = (s1) => s1 ? 1 : 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1?", "bool").WithLocation(23, 56)
                );
        }

        [Fact]
        public void Unary_051_Consumption_True_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1? s1)
    {
        public static bool operator true(S1? x)
        {
            System.Console.Write("operator1");
            return true;
        }
        public static bool operator false(S1? x) => throw null;

        public void Test()
        {
            Expression<System.Func<S1?, int>> ex = (s1) => s1 ? 1 : 0;
            ex.Compile()(s1);
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1?, int>> ex = (s1) => s1 ? 1 : 0;

        S1? s1 = new S1();
        ex.Compile()(s1);

        s1.Test();

        System.Console.Write(":");
        System.Console.Write(ex);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1:s1 => IIF(op_True(s1), 1, 0)").VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedUnaryOperator_RefStruct
        /// </summary>
        [Fact]
        public void Unary_052_RefSafety()
        {
            var source = """
class C
{
    S M1()
    {
        S s;
        s = +s; // 1
        return s;
    }

    S M2()
    {
        return +new S(); // 2
    }

    S M3(in S x)
    {
        S s;
        s = +x; // 3
        return s;
    }

    S M4(in S x)
    {
        return +x;
    }

    S M4s(scoped in S x)
    {
        return +x; // 4
    }

    S M5(in S x)
    {
        S s = +x;
        return s;
    }

    S M5s(scoped in S x)
    {
        S s = +x;
        return s; // 5
    }

    S M6()
    {
        S s = +new S();
        return s; // 6
    }

    void M7(in S x)
    {
        scoped S s;
        s = +x;
        s = +new S();
    }
}

ref struct S
{
}

static class Extensions
{
    extension(S)
    {
        public static S operator+(in S s) => throw null;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,13): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in S)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         s = +s; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "+s").WithArguments("Extensions.extension(S).operator +(in S)", "s").WithLocation(6, 13),
                // (6,14): error CS8168: Cannot return local 's' by reference because it is not a ref local
                //         s = +s; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "s").WithArguments("s").WithLocation(6, 14),
                // (12,16): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in S)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         return +new S(); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "+new S()").WithArguments("Extensions.extension(S).operator +(in S)", "s").WithLocation(12, 16),
                // (12,17): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return +new S(); // 2
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "new S()").WithLocation(12, 17),
                // (18,13): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in S)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         s = +x; // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "+x").WithArguments("Extensions.extension(S).operator +(in S)", "s").WithLocation(18, 13),
                // (18,14): error CS9077: Cannot return a parameter by reference 'x' through a ref parameter; it can only be returned in a return statement
                //         s = +x; // 3
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "x").WithArguments("x").WithLocation(18, 14),
                // (29,16): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in S)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         return +x; // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "+x").WithArguments("Extensions.extension(S).operator +(in S)", "s").WithLocation(29, 16),
                // (29,17): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return +x; // 4
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(29, 17),
                // (41,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(41, 16),
                // (47,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(47, 16)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedUnaryOperator_RefStruct_Scoped
        /// </summary>
        [Fact]
        public void Unary_053_RefSafety()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
        return !new R(0);
    }
}

static class Extensions
{
    extension(R)
    {
        public static R operator !(scoped R r) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        [Fact]
        public void Unary_054_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }

    extension(C2)
    {
        public static C2 operator -(C2? x)
        {
            System.Console.Write("operator2");
            return new C2();
        }
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x = null;
#line 23
        _ = -x;
        C1 y = new C1();
        y = -y;
        C2? z = null;
        _ = -z;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,14): warning CS8604: Possible null reference argument for parameter 'x' in 'C1 Extensions1.extension(C1).operator -(C1 x)'.
                //         _ = -x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "C1 Extensions1.extension(C1).operator -(C1 x)").WithLocation(23, 14)
                );
        }

        [Fact]
        public void Unary_055_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1? operator -(C1 x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var x = new C1();
        C1 y = -x;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,16): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         C1 y = -x;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "-x").WithLocation(23, 16)
                );
        }

        [Fact]
        public void Unary_056_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(T x)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (-x).ToString();
        var y = new C2();
        (-y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (22,10): warning CS8602: Dereference of a possibly null reference.
                //         (-x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "-x").WithLocation(22, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator -(C2?)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator -(C2)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Unary_057_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T>) where T : new()
    {
        public static T operator -(C1<T> x)
        {
            return x.F;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (-x).ToString();
        var y = Get(new C2());
        (-y).ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,10): warning CS8602: Dereference of a possibly null reference.
                //         (-x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "-x").WithLocation(27, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator -(C1<C2?>)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C1<C2>).operator -(C1<C2>)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Unary_058_NullableAnalysis_Lifted()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(S1<T>) where T : new()
    {
        public static S1<T> operator -(S1<T> x)
        {
            return x;
        }
    }
}

public struct S1<T> where T : new()
{
    public T F;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = -Get((C2?)null);

        if (x != null)
            x.Value.F.ToString();

        var y = -Get(new C2());

        if (y != null)
            y.Value.F.ToString();
    }

    static S1<T>? Get<T>(T x) where T : new()
    {
        return new S1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,13): warning CS8602: Dereference of a possibly null reference.
                //             x.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x.Value.F").WithLocation(29, 13)
                );
        }

        [Fact]
        public void Unary_059_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T) where T : notnull
    {
        public static object operator -(T x)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (-x).ToString();
        var y = new C2();
        (-y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (22,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (-x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "-x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(22, 10)
                );
        }

        [Fact]
        public void Unary_060_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T>) where T : notnull, new()
    {
        public static T operator -(C1<T> x)
        {
            return x.F;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (-x).ToString();
        var y = Get(new C2());
        (-y).ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (-x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "-x").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(27, 10),
                // (27,10): warning CS8602: Dereference of a possibly null reference.
                //         (-x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "-x").WithLocation(27, 10)
                );
        }

        [Fact]
        public void Unary_061_NullableAnalysis_True()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static bool operator true(C1 x) => true;
        public static bool operator false(C1 x) => false;
    }

    extension(C2)
    {
        public static bool operator true(C2? x) => true;
        public static bool operator false(C2? x) => false;
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x = null;
#line 20
        if (x)
        {}

        C1 y = new C1();
        if (y)
        {}

        C2? z= null;
        if (z)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (20,13): warning CS8604: Possible null reference argument for parameter 'x' in 'bool Extensions1.extension(C1).operator true(C1 x)'.
                //         if (x)
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "bool Extensions1.extension(C1).operator true(C1 x)").WithLocation(20, 13)
                );
        }

        [Fact]
        public void Unary_062_NullableAnalysis_True_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T) where T : notnull
    {
        public static bool operator true(T x) => true;
        public static bool operator false(T x) => false;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        if (x)
        {}

        var y = new C2();
        if (y)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (20,13): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         if (x)
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(20, 13)
                );
        }

        [Fact]
        public void Unary_063_Declaration_Extern()
        {
            var src = $$$"""
using System.Runtime.InteropServices;

public static class Extensions1
{
    extension(C2)
    {
        extern public static C2 operator -(C2 x) => x;

        [DllImport("something.dll")]
        public static C2 operator !(C2 x) => x;
    }
}

public class C2
{}
""";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,42): error CS0179: 'Extensions1.extension(C2).operator -(C2)' cannot be extern and declare a body
                //         extern public static C2 operator -(C2 x) => x;
                Diagnostic(ErrorCode.ERR_ExternHasBody, "-").WithArguments("Extensions1.extension(C2).operator -(C2)").WithLocation(7, 42),
                // (9,10): error CS0601: The DllImport attribute must be specified on a method marked 'extern' that is either 'static' or an extension member
                //         [DllImport("something.dll")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(9, 10)
                );
        }

        [Fact]
        public void Unary_064_Declaration_Extern()
        {
            var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(C2)
    {
        [DllImport("something.dll")]
        extern public static C2 operator -(C2 x);
    }
}

public class C2
{}
""";
            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$3D0C2090833F9460B6F186EEC21CE3B0'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$3D0C2090833F9460B6F186EEC21CE3B0'::'<Extension>$'
        } // end of class <M>$3D0C2090833F9460B6F186EEC21CE3B0
        // Methods
        .method public hidebysig specialname static 
            class C2 op_UnaryNegation (
                class C2 x
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 33 44 30 43 32 30 39 30 38
                33 33 46 39 34 36 30 42 36 46 31 38 36 45 45 43
                32 31 43 45 33 42 30 00 00
            )
            // Method begins at RVA 0x2086
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_UnaryNegation
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static pinvokeimpl("something.dll" winapi) 
        class C2 op_UnaryNegation (
            class C2 x
        ) cil managed preservesig 
    {
    } // end of method E::op_UnaryNegation
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void Unary_065_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2)
    {
        extern public static C2 operator -(C2 x);
    }
}

public class C2
{}
""";
            var verifier = CompileAndVerify(source, verify: Verification.FailsPEVerify with { PEVerifyMessage = """
                Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                Type load failed.
                """ });

            verifier.VerifyDiagnostics(
                // (5,42): warning CS0626: Method, operator, or accessor 'E.extension(C2).operator -(C2)' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern public static C2 operator -(C2 x);
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "-").WithArguments("E.extension(C2).operator -(C2)").WithLocation(5, 42)
                );

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$3D0C2090833F9460B6F186EEC21CE3B0'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$3D0C2090833F9460B6F186EEC21CE3B0'::'<Extension>$'
        } // end of class <M>$3D0C2090833F9460B6F186EEC21CE3B0
        // Methods
        .method public hidebysig specialname static 
            class C2 op_UnaryNegation (
                class C2 x
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 33 44 30 43 32 30 39 30 38
                33 33 46 39 34 36 30 42 36 46 31 38 36 45 45 43
                32 31 43 45 33 42 30 00 00
            )
            // Method begins at RVA 0x2086
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_UnaryNegation
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        class C2 op_UnaryNegation (
            class C2 x
        ) cil managed 
    {
    } // end of method E::op_UnaryNegation
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void Unary_066_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2)
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        extern public static C2 operator -(C2 x);
    }
}

public class C2
{}
""";
            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$3D0C2090833F9460B6F186EEC21CE3B0'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$3D0C2090833F9460B6F186EEC21CE3B0'::'<Extension>$'
        } // end of class <M>$3D0C2090833F9460B6F186EEC21CE3B0
        // Methods
        .method public hidebysig specialname static 
            class C2 op_UnaryNegation (
                class C2 x
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 33 44 30 43 32 30 39 30 38
                33 33 46 39 34 36 30 42 36 46 31 38 36 45 45 43
                32 31 43 45 33 42 30 00 00
            )
            // Method begins at RVA 0x2086
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_UnaryNegation
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        class C2 op_UnaryNegation (
            class C2 x
        ) cil managed internalcall 
    {
    } // end of method E::op_UnaryNegation
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Theory]
        [CombinatorialData]
        public void Unary_067_Consumption_CRef([CombinatorialValues("!", "~")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{op}}}"/>
/// <see cref="E.extension(S1).operator {{{op}}}(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator {{{op}}}(S1 x) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = UnaryOperatorName(op);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator " + op + ", S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))",
                "(E.extension(S1).operator " + op + "(S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Unary_068_Consumption_CRef([CombinatorialValues("+", "-")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{op}}}(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator {{{op}}}(S1 x) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = UnaryOperatorName(op);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal(["(E.extension(S1).operator " + op + "(S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Fact]
        public void Unary_068_Consumption_CRef_Checked()
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator checked -(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator -(S1 x) => throw null;
        ///
        public static S1 operator checked -(S1 x) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = UnaryOperatorName("-", isChecked: true);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal(["(E.extension(S1).operator checked -(S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Fact]
        public void Unary_069_Consumption_CRef_TrueFalse()
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator true"/>
/// <see cref="E.extension(S1).operator true(S1)"/>
/// <see cref="E.extension(S1).operator false"/>
/// <see cref="E.extension(S1).operator false(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static bool operator true(S1 x) => throw null;
        ///
        public static bool operator false(S1 x) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var trueName = UnaryOperatorName("true");
            var falseName = UnaryOperatorName("false");

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{trueName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{trueName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{falseName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{falseName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator true, System.Boolean E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + trueName + "(S1 x))",
                "(E.extension(S1).operator true(S1), System.Boolean E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + trueName + "(S1 x))",
                "(E.extension(S1).operator false, System.Boolean E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + falseName + "(S1 x))",
                "(E.extension(S1).operator false(S1), System.Boolean E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + falseName + "(S1 x))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Unary_070_Consumption_CRef_Error([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{op}}}"/>
/// <see cref="E.extension(S1).operator {{{op}}}()"/>
/// <see cref="E.extension(S1).operator {{{op}}}(S1)"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}()"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics(
                // (1,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator !' that could not be resolved
                // /// <see cref="E.extension(S1).operator !"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + op).WithArguments("extension(S1).operator " + op).WithLocation(1, 16),
                // (2,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator !()' that could not be resolved
                // /// <see cref="E.extension(S1).operator !()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + op + "()").WithArguments("extension(S1).operator " + op + "()").WithLocation(2, 16),
                // (3,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator !(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator !(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + op + "(S1)").WithArguments("extension(S1).operator " + op + "(S1)").WithLocation(3, 16),
                // (4,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked !' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked !"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + op).WithArguments("extension(S1).operator checked " + op).WithLocation(4, 16),
                // (5,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked !()' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked !()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + op + "()").WithArguments("extension(S1).operator checked " + op + "()").WithLocation(5, 16),
                // (6,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked !(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked !(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + op + "(S1)").WithArguments("extension(S1).operator checked " + op + "(S1)").WithLocation(6, 16)
                );
        }

        [Fact]
        public void Unary_071_Consumption_CRef_Error()
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator true"/>
/// <see cref="E.extension(S1).operator true()"/>
/// <see cref="E.extension(S1).operator true(S1)"/>
/// <see cref="E.extension(S1).operator false"/>
/// <see cref="E.extension(S1).operator false()"/>
/// <see cref="E.extension(S1).operator false(S1)"/>
/// <see cref="E.extension(S1).operator checked true"/>
/// <see cref="E.extension(S1).operator checked true()"/>
/// <see cref="E.extension(S1).operator checked true(S1)"/>
/// <see cref="E.extension(S1).operator checked false"/>
/// <see cref="E.extension(S1).operator checked false()"/>
/// <see cref="E.extension(S1).operator checked false(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics(
                // (1,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator true' that could not be resolved
                // /// <see cref="E.extension(S1).operator true"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator true").WithArguments("extension(S1).operator true").WithLocation(1, 16),
                // (2,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator true()' that could not be resolved
                // /// <see cref="E.extension(S1).operator true()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator true()").WithArguments("extension(S1).operator true()").WithLocation(2, 16),
                // (3,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator true(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator true(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator true(S1)").WithArguments("extension(S1).operator true(S1)").WithLocation(3, 16),
                // (4,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator false' that could not be resolved
                // /// <see cref="E.extension(S1).operator false"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator false").WithArguments("extension(S1).operator false").WithLocation(4, 16),
                // (5,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator false()' that could not be resolved
                // /// <see cref="E.extension(S1).operator false()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator false()").WithArguments("extension(S1).operator false()").WithLocation(5, 16),
                // (6,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator false(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator false(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator false(S1)").WithArguments("extension(S1).operator false(S1)").WithLocation(6, 16),
                // (7,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked true' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked true"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked true").WithArguments("extension(S1).operator checked true").WithLocation(7, 16),
                // (8,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked true()' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked true()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked true()").WithArguments("extension(S1).operator checked true()").WithLocation(8, 16),
                // (9,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked true(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked true(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked true(S1)").WithArguments("extension(S1).operator checked true(S1)").WithLocation(9, 16),
                // (10,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked false' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked false"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked false").WithArguments("extension(S1).operator checked false").WithLocation(10, 16),
                // (11,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked false()' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked false()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked false()").WithArguments("extension(S1).operator checked false()").WithLocation(11, 16),
                // (12,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked false(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked false(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked false(S1)").WithArguments("extension(S1).operator checked false(S1)").WithLocation(12, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_072_ERR_VoidError([CombinatorialValues("+", "-", "!", "~")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(void* x) => x;
    }
}

class Program
{
    unsafe void* Test(void* x) => {{{op}}}x;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (11,35): error CS0023: Operator '!' cannot be applied to operand of type 'void*'
                //     unsafe void* Test(void* x) => !x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, $"{op}x").WithArguments(op, "void*").WithLocation(11, 35)
                );
        }

        [Fact]
        public void Unary_073_ERR_VoidError_True()
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static bool operator true(void* x) => true;
        public static bool operator false(void* x) => false;
    }
}

class Program
{
    unsafe void Test(void* x)
    {
        if (x)
        {}
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (14,13): error CS0029: Cannot implicitly convert type 'void*' to 'bool'
                //         if (x)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("void*", "bool").WithLocation(14, 13)
                );
        }

        [Fact]
        public void Unary_074_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator -(S2 x) => throw null;
    }
}

public static class Extensions2
{
    extension(S2)
    {
        public static S2 operator -(S2 x) => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = -s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
#if DEBUG
            comp.VerifyDiagnostics(
                // (26,13): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(S2).operator -(S2)' and 'Extensions1.extension(S2).operator -(S2)'
                //         _ = -s2;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions2.extension(S2).operator -(S2)", "Extensions1.extension(S2).operator -(S2)").WithLocation(26, 13)
                );
#else
            comp.VerifyDiagnostics(
                // (26,13): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(S2).operator -(S2)' and 'Extensions2.extension(S2).operator -(S2)'
                //         _ = -s2;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(S2).operator -(S2)", "Extensions2.extension(S2).operator -(S2)").WithLocation(26, 13)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);

#if DEBUG // Collection of extension blocks depends on GetTypeMembersUnordered for namespace, which conditionally de-orders types for DEBUG only.
            AssertEx.Equal("Extensions2.extension(S2).operator -(S2)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions1.extension(S2).operator -(S2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
#else
            AssertEx.Equal("Extensions1.extension(S2).operator -(S2)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(S2).operator -(S2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
#endif
        }

        [Theory]
        [CombinatorialData]
        public void Increment_001_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions2
{
    extension(ref S1 s1)
    {
        public S1 operator {{{op}}}() => throw null;
    }
}

static class Extensions3
{
    extension(ref S1 s1)
    {
        void operator {{{op}}}() {}
    }
    extension(C1)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions4
{
    extension(ref S1? s1)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions5
{
    extension(S1 s1)
    {
#line 600
        public void operator {{{op}}}() {}
    }
#line 700
    extension(ref C2 c2)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions6
{
    extension(C2 c2)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions7
{
    extension(in S1 s1)
    {
#line 800
        public void operator {{{op}}}() {}
    }
#line 900
    extension(in C2 c2)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions8
{
    extension(ref readonly S1 s1)
    {
#line 1000
        public void operator {{{op}}}() {}
    }
#line 1100
    extension(ref readonly C2 c2)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions9
{
    extension<T>(T t) where T : struct
    {
#line 1200
        public void operator {{{op}}}() {}
    }
}

static class Extensions10
{
    extension<T>(T t) where T : class
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions11
{
    extension<T>(T t)
    {
#line 1300
        public void operator {{{op}}}() {}
    }
}

static class Extensions12
{
    extension<T>(ref T t) where T : struct
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions13
{
#line 1400
    extension<T>(ref T t) where T : class
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions14
{
#line 1500
    extension<T>(ref T t)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions15
{
#line 1600
    extension<T>(in T t) where T : struct
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions16
{
#line 1700
    extension<T>(in T t) where T : class
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions17
{
#line 1800
    extension<T>(in T t)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions18
{
#line 1900
    extension<T>(ref readonly T t) where T : struct
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions19
{
#line 2000
    extension<T>(ref readonly T t) where T : class
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions20
{
#line 2100
    extension<T>(ref readonly T t)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions21
{
    extension(C2)
    {
#line 2200
        public void operator {{{op}}}() {}
    }
}

struct S1
{}

static class C1
{}

class C2
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (13,28): error CS9503: The return type for this operator must be void
                //         public S1 operator ++() => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(13, 28),
                // (21,23): error CS9501: User-defined operator 'Extensions3.extension(ref S1).operator ++()' must be declared public
                //         void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("Extensions3.extension(ref S1).operator " + op + "()").WithLocation(21, 23),
                // (25,30): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(25, 30),
                // (600,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(600, 30),
                // (700,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension(ref C2 c2)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "C2").WithLocation(700, 19),
                // (800,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(800, 30),
                // (900,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(900, 18),
                // (1000,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1000, 30),
                // (1100,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(1100, 28),
                // (1200,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1200, 30),
                // (1300,30): error CS9323: Cannot declare instance extension operator for a type that is not known to be a struct and is not known to be a class
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorExtensionWrongReceiverType, op).WithLocation(1300, 30),
                // (1400,22): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension<T>(ref T t) where T : class
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "T").WithLocation(1400, 22),
                // (1500,22): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension<T>(ref T t)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "T").WithLocation(1500, 22),
                // (1600,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t) where T : struct
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1600, 21),
                // (1700,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t) where T : class
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1700, 21),
                // (1800,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1800, 21),
                // (1900,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t) where T : struct
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1900, 31),
                // (2000,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t) where T : class
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(2000, 31),
                // (2100,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(2100, 31),
                // (2200,30): error CS9303: 'operator ++': cannot declare instance members in an extension block with an unnamed receiver parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceMemberWithUnnamedExtensionsParameter, op).WithArguments("operator " + op).WithLocation(2200, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_002_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator {{{op}}}() {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var name = CompoundAssignmentOperatorName(op);
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(ref S1)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_003_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1 s1)
    {
        public void operator {{{op}}}() {}
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

class S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);

            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var name = CompoundAssignmentOperatorName(op);
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(S1)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);

                name = op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName;
                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(S1)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_004_Declaration_2([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator checked {{{op}}}() {}
        public void operator {{{op}}}() {}
    }
}

static class Extensions2
{
    extension(ref S1 s1)
    {
#line 100
        public void operator checked {{{op}}}() {}
    }
    extension(ref S1 s1)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions3
{
    extension(ref S1 s1)
    {
        public void operator checked {{{op}}}() {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (112,38): error CS9025: The operator 'Extensions3.extension(ref S1).operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(ref S1).operator checked " + op + "()").WithLocation(112, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_004_Declaration([CombinatorialValues("++", "--")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        {{{modifier}}}
        public void operator {{{op}}}() {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (6,30): error CS0106: The modifier 'abstract' is not valid for this item
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_005_Consumption(bool fromMetadata, [CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            return new S1 { F = x.F + 1 };
        }
    }
}

public {{{typeKind}}} S1
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = 101 };
        {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s1.F);
        System.Console.Write(":");
        var s2 = {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s2.F);
        System.Console.Write(":");
        System.Console.Write(s1.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:103:103").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator " + op + "(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:103:103").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         --s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(6, 9),
                // (10,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = --s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(10, 18)
                );

            var opName = UnaryOperatorName(op);
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.{{{opName}}}(s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.{{{opName}}}();
        S1.{{{opName}}}(s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_Increment' and no accessible extension method 'op_Increment' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_Increment();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("S1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_Increment'
                //         S1.op_Increment(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("S1", opName).WithLocation(7, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_006_Consumption(bool fromMetadata, [CombinatorialValues("++", "--")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator {{{op}}}()
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            x.F++;
        }
    }
}

public struct S1
{
    public int F;
}

""" + CompilerFeatureRequiredAttribute;

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = 101 };
        {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s1.F);
        System.Console.Write(":");
        var s2 = {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s2.F);
        System.Console.Write(":");
        System.Console.Write(s1.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:103:103").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S1).operator " + op + "()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:103:103").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,9): error CS0023: Operator '++' cannot be applied to operand of type 'S1'
                //         ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "s1").WithArguments(op, "S1").WithLocation(6, 9),
                // (10,18): error CS0023: Operator '++' cannot be applied to operand of type 'S1'
                //         var s2 = ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "s1").WithArguments(op, "S1").WithLocation(10, 18)
                );

            var opName = CompoundAssignmentOperatorName(op);

            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.{{{opName}}}(ref s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.{{{opName}}}();
        S1.{{{opName}}}(ref s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_IncrementAssignment' and no accessible extension method 'op_IncrementAssignment' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_IncrementAssignment();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("S1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_IncrementAssignment'
                //         S1.op_IncrementAssignment(ref s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("S1", opName).WithLocation(7, 12)
                );

            var src5 = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator {{{op}}}()
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            x.F++;
        }
    }
}

public class C1
{
    public int F;
}

""" + CompilerFeatureRequiredAttribute;

            var src6 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1() { F = 101 };
        var c2 = c1;
        {{{op}}}c1;
        System.Console.Write(":");
        System.Console.Write(c1.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c2) ? "True" : "False");
        System.Console.Write(":");
        var c3 = {{{op}}}c1;
        System.Console.Write(":");
        System.Console.Write(c1.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c2) ? "True" : "False");
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c3) ? "True" : "False");
    }
}
""";

            var comp5 = CreateCompilation(src5);
            var comp5Ref = fromMetadata ? comp5.EmitToImageReference() : comp5.ToMetadataReference();

            var comp6 = CreateCompilation(src6, references: [comp5Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp6, expectedOutput: "operator1:101:102:True:operator1:102:103:True:True").VerifyDiagnostics();

            comp6 = CreateCompilation(src6, references: [comp5Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp6, expectedOutput: "operator1:101:102:True:operator1:102:103:True:True").VerifyDiagnostics();

            comp6 = CreateCompilation(src6, references: [comp5Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp6.VerifyEmitDiagnostics(
                // (7,9): error CS0023: Operator '--' cannot be applied to operand of type 'C1'
                //         --c1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "c1").WithArguments(op, "C1").WithLocation(7, 9),
                // (13,18): error CS0023: Operator '--' cannot be applied to operand of type 'C1'
                //         var c3 = --c1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "c1").WithArguments(op, "C1").WithLocation(13, 18)
                );
        }

        [Fact]
        public void Increment_007_Consumption_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator ++(S2 x) => throw null;
    }
}

public struct S2
{
    public static implicit operator int(S2 x)
    {
        System.Console.Write("operator2");
        return 0;
    }
    public static implicit operator S2(int x)
    {
        System.Console.Write("operator3");
        return default;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        ++s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator3").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator ++(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_008_Consumption_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S2
{
    public static implicit operator int(S2 x)
    {
        System.Console.Write("operator2");
        return 0;
    }
    public static implicit operator S2(int x)
    {
        System.Console.Write("operator3");
        return default;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator3").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator ++(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_009_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator ++(S2 x) => throw null;
    }
}

public struct S2
{
    public static S2 operator ++(S2 x)
    {
        System.Console.Write("operator2");
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator ++(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_010_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator ++(S2 x) => throw null;
    }
}

public struct S2
{
    public void operator ++()
    {
        System.Console.Write("operator2");
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_011_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S2
{
    public static S2 operator ++(S2 x)
    {
        System.Console.Write("operator2");
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator ++(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_012_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S2
{
    public void operator ++()
    {
        System.Console.Write("operator2");
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_013_Consumption_InstanceInTheSameScopeComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++()
        {
            System.Console.Write("operator2");
        }

        public static S2 operator ++(S2 y) => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S2).operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_014_Consumption_InstanceInTheSameScopeComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++()
        {
            System.Console.Write("operator2");
        }
    }
    extension(S2)
    {
        public static S2 operator ++(S2 y) => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S2).operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_015_Consumption_InstanceInTheSameScopeComesFirst()
        {
            var src = $$$"""
public static class Extensions2
{
    extension(S2)
    {
        public static S2 operator ++(S2 y) => throw null;
    }
}

public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++()
        {
            System.Console.Write("operator2");
        }
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S2).operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_016_Consumption_StaticTriedAfterInapplicableInstanceInTheSameScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator ++() => throw null;
    }
    extension(S2)
    {
        public static S2 operator ++(S2 y)
        {
            System.Console.Write("operator2");
            return y;
        }
    }
}

public struct S1
{
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_017_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator ++(S1 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator ++(S1 x)
            {
                System.Console.Write("operator1");
                return x;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator ++(S2 x) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = ++s1;
            }
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator ++(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_018_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator ++(S1 x)
            {
                System.Console.Write("operator1");
                return x;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(ref S2 x)
            {
                public void operator ++() => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = ++s1;
            }
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator ++(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_019_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(ref S1 x)
        {
            public void operator ++()
            {
                System.Console.Write("operator1");
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(ref S2 x)
            {
                public void operator ++() => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = ++s1;
            }
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(ref S1).operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_020_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator ++(S1 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(ref S1 x)
        {
            public void operator ++()
            {
                System.Console.Write("operator1");
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator ++(S2 x) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = ++s1;
            }
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(ref S1).operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_021_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static I1 operator --(I1 x) => x;
}

public interface I3
{
    public static I3 operator --(I3 x) => x;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2 x)
    {
        public void operator --() {}
        public static I2 operator --(I2 y) => y;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = --x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (33,17): error CS9339: Operator resolution is ambiguous between the following members:'I1.operator --(I1)' and 'I3.operator --(I3)'
                //         var y = --x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "--").WithArguments("I1.operator --(I1)", "I3.operator --(I3)").WithLocation(33, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator --(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator --(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_022_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public void operator --() {}
}

public interface I3
{
    public void operator --() {}
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2 x)
    {
        public void operator --() {}
        public static I2 operator --(I2 y) => y;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = --x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (33,17): error CS0121: The call is ambiguous between the following methods or properties: 'I1.operator --()' and 'I3.operator --()'
                //         var y = --x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("I1.operator --()", "I3.operator --()").WithLocation(33, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator --()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator --()", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_023_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public void operator --() {}
}

public interface I3
{
    public void operator --() {}
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
    public static I2 operator --(I2 y) => y;
}

public static class Extensions1
{
    extension(I2 x)
    {
        public void operator --() {}
        public static I2 operator --(I2 y) => y;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
#line 33
        var y = --x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (33,17): error CS0121: The call is ambiguous between the following methods or properties: 'I1.operator --()' and 'I3.operator --()'
                //         var y = --x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("I1.operator --()", "I3.operator --()").WithLocation(33, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator --()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator --()", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78830")]
        public void Increment_024_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2 y)
    {
        public void operator --() {}
        public static I2 operator --(I2 x) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static I1 operator --(I1 x) => x;
        }

        extension(I3)
        {
            public static I3 operator --(I3 x) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = --x;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (35,21): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(I1).operator --(I1)' and 'Extensions2.extension(I3).operator --(I3)'
                //             var y = --x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "--").WithArguments("NS1.Extensions2.extension(I1).operator --(I1)", "NS1.Extensions2.extension(I3).operator --(I3)").WithLocation(35, 21)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator --(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator --(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_025_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2 y)
    {
        public void operator --() {}
        public static I2 operator --(I2 x) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1 x)
        {
            public void operator --() {}
        }

        extension(I3 x)
        {
            public void operator --() {}
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = --x;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (35,21): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(I1).operator --()' and 'Extensions2.extension(I3).operator --()'
                //             var y = --x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("NS1.Extensions2.extension(I1).operator --()", "NS1.Extensions2.extension(I3).operator --()").WithLocation(35, 21)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator --()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator --()", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_026_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2 y)
    {
        public void operator --() {}
        public static I2 operator --(I2 x) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1 x)
        {
            public void operator --() {}
        }

        extension(I3 x)
        {
            public void operator --() {}
        }

        extension(I2)
        {
            public static I2 operator --(I2 x) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
#line 35
            var y = --x;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (35,21): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(I1).operator --()' and 'Extensions2.extension(I3).operator --()'
                //             var y = --x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("NS1.Extensions2.extension(I1).operator --()", "NS1.Extensions2.extension(I3).operator --()").WithLocation(35, 21)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator --()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator --()", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_027_Consumption_Lifted([CombinatorialValues("++", "--")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(ref S1 y)
    {
        public void operator {{{op}}}() => throw null;

        public static S1 operator {{{op}}}(S1 x)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            return new S1 { F = x.F + 1 };
        }
    }
}

public struct S1
{
    public int F;
}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1? s1 = new S1() { F = 101 };
        {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s1.Value.F);
        System.Console.Write(":");
        var s2 = {{{op}}}s1;
        System.Console.Write(":");
        System.Console.Write(s2.Value.F);
        System.Console.Write(":");
        System.Console.Write(s1.Value.F);
        System.Console.Write(":");
        s1 = null;
        {{{op}}}s1;
        System.Console.Write(s1?.F ?? -1);
        System.Console.Write(":");
        s2 = {{{op}}}s1;
        System.Console.Write(s2?.F ?? -1);
        System.Console.Write(":");
        System.Console.Write(s1?.F ?? -1);

        System.Console.Write(" | ");

        s1 = new S1() { F = 101 };
        s1{{{op}}};
        System.Console.Write(":");
        System.Console.Write(s1.Value.F);
        System.Console.Write(":");
        s2 = s1{{{op}}};
        System.Console.Write(":");
        System.Console.Write(s2.Value.F);
        System.Console.Write(":");
        System.Console.Write(s1.Value.F);
        System.Console.Write(":");
        s1 = null;
        s1{{{op}}};
        System.Console.Write(s1?.F ?? -1);
        System.Console.Write(":");
        s2 = s1{{{op}}};
        System.Console.Write(s2?.F ?? -1);
        System.Console.Write(":");
        System.Console.Write(s1?.F ?? -1);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "operator1:101:102:operator1:102:103:103:-1:-1:-1 | operator1:101:102:operator1:102:102:103:-1:-1:-1").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:103:103:-1:-1:-1 | operator1:101:102:operator1:102:102:103:-1:-1:-1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         --s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(6, 9),
                // (10,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = --s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(10, 18),
                // (17,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         --s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(17, 9),
                // (20,14): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s2 = --s1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "s1").WithArguments("extensions", "14.0").WithLocation(20, 14),
                // (28,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s1--;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1" + op).WithArguments("extensions", "14.0").WithLocation(28, 9),
                // (32,14): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s2 = s1--;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1" + op).WithArguments("extensions", "14.0").WithLocation(32, 14),
                // (39,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s1--;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1" + op).WithArguments("extensions", "14.0").WithLocation(39, 9),
                // (42,14): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s2 = s1--;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1" + op).WithArguments("extensions", "14.0").WithLocation(42, 14)
                );
        }

        [Fact]
        public void Increment_028_Consumption_Lifted()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = ++s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (20,13): error CS9341: Operator cannot be applied to operand of type 'S1?'. The closest inapplicable candidate is 'Extensions1.extension(ref S1).operator ++()'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1?", "Extensions1.extension(ref S1).operator ++()").WithLocation(20, 13)
                );
        }

        [Fact]
        public void Increment_029_Consumption_LiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator ++(S1 x) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator ++(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = ++s1;
        System.Console.Write(":");
        s1 = null;
        _ = ++s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_030_Consumption_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static S1? operator ++(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = ++s1;
        Extensions1.op_Increment(s1);

        S1? s2 = new S1();
        _ = ++s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (21,13): error CS0023: Operator '++' cannot be applied to operand of type 'S1'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s1").WithArguments("++", "S1").WithLocation(21, 13)
                );
        }

        [Fact]
        public void Increment_031_Consumption_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1? x)
    {
        public void operator ++()
        {
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = ++s1;
        Extensions1.op_IncrementAssignment(s1);
        Extensions1.op_IncrementAssignment(ref s1);

        S1? s2 = new S1();
        _ = ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (19,13): error CS9341: Operator cannot be applied to operand of type 'S1'. The closest inapplicable candidate is 'Extensions1.extension(ref S1?).operator ++()'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1", "Extensions1.extension(ref S1?).operator ++()").WithLocation(19, 13),
                // (20,44): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         Extensions1.op_IncrementAssignment(s1);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s1").WithArguments("1", "ref").WithLocation(20, 44),
                // (21,48): error CS1503: Argument 1: cannot convert from 'ref S1' to 'ref S1?'
                //         Extensions1.op_IncrementAssignment(ref s1);
                Diagnostic(ErrorCode.ERR_BadArgType, "s1").WithArguments("1", "ref S1", "ref S1?").WithLocation(21, 48)
                );
        }

        [Fact]
        public void Increment_032_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator ++(S2 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = ++s1;
        Extensions1.op_Increment(s1);

        S1? s2 = new S1();
        _ = ++s2;
        Extensions1.op_Increment(s2);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,13): error CS0023: Operator '++' cannot be applied to operand of type 'S1'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s1").WithArguments("++", "S1").WithLocation(22, 13),
                // (26,13): error CS0023: Operator '++' cannot be applied to operand of type 'S1?'
                //         _ = ++s2;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s2").WithArguments("++", "S1?").WithLocation(26, 13),
                // (27,34): error CS1503: Argument 1: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_Increment(s2);
                Diagnostic(ErrorCode.ERR_BadArgType, "s2").WithArguments("1", "S1?", "S2").WithLocation(27, 34)
                );
        }

        [Fact]
        public void Increment_033_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = ++s1;
        Extensions1.op_IncrementAssignment(ref s1);

        S1? s2 = new S1();
        _ = ++s2;
        Extensions1.op_IncrementAssignment(ref s2);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,13): error CS9341: Operator cannot be applied to operand of type 'S1'. The closest inapplicable candidate is 'Extensions1.extension(ref S2).operator ++()'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1", "Extensions1.extension(ref S2).operator ++()").WithLocation(22, 13),
                // (23,48): error CS1503: Argument 1: cannot convert from 'ref S1' to 'ref S2'
                //         Extensions1.op_IncrementAssignment(ref s1);
                Diagnostic(ErrorCode.ERR_BadArgType, "s1").WithArguments("1", "ref S1", "ref S2").WithLocation(23, 48),
                // (26,13): error CS9341: Operator cannot be applied to operand of type 'S1?'. The closest inapplicable candidate is 'Extensions1.extension(ref S2).operator ++()'
                //         _ = ++s2;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1?", "Extensions1.extension(ref S2).operator ++()").WithLocation(26, 13),
                // (27,48): error CS1503: Argument 1: cannot convert from 'ref S1?' to 'ref S2'
                //         Extensions1.op_IncrementAssignment(ref s2);
                Diagnostic(ErrorCode.ERR_BadArgType, "s2").WithArguments("1", "ref S1?", "ref S2").WithLocation(27, 48)
                );
        }

        [Fact]
        public void Increment_034_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static S1 operator ++(object x)
        {
            System.Console.Write("operator1");
            return default;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        s1 = ++s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_035_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object x)
    {
        public void operator ++() => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = ++s1;
        Extensions1.op_IncrementAssignment(s1);

        S1? s2 = new S1();
        _ = ++s2;
        Extensions1.op_IncrementAssignment(s2);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS9341: Operator cannot be applied to operand of type 'S1'. The closest inapplicable candidate is 'Extensions1.extension(object).operator ++()'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1", "Extensions1.extension(object).operator ++()").WithLocation(17, 13),
                // (21,13): error CS9341: Operator cannot be applied to operand of type 'S1?'. The closest inapplicable candidate is 'Extensions1.extension(object).operator ++()'
                //         _ = ++s2;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1?", "Extensions1.extension(object).operator ++()").WithLocation(21, 13)
                );
        }

        [Fact]
        public void Increment_036_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static C1 operator ++(object x)
        {
            System.Console.Write("operator1");
            return null;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1 = ++c1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_037_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1Base x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            x.F++;
        }
    }
}


public class C1Base
{
    public int F;
}

public class C1 : C1Base
{}

class Program
{
    static void Main()
    {
        var c1 = new C1() { F = 101 };
        var c2 = c1;
        ++c1;
        System.Console.Write(":");
        System.Console.Write(c1.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c2) ? "True" : "False");
        System.Console.Write(":");
        var c3 = ++c1;
        System.Console.Write(":");
        System.Console.Write(c1.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c2) ? "True" : "False");
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c3) ? "True" : "False");
        System.Console.Write(":");
        c1++;
        System.Console.Write(":");
        System.Console.Write(c1.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c2) ? "True" : "False");
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:101:102:True:operator1:102:103:True:True:operator1:103:104:True").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_038_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(dynamic x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1");
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1 = ++c1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'dynamic'
                //     extension(dynamic x)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic").WithLocation(3, 15),
                // (20,14): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         c1 = ++c1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++c1").WithArguments("++", "C1").WithLocation(20, 14)
                );
        }

        [Fact]
        public void Increment_039_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1");
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        Test(new C1());
    }

    static void Test<T>(T c1) where T : class
    {
        ++c1;
        c1 = ++c1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_040_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref System.Span<int> x)
    {
        public void operator ++() => throw null;
    }
}

class Program
{
    static void Main()
    {
        int[] a1 = null;
#line 17
        _ = ++a1;
        Extensions1.op_IncrementAssignment(ref a1);
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS9341: Operator cannot be applied to operand of type 'int[]'. The closest inapplicable candidate is 'Extensions1.extension(ref Span<int>).operator ++()'
                //         _ = ++a1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("int[]", "Extensions1.extension(ref System.Span<int>).operator ++()").WithLocation(17, 13),
                // (18,48): error CS1503: Argument 1: cannot convert from 'ref int[]' to 'ref System.Span<int>'
                //         Extensions1.op_IncrementAssignment(ref a1);
                Diagnostic(ErrorCode.ERR_BadArgType, "a1").WithArguments("1", "ref int[]", "ref System.Span<int>").WithLocation(18, 48)
                );
        }

        [Fact]
        public void Increment_041_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : struct
    {
        public static S1<T> operator ++(S1<T> x)
        {
            System.Console.Write(typeof(T).ToString());
            return x;
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        s1 = ++s1;
        Extensions1.op_Increment(s1);

        S1<int>? s2 = new S1<int>();
        _ = (++s2).GetValueOrDefault();
        s2 = null;
        System.Console.Write(":");
        _ = (++s2).GetValueOrDefault();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32System.Int32:").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_042_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(ref S1<T> x) where T : struct
    {
        public void operator ++()
        {
            System.Console.Write(typeof(T).ToString());
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        s1 = ++s1;
        Extensions1.op_IncrementAssignment(ref s1);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_043_Consumption_Generic_Worse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>)
    {
        public static S1<T> operator ++(S1<T> x)
        {
            System.Console.Write("[S1<T>]");
            return x;
        }
    }

    extension<T>(S1<T>?)
    {
        public static S1<T>? operator ++(S1<T>? x)
        {
            System.Console.Write("[S1<T>?]");
            return x;
        }
    }

    extension(S1<int>)
    {
        public static S1<int> operator ++(S1<int> x)
        {
            System.Console.Write("[S1<int>]");
            return x;
        }
    }

    extension<T>(S2<T>)
    {
        public static S2<T> operator ++(in S2<T> x) => throw null;

        public static S2<T> operator ++(S2<T> x)
        {
            System.Console.Write("[S2<T>]");
            return x;
        }
    }

    extension(S2<int>)
    {
        public static S2<int> operator ++(in S2<int> x)
        {
            System.Console.Write("[in S2<int>]");
            return x;
        }
    }
}

public struct S1<T>
{}

public struct S2<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        s11 = ++s11;
        Extensions1.op_Increment(s11);

        System.Console.WriteLine();

        var s12 = new S1<byte>();
        s12 = ++s12;
        Extensions1.op_Increment(s12);

        System.Console.WriteLine();

        var s21 = new S2<int>();
        s21 = ++s21;
        Extensions1.op_Increment(s21);

        System.Console.WriteLine();

        var s22 = new S2<byte>();
        s22 = ++s22;
        Extensions1.op_Increment(s22);

        System.Console.WriteLine();

        S1<int>? s13 = new S1<int>();
        s13 = ++s13;
        s13 = null;
        s13 = ++s13;

        System.Console.WriteLine();

        S1<byte>? s14 = new S1<byte>();
        s14 = ++s14;
        s14 = null;
        s14 = ++s14;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
[S1<int>][S1<int>]
[S1<T>][S1<T>]
[in S2<int>][in S2<int>]
[S2<T>][S2<T>]
[S1<int>]
[S1<T>?][S1<T>?]
").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_044_Consumption_Generic_Worse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(ref S1<T> x)
    {
        public void operator ++()
        {
            System.Console.Write("[S1<T>]");
        }
    }

    extension<T>(ref S1<T>? x)
    {
        public void operator ++()
        {
            System.Console.Write("[S1<T>?]");
        }
    }

    extension(ref S1<int> x)
    {
        public void operator ++()
        {
            System.Console.Write("[S1<int>]");
        }
    }

    extension(ref S1<int>? x)
    {
        public void operator ++()
        {
            System.Console.Write("[S1<int>?]");
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        s11 = ++s11;
        Extensions1.op_IncrementAssignment(ref s11);

        System.Console.WriteLine();

        var s12 = new S1<byte>();
        s12 = ++s12;
        Extensions1.op_IncrementAssignment(ref s12);

        System.Console.WriteLine();

        S1<int>? s13 = new S1<int>();
        s13 = ++s13;
        s13 = null;
        s13 = ++s13;

        System.Console.WriteLine();

        S1<byte>? s14 = new S1<byte>();
        s14 = ++s14;
        s14 = null;
        s14 = ++s14;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
[S1<int>][S1<int>]
[S1<T>][S1<T>]
[S1<int>?][S1<int>?]
[S1<T>?][S1<T>?]
").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_045_Consumption_Generic_ConstraintsViolation()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : class
    {
        public static S1<T> operator ++(S1<T> x) => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        _ = ++s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS0023: Operator '++' cannot be applied to operand of type 'S1<int>'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s1").WithArguments("++", "S1<int>").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Increment_046_Consumption_Generic_ConstraintsViolation()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(ref S1<T> x) where T : class
    {
        public void operator ++() => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        _ = ++s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS9341: Operator cannot be applied to operand of type 'S1<int>'. The closest inapplicable candidate is 'Extensions1.extension<int>(ref S1<int>).operator ++()'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableUnaryOperator, "++").WithArguments("S1<int>", "Extensions1.extension<int>(ref S1<int>).operator ++()").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Increment_047_Consumption_OverloadResolutionPriority()
        {
            var src = $$$"""
using System.Runtime.CompilerServices;

public static class Extensions1
{
    extension(C1)
    {
        [OverloadResolutionPriority(1)]
        public static C2 operator ++(C1 x)
        {
            System.Console.Write("C1");
            return (C2)x;
        }
    }
    extension(C2)
    {
        public static C2 operator ++(C2 x)
        {
            System.Console.Write("C2");
            return x;
        }
    }
    extension(C3)
    {
        public static C4 operator ++(C3 x)
        {
            System.Console.Write("C3");
            return (C4)x;
        }
    }
    extension(C4)
    {
        public static C4 operator ++(C4 x)
        {
            System.Console.Write("C4");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;

public class C3;
public class C4 : C3;

class Program
{
    static void Main()
    {
        var c2 = new C2();
        _ = ++c2;
        var c4 = new C4();
        _ = ++c4;
    }
}
""";

            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1C4").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_048_Consumption_OverloadResolutionPriority()
        {
            var src = $$$"""
using System.Runtime.CompilerServices;

public static class Extensions1
{
    extension(C1 x)
    {
        [OverloadResolutionPriority(1)]
        public void operator ++()
        {
            System.Console.Write("C1");
        }
    }
    extension(C2 x)
    {
        public void operator ++()
        {
            System.Console.Write("C2");
        }
    }
    extension(C3 x)
    {
        public void operator ++()
        {
            System.Console.Write("C3");
        }
    }
    extension(C4 x)
    {
        public void operator ++()
        {
            System.Console.Write("C4");
        }
    }
}

public class C1;
public class C2 : C1;

public class C3;
public class C4 : C3;

class Program
{
    static void Main()
    {
        var c2 = new C2();
        _ = ++c2;
        var c4 = new C4();
        _ = ++c4;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1C4").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_049_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator --(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_050_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator --()
        {
            System.Console.Write("regular");
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_051_Consumption_Checked([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
        public static C1 operator checked {{{op}}}(C1 x)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public {{{typeKind}}} C1;
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = {{{op}}}c1;

        checked
        {
            _ = {{{op}}}c1;
        }
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "regularchecked").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "regularchecked").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = --c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "c1").WithArguments("extensions", "14.0").WithLocation(6, 13),
                // (10,17): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //             _ = --c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op + "c1").WithArguments("extensions", "14.0").WithLocation(10, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_052_Consumption_Checked([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension({{{(typeKind == "struct" ? "ref " : "")}}}C1 x)
    {
        public void operator {{{op}}}()
        {
            System.Console.Write("regular");
        }
        public void operator checked {{{op}}}()
        {
            System.Console.Write("checked");
        }
    }
}

public {{{typeKind}}} C1;
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = {{{op}}}c1;

        checked
        {
            _ = {{{op}}}c1;
        }
    }
}
""";

            var comp1 = CreateCompilation([src1, src2, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "regularchecked").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "regularchecked").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS0023: Operator '--' cannot be applied to operand of type 'C1'
                //         _ = --c1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "c1").WithArguments(op, "C1").WithLocation(6, 13),
                // (10,17): error CS0023: Operator '--' cannot be applied to operand of type 'C1'
                //             _ = --c1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "c1").WithArguments(op, "C1").WithLocation(10, 17)
                );
        }

        [Fact]
        public void Increment_053_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator --(C1 x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension(C1)
    {
        public static C1 operator checked --(C1 x)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_054_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator --()
        {
            System.Console.Write("regular");
        }
    }
    extension(C1 x)
    {
        public void operator checked --()
        {
            System.Console.Write("checked");
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_055_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator --(C1 x)
        {
            return x;
        }

        public static C1 operator checked --(C1 x)
        {
            return x;
        }
    }
}

public static class Extensions2
{
    extension(C1)
    {
        public static C1 operator --(C1 x)
        {
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
#if DEBUG
            comp.VerifyEmitDiagnostics(
                // (35,13): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(C1).operator --(C1)' and 'Extensions1.extension(C1).operator --(C1)'
                //         _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "--").WithArguments("Extensions2.extension(C1).operator --(C1)", "Extensions1.extension(C1).operator --(C1)").WithLocation(35, 13),
                // (39,17): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked --(C1)' and 'Extensions2.extension(C1).operator --(C1)'
                //             _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "--").WithArguments("Extensions1.extension(C1).operator checked --(C1)", "Extensions2.extension(C1).operator --(C1)").WithLocation(39, 17)
                );
#else
            comp.VerifyEmitDiagnostics(
                // (35,13): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator --(C1)' and 'Extensions2.extension(C1).operator --(C1)'
                //         _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "--").WithArguments("Extensions1.extension(C1).operator --(C1)", "Extensions2.extension(C1).operator --(C1)").WithLocation(35, 13),
                // (39,17): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked --(C1)' and 'Extensions2.extension(C1).operator --(C1)'
                //             _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "--").WithArguments("Extensions1.extension(C1).operator checked --(C1)", "Extensions2.extension(C1).operator --(C1)").WithLocation(39, 17)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().Last();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("Extensions1.extension(C1).operator checked --(C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(C1).operator --(C1)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78968")]
        public void Increment_056_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator --()
        {
        }

        public void operator checked --()
        {
        }
    }
}

public static class Extensions2
{
    extension(C1 x)
    {
        public void operator --()
        {
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

#if DEBUG // Collection of extension blocks depends on GetTypeMembersUnordered for namespace, which conditionally de-orders types for DEBUG only.
            comp.VerifyEmitDiagnostics(
                // (32,13): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(C1).operator --()' and 'Extensions1.extension(C1).operator --()'
                //         _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("Extensions2.extension(C1).operator --()", "Extensions1.extension(C1).operator --()").WithLocation(32, 13),
                // (36,17): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(C1).operator checked --()' and 'Extensions2.extension(C1).operator --()'
                //             _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("Extensions1.extension(C1).operator checked --()", "Extensions2.extension(C1).operator --()").WithLocation(36, 17)
                );
#else
            // Ordering difference is acceptable and doesn't affect determinism. It is caused by ConditionallyDeOrder
            comp.VerifyEmitDiagnostics(
                // (32,13): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(C1).operator --()' and 'Extensions2.extension(C1).operator --()'
                //         _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("Extensions1.extension(C1).operator --()", "Extensions2.extension(C1).operator --()").WithLocation(32, 13),
                // (36,17): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(C1).operator checked --()' and 'Extensions2.extension(C1).operator --()'
                //             _ = --c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "--").WithArguments("Extensions1.extension(C1).operator checked --()", "Extensions2.extension(C1).operator --()").WithLocation(36, 17)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().Last();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("Extensions1.extension(C1).operator checked --()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(C1).operator --()", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        public void Increment_057_Consumption_CheckedLiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator --(S1 x) => throw null;
        public static S1 operator checked --(S1 x) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator --(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = --s1;
        System.Console.Write(":");

        checked
        {
            _ = --s1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_058_Consumption_CheckedNoLiftedForm()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator --() => throw null;
        public void operator checked --() => throw null;
    }
    extension(ref S1? x)
    {
        public void operator --()
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = --s1;
        System.Console.Write(":");

        checked
        {
            _ = --s1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_059_Consumption_OverloadResolutionPlusRegularVsChecked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C3 operator --(C1 x)
        {
            System.Console.Write("C1");
            return (C3)x;
        }
        public static C3 operator checked --(C1 x)
        {
            System.Console.Write("checkedC1");
            return (C3)x;
        }
    }
    extension(C2)
    {
        public static C2 operator --(C2 x)
        {
            System.Console.Write("C2");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;
public class C3 : C1;

class Program
{
    static void Main()
    {
        var c3 = new C3();
        _ = --c3;

        checked
        {
            _ = --c3;
        }

        var c2 = new C2();
        _ = --c2;

        checked
        {
            _ = --c2;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1checkedC1C2C2").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_060_Consumption_OverloadResolutionPlusRegularVsChecked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator --()
        {
            System.Console.Write("C1");
        }
        public void operator checked --()
        {
            System.Console.Write("checkedC1");
        }
    }
    extension(C2 x)
    {
        public void operator --()
        {
            System.Console.Write("C2");
        }
    }
}

public class C1;
public class C2 : C1;
public class C3 : C1;

class Program
{
    static void Main()
    {
        var c3 = new C3();
        _ = --c3;

        checked
        {
            _ = --c3;
        }

        var c2 = new C2();
        _ = --c2;

        checked
        {
            _ = --c2;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1checkedC1C2C2").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_061_Consumption()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1? operator ++(S1? x)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static void M1(S1? x) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
#line 21
        _ = ++s1;
        S1 s2 = new S1();
        _ = ++s2;

        System.Nullable<S1>.M1(s1);
        S1.M1(s1);
        S1.M1(s2);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (5,36): error CS9318: The parameter type for ++ or -- operator must be the extended type.
                //         public static S1? operator ++(S1? x)
                Diagnostic(ErrorCode.ERR_BadExtensionIncDecSignature, "++").WithLocation(5, 36),
                // (21,13): error CS0023: Operator '++' cannot be applied to operand of type 'S1?'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s1").WithArguments("++", "S1?").WithLocation(21, 13),
                // (23,13): error CS0266: Cannot implicitly convert type 'S1?' to 'S1'. An explicit conversion exists (are you missing a cast?)
                //         _ = ++s2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "++s2").WithArguments("S1?", "S1").WithLocation(23, 13),
                // (25,9): error CS1929: 'S1?' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1).M1(S1?)' requires a receiver of type 'S1'
                //         System.Nullable<S1>.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "System.Nullable<S1>").WithArguments("S1?", "M1", "Extensions1.extension(S1).M1(S1?)", "S1").WithLocation(25, 9)
                );
        }

        [Fact]
        public void Increment_062_Consumption_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator ++(object x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = ++s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_063_Consumption_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1");
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = ++s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_064_Consumption_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object y)
    {
        public static object operator ++(object x)
        {
            System.Console.Write("operator1");
            return x;
        }
        public void operator ++()
        {
            System.Console.Write("operator2");
        }
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        try
        {
            _ = ++s1;
        }
        catch
        {
            System.Console.Write("exception");
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "exception").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_065_Consumption_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object y)
    {
        public static object operator ++(object x)
        {
            System.Console.Write("operator1");
            return x;
        }
        public void operator ++()
        {
            System.Console.Write("operator2");
        }
    }
}

class Program
{
    static void Main()
    {
#line 19
        _ = ++null;
        _ = ++default;
        _ = ++new();
        _ = ++(() => 1);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (19,15): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         _ = ++null;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "null").WithLocation(19, 15),
                // (20,15): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         _ = ++default;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "default").WithLocation(20, 15),
                // (21,15): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         _ = ++new();
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "new()").WithLocation(21, 15),
                // (22,16): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         _ = ++(() => 1);
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "() => 1").WithLocation(22, 16)
                );
        }

        [Fact]
        public void Increment_066_Consumption_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object y)
    {
        public void operator ++()
        {
            System.Console.Write("operator2");
        }
    }
}

class Program
{
    static object P {get; set;}

    static void Main()
    {
        _ = ++P;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (18,13): error CS0023: Operator '++' cannot be applied to operand of type 'object'
                //         _ = ++P;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++P").WithArguments("++", "object").WithLocation(18, 13)
                );
        }

        [Fact]
        public void Increment_067_Consumption_BadReceiver()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(__arglist)
    {
        public static object operator ++(object x)
        {
            return x;
        }
        public void operator ++()
        {
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
#line 17
        _ = ++s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9318: The parameter type for ++ or -- operator must be the extended type.
                //         public static object operator ++(object x)
                Diagnostic(ErrorCode.ERR_BadExtensionIncDecSignature, "++").WithLocation(5, 39),
                // (17,13): error CS0023: Operator '++' cannot be applied to operand of type 'object'
                //         _ = ++s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s1").WithArguments("++", "object").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Increment_068_Consumption_Checked_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T1>(C1<T1>)
    {
        public static C1<T1> operator --(C1<T1> x)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension<T2>(C1<T2>)
    {
        public static C1<T2> operator checked --(C1<T2> x)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1<T>;

class Program
{
    static void Main()
    {
        var c1 = new C1<int>();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_069_Consumption_Checked_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T1>(C1<T1> x)
    {
        public void operator --()
        {
            System.Console.Write("regular");
        }
    }
    extension<T2>(C1<T2> x)
    {
        public void operator checked --()
        {
            System.Console.Write("checked");
        }
    }
}

public class C1<T>;

class Program
{
    static void Main()
    {
        var c1 = new C1<int>();
        _ = --c1;

        checked
        {
            _ = --c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_070_Consumption_Postfix(bool fromMetadata)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator ++(S1 x)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            return new S1 { F = x.F + 1 };
        }
    }
}

public struct S1
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = 101 };
        s1++;
        System.Console.Write(":");
        System.Console.Write(s1.F);
        System.Console.Write(":");
        var s2 = s1++;
        System.Console.Write(":");
        System.Console.Write(s2.F);
        System.Console.Write(":");
        System.Console.Write(s1.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:102:103").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PostfixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator ++(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102:operator1:102:102:103").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s1++;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1++").WithArguments("extensions", "14.0").WithLocation(6, 9),
                // (10,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = s1++;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1++").WithArguments("extensions", "14.0").WithLocation(10, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_071_Consumption_Postfix(bool fromMetadata)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            x.F++;
        }
    }
}

public struct S1
{
    public int F;
}

""" + CompilerFeatureRequiredAttribute;

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = 101 };
        s1++;
        System.Console.Write(":");
        System.Console.Write(s1.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PostfixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S1).operator ++()", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:102").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,9): error CS0023: Operator '++' cannot be applied to operand of type 'S1'
                //         s1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "s1++").WithArguments("++", "S1").WithLocation(6, 9)
                );

            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        _ = s1++;
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            comp3.VerifyDiagnostics(
                // (6,13): error CS0023: Operator '++' cannot be applied to operand of type 'S1'
                //         _ = s1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "s1++").WithArguments("++", "S1").WithLocation(6, 13)
                );

            var src4 = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator ++()
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            x.F++;
        }
    }
}

public class C1
{
    public int F;
}

""" + CompilerFeatureRequiredAttribute;

            var src5 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1() { F = 101 };
        var c2 = c1;
        c1++;
        System.Console.Write(":");
        System.Console.Write(c1.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c1, c2) ? "True" : "False");
    }
}
""";

            var comp4 = CreateCompilation(src4);
            var comp4Ref = fromMetadata ? comp4.EmitToImageReference() : comp4.ToMetadataReference();

            var comp5 = CreateCompilation(src5, references: [comp4Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp5, expectedOutput: "operator1:101:102:True").VerifyDiagnostics();

            var src6 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1++;
    }
}
""";
            var comp6 = CreateCompilation(src6, references: [comp4Ref], options: TestOptions.DebugExe);
            comp6.VerifyDiagnostics(
                // (6,13): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         _ = c1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "c1++").WithArguments("++", "C1").WithLocation(6, 13)
                );
        }

        [Fact]
        public void Increment_072_Consumption_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator ++(S1 x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => ++s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,54): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<System.Func<S1, S1>> ex = (s1) => ++s1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "++s1").WithLocation(22, 54)
                );
        }

        [Fact]
        public void Increment_073_Consumption_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator --()
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => --s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (21,54): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<System.Func<S1, S1>> ex = (s1) => --s1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "--s1").WithLocation(21, 54)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left
        /// </summary>
        [Fact]
        public void Increment_074_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(scoped C left) => throw null;
    public void M(scoped C c1)
    {
#line 7
        ++c1;
#line 9
        c1 = X(c1);
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator ++(scoped C left) => throw null;
    }
}
""";

            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left
        /// </summary>
        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78964")]
        public void Increment_075_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(scoped C left) => throw null;
    public C M(C c, scoped C c1)
    {
#line 7
        c = ++c1;
#line 9
        c = X(c1);
        return c;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator ++(scoped C left) => throw null;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left
        /// </summary>
        [Fact]
        public void Increment_076_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M(C c, scoped C c1)
    {
#line 7
        c = c1++;
        return c;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator ++(scoped C left) => throw null;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (7,13): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = c1++;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1++").WithArguments("scoped C c1").WithLocation(7, 13)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_01
        /// </summary>
        [Fact]
        public void Increment_077_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M(scoped C c, C c1)
    {
        c = ++c1;
        return c1;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator ++(C right) => right;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_01
        /// </summary>
        [Fact]
        public void Increment_078_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M(scoped C c, C c1)
    {
        c = c1++;
        return c1;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator ++(C right) => right;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left
        /// </summary>
        [Fact]
        public void Increment_079_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X() => throw null;
    public void M(scoped C c1)
    {
#line 7
        ++c1;
#line 9
        c1.X();
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator ++() => throw null;
    }
}
""";

            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left
        /// </summary>
        [Fact]
        public void Increment_080_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X() => throw null;
    public C M(C c, scoped C c1)
    {
#line 7
        c = ++c1;
#line 9
        c1.X();
        c = c1;
        return c;
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator ++() => throw null;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (7,13): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = ++c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "++c1").WithArguments("scoped C c1").WithLocation(7, 13),
                // (10,13): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(10, 13)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_01
        /// </summary>
        [Fact]
        public void Increment_081_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M(scoped C c, C c1)
    {
        c = ++c1;
        return c1;
    }
}

static class Extensions
{
    extension(ref C right)
    {
        public void operator ++() {}
    }
}

""" + CompilerFeatureRequiredAttribute;

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void Increment_082_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator --(C1 x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        C1? x = null;
        _ = --x;
        C1 y = new C1();
        y = --y;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,15): warning CS8604: Possible null reference argument for parameter 'x' in 'C1 Extensions1.extension(C1).operator --(C1 x)'.
                //         _ = --x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "C1 Extensions1.extension(C1).operator --(C1 x)").WithLocation(23, 15)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/79011")]
        public void Increment_083_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1? operator --(C1 x)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        C1? x = new C1();
        C1 y = --x;
        C1 z = new C1();
        --z;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // This warning is unexpected - https://github.com/dotnet/roslyn/issues/79011
                // (23,16): warning CS8601: Possible null reference assignment.
                //         C1 y = --x;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--x").WithLocation(23, 16),

                // (23,16): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         C1 y = --x;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "--x").WithLocation(23, 16),
                // (25,9): warning CS8601: Possible null reference assignment.
                //         --z;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "--z").WithLocation(25, 9)
                );
        }

        [Fact]
        public void Increment_084_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator --(T x)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (--x).ToString();
        var y = new C2();
        (--y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (22,10): warning CS8602: Dereference of a possibly null reference.
                //         (--x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "--x").WithLocation(22, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator --(C2?)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator --(C2)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Increment_085_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T>) where T : new()
    {
        public static C1<T> operator --(C1<T> x)
        {
            return x;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (--x).F.ToString();
        var y = Get(new C2());
        (--y).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,9): warning CS8602: Dereference of a possibly null reference.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(--x).F").WithLocation(27, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator --(C1<C2?>)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C1<C2>).operator --(C1<C2>)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Increment_086_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public class C1<T> where T : new()
{
    public T F = new T();

    public static C1<T> operator --(C1<T> x)
    {
        return x;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
#line 27
        (--x).F.ToString();
        var y = Get(new C2());
        (--y).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,9): warning CS8602: Dereference of a possibly null reference.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(--x).F").WithLocation(27, 9)
                );
        }

        [Fact]
        public void Increment_087_NullableAnalysis_Lifted()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(S1<T>) where T : new()
    {
        public static S1<T> operator --(S1<T> x)
        {
            return x;
        }
    }
}

public struct S1<T> where T : new()
{
    public T F;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = --Get((C2?)null);

        if (x != null)
            x.Value.F.ToString();

        var y = --Get(new C2());

        if (y != null)
            y.Value.F.ToString();
    }

    static ref S1<T>? Get<T>(T x) where T : new()
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,13): warning CS8602: Dereference of a possibly null reference.
                //             x.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x.Value.F").WithLocation(29, 13)
                );
        }

        [Fact]
        public void Increment_088_NullableAnalysis_Lifted()
        {
            var src = $$$"""
#nullable enable

public struct S1<T> where T : new()
{
    public T F;

    public static S1<T> operator --(S1<T> x)
    {
        return x;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = --Get((C2?)null);

        if (x != null)
#line 29
            x.Value.F.ToString();

        var y = --Get(new C2());

        if (y != null)
            y.Value.F.ToString();
    }

    static ref S1<T>? Get<T>(T x) where T : new()
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,13): warning CS8602: Dereference of a possibly null reference.
                //             x.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x.Value.F").WithLocation(29, 13)
                );
        }

        [Fact]
        public void Increment_089_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T) where T : notnull
    {
        public static T operator --(T x)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (--x).ToString();
        var y = new C2();
        (--y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (22,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (--x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "--x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(22, 10),
                // (22,10): warning CS8602: Dereference of a possibly null reference.
                //         (--x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "--x").WithLocation(22, 10)
                );
        }

        [Fact]
        public void Increment_090_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T>) where T : notnull, new()
    {
        public static C1<T> operator --(C1<T> x)
        {
            return x;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (--x).F.ToString();
        var y = Get(new C2());
        (--y).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,9): warning CS8602: Dereference of a possibly null reference.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(--x).F").WithLocation(27, 9),
                // (27,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "--x").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(27, 10)
                );
        }

        [Fact]
        public void Increment_091_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1 x)
    {
        public void operator --() {}
    }

    extension(C2? x)
    {
        public void operator --() {}
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x = null;
        var x1 = --x;
        x.ToString();
        x1.ToString();

        C1 y = new C1();
        var y1 = --y;
        y.ToString();
        y1.ToString();

        C2? z = null;
        var z1 = --z;
        z.ToString();
        z1.ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,20): warning CS8604: Possible null reference argument for parameter 'x' in 'Extensions1.extension(C1)'.
                //         var x1 = --x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "Extensions1.extension(C1)").WithLocation(27, 20),
                // (38,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(38, 9),
                // (39,9): warning CS8602: Dereference of a possibly null reference.
                //         z1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z1").WithLocation(39, 9)
                );
        }

        [Fact]
        public void Increment_092_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

using System.Diagnostics.CodeAnalysis;

public static class Extensions1
{
    extension([NotNull] ref S1? x)
    {
        public void operator --() { throw null!; }
    }

    extension([NotNull] C2? x)
    {
        public void operator --() { throw null!; }
    }
}

public struct S1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        S1? x = null;
        var x1 = --x;
        _ = x.Value;
        _ = x1.Value;

        C2? z = null;
        var z1 = --z;
        z.ToString();
        z1.ToString();
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Increment_093_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(ref S1? x)
    {
        public void operator --() {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? x = null;
        var x1 = --x;
        _ = x.Value;
        _ = x1.Value;

        S1? y = new S1();
        var y1 = --y;
        _ = y.Value;
        _ = y1.Value;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (20,13): warning CS8629: Nullable value type may be null.
                //         _ = x.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "x").WithLocation(20, 13),
                // (21,13): warning CS8629: Nullable value type may be null.
                //         _ = x1.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "x1").WithLocation(21, 13),
                // (25,13): warning CS8629: Nullable value type may be null.
                //         _ = y.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "y").WithLocation(25, 13),
                // (26,13): warning CS8629: Nullable value type may be null.
                //         _ = y1.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "y1").WithLocation(26, 13)
                );
        }

        [Fact]
        public void Increment_094_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(ref S1? x)
    {
        public void operator --() {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        _ = Get(new S1()).Value;
        var y1 = --Get(new S1());
#line 26
        _ = y1.Value;
    }

    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("x")]
    static ref S1? Get(S1? x) => throw null!;
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (26,13): warning CS8629: Nullable value type may be null.
                //         _ = y1.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "y1").WithLocation(26, 13)
                );
        }

        [Fact]
        public void Increment_095_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1Base x)
    {
        public void operator --() {}
    }

    extension(C2Base? x)
    {
        public void operator --() {}
    }
}

public class C1Base {}
public class C1 : C1Base {}

public class C2Base {}
public class C2 : C2Base {}

class Program
{
    static void Main()
    {
        C1? x = null;
        var x1 = --x;
        x.ToString();
        x1.ToString();

        C1 y = new C1();
        var y1 = --y;
        y.ToString();
        y1.ToString();

        C2? z = null;
        var z1 = --z;
        z.ToString();
        z1.ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,20): warning CS8604: Possible null reference argument for parameter 'x' in 'Extensions1.extension(C1Base)'.
                //         var x1 = --x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "Extensions1.extension(C1Base)").WithLocation(27, 20),
                // (38,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(38, 9),
                // (39,9): warning CS8602: Dereference of a possibly null reference.
                //         z1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z1").WithLocation(39, 9)
                );
        }

        [Fact]
        public void Increment_096_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C2Base<string> x)
    {
        public void operator --() {}
    }
}

public class C2Base<T> {}
public class C2<T> : C2Base<T> {}

class Program
{
    static void Main()
    {
        C2<string?> z = new C2<string?>();
        var z1 = --z;
        z.ToString();
        z1.ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (19,20): warning CS8620: Argument of type 'C2<string?>' cannot be used for parameter 'x' of type 'C2Base<string>' in 'Extensions1.extension(C2Base<string>)' due to differences in the nullability of reference types.
                //         var z1 = --z;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("C2<string?>", "C2Base<string>", "x", "Extensions1.extension(C2Base<string>)").WithLocation(19, 20)
                );
        }

        [Fact]
        public void Increment_097_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

using System.Diagnostics.CodeAnalysis;

public static class Extensions1
{
    extension([NotNull] C2Base? x)
    {
        public void operator --() { throw null!; }
    }
}

public class C2Base
{}

public class C2 : C2Base
{}

class Program
{
    static void Main()
    {
        C2? z = null;
        var z1 = --z;
        z.ToString();
        z1.ToString();
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Increment_098_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T x) where T : class
    {
        public void operator --() {}
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (--x).ToString();
        var y = new C2();
        (--y).ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (19,10): warning CS8634: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'class' constraint.
                //         (--x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "--x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(19, 10),
                // (19,10): warning CS8602: Dereference of a possibly null reference.
                //         (--x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "--x").WithLocation(19, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator --()", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator --()", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Increment_099_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T> x) where T : notnull, new()
    {
        public void operator --() {}
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (--x).F.ToString();
        var y = Get(new C2());
        (--y).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,9): warning CS8602: Dereference of a possibly null reference.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(--x).F").WithLocation(24, 9),
                // (24,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "--x").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(24, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator --()", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C1<C2>).operator --()", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Increment_100_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T x) where T : class?
    {
        public void operator --() {}
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (--x).ToString();
        var y = new C2();
        (--y).ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (19,10): warning CS8602: Dereference of a possibly null reference.
                //         (--x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "--x").WithLocation(19, 10)
                );
        }

        [Fact]
        public void Increment_101_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T> x) where T : new()
    {
        public void operator --() {}
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (--x).F.ToString();
        var y = Get(new C2());
        (--y).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,9): warning CS8602: Dereference of a possibly null reference.
                //         (--x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(--x).F").WithLocation(24, 9)
                );
        }

        [Fact]
        public void Increment_102_Declaration_Extern()
        {
            var src = $$$"""
using System.Runtime.InteropServices;

public static class Extensions1
{
    extension(C2 x)
    {
        extern public void operator --() {}

        [DllImport("something.dll")]
        public void operator ++() {}
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,37): error CS0179: 'Extensions1.extension(C2).operator --()' cannot be extern and declare a body
                //         extern public void operator --() {}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "--").WithArguments("Extensions1.extension(C2).operator --()").WithLocation(7, 37),
                // (9,10): error CS0601: The DllImport attribute must be specified on a method marked 'extern' that is either 'static' or an extension member
                //         [DllImport("something.dll")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(9, 10)
                );
        }

        [Fact]
        public void Increment_103_Declaration_Extern()
        {
            var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(C2 x)
    {
        [DllImport("something.dll")]
        extern public void operator --();
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A5B9DA57687B6EBB6576FC573B145969'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 x
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20b5
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A5B9DA57687B6EBB6576FC573B145969'::'<Extension>$'
        } // end of class <M>$A5B9DA57687B6EBB6576FC573B145969
        // Methods
        .method public hidebysig specialname 
            instance void op_DecrementAssignment () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                01 00 26 55 73 65 72 44 65 66 69 6e 65 64 43 6f
                6d 70 6f 75 6e 64 41 73 73 69 67 6e 6d 65 6e 74
                4f 70 65 72 61 74 6f 72 73 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 35 42 39 44 41 35 37 36
                38 37 42 36 45 42 42 36 35 37 36 46 43 35 37 33
                42 31 34 35 39 36 39 00 00
            )
            // Method begins at RVA 0x20ae
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_DecrementAssignment
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static pinvokeimpl("something.dll" winapi) 
        void op_DecrementAssignment (
            class C2 x
        ) cil managed preservesig 
    {
    } // end of method E::op_DecrementAssignment
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void Increment_104_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2 x)
    {
        extern public void operator --();
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var verifier = CompileAndVerify(source, verify: Verification.FailsPEVerify with { PEVerifyMessage = """
                Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                Type load failed.
                """ });

            verifier.VerifyDiagnostics(
                // (5,37): warning CS0626: Method, operator, or accessor 'E.extension(C2).operator --()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern public void operator --();
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "--").WithArguments("E.extension(C2).operator --()").WithLocation(5, 37)
                );

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A5B9DA57687B6EBB6576FC573B145969'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 x
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20b5
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A5B9DA57687B6EBB6576FC573B145969'::'<Extension>$'
        } // end of class <M>$A5B9DA57687B6EBB6576FC573B145969
        // Methods
        .method public hidebysig specialname 
            instance void op_DecrementAssignment () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                01 00 26 55 73 65 72 44 65 66 69 6e 65 64 43 6f
                6d 70 6f 75 6e 64 41 73 73 69 67 6e 6d 65 6e 74
                4f 70 65 72 61 74 6f 72 73 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 35 42 39 44 41 35 37 36
                38 37 42 36 45 42 42 36 35 37 36 46 43 35 37 33
                42 31 34 35 39 36 39 00 00
            )
            // Method begins at RVA 0x20ae
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_DecrementAssignment
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        void op_DecrementAssignment (
            class C2 x
        ) cil managed 
    {
    } // end of method E::op_DecrementAssignment
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void Increment_105_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2 x)
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        extern public void operator --();
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A5B9DA57687B6EBB6576FC573B145969'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 x
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20b5
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A5B9DA57687B6EBB6576FC573B145969'::'<Extension>$'
        } // end of class <M>$A5B9DA57687B6EBB6576FC573B145969
        // Methods
        .method public hidebysig specialname 
            instance void op_DecrementAssignment () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                01 00 26 55 73 65 72 44 65 66 69 6e 65 64 43 6f
                6d 70 6f 75 6e 64 41 73 73 69 67 6e 6d 65 6e 74
                4f 70 65 72 61 74 6f 72 73 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 35 42 39 44 41 35 37 36
                38 37 42 36 45 42 42 36 35 37 36 46 43 35 37 33
                42 31 34 35 39 36 39 00 00
            )
            // Method begins at RVA 0x20ae
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_DecrementAssignment
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        void op_DecrementAssignment (
            class C2 x
        ) cil managed internalcall 
    {
    } // end of method E::op_DecrementAssignment
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Theory]
        [CombinatorialData]
        public void Increment_106_Consumption_CRef([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{op}}}"/>
/// <see cref="E.extension(S1).operator {{{op}}}(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator {{{op}}}(S1 x) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = UnaryOperatorName(op);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator " + op + ", S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))",
                "(E.extension(S1).operator " + op + "(S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Increment_107_Consumption_CRef([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(ref S1).operator {{{op}}}()"/>
public static class E
{
    ///
    extension(ref S1 x)
    {
        ///
        public void operator {{{op}}}() => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = CompoundAssignmentOperatorName(op);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal(["(E.extension(ref S1).operator " + op + "(), void E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "())"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Increment_108_Consumption_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator checked {{{op}}}"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator {{{op}}}(S1 x) => throw null;
        ///
        public static S1 operator checked {{{op}}}(S1 x) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = UnaryOperatorName(op, isChecked: true);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator checked " + op + ", S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))",
                "(E.extension(S1).operator checked " + op + "(S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Increment_109_Consumption_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(ref S1).operator checked {{{op}}}()"/>
public static class E
{
    ///
    extension(ref S1 x)
    {
        ///
        public void operator {{{op}}}() => throw null;
        ///
        public void operator checked {{{op}}}() => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = CompoundAssignmentOperatorName(op, isChecked: true);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal(["(E.extension(ref S1).operator checked " + op + "(), void E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "())"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Increment_110_Consumption_CRef_Error([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{op}}}"/>
/// <see cref="E.extension(S1).operator {{{op}}}()"/>
/// <see cref="E.extension(S1).operator {{{op}}}(S1)"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}()"/>
/// <see cref="E.extension(S1).operator checked {{{op}}}(S1)"/>
public static class E
{
    ///
    extension(S1)
    {
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics(
                // (1,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator ++' that could not be resolved
                // /// <see cref="E.extension(S1).operator ++"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + op).WithArguments("extension(S1).operator " + op).WithLocation(1, 16),
                // (2,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator ++()' that could not be resolved
                // /// <see cref="E.extension(S1).operator ++()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + op + "()").WithArguments("extension(S1).operator " + op + "()").WithLocation(2, 16),
                // (3,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator ++(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator ++(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + op + "(S1)").WithArguments("extension(S1).operator " + op + "(S1)").WithLocation(3, 16),
                // (4,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked ++' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked ++"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + op).WithArguments("extension(S1).operator checked " + op).WithLocation(4, 16),
                // (5,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked ++()' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked ++()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + op + "()").WithArguments("extension(S1).operator checked " + op + "()").WithLocation(5, 16),
                // (6,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked ++(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked ++(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + op + "(S1)").WithArguments("extension(S1).operator checked " + op + "(S1)").WithLocation(6, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_111_ERR_VoidError([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(void* x) => x;
    }
}

class Program
{
    unsafe void* Test(void* x) => {{{op}}}x;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (11,35): error CS0242: The operation in question is undefined on void pointers
                //     unsafe void* Test(void* x) => ++x;
                Diagnostic(ErrorCode.ERR_VoidError, op + "x").WithLocation(11, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_112_ERR_VoidError([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(ref void* x)
    {
        public void operator {{{op}}}() {}
    }
}

class Program
{
    unsafe void* Test(void* x) => {{{op}}}x;
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,19): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(ref void* x)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 19),
                // (11,35): error CS0242: The operation in question is undefined on void pointers
                //     unsafe void* Test(void* x) => ++x;
                Diagnostic(ErrorCode.ERR_VoidError, op + "x").WithLocation(11, 35)
                );
        }

        [Fact]
        public void Increment_113_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator ++() => throw null;
    }
}

public static class Extensions2
{
    extension(ref S2 x)
    {
        public void operator ++() => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

#if DEBUG // Collection of extension blocks depends on GetTypeMembersUnordered for namespace, which conditionally de-orders types for DEBUG only.
            comp.VerifyDiagnostics(
                // (26,9): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(ref S2).operator ++()' and 'Extensions1.extension(ref S2).operator ++()'
                //         ++s2;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("Extensions2.extension(ref S2).operator ++()", "Extensions1.extension(ref S2).operator ++()").WithLocation(26, 9)
                );
#else
            comp.VerifyDiagnostics(
                // (26,9): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(ref S2).operator ++()' and 'Extensions2.extension(ref S2).operator ++()'
                //         ++s2;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("Extensions1.extension(ref S2).operator ++()", "Extensions2.extension(ref S2).operator ++()").WithLocation(26, 9)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);

#if DEBUG // Collection of extension blocks depends on GetTypeMembersUnordered for namespace, which conditionally de-orders types for DEBUG only.
            AssertEx.Equal("Extensions2.extension(ref S2).operator ++()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions1.extension(ref S2).operator ++()", symbolInfo.CandidateSymbols[1].ToDisplayString());
#else
            AssertEx.Equal("Extensions1.extension(ref S2).operator ++()", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(ref S2).operator ++()", symbolInfo.CandidateSymbols[1].ToDisplayString());
#endif
        }

        [Fact]
        public void Increment_114_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator ++(S2 x) => throw null;
    }
}

public static class Extensions2
{
    extension(S2)
    {
        public static S2 operator ++(S2 x) => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        ++s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
#if DEBUG
            comp.VerifyDiagnostics(
                // (26,9): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(S2).operator ++(S2)' and 'Extensions1.extension(S2).operator ++(S2)'
                //         ++s2;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "++").WithArguments("Extensions2.extension(S2).operator ++(S2)", "Extensions1.extension(S2).operator ++(S2)").WithLocation(26, 9)
                );
#else
            comp.VerifyDiagnostics(
                // (26,9): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(S2).operator ++(S2)' and 'Extensions2.extension(S2).operator ++(S2)'
                //         ++s2;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "++").WithArguments("Extensions1.extension(S2).operator ++(S2)", "Extensions2.extension(S2).operator ++(S2)").WithLocation(26, 9)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);

#if DEBUG // Collection of extension blocks depends on GetTypeMembersUnordered for namespace, which conditionally de-orders types for DEBUG only.
            AssertEx.Equal("Extensions2.extension(S2).operator ++(S2)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions1.extension(S2).operator ++(S2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
#else
            AssertEx.Equal("Extensions1.extension(S2).operator ++(S2)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(S2).operator ++(S2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
#endif
        }

        [Theory]
        [CombinatorialData]
        public void Binary_001_Declaration([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S2 operator {{{op}}}(S1 x, S2 y) => default;
        public static S2? operator {{{op}}}(S1? x, S2 y) => default;
        public static S2 operator {{{op}}}(S2 y, S1 x) => default;
        public static S2? operator {{{op}}}(S2 y, S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S2 x, S2 y) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
        public static void operator {{{op}}}(S1 x, S2 y) {}
    }
}

static class Extensions4
{
    extension(S1)
    {
        static S1 operator {{{op}}}(S1 x, S1 y) => default;
    }

    extension(S2)
    {
        public S2 operator {{{op}}}(S2 x, S2 y) => default;
    }

    extension(C1)
    {
        public static S1 operator {{{op}}}(C1 x, S1 y) => default;
    }
}

static class Extensions5
{
    extension(S1?)
    {
        public static S2 operator {{{op}}}(S1 x, S1 y) => default;
        public static S2 operator {{{op}}}(S1? x, S2 y) => default;
        public static S2 operator {{{op}}}(S2 y, S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator +(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, op).WithLocation(6, 36),
                // (8,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator +(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, op).WithLocation(8, 36),
                // (16,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S1 operator -(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, op).WithLocation(16, 35),
                // (24,37): error CS0590: User-defined operators cannot return void
                //         public static void operator +(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, op).WithLocation(24, 37),
                // (32,28): error CS0558: User-defined operator 'Extensions4.extension(S1).operator +(S1, S1)' must be declared static and public
                //         static S1 operator +(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions4.extension(S1).operator " + op + "(S1, S1)").WithLocation(32, 28),
                // (37,28): error CS0558: User-defined operator 'Extensions4.extension(S2).operator +(S2, S2)' must be declared static and public
                //         public S2 operator +(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions4.extension(S2).operator " + op + "(S2, S2)").WithLocation(37, 28),
                // (42,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator +(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(42, 35),
                // (42,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator +(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(42, 37),
                // (50,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator +(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, op).WithLocation(50, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_002_Declaration([CombinatorialValues("<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S2 operator {{{op}}}(S1 x, S2 y) => default;
        public static S2? operator {{{op}}}(S1? x, S2 y) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S2 x, S1 y) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
        public static void operator {{{op}}}(S1 x, S2 y) {}
    }
}

static class Extensions4
{
    extension(S1)
    {
        static S1 operator {{{op}}}(S1 x, S1 y) => default;
    }
    extension(S2)
    {
        public S2 operator {{{op}}}(S2 x, S2 y) => default;
    }
    extension(C1)
    {
        public static S1 operator {{{op}}}(C1 x, S1 y) => default;
    }
}

static class Extensions5
{
    extension(S1?)
    {
        public static S2 operator {{{op}}}(S1 x, S1 y) => default;
        public static S2 operator {{{op}}}(S1? x, S2 y) => default;
        public static S2 operator {{{op}}}(S2 y, S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,36): error CS9320: The first operand of an overloaded shift operator must have the same type as the extended type
                //         public static S2? operator <<(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionShiftOperatorSignature, op).WithLocation(6, 36),
                // (14,35): error CS9320: The first operand of an overloaded shift operator must have the same type as the extended type
                //         public static S1 operator <<(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionShiftOperatorSignature, op).WithLocation(14, 35),
                // (22,37): error CS0590: User-defined operators cannot return void
                //         public static void operator <<(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, op).WithLocation(22, 37),
                // (30,28): error CS0558: User-defined operator 'Extensions4.extension(S1).operator <<(S1, S1)' must be declared static and public
                //         static S1 operator <<(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions4.extension(S1).operator " + op + "(S1, S1)").WithLocation(30, 28),
                // (34,28): error CS0558: User-defined operator 'Extensions4.extension(S2).operator <<(S2, S2)' must be declared static and public
                //         public S2 operator <<(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions4.extension(S2).operator " + op + "(S2, S2)").WithLocation(34, 28),
                // (38,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator >>>(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(38, 35),
                // (38,39): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator >>>(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(38, 39 - (op == ">>>" ? 0 : 1)),
                // (46,35): error CS9320: The first operand of an overloaded shift operator must have the same type as the extended type
                //         public static S2 operator <<(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionShiftOperatorSignature, op).WithLocation(46, 35),
                // (48,35): error CS9320: The first operand of an overloaded shift operator must have the same type as the extended type
                //         public static S2 operator <<(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionShiftOperatorSignature, op).WithLocation(48, 35)
                );
        }

        [Fact]
        public void Binary_003_Declaration()
        {
            var src = """
static class Extensions1
{
    extension(S1)
    {
#line 100
        public static S2 operator !=(S1 x, S2 y) => default;
        public static S2 operator ==(S1 x, S2 y) => default;
        public static S2? operator !=(S1? x, S2 y) => default;
        public static S2? operator ==(S1? x, S2 y) => default;
        public static S2 operator !=(S2 y, S1 x) => default;
        public static S2 operator ==(S2 y, S1 x) => default;
        public static S2? operator !=(S2 y, S1? x) => default;
        public static S2? operator ==(S2 y, S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 200
        public static bool operator !=(S1 x, S2 y) => default;
    }

    extension(S1)
    {
#line 300
        public static bool operator ==(S1 x, S2 y) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
#line 400
        public static bool operator !=(S1 x, S2 y) => default;
    }

    extension(S2)
    {
        public static bool operator ==(S2 x, S1 y) => default;
    }
}

static class Extensions4
{
    extension(S1)
    {
#line 500
        public static bool operator !=(S2 x, S2 y) => default;
        public static bool operator ==(S2 x, S2 y) => default;
    }
}

static class Extensions5
{
    extension(S1)
    {
#line 600
        public static void operator !=(S1 x, S2 y) {}
        public static void operator ==(S1 x, S2 y) {}
    }
}

static class Extensions6
{
    extension(S1)
    {
#line 700
        static S1 operator !=(S1 x, S1 y) => default;
        public S1 operator ==(S1 x, S1 y) => default;
    }
    extension(C1)
    {
#line 800
        public static S1 operator !=(C1 x, S1 y) => default;
        public static S1 operator ==(C1 x, S1 y) => default;
    }
}

static class Extensions7
{
    extension(S1?)
    {
#line 900
        public static S2 operator !=(S1 x, S1 y) => default;
        public static S2 operator !=(S1? x, S2 y) => default;
        public static S2 operator !=(S2 y, S1? x) => default;

        public static S2 operator ==(S1 x, S1 y) => default;
        public static S2 operator ==(S1? x, S2 y) => default;
        public static S2 operator ==(S2 y, S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (102,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator !=(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "!=").WithLocation(102, 36),
                // (103,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator ==(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "==").WithLocation(103, 36),
                // (106,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator !=(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "!=").WithLocation(106, 36),
                // (107,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator ==(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "==").WithLocation(107, 36),
                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator !=(S1, S2)' requires a matching operator '==' to also be defined
                //         public static bool operator !=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "!=").WithArguments("Extensions3.extension(S1).operator !=(S1, S2)", "==").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator ==(S2, S1)' requires a matching operator '!=' to also be defined
                //         public static bool operator ==(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "==").WithArguments("Extensions3.extension(S2).operator ==(S2, S1)", "!=").WithLocation(405, 37),
                // (500,37): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator !=(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "!=").WithLocation(500, 37),
                // (501,37): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator ==(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "==").WithLocation(501, 37),
                // (600,37): error CS0590: User-defined operators cannot return void
                //         public static void operator !=(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "!=").WithLocation(600, 37),
                // (601,37): error CS0590: User-defined operators cannot return void
                //         public static void operator ==(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "==").WithLocation(601, 37),
                // (700,28): error CS0558: User-defined operator 'Extensions6.extension(S1).operator !=(S1, S1)' must be declared static and public
                //         static S1 operator !=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "!=").WithArguments("Extensions6.extension(S1).operator !=(S1, S1)").WithLocation(700, 28),
                // (701,28): error CS0558: User-defined operator 'Extensions6.extension(S1).operator ==(S1, S1)' must be declared static and public
                //         public S1 operator ==(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "==").WithArguments("Extensions6.extension(S1).operator ==(S1, S1)").WithLocation(701, 28),
                // (800,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator !=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "!=").WithLocation(800, 35),
                // (800,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator !=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 38),
                // (801,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator ==(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "==").WithLocation(801, 35),
                // (801,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator ==(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 38),
                // (900,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator !=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "!=").WithLocation(900, 35),
                // (904,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator ==(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "==").WithLocation(904, 35)
                );
        }

        [Fact]
        public void Binary_004_Declaration()
        {
            var src = """
static class Extensions1
{
    extension(S1)
    {
#line 100
        public static S2 operator >=(S1 x, S2 y) => default;
        public static S2 operator <=(S1 x, S2 y) => default;
        public static S2? operator >=(S1? x, S2 y) => default;
        public static S2? operator <=(S1? x, S2 y) => default;
        public static S2 operator >=(S2 y, S1 x) => default;
        public static S2 operator <=(S2 y, S1 x) => default;
        public static S2? operator >=(S2 y, S1? x) => default;
        public static S2? operator <=(S2 y, S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 200
        public static bool operator >=(S1 x, S2 y) => default;
    }

    extension(S1)
    {
#line 300
        public static bool operator <=(S1 x, S2 y) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
#line 400
        public static bool operator >=(S1 x, S2 y) => default;
    }

    extension(S2)
    {
        public static bool operator <=(S2 x, S1 y) => default;
    }
}

static class Extensions4
{
    extension(S1)
    {
#line 500
        public static bool operator >=(S2 x, S2 y) => default;
        public static bool operator <=(S2 x, S2 y) => default;
    }
}

static class Extensions5
{
    extension(S1)
    {
#line 600
        public static void operator >=(S1 x, S2 y) {}
        public static void operator <=(S1 x, S2 y) {}
    }
}

static class Extensions6
{
    extension(S1)
    {
#line 700
        static S1 operator >=(S1 x, S1 y) => default;
        public S1 operator <=(S1 x, S1 y) => default;
    }
    extension(C1)
    {
#line 800
        public static S1 operator >=(C1 x, S1 y) => default;
        public static S1 operator <=(C1 x, S1 y) => default;
    }
}

static class Extensions7
{
    extension(S1?)
    {
#line 900
        public static S2 operator >=(S1 x, S1 y) => default;
        public static S2 operator >=(S1? x, S2 y) => default;
        public static S2 operator >=(S2 y, S1? x) => default;

        public static S2 operator <=(S1 x, S1 y) => default;
        public static S2 operator <=(S1? x, S2 y) => default;
        public static S2 operator <=(S2 y, S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (102,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator >=(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">=").WithLocation(102, 36),
                // (103,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator <=(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<=").WithLocation(103, 36),
                // (106,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator >=(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">=").WithLocation(106, 36),
                // (107,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator <=(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<=").WithLocation(107, 36),
                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator >=(S1, S2)' requires a matching operator '<=' to also be defined
                //         public static bool operator >=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">=").WithArguments("Extensions3.extension(S1).operator >=(S1, S2)", "<=").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator <=(S2, S1)' requires a matching operator '>=' to also be defined
                //         public static bool operator <=(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<=").WithArguments("Extensions3.extension(S2).operator <=(S2, S1)", ">=").WithLocation(405, 37),
                // (500,37): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator >=(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">=").WithLocation(500, 37),
                // (501,37): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator <=(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<=").WithLocation(501, 37),
                // (600,37): error CS0590: User-defined operators cannot return void
                //         public static void operator >=(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, ">=").WithLocation(600, 37),
                // (601,37): error CS0590: User-defined operators cannot return void
                //         public static void operator <=(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "<=").WithLocation(601, 37),
                // (700,28): error CS0558: User-defined operator 'Extensions6.extension(S1).operator >=(S1, S1)' must be declared static and public
                //         static S1 operator >=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, ">=").WithArguments("Extensions6.extension(S1).operator >=(S1, S1)").WithLocation(700, 28),
                // (701,28): error CS0558: User-defined operator 'Extensions6.extension(S1).operator <=(S1, S1)' must be declared static and public
                //         public S1 operator <=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "<=").WithArguments("Extensions6.extension(S1).operator <=(S1, S1)").WithLocation(701, 28),
                // (800,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator >=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, ">=").WithLocation(800, 35),
                // (800,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator >=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 38),
                // (801,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator <=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "<=").WithLocation(801, 35),
                // (801,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator <=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 38),
                // (900,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator >=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">=").WithLocation(900, 35),
                // (904,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator <=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<=").WithLocation(904, 35)
                );
        }

        [Fact]
        public void Binary_005_Declaration()
        {
            var src = """
static class Extensions1
{
    extension(S1)
    {
#line 100
        public static S2 operator >(S1 x, S2 y) => default;
        public static S2 operator <(S1 x, S2 y) => default;
        public static S2? operator >(S1? x, S2 y) => default;
        public static S2? operator <(S1? x, S2 y) => default;
        public static S2 operator >(S2 y, S1 x) => default;
        public static S2 operator <(S2 y, S1 x) => default;
        public static S2? operator >(S2 y, S1? x) => default;
        public static S2? operator <(S2 y, S1? x) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 200
        public static bool operator >(S1 x, S2 y) => default;
    }

    extension(S1)
    {
#line 300
        public static bool operator <(S1 x, S2 y) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
#line 400
        public static bool operator >(S1 x, S2 y) => default;
    }

    extension(S2)
    {
        public static bool operator <(S2 x, S1 y) => default;
    }
}

static class Extensions4
{
    extension(S1)
    {
#line 500
        public static bool operator >(S2 x, S2 y) => default;
        public static bool operator <(S2 x, S2 y) => default;
    }
}

static class Extensions5
{
    extension(S1)
    {
#line 600
        public static void operator >(S1 x, S2 y) {}
        public static void operator <(S1 x, S2 y) {}
    }
}

static class Extensions6
{
    extension(S1)
    {
#line 700
        static S1 operator >(S1 x, S1 y) => default;
        public S1 operator <(S1 x, S1 y) => default;
    }
    extension(C1)
    {
#line 800
        public static S1 operator >(C1 x, S1 y) => default;
        public static S1 operator <(C1 x, S1 y) => default;
    }
}

static class Extensions7
{
    extension(S1?)
    {
#line 900
        public static S2 operator >(S1 x, S1 y) => default;
        public static S2 operator >(S1? x, S2 y) => default;
        public static S2 operator >(S2 y, S1? x) => default;

        public static S2 operator <(S1 x, S1 y) => default;
        public static S2 operator <(S1? x, S2 y) => default;
        public static S2 operator <(S2 y, S1? x) => default;
    }
}

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (102,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator >(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">").WithLocation(102, 36),
                // (103,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator <(S1? x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<").WithLocation(103, 36),
                // (106,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator >(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">").WithLocation(106, 36),
                // (107,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator <(S2 y, S1? x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<").WithLocation(107, 36),
                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator >(S1, S2)' requires a matching operator '<' to also be defined
                //         public static bool operator >(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">").WithArguments("Extensions3.extension(S1).operator >(S1, S2)", "<").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator <(S2, S1)' requires a matching operator '>' to also be defined
                //         public static bool operator <(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<").WithArguments("Extensions3.extension(S2).operator <(S2, S1)", ">").WithLocation(405, 37),
                // (500,37): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator >(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">").WithLocation(500, 37),
                // (501,37): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator <(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<").WithLocation(501, 37),
                // (600,37): error CS0590: User-defined operators cannot return void
                //         public static void operator >(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, ">").WithLocation(600, 37),
                // (601,37): error CS0590: User-defined operators cannot return void
                //         public static void operator <(S1 x, S2 y) {}
                Diagnostic(ErrorCode.ERR_OperatorCantReturnVoid, "<").WithLocation(601, 37),
                // (700,28): error CS0558: User-defined operator 'Extensions6.extension(S1).operator >(S1, S1)' must be declared static and public
                //         static S1 operator >(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, ">").WithArguments("Extensions6.extension(S1).operator >(S1, S1)").WithLocation(700, 28),
                // (701,28): error CS0558: User-defined operator 'Extensions6.extension(S1).operator <(S1, S1)' must be declared static and public
                //         public S1 operator <(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "<").WithArguments("Extensions6.extension(S1).operator <(S1, S1)").WithLocation(701, 28),
                // (800,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator >(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, ">").WithLocation(800, 35),
                // (800,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator >(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 37),
                // (801,35): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator <(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "<").WithLocation(801, 35),
                // (801,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator <(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 37),
                // (900,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator >(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">").WithLocation(900, 35),
                // (904,35): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator <(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "<").WithLocation(904, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_006_Declaration([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y) => default;
    }
}

public struct S1
{}
""";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var name = BinaryOperatorName(op);
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(S1, S1)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        private static string BinaryOperatorName(string op, bool isChecked = false)
        {
            var kind = op switch
            {
                ">>" => SyntaxKind.GreaterThanGreaterThanToken,
                ">>>" => SyntaxKind.GreaterThanGreaterThanGreaterThanToken,
                _ => SyntaxFactory.ParseToken(op).Kind(),
            };
            return OperatorFacts.BinaryOperatorNameFromSyntaxKind(kind, isChecked);
        }

        [Fact]
        public void Binary_007_Declaration()
        {
            var src = """
static class Extensions1
{
    extension(S1)
    {
        public static bool operator !=(S1 x, S1 y) => default;
        public static bool operator ==(S1 x, S1 y) => default;
        public static bool operator >=(S1 x, S1 y) => default;
        public static bool operator <=(S1 x, S1 y) => default;
        public static bool operator >(S1 x, S1 y) => default;
        public static bool operator <(S1 x, S1 y) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.EqualityOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.EqualityOperatorName + "(S1, S1)", method.ToDisplayString());
                verifyMethod(method);

                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.InequalityOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.InequalityOperatorName + "(S1, S1)", method.ToDisplayString());
                verifyMethod(method);

                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.GreaterThanOrEqualOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.GreaterThanOrEqualOperatorName + "(S1, S1)", method.ToDisplayString());
                verifyMethod(method);

                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.LessThanOrEqualOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.LessThanOrEqualOperatorName + "(S1, S1)", method.ToDisplayString());
                verifyMethod(method);

                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.GreaterThanOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.GreaterThanOperatorName + "(S1, S1)", method.ToDisplayString());
                verifyMethod(method);

                method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + WellKnownMemberNames.LessThanOperatorName);
                AssertEx.Equal("Extensions1." + WellKnownMemberNames.LessThanOperatorName + "(S1, S1)", method.ToDisplayString());
                verifyMethod(method);
            }

            static void verifyMethod(MethodSymbol method)
            {
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Binary_008_Declaration([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        {{{modifier}}}
        public static S1 operator {{{op}}}(S1 x, S1 y) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,35): error CS0106: The modifier 'abstract' is not valid for this item
                //         public static S1 operator -(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_009_Declaration([CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        {{{modifier}}}
        public static bool operator !=(S1 x, S1 y) => default;

        {{{modifier}}}
        public static bool operator ==(S1 x, S1 y) => default;

        {{{modifier}}}
        public static bool operator >=(S1 x, S1 y) => default;

        {{{modifier}}}
        public static bool operator <=(S1 x, S1 y) => default;

        {{{modifier}}}
        public static bool operator >(S1 x, S1 y) => default;

        {{{modifier}}}
        public static bool operator <(S1 x, S1 y) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                    // (6,37): error CS0106: The modifier 'abstract' is not valid for this item
                    //         public static bool operator !=(S1 x, S1 y) => default;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "!=").WithArguments(modifier).WithLocation(6, 37),
                    // (9,37): error CS0106: The modifier 'abstract' is not valid for this item
                    //         public static bool operator ==(S1 x, S1 y) => default;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "==").WithArguments(modifier).WithLocation(9, 37),
                    // (12,37): error CS0106: The modifier 'abstract' is not valid for this item
                    //         public static bool operator >=(S1 x, S1 y) => default;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, ">=").WithArguments(modifier).WithLocation(12, 37),
                    // (15,37): error CS0106: The modifier 'abstract' is not valid for this item
                    //         public static bool operator <=(S1 x, S1 y) => default;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "<=").WithArguments(modifier).WithLocation(15, 37),
                    // (18,37): error CS0106: The modifier 'abstract' is not valid for this item
                    //         public static bool operator >(S1 x, S1 y) => default;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, ">").WithArguments(modifier).WithLocation(18, 37),
                    // (21,37): error CS0106: The modifier 'abstract' is not valid for this item
                    //         public static bool operator <(S1 x, S1 y) => default;
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "<").WithArguments(modifier).WithLocation(21, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_010_Declaration([CombinatorialValues("+", "-", "*", "/")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public static S1 operator checked {{{op}}}(S1 x, S1 y) => default;
        public static S1 operator {{{op}}}(S1 x, S1 y) => default;
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 100
        public static S1 operator checked {{{op}}}(S1 x, S1 y) => default;
    }
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y) => default;
    }
}

static class Extensions3
{
    extension(S1)
    {
        public static S1 operator checked {{{op}}}(S1 x, S1 y) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (112,43): error CS9025: The operator 'Extensions3.extension(S1).operator checked +(S1, S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked +(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "(S1, S1)").WithLocation(112, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_011_Consumption(bool fromMetadata, [CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>", ">", "<", ">=", "<=", "==", "!=")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            if (op is ("==" or "!=") && typeKind is "class")
            {
                return;
            }

            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
        public static S1 operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, S1 y) => throw null;
""";
            }

            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F + y.F };
        }
{{{pairedOp}}}
    }
}

public {{{typeKind}}} S1
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s11 = new S1() { F = 101 };
        var s12 = new S1() { F = 202 };
        var s2 = s11 {{{op}}} s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator " + op + "(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (7,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = s11 != s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(7, 18)
                );

            var opName = BinaryOperatorName(op);
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.{{{opName}}}(s1, s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.{{{opName}}}(s1);
        S1.{{{opName}}}(s1, s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_Addition' and no accessible extension method 'op_Addition' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_Addition(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("S1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_Addition'
                //         S1.op_Addition(s1, s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("S1", opName).WithLocation(7, 12)
                );
        }

        [Fact]
        public void Binary_012_Consumption_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S2
{
    public static implicit operator int(S2 x)
    {
        System.Console.Write("operator2");
        return 0;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        int x = s2 + s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("int.operator +(int, int)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Int32", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_013_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, S2 y)
    {
        System.Console.Write("operator2");
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 + s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator +(S2, S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_014_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator +(S1 x, S1 y)
            {
                System.Console.Write("operator1");
                return x;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator +(S2 x, S2 y) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = s1 + s1;
            }
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator +(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_015_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static I1 operator -(I1 x, I1 y) => x;
}

public interface I3
{
    public static I3 operator -(I3 x, I3 y) => x;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = x - x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (32,19): error CS9339: Operator resolution is ambiguous between the following members:'I1.operator -(I1, I1)' and 'I3.operator -(I3, I3)'
                //         var y = x - x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("I1.operator -(I1, I1)", "I3.operator -(I3, I3)").WithLocation(32, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator -(I1, I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator -(I3, I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78830")]
        public void Binary_016_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static I1 operator -(I1 x, I1 y) => x;
        }

        extension(I3)
        {
            public static I3 operator -(I3 x, I3 y) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = x - x;
        }
    }
}
""";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (34,23): error CS9339: Operator resolution is ambiguous between the following members:'Extensions2.extension(I1).operator -(I1, I1)' and 'Extensions2.extension(I3).operator -(I3, I3)'
                //             var y = x - x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("NS1.Extensions2.extension(I1).operator -(I1, I1)", "NS1.Extensions2.extension(I3).operator -(I3, I3)").WithLocation(34, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator -(I1, I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator -(I3, I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Theory]
        [CombinatorialData]
        public void Binary_017_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1? s12 = new S1();
        _ = s11 {{{op}}} s12;
        System.Console.Write(":");
        s11 = null;
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
        _ = s11 {{{op}}} s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_018_Consumption_Lifted_01([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1 s12 = new S1();
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
        System.Console.Write(":");
        s11 = null;
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "operator1operator1:").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1operator1:").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (7,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 - s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(7, 13),
                // (8,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s12 - s11;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s12 " + op + " s11").WithArguments("extensions", "14.0").WithLocation(8, 13),
                // (11,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 - s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(11, 13),
                // (12,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s12 - s11;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s12 " + op + " s11").WithArguments("extensions", "14.0").WithLocation(12, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_018_Consumption_Lifted_02([CombinatorialValues(">", "<", ">=", "<=", "==", "!=")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return true;
        }

        public static bool operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, S1 y) => throw null;
    }
}

public struct S1
{}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1 s12 = new S1();
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
        System.Console.Write(":");
        s11 = null;
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "operator1operator1:").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1operator1:").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (7,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 != s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(7, 13),
                // (8,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s12 != s11;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s12 " + op + " s11").WithArguments("extensions", "14.0").WithLocation(8, 13),
                // (11,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 != s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(11, 13),
                // (12,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s12 != s11;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s12 " + op + " s11").WithArguments("extensions", "14.0").WithLocation(12, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_019_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static S1 operator {{{op}}}(S2 x, S1 y)
        {
            System.Console.Write("operator2");
            return y;
        }
    }
}

public struct S1
{}

public struct S2
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        S2? s2 = new S2();
        _ = s1 {{{op}}} s2;
        _ = s2 {{{op}}} s1;
        System.Console.Write(":");
        s1 = null;
        _ = s1 {{{op}}} s2;
        _ = s2 {{{op}}} s1;
        s1 = new S1();
        s2 = null;
        _ = s1 {{{op}}} s2;
        _ = s2 {{{op}}} s1;
        s1 = null;
        _ = s1 {{{op}}} s2;
        _ = s2 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator2:").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_020_Consumption_Lifted_Shift([CombinatorialValues("<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

public struct S2
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        S2? s2 = new S2();
        _ = s1 {{{op}}} s2;
        System.Console.Write(":");
        s1 = null;
        _ = s1 {{{op}}} s2;
        s1 = new S1();
        s2 = null;
        _ = s1 {{{op}}} s2;
        s1 = null;
        _ = s1 {{{op}}} s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_021_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S2 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F * 1000 + y.F };
        }
        public static S1 operator {{{op}}}(S2 x, S1 y)
        {
            System.Console.Write("operator2:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F * 1000 + y.F };
        }
    }
}

public struct S1
{
    public int F;
}

public struct S2
{
    public int F;
}

class Program
{
    static void Main()
    {
        S1? s11 = new S1() { F = 101 };
        S2 s12 = new S2() { F = 202 };
        S1 s21 = new S1() { F = 303 };
        S2? s22 = new S2() { F = 404 };
        Print(s11 {{{op}}} s12);
        System.Console.WriteLine();
        Print(s12 {{{op}}} s11);
        System.Console.WriteLine();
        Print(s21 {{{op}}} s22);
        System.Console.WriteLine();
        Print(s22 {{{op}}} s21);
        System.Console.WriteLine();
        s11 = null;
        s22 = null;
        Print(s11 {{{op}}} s12);
        System.Console.WriteLine();
        Print(s12 {{{op}}} s11);
        System.Console.WriteLine();
        Print(s21 {{{op}}} s22);
        System.Console.WriteLine();
        Print(s22 {{{op}}} s21);
        System.Console.WriteLine();
        Print(s11 {{{op}}} s22);
        System.Console.WriteLine();
        Print(s22 {{{op}}} s11);
    }

    static void Print(S1? x)
    {
        System.Console.Write(":");
        System.Console.Write(x?.F ?? -1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput:
@"
operator1:101:202:101202
operator2:202:101:202101
operator1:303:404:303404
operator2:404:303:404303
:-1
:-1
:-1
:-1
:-1
:-1
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_022_Consumption_Lifted_Shift([CombinatorialValues("<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

public struct S2
{}

class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S2 s12 = new S2();
        S1 s21 = new S1();
        S2? s22 = new S2();
        _ = s11 {{{op}}} s12;
        _ = s21 {{{op}}} s22;
        System.Console.Write(":");
        s11 = null;
        s22 = null;
        _ = s11 {{{op}}} s12;
        _ = s21 {{{op}}} s22;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1:").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_023_Consumption_LiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, S1 y) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator +(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 + s1;
        System.Console.Write(":");
        s1 = null;
        _ = s1 + s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_024_Consumption_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static S1? operator +(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 + s1;
        Extensions1.op_Addition(s1, s1);

        S1? s2 = new S1();
        _ = s2 + s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (21,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(21, 13)
                );
        }

        [Fact]
        public void Binary_025_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 + s2;
        _ = s2 + s1;
        _ = s1 + s1;
        Extensions1.op_Addition(s1, s1);

        S1? s3 = new S1();
        _ = s3 + s3;
        Extensions1.op_Addition(s3, s3);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (25,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(25, 13),
                // (29,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1?' and 'S1?'
                //         _ = s3 + s3;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s3 + s3").WithArguments("+", "S1?", "S1?").WithLocation(29, 13),
                // (30,33): error CS1503: Argument 1: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_Addition(s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("1", "S1?", "S2").WithLocation(30, 33),
                // (30,37): error CS1503: Argument 2: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_Addition(s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("2", "S1?", "S2").WithLocation(30, 37)
                );

            var src1 = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
    public static implicit operator S1(S2 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 + s1;
        _ = s1 + s2;
        _ = s2 + s1;
        _ = s2 + s2;
        Extensions1.op_Addition(s1, s1);
        Extensions1.op_Addition(s1, s2);
    }
}
""";

            var comp1 = CreateCompilation(src1, options: TestOptions.DebugExe);
            comp1.VerifyEmitDiagnostics(
                // (24,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(24, 13),
                // (25,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S2'
                //         _ = s1 + s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s2").WithArguments("+", "S1", "S2").WithLocation(25, 13)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S1 x, S2 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
    public static implicit operator S1(S2 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 + s1;
        _ = s1 + s2;
        _ = s2 + s1;
        _ = s2 + s2;
        Extensions1.op_Addition(s1, s1);
        Extensions1.op_Addition(s2, s1);
    }
}
""";

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            comp2.VerifyEmitDiagnostics(
                // (24,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(24, 13),
                // (26,13): error CS0019: Operator '+' cannot be applied to operands of type 'S2' and 'S1'
                //         _ = s2 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 + s1").WithArguments("+", "S2", "S1").WithLocation(26, 13)
                );

            var src3 = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2? operator +(S2? x, S1? y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
    public static implicit operator S1(S2 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 + s1;
        _ = s1 + s2;
        _ = s2 + s1;
        _ = s2 + s2;
        Extensions1.op_Addition(s1, s1);
        Extensions1.op_Addition(s1, s2);
    }
}
""";

            var comp3 = CreateCompilation(src3, options: TestOptions.DebugExe);
            comp3.VerifyEmitDiagnostics(
                // (5,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator +(S2? x, S1? y) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "+").WithLocation(5, 36),
                // (24,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(24, 13),
                // (25,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S2'
                //         _ = s1 + s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s2").WithArguments("+", "S1", "S2").WithLocation(25, 13),
                // (26,13): error CS0019: Operator '+' cannot be applied to operands of type 'S2' and 'S1'
                //         _ = s2 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 + s1").WithArguments("+", "S2", "S1").WithLocation(26, 13),
                // (27,13): error CS0019: Operator '+' cannot be applied to operands of type 'S2' and 'S2'
                //         _ = s2 + s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 + s2").WithArguments("+", "S2", "S2").WithLocation(27, 13)
                );

            var src4 = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2? operator +(S1? x, S2? y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
    public static implicit operator S1(S2 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 + s1;
        _ = s1 + s2;
        _ = s2 + s1;
        _ = s2 + s2;
        Extensions1.op_Addition(s1, s1);
        Extensions1.op_Addition(s2, s1);
    }
}
""";

            var comp4 = CreateCompilation(src4, options: TestOptions.DebugExe);
            comp4.VerifyEmitDiagnostics(
                // (5,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S2? operator +(S1? x, S2? y) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "+").WithLocation(5, 36),
                // (24,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(24, 13),
                // (25,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S2'
                //         _ = s1 + s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s2").WithArguments("+", "S1", "S2").WithLocation(25, 13),
                // (26,13): error CS0019: Operator '+' cannot be applied to operands of type 'S2' and 'S1'
                //         _ = s2 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 + s1").WithArguments("+", "S2", "S1").WithLocation(26, 13),
                // (27,13): error CS0019: Operator '+' cannot be applied to operands of type 'S2' and 'S2'
                //         _ = s2 + s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 + s2").WithArguments("+", "S2", "S2").WithLocation(27, 13)
                );
        }

        [Fact]
        public void Binary_026_Consumption_ReceiverTypeMismatch_Shift()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator <<(S2 x, S2 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 << s2;
        _ = s2 << s1;
        _ = s1 << s1;
        Extensions1.op_LeftShift(s1, s1);

        S1? s3 = new S1();
        _ = s3 << s3;
        Extensions1.op_LeftShift(s3, s3);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (23,13): error CS0019: Operator '<<' cannot be applied to operands of type 'S1' and 'S2'
                //         _ = s1 << s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 << s2").WithArguments("<<", "S1", "S2").WithLocation(23, 13),
                // (25,13): error CS0019: Operator '<<' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 << s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 << s1").WithArguments("<<", "S1", "S1").WithLocation(25, 13),
                // (29,13): error CS0019: Operator '<<' cannot be applied to operands of type 'S1?' and 'S1?'
                //         _ = s3 << s3;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s3 << s3").WithArguments("<<", "S1?", "S1?").WithLocation(29, 13),
                // (30,34): error CS1503: Argument 1: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_LeftShift(s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("1", "S1?", "S2").WithLocation(30, 34),
                // (30,38): error CS1503: Argument 2: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_LeftShift(s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("2", "S1?", "S2").WithLocation(30, 38)
                );
        }

        [Fact]
        public void Binary_027_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : struct
    {
        public static S1<T> operator +(S1<T> x, S1<T> y)
        {
            System.Console.Write(typeof(T).ToString());
            return x;
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        s1 = s1 + s1;
        Extensions1.op_Addition(s1, s1);

        S1<int>? s2 = new S1<int>();
        _ = (s2 + s2).GetValueOrDefault();
        s2 = null;
        System.Console.Write(":");
        _ = (s2 + s2).GetValueOrDefault();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32System.Int32:").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_027_Consumption_Generic_Worse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>)
    {
        public static S1<T> operator +(S1<T> x, S1<T> y)
        {
            System.Console.Write("[S1<T>]");
            return x;
        }
    }

    extension<T>(S1<T>?)
    {
        public static S1<T>? operator +(S1<T>? x, S1<T>? y)
        {
            System.Console.Write("[S1<T>?]");
            return x;
        }
    }

    extension(S1<int>)
    {
        public static S1<int> operator +(S1<int> x, S1<int> y)
        {
            System.Console.Write("[S1<int>]");
            return x;
        }
    }

    extension<T>(S2<T>)
    {
        public static S2<T> operator +(in S2<T> x, S2<T> y) => throw null;

        public static S2<T> operator +(S2<T> x, S2<T> y)
        {
            System.Console.Write("[S2<T>]");
            return x;
        }
    }

    extension(S2<int>)
    {
        public static S2<int> operator +(in S2<int> x, S2<int> y)
        {
            System.Console.Write("[in S2<int>]");
            return x;
        }
    }
}

public struct S1<T>
{}

public struct S2<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        s11 = s11 + s11;
        Extensions1.op_Addition(s11, s11);

        System.Console.WriteLine();

        var s12 = new S1<byte>();
        s12 = s12 + s12;
        Extensions1.op_Addition(s12, s12);

        System.Console.WriteLine();

        var s21 = new S2<int>();
        s21 = s21 + s21;
        Extensions1.op_Addition(s21, s21);

        System.Console.WriteLine();

        var s22 = new S2<byte>();
        s22 = s22 + s22;
        Extensions1.op_Addition(s22, s22);

        System.Console.WriteLine();

        S1<int>? s13 = new S1<int>();
        s13 = s13 + s13;
        s13 = null;
        s13 = s13 + s13;

        System.Console.WriteLine();

        S1<byte>? s14 = new S1<byte>();
        s14 = s14 + s14;
        s14 = null;
        s14 = s14 + s14;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
[S1<int>][S1<int>]
[S1<T>][S1<T>]
[in S2<int>][in S2<int>]
[S2<T>][S2<T>]
[S1<int>]
[S1<T>?][S1<T>?]
").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_028_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : struct
    {
        public static bool operator >(S1<T> x, S1<T> y)
        {
            System.Console.Write(typeof(T).ToString());
            return true;
        }
        public static bool operator <(S1<T> x, S1<T> y) => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        var s12 = new S1<int>();
        bool b = s11 > s12;
        Extensions1.op_GreaterThan(s11, s12);

        S1<int>? s21 = new S1<int>();
        S1<int>? s22 = new S1<int>();
        b = s21 > s22;
        s22 = null;
        System.Console.Write(":");
        b = s21 > s22;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32System.Int32:").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_029_Consumption_Generic_ConstraintsViolation()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : class
    {
        public static S1<T> operator +(S1<T> x, S1<T> y) => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        _ = s1 + s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1<int>' and 'S1<int>'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1<int>", "S1<int>").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Binary_030_Consumption_OverloadResolutionPriority()
        {
            var src = $$$"""
using System.Runtime.CompilerServices;

public static class Extensions1
{
    extension(C1)
    {
        [OverloadResolutionPriority(1)]
        public static C1 operator +(C1 x, C1 y)
        {
            System.Console.Write("C1");
            return x;
        }
    }
    extension(C2)
    {
        public static C2 operator +(C2 x, C2 y)
        {
            System.Console.Write("C2");
            return x;
        }
    }
    extension(C3)
    {
        public static C3 operator +(C3 x, C3 y)
        {
            System.Console.Write("C3");
            return x;
        }
    }
    extension(C4)
    {
        public static C4 operator +(C4 x, C4 y)
        {
            System.Console.Write("C4");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;

public class C3;
public class C4 : C3;

class Program
{
    static void Main()
    {
        var c2 = new C2();
        _ = c2 + c2;
        var c4 = new C4();
        _ = c4 + c4;
    }
}
""";

            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1C4").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_031_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 - c1;

        checked
        {
            _ = c1 - c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_031_Consumption_Checked_CheckedFormNotSupported()
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator |(C1 x, C1 y) => throw null;
        public static C1 operator checked |(C1 x, C1 y) => throw null;
    }
}

public class C1;
""";

            var comp1 = CreateCompilation(src1);
            comp1.VerifyEmitDiagnostics(
                // (6,35): error CS9023: User-defined operator '|' cannot be declared checked
                //         public static C1 operator checked |(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("|").WithLocation(6, 35),
                // (6,43): error CS0111: Type 'Extensions1' already defines a member called 'op_BitwiseOr' with the same parameter types
                //         public static C1 operator checked |(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "|").WithArguments("op_BitwiseOr", "Extensions1").WithLocation(6, 43)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator |(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 | c1;

        checked
        {
            _ = c1 | c1;
        }
    }
}
""";

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_032_Consumption_Checked([CombinatorialValues("+", "-", "*", "/")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
        public static C1 operator checked {{{op}}}(C1 x, C1 y)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public {{{typeKind}}} C1;
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 {{{op}}} c1;

        checked
        {
            _ = c1 {{{op}}} c1;
        }
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "regularchecked").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "regularchecked").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "c1 " + op + " c1").WithArguments("extensions", "14.0").WithLocation(6, 13),
                // (10,17): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //             _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "c1 " + op + " c1").WithArguments("extensions", "14.0").WithLocation(10, 17)
                );
        }

        [Fact]
        public void Binary_033_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension(C1)
    {
        public static C1 operator checked -(C1 x, C1 y)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 - c1;

        checked
        {
            _ = c1 - c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_034_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            return x;
        }

        public static C1 operator checked -(C1 x, C1 y)
        {
            return x;
        }
    }
}

public static class Extensions2
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 - c1;

        checked
        {
            _ = c1 - c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
#if DEBUG
            comp.VerifyEmitDiagnostics(
                // (35,16): error CS9339: Operator resolution is ambiguous between the following members:'Extensions2.extension(C1).operator -(C1, C1)' and 'Extensions1.extension(C1).operator -(C1, C1)'
                //         _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions2.extension(C1).operator -(C1, C1)", "Extensions1.extension(C1).operator -(C1, C1)").WithLocation(35, 16),
                // (39,20): error CS9339: Operator resolution is ambiguous between the following members:'Extensions1.extension(C1).operator checked -(C1, C1)' and 'Extensions2.extension(C1).operator -(C1, C1)'
                //             _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(C1).operator checked -(C1, C1)", "Extensions2.extension(C1).operator -(C1, C1)").WithLocation(39, 20)
                );
#else
            comp.VerifyEmitDiagnostics(
                // (35,16): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator -(C1, C1)' and 'Extensions2.extension(C1).operator -(C1, C1)'
                //         _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(C1).operator -(C1, C1)", "Extensions2.extension(C1).operator -(C1, C1)").WithLocation(35, 16),
                // (39,20): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked -(C1, C1)' and 'Extensions2.extension(C1).operator -(C1, C1)'
                //             _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-").WithArguments("Extensions1.extension(C1).operator checked -(C1, C1)", "Extensions2.extension(C1).operator -(C1, C1)").WithLocation(39, 20)
              );
#endif
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().Last();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("Extensions1.extension(C1).operator checked -(C1, C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(C1).operator -(C1, C1)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        public void Binary_035_Consumption_CheckedLiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator -(S1 x, S1 y) => throw null;
        public static S1 operator checked -(S1 x, S1 y) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator -(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 - s1;
        System.Console.Write(":");

        checked
        {
            _ = s1 - s1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_036_Consumption_OverloadResolutionPlusRegularVsChecked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("C1");
            return x;
        }
        public static C1 operator checked -(C1 x, C1 y)
        {
            System.Console.Write("checkedC1");
            return x;
        }
    }
    extension(C2)
    {
        public static C2 operator -(C2 x, C2 y)
        {
            System.Console.Write("C2");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;
public class C3 : C1;

class Program
{
    static void Main()
    {
        var c3 = new C3();
        _ = c3 - c3;

        checked
        {
            _ = c3 - c3;
        }

        var c2 = new C2();
        _ = c2 - c2;

        checked
        {
            _ = c2 - c2;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1checkedC1C2C2").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_037_Consumption()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1? operator +(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static void M1(S1? x) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
#line 21
        _ = s1 + s1;
        S1 s2 = new S1();
        _ = s2 + s2;

        System.Nullable<S1>.M1(s1);
        S1.M1(s1);
        S1.M1(s2);
    }
}

public static class Extensions2
{
    extension(S2)
    {
        public static S2? operator +(S2 x, S2 y)
        {
            return x;
        }
    }
}

public struct S2
{}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (5,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S1? operator +(S1? x, S1? y)
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "+").WithLocation(5, 36),
                // (21,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1?' and 'S1?'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1?", "S1?").WithLocation(21, 13),
                // (23,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s2 + s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 + s2").WithArguments("+", "S1", "S1").WithLocation(23, 13),
                // (25,9): error CS1929: 'S1?' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1).M1(S1?)' requires a receiver of type 'S1'
                //         System.Nullable<S1>.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "System.Nullable<S1>").WithArguments("S1?", "M1", "Extensions1.extension(S1).M1(S1?)", "S1").WithLocation(25, 9)
                );
        }

        [Fact]
        public void Binary_038_Consumption_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator +(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 + s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_039_Consumption_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator +(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        var s2 = new object();
        try
        {
            _ = s1 + s2;
        }
        catch
        {
            System.Console.Write("exception1");
        }

        try
        {
            _ = s2 + s1;
        }
        catch
        {
            System.Console.Write("exception2");
        }
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "exception1exception2").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_040_Consumption_WithLambda()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, System.Func<int> y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static S1 operator +(System.Func<int> y, S1 x)
        {
            System.Console.Write("operator2");
            return x;
        }
    }
}

public struct S1
{}

public class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 + (() => 1);
        _ = (() => 1) + s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator2").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_041_Consumption_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, S2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static S1 operator +(S2 y, S1 x)
        {
            System.Console.Write("operator2");
            return x;
        }
    }
}

public struct S1
{}
public struct S2
{}

public class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 + new();
        _ = new() + s1;
        _ = new() + new();
        _ = s1 + default;
        _ = default + s1;
        _ = default + default;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (28,13): error CS8310: Operator '+' cannot be applied to operand 'new()'
                //         _ = s1 + new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s1 + new()").WithArguments("+", "new()").WithLocation(28, 13),
                // (29,13): error CS8310: Operator '+' cannot be applied to operand 'new()'
                //         _ = new() + s1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() + s1").WithArguments("+", "new()").WithLocation(29, 13),
                // (30,13): error CS8310: Operator '+' cannot be applied to operand 'new()'
                //         _ = new() + new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "new() + new()").WithArguments("+", "new()").WithLocation(30, 13),
                // (31,13): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         _ = s1 + default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s1 + default").WithArguments("+", "default").WithLocation(31, 13),
                // (32,13): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         _ = default + s1;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + s1").WithArguments("+", "default").WithLocation(32, 13),
                // (33,13): error CS8310: Operator '+' cannot be applied to operand 'default'
                //         _ = default + default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "default + default").WithArguments("+", "default").WithLocation(33, 13)
                );
        }

        [Fact]
        public void Binary_042_Consumption_BadReceiver()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(__arglist)
    {
        public static object operator +(object x, object y)
        {
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 + s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static object operator +(object x, object y)
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "+").WithLocation(5, 39),
                // (17,13): error CS0019: Operator '+' cannot be applied to operands of type 'object' and 'object'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "object", "object").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Binary_043_Consumption_Checked_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T1>(C1<T1>)
    {
        public static C1<T1> operator -(C1<T1> x, C1<T1> y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension<T2>(C1<T2>)
    {
        public static C1<T2> operator checked -(C1<T2> x, C1<T2> y)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1<T>;

class Program
{
    static void Main()
    {
        var c1 = new C1<int>();
        _ = c1 - c1;

        checked
        {
            _ = c1 - c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_044_Consumption_Logical(bool fromMetadata, [CombinatorialValues("&&", "||")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F {{{op[0]}}} y.F };
        }

        public static bool operator true(S1 x)
        {
            System.Console.Write("operator2:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            return x.F;
        }

        public static bool operator false(S1 x)
        {
            System.Console.Write("operator3:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            return !x.F;
        }
    }
}

public {{{typeKind}}} S1
{
    public bool F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1[] s = [new S1() { F = false }, new S1() { F = true }];

        foreach (var s1 in s)
        {
            foreach (var s2 in s)
            {
                Print(s1 {{{op}}} s2);
                System.Console.WriteLine();
            }
        }
    }

    static void Print(S1 x)
    {
        System.Console.Write(":");
        System.Console.Write(x.F);
    }
}
""";

            string expected = op == "&&" ?
@"
operator3:False::False
operator3:False::False
operator3:True:operator1:True:False:False
operator3:True:operator1:True:True:True
"
:
@"
operator2:False:operator1:False:False:False
operator2:False:operator1:False:True:True
operator2:True::True
operator2:True::True
";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: expected).VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator " + op[0] + "(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: expected).VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (11,23): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //                 Print(s1 && s2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1 " + op + " s2").WithArguments("extensions", "14.0").WithLocation(11, 23)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_045_Consumption_Logical_InDifferentBlocks([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(S1)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
        {
            System.Console.Write("operator2");
            return false;
        }
    }
    extension(S1)
    {
        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
    }
}

public struct S1
{}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();

            var comp1 = CreateCompilation(src1, options: TestOptions.DebugDll);

            comp = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();

            comp = CreateCompilation(src2, references: [comp1.EmitToImageReference()], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_046_Consumption_Logical_DifferentTupleNames([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension((int a, int b))
    {
        public static (int c, int d) operator {{{op[0]}}}((int e, int f) x, (int g, int h) y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator {{{(op == "&&" ? "false" : "true")}}}((int i, int j) x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}((int k, int l) x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = (1, 2);
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_047_Consumption_Logical_DifferentParameterTypes([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
namespace NS
{
    public static class Extensions1
    {
        extension(S1)
        {
            public static S1 operator {{{op[0]}}}(S1 x, S2 y)
            {
                System.Console.Write("operator1");
                return x;
            }
            public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
            {
                System.Console.Write("operator2");
                return false;
            }

            public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
        }
    }

    class Program
    {
        static void Main()
        {
            S1 s1 = new S1();
            S2 s2 = new S2();
            _ = s1 {{{op}}} s2;
        }
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S1(S2 x) => default;
}
""";
            var src2 = $$$"""
public static class Extensions2
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator3");
            return x;
        }
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
        {
            System.Console.Write("operator4");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
    }
}
""";

            var comp = CreateCompilation(src1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (28,17): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('Extensions1.extension(S1).operator &(S1, S2)') must have the same return type and parameter types
                //             _ = s1 && s2;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s1 " + op + " s2").WithArguments("NS.Extensions1.extension(S1).operator " + op[0] + "(S1, S2)").WithLocation(28, 17)
                );

            comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (28,17): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('Extensions1.extension(S1).operator &(S1, S2)') must have the same return type and parameter types
                //             _ = s1 && s2;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s1 " + op + " s2").WithArguments("NS.Extensions1.extension(S1).operator " + op[0] + "(S1, S2)").WithLocation(28, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_048_Consumption_Logical_DifferentReturnType([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
namespace NS
{
    public static class Extensions1
    {
        extension(S1)
        {
            public static S2 operator {{{op[0]}}}(S1 x, S1 y)
            {
                System.Console.Write("operator1");
                return default;
            }
            public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
            {
                System.Console.Write("operator2");
                return false;
            }

            public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
        }
    }

    class Program
    {
        static void Main()
        {
            S1 s1 = new S1();
            S2 s2 = new S2();
            _ = s1 {{{op}}} s2;
        }
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S1(S2 x) => default;
}
""";
            var src2 = $$$"""
public static class Extensions2
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator3");
            return x;
        }
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
        {
            System.Console.Write("operator4");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
    }
}
""";

            var comp = CreateCompilation(src1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (28,17): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('Extensions1.extension(S1).operator &(S1, S1)') must have the same return type and parameter types
                //             _ = s1 && s2;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s1 " + op + " s2").WithArguments("NS.Extensions1.extension(S1).operator " + op[0] + "(S1, S1)").WithLocation(28, 17)
                );

            comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (28,17): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('Extensions1.extension(S1).operator &(S1, S1)') must have the same return type and parameter types
                //             _ = s1 && s2;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s1 " + op + " s2").WithArguments("NS.Extensions1.extension(S1).operator " + op[0] + "(S1, S1)").WithLocation(28, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_049_Consumption_Logical_TrueFalseBetterness([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T, S>((T, S))
    {
        public static (T, S) operator {{{op[0]}}}((T, S) x, (T, S) y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator true((T, S) x) => throw null;
        public static bool operator false((T, S) x) => throw null;
    }

    extension((int, int))
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}((int, int) x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}((int, int) x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = (1, 2);
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            AssertEx.Equal("Extensions1.extension<int, int>((int, int)).operator " + op[0] + "((int, int), (int, int))", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Theory]
        [CombinatorialData]
        public void Binary_050_Consumption_Logical_TrueFalseApplicability([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op[0]}}}(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(C1 x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(C1 x) => throw null;
    }

    extension(C2)
    {
        public static bool operator true(C2 x) => throw null;
        public static bool operator false(C2 x) => throw null;
    }
}

public class C1
{}

public class C2  : C1
{}

class Program
{
    static void Main()
    {
        C1 c1 = new C1();
        c1 = c1 {{{op}}} c1;

        C2 c2 = new C2();
        c1 = c2 {{{op}}} c2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_051_Consumption_Logical_TrueFalseApplicability([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(C1 x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(C1 x) => throw null;
    }

    extension(C2)
    {
        public static C2 operator {{{op[0]}}}(C2 x, C2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public class C1
{}

public class C2  : C1
{}

class Program
{
    static void Main()
    {
        C2 c2 = new C2();
        c2 = c2 {{{op}}} c2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_052_Consumption_Logical_TrueFalseApplicability([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
namespace NS
{
    public static class Extensions1
    {
        extension(C1)
        {
            public static C1 operator {{{op[0]}}}(C1 x, C1 y) => throw null;
        }

        extension(C2)
        {
            public static bool operator true(C2 x) => throw null;
            public static bool operator false(C2 x) => throw null;
        }
    }

    class Program
    {
        static void Main()
        {
            C1 c1 = new C1();
            c1 = c1 {{{op}}} c1;

            C2 c2 = new C2();
            c1 = c2 {{{op}}} c2;
        }
    }
}

public class C1
{}

public class C2  : C1
{}
""";
            var src2 = $$$"""
public static class Extensions2
{
    extension(C1)
    {
        public static C1 operator {{{op[0]}}}(C1 x, C1 y) => throw null;
        public static bool operator true(C1 x) => throw null;
        public static bool operator false(C1 x) => throw null;
    }
}
""";

            var comp = CreateCompilation(src1, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,18): error CS0218: In order for 'NS.Extensions1.extension(C1).operator &(C1, C1)' to be applicable as a short circuit operator, its declaring type 'NS.Extensions1' must define operator true and operator false
                //         c1 = c1 && c1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "c1 " + op + " c1").WithArguments("NS.Extensions1.extension(C1).operator " + op[0] + "(C1, C1)", "NS.Extensions1").WithLocation(22, 18),
                // (25,18): error CS0218: In order for 'NS.Extensions1.extension(C1).operator &(C1, C1)' to be applicable as a short circuit operator, its declaring type 'NS.Extensions1' must define operator true and operator false
                //         c1 = c2 && c2;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "c2 " + op + " c2").WithArguments("NS.Extensions1.extension(C1).operator " + op[0] + "(C1, C1)", "NS.Extensions1").WithLocation(25, 18)
                );

            comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (22,18): error CS0218: In order for 'NS.Extensions1.extension(C1).operator &(C1, C1)' to be applicable as a short circuit operator, its declaring type 'NS.Extensions1' must define operator true and operator false
                //         c1 = c1 && c1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "c1 " + op + " c1").WithArguments("NS.Extensions1.extension(C1).operator " + op[0] + "(C1, C1)", "NS.Extensions1").WithLocation(22, 18),
                // (25,18): error CS0218: In order for 'NS.Extensions1.extension(C1).operator &(C1, C1)' to be applicable as a short circuit operator, its declaring type 'NS.Extensions1' must define operator true and operator false
                //         c1 = c2 && c2;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "c2 " + op + " c2").WithArguments("NS.Extensions1.extension(C1).operator " + op[0] + "(C1, C1)", "NS.Extensions1").WithLocation(25, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_053_Consumption_Logical_TrueOrFalseInDifferentClass([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(S1)
    {
        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
    }
}

public static class Extensions2
{
    extension(S1)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
        {
            System.Console.Write("operator2");
            return false;
        }
    }
}

public struct S1
{}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyEmitDiagnostics(
                // (6,14): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 " + op + " s1").WithArguments("Extensions1.extension(S1).operator " + op[0] + "(S1, S1)", "Extensions1").WithLocation(6, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_054_Consumption_Logical_TrueOrFalseInDifferentClass([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(S1)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
        {
            System.Console.Write("operator2");
            return false;
        }
    }
}

public static class Extensions2
{
    extension(S1)
    {
        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
    }
}

public struct S1
{}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyEmitDiagnostics(
                // (6,14): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 " + op + " s1").WithArguments("Extensions1.extension(S1).operator " + op[0] + "(S1, S1)", "Extensions1").WithLocation(6, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_055_Consumption_Logical_TrueOrFalseInDifferentClass([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public static class Extensions2
{
    extension(S1)
    {
        public static bool operator false(S1 x) => throw null;
        public static bool operator true(S1 x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new S1();
#line 6
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (6,14): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 " + op + " s1").WithArguments("Extensions1.extension(S1).operator " + op[0] + "(S1, S1)", "Extensions1").WithLocation(6, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_056_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator true(S1? x) => throw null;

        public static bool operator false(S1? x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (11,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S1? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(11, 37),
                // (13,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S1? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(13, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_057_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(S1?)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1? x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1? x) => throw null;

        public static void M1(S1? x) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = s1 {{{op}}} s1;

        S1.M1(s1);
        System.Nullable<S1>.M1(s1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (33,14): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 " + op + " s1").WithArguments("Extensions1.extension(S1).operator " + op[0] + "(S1, S1)", "Extensions1").WithLocation(33, 14),
                // (35,9): error CS1929: 'S1' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1?).M1(S1?)' requires a receiver of type 'S1?'
                //         S1.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "S1").WithArguments("S1", "M1", "Extensions1.extension(S1?).M1(S1?)", "S1?").WithLocation(35, 9)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_058_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1? operator {{{op[0]}}}(S1? x, S1? y) => throw null;
    }
    extension(S1)
    {
        public static bool operator true(S1? x) => throw null;

        public static bool operator false(S1? x) => throw null;

        public static void M1(S1? x) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new S1();
#line 33
        s1 = s1 {{{op}}} s1;

        S1.M1(s1);
        System.Nullable<S1>.M1(s1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (5,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S1? operator &(S1? x, S1? y) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, op[..1]).WithLocation(5, 36),
                // (9,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S1? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(9, 37),
                // (11,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S1? x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(11, 37),
                // (33,14): error CS0019: Operator '&&' cannot be applied to operands of type 'S1' and 'S1'
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 " + op + " s1").WithArguments(op, "S1", "S1").WithLocation(33, 14),
                // (36,9): error CS1929: 'S1?' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1).M1(S1?)' requires a receiver of type 'S1'
                //         System.Nullable<S1>.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "System.Nullable<S1>").WithArguments("S1?", "M1", "Extensions1.extension(S1).M1(S1?)", "S1").WithLocation(36, 9)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_059_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1? operator {{{op[0]}}}(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(S1?)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1? x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1? x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = (s1 {{{op}}} s1).GetValueOrDefault();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (5,36): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static S1? operator &(S1? x, S1? y)
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, op[..1]).WithLocation(5, 36),
                // (31,15): error CS0019: Operator '&&' cannot be applied to operands of type 'S1' and 'S1'
                //         s1 = (s1 && s1).GetValueOrDefault();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 " + op + " s1").WithArguments(op, "S1", "S1").WithLocation(31, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_060_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static S1? operator {{{op[0]}}}(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1? x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1? x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        s1 = (s1 {{{op}}} s1).GetValueOrDefault();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_061_Consumption_Logical_TrueFalseTakesObject([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(object)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(object x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(object x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_062_Consumption_Logical_TrueFalseTakesSpan([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(int[])
    {
        public static int[] operator {{{op[0]}}}(int[] x, int[] y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension(System.Span<int>)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(System.Span<int> x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(System.Span<int> x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        var s1 = new int[] {};
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "operator2operator1" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_063_Consumption_Logical_TrueFalseTakesDifferentTuple([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension((int, int))
    {
        public static (int, int) operator {{{op[0]}}}((int, int) x, (int, int) y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
    extension((int, object))
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}((int, object) x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}((int, object) x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = (1, 2);
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_064_Consumption_Logical_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator &(S2 x, S2 y) => throw null;
        public static bool operator true (S2 x) => throw null;
        public static bool operator false(S2 x) => throw null;
    }
}

public struct S2
{
    public static implicit operator bool(S2 x)
    {
        System.Console.Write("operator3");
        return true;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        bool x = s2 && s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator3operator3").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();

            Assert.Equal("System.Boolean", model.GetTypeInfo(opNode).Type.ToTestDisplayString());
        }

        [Fact]
        public void Binary_065_Consumption_Logical_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator &(S2 x, S2 y) => throw null;
        public static bool operator false(S2 x) => throw null;
        public static bool operator true(S2 x) => throw null;
    }
}

public struct S2
{
    public static S2 operator &(S2 x, S2 y)
    {
        System.Console.Write("operator2");
        return x;
    }
    public static bool operator false(S2 x)
    {
        System.Console.Write("operator1");
        return false;
    }

    public static bool operator true(S2 x) => throw null;
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 && s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator &(S2, S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_066_Consumption_Logical_NonExtensionComesFirst_DifferentParameterTypes()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator &(S2 x, S2 y) => throw null;
        public static bool operator false(S2 x) => throw null;
        public static bool operator true(S2 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static S2 operator &(S2 x, S1 y)
    {
        return x;
    }

    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        var s2 = new S2();
        _ = s2 && s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (30,13): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('S2.operator &(S2, S1)') must have the same return type and parameter types
                //         _ = s2 && s1;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s2 && s1").WithArguments("S2.operator &(S2, S1)").WithLocation(30, 13)
                );
        }

        [Fact]
        public void Binary_067_Consumption_Logical_NonExtensionComesFirst_TrueFalseIsMissing()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator &(S2 x, S2 y) => throw null;
        public static bool operator false(S2 x) => throw null;
        public static bool operator true(S2 x) => throw null;
    }
}

public struct S2
{
    public static S2 operator &(S2 x, S2 y)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 && s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (24,13): error CS0218: In order for 'S2.operator &(S2, S2)' to be applicable as a short circuit operator, its declaring type 'S2' must define operator true and operator false
                //         _ = s2 && s2;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s2 && s2").WithArguments("S2.operator &(S2, S2)", "S2").WithLocation(24, 13)
                );
        }

        [Fact]
        public void Binary_068_Consumption_Logical_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator &(S1 x, S1 y) => throw null;
        public static bool operator true(S1 x) => throw null;
        public static bool operator false(S1 x) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator &(S1 x, S1 y)
            {
                System.Console.Write("operator2");
                return x;
            }

            public static bool operator true(S1 x) => throw null;
            public static bool operator false(S1 x)
            {
                System.Console.Write("operator1");
                return false;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator &(S2 x, S2 y) => throw null;
                public static bool operator true(S2 x) => throw null;
                public static bool operator false(S2 x) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = s1 && s1;
            }
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator &(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_069_Consumption_Logical_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static I1 operator &(I1 x, I1 y) => x;
    public static bool operator true(I1 x) => true;
    public static bool operator false(I1 x) => false;
}

public interface I3
{
    public static I3 operator &(I3 x, I3 y) => x;
    public static bool operator true(I3 x) => true;
    public static bool operator false(I3 x) => false;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator &(I2 x, I2 y) => x;
        public static bool operator true(I2 x) => true;
        public static bool operator false(I2 x) => false;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = x && x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (38,19): error CS9339: Operator resolution is ambiguous between the following members:'I1.operator &(I1, I1)' and 'I3.operator &(I3, I3)'
                //         var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "&&").WithArguments("I1.operator &(I1, I1)", "I3.operator &(I3, I3)").WithLocation(38, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator &(I1, I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator &(I3, I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_070_Consumption_Logical_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static I1 operator &(I1 x, I1 y) => x;
}

public interface I3
{
    public static I3 operator &(I3 x, I3 y) => x;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator &(I2 x, I2 y) => x;
        public static bool operator true(I2 x) => true;
        public static bool operator false(I2 x) => false;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = x && x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);

            comp.VerifyEmitDiagnostics(
                // (34,19): error CS9339: Operator resolution is ambiguous between the following members:'I1.operator &(I1, I1)' and 'I3.operator &(I3, I3)'
                //         var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "&&").WithArguments("I1.operator &(I1, I1)", "I3.operator &(I3, I3)").WithLocation(34, 19)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78830")]
        public void Binary_071_Consumption_Logical_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2)
    {
        public static I2 operator &(I2 x, I2 y) => x;
        public static bool operator true(I2 x) => true;
        public static bool operator false(I2 x) => false;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static I1 operator &(I1 x, I1 y) => x;
            public static bool operator true(I1 x) => true;
            public static bool operator false(I1 x) => false;
        }

        extension(I3)
        {
            public static I3 operator &(I3 x, I3 y) => x;
            public static bool operator true(I3 x) => true;
            public static bool operator false(I3 x) => false;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = x && x;
        }
    }
}
""";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (40,23): error CS9339: Operator resolution is ambiguous between the following members:'Extensions2.extension(I1).operator &(I1, I1)' and 'Extensions2.extension(I3).operator &(I3, I3)'
                //             var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "&&").WithArguments("NS1.Extensions2.extension(I1).operator &(I1, I1)", "NS1.Extensions2.extension(I3).operator &(I3, I3)").WithLocation(40, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator &(I1, I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator &(I3, I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Binary_072_Consumption_Logical_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions2
{
    extension(I2)
    {
        public static I2 operator &(I2 x, I2 y) => x;
        public static bool operator true(I2 x) => true;
        public static bool operator false(I2 x) => false;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static I1 operator &(I1 x, I1 y) => x;
        }

        extension(I3)
        {
            public static I3 operator &(I3 x, I3 y) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = x && x;
        }
    }
}
""";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (36,23): error CS9339: Operator resolution is ambiguous between the following members:'Extensions2.extension(I1).operator &(I1, I1)' and 'Extensions2.extension(I3).operator &(I3, I3)'
                //             var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "&&").WithArguments("NS1.Extensions2.extension(I1).operator &(I1, I1)", "NS1.Extensions2.extension(I3).operator &(I3, I3)").WithLocation(36, 23)
                );
        }

        [Fact]
        public void Binary_073_Consumption_Logical_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions2
{
    extension(I1)
    {
        public static I1 operator &(I1 x, I1 y) => x;
        public static bool operator true(I1 x) => true;
        public static bool operator false(I1 x) => false;
    }

    extension(I3)
    {
        public static I3 operator &(I3 x, I3 y) => x;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = x && x;
    }
}
""";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (26,19): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(I1).operator &(I1, I1)' and 'Extensions2.extension(I3).operator &(I3, I3)'
                //         var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "&&").WithArguments("Extensions2.extension(I1).operator &(I1, I1)", "Extensions2.extension(I3).operator &(I3, I3)").WithLocation(26, 19)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_074_Consumption_Logical_Lifted([CombinatorialValues("&&", "||")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F {{{op[0]}}} y.F };
        }
    }
    extension(S1?)
    {
        public static bool operator true(S1? x)
        {
            System.Console.Write("operator2:");
            System.Console.Write(x?.F.ToString() ?? "null");
            System.Console.Write(":");
            return x?.F == true;
        }

        public static bool operator false(S1? x)
        {
            System.Console.Write("operator3:");
            System.Console.Write(x?.F.ToString() ?? "null");
            System.Console.Write(":");
            return x?.F == false;
        }
    }
}

public struct S1
{
    public bool F;
}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1?[] s = [new S1() { F = false }, new S1() { F = true }, null];

        foreach (var s1 in s)
        {
            foreach (var s2 in s)
            {
                Print(s1 {{{op}}} s2);
                System.Console.WriteLine();
            }
        }
    }

    static void Print(S1? x)
    {
        System.Console.Write(":");
        System.Console.Write(x?.F.ToString() ?? "null");
    }
}
""";

            string expected = op == "&&" ?
@"
operator3:False::False
operator3:False::False
operator3:False::False
operator3:True:operator1:True:False:False
operator3:True:operator1:True:True:True
operator3:True::null
operator3:null::null
operator3:null::null
operator3:null::null
"
:
@"
operator2:False:operator1:False:False:False
operator2:False:operator1:False:True:True
operator2:False::null
operator2:True::True
operator2:True::True
operator2:True::True
operator2:null::null
operator2:null::null
operator2:null::null
";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: expected).VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: expected).VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (11,23): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //                 Print(s1 && s2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s1 " + op + " s2").WithArguments("extensions", "14.0").WithLocation(11, 23)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_075_Consumption_Logical_Lifted([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F {{{op[0]}}} y.F };
        }
    }
    extension(S1?)
    {
        public static bool operator true(S1? x)
        {
            System.Console.Write("operator2:");
            System.Console.Write(x?.F.ToString() ?? "null");
            System.Console.Write(":");
            return x?.F == true;
        }

        public static bool operator false(S1? x)
        {
            System.Console.Write("operator3:");
            System.Console.Write(x?.F.ToString() ?? "null");
            System.Console.Write(":");
            return x?.F == false;
        }
    }
}

public struct S1
{
    public bool F;
}

class Program
{
    static void Main()
    {
        S1?[] s1 = [new S1() { F = false }, new S1() { F = true }, null];
        S1[] s2 = [new S1() { F = false }, new S1() { F = true }];

        foreach (var s11 in s1)
        {
            foreach (var s12 in s2)
            {
                Print(s11 {{{op}}} s12);
                System.Console.WriteLine();
            }
        }

        foreach (var s11 in s2)
        {
            foreach (var s12 in s1)
            {
                Print(s11 {{{op}}} s12);
                System.Console.WriteLine();
            }
        }
    }

    static void Print(S1? x)
    {
        System.Console.Write(":");
        System.Console.Write(x?.F.ToString() ?? "null");
    }
}
""";

            string expected = op == "&&" ?
@"
operator3:False::False
operator3:False::False
operator3:True:operator1:True:False:False
operator3:True:operator1:True:True:True
operator3:null::null
operator3:null::null
operator3:False::False
operator3:False::False
operator3:False::False
operator3:True:operator1:True:False:False
operator3:True:operator1:True:True:True
operator3:True::null
"
:
@"
operator2:False:operator1:False:False:False
operator2:False:operator1:False:True:True
operator2:True::True
operator2:True::True
operator2:null::null
operator2:null::null
operator2:False:operator1:False:False:False
operator2:False:operator1:False:True:True
operator2:False::null
operator2:True::True
operator2:True::True
operator2:True::True
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        public void Binary_076_Consumption_Logical_LiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator &(S1 x, S1 y) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator &(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(S1? x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(S1? x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 && s1;
        System.Console.Write(":");
        s1 = null;
        _ = s1 && s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1:operator2operator1").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78830")]
        public void Binary_077_Consumption_Logical_NoLiftedFormForTrueFalse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator &(S1 x, S1 y) => throw null;
        public static bool operator false(S1 x) => throw null;
        public static bool operator true(S1 x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 && s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            // https://github.com/dotnet/roslyn/issues/78830: The wording is somewhat confusing because there are operators for S1, what is missing are the true/false operators for S1?.
            comp.VerifyEmitDiagnostics(
                // (19,13): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         _ = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 && s1").WithArguments("Extensions1.extension(S1).operator &(S1, S1)", "Extensions1").WithLocation(19, 13)
                );
        }

        [Fact]
        public void Binary_078_Consumption_Logical_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator &(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(object x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 && s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_079_Consumption_Logical_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator &(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(object x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        var s2 = new object();

        try
        {
            _ = s1 && s2;
        }
        catch
        {
            System.Console.Write("exception1");
        }


        try
        {
            _ = s1 && s1;
        }
        catch
        {
            System.Console.Write("exception2");
        }
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "exception1exception2").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_080_Consumption_Logical_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator &(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(object x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        var s2 = new object();
        _ = s2 && s1;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);

            // Note, an attempt to do compile time optimization using non-dynamic static type of 's2' ignores true/false extensions.
            // This is desirable because runtime binder wouldn't be able to use them as well.
            comp.VerifyEmitDiagnostics(
                // (26,13): error CS7083: Expression must be implicitly convertible to Boolean or its type 'object' must not be an interface and must define operator 'false'.
                //         _ = s2 && s1;
                Diagnostic(ErrorCode.ERR_InvalidDynamicCondition, "s2").WithArguments("object", "false").WithLocation(26, 13)
                );
        }

        [Fact]
        public void Binary_081_Consumption_Logical_WithLambda()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(System.Func<int>)
    {
        public static System.Func<int> operator &(System.Func<int> x, System.Func<int> y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(System.Func<int> x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(System.Func<int> x) => throw null;
    }
}

public struct S1
{}

public class Program
{
    static void Main()
    {
        System.Func<int> s1 = null;
        s1 = s1 && (() => 1);
        s1 = (() => 1) && s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1operator2operator1").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_082_Consumption_Logical_WithLambda()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(System.Func<int>)
    {
        public static System.Func<int> operator &(System.Func<int> x, System.Func<int> y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(System.Func<int> x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(System.Func<int> x) => throw null;
    }
}

public struct S1
{}

public class Program
{
    static void Main()
    {
        _ = (() => 1) && (() => 1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (27,13): error CS0019: Operator '&&' cannot be applied to operands of type 'lambda expression' and 'lambda expression'
                //         _ = (() => 1) && (() => 1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(() => 1) && (() => 1)").WithArguments("&&", "lambda expression", "lambda expression").WithLocation(27, 13)
                );
        }

        [Fact]
        public void Binary_083_Consumption_Logical_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator &(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static bool operator false(object x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 && new();
        _ = new() && s1;
        _ = new() && new();
        _ = s1 && default;
        _ = default && s1;
        _ = default && default;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (25,19): error CS8754: There is no target type for 'new()'
                //         _ = s1 && new();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(25, 19),
                // (26,13): error CS8754: There is no target type for 'new()'
                //         _ = new() && s1;
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(26, 13),
                // (27,13): error CS8754: There is no target type for 'new()'
                //         _ = new() && new();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(27, 13),
                // (27,22): error CS8754: There is no target type for 'new()'
                //         _ = new() && new();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(27, 22),
                // (28,19): error CS8716: There is no target type for the default literal.
                //         _ = s1 && default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(28, 19),
                // (29,13): error CS8716: There is no target type for the default literal.
                //         _ = default && s1;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(29, 13),
                // (30,13): error CS8716: There is no target type for the default literal.
                //         _ = default && default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(30, 13),
                // (30,24): error CS8716: There is no target type for the default literal.
                //         _ = default && default;
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(30, 24)
                );
        }

        [Fact]
        public void Binary_084_Consumption_Logical_BadReceiver()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(__arglist)
    {
        public static object operator &(object x, object y)
        {
            return x;
        }
        public static bool operator false(object x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator true(object x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 && s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static object operator &(object x, object y)
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "&").WithLocation(5, 39),
                // (9,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(object x)
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(9, 37),
                // (15,37): error CS9317: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(object x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(15, 37),
                // (24,13): error CS0019: Operator '&&' cannot be applied to operands of type 'object' and 'object'
                //         _ = s1 && s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 && s1").WithArguments("&&", "object", "object").WithLocation(24, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_085_Consumption_Logical_Generic([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T, S>((T, S))
    {
        public static (T, S) operator {{{op[0]}}}((T, S) x, (T, S) y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator {{{(op == "&&" ? "false" : "true")}}}((T, S) x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}((T, S) x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = (1, 2);
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            AssertEx.Equal("Extensions1.extension<int, int>((int, int)).operator " + op[0] + "((int, int), (int, int))", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Theory]
        [CombinatorialData]
        public void Binary_086_Consumption_Logical_Generic([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T, S>((T, S))
    {
        public static (T, S) operator {{{op[0]}}}((T, S) x, (T, S) y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }

    extension<U>(U)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(U x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(U x) => throw null;
    }
}

class Program
{
    static void Main()
    {
        var s1 = (1, 2);
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            AssertEx.Equal("Extensions1.extension<int, int>((int, int)).operator " + op[0] + "((int, int), (int, int))", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Theory]
        [CombinatorialData]
        public void Binary_087_Consumption_ExpressionTree([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
        public static S1 operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, S1 y) => throw null;
""";
            }

            var src = $$$"""
using System.Linq.Expressions;

#pragma warning disable CS1718 // Comparison made to same variable; did you mean to compare something else?

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
{{{pairedOp}}}

        public void Test()
        {
            Expression<System.Func<S1, S1>> ex = (s1) => s1 {{{op}}} s1;
            ex.Compile()(s1);
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => s1 {{{op}}} s1;

        var s1 = new S1();
        ex.Compile()(s1);

        s1.Test();

        System.Console.Write(":");
        System.Console.Write(ex);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1:s1 => (s1 " + op + " s1)").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_088_Consumption_ExpressionTree_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>")] string op)
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public void Test()
        {
            Expression<System.Func<S1?, S1?>> ex = (s1) => s1 {{{op}}} s1;
            var d = ex.Compile();
            d(s1);
            System.Console.Write(":");
            d(null);
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1?, S1?>> ex = (s1) => s1 {{{op}}} s1;

        var s1 = new S1();
        var d = ex.Compile();
        d(s1);
        System.Console.Write(":");
        d(null);

        System.Console.Write(":");
        s1.Test();

        System.Console.Write(":");
        System.Console.Write(ex);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1::operator1::s1 => (s1 " + op + " s1)").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_089_Consumption_UnsignedRightShift_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator >>>(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => s1 >>> s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,54): error CS7053: An expression tree may not contain '>>>'
                //         Expression<System.Func<S1, S1>> ex = (s1) => s1 >>> s1;
                Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "s1 >>> s1").WithArguments(">>>").WithLocation(22, 54)
                );
        }

        [Fact]
        public void Binary_090_Consumption_UnsignedRightShift_Lifted_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator >>>(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1?, S1?>> ex = (s1) => s1 >>> s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,56): error CS7053: An expression tree may not contain '>>>'
                //         Expression<System.Func<S1?, S1?>> ex = (s1) => s1 >>> s1;
                Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "s1 >>> s1").WithArguments(">>>").WithLocation(22, 56)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_091_Consumption_Logical_ExpressionTree([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1 x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1 x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (30,54): error CS9324: An expression tree may not contain '&&' or '||' operators that use extension user defined operators.
                //         Expression<System.Func<S1, S1>> ex = (s1) => s1 && s1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsExtensionBasedConditionalLogicalOperator, "s1 " + op + " s1").WithLocation(30, 54)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_092_Consumption_Logical_Lifted_ExpressionTree([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator {{{op[0]}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }

    extension(S1? s1)
    {
        public static bool operator {{{(op == "&&" ? "false" : "true")}}}(S1? x)
        {
            System.Console.Write("operator2");
            return false;
        }

        public static bool operator {{{(op == "&&" ? "true" : "false")}}}(S1? x) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1?, S1?>> ex = (s1) => s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (33,56): error CS9324: An expression tree may not contain '&&' or '||' operators that use extension user defined operators.
                //         Expression<System.Func<S1?, S1?>> ex = (s1) => s1 && s1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsExtensionBasedConditionalLogicalOperator, "s1 " + op + " s1").WithLocation(33, 56)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct
        /// </summary>
        [Fact]
        public void Binary_093_RefSafety()
        {
            var source = """
class C
{
    S M1()
    {
        S s;
        s = 100 + default(S); // 1
        return s;
    }

    S M2()
    {
        return 200 + default(S); // 2
    }

    S M3(in int x)
    {
        S s;
        s = x + default(S); // 3
        return s;
    }

    S M4(in int x)
    {
        return x + default(S);
    }

    S M4s(scoped in int x)
    {
        return x + default(S); // 4
    }

    S M5(in int x)
    {
        S s = x + default(S);
        return s;
    }

    S M5s(scoped in int x)
    {
        S s = x + default(S);
        return s; // 5
    }

    S M6()
    {
        S s = 300 + default(S);
        return s; // 6
    }

    void M7(in int x)
    {
        scoped S s;
        s = x + default(S);
        s = 100 + default(S);
    }
}

ref struct S
{
}

static class Extensions
{
    extension(S)
    {
        public static S operator+(in int x, S y) => throw null;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,13): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         s = 100 + default(S); // 1
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "100").WithLocation(6, 13),
                // (6,13): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in int, S)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = 100 + default(S); // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "100 + default(S)").WithArguments("Extensions.extension(S).operator +(in int, S)", "x").WithLocation(6, 13),
                // (12,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return 200 + default(S); // 2
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "200").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in int, S)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return 200 + default(S); // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "200 + default(S)").WithArguments("Extensions.extension(S).operator +(in int, S)", "x").WithLocation(12, 16),
                // (18,13): error CS9077: Cannot return a parameter by reference 'x' through a ref parameter; it can only be returned in a return statement
                //         s = x + default(S); // 3
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "x").WithArguments("x").WithLocation(18, 13),
                // (18,13): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in int, S)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = x + default(S); // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "x + default(S)").WithArguments("Extensions.extension(S).operator +(in int, S)", "x").WithLocation(18, 13),
                // (29,16): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return x + default(S); // 4
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(29, 16),
                // (29,16): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(in int, S)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return x + default(S); // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "x + default(S)").WithArguments("Extensions.extension(S).operator +(in int, S)", "x").WithLocation(29, 16),
                // (41,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(41, 16),
                // (47,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(47, 16)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Nested
        /// </summary>
        [Fact]
        public void Binary_094_RefSafety()
        {
            var source = """
class C
{
    S M()
    {
        S s;
        s = default(S) + 100 + 200;
        return s;
    }
}

ref struct S
{
}

static class Extensions
{
    extension(S)
    {
        public static S operator+(S y, in int x) => throw null;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,13): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(S, in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = default(S) + 100 + 200;
                Diagnostic(ErrorCode.ERR_EscapeCall, "default(S) + 100").WithArguments("Extensions.extension(S).operator +(S, in int)", "x").WithLocation(6, 13),
                // (6,13): error CS8347: Cannot use a result of 'Extensions.extension(S).operator +(S, in int)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         s = default(S) + 100 + 200;
                Diagnostic(ErrorCode.ERR_EscapeCall, "default(S) + 100 + 200").WithArguments("Extensions.extension(S).operator +(S, in int)", "y").WithLocation(6, 13),
                // (6,26): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         s = default(S) + 100 + 200;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "100").WithLocation(6, 26)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Scoped_Left
        /// </summary>
        [Fact]
        public void Binary_095_RefSafety()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
#line 11
        return new R(1) + new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static R operator +(scoped R x, R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (11,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator +(scoped R, R)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) + new R(2)").WithArguments("Extensions.extension(R).operator +(scoped R, R)", "y").WithLocation(11, 16),
                // (11,27): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(2)").WithArguments("R.R(in int)", "i").WithLocation(11, 27),
                // (11,33): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(11, 33)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Scoped_Right
        /// </summary>
        [Fact]
        public void Binary_096_RefSafety()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
#line 11
        return new R(1) + new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static R operator +(R x, scoped R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (11,16): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1)").WithArguments("R.R(in int)", "i").WithLocation(11, 16),
                // (11,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator +(R, scoped R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) + new R(2)").WithArguments("Extensions.extension(R).operator +(R, scoped R)", "x").WithLocation(11, 16),
                // (11,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(11, 22)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Scoped_Both
        /// </summary>
        [Fact]
        public void Binary_097_RefSafety()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
        return new R(1) + new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static R operator +(scoped R x, scoped R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Scoped_None
        /// </summary>
        [Fact]
        public void Binary_098_RefSafety()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
#line 11
        return new R(1) + new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static R operator +(R x, R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (11,16): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1)").WithArguments("R.R(in int)", "i").WithLocation(11, 16),
                // (11,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator +(R, R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) + new R(2)").WithArguments("Extensions.extension(R).operator +(R, R)", "x").WithLocation(11, 16),
                // (11,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) + new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(11, 22)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedLogical
        /// </summary>
        [Fact]
        public void Binary_099_RefSafety_Logical()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
    }

    S1 Test()
    {
        S1 global = default;
        S1 local = stackalloc int[100];

        // ok
        local = global && local;
        local = local && local;

        // ok
        global = global && global;

        // error
        global = local && global;

        // error
        return global || local;
    }
}

ref struct S1
{
    public static implicit operator S1(Span<int> o) => default;
}

static class Extensions
{
    extension(S1)
    {
        public static bool operator true(S1 o) => true;
        public static bool operator false(S1 o) => false;

        public static S1 operator &(S1 x, S1 y) => x;
        public static S1 operator |(S1 x, S1 y) => x;
    }
}
";
            CreateCompilation(text, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (22,18): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         global = local && global;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(22, 18),
                // (22,18): error CS8347: Cannot use a result of 'Extensions.extension(S1).operator &(S1, S1)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         global = local && global;
                Diagnostic(ErrorCode.ERR_EscapeCall, "local && global").WithArguments("Extensions.extension(S1).operator &(S1, S1)", "x").WithLocation(22, 18),
                // (25,16): error CS8347: Cannot use a result of 'Extensions.extension(S1).operator |(S1, S1)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return global || local;
                Diagnostic(ErrorCode.ERR_EscapeCall, "global || local").WithArguments("Extensions.extension(S1).operator |(S1, S1)", "y").WithLocation(25, 16),
                // (25,26): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         return global || local;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(25, 26)
                 );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedLogicalOperator_RefStruct
        /// </summary>
        [Fact]
        public void Binary_100_RefSafety_Logical()
        {
            var source = """
class C
{
    S M1(S s1, S s2)
    {
        S s = s1 && s2;
        return s; // 1
    }

    S M2(S s1, S s2)
    {
        return s1 && s2; // 2
    }

    S M3(in S s1, in S s2)
    {
        S s = s1 && s2;
        return s;
    }

    S M4(scoped in S s1, in S s2)
    {
        S s = s1 && s2;
        return s; // 3
    }

    S M5(in S s1, scoped in S s2)
    {
        S s = s1 && s2;
        return s; // 4
    }
}

ref struct S
{
}

static class Extensions
{
    extension(S)
    {
        public static bool operator true(in S s) => throw null;
        public static bool operator false(in S s) => throw null;
        public static S operator &(in S x, in S y) => throw null;
        public static S operator |(in S x, in S y) => throw null;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(6, 16),
                // (11,16): error CS8166: Cannot return a parameter by reference 's1' because it is not a ref parameter
                //         return s1 && s2; // 2
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "s1").WithArguments("s1").WithLocation(11, 16),
                // (11,16): error CS8347: Cannot use a result of 'Extensions.extension(S).operator &(in S, in S)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return s1 && s2; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "s1 && s2").WithArguments("Extensions.extension(S).operator &(in S, in S)", "x").WithLocation(11, 16),
                // (23,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 3
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(23, 16),
                // (29,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(29, 16)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedLogicalOperator_RefStruct_Scoped_Left
        /// </summary>
        [Fact]
        public void Binary_101_RefSafety_Logical()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
#line 13
        return new R(1) || new R(2);
    }

    static R F2()
    {
        return new R(1) | new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static bool operator true(R r) => true;
        public static bool operator false(R r) => false;
        public static R operator |(scoped R x, R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (13,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator |(scoped R, R)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) || new R(2)").WithArguments("Extensions.extension(R).operator |(scoped R, R)", "y").WithLocation(13, 16),
                // (13,28): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(2)").WithArguments("R.R(in int)", "i").WithLocation(13, 28),
                // (13,34): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(13, 34),
                // (18,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator |(scoped R, R)' in this context because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) | new R(2)").WithArguments("Extensions.extension(R).operator |(scoped R, R)", "y").WithLocation(18, 16),
                // (18,27): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(2)").WithArguments("R.R(in int)", "i").WithLocation(18, 27),
                // (18,33): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(18, 33)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedLogicalOperator_RefStruct_Scoped_Right
        /// </summary>
        [Fact]
        public void Binary_102_RefSafety_Logical()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
#line 13
        return new R(1) || new R(2);
    }

    static R F2()
    {
        return new R(1) | new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static bool operator true(R r) => true;
        public static bool operator false(R r) => false;
        public static R operator |(R x, scoped R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (13,16): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1)").WithArguments("R.R(in int)", "i").WithLocation(13, 16),
                // (13,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator |(R, scoped R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) || new R(2)").WithArguments("Extensions.extension(R).operator |(R, scoped R)", "x").WithLocation(13, 16),
                // (13,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(13, 22),
                // (18,16): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1)").WithArguments("R.R(in int)", "i").WithLocation(18, 16),
                // (18,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator |(R, scoped R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) | new R(2)").WithArguments("Extensions.extension(R).operator |(R, scoped R)", "x").WithLocation(18, 16),
                // (18,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(18, 22)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedLogicalOperator_RefStruct_Scoped_Both
        /// </summary>
        [Fact]
        public void Binary_103_RefSafety_Logical()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
        return new R(1) || new R(2);
    }

    static R F2()
    {
        return new R(1) | new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static bool operator true(R r) => true;
        public static bool operator false(R r) => false;
        public static R operator |(scoped R x, scoped R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedLogicalOperator_RefStruct_Scoped_None
        /// </summary>
        [Fact]
        public void Binary_104_RefSafety_Logical()
        {
            var source = """
ref struct R
{
    private ref readonly int _i;
    public R(in int i) { _i = ref i; }
}
class Program
{
    static R F()
    {
#line 13
        return new R(1) || new R(2);
    }

    static R F2()
    {
        return new R(1) | new R(2);
    }
}

static class Extensions
{
    extension(R)
    {
        public static bool operator true(R r) => true;
        public static bool operator false(R r) => false;
        public static R operator |(R x, R y) => default;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (13,16): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1)").WithArguments("R.R(in int)", "i").WithLocation(13, 16),
                // (13,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator |(R, R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) || new R(2)").WithArguments("Extensions.extension(R).operator |(R, R)", "x").WithLocation(13, 16),
                // (13,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) || new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(13, 22),
                // (18,16): error CS8347: Cannot use a result of 'R.R(in int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1)").WithArguments("R.R(in int)", "i").WithLocation(18, 16),
                // (18,16): error CS8347: Cannot use a result of 'Extensions.extension(R).operator |(R, R)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new R(1) | new R(2)").WithArguments("Extensions.extension(R).operator |(R, R)", "x").WithLocation(18, 16),
                // (18,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return new R(1) | new R(2);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(18, 22)
                );
        }

        [Fact]
        public void Binary_105_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }

    extension(C2)
    {
        public static C2 operator -(C2? x, C2? y)
        {
            System.Console.Write("operator2");
            return new C2();
        }
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x1 = null;
        C1? x2 = null;
        C1 y = new C1();
#line 25
        _ = x1 - y;
        y = y - x2;

        C2? z = null;
        _ = z - z;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (25,13): warning CS8604: Possible null reference argument for parameter 'x' in 'C1 Extensions1.extension(C1).operator -(C1 x, C1 y)'.
                //         _ = x1 - y;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "C1 Extensions1.extension(C1).operator -(C1 x, C1 y)").WithLocation(25, 13),
                // (26,17): warning CS8604: Possible null reference argument for parameter 'y' in 'C1 Extensions1.extension(C1).operator -(C1 x, C1 y)'.
                //         y = y - x2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("y", "C1 Extensions1.extension(C1).operator -(C1 x, C1 y)").WithLocation(26, 17)
                );
        }

        [Fact]
        public void Binary_106_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1? operator -(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var x = new C1();
        C1 y = x - x;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,16): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         C1 y = x - x;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "x - x").WithLocation(23, 16)
                );
        }

        [Fact]
        public void Binary_107_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(T x, T y)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x1 = null;
        C2? x2 = null;
        var y = new C2();
        (x1 - y).ToString();
        (y - x2).ToString();
        (y - y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,10): warning CS8602: Dereference of a possibly null reference.
                //         (x1 - y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1 - y").WithLocation(24, 10),
                // (25,10): warning CS8602: Dereference of a possibly null reference.
                //         (y - x2).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y - x2").WithLocation(25, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().ToArray();

            Assert.Equal(3, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator -(C2?, C2?)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator -(C2?, C2?)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator -(C2, C2)", model.GetSymbolInfo(opNodes[2]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Binary_108_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(int x, T y)
        {
            return y;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (1 - x).ToString();
        (1 - y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,10): warning CS8602: Dereference of a possibly null reference.
                //         (1 - x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "1 - x").WithLocation(23, 10)
                );
        }

        [Fact]
        public void Binary_109_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(T x, int y)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (x - 1).ToString();
        (y - 1).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,10): warning CS8602: Dereference of a possibly null reference.
                //         (x - 1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x - 1").WithLocation(23, 10)
                );
        }

        [Fact]
        public void Binary_110_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T, S>(C1<T>) where T : new() where S : new()
    {
        public static T operator -(C1<T> x, C1<S> y)
        {
            return x.F;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());

        (x - y).ToString();
        (y - x).ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,10): warning CS8602: Dereference of a possibly null reference.
                //         (x - y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x - y").WithLocation(29, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?, C2>(C1<C2?>).operator -(C1<C2?>, C1<C2>)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2, C2?>(C1<C2>).operator -(C1<C2>, C1<C2?>)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Binary_111_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T, S>(C1<T>) where T : new() where S : new()
    {
        public static T operator -(C1<S> y, C1<T> x)
        {
            return x.F;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());

        (x - y).ToString();
        (y - x).ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (30,10): warning CS8602: Dereference of a possibly null reference.
                //         (y - x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y - x").WithLocation(30, 10)
                );
        }

        [Fact]
        public void Binary_112_NullableAnalysis_Lifted()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(S1<T>) where T : new()
    {
        public static S1<T> operator -(S1<T> x, int y)
        {
            return x;
        }
    }
}

public struct S1<T> where T : new()
{
    public T F;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null) - 1;

        if (x != null)
            x.Value.F.ToString();

        var y = Get(new C2()) - 1;

        if (y != null)
            y.Value.F.ToString();
    }

    static S1<T>? Get<T>(T x) where T : new()
    {
        return new S1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,13): warning CS8602: Dereference of a possibly null reference.
                //             x.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x.Value.F").WithLocation(29, 13)
                );
        }

        [Fact]
        public void Binary_113_NullableAnalysis_Lifted()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(S1<T>) where T : new()
    {
        public static S1<T> operator -(int y, S1<T> x)
        {
            return x;
        }
    }
}

public struct S1<T> where T : new()
{
    public T F;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = 1 - Get((C2?)null);

        if (x != null)
            x.Value.F.ToString();

        var y = 1 - Get(new C2());

        if (y != null)
            y.Value.F.ToString();
    }

    static S1<T>? Get<T>(T x) where T : new()
    {
        return new S1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,13): warning CS8602: Dereference of a possibly null reference.
                //             x.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x.Value.F").WithLocation(29, 13)
                );
        }

        [Fact]
        public void Binary_114_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T) where T : notnull
    {
        public static object operator -(T x, T y)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (x - y).ToString();
        (y - x).ToString();
        (y - y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x - y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x - y").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(23, 10),
                // (24,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (y - x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "y - x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(24, 10)
                );
        }

        [Fact]
        public void Binary_115_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T, S>(C1<T>) where T : notnull, new() where S : notnull, new()
    {
        public static T operator -(C1<T> x, C1<S> y)
        {
            return x.F;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());
        (x - y).ToString();
        (y - x).ToString();
        (y - y).ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (28,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T, S>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x - y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x - y").WithArguments("Extensions1.extension<T, S>(C1<T>)", "T", "C2?").WithLocation(28, 10),
                // (28,10): warning CS8602: Dereference of a possibly null reference.
                //         (x - y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x - y").WithLocation(28, 10),
                // (29,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'S' in the generic type or method 'Extensions1.extension<T, S>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (y - x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "y - x").WithArguments("Extensions1.extension<T, S>(C1<T>)", "S", "C2?").WithLocation(29, 10)
                );
        }

        [Fact]
        public void Binary_116_NullableAnalysis_Logical()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator &(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator true(C1 x) => true;
        public static bool operator false(C1 x) => false;
    }

    extension(C2)
    {
        public static C2 operator &(C2? x, C2? y)
        {
            System.Console.Write("operator1");
            return new C2();
        }

        public static bool operator true(C2? x) => true;
        public static bool operator false(C2? x) => false;
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x1 = null;
        C1? x2 = null;
        C1 y = new C1();
#line 28
        _ = x1 && y;
        y = y && x2;

        C2? z = null;
        _ = z && z;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (28,13): warning CS8604: Possible null reference argument for parameter 'x' in 'C1 Extensions1.extension(C1).operator &(C1 x, C1 y)'.
                //         _ = x1 && y;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "C1 Extensions1.extension(C1).operator &(C1 x, C1 y)").WithLocation(28, 13),
                // (28,13): warning CS8604: Possible null reference argument for parameter 'x' in 'bool Extensions1.extension(C1).operator false(C1 x)'.
                //         _ = x1 && y;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "bool Extensions1.extension(C1).operator false(C1 x)").WithLocation(28, 13),
                // (29,18): warning CS8604: Possible null reference argument for parameter 'y' in 'C1 Extensions1.extension(C1).operator &(C1 x, C1 y)'.
                //         y = y && x2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("y", "C1 Extensions1.extension(C1).operator &(C1 x, C1 y)").WithLocation(29, 18)
                );
        }

        [Fact]
        public void Binary_117_NullableAnalysis_Logical()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1? operator &(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public static bool operator true(C1 x) => true;
        public static bool operator false(C1 x) => false;
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var x = new C1();
        C1 y = x && x;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (26,16): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         C1 y = x && x;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "x && x").WithLocation(26, 16)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/29605")]
        public void Binary_118_NullableAnalysis_Logical()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator &(T x, T y)
        {
            return x;
        }

        public static bool operator true(T x) => true;
        public static bool operator false(T x) => false;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x1 = null;
        C2? x2 = null;
        var y = new C2();
        (x1 && y).ToString();
        (y && x2).ToString();
        (y && y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (27,10): warning CS8602: Dereference of a possibly null reference.
                //         (x1 && y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1 && y").WithLocation(27, 10),
                // (28,10): warning CS8602: Dereference of a possibly null reference.
                //         (y && x2).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y && x2").WithLocation(28, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().ToArray();

            Assert.Equal(3, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator &(C2, C2)", model.GetSymbolInfo(opNodes[2]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Binary_119_NullableAnalysis_Logical()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T>)
    {
        public static C1<T> operator &(C1<T> x, C1<T> y)
        {
            return x;
        }

        public static bool operator true(C1<T> x) => true;
        public static bool operator false(C1<T> x) => false;
    }
}

public interface C1<out T>
{
    public T F { get; }
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());

        (x && y).F.ToString();
        (y && x).F.ToString();

        var z = Get((C2?)null);
        (y && z).F.ToString();
        (y && y).F.ToString();
    }

    static C1<T> Get<T>(T x)
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (32,9): warning CS8602: Dereference of a possibly null reference.
                //         (x && y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(x && y).F").WithLocation(32, 9),
                // (33,9): warning CS8602: Dereference of a possibly null reference.
                //         (y && x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(y && x).F").WithLocation(33, 9),
                // (36,9): warning CS8602: Dereference of a possibly null reference.
                //         (y && z).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(y && z).F").WithLocation(36, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().ToArray();

            Assert.Equal(4, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator &(C1<C2?>, C1<C2?>)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator &(C1<C2?>, C1<C2?>)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator &(C1<C2?>, C1<C2?>)", model.GetSymbolInfo(opNodes[2]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C1<C2>).operator &(C1<C2>, C1<C2>)", model.GetSymbolInfo(opNodes[3]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Binary_120_NullableAnalysis_Logical()
        {
            var src = $$$"""
#nullable enable

public interface C1<out T>
{
    public T F { get; }

    public static C1<T> operator &(C1<T> x, C1<T> y)
    {
        return x;
    }

    public static bool operator true(C1<T> x) => true;
    public static bool operator false(C1<T> x) => false;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());

        (x && y).F.ToString();
        (y && x).F.ToString();

        var z = Get((C2?)null);
        (y && z).F.ToString();
        (y && y).F.ToString();
    }

    static C1<T> Get<T>(T x)
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (26,9): warning CS8602: Dereference of a possibly null reference.
                //         (x && y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(x && y).F").WithLocation(26, 9),
                // (27,15): warning CS8620: Argument of type 'C1<C2?>' cannot be used for parameter 'y' of type 'C1<C2>' in 'C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)' due to differences in the nullability of reference types.
                //         (y && x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("C1<C2?>", "C1<C2>", "y", "C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)").WithLocation(27, 15),
                // (30,15): warning CS8620: Argument of type 'C1<C2?>' cannot be used for parameter 'y' of type 'C1<C2>' in 'C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)' due to differences in the nullability of reference types.
                //         (y && z).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("C1<C2?>", "C1<C2>", "y", "C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)").WithLocation(30, 15)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/29605")]
        public void Binary_121_NullableAnalysis_Logical_Lifted()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(S1<T>) where T : new()
    {
        public static S1<T> operator &(S1<T> x, S1<T> y)
        {
            return x;
        }
    }

    extension<T>(S1<T>?) where T : new()
    {
        public static bool operator true(S1<T>? x) => true;
        public static bool operator false(S1<T>? x) => false;
    }
}

public struct S1<T> where T : new()
{
    public T F;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var x1 = x && x;

        if (x1 != null)
            x1.Value.F.ToString();

        var y = Get(new C2());
        var y1 = y && y;

        if (y1 != null)
            y1.Value.F.ToString();
    }

    static S1<T>? Get<T>(T x) where T : new()
    {
        return new S1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (36,13): warning CS8602: Dereference of a possibly null reference.
                //             x1.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1.Value.F").WithLocation(36, 13)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/29605")]
        public void Binary_122_NullableAnalysis_Logical_Lifted()
        {
            var src = $$$"""
#nullable enable

public struct S1<T> where T : new()
{
    public T F;

    public static S1<T> operator &(S1<T> x, S1<T> y)
    {
        return x;
    }

    public static bool operator true(S1<T>? x) => true;
    public static bool operator false(S1<T>? x) => false;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var x1 = x && x;

        if (x1 != null)
            x1.Value.F.ToString();

        var y = Get(new C2());
        var y1 = y && y;

        if (y1 != null)
            y1.Value.F.ToString();
    }

    static S1<T>? Get<T>(T x) where T : new()
    {
        return new S1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (27,13): warning CS8602: Dereference of a possibly null reference.
                //             x1.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1.Value.F").WithLocation(27, 13)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/29605")]
        public void Binary_123_NullableAnalysis_Logical_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T) where T : notnull
    {
        public static T operator &(T x, T y)
        {
            return x;
        }
    }

    extension<T>(T)
    {
        public static bool operator true(T x) => true;
        public static bool operator false(T x) => false;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (x && y).ToString();
        (y && x).ToString();
        (y && y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (29,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x && y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x && y").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(29, 10),
                // (29,10): warning CS8602: Dereference of a possibly null reference.
                //         (x && y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x && y").WithLocation(29, 10),
                // (30,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (y && x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "y && x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(30, 10),
                // (30,10): warning CS8602: Dereference of a possibly null reference.
                //         (y && x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y && x").WithLocation(30, 10)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/29605")]
        public void Binary_124_NullableAnalysis_Logical_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator &(T x, T y)
        {
            return x;
        }
    }

    extension<T>(T) where T : notnull
    {
        public static bool operator true(T x) => true;
        public static bool operator false(T x) => false;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (x && y).ToString();
        (y && x).ToString();
        (y && y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (29,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x && y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(29, 10),
                // (29,10): warning CS8602: Dereference of a possibly null reference.
                //         (x && y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x && y").WithLocation(29, 10),
                // (30,10): warning CS8602: Dereference of a possibly null reference.
                //         (y && x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y && x").WithLocation(30, 10)
                );
        }

        [Fact]
        public void Binary_125_NullableAnalysis_Logical_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T>) where T : notnull
    {
        public static C1<T> operator &(C1<T> x, C1<T> y)
        {
            return x;
        }

        public static bool operator true(C1<T> x) => true;
        public static bool operator false(C1<T> x) => false;
    }
}

public interface C1<out T>
{
    public T F { get; }
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());
        (x && y).F.ToString();
        (y && x).F.ToString();
        (y && y).F.ToString();
    }

    static C1<T> Get<T>(T x)
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (31,9): warning CS8602: Dereference of a possibly null reference.
                //         (x && y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(x && y).F").WithLocation(31, 9),
                // (31,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x && y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x && y").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(31, 10),
                // (31,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x && y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(31, 10),
                // (32,9): warning CS8602: Dereference of a possibly null reference.
                //         (y && x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(y && x).F").WithLocation(32, 9),
                // (32,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (y && x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "y && x").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(32, 10),
                // (32,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (y && x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "y").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(32, 10)
                );
        }

        [Fact]
        public void Binary_126_NullableAnalysis_Logical_Constraints()
        {
            var src = $$$"""
#nullable enable

public interface C1<out T> where T : notnull
{
    public T F { get; }

    public static C1<T> operator &(C1<T> x, C1<T> y)
    {
        return x;
    }

    public static bool operator true(C1<T> x) => true;
    public static bool operator false(C1<T> x) => false;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());
        (x && y).F.ToString();
        (y && x).F.ToString();
        (y && y).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : notnull
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,17): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Program.Get<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         var x = Get((C2?)null);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "Get").WithArguments("Program.Get<T>(T)", "T", "C2?").WithLocation(23, 17),
                // (25,10): warning CS8620: Argument of type 'C1<C2?>' cannot be used for parameter 'x' of type 'C1<C2>' in 'C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)' due to differences in the nullability of reference types.
                //         (x && y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("C1<C2?>", "C1<C2>", "x", "C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)").WithLocation(25, 10),
                // (26,15): warning CS8620: Argument of type 'C1<C2?>' cannot be used for parameter 'y' of type 'C1<C2>' in 'C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)' due to differences in the nullability of reference types.
                //         (y && x).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("C1<C2?>", "C1<C2>", "y", "C1<C2> C1<C2>.operator &(C1<C2> x, C1<C2> y)").WithLocation(26, 15)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78828")]
        public void Binary_127_NullableAnalysis_WithLambda()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(T x, System.Func<T> y)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x1 = null;
        var y = new C2();
        (y - (() => x1)).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            // https://github.com/dotnet/roslyn/issues/78828: Expect to infer T as C2? and get a null dereference warning.
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().ToArray();

            Assert.Equal(1, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator -(C2, System.Func<C2>)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Binary_128_NullableAnalysis_Logical_Chained()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator &(T x, T y)
        {
            return x;
        }

        public static bool operator true(T x) => true;
        public static bool operator false(T x) => false;
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x1 = null;
        var y = new C2();
        (x1 && y && y && y && y && y).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (26,10): warning CS8602: Dereference of a possibly null reference.
                //         (x1 && y && y && y && y && y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1 && y && y && y && y && y").WithLocation(26, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().ToArray();

            Assert.Equal(5, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[2]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[3]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator &(C2?, C2?)", model.GetSymbolInfo(opNodes[4]).Symbol.ToDisplayString());
        }

        [Fact]
        public void Binary_129_Declaration_Extern()
        {
            var src = $$$"""
using System.Runtime.InteropServices;

public static class Extensions1
{
    extension(C2)
    {
        extern public static C2 operator -(C2 x, C2 y) => x;

        [DllImport("something.dll")]
        public static C2 operator +(C2 x, C2 y) => x;
    }
}

public class C2
{}
""";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,42): error CS0179: 'Extensions1.extension(C2).operator -(C2, C2)' cannot be extern and declare a body
                //         extern public static C2 operator -(C2 x, C2 y) => x;
                Diagnostic(ErrorCode.ERR_ExternHasBody, "-").WithArguments("Extensions1.extension(C2).operator -(C2, C2)").WithLocation(7, 42),
                // (9,10): error CS0601: The DllImport attribute must be specified on a method marked 'extern' that is either 'static' or an extension member
                //         [DllImport("something.dll")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(9, 10)
                );
        }

        [Fact]
        public void Binary_130_Declaration_Extern()
        {
            var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(C2)
    {
        [DllImport("something.dll")]
        extern public static C2 operator -(C2 x, C2 y);
    }
}

public class C2
{}
""";
            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$3D0C2090833F9460B6F186EEC21CE3B0'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$3D0C2090833F9460B6F186EEC21CE3B0'::'<Extension>$'
        } // end of class <M>$3D0C2090833F9460B6F186EEC21CE3B0
        // Methods
        .method public hidebysig specialname static 
            class C2 op_Subtraction (
                class C2 x,
                class C2 y
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 33 44 30 43 32 30 39 30 38
                33 33 46 39 34 36 30 42 36 46 31 38 36 45 45 43
                32 31 43 45 33 42 30 00 00
            )
            // Method begins at RVA 0x2086
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_Subtraction
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static pinvokeimpl("something.dll" winapi) 
        class C2 op_Subtraction (
            class C2 x,
            class C2 y
        ) cil managed preservesig 
    {
    } // end of method E::op_Subtraction
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void Binary_131_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2)
    {
        extern public static C2 operator -(C2 x, C2 y);
    }
}

public class C2
{}
""";
            var verifier = CompileAndVerify(source, verify: Verification.FailsPEVerify with { PEVerifyMessage = """
                Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                Type load failed.
                """ });

            verifier.VerifyDiagnostics(
                // (5,42): warning CS0626: Method, operator, or accessor 'E.extension(C2).operator -(C2, C2)' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern public static C2 operator -(C2 x, C2 y);
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "-").WithArguments("E.extension(C2).operator -(C2, C2)").WithLocation(5, 42)
                );

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$3D0C2090833F9460B6F186EEC21CE3B0'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$3D0C2090833F9460B6F186EEC21CE3B0'::'<Extension>$'
        } // end of class <M>$3D0C2090833F9460B6F186EEC21CE3B0
        // Methods
        .method public hidebysig specialname static 
            class C2 op_Subtraction (
                class C2 x,
                class C2 y
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 33 44 30 43 32 30 39 30 38
                33 33 46 39 34 36 30 42 36 46 31 38 36 45 45 43
                32 31 43 45 33 42 30 00 00
            )
            // Method begins at RVA 0x2086
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_Subtraction
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        class C2 op_Subtraction (
            class C2 x,
            class C2 y
        ) cil managed 
    {
    } // end of method E::op_Subtraction
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void Binary_132_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2)
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        extern public static C2 operator -(C2 x, C2 y);
    }
}

public class C2
{}
""";
            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$3D0C2090833F9460B6F186EEC21CE3B0'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 ''
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x208d
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$3D0C2090833F9460B6F186EEC21CE3B0'::'<Extension>$'
        } // end of class <M>$3D0C2090833F9460B6F186EEC21CE3B0
        // Methods
        .method public hidebysig specialname static 
            class C2 op_Subtraction (
                class C2 x,
                class C2 y
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 33 44 30 43 32 30 39 30 38
                33 33 46 39 34 36 30 42 36 46 31 38 36 45 45 43
                32 31 43 45 33 42 30 00 00
            )
            // Method begins at RVA 0x2086
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_Subtraction
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        class C2 op_Subtraction (
            class C2 x,
            class C2 y
        ) cil managed internalcall 
    {
    } // end of method E::op_Subtraction
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Theory]
        [CombinatorialData]
        public void Binary_133_Consumption_TupleComparison(bool fromMetadata)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static bool operator ==(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x.F == y.F;
        }

        public static bool operator !=(S1 x, S1 y)
        {
            System.Console.Write("operator2");
            return x.F != y.F;
        }
    }

    extension((S1, int))
    {
        public static bool operator ==((S1, int) x, (S1, int) y) => throw null;
        public static bool operator !=((S1, int) x, (S1, int) y) => throw null;
    }
}

public struct S1
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1() { F = 101 };
        var s2 = new S1() { F = 202 };

        if ((s1, 1) == (s1, 1))
        {
            System.Console.Write(": ==");
        }

        System.Console.Write(":");

        if ((s1, 1) == (s2, 1))
        {}
        else
        {
            System.Console.Write(": !=");
        }

        System.Console.Write(":");

        if ((s1, 1) != (s1, 1))
        {}
        else
        {
            System.Console.Write(": ==");
        }

        System.Console.Write(":");

        if ((s1, 1) != (s2, 1))
        {
            System.Console.Write(": !=");
        }
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1: ==:operator1: !=:operator2: ==:operator2: !=").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1: ==:operator1: !=:operator2: ==:operator2: !=").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (8,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if ((s1, 1) == (s1, 1))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "(s1, 1) == (s1, 1)").WithArguments("extensions", "14.0").WithLocation(8, 13),
                // (15,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if ((s1, 1) == (s2, 1))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "(s1, 1) == (s2, 1)").WithArguments("extensions", "14.0").WithLocation(15, 13),
                // (24,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if ((s1, 1) != (s1, 1))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "(s1, 1) != (s1, 1)").WithArguments("extensions", "14.0").WithLocation(24, 13),
                // (33,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         if ((s1, 1) != (s2, 1))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "(s1, 1) != (s2, 1)").WithArguments("extensions", "14.0").WithLocation(33, 13)
                );

            var src3 = $$$"""
public static class Extensions1
{
    extension((S1, int))
    {
        public static bool operator ==((S1, int) x, (S1, int) y) => throw null;
        public static bool operator !=((S1, int) x, (S1, int) y) => throw null;
    }
}

public struct S1
{
    public int F;
}
""";

            var comp3 = CreateCompilation(src3);
            var comp3Ref = fromMetadata ? comp3.EmitToImageReference() : comp3.ToMetadataReference();

            var comp4 = CreateCompilation(src2, references: [comp3Ref], options: TestOptions.DebugExe);
            comp4.VerifyDiagnostics(
                // (8,13): error CS0019: Operator '==' cannot be applied to operands of type 'S1' and 'S1'
                //         if ((s1, 1) == (s1, 1))
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(s1, 1) == (s1, 1)").WithArguments("==", "S1", "S1").WithLocation(8, 13),
                // (15,13): error CS0019: Operator '==' cannot be applied to operands of type 'S1' and 'S1'
                //         if ((s1, 1) == (s2, 1))
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(s1, 1) == (s2, 1)").WithArguments("==", "S1", "S1").WithLocation(15, 13),
                // (24,13): error CS0019: Operator '!=' cannot be applied to operands of type 'S1' and 'S1'
                //         if ((s1, 1) != (s1, 1))
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(s1, 1) != (s1, 1)").WithArguments("!=", "S1", "S1").WithLocation(24, 13),
                // (33,13): error CS0019: Operator '!=' cannot be applied to operands of type 'S1' and 'S1'
                //         if ((s1, 1) != (s2, 1))
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(s1, 1) != (s2, 1)").WithArguments("!=", "S1", "S1").WithLocation(33, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_134_Consumption_ReferenceTypeEquality_UnrelatedTypes(bool fromMetadata, [CombinatorialValues("==", "!=")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x, C2 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new C1 { F = x.F + y.F };
        }

        public static C1 operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(C1 x, C2 y) => throw null;
    }
}

public class C1
{
    public int F;
}

public class C2
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s11 = new C1() { F = 101 };
        var s12 = new C2() { F = 202 };
        var s2 = s11 {{{op}}} s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(C1).operator " + op + "(C1, C2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (7,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = s11 != s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(7, 18)
                );

            var opName = BinaryOperatorName(op);
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new C1();
        var s2 = new C2();
        Extensions1.{{{opName}}}(s1, s2);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new C1();
        var s2 = new C2();
#line 6
        s1.{{{opName}}}(s1);
        C1.{{{opName}}}(s1, s2);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_Addition' and no accessible extension method 'op_Addition' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_Addition(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("C1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_Addition'
                //         C1.op_Addition(s1, s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("C1", opName).WithLocation(7, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_135_Consumption_ReferenceTypeEquality_UnrelatedTypes(bool fromMetadata, [CombinatorialValues("==", "!=")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x, C1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new C1 { F = x.F + y.F };
        }

        public static C1 operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(C1 x, C1 y) => throw null;
    }
}

public class C1
{
    public int F;
}

public class C2
{
    public int F;

    public static implicit operator C1(C2 x) => new C1 { F = x.F };
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s11 = new C1() { F = 101 };
        var s12 = new C2() { F = 202 };
        var s2 = s11 {{{op}}} s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(C1).operator " + op + "(C1, C1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (7,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = s11 != s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + " s12").WithArguments("extensions", "14.0").WithLocation(7, 18)
                );

            var opName = BinaryOperatorName(op);
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new C1();
        var s2 = new C2();
        Extensions1.{{{opName}}}(s1, s2);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new C1();
        var s2 = new C2();
#line 6
        s1.{{{opName}}}(s1);
        C1.{{{opName}}}(s1, s2);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_Addition' and no accessible extension method 'op_Addition' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_Addition(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("C1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_Addition'
                //         C1.op_Addition(s1, s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("C1", opName).WithLocation(7, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_136_Consumption_ReferenceTypeEquality_RelatedTypes([CombinatorialValues("==", "!=")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x, C2 y) => throw null;
        public static C1 operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(C1 x, C2 y) => throw null;
    }
}

public class C1
{
}

public class C2 : C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        var c2 = new C2();
        var c3 = c1;
        System.Console.Write(c1 {{{op}}} c2);
        System.Console.Write(c1 {{{op}}} c3);
    }
}
""";

            var comp2 = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: op == "==" ? "FalseTrue" : "TrueFalse").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("object.operator " + op + "(object, object)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Boolean", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Theory]
        [CombinatorialData]
        public void Binary_137_Consumption_ReferenceTypeEquality_RelatedTypes([CombinatorialValues("==", "!=")] string op)
        {
            var src = $$$"""
#pragma warning disable CS0660 // 'C1' defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // 'C1' defines operator == or operator != but does not override Object.GetHashCode()

public class C1
{
    public int F;

    public static C1 operator {{{op}}}(C1 x, C2 y)
    {
        System.Console.Write("operator1:");
        System.Console.Write(x.F);
        System.Console.Write(":");
        System.Console.Write(y.F);
        return new C1 { F = x.F + y.F };
    }

    public static C1 operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(C1 x, C2 y) => throw null;
}

public class C2 : C1;

class Program
{
    static void Main()
    {
        var s11 = new C1() { F = 101 };
        var s12 = new C2() { F = 202 };
        var s2 = s11 {{{op}}} s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp2 = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:101:202:303").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().Where(a => a.Kind() != SyntaxKind.AddExpression).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            AssertEx.Equal("C1.operator " + op + "(C1, C2)", symbolInfo.Symbol.ToDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void Binary_138_Consumption_TypeParameterEquality([CombinatorialValues("==", "!=")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(T) where T : I1
    {
        public static bool operator {{{op}}}(T x, T y)
        {
            System.Console.Write("operator1:");
            return x.F.Equals(y.F);
        }

        public static bool operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(T x, T y) => throw null;
    }
}

public interface I1
{
    public int F {get;}
}

public class C1 : I1
{
    public int F {get; set;}
}

class Program
{
    static void Main()
    {
        var s1 = new C1() { F = 101 };
        var s2 = new C1() { F = 202 };
        var s3 = new C1() { F = 101 };
        Compare(s1, s2);
        System.Console.Write(":");
        Compare(s1, s1);
        System.Console.Write(":");
        Compare(s1, s3);
    }

    static void Compare<T>(T x, T y) where T : I1
    {
        System.Console.Write(x {{{op}}} y);
    }
}
""";

            var comp2 = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:False:operator1:True:operator1:True").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension<T>(T).operator " + op + "(T, T)", symbolInfo.Symbol.ToDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void Binary_139_Consumption_NullableTypeEquality_WithNull([CombinatorialValues("==", "!=")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static S1? operator {{{op}}}(S1? x, S1? y) => throw null;
        public static S1? operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1? x, S1? y) => throw null;
    }
}

public struct S1
{
}

class Program
{
    static void Main()
    {
        S1? s = new S1();
        System.Console.Write(s {{{op}}} null);
    }
}
""";

            var comp2 = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: op == "==" ? "False" : "True").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("object.operator " + op + "(object, object)", symbolInfo.Symbol.ToDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void Binary_140_Consumption_NullableTypeEquality_WithNull([CombinatorialValues("==", "!=")] string op)
        {
            var src = $$$"""
#pragma warning disable CS0660 // 'S1' defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // 'S1' defines operator == or operator != but does not override Object.GetHashCode()

public struct S1
{
    public int F;

    public static S1 operator {{{op}}}(S1? x, S1? y)
    {
        System.Console.Write("operator1:");
        return new S1 { F = (x?.F ?? 0) + (y?.F ?? 0) };
    }

    public static S1 operator {{{op switch { "==" => "!=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1? x, S1? y) => throw null;
}

class Program
{
    static void Main()
    {
        S1? s1 = new S1() { F = 101 };
        var s2 = s1 {{{op}}} null;
        System.Console.Write(s2.F);
    }
}
""";

            var comp2 = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().Where(a => a.Kind() is not (SyntaxKind.AddExpression or SyntaxKind.CoalesceExpression)).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S1.operator " + op + "(S1?, S1?)", symbolInfo.Symbol.ToDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void Binary_141_Consumption_CRef([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
        ///
        public static S1 operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, S1 y) => throw null;
""";
            }

            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}(S1, S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator {{{op}}}(S1 x, S1 y) => throw null;
{{{pairedOp}}}
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = BinaryOperatorName(op);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1,S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1,S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator " + ToCRefOp(op) + ", S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x, S1 y))",
                "(E.extension(S1).operator " + ToCRefOp(op) + "(S1, S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x, S1 y))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Binary_142_Consumption_CRef_Checked([CombinatorialValues("+", "-", "*", "/")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}(S1, S1)"/>
public static class E
{
    ///
    extension(S1)
    {
        ///
        public static S1 operator {{{op}}}(S1 x, S1 y) => throw null;
        ///
        public static S1 operator checked {{{op}}}(S1 x, S1 y) => throw null;
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = BinaryOperatorName(op, isChecked: true);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1,S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1,S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator checked " + ToCRefOp(op) + ", S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x, S1 y))",
                "(E.extension(S1).operator checked " + ToCRefOp(op) + "(S1, S1), S1 E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 x, S1 y))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void Binary_143_Consumption_CRef_Error([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}()"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}(S1)"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}(S1, S1)"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}()"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}(S1)"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}(S1, S1)"/>
public static class E
{
    ///
    extension(S1)
    {
    }
}

///
public struct S1;
""";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics(
                // (1,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +' that could not be resolved
                // /// <see cref="E.extension(S1).operator +"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op)).WithArguments("extension(S1).operator " + ToCRefOp(op)).WithLocation(1, 16),
                // (2,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +()' that could not be resolved
                // /// <see cref="E.extension(S1).operator +()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op) + "()").WithArguments("extension(S1).operator " + ToCRefOp(op) + "()").WithLocation(2, 16),
                // (3,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator +(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op) + "(S1)").WithArguments("extension(S1).operator " + ToCRefOp(op) + "(S1)").WithLocation(3, 16),
                // (4,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +(S1, S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator +(S1, S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op) + "(S1, S1)").WithArguments("extension(S1).operator " + ToCRefOp(op) + "(S1, S1)").WithLocation(4, 16),
                // (5,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op)).WithArguments("extension(S1).operator checked " + ToCRefOp(op)).WithLocation(5, 16),
                // (6,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +()' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op) + "()").WithArguments("extension(S1).operator checked " + ToCRefOp(op) + "()").WithLocation(6, 16),
                // (7,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op) + "(S1)").WithArguments("extension(S1).operator checked " + ToCRefOp(op) + "(S1)").WithLocation(7, 16),
                // (8,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +(S1, S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +(S1, S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op) + "(S1, S1)").WithArguments("extension(S1).operator checked " + ToCRefOp(op) + "(S1, S1)").WithLocation(8, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_144_ERR_VoidError_ArithmeticAndBitwise([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(void* x, S1 y) => x;
    }
}

public struct S1;

class Program
{
    unsafe void* Test(void* x, S1 y) => x {{{op}}} y;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (13,41): error CS0019: Operator '*' cannot be applied to operands of type 'void*' and 'S1'
                //     unsafe void* Test(void* x, S1 y) => x * y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, "void*", "S1").WithLocation(13, 41),
                // (13,41): error CS0242: The operation in question is undefined on void pointers
                //     unsafe void* Test(void* x, S1 y) => x * y;
                Diagnostic(ErrorCode.ERR_VoidError, $"x {op} y").WithLocation(13, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_145_ERR_VoidError_Comparison([CombinatorialValues(">", "<", ">=", "<=", "==", "!=")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(void* x, S1 y) => x;
        public static void* operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(void* x, S1 y) => throw null;
    }
}

public struct S1;

class Program
{
    unsafe void* Test(void* x, S1 y) => x {{{op}}} y;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (14,41): error CS0019: Operator '>' cannot be applied to operands of type 'void*' and 'S1'
                //     unsafe void* Test(void* x, S1 y) => x > y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, "void*", "S1").WithLocation(14, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_146_ERR_VoidError_ArithmeticAndBitwise([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(S1 x, void* y) => y;
    }
}

public struct S1;

class Program
{
    unsafe void* Test(void* x, S1 y) => y {{{op}}} x;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (13,41): error CS0019: Operator '^' cannot be applied to operands of type 'S1' and 'void*'
                //     unsafe void* Test(void* x, S1 y) => y ^ x;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"y {op} x").WithArguments(op, "S1", "void*").WithLocation(13, 41),
                // (13,41): error CS0242: The operation in question is undefined on void pointers
                //     unsafe void* Test(void* x, S1 y) => y ^ x;
                Diagnostic(ErrorCode.ERR_VoidError, $"y {op} x").WithLocation(13, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_147_ERR_VoidError_Comparison([CombinatorialValues(">", "<", ">=", "<=", "==", "!=")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(S1 x, void* y) => y;
        public static void* operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, void* y) => throw null;
    }
}

public struct S1;

class Program
{
    unsafe void* Test(void* x, S1 y) => y {{{op}}} x;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (14,41): error CS0019: Operator '&' cannot be applied to operands of type 'S1' and 'void*'
                //     unsafe void* Test(void* x, S1 y) => y & x;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"y {op} x").WithArguments(op, "S1", "void*").WithLocation(14, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_148_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
        public unsafe static void* operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(void* x, S1 y) => throw null;
""";
            }

            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        unsafe public static void* operator {{{op}}}(void* x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
{{{pairedOp}}}
    }
}

public struct S1;

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        var s2 = s11 {{{op}}} s12;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_149_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
    public unsafe static void* operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(void* x, S1 y) => throw null;
""";
            }

            var src = $$$"""
public struct S1
{
    unsafe public static void* operator {{{op}}}(void* x, S1 y)
    {
        System.Console.Write("operator1");
        return x;
    }
{{{pairedOp}}}

    public override bool Equals(object other) => throw null;
    public override int GetHashCode() => throw null;
}

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        var s2 = s11 {{{op}}} s12;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_150_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
        public unsafe static S1 operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, void* y) => throw null;
""";
            }

            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        unsafe public static S1 operator {{{op}}}(S1 x, void* y)
        {
            System.Console.Write("operator1");
            return x;
        }
{{{pairedOp}}}
    }
}

public struct S1;

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        var s2 = s12 {{{op}}} s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_151_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>", ">", "<", ">=", "<=", "==", "!=")] string op)
        {
            string pairedOp = "";

            if (op is ">" or "<" or ">=" or "<=" or "==" or "!=")
            {
                pairedOp = $$$"""
    public unsafe static S1 operator {{{op switch { ">" => "<", ">=" => "<=", "==" => "!=", "<" => ">", "<=" => ">=", "!=" => "==", _ => throw ExceptionUtilities.UnexpectedValue(op) }}}}(S1 x, void* y) => throw null;
""";
            }

            var src = $$$"""
public struct S1
{
    unsafe public static S1 operator {{{op}}}(S1 x, void* y)
    {
        System.Console.Write("operator1");
        return x;
    }
{{{pairedOp}}}

    public override bool Equals(object other) => throw null;
    public override int GetHashCode() => throw null;
}

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        var s2 = s12 {{{op}}} s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_152_ERR_VoidError_Logical([CombinatorialValues("&&", "||")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op[0]}}}(void* x, void* y) => x;
        public static bool operator true(void* x) => true;
        public static bool operator false(void* x) => false;
    }
}

class Program
{
    unsafe void* Test(void* x, void* y) => x {{{op}}} y;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (13,44): error CS0019: Operator '&&' cannot be applied to operands of type 'void*' and 'void*'
                //     unsafe void* Test(void* x, void* y) => x && y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op} y").WithArguments(op, "void*", "void*").WithLocation(13, 44)
                );
        }

        [Fact]
        public void Binary_153_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, int y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, int y)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 + s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (22,16): error CS9340: Operator cannot be applied to operands of type 'S2' and 'S2'. The closest inapplicable candidate is 'Extensions1.extension(S2).operator +(S2, int)'
                //         _ = s2 + s2;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+").WithArguments("S2", "S2", "Extensions1.extension(S2).operator +(S2, int)").WithLocation(22, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Fact]
        public void Binary_154_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, C1 y) => throw null;
        public static S2 operator +(S2 x, C2 y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, int y)
    {
        return x;
    }

    public static implicit operator C1 (S2 x) => null;
    public static implicit operator C2 (S2 x) => null;
}

public class C1;
public class C2;

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 + s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,16): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions1.extension(S2).operator +(S2, C1)' and 'Extensions1.extension(S2).operator +(S2, C2)'
                //         _ = s2 + s2;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("Extensions1.extension(S2).operator +(S2, C1)", "Extensions1.extension(S2).operator +(S2, C2)").WithLocation(29, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.Equal("Extensions1.extension(S2).operator +(S2, C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions1.extension(S2).operator +(S2, C2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_001_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions2
{
    extension(ref S1 s1)
    {
        public S1 operator {{{op}}}(int x) => throw null;
    }
}

static class Extensions3
{
    extension(ref S1 s1)
    {
        void operator {{{op}}}(int x) {}
    }
    extension(C1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions4
{
    extension(ref S1? s1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions5
{
    extension(S1 s1)
    {
#line 600
        public void operator {{{op}}}(int x) {}
    }
#line 700
    extension(ref C2 c2)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions6
{
    extension(C2 c2)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions7
{
    extension(in S1 s1)
    {
#line 800
        public void operator {{{op}}}(int x) {}
    }
#line 900
    extension(in C2 c2)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions8
{
    extension(ref readonly S1 s1)
    {
#line 1000
        public void operator {{{op}}}(int x) {}
    }
#line 1100
    extension(ref readonly C2 c2)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions9
{
    extension<T>(T t) where T : struct
    {
#line 1200
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions10
{
    extension<T>(T t) where T : class
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions11
{
    extension<T>(T t)
    {
#line 1300
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions12
{
    extension<T>(ref T t) where T : struct
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions13
{
#line 1400
    extension<T>(ref T t) where T : class
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions14
{
#line 1500
    extension<T>(ref T t)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions15
{
#line 1600
    extension<T>(in T t) where T : struct
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions16
{
#line 1700
    extension<T>(in T t) where T : class
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions17
{
#line 1800
    extension<T>(in T t)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions18
{
#line 1900
    extension<T>(ref readonly T t) where T : struct
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions19
{
#line 2000
    extension<T>(ref readonly T t) where T : class
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions20
{
#line 2100
    extension<T>(ref readonly T t)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions21
{
    extension(C2)
    {
#line 2200
        public void operator {{{op}}}(int x) {}
    }
}

struct S1
{}

static class C1
{}

class C2
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (13,28): error CS9503: The return type for this operator must be void
                //         public S1 operator +=(int x) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(13, 28),
                // (21,23): error CS9501: User-defined operator 'Extensions3.extension(ref S1).operator +=(int)' must be declared public
                //         void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("Extensions3.extension(ref S1).operator " + op + "(int)").WithLocation(21, 23),
                // (25,30): error CS9321: An extension block extending a static class cannot contain user-defined operators
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(25, 30),
                // (600,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(600, 30),
                // (700,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension(ref C2 c2)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "C2").WithLocation(700, 19),
                // (800,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(800, 30),
                // (900,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(900, 18),
                // (1000,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1000, 30),
                // (1100,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(1100, 28),
                // (1200,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1200, 30),
                // (1300,30): error CS9323: Cannot declare instance extension operator for a type that is not known to be a struct and is not known to be a class
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorExtensionWrongReceiverType, op).WithLocation(1300, 30),
                // (1400,22): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension<T>(ref T t) where T : class
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "T").WithLocation(1400, 22),
                // (1500,22): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension<T>(ref T t)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "T").WithLocation(1500, 22),
                // (1600,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t) where T : struct
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1600, 21),
                // (1700,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t) where T : class
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1700, 21),
                // (1800,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1800, 21),
                // (1900,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t) where T : struct
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(1900, 31),
                // (2000,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t) where T : class
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(2000, 31),
                // (2100,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(2100, 31),
                // (2200,30): error CS9303: 'operator +=': cannot declare instance members in an extension block with an unnamed receiver parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceMemberWithUnnamedExtensionsParameter, op).WithArguments("operator " + op).WithLocation(2200, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_002_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: Verification.FailsPEVerify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var name = CompoundAssignmentOperatorName(op);
                var method = m.GlobalNamespace.GetMember<MethodSymbol>("Extensions1." + name);

                AssertEx.Equal("Extensions1." + name + "(ref S1, int)", method.ToDisplayString());
                Assert.Equal(MethodKind.Ordinary, method.MethodKind);
                Assert.True(method.IsStatic);
                Assert.False(method.IsExtensionMethod);
                Assert.False(method.HasSpecialName);
                Assert.False(method.HasRuntimeSpecialName);
                Assert.False(method.HasUnsupportedMetadata);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_003_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        {{{modifier}}}
        public void operator {{{op}}}(int x) {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (6,30): error CS0106: The modifier 'abstract' is not valid for this item
                //         public void operator %=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_004_Declaration([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator checked {{{op}}}(int x) {}
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions2
{
    extension(ref S1 s1)
    {
#line 100
        public void operator checked {{{op}}}(int x) {}
    }
    extension(ref S1 s1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions3
{
    extension(ref S1 s1)
    {
        public void operator checked {{{op}}}(int x) {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (112,38): error CS9025: The operator 'Extensions3.extension(ref S1).operator checked +=(int)' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(ref S1).operator checked " + op + "(int)").WithLocation(112, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_005_Consumption(bool fromMetadata, [CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F + y.F };
        }
    }
}

public {{{typeKind}}} S1
{
    public int F;
}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s11 = new S1() { F = 101 };
        var s12 = new S1() { F = 202 };

        s11 {{{op}}}= s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");

        var s2 = s11 {{{op}}}= s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:303:202:operator1:303:202:505:202:505").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(a => a.Kind() != SyntaxKind.SimpleAssignmentExpression).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator " + op + "(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:303:202:operator1:303:202:505:202:505").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (8,9): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         s11 -= s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + "= s12").WithArguments("extensions", "14.0").WithLocation(8, 9),
                // (15,18): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         var s2 = s11 -= s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + "= s12").WithArguments("extensions", "14.0").WithLocation(15, 18)
                );

            var opName = BinaryOperatorName(op);
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 = Extensions1.{{{opName}}}(s1, s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.{{{opName}}}(s1);
        S1.{{{opName}}}(s1, s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_Addition' and no accessible extension method 'op_Addition' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_Addition(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("S1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_Addition'
                //         S1.op_Addition(s1, s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("S1", opName).WithLocation(7, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_006_Consumption(bool fromMetadata, [CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator {{{op}}}=(S1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            x.F = x.F + y.F;
        }
    }
}

public struct S1
{
    public int F;
}

""" + CompilerFeatureRequiredAttribute;

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s11 = new S1() { F = 101 };
        var s12 = new S1() { F = 202 };

        s11 {{{op}}}= s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");

        var s2 = s11 {{{op}}}= s12;
        System.Console.Write(":");
        System.Console.Write(s11.F);
        System.Console.Write(":");
        System.Console.Write(s12.F);
        System.Console.Write(":");
        System.Console.Write(s2.F);
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:303:202:operator1:303:202:505:202:505").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(a => a.Kind() != SyntaxKind.SimpleAssignmentExpression).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S1).operator " + op + "=(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:101:202:303:202:operator1:303:202:505:202:505").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (8,9): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S1'
                //         s11 += s12;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s11 " + op + "= s12").WithArguments(op + "=", "S1", "S1").WithLocation(8, 9),
                // (15,18): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S1'
                //         var s2 = s11 += s12;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s11 " + op + "= s12").WithArguments(op + "=", "S1", "S1").WithLocation(15, 18)
                );

            var opName = CompoundAssignmentOperatorName(op + "=");
            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.{{{opName}}}(ref s1, s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1:0:0").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.{{{opName}}}(s1);
        S1.{{{opName}}}(ref s1, s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyEmitDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_AdditionAssignment' and no accessible extension method 'op_AdditionAssignment' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_AdditionAssignment(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, opName).WithArguments("S1", opName).WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_AdditionAssignment'
                //         S1.op_AdditionAssignment(ref s1, s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, opName).WithArguments("S1", opName).WithLocation(7, 12)
                );

            var src5 = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator {{{op}}}=(C1 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            x.F = x.F + y.F;
        }
    }
}

public class C1
{
    public int F;
}

""" + CompilerFeatureRequiredAttribute;

            var src6 = $$$"""
class Program
{
    static void Main()
    {
        var c11 = new C1() { F = 101 };
        var c1 = c11;
        var c12 = new C1() { F = 202 };

        c11 {{{op}}}= c12;
        System.Console.Write(":");
        System.Console.Write(c11.F);
        System.Console.Write(":");
        System.Console.Write(c12.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c11, c1) ? "True" : "False");
        System.Console.Write(":");

        var c2 = c11 {{{op}}}= c12;
        System.Console.Write(":");
        System.Console.Write(c11.F);
        System.Console.Write(":");
        System.Console.Write(c12.F);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c11, c1) ? "True" : "False");
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c11, c2) ? "True" : "False");
    }
}
""";

            var comp5 = CreateCompilation(src5);
            var comp5Ref = fromMetadata ? comp5.EmitToImageReference() : comp5.ToMetadataReference();

            var comp6 = CreateCompilation(src6, references: [comp5Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp6, expectedOutput: "operator1:101:202:303:202:True:operator1:303:202:505:202:True:True").VerifyDiagnostics();

            comp6 = CreateCompilation(src6, references: [comp5Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp6, expectedOutput: "operator1:101:202:303:202:True:operator1:303:202:505:202:True:True").VerifyDiagnostics();

            comp6 = CreateCompilation(src6, references: [comp5Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp6.VerifyEmitDiagnostics(
                // (9,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'C1'
                //         c11 += c12;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c11 " + op + "= c12").WithArguments(op + "=", "C1", "C1").WithLocation(9, 9),
                // (18,18): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'C1'
                //         var c2 = c11 += c12;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c11 " + op + "= c12").WithArguments(op + "=", "C1", "C1").WithLocation(18, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_007_Consumption_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S2
{
    public static implicit operator int(S2 x)
    {
        System.Console.Write("operator2");
        return 0;
    }
    public static implicit operator S2(int x)
    {
        System.Console.Write("operator3");
        return default;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator2operator3").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("int.operator +(int, int)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_008_Consumption_PredefinedComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y) => throw null;
    }
}

public struct S2
{
    public static implicit operator int(S2 x)
    {
        System.Console.Write("operator2");
        return 0;
    }
    public static implicit operator S2(int x)
    {
        System.Console.Write("operator3");
        return default;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator2operator3").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("int.operator +(int, int)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_009_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, S2 y)
    {
        System.Console.Write("operator2");
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator +(S2, S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_010_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S2
{
    public void operator +=(S2 y)
    {
        System.Console.Write("operator2");
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator +=(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_011_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, S2 y)
    {
        System.Console.Write("operator2");
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator +(S2, S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_012_Consumption_NonExtensionComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y) => throw null;
    }
}

public struct S2
{
    public void operator +=(S2 y)
    {
        System.Console.Write("operator2");
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("S2.operator +=(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_013_Consumption_InstanceInTheSameScopeComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y)
        {
            System.Console.Write("operator2");
        }

        public static S2 operator +(S2 y, S2 z) => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S2).operator +=(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_014_Consumption_InstanceInTheSameScopeComesFirst()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y)
        {
            System.Console.Write("operator2");
        }
    }
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S2).operator +=(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_015_Consumption_InstanceInTheSameScopeComesFirst()
        {
            var src = $$$"""
public static class Extensions2
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y)
        {
            System.Console.Write("operator2");
        }
    }
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(ref S2).operator +=(S2)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S2", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_016_Consumption_StaticTriedAfterInapplicableInstanceInTheSameScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator +=(S2 y) => throw null;
    }
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y)
        {
            System.Console.Write("operator2");
            return y;
        }
    }
}

public struct S1
{
}

public struct S2
{
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_017_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator +(S1 x, S1 y)
            {
                System.Console.Write("operator1");
                return x;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator +(S2 x, S2 y) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = s1 += s1;
            }
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator +(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_018_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator +=(S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(S1)
        {
            public static S1 operator +(S1 x, S1 y)
            {
                System.Console.Write("operator1");
                return x;
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(ref S2 x)
            {
                public void operator +=(S2 y) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = s1 += s1;
            }
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(S1).operator +(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_019_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator +=(S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(ref S1 x)
        {
            public void operator +=(S1 y)
            {
                System.Console.Write("operator1");
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(ref S2 x)
            {
                public void operator +=(S2 y) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = s1 += s1;
            }
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(ref S1).operator +=(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_020_Consumption_ScopeByScope()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{}

namespace NS1
{
    public static class Extensions2
    {
        extension(ref S1 x)
        {
            public void operator +=(S1 y)
            {
                System.Console.Write("operator1");
            }
        }
    }

    namespace NS2
    {
        public static class Extensions3
        {
            extension(S2)
            {
                public static S2 operator +(S2 x, S2 y) => throw null;
            }
        }

        class Program
        {
            static void Main()
            {
                var s1 = new S1();
                _ = s1 += s1;
            }
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ElementAt(1);
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("NS1.Extensions2.extension(ref S1).operator +=(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_021_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public static I1 operator -(I1 x, I1 y) => x;
}

public interface I3
{
    public static I3 operator -(I3 x, I3 y) => x;
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2 z)
    {
        public void operator -=(I2 y) {}
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = x -= x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (33,19): error CS9339: Operator resolution is ambiguous between the following members:'I1.operator -(I1, I1)' and 'I3.operator -(I3, I3)'
                //         var y = x -= x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-=").WithArguments("I1.operator -(I1, I1)", "I3.operator -(I3, I3)").WithLocation(33, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator -(I1, I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator -(I3, I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_022_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public void operator -=(I1 y) {}
}

public interface I3
{
    public void operator -=(I3 y) {}
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
}

public static class Extensions1
{
    extension(I2 z)
    {
        public void operator -=(I2 y) {}
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
        var y = x -= x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (33,19): error CS0121: The call is ambiguous between the following methods or properties: 'I1.operator -=(I1)' and 'I3.operator -=(I3)'
                //         var y = x -= x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("I1.operator -=(I1)", "I3.operator -=(I3)").WithLocation(33, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator -=(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator -=(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_023_Consumption_NonExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1
{
    public void operator -=(I1 y) {}
}

public interface I3
{
    public void operator -=(I3 y) {}
}

public interface I4 : I1, I3
{
}

public interface I2 : I4
{
    public static I2 operator -(I2 x, I2 y) => y;
}

public static class Extensions1
{
    extension(I2 z)
    {
        public void operator -=(I2 y) {}
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

class Test2 : I2
{
    static void Main()
    {
        I2 x = new Test2();
#line 33
        var y = x -= x;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyEmitDiagnostics(
                // (33,19): error CS0121: The call is ambiguous between the following methods or properties: 'I1.operator -=(I1)' and 'I3.operator -=(I3)'
                //         var y = x -= x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("I1.operator -=(I1)", "I3.operator -=(I3)").WithLocation(33, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("I1.operator -=(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("I3.operator -=(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78830")]
        public void CompoundAssignment_024_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2 z)
    {
        public void operator -=(I2 y) {}
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1)
        {
            public static I1 operator -(I1 x, I1 y) => x;
        }

        extension(I3)
        {
            public static I3 operator -(I3 x, I3 y) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = x -= x;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (35,23): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(I1).operator -(I1, I1)' and 'Extensions2.extension(I3).operator -(I3, I3)'
                //             var y = x -= x;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-=").WithArguments("NS1.Extensions2.extension(I1).operator -(I1, I1)", "NS1.Extensions2.extension(I3).operator -(I3, I3)").WithLocation(35, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator -(I1, I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator -(I3, I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_025_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2 z)
    {
        public void operator -=(I2 y) {}
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1 x)
        {
            public void operator -=(I1 y) {}
        }

        extension(I3 x)
        {
            public void operator -=(I3 y) {}
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
            var y = x -= x;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (35,23): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(I1).operator -=(I1)' and 'Extensions2.extension(I3).operator -=(I3)'
                //             var y = x -= x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("NS1.Extensions2.extension(I1).operator -=(I1)", "NS1.Extensions2.extension(I3).operator -=(I3)").WithLocation(35, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator -=(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator -=(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_026_Consumption_ExtensionAmbiguity()
        {
            var src = $$$"""
public interface I1;
public interface I3;
public interface I4 : I1, I3;
public interface I2 : I4;

public static class Extensions1
{
    extension(I2 z)
    {
        public void operator -=(I2 y) {}
        public static I2 operator -(I2 x, I2 y) => x;
    }
}

namespace NS1
{
    public static class Extensions2
    {
        extension(I1 x)
        {
            public void operator -=(I1 y) {}
        }

        extension(I3 x)
        {
            public void operator -=(I3 y) {}
        }

        extension(I2)
        {
            public static I2 operator -(I2 x, I2 y) => x;
        }
    }

    class Test2 : I2
    {
        static void Main()
        {
            I2 x = new Test2();
#line 35
            var y = x -= x;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (35,23): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(I1).operator -=(I1)' and 'Extensions2.extension(I3).operator -=(I3)'
                //             var y = x -= x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("NS1.Extensions2.extension(I1).operator -=(I1)", "NS1.Extensions2.extension(I3).operator -=(I3)").WithLocation(35, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("NS1.Extensions2.extension(I1).operator -=(I1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("NS1.Extensions2.extension(I3).operator -=(I3)", symbolInfo.CandidateSymbols[1].ToDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_027_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(ref S1 z)
    {
        public void operator {{{op}}}=(S1 y) => throw null;

        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1? s12 = new S1();
        _ = s11 {{{op}}}= s12;
        System.Console.Write(":");
        s11 = null;
        _ = s11 {{{op}}}= s12;
        _ = s12 {{{op}}}= s11;
        _ = s11 {{{op}}}= s11;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "operator1:").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "operator1:").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (7,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 -= s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + "= s12").WithArguments("extensions", "14.0").WithLocation(7, 13),
                // (10,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 -= s12;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + "= s12").WithArguments("extensions", "14.0").WithLocation(10, 13),
                // (11,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s12 -= s11;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s12 " + op + "= s11").WithArguments("extensions", "14.0").WithLocation(11, 13),
                // (12,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = s11 -= s11;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "s11 " + op + "= s11").WithArguments("extensions", "14.0").WithLocation(12, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_028_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 z)
    {
        public void operator {{{op}}}=(S1 y) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1? s12 = new S1();
        _ = s11 {{{op}}}= s12;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (18,17): error CS9340: Operator cannot be applied to operands of type 'S1?' and 'S1?'. The closest inapplicable candidate is 'Extensions1.extension(ref S1).operator %=(S1)'
                //         _ = s11 %= s12;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, op + "=").WithArguments("S1?", "S1?", "Extensions1.extension(ref S1).operator " + op + "=(S1)").WithLocation(18, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_029_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 z)
    {
        public void operator {{{op}}}=(S1 y) => throw null;

        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1 s12 = new S1();
        _ = s11 {{{op}}}= s12;
        System.Console.Write(":");
        s11 = null;
        _ = s11 {{{op}}}= s12;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_030_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 z)
    {
        public void operator {{{op}}}=(S1 y) => throw null;

        public static S1 operator {{{op}}}(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1 s12 = new S1();
        _ = s12 {{{op}}}= s11;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,13): error CS0266: Cannot implicitly convert type 'S1?' to 'S1'. An explicit conversion exists (are you missing a cast?)
                //         _ = s12 += s11;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "s12 " + op + "= s11").WithArguments("S1?", "S1").WithLocation(24, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_031_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 z)
    {
        public void operator {{{op}}}=(S1 y) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s11 = new S1();
        S1 s12 = new S1();
        _ = s11 {{{op}}}= s12;
        _ = s12 {{{op}}}= s11;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (18,17): error CS9340: Operator cannot be applied to operands of type 'S1?' and 'S1'. The closest inapplicable candidate is 'Extensions1.extension(ref S1).operator %=(S1)'
                //         _ = s11 %= s12;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, op + "=").WithArguments("S1?", "S1", "Extensions1.extension(ref S1).operator " + op + "=(S1)").WithLocation(18, 17),
                // (19,17): error CS9340: Operator cannot be applied to operands of type 'S1' and 'S1?'. The closest inapplicable candidate is 'Extensions1.extension(ref S1).operator %=(S1)'
                //         _ = s12 %= s11;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, op + "=").WithArguments("S1", "S1?", "Extensions1.extension(ref S1).operator " + op + "=(S1)").WithLocation(19, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_032_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S2 y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S1 { F = x.F * 1000 + y.F };
        }
        public static S2 operator {{{op}}}(S2 x, S1 y)
        {
            System.Console.Write("operator2:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y.F);
            return new S2 { F = x.F * 1000 + y.F };
        }
    }
}

public struct S1
{
    public int F;
}

public struct S2
{
    public int F;
}

class Program
{
    static void Main()
    {
        S1?[] s1 = [new S1() { F = 101 }, null];
        S2?[] s2 = [new S2() { F = 202 }, null];

        foreach (var s11 in s1)
        {
            foreach (var s12 in s2)
            {
                var s21 = s11;
                var s22 = s12;

                Print(s21 {{{op}}}= s22, s21, s22);
                System.Console.WriteLine();

                s21 = s11;
                s22 = s12;

                Print(s22 {{{op}}}= s21, s22, s21);
                System.Console.WriteLine();
            }
        }
    }

    static void Print(S1? x, S1? y, S2? z)
    {
        System.Console.Write(":");
        System.Console.Write(x?.F.ToString() ?? "null");
        System.Console.Write(":");
        System.Console.Write(y?.F.ToString() ?? "null");
        System.Console.Write(":");
        System.Console.Write(z?.F.ToString() ?? "null");
    }

    static void Print(S2? x, S2? y, S1? z)
    {
        System.Console.Write(":");
        System.Console.Write(x?.F.ToString() ?? "null");
        System.Console.Write(":");
        System.Console.Write(y?.F.ToString() ?? "null");
        System.Console.Write(":");
        System.Console.Write(z?.F.ToString() ?? "null");
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput:
@"
operator1:101:202:101202:101202:202
operator2:202:101:202101:202101:101
:null:null:null
:null:null:101
:null:null:202
:null:null:null
:null:null:null
:null:null:null
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_033_Consumption_Lifted_Shift([CombinatorialValues("<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator {{{op}}}(S1 x, S2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

public struct S2
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        S2? s2 = new S2();
        _ = s1 {{{op}}}= s2;
        System.Console.Write(":");
        s1 = null;
        _ = s1 {{{op}}}= s2;
        s1 = new S1();
        s2 = null;
        _ = s1 {{{op}}}= s2;
        s1 = null;
        _ = s1 {{{op}}}= s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_034_Consumption_LiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, S1 y) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator +(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 += s1;
        System.Console.Write(":");
        s1 = null;
        _ = s1 += s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_035_Consumption_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1?)
    {
        public static S1? operator +(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 += s1;
        Extensions1.op_Addition(s1, s1);

        S1? s2 = new S1();
        _ = s2 += s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (21,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s1").WithArguments("+=", "S1", "S1").WithLocation(21, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_036_Consumption_ExtendedTypeIsNullable()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1? x)
    {
        public void operator +=(S1? y) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 += s1;
        Extensions1.op_AdditionAssignment(s1, s1);
        Extensions1.op_AdditionAssignment(ref s1, s1);

        S1? s2 = new S1();
        _ = s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,16): error CS9340: Operator could not be resolved on operands of type 'S1' and 'S1'. The closest inapplicable candidate is 'Extensions1.extension(ref S1?).operator +=(S1?)'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1", "S1", "Extensions1.extension(ref S1?).operator +=(S1?)").WithLocation(17, 16),
                // (18,43): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         Extensions1.op_AdditionAssignment(s1, s1);
                Diagnostic(ErrorCode.ERR_BadArgRef, "s1").WithArguments("1", "ref").WithLocation(18, 43),
                // (19,47): error CS1503: Argument 1: cannot convert from 'ref S1' to 'ref S1?'
                //         Extensions1.op_AdditionAssignment(ref s1, s1);
                Diagnostic(ErrorCode.ERR_BadArgType, "s1").WithArguments("1", "ref S1", "ref S1?").WithLocation(19, 47)
                );
        }

        [Fact]
        public void CompoundAssignment_037_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S2 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 += s2;
        _ = s2 += s1;
        _ = s1 += s1;
        Extensions1.op_Addition(s1, s1);

        S1? s3 = new S1();
        _ = s3 += s3;
        Extensions1.op_Addition(s3, s3);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (23,13): error CS0029: Cannot implicitly convert type 'S2' to 'S1'
                //         _ = s1 += s2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1 += s2").WithArguments("S2", "S1").WithLocation(23, 13),
                // (25,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s1").WithArguments("+=", "S1", "S1").WithLocation(25, 13),
                // (29,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1?' and 'S1?'
                //         _ = s3 += s3;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s3 += s3").WithArguments("+=", "S1?", "S1?").WithLocation(29, 13),
                // (30,33): error CS1503: Argument 1: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_Addition(s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("1", "S1?", "S2").WithLocation(30, 33),
                // (30,37): error CS1503: Argument 2: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_Addition(s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("2", "S1?", "S2").WithLocation(30, 37)
                );

            var src1 = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, S1 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
    public static implicit operator S1(S2 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 += s1;
        _ = s1 += s2;
        _ = s2 += s1;
        _ = s2 += s2;
        Extensions1.op_Addition(s1, s1);
        Extensions1.op_Addition(s1, s2);
    }
}
""";

            var comp1 = CreateCompilation(src1, options: TestOptions.DebugExe);
            comp1.VerifyEmitDiagnostics(
                // (24,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s1").WithArguments("+=", "S1", "S1").WithLocation(24, 13),
                // (25,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S2'
                //         _ = s1 += s2;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s2").WithArguments("+=", "S1", "S2").WithLocation(25, 13)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S1 x, S2 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
    public static implicit operator S1(S2 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 += s1;
        _ = s1 += s2;
        _ = s2 += s1;
        _ = s2 += s2;
        Extensions1.op_Addition(s1, s1);
        Extensions1.op_Addition(s2, s1);
    }
}
""";

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            comp2.VerifyEmitDiagnostics(
                // (24,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s1").WithArguments("+=", "S1", "S1").WithLocation(24, 13),
                // (26,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S2' and 'S1'
                //         _ = s2 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s2 += s1").WithArguments("+=", "S2", "S1").WithLocation(26, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_038_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(S2 y) => throw null;
    }
}

public struct S1
{}

public struct S2
{
    public static implicit operator S2(S1 x) => default;
}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        _ = s1 += s2;
        _ = s2 += s1;
        _ = s1 += s1;
        Extensions1.op_AdditionAssignment(ref s1, s1);

        S1? s3 = new S1();
        _ = s3 += s3;
        Extensions1.op_AdditionAssignment(ref s3, s3);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (23,16): error CS9340: Operator could not be resolved on operands of type 'S1' and 'S2'. The closest inapplicable candidate is 'Extensions1.extension(ref S2).operator +=(S2)'
                //         _ = s1 += s2;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1", "S2", "Extensions1.extension(ref S2).operator +=(S2)").WithLocation(23, 16),
                // (25,16): error CS9340: Operator could not be resolved on operands of type 'S1' and 'S1'. The closest inapplicable candidate is 'Extensions1.extension(ref S2).operator +=(S2)'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1", "S1", "Extensions1.extension(ref S2).operator +=(S2)").WithLocation(25, 16),
                // (26,47): error CS1503: Argument 1: cannot convert from 'ref S1' to 'ref S2'
                //         Extensions1.op_AdditionAssignment(ref s1, s1);
                Diagnostic(ErrorCode.ERR_BadArgType, "s1").WithArguments("1", "ref S1", "ref S2").WithLocation(26, 47),
                // (29,16): error CS9340: Operator could not be resolved on operands of type 'S1?' and 'S1?'. The closest inapplicable candidate is 'Extensions1.extension(ref S2).operator +=(S2)'
                //         _ = s3 += s3;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1?", "S1?", "Extensions1.extension(ref S2).operator +=(S2)").WithLocation(29, 16),
                // (30,47): error CS1503: Argument 1: cannot convert from 'ref S1?' to 'ref S2'
                //         Extensions1.op_AdditionAssignment(ref s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("1", "ref S1?", "ref S2").WithLocation(30, 47),
                // (30,51): error CS1503: Argument 2: cannot convert from 'S1?' to 'S2'
                //         Extensions1.op_AdditionAssignment(ref s3, s3);
                Diagnostic(ErrorCode.ERR_BadArgType, "s3").WithArguments("2", "S1?", "S2").WithLocation(30, 51)
                );
        }

        [Fact]
        public void CompoundAssignment_039_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static S1 operator +(object x, int y)
        {
            System.Console.Write("operator1");
            return default;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        s1 += 1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_040_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object x)
    {
        public void operator +=(int y) => throw null;
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        s1 += 1;
        Extensions1.op_AdditionAssignment(s1, 1);

        S1? s2 = new S1();
        s2 += 1;
        Extensions1.op_AdditionAssignment(s2, 1);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,12): error CS9340: Operator could not be resolved on operands of type 'S1' and 'int'. The closest inapplicable candidate is 'Extensions1.extension(object).operator +=(int)'
                //         s1 += 1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1", "int", "Extensions1.extension(object).operator +=(int)").WithLocation(17, 12),
                // (21,12): error CS9340: Operator could not be resolved on operands of type 'S1?' and 'int'. The closest inapplicable candidate is 'Extensions1.extension(object).operator +=(int)'
                //         s2 += 1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1?", "int", "Extensions1.extension(object).operator +=(int)").WithLocation(21, 12)
                );
        }

        [Fact]
        public void CompoundAssignment_041_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static C1 operator +(object x, int y)
        {
            System.Console.Write("operator1");
            return null;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1 = c1 += 1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_042_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1Base x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("operator1:");
            System.Console.Write(x.F);
            System.Console.Write(":");
            System.Console.Write(y);
            x.F += y;
        }
    }
}

public class C1Base
{
    public int F;
}

public class C1 : C1Base
{}

class Program
{
    static void Main()
    {
        var c11 = new C1() { F = 101 };
        var c1 = c11;
        var c12 = 202;

        c11 += c12;
        System.Console.Write(":");
        System.Console.Write(c11.F);
        System.Console.Write(":");
        System.Console.Write(c12);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c11, c1) ? "True" : "False");
        System.Console.Write(":");

        var c2 = c11 += c12;
        System.Console.Write(":");
        System.Console.Write(c11.F);
        System.Console.Write(":");
        System.Console.Write(c12);
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c11, c1) ? "True" : "False");
        System.Console.Write(":");
        System.Console.Write(ReferenceEquals(c11, c2) ? "True" : "False");
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:101:202:303:202:True:operator1:303:202:505:202:True:True").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_043_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(dynamic x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("operator1");
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1 += 1;
        c1 = c1 += 1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'dynamic'
                //     extension(dynamic x)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic").WithLocation(3, 15),
                // (20,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         c1 += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 += 1").WithArguments("+=", "C1", "int").WithLocation(20, 9),
                // (21,14): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         c1 = c1 += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 += 1").WithArguments("+=", "C1", "int").WithLocation(21, 14)
                );
        }

        [Fact]
        public void CompoundAssignment_044_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("operator1");
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        Test(new C1());
    }

    static void Test<T>(T c1) where T : class
    {
        c1 += 1;
        c1 = c1 += 1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_045_Consumption_ReceiverTypeMismatch()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref System.Span<int> x)
    {
        public void operator +=(int y) => throw null;
    }
}

class Program
{
    static void Main()
    {
        int[] a1 = null;
#line 17
        _ = a1 += 1;
        Extensions1.op_AdditionAssignment(ref a1, 1);
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,16): error CS9340: Operator could not be resolved on operands of type 'int[]' and 'int'. The closest inapplicable candidate is 'Extensions1.extension(ref Span<int>).operator +=(int)'
                //         _ = a1 += 1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("int[]", "int", "Extensions1.extension(ref System.Span<int>).operator +=(int)").WithLocation(17, 16),
                // (18,47): error CS1503: Argument 1: cannot convert from 'ref int[]' to 'ref System.Span<int>'
                //         Extensions1.op_AdditionAssignment(ref a1, 1);
                Diagnostic(ErrorCode.ERR_BadArgType, "a1").WithArguments("1", "ref int[]", "ref System.Span<int>").WithLocation(18, 47)
                );
        }

        [Fact]
        public void CompoundAssignment_046_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : struct
    {
        public static S1<T> operator +(S1<T> x, S1<T> y)
        {
            System.Console.Write(typeof(T).ToString());
            return x;
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        s1 = s1 += s1;
        Extensions1.op_Addition(s1, s1);

        S1<int>? s2 = new S1<int>();
        _ = (s2 += s2).GetValueOrDefault();
        s2 = null;
        System.Console.Write(":");
        _ = (s2 += s2).GetValueOrDefault();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32System.Int32:").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_047_Consumption_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(ref S1<T> x) where T : struct
    {
        public void operator +=(S1<T> y)
        {
            System.Console.Write(typeof(T).ToString());
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        s1 = s1 += s1;
        Extensions1.op_AdditionAssignment(ref s1, s1);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_048_Consumption_Generic_Worse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>)
    {
        public static S1<T> operator +(S1<T> x, S1<T> y)
        {
            System.Console.Write("[S1<T>]");
            return x;
        }
    }

    extension<T>(S1<T>?)
    {
        public static S1<T>? operator +(S1<T>? x, S1<T>? y)
        {
            System.Console.Write("[S1<T>?]");
            return x;
        }
    }

    extension(S1<int>)
    {
        public static S1<int> operator +(S1<int> x, S1<int> y)
        {
            System.Console.Write("[S1<int>]");
            return x;
        }
    }

    extension<T>(S2<T>)
    {
        public static S2<T> operator +(in S2<T> x, S2<T> y) => throw null;

        public static S2<T> operator +(S2<T> x, S2<T> y)
        {
            System.Console.Write("[S2<T>]");
            return x;
        }
    }

    extension(S2<int>)
    {
        public static S2<int> operator +(in S2<int> x, S2<int> y)
        {
            System.Console.Write("[in S2<int>]");
            return x;
        }
    }
}

public struct S1<T>
{}

public struct S2<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        s11 = s11 += s11;
        Extensions1.op_Addition(s11, s11);

        System.Console.WriteLine();

        var s12 = new S1<byte>();
        s12 = s12 += s12;
        Extensions1.op_Addition(s12, s12);

        System.Console.WriteLine();

        var s21 = new S2<int>();
        s21 = s21 += s21;
        Extensions1.op_Addition(s21, s21);

        System.Console.WriteLine();

        var s22 = new S2<byte>();
        s22 = s22 += s22;
        Extensions1.op_Addition(s22, s22);

        System.Console.WriteLine();

        S1<int>? s13 = new S1<int>();
        s13 = s13 += s13;
        s13 = null;
        s13 = s13 += s13;

        System.Console.WriteLine();

        S1<byte>? s14 = new S1<byte>();
        s14 = s14 += s14;
        s14 = null;
        s14 = s14 += s14;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
[S1<int>][S1<int>]
[S1<T>][S1<T>]
[in S2<int>][in S2<int>]
[S2<T>][S2<T>]
[S1<int>]
[S1<T>?][S1<T>?]
").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_049_Consumption_Generic_Worse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(ref S1<T> x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("[S1<T>]");
        }
    }

    extension<T>(ref S1<T>? x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("[S1<T>?]");
        }
    }

    extension(ref S1<int> x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("[S1<int>]");
        }
    }

    extension(ref S1<int>? x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("[S1<int>?]");
        }
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s11 = new S1<int>();
        s11 = s11 += 1;
        Extensions1.op_AdditionAssignment(ref s11, 1);

        System.Console.WriteLine();

        var s12 = new S1<byte>();
        s12 = s12 += 1;
        Extensions1.op_AdditionAssignment(ref s12, 1);

        System.Console.WriteLine();

        S1<int>? s13 = new S1<int>();
        s13 = s13 += 1;
        s13 = null;
        s13 = s13 += 1;

        System.Console.WriteLine();

        S1<byte>? s14 = new S1<byte>();
        s14 = s14 += 1;
        s14 = null;
        s14 = s14 += 1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: @"
[S1<int>][S1<int>]
[S1<T>][S1<T>]
[S1<int>?][S1<int>?]
[S1<T>?][S1<T>?]
").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_050_Consumption_Generic_ConstraintsViolation()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(S1<T>) where T : class
    {
        public static S1<T> operator +(S1<T> x, S1<T> y) => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        _ = s1 += s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,13): error CS0019: Operator '+=' cannot be applied to operands of type 'S1<int>' and 'S1<int>'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s1").WithArguments("+=", "S1<int>", "S1<int>").WithLocation(17, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_051_Consumption_Generic_ConstraintsViolation()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T>(ref S1<T> x) where T : class
    {
        public void operator +=(int i) => throw null;
    }
}

public struct S1<T>
{}

class Program
{
    static void Main()
    {
        var s1 = new S1<int>();
        _ = s1 += 1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (17,16): error CS9340: Operator could not be resolved on operands of type 'S1<int>' and 'int'. The closest inapplicable candidate is 'Extensions1.extension<int>(ref S1<int>).operator +=(int)'
                //         _ = s1 += 1;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S1<int>", "int", "Extensions1.extension<int>(ref S1<int>).operator +=(int)").WithLocation(17, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_052_Consumption_OverloadResolutionPriority()
        {
            var src = $$$"""
using System.Runtime.CompilerServices;

public static class Extensions1
{
    extension(C1)
    {
        [OverloadResolutionPriority(1)]
        public static C2 operator +(C1 x, C1 y)
        {
            System.Console.Write("C1");
            return null;
        }
    }
    extension(C2)
    {
        public static C2 operator +(C2 x, C2 y)
        {
            System.Console.Write("C2");
            return x;
        }
    }
    extension(C3)
    {
        public static C4 operator +(C3 x, C3 y)
        {
            System.Console.Write("C3");
            return null;
        }
    }
    extension(C4)
    {
        public static C4 operator +(C4 x, C4 y)
        {
            System.Console.Write("C4");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;

public class C3;
public class C4 : C3;

class Program
{
    static void Main()
    {
        var c2 = new C2();
        _ = c2 += c2;
        var c4 = new C4();
        _ = c4 += c4;
    }
}
""";

            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1C4").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_053_Consumption_OverloadResolutionPriority()
        {
            var src = $$$"""
using System.Runtime.CompilerServices;

public static class Extensions1
{
    extension(C1 x)
    {
        [OverloadResolutionPriority(1)]
        public void operator +=(int y)
        {
            System.Console.Write("C1");
        }
    }
    extension(C2 x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("C2");
        }
    }
    extension(C3 x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("C3");
        }
    }
    extension(C4 x)
    {
        public void operator +=(int y)
        {
            System.Console.Write("C4");
        }
    }
}

public class C1;
public class C2 : C1;

public class C3;
public class C4 : C3;

class Program
{
    static void Main()
    {
        var c2 = new C2();
        _ = c2 += 1;
        var c4 = new C4();
        _ = c4 += 1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation([src, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1C4").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_054_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_055_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator -=(C1 y)
        {
            System.Console.Write("regular");
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_056_Consumption_Checked_CheckedFormNotSupported()
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator |(C1 x, C1 y) => throw null;
        public static C1 operator checked |(C1 x, C1 y) => throw null;
    }
}

public class C1;
""";

            var comp1 = CreateCompilation(src1);
            comp1.VerifyEmitDiagnostics(
                // (6,35): error CS9023: User-defined operator '|' cannot be declared checked
                //         public static C1 operator checked |(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("|").WithLocation(6, 35),
                // (6,43): error CS0111: Type 'Extensions1' already defines a member called 'op_BitwiseOr' with the same parameter types
                //         public static C1 operator checked |(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "|").WithArguments("op_BitwiseOr", "Extensions1").WithLocation(6, 43)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator |(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 |= c1;

        checked
        {
            _ = c1 |= c1;
        }
    }
}
""";

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_057_Consumption_Checked_CheckedFormNotSupported()
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator |(C1 x, C1 y) => throw null;
        public static C1 operator checked |(C1 x, C1 y) => throw null;
    }
}

public class C1;
""";

            var comp1 = CreateCompilation(src1);
            comp1.VerifyEmitDiagnostics(
                // (6,35): error CS9023: User-defined operator '|' cannot be declared checked
                //         public static C1 operator checked |(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("|").WithLocation(6, 35),
                // (6,43): error CS0111: Type 'Extensions1' already defines a member called 'op_BitwiseOr' with the same parameter types
                //         public static C1 operator checked |(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "|").WithArguments("op_BitwiseOr", "Extensions1").WithLocation(6, 43)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator |(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 |= c1;

        checked
        {
            _ = c1 |= c1;
        }
    }
}
""";

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_058_Consumption_Checked_CheckedFormNotSupported()
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator |=(C1 y) => throw null;
        public void operator checked |=(C1 y) => throw null;
    }
}

public class C1;

""" + CompilerFeatureRequiredAttribute;

            var comp1 = CreateCompilation(src1);
            comp1.VerifyEmitDiagnostics(
                // (6,30): error CS9023: User-defined operator '|=' cannot be declared checked
                //         public void operator checked |=(C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("|=").WithLocation(6, 30),
                // (6,38): error CS0111: Type 'Extensions1' already defines a member called 'op_BitwiseOrAssignment' with the same parameter types
                //         public void operator checked |=(C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "|=").WithArguments("op_BitwiseOrAssignment", "Extensions1").WithLocation(6, 38)
                );

            var src2 = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator |=(C1 y)
        {
            System.Console.Write("regular");
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 |= c1;

        checked
        {
            _ = c1 |= c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp2 = CreateCompilation(src2, options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "regularregular").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_059_Consumption_Checked([CombinatorialValues("+", "-", "*", "/")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator {{{op}}}(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
        public static C1 operator checked {{{op}}}(C1 x, C1 y)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public {{{typeKind}}} C1;
""";
            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 {{{op}}}= c1;

        checked
        {
            _ = c1 {{{op}}}= c1;
        }
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "regularchecked").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "regularchecked").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //         _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "c1 " + op + "= c1").WithArguments("extensions", "14.0").WithLocation(6, 13),
                // (10,17): error CS9260: Feature 'extensions' is not available in C# 13.0. Please use language version 14.0 or greater.
                //             _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "c1 " + op + "= c1").WithArguments("extensions", "14.0").WithLocation(10, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_060_Consumption_Checked([CombinatorialValues("+", "-", "*", "/")] string op, [CombinatorialValues("struct", "class")] string typeKind)
        {
            var src1 = $$$"""
public static class Extensions1
{
    extension({{{(typeKind == "struct" ? "ref " : "")}}}C1 x)
    {
        public void operator {{{op}}}=(C1 y)
        {
            System.Console.Write("regular");
        }
        public void operator checked {{{op}}}=(C1 y)
        {
            System.Console.Write("checked");
        }
    }
}

public {{{typeKind}}} C1;

""" + CompilerFeatureRequiredAttribute;

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 {{{op}}}= c1;

        checked
        {
            _ = c1 {{{op}}}= c1;
        }
    }
}
""";

            var comp1 = CreateCompilation([src1, src2], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "regularchecked").VerifyDiagnostics();

            var comp2 = CreateCompilation([src1, src2], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            CompileAndVerify(comp2, expectedOutput: "regularchecked").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyEmitDiagnostics(
                // (6,13): error CS0019: Operator '-=' cannot be applied to operands of type 'C1' and 'C1'
                //         _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 " + op + "= c1").WithArguments(op + "=", "C1", "C1").WithLocation(6, 13),
                // (10,17): error CS0019: Operator '-=' cannot be applied to operands of type 'C1' and 'C1'
                //             _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 " + op + "= c1").WithArguments(op + "=", "C1", "C1").WithLocation(10, 17)
                );
        }

        [Fact]
        public void CompoundAssignment_061_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension(C1)
    {
        public static C1 operator checked -(C1 x, C1 y)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_062_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator -=(C1 y)
        {
            System.Console.Write("regular");
        }
    }
    extension(C1 x)
    {
        public void operator checked -=(C1 y)
        {
            System.Console.Write("checked");
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_063_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            return x;
        }

        public static C1 operator checked -(C1 x, C1 y)
        {
            return x;
        }
    }
}

public static class Extensions2
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            return x;
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
#if DEBUG
            comp.VerifyEmitDiagnostics(
                // (35,16): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions2.extension(C1).operator -(C1, C1)' and 'Extensions1.extension(C1).operator -(C1, C1)'
                //         _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-=").WithArguments("Extensions2.extension(C1).operator -(C1, C1)", "Extensions1.extension(C1).operator -(C1, C1)").WithLocation(35, 16),
                // (39,20): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked -(C1, C1)' and 'Extensions2.extension(C1).operator -(C1, C1)'
                //             _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-=").WithArguments("Extensions1.extension(C1).operator checked -(C1, C1)", "Extensions2.extension(C1).operator -(C1, C1)").WithLocation(39, 20)
                );
#else
            comp.VerifyEmitDiagnostics(
                // (35,16): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator -(C1, C1)' and 'Extensions2.extension(C1).operator -(C1, C1)'
                //         _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-=").WithArguments("Extensions1.extension(C1).operator -(C1, C1)", "Extensions2.extension(C1).operator -(C1, C1)").WithLocation(35, 16),
                // (39,20): error CS9342: Operator resolution is ambiguous between the following members: 'Extensions1.extension(C1).operator checked -(C1, C1)' and 'Extensions2.extension(C1).operator -(C1, C1)'
                //             _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "-=").WithArguments("Extensions1.extension(C1).operator checked -(C1, C1)", "Extensions2.extension(C1).operator -(C1, C1)").WithLocation(39, 20)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Last();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("Extensions1.extension(C1).operator checked -(C1, C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(C1).operator -(C1, C1)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78968")]
        public void CompoundAssignment_064_Consumption_Checked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator -=(C1 y)
        {
        }

        public void operator checked -=(C1 y)
        {
        }
    }
}

public static class Extensions2
{
    extension(C1 x)
    {
        public void operator -=(C1 y)
        {
        }
    }
}

public class C1;

class Program
{
    static void Main()
    {
        var c1 = new C1();
#line 35
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);

#if DEBUG // Collection of extension blocks depends on GetTypeMembersUnordered for namespace, which conditionally de-orders types for DEBUG only.
            comp.VerifyEmitDiagnostics(
                // (35,16): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions2.extension(C1).operator -=(C1)' and 'Extensions1.extension(C1).operator -=(C1)'
                //         _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("Extensions2.extension(C1).operator -=(C1)", "Extensions1.extension(C1).operator -=(C1)").WithLocation(35, 16),
                // (39,20): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(C1).operator checked -=(C1)' and 'Extensions2.extension(C1).operator -=(C1)'
                //             _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("Extensions1.extension(C1).operator checked -=(C1)", "Extensions2.extension(C1).operator -=(C1)").WithLocation(39, 20)
                );
#else
            // Ordering difference is acceptable and doesn't affect determinism. It is caused by ConditionallyDeOrder
            comp.VerifyEmitDiagnostics(
                // (35,16): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(C1).operator -=(C1)' and 'Extensions2.extension(C1).operator -=(C1)'
                //         _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("Extensions1.extension(C1).operator -=(C1)", "Extensions2.extension(C1).operator -=(C1)").WithLocation(35, 16),
                // (39,20): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(C1).operator checked -=(C1)' and 'Extensions2.extension(C1).operator -=(C1)'
                //             _ = c1 -= c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "-=").WithArguments("Extensions1.extension(C1).operator checked -=(C1)", "Extensions2.extension(C1).operator -=(C1)").WithLocation(39, 20)
                );
#endif

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Last();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            AssertEx.Equal("Extensions1.extension(C1).operator checked -=(C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions2.extension(C1).operator -=(C1)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_065_Consumption_CheckedLiftedIsWorse()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator -(S1 x, S1 y) => throw null;
        public static S1 operator checked -(S1 x, S1 y) => throw null;
    }
    extension(S1?)
    {
        public static S1? operator -(S1? x, S1? y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 -= s1;
        System.Console.Write(":");

        checked
        {
            _ = s1 -= s1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_066_Consumption_CheckedNoLiftedForm()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator -=(int i) => throw null;
        public void operator checked -=(int i) => throw null;
    }
    extension(ref S1? x)
    {
        public void operator -=(int i)
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? s1 = new S1();
        _ = s1 -= 1;
        System.Console.Write(":");

        checked
        {
            _ = s1 -= 1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1:operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_067_Consumption_OverloadResolutionPlusRegularVsChecked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1)
    {
        public static C2 operator -(C1 x, C1 y)
        {
            System.Console.Write("C1");
            return (C2)x;
        }
        public static C2 operator checked -(C1 x, C1 y)
        {
            System.Console.Write("checkedC1");
            return (C2)x;
        }
    }
    extension(C2)
    {
        public static C2 operator -(C2 x, C2 y)
        {
            System.Console.Write("C2");
            return x;
        }
    }
}

public class C1;
public class C2 : C1;

class Program
{
    static void Main()
    {
        C1 c1 = new C2();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }

        var c2 = new C2();
        _ = c2 -= c2;

        checked
        {
            _ = c2 -= c2;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1checkedC1C2C2").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_068_Consumption_OverloadResolutionPlusRegularVsChecked()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(C1 x)
    {
        public void operator -=(C1 y)
        {
            System.Console.Write("C1");
        }
        public void operator checked -=(C1 y)
        {
            System.Console.Write("checkedC1");
        }
    }
    extension(C2 x)
    {
        public void operator -=(C2 y)
        {
            System.Console.Write("C2");
        }
    }
}

public class C1;
public class C2 : C1;
public class C3 : C1;

class Program
{
    static void Main()
    {
        var c3 = new C3();
        _ = c3 -= c3;

        checked
        {
            _ = c3 -= c3;
        }

        var c2 = new C2();
        _ = c2 -= c2;

        checked
        {
            _ = c2 -= c2;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "C1checkedC1C2C2").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_069_Consumption_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object)
    {
        public static object operator +(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 += s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_070_Consumption_OnObject()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object x)
    {
        public void operator +=(object y)
        {
            System.Console.Write("operator1");
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 += s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_071_Consumption_NotOnDynamic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object z)
    {
        public static object operator +(object x, object y)
        {
            System.Console.Write("operator1");
            return x;
        }

        public void operator +=(object y)
        {
            System.Console.Write("operator2");
        }
    }
}

class Program
{
    static void Main()
    {
        dynamic s1 = new object();
        var s2 = new object();
        try
        {
            _ = s1 + s2;
        }
        catch
        {
            System.Console.Write("exception1");
        }

        try
        {
            _ = s2 + s1;
        }
        catch
        {
            System.Console.Write("exception2");
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "exception1exception2").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_072_Consumption_WithLambda()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        public static S1 operator +(S1 x, System.Func<int> y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

public class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 += (() => 1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_073_Consumption_WithLambda()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        public void operator +=(System.Func<int> y)
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1
{}

public class Program
{
    static void Main()
    {
        S1 s1 = new S1();
        _ = s1 += (() => 1);
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_074_Consumption_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 z)
    {
        public static S1 operator +(S1 x, S2 y)
        {
            System.Console.Write("operator1");
            return x;
        }
        public static S1 operator +(S2 y, S1 x)
        {
            System.Console.Write("operator2");
            return x;
        }
        public void operator +=(S2 y)
        {
            System.Console.Write("operator3");
        }
    }
}

public struct S1
{}
public struct S2
{}

public class Program
{
    static void Main()
    {
        S1 s1 = new S1();
#line 28
        _ = s1 += new();
        _ = new() += s1;
        _ = new() += new();
        _ = s1 += default;
        _ = default += s1;
        _ = default += default;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (28,13): error CS8310: Operator '+=' cannot be applied to operand 'new()'
                //         _ = s1 += new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s1 += new()").WithArguments("+=", "new()").WithLocation(28, 13),
                // (29,13): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         _ = new() += s1;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new()").WithLocation(29, 13),
                // (30,13): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         _ = new() += new();
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new()").WithLocation(30, 13),
                // (31,13): error CS8310: Operator '+=' cannot be applied to operand 'default'
                //         _ = s1 += default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s1 += default").WithArguments("+=", "default").WithLocation(31, 13),
                // (32,13): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         _ = default += s1;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default").WithLocation(32, 13),
                // (33,13): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         _ = default += default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "default").WithLocation(33, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_075_Consumption_BadOperand()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(object y)
    {
        public void operator +=(int i)
        {
            System.Console.Write("operator2");
        }
    }
}

class Program
{
    static object P {get; set;}

    static void Main()
    {
        _ = P += 1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (18,13): error CS0019: Operator '+=' cannot be applied to operands of type 'object' and 'int'
                //         _ = P += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "P += 1").WithArguments("+=", "object", "int").WithLocation(18, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_076_Consumption_BadReceiver()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(__arglist)
    {
        public static object operator +(object x, object y)
        {
            return x;
        }
        public void operator +=(object y)
        {
        }
    }
}

class Program
{
    static void Main()
    {
        var s1 = new object();
        _ = s1 += s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9319: One of the parameters of a binary operator must be the extended type.
                //         public static object operator +(object x, object y)
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "+").WithLocation(5, 39),
                // (20,13): error CS0019: Operator '+=' cannot be applied to operands of type 'object' and 'object'
                //         _ = s1 += s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 += s1").WithArguments("+=", "object", "object").WithLocation(20, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_077_Consumption_Checked_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T1>(C1<T1>)
    {
        public static C1<T1> operator -(C1<T1> x, C1<T1> y)
        {
            System.Console.Write("regular");
            return x;
        }
    }
    extension<T2>(C1<T2>)
    {
        public static C1<T2> operator checked -(C1<T2> x, C1<T2> y)
        {
            System.Console.Write("checked");
            return x;
        }
    }
}

public class C1<T>;

class Program
{
    static void Main()
    {
        var c1 = new C1<int>();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_078_Consumption_Checked_Generic()
        {
            var src = $$$"""
public static class Extensions1
{
    extension<T1>(C1<T1> x)
    {
        public void operator -=(C1<T1> y)
        {
            System.Console.Write("regular");
        }
    }
    extension<T2>(C1<T2> x)
    {
        public void operator checked -=(C1<T2> y)
        {
            System.Console.Write("checked");
        }
    }
}

public class C1<T>;

class Program
{
    static void Main()
    {
        var c1 = new C1<int>();
        _ = c1 -= c1;

        checked
        {
            _ = c1 -= c1;
        }
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "regularchecked").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_079_Consumption_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(S1 s1)
    {
        public static S1 operator +(S1 x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => s1 += s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (22,54): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<System.Func<S1, S1>> ex = (s1) => s1 += s1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "s1 += s1").WithLocation(22, 54)
                );
        }

        [Fact]
        public void CompoundAssignment_080_Consumption_ExpressionTree()
        {
            var src = $$$"""
using System.Linq.Expressions;

public static class Extensions1
{
    extension(ref S1 s1)
    {
        public void operator +=(S1 y)
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        Expression<System.Func<S1, S1>> ex = (s1) => s1 += s1;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics(
                // (21,54): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<System.Func<S1, S1>> ex = (s1) => s1 += s1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "s1 += s1").WithLocation(21, 54)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_01
        /// </summary>
        [Fact]
        public void CompoundAssignment_081_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, C right) => right;
    public C M(C c, scoped C c1)
    {
#line 7
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, C right) => right;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c += c1").WithArguments("Extensions.extension(C).operator +(C, C)", "right").WithLocation(7, 9),
                // (7,14): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(7, 14),
                // (8,13): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c = c + c1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c + c1").WithArguments("Extensions.extension(C).operator +(C, C)", "right").WithLocation(8, 13),
                // (8,17): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = c + c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(8, 17),
                // (9,13): error CS8347: Cannot use a result of 'C.X(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c = X(c, c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c, c1)").WithArguments("C.X(C, C)", "right").WithLocation(9, 13),
                // (9,18): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = X(c, c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(9, 18)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_02
        /// </summary>
        [Fact]
        public void CompoundAssignment_082_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, C right) => right;
    public static C Y(C left) => left;
    public C M1(C c, scoped C c1)
    {
#line 8
        return Y(c += c1);
    }
    public C M2(C c, scoped C c1)
    {
        return Y(c = X(c, c1));
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, C right) => right;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,16): error CS8347: Cannot use a result of 'C.Y(C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Y(c += c1)").WithArguments("C.Y(C)", "left").WithLocation(8, 16),
                // (8,18): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "c += c1").WithArguments("Extensions.extension(C).operator +(C, C)", "right").WithLocation(8, 18),
                // (8,18): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "c += c1").WithArguments("Extensions.extension(C).operator +(C, C)", "right").WithLocation(8, 18),
                // (8,23): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(8, 23),
                // (8,23): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(8, 23),
                // (12,16): error CS8347: Cannot use a result of 'C.Y(C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Y(c = X(c, c1))").WithArguments("C.Y(C)", "left").WithLocation(12, 16),
                // (12,22): error CS8347: Cannot use a result of 'C.X(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c, c1)").WithArguments("C.X(C, C)", "right").WithLocation(12, 22),
                // (12,22): error CS8347: Cannot use a result of 'C.X(C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c, c1)").WithArguments("C.X(C, C)", "right").WithLocation(12, 22),
                // (12,27): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(12, 27),
                // (12,27): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(12, 27)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left
        /// </summary>
        [Fact]
        public void CompoundAssignment_083_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(scoped C left, C right) => right;
    public C M(C c, scoped C c1)
    {
#line 7
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(scoped C left, C right) => right;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (7,9): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(scoped C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c += c1").WithArguments("Extensions.extension(C).operator +(scoped C, C)", "right").WithLocation(7, 9),
                // (7,14): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(7, 14),
                // (8,13): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(scoped C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c = c + c1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c + c1").WithArguments("Extensions.extension(C).operator +(scoped C, C)", "right").WithLocation(8, 13),
                // (8,17): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = c + c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(8, 17),
                // (9,13): error CS8347: Cannot use a result of 'C.X(scoped C, C)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c = X(c, c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c, c1)").WithArguments("C.X(scoped C, C)", "right").WithLocation(9, 13),
                // (9,18): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c = X(c, c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(9, 18)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Right
        /// </summary>
        [Fact]
        public void CompoundAssignment_084_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, scoped C right) => left;
    public C M(C c, scoped C c1)
    {
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, scoped C right) => left;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Both
        /// </summary>
        [Fact]
        public void CompoundAssignment_085_RefSafety()
        {
            var source = """
static class Extensions
{
    extension(C)
    {
#line 3
        public static C operator +(scoped C left, scoped C right) => right;
    }
}

public ref struct C
{
#line 4
    public static C X(scoped C left, scoped C right) => right;
    public C M(C c, scoped C c1)
    {
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics(
                // (3,70): error CS8352: Cannot use variable 'scoped C right' in this context because it may expose referenced variables outside of their declaration scope
                //         public static C operator +(scoped C left, scoped C right) => right;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "right").WithArguments("scoped C right").WithLocation(3, 70),
                // (4,57): error CS8352: Cannot use variable 'scoped C right' in this context because it may expose referenced variables outside of their declaration scope
                //     public static C X(scoped C left, scoped C right) => right;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "right").WithArguments("scoped C right").WithLocation(4, 57)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_01
        /// </summary>
        [Fact]
        public void CompoundAssignment_086_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, C right) => right;
    public C M(scoped C c, C c1)
    {
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c1;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, C right) => right;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_02
        /// </summary>
        [Fact]
        public void CompoundAssignment_087_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, C right) => right;
    public static C Y(C left) => left;
    public C M1(scoped C c, C c1)
    {
#line 8
        return Y(c += c1);
    }
    public C M2(scoped C c, C c1)
    {
        return Y(c = X(c, c1));
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, C right) => right;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,16): error CS8347: Cannot use a result of 'C.Y(C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Y(c += c1)").WithArguments("C.Y(C)", "left").WithLocation(8, 16),
                // (8,18): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(8, 18),
                // (8,18): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(C, C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "c += c1").WithArguments("Extensions.extension(C).operator +(C, C)", "left").WithLocation(8, 18),
                // (12,16): error CS8347: Cannot use a result of 'C.Y(C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Y(c = X(c, c1))").WithArguments("C.Y(C)", "left").WithLocation(12, 16),
                // (12,22): error CS8347: Cannot use a result of 'C.X(C, C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c, c1)").WithArguments("C.X(C, C)", "left").WithLocation(12, 22),
                // (12,24): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(12, 24)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_03
        /// </summary>
        [Fact]
        public void CompoundAssignment_088_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, scoped C right) => left;
    public static C Y(C left) => left;
    public C M1(scoped C c, C c1)
    {
#line 8
        return Y(c += c1);
    }
    public C M2(scoped C c, C c1)
    {
        return Y(c = X(c, c1));
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, scoped C right) => left;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,16): error CS8347: Cannot use a result of 'C.Y(C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Y(c += c1)").WithArguments("C.Y(C)", "left").WithLocation(8, 16),
                // (8,18): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(8, 18),
                // (8,18): error CS8347: Cannot use a result of 'Extensions.extension(C).operator +(C, scoped C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "c += c1").WithArguments("Extensions.extension(C).operator +(C, scoped C)", "left").WithLocation(8, 18),
                // (12,16): error CS8347: Cannot use a result of 'C.Y(C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Y(c = X(c, c1))").WithArguments("C.Y(C)", "left").WithLocation(12, 16),
                // (12,22): error CS8347: Cannot use a result of 'C.X(C, scoped C)' in this context because it may expose variables referenced by parameter 'left' outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c, c1)").WithArguments("C.X(C, scoped C)", "left").WithLocation(12, 22),
                // (12,24): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return Y(c = X(c, c1));
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(12, 24)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_ScopedTarget_04
        /// </summary>
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79054")]
        public void CompoundAssignment_089_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(scoped C left, C right) => right;
    public static C Y(C left) => left;
    public C M1(scoped C c, C c1)
    {
        return Y(c += c1);
    }
    public C M2(scoped C c, C c1)
    {
#line 12
        return Y(c = X(c, c1));
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(scoped C left, C right) => right; 
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Left_ScopedTarget
        /// </summary>
        [Fact]
        public void CompoundAssignment_090_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(scoped C left, C right) => right;
    public C M(scoped C c, C c1)
    {
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c1;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(scoped C left, C right) => right;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Right_ScopedTarget
        /// </summary>
        [Fact]
        public void CompoundAssignment_091_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(C left, scoped C right) => left;
    public C M(scoped C c, C c1)
    {
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c1;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(C left, scoped C right) => left;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_Scoped_Both_ScopedTarget
        /// </summary>
        [Fact]
        public void CompoundAssignment_092_RefSafety()
        {
            var source = """
public ref struct C
{
    public static C X(scoped C left, scoped C right) => throw null;
    public C M(scoped C c, C c1)
    {
        c += c1;
        c = c + c1;
        c = X(c, c1);
        return c1;
    }
}

static class Extensions
{
    extension(C)
    {
        public static C operator +(scoped C left, scoped C right) => throw null;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_RegressionTest1
        /// </summary>
        [Fact]
        public void CompoundAssignment_093_RefSafety()
        {
            var source = """
using System;

public ref struct S1
{
    public S1(Span<int> span) { }

    static void Test()
    {
        S1 stackLocal = new S1(stackalloc int[1]);
        S1 heapLocal = new S1(default);

        stackLocal += stackLocal;
        stackLocal += heapLocal;
        heapLocal += heapLocal;
#line 16
        heapLocal += stackLocal; // 1

    }
}

static class Extensions
{
    extension(S1)
    {
        public static S1 operator +(S1 a, S1 b) => default;
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (16,9): error CS8347: Cannot use a result of 'Extensions.extension(S1).operator +(S1, S1)' in this context because it may expose variables referenced by parameter 'b' outside of their declaration scope
                //         heapLocal += stackLocal; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "heapLocal += stackLocal").WithArguments("Extensions.extension(S1).operator +(S1, S1)", "b").WithLocation(16, 9),
                // (16,22): error CS8352: Cannot use variable 'stackLocal' in this context because it may expose referenced variables outside of their declaration scope
                //         heapLocal += stackLocal; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "stackLocal").WithArguments("stackLocal").WithLocation(16, 22)
                );
        }

        /// <summary>
        /// This is a clone of Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics.RefEscapingTests.UserDefinedBinaryOperator_RefStruct_Compound_RegressionTest2
        /// </summary>
        [Fact]
        public void CompoundAssignment_094_RefSafety()
        {
            var source = """
using System;

public ref struct S1
{
    public S1(Span<int> span) { }

    static void Test()
    {
        S1 stackLocal = new(stackalloc int[1]);
        S1 heapLocal = new(default);

        stackLocal += stackLocal;
        stackLocal += heapLocal;
        heapLocal += heapLocal;
#line 16
        heapLocal += stackLocal; // 1

    }
}

public ref struct S2
{
    public S2(Span<int> span) { }

    static void Test()
    {
        S2 stackLocal = new(stackalloc int[1]);
        S2 heapLocal = new(default);

        stackLocal += stackLocal;
        stackLocal += heapLocal;
        heapLocal += heapLocal;
        heapLocal += stackLocal;
    }
}

public ref struct S3
{
    public S3(Span<int> span) { }

    static void Test()
    {
        S3 stackLocal = new(stackalloc int[1]);
        S3 heapLocal = new(default);

        stackLocal += stackLocal;
        stackLocal += heapLocal;
#line 50
        heapLocal += heapLocal; // 2
        heapLocal += stackLocal;  // 3
    }
}

public ref struct S4
{
    public S4(Span<int> span) { }

    static void Test()
    {
        S4 stackLocal = new(stackalloc int[1]);
        S4 heapLocal = new(default);

        stackLocal += stackLocal;
        stackLocal += heapLocal;
        heapLocal += heapLocal;
#line 68
        heapLocal += stackLocal; // 4
    }
}

static class Extensions
{
    extension(S1)
    {
        public static S1 operator +(S1 a, S1 b) => default;
    }
    extension(S2)
    {
        public static S2 operator +(S2 a, scoped S2 b) => default;
    }
    extension(S3)
    {
        public static S3 operator +(in S3 a, in S3 b) => default;
    }
    extension(S4)
    {
        public static S4 operator +(scoped in S4 a, scoped in S4 b) => default;
    }
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (16,9): error CS8347: Cannot use a result of 'Extensions.extension(S1).operator +(S1, S1)' in this context because it may expose variables referenced by parameter 'b' outside of their declaration scope
                //         heapLocal += stackLocal; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "heapLocal += stackLocal").WithArguments("Extensions.extension(S1).operator +(S1, S1)", "b").WithLocation(16, 9),
                // (16,22): error CS8352: Cannot use variable 'stackLocal' in this context because it may expose referenced variables outside of their declaration scope
                //         heapLocal += stackLocal; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "stackLocal").WithArguments("stackLocal").WithLocation(16, 22),
                // (50,9): error CS8168: Cannot return local 'heapLocal' by reference because it is not a ref local
                //         heapLocal += heapLocal; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "heapLocal").WithArguments("heapLocal").WithLocation(50, 9),
                // (50,9): error CS8347: Cannot use a result of 'Extensions.extension(S3).operator +(in S3, in S3)' in this context because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         heapLocal += heapLocal; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "heapLocal += heapLocal").WithArguments("Extensions.extension(S3).operator +(in S3, in S3)", "a").WithLocation(50, 9),
                // (51,9): error CS8168: Cannot return local 'heapLocal' by reference because it is not a ref local
                //         heapLocal += stackLocal;  // 3
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "heapLocal").WithArguments("heapLocal").WithLocation(51, 9),
                // (51,9): error CS8347: Cannot use a result of 'Extensions.extension(S3).operator +(in S3, in S3)' in this context because it may expose variables referenced by parameter 'a' outside of their declaration scope
                //         heapLocal += stackLocal;  // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "heapLocal += stackLocal").WithArguments("Extensions.extension(S3).operator +(in S3, in S3)", "a").WithLocation(51, 9),
                // (68,9): error CS8347: Cannot use a result of 'Extensions.extension(S4).operator +(scoped in S4, scoped in S4)' in this context because it may expose variables referenced by parameter 'b' outside of their declaration scope
                //         heapLocal += stackLocal; // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "heapLocal += stackLocal").WithArguments("Extensions.extension(S4).operator +(scoped in S4, scoped in S4)", "b").WithLocation(68, 9),
                // (68,22): error CS8352: Cannot use variable 'stackLocal' in this context because it may expose referenced variables outside of their declaration scope
                //         heapLocal += stackLocal; // 4
                Diagnostic(ErrorCode.ERR_EscapeVariable, "stackLocal").WithArguments("stackLocal").WithLocation(68, 22)
                );
        }

        [Fact]
        public void CompoundAssignment_095_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X(C right) {}
    public C M1(C c, C c1)
    {
        c += c1;
        return c;
    }
    public C M2(C c, C c1)
    {
        c.X(c1);
        return c;
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_096_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X(C right) {}
    public C M1(C c, scoped C c1)
    {
#line 7
        c += c1;
        return c;
    }
    public C M2(C c, scoped C c1)
    {
        c.X(c1);
        return c;
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (7,9): error CS8350: This combination of arguments to 'Extensions.extension(scoped ref C).operator +=(C)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c += c1").WithArguments("Extensions.extension(scoped ref C).operator +=(C)", "right").WithLocation(7, 9),
                // (7,14): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(7, 14),
                // (12,9): error CS8350: This combination of arguments to 'C.X(C)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c.X(c1);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c.X(c1)").WithArguments("C.X(C)", "right").WithLocation(12, 9),
                // (12,13): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         c.X(c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(12, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_097_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X(C right) {}
    public C M1(scoped C c, C c1)
    {
        c += c1;
#line 8
        return c;
    }
    public C M2(scoped C c, C c1)
    {
        c.X(c1);
        return c;
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (8,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(8, 16),
                // (13,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(13, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_098_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X(C right) {}
    public C M1(scoped C c, scoped C c1)
    {
        c += c1;
#line 8
        return c;
    }
    public C M2(scoped C c, scoped C c1)
    {
        c.X(c1);
        return c;
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (8,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(8, 16),
                // (13,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(13, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_099_RefSafety()
        {
            var source = """
public ref struct C
{
    public void X(scoped C right) {}
    public C M1(C c, scoped C c1)
    {
        c += c1;
        return c;
    }
    public C M2(C c, scoped C c1)
    {
        c.X(c1);
        return c;
    }
}

static class Extensions
{
    extension(scoped ref C left)
    {
        public void operator +=(scoped C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_100_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M1(C c, C c1)
    {
        return c += c1;
    }
}

static class Extensions
{
    extension(ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_101_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M1(scoped C c, C c1)
    {
#line 6
        return c += c1;
    }
}

static class Extensions
{
    extension(ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (6,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(6, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_102_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M1(C c, scoped C c1)
    {
#line 6
        return c += c1;
    }
}

static class Extensions
{
    extension(ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (6,16): error CS8350: This combination of arguments to 'Extensions.extension(ref C).operator +=(C)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c += c1;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c += c1").WithArguments("Extensions.extension(ref C).operator +=(C)", "right").WithLocation(6, 16),
                // (6,21): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         return c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(6, 21)
                );
        }

        [Fact]
        public void CompoundAssignment_103_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M1(C c, scoped C c1)
    {
        return c += c1;
    }
}

static class Extensions
{
    extension(ref C left)
    {
        public void operator +=(scoped C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_104_RefSafety()
        {
            var source = """
public ref struct C
{
    public C M1(scoped C c, scoped C c1)
    {
#line 6
        return c += c1;
    }
}

static class Extensions
{
    extension(ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (6,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(6, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_105_RefSafety()
        {
            var source = """
public ref struct C
{
    static C X(C c) => throw null;
    public C M1(scoped C c, scoped C c1)
    {
#line 7
        return X(c += c1);
    }
}

static class Extensions
{
    extension(ref C left)
    {
        public void operator +=(C right) {}
    }
}
""";
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (7,16): error CS8347: Cannot use a result of 'C.X(C)' in this context because it may expose variables referenced by parameter 'c' outside of their declaration scope
                //         return X(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "X(c += c1)").WithArguments("C.X(C)", "c").WithLocation(7, 16),
                // (7,18): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return X(c += c1);
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(7, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_106_RefSafety()
        {
            var source = $$$"""
ref struct C
{
    public C M1(C c1)
    {
#line 8
        return c1 += 1;
    }
    public C M2(C c2)
    {
        return c2 -= 1;
    }
    public C M3(C c3, in int right)
    {
        return c3 += right;
    }
    public C M4(C c4, in int right)
    {
        return c4 -= right;
    }
}

static class Extensions
{
    extension(ref C x)
    {
        public void operator +=([System.Diagnostics.CodeAnalysis.UnscopedRef] in int right) {}
        public static C operator -(C left, [System.Diagnostics.CodeAnalysis.UnscopedRef] in int right) => throw null;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
                // (8,16): error CS8350: This combination of arguments to 'Extensions.extension(ref C).operator +=(in int)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c1 += 1;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c1 += 1").WithArguments("Extensions.extension(ref C).operator +=(in int)", "right").WithLocation(8, 16),
                // (8,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c1 += 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(8, 22),
                // (12,16): error CS8347: Cannot use a result of 'Extensions.extension(ref C).operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("Extensions.extension(ref C).operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'Extensions.extension(ref C).operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("Extensions.extension(ref C).operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (16,16): error CS8350: This combination of arguments to 'Extensions.extension(ref C).operator +=(in int)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c3 += right;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c3 += right").WithArguments("Extensions.extension(ref C).operator +=(in int)", "right").WithLocation(16, 16),
                // (16,22): error CS9077: Cannot return a parameter by reference 'right' through a ref parameter; it can only be returned in a return statement
                //         return c3 += right;
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "right").WithArguments("right").WithLocation(16, 22),
                // (20,16): error CS8347: Cannot use a result of 'Extensions.extension(ref C).operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c4 -= right").WithArguments("Extensions.extension(ref C).operator -(C, in int)", "right").WithLocation(20, 16),
                // (20,22): error CS9077: Cannot return a parameter by reference 'right' through a ref parameter; it can only be returned in a return statement
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "right").WithArguments("right").WithLocation(20, 22)
                );
        }

        [Fact]
        public void CompoundAssignment_107_RefSafety()
        {
            var source = """
ref struct C
{
    public C M1(C c1)
    {
        return c1 += 1;
    }
    public C M2(C c2)
    {
#line 12
        return c2 -= 1;
    }
    public C M3(C c3, in int right)
    {
        return c3 += right;
    }
    public C M4(C c4, in int right)
    {
        return c4 -= right;
    }
}

static class Extensions
{
    extension(ref C x)
    {
        public void operator +=(in int right) {}
        public static C operator -(C left, in int right) => throw null;
    }
}
""";
            CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
                // (12,16): error CS8347: Cannot use a result of 'Extensions.extension(ref C).operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("Extensions.extension(ref C).operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'Extensions.extension(ref C).operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("Extensions.extension(ref C).operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (20,16): error CS8347: Cannot use a result of 'Extensions.extension(ref C).operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c4 -= right").WithArguments("Extensions.extension(ref C).operator -(C, in int)", "right").WithLocation(20, 16),
                // (20,22): error CS9077: Cannot return a parameter by reference 'right' through a ref parameter; it can only be returned in a return statement
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "right").WithArguments("right").WithLocation(20, 22)
                );
        }

        [Fact]
        public void CompoundAssignment_108_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1 operator -(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }

    extension(C2)
    {
        public static C2 operator -(C2? x, C2? y)
        {
            System.Console.Write("operator2");
            return new C2();
        }
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x1 = null;
        C1? x2 = null;
        C1 y = new C1();
#line 25
        _ = x1 -= y;
        y = y -= x2;

        C2? z = null;
        _ = z -= z;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (25,13): warning CS8604: Possible null reference argument for parameter 'x' in 'C1 Extensions1.extension(C1).operator -(C1 x, C1 y)'.
                //         _ = x1 -= y;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "C1 Extensions1.extension(C1).operator -(C1 x, C1 y)").WithLocation(25, 13),
                // (26,18): warning CS8604: Possible null reference argument for parameter 'y' in 'C1 Extensions1.extension(C1).operator -(C1 x, C1 y)'.
                //         y = y -= x2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("y", "C1 Extensions1.extension(C1).operator -(C1 x, C1 y)").WithLocation(26, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_109_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1)
    {
        public static C1? operator -(C1 x, C1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public class C1
{}

class Program
{
    static void Main()
    {
        var x = new C1();
        C1 y = x -= x;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,16): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         C1 y = x -= x;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "x -= x").WithLocation(23, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_110_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(T x, T y)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x1 = null;
        C2? x2 = null;
        var y = new C2();
        (x1 -= y).ToString();
        (y -= x2).ToString();
        y.ToString();
        var z = new C2();
        (z -= z).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,10): warning CS8602: Dereference of a possibly null reference.
                //         (x1 -= y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1 -= y").WithLocation(24, 10),
                // (25,10): warning CS8602: Dereference of a possibly null reference.
                //         (y -= x2).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y -= x2").WithLocation(25, 10),
                // (26,9): warning CS8602: Dereference of a possibly null reference.
                //         y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y").WithLocation(26, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ToArray();

            Assert.Equal(3, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator -(C2?, C2?)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator -(C2?, C2?)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator -(C2, C2)", model.GetSymbolInfo(opNodes[2]).Symbol.ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_111_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T)
    {
        public static T operator -(T x, int y)
        {
            return x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (x -= 1).ToString();
        (y -= 1).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,10): warning CS8602: Dereference of a possibly null reference.
                //         (x -= 1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x -= 1").WithLocation(23, 10)
                );
        }

        [Fact]
        public void CompoundAssignment_112_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T, S>(C1<T>) where T : new() where S : new()
    {
        public static C1<T> operator -(C1<T> x, C1<S> y)
        {
            return x;
        }
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        var y = Get(new C2());

        (x -= y).F.ToString();
        (y -= x).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,9): warning CS8602: Dereference of a possibly null reference.
                //         (x -= y).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(x -= y).F").WithLocation(29, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?, C2>(C1<C2?>).operator -(C1<C2?>, C1<C2>)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2, C2?>(C1<C2>).operator -(C1<C2>, C1<C2?>)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_113_NullableAnalysis_Lifted()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(S1<T>) where T : new()
    {
        public static S1<T> operator -(S1<T> x, int y)
        {
            return x;
        }
    }
}

public struct S1<T> where T : new()
{
    public T F;
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null) -= 1;

        if (x != null)
            x.Value.F.ToString();

        var y = Get(new C2()) -= 1;

        if (y != null)
            y.Value.F.ToString();
    }

    static ref S1<T>? Get<T>(T x) where T : new()
    {
        throw null!;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,13): warning CS8602: Dereference of a possibly null reference.
                //             x.Value.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x.Value.F").WithLocation(29, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_114_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T) where T : notnull
    {
        public static C2 operator -(T x, T y)
        {
            return (C2)(object)x;
        }
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        var y = new C2();
        (x -= y).ToString();
        x = null;
#line 24
        (y -= x).ToString();

        var z = new C2();
        (z -= z).ToString();
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (23,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x -= y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x -= y").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(23, 10),
                // (24,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (y -= x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "y -= x").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(24, 10)
                );
        }

        [Fact]
        public void CompoundAssignment_115_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1 x)
    {
        public void operator -=(C1 y) {}
    }

    extension(C2? x)
    {
        public void operator -=(C2? y) {}
    }
}

public class C1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        C1? x1 = null;
        C1? x2 = null;
        C1 y = new C1();
#line 25
        _ = x1 -= y;
        y = y -= x2;

        C2? z = null;
        _ = z -= z;

        C2 a = new C2();
        C2? b = null;
        C2 c = a -= b;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (25,13): warning CS8604: Possible null reference argument for parameter 'x' in 'Extensions1.extension(C1)'.
                //         _ = x1 -= y;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithArguments("x", "Extensions1.extension(C1)").WithLocation(25, 13),
                // (26,18): warning CS8604: Possible null reference argument for parameter 'y' in 'void Extensions1.extension(C1).operator -=(C1 y)'.
                //         y = y -= x2;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithArguments("y", "void Extensions1.extension(C1).operator -=(C1 y)").WithLocation(26, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_116_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

using System.Diagnostics.CodeAnalysis;

public static class Extensions1
{
    extension([NotNull] ref S1? x)
    {
        public void operator -=(int y) { throw null!; }
    }

    extension([NotNull] C2? x)
    {
        public void operator -=(int y) { throw null!; }
    }
}

public struct S1
{}

public class C2
{}

class Program
{
    static void Main()
    {
        S1? x = null;
        var x1 = x -= 1;
        _ = x.Value;
        _ = x1.Value;

        C2? z = null;
        var z1 = z -= 1;
        z.ToString();
        z1.ToString();
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_117_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(ref S1? x)
    {
        public void operator -=(int y) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        S1? x = null;
        var x1 = x -= 1;
        _ = x.Value;
        _ = x1.Value;

        S1? y = new S1();
        var y1 = y -= 1;
        _ = y.Value;
        _ = y1.Value;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (20,13): warning CS8629: Nullable value type may be null.
                //         _ = x.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "x").WithLocation(20, 13),
                // (21,13): warning CS8629: Nullable value type may be null.
                //         _ = x1.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "x1").WithLocation(21, 13),
                // (25,13): warning CS8629: Nullable value type may be null.
                //         _ = y.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "y").WithLocation(25, 13),
                // (26,13): warning CS8629: Nullable value type may be null.
                //         _ = y1.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "y1").WithLocation(26, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_118_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(ref S1? x)
    {
        public void operator -=(int y) {}
    }
}

public struct S1
{}

class Program
{
    static void Main()
    {
        _ = Get(new S1()).Value;
        var y1 = Get(new S1()) -= 1;
#line 26
        _ = y1.Value;
    }

    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("x")]
    static ref S1? Get(S1? x) => throw null!;
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (26,13): warning CS8629: Nullable value type may be null.
                //         _ = y1.Value;
                Diagnostic(ErrorCode.WRN_NullableValueTypeMayBeNull, "y1").WithLocation(26, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_119_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C1Base x)
    {
        public void operator -=(int y) {}
    }

    extension(C2Base? x)
    {
        public void operator -=(int y) {}
    }
}

public class C1Base {}
public class C1 : C1Base {}

public class C2Base {}
public class C2 : C2Base {}

class Program
{
    static void Main()
    {
        C1? x = null;
        var x1 = x -= 1;
        x.ToString();
        x1.ToString();

        C1 y = new C1();
        var y1 = y -= 1;
        y.ToString();
        y1.ToString();

        C2? z = null;
        var z1 = z -= 1;
        z.ToString();
        z1.ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,18): warning CS8604: Possible null reference argument for parameter 'x' in 'Extensions1.extension(C1Base)'.
                //         var x1 = x -= 1;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "Extensions1.extension(C1Base)").WithLocation(27, 18),
                // (38,9): warning CS8602: Dereference of a possibly null reference.
                //         z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z").WithLocation(38, 9),
                // (39,9): warning CS8602: Dereference of a possibly null reference.
                //         z1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "z1").WithLocation(39, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_120_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension(C2Base<string> x)
    {
        public void operator -=(int y) {}
    }
}

public class C2Base<T> {}
public class C2<T> : C2Base<T> {}

class Program
{
    static void Main()
    {
        C2<string?> z = new C2<string?>();
        var z1 = z -= 1;
        z.ToString();
        z1.ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (19,18): warning CS8620: Argument of type 'C2<string?>' cannot be used for parameter 'x' of type 'C2Base<string>' in 'Extensions1.extension(C2Base<string>)' due to differences in the nullability of reference types.
                //         var z1 = z -= 1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("C2<string?>", "C2Base<string>", "x", "Extensions1.extension(C2Base<string>)").WithLocation(19, 18)
                );
        }

        [Fact]
        public void CompoundAssignment_121_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

using System.Diagnostics.CodeAnalysis;

public static class Extensions1
{
    extension([NotNull] C2Base? x)
    {
        public void operator -=(int y) { throw null!; }
    }
}

public class C2Base
{}

public class C2 : C2Base
{}

class Program
{
    static void Main()
    {
        C2? z = null;
        var z1 = z -= 1;
        z.ToString();
        z1.ToString();
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_122_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T x) where T : class?
    {
        public void operator -=(int y) {}
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (x -= 1).ToString();
        var y = new C2();
        (y -= 1).ToString();
    }
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (19,10): warning CS8602: Dereference of a possibly null reference.
                //         (x -= 1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x -= 1").WithLocation(19, 10)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C2?).operator -=(int)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C2).operator -=(int)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_123_NullableAnalysis()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T> x) where T : new()
    {
        public void operator -=(int y) {}
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (x -= 1).F.ToString();
        var y = Get(new C2());
        (y -= 1).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,9): warning CS8602: Dereference of a possibly null reference.
                //         (x -= 1).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(x -= 1).F").WithLocation(24, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNodes = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().ToArray();

            Assert.Equal(2, opNodes.Length);
            AssertEx.Equal("Extensions1.extension<C2?>(C1<C2?>).operator -=(int)", model.GetSymbolInfo(opNodes[0]).Symbol.ToDisplayString());
            AssertEx.Equal("Extensions1.extension<C2>(C1<C2>).operator -=(int)", model.GetSymbolInfo(opNodes[1]).Symbol.ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_124_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(T x) where T : class
    {
        public void operator -=(int y) {}
    }
}

public class C2
{}

class Program
{
    static void Main()
    {
        C2? x = null;
        (x -= 1).ToString();
        var y = new C2();
        (y -= 1).ToString();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (19,10): warning CS8634: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(T)'. Nullability of type argument 'C2?' doesn't match 'class' constraint.
                //         (x -= 1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "x -= 1").WithArguments("Extensions1.extension<T>(T)", "T", "C2?").WithLocation(19, 10),
                // (19,10): warning CS8602: Dereference of a possibly null reference.
                //         (x -= 1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x -= 1").WithLocation(19, 10)
                );
        }

        [Fact]
        public void CompoundAssignment_125_NullableAnalysis_Constraints()
        {
            var src = $$$"""
#nullable enable

public static class Extensions1
{
    extension<T>(C1<T> x) where T : notnull, new()
    {
        public void operator -=(int y) {}
    }
}

public class C1<T> where T : new()
{
    public T F = new T();
}

public class C2
{}

class Program
{
    static void Main()
    {
        var x = Get((C2?)null);
        (x -= 1).F.ToString();
        var y = Get(new C2());
        (y -= 1).F.ToString();
    }

    static C1<T> Get<T>(T x) where T : new()
    {
        return new C1<T>();
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (24,9): warning CS8602: Dereference of a possibly null reference.
                //         (x -= 1).F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "(x -= 1).F").WithLocation(24, 9),
                // (24,10): warning CS8714: The type 'C2?' cannot be used as type parameter 'T' in the generic type or method 'Extensions1.extension<T>(C1<T>)'. Nullability of type argument 'C2?' doesn't match 'notnull' constraint.
                //         (x -= 1).F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "x -= 1").WithArguments("Extensions1.extension<T>(C1<T>)", "T", "C2?").WithLocation(24, 10)
                );
        }

        [Fact]
        public void CompoundAssignment_126_Declaration_Extern()
        {
            var src = $$$"""
using System.Runtime.InteropServices;

public static class Extensions1
{
    extension(C2 x)
    {
        extern public void operator -=(C2 y) {}

        [DllImport("something.dll")]
        public void operator +=(C2 y) {}
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,37): error CS0179: 'Extensions1.extension(C2).operator -=(C2)' cannot be extern and declare a body
                //         extern public void operator -=(C2 y) {}
                Diagnostic(ErrorCode.ERR_ExternHasBody, "-=").WithArguments("Extensions1.extension(C2).operator -=(C2)").WithLocation(7, 37),
                // (9,10): error CS0601: The DllImport attribute must be specified on a method marked 'extern' that is either 'static' or an extension member
                //         [DllImport("something.dll")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(9, 10)
                );
        }

        [Fact]
        public void CompoundAssignment_127_Declaration_Extern()
        {
            var source = """
using System.Runtime.InteropServices;
static class E
{
    extension(C2 x)
    {
        [DllImport("something.dll")]
        extern public void operator -=(C2 y);
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A5B9DA57687B6EBB6576FC573B145969'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 x
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20b5
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A5B9DA57687B6EBB6576FC573B145969'::'<Extension>$'
        } // end of class <M>$A5B9DA57687B6EBB6576FC573B145969
        // Methods
        .method public hidebysig specialname 
            instance void op_SubtractionAssignment (
                class C2 y
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                01 00 26 55 73 65 72 44 65 66 69 6e 65 64 43 6f
                6d 70 6f 75 6e 64 41 73 73 69 67 6e 6d 65 6e 74
                4f 70 65 72 61 74 6f 72 73 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 35 42 39 44 41 35 37 36
                38 37 42 36 45 42 42 36 35 37 36 46 43 35 37 33
                42 31 34 35 39 36 39 00 00
            )
            // Method begins at RVA 0x20ae
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_SubtractionAssignment
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static pinvokeimpl("something.dll" winapi) 
        void op_SubtractionAssignment (
            class C2 x,
            class C2 y
        ) cil managed preservesig 
    {
    } // end of method E::op_SubtractionAssignment
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void CompoundAssignment_128_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2 x)
    {
        extern public void operator -=(C2 y);
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var verifier = CompileAndVerify(source, verify: Verification.FailsPEVerify with { PEVerifyMessage = """
                Error: Method marked Abstract, Runtime, InternalCall or Imported must have zero RVA, and vice versa.
                Type load failed.
                """ });

            verifier.VerifyDiagnostics(
                // (5,37): warning CS0626: Method, operator, or accessor 'E.extension(C2).operator -=(C2)' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                //         extern public void operator -=(C2 y);
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "-=").WithArguments("E.extension(C2).operator -=(C2)").WithLocation(5, 37)
                );

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A5B9DA57687B6EBB6576FC573B145969'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 x
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20b5
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A5B9DA57687B6EBB6576FC573B145969'::'<Extension>$'
        } // end of class <M>$A5B9DA57687B6EBB6576FC573B145969
        // Methods
        .method public hidebysig specialname 
            instance void op_SubtractionAssignment (
                class C2 y
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                01 00 26 55 73 65 72 44 65 66 69 6e 65 64 43 6f
                6d 70 6f 75 6e 64 41 73 73 69 67 6e 6d 65 6e 74
                4f 70 65 72 61 74 6f 72 73 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 35 42 39 44 41 35 37 36
                38 37 42 36 45 42 42 36 35 37 36 46 43 35 37 33
                42 31 34 35 39 36 39 00 00
            )
            // Method begins at RVA 0x20ae
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_SubtractionAssignment
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        void op_SubtractionAssignment (
            class C2 x,
            class C2 y
        ) cil managed 
    {
    } // end of method E::op_SubtractionAssignment
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void CompoundAssignment_129_Declaration_Extern()
        {
            var source = """
static class E
{
    extension(C2 x)
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        extern public void operator -=(C2 y);
    }
}

public class C2
{}

""" + CompilerFeatureRequiredAttribute;

            var verifier = CompileAndVerify(source).VerifyDiagnostics();

            verifier.VerifyTypeIL("E", """
.class private auto ansi abstract sealed beforefieldinit E
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    // Nested Types
    .class nested public auto ansi sealed specialname '<G>$3D0C2090833F9460B6F186EEC21CE3B0'
        extends [mscorlib]System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        // Nested Types
        .class nested public auto ansi abstract sealed specialname '<M>$A5B9DA57687B6EBB6576FC573B145969'
            extends [mscorlib]System.Object
        {
            // Methods
            .method public hidebysig specialname static 
                void '<Extension>$' (
                    class C2 x
                ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                // Method begins at RVA 0x20b5
                // Code size 1 (0x1)
                .maxstack 8
                IL_0000: ret
            } // end of method '<M>$A5B9DA57687B6EBB6576FC573B145969'::'<Extension>$'
        } // end of class <M>$A5B9DA57687B6EBB6576FC573B145969
        // Methods
        .method public hidebysig specialname 
            instance void op_SubtractionAssignment (
                class C2 y
            ) cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                01 00 26 55 73 65 72 44 65 66 69 6e 65 64 43 6f
                6d 70 6f 75 6e 64 41 73 73 69 67 6e 6d 65 6e 74
                4f 70 65 72 61 74 6f 72 73 00 00
            )
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 41 35 42 39 44 41 35 37 36
                38 37 42 36 45 42 42 36 35 37 36 46 43 35 37 33
                42 31 34 35 39 36 39 00 00
            )
            // Method begins at RVA 0x20ae
            // Code size 6 (0x6)
            .maxstack 8
            IL_0000: newobj instance void [mscorlib]System.NotSupportedException::.ctor()
            IL_0005: throw
        } // end of method '<G>$3D0C2090833F9460B6F186EEC21CE3B0'::op_SubtractionAssignment
    } // end of class <G>$3D0C2090833F9460B6F186EEC21CE3B0
    // Methods
    .method public hidebysig static 
        void op_SubtractionAssignment (
            class C2 x,
            class C2 y
        ) cil managed internalcall 
    {
    } // end of method E::op_SubtractionAssignment
} // end of class E
""".Replace("[mscorlib]", ExecutionConditionUtil.IsMonoOrCoreClr ? "[netstandard]" : "[mscorlib]"));
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_130_Consumption_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}(S1)"/>
public static class E
{
    ///
    extension(S1 x)
    {
        ///
        public void operator {{{op}}}(S1 y) => throw null;
    }
}

///
public class S1;
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = CompoundAssignmentOperatorName(op);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator " + ToCRefOp(op) + ", void E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 y))",
                "(E.extension(S1).operator " + ToCRefOp(op) + "(S1), void E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 y))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_131_Consumption_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}(S1)"/>
public static class E
{
    ///
    extension(S1 x)
    {
        ///
        public void operator {{{op}}}(S1 y) => throw null;
        ///
        public void operator checked {{{op}}}(S1 y) => throw null;
    }
}

///
public class S1;
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics();

            var opName = CompoundAssignmentOperatorName(op, isChecked: true);

            var e = comp.GetMember<NamedTypeSymbol>("E");
            AssertEx.Equal($$$"""
<member name="T:E">
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
    <see cref="M:E.&lt;G&gt;$78CFE6F93D970DBBE44B05C24FFEB91E.{{{opName}}}(S1)"/>
</member>

""", e.GetDocumentationCommentXml());

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            AssertEx.Equal([
                "(E.extension(S1).operator checked " + ToCRefOp(op) + ", void E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 y))",
                "(E.extension(S1).operator checked " + ToCRefOp(op) + "(S1), void E.<G>$78CFE6F93D970DBBE44B05C24FFEB91E." + opName + "(S1 y))"],
                ExtensionTests.PrintXmlCrefSymbols(tree, model));
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_132_Consumption_CRef_Error([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var src = $$$"""
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}()"/>
/// <see cref="E.extension(S1).operator {{{ToCRefOp(op)}}}(S1)"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}()"/>
/// <see cref="E.extension(S1).operator checked {{{ToCRefOp(op)}}}(S1)"/>
public static class E
{
    ///
    extension(S1 x)
    {
    }
}

///
public class S1;
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreviewWithDocumentationComments);
            comp.VerifyEmitDiagnostics(
                // (1,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +=' that could not be resolved
                // /// <see cref="E.extension(S1).operator +="/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op)).WithArguments("extension(S1).operator " + ToCRefOp(op)).WithLocation(1, 16),
                // (2,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +=()' that could not be resolved
                // /// <see cref="E.extension(S1).operator +=()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op) + "()").WithArguments("extension(S1).operator " + ToCRefOp(op) + "()").WithLocation(2, 16),
                // (3,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator +=(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator +=(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator " + ToCRefOp(op) + "(S1)").WithArguments("extension(S1).operator " + ToCRefOp(op) + "(S1)").WithLocation(3, 16),
                // (4,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +=' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +="/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op)).WithArguments("extension(S1).operator checked " + ToCRefOp(op)).WithLocation(4, 16),
                // (5,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +=()' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +=()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op) + "()").WithArguments("extension(S1).operator checked " + ToCRefOp(op) + "()").WithLocation(5, 16),
                // (6,16): warning CS1574: XML comment has cref attribute 'extension(S1).operator checked +=(S1)' that could not be resolved
                // /// <see cref="E.extension(S1).operator checked +=(S1)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "E.extension(S1).operator checked " + ToCRefOp(op) + "(S1)").WithArguments("extension(S1).operator checked " + ToCRefOp(op) + "(S1)").WithLocation(6, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_133_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static void* operator {{{op}}}(void* x, S1 y) => x;
    }
}

public struct S1;

class Program
{
    unsafe void Test(void* x, S1 y) => x {{{op}}}= y;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (13,40): error CS0019: Operator '>>=' cannot be applied to operands of type 'void*' and 'S1'
                //     unsafe void Test(void* x, S1 y) => x >>= y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op}= y").WithArguments($"{op}=", "void*", "S1").WithLocation(13, 40)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_134_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(void*)
    {
        public static S1 operator {{{op}}}(S1 x, void* y) => x;
    }
}

public struct S1;

class Program
{
    unsafe void Test(void* x, S1 y) => y {{{op}}}= x;
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,15): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(void*)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 15),
                // (13,40): error CS0019: Operator '^=' cannot be applied to operands of type 'S1' and 'void*'
                //     unsafe void Test(void* x, S1 y) => y ^= x;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"y {op}= x").WithArguments($"{op}=", "S1", "void*").WithLocation(13, 40)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_135_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        unsafe public static void* operator {{{op}}}(void* x, S1 y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1;

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        s11 {{{op}}}= s12;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_136_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^")] string op)
        {
            var src = $$$"""
public struct S1
{
    unsafe public static void* operator {{{op}}}(void* x, S1 y)
    {
        System.Console.Write("operator1");
        return x;
    }

    public override bool Equals(object other) => throw null;
    public override int GetHashCode() => throw null;
}

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        s11 {{{op}}}= s12;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_137_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S1)
    {
        unsafe public static S1 operator {{{op}}}(S1 x, void* y)
        {
            System.Console.Write("operator1");
            return x;
        }
    }
}

public struct S1;

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        s12 {{{op}}}= s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_138_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public struct S1
{
    unsafe public static S1 operator {{{op}}}(S1 x, void* y)
    {
        System.Console.Write("operator1");
        return x;
    }
}

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        s12 {{{op}}}= s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_139_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
unsafe public static class Extensions1
{
    extension(ref void* x)
    {
        public void operator {{{op}}}=(S1 y) {}
    }
}

public struct S1;

class Program
{
    unsafe void Test(void* x, S1 y) => x {{{op}}}= y;
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (3,19): error CS1103: The receiver parameter of an extension cannot be of type 'void*'
                //     extension(ref void* x)
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "void*").WithArguments("void*").WithLocation(3, 19),
                // (13,40): error CS0019: Operator '<<=' cannot be applied to operands of type 'void*' and 'S1'
                //     unsafe void Test(void* x, S1 y) => x <<= y;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, $"x {op}= y").WithArguments($"{op}=", "void*", "S1").WithLocation(13, 40)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_140_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S1 x)
    {
        unsafe public void operator {{{op}}}=(void* y)
        {
            System.Console.Write("operator1");
        }
    }
}

public struct S1;

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        s12 {{{op}}}= s11;
    }
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_141_ERR_VoidError([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
        {
            var src = $$$"""
public struct S1
{
    unsafe public void operator {{{op}}}=(void* y)
    {
        System.Console.Write("operator1");
    }
}

class Program
{
    unsafe static void Main()
    {
        void* s11 = null;
        var s12 = new S1();
        s12 {{{op}}}= s11;
    }
}
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "operator1", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_142_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(int y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, int y)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (22,12): error CS9340: Operator could not be resolved on operands of type 'S2' and 'S2'. The closest inapplicable candidate is 'Extensions1.extension(ref S2).operator +=(int)'
                //         s2 += s2;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S2", "S2", "Extensions1.extension(ref S2).operator +=(int)").WithLocation(22, 12)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Fact]
        public void CompoundAssignment_143_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(ref S2 x)
    {
        public void operator +=(C1 y) => throw null;
        public void operator +=(C2 y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, int y)
    {
        return x;
    }

    public static implicit operator C1 (S2 x) => null;
    public static implicit operator C2 (S2 x) => null;
}

public class C1;
public class C2;

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,12): error CS0121: The call is ambiguous between the following methods or properties: 'Extensions1.extension(ref S2).operator +=(C1)' and 'Extensions1.extension(ref S2).operator +=(C2)'
                //         s2 += s2;
                Diagnostic(ErrorCode.ERR_AmbigCall, "+=").WithArguments("Extensions1.extension(ref S2).operator +=(C1)", "Extensions1.extension(ref S2).operator +=(C2)").WithLocation(29, 12)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            AssertEx.Equal("Extensions1.extension(ref S2).operator +=(C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions1.extension(ref S2).operator +=(C2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_144_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, int y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, int y)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (22,12): error CS9340: Operator could not be resolved on operands of type 'S2' and 'S2'. The closest inapplicable candidate is 'Extensions1.extension(S2).operator +(S2, int)'
                //         s2 += s2;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("S2", "S2", "Extensions1.extension(S2).operator +(S2, int)").WithLocation(22, 12)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Fact]
        public void CompoundAssignment_145_Consumption_ErrorScenarioCandidates()
        {
            var src = $$$"""
public static class Extensions1
{
    extension(S2)
    {
        public static S2 operator +(S2 x, C1 y) => throw null;
        public static S2 operator +(S2 x, C2 y) => throw null;
    }
}

public struct S2
{
    public static S2 operator +(S2 x, int y)
    {
        return x;
    }

    public static implicit operator C1 (S2 x) => null;
    public static implicit operator C2 (S2 x) => null;
}

public class C1;
public class C2;

class Program
{
    static void Main()
    {
        var s2 = new S2();
        s2 += s2;
    }
}

""" + CompilerFeatureRequiredAttribute;

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (29,12): error CS9339: Operator resolution is ambiguous between the following members: 'Extensions1.extension(S2).operator +(S2, C1)' and 'Extensions1.extension(S2).operator +(S2, C2)'
                //         s2 += s2;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+=").WithArguments("Extensions1.extension(S2).operator +(S2, C1)", "Extensions1.extension(S2).operator +(S2, C2)").WithLocation(29, 12)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.Equal("Extensions1.extension(S2).operator +(S2, C1)", symbolInfo.CandidateSymbols[0].ToDisplayString());
            AssertEx.Equal("Extensions1.extension(S2).operator +(S2, C2)", symbolInfo.CandidateSymbols[1].ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_146_Consumption_BadOperator()
        {
            var source1 = @"
public static class Extensions1
{
    extension(C1 p)
    {
        public void operator +=(int a, int x = 0) {}
    }
}

public class C1;
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (6,30): error CS1020: Overloadable binary operator expected
                //         public void operator +=(int a, int x = 0) {}
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "+=").WithLocation(6, 30),
                // (6,44): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //         public void operator +=(int a, int x = 0) {}
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(6, 44)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_147_Consumption_BadOperator()
        {
            var source1 = @"
public static class Extensions1
{
    extension(ref C1 p)
    {
        public void operator +=(int a) {}
    }
}

public class C1;
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (4,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension(ref C1 p)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "C1").WithLocation(4, 19)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_148_Consumption_BadOperator()
        {
            var source1 = @"
public static class Extensions1
{
    extension(C1 p)
    {
        public void operator +=(int a) {}
    }
}

public struct C1;
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (6,30): error CS9322: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int a) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, "+=").WithLocation(6, 30)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79171")]
        public void RemoveWorseMembers_01()
        {
            // by-value vs. in extension parameter, static operator
            var src = """
_ = new S() + new S();

struct S { }

static class E1
{
    extension(S)
    {
        public static S operator +(S s1, S s2) => throw null;
    }
}

static class E2
{
    extension(in S s)
    {
        public static S operator +(S s1, S s2) => throw null;
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (1,13): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(S).operator +(S, S)' and 'E2.extension(in S).operator +(S, S)'
                // _ = new S() + new S();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("E1.extension(S).operator +(S, S)", "E2.extension(in S).operator +(S, S)").WithLocation(1, 13));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var binary = GetSyntax<BinaryExpressionSyntax>(tree, "new S() + new S()");
            Assert.Null(model.GetSymbolInfo(binary).Symbol);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79171")]
        public void RemoveWorseMembers_02()
        {
            // by-value vs. in parameter, static operator
            var src = """
_ = new S() + new S();

struct S { }

static class E1
{
    extension(S)
    {
        public static S operator +(S s1, S s2) => throw null;
    }
}

static class E2
{
    extension(S)
    {
        public static S operator +(in S s1, S s2) => throw null;
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var binary = GetSyntax<BinaryExpressionSyntax>(tree, "new S() + new S()");
            AssertEx.Equal("S E1.<G>$3B24C9A1A6673CA92CA71905DDEE0A6C.op_Addition(S s1, S s2)", model.GetSymbolInfo(binary).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void Using_Binary_01()
        {
            var src = """
using N1;
using N2;

_ = new C() + new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C).operator +(C, C)' and 'E2.extension(C).operator +(C, C)'
                // _ = new C() + new C();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("N1.E1.extension(C).operator +(C, C)", "N2.E2.extension(C).operator +(C, C)").WithLocation(4, 13));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<BinaryExpressionSyntax>(tree, "new C() + new C()");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "C N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Addition(C c1, C c2)",
                "C N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Addition(C c1, C c2)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Binary_02()
        {
            var src = """
using N1;
using N2;

_ = new C() + new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator +(C c1, int i) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Binary_03()
        {
            var src = """
using N1;
using N2;

_ = new C() + new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator checked +(C c1, C c2) => throw null;
            public static C operator +(C c1, C c2) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C).operator +(C, C)' and 'E2.extension(C).operator +(C, C)'
                // _ = new C() + new C();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("N1.E1.extension(C).operator +(C, C)", "N2.E2.extension(C).operator +(C, C)").WithLocation(4, 13));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<BinaryExpressionSyntax>(tree, "new C() + new C()");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "C N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Addition(C c1, C c2)",
                "C N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Addition(C c1, C c2)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Binary_04()
        {
            var src = """
using N1;
using N2;

_ = new C() + new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T)
        {
            public static T operator +(T t1, T t2) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Binary_05()
        {
            var src = """
using N1;
using N2;

_ = new C() + new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator -(C c1, C c2) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Compound_01()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c1)
        {
            public void operator +=(C c2) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (5,3): error CS0121: The call is ambiguous between the following methods or properties: 'N1.E1.extension(C).operator +=(C)' and 'N2.E2.extension(C).operator +=(C)'
                // c += new C();
                Diagnostic(ErrorCode.ERR_AmbigCall, "+=").WithArguments("N1.E1.extension(C).operator +=(C)", "N2.E2.extension(C).operator +=(C)").WithLocation(5, 3));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<AssignmentExpressionSyntax>(tree, "c += new C()");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "void N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_AdditionAssignment(C c2)",
                "void N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_AdditionAssignment(C c2)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Compound_02()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c1)
        {
            public void operator +=(int i) => throw null;
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Compound_03()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c1)
        {
            public void operator checked +=(C c2) => throw null;
            public void operator +=(C c2) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (5,3): error CS0121: The call is ambiguous between the following methods or properties: 'N1.E1.extension(C).operator +=(C)' and 'N2.E2.extension(C).operator +=(C)'
                // c += new C();
                Diagnostic(ErrorCode.ERR_AmbigCall, "+=").WithArguments("N1.E1.extension(C).operator +=(C)", "N2.E2.extension(C).operator +=(C)").WithLocation(5, 3));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<AssignmentExpressionSyntax>(tree, "c += new C()");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "void N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_AdditionAssignment(C c2)",
                "void N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_AdditionAssignment(C c2)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Compound_04()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c1)
        {
            public void operator checked +=(int i) => throw null;
            public void operator +=(int i) => throw null;
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Compound_05()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T t1) where T : class
        {
            public void operator +=(T t2) => throw null;
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Compound_06()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) => throw null; 
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Compound_07()
        {
            var src = """
using N1;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c1)
        {
            public void operator +=(C c2) => throw null;
        }
    }
}

static class E2
{
    extension(C)
    {
        public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1));
        }

        [Fact]
        public void Using_Compound_08()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) => throw null; 
        }
    }
}
namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) => throw null; 
        }
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,3): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C).operator +(C, C)' and 'E2.extension(C).operator +(C, C)'
                // c += new C();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+=").WithArguments("N1.E1.extension(C).operator +(C, C)", "N2.E2.extension(C).operator +(C, C)").WithLocation(5, 3));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<AssignmentExpressionSyntax>(tree, "c += new C()");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "C N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Addition(C c1, C c2)",
                "C N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Addition(C c1, C c2)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Compound_09()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
        }
    }
}
namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator -(C c1, C c2) => throw null; 
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Compound_10()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
        }
    }
}
namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator +(C c1, int i) => throw null; 
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Compound_11()
        {
            var src = """
using N1;
using N2;

C c = new C();
c += new C();

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c1, C c2) { System.Console.Write("ran"); return c1; }
        }
    }
}
namespace N2
{
    static class E2
    {
        extension<T>(T)
        {
            public static T operator +(T t1, T t2) => throw null; 
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Unary_01()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = +c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator +(C c) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,5): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C).operator +(C)' and 'E2.extension(C).operator +(C)'
                // _ = +c;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("N1.E1.extension(C).operator +(C)", "N2.E2.extension(C).operator +(C)").WithLocation(5, 5));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<PrefixUnaryExpressionSyntax>(tree, "+c");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "C N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_UnaryPlus(C c)",
                "C N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_UnaryPlus(C c)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Unary_02()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = +c;

class C { }
class D { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(D)
        {
            public static D operator +(D d) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Unary_03()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = +c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator -(C c) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Unary_04()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = +c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator +(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T)
        {
            public static T operator +(T t) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_01()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = c++;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator ++(C c) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,6): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C).operator ++(C)' and 'E2.extension(C).operator ++(C)'
                // _ = c++;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "++").WithArguments("N1.E1.extension(C).operator ++(C)", "N2.E2.extension(C).operator ++(C)").WithLocation(5, 6));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<PostfixUnaryExpressionSyntax>(tree, "c++");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "C N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Increment(C c)",
                "C N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Increment(C c)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Increment_02()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = c++;

class C { }
class D { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(D)
        {
            public static D operator ++(D d) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_03()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = c++;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T)
        {
            public static T operator ++(T t) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_04()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator ++(C c) => throw null;
        }
    }
}
""";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (5,5): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C).operator ++(C)' and 'E2.extension(C).operator ++(C)'
                // _ = ++c;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "++").WithArguments("N1.E1.extension(C).operator ++(C)", "N2.E2.extension(C).operator ++(C)").WithLocation(5, 5));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<PrefixUnaryExpressionSyntax>(tree, "++c");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "C N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Increment(C c)",
                "C N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_Increment(C c)"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Increment_05()
        {
            // IL has class C and an extension property `N2.E2.op_Increment`
            // Because the property is not an operator, the namespace N2 is not considered used
            var ilSrc = """
.class public auto ansi beforefieldinit C
    extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void System.Object::.ctor()
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit N2.E2
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$9794DAFCCB9E752B29BFD6350ADA77F2'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$97E2B955DB039EABEEA2419CE447FF1C'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( class C '' ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }

        .method public hidebysig specialname static class C get_op_Increment () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 39 37 45 32 42 39 35 35 44
                42 30 33 39 45 41 42 45 45 41 32 34 31 39 43 45
                34 34 37 46 46 31 43 00 00
            )

            ldnull
            throw
        }

        .property class C op_Increment()
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 39 37 45 32 42 39 35 35 44
                42 30 33 39 45 41 42 45 45 41 32 34 31 39 43 45
                34 34 37 46 46 31 43 00 00
            )
            .get class C N2.E2/'<G>$9794DAFCCB9E752B29BFD6350ADA77F2'::get_op_Increment()
        }
    }

    .method public hidebysig static class C get_op_Increment () cil managed 
    {
        ldnull
        ret
    }
}
""";

            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) => throw null;
        }
    }
}
""";
            var comp = CreateCompilationWithIL(src, ilSrc + ExtensionMarkerAttributeIL);
            comp.VerifyEmitDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Increment_06()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }
class D { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(D)
        {
            public static D operator ++(D d) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_07()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T)
        {
            public static T operator ++(T t) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_08()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) { System.Console.Write("ran"); return c; }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C)
        {
            public static C operator --(C c) => throw null;
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "ran").VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Increment_09()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C)
        {
            public static C operator ++(C c) => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c)
        {
            public void operator ++() { System.Console.Write("ran"); }
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using N1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1;").WithLocation(1, 1));
        }

        [Fact]
        public void Using_Increment_10()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}
""";
            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (5,5): error CS0121: The call is ambiguous between the following methods or properties: 'N1.E1.extension(C).operator ++()' and 'N2.E2.extension(C).operator ++()'
                // _ = ++c;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("N1.E1.extension(C).operator ++()", "N2.E2.extension(C).operator ++()").WithLocation(5, 5));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var opNode = GetSyntax<PrefixUnaryExpressionSyntax>(tree, "++c");
            var symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            AssertEx.SetEqual([
                "void N1.E1.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_IncrementAssignment()",
                "void N2.E2.<G>$9794DAFCCB9E752B29BFD6350ADA77F2.op_IncrementAssignment()"
                ], symbolInfo.CandidateSymbols.ToTestDisplayStrings());
        }

        [Fact]
        public void Using_Increment_11()
        {
            var src = """
using N1;
using N2;

C c = new C();
_ = ++c;

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T t) where T : class
        {
            public void operator ++() => throw null;
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_12()
        {
            var src = """
using N1;
using N2;

C c = new C();
checked
{
    _ = ++c;
}

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T t) where T : class
        {
            public void operator ++() => throw null;
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_13()
        {
            var src = """
using N1;
using N2;

C c = new C();
checked
{
    _ = ++c;
}

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}
""";
            CreateCompilation([src, CompilerFeatureRequiredAttribute]).VerifyEmitDiagnostics(
                // (7,9): error CS0121: The call is ambiguous between the following methods or properties: 'N1.E1.extension(C).operator ++()' and 'N2.E2.extension(C).operator ++()'
                //     _ = ++c;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("N1.E1.extension(C).operator ++()", "N2.E2.extension(C).operator ++()").WithLocation(7, 9));
        }

        [Fact]
        public void Using_Increment_14()
        {
            var src = """
using N1;
using N2;

C c = new C();
checked
{
    _ = ++c;
}

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c)
        {
            public void operator --() => throw null;
        }
    }
}
""";
            CreateCompilation([src, CompilerFeatureRequiredAttribute]).VerifyEmitDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Increment_15()
        {
            var src = """
using static N1.E1;
using static N2.E2;

C c = new C();
checked
{
    _ = ++c;
}

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() { System.Console.Write("ran"); }
        }
    }
}

namespace N2
{
    static class E2
    {
        extension<T>(T t) where T : class
        {
            public void operator ++() => throw null;
        }
    }
}
""";
            CompileAndVerify([src, CompilerFeatureRequiredAttribute], expectedOutput: "ran").VerifyDiagnostics();
        }

        [Fact]
        public void Using_Increment_16()
        {
            var src = """
using static N1.E1;
using static N2.E2;

C c = new C();
checked
{
    _ = ++c;
}

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}
""";
            CreateCompilation([src, CompilerFeatureRequiredAttribute]).VerifyEmitDiagnostics(
                // (7,9): error CS0121: The call is ambiguous between the following methods or properties: 'N1.E1.extension(C).operator ++()' and 'N2.E2.extension(C).operator ++()'
                //     _ = ++c;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("N1.E1.extension(C).operator ++()", "N2.E2.extension(C).operator ++()").WithLocation(7, 9));
        }

        [Fact]
        public void Using_Increment_17()
        {
            var src = """
using static N1.E1;
using static N2.E2;

C c = new C();
checked
{
    _ = ++c;
}

class C { }

namespace N1 
{
    static class E1
    {
        extension(C c)
        {
            public void operator ++() => throw null;
        }
    }
}

namespace N2
{
    static class E2
    {
        extension(C c)
        {
            public void operator --() => throw null;
        }
    }
}
""";
            CreateCompilation([src, CompilerFeatureRequiredAttribute]).VerifyEmitDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using static N2.E2;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N2.E2;").WithLocation(2, 1));
        }

        [Fact]
        public void Using_Increment_18()
        {
            //public struct S { }
            //
            //namespace N1
            //{
            //    static class E1
            //    {
            //        extension(S s)
            //        {
            //            public void operator ++() => throw null;
            //        }
            //    }
            //}
            var ilSrc = """
.class public sequential ansi sealed beforefieldinit S
    extends System.ValueType
{
    .pack 0
    .size 1
}

.class private auto ansi abstract sealed beforefieldinit N1.E1
    extends System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .class nested public auto ansi sealed specialname '<G>$3B24C9A1A6673CA92CA71905DDEE0A6C'
        extends System.Object
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        .class nested public auto ansi abstract sealed specialname '<M>$B97DBA9601C1D9D405F877E292405C09'
            extends System.Object
        {
            .method public hidebysig specialname static void '<Extension>$' ( valuetype S s ) cil managed 
            {
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
                ret
            }
        }

        .method public hidebysig specialname instance void op_IncrementAssignment () cil managed 
        {
            .custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
                01 00 24 3c 4d 3e 24 42 39 37 44 42 41 39 36 30
                31 43 31 44 39 44 34 30 35 46 38 37 37 45 32 39
                32 34 30 35 43 30 39 00 00
            )

            ldnull
            throw
        }
    }

    .method public hidebysig static void op_IncrementAssignment ( valuetype S s ) cil managed 
    {
        ldnull
        throw
    }
}
""" + ExtensionMarkerAttributeIL;

            var src = """
using N1;

S s = new S();
_ = ++s;
""";
            var comp = CreateCompilationWithIL(src, ilSrc);
            comp.VerifyEmitDiagnostics(
                // (4,5): error CS0023: Operator '++' cannot be applied to operand of type 'S'
                // _ = ++s;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++s").WithArguments("++", "S").WithLocation(4, 5));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_01()
        {
            // inner scope has inapplicable operator, outer scope too
            var src = """
using N;

I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1)
    {
        public static I1 operator +(I1 i1, I2 i2) => throw null;
    }
}

namespace N
{
    public static class E2
    {
        extension(I1)
        {
            public static I1 operator +(I1 i1, I2 i2) => throw null;
        }
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,4): error CS9340: Operator 'I1' could not be resolved on operands of type 'I1' and 'int'. The closest inapplicable candidate is 'E1.extension(I1).operator +(I1, I2)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("I1", "int", "E1.extension(I1).operator +(I1, I2)").WithLocation(4, 4)
                );
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_02()
        {
            // inner scope has inapplicable operator, outer scope has two applicable/worse operators
            var src = """
using N;

I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1)
    {
        public static I1 operator +(I1 i1, I2 i2) => throw null;
    }
}

namespace N
{
    public static class E2
    {
        extension(I1)
        {
            public static I1 operator +(I1 i1, int i) => throw null;
        }
    }
    public static class E3
    {
        extension(I1)
        {
            public static I1 operator +(I1 i1, int i) => throw null;
        }
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,4): error CS9340: Operator 'I1' could not be resolved on operands of type 'I1' and 'int'. The closest inapplicable candidate is 'E1.extension(I1).operator +(I1, I2)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("I1", "int", "E1.extension(I1).operator +(I1, I2)").WithLocation(4, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_03()
        {
            // inner scope has two inapplicable operators, outer scope has two applicable/worse operators
            var src = """
using N;

I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1)
    {
        public static I1 operator +(I1 i1, I2 i2) => throw null;
    }
}

public static class E2
{
    extension(I1)
    {
        public static I1 operator +(I1 i1, I2 i2) => throw null;
    }
}

namespace N
{
    public static class E3
    {
        extension(I1)
        {
            public static I1 operator +(I1 i1, int i) => throw null;
        }
    }
    public static class E4
    {
        extension(I1)
        {
            public static I1 operator +(I1 i1, int i) => throw null;
        }
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,1): error CS0034: Operator '+=' is ambiguous on operands of type 'I1' and 'int'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "i1 += 42").WithArguments("+=", "I1", "int").WithLocation(4, 1));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_04()
        {
            // outer scope has inapplicable operator, inner scope has two applicable/worse operators
            var src = """
using N;

I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1)
    {
        public static I1 operator +(I1 i1, int i) => throw null;
    }
}

public static class E2
{
    extension(I1)
    {
        public static I1 operator +(I1 i1, int i) => throw null;
    }
}

namespace N
{
    public static class E3
    {
        extension(I1)
        {
            public static I1 operator +(I1 i1, I2 i2) => throw null;
        }
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,4): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(I1).operator +(I1, int)' and 'E2.extension(I1).operator +(I1, int)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+=").WithArguments("E1.extension(I1).operator +(I1, int)", "E2.extension(I1).operator +(I1, int)").WithLocation(4, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_05()
        {
            // single inapplicable instance operator and single inapplicable static operator
            var src = """
I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1 i1)
    {
        public void operator +=(I2 i2) => throw null;
        public static I1 operator +(I1 i2, I2 i3) => throw null;
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9340: Operator 'I1' could not be resolved on operands of type 'I1' and 'int'. The closest inapplicable candidate is 'E1.extension(I1).operator +=(I2)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("I1", "int", "E1.extension(I1).operator +=(I2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_06()
        {
            // single inapplicable instance operator
            var src = """
I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1 i1)
    {
        public void operator +=(I2 i2) => throw null;
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9340: Operator 'I1' could not be resolved on operands of type 'I1' and 'int'. The closest inapplicable candidate is 'E1.extension(I1).operator +=(I2)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("I1", "int", "E1.extension(I1).operator +=(I2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_07()
        {
            // single inapplicable static operator
            var src = """
I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1 i1)
    {
        public static I1 operator +(I1 i2, I2 i3) => throw null;
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9340: Operator 'I1' could not be resolved on operands of type 'I1' and 'int'. The closest inapplicable candidate is 'E1.extension(I1).operator +(I1, I2)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("I1", "int", "E1.extension(I1).operator +(I1, I2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_08()
        {
            // inapplicable static operator and irrelevant static operator (not compatible with receiver)
            var src = """
I1 i1 = null;
i1 += 42;

public static class E1
{
    extension(I1)
    {
        public static I1 operator +(I1 i2, I2 i3) => throw null;
    }

    extension(I2)
    {
        public static I2 operator +(I2 i, I2 j) => throw null;
    }
}

public interface I1 { }
public interface I2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9340: Operator 'I1' could not be resolved on operands of type 'I1' and 'int'. The closest inapplicable candidate is 'E1.extension(I1).operator +(I1, I2)'
                // i1 += 42;
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("I1", "int", "E1.extension(I1).operator +(I1, I2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_09()
        {
            // inapplicable non-extension static operator
            var src = """
C1 c1 = null;
c1 += new C3();

public class C1
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public class C2 { }
public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'C3'
                // c1 += new C3();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 += new C3()").WithArguments("+=", "C1", "C3").WithLocation(2, 1));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_10()
        {
            // inapplicable non-extension static operator, single inapplicable extension static operator
            var src = """
C1 c1 = null;
c1 += new C3();

public class C1
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public static class E1
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C2 c2) => throw null;
    }
}

public class C2 { }
public class C3 { }
""";

            // Note: GetUserDefinedOperators clears its results when no candidate was applicable. That's why we report the extension operator rather than the equivalent non-extension operator
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9340: Operator 'C1' could not be resolved on operands of type 'C1' and 'C3'. The closest inapplicable candidate is 'E1.extension(C1).operator +(C1, C2)'
                // c1 += new C3();
                Diagnostic(ErrorCode.ERR_SingleInapplicableBinaryOperator, "+=").WithArguments("C1", "C3", "E1.extension(C1).operator +(C1, C2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_11()
        {
            // inapplicable non-extension static operator, two inapplicable extension static operators
            var src = """
C1 c1 = null;
c1 += new C3();

public class C1
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public static class E1
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C2 c2) => throw null;
    }
}

public static class E2
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C2 c2) => throw null;
    }
}

public class C2 { }
public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'C3'
                // c1 += new C3();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 += new C3()").WithArguments("+=", "C1", "C3").WithLocation(2, 1));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_12()
        {
            // inapplicable non-extension static operator, two applicable extension static operators
            var src = """
C1 c1 = null;
c1 += new C3();

public class C1
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public static class E1
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C3 c3) => throw null;
    }
}

public static class E2
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C3 c3) => throw null;
    }
}

public class C2 { }
public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9339: Operator resolution is ambiguous between the following members: 'E1.extension(C1).operator +(C1, C3)' and 'E2.extension(C1).operator +(C1, C3)'
                // c1 += new C3();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+=").WithArguments("E1.extension(C1).operator +(C1, C3)", "E2.extension(C1).operator +(C1, C3)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_13()
        {
            // two applicable non-extension static operators
            var src = """
C1 c1 = null;
c1 += new C2();

public class C1
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public class C2 
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9339: Operator resolution is ambiguous between the following members: 'C1.operator +(C1, C2)' and 'C2.operator +(C1, C2)'
                // c1 += new C2();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+=").WithArguments("C1.operator +(C1, C2)", "C2.operator +(C1, C2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_14()
        {
            // two applicable non-extension static operators, single applicable extension static operator
            var src = """
C1 c1 = null;
c1 += new C2();

public class C1
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public class C2 
{ 
    public static C1 operator +(C1 c1, C2 c2) => throw null;
}

public static class E
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C2 c2) => throw null;
    }
}

public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,4): error CS9339: Operator resolution is ambiguous between the following members: 'C1.operator +(C1, C2)' and 'C2.operator +(C1, C2)'
                // c1 += new C2();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+=").WithArguments("C1.operator +(C1, C2)", "C2.operator +(C1, C2)").WithLocation(2, 4));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_15()
        {
            // two inapplicable non-extension static operators
            var src = """
C1 c1 = null;
c1 += new C2();

public class C1
{ 
    public static C1 operator +(C1 c1, C3 c3) => throw null;
}

public class C2 
{ 
    public static C1 operator +(C3 c3, C2 c2) => throw null;
}

public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'C2'
                // c1 += new C2();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 += new C2()").WithArguments("+=", "C1", "C2").WithLocation(2, 1));
        }

        [Fact]
        public void ReportDiagnostics_CompoundAssignment_16()
        {
            // two inapplicable non-extension static operators, second operand type is integer
            var src = """
C1 c1 = null;
c1 += 42;

public class C1
{ 
    public static C1 operator +(C1 c1, C3 c3) => throw null;
}

public class C2 
{ 
    public static C1 operator +(C3 c3, C2 c2) => throw null;
}

public class C3 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                // c1 += 42;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 += 42").WithArguments("+=", "C1", "int").WithLocation(2, 1));
        }

        [Fact]
        public void ReportDiagnostics_Binary_01()
        {
            var src = """
_ = new C1() + new C2();

public static class E1
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C2 c2) => throw null;
    }
}

public static class E2
{
    extension(C1)
    {
        public static C1 operator +(C1 c1, C2 c2) => throw null;
    }
}

public class C1 { }
public class C2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (1,14): error CS9342: Operator resolution is ambiguous between the following members: 'E1.extension(C1).operator +(C1, C2)' and 'E2.extension(C1).operator +(C1, C2)'
                // _ = new C1() + new C2();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("E1.extension(C1).operator +(C1, C2)", "E2.extension(C1).operator +(C1, C2)").WithLocation(1, 14));
        }

        [Fact]
        public void ReportDiagnostics_Binary_02()
        {
            var src = """
_ = new C1() == new C2();

public static class E1
{
    extension(C1)
    {
        public static bool operator ==(C1 c1, C2 c2) => throw null;
        public static bool operator !=(C1 c1, C2 c2) => throw null;
    }
}

public static class E2
{
    extension(C1)
    {
        public static bool operator ==(C1 c1, C2 c2) => throw null;
        public static bool operator !=(C1 c1, C2 c2) => throw null;
    }
}

public class C1 { }
public class C2 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (1,14): error CS9342: Operator resolution is ambiguous between the following members: 'E1.extension(C1).operator ==(C1, C2)' and 'E2.extension(C1).operator ==(C1, C2)'
                // _ = new C1() == new C2();
                Diagnostic(ErrorCode.ERR_AmbigOperator, "==").WithArguments("E1.extension(C1).operator ==(C1, C2)", "E2.extension(C1).operator ==(C1, C2)").WithLocation(1, 14));
        }

        [Fact]
        public void ReportDiagnostics_Binary_03()
        {
            var src = """
_ = null == null;
_ = null == default;
_ = null == (() => { });
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS0034: Operator '==' is ambiguous on operands of type '<null>' and 'default'
                // _ = null == default;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "null == default").WithArguments("==", "<null>", "default").WithLocation(2, 5));
        }

        [Fact]
        public void ReportDiagnostics_Unary_01()
        {
            var src = """
C1 c1 = null;
_ = +c1;

public static class E1
{
    extension(C1)
    {
        public static C1 operator +(C1 c1) => throw null;
    }
}

public static class E2
{
    extension(C1)
    {
        public static C1 operator +(C1 c1) => throw null;
    }
}

public class C1 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9342: Operator resolution is ambiguous between the following members: 'E1.extension(C1).operator +(C1)' and 'E2.extension(C1).operator +(C1)'
                // _ = +c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "+").WithArguments("E1.extension(C1).operator +(C1)", "E2.extension(C1).operator +(C1)").WithLocation(2, 5));
        }

        [Fact]
        public void ReportDiagnostics_Increment_01()
        {
            var src = """
C1 c1 = null;
_ = ++c1;

public static class E1
{
    extension(C1)
    {
        public static C1 operator ++(C1 c1) => throw null;
    }
}

public static class E2
{
    extension(C1)
    {
        public static C1 operator ++(C1 c1) => throw null;
    }
}

public class C1 { }
""";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS9342: Operator resolution is ambiguous between the following members: 'E1.extension(C1).operator ++(C1)' and 'E2.extension(C1).operator ++(C1)'
                // _ = ++c1;
                Diagnostic(ErrorCode.ERR_AmbigOperator, "++").WithArguments("E1.extension(C1).operator ++(C1)", "E2.extension(C1).operator ++(C1)").WithLocation(2, 5));
        }

        [Fact]
        public void ReportDiagnostics_Increment_02()
        {
            var src = """
C1 c1 = null;
_ = ++c1;

public static class E1
{
    extension(C1 c1)
    {
        public void operator ++() => throw null;
    }
}

public static class E2
{
    extension(C1 c1)
    {
        public void operator ++() => throw null;
    }
}

public class C1 { }
""";

            var comp = CreateCompilation([src, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics(
                // (2,5): error CS0121: The call is ambiguous between the following methods or properties: 'E1.extension(C1).operator ++()' and 'E2.extension(C1).operator ++()'
                // _ = ++c1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("E1.extension(C1).operator ++()", "E2.extension(C1).operator ++()").WithLocation(2, 5));
        }
    }
}
