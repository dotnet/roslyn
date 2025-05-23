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
            comp1.VerifyDiagnostics(
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

            // PROTOTYPE: It looks like declaring operators on nullable of receiver type is pretty useless.
            //            One can consume them only on the receiver type, not on nullable of receiver type.
            //            Should we disallow declarations like that?
            comp.VerifyDiagnostics(
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
        public void Unary_035_Consumption_True(bool fromMetadata)
        {
            var src1 = $$$"""
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
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        if (s1)
        {}
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

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

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                // (6,13): error CS0029: Cannot implicitly convert type 'S1' to 'bool'
                //         if (s1)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1", "bool").WithLocation(6, 13)
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
        s1.op_True();
        S1.op_True(s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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

            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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

            // PROTOTYPE: It looks like declaring operators on nullable of receiver type is pretty useless.
            //            One can consume them only on the receiver type, not on nullable of receiver type.
            //            Should we disallow declarations like that?
            comp.VerifyDiagnostics(
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
        public void Unary_035_Consumption_TupleComparison(bool fromMetadata)
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

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                // (6,13): error CS0029: Cannot implicitly convert type 'S1' to 'bool'
                //         if ((s1, 1) == (s1, 1))
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(s1, 1) == (s1, 1)").WithArguments("S1", "bool").WithLocation(6, 13)
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
        public void Binary_011_Consumption(bool fromMetadata)
        {
            var src1 = $$$"""
public static class Extensions1
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

public struct S1
{}
""";

            var src2 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        _ = s1 + s1;
    }
}
""";

            var comp1 = CreateCompilation(src1);
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.BinaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("Extensions1.extension(S1).operator +(S1, S1)", symbolInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("S1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp2, expectedOutput: "operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                // (6,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1' and 'S1'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1", "S1").WithLocation(6, 13)
                );

            var src3 = $$$"""
class Program
{
    static void Main()
    {
        var s1 = new S1();
        Extensions1.op_Addition(s1, s1);
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
        s1.op_Addition(s1);
        S1.op_Addition(s1, s1);
    }
}
""";
            var comp4 = CreateCompilation(src4, references: [comp1Ref]);
            comp4.VerifyDiagnostics(
                // (6,12): error CS1061: 'S1' does not contain a definition for 'op_Addition' and no accessible extension method 'op_Addition' accepting a first argument of type 'S1' could be found (are you missing a using directive or an assembly reference?)
                //         s1.op_Addition(s1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "op_Addition").WithArguments("S1", "op_Addition").WithLocation(6, 12),
                // (7,12): error CS0117: 'S1' does not contain a definition for 'op_Addition'
                //         S1.op_Addition(s1, s1);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "op_Addition").WithArguments("S1", "op_Addition").WithLocation(7, 12)
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
            comp.VerifyDiagnostics(
                // (32,17): error CS0034: Operator '-' is ambiguous on operands of type 'I2' and 'I2'
                //         var y = x - x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x - x").WithArguments("-", "I2", "I2").WithLocation(32, 17)
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

            // PROTOTYPE: We might want to include more information into the error. Like what methods conflict.
            comp.VerifyDiagnostics(
                // (34,21): error CS0034: Operator '-' is ambiguous on operands of type 'I2' and 'I2'
                //             var y = x - x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x - x").WithArguments("-", "I2", "I2").WithLocation(34, 21)
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
        public void Binary_018_Consumption_Lifted([CombinatorialValues("+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>")] string op)
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

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator1:").VerifyDiagnostics();
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
        S1? s11 = new S1();
        S2 s12 = new S2();
        S1 s21 = new S1();
        S2? s22 = new S2();
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
        _ = s21 {{{op}}} s22;
        _ = s22 {{{op}}} s21;
        System.Console.Write(":");
        s11 = null;
        s22 = null;
        _ = s11 {{{op}}} s12;
        _ = s12 {{{op}}} s11;
        _ = s21 {{{op}}} s22;
        _ = s22 {{{op}}} s21;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator1operator2operator1operator2:").VerifyDiagnostics();
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
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
            comp1.VerifyDiagnostics(
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

        [Fact]
        public void Binary_032_Consumption_Checked()
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
            comp.VerifyDiagnostics(
                // (35,13): error CS0034: Operator '-' is ambiguous on operands of type 'C1' and 'C1'
                //         _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "c1 - c1").WithArguments("-", "C1", "C1").WithLocation(35, 13),
                // (39,17): error CS0034: Operator '-' is ambiguous on operands of type 'C1' and 'C1'
                //             _ = c1 - c1;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "c1 - c1").WithArguments("-", "C1", "C1").WithLocation(39, 17)
                );

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

            // PROTOTYPE: It looks like declaring operators on nullable of receiver type is pretty useless.
            //            One can consume them only on the receiver type, not on nullable of receiver type.
            //            Should we disallow declarations like that?
            comp.VerifyDiagnostics(
                // (21,13): error CS0019: Operator '+' cannot be applied to operands of type 'S1?' and 'S1?'
                //         _ = s1 + s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 + s1").WithArguments("+", "S1?", "S1?").WithLocation(21, 13),
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
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9553: One of the parameters of a binary operator must be the extended type.
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
        public void Binary_044_Consumption_Logical(bool fromMetadata, [CombinatorialValues("&&", "||")] string op)
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
            var comp1Ref = fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference();

            var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "operator2operator1").VerifyDiagnostics();

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

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp2, expectedOutput: "operator2operator1").VerifyDiagnostics();

            comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                // (6,14): error CS0019: Operator '&&' cannot be applied to operands of type 'S1' and 'S1'
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 " + op + " s1").WithArguments(op, "S1", "S1").WithLocation(6, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_045_Consumption_Logical_InDifferentBlocks([CombinatorialValues("&&", "||")] string op)
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
            comp.VerifyDiagnostics(
                // (28,17): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('Extensions1.extension(S1).operator &(S1, S2)') must have the same return type and parameter types
                //             _ = s1 && s2;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s1 " + op + " s2").WithArguments("NS.Extensions1.extension(S1).operator " + op[0] + "(S1, S2)").WithLocation(28, 17)
                );

            comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);

            // PROTOTYPE: Should we move on to the next scope and finding Extensions2 instead of failing?
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
                // (28,17): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('Extensions1.extension(S1).operator &(S1, S1)') must have the same return type and parameter types
                //             _ = s1 && s2;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s1 " + op + " s2").WithArguments("NS.Extensions1.extension(S1).operator " + op[0] + "(S1, S1)").WithLocation(28, 17)
                );

            comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);

            // PROTOTYPE: Should we move on to the next scope and finding Extensions2 instead of failing?
            comp.VerifyDiagnostics(
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
            comp.VerifyDiagnostics(
                // (22,18): error CS0218: In order for 'NS.Extensions1.extension(C1).operator &(C1, C1)' to be applicable as a short circuit operator, its declaring type 'NS.Extensions1' must define operator true and operator false
                //         c1 = c1 && c1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "c1 " + op + " c1").WithArguments("NS.Extensions1.extension(C1).operator " + op[0] + "(C1, C1)", "NS.Extensions1").WithLocation(22, 18),
                // (25,18): error CS0218: In order for 'NS.Extensions1.extension(C1).operator &(C1, C1)' to be applicable as a short circuit operator, its declaring type 'NS.Extensions1' must define operator true and operator false
                //         c1 = c2 && c2;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "c2 " + op + " c2").WithArguments("NS.Extensions1.extension(C1).operator " + op[0] + "(C1, C1)", "NS.Extensions1").WithLocation(25, 18)
                );

            comp = CreateCompilation([src1, src2], options: TestOptions.DebugExe);

            // PROTOTYPE: Should we move on to the next scope and finding Extensions2 instead of failing?
            comp.VerifyDiagnostics(
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
            comp2.VerifyDiagnostics(
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
            comp2.VerifyDiagnostics(
                // (6,14): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 " + op + " s1").WithArguments("Extensions1.extension(S1).operator " + op[0] + "(S1, S1)", "Extensions1").WithLocation(6, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_055_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
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
        s1 = s1 {{{op}}} s1;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
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
            comp.VerifyDiagnostics(
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
        public void Binary_057_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
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
    extension(S1)
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
            comp.VerifyDiagnostics(
                // (33,14): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1?, S1?)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         s1 = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 " + op + " s1").WithArguments("Extensions1.extension(S1).operator " + op[0] + "(S1?, S1?)", "Extensions1").WithLocation(33, 14),
                // (36,9): error CS1929: 'S1?' does not contain a definition for 'M1' and the best extension method overload 'Extensions1.extension(S1).M1(S1?)' requires a receiver of type 'S1'
                //         System.Nullable<S1>.M1(s1);
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "System.Nullable<S1>").WithArguments("S1?", "M1", "Extensions1.extension(S1).M1(S1?)", "S1").WithLocation(36, 9)
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
            CompileAndVerify(comp, expectedOutput: "operator2operator1").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_059_Consumption_Logical_TrueFalseTakesNullable([CombinatorialValues("&&", "||")] string op)
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
        public void Binary_060_Consumption_Logical_TrueFalseTakesObject([CombinatorialValues("&&", "||")] string op)
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
        public void Binary_061_Consumption_Logical_TrueFalseTakesSpan([CombinatorialValues("&&", "||")] string op)
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
        public void Binary_062_Consumption_Logical_TrueFalseTakesDifferentTuple([CombinatorialValues("&&", "||")] string op)
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
        public void Binary_063_Consumption_Logical_PredefinedComesFirst()
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
        public void Binary_064_Consumption_Logical_NonExtensionComesFirst()
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
        public void Binary_065_Consumption_Logical_NonExtensionComesFirst_DifferentParameterTypes()
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

            // PROTOTYPE: Should we move on to the extensions and finding Extensions1 instead of failing?
            comp.VerifyDiagnostics(
                // (30,13): error CS0217: In order to be applicable as a short circuit operator a user-defined logical operator ('S2.operator &(S2, S1)') must have the same return type and parameter types
                //         _ = s2 && s1;
                Diagnostic(ErrorCode.ERR_BadBoolOp, "s2 && s1").WithArguments("S2.operator &(S2, S1)").WithLocation(30, 13)
                );
        }

        [Fact]
        public void Binary_066_Consumption_Logical_NonExtensionComesFirst_TrueFalseIsMissing()
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

            // PROTOTYPE: Should we move on to the extensions and finding Extensions1 instead of failing?
            comp.VerifyDiagnostics(
                // (24,13): error CS0218: In order for 'S2.operator &(S2, S2)' to be applicable as a short circuit operator, its declaring type 'S2' must define operator true and operator false
                //         _ = s2 && s2;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s2 && s2").WithArguments("S2.operator &(S2, S2)", "S2").WithLocation(24, 13)
                );
        }

        [Fact]
        public void Binary_067_Consumption_Logical_ScopeByScope()
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
        public void Binary_068_Consumption_Logical_NonExtensionAmbiguity()
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
            comp.VerifyDiagnostics(
                // (38,17): error CS0034: Operator '&&' is ambiguous on operands of type 'I2' and 'I2'
                //         var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x && x").WithArguments("&&", "I2", "I2").WithLocation(38, 17)
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
        public void Binary_069_Consumption_Logical_NonExtensionAmbiguity()
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

            // PROTOTYPE: Since neither candidate has matching true/false, should we move on to the extensions and finding Extensions1 instead of failing?
            comp.VerifyDiagnostics(
                // (34,17): error CS0034: Operator '&&' is ambiguous on operands of type 'I2' and 'I2'
                //         var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x && x").WithArguments("&&", "I2", "I2").WithLocation(34, 17)
                );
        }

        [Fact]
        public void Binary_070_Consumption_Logical_ExtensionAmbiguity()
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

            // PROTOTYPE: We might want to include more information into the error. Like what methods conflict.
            comp.VerifyDiagnostics(
                // (40,21): error CS0034: Operator '&&' is ambiguous on operands of type 'I2' and 'I2'
                //             var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x && x").WithArguments("&&", "I2", "I2").WithLocation(40, 21)
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
        public void Binary_071_Consumption_Logical_ExtensionAmbiguity()
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

            // PROTOTYPE: Since neither candidate has matching true/false, should we move on to the next scope and finding Extensions2 instead of failing?
            comp.VerifyDiagnostics(
                // (36,21): error CS0034: Operator '&&' is ambiguous on operands of type 'I2' and 'I2'
                //             var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x && x").WithArguments("&&", "I2", "I2").WithLocation(36, 21)
                );
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

            // PROTOTYPE: Since I3 candidate has no matching true/false, should we be succeeding with I1 extension instead?
            comp.VerifyDiagnostics(
                // (26,17): error CS0034: Operator '&&' is ambiguous on operands of type 'I2' and 'I2'
                //         var y = x && x;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x && x").WithArguments("&&", "I2", "I2").WithLocation(26, 17)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Binary_073_Consumption_Logical_Lifted([CombinatorialValues("&&", "||")] string op)
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
        System.Console.Write(":");
        _ = s12 {{{op}}} s11;
        System.Console.Write(":");
        _ = s11 {{{op}}} s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1:operator2:operator2:operator2").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Binary_074_Consumption_Logical_Lifted([CombinatorialValues("&&", "||")] string op)
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
        _ = s11 {{{op}}} s12;
        System.Console.Write(":");
        _ = s12 {{{op}}} s11;
        System.Console.Write(":");
        s11 = null;
        _ = s11 {{{op}}} s12;
        System.Console.Write(":");
        _ = s12 {{{op}}} s11;
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "operator2operator1:operator2operator1:operator2:operator2").VerifyDiagnostics();
        }

        [Fact]
        public void Binary_075_Consumption_Logical_LiftedIsWorse()
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
        public void Binary_076_Consumption_Logical_NoLiftedFormForTrueFalse()
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

            // PROTOTYPE: The wording is somewhat confusing because there are operators for S1, what is missing are the true/false operators for S1?.
            comp.VerifyDiagnostics(
                // (19,13): error CS0218: In order for 'Extensions1.extension(S1).operator &(S1, S1)' to be applicable as a short circuit operator, its declaring type 'Extensions1' must define operator true and operator false
                //         _ = s1 && s1;
                Diagnostic(ErrorCode.ERR_MustHaveOpTF, "s1 && s1").WithArguments("Extensions1.extension(S1).operator &(S1, S1)", "Extensions1").WithLocation(19, 13)
                );
        }

        [Fact]
        public void Binary_077_Consumption_Logical_OnObject()
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
        public void Binary_078_Consumption_Logical_NotOnDynamic()
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
        _ = s2 && s1;
    }
}
""";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.DebugExe);

            // PROTOTYPE: Note, an attempt to do compile time optimization using non-dynamic static type of 's2' ignores true/false extensions .
            //            One might say this is desirable because runtime binder wouldn't be able to use them as well.
            comp.VerifyDiagnostics(
                // (26,13): error CS7083: Expression must be implicitly convertible to Boolean or its type 'object' must define operator 'false'.
                //         _ = s2 && s1;
                Diagnostic(ErrorCode.ERR_InvalidDynamicCondition, "s2").WithArguments("object", "false").WithLocation(26, 13)
                );
        }

        [Fact]
        public void Binary_080_Consumption_Logical_WithLambda()
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
        _ = (() => 1) && (() => 1);
    }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (27,13): error CS0019: Operator '&&' cannot be applied to operands of type 'lambda expression' and 'lambda expression'
                //         _ = (() => 1) && (() => 1);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(() => 1) && (() => 1)").WithArguments("&&", "lambda expression", "lambda expression").WithLocation(27, 13)
                );
        }

        [Fact]
        public void Binary_082_Consumption_Logical_BadOperand()
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
            comp.VerifyDiagnostics(
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
        public void Binary_083_Consumption_Logical_BadReceiver()
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
            comp.VerifyDiagnostics(
                // (3,15): error CS1669: __arglist is not valid in this context
                //     extension(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(3, 15),
                // (5,39): error CS9553: One of the parameters of a binary operator must be the extended type.
                //         public static object operator &(object x, object y)
                Diagnostic(ErrorCode.ERR_BadExtensionBinaryOperatorSignature, "&").WithLocation(5, 39),
                // (9,37): error CS9551: The parameter of a unary operator must be the extended type.
                //         public static bool operator false(object x)
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "false").WithLocation(9, 37),
                // (15,37): error CS9551: The parameter of a unary operator must be the extended type.
                //         public static bool operator true(object x) => throw null;
                Diagnostic(ErrorCode.ERR_BadExtensionUnaryOperatorSignature, "true").WithLocation(15, 37),
                // (24,13): error CS0019: Operator '&&' cannot be applied to operands of type 'object' and 'object'
                //         _ = s1 && s1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "s1 && s1").WithArguments("&&", "object", "object").WithLocation(24, 13)
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

// PROTOTYPE: Test unsafe and partial, IOperation/CFG , Linq expression tree, Nullable analysis
