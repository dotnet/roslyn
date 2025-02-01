// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class UserDefinedCompoundAssignmentOperatorsTests : CSharpTestBase
    {
        private static Verification VerifyOnMonoOrCoreClr
        {
            get
            {
                return ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped;
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_001([CombinatorialValues("++", "--")] string op, bool structure)
        {
            var source =
(structure ? "struct" : "class") + @" C1
{
    public void operator" + op + @"() {} 
    public void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular13);
            comp.VerifyDiagnostics(
                // (3,25): error CS8652: The feature 'user-defined compound assignment operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public void operator++() {} 
                Diagnostic(ErrorCode.ERR_FeatureInPreview, op).WithArguments("user-defined compound assignment operators").WithLocation(3, 25),
                // (4,33): error CS8652: The feature 'user-defined compound assignment operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_FeatureInPreview, op).WithArguments("user-defined compound assignment operators").WithLocation(4, 33)
                );

            validate(comp.SourceModule);

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_001_NotInStaticClass([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
static class C1
{
    public void operator" + op + @"() {} 
    public void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,25): error CS0715: 'C1.operator ++()': static classes cannot contain user-defined operators
                //     public void operator++() {} 
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C1.operator " + op + @"()").WithLocation(4, 25),
                // (5,33): error CS0715: 'C1.operator checked ++()': static classes cannot contain user-defined operators
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(5, 33)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_002_MustBePublic([CombinatorialValues("++", "--")] string op, bool structure, bool isChecked)
        {
            var source =
(structure ? "struct" : "class") + @" C1
{
    void operator " + (isChecked ? "checked " : "") + op + @"() {} 
" + (isChecked ? "public void operator " + op + @"() {}" : "") + @"
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,18): error CS9501: User-defined operator 'C1.operator ++()' must be declared public
                //     void operator ++() {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + (isChecked ? "checked " : "") + op + @"()").WithLocation(3, 19 + (isChecked ? 8 : 0))
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_003_MustReturnVoid([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public C1 operator " + op + @"() => throw null; 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,23): error CS9503: The return type for this operator must be void
                //     public C1 operator ++() => throw null; 
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(3, 24)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_004_MustReturnVoid_Checked([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public C1 operator checked " + op + @"() => throw null; 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,32): error CS9503: The return type for this operator must be void
                //     public C1 operator checked ++() => throw null; 
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(3, 32),
                // (3,32): error CS9025: The operator 'C1.operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //     public C1 operator checked ++() => throw null; 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "()").WithLocation(3, 32)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_005_WrongNumberOfParameters([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator" + op + @"(C1 x) {} 
    public void operator" + op + @"(C1 x, C1 y) {} 
    public void operator" + op + @"(C1 x, C1 y, C1 z) {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,25): error CS9502: Overloaded instance increment operator '++' takes no parameters
                //     public void operator++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_BadIncrementOpArgs, op).WithArguments(op).WithLocation(3, 25),
                // (4,25): error CS1020: Overloadable binary operator expected
                //     public void operator++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 25),
                // (5,25): error CS9502: Overloaded instance increment operator '++' takes no parameters
                //     public void operator++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_BadIncrementOpArgs, op).WithArguments(op).WithLocation(5, 25)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_006_WrongNumberOfParameters_Checked([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator checked " + op + @"(C1 x) {} 
    public void operator checked " + op + @"(C1 x, C1 y) {} 
    public void operator checked " + op + @"(C1 x, C1 y, C1 z) {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,34): error CS9502: Overloaded instance increment operator '++' takes no parameters
                //     public void operator checked ++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_BadIncrementOpArgs, op).WithArguments(op).WithLocation(3, 34),
                // (3,34): error CS9025: The operator 'C1.operator checked ++(C1)' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked ++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1)").WithLocation(3, 34),
                // (4,34): error CS1020: Overloadable binary operator expected
                //     public void operator checked ++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 34),
                // (4,34): error CS9025: The operator 'C1.operator checked ++(C1, C1)' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked ++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1, C1)").WithLocation(4, 34),
                // (5,34): error CS9502: Overloaded instance increment operator '++' takes no parameters
                //     public void operator checked ++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_BadIncrementOpArgs, op).WithArguments(op).WithLocation(5, 34),
                // (5,34): error CS9025: The operator 'C1.operator checked ++(C1, C1, C1)' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked ++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1, C1, C1)").WithLocation(5, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_007_WrongNumberOfParameters_ForStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public static C1 operator" + op + @"() => throw null;
    public static C1 operator" + op + @"(C1 x) => throw null;
    public static C1 operator" + op + @"(C1 x, C1 y) => throw null;
    public static C1 operator" + op + @"(C1 x, C1 y, C1 z) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,30): error CS1535: Overloaded unary operator '++' takes one parameter
                //     public static C1 operator++() => throw null;
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(3, 30),
                // (5,30): error CS1020: Overloadable binary operator expected
                //     public static C1 operator++(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(5, 30),
                // (6,30): error CS1535: Overloaded unary operator '++' takes one parameter
                //     public static C1 operator++(C1 x, C1 y, C1 z) => throw null;
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(6, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_008_WrongNumberOfParameters_ForStatic_Checked([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public static C1 operator checked " + op + @"() => throw null;
    public static C1 operator checked " + op + @"(C1 x) => throw null;
    public static C1 operator " + op + @"(C1 x) => throw null;
    public static C1 operator checked " + op + @"(C1 x, C1 y) => throw null;
    public static C1 operator checked " + op + @"(C1 x, C1 y, C1 z) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,39): error CS1535: Overloaded unary operator '++' takes one parameter
                //     public static C1 operator checked ++() => throw null;
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(3, 39),
                // (3,39): error CS9025: The operator 'C1.operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //     public static C1 operator checked ++() => throw null;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "()").WithLocation(3, 39),
                // (6,39): error CS1020: Overloadable binary operator expected
                //     public static C1 operator checked ++(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(6, 39),
                // (6,39): error CS9025: The operator 'C1.operator checked ++(C1, C1)' requires a matching non-checked version of the operator to also be defined
                //     public static C1 operator checked ++(C1 x, C1 y) => throw null;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1, C1)").WithLocation(6, 39),
                // (7,39): error CS1535: Overloaded unary operator '++' takes one parameter
                //     public static C1 operator checked ++(C1 x, C1 y, C1 z) => throw null;
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(7, 39),
                // (7,39): error CS9025: The operator 'C1.operator checked ++(C1, C1, C1)' requires a matching non-checked version of the operator to also be defined
                //     public static C1 operator checked ++(C1 x, C1 y, C1 z) => throw null;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1, C1, C1)").WithLocation(7, 39)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_009_AbstractAllowedInClassAndInterface([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public abstract void operator" + op + @"();
    public abstract void operator checked" + op + @"(); 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.True(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }

            comp = CreateCompilation(["class C2 : C1 {}", source]);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (1,12): error CS0535: 'C2' does not implement interface member 'C1.operator ++()'
                    // class C2 : C1 {}
                    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "C1").WithArguments("C2", "C1.operator " + op + @"()").WithLocation(1, 12),
                    // (1,12): error CS0535: 'C2' does not implement interface member 'C1.operator checked ++()'
                    // class C2 : C1 {}
                    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "C1").WithArguments("C2", "C1.operator checked " + op + @"()").WithLocation(1, 12)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (1,7): error CS0534: 'C2' does not implement inherited abstract member 'C1.operator checked ++()'
                    // class C2 : C1 {}
                    Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C2").WithArguments("C2", "C1.operator checked " + op + @"()").WithLocation(1, 7),
                    // (1,7): error CS0534: 'C2' does not implement inherited abstract member 'C1.operator ++()'
                    // class C2 : C1 {}
                    Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C2").WithArguments("C2", "C1.operator " + op + @"()").WithLocation(1, 7)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_010_AbstractIsOptionalInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface C1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.True(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_011_AbstractCanBeImplementedInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

interface I3 : I1
{
    void I1.operator " + op + @"() {}
}

interface I4 : I2
{
    void I2.operator checked " + op + @"() {}
}

class C : I3, I4
{}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName) + "()");
            }

            static void validateOp(MethodSymbol m, string implements)
            {
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.False(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
                Assert.Equal(implements, m.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_012_AbstractCanBeImplementedExplicitlyInClassAndStruct([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName) + "()");
            }

            static void validateOp(MethodSymbol m, string implements)
            {
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.False(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
                Assert.Equal(implements, m.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_013_AbstractCanBeImplementedImplicitlyInClassAndStruct([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    public void operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    public void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)).Single());
                validateOp(m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)).Single());
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);

                if (m is PEMethodSymbol)
                {
                    Assert.True(m.IsMetadataVirtual());
                    Assert.True(m.IsMetadataFinal);
                }

                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_014_AbstractAllowedOnExplicitImplementationInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"() {}
}

interface I2
{
    void operator checked " + op + @"() {}
    sealed void operator " + op + @"() {}
}

interface I3 : I1
{
    abstract void I1.operator " + op + @"();
}

interface I4 : I2
{
    abstract void I2.operator checked " + op + @"();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName) + "()");
            }

            static void validateOp(MethodSymbol m, string implements)
            {
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.True(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.True(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.False(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
                Assert.Equal(implements, m.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            }

            comp = CreateCompilation(["class C1 : I3, I4 {}", source], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (1,12): error CS0535: 'C1' does not implement interface member 'I1.operator ++()'
                // class C1 : I3, I4 {}
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("C1", "I1.operator " + op + @"()").WithLocation(1, 12),
                // (1,16): error CS0535: 'C1' does not implement interface member 'I2.operator checked ++()'
                // class C1 : I3, I4 {}
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I4").WithArguments("C1", "I2.operator checked " + op + @"()").WithLocation(1, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_015_AbstractCannotHaveBody([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public abstract void operator" + op + @"() {}
    public abstract void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,34): error CS0500: 'C1.operator ++()' cannot declare a body because it is marked abstract
                //     public abstract void operator++() {}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 34),
                // (4,42): error CS0500: 'C1.operator checked ++()' cannot declare a body because it is marked abstract
                //     public abstract void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_016_AbstractExplicitImplementationCannotHaveBody([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator" + op + @"();
    void operator checked" + op + @"();
}

interface I2 : I1
{
   abstract void I1.operator" + op + @"() {}
   abstract void I1.operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (10,29): error CS0500: 'I2.I1.operator ++()' cannot declare a body because it is marked abstract
                //    abstract void I1.operator++() {}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("I2.I1.operator " + op + @"()").WithLocation(10, 29),
                // (11,37): error CS0500: 'I2.I1.operator checked ++()' cannot declare a body because it is marked abstract
                //    abstract void I1.operator checked++() {} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("I2.I1.operator checked " + op + @"()").WithLocation(11, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_017_AbstractNotAllowedInNonAbstractClass([CombinatorialValues("++", "--")] string op, [CombinatorialValues("sealed", "")] string typeModifier)
        {
            var source = @"
" + typeModifier + @" class C1
{
    public abstract void operator" + op + @"();
}

" + typeModifier + @" class C2
{
    public abstract void operator checked " + op + @"();
    public void operator " + op + @"() {}
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,34): error CS0513: 'C1.operator ++()' is abstract but it is contained in non-abstract type 'C1'
                //     public abstract void operator++();
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, op).WithArguments("C1.operator " + op + @"()", "C1").WithLocation(4, 34),
                // (9,43): error CS0513: 'C2.operator checked ++()' is abstract but it is contained in non-abstract type 'C2'
                //     public abstract void operator checked ++();
                Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, op).WithArguments("C2.operator checked " + op + @"()", "C2").WithLocation(9, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_018_AbstractNotAllowedInClass_OnStatic([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
abstract class C1
{
    public static abstract C1 operator" + op + @"(C1 x) => throw null;
}

abstract class C2
{
    public static abstract C2 operator checked " + op + @"(C2 x) => throw null;
    public static C2 operator " + op + @"(C2 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,39): error CS0112: A static member cannot be marked as 'abstract'
                //     public static abstract C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("abstract").WithLocation(4, 39),
                // (9,48): error CS0112: A static member cannot be marked as 'abstract'
                //     public static abstract C2 operator checked ++(C2 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("abstract").WithLocation(9, 48)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_019_AbstractNotAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct C1
{
    public abstract void operator" + op + @"() {}
}

struct C2
{
    public abstract void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

struct C3
{
    public static abstract C3 operator" + op + @"(C3 x) => throw null;
}

struct C4
{
    public static abstract C4 operator checked " + op + @"(C4 x) => throw null;
    public static C4 operator " + op + @"(C4 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,34): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 34),
                // (9,43): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(9, 43),
                // (15,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static abstract C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(15, 39),
                // (20,48): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static abstract C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(20, 48)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_020_AbstractNotAllowedOnExplicitImplementationInClassAndStruct([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    abstract void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    abstract void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,31): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(15, 31),
                // (20,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(20, 39)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_021_VirtualAllowedInClassAndInterface([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual void operator" + op + @"() {}
    public virtual void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.True(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_022_VirtualIsOptionalInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface C1
{
    void operator" + op + @"() {}
    void operator checked" + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.True(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_023_VirtualCanBeImplementedInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    virtual void operator " + op + @"() {}
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

interface I3 : I1
{
    void I1.operator " + op + @"() {}
}

interface I4 : I2
{
    void I2.operator checked " + op + @"() {}
}

class C : I3, I4
{}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName) + "()");
            }

            static void validateOp(MethodSymbol m, string implements)
            {
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.False(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
                Assert.Equal(implements, m.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_024_VirtualCanBeImplementedExplicitlyInClassAndStruct([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    virtual void operator " + op + @"() {}
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName) + "()");
            }

            static void validateOp(MethodSymbol m, string implements)
            {
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.False(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Private, m.DeclaredAccessibility);
                Assert.Equal(implements, m.ExplicitInterfaceImplementations.Single().ToTestDisplayString());
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_025_VirtualCanBeImplementedImplicitlyInClassAndStruct([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    virtual void operator " + op + @"() {}
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    public void operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    public void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)).Single());
                validateOp(m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)).Single());
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);

                if (m is PEMethodSymbol)
                {
                    Assert.True(m.IsMetadataVirtual());
                    Assert.True(m.IsMetadataFinal);
                }

                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_026_VirtualMustHaveBody([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual void operator" + op + @"();
    public virtual void operator checked" + op + @"(); 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,33): error CS0501: 'C1.operator ++()' must declare a body because it is not marked abstract, extern, or partial
                //     public virtual void operator++();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 33),
                // (4,41): error CS0501: 'C1.operator checked ++()' must declare a body because it is not marked abstract, extern, or partial
                //     public virtual void operator checked++(); 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_027_VirtualNotAllowedInSealedClass([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
sealed class C1
{
    public virtual void operator" + op + @"() {}
}

sealed class C2
{
    public virtual void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,33): error CS0549: 'C1.operator ++()' is a new virtual member in sealed type 'C1'
                //     public virtual void operator++() {}
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, op).WithArguments("C1.operator " + op + @"()", "C1").WithLocation(4, 33),
                // (9,42): error CS0549: 'C2.operator checked ++()' is a new virtual member in sealed type 'C2'
                //     public virtual void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, op).WithArguments("C2.operator checked " + op + @"()", "C2").WithLocation(9, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_028_VirtualNotAllowedInClass_OnStatic([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
class C1
{
    public static virtual C1 operator" + op + @"(C1 x) => throw null;
}

class C2
{
    public static virtual C2 operator checked " + op + @"(C2 x) => throw null;
    public static C2 operator " + op + @"(C2 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,38): error CS0112: A static member cannot be marked as 'virtual'
                //     public static virtual C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("virtual").WithLocation(4, 38),
                // (9,47): error CS0112: A static member cannot be marked as 'virtual'
                //     public static virtual C2 operator checked ++(C2 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("virtual").WithLocation(9, 47)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_029_VirtualNotAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct C1
{
    public virtual void operator" + op + @"() {}
}

struct C2
{
    public virtual void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

struct C3
{
    public static virtual C3 operator" + op + @"(C3 x) => throw null;
}

struct C4
{
    public static virtual C4 operator checked " + op + @"(C4 x) => throw null;
    public static C4 operator " + op + @"(C4 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,33): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(4, 33),
                // (9,42): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(9, 42),
                // (15,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     public static virtual C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(15, 38),
                // (20,47): error CS0106: The modifier 'virtual' is not valid for this item
                //     public static virtual C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(20, 47)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_030_VirtualNotAllowedOnExplicitImplementation([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    virtual void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    virtual void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,30): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(15, 30),
                // (20,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(20, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_031_VirtualNotAllowedOnExplicitImplementation_OnStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    static abstract I1 operator " + op + @"(I1 x);
}

interface I2
{
    static abstract I2 operator checked " + op + @"(I2 x);
    static sealed I2 operator " + op + @"(I2 x) => throw null;
}

" + typeKeyword + @" C3 : I1
{
    static virtual I1 I1.operator " + op + @"(I1 x) => throw null;
}

" + typeKeyword + @" C4 : I2
{
    static virtual I2 I2.operator checked " + op + @"(I2 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,35): error CS0106: The modifier 'virtual' is not valid for this item
                //     static virtual I1 I1.operator ++(I1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(15, 35),
                // (20,43): error CS0106: The modifier 'virtual' is not valid for this item
                //     static virtual I2 I2.operator checked ++(I2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(20, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_032_VirtualAbstractNotAllowed([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual abstract void operator" + op + @"() {}
    public virtual abstract void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,42): error CS0503: The abstract method 'C1.operator ++()' cannot be marked virtual
                //     public virtual abstract void operator++() {}
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, op).WithArguments("method", "C1.operator " + op + @"()").WithLocation(3, 42),
                // (4,50): error CS0503: The abstract method 'C1.operator checked ++()' cannot be marked virtual
                //     public virtual abstract void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, op).WithArguments("method", "C1.operator checked " + op + @"()").WithLocation(4, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_033_OverrideAllowedInClass([CombinatorialValues("++", "--")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"() " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"() " + (abstractInBase ? ";" : "{}") + @" 
}

class C2 : C1
{
    public override void operator" + op + @"() {}
    public override void operator checked" + op + @"() {} 
}

class C3 : C2
{
    public override void operator" + op + @"() {}
    public override void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            var source2 = @"
abstract class C1
{
    public virtual void operator" + op + @"() {}
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"() " + (abstractInBase ? ";" : "{}") + @" 
}

class C2 : C1
{
    public override void operator checked" + op + @"() {} 
}
";
            var comp2 = CreateCompilation(source2);

            // PROTOTYPE: The error below is not expected. One should be able to override just one version of the operator.
            comp2.VerifyDiagnostics(
                // (10,42): error CS9025: The operator 'C2.operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //     public override void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(10, 42)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m, MethodSymbol overridden)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.True(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
                Assert.Same(overridden, m.OverriddenMethod);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_034_AbstractOverrideAllowedInClass([CombinatorialValues("++", "--")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"() " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"() " + (abstractInBase ? ";" : "{}") + @" 
}

abstract class C2 : C1
{
    public abstract override void operator" + op + @"();
    public abstract override void operator checked" + op + @"(); 
}

class C3 : C2
{
    public override void operator" + op + @"() {}
    public override void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m, MethodSymbol overridden)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.Equal(m.ContainingType.Name == "C2", m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.True(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
                Assert.Same(overridden, m.OverriddenMethod);
            }

            comp = CreateCompilation(["class C4 : C2 {}", source]);
            comp.VerifyDiagnostics(
                // (1,7): error CS0534: 'C4' does not implement inherited abstract member 'C2.operator checked ++()'
                // class C3 : C2 {}
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C4").WithArguments("C4", "C2.operator checked " + op + @"()").WithLocation(1, 7),
                // (1,7): error CS0534: 'C4' does not implement inherited abstract member 'C2.operator ++()'
                // class C3 : C2 {}
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C4").WithArguments("C4", "C2.operator " + op + @"()").WithLocation(1, 7)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_035_OverrideAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct S1
{
    public override void operator" + op + @"() {}
    public override void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);

            validateOp(comp.GetMember<MethodSymbol>("S1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
            validateOp(comp.GetMember<MethodSymbol>("S1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));

            comp.VerifyDiagnostics(
                // (4,34): error CS0115: 'S1.operator ++()': no suitable method found to override
                //     public override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator " + op + @"()").WithLocation(4, 34),
                // (5,42): error CS0115: 'S1.operator checked ++()': no suitable method found to override
                //     public override void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator checked " + op + @"()").WithLocation(5, 42)
                );

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.True(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
                Assert.Null(m.OverriddenMethod);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_036_OverrideNotAllowed_OnStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public static override C1 operator" + op + @"(C1 x) => throw null;
}

" + typeKeyword + @" C2
{
    public static override C2 operator checked " + op + @"(C2 x) => throw null;
    public static C2 operator " + op + @"(C2 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,39): error CS0112: A static member cannot be marked as 'override'
                //     public static override C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("override").WithLocation(3, 39),
                // (8,48): error CS0112: A static member cannot be marked as 'override'
                //     public static override C2 operator checked ++(C2 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("override").WithLocation(8, 48)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_037_OverrideNotAllowedInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    public override void operator" + op + @"() {}
}

interface I2
{
    public override void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

interface I3
{
    public static override I3 operator" + op + @"(I3 x) => throw null;
}

interface I4
{
    public static override I4 operator checked " + op + @"(I4 x) => throw null;
    public static I4 operator " + op + @"(I4 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,34): error CS0106: The modifier 'override' is not valid for this item
                //     public override void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(4, 34),
                // (9,43): error CS0106: The modifier 'override' is not valid for this item
                //     public override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(9, 43),
                // (15,39): error CS0106: The modifier 'override' is not valid for this item
                //     public static override I3 operator++(I3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 39),
                // (20,48): error CS0106: The modifier 'override' is not valid for this item
                //     public static override I4 operator checked ++(I4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(20, 48)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_038_OverrideNotAllowedOnExplicitImplementation([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    override void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    override void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,31): error CS0106: The modifier 'override' is not valid for this item
                //     override void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 31),
                // (20,39): error CS0106: The modifier 'override' is not valid for this item
                //     override void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(20, 39)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_039_OverrideNotAllowedOnExplicitImplementation_OnStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    static abstract I1 operator " + op + @"(I1 x);
}

interface I2
{
    static abstract I2 operator checked " + op + @"(I2 x);
    static sealed I2 operator " + op + @"(I2 x) => throw null;
}

" + typeKeyword + @" C3 : I1
{
    static override I1 I1.operator " + op + @"(I1 x) => throw null;
}

" + typeKeyword + @" C4 : I2
{
    static override I2 I2.operator checked " + op + @"(I2 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,36): error CS0106: The modifier 'override' is not valid for this item
                //     static override I1 I1.operator ++(I1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 36),
                // (20,44): error CS0106: The modifier 'override' is not valid for this item
                //     static override I2 I2.operator checked ++(I2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(20, 44)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_040_VirtualOverrideNotAllowed([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"() {}
    public virtual void operator checked" + op + @"() {} 
}

class C2 : C1
{
    public virtual override void operator" + op + @"() {}
    public virtual override void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,42): error CS0113: A member 'C2.operator ++()' marked as override cannot be marked as new or virtual
                //     public virtual override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C2.operator " + op + @"()").WithLocation(10, 42),
                // (11,50): error CS0113: A member 'C2.operator checked ++()' marked as override cannot be marked as new or virtual
                //     public virtual override void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(11, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_041_SealedAllowedInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface C1
{
    sealed void operator" + op + @"() {}
    sealed void operator checked" + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }

            var source2 = @"
class C3 : C1
{
    void C1.operator" + op + @"() {}
    void C1.operator checked" + op + @"() {} 
}
";
            comp = CreateCompilation([source2, source], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,21): error CS0539: 'C3.operator ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void C1.operator++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C3.operator " + op + @"()").WithLocation(4, 21),
                // (5,29): error CS0539: 'C3.operator checked ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void C1.operator checked++() {} 
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C3.operator checked " + op + @"()").WithLocation(5, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_042_SealedOverrideAllowedInClass([CombinatorialValues("++", "--")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"() " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"() " + (abstractInBase ? ";" : "{}") + @" 
}

abstract class C2 : C1
{
    public sealed override void operator" + op + @"() {}
    public sealed override void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m, MethodSymbol overridden)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.True(m.IsSealed);
                Assert.True(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
                Assert.Same(overridden, m.OverriddenMethod);
            }

            var source2 = @"
class C3 : C2
{
    public override void operator" + op + @"() {}
    public override void operator checked" + op + @"() {} 
}
";
            comp = CreateCompilation([source2, source]);
            comp.VerifyDiagnostics(
                // (4,34): error CS0239: 'C3.operator ++()': cannot override inherited member 'C2.operator ++()' because it is sealed
                //     public override void operator++() {}
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, op).WithArguments("C3.operator " + op + @"()", "C2.operator " + op + @"()").WithLocation(4, 34),
                // (5,42): error CS0239: 'C3.operator checked ++()': cannot override inherited member 'C2.operator checked ++()' because it is sealed
                //     public override void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, op).WithArguments("C3.operator checked " + op + @"()", "C2.operator checked " + op + @"()").WithLocation(5, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_043_SealedNotAllowedInClass([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
class C1
{
    public sealed void operator" + op + @"() {}
}

class C2
{
    public sealed void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

class C3
{
    public static sealed C3 operator" + op + @"(C3 x) => throw null;
}

class C4
{
    public static sealed C4 operator checked " + op + @"(C4 x) => throw null;
    public static C4 operator " + op + @"(C4 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,32): error CS0238: 'C1.operator ++()' cannot be sealed because it is not an override
                //     public sealed void operator++() {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"()").WithLocation(4, 32),
                // (9,41): error CS0238: 'C2.operator checked ++()' cannot be sealed because it is not an override
                //     public sealed void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(9, 41),
                // (15,37): error CS0238: 'C3.operator ++(C3)' cannot be sealed because it is not an override
                //     public static sealed C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C3.operator " + op + @"(C3)").WithLocation(15, 37),
                // (20,46): error CS0238: 'C4.operator checked ++(C4)' cannot be sealed because it is not an override
                //     public static sealed C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C4.operator checked " + op + @"(C4)").WithLocation(20, 46)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_044_SealedNotAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct C1
{
    public sealed void operator" + op + @"() {}
}

struct C2
{
    public sealed void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

struct C3
{
    public static sealed C3 operator" + op + @"(C3 x) => throw null;
}

struct C4
{
    public static sealed C4 operator checked " + op + @"(C4 x) => throw null;
    public static C4 operator " + op + @"(C4 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,32): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 32),
                // (9,41): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(9, 41),
                // (15,37): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 37),
                // (20,46): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 46)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_045_SealedOverrideNotAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct C1
{
    public sealed override void operator" + op + @"() {}
}

struct C2
{
    public sealed override void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

struct C3
{
    public static sealed override C3 operator" + op + @"(C3 x) => throw null;
}

struct C4
{
    public static sealed override C4 operator checked " + op + @"(C4 x) => throw null;
    public static C4 operator " + op + @"(C4 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,41): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 41),
                // (4,41): error CS0115: 'C1.operator ++()': no suitable method found to override
                //     public sealed override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C1.operator " + op + @"()").WithLocation(4, 41),
                // (9,50): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(9, 50),
                // (9,50): error CS0115: 'C2.operator checked ++()': no suitable method found to override
                //     public sealed override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(9, 50),
                // (15,46): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 46),
                // (15,46): error CS0112: A static member cannot be marked as 'override'
                //     public static sealed override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("override").WithLocation(15, 46),
                // (20,55): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 55),
                // (20,55): error CS0112: A static member cannot be marked as 'override'
                //     public static sealed override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("override").WithLocation(20, 55)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_046_SealedAbstractOverrideNotAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct C1
{
    public sealed abstract override void operator" + op + @"() {}
}

struct C2
{
    public sealed abstract override void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

struct C3
{
    public static sealed abstract override C3 operator" + op + @"(C3 x) => throw null;
}

struct C4
{
    public static sealed abstract override C4 operator checked " + op + @"(C4 x) => throw null;
    public static C4 operator " + op + @"(C4 x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,50): error CS0106: The modifier 'abstract' is not valid for this item
                //     public sealed abstract override void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 50),
                // (4,50): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed abstract override void operator++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 50),
                // (4,50): error CS0115: 'C1.operator ++()': no suitable method found to override
                //     public sealed abstract override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C1.operator " + op + @"()").WithLocation(4, 50),
                // (9,59): error CS0106: The modifier 'abstract' is not valid for this item
                //     public sealed abstract override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(9, 59),
                // (9,59): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed abstract override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(9, 59),
                // (9,59): error CS0115: 'C2.operator checked ++()': no suitable method found to override
                //     public sealed abstract override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(9, 59),
                // (15,55): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static sealed abstract override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(15, 55),
                // (15,55): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed abstract override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 55),
                // (15,55): error CS0112: A static member cannot be marked as 'override'
                //     public static sealed abstract override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("override").WithLocation(15, 55),
                // (20,64): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static sealed abstract override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(20, 64),
                // (20,64): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed abstract override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 64),
                // (20,64): error CS0112: A static member cannot be marked as 'override'
                //     public static sealed abstract override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticNotVirtual, op).WithArguments("override").WithLocation(20, 64)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_047_SealedNotAllowedOnExplicitImplementation([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    sealed void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    sealed void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,29): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 29),
                // (20,37): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_048_SealedNotAllowedOnExplicitImplementation_OnStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    static abstract I1 operator " + op + @"(I1 x);
}

interface I2
{
    static abstract I2 operator checked " + op + @"(I2 x);
    static sealed I2 operator " + op + @"(I2 x) => throw null;
}

" + typeKeyword + @" C3 : I1
{
    static sealed I1 I1.operator " + op + @"(I1 x) => throw null;
}

" + typeKeyword + @" C4 : I2
{
    static sealed I2 I2.operator checked " + op + @"(I2 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,34): error CS0106: The modifier 'sealed' is not valid for this item
                //     static sealed I1 I1.operator ++(I1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 34),
                // (20,42): error CS0106: The modifier 'sealed' is not valid for this item
                //     static sealed I2 I2.operator checked ++(I2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_049_SealedAbstractNotAllowed([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public sealed abstract void operator" + op + @"();
    public sealed abstract void operator checked" + op + @"(); 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (3,41): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public sealed abstract void operator--();
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(3, 41),
                    // (4,49): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public sealed abstract void operator checked--(); 
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 49)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,41): error CS0238: 'C1.operator ++()' cannot be sealed because it is not an override
                    //     public sealed abstract void operator++();
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 41),
                    // (4,49): error CS0238: 'C1.operator checked ++()' cannot be sealed because it is not an override
                    //     public sealed abstract void operator checked++(); 
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 49)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_050_SealedAbstractOverrideNotAllowedInClass([CombinatorialValues("++", "--")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"() " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"() " + (abstractInBase ? ";" : "{}") + @" 
}

abstract class C2 : C1
{
    public sealed abstract override void operator" + op + @"();
    public sealed abstract override void operator checked" + op + @"();
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,50): error CS0502: 'C2.operator ++()' cannot be both abstract and sealed
                //     public sealed abstract override void operator++();
                Diagnostic(ErrorCode.ERR_AbstractAndSealed, op).WithArguments("C2.operator " + op + @"()").WithLocation(10, 50),
                // (11,58): error CS0502: 'C2.operator checked ++()' cannot be both abstract and sealed
                //     public sealed abstract override void operator checked++();
                Diagnostic(ErrorCode.ERR_AbstractAndSealed, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(11, 58)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_051_SealedAbstractNotAllowedOnExplicitImplementation([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

interface I3 : I1
{
    sealed abstract void I1.operator " + op + @"();
}

interface I4 : I2
{
    sealed abstract void I2.operator checked " + op + @"();
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,38): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed abstract void I1.operator ++();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 38),
                // (20,46): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed abstract void I2.operator checked ++();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 46)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_052_SealedAbstractNotAllowedOnExplicitImplementation_OnStatic([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    static abstract I1 operator " + op + @"(I1 x);
}

interface I2
{
    static abstract I2 operator checked " + op + @"(I2 x);
    static sealed I2 operator " + op + @"(I2 x) => throw null;
}

interface I3 : I1
{
    static sealed abstract I1 I1.operator " + op + @"(I1 x);
}

interface I4 : I2
{
    static sealed abstract I2 I2.operator checked " + op + @"(I2 x);
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,43): error CS0106: The modifier 'sealed' is not valid for this item
                //     static sealed abstract I1 I1.operator ++(I1 x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 43),
                // (20,51): error CS0106: The modifier 'sealed' is not valid for this item
                //     static sealed abstract I2 I2.operator checked ++(I2 x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 51)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_053_SealedVirtualNotAllowed([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual sealed void operator" + op + @"() {}
    public virtual sealed void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (3,40): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public virtual sealed void operator++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(3, 40),
                    // (4,48): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public virtual sealed void operator checked++() {} 
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 48)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,40): error CS0238: 'C1.operator ++()' cannot be sealed because it is not an override
                    //     public virtual sealed void operator++() {}
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 40),
                    // (4,48): error CS0238: 'C1.operator checked ++()' cannot be sealed because it is not an override
                    //     public virtual sealed void operator checked++() {} 
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 48)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_054_NewAllowedInInterface_WRN_NewRequired([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract", "virtual", "sealed")] string baseModifier)
        {
            var source = @"
interface C1
{
    public " + baseModifier + @" void operator" + op + @"() " + (baseModifier == "abstract" ? ";" : "{}") + @"
    public " + baseModifier + @" void operator checked" + op + @"() " + (baseModifier == "abstract" ? ";" : "{}") + @" 
}

interface C2 : C1
{
    public void operator" + op + @"() {}
    public void operator checked" + op + @"() {} 
}

interface C3 : C1
{
    public new void operator" + op + @"() {}
    public new void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (10,25): warning CS0108: 'C2.operator ++()' hides inherited member 'C1.operator ++()'. Use the new keyword if hiding was intended.
                //     public void operator++() {}
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator " + op + @"()", "C1.operator " + op + @"()").WithLocation(10, 25),
                // (11,33): warning CS0108: 'C2.operator checked ++()' hides inherited member 'C1.operator checked ++()'. Use the new keyword if hiding was intended.
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator checked " + op + @"()", "C1.operator checked " + op + @"()").WithLocation(11, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.True(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_055_NewAllowedInClass_WRN_NewRequired([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
class C1
{
    public void operator" + op + @"() {}
    public void operator checked" + op + @"() {}
}

class C2 : C1
{
    public void operator" + op + @"() {}
    public void operator checked" + op + @"() {} 
}

class C3 : C1
{
    public new void operator" + op + @"() {}
    public new void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics(
                // (10,25): warning CS0108: 'C2.operator ++()' hides inherited member 'C1.operator ++()'. Use the new keyword if hiding was intended.
                //     public void operator++() {}
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator " + op + @"()", "C1.operator " + op + @"()").WithLocation(10, 25),
                // (11,33): warning CS0108: 'C2.operator checked ++()' hides inherited member 'C1.operator checked ++()'. Use the new keyword if hiding was intended.
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator checked " + op + @"()", "C1.operator checked " + op + @"()").WithLocation(11, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_056_NewAllowedInClass_WRN_NewOrOverrideExpected([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"() {}
    public virtual void operator checked" + op + @"() {}
}

class C2 : C1
{
    public void operator" + op + @"() {}
    public void operator checked" + op + @"() {} 
}

class C3 : C1
{
    public new void operator" + op + @"() {}
    public new void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics(
                // (10,25): warning CS0114: 'C2.operator ++()' hides inherited member 'C1.operator ++()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator++() {}
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator " + op + @"()", "C1.operator " + op + @"()").WithLocation(10, 25),
                // (11,33): warning CS0114: 'C2.operator checked ++()' hides inherited member 'C1.operator checked ++()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator checked " + op + @"()", "C1.operator checked " + op + @"()").WithLocation(11, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_057_NewAllowedInClass_ERR_HidingAbstractMethod([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
abstract class C1
{
    public abstract void operator" + op + @"();
    public abstract void operator checked" + op + @"();
}

abstract class C2 : C1
{
    public void operator" + op + @"() {}
    public void operator checked" + op + @"() {} 
}

abstract class C3 : C1
{
    public new void operator" + op + @"() {}
    public new void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,25): error CS0533: 'C2.operator ++()' hides inherited abstract member 'C1.operator ++()'
                //     public void operator++() {}
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C2.operator " + op + @"()", "C1.operator " + op + @"()").WithLocation(10, 25),
                // (10,25): warning CS0114: 'C2.operator ++()' hides inherited member 'C1.operator ++()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator++() {}
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator " + op + @"()", "C1.operator " + op + @"()").WithLocation(10, 25),
                // (11,33): error CS0533: 'C2.operator checked ++()' hides inherited abstract member 'C1.operator checked ++()'
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C2.operator checked " + op + @"()", "C1.operator checked " + op + @"()").WithLocation(11, 33),
                // (11,33): warning CS0114: 'C2.operator checked ++()' hides inherited member 'C1.operator checked ++()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator checked " + op + @"()", "C1.operator checked " + op + @"()").WithLocation(11, 33),
                // (16,29): error CS0533: 'C3.operator ++()' hides inherited abstract member 'C1.operator ++()'
                //     public new void operator++() {}
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C3.operator " + op + @"()", "C1.operator " + op + @"()").WithLocation(16, 29),
                // (17,37): error CS0533: 'C3.operator checked ++()' hides inherited abstract member 'C1.operator checked ++()'
                //     public new void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C3.operator checked " + op + @"()", "C1.operator checked " + op + @"()").WithLocation(17, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_058_NewAllowed_WRN_NewNotRequired([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new void operator" + op + @"() {}
    public new void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,29): warning CS0109: The member 'C1.operator ++()' does not hide an accessible member. The new keyword is not required.
                //     public new void operator++() {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 29),
                // (4,37): warning CS0109: The member 'C1.operator checked ++()' does not hide an accessible member. The new keyword is not required.
                //     public new void operator checked++() {} 
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 37)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.Equal(typeKeyword == "interface", m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_059_NewAbstractAllowedInClassAndInterface([CombinatorialValues("++", "--")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new abstract void operator" + op + @"();
    public new abstract void operator checked" + op + @"(); 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,38): warning CS0109: The member 'C1.operator ++()' does not hide an accessible member. The new keyword is not required.
                //     public new abstract void operator++();
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 38),
                // (4,46): warning CS0109: The member 'C1.operator checked ++()' does not hide an accessible member. The new keyword is not required.
                //     public new abstract void operator checked++(); 
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 46)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.True(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_060_NewVirtualAllowedInClassAndInterface([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new virtual void operator" + op + @"() {}
    public new virtual void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,37): warning CS0109: The member 'C1.operator ++()' does not hide an accessible member. The new keyword is not required.
                //     public new virtual void operator++() {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"()").WithLocation(3, 37),
                // (4,45): warning CS0109: The member 'C1.operator checked ++()' does not hide an accessible member. The new keyword is not required.
                //     public new virtual void operator checked++() {} 
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(4, 45)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.True(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_061_NewSealedAllowedInInterface([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface C1
{
    sealed new void operator" + op + @"() {}
    sealed new void operator checked" + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (4,29): warning CS0109: The member 'C1.operator ++()' does not hide an accessible member. The new keyword is not required.
                //     sealed new void operator++() {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"()").WithLocation(4, 29),
                // (5,37): warning CS0109: The member 'C1.operator checked ++()' does not hide an accessible member. The new keyword is not required.
                //     sealed new void operator checked++() {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"()").WithLocation(5, 37)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName)));
            }

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.False(m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_062_NewOverrideNotAllowedInClass([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"() {}
}

class C2
{
    public virtual void operator checked " + op + @"() {}
    public void operator " + op + @"() {}
}

class C3 : C1
{
    public new override void operator" + op + @"() {}
}

class C4 : C2
{
    public new override void operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,38): error CS0113: A member 'C3.operator ++()' marked as override cannot be marked as new or virtual
                //     public new override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C3.operator " + op + @"()").WithLocation(15, 38),
                // (20,47): error CS0113: A member 'C4.operator checked ++()' marked as override cannot be marked as new or virtual
                //     public new override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C4.operator checked " + op + @"()").WithLocation(20, 47),

                // PROTOTYPE: The error below is not expected. One should be able to override just one version of the operator.

                // (20,47): error CS9025: The operator 'C4.operator checked ++()' requires a matching non-checked version of the operator to also be defined
                //     public new override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C4.operator checked " + op + @"()").WithLocation(20, 47)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_063_NewOverrideNotAllowedInStruct([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct S1
{
    public new override void operator" + op + @"() {}
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,38): error CS0113: A member 'S1.operator ++()' marked as override cannot be marked as new or virtual
                //     public new override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("S1.operator " + op + @"()").WithLocation(4, 38),
                // (4,38): error CS0115: 'S1.operator ++()': no suitable method found to override
                //     public new override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator " + op + @"()").WithLocation(4, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_064_NewNotAllowed_OnStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public static new C1 operator" + op + @"(C1 x) => throw null;
    public static new C1 operator checked " + op + @"(C1 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,34): error CS0106: The modifier 'new' is not valid for this item
                //     public static new C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(3, 34),
                // (4,43): error CS0106: The modifier 'new' is not valid for this item
                //     public static new C1 operator checked ++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(4, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_065_NewNotAllowedOnExplicitImplementation([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3 : I1
{
    new void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    new void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,26): error CS0106: The modifier 'new' is not valid for this item
                //     new void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(15, 26),
                // (20,34): error CS0106: The modifier 'new' is not valid for this item
                //     new void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(20, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_066_NewNotAllowedOnExplicitImplementation_OnStatic([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    static abstract I1 operator " + op + @"(I1 x);
}

interface I2
{
    static abstract I2 operator checked " + op + @"(I2 x);
    static sealed I2 operator " + op + @"(I2 x) => throw null;
}

" + typeKeyword + @" C3 : I1
{
    static new I1 I1.operator " + op + @"(I1 x) => throw null;
}

" + typeKeyword + @" C4 : I2
{
    static new I2 I2.operator checked " + op + @"(I2 x) => throw null;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,31): error CS0106: The modifier 'new' is not valid for this item
                //     static new I1 I1.operator ++(I1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(15, 31),
                // (20,39): error CS0106: The modifier 'new' is not valid for this item
                //     static new I2 I2.operator checked ++(I2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(20, 39)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_067_ExplicitImplementationStaticVsInstanceMismatch([CombinatorialValues("++", "--")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

" + typeKeyword + @" C3
    : I1
{
    static void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4
    : I2
{
    static void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (14,7): error CS0535: 'C3' does not implement interface member 'I1.operator ++()'
                //     : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("C3", "I1.operator " + op + @"()").WithLocation(14, 7),
                // (16,29): error CS1535: Overloaded unary operator '++' takes one parameter
                //     static void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(16, 29),
                // (16,29): error CS0539: 'C3.operator ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C3.operator " + op + @"()").WithLocation(16, 29),
                // (20,7): error CS0535: 'C4' does not implement interface member 'I2.operator checked ++()'
                //     : I2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("C4", "I2.operator checked " + op + @"()").WithLocation(20, 7),
                // (22,37): error CS1535: Overloaded unary operator '++' takes one parameter
                //     static void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(22, 37),
                // (22,37): error CS0539: 'C4.operator checked ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C4.operator checked " + op + @"()").WithLocation(22, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_068_ExplicitImplementationStaticVsInstanceMismatch([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
}

interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}

interface I3 : I1
{
    static void I1.operator " + op + @"() {}
}

interface I4 : I2
{
    static void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,29): error CS1535: Overloaded unary operator '++' takes one parameter
                //     static void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(15, 29),
                // (15,29): error CS0539: 'I3.operator ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("I3.operator " + op + @"()").WithLocation(15, 29),
                // (20,37): error CS1535: Overloaded unary operator '++' takes one parameter
                //     static void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(20, 37),
                // (20,37): error CS0539: 'I4.operator checked ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     static void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("I4.operator checked " + op + @"()").WithLocation(20, 37)
                );
        }
    }
}
