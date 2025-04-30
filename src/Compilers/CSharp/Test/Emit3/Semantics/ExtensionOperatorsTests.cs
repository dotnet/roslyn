// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CSharp.UnitTests.UserDefinedCompoundAssignmentOperatorsTests;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.Extensions)]
    public class ExtensionOperatorsTests : CompilingTestBase
    {
        [Theory]
        [CombinatorialData]
        public void Unary_001_Declaration([CombinatorialValues("+", "-", "!")] string op)
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (15,35): error CS9551: The parameter of a unary operator must be the extended type.
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
                // (41,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator +(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(41, 35),
                // (41,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator +(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(41, 37)
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (14,35): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //         public static S2 operator ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(14, 35),
                // (15,35): error CS9552: The parameter type for ++ or -- operator must be the extended type.
                //         public static S1 operator ++(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionIncDecSignature, op).WithLocation(15, 35),
                // (23,28): error CS0558: User-defined operator 'Extensions3.extension(S1).operator ++(S1)' must be declared static and public
                //         static S1 operator ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions3.extension(S1).operator " + op + "(S1)").WithLocation(23, 28),
                // (28,28): error CS9502: Overloaded instance increment operator '++' must take no parameters
                //         public S2 operator ++(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadIncrementOpArgs, op).WithArguments(op).WithLocation(28, 28),
                // (33,23): error CS0722: 'C1': static types cannot be used as return types
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C1").WithArguments("C1").WithLocation(33, 23),
                // (33,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(33, 35),
                // (33,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(33, 38)
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (200,37): error CS0216: The operator 'Extensions2.extension(S1).operator true(S1)' requires a matching operator 'false' to also be defined
                //         public static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "true").WithArguments("Extensions2.extension(S1).operator true(S1)", "false").WithLocation(200, 37),
                // (300,37): error CS0216: The operator 'Extensions2.extension(S1).operator false(S1)' requires a matching operator 'true' to also be defined
                //         public static bool operator false(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "false").WithArguments("Extensions2.extension(S1).operator false(S1)", "true").WithLocation(300, 37),

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
                // (600,37): error CS9551: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(600, 37),
                // (601,37): error CS9551: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(S2 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(601, 37),
                // (700,30): error CS0558: User-defined operator 'Extensions6.extension(S1).operator true(S1)' must be declared static and public
                //         static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "true").WithArguments("Extensions6.extension(S1).operator true(S1)").WithLocation(700, 30),
                // (701,30): error CS0558: User-defined operator 'Extensions6.extension(S1).operator false(S1)' must be declared static and public
                //         public bool operator false(S1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "false").WithArguments("Extensions6.extension(S1).operator false(S1)").WithLocation(701, 30),
                // (800,37): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static bool operator true(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "true").WithLocation(800, 37),
                // (800,42): error CS0721: 'C1': static types cannot be used as parameters
                //         public static bool operator true(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 42),
                // (801,37): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static bool operator false(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "false").WithLocation(801, 37),
                // (801,43): error CS0721: 'C1': static types cannot be used as parameters
                //         public static bool operator false(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_004_Declaration([CombinatorialValues("+", "-", "!")] string op)
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

        private static string UnaryOperatorName(string op)
        {
            return OperatorFacts.UnaryOperatorNameFromSyntaxKind(SyntaxFactory.ParseToken(op).Kind(), isChecked: false);
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
        public void Unary_007_Declaration([CombinatorialValues("+", "-", "!", "++", "--")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
            comp.VerifyDiagnostics(
                // (6,35): error CS0106: The modifier 'abstract' is not valid for this item
                //         public static S1 operator !(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_008_Declaration([CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (100,43): error CS9025: The operator 'Extensions2.extension(S1).operator checked ++(S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions2.extension(S1).operator checked " + op + "(S1)").WithLocation(100, 43),

                // (112,43): error CS9025: The operator 'Extensions3.extension(S1).operator checked ++(S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "(S1)").WithLocation(112, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_001_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions2
{
    extension(S1)
    {
        public S1 operator {{{op}}}() => throw null;
    }
}

static class Extensions3
{
    extension(S1)
    {
        void operator {{{op}}}() {}
    }
    extension(C1)
    {
        public void operator {{{op}}}() {}
    }
}

struct S1
{}

static class C1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (13,28): error CS9503: The return type for this operator must be void
                //         public S1 operator ++() => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(13, 28),
                // (21,23): error CS9501: User-defined operator 'Extensions3.extension(S1).operator ++()' must be declared public
                //         void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("Extensions3.extension(S1).operator " + op + "()").WithLocation(21, 23),
                // (25,30): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(25, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_002_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
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

        [Theory]
        [CombinatorialData]
        public void Increment_003_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public void operator {{{op}}}() {}
        public static S1 operator {{{op}}}(S1 x) => default;
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);

            // PROTOTYPE: We probably should enable this scenario
            comp.VerifyDiagnostics(
                // (6,35): error CS0111: Type 'Extensions1' already defines a member called 'op_Increment' with the same parameter types
                //         public static S1 operator ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(UnaryOperatorName(op), "Extensions1").WithLocation(6, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_005_Declaration([CombinatorialValues("++", "--")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public void operator checked {{{op}}}() {}
        public void operator {{{op}}}() {}
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 100
        public void operator checked {{{op}}}() {}
    }
    extension(S1)
    {
        public void operator {{{op}}}() {}
    }
}

static class Extensions3
{
    extension(S1)
    {
        public void operator checked {{{op}}}() {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (100,38): error CS9025: The operator 'Extensions2.extension(S1).operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions2.extension(S1).operator checked " + op + "()").WithLocation(100, 38),

                // (112,38): error CS9025: The operator 'Extensions3.extension(S1).operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "()").WithLocation(112, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_004_Declaration([CombinatorialValues("++", "--")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        {{{modifier}}}
        public void operator {{{op}}}() {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (6,30): error CS0106: The modifier 'abstract' is not valid for this item
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 30)
                );
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (16,35): error CS9553: One of the parameters of a binary operator must be the extended type.
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
                // (42,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator +(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(42, 35),
                // (42,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator +(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(42, 37)
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,35): error CS9554: The first operand of an overloaded shift operator must have the same type as the extended type
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
                // (38,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator >>>(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(38, 35),
                // (38,39): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator >>>(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(38, 39 - (op == ">>>" ? 0 : 1))
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (200,37): error CS0216: The operator 'Extensions2.extension(S1).operator !=(S1, S2)' requires a matching operator '==' to also be defined
                //         public static bool operator !=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "!=").WithArguments("Extensions2.extension(S1).operator !=(S1, S2)", "==").WithLocation(200, 37),
                // (300,37): error CS0216: The operator 'Extensions2.extension(S1).operator ==(S1, S2)' requires a matching operator '!=' to also be defined
                //         public static bool operator ==(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "==").WithArguments("Extensions2.extension(S1).operator ==(S1, S2)", "!=").WithLocation(300, 37),

                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator !=(S1, S2)' requires a matching operator '==' to also be defined
                //         public static bool operator !=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "!=").WithArguments("Extensions3.extension(S1).operator !=(S1, S2)", "==").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator ==(S2, S1)' requires a matching operator '!=' to also be defined
                //         public static bool operator ==(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "==").WithArguments("Extensions3.extension(S2).operator ==(S2, S1)", "!=").WithLocation(405, 37),
                // (500,37): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator !=(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "!=").WithLocation(500, 37),
                // (501,37): error CS9553: One of the parameters of a binary operator must be the extended type.
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
                // (800,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator !=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "!=").WithLocation(800, 35),
                // (800,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator !=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 38),
                // (801,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator ==(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "==").WithLocation(801, 35),
                // (801,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator ==(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 38)
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (200,37): error CS0216: The operator 'Extensions2.extension(S1).operator >=(S1, S2)' requires a matching operator '<=' to also be defined
                //         public static bool operator >=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">=").WithArguments("Extensions2.extension(S1).operator >=(S1, S2)", "<=").WithLocation(200, 37),
                // (300,37): error CS0216: The operator 'Extensions2.extension(S1).operator <=(S1, S2)' requires a matching operator '>=' to also be defined
                //         public static bool operator <=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<=").WithArguments("Extensions2.extension(S1).operator <=(S1, S2)", ">=").WithLocation(300, 37),

                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator >=(S1, S2)' requires a matching operator '<=' to also be defined
                //         public static bool operator >=(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">=").WithArguments("Extensions3.extension(S1).operator >=(S1, S2)", "<=").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator <=(S2, S1)' requires a matching operator '>=' to also be defined
                //         public static bool operator <=(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<=").WithArguments("Extensions3.extension(S2).operator <=(S2, S1)", ">=").WithLocation(405, 37),
                // (500,37): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator >=(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">=").WithLocation(500, 37),
                // (501,37): error CS9553: One of the parameters of a binary operator must be the extended type.
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
                // (800,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator >=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, ">=").WithLocation(800, 35),
                // (800,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator >=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 38),
                // (801,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator <=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "<=").WithLocation(801, 35),
                // (801,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator <=(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 38)
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

struct S1
{}

struct S2
{}

static class C1
{}
""";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (200,37): error CS0216: The operator 'Extensions2.extension(S1).operator >(S1, S2)' requires a matching operator '<' to also be defined
                //         public static bool operator >(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">").WithArguments("Extensions2.extension(S1).operator >(S1, S2)", "<").WithLocation(200, 37),
                // (300,37): error CS0216: The operator 'Extensions2.extension(S1).operator <(S1, S2)' requires a matching operator '>' to also be defined
                //         public static bool operator <(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<").WithArguments("Extensions2.extension(S1).operator <(S1, S2)", ">").WithLocation(300, 37),

                // (400,37): error CS0216: The operator 'Extensions3.extension(S1).operator >(S1, S2)' requires a matching operator '<' to also be defined
                //         public static bool operator >(S1 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, ">").WithArguments("Extensions3.extension(S1).operator >(S1, S2)", "<").WithLocation(400, 37),
                // (405,37): error CS0216: The operator 'Extensions3.extension(S2).operator <(S2, S1)' requires a matching operator '>' to also be defined
                //         public static bool operator <(S2 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorNeedsMatch, "<").WithArguments("Extensions3.extension(S2).operator <(S2, S1)", ">").WithLocation(405, 37),
                // (500,37): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static bool operator >(S2 x, S2 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">").WithLocation(500, 37),
                // (501,37): error CS9553: One of the parameters of a binary operator must be the extended type.
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
                // (800,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator >(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, ">").WithLocation(800, 35),
                // (800,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator >(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(800, 37),
                // (801,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static S1 operator <(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, "<").WithLocation(801, 35),
                // (801,37): error CS0721: 'C1': static types cannot be used as parameters
                //         public static S1 operator <(C1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 37)
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

        private static string BinaryOperatorName(string op)
        {
            var kind = op switch
            {
                ">>" => SyntaxKind.GreaterThanGreaterThanToken,
                ">>>" => SyntaxKind.GreaterThanGreaterThanGreaterThanToken,
                _ => SyntaxFactory.ParseToken(op).Kind(),
            };
            return OperatorFacts.BinaryOperatorNameFromSyntaxKind(kind, isChecked: false);
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
        public void Binary_008_Declaration([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
            comp.VerifyDiagnostics(
                // (6,35): error CS0106: The modifier 'abstract' is not valid for this item
                //         public static S1 operator -(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments(modifier).WithLocation(6, 35)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_009_Declaration([CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (100,43): error CS9025: The operator 'Extensions2.extension(S1).operator checked +(S1, S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked +(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions2.extension(S1).operator checked " + op + "(S1, S1)").WithLocation(100, 43),

                // (112,43): error CS9025: The operator 'Extensions3.extension(S1).operator checked +(S1, S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked +(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "(S1, S1)").WithLocation(112, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_001_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions2
{
    extension(S1)
    {
        public S1 operator {{{op}}}(int x) => throw null;
    }
}

static class Extensions3
{
    extension(S1)
    {
        void operator {{{op}}}(int x) {}
    }
    extension(C1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

struct S1
{}

static class C1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (13,28): error CS9503: The return type for this operator must be void
                //         public S1 operator +=(int x) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(13, 28),
                // (21,23): error CS9501: User-defined operator 'Extensions3.extension(S1).operator +=(int)' must be declared public
                //         void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("Extensions3.extension(S1).operator " + op + "(int)").WithLocation(21, 23),
                // (25,30): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public void operator *=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(25, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_002_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
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

                AssertEx.Equal("Extensions1." + name + "(S1, int)", method.ToDisplayString());
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
        public void CompoundAssignment_003_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
        {
            var src = $$$"""
static class Extensions1
{
    extension(S1)
    {
        {{{modifier}}}
        public void operator {{{op}}}(int x) {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
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
    extension(S1)
    {
        public void operator checked {{{op}}}(int x) {}
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions2
{
    extension(S1)
    {
#line 100
        public void operator checked {{{op}}}(int x) {}
    }
    extension(S1)
    {
        public void operator {{{op}}}(int x) {}
    }
}

static class Extensions3
{
    extension(S1)
    {
        public void operator checked {{{op}}}(int x) {}
    }
}

struct S1
{}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // PROTOTYPE: Should the pair check be performed across all extension blocks?

                // (100,38): error CS9025: The operator 'Extensions2.extension(S1).operator checked +=(int)' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions2.extension(S1).operator checked " + op + "(int)").WithLocation(100, 38),

                // (112,38): error CS9025: The operator 'Extensions3.extension(S1).operator checked +=(int)' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "(int)").WithLocation(112, 38)
                );
        }
    }
}
