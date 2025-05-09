// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(41, 37),
                // (49,35): error CS9551: The parameter of a unary operator must be the extended type.
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
                // (28,28): error CS0558: User-defined operator 'Extensions3.extension(S2).operator ++(S2)' must be declared static and public
                //         public S2 operator ++(S2 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("Extensions3.extension(S2).operator " + op + "(S2)").WithLocation(28, 28),
                // (33,23): error CS0722: 'C1': static types cannot be used as return types
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C1").WithArguments("C1").WithLocation(33, 23),
                // (33,35): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(33, 35),
                // (33,38): error CS0721: 'C1': static types cannot be used as parameters
                //         public static C1 operator ++(C1 x) => default;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(33, 38),
                // (41,35): error CS9552: The parameter type for ++ or -- operator must be the extended type.
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
            comp.VerifyDiagnostics(
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 43),
                // (900,37): error CS9551: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(S1 x) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(900, 37),
                // (901,37): error CS9551: The parameter of a unary operator must be the extended type.
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
        public void Unary_007_Declaration([CombinatorialValues("+", "-", "!", "~", "++", "--")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
                // (112,43): error CS9025: The operator 'Extensions3.extension(S1).operator checked ++(S1)' requires a matching non-checked version of the operator to also be defined
                //         public static S1 operator checked ++(S1 x) => default;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(S1).operator checked " + op + "(S1)").WithLocation(112, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Unary_010_Consumption(bool fromMetadata)
        {
            var src1 = $$$"""
public static class Extensions1
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

public struct S1
{}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        _ = +s1;
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator +(S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                // (6,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "S1").WithLocation(6, 13)
                );

            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.op_UnaryPlus(s1);
    }
}
""";
            var comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "operator1").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp3, expectedOutput: "operator1").VerifyDiagnostics();

            comp3 = CreateCompilation(src3, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp3, expectedOutput: "operator1").VerifyDiagnostics();

            var src4 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1.op_UnaryPlus();
        S1.op_UnaryPlus(s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_UnaryPlus' and no accessible extension method 'op_UnaryPlus' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_UnaryPlus();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "op_UnaryPlus").WithArguments("S1", "op_UnaryPlus").WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_UnaryPlus'
                //         S1.op_UnaryPlus(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "op_UnaryPlus").WithArguments("S1", "op_UnaryPlus").WithLocation(7, 12)
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
            comp.VerifyDiagnostics(
                // (32,17): error CS0035: Operator '-' is ambiguous on an operand of type 'I2'
                //         var y = -x;
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-x").WithArguments("-", "I2").WithLocation(32, 17)
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

            // PROTOTYPE: We might want to include more information into the error. Like what methods conflict.
            comp.VerifyDiagnostics(
                // (34,21): error CS0035: Operator '-' is ambiguous on an operand of type 'I2'
                //             var y = -x;
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-x").WithArguments("-", "I2").WithLocation(34, 21)
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

        [Fact]
        public void Unary_016_Consumption_Lifted()
        {
            var src = $$$"""
public static class Extensions1
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
            CompileAndVerify(comp, expectedOutput: "operator1:").VerifyDiagnostics();
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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
        _ = +s1;
        Extensions1.op_UnaryPlus(s1);

        S1<int>? s2 = new S1<int>();
        _ = +s2;
        s2 = null;
        System.Console.Write(":");
        _ = +s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32System.Int32System.Int32:").VerifyDiagnostics();
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
            comp.VerifyDiagnostics(
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
        public void Unary_024_Consumption_Checked()
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
            comp.VerifyDiagnostics(
                // (35,13): error CS0035: Operator '-' is ambiguous on an operand of type 'C1'
                //         _ = -c1;
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-c1").WithArguments("-", "C1").WithLocation(35, 13),
                // (39,17): error CS0035: Operator '-' is ambiguous on an operand of type 'C1'
                //             _ = -c1;
                Diagnostic(ErrorCode.ERR_AmbigUnaryOp, "-c1").WithArguments("-", "C1").WithLocation(39, 17)
                );

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
        public void Unary_027_Consumption_CheckLiftedIsWorse()
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
        S1 s2 = new S1();
        _ = +s2;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);

            // PROTOTYPE: It looks like declaring operators on nullable of receiver type is pretty useless.
            //            One can consume them only on the receiver type, not on nullable of receiver type.
            //            Should we disallow declarations like that?
            comp.VerifyDiagnostics(
                // (21,13): error CS0023: Operator '+' cannot be applied to operand of type 'S1?'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "S1?").WithLocation(21, 13)
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9551: The parameter of a unary operator must be the extended type.
                //         public static object operator +(object x)
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "+").WithLocation(5, 39),
                // (17,13): error CS0023: Operator '+' cannot be applied to operand of type 'object'
                //         _ = +s1;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+s1").WithArguments("+", "object").WithLocation(17, 13)
                );
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
            comp.VerifyDiagnostics(
                // (13,28): error CS9503: The return type for this operator must be void
                //         public S1 operator ++() => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(13, 28),
                // (21,23): error CS9501: User-defined operator 'Extensions3.extension(ref S1).operator ++()' must be declared public
                //         void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("Extensions3.extension(ref S1).operator " + op + "()").WithLocation(21, 23),
                // (25,30): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(25, 30),
                // (600,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(600, 30),
                // (700,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension(ref C2 c2)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "C2").WithLocation(700, 19),
                // (800,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(800, 30),
                // (900,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(900, 18),
                // (1000,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1000, 30),
                // (1100,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(1100, 28),
                // (1200,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator ++() {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1200, 30),
                // (1300,30): error CS9557: Cannot declare instance extension operator for a type that is not known to be a struct and is not known to be a class
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

            // PROTOTYPE: Check implementation symbols like in the test above
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_005_Declaration([CombinatorialValues("++", "--")] string op)
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
            comp.VerifyDiagnostics(
                // (112,38): error CS9025: The operator 'Extensions3.extension(ref S1).operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(ref S1).operator checked " + op + "()").WithLocation(112, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_004_Declaration([CombinatorialValues("++", "--")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(42, 37),
                // (50,35): error CS9553: One of the parameters of a binary operator must be the extended type.
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(38, 39 - (op == ">>>" ? 0 : 1)),
                // (46,35): error CS9554: The first operand of an overloaded shift operator must have the same type as the extended type
                //         public static S2 operator <<(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionShiftOperatorSignature, op).WithLocation(46, 35),
                // (48,35): error CS9554: The first operand of an overloaded shift operator must have the same type as the extended type
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
            comp.VerifyDiagnostics(
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 38),
                // (900,35): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator !=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "!=").WithLocation(900, 35),
                // (904,35): error CS9553: One of the parameters of a binary operator must be the extended type.
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
            comp.VerifyDiagnostics(
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 38),
                // (900,35): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator >=(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">=").WithLocation(900, 35),
                // (904,35): error CS9553: One of the parameters of a binary operator must be the extended type.
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
            comp.VerifyDiagnostics(
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
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C1").WithArguments("C1").WithLocation(801, 37),
                // (900,35): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static S2 operator >(S1 x, S1 y) => default;
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, ">").WithLocation(900, 35),
                // (904,35): error CS9553: One of the parameters of a binary operator must be the extended type.
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
            comp.VerifyDiagnostics(
                // (13,28): error CS9503: The return type for this operator must be void
                //         public S1 operator +=(int x) => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(13, 28),
                // (21,23): error CS9501: User-defined operator 'Extensions3.extension(ref S1).operator +=(int)' must be declared public
                //         void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("Extensions3.extension(ref S1).operator " + op + "(int)").WithLocation(21, 23),
                // (25,30): error CS9555: An extension block extending a static class cannot contain user-defined operators
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorInExtensionOfStaticClass, op).WithLocation(25, 30),
                // (600,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(600, 30),
                // (700,19): error CS9300: The 'ref' receiver parameter of an extension block must be a value type or a generic type constrained to struct.
                //     extension(ref C2 c2)
                Diagnostic(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, "C2").WithLocation(700, 19),
                // (800,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(800, 30),
                // (900,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(900, 18),
                // (1000,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1000, 30),
                // (1100,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly C2 c2)
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C2").WithLocation(1100, 28),
                // (1200,30): error CS9556: Cannot declare instance operator for a struct unless containing extension block receiver parameter is a 'ref' parameter
                //         public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind, op).WithLocation(1200, 30),
                // (1300,30): error CS9557: Cannot declare instance extension operator for a type that is not known to be a struct and is not known to be a class
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
        public void CompoundAssignment_003_Declaration([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("virtual", "abstract", "new", "override", "sealed", "readonly", "extern")] string modifier)
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
            comp.VerifyDiagnostics(
                // (112,38): error CS9025: The operator 'Extensions3.extension(ref S1).operator checked +=(int)' requires a matching non-checked version of the operator to also be defined
                //         public void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("Extensions3.extension(ref S1).operator checked " + op + "(int)").WithLocation(112, 38)
                );
        }
    }
}

// PROTOTYPE: Test unsafe and partial
