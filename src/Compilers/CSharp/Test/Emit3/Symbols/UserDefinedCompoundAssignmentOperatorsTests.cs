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
        public void Increment_001([CombinatorialValues("++", "--")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator" + op + @"() {} 
    public void operator checked" + op + @"() {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net60);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                // (3,25): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                //     public void operator--() {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(3, 25),
                // (4,33): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                //     public void operator checked--() {} 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(4, 33)
                );

            validate(comp.SourceModule);

            comp = CreateCompilation(source, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                // (3,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
                //     public void operator++() {} 
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, op).WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(3, 25),
                // (4,33): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
                //     public void operator checked++() {} 
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, op).WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(4, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
                Assert.Empty(m.GetAttributes());
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (3,19): error CS9308: User-defined operator 'C1.operator ++()' must be declared public
                //     void operator ++() {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + (isChecked ? "checked " : "") + op + @"()").WithLocation(3, 19 + (isChecked ? 8 : 0))
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_002_MustBePublic_ExplicitAccessibility(
            [CombinatorialValues("++", "--")] string op,
            [CombinatorialValues("struct", "class", "interface")] string typeKeyword,
            [CombinatorialValues("private", "internal", "protected", "internal protected", "private protected")] string accessibility,
            bool isChecked)
        {
            var source =
typeKeyword + @" C1
{
    " + accessibility + @"
    void operator " + (isChecked ? "checked " : "") + op + @"() {} 
" + (isChecked ? "public void operator " + op + @"() {}" : "") + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                // (4,19): error CS9308: User-defined operator 'C1.operator ++()' must be declared public
                //     void operator ++() {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + (isChecked ? "checked " : "") + op + @"()").WithLocation(4, 19 + (isChecked ? 8 : 0))
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,23): error CS9310: The return type for this operator must be void
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,32): error CS9310: The return type for this operator must be void
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
            var source1 = @"
public " + typeKeyword + @" C1
{
#line 3
    public void operator" + op + @"(C1 x) {} 
    public void operator" + op + @"(C1 x, C1 y) {} 
    public void operator" + op + @"(C1 x, C1 y, C1 z) {} 
}
";
            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Net90);
            comp1.VerifyDiagnostics(
                // (3,25): error CS0558: User-defined operator 'C1.operator ++(C1)' must be declared static and public
                //     public void operator++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C1.operator " + op + "(C1)").WithLocation(3, 25),
                // (3,25): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     public void operator++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(3, 25),
                // (4,25): error CS1020: Overloadable binary operator expected
                //     public void operator++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 25),
                // (4,25): error CS0558: User-defined operator 'C1.operator ++(C1, C1)' must be declared static and public
                //     public void operator++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C1.operator " + op + "(C1, C1)").WithLocation(4, 25),
                // (5,25): error CS1535: Overloaded unary operator '++' takes one parameter
                //     public void operator++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(5, 25),
                // (5,25): error CS0558: User-defined operator 'C1.operator ++(C1, C1, C1)' must be declared static and public
                //     public void operator++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C1.operator " + op + "(C1, C1, C1)").WithLocation(5, 25)
                );

            if (typeKeyword == "interface")
            {
                var source2 = @"
class C2 : C1
{
    void C1.operator" + op + @"(C1 x) {} 
    void C1.operator" + op + @"(C1 x, C1 y) {} 
    void C1.operator" + op + @"(C1 x, C1 y, C1 z) {} 
}
";
                var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net90);
                comp2.VerifyDiagnostics(
                    // (4,21): error CS8930: Explicit implementation of a user-defined operator 'C2.operator ++(C1)' must be declared static
                    //     void C1.operator++(C1 x) {} 
                    Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, op).WithArguments("C2.operator " + op + "(C1)").WithLocation(4, 21),
                    // (4,21): error CS0539: 'C2.operator ++(C1)' in explicit interface declaration is not found among members of the interface that can be implemented
                    //     void C1.operator++(C1 x) {} 
                    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C2.operator " + op + "(C1)").WithLocation(4, 21),
                    // (5,21): error CS1020: Overloadable binary operator expected
                    //     void C1.operator++(C1 x, C1 y) {} 
                    Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(5, 21),
                    // (5,21): error CS8930: Explicit implementation of a user-defined operator 'C2.operator ++(C1, C1)' must be declared static
                    //     void C1.operator++(C1 x, C1 y) {} 
                    Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, op).WithArguments("C2.operator " + op + "(C1, C1)").WithLocation(5, 21),
                    // (5,21): error CS0539: 'C2.operator ++(C1, C1)' in explicit interface declaration is not found among members of the interface that can be implemented
                    //     void C1.operator++(C1 x, C1 y) {} 
                    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C2.operator " + op + "(C1, C1)").WithLocation(5, 21),
                    // (6,21): error CS1535: Overloaded unary operator '++' takes one parameter
                    //     void C1.operator++(C1 x, C1 y, C1 z) {} 
                    Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(6, 21),
                    // (6,21): error CS8930: Explicit implementation of a user-defined operator 'C2.operator ++(C1, C1, C1)' must be declared static
                    //     void C1.operator++(C1 x, C1 y, C1 z) {} 
                    Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, op).WithArguments("C2.operator " + op + "(C1, C1, C1)").WithLocation(6, 21),
                    // (6,21): error CS0539: 'C2.operator ++(C1, C1, C1)' in explicit interface declaration is not found among members of the interface that can be implemented
                    //     void C1.operator++(C1 x, C1 y, C1 z) {} 
                    Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C2.operator " + op + "(C1, C1, C1)").WithLocation(6, 21)
                    );
            }
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,34): error CS0558: User-defined operator 'C1.operator checked ++(C1)' must be declared static and public
                //     public void operator checked ++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C1.operator checked " + op + "(C1)").WithLocation(3, 34),
                // (3,34): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     public void operator checked ++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(3, 34),
                // (3,34): error CS9025: The operator 'C1.operator checked ++(C1)' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked ++(C1 x) {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1)").WithLocation(3, 34),
                // (4,34): error CS1020: Overloadable binary operator expected
                //     public void operator checked ++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 34),
                // (4,34): error CS0558: User-defined operator 'C1.operator checked ++(C1, C1)' must be declared static and public
                //     public void operator checked ++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C1.operator checked " + op + "(C1, C1)").WithLocation(4, 34),
                // (4,34): error CS9025: The operator 'C1.operator checked ++(C1, C1)' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked ++(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1, C1)").WithLocation(4, 34),
                // (5,34): error CS1535: Overloaded unary operator '++' takes one parameter
                //     public void operator checked ++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(5, 34),
                // (5,34): error CS0558: User-defined operator 'C1.operator checked ++(C1, C1, C1)' must be declared static and public
                //     public void operator checked ++(C1 x, C1 y, C1 z) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C1.operator checked " + op + "(C1, C1, C1)").WithLocation(5, 34),
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,30): error CS0106: The modifier 'static' is not valid for this item
                //     public static C1 operator++() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(3, 30),
                // (3,30): error CS9310: The return type for this operator must be void
                //     public static C1 operator++() => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(3, 30),
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,39): error CS0106: The modifier 'static' is not valid for this item
                //     public static C1 operator checked ++() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(3, 39),
                // (3,39): error CS9310: The return type for this operator must be void
                //     public static C1 operator checked ++() => throw null;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(3, 39),
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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

            comp = CreateCompilation(["class C2 : C1 {}", source, CompilerFeatureRequiredAttribute]);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + "()");
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
            var source1 = @"
public interface I1
{
    void operator " + op + @"();
}

public interface I2
{
    void operator checked " + op + @"();
    sealed void operator " + op + @"() {}
}
";
            var source2 =
typeKeyword + @" C3 : I1
{
    void I1.operator " + op + @"() {}
}

" + typeKeyword + @" C4 : I2
{
    void I2.operator checked " + op + @"() {}
}
";
            var comp = CreateCompilation(source1 + source2, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Net90);

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net90, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                // (3,22): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                //     void I1.operator --() {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(3, 22),
                // (8,30): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                //     void I2.operator checked --() {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(8, 30)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + "()");
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)).Single());
                validateOp(m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)).Single());
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + "()");
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static abstract C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 39),
                // (9,48): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static abstract C2 operator checked ++(C2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(9, 48)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + "()");
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + "()");
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + "()");
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)).Single());
                validateOp(m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)).Single());
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     public static virtual C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(4, 38),
                // (9,47): error CS0106: The modifier 'virtual' is not valid for this item
                //     public static virtual C2 operator checked ++(C2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(9, 47)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate1, sourceSymbolValidator: validate1).VerifyDiagnostics();

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
            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute]);

            CompileAndVerify(comp2, symbolValidator: validate2, sourceSymbolValidator: validate2).VerifyDiagnostics();

            void validate1(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
            }

            void validate2(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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

            comp = CreateCompilation(["class C4 : C2 {}", source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);

            validateOp(comp.GetMember<MethodSymbol>("S1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
            validateOp(comp.GetMember<MethodSymbol>("S1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));

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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,39): error CS0106: The modifier 'override' is not valid for this item
                //     public static override C1 operator++(C1 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(3, 39),
                // (8,48): error CS0106: The modifier 'override' is not valid for this item
                //     public static override C2 operator checked ++(C2 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(8, 48)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            comp = CreateCompilation([source2, source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,32): error CS0238: 'C1.operator ++()' cannot be sealed because it is not an override
                //     public sealed void operator++() {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"()").WithLocation(4, 32),
                // (9,41): error CS0238: 'C2.operator checked ++()' cannot be sealed because it is not an override
                //     public sealed void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C2.operator checked " + op + @"()").WithLocation(9, 41),
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
                // (15,46): error CS0106: The modifier 'override' is not valid for this item
                //     public static sealed override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 46),
                // (20,55): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 55),
                // (20,55): error CS0106: The modifier 'override' is not valid for this item
                //     public static sealed override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(20, 55)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
                // (15,55): error CS0106: The modifier 'override' is not valid for this item
                //     public static sealed abstract override C3 operator++(C3 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 55),
                // (20,64): error CS0106: The modifier 'abstract' is not valid for this item
                //     public static sealed abstract override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(20, 64),
                // (20,64): error CS0106: The modifier 'sealed' is not valid for this item
                //     public static sealed abstract override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 64),
                // (20,64): error CS0106: The modifier 'override' is not valid for this item
                //     public static sealed abstract override C4 operator checked ++(C4 x) => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(20, 64)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName)));
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (15,38): error CS0113: A member 'C3.operator ++()' marked as override cannot be marked as new or virtual
                //     public new override void operator++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C3.operator " + op + @"()").WithLocation(15, 38),
                // (20,47): error CS0113: A member 'C4.operator checked ++()' marked as override cannot be marked as new or virtual
                //     public new override void operator checked ++() {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C4.operator checked " + op + @"()").WithLocation(20, 47)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (16,29): error CS0106: The modifier 'static' is not valid for this item
                //     static void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(16, 29),
                // (22,37): error CS0106: The modifier 'static' is not valid for this item
                //     static void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(22, 37)
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
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,29): error CS0106: The modifier 'static' is not valid for this item
                //     static void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(15, 29),
                // (20,37): error CS0106: The modifier 'static' is not valid for this item
                //     static void I2.operator checked ++() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(20, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_069_Consumption_OnNonVariable([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public int _F;

    public void operator" + op + @"() => throw null; 
    public void operator checked" + op + @"() => throw null; 

    public static C1 operator" + op + @"(C1 x)
    {
        System.Console.Write(""[operator]"");
        return new C1() { _F = x._F + 1 };
    } 
    public static C1 operator checked" + op + @"(C1 x)
    {
        System.Console.Write(""[operator checked]"");
        checked
        {
            return new C1() { _F = x._F + 1 };
        }
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1 P {get; set;} = new C1();

    static void Main()
    {
        C1 x;

        " + op + @"P;
        System.Console.WriteLine(P._F);
        P" + op + @";
        System.Console.WriteLine(P._F);
        x = " + op + @"P;
        System.Console.WriteLine(P._F);
        x = P" + op + @";
        System.Console.WriteLine(P._F);

        checked
        {
            " + op + @"P;
            System.Console.WriteLine(P._F);
            P" + op + @";
            System.Console.WriteLine(P._F);
            x = " + op + @"P;
            System.Console.WriteLine(P._F);
            x = P" + op + @";
            System.Console.WriteLine(P._F);
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[operator]1
[operator]2
[operator]3
[operator]4
[operator checked]5
[operator checked]6
[operator checked]7
[operator checked]8
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_070_Consumption_Prefix_NotUsed_Class([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
    public void operator checked" + op + @"()
    {
        System.Console.Write(""[operator checked]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static void Test3<T>(T[] x) where T : class, I1
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static void Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            const string expectedOutput = @"
[GetA][Get0][operator]1
[GetA][Get0][operator checked]2
[GetA][Get0][operator]3
[GetA][Get0][operator checked]4
[GetA][Get0][operator]5
[GetA][Get0][operator checked]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  ldelem.ref
  IL_000d:  callvirt   ""void C1." + methodName + @"()""
  IL_0012:  nop
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""T[] Program.GetA<T>(T[])""
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  ldelem     ""T""
  IL_0011:  box        ""T""
  IL_0016:  callvirt   ""void I1." + methodName + @"()""
  IL_001b:  nop
  IL_001c:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""T[] Program.GetA<T>(T[])""
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  readonly.
  IL_000e:  ldelema    ""T""
  IL_0013:  constrained. ""T""
  IL_0019:  callvirt   ""void I1." + methodName + @"()""
  IL_001e:  nop
  IL_001f:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "()", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
IIncrementOrDecrementOperation (Prefix) (OperatorMethod: void C1." + methodName + @"()) (OperationKind." + (op == "++" ? "Increment" : "Decrement") + @", Type: System.Void) (Syntax: '" + op + @"GetA(x)[Get0()]')
  Target:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
");

            methodName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0008:  call       ""int Program.Get0()""
  IL_000d:  ldelem.ref
  IL_000e:  callvirt   ""void C1." + methodName + @"()""
  IL_0013:  nop
  IL_0014:  nop
  IL_0015:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""T[] Program.GetA<T>(T[])""
  IL_0008:  call       ""int Program.Get0()""
  IL_000d:  ldelem     ""T""
  IL_0012:  box        ""T""
  IL_0017:  callvirt   ""void I1." + methodName + @"()""
  IL_001c:  nop
  IL_001d:  nop
  IL_001e:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""T[] Program.GetA<T>(T[])""
  IL_0008:  call       ""int Program.Get0()""
  IL_000d:  readonly.
  IL_000f:  ldelema    ""T""
  IL_0014:  constrained. ""T""
  IL_001a:  callvirt   ""void I1." + methodName + @"()""
  IL_001f:  nop
  IL_0020:  nop
  IL_0021:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "()", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
IIncrementOrDecrementOperation (Prefix, Checked) (OperatorMethod: void I1." + methodName + @"()) (OperationKind." + (op == "++" ? "Increment" : "Decrement") + @", Type: System.Void) (Syntax: '" + op + @"GetA(x)[Get0()]')
  Target:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);

            var expectedErrors = new[] {
                // (23,9): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op +"GetA(x)[Get0()]").WithArguments(op, "C1").WithLocation(23, 9),
                // (30,13): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //             ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op +"GetA(x)[Get0()]").WithArguments(op, "C1").WithLocation(30, 13),
                // (36,9): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //         ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op +"GetA(x)[Get0()]").WithArguments(op, "T").WithLocation(36, 9),
                // (43,13): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //             ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op +"GetA(x)[Get0()]").WithArguments(op, "T").WithLocation(43, 13),
                // (49,9): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //         ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op +"GetA(x)[Get0()]").WithArguments(op, "T").WithLocation(49, 9),
                // (56,13): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //             ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op +"GetA(x)[Get0()]").WithArguments(op, "T").WithLocation(56, 13)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_071_Consumption_Prefix_NotUsed_Struct([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
    public void operator checked" + op + @"()
    {
        System.Console.Write(""[operator checked]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static void Test3<T>(T[] x) where T : struct, I1
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static void Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]1
[GetA][Get0][operator checked]2
[GetA][Get0][operator]3
[GetA][Get0][operator checked]4
[GetA][Get0][operator]5
[GetA][Get0][operator checked]6
").VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  ldelema    ""C1""
  IL_0011:  call       ""void C1." + methodName + @"()""
  IL_0016:  nop
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""T[] Program.GetA<T>(T[])""
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  readonly.
  IL_000e:  ldelema    ""T""
  IL_0013:  constrained. ""T""
  IL_0019:  callvirt   ""void I1." + methodName + @"()""
  IL_001e:  nop
  IL_001f:  ret
}
");

            methodName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0008:  call       ""int Program.Get0()""
  IL_000d:  ldelema    ""C1""
  IL_0012:  call       ""void C1." + methodName + @"()""
  IL_0017:  nop
  IL_0018:  nop
  IL_0019:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""T[] Program.GetA<T>(T[])""
  IL_0008:  call       ""int Program.Get0()""
  IL_000d:  readonly.
  IL_000f:  ldelema    ""T""
  IL_0014:  constrained. ""T""
  IL_001a:  callvirt   ""void I1." + methodName + @"()""
  IL_001f:  nop
  IL_0020:  nop
  IL_0021:  ret
}
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]1
[GetA][Get0][operator checked]2
[GetA][Get0][operator]3
[GetA][Get0][operator checked]4
[GetA][Get0][operator]5
[GetA][Get0][operator checked]6
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_072_Consumption_Prefix_Used_Class([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
    public void operator checked" + op + @"()
    {
        System.Console.Write(""[operator checked]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)x[0] == y);
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)x[0] == y);
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)x[0] == y);
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)x[0] == y);
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)x[0] == y);
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)x[0] == y);
    } 

    static C1 Test1(C1[] x)
    {
        return " + op + @"GetA(x)[Get0()];
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return " + op + @"GetA(x)[Get0()];
        }
    } 

    static T Test3<T>(T[] x) where T : class, I1
    {
        return " + op + @"GetA(x)[Get0()];
    } 

    static T Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            return " + op + @"GetA(x)[Get0()];
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return " + op + @"GetA(x)[Get0()];
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return " + op + @"GetA(x)[Get0()];
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]1True
[GetA][Get0][operator checked]2True
[GetA][Get0][operator]3True
[GetA][Get0][operator checked]4True
[GetA][Get0][operator]5True
[GetA][Get0][operator checked]6True
").VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  callvirt   ""void C1." + methodName + @"()""
  IL_0012:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  callvirt   ""void I1." + methodName + @"()""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (T[] V_0,
            int V_1,
            T V_2,
            T V_3)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  ldloca.s   V_3
  IL_0017:  initobj    ""T""
  IL_001d:  ldloc.3
  IL_001e:  box        ""T""
  IL_0023:  brtrue.s   IL_0034
  IL_0025:  ldloca.s   V_2
  IL_0027:  constrained. ""T""
  IL_002d:  callvirt   ""void I1." + methodName + @"()""
  IL_0032:  br.s       IL_0049
  IL_0034:  ldloca.s   V_2
  IL_0036:  constrained. ""T""
  IL_003c:  callvirt   ""void I1." + methodName + @"()""
  IL_0041:  ldloc.0
  IL_0042:  ldloc.1
  IL_0043:  ldloc.2
  IL_0044:  stelem     ""T""
  IL_0049:  ldloc.2
  IL_004a:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "()", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
IIncrementOrDecrementOperation (Prefix) (OperatorMethod: void C1." + methodName + @"()) (OperationKind." + (op == "++" ? "Increment" : "Decrement") + @", Type: C1) (Syntax: '" + op + @"GetA(x)[Get0()]')
  Target:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
");

            methodName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  callvirt   ""void C1." + methodName + @"()""
  IL_0012:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  callvirt   ""void I1." + methodName + @"()""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       75 (0x4b)
  .maxstack  3
  .locals init (T[] V_0,
            int V_1,
            T V_2,
            T V_3)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  ldloca.s   V_3
  IL_0017:  initobj    ""T""
  IL_001d:  ldloc.3
  IL_001e:  box        ""T""
  IL_0023:  brtrue.s   IL_0034
  IL_0025:  ldloca.s   V_2
  IL_0027:  constrained. ""T""
  IL_002d:  callvirt   ""void I1." + methodName + @"()""
  IL_0032:  br.s       IL_0049
  IL_0034:  ldloca.s   V_2
  IL_0036:  constrained. ""T""
  IL_003c:  callvirt   ""void I1." + methodName + @"()""
  IL_0041:  ldloc.0
  IL_0042:  ldloc.1
  IL_0043:  ldloc.2
  IL_0044:  stelem     ""T""
  IL_0049:  ldloc.2
  IL_004a:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "()", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("T", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
IIncrementOrDecrementOperation (Prefix, Checked) (OperatorMethod: void I1." + methodName + @"()) (OperationKind." + (op == "++" ? "Increment" : "Decrement") + @", Type: T) (Syntax: '" + op + @"GetA(x)[Get0()]')
  Target:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]1True
[GetA][Get0][operator checked]2True
[GetA][Get0][operator]3True
[GetA][Get0][operator checked]4True
[GetA][Get0][operator]5True
[GetA][Get0][operator checked]6True
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_073_Consumption_Prefix_Used_Struct([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
    public void operator checked" + op + @"()
    {
        System.Console.Write(""[operator checked]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
    } 

    static C1 Test1(C1[] x)
    {
        return " + op + @"GetA(x)[Get0()];
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return " + op + @"GetA(x)[Get0()];
        }
    } 

    static T Test3<T>(T[] x) where T : struct, I1
    {
        return " + op + @"GetA(x)[Get0()];
    } 

    static T Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            return " + op + @"GetA(x)[Get0()];
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return " + op + @"GetA(x)[Get0()];
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return " + op + @"GetA(x)[Get0()];
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]11
[GetA][Get0][operator checked]22
[GetA][Get0][operator]33
[GetA][Get0][operator checked]44
[GetA][Get0][operator]55
[GetA][Get0][operator checked]66
").VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""void C1." + methodName + @"()""
  IL_001e:  ldloc.0
  IL_001f:  stobj      ""C1""
  IL_0024:  ldloc.0
  IL_0025:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (int V_0,
            T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""void I1." + methodName + @"()""
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  stelem     ""T""
  IL_0028:  ldloc.1
  IL_0029:  ret
}
");

            methodName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""void C1." + methodName + @"()""
  IL_001e:  ldloc.0
  IL_001f:  stobj      ""C1""
  IL_0024:  ldloc.0
  IL_0025:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       42 (0x2a)
  .maxstack  3
  .locals init (int V_0,
            T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""void I1." + methodName + @"()""
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  stelem     ""T""
  IL_0028:  ldloc.1
  IL_0029:  ret
}
");
            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]11
[GetA][Get0][operator checked]22
[GetA][Get0][operator]33
[GetA][Get0][operator checked]44
[GetA][Get0][operator]55
[GetA][Get0][operator checked]66
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_074_Consumption_Postfix_NotUsed_Class([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
    public void operator checked" + op + @"()
    {
        System.Console.Write(""[operator checked]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @";
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @";
        }
    } 

    static void Test3<T>(T[] x) where T : class, I1
    {
        GetA(x)[Get0()]" + op + @";
    } 

    static void Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @";
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @";
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @";
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][operator]1
[GetA][Get0][operator checked]2
[GetA][Get0][operator]3
[GetA][Get0][operator checked]4
[GetA][Get0][operator]5
[GetA][Get0][operator checked]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  callvirt   ""void C1." + methodName + @"()""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  callvirt   ""void I1." + methodName + @"()""
  IL_001a:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I1." + methodName + @"()""
  IL_001d:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PostfixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "()", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
IIncrementOrDecrementOperation (Postfix) (OperatorMethod: void C1." + methodName + @"()) (OperationKind." + (op == "++" ? "Increment" : "Decrement") + @", Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @"')
  Target:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
");

            methodName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  callvirt   ""void C1." + methodName + @"()""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  callvirt   ""void I1." + methodName + @"()""
  IL_001a:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I1." + methodName + @"()""
  IL_001d:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PostfixUnaryExpressionSyntax>().Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "()", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
IIncrementOrDecrementOperation (Postfix, Checked) (OperatorMethod: void I1." + methodName + @"()) (OperationKind." + (op == "++" ? "Increment" : "Decrement") + @", Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @"')
  Target:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,9): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         GetA(x)[Get0()]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "GetA(x)[Get0()]" + op).WithArguments(op, "C1").WithLocation(23, 9),
                // (30,13): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //             GetA(x)[Get0()]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "GetA(x)[Get0()]" + op).WithArguments(op, "C1").WithLocation(30, 13),
                // (36,9): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //         GetA(x)[Get0()]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "GetA(x)[Get0()]" + op).WithArguments(op, "T").WithLocation(36, 9),
                // (43,13): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //             GetA(x)[Get0()]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "GetA(x)[Get0()]" + op).WithArguments(op, "T").WithLocation(43, 13),
                // (49,9): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //         GetA(x)[Get0()]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "GetA(x)[Get0()]" + op).WithArguments(op, "T").WithLocation(49, 9),
                // (56,13): error CS0023: Operator '++' cannot be applied to operand of type 'T'
                //             GetA(x)[Get0()]++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "GetA(x)[Get0()]" + op).WithArguments(op, "T").WithLocation(56, 13)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_075_Consumption_Postfix_NotUsed_Struct([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"();
    public void operator checked" + op + @"();
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
    public void operator checked" + op + @"()
    {
        System.Console.Write(""[operator checked]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @";
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @";
        }
    } 

    static void Test3<T>(T[] x) where T : struct, I1
    {
        GetA(x)[Get0()]" + op + @";
    } 

    static void Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @";
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @";
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @";
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]1
[GetA][Get0][operator checked]2
[GetA][Get0][operator]3
[GetA][Get0][operator checked]4
[GetA][Get0][operator]5
[GetA][Get0][operator checked]6
").VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""void C1." + methodName + @"()""
  IL_0015:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I1." + methodName + @"()""
  IL_001d:  ret
}
");

            methodName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""void C1." + methodName + @"()""
  IL_0015:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       30 (0x1e)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I1." + methodName + @"()""
  IL_001d:  ret
}
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: @"
[GetA][Get0][operator]1
[GetA][Get0][operator checked]2
[GetA][Get0][operator]3
[GetA][Get0][operator checked]4
[GetA][Get0][operator]5
[GetA][Get0][operator checked]6
").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_076_Consumption_Postfix_For()
        {
            var source = @"
public class C1
{
    public int _F;
    public void operator ++()
    {
        _F++;
    } 
}

public class Program
{
    static void Main()
    {
        C1 x = new C1();
        for (x++; x._F < 4; x++)
        {
            System.Console.Write(x._F);
        }
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"123").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_077_Consumption_Postfix_Used([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public void operator" + op + @"() {}
    public void operator checked" + op + @"() {}
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1 x = new C1();
        C1 y = x" + op + @";
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            comp2.VerifyDiagnostics(
                // (7,16): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         C1 y = x++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "x" + op).WithArguments(op, "C1").WithLocation(7, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_078_Consumption_Postfix_Used([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public int _F;

    public void operator" + op + @"() => throw null;
    public void operator checked" + op + @"() => throw null;

    public static C1 operator" + op + @"(C1 x)
    {
        System.Console.Write(""[operator]"");
        return new C1() { _F = x._F + 1 };
    } 

    public static C1 operator checked" + op + @"(C1 x)
    {
        System.Console.Write(""[operator checked]"");
        return new C1() { _F = x._F + 1 };
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1 x = new C1();
        C1 y = x" + op + @";
        System.Console.Write(y._F);
        System.Console.Write(x._F);
        
        checked
        {
            y = x" + op + @";
        }

        System.Console.Write(y._F);
        System.Console.Write(x._F);
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp2, expectedOutput: "[operator]01[operator checked]12").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_079_Consumption_RegularVersionInCheckedContext([CombinatorialValues("++", "--")] string op, bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public int _F;
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test2(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            " + op + @"GetA(x)[Get0()];
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"[GetA][Get0][operator]1").VerifyDiagnostics();

            var methodName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  ldarg.0
  IL_0003:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0008:  call       ""int Program.Get0()""
  IL_000d:  ldelem.ref
  IL_000e:  callvirt   ""void C1." + methodName + @"()""
  IL_0013:  nop
  IL_0014:  nop
  IL_0015:  ret
}
");
        }

        [Theory]
        [CombinatorialData]
        public void Increment_080_Consumption_CheckedVersionInRegularContext([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public class C1
{
    public int _F;
    public void operator checked " + op + @"()
    {
        System.Console.Write(""[operator]"");
        _F++;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        " + op + @"GetA(x)[Get0()];
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (13,9): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         ++GetA(x)[Get0()];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "GetA(x)[Get0()]").WithArguments(op, "C1").WithLocation(13, 9)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_081_Consumption_Shadowing([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public class C1
{
    public void operator" + op + @"() => throw null;
    public void operator checked" + op + @"() => throw null;
}

public class C2 : C1
{
    public new void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C2();
        " + op + @"x;
        checked
        {
            " + op + @"x;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[operator][operator]").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_082_Consumption_Shadowing([CombinatorialValues("++", "--")] string op)
        {
            var source1_1 = @"
public class C1
{
    public void operator" + op + @"() => throw null;
    public void operator checked" + op + @"() => throw null;
}

public class C2 : C1
{
    public new void operator checked " + op + @"() => throw null;
}
";

            var comp1_1 = CreateCompilation([source1_1, CompilerFeatureRequiredAttribute], assemblyName: "C");

            var source2 = @"
public class Test
{
    public static void Main()
    {
        var x = new C2();
        " + op + @"x;
        checked
        {
            " + op + @"x;
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1_1.ToMetadataReference()]);
            comp2.VerifyDiagnostics();

            var source1_2 = @"
public class C1
{
    public void operator" + op + @"()
    {
        System.Console.Write(""[operator]"");
    } 

    public void operator checked" + op + @"() => throw null;
}

public class C2 : C1
{
    public new void operator checked " + op + @"()
    {
        System.Console.Write(""[checked operator]"");
    } 

    public new void operator " + op + @"() => throw null;
}
";

            var source3 = @"
public class Program
{
    static void Main()
    {
        Test.Main();
    } 
}
";
            var comp1_2 = CreateCompilation([source1_2, CompilerFeatureRequiredAttribute], assemblyName: "C");

            var comp3 = CreateCompilation([source3, CompilerFeatureRequiredAttribute], references: [comp1_2.EmitToImageReference(), comp2.EmitToImageReference()], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "[operator][checked operator]").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_083_Consumption_Shadowing([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public abstract class C1
{
    public abstract void operator" + op + @"();
    public void operator checked" + op + @"() => throw null;
}

public class C2 : C1
{
    public new void operator checked " + op + @"()
    {
        System.Console.Write(""[checked operator]"");
    } 

    public override void operator " + op + @"()
    {
        System.Console.Write(""[operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C2();
        " + op + @"x;
        checked
        {
            " + op + @"x;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[operator][checked operator]").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_084_Consumption_Overriding([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public abstract class C1
{
    public abstract void operator" + op + @"();
    public abstract void operator checked" + op + @"();
}

public abstract class C2 : C1
{
    public override void operator checked " + op + @"()
    {
        System.Console.Write(""[checked operator]"");
    } 
}

public class C3 : C2
{
    public override void operator " + op + @"()
    {
        System.Console.Write(""[operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C3();
        " + op + @"x;
        checked
        {
            " + op + @"x;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[operator][checked operator]").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_085_Consumption_Ambiguity()
        {
            var source1 = @"
public interface I1
{
    public void operator ++();
}

public interface I2<T> where T : I2<T>
{
    public void operator ++();
    public abstract static T operator ++(T x);
    public abstract static T operator --(T x);
}

public class Program
{
    static void Test5<T>(T x) where T : I1, I2<T>
    {
        ++x;
        --x;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp1.VerifyDiagnostics(
                // (18,9): error CS0121: The call is ambiguous between the following methods or properties: 'I1.operator ++()' and 'I2<T>.operator ++()'
                //         ++x;
                Diagnostic(ErrorCode.ERR_AmbigCall, "++").WithArguments("I1.operator ++()", "I2<T>.operator ++()").WithLocation(18, 9)
                );

            var tree = comp1.SyntaxTrees.First();
            var model = comp1.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void I1.op_IncrementAssignment()", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void I2<T>.op_IncrementAssignment()", symbolInfo.CandidateSymbols[1].ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.PrefixUnaryExpressionSyntax>().Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("T I2<T>.op_Decrement(T x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void Increment_086_Consumption_UseStaticOperatorsInOldVersion()
        {
            var source1 = @"
public class C1
{
    public int _F;

    public void operator ++()
    {
        System.Console.Write(""[instance operator]"");
        _F++;
    } 

    public void operator checked ++()
    {
        System.Console.Write(""[instance operator checked]"");
        checked
        {
            _F++;
        }
    } 

    public static C1 operator ++(C1 x)
    {
        System.Console.Write(""[static operator]"");
        return new C1() { _F = x._F + 1 };
    } 
    public static C1 operator checked ++(C1 x)
    {
        System.Console.Write(""[static operator checked]"");
        checked
        {
            return new C1() { _F = x._F + 1 };
        }
    } 
}
";
            var comp1Ref = CreateCompilation([source1, CompilerFeatureRequiredAttribute]).EmitToImageReference();

            var source2 = @"
public class Program
{
    static C1 P = new C1();

    static void Main()
    {
        C1 x;

        ++P;
        System.Console.WriteLine(P._F);
        P++;
        System.Console.WriteLine(P._F);
        x = ++P;
        System.Console.WriteLine(P._F);
        x = P++;
        System.Console.WriteLine(P._F);

        checked
        {
            ++P;
            System.Console.WriteLine(P._F);
            P++;
            System.Console.WriteLine(P._F);
            x = ++P;
            System.Console.WriteLine(P._F);
            x = P++;
            System.Console.WriteLine(P._F);
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: @"
[instance operator]1
[instance operator]2
[instance operator]3
[static operator]4
[instance operator checked]5
[instance operator checked]6
[instance operator checked]7
[static operator checked]8
").VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp2, expectedOutput: @"
[static operator]1
[static operator]2
[static operator]3
[static operator]4
[static operator checked]5
[static operator checked]6
[static operator checked]7
[static operator checked]8
").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_087_Consumption_Obsolete()
        {
            var source1 = @"
public class C1
{
    [System.Obsolete(""Test"")]
    public void operator ++() {}
}

public class Program
{
    static void Main()
    {
        var x = new C1();
        ++x;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1).VerifyDiagnostics(
                // (13,9): warning CS0618: 'C1.operator ++()' is obsolete: 'Test'
                //         ++x;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "++x").WithArguments("C1.operator ++()", "Test").WithLocation(13, 9)
                );
        }

        [Fact]
        public void Increment_088_Consumption_UnmanagedCallersOnly()
        {
            var source1 = @"
public class C1
{
    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    public void operator ++() {}
}

public class Program
{
    static void Main()
    {
        var x = new C1();
        ++x;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp1.VerifyDiagnostics(
                // (4,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [System.Runtime.InteropServices.UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "System.Runtime.InteropServices.UnmanagedCallersOnly").WithLocation(4, 6),
                // (13,9): error CS8901: 'C1.operator ++()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         ++x;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "++x").WithArguments("C1.operator ++()").WithLocation(13, 9)
                );
        }

        [Fact]
        public void Increment_089_Consumption_NullableAnalysis()
        {
            var source1 = @"
public class C1
{
    public void operator ++() {}
}

#nullable enable

public class Program
{
    static void Main()
    {
        C1? x = null;

        try
        {
            ++x;
            System.Console.Write(""unreachable"");
            x.ToString();
        }
        catch (System.NullReferenceException)
        {
            System.Console.Write(""in catch"");
        }

        C1? y = new C1();
        ++y;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "in catch").VerifyDiagnostics(
                // (17,15): warning CS8602: Dereference of a possibly null reference.
                //             ++x;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(17, 15)
                );

            var source2 = @"
public class C1
{
    public void operator ++() {}
}

#nullable enable

public class Program
{
    static void Main()
    {
        C1? x = null;

        if (false)
        {
            ++x;
            System.Console.Write(""unreachable"");
            x.ToString();
        }

        System.Console.Write(""Done"");
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "Done").VerifyDiagnostics(
                // (17,13): warning CS0162: Unreachable code detected
                //             ++x;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "++").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Increment_090_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator ++(int x = 0) {}
    public int operator --() { return 0; }
}
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        ++x;
        --x;
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (4,26): error CS0558: User-defined operator 'C1.operator ++(int)' must be declared static and public
                //     public void operator ++(int x = 0) {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "++").WithArguments("C1.operator ++(int)").WithLocation(4, 26),
                // (4,26): error CS0559: The parameter type for ++ or -- operator must be the containing type
                //     public void operator ++(int x = 0) {}
                Diagnostic(ErrorCode.ERR_BadIncDecSignature, "++").WithLocation(4, 26),
                // (4,33): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public void operator ++(int x = 0) {}
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(4, 33),
                // (5,25): error CS9310: The return type for this operator must be void
                //     public int operator --() { return 0; }
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, "--").WithLocation(5, 25)
                );

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         ++x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++x").WithArguments("++", "C1").WithLocation(7, 9),
                // (8,9): error CS0023: Operator '--' cannot be applied to operand of type 'C1'
                //         --x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "--x").WithArguments("--", "C1").WithLocation(8, 9)
                );
        }

        [Fact]
        public void Increment_091_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator ++(params int[] x) {}
    public void operator --(__arglist) {}
}
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        ++x;
        --x;
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (4,26): error CS0558: User-defined operator 'C1.operator ++(params int[])' must be declared static and public
                //     public void operator ++(params int[] x) {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "++").WithArguments("C1.operator ++(params int[])").WithLocation(4, 26),
                // (4,26): error CS0559: The parameter type for ++ or -- operator must be the containing type
                //     public void operator ++(params int[] x) {}
                Diagnostic(ErrorCode.ERR_BadIncDecSignature, "++").WithLocation(4, 26),
                // (4,29): error CS1670: params is not valid in this context
                //     public void operator ++(params int[] x) {}
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(4, 29),
                // (5,26): error CS0558: User-defined operator 'C1.operator --()' must be declared static and public
                //     public void operator --(__arglist) {}
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "--").WithArguments("C1.operator --()").WithLocation(5, 26),
                // (5,29): error CS1669: __arglist is not valid in this context
                //     public void operator --(__arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(5, 29)
                );

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         ++x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++x").WithArguments("++", "C1").WithLocation(7, 9),
                // (8,9): error CS0023: Operator '--' cannot be applied to operand of type 'C1'
                //         --x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "--x").WithArguments("--", "C1").WithLocation(8, 9)
                );

            var decrement = comp2.GetMember<MethodSymbol>("C1.op_Decrement");
            Assert.False(decrement.IsVararg);
            AssertEx.Equal("void C1.op_Decrement()", decrement.ToTestDisplayString());
        }

        [Fact]
        public void Increment_092_ConflictWithRegular()
        {
            var source1 = @"
public class C1
{
    public void operator ++() {}

    public void op_IncrementAssignment() {}
}

public class C2
{
    public static C2 operator ++(C2 x) => x;

    public static C2 op_Increment(C2 x) => x;
}

public class C3
{
    public void op_IncrementAssignment() {}

    public void operator ++() {}
}

public class C4
{
    public static C4 op_Increment(C4 x) => x;

    public static C4 operator ++(C4 x) => x;
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (6,17): error CS0111: Type 'C1' already defines a member called 'op_IncrementAssignment' with the same parameter types
                //     public void op_IncrementAssignment() {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_IncrementAssignment").WithArguments("op_IncrementAssignment", "C1").WithLocation(6, 17),
                // (13,22): error CS0111: Type 'C2' already defines a member called 'op_Increment' with the same parameter types
                //     public static C2 op_Increment(C2 x) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Increment").WithArguments("op_Increment", "C2").WithLocation(13, 22),
                // (20,26): error CS0111: Type 'C3' already defines a member called 'op_IncrementAssignment' with the same parameter types
                //     public void operator ++() {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "++").WithArguments("op_IncrementAssignment", "C3").WithLocation(20, 26),
                // (27,31): error CS0111: Type 'C4' already defines a member called 'op_Increment' with the same parameter types
                //     public static C4 operator ++(C4 x) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "++").WithArguments("op_Increment", "C4").WithLocation(27, 31)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_093_Consumption_RegularVsOperator(bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public void op_IncrementAssignment() {}
}

public class C2
{
    public void operator++() {}
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1 x = new C1();
        ++x;
        C2 y = new C2();
        y.op_IncrementAssignment();
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0023: Operator '++' cannot be applied to operand of type 'C1'
                //         ++x;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "++x").WithArguments("++", "C1").WithLocation(7, 9),
                // (9,11): error CS0571: 'C2.operator ++()': cannot explicitly call operator or accessor
                //         y.op_IncrementAssignment();
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_IncrementAssignment").WithArguments("C2.operator ++()").WithLocation(9, 11)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_094_Override_RegularVsOperatorMismatch(bool fromMetadata)
        {
            var source1 = @"
abstract public class C1
{
    public abstract void op_IncrementAssignment();
}

abstract public class C3
{
    public abstract void operator ++();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class C2 : C1
{
    public override void operator ++() {}
}

public class C4 : C3
{
    public override void op_IncrementAssignment() {}
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()]);

            comp2.VerifyDiagnostics(
                // (4,35): error CS9312: 'C2.operator ++()': cannot override inherited member 'C1.op_IncrementAssignment()' because one of them is not an operator.
                //     public override void operator ++() {}
                Diagnostic(ErrorCode.ERR_OperatorMismatchOnOverride, "++").WithArguments("C2.operator ++()", "C1.op_IncrementAssignment()").WithLocation(4, 35),
                // (9,26): error CS9312: 'C4.op_IncrementAssignment()': cannot override inherited member 'C3.operator ++()' because one of them is not an operator.
                //     public override void op_IncrementAssignment() {}
                Diagnostic(ErrorCode.ERR_OperatorMismatchOnOverride, "op_IncrementAssignment").WithArguments("C4.op_IncrementAssignment()", "C3.operator ++()").WithLocation(9, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_095_Implement_RegularVsOperatorMismatch(bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    void op_IncrementAssignment();
}

public interface I2
{
    void operator ++();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class C1 : I1
{
    public void operator ++() {}
}

public class C2 : I2
{
    public void op_IncrementAssignment() {}
}

public class C3 : I1
{
    void I1.operator ++() {}
}

public class C4 : I2
{
    void I2.op_IncrementAssignment() {}
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()]);
            comp2.VerifyDiagnostics(
                // (2,19): error CS9311: 'C1' does not implement interface member 'I1.op_IncrementAssignment()'. 'C1.operator ++()' cannot implement 'I1.op_IncrementAssignment()' because one of them is not an operator.
                // public class C1 : I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C1", "I1.op_IncrementAssignment()", "C1.operator ++()").WithLocation(2, 19),
                // (7,19): error CS9311: 'C2' does not implement interface member 'I2.operator ++()'. 'C2.op_IncrementAssignment()' cannot implement 'I2.operator ++()' because one of them is not an operator.
                // public class C2 : I2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I2").WithArguments("C2", "I2.operator ++()", "C2.op_IncrementAssignment()").WithLocation(7, 19),
                // (12,19): error CS0535: 'C3' does not implement interface member 'I1.op_IncrementAssignment()'
                // public class C3 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("C3", "I1.op_IncrementAssignment()").WithLocation(12, 19),
                // (14,22): error CS0539: 'C3.operator ++()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.operator ++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "++").WithArguments("C3.operator ++()").WithLocation(14, 22),
                // (17,19): error CS0535: 'C4' does not implement interface member 'I2.operator ++()'
                // public class C4 : I2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("C4", "I2.operator ++()").WithLocation(17, 19),
                // (19,13): error CS0539: 'C4.op_IncrementAssignment()' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I2.op_IncrementAssignment() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "op_IncrementAssignment").WithArguments("C4.op_IncrementAssignment()").WithLocation(19, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_096_Implement_RegularVsOperatorMismatch(bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    void op_IncrementAssignment();
}

public interface I2
{
    void operator ++();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class C11
{
    public void operator ++() {}
}

public class C12 : C11, I1
{
}

public class C21
{
    public void op_IncrementAssignment() {}
}

public class C22 : C21, I2
{
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()]);
            comp2.VerifyDiagnostics(
                // (7,25): error CS9311: 'C12' does not implement interface member 'I1.op_IncrementAssignment()'. 'C11.operator ++()' cannot implement 'I1.op_IncrementAssignment()' because one of them is not an operator.
                // public class C12 : C11, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C12", "I1.op_IncrementAssignment()", "C11.operator ++()").WithLocation(7, 25),
                // (16,25): error CS9311: 'C22' does not implement interface member 'I2.operator ++()'. 'C21.op_IncrementAssignment()' cannot implement 'I2.operator ++()' because one of them is not an operator.
                // public class C22 : C21, I2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I2").WithArguments("C22", "I2.operator ++()", "C21.op_IncrementAssignment()").WithLocation(16, 25)
                );
        }

        [Fact]
        public void Increment_097_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void operator ++()
    {
        System.Console.Write(""[I1.operator]"");
    } 
}

public class C1 : I1
{
    public virtual void op_IncrementAssignment()
    {
        System.Console.Write(""[C1.op_IncrementAssignment]"");
    } 
}

public class C2
{
    public virtual void op_IncrementAssignment()
    {
        System.Console.Write(""[C2.op_IncrementAssignment]"");
    } 
}

public class C3 : C2, I1
{
}

public class Program
{
    static void Main()
    {
        I1 x = new C1();
        ++x;
        x = new C3();
        ++x;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,19): error CS9311: 'C1' does not implement interface member 'I1.operator ++()'. 'C1.op_IncrementAssignment()' cannot implement 'I1.operator ++()' because one of them is not an operator.
                // public class C1 : I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C1", "I1.operator ++()", "C1.op_IncrementAssignment()").WithLocation(10, 19),
                // (26,23): error CS9311: 'C3' does not implement interface member 'I1.operator ++()'. 'C2.op_IncrementAssignment()' cannot implement 'I1.operator ++()' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.operator ++()", "C2.op_IncrementAssignment()").WithLocation(26, 23)
                );
        }

        [Fact]
        public void Increment_098_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void op_IncrementAssignment()
    {
        System.Console.Write(""[I1.operator]"");
    } 
}

public class C1 : I1
{
    public virtual void operator ++()
    {
        System.Console.Write(""[C1.operator]"");
    } 
}

public class C2
{
    public virtual void operator ++()
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class C3 : C2, I1
{
}

public class Program
{
    static void Main()
    {
        I1 x = new C1();
        x.op_IncrementAssignment();
        x = new C3();
        x.op_IncrementAssignment();
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,19): error CS9311: 'C1' does not implement interface member 'I1.op_IncrementAssignment()'. 'C1.operator ++()' cannot implement 'I1.op_IncrementAssignment()' because one of them is not an operator.
                // public class C1 : I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C1", "I1.op_IncrementAssignment()", "C1.operator ++()").WithLocation(10, 19),
                // (26,23): error CS9311: 'C3' does not implement interface member 'I1.op_IncrementAssignment()'. 'C2.operator ++()' cannot implement 'I1.op_IncrementAssignment()' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.op_IncrementAssignment()", "C2.operator ++()").WithLocation(26, 23)
                );
        }

        [Fact]
        public void Increment_099_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void operator ++();
}

public class C2 : I1
{
    void I1.operator ++()
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class C3 : C2, I1
{
    public virtual void op_IncrementAssignment()
    {
        System.Console.Write(""[C3.op_IncrementAssignment]"");
    } 
}

public class Program
{
    static void Main()
    {
        I1 x = new C3();
        ++x;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (15,23): error CS9311: 'C3' does not implement interface member 'I1.operator ++()'. 'C3.op_IncrementAssignment()' cannot implement 'I1.operator ++()' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.operator ++()", "C3.op_IncrementAssignment()").WithLocation(15, 23)
                );
        }

        [Fact]
        public void Increment_100_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void op_IncrementAssignment();
}

public class C2 : I1
{
    void I1.op_IncrementAssignment()
    {
        System.Console.Write(""[C2.op_IncrementAssignment]"");
    } 
}

public class C3 : C2, I1
{
    public virtual void operator ++()
    {
        System.Console.Write(""[C3.operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        I1 x = new C3();
        x.op_IncrementAssignment();
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (15,23): error CS9311: 'C3' does not implement interface member 'I1.op_IncrementAssignment()'. 'C3.operator ++()' cannot implement 'I1.op_IncrementAssignment()' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.op_IncrementAssignment()", "C3.operator ++()").WithLocation(15, 23)
                );
        }

        [Fact]
        public void Increment_101_Implement_RegularVsOperatorMismatch()
        {
            /*
                public interface I1
                {
                    public void operator++();
                }

                public class C1 : I1
                {
                    public virtual void op_IncrementAssignment()
                    {
                        System.Console.Write(1);
                    }
                }
            */
            var ilSource = @"
.class interface public auto ansi abstract beforefieldinit I1
{
    .method public hidebysig newslot abstract virtual specialname
        instance void op_IncrementAssignment () cil managed 
    {
    }
}

.class public auto ansi beforefieldinit C1
    extends [mscorlib]System.Object
    implements I1
{
    .method public hidebysig newslot virtual 
        instance void op_IncrementAssignment () cil managed 
    {
        .maxstack 8

        IL_0000: ldc.i4.1
        IL_0001: call void [mscorlib]System.Console::Write(int32)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
";

            var source1 =
@"
public class C2 : C1, I1
{
}
";
            var compilation1 = CreateCompilationWithIL(source1, ilSource);

            compilation1.VerifyDiagnostics(
                // (2,23): error CS9311: 'C2' does not implement interface member 'I1.operator ++()'. 'C1.op_IncrementAssignment()' cannot implement 'I1.operator ++()' because one of them is not an operator.
                // public class C2 : C1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C2", "I1.operator ++()", "C1.op_IncrementAssignment()").WithLocation(2, 23)
                );

            var source2 =
@"
class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1.op_IncrementAssignment();
        I1 x = c1;
        ++x;
    }
}
";
            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugExe);

            CompileAndVerify(compilation2, expectedOutput: "11", verify: Verification.Skipped).VerifyDiagnostics();

            var i1M1 = compilation1.GetTypeByMetadataName("I1").GetMembers().Single();
            var c1 = compilation1.GetTypeByMetadataName("C1");

            AssertEx.Equal("C1.op_IncrementAssignment()", c1.FindImplementationForInterfaceMember(i1M1).ToDisplayString());
        }

        [Fact]
        public void Increment_102_Implement_RegularVsOperatorMismatch()
        {
            /*
                public interface I1
                {
                    public void op_IncrementAssignment();
                }

                public class C1 : I1
                {
                    public virtual void operator++()
                    {
                        System.Console.Write(1);
                    }
                }
            */
            var ilSource = @"
.class interface public auto ansi abstract beforefieldinit I1
{
    .method public hidebysig newslot abstract virtual 
        instance void op_IncrementAssignment () cil managed 
    {
    }
}

.class public auto ansi beforefieldinit C1
    extends [mscorlib]System.Object
    implements I1
{
    .method public hidebysig newslot virtual specialname
        instance void op_IncrementAssignment () cil managed 
    {
        .maxstack 8

        IL_0000: ldc.i4.1
        IL_0001: call void [mscorlib]System.Console::Write(int32)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
";

            var source1 =
@"
public class C2 : C1, I1
{
}
";
            var compilation1 = CreateCompilationWithIL(source1, ilSource);

            compilation1.VerifyDiagnostics(
                // (2,23): error CS9311: 'C2' does not implement interface member 'I1.op_IncrementAssignment()'. 'C1.operator ++()' cannot implement 'I1.op_IncrementAssignment()' because one of them is not an operator.
                // public class C2 : C1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C2", "I1.op_IncrementAssignment()", "C1.operator ++()").WithLocation(2, 23)
                );

            var source2 =
@"
class Program
{
    static void Main()
    {
        var c1 = new C1();
        ++c1;
        I1 x = c1;
        x.op_IncrementAssignment();
    }
}
";
            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugExe);

            CompileAndVerify(compilation2, expectedOutput: "11", verify: Verification.Skipped).VerifyDiagnostics();

            var i1M1 = compilation1.GetTypeByMetadataName("I1").GetMembers().Single();
            var c1 = compilation1.GetTypeByMetadataName("C1");

            AssertEx.Equal("C1.operator ++()", c1.FindImplementationForInterfaceMember(i1M1).ToDisplayString());
        }

        [Fact]
        public void Increment_103_Consumption_Implementation()
        {
            var source = @"
public interface I1
{
    public void operator ++();
}

public class C1 : I1
{
    public void operator ++()
    {
        System.Console.Write(""[C1.operator]"");
    } 
}

public class C2 : I1
{
    void I1.operator ++()
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        I1 x = new C1();
        ++x;
        x = new C2();
        ++x;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "[C1.operator][C2.operator]").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_104_Consumption_Overriding()
        {
            var source = @"
public abstract class C1
{
    public abstract void operator ++();
}

public class C2 : C1
{
    public override void operator ++()
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        C1 x = new C2();
        ++x;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "[C2.operator]").VerifyDiagnostics();
        }

        [Fact]
        public void Increment_105_Shadow_RegularVsOperatorMismatch()
        {
            var source = @"
public class C1
{
    public void operator ++(){}
    public static C1 operator ++(C1 x) => x;
}

public class C2 : C1
{
    public void op_IncrementAssignment(){}
    public static C1 op_Increment(C1 x) => x;
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Increment_106_Shadow_RegularVsOperatorMismatch()
        {
            var source = @"
public class C2
{
    public void op_IncrementAssignment(){}
    public static C1 op_Increment(C1 x) => x;
}

public class C1 : C2
{
    public void operator ++(){}
    public static C1 operator ++(C1 x) => x;
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Increment_107_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"()""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
    public void operator " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"()""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"()", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_108_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator ++' that could not be resolved
                // /// See <see cref="operator ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op).WithArguments("operator " + op).WithLocation(3, 20),
                // (11,20): warning CS1574: XML comment has cref attribute 'operator ++' that could not be resolved
                // /// See <see cref="C1.operator ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator " + op).WithArguments("operator " + op).WithLocation(11, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_109_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_110_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
    public void operator " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_111_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public static C1 operator " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_112_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"(C1)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public static C1 operator " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"(C1)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_113_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"(C1)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"(C1)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator ++(C1)' that could not be resolved
                // /// See <see cref="operator ++(C1)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op + @"(C1)").WithArguments("operator " + op + @"(C1)").WithLocation(3, 20),
                // (11,20): warning CS1574: XML comment has cref attribute 'operator ++(C1)' that could not be resolved
                // /// See <see cref="C1.operator ++(C1)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator " + op + @"(C1)").WithArguments("operator " + op + @"(C1)").WithLocation(11, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void Increment_114_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C1
{
    public static C1 " + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + @"() => null;
}

/// <summary>
/// See <see cref=""C1.operator " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator ++' that could not be resolved
                // /// See <see cref="operator ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op).WithArguments("operator " + op).WithLocation(3, 20),
                // (11,20): warning CS1574: XML comment has cref attribute 'operator ++' that could not be resolved
                // /// See <see cref="C1.operator ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator " + op).WithArguments("operator " + op).WithLocation(11, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void Increment_115_CRef([CombinatorialValues("++", "--")] string op)
        {
            string name = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            var source = @"
/// <summary>
/// See <see cref=""" + name + @"""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1." + name + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'op_IncrementAssignment' that could not be resolved
                // /// See <see cref="op_IncrementAssignment"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, name).WithArguments(name).WithLocation(3, 20),
                // (11,20): warning CS1574: XML comment has cref attribute 'op_IncrementAssignment' that could not be resolved
                // /// See <see cref="C1.op_IncrementAssignment"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1." + name).WithArguments(name).WithLocation(11, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void Increment_116_CRef([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""" + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1." + (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"()", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_117_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"()""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
    public static C1 operator checked " + op + @"(C1 x) => x;
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1.operator checked " + op + @"()""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"()", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_118_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1.operator checked " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                    // (3,20): warning CS1574: XML comment has cref attribute 'operator checked ++' that could not be resolved
                // /// See <see cref="operator checked ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator checked ++' that could not be resolved
                // /// See <see cref="C1.operator checked ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + op).WithArguments("operator checked " + op).WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_119_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
    public static C1 operator checked " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1.operator checked " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_120_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
    public static C1 operator checked " + op + @"(C1 x) => x;
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_121_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
    public static C1 operator " + op + @"(C1 x) => x;
    public static C1 operator checked " + op + @"(C1 x) => x;
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_122_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C1)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
    public static C1 operator " + op + @"(C1 x) => x;
    public static C1 operator checked " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1.operator checked " + op + @"(C1)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(C1)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_123_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C1)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
}

#line 10
/// <summary>
/// See <see cref=""C1.operator checked " + op + @"(C1)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked ++(C1)' that could not be resolved
                // /// See <see cref="operator checked ++(C1)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op + @"(C1)").WithArguments("operator checked " + op + @"(C1)").WithLocation(3, 20),
                // (11,20): warning CS1574: XML comment has cref attribute 'operator ++(C1)' that could not be resolved
                // /// See <see cref="C1.operator checked ++(C1)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + op + @"(C1)").WithArguments("operator checked " + op + @"(C1)").WithLocation(11, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void Increment_124_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C1
{
    public static C1 " + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + @"() => null;
}

/// <summary>
/// See <see cref=""C1.operator checked " + op + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                    // (3,20): warning CS1574: XML comment has cref attribute 'operator checked ++' that could not be resolved
                // /// See <see cref="operator checked ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (11,20): warning CS1574: XML comment has cref attribute 'operator checked ++' that could not be resolved
                // /// See <see cref="C1.operator checked ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + op).WithArguments("operator checked " + op).WithLocation(11, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void Increment_125_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            string name = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            var source = @"
/// <summary>
/// See <see cref=""" + name + @"""/>.
/// </summary>
class C1
{
    public static C1 operator " + op + @"(C1 x) => x;
    public static C1 operator checked " + op + @"(C1 x) => x;
}

/// <summary>
/// See <see cref=""C1." + name + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'op_CheckedIncrementAssignment' that could not be resolved
                // /// See <see cref="op_CheckedIncrementAssignment"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, name).WithArguments(name).WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'op_CheckedIncrementAssignment' that could not be resolved
                // /// See <see cref="C1.op_CheckedIncrementAssignment"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1." + name).WithArguments(name).WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void Increment_126_CRef_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""" + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
}

/// <summary>
/// See <see cref=""C1." + (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"()", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_127_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct S1
{
    public static readonly S1 operator " + op + @"(S1 s) => s;
    public static readonly S1 operator checked " + op + @"(S1 s) => s;
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (4,40): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly S1 operator ++(S1 s) => s;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(4, 40),
                // (5,48): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly S1 operator checked ++(S1 s) => s;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(5, 48)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>())
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_128_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct S1
{
    public readonly void operator " + op + @"() {}
    public readonly void operator checked " + op + @"() {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_129_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
readonly struct S1
{
    public void operator " + op + @"() {}
    public void operator checked " + op + @"() {}
}

readonly struct S2
{
    public readonly void operator " + op + @"() {}
    public readonly void operator checked " + op + @"() {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("S2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_130_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
struct S1
{
    int F;
    public readonly void operator " + op + @"() { F++; }
    public readonly void operator checked " + op + @"() { F++; }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (5,42): error CS1604: Cannot assign to 'F' because it is read-only
                //     public readonly void operator ++() { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(5, 42),
                // (6,50): error CS1604: Cannot assign to 'F' because it is read-only
                //     public readonly void operator checked ++() { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(6, 50)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_131_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
readonly struct S1
{
    public void operator " + op + @"() { this = new S1(); }
    public void operator checked " + op + @"() { this = new S1(); }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (4,33): error CS1604: Cannot assign to 'this' because it is read-only
                //     public void operator ++() { this = new S1(); }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(4, 33),
                // (5,41): error CS1604: Cannot assign to 'this' because it is read-only
                //     public void operator checked ++() { this = new S1(); }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(5, 41)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_132_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1<T> where T : I1<T>
{
    static abstract T operator " + op + @"(T s);
    static abstract T operator checked " + op + @"(T s);
}

struct S1 : I1<S1>
{
    static readonly S1 I1<S1>.operator " + op + @"(S1 s) => s;
    static readonly S1 I1<S1>.operator checked " + op + @"(S1 s) => s;
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            compilation.VerifyDiagnostics(
                // (10,40): error CS0106: The modifier 'readonly' is not valid for this item
                //     static readonly S1 I1<S1>.operator ++(S1 s) => s;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(10, 40),
                // (11,48): error CS0106: The modifier 'readonly' is not valid for this item
                //     static readonly S1 I1<S1>.operator checked ++(S1 s) => s;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(11, 48)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_133_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
    void operator checked " + op + @"();
}

struct S1 : I1
{
    readonly void I1.operator " + op + @"() {}
    readonly void I1.operator checked " + op + @"() {}
}

readonly struct S2 : I1
{
    readonly void I1.operator " + op + @"() {}
    readonly void I1.operator checked " + op + @"() {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("S2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_134_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
    void operator checked " + op + @"();
}

readonly struct S1 : I1
{
    void I1.operator " + op + @"() {}
    void I1.operator checked " + op + @"() {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_135_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
    void operator checked " + op + @"();
}

struct S1 : I1
{
    int F;
    readonly void I1.operator " + op + @"() { F++; }
    readonly void I1.operator checked " + op + @"() { F++; }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (11,38): error CS1604: Cannot assign to 'F' because it is read-only
                //     readonly void I1.operator --() { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(11, 38),
                // (12,46): error CS1604: Cannot assign to 'F' because it is read-only
                //     readonly void I1.operator checked --() { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(12, 46)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_136_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
    void operator checked " + op + @"();
}

readonly struct S1 : I1
{
    void I1.operator " + op + @"() { this = new S1(); }
    void I1.operator checked " + op + @"() { this = new S1(); }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (10,29): error CS1604: Cannot assign to 'this' because it is read-only
                //     void I1.operator ++() { this = new S1(); }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(10, 29),
                // (11,37): error CS1604: Cannot assign to 'this' because it is read-only
                //     void I1.operator checked ++() { this = new S1(); }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(11, 37)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_137_Readonly([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"();
    void operator checked " + op + @"();
}

interface I2 : I1
{
#line 200
    readonly void I1.operator " + op + @"() {}
    readonly void I1.operator checked " + op + @"() {}
}

class C3 : I1
{
#line 300
    readonly void I1.operator " + op + @"() {}
    readonly void I1.operator checked " + op + @"() {}
}

class C4
{
#line 400
    public readonly void operator " + op + @"() {}
    public readonly void operator checked " + op + @"() {}
}

interface I5
{
#line 500
    readonly void operator " + op + @"();
    readonly void operator checked " + op + @"();
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            compilation.VerifyDiagnostics(
                    // (200,31): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator ++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(200, 31),
                    // (201,39): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator checked ++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(201, 39),
                    // (300,31): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator ++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(300, 31),
                    // (301,39): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator checked ++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(301, 39),
                    // (400,35): error CS0106: The modifier 'readonly' is not valid for this item
                    //     public readonly void operator ++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(400, 35),
                    // (401,43): error CS0106: The modifier 'readonly' is not valid for this item
                    //     public readonly void operator checked ++() {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(401, 43),
                    // (500,28): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void operator ++();
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(500, 28),
                    // (501,36): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void operator checked ++();
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(501, 36)
                );

            foreach (var m in compilation.GetTypeByMetadataName("I2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("I5").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Increment_138_VisualBasic([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public class C1
{
    public void operator " + op + @"() {}
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            var source2 = @"
Public Module Program
    Public Sub Main()
        Dim c1 = New C1()
        c1" + op + @"
    End Sub
End Module
";
            CreateVisualBasicCompilation("Program", source2, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // error BC30800: Method arguments must be enclosed in parentheses.
                Diagnostic(30800 /*ERRID.ERR_ObsoleteArgumentsNeedParens*/, op).WithLocation(5, 11),
                // error BC30201: Expression expected.
                Diagnostic(30201 /*ERRID.ERR_ExpectedExpression*/, "").WithLocation(5, 13),
                // error BC30454: Expression is not a method.
                Diagnostic(30454 /*ERRID.ERR_ExpectedProcedure*/, "c1").WithLocation(5, 9)
                );

            string opName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            var source3 = @"
Public Module Program
    Public Sub Main()
        Dim c1 = New C1()
        c1." + opName + @"()
    End Sub
End Module
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public Overloads Sub op_IncrementAssignment()' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public Overloads Sub " + opName + @"()", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_139_VisualBasic([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public interface I1
{
    public void operator " + op + @"();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            var source3 = @"
Public Class Program
    Implements I1

    Public Sub " + opName + @"() Implements I1." + opName + @"
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Sub op_IncrementAssignment()' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Sub " + opName + @"()", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_140_VisualBasic_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
public interface I1
{
    public sealed void operator " + op + @"(){}
    public void operator checked " + op + @"();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            var source3 = @"
Public Class Program
    Implements I1

    Public Sub " + opName + @"() Implements I1." + opName + @"
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Sub op_IncrementAssignment()' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Sub " + opName + @"()", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_141_VisualBasic([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
abstract public class C1
{
    public abstract void operator " + op + @"();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            var source3 = @"
Public Class Program
    Inherits C1

    Public Overrides Sub " + opName + @"()
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public MustOverride Overloads Sub op_IncrementAssignment()' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public MustOverride Overloads Sub " + opName + @"()", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_142_VisualBasic_Checked([CombinatorialValues("++", "--")] string op)
        {
            var source1 = @"
abstract public class C1
{
    public void operator " + op + @"(){}
    public abstract void operator checked " + op + @"();
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName);

            var source3 = @"
Public Class Program
    Inherits C1

    Public Overrides Sub " + opName + @"()
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public MustOverride Overloads Sub op_IncrementAssignment()' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public MustOverride Overloads Sub " + opName + @"()", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 26)
                );
        }

        private static void AssertMetadataSymbol(MethodSymbol m, MethodKind kind, string display)
        {
            Assert.Equal(kind, m.MethodKind);
            AssertEx.Equal(display, m.ToDisplayString());
            Assert.False(m.HasUnsupportedMetadata);
            Assert.True(m.HasSpecialName);
        }

        [Theory]
        [CombinatorialData]
        public void Increment_143_MetadataValidation([CombinatorialValues("++", "--")] string op, bool isChecked)
        {
            string name = isChecked ?
                (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) :
                (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            string staticName = isChecked ?
                (op == "++" ? WellKnownMemberNames.CheckedIncrementOperatorName : WellKnownMemberNames.CheckedDecrementOperatorName) :
                (op == "++" ? WellKnownMemberNames.IncrementOperatorName : WellKnownMemberNames.DecrementOperatorName);

            var source1 = @"
using System.Runtime.CompilerServices;

public class C1
{
    [SpecialName]
    public void " + name + @"() {}
}

public class C2
{
    [SpecialName]
    public int " + name + @"() => 0; // Not void returning
}

public class C3
{
    [SpecialName]
    public void " + name + @"(int x = 0) {} // Has parameter
}

public class C4
{
    [SpecialName]
    public void " + name + @"(params int[] x) {} // Has params
}

public class C5
{
    [SpecialName]
    public void " + name + @"(__arglist) {} // Is vararg
}

public class C6
{
    [SpecialName]
    public void " + name + @"<T>() {} // Generic
}

public class C7
{
    [SpecialName]
    public void " + staticName + @"() {}
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.EmitToImageReference();

            var comp2 = CreateCompilation("", references: [comp1Ref]);

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C1." + name),
                                 MethodKind.UserDefinedOperator,
                                 "C1.operator " + (isChecked ? "checked " : "") + op + "()");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C2." + name),
                                 MethodKind.Ordinary,
                                 "C2." + name + "()");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C3." + name),
                                 MethodKind.Ordinary,
                                 "C3." + name + "(int)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C4." + name),
                                 MethodKind.Ordinary,
                                 "C4." + name + "(params int[])");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C5." + name),
                                 MethodKind.Ordinary,
                                 "C5." + name + "(__arglist)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C6." + name),
                                 MethodKind.Ordinary,
                                 "C6." + name + "<T>()");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C7." + staticName),
                                 MethodKind.Ordinary,
                                 "C7." + staticName + "()");
        }

        [Theory]
        [CombinatorialData]
        public void Increment_144_MetadataValidation(
            [CombinatorialValues("++", "--")] string op,
            [CombinatorialValues("private", "protected", "private protected", "internal", "internal protected")] string accessibility,
            bool isChecked)
        {
            string name = isChecked ?
                (op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) :
                (op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName);

            var source1 = @"
using System.Runtime.CompilerServices;
public class C1
{
    [SpecialName]
    " + accessibility + @" void " + name + @"() {}
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.EmitToImageReference();

            var comp2 = CreateCompilation("", references: [comp1Ref], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C1." + name),
                                 MethodKind.Ordinary,
                                 "C1." + name + "()");
        }

        [Theory]
        [CombinatorialData]
        public void Increment_145_ExpressionTree([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
using System.Linq.Expressions;

public class C1
{
    public void operator" + op + @"()
    {
    } 
}

public class Program
{
    static void Main()
    {
        Expression<System.Action<C1>> x = (c1) => c1" + op + @";
    } 
}
";

            var comp2 = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp2.VerifyDiagnostics(
                // (15,51): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<System.Action<C1>> x = (c1) => c1++;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "c1" + op).WithLocation(15, 51)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_146_Partial([CombinatorialValues("++", "--")] string op)
        {
            var source = @"
partial class C1
{
    public partial void operator" + op + @"();

    public partial void M()
    {
    } 

    public partial void M();
}
";

            var comp2 = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp2.VerifyDiagnostics(
                // (4,20): error CS1519: Invalid token 'void' in a member declaration
                //     public partial void operator++();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "void").WithArguments("void").WithLocation(4, 20),
                // (4,33): error CS9308: User-defined operator 'C1.operator ++()' must be declared public
                //     public partial void operator++();
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + op + "()").WithLocation(4, 33),
                // (4,33): error CS0501: 'C1.operator ++()' must declare a body because it is not marked abstract, extern, or partial
                //     public partial void operator++();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, op).WithArguments("C1.operator " + op + "()").WithLocation(4, 33)
                );
        }

        [Fact]
        public void Increment_147_CopyModifiers()
        {
            /*
                public class C1
                {
                    public virtual void modopt(int64) operator ++() {}
                }
            */
            var ilSource = @"
.class public auto ansi beforefieldinit C1
    extends System.Object
{
    .method public hidebysig specialname newslot virtual 
        instance void modopt(int64) op_IncrementAssignment () cil managed 
    {
        // Method begins at RVA 0x2069
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: nop
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
public class C2 : C1
{
    public override void operator ++()
    {
        System.Console.Write(""C2"");
    }

    static void Main()
    {
        C1 c1 = new C2();
        c1++;
    }
}
";
            var compilation1 = CreateCompilationWithIL([source1, CompilerFeatureRequiredAttribute], ilSource, options: TestOptions.DebugExe);
            CompileAndVerify(compilation1, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "C2").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                AssertEx.Equal("void modopt(System.Int64) C2.op_IncrementAssignment()", m.GlobalNamespace.GetMember("C2.op_IncrementAssignment").ToTestDisplayString());
            }
        }

        [Fact]
        public void Increment_148_Consumption_InCatchFilter()
        {
            var source = @"
public struct C1
{
    public int _F;
    public void operator ++()
    {
        System.Console.Write(""++"");
        _F++;
    } 
    public void operator --()
    {
        System.Console.Write(""--"");
        _F--;
    } 

    public static implicit operator bool (C1 x) => x._F % 2 == 0;
}

public class Program
{
    static void Main()
    {
        C1 x = new C1();
        
        try 
        {
            try 
            {
                throw null;
            }
            catch when (++x)
            {
                System.Console.Write(""!"");
            }
        }
        catch when (--x)
        {
            System.Console.Write(x._F);
        }
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "++--0").VerifyDiagnostics();

            verifier.VerifyIL("Program.Main",
@"
{
  // Code size      102 (0x66)
  .maxstack  2
  .locals init (C1 V_0, //x
                bool V_1,
                C1 V_2,
                bool V_3)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""C1""
  .try
  {
    IL_0009:  nop
    .try
    {
      IL_000a:  nop
      IL_000b:  ldnull
      IL_000c:  throw
    }
    filter
    {
      IL_000d:  pop
      IL_000e:  ldloc.0
      IL_000f:  stloc.2
      IL_0010:  ldloca.s   V_2
      IL_0012:  call       ""void C1.op_IncrementAssignment()""
      IL_0017:  nop
      IL_0018:  ldloc.2
      IL_0019:  stloc.0
      IL_001a:  ldloc.2
      IL_001b:  call       ""bool C1.op_Implicit(C1)""
      IL_0020:  stloc.1
      IL_0021:  ldloc.1
      IL_0022:  ldc.i4.0
      IL_0023:  cgt.un
      IL_0025:  endfilter
    }  // end filter
    {  // handler
      IL_0027:  pop
      IL_0028:  nop
      IL_0029:  ldstr      ""!""
      IL_002e:  call       ""void System.Console.Write(string)""
      IL_0033:  nop
      IL_0034:  nop
      IL_0035:  leave.s    IL_0037
    }
    IL_0037:  nop
    IL_0038:  leave.s    IL_0065
  }
  filter
  {
    IL_003a:  pop
    IL_003b:  ldloc.0
    IL_003c:  stloc.2
    IL_003d:  ldloca.s   V_2
    IL_003f:  call       ""void C1.op_DecrementAssignment()""
    IL_0044:  nop
    IL_0045:  ldloc.2
    IL_0046:  stloc.0
    IL_0047:  ldloc.2
    IL_0048:  call       ""bool C1.op_Implicit(C1)""
    IL_004d:  stloc.3
    IL_004e:  ldloc.3
    IL_004f:  ldc.i4.0
    IL_0050:  cgt.un
    IL_0052:  endfilter
  }  // end filter
  {  // handler
    IL_0054:  pop
    IL_0055:  nop
    IL_0056:  ldloc.0
    IL_0057:  ldfld      ""int C1._F""
    IL_005c:  call       ""void System.Console.Write(int)""
    IL_0061:  nop
    IL_0062:  nop
    IL_0063:  leave.s    IL_0065
  }
  IL_0065:  ret
}
");
        }

        [Fact]
        public void Increment_149_Consumption_ConditionalAccessTarget()
        {
            var source = @"
class C1
{
    public int _F;
    public void operator ++()
    {
        System.Console.Write(""++"");
        _F++;
    } 
}

class C2
{
    public C1 _F;
}

class Program
{
    static void Test(C2 x)
    {
        x._F++; 
        ++x._F;
        x?._F++; 
        ++x?._F; 
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (23,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         x?._F++; 
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "x?._F").WithLocation(23, 9),
                // (24,11): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         ++x?._F; 
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "x?._F").WithLocation(24, 11)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Increment_150_GetOperatorKind([CombinatorialValues("++", "--")] string op)
        {
            SyntaxKind kind = SyntaxFactory.ParseToken(op).Kind();

            string name = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(kind, isChecked: false);
            Assert.Equal(op == "++" ? WellKnownMemberNames.IncrementAssignmentOperatorName : WellKnownMemberNames.DecrementAssignmentOperatorName, name);
            Assert.Equal(kind, SyntaxFacts.GetOperatorKind(name));

            name = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(kind, isChecked: true);
            Assert.Equal(op == "++" ? WellKnownMemberNames.CheckedIncrementAssignmentOperatorName : WellKnownMemberNames.CheckedDecrementAssignmentOperatorName, name);
            Assert.Equal(kind, SyntaxFacts.GetOperatorKind(name));
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/78964")]
        public void Increment_151_RefSafety([CombinatorialValues("++", "--")] string op)
        {
            //      r1 = ++r2;
            // is equivalent to
            //      var tmp = r2;
            //      tmp.op_IncrementAssignment();
            //      r2 = tmp;
            //      r1 = tmp;
            // which is *not* ref safe (scoped tmp cannot be assigned to unscoped r1)
            var source = $$"""
                ref struct R
                {
                    private ref readonly int _i;
                    public R(in int i) { _i = ref i; }
                    public void operator {{op}}() { }
                }
                class Program
                {
                    static R F1(R r1, scoped R r2)
                    {
                        r1 = {{op}}r2;
                        return r1;
                    }
                }
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (11,14): error CS8352: Cannot use variable 'scoped R r2' in this context because it may expose referenced variables outside of their declaration scope
                //         r1 = ++r2;
                Diagnostic(ErrorCode.ERR_EscapeVariable, $"{op}r2").WithArguments("scoped R r2").WithLocation(11, 14));
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/78964")]
        public void Increment_152_RefSafety([CombinatorialValues("++", "--")] string op)
        {
            //      F2(out var r1, ++r2); return r1;
            // is equivalent to
            //      var tmp = r2;
            //      tmp.op_IncrementAssignment();
            //      r2 = tmp;
            //      F2(out var r1, tmp); return r1;
            // which is *not* ref safe (r1 is inferred as scoped from tmp but scoped r1 cannot be returned)
            var source = $$"""
                ref struct R
                {
                    private ref readonly int _i;
                    public R(in int i) { _i = ref i; }
                    public void operator {{op}}() { }
                }
                class Program
                {
                    static R F1(scoped R r2)
                    {
                        F2(out var r1, {{op}}r2);
                        return r1;
                    }
                    static void F2(out R r1, R r2) => r1 = r2;
                }
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
                // (12,16): error CS8352: Cannot use variable 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         return r1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "r1").WithArguments("r1").WithLocation(12, 16));
        }

        internal static string CompoundAssignmentOperatorName(string op, bool isChecked = false)
        {
            SyntaxKind kind = CompoundAssignmentOperatorTokenKind(op);

            return OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(kind, isChecked: isChecked);
        }

        private static SyntaxKind CompoundAssignmentOperatorTokenKind(string op)
        {
            return op switch
            {
                ">>=" => SyntaxKind.GreaterThanGreaterThanEqualsToken,
                ">>>=" => SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken,
                _ => SyntaxFactory.ParseToken(op).Kind(),
            };
        }

        private static bool CompoundAssignmentOperatorHasCheckedForm(string op) => op is "+=" or "-=" or "*=" or "/=";

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00010([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public void operator checked" + op + @"(C1 x) {} 
";
            }

            var source =
typeKeyword + @" C1
{
    public void operator" + op + @"(C1 x) {}
" + checkedForm + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.Regular14, targetFramework: TargetFramework.Net60);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                checkedForm is null ?
                    [
                        // (3,25): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                        //     public void operator%=(C1 x) {}
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(3, 25)
                    ] :
                    [
                        // (3,25): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                        //     public void operator*=(C1 x) {}
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(3, 25),
                        // (5,33): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                        //     public void operator checked*=(C1 x) {} 
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(5, 33)
                    ]
                );

            validate(comp.SourceModule);

            comp = CreateCompilation(source, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                checkedForm is null ?
                    [
                        // (3,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
                        //     public void operator+=(C1 x) {}
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, op).WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(3, 25)
                    ] :
                    [
                        // (3,25): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
                        //     public void operator+=(C1 x) {}
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, op).WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(3, 25),
                        // (5,33): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
                        //     public void operator checked+=(C1 x) {} 
                        Diagnostic(ErrorCode.ERR_MissingPredefinedMember, op).WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(5, 33)
                    ]
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
                Assert.Empty(m.GetAttributes());
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00011_HasCheckedForm([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            Assert.True(CompoundAssignmentOperatorHasCheckedForm(op));

            var source =
typeKeyword + @" C1
{
    public void operator " + op + @"(C1 x) {} 
    public void operator checked" + op + @"(C1 x) {} 
}

interface I1
{
    public void operator " + op + @"(C1 x) {} 
    public void operator checked" + op + @"(C1 x) {} 
}

" + typeKeyword + @" C2 : I1
{
    void I1.operator " + op + @"(C1 x) {} 
    void I1.operator checked" + op + @"(C1 x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00012_DoesNotHaveCheckedForm([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            Assert.False(CompoundAssignmentOperatorHasCheckedForm(op));

            var source =
typeKeyword + @" C1
{
    public void operator checked" + op + @"(C1 x) {} 
}

interface I1
{
    public void operator " + op + @"(C1 x) {} 
}

" + typeKeyword + @" C2 : I1
{
    void I1.operator checked" + op + @"(C1 x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                // (3,26): error CS9023: User-defined operator '%=' cannot be declared checked
                //     public void operator checked%=(C1 x) {} 
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(3, 26),
                // (13,22): error CS9023: User-defined operator '%=' cannot be declared checked
                //     void I1.operator checked%=(C1 x) {} 
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(13, 22)
                );

            validateOp(comp.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));

            static void validateOp(MethodSymbol m)
            {
                Assert.Equal(MethodKind.UserDefinedOperator, m.MethodKind);
                Assert.False(m.IsStatic);
                Assert.False(m.IsAbstract);
                Assert.Equal(m.ContainingType.IsInterface, m.IsVirtual);
                Assert.False(m.IsSealed);
                Assert.False(m.IsOverride);
                Assert.True(m.HasSpecialName);
                Assert.False(m.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00013_Not_Static(
            [CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op,
            [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public static void operator checked" + op + @"(C1 x) {} 
";
            }

            var source =
typeKeyword + @" C1
{
    public static void operator" + op + @"(C1 x) {}
" + checkedForm + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                checkedForm is null ?
                    [
                        // (3,32): error CS0106: The modifier 'static' is not valid for this item
                        //     public static void operator+=(C1 x) {}
                        Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(3, 32)
                    ] :
                    [
                        // (3,32): error CS0106: The modifier 'static' is not valid for this item
                        //     public static void operator+=(C1 x) {}
                        Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(3, 32),
                        // (5,40): error CS0106: The modifier 'static' is not valid for this item
                        //     public static void operator checked+=(C1 x) {} 
                        Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(5, 40)
                    ]
                );

            validate(comp.SourceModule);

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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
                Assert.Equal(Accessibility.Public, m.DeclaredAccessibility);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00014_NotInStaticClass([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public void operator checked" + op + @"(int x) {} 
";
            }

            var source = @"
static class C1
{
    public void operator" + op + @"(int x) {}
" + checkedForm + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                checkedForm is null ?
                    [
                        // (4,25): error CS0715: 'C1.operator +=(int)': static classes cannot contain user-defined operators
                        //     public void operator+=(int x) {}
                        Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 25)
                    ] :
                    [
                        // (4,25): error CS0715: 'C1.operator +=(int)': static classes cannot contain user-defined operators
                        //     public void operator+=(int x) {}
                        Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 25),
                        // (6,33): error CS0715: 'C1.operator checked +=(int)': static classes cannot contain user-defined operators
                        //     public void operator checked+=(int x) {} 
                        Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(6, 33)
                    ]
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00020_MustBePublic([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool structure, bool isChecked)
        {
            if (isChecked && !CompoundAssignmentOperatorHasCheckedForm(op))
            {
                return;
            }

            var source =
(structure ? "struct" : "class") + @" C1
{
    void operator " + (isChecked ? "checked " : "") + op + @"(int x) {} 
" + (isChecked ? "public void operator " + op + @"(int x) {}" : "") + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (3,19): error CS9308: User-defined operator 'C1.operator +=(int)' must be declared public
                //     void operator +=(int x) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + (isChecked ? "checked " : "") + op + @"(int)").WithLocation(3, 19 + (isChecked ? 8 : 0))
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00021_ImplicitlyPublicInInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    void operator checked" + op + @"(C1 x);
";
            }

            var source = @"
interface C1
{
    void operator" + op + @"(C1 x);
" + checkedForm + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            validate(comp.SourceModule);

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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
        public void CompoundAssignment_00022_MustBePublic_ExplicitAccessibility(
            [CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op,
            [CombinatorialValues("struct", "class", "interface")] string typeKeyword,
            [CombinatorialValues("private", "internal", "protected", "internal protected", "private protected")] string accessibility,
            bool isChecked)
        {
            if (isChecked && !CompoundAssignmentOperatorHasCheckedForm(op))
            {
                return;
            }

            var source =
typeKeyword + @" C1
{
    " + accessibility + @"
    void operator " + (isChecked ? "checked " : "") + op + @"(int x) {} 
" + (isChecked ? "public void operator " + op + @"(int x) {}" : "") + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics(
                // (4,19): error CS9308: User-defined operator 'C1.operator +=(int)' must be declared public
                //     void operator +=(int x) {} 
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + (isChecked ? "checked " : "") + op + @"(int)").WithLocation(4, 19 + (isChecked ? 8 : 0))
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00030_MustReturnVoid([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public C1 operator " + op + @"(int x) => throw null; 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,24): error CS9310: The return type for this operator must be void
                //     public C1 operator +=(int x) => throw null; 
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(3, 24)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00040_MustReturnVoid_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public C1 operator checked " + op + @"(int x) => throw null; 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,32): error CS9310: The return type for this operator must be void
                //     public C1 operator checked +=(int x) => throw null; 
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(3, 32),
                // (3,32): error CS9025: The operator 'C1.operator checked +=(int)' requires a matching non-checked version of the operator to also be defined
                //     public C1 operator checked +=(int x) => throw null; 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(int)").WithLocation(3, 32)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00050_WrongNumberOfParameters([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator" + op + @"() {} 
    public void operator" + op + @"(C1 x, C1 y) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,25): error CS9313: Overloaded compound assignment operator '+=' takes one parameter
                //     public void operator+=() {} 
                Diagnostic(ErrorCode.ERR_BadCompoundAssignmentOpArgs, op).WithArguments(op).WithLocation(3, 25),
                // (4,25): error CS1020: Overloadable binary operator expected
                //     public void operator+=(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 25)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00060_WrongNumberOfParameters_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("struct", "class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator checked " + op + @"() {} 
    public void operator checked " + op + @"(C1 x, C1 y) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,34): error CS9313: Overloaded compound assignment operator '+=' takes one parameter
                //     public void operator checked +=() {} 
                Diagnostic(ErrorCode.ERR_BadCompoundAssignmentOpArgs, op).WithArguments(op).WithLocation(3, 34),
                // (3,34): error CS9025: The operator 'C1.operator checked +=()' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked +=() {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "()").WithLocation(3, 34),
                // (4,34): error CS1020: Overloadable binary operator expected
                //     public void operator checked +=(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 34),
                // (4,34): error CS9025: The operator 'C1.operator checked +=(C1, C1)' requires a matching non-checked version of the operator to also be defined
                //     public void operator checked +=(C1 x, C1 y) {} 
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C1.operator checked " + op + "(C1, C1)").WithLocation(4, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00090_AbstractAllowedInClassAndInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public abstract void operator checked" + op + @"(int x); 
";
            }

            var source =
typeKeyword + @" C1
{
    public abstract void operator" + op + @"(int x);
" + checkedForm + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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

            comp = CreateCompilation(["class C2 : C1 {}", source, CompilerFeatureRequiredAttribute]);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    checkedForm is null ?
                        [
                            // (1,12): error CS0535: 'C2' does not implement interface member 'C1.operator +=(int)'
                            // class C2 : C1 {}
                            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "C1").WithArguments("C2", "C1.operator " + op + @"(int)").WithLocation(1, 12)
                        ] :
                        [
                            // (1,12): error CS0535: 'C2' does not implement interface member 'C1.operator +=(int)'
                            // class C2 : C1 {}
                            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "C1").WithArguments("C2", "C1.operator " + op + @"(int)").WithLocation(1, 12),
                            // (1,12): error CS0535: 'C2' does not implement interface member 'C1.operator checked +=(int)'
                            // class C2 : C1 {}
                            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "C1").WithArguments("C2", "C1.operator checked " + op + @"(int)").WithLocation(1, 12)
                        ]
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    checkedForm is null ?
                        [
                            // (1,7): error CS0534: 'C2' does not implement inherited abstract member 'C1.operator +=(int)'
                            // class C2 : C1 {}
                            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C2").WithArguments("C2", "C1.operator " + op + @"(int)").WithLocation(1, 7)
                        ] :
                        [
                            // (1,7): error CS0534: 'C2' does not implement inherited abstract member 'C1.operator checked +=(int)'
                            // class C2 : C1 {}
                            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C2").WithArguments("C2", "C1.operator checked " + op + @"(int)").WithLocation(1, 7),
                            // (1,7): error CS0534: 'C2' does not implement inherited abstract member 'C1.operator +=(int)'
                            // class C2 : C1 {}
                            Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C2").WithArguments("C2", "C1.operator " + op + @"(int)").WithLocation(1, 7)
                        ]
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00100_AbstractIsOptionalInInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public void operator checked" + op + @"(int x);
";
            }

            var source = @"
interface C1
{
    public void operator" + op + @"(int x);
" + checkedForm + @"
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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
        public void CompoundAssignment_00110_AbstractCanBeImplementedInInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I3 : I1
{
    void I1.operator " + op + @"(int x) {}
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source += @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

interface I4 : I2
{
    void I2.operator checked " + op + @"(int x) {}
}

class C : I3, I4
{}
";
            }
            else
            {
                source += @"
class C : I3
{}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + CompoundAssignmentOperatorName(op, isChecked: false) + "(System.Int32 x)");
                if (hasCheckedForm)
                {
                    validateOp(
                        m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                        "void I2." + CompoundAssignmentOperatorName(op, isChecked: true) + "(System.Int32 x)");
                }
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
        public void CompoundAssignment_00120_AbstractCanBeImplementedExplicitlyInClassAndStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source1 = @"
public interface I1
{
    void operator " + op + @"(int x);
}
";
            var source2 =
typeKeyword + @" C3 : I1
{
    void I1.operator " + op + @"(int x) {}
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source1 += @"
public interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}
";
                source2 += @"
" + typeKeyword + @" C4 : I2
{
    void I2.operator checked " + op + @"(int x) {}
}
";
            }

            var comp = CreateCompilation(source1 + source2, targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            var comp1 = CreateCompilation(source1, targetFramework: TargetFramework.Net90);

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], targetFramework: TargetFramework.Net90, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(
                hasCheckedForm ?
                    [
                        // (3,22): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                        //     void I1.operator *=(int x) {}
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(3, 22),
                        // (8,30): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                        //     void I2.operator checked *=(int x) {}
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(8, 30)
                    ] :
                    [
                        // (3,22): error CS9260: Feature 'user-defined compound assignment operators' is not available in C# 13.0. Please use language version 14.0 or greater.
                        //     void I1.operator %=(int x) {}
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, op).WithArguments("user-defined compound assignment operators", "14.0").WithLocation(3, 22)
                    ]
                );

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I1." + CompoundAssignmentOperatorName(op, isChecked: false) + "(System.Int32 x)");
                if (hasCheckedForm)
                {
                    validateOp(
                        m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                        "void I2." + CompoundAssignmentOperatorName(op, isChecked: true) + "(System.Int32 x)");
                }
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
        public void CompoundAssignment_00130_AbstractCanBeImplementedImplicitlyInClassAndStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3 : I1
{
    public void operator " + op + @"(int x) {}
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source += @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    public void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == CompoundAssignmentOperatorName(op, isChecked: false)).Single());
                if (hasCheckedForm)
                {
                    validateOp(m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().
                        Where(m => m.Name == CompoundAssignmentOperatorName(op, isChecked: true)).Single());
                }
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
        public void CompoundAssignment_00140_AbstractAllowedOnExplicitImplementationInInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x) {}
}

interface I3 : I1
{
    abstract void I1.operator " + op + @"(int x);
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source += @"
interface I2
{
    void operator checked " + op + @"(int x) {}
    sealed void operator " + op + @"(int x) {}
}

interface I4 : I2
{
    abstract void I2.operator checked " + op + @"(int x);
}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + CompoundAssignmentOperatorName(op, isChecked: false) + "(System.Int32 x)");
                if (hasCheckedForm)
                {
                    validateOp(
                        m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                        "void I2." + CompoundAssignmentOperatorName(op, isChecked: true) + "(System.Int32 x)");
                }
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

            comp = CreateCompilation(["class C1 : I3 {}", source], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (1,12): error CS0535: 'C1' does not implement interface member 'I1.operator +=(int)'
                // class C1 : I3 {}
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I3").WithArguments("C1", "I1.operator " + op + @"(int)").WithLocation(1, 12)
                );

            if (hasCheckedForm)
            {
                comp = CreateCompilation(["class C1 : I4 {}", source], targetFramework: TargetFramework.Net90);
                comp.VerifyDiagnostics(
                    // (1,12): error CS0535: 'C1' does not implement interface member 'I2.operator checked +=(int)'
                    // class C1 : I4 {}
                    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I4").WithArguments("C1", "I2.operator checked " + op + @"(int)").WithLocation(1, 12)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00150_AbstractCannotHaveBody([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public abstract void operator checked" + op + @"(int x) {} 
";
            }

            var source =
typeKeyword + @" C1
{
    public abstract void operator" + op + @"(int x) {}
" + checkedForm + @"
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                checkedForm is null ?
                    [
                        // (3,34): error CS0500: 'C1.operator +=(int)' cannot declare a body because it is marked abstract
                        //     public abstract void operator+=(int x) {}
                        Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 34)
                    ] :
                    [
                        // (3,34): error CS0500: 'C1.operator +=(int)' cannot declare a body because it is marked abstract
                        //     public abstract void operator+=(int x) {}
                        Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 34),
                        // (5,42): error CS0500: 'C1.operator checked +=(int)' cannot declare a body because it is marked abstract
                        //     public abstract void operator checked+=(int x) {} 
                        Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(5, 42)
                    ]
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00160_AbstractExplicitImplementationCannotHaveBody([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator" + op + @"(int x);
}

interface I2 : I1
{
   abstract void I1.operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (9,29): error CS0500: 'I2.I1.operator +=(int)' cannot declare a body because it is marked abstract
                //    abstract void I1.operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("I2.I1.operator " + op + @"(int)").WithLocation(9, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00161_AbstractExplicitImplementationCannotHaveBody_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    sealed void operator" + op + @"(int x) {}
    void operator checked" + op + @"(int x);
}

interface I2 : I1
{
   abstract void I1.operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (10,37): error CS0500: 'I2.I1.operator checked +=(int)' cannot declare a body because it is marked abstract
                //    abstract void I1.operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_AbstractHasBody, op).WithArguments("I2.I1.operator checked " + op + @"(int)").WithLocation(10, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00170_AbstractNotAllowedInNonAbstractClass([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("sealed", "")] string typeModifier)
        {
            var source = @"
" + typeModifier + @" class C1
{
    public abstract void operator" + op + @"(int x);
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source +=
typeModifier + @" class C2
{
    public abstract void operator checked " + op + @"(int x);
    public void operator " + op + @"(int x) {}
}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                hasCheckedForm ?
                    [
                        // (4,34): error CS0513: 'C1.operator +=(int)' is abstract but it is contained in non-abstract type 'C1'
                        //     public abstract void operator+=(int x);
                        Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, op).WithArguments("C1.operator " + op + @"(int)", "C1").WithLocation(4, 34),
                        // (8,43): error CS0513: 'C2.operator checked +=(int)' is abstract but it is contained in non-abstract type 'C2'
                        //     public abstract void operator checked +=(int x);
                        Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, op).WithArguments("C2.operator checked " + op + @"(int)", "C2").WithLocation(8, 43)
                    ] :
                    [
                        // (4,34): error CS0513: 'C1.operator +=(int)' is abstract but it is contained in non-abstract type 'C1'
                        //     public abstract void operator+=(int x);
                        Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, op).WithArguments("C1.operator " + op + @"(int)", "C1").WithLocation(4, 34)
                    ]
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00190_AbstractNotAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct C1
{
    public abstract void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,34): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00191_AbstractNotAllowedInStruct_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct C2
{
    public abstract void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,43): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00200_AbstractNotAllowedOnExplicitImplementationInClassAndStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3 : I1
{
    abstract void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (9,31): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(9, 31)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00201_AbstractNotAllowedOnExplicitImplementationInClassAndStruct_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("abstract class", "struct")] string typeKeyword)
        {
            var source = @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    abstract void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (10,39): error CS0106: The modifier 'abstract' is not valid for this item
                //     abstract void I2.operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(10, 39)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00210_VirtualAllowedInClassAndInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public virtual void operator checked" + op + @"(int x) {} 
";
            }

            var source =
typeKeyword + @" C1
{
    public virtual void operator" + op + @"(int x) {}
" + checkedForm + @" 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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
        public void CompoundAssignment_00220_VirtualIsOptionalInInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    void operator checked" + op + @"(int x) {}
";
            }

            var source = @"
interface C1
{
    void operator" + op + @"(int x) {}
" + checkedForm + @" 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                if (checkedForm is not null)
                {
                    validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
                }
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
        public void CompoundAssignment_00230_VirtualCanBeImplementedInInterface([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    virtual void operator " + op + @"(int x) {}
}

interface I3 : I1
{
    void I1.operator " + op + @"(int x) {}
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source += @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

interface I4 : I2
{
    void I2.operator checked " + op + @"(int x) {}
}

class C : I3, I4
{}
";
            }
            else
            {
                source += @"
class C : I3
{}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("I3").GetMembers().OfType<MethodSymbol>().Single(),
                    "void I1." + CompoundAssignmentOperatorName(op, isChecked: false) + "(System.Int32 x)");
                if (hasCheckedForm)
                {
                    validateOp(
                        m.GlobalNamespace.GetTypeMember("I4").GetMembers().OfType<MethodSymbol>().Single(),
                        "void I2." + CompoundAssignmentOperatorName(op, isChecked: true) + "(System.Int32 x)");
                }
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
        public void CompoundAssignment_00240_VirtualCanBeImplementedExplicitlyInClassAndStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    virtual void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C3 : I1
{
    void I1.operator " + op + @"(int x) {}
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source += @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    void I2.operator checked " + op + @"(int x) {}
}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                    "void I1." + CompoundAssignmentOperatorName(op, isChecked: false) + "(System.Int32 x)");
                if (hasCheckedForm)
                {
                    validateOp(
                        m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()).Single(),
                        "void I2." + CompoundAssignmentOperatorName(op, isChecked: true) + "(System.Int32 x)");
                }
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
        public void CompoundAssignment_00250_VirtualCanBeImplementedImplicitlyInClassAndStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    virtual void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C3 : I1
{
    public void operator " + op + @"(int x) {}
}
";
            bool hasCheckedForm = CompoundAssignmentOperatorHasCheckedForm(op);

            if (hasCheckedForm)
            {
                source += @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    public void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            }

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetTypeMember("C3").GetMembers().OfType<MethodSymbol>().
                    Where(m => m.Name == CompoundAssignmentOperatorName(op, isChecked: false)).Single());
                if (hasCheckedForm)
                {
                    validateOp(m.GlobalNamespace.GetTypeMember("C4").GetMembers().OfType<MethodSymbol>().
                        Where(m => m.Name == CompoundAssignmentOperatorName(op, isChecked: true)).Single());
                }
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
        public void CompoundAssignment_00260_VirtualMustHaveBody([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual void operator" + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,33): error CS0501: 'C1.operator +=(int)' must declare a body because it is not marked abstract, extern, or partial
                //     public virtual void operator+=(int x);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 33)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00261_VirtualMustHaveBody_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator" + op + @"(int x) {}
    public virtual void operator checked" + op + @"(int x); 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,41): error CS0501: 'C1.operator checked +=(int)' must declare a body because it is not marked abstract, extern, or partial
                //     public virtual void operator checked+=(int x); 
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(4, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00270_VirtualNotAllowedInSealedClass([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
sealed class C1
{
    public virtual void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,33): error CS0549: 'C1.operator +=(int x)' is a new virtual member in sealed type 'C1'
                //     public virtual void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, op).WithArguments("C1.operator " + op + @"(int)", "C1").WithLocation(4, 33)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00271_VirtualNotAllowedInSealedClass_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
sealed class C2
{
#line 9
    public virtual void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (9,42): error CS0549: 'C2.operator checked +=(int x)' is a new virtual member in sealed type 'C2'
                //     public virtual void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_NewVirtualInSealed, op).WithArguments("C2.operator checked " + op + @"(int)", "C2").WithLocation(9, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00290_VirtualNotAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct C1
{
    public virtual void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,33): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(4, 33)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00291_VirtualNotAllowedInStruct_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct C2
{
#line 9
    public virtual void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (9,42): error CS0106: The modifier 'virtual' is not valid for this item
                //     public virtual void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(9, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00300_VirtualNotAllowedOnExplicitImplementation([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3 : I1
{
#line 15
    virtual void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,30): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(15, 30)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00301_VirtualNotAllowedOnExplicitImplementation_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
#line 20
    virtual void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (20,38): error CS0106: The modifier 'virtual' is not valid for this item
                //     virtual void I2.operator checked +(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("virtual").WithLocation(20, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00320_VirtualAbstractNotAllowed([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual abstract void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (3,42): error CS0503: The abstract method 'C1.operator +=(int)' cannot be marked virtual
                //     public virtual abstract void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, op).WithArguments("method", "C1.operator " + op + @"(int)").WithLocation(3, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00321_VirtualAbstractNotAllowed_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public void operator" + op + @"(int x) {}
#line 4
    public virtual abstract void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,50): error CS0503: The abstract method 'C1.operator checked +=(int)' cannot be marked virtual
                //     public virtual abstract void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_AbstractNotVirtual, op).WithArguments("method", "C1.operator checked " + op + @"(int)").WithLocation(4, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00330_OverrideAllowedInClass([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @" 
}

class C2 : C1
{
    public override void operator" + op + @"(int x) {}
    public override void operator checked" + op + @"(int x) {} 
}

class C3 : C2
{
    public override void operator" + op + @"(int x) {}
    public override void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate1, sourceSymbolValidator: validate1).VerifyDiagnostics();

            var source2 = @"
abstract class C1
{
    public virtual void operator" + op + @"(int x) {}
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @" 
}

class C2 : C1
{
    public override void operator checked" + op + @"(int x) {} 
}
";
            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute]);

            CompileAndVerify(comp2, symbolValidator: validate2, sourceSymbolValidator: validate2).VerifyDiagnostics();

            void validate1(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: true)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)));
            }

            void validate2(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00331_OverrideAllowedInClass([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
}

class C2 : C1
{
    public override void operator" + op + @"(int x) {}
}

class C3 : C2
{
    public override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate1, sourceSymbolValidator: validate1).VerifyDiagnostics();

            void validate1(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00340_AbstractOverrideAllowedInClass([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @" 
}

abstract class C2 : C1
{
    public abstract override void operator" + op + @"(int x);
    public abstract override void operator checked" + op + @"(int x); 
}

class C3 : C2
{
    public override void operator" + op + @"(int x) {}
    public override void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: true)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)));
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

            comp = CreateCompilation(["class C4 : C2 {}", source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (1,7): error CS0534: 'C4' does not implement inherited abstract member 'C2.operator checked +=(int)'
                // class C4 : C2 {}
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C4").WithArguments("C4", "C2.operator checked " + op + @"(int)").WithLocation(1, 7),
                // (1,7): error CS0534: 'C4' does not implement inherited abstract member 'C2.operator +=(int)'
                // class C4 : C2 {}
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C4").WithArguments("C4", "C2.operator " + op + @"(int)").WithLocation(1, 7)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00341_AbstractOverrideAllowedInClass([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
}

abstract class C2 : C1
{
    public abstract override void operator" + op + @"(int x);
}

class C3 : C2
{
    public override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));

                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
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

            comp = CreateCompilation(["class C4 : C2 {}", source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (1,7): error CS0534: 'C4' does not implement inherited abstract member 'C2.operator +=(int)'
                // class C4 : C2 {}
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C4").WithArguments("C4", "C2.operator " + op + @"(int)").WithLocation(1, 7)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00350_OverrideAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct S1
{
    public override void operator" + op + @"(int x) {}
    public override void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);

            validateOp(comp.GetMember<MethodSymbol>("S1." + CompoundAssignmentOperatorName(op, isChecked: false)));
            validateOp(comp.GetMember<MethodSymbol>("S1." + CompoundAssignmentOperatorName(op, isChecked: true)));

            comp.VerifyDiagnostics(
                // (4,34): error CS0115: 'S1.operator +=(int)': no suitable method found to override
                //     public override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator " + op + @"(int)").WithLocation(4, 34),
                // (5,42): error CS0115: 'S1.operator checked +=(int)': no suitable method found to override
                //     public override void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator checked " + op + @"(int)").WithLocation(5, 42)
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
        public void CompoundAssignment_00351_OverrideAllowedInStruct([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct S1
{
    public override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);

            validateOp(comp.GetMember<MethodSymbol>("S1." + CompoundAssignmentOperatorName(op, isChecked: false)));
            validateOp(comp.GetMember<MethodSymbol>("S1." + CompoundAssignmentOperatorName(op, isChecked: true)));

            comp.VerifyDiagnostics(
                // (4,34): error CS0115: 'S1.operator +=(int)': no suitable method found to override
                //     public override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator " + op + @"(int)").WithLocation(4, 34)
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
        public void CompoundAssignment_00370_OverrideNotAllowedInInterface([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    public override void operator" + op + @"(int x) {}
}

interface I2
{
    public override void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,34): error CS0106: The modifier 'override' is not valid for this item
                //     public override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(4, 34),
                // (9,43): error CS0106: The modifier 'override' is not valid for this item
                //     public override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(9, 43)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00371_OverrideNotAllowedInInterface([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    public override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,34): error CS0106: The modifier 'override' is not valid for this item
                //     public override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(4, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00380_OverrideNotAllowedOnExplicitImplementation([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C3 : I1
{
    override void I1.operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    override void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,31): error CS0106: The modifier 'override' is not valid for this item
                //     override void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 31),
                // (20,39): error CS0106: The modifier 'override' is not valid for this item
                //     override void I2.operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(20, 39)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00381_OverrideNotAllowedOnExplicitImplementation([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3 : I1
{
#line 15
    override void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,31): error CS0106: The modifier 'override' is not valid for this item
                //     override void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("override").WithLocation(15, 31)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00400_VirtualOverrideNotAllowed([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"(int x) {}
    public virtual void operator checked" + op + @"(int x) {} 
}

class C2 : C1
{
    public virtual override void operator" + op + @"(int x) {}
    public virtual override void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (10,42): error CS0113: A member 'C2.operator +=(int)' marked as override cannot be marked as new or virtual
                //     public virtual override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C2.operator " + op + @"(int)").WithLocation(10, 42),
                // (11,50): error CS0113: A member 'C2.operator checked +=(int)' marked as override cannot be marked as new or virtual
                //     public virtual override void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C2.operator checked " + op + @"(int)").WithLocation(11, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00401_VirtualOverrideNotAllowed([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"(int x) {}
}

class C2 : C1
{
#line 10
    public virtual override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (10,42): error CS0113: A member 'C2.operator +=(int)' marked as override cannot be marked as new or virtual
                //     public virtual override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C2.operator " + op + @"(int)").WithLocation(10, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00410_SealedAllowedInInterface([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface C1
{
    sealed void operator" + op + @"(int x) {}
    sealed void operator checked" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
    void C1.operator" + op + @"(int x) {}
    void C1.operator checked" + op + @"(int x) {} 
}
";
            comp = CreateCompilation([source2, source], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,21): error CS0539: 'C3.operator +=(int)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void C1.operator++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C3.operator " + op + @"(int)").WithLocation(4, 21),
                // (5,29): error CS0539: 'C3.operator checked +=(int)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void C1.operator checked++() {} 
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C3.operator checked " + op + @"(int)").WithLocation(5, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00411_SealedAllowedInInterface([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface C1
{
    sealed void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
    void C1.operator" + op + @"(int x) {}
}
";
            comp = CreateCompilation([source2, source], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (4,21): error CS0539: 'C3.operator +=(int)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void C1.operator++() {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, op).WithArguments("C3.operator " + op + @"(int)").WithLocation(4, 21)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00420_SealedOverrideAllowedInClass([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @" 
}

abstract class C2 : C1
{
    public sealed override void operator" + op + @"(int x) {}
    public sealed override void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
    public override void operator" + op + @"(int x) {}
    public override void operator checked" + op + @"(int x) {} 
}
";
            comp = CreateCompilation([source2, source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,34): error CS0239: 'C3.operator +=(int)': cannot override inherited member 'C2.operator +=(int)' because it is sealed
                //     public override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, op).WithArguments("C3.operator " + op + @"(int)", "C2.operator " + op + @"(int)").WithLocation(4, 34),
                // (5,42): error CS0239: 'C3.operator checked +=(int)': cannot override inherited member 'C2.operator checked +=(int)' because it is sealed
                //     public override void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, op).WithArguments("C3.operator checked " + op + @"(int)", "C2.operator checked " + op + @"(int)").WithLocation(5, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00421_SealedOverrideAllowedInClass([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
}

abstract class C2 : C1
{
    public sealed override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            void validate(ModuleSymbol m)
            {
                validateOp(
                    m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)),
                    m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
    public override void operator" + op + @"(int x) {}
}
";
            comp = CreateCompilation([source2, source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,34): error CS0239: 'C3.operator +=(int)': cannot override inherited member 'C2.operator +=(int)' because it is sealed
                //     public override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, op).WithArguments("C3.operator " + op + @"(int)", "C2.operator " + op + @"(int)").WithLocation(4, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00430_SealedNotAllowedInClass([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
class C1
{
    public sealed void operator" + op + @"(int x) {}
}

class C2
{
    public sealed void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,32): error CS0238: 'C1.operator +=(int)' cannot be sealed because it is not an override
                //     public sealed void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 32),
                // (9,41): error CS0238: 'C2.operator checked +=(int x)' cannot be sealed because it is not an override
                //     public sealed void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C2.operator checked " + op + @"(int)").WithLocation(9, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00431_SealedNotAllowedInClass([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
class C1
{
    public sealed void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,32): error CS0238: 'C1.operator +=(int)' cannot be sealed because it is not an override
                //     public sealed void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 32)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00440_SealedNotAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct C1
{
    public sealed void operator" + op + @"(int x) {}
}

struct C2
{
    public sealed void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,32): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 32),
                // (9,41): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(9, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00441_SealedNotAllowedInStruct([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct C1
{
    public sealed void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,32): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 32)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00450_SealedOverrideNotAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct C1
{
    public sealed override void operator" + op + @"(int x) {}
}

struct C2
{
    public sealed override void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,41): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 41),
                // (4,41): error CS0115: 'C1.operator +=(int)': no suitable method found to override
                //     public sealed override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 41),
                // (9,50): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(9, 50),
                // (9,50): error CS0115: 'C2.operator checked +=(int)': no suitable method found to override
                //     public sealed override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C2.operator checked " + op + @"(int)").WithLocation(9, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00451_SealedOverrideNotAllowedInStruct([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct C1
{
    public sealed override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,41): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 41),
                // (4,41): error CS0115: 'C1.operator +=(int)': no suitable method found to override
                //     public sealed override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 41)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00460_SealedAbstractOverrideNotAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct C1
{
    public sealed abstract override void operator" + op + @"(int x) {}
}

struct C2
{
    public sealed abstract override void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,50): error CS0106: The modifier 'abstract' is not valid for this item
                //     public sealed abstract override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 50),
                // (4,50): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed abstract override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 50),
                // (4,50): error CS0115: 'C1.operator +=(int)': no suitable method found to override
                //     public sealed abstract override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 50),
                // (9,59): error CS0106: The modifier 'abstract' is not valid for this item
                //     public sealed abstract override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(9, 59),
                // (9,59): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed abstract override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(9, 59),
                // (9,59): error CS0115: 'C2.operator checked +=(int)': no suitable method found to override
                //     public sealed abstract override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C2.operator checked " + op + @"(int)").WithLocation(9, 59)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00461_SealedAbstractOverrideNotAllowedInStruct([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct C1
{
    public sealed abstract override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,50): error CS0106: The modifier 'abstract' is not valid for this item
                //     public sealed abstract override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("abstract").WithLocation(4, 50),
                // (4,50): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed abstract override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 50),
                // (4,50): error CS0115: 'C1.operator +=(int)': no suitable method found to override
                //     public sealed abstract override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00470_SealedNotAllowedOnExplicitImplementation([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C3 : I1
{
    sealed void I1.operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    sealed void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,29): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void I1.operator +(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 29),
                // (20,37): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void I2.operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00471_SealedNotAllowedOnExplicitImplementation([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3 : I1
{
#line 15
    sealed void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,29): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed void I1.operator +(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00490_SealedAbstractNotAllowed([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public sealed abstract void operator" + op + @"(int x);
    public sealed abstract void operator checked" + op + @"(int x); 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (3,41): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public sealed abstract void operator-=(int x);
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(3, 41),
                    // (4,49): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public sealed abstract void operator checked-=(int x); 
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 49)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,41): error CS0238: 'C1.operator +=(int)' cannot be sealed because it is not an override
                    //     public sealed abstract void operator+=(int x);
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 41),
                    // (4,49): error CS0238: 'C1.operator checked +=(int)' cannot be sealed because it is not an override
                    //     public sealed abstract void operator checked+=(int x); 
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(4, 49)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00491_SealedAbstractNotAllowed([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public sealed abstract void operator" + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (3,41): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public sealed abstract void operator-=(int x);
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(3, 41)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,41): error CS0238: 'C1.operator +=(int)' cannot be sealed because it is not an override
                    //     public sealed abstract void operator+=(int x);
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 41)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00500_SealedAbstractOverrideNotAllowedInClass([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator checked" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @" 
}

abstract class C2 : C1
{
    public sealed abstract override void operator" + op + @"(int x);
    public sealed abstract override void operator checked" + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (10,50): error CS0502: 'C2.operator +=(int)' cannot be both abstract and sealed
                //     public sealed abstract override void operator+=(int x);
                Diagnostic(ErrorCode.ERR_AbstractAndSealed, op).WithArguments("C2.operator " + op + @"(int)").WithLocation(10, 50),
                // (11,58): error CS0502: 'C2.operator checked +=(int)' cannot be both abstract and sealed
                //     public sealed abstract override void operator checked+=(int x);
                Diagnostic(ErrorCode.ERR_AbstractAndSealed, op).WithArguments("C2.operator checked " + op + @"(int)").WithLocation(11, 58)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00501_SealedAbstractOverrideNotAllowedInClass([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool abstractInBase)
        {
            var source = @"
abstract class C1
{
    public " + (abstractInBase ? "abstract" : "virtual") + @" void operator" + op + @"(int x) " + (abstractInBase ? ";" : "{}") + @"
}

abstract class C2 : C1
{
    public sealed abstract override void operator" + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (9,50): error CS0502: 'C2.operator +=(int)' cannot be both abstract and sealed
                //     public sealed abstract override void operator+=(int x);
                Diagnostic(ErrorCode.ERR_AbstractAndSealed, op).WithArguments("C2.operator " + op + @"(int)").WithLocation(9, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00510_SealedAbstractNotAllowedOnExplicitImplementation([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

interface I3 : I1
{
    sealed abstract void I1.operator " + op + @"(int x);
}

interface I4 : I2
{
    sealed abstract void I2.operator checked " + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,38): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed abstract void I1.operator +=(int x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 38),
                // (20,46): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed abstract void I2.operator checked +=(int x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(20, 46)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00511_SealedAbstractNotAllowedOnExplicitImplementation([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I3 : I1
{
#line 15
    sealed abstract void I1.operator " + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,38): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed abstract void I1.operator +=(int x);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(15, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00530_SealedVirtualNotAllowed([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual sealed void operator" + op + @"(int x) {}
    public virtual sealed void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (3,40): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public virtual sealed void operator+(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(3, 40),
                    // (4,48): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public virtual sealed void operator checked+(int x) {} 
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(4, 48)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,40): error CS0238: 'C1.operator +(int)' cannot be sealed because it is not an override
                    //     public virtual sealed void operator+=(int x) {}
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 40),
                    // (4,48): error CS0238: 'C1.operator checked +=(int)' cannot be sealed because it is not an override
                    //     public virtual sealed void operator checked+(int x) {} 
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(4, 48)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00531_SealedVirtualNotAllowed([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public virtual sealed void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            if (typeKeyword == "interface")
            {
                comp.VerifyDiagnostics(
                    // (3,40): error CS0106: The modifier 'sealed' is not valid for this item
                    //     public virtual sealed void operator+(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("sealed").WithLocation(3, 40)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (3,40): error CS0238: 'C1.operator +(int)' cannot be sealed because it is not an override
                    //     public virtual sealed void operator+=(int x) {}
                    Diagnostic(ErrorCode.ERR_SealedNonOverride, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 40)
                    );
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00540_NewAllowedInInterface_WRN_NewRequired([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("abstract", "virtual", "sealed")] string baseModifier)
        {
            var source = @"
interface C1
{
    public " + baseModifier + @" void operator" + op + @"(int x) " + (baseModifier == "abstract" ? ";" : "{}") + @"
    public " + baseModifier + @" void operator checked" + op + @"(int x) " + (baseModifier == "abstract" ? ";" : "{}") + @" 
}

interface C2 : C1
{
    public void operator" + op + @"(int x) {}
    public void operator checked" + op + @"(int x) {} 
}

interface C3 : C1
{
    public new void operator" + op + @"(int x) {}
    public new void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (10,25): warning CS0108: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. Use the new keyword if hiding was intended.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (11,33): warning CS0108: 'C2.operator checked +=(int)' hides inherited member 'C1.operator checked +=(int)'. Use the new keyword if hiding was intended.
                //     public void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(int)").WithLocation(11, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00541_NewAllowedInInterface_WRN_NewRequired([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract", "virtual", "sealed")] string baseModifier)
        {
            var source = @"
interface C1
{
    public " + baseModifier + @" void operator" + op + @"(int x) " + (baseModifier == "abstract" ? ";" : "{}") + @"
}

interface C2 : C1
{
#line 10
    public void operator" + op + @"(int x) {}
}

interface C3 : C1
{
    public new void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (10,25): warning CS0108: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. Use the new keyword if hiding was intended.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00550_NewAllowedInClass_WRN_NewRequired([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
class C1
{
    public void operator" + op + @"(int x) {}
    public void operator checked" + op + @"(int x) {}
}

class C2 : C1
{
    public void operator" + op + @"(int x) {}
    public void operator checked" + op + @"(int x) {} 
}

class C3 : C1
{
    public new void operator" + op + @"(int x) {}
    public new void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics(
                // (10,25): warning CS0108: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. Use the new keyword if hiding was intended.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (11,33): warning CS0108: 'C2.operator checked +=(int)' hides inherited member 'C1.operator checked +(int)'. Use the new keyword if hiding was intended.
                //     public void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(int)").WithLocation(11, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00551_NewAllowedInClass_WRN_NewRequired([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
class C1
{
    public void operator" + op + @"(int x) {}
}

class C2 : C1
{
#line 10
    public void operator" + op + @"(int x) {}
}

class C3 : C1
{
    public new void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics(
                // (10,25): warning CS0108: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. Use the new keyword if hiding was intended.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewRequired, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00560_NewAllowedInClass_WRN_NewOrOverrideExpected([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"(int x) {}
    public virtual void operator checked" + op + @"(int x) {}
}

class C2 : C1
{
    public void operator" + op + @"(int x) {}
    public void operator checked" + op + @"(int x) {} 
}

class C3 : C1
{
    public new void operator" + op + @"(int x) {}
    public new void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics(
                // (10,25): warning CS0114: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (11,33): warning CS0114: 'C2.operator checked +=(int)' hides inherited member 'C1.operator checked +=(int)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(int)").WithLocation(11, 33)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: true)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00561_NewAllowedInClass_WRN_NewOrOverrideExpected([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"(int x) {}
}

class C2 : C1
{
#line 10
    public void operator" + op + @"(int x) {}
}

class C3 : C1
{
    public new void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics(
                // (10,25): warning CS0114: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C2." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C3." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00570_NewAllowedInClass_ERR_HidingAbstractMethod([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
abstract class C1
{
    public abstract void operator" + op + @"(int x);
    public abstract void operator checked" + op + @"(int x);
}

abstract class C2 : C1
{
    public void operator" + op + @"(int x) {}
    public void operator checked" + op + @"(int x) {} 
}

abstract class C3 : C1
{
    public new void operator" + op + @"(int x) {}
    public new void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (10,25): error CS0533: 'C2.operator +=(int)' hides inherited abstract member 'C1.operator +=(int)'
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (10,25): warning CS0114: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (11,33): error CS0533: 'C2.operator checked +=(int)' hides inherited abstract member 'C1.operator checked +=(int)'
                //     public void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C2.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(int)").WithLocation(11, 33),
                // (11,33): warning CS0114: 'C2.operator checked +=(int)' hides inherited member 'C1.operator checked +=(int)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(int)").WithLocation(11, 33),
                // (16,29): error CS0533: 'C3.operator +=(int)' hides inherited abstract member 'C1.operator +=(int)'
                //     public new void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C3.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(16, 29),
                // (17,37): error CS0533: 'C3.operator checked +=(int)' hides inherited abstract member 'C1.operator checked +=(int)'
                //     public new void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C3.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(int)").WithLocation(17, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00571_NewAllowedInClass_ERR_HidingAbstractMethod([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
abstract class C1
{
    public abstract void operator" + op + @"(int x);
}

abstract class C2 : C1
{
#line 10
    public void operator" + op + @"(int x) {}
}

abstract class C3 : C1
{
#line 16
    public new void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (10,25): error CS0533: 'C2.operator +=(int)' hides inherited abstract member 'C1.operator +=(int)'
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (10,25): warning CS0114: 'C2.operator +=(int)' hides inherited member 'C1.operator +=(int)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, op).WithArguments("C2.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(10, 25),
                // (16,29): error CS0533: 'C3.operator +=(int)' hides inherited abstract member 'C1.operator +=(int)'
                //     public new void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, op).WithArguments("C3.operator " + op + @"(int)", "C1.operator " + op + @"(int)").WithLocation(16, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00580_NewAllowed_WRN_NewNotRequired([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new void operator" + op + @"(int x) {}
    public new void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,29): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 29),
                // (4,37): warning CS0109: The member 'C1.operator checked +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(4, 37)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00581_NewAllowed_WRN_NewNotRequired([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,29): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 29)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00590_NewAbstractAllowedInClassAndInterface([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new abstract void operator" + op + @"(int x);
    public new abstract void operator checked" + op + @"(int x); 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,38): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new abstract void operator+=(int x);
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 38),
                // (4,46): warning CS0109: The member 'C1.operator checked +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new abstract void operator checked+=(int x); 
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(4, 46)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_005910_NewAbstractAllowedInClassAndInterface([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("abstract class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new abstract void operator" + op + @"(int x);
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,38): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new abstract void operator+=(int x);
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 38)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00600_NewVirtualAllowedInClassAndInterface([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new virtual void operator" + op + @"(int x) {}
    public new virtual void operator checked" + op + @"(int x) {} 
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,37): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new virtual void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 37),
                // (4,45): warning CS0109: The member 'C1.operator checked +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new virtual void operator checked+=(int x) {} 
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(4, 45)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00601_NewVirtualAllowedInClassAndInterface([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "interface")] string typeKeyword)
        {
            var source =
typeKeyword + @" C1
{
    public new virtual void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (3,37): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     public new virtual void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(3, 37)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00610_NewSealedAllowedInInterface([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface C1
{
    sealed new void operator" + op + @"(int x) {}
    sealed new void operator checked" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (4,29): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     sealed new void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 29),
                // (5,37): warning CS0109: The member 'C1.operator checked +=(int)' does not hide an accessible member. The new keyword is not required.
                //     sealed new void operator checked+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator checked " + op + @"(int)").WithLocation(5, 37)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: true)));
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
        public void CompoundAssignment_00611_NewSealedAllowedInInterface([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface C1
{
    sealed new void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (4,29): warning CS0109: The member 'C1.operator +=(int)' does not hide an accessible member. The new keyword is not required.
                //     sealed new void operator+=(int x) {}
                Diagnostic(ErrorCode.WRN_NewNotRequired, op).WithArguments("C1.operator " + op + @"(int)").WithLocation(4, 29)
                );

            void validate(ModuleSymbol m)
            {
                validateOp(m.GlobalNamespace.GetMember<MethodSymbol>("C1." + CompoundAssignmentOperatorName(op, isChecked: false)));
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
        public void CompoundAssignment_00620_NewOverrideNotAllowedInClass([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"(int x) {}
}

class C2
{
    public virtual void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(int x) {}
}

class C3 : C1
{
    public new override void operator" + op + @"(int x) {}
}

class C4 : C2
{
    public new override void operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (15,38): error CS0113: A member 'C3.operator +=(int)' marked as override cannot be marked as new or virtual
                //     public new override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C3.operator " + op + @"(int)").WithLocation(15, 38),
                // (20,47): error CS0113: A member 'C4.operator checked +=(int)' marked as override cannot be marked as new or virtual
                //     public new override void operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C4.operator checked " + op + @"(int)").WithLocation(20, 47)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00621_NewOverrideNotAllowedInClass([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
class C1
{
    public virtual void operator" + op + @"(int x) {}
}

class C3 : C1
{
#line 15
    public new override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (15,38): error CS0113: A member 'C3.operator +=(int)' marked as override cannot be marked as new or virtual
                //     public new override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("C3.operator " + op + @"(int)").WithLocation(15, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00630_NewOverrideNotAllowedInStruct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct S1
{
    public new override void operator" + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (4,38): error CS0113: A member 'S1.operator +=(int)' marked as override cannot be marked as new or virtual
                //     public new override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotNew, op).WithArguments("S1.operator " + op + @"(int)").WithLocation(4, 38),
                // (4,38): error CS0115: 'S1.operator +=(int)': no suitable method found to override
                //     public new override void operator+=(int x) {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, op).WithArguments("S1.operator " + op + @"(int)").WithLocation(4, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00650_NewNotAllowedOnExplicitImplementation([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C3 : I1
{
    new void I1.operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4 : I2
{
    new void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,26): error CS0106: The modifier 'new' is not valid for this item
                //     new void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(15, 26),
                // (20,34): error CS0106: The modifier 'new' is not valid for this item
                //     new void I2.operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(20, 34)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00651_NewNotAllowedOnExplicitImplementation([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct", "interface")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3 : I1
{
#line 15
    new void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (15,26): error CS0106: The modifier 'new' is not valid for this item
                //     new void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("new").WithLocation(15, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00670_ExplicitImplementationStaticVsInstanceMismatch([CombinatorialValues("+=", "-=", "*=", "/=")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

" + typeKeyword + @" C3
    : I1
{
    static void I1.operator " + op + @"(int x) {}
}

" + typeKeyword + @" C4
    : I2
{
    static void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (16,29): error CS0106: The modifier 'static' is not valid for this item
                //     static void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(16, 29),
                // (22,37): error CS0106: The modifier 'static' is not valid for this item
                //     static void I2.operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(22, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00671_ExplicitImplementationStaticVsInstanceMismatch([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, [CombinatorialValues("class", "struct")] string typeKeyword)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

" + typeKeyword + @" C3
    : I1
{
#line 16
    static void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (16,29): error CS0106: The modifier 'static' is not valid for this item
                //     static void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(16, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00680_ExplicitImplementationStaticVsInstanceMismatch([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2
{
    void operator checked " + op + @"(int x);
    sealed void operator " + op + @"(int x) {}
}

interface C3
    : I1
{
    static void I1.operator " + op + @"(int x) {}
}

interface C4
    : I2
{
    static void I2.operator checked " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (16,29): error CS0106: The modifier 'static' is not valid for this item
                //     static void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(16, 29),
                // (22,37): error CS0106: The modifier 'static' is not valid for this item
                //     static void I2.operator checked +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(22, 37)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00681_ExplicitImplementationStaticVsInstanceMismatch([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface C3
    : I1
{
#line 16
    static void I1.operator " + op + @"(int x) {}
}
";
            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp.VerifyDiagnostics(
                // (16,29): error CS0106: The modifier 'static' is not valid for this item
                //     static void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(16, 29)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00690_Consumption_OnNonVariable([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public int _F;

    public void operator" + op + @"(int x) => throw null; 
    public void operator checked" + op + @"(int x) => throw null; 

    public static C1 operator" + op[..^1] + @"(C1 x, int y)
    {
        System.Console.Write(""[operator]"");
        return new C1() { _F = x._F + y };
    } 
    public static C1 operator checked" + op[..^1] + @"(C1 x, int y)
    {
        System.Console.Write(""[operator checked]"");
        checked
        {
            return new C1() { _F = x._F + y };
        }
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1 P {get; set;} = new C1();

    static void Main()
    {
        C1 x;

        P" + op + @" 1;
        System.Console.WriteLine(P._F);
        x = P" + op + @" 1;
        System.Console.WriteLine(P._F);

        checked
        {
            P" + op + @" 1;
            System.Console.WriteLine(P._F);
            x = P" + op + @" 1;
            System.Console.WriteLine(P._F);
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[operator]1
[operator]2
[operator checked]3
[operator checked]4
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00691_Consumption_OnNonVariable([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public int _F;

    public void operator" + op + @"(int x) => throw null; 

    public static C1 operator" + op[..^1] + @"(C1 x, int y)
    {
        System.Console.Write(""[operator]"");
        return new C1() { _F = x._F + y };
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1 P {get; set;} = new C1();

    static void Main()
    {
        C1 x;

        P" + op + @" 1;
        System.Console.WriteLine(P._F);
        x = P" + op + @" 1;
        System.Console.WriteLine(P._F);

        checked
        {
            P" + op + @" 1;
            System.Console.WriteLine(P._F);
            x = P" + op + @" 1;
            System.Console.WriteLine(P._F);
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp2, expectedOutput: @"
[operator]1
[operator]2
[operator]3
[operator]4
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00700_Consumption_NotUsed_Class([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
    public void operator checked" + op + @"(long x);
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
    public void operator checked" + op + @"(long x)
    {
        System.Console.Write(""[operator checked]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @" Get1();
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" Get1();
        }
    } 

    static void Test3<T>(T[] x) where T : class, I1
    {
        GetA(x)[Get0()]" + op + @" Get1();
    } 

    static void Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" Get1();
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @" Get1();
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" Get1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int Get1()
    {
        System.Console.Write(""[Get1]"");
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][Get1][operator]1
[GetA][Get0][Get1][operator checked]2
[GetA][Get0][Get1][operator]3
[GetA][Get0][Get1][operator checked]4
[GetA][Get0][Get1][operator]5
[GetA][Get0][Get1][operator checked]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  conv.i8
  IL_0012:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  conv.i8
  IL_001b:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0020:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  ldloca.s   V_0
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_002a
  IL_0022:  ldobj      ""T""
  IL_0027:  stloc.0
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int Program.Get1()""
  IL_002f:  conv.i8
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var typeInfo = model.GetTypeInfo(opNode.Left);
            Assert.Equal("C1", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("C1", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(opNode.Left).IsIdentity);

            typeInfo = model.GetTypeInfo(opNode.Right);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(opNode.Right).IsNumeric);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" Get1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'Get1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.Get1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get1()')
      Instance Receiver:
        null
      Arguments(0)
");

            methodName = CompoundAssignmentOperatorName(op, isChecked: true);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  call       ""int Program.Get1()""
  IL_0011:  conv.i8
  IL_0012:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  call       ""int Program.Get1()""
  IL_001a:  conv.i8
  IL_001b:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0020:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  ldloca.s   V_0
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_002a
  IL_0022:  ldobj      ""T""
  IL_0027:  stloc.0
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int Program.Get1()""
  IL_002f:  conv.i8
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @", Checked) (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" Get1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'Get1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.Get1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "C1", "int").WithLocation(23, 9),
                // (30,13): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "C1", "int").WithLocation(30, 13),
                // (36,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(36, 9),
                // (43,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(43, 13),
                // (49,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(49, 9),
                // (56,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(56, 13)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        private static Operations.BinaryOperatorKind CompoundAssignmentOperatorToBinaryOperatorKind(string op)
        {
            switch (op)
            {
                case "*=": return Operations.BinaryOperatorKind.Multiply;
                case "/=": return Operations.BinaryOperatorKind.Divide;
                case "%=": return Operations.BinaryOperatorKind.Remainder;
                case "+=": return Operations.BinaryOperatorKind.Add;
                case "-=": return Operations.BinaryOperatorKind.Subtract;
                case ">>=": return Operations.BinaryOperatorKind.RightShift;
                case ">>>=": return Operations.BinaryOperatorKind.UnsignedRightShift;
                case "<<=": return Operations.BinaryOperatorKind.LeftShift;
                case "&=": return Operations.BinaryOperatorKind.And;
                case "|=": return Operations.BinaryOperatorKind.Or;
                case "^=": return Operations.BinaryOperatorKind.ExclusiveOr;
                default: throw ExceptionUtilities.UnexpectedValue(op);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00701_Consumption_NotUsed_Class([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test3<T>(T[] x) where T : class, I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[Get1]"");
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][Get1][operator]1
[GetA][Get0][Get1][operator]2
[GetA][Get0][Get1][operator]3
[GetA][Get0][Get1][operator]4
[GetA][Get0][Get1][operator]5
[GetA][Get0][Get1][operator]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  call       ""int Program.G1()""
  IL_0011:  conv.i8
  IL_0012:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0020:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  ldloca.s   V_0
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_002a
  IL_0022:  ldobj      ""T""
  IL_0027:  stloc.0
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int Program.G1()""
  IL_002f:  conv.i8
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
      Instance Receiver:
        null
      Arguments(0)
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  call       ""int Program.G1()""
  IL_0011:  conv.i8
  IL_0012:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0020:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  ldloca.s   V_0
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_002a
  IL_0022:  ldobj      ""T""
  IL_0027:  stloc.0
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int Program.G1()""
  IL_002f:  conv.i8
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(23, 9),
                // (30,13): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(30, 13),
                // (36,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(36, 9),
                // (43,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(43, 13),
                // (49,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(49, 9),
                // (56,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(56, 13)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00702_Consumption_NotUsed_Class([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(in long x);
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"(in long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test3<T>(T[] x) where T : class, I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[Get1]"");
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][Get1][operator]1
[GetA][Get0][Get1][operator]2
[GetA][Get0][Get1][operator]3
[GetA][Get0][Get1][operator]4
[GetA][Get0][Get1][operator]5
[GetA][Get0][Get1][operator]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  call       ""int Program.G1()""
  IL_0011:  conv.i8
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  callvirt   ""void C1." + methodName + @"(in long)""
  IL_001a:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_0023:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (T V_0,
            long V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  ldloca.s   V_0
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_002a
  IL_0022:  ldobj      ""T""
  IL_0027:  stloc.0
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int Program.G1()""
  IL_002f:  conv.i8
  IL_0030:  stloc.1
  IL_0031:  ldloca.s   V_1
  IL_0033:  constrained. ""T""
  IL_0039:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_003e:  ret
}
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  call       ""int Program.G1()""
  IL_0011:  conv.i8
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  callvirt   ""void C1." + methodName + @"(in long)""
  IL_001a:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  box        ""T""
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_0023:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (T V_0,
            long V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  ldloca.s   V_0
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.0
  IL_001b:  box        ""T""
  IL_0020:  brtrue.s   IL_002a
  IL_0022:  ldobj      ""T""
  IL_0027:  stloc.0
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""int Program.G1()""
  IL_002f:  conv.i8
  IL_0030:  stloc.1
  IL_0031:  ldloca.s   V_1
  IL_0033:  constrained. ""T""
  IL_0039:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_003e:  ret
}
");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00710_Consumption_NotUsed_Struct([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
    public void operator checked" + op + @"(long x);
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
    public void operator checked" + op + @"(long x)
    {
        System.Console.Write(""[operator checked]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @" Get1();
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" Get1();
        }
    } 

    static void Test3<T>(T[] x) where T : struct, I1
    {
        GetA(x)[Get0()]" + op + @" Get1();
    } 

    static void Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" Get1();
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @" Get1();
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" Get1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int Get1()
    {
        System.Console.Write(""[Get1]"");
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][Get1][operator]1
[GetA][Get0][Get1][operator checked]2
[GetA][Get0][Get1][operator]3
[GetA][Get0][Get1][operator checked]4
[GetA][Get0][Get1][operator]5
[GetA][Get0][Get1][operator checked]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  conv.i8
  IL_0016:  call       ""void C1." + methodName + @"(long)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  call       ""int Program.Get1()""
  IL_0017:  conv.i8
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0023:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" Get1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'Get1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.Get1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get1()')
      Instance Receiver:
        null
      Arguments(0)
");

            methodName = CompoundAssignmentOperatorName(op, isChecked: true);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""int Program.Get1()""
  IL_0015:  conv.i8
  IL_0016:  call       ""void C1." + methodName + @"(long)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  call       ""int Program.Get1()""
  IL_0017:  conv.i8
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0023:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @", Checked) (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" Get1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'Get1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.Get1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "C1", "int").WithLocation(23, 9),
                // (30,13): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "C1", "int").WithLocation(30, 13),
                // (36,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(36, 9),
                // (43,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(43, 13),
                // (49,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(49, 9),
                // (56,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= Get1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" Get1()").WithArguments(op, "T", "int").WithLocation(56, 13)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00711_Consumption_NotUsed_Struct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test3<T>(T[] x) where T : struct, I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[Get1]"");
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][Get1][operator]1
[GetA][Get0][Get1][operator]2
[GetA][Get0][Get1][operator]3
[GetA][Get0][Get1][operator]4
[GetA][Get0][Get1][operator]5
[GetA][Get0][Get1][operator]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""int Program.G1()""
  IL_0015:  conv.i8
  IL_0016:  call       ""void C1." + methodName + @"(long)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  call       ""int Program.G1()""
  IL_0017:  conv.i8
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0023:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
      Instance Receiver:
        null
      Arguments(0)
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""int Program.G1()""
  IL_0015:  conv.i8
  IL_0016:  call       ""void C1." + methodName + @"(long)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  call       ""int Program.G1()""
  IL_0017:  conv.i8
  IL_0018:  constrained. ""T""
  IL_001e:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0023:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("System.Void", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(23, 9),
                // (30,13): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(30, 13),
                // (36,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(36, 9),
                // (43,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(43, 13),
                // (49,9): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(49, 9),
                // (56,13): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(56, 13)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00712_Consumption_NotUsed_Struct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(in long x);
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"(in long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
        Test2(x);
        System.Console.WriteLine(x[0]._F);
        Test3(x);
        System.Console.WriteLine(x[0]._F);
        Test4(x);
        System.Console.WriteLine(x[0]._F);
        Test5(x);
        System.Console.WriteLine(x[0]._F);
        Test6(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test2(C1[] x)
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test3<T>(T[] x) where T : struct, I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static void Test5<T>(T[] x) where T : I1
    {
        GetA(x)[Get0()]" + op + @" G1();
    } 

    static void Test6<T>(T[] x) where T : I1
    {
        checked
        {
            GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[Get1]"");
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][Get1][operator]1
[GetA][Get0][Get1][operator]2
[GetA][Get0][Get1][operator]3
[GetA][Get0][Get1][operator]4
[GetA][Get0][Get1][operator]5
[GetA][Get0][Get1][operator]6
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""int Program.G1()""
  IL_0015:  conv.i8
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""void C1." + methodName + @"(in long)""
  IL_001e:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  call       ""int Program.G1()""
  IL_0017:  conv.i8
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  constrained. ""T""
  IL_0021:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_0026:  ret
}
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  call       ""int Program.G1()""
  IL_0015:  conv.i8
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""void C1." + methodName + @"(in long)""
  IL_001e:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  readonly.
  IL_000d:  ldelema    ""T""
  IL_0012:  call       ""int Program.G1()""
  IL_0017:  conv.i8
  IL_0018:  stloc.0
  IL_0019:  ldloca.s   V_0
  IL_001b:  constrained. ""T""
  IL_0021:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_0026:  ret
}
");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00720_Consumption_Used_Class([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
    public void operator checked" + op + @"(long x);
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
    public void operator checked" + op + @"(long x)
    {
        System.Console.Write(""[operator checked]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1[] x = [null];

    static void Main()
    {
        var val = new C1();
        x[0] = val;
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
    } 

    static C1 Test1(C1[] x)
    {
#line 23
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test3<T>(T[] x) where T : class, I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[G1]"");
        x[0] = null;
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][G1][operator]1True
[GetA][Get0][G1][operator checked]2True
[GetA][Get0][G1][operator]3True
[GetA][Get0][G1][operator checked]4True
[GetA][Get0][G1][operator]5True
[GetA][Get0][G1][operator checked]6True
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  call       ""int Program.G1()""
  IL_0012:  conv.i8
  IL_0013:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0018:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0021:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2,
                long V_3,
                T V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_4
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_4
  IL_0026:  box        ""T""
  IL_002b:  brtrue.s   IL_003d
  IL_002d:  ldloca.s   V_2
  IL_002f:  ldloc.3
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  br.s       IL_0053
  IL_003d:  ldloca.s   V_2
  IL_003f:  ldloc.3
  IL_0040:  constrained. ""T""
  IL_0046:  callvirt   ""void I1." + methodName + @"(long)""
  IL_004b:  ldloc.0
  IL_004c:  ldloc.1
  IL_004d:  ldloc.2
  IL_004e:  stelem     ""T""
  IL_0053:  ldloc.2
  IL_0054:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: C1) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
      Instance Receiver:
        null
      Arguments(0)
");

            methodName = CompoundAssignmentOperatorName(op, isChecked: true);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  call       ""int Program.G1()""
  IL_0012:  conv.i8
  IL_0013:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0018:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0021:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2,
                long V_3,
                T V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_4
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_4
  IL_0026:  box        ""T""
  IL_002b:  brtrue.s   IL_003d
  IL_002d:  ldloca.s   V_2
  IL_002f:  ldloc.3
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  br.s       IL_0053
  IL_003d:  ldloca.s   V_2
  IL_003f:  ldloc.3
  IL_0040:  constrained. ""T""
  IL_0046:  callvirt   ""void I1." + methodName + @"(long)""
  IL_004b:  ldloc.0
  IL_004c:  ldloc.1
  IL_004d:  ldloc.2
  IL_004e:  stelem     ""T""
  IL_0053:  ldloc.2
  IL_0054:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("T", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @", Checked) (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: T) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,16): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(23, 16),
                // (30,20): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(30, 20),
                // (36,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(36, 16),
                // (43,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(43, 20),
                // (49,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(49, 16),
                // (56,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(56, 20)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00721_Consumption_Used_Class([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1[] x = [null];

    static void Main()
    {
        var val = new C1();
        x[0] = val;
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
    } 

    static C1 Test1(C1[] x)
    {
#line 23
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test3<T>(T[] x) where T : class, I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[G1]"");
        x[0] = null;
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][G1][operator]1True
[GetA][Get0][G1][operator]2True
[GetA][Get0][G1][operator]3True
[GetA][Get0][G1][operator]4True
[GetA][Get0][G1][operator]5True
[GetA][Get0][G1][operator]6True
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  call       ""int Program.G1()""
  IL_0012:  conv.i8
  IL_0013:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0018:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0021:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2,
                long V_3,
                T V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_4
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_4
  IL_0026:  box        ""T""
  IL_002b:  brtrue.s   IL_003d
  IL_002d:  ldloca.s   V_2
  IL_002f:  ldloc.3
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  br.s       IL_0053
  IL_003d:  ldloca.s   V_2
  IL_003f:  ldloc.3
  IL_0040:  constrained. ""T""
  IL_0046:  callvirt   ""void I1." + methodName + @"(long)""
  IL_004b:  ldloc.0
  IL_004c:  ldloc.1
  IL_004d:  ldloc.2
  IL_004e:  stelem     ""T""
  IL_0053:  ldloc.2
  IL_0054:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: C1) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
      Instance Receiver:
        null
      Arguments(0)
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  call       ""int Program.G1()""
  IL_0012:  conv.i8
  IL_0013:  callvirt   ""void C1." + methodName + @"(long)""
  IL_0018:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0021:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2,
                long V_3,
                T V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_4
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_4
  IL_0026:  box        ""T""
  IL_002b:  brtrue.s   IL_003d
  IL_002d:  ldloca.s   V_2
  IL_002f:  ldloc.3
  IL_0030:  constrained. ""T""
  IL_0036:  callvirt   ""void I1." + methodName + @"(long)""
  IL_003b:  br.s       IL_0053
  IL_003d:  ldloca.s   V_2
  IL_003f:  ldloc.3
  IL_0040:  constrained. ""T""
  IL_0046:  callvirt   ""void I1." + methodName + @"(long)""
  IL_004b:  ldloc.0
  IL_004c:  ldloc.1
  IL_004d:  ldloc.2
  IL_004e:  stelem     ""T""
  IL_0053:  ldloc.2
  IL_0054:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("T", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: T) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,16): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(23, 16),
                // (30,20): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(30, 20),
                // (36,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(36, 16),
                // (43,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(43, 20),
                // (49,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(49, 16),
                // (56,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(56, 20)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00722_Consumption_Used_Class([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(in long x);
}

public class C1 : I1
{
    public int _F;
    public void operator" + op + @"(in long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1[] x = [null];

    static void Main()
    {
        var val = new C1();
        x[0] = val;
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
        x[0] = val;
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine((object)val == y && x[0] is null);
    } 

    static C1 Test1(C1[] x)
    {
#line 23
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test3<T>(T[] x) where T : class, I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test4<T>(T[] x) where T : class, I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[G1]"");
        x[0] = null;
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][G1][operator]1True
[GetA][Get0][G1][operator]2True
[GetA][Get0][G1][operator]3True
[GetA][Get0][G1][operator]4True
[GetA][Get0][G1][operator]5True
[GetA][Get0][G1][operator]6True
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  call       ""int Program.G1()""
  IL_0012:  conv.i8
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  callvirt   ""void C1." + methodName + @"(in long)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  stloc.0
  IL_001d:  ldloca.s   V_0
  IL_001f:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_0024:  ret
}
");

            verifier.VerifyIL("Program.Test5<T>(T[])",
@"
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2,
                long V_3,
                T V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_4
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_4
  IL_0026:  box        ""T""
  IL_002b:  brtrue.s   IL_003e
  IL_002d:  ldloca.s   V_2
  IL_002f:  ldloca.s   V_3
  IL_0031:  constrained. ""T""
  IL_0037:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_003c:  br.s       IL_0055
  IL_003e:  ldloca.s   V_2
  IL_0040:  ldloca.s   V_3
  IL_0042:  constrained. ""T""
  IL_0048:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_004d:  ldloc.0
  IL_004e:  ldloc.1
  IL_004f:  ldloc.2
  IL_0050:  stelem     ""T""
  IL_0055:  ldloc.2
  IL_0056:  ret
}
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  3
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem.ref
  IL_000c:  dup
  IL_000d:  call       ""int Program.G1()""
  IL_0012:  conv.i8
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  callvirt   ""void C1." + methodName + @"(in long)""
  IL_001b:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (long V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelem     ""T""
  IL_0010:  dup
  IL_0011:  box        ""T""
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  stloc.0
  IL_001d:  ldloca.s   V_0
  IL_001f:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_0024:  ret
}
");

            verifier.VerifyIL("Program.Test6<T>(T[])",
@"
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2,
                long V_3,
                T V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  stloc.0
  IL_0007:  call       ""int Program.Get0()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldloc.1
  IL_000f:  ldelem     ""T""
  IL_0014:  stloc.2
  IL_0015:  call       ""int Program.G1()""
  IL_001a:  conv.i8
  IL_001b:  stloc.3
  IL_001c:  ldloca.s   V_4
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_4
  IL_0026:  box        ""T""
  IL_002b:  brtrue.s   IL_003e
  IL_002d:  ldloca.s   V_2
  IL_002f:  ldloca.s   V_3
  IL_0031:  constrained. ""T""
  IL_0037:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_003c:  br.s       IL_0055
  IL_003e:  ldloca.s   V_2
  IL_0040:  ldloca.s   V_3
  IL_0042:  constrained. ""T""
  IL_0048:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_004d:  ldloc.0
  IL_004e:  ldloc.1
  IL_004f:  ldloc.2
  IL_0050:  stelem     ""T""
  IL_0055:  ldloc.2
  IL_0056:  ret
}
");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00730_Consumption_Used_Struct([CombinatorialValues("+=", "-=", "*=", "/=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
    public void operator checked" + op + @"(long x);
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
    public void operator checked" + op + @"(long x)
    {
        System.Console.Write(""[operator checked]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1[] x = [new C1()];

    static void Main()
    {
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
    } 

    static C1 Test1(C1[] x)
    {
#line 23
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test3<T>(T[] x) where T : struct, I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[G1]"");
        x[0] = new C1() { _F = -1 };
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][G1][operator]11
[GetA][Get0][G1][operator checked]22
[GetA][Get0][G1][operator]33
[GetA][Get0][G1][operator checked]44
[GetA][Get0][G1][operator]55
[GetA][Get0][G1][operator checked]66
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""int Program.G1()""
  IL_001e:  conv.i8
  IL_001f:  call       ""void C1." + methodName + @"(long)""
  IL_0024:  ldloc.0
  IL_0025:  stobj      ""C1""
  IL_002a:  ldloc.0
  IL_002b:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (int V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  stelem     ""T""
  IL_002e:  ldloc.1
  IL_002f:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: C1) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
      Instance Receiver:
        null
      Arguments(0)
");

            methodName = CompoundAssignmentOperatorName(op, isChecked: true);

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""int Program.G1()""
  IL_001e:  conv.i8
  IL_001f:  call       ""void C1." + methodName + @"(long)""
  IL_0024:  ldloc.0
  IL_0025:  stobj      ""C1""
  IL_002a:  ldloc.0
  IL_002b:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (int V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  stelem     ""T""
  IL_002e:  ldloc.1
  IL_002f:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("T", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @", Checked) (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: T) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,16): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(23, 16),
                // (30,20): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(30, 20),
                // (36,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(36, 16),
                // (43,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(43, 20),
                // (49,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(49, 16),
                // (56,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(56, 20)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00731_Consumption_Used_Struct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(long x);
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"(long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1[] x = [new C1()];

    static void Main()
    {
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
    } 

    static C1 Test1(C1[] x)
    {
#line 23
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test3<T>(T[] x) where T : struct, I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[G1]"");
        x[0] = new C1() { _F = -1 };
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][G1][operator]11
[GetA][Get0][G1][operator]22
[GetA][Get0][G1][operator]33
[GetA][Get0][G1][operator]44
[GetA][Get0][G1][operator]55
[GetA][Get0][G1][operator]66
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""int Program.G1()""
  IL_001e:  conv.i8
  IL_001f:  call       ""void C1." + methodName + @"(long)""
  IL_0024:  ldloc.0
  IL_0025:  stobj      ""C1""
  IL_002a:  ldloc.0
  IL_002b:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (int V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  stelem     ""T""
  IL_002e:  ldloc.1
  IL_002f:  ret
}
");

            var tree = comp2.SyntaxTrees.First();
            var model = comp2.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Equal("void C1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("C1", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            var iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void C1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: C1) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: C1) (Syntax: 'GetA(x)[Get0()]')
  Array reference:
    IInvocationOperation (C1[] Program.GetA<C1>(C1[] x)) (OperationKind.Invocation, Type: C1[]) (Syntax: 'GetA(x)')
      Instance Receiver:
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C1[]) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Indices(1):
      IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
        Instance Receiver:
          null
        Arguments(0)
  Right:
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
      Instance Receiver:
        null
      Arguments(0)
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (C1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""int Program.G1()""
  IL_001e:  conv.i8
  IL_001f:  call       ""void C1." + methodName + @"(long)""
  IL_0024:  ldloc.0
  IL_0025:  stobj      ""C1""
  IL_002a:  ldloc.0
  IL_002b:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (int V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  constrained. ""T""
  IL_0022:  callvirt   ""void I1." + methodName + @"(long)""
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  stelem     ""T""
  IL_002e:  ldloc.1
  IL_002f:  ret
}
");

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Where(n => n.OperatorToken.Text == op).Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("void I1." + methodName + "(System.Int64 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal("T", model.GetTypeInfo(opNode).Type.ToTestDisplayString());

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            iOp = model.GetOperation(opNode);
            VerifyOperationTree(comp2, iOp, @"
ICompoundAssignmentOperation (BinaryOperatorKind." + CompoundAssignmentOperatorToBinaryOperatorKind(op) + @") (OperatorMethod: void I1." + methodName + @"(System.Int64 x)) (OperationKind.CompoundAssignment, Type: T) (Syntax: 'GetA(x)[Get0()]" + op + @" G1()')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: T) (Syntax: 'GetA(x)[Get0()]')
      Array reference:
        IInvocationOperation (T[] Program.GetA<T>(T[] x)) (OperationKind.Invocation, Type: T[]) (Syntax: 'GetA(x)')
          Instance Receiver:
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: T[]) (Syntax: 'x')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Indices(1):
          IInvocationOperation (System.Int32 Program.Get0()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Get0()')
            Instance Receiver:
              null
            Arguments(0)
  Right:
    IConversionOperation (TryCast: False, Checked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'G1()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IInvocationOperation (System.Int32 Program.G1()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'G1()')
          Instance Receiver:
            null
          Arguments(0)
");

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular14);
            verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular13);
            var expectedErrors = new[] {
                // (23,16): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(23, 16),
                // (30,20): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "C1", "int").WithLocation(30, 20),
                // (36,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(36, 16),
                // (43,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(43, 20),
                // (49,16): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //         return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(49, 16),
                // (56,20): error CS0019: Operator '+=' cannot be applied to operands of type 'T' and 'int'
                //             return GetA(x)[Get0()]+= G1();
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()]" + op + @" G1()").WithArguments(op, "T", "int").WithLocation(56, 20)
                };
            comp2.VerifyDiagnostics(expectedErrors);

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            comp2.VerifyDiagnostics(expectedErrors);
        }

        [Fact]
        public void CompoundAssignment_00740_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
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
                """;
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_00741_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public void X(C right) {}
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
                """;
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (7,9): error CS8350: This combination of arguments to 'C.operator +=(C)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         c += c1;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c += c1").WithArguments("C.operator +=(C)", "right").WithLocation(7, 9),
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
        public void CompoundAssignment_00742_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public void X(C right) {}
                    public C M1(scoped C c, C c1)
                    {
                        c += c1;
                        return c;
                    }
                    public C M2(scoped C c, C c1)
                    {
                        c.X(c1);
                        return c;
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
        public void CompoundAssignment_00743_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public void X(C right) {}
                    public C M1(scoped C c, scoped C c1)
                    {
                        c += c1;
                        return c;
                    }
                    public C M2(scoped C c, scoped C c1)
                    {
                        c.X(c1);
                        return c;
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
        public void CompoundAssignment_00744_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(scoped C right) {}
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
                """;
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_00745_Consumption_RefSafety()
        {
            var source = """
                using System.Diagnostics.CodeAnalysis;

                public ref struct C
                {
                    [UnscopedRef]
                    public void operator +=(C right) {}
                    [UnscopedRef]
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
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
                // (8,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(8, 16),
                // (13,16): error CS8352: Cannot use variable 'scoped C c' in this context because it may expose referenced variables outside of their declaration scope
                //         return c;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c").WithArguments("scoped C c").WithLocation(13, 16)
                );
        }

        [Fact]
        public void CompoundAssignment_00746_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public C M1(C c, C c1)
                    {
                        return c += c1;
                    }
                }
                """;
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_00747_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public C M1(scoped C c, C c1)
                    {
                        return c += c1;
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
        public void CompoundAssignment_00748_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public C M1(C c, scoped C c1)
                    {
                        return c += c1;
                    }
                }
                """;
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
                // (6,16): error CS8350: This combination of arguments to 'C.operator +=(C)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c += c1;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c += c1").WithArguments("C.operator +=(C)", "right").WithLocation(6, 16),
                // (6,21): error CS8352: Cannot use variable 'scoped C c1' in this context because it may expose referenced variables outside of their declaration scope
                //         return c += c1;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "c1").WithArguments("scoped C c1").WithLocation(6, 21)
                );
        }

        [Fact]
        public void CompoundAssignment_00749_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(scoped C right) {}
                    public C M1(C c, scoped C c1)
                    {
                        return c += c1;
                    }
                }
                """;
            CreateCompilation([source, CompilerFeatureRequiredAttribute]).VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_00750_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    public C M1(scoped C c, scoped C c1)
                    {
                        return c += c1;
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
        public void CompoundAssignment_00751_Consumption_RefSafety()
        {
            var source = """
                public ref struct C
                {
                    public void operator +=(C right) {}
                    static C X(C c) => throw null;
                    public C M1(scoped C c, scoped C c1)
                    {
                        return X(c += c1);
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
        public void CompoundAssignment_00752_Consumption_RefSafety()
        {
            var source = $$$"""
                ref struct C
                {
                    private ref readonly int _i;
                    public void operator +=([System.Diagnostics.CodeAnalysis.UnscopedRef] in int right) { _i = ref right; }
                    public static C operator -(C left, [System.Diagnostics.CodeAnalysis.UnscopedRef] in int right) { left._i = ref right; return left; }
                    public C M1(C c1)
                    {
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
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
                // (8,16): error CS8350: This combination of arguments to 'C.operator +=(in int)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c1 += 1;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c1 += 1").WithArguments("C.operator +=(in int)", "right").WithLocation(8, 16),
                // (8,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c1 += 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(8, 22),
                // (12,16): error CS8347: Cannot use a result of 'C.operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("C.operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'C.operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("C.operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (16,16): error CS8350: This combination of arguments to 'C.operator +=(in int)' is disallowed because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c3 += right;
                Diagnostic(ErrorCode.ERR_CallArgMixing, "c3 += right").WithArguments("C.operator +=(in int)", "right").WithLocation(16, 16),
                // (16,22): error CS9077: Cannot return a parameter by reference 'right' through a ref parameter; it can only be returned in a return statement
                //         return c3 += right;
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "right").WithArguments("right").WithLocation(16, 22),
                // (20,16): error CS8347: Cannot use a result of 'C.operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c4 -= right").WithArguments("C.operator -(C, in int)", "right").WithLocation(20, 16),
                // (20,22): error CS9077: Cannot return a parameter by reference 'right' through a ref parameter; it can only be returned in a return statement
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "right").WithArguments("right").WithLocation(20, 22)
                );
        }

        [Fact]
        public void CompoundAssignment_00753_Consumption_RefSafety()
        {
            var source = """
                ref struct C
                {
                    private ref readonly int _i;
                    public void operator +=(in int right) { _i = ref right; }
                    public static C operator -(C left, in int right) { left._i = ref right; return left; }
                    public C M1(C c1)
                    {
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
                """;
            CreateCompilation(source, targetFramework: TargetFramework.Net90).VerifyDiagnostics(
                // (4,45): error CS9079: Cannot ref-assign 'right' to '_i' because 'right' can only escape the current method through a return statement.
                //     public void operator +=(in int right) { _i = ref right; }
                Diagnostic(ErrorCode.ERR_RefAssignReturnOnly, "_i = ref right").WithArguments("_i", "right").WithLocation(4, 45),
                // (5,56): error CS9079: Cannot ref-assign 'right' to '_i' because 'right' can only escape the current method through a return statement.
                //     public static C operator -(C left, in int right) { left._i = ref right; return left; }
                Diagnostic(ErrorCode.ERR_RefAssignReturnOnly, "left._i = ref right").WithArguments("_i", "right").WithLocation(5, 56),
                // (12,16): error CS8347: Cannot use a result of 'C.operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("C.operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'C.operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c2 -= 1").WithArguments("C.operator -(C, in int)", "right").WithLocation(12, 16),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (12,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return c2 -= 1;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "1").WithLocation(12, 22),
                // (20,16): error CS8347: Cannot use a result of 'C.operator -(C, in int)' in this context because it may expose variables referenced by parameter 'right' outside of their declaration scope
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_EscapeCall, "c4 -= right").WithArguments("C.operator -(C, in int)", "right").WithLocation(20, 16),
                // (20,22): error CS9077: Cannot return a parameter by reference 'right' through a ref parameter; it can only be returned in a return statement
                //         return c4 -= right;
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "right").WithArguments("right").WithLocation(20, 22)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00732_Consumption_Used_Struct([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    public void operator" + op + @"(in long x);
}

public struct C1 : I1
{
    public int _F;
    public void operator" + op + @"(in long x)
    {
        System.Console.Write(""[operator]"");
        _F = _F + (int)x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static C1[] x = [new C1()];

    static void Main()
    {
        C1 y = Test1(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test2(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test3(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test4(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test5(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
        y = Test6(x);
        System.Console.Write(y._F);
        System.Console.WriteLine(x[0]._F);
    } 

    static C1 Test1(C1[] x)
    {
#line 23
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static C1 Test2(C1[] x)
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test3<T>(T[] x) where T : struct, I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test4<T>(T[] x) where T : struct, I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T Test5<T>(T[] x) where T : I1
    {
        return GetA(x)[Get0()]" + op + @" G1();
    } 

    static T Test6<T>(T[] x) where T : I1
    {
        checked
        {
            return GetA(x)[Get0()]" + op + @" G1();
        }
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }

    static int G1()
    {
        System.Console.Write(""[G1]"");
        x[0] = new C1() { _F = -1 };
        return 1;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.ReleaseExe);
            const string expectedOutput = @"
[GetA][Get0][G1][operator]11
[GetA][Get0][G1][operator]22
[GetA][Get0][G1][operator]33
[GetA][Get0][G1][operator]44
[GetA][Get0][G1][operator]55
[GetA][Get0][G1][operator]66
";
            var verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput).VerifyDiagnostics();

            var methodName = CompoundAssignmentOperatorName(op, isChecked: false);

            verifier.VerifyIL("Program.Test1",
@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (C1 V_0,
                long V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""int Program.G1()""
  IL_001e:  conv.i8
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  call       ""void C1." + methodName + @"(in long)""
  IL_0027:  ldloc.0
  IL_0028:  stobj      ""C1""
  IL_002d:  ldloc.0
  IL_002e:  ret
}
");

            verifier.VerifyIL("Program.Test3<T>(T[])",
@"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                long V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  stloc.2
  IL_001d:  ldloca.s   V_2
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_002a:  ldloc.0
  IL_002b:  ldloc.1
  IL_002c:  stelem     ""T""
  IL_0031:  ldloc.1
  IL_0032:  ret
}
");

            verifier.VerifyIL("Program.Test2",
@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  .locals init (C1 V_0,
            long V_1)
  IL_0000:  ldarg.0
  IL_0001:  call       ""C1[] Program.GetA<C1>(C1[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  ldelema    ""C1""
  IL_0010:  dup
  IL_0011:  ldobj      ""C1""
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""int Program.G1()""
  IL_001e:  conv.i8
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  call       ""void C1." + methodName + @"(in long)""
  IL_0027:  ldloc.0
  IL_0028:  stobj      ""C1""
  IL_002d:  ldloc.0
  IL_002e:  ret
}
");

            verifier.VerifyIL("Program.Test4<T>(T[])",
@"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (int V_0,
            T V_1,
            long V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T[] Program.GetA<T>(T[])""
  IL_0006:  call       ""int Program.Get0()""
  IL_000b:  stloc.0
  IL_000c:  dup
  IL_000d:  ldloc.0
  IL_000e:  ldelem     ""T""
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       ""int Program.G1()""
  IL_001b:  conv.i8
  IL_001c:  stloc.2
  IL_001d:  ldloca.s   V_2
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""void I1." + methodName + @"(in long)""
  IL_002a:  ldloc.0
  IL_002b:  ldloc.1
  IL_002c:  stelem     ""T""
  IL_0031:  ldloc.1
  IL_0032:  ret
}
");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00800_Consumption_CheckedVersionInRegularContext([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source1 = @"
public class C1
{
    public int _F;
    public void operator checked " + op + @"(int x)
    {
        System.Console.Write(""[operator]"");
        _F+=x;
    } 
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1[] x = [new C1()];
        Test1(x);
        System.Console.WriteLine(x[0]._F);
    } 

    static void Test1(C1[] x)
    {
        GetA(x)[Get0()] " + op + @" 1;
    } 

    static T[] GetA<T>(T[] x)
    {
        System.Console.Write(""[GetA]"");
        return x;
    } 

    static int Get0()
    {
        System.Console.Write(""[Get0]"");
        return 0;
    }
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (13,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         GetA(x)[Get0()] += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "GetA(x)[Get0()] " + op + @" 1").WithArguments(op, "C1", "int").WithLocation(13, 9)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00810_Consumption_Shadowing([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            string checkedForm = null;

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                checkedForm = @"
    public void operator checked" + op + @"(int x) => throw null;
";
            }

            var source1 = @"
public class C1
{
    public void operator" + op + @"(int x) => throw null;
" + checkedForm + @"
}

public class C2 : C1
{
    public new void operator" + op + @"(int x)
    {
        System.Console.Write(""[operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C2();
        x " + op + @" 1;
        checked
        {
            x " + op + @" 1;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[operator][operator]").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00820_Consumption_Shadowing([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source1_1 = @"
public class C1
{
    public void operator" + op + @"(int x) => throw null;
    public void operator checked" + op + @"(int x) => throw null;
}

public class C2 : C1
{
    public new void operator checked " + op + @"(int x) => throw null;
}
";

            var comp1_1 = CreateCompilation([source1_1, CompilerFeatureRequiredAttribute], assemblyName: "C");

            var source2 = @"
public class Test
{
    public static void Main()
    {
        var x = new C2();
        x " + op + @" 1;
        checked
        {
            x " + op + @" 1;
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1_1.ToMetadataReference()]);
            comp2.VerifyDiagnostics();

            var source1_2 = @"
public class C1
{
    public void operator" + op + @"(int x)
    {
        System.Console.Write(""[operator]"");
    } 

    public void operator checked" + op + @"(int x) => throw null;
}

public class C2 : C1
{
    public new void operator checked " + op + @"(int x)
    {
        System.Console.Write(""[checked operator]"");
    } 

    public new void operator " + op + @"(int x) => throw null;
}
";

            var source3 = @"
public class Program
{
    static void Main()
    {
        Test.Main();
    } 
}
";
            var comp1_2 = CreateCompilation([source1_2, CompilerFeatureRequiredAttribute], assemblyName: "C");

            var comp3 = CreateCompilation([source3, CompilerFeatureRequiredAttribute], references: [comp1_2.EmitToImageReference(), comp2.EmitToImageReference()], options: TestOptions.DebugExe);
            CompileAndVerify(comp3, expectedOutput: "[operator][checked operator]").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00830_Consumption_Shadowing([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source1 = @"
public abstract class C1
{
    public abstract void operator" + op + @"(int x);
    public void operator checked" + op + @"(int x) => throw null;
}

public class C2 : C1
{
    public new void operator checked " + op + @"(int x)
    {
        System.Console.Write(""[checked operator]"");
    } 

    public override void operator " + op + @"(int x)
    {
        System.Console.Write(""[operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C2();
        x " + op + @" 1;
        checked
        {
            x " + op + @" 1;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[operator][checked operator]").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00840_Consumption_Overriding([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source1 = @"
public abstract class C1
{
    public abstract void operator" + op + @"(int x);
    public abstract void operator checked" + op + @"(int x);
}

public abstract class C2 : C1
{
    public override void operator checked " + op + @"(int x)
    {
        System.Console.Write(""[checked operator]"");
    } 
}

public class C3 : C2
{
    public override void operator " + op + @"(int x)
    {
        System.Console.Write(""[operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C3();
        x " + op + @" 1;
        checked
        {
            x " + op + @" 1;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[operator][checked operator]").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_00850_Consumption_Ambiguity()
        {
            var source1 = @"
public interface I1
{
    public void operator +=(int x);
}

public interface I2<T> where T : I2<T>
{
    public void operator +=(int x);
    public abstract static T operator +(T x, int y);
    public abstract static T operator -(T x, int y);
}

public class Program
{
    static void Test5<T>(T x) where T : I1, I2<T>
    {
        x += 1;
        x -= 1;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            comp1.VerifyDiagnostics(
                // (18,11): error CS0121: The call is ambiguous between the following methods or properties: 'I1.operator +=(int)' and 'I2<T>.operator +=(int)'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_AmbigCall, "+=").WithArguments("I1.operator +=(int)", "I2<T>.operator +=(int)").WithLocation(18, 11)
                );

            var tree = comp1.SyntaxTrees.First();
            var model = comp1.GetSemanticModel(tree);
            var opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(opNode);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("void I1.op_AdditionAssignment(System.Int32 x)", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void I2<T>.op_AdditionAssignment(System.Int32 x)", symbolInfo.CandidateSymbols[1].ToTestDisplayString());

            var group = model.GetMemberGroup(opNode);
            Assert.Empty(group);

            opNode = tree.GetRoot().DescendantNodes().OfType<Syntax.AssignmentExpressionSyntax>().Last();
            symbolInfo = model.GetSymbolInfo(opNode);
            Assert.Equal("T I2<T>.op_Subtraction(T x, System.Int32 y)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);

            group = model.GetMemberGroup(opNode);
            Assert.Empty(group);
        }

        [Fact]
        public void CompoundAssignment_00860_Consumption_UseStaticOperatorsInOldVersion()
        {
            var source1 = @"
public class C1
{
    public int _F;

    public void operator +=(int x)
    {
        System.Console.Write(""[instance operator]"");
        _F+=x;
    } 

    public void operator checked +=(int x)
    {
        System.Console.Write(""[instance operator checked]"");
        checked
        {
            _F+=x;
        }
    } 

    public static C1 operator +(C1 x, int y)
    {
        System.Console.Write(""[static operator]"");
        return new C1() { _F = x._F + y };
    } 
    public static C1 operator checked +(C1 x, int y)
    {
        System.Console.Write(""[static operator checked]"");
        checked
        {
            return new C1() { _F = x._F + y };
        }
    } 
}
";
            var comp1Ref = CreateCompilation([source1, CompilerFeatureRequiredAttribute]).EmitToImageReference();

            var source2 = @"
public class Program
{
    static C1 P = new C1();

    static void Main()
    {
        C1 x;

        P += 1;
        System.Console.WriteLine(P._F);
        x = P += 1;
        System.Console.WriteLine(P._F);

        checked
        {
            P += 1;
            System.Console.WriteLine(P._F);
            x = P += 1;
            System.Console.WriteLine(P._F);
        }
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1Ref], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: @"
[instance operator]1
[instance operator]2
[instance operator checked]3
[instance operator checked]4
").VerifyDiagnostics();

            comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1Ref], options: TestOptions.DebugExe, parseOptions: TestOptions.Regular13);
            CompileAndVerify(comp2, expectedOutput: @"
[static operator]1
[static operator]2
[static operator checked]3
[static operator checked]4
").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_00870_Consumption_Obsolete()
        {
            var source1 = @"
public class C1
{
    [System.Obsolete(""Test"")]
    public void operator +=(int x) {}
}

public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1).VerifyDiagnostics(
                // (13,9): warning CS0618: 'C1.operator +=(int)' is obsolete: 'Test'
                //         x += 1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "x += 1").WithArguments("C1.operator +=(int)", "Test").WithLocation(13, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00880_Consumption_UnmanagedCallersOnly()
        {
            var source1 = @"
public class C1
{
    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    public void operator +=(int x) {}
}

public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp1.VerifyDiagnostics(
                // (4,6): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //     [System.Runtime.InteropServices.UnmanagedCallersOnly]
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "System.Runtime.InteropServices.UnmanagedCallersOnly").WithLocation(4, 6),
                // (13,9): error CS8901: 'C1.operator +=(int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                //         x += 1;
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "x += 1").WithArguments("C1.operator +=(int)").WithLocation(13, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00890_Consumption_NullableAnalysis()
        {
            var source1 = @"
public class C1
{
    public void operator +=(int x) {}
}

#nullable enable

public class Program
{
    static void Main()
    {
        C1? x = null;

        try
        {
            x += 1;
            System.Console.Write(""unreachable"");
            x.ToString();
        }
        catch (System.NullReferenceException)
        {
            System.Console.Write(""in catch"");
        }

        C1? y = new C1();
        y += 1;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "in catch").VerifyDiagnostics(
                // (17,13): warning CS8602: Dereference of a possibly null reference.
                //             x += 1;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(17, 13)
                );

            var source2 = @"
public class C1
{
    public void operator +=(int x) {}
}

#nullable enable

public class Program
{
    static void Main()
    {
        C1? x = null;

        if (false)
        {
            x += 1;
            System.Console.Write(""unreachable"");
            x.ToString();
        }

        System.Console.Write(""Done"");
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp2, expectedOutput: "Done").VerifyDiagnostics(
                // (17,13): warning CS0162: Unreachable code detected
                //             x += 1;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(17, 13)
                );
        }

        [Fact]
        public void CompoundAssignment_00891_Consumption_NullableAnalysis()
        {
            var source1 = @"
public class C1<T>
{
    public void operator +=(T x) {}
}

#nullable enable

public class Program
{
    static C1<T> GetC1<T>(T x) => new C1<T>();

    static void Main()
    {
        string? x = null;
        var c1 = GetC1(new object());

        c1 += x;
        x.ToString();

        var c2 = GetC1((object?)null);
        c2 += null;
        c2 += (string?)null;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp1.VerifyDiagnostics(
                // (18,15): warning CS8604: Possible null reference argument for parameter 'x' in 'void C1<object>.operator +=(object x)'.
                //         c1 += x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "void C1<object>.operator +=(object x)").WithLocation(18, 15)
                );
        }

        [Fact]
        public void CompoundAssignment_00900_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator +=(int a, int x = 0) {}
}
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
                // (4,26): error CS1020: Overloadable binary operator expected
                //     public void operator +=(int a, int x = 0) {}
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "+=").WithLocation(4, 26),
                // (4,40): warning CS1066: The default value specified for parameter 'x' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     public void operator +=(int a, int x = 0) {}
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "x").WithArguments("x").WithLocation(4, 40)
                );

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00901_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public int operator +=(int a) { return 0; }
}
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
                // (4,25): error CS9310: The return type for this operator must be void
                //     public int operator +=(int a) { return 0; }
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, "+=").WithLocation(4, 25)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00902_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator +=(__arglist) {}
}
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
                // (4,29): error CS1669: __arglist is not valid in this context
                //     public void operator +=(__arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(4, 29)
                );

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00903_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator +=(int x, __arglist) {}
}
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
                // (4,26): error CS1020: Overloadable binary operator expected
                //     public void operator +=(int x, __arglist) {}
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "+=").WithLocation(4, 26),
                // (4,36): error CS1669: __arglist is not valid in this context
                //     public void operator +=(int x, __arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(4, 36)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyEmitDiagnostics();

            var method = comp2.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.AdditionAssignmentOperatorName);
            Assert.False(method.IsVararg);
            AssertEx.Equal("void C1.op_AdditionAssignment(System.Int32 x)", method.ToTestDisplayString());
        }

        [Fact]
        public void CompoundAssignment_00904_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator +=(ref int x) {}
    public void operator -=(ref readonly int x) {}
    public void operator *=(out int x) { x = 0; }
}
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
        x -= 1;
        x *= 1;
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (4,26): error CS0631: ref and out are not valid in this context
                //     public void operator +=(ref int x) {}
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "+=").WithLocation(4, 26),
                // (5,26): error CS0631: ref and out are not valid in this context
                //     public void operator -=(ref readonly int x) {}
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "-=").WithLocation(5, 26),
                // (6,26): error CS0631: ref and out are not valid in this context
                //     public void operator *=(out int x) { x = 0; }
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "*=").WithLocation(6, 26)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9),
                // (8,9): error CS0019: Operator '-=' cannot be applied to operands of type 'C1' and 'int'
                //         x -= 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x -= 1").WithArguments("-=", "C1", "int").WithLocation(8, 9),
                // (9,9): error CS0019: Operator '*=' cannot be applied to operands of type 'C1' and 'int'
                //         x *= 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x *= 1").WithArguments("*=", "C1", "int").WithLocation(9, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00910_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator +=(params int[] x) {}
}
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
                // (4,29): error CS1670: params is not valid in this context
                //     public void operator +=(params int[] x) {}
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(4, 29)
                );

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00911_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    [System.Runtime.CompilerServices.SpecialName]
    public void " + WellKnownMemberNames.AdditionAssignmentOperatorName + @"(params int[] x) {}

    [System.Runtime.CompilerServices.SpecialName]
    public void " + WellKnownMemberNames.AdditionAssignmentOperatorName + @"(string x) {}

    [System.Runtime.CompilerServices.SpecialName]
    public void " + WellKnownMemberNames.AdditionAssignmentOperatorName + @"(string[] x) {}
}
";
            var source2 = @"
public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
        x += [2];
        x += ""3"";
        x += [""4""];
    } 
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyEmitDiagnostics();

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [comp1.EmitToImageReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9),
                // (8,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'collection expression'
                //         x += [2];
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += [2]").WithArguments("+=", "C1", "collection expression").WithLocation(8, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00912_Consumption_BadOperator()
        {
            var source1 = @"
public class C1
{
    public void operator +=(int a, params int[] x) {}
}
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
                // (4,26): error CS1020: Overloadable binary operator expected
                //     public void operator +=(int a, params int[] x) {}
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "+=").WithLocation(4, 26),
                // (4,36): error CS1670: params is not valid in this context
                //     public void operator +=(int a, params int[] x) {}
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(4, 36)
                );

            var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_00920_ConflictWithRegular()
        {
            var source1 = @"
public class C1
{
    public void operator +=(int x) {}

#line 1000
    public void op_AdditionAssignment(int x) {}
}

public class C2
{
    public void op_AdditionAssignment(int x) {}

#line 2000
    public void operator +=(int x) {}
}

public class C3
{
    public void operator +=(int x) {}

    public void op_AdditionAssignment(long x) {}
}

public class C4
{
    public void op_AdditionAssignment(int x) {}

    public void operator +=(long x) {}
}
";

            CSharpCompilation comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);
            comp1.VerifyDiagnostics(
                // (1000,17): error CS0111: Type 'C1' already defines a member called 'op_AdditionAssignment' with the same parameter types
                //     public void op_AdditionAssignment(int x) {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_AdditionAssignment").WithArguments("op_AdditionAssignment", "C1").WithLocation(1000, 17),
                // (2000,26): error CS0111: Type 'C2' already defines a member called 'op_AdditionAssignment' with the same parameter types
                //     public void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "+=").WithArguments("op_AdditionAssignment", "C2").WithLocation(2000, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00930_Consumption_RegularVsOperator(bool fromMetadata)
        {
            var source1 = @"
public class C1
{
    public void op_AdditionAssignment(int x) {}
}

public class C2
{
    public void operator+=(int x) {}
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class Program
{
    static void Main()
    {
        C1 x = new C1();
        x += 1;
        C2 y = new C2();
        y.op_AdditionAssignment(1);
    } 
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()], options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,9): error CS0019: Operator '+=' cannot be applied to operands of type 'C1' and 'int'
                //         x += 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x += 1").WithArguments("+=", "C1", "int").WithLocation(7, 9),
                // (9,11): error CS0571: 'C2.operator +=(int)': cannot explicitly call operator or accessor
                //         y.op_AdditionAssignment(1);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_AdditionAssignment").WithArguments("C2.operator +=(int)").WithLocation(9, 11)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00940_Override_RegularVsOperatorMismatch(bool fromMetadata)
        {
            var source1 = @"
abstract public class C1
{
    public abstract void op_AdditionAssignment(int x);
}

abstract public class C3
{
    public abstract void operator +=(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class C2 : C1
{
    public override void operator +=(int x) {}
}

public class C4 : C3
{
    public override void op_AdditionAssignment(int x) {}
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()]);

            comp2.VerifyDiagnostics(
                // (4,35): error CS9312: 'C2.operator +=(int)': cannot override inherited member 'C1.op_AdditionAssignment(int)' because one of them is not an operator.
                //     public override void operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorMismatchOnOverride, "+=").WithArguments("C2.operator +=(int)", "C1.op_AdditionAssignment(int)").WithLocation(4, 35),
                // (9,26): error CS9312: 'C4.op_AdditionAssignment(int)': cannot override inherited member 'C3.operator +=(int)' because one of them is not an operator.
                //     public override void op_AdditionAssignment(int x) {}
                Diagnostic(ErrorCode.ERR_OperatorMismatchOnOverride, "op_AdditionAssignment").WithArguments("C4.op_AdditionAssignment(int)", "C3.operator +=(int)").WithLocation(9, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00950_Implement_RegularVsOperatorMismatch(bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    void op_AdditionAssignment(int x);
}

public interface I2
{
    void operator +=(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class C1 : I1
{
    public void operator +=(int x) {}
}

public class C2 : I2
{
    public void op_AdditionAssignment(int x) {}
}

public class C3 : I1
{
    void I1.operator +=(int x) {}
}

public class C4 : I2
{
    void I2.op_AdditionAssignment(int x) {}
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()]);
            comp2.VerifyDiagnostics(
                // (2,19): error CS9311: 'C1' does not implement interface member 'I1.op_AdditionAssignment(int)'. 'C1.operator +=(int)' cannot implement 'I1.op_AdditionAssignment(int)' because one of them is not an operator.
                // public class C1 : I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C1", "I1.op_AdditionAssignment(int)", "C1.operator +=(int)").WithLocation(2, 19),
                // (7,19): error CS9311: 'C2' does not implement interface member 'I2.operator +=(int)'. 'C2.op_AdditionAssignment(int)' cannot implement 'I2.operator +=(int)' because one of them is not an operator.
                // public class C2 : I2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I2").WithArguments("C2", "I2.operator +=(int)", "C2.op_AdditionAssignment(int)").WithLocation(7, 19),
                // (12,19): error CS0535: 'C3' does not implement interface member 'I1.op_AdditionAssignment(int)'
                // public class C3 : I1
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I1").WithArguments("C3", "I1.op_AdditionAssignment(int)").WithLocation(12, 19),
                // (14,22): error CS0539: 'C3.operator +=(int)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I1.operator +=(int x) {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "+=").WithArguments("C3.operator +=(int)").WithLocation(14, 22),
                // (17,19): error CS0535: 'C4' does not implement interface member 'I2.operator +=(int)'
                // public class C4 : I2
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I2").WithArguments("C4", "I2.operator +=(int)").WithLocation(17, 19),
                // (19,13): error CS0539: 'C4.op_AdditionAssignment(int)' in explicit interface declaration is not found among members of the interface that can be implemented
                //     void I2.op_AdditionAssignment(int x) {}
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "op_AdditionAssignment").WithArguments("C4.op_AdditionAssignment(int)").WithLocation(19, 13)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_00960_Implement_RegularVsOperatorMismatch(bool fromMetadata)
        {
            var source1 = @"
public interface I1
{
    void op_AdditionAssignment(int x);
}

public interface I2
{
    void operator +=(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute]);

            var source2 = @"
public class C11
{
    public void operator +=(int x) {}
}

public class C12 : C11, I1
{
}

public class C21
{
    public void op_AdditionAssignment(int x) {}
}

public class C22 : C21, I2
{
}
";

            var comp2 = CreateCompilation([source2, CompilerFeatureRequiredAttribute], references: [fromMetadata ? comp1.EmitToImageReference() : comp1.ToMetadataReference()]);
            comp2.VerifyDiagnostics(
                // (7,25): error CS9311: 'C12' does not implement interface member 'I1.op_AdditionAssignment(int)'. 'C11.operator +=(int)' cannot implement 'I1.op_AdditionAssignment(int)' because one of them is not an operator.
                // public class C12 : C11, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C12", "I1.op_AdditionAssignment(int)", "C11.operator +=(int)").WithLocation(7, 25),
                // (16,25): error CS9311: 'C22' does not implement interface member 'I2.operator +=(int)'. 'C21.op_AdditionAssignment(int)' cannot implement 'I2.operator +=(int)' because one of them is not an operator.
                // public class C22 : C21, I2
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I2").WithArguments("C22", "I2.operator +=(int)", "C21.op_AdditionAssignment(int)").WithLocation(16, 25)
                );
        }

        [Fact]
        public void CompoundAssignment_00970_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void operator +=(int x)
    {
        System.Console.Write(""[I1.operator]"");
    } 
}

public class C1 : I1
{
    public virtual void op_AdditionAssignment(int x)
    {
        System.Console.Write(""[C1.op_AdditionAssignment]"");
    } 
}

public class C2
{
    public virtual void op_AdditionAssignment(int x)
    {
        System.Console.Write(""[C2.op_AdditionAssignment]"");
    } 
}

public class C3 : C2, I1
{
}

public class Program
{
    static void Main()
    {
        I1 x = new C1();
        x += 1;
        x = new C3();
        x += 1;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,19): error CS9311: 'C1' does not implement interface member 'I1.operator +=(int)'. 'C1.op_AdditionAssignment(int)' cannot implement 'I1.operator +=(int)' because one of them is not an operator.
                // public class C1 : I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C1", "I1.operator +=(int)", "C1.op_AdditionAssignment(int)").WithLocation(10, 19),
                // (26,23): error CS9311: 'C3' does not implement interface member 'I1.operator +=(int)'. 'C2.op_AdditionAssignment(int)' cannot implement 'I1.operator +=(int)' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.operator +=(int)", "C2.op_AdditionAssignment(int)").WithLocation(26, 23)
                );
        }

        [Fact]
        public void CompoundAssignment_00980_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void op_AdditionAssignment(int x)
    {
        System.Console.Write(""[I1.operator]"");
    } 
}

public class C1 : I1
{
    public virtual void operator +=(int x)
    {
        System.Console.Write(""[C1.operator]"");
    } 
}

public class C2
{
    public virtual void operator +=(int x)
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class C3 : C2, I1
{
}

public class Program
{
    static void Main()
    {
        I1 x = new C1();
        x.op_AdditionAssignment(1);
        x = new C3();
        x.op_AdditionAssignment(1);
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (10,19): error CS9311: 'C1' does not implement interface member 'I1.op_AdditionAssignment(int)'. 'C1.operator +=(int)' cannot implement 'I1.op_AdditionAssignment(int)' because one of them is not an operator.
                // public class C1 : I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C1", "I1.op_AdditionAssignment(int)", "C1.operator +=(int)").WithLocation(10, 19),
                // (26,23): error CS9311: 'C3' does not implement interface member 'I1.op_AdditionAssignment(int)'. 'C2.operator +=(int)' cannot implement 'I1.op_AdditionAssignment(int)' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.op_AdditionAssignment(int)", "C2.operator +=(int)").WithLocation(26, 23)
                );
        }

        [Fact]
        public void CompoundAssignment_00990_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void operator +=(int x);
}

public class C2 : I1
{
    void I1.operator +=(int x)
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class C3 : C2, I1
{
    public virtual void op_AdditionAssignment(int x)
    {
        System.Console.Write(""[C3.op_IncrementAssignment]"");
    } 
}

public class Program
{
    static void Main()
    {
        I1 x = new C3();
        x += 1;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (15,23): error CS9311: 'C3' does not implement interface member 'I1.operator +=(int)'. 'C3.op_AdditionAssignment(int)' cannot implement 'I1.operator +=(int)' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.operator +=(int)", "C3.op_AdditionAssignment(int)").WithLocation(15, 23)
                );
        }

        [Fact]
        public void CompoundAssignment_01000_Implement_RegularVsOperatorMismatch()
        {
            var source = @"
public interface I1
{
    public void op_AdditionAssignment(int x);
}

public class C2 : I1
{
    void I1.op_AdditionAssignment(int x)
    {
        System.Console.Write(""[C2.op_IncrementAssignment]"");
    } 
}

public class C3 : C2, I1
{
    public virtual void operator +=(int x)
    {
        System.Console.Write(""[C3.operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        I1 x = new C3();
        x.op_AdditionAssignment(1);
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (15,23): error CS9311: 'C3' does not implement interface member 'I1.op_AdditionAssignment(int)'. 'C3.operator +=(int)' cannot implement 'I1.op_AdditionAssignment(int)' because one of them is not an operator.
                // public class C3 : C2, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C3", "I1.op_AdditionAssignment(int)", "C3.operator +=(int)").WithLocation(15, 23)
                );
        }

        [Fact]
        public void CompoundAssignment_01010_Implement_RegularVsOperatorMismatch()
        {
            /*
                public interface I1
                {
                    public void operator+=(int x);
                }

                public class C1 : I1
                {
                    public virtual void op_AdditionAssignment(int x)
                    {
                        System.Console.Write(1);
                    }
                }
            */
            var ilSource = @"
.class interface public auto ansi abstract beforefieldinit I1
{
    .method public hidebysig newslot abstract virtual specialname
        instance void op_AdditionAssignment (int32 x) cil managed 
    {
    }
}

.class public auto ansi beforefieldinit C1
    extends [mscorlib]System.Object
    implements I1
{
    .method public hidebysig newslot virtual 
        instance void op_AdditionAssignment (int32 x) cil managed 
    {
        .maxstack 8

        IL_0000: ldc.i4.1
        IL_0001: call void [mscorlib]System.Console::Write(int32)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
";

            var source1 =
@"
public class C2 : C1, I1
{
}
";
            var compilation1 = CreateCompilationWithIL(source1, ilSource);

            compilation1.VerifyDiagnostics(
                // (2,23): error CS9311: 'C2' does not implement interface member 'I1.operator +=(int)'. 'C1.op_AdditionAssignment(int)' cannot implement 'I1.operator +=(int)' because one of them is not an operator.
                // public class C2 : C1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C2", "I1.operator +=(int)", "C1.op_AdditionAssignment(int)").WithLocation(2, 23)
                );

            var source2 =
@"
class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1.op_AdditionAssignment(1);
        I1 x = c1;
        x += 1;
    }
}
";
            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugExe);

            CompileAndVerify(compilation2, expectedOutput: "11", verify: Verification.Skipped).VerifyDiagnostics();

            var i1M1 = compilation1.GetTypeByMetadataName("I1").GetMembers().Single();
            var c1 = compilation1.GetTypeByMetadataName("C1");

            AssertEx.Equal("C1.op_AdditionAssignment(int)", c1.FindImplementationForInterfaceMember(i1M1).ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_01020_Implement_RegularVsOperatorMismatch()
        {
            /*
                public interface I1
                {
                    public void op_AdditionAssignment(int x);
                }

                public class C1 : I1
                {
                    public virtual void operator+=(int x)
                    {
                        System.Console.Write(1);
                    }
                }
            */
            var ilSource = @"
.class interface public auto ansi abstract beforefieldinit I1
{
    .method public hidebysig newslot abstract virtual 
        instance void op_AdditionAssignment (int32 x) cil managed 
    {
    }
}

.class public auto ansi beforefieldinit C1
    extends [mscorlib]System.Object
    implements I1
{
    .method public hidebysig newslot virtual specialname
        instance void op_AdditionAssignment (int32 x) cil managed 
    {
        .maxstack 8

        IL_0000: ldc.i4.1
        IL_0001: call void [mscorlib]System.Console::Write(int32)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
";

            var source1 =
@"
public class C2 : C1, I1
{
}
";
            var compilation1 = CreateCompilationWithIL(source1, ilSource);

            compilation1.VerifyDiagnostics(
                // (2,23): error CS9311: 'C2' does not implement interface member 'I1.op_AdditionAssignment(int)'. 'C1.operator +=(int)' cannot implement 'I1.op_AdditionAssignment(int)' because one of them is not an operator.
                // public class C2 : C1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch, "I1").WithArguments("C2", "I1.op_AdditionAssignment(int)", "C1.operator +=(int)").WithLocation(2, 23)
                );

            var source2 =
@"
class Program
{
    static void Main()
    {
        var c1 = new C1();
        c1 += 1;
        I1 x = c1;
        x.op_AdditionAssignment(1);
    }
}
";
            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugExe);

            CompileAndVerify(compilation2, expectedOutput: "11", verify: Verification.Skipped).VerifyDiagnostics();

            var i1M1 = compilation1.GetTypeByMetadataName("I1").GetMembers().Single();
            var c1 = compilation1.GetTypeByMetadataName("C1");

            AssertEx.Equal("C1.operator +=(int)", c1.FindImplementationForInterfaceMember(i1M1).ToDisplayString());
        }

        [Fact]
        public void CompoundAssignment_01030_Consumption_Implementation()
        {
            var source = @"
public interface I1
{
    public void operator +=(int x);
}

public class C1 : I1
{
    public void operator +=(int x)
    {
        System.Console.Write(""[C1.operator]"");
    } 
}

public class C2 : I1
{
    void I1.operator +=(int x)
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        I1 x = new C1();
        x += 1;
        x = new C2();
        x += 1;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "[C1.operator][C2.operator]").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01040_Consumption_Overriding()
        {
            var source = @"
public abstract class C1
{
    public abstract void operator +=(int x);
}

public class C2 : C1
{
    public override void operator +=(int x)
    {
        System.Console.Write(""[C2.operator]"");
    } 
}

public class Program
{
    static void Main()
    {
        C1 x = new C2();
        x += 1;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "[C2.operator]").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01050_Shadow_RegularVsOperatorMismatch()
        {
            var source = @"
public class C1
{
    public void operator +=(int x){}
}

public class C2 : C1
{
    public void op_AdditionAssignment(int x){}
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01060_Shadow_RegularVsOperatorMismatch()
        {
            var source = @"
public class C2
{
    public void op_AdditionAssignment(int x){}
}

public class C1 : C2
{
    public void operator +=(int x){}
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyEmitDiagnostics();
        }

        internal static string ToCRefOp(string op)
        {
            return op.Replace("&", "&amp;").Replace("<", "&lt;");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01070_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"()""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"()""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator +=()' that could not be resolved
                // /// See <see cref="operator +=()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + ToCRefOp(op) + @"()").WithArguments("operator " + ToCRefOp(op) + @"()").WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator +=()' that could not be resolved
                // /// See <see cref="C1.operator +=()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator " + ToCRefOp(op) + @"()").WithArguments("operator " + ToCRefOp(op) + @"()").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01080_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(int)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01100_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

            var expected = new[] {
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'operator +='. Assuming 'C1.operator +=(int)', but could have also matched other overloads including 'C1.operator +=(long)'.
                // /// See <see cref="operator +="/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "operator " + ToCRefOp(op)).WithArguments("operator " + ToCRefOp(op), "C1.operator " + op + @"(int)", "C1.operator " + op + @"(long)").WithLocation(3, 20),
                // (12,20): warning CS0419: Ambiguous reference in cref attribute: 'C1.operator +='. Assuming 'C1.operator +=(int)', but could have also matched other overloads including 'C1.operator +=(long)'.
                // /// See <see cref="C1.operator +="/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C1.operator " + ToCRefOp(op)).WithArguments("C1.operator " + ToCRefOp(op), "C1.operator " + op + @"(int)", "C1.operator " + op + @"(long)").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbols = GetReferencedSymbols(crefSyntax, compilation, out var ambiguityWinner, expected[count]);
                AssertEx.Equal("C1.operator " + op + @"(int)", ambiguityWinner.ToDisplayString());
                Assert.Equal(2, actualSymbols.Length);
                AssertEx.Equal("C1.operator " + op + @"(int)", actualSymbols[0].ToDisplayString());
                AssertEx.Equal("C1.operator " + op + @"(long)", actualSymbols[1].ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01120_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"(int)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(long x) {}
    public void operator " + op + @"(int x) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"(int)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(int)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01130_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"(string)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"(string)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator +=(string)' that could not be resolved
                // /// See <see cref="operator +=(string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + ToCRefOp(op) + @"(string)").WithArguments("operator " + ToCRefOp(op) + @"(string)").WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator +=(string)' that could not be resolved
                // /// See <see cref="C1.operator +=(string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator " + ToCRefOp(op) + @"(string)").WithArguments("operator " + ToCRefOp(op) + @"(string)").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01131_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"(int, string)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"(int, string)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator +=(int, string)' that could not be resolved
                // /// See <see cref="operator +=(int, string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + ToCRefOp(op) + @"(int, string)").WithArguments("operator " + ToCRefOp(op) + @"(int, string)").WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator +=(int, string)' that could not be resolved
                // /// See <see cref="C1.operator +=(int, string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator " + ToCRefOp(op) + @"(int, string)").WithArguments("operator " + ToCRefOp(op) + @"(int, string)").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void CompoundAssignment_01140_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public static void " + CompoundAssignmentOperatorName(op, isChecked: false) + @"(int x, long y) {}
}

/// <summary>
/// See <see cref=""C1.operator " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1." + CompoundAssignmentOperatorName(op, isChecked: false) + @"(int, long)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void CompoundAssignment_01160_CRef([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""" + CompoundAssignmentOperatorName(op, isChecked: false) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
}

/// <summary>
/// See <see cref=""C1." + CompoundAssignmentOperatorName(op, isChecked: false) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator " + op + @"(int)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01170_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"()""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
    public void operator checked " + op + @"(long x) {}
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"()""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked +=()' that could not be resolved
                // /// See <see cref="operator checked +=()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + ToCRefOp(op) + @"()").WithArguments("operator checked " + ToCRefOp(op) + @"()").WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator checked +=()' that could not be resolved
                // /// See <see cref="C1.operator checked +=()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + ToCRefOp(op) + @"()").WithArguments("operator checked " + ToCRefOp(op) + @"()").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01180_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
}

/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(int)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01181_CRef_Checked([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked %=' that could not be resolved
                // /// See <see cref="operator checked %="/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + ToCRefOp(op)).WithArguments("operator checked " + ToCRefOp(op)).WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator checked %=' that could not be resolved
                // /// See <see cref="C1.operator checked %="/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + ToCRefOp(op)).WithArguments("operator checked " + ToCRefOp(op)).WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01200_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
    public void operator checked " + op + @"(long x) {}
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

            var expected = new[] {
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'operator checked +='. Assuming 'C1.operator checked +=(int)', but could have also matched other overloads including 'C1.operator checked +=(long)'.
                // /// See <see cref="operator checked +="/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "operator checked " + ToCRefOp(op)).WithArguments("operator checked " + ToCRefOp(op), "C1.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(long)").WithLocation(3, 20),
                // (12,20): warning CS0419: Ambiguous reference in cref attribute: 'C1.operator checked +='. Assuming 'C1.operator checked +=(int)', but could have also matched other overloads including 'C1.operator checked +=(long)'.
                // /// See <see cref="C1.operator +="/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C1.operator checked " + ToCRefOp(op)).WithArguments("C1.operator checked " + ToCRefOp(op), "C1.operator checked " + op + @"(int)", "C1.operator checked " + op + @"(long)").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbols = GetReferencedSymbols(crefSyntax, compilation, out var ambiguityWinner, expected[count]);
                AssertEx.Equal("C1.operator checked " + op + @"(int)", ambiguityWinner.ToDisplayString());
                Assert.Equal(2, actualSymbols.Length);
                AssertEx.Equal("C1.operator checked " + op + @"(int)", actualSymbols[0].ToDisplayString());
                AssertEx.Equal("C1.operator checked " + op + @"(long)", actualSymbols[1].ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01220_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"(int)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(long x) {}
    public void operator checked " + op + @"(long x) {}
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
}

/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"(int)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(int)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01230_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"(string)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
    public void operator checked " + op + @"(long x) {}
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"(string)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked +=(string)' that could not be resolved
                // /// See <see cref="operator checked +=(string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + ToCRefOp(op) + @"(string)").WithArguments("operator checked " + ToCRefOp(op) + @"(string)").WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator checked +=(string)' that could not be resolved
                // /// See <see cref="C1.operator checked +=(string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + ToCRefOp(op) + @"(string)").WithArguments("operator checked " + ToCRefOp(op) + @"(string)").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01231_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"(int, string)""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
    public void operator " + op + @"(long x) {}
    public void operator checked " + op + @"(long x) {}
}

#line 11
/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"(int, string)""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked +=(int, string)' that could not be resolved
                // /// See <see cref="operator checked +=(int, string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + ToCRefOp(op) + @"(int, string)").WithArguments("operator checked " + ToCRefOp(op) + @"(int, string)").WithLocation(3, 20),
                // (12,20): warning CS1574: XML comment has cref attribute 'operator checked +=(int, string)' that could not be resolved
                // /// See <see cref="C1.operator checked +=(int, string)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C1.operator checked " + ToCRefOp(op) + @"(int, string)").WithArguments("operator checked " + ToCRefOp(op) + @"(int, string)").WithLocation(12, 20)
                };

            compilation.VerifyDiagnostics(expected);

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation, expected[count]);
                Assert.Null(actualSymbol);
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void CompoundAssignment_01240_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C1
{
    public static void " + CompoundAssignmentOperatorName(op, isChecked: true) + @"(int x, long y) {}
}

/// <summary>
/// See <see cref=""C1.operator checked " + ToCRefOp(op) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1." + CompoundAssignmentOperatorName(op, isChecked: true) + @"(int, long)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/78103")]
        public void CompoundAssignment_01260_CRef_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""" + CompoundAssignmentOperatorName(op, isChecked: true) + @"""/>.
/// </summary>
class C1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
}

/// <summary>
/// See <see cref=""C1." + CompoundAssignmentOperatorName(op, isChecked: true) + @"""/>.
/// </summary>
class C2
{}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            int count = 0;
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
                AssertEx.Equal("C1.operator checked " + op + @"(int)", actualSymbol.ToDisplayString());
                count++;
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01280_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct S1
{
    public readonly void operator " + op + @"(int x) {}
    public readonly void operator checked " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01281_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct S1
{
    public readonly void operator " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01290_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
readonly struct S1
{
    public void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x) {}
}

readonly struct S2
{
    public readonly void operator " + op + @"(int x) {}
    public readonly void operator checked " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("S2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01291_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
readonly struct S1
{
    public void operator " + op + @"(int x) {}
}

readonly struct S2
{
    public readonly void operator " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("S2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01300_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
struct S1
{
    int F;
    public readonly void operator " + op + @"(int x) { F++; }
    public readonly void operator checked " + op + @"(int x) { F++; }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (5,47): error CS1604: Cannot assign to 'F' because it is read-only
                //     public readonly void operator +=(int x) { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(5, 47),
                // (6,55): error CS1604: Cannot assign to 'F' because it is read-only
                //     public readonly void operator checked +=(int x) { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(6, 55)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01301_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
struct S1
{
    int F;
    public readonly void operator " + op + @"(int x)
    {
        F++;
    }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (7,9): error CS1604: Cannot assign to 'F' because it is read-only
                //         F++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(7, 9)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01310_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
readonly struct S1
{
    public void operator " + op + @"(int x) { this = new S1(); }
    public void operator checked " + op + @"(int x) { this = new S1(); }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (4,38): error CS1604: Cannot assign to 'this' because it is read-only
                //     public void operator +=(int x) { this = new S1(); }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(4, 38),
                // (5,46): error CS1604: Cannot assign to 'this' because it is read-only
                //     public void operator checked +=(int x) { this = new S1(); }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(5, 46)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01311_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
readonly struct S1
{
    public void operator " + op + @"(int x)
    {
        this = new S1();
    }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (6,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this = new S1();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(6, 9)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01330_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
    void operator checked " + op + @"(int x);
}

struct S1 : I1
{
    readonly void I1.operator " + op + @"(int x) {}
    readonly void I1.operator checked " + op + @"(int x) {}
}

readonly struct S2 : I1
{
    readonly void I1.operator " + op + @"(int x) {}
    readonly void I1.operator checked " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("S2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01331_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

struct S1 : I1
{
    readonly void I1.operator " + op + @"(int x) {}
}

readonly struct S2 : I1
{
    readonly void I1.operator " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("S2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01340_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
    void operator checked " + op + @"(int x);
}

readonly struct S1 : I1
{
    void I1.operator " + op + @"(int x) {}
    void I1.operator checked " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01341_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

readonly struct S1 : I1
{
    void I1.operator " + op + @"(int x) {}
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyEmitDiagnostics();

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01350_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
    void operator checked " + op + @"(int x);
}

struct S1 : I1
{
    int F;
    readonly void I1.operator " + op + @"(int x) { F++; }
    readonly void I1.operator checked " + op + @"(int x) { F++; }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (11,43): error CS1604: Cannot assign to 'F' because it is read-only
                //     readonly void I1.operator +=(int x) { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(11, 43),
                // (12,51): error CS1604: Cannot assign to 'F' because it is read-only
                //     readonly void I1.operator checked +=(int x) { F++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(12, 51)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01351_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

struct S1 : I1
{
    int F;
    readonly void I1.operator " + op + @"(int x)
    {
        F++;
    }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (12,9): error CS1604: Cannot assign to 'F' because it is read-only
                //         F++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "F").WithArguments("F").WithLocation(12, 9)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.True(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01360_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
    void operator checked " + op + @"(int x);
}

readonly struct S1 : I1
{
    void I1.operator " + op + @"(int x)
    {
        this = new S1();
    }

    void I1.operator checked " + op + @"(int x)
    {
        this = new S1();
    }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (12,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this = new S1();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(12, 9),
                // (17,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this = new S1();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(17, 9)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01361_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

readonly struct S1 : I1
{
    void I1.operator " + op + @"(int x)
    {
        this = new S1();
    }
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            compilation.VerifyDiagnostics(
                // (11,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this = new S1();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(11, 9)
                );

            foreach (var m in compilation.GetTypeByMetadataName("S1").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.True(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01370_Readonly([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
    void operator checked " + op + @"(int x);
}

interface I2 : I1
{
#line 200
    readonly void I1.operator " + op + @"(int x) {}
    readonly void I1.operator checked " + op + @"(int x) {}
}

class C3 : I1
{
#line 300
    readonly void I1.operator " + op + @"(int x) {}
    readonly void I1.operator checked " + op + @"(int x) {}
}

class C4
{
#line 400
    public readonly void operator " + op + @"(int x) {}
    public readonly void operator checked " + op + @"(int x) {}
}

interface I5
{
#line 500
    readonly void operator " + op + @"(int x);
    readonly void operator checked " + op + @"(int x);
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            compilation.VerifyDiagnostics(
                    // (200,31): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(200, 31),
                    // (201,39): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator checked +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(201, 39),
                    // (300,31): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(300, 31),
                    // (301,39): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator checked +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(301, 39),
                    // (400,35): error CS0106: The modifier 'readonly' is not valid for this item
                    //     public readonly void operator +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(400, 35),
                    // (401,43): error CS0106: The modifier 'readonly' is not valid for this item
                    //     public readonly void operator checked +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(401, 43),
                    // (500,28): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void operator +=(int x);
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(500, 28),
                    // (501,36): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void operator checked +=(int x);
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(501, 36)
                );

            foreach (var m in compilation.GetTypeByMetadataName("I2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("I5").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01371_Readonly([CombinatorialValues("%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
interface I1
{
    void operator " + op + @"(int x);
}

interface I2 : I1
{
#line 200
    readonly void I1.operator " + op + @"(int x) {}
}

class C3 : I1
{
#line 300
    readonly void I1.operator " + op + @"(int x) {}
}

class C4
{
#line 400
    public readonly void operator " + op + @"(int x) {}
}

interface I5
{
#line 500
    readonly void operator " + op + @"(int x);
}
";
            var compilation = CreateCompilation([source, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net90);
            compilation.VerifyDiagnostics(
                    // (200,31): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(200, 31),
                    // (300,31): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void I1.operator +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(300, 31),
                    // (400,35): error CS0106: The modifier 'readonly' is not valid for this item
                    //     public readonly void operator +=(int x) {}
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(400, 35),
                    // (500,28): error CS0106: The modifier 'readonly' is not valid for this item
                    //     readonly void operator +=(int x);
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("readonly").WithLocation(500, 28)
                );

            foreach (var m in compilation.GetTypeByMetadataName("I2").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("C3").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("C4").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }

            foreach (var m in compilation.GetTypeByMetadataName("I5").GetMembers().OfType<MethodSymbol>().Where(m => !m.IsConstructor()))
            {
                Assert.False(m.IsDeclaredReadOnly);
                Assert.False(m.IsEffectivelyReadOnly);
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01380_VisualBasic([CombinatorialValues("+=", "-=", "*=", "/=", "<<=", ">>=")] string op)
        {
            var source1 = @"
public class C1
{
    public void operator " + op + @"(int x) {}
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            var source2 = @"
Public Module Program
    Public Sub Main()
        Dim c1 = New C1()
        c1 " + op + @" 1
    End Sub
End Module
";
            CreateVisualBasicCompilation("Program", source2, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // error BC30452: Operator '+' is not defined for types 'C1' and 'Integer'.
                Diagnostic(30452 /*ERRID.ERR_BinaryOperands3*/, "c1 " + op + @" 1").WithArguments(op[..^1], "C1", "Integer").WithLocation(5, 9)
                );

            string opName = CompoundAssignmentOperatorName(op, isChecked: false);

            var source3 = @"
Public Module Program
    Public Sub Main()
        Dim c1 = New C1()
        c1." + opName + @"(1)
    End Sub
End Module
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public Overloads Sub op_AdditionAssignment(x As Integer)' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public Overloads Sub " + opName + @"(x As Integer)", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01381_VisualBasic([CombinatorialValues("%=", "&=", "|=", "^=", ">>>=")] string op)
        {
            var source1 = @"
public class C1
{
    public void operator " + op + @"(int x) {}
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = CompoundAssignmentOperatorName(op, isChecked: false);

            var source3 = @"
Public Module Program
    Public Sub Main()
        Dim c1 = New C1()
        c1." + opName + @"(1)
    End Sub
End Module
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public Overloads Sub op_BitwiseOrAssignment(x As Integer)' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public Overloads Sub " + opName + @"(x As Integer)", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01390_VisualBasic([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source1 = @"
public interface I1
{
    public void operator " + op + @"(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = CompoundAssignmentOperatorName(op, isChecked: false);

            var source3 = @"
Public Class Program
    Implements I1

    Public Sub " + opName + @"(x As Integer) Implements I1." + opName + @"
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Sub op_BitwiseOrAssignment(x As Integer)' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Sub " + opName + @"(x As Integer)", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01400_VisualBasic_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source1 = @"
public interface I1
{
    public sealed void operator " + op + @"(int x) {}
    public void operator checked " + op + @"(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = CompoundAssignmentOperatorName(op, isChecked: true);

            var source3 = @"
Public Class Program
    Implements I1

    Public Sub " + opName + @"(x As Integer) Implements I1." + opName + @"
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Sub op_BitwiseOrAssignment(x As Integer)' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Sub " + opName + @"(x As Integer)", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01410_VisualBasic([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source1 = @"
public abstract class C1
{
    public abstract void operator " + op + @"(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = CompoundAssignmentOperatorName(op, isChecked: false);

            var source3 = @"
Public Class Program
    Inherits C1

    Public Overrides Sub " + opName + @"(x As Integer)
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public MustOverride Overloads Sub op_BitwiseOrAssignment(x As Integer)' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public MustOverride Overloads Sub " + opName + @"(x As Integer)", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01420_VisualBasic_Checked([CombinatorialValues("+=", "-=", "*=", "/=")] string op)
        {
            var source1 = @"
public abstract class C1
{
    public void operator " + op + @"(int x) {}
    public abstract void operator checked " + op + @"(int x);
}
";
            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute], targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics();

            string opName = CompoundAssignmentOperatorName(op, isChecked: true);

            var source3 = @"
Public Class Program
    Inherits C1

    Public Overrides Sub " + opName + @"(x As Integer)
    End Sub
End Class
";
            CreateVisualBasicCompilation("Program", source3, referencedCompilations: new[] { comp1 }, referencedAssemblies: comp1.References).VerifyDiagnostics(
                // BC37319: 'Public MustOverride Overloads Sub op_BitwiseOrAssignment(x As Integer)' requires compiler feature 'UserDefinedCompoundAssignmentOperators', which is not supported by this version of the Visual Basic compiler.
                Diagnostic(37319 /*ERRID.ERR_UnsupportedCompilerFeature*/, opName).WithArguments("Public MustOverride Overloads Sub " + opName + @"(x As Integer)", "UserDefinedCompoundAssignmentOperators").WithLocation(5, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01430_MetadataValidation([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op, bool isChecked)
        {
            if (isChecked && !CompoundAssignmentOperatorHasCheckedForm(op))
            {
                return;
            }

            string name = CompoundAssignmentOperatorName(op, isChecked);

            var source1 = @"
using System.Runtime.CompilerServices;

public class C1
{
    [SpecialName]
    public void " + name + @"(int x) {}
}

public class C2
{
    [SpecialName]
    public int " + name + @"(int x) => 0; // Not void returning
}

public class C3
{
    [SpecialName]
    public void " + name + @"(int a, int x = 0) {} // Has two parameters
}

public class C4
{
    [SpecialName]
    public void " + name + @"(params int[] x) {} // Has params
}

public class C5
{
    [SpecialName]
    public void " + name + @"(__arglist) {} // Is vararg
}

public class C6
{
    [SpecialName]
    public void " + name + @"<T>(T x) {} // Generic
}

public class C7
{
    [SpecialName]
    public void " + name + @"() {} // Has no parameter
}

public class C8
{
    [SpecialName]
    public void " + name + @"(int a, params int[] x) {} // Has params
}

public class C9
{
    [SpecialName]
    public void " + name + @"(int a, __arglist) {} // Is vararg
}

public class C10
{
    [SpecialName]
    public void " + name + @"(in decimal x) {}
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.EmitToImageReference();

            var comp2 = CreateCompilation("", references: [comp1Ref]);

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C1." + name),
                                 MethodKind.UserDefinedOperator,
                                 "C1.operator " + (isChecked ? "checked " : "") + op + "(int)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C2." + name),
                                 MethodKind.Ordinary,
                                 "C2." + name + "(int)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C3." + name),
                                 MethodKind.Ordinary,
                                 "C3." + name + "(int, int)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C4." + name),
                                 MethodKind.Ordinary,
                                 "C4." + name + "(params int[])");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C5." + name),
                                 MethodKind.Ordinary,
                                 "C5." + name + "(__arglist)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C6." + name),
                                 MethodKind.Ordinary,
                                 "C6." + name + "<T>(T)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C7." + name),
                                 MethodKind.Ordinary,
                                 "C7." + name + "()");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C8." + name),
                                 MethodKind.Ordinary,
                                 "C8." + name + "(int, params int[])");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C9." + name),
                                 MethodKind.Ordinary,
                                 "C9." + name + "(int, __arglist)");

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C10." + name),
                                 MethodKind.UserDefinedOperator,
                                 "C10.operator " + (isChecked ? "checked " : "") + op + "(in decimal)");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01431_MetadataValidation(
            [CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op,
            [CombinatorialValues("ref", "ref readonly", "out")] string refModifier,
            bool isChecked)
        {
            if (isChecked && !CompoundAssignmentOperatorHasCheckedForm(op))
            {
                return;
            }

            string name = CompoundAssignmentOperatorName(op, isChecked);

            var source1 = @"
using System.Runtime.CompilerServices;
public abstract class C1
{
    [SpecialName]
    public abstract void " + name + @"(" + refModifier + @" int x);
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.EmitToImageReference();

            var comp2 = CreateCompilation("", references: [comp1Ref]);

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C1." + name),
                                 MethodKind.Ordinary,
                                 "C1." + name + "(" + refModifier + " int)");
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01440_MetadataValidation(
            [CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op,
            [CombinatorialValues("private", "protected", "private protected", "internal", "internal protected")] string accessibility,
            bool isChecked)
        {
            if (isChecked && !CompoundAssignmentOperatorHasCheckedForm(op))
            {
                return;
            }

            string name = CompoundAssignmentOperatorName(op, isChecked);

            var source1 = @"
using System.Runtime.CompilerServices;
public class C1
{
    [SpecialName]
    " + accessibility + @" void " + name + @"(int x) {}
}
";
            var comp1 = CreateCompilation(source1);
            var comp1Ref = comp1.EmitToImageReference();

            var comp2 = CreateCompilation("", references: [comp1Ref], options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            AssertMetadataSymbol(comp2.GetMember<MethodSymbol>("C1." + name),
                                 MethodKind.Ordinary,
                                 "C1." + name + "(int)");
        }

        [Fact]
        public void CompoundAssignment_01450_Consumption_OverloadResolutionPriority()
        {
            var source1 = @"
using System.Runtime.CompilerServices;

public class C1
{
    public void operator +=(int x)
    {
        System.Console.Write(""[intC1]"");
    } 

    [OverloadResolutionPriority(1)]
    public void operator +=(long x)
    {
        System.Console.Write(""[longC1]"");
    } 
}

public class C2
{
    public static C2 operator +(C2 a, int x)
    {
        System.Console.Write(""[intC2]"");
        return a;
    } 

    [OverloadResolutionPriority(1)]
    public static C2 operator +(C2 a, long x)
    {
        System.Console.Write(""[longC2]"");
        return a;
    } 
}

public class Program
{
    static void Main()
    {
        var x = new C1();
        x += 1;
        var y = new C2();
        y += 1;
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[longC1][longC2]").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01460_Consumption_OverloadResolutionPriority_CheckedContext()
        {
            var source1 = @"
using System.Runtime.CompilerServices;

public class C1
{
    public void operator +=(int x)
    {
        System.Console.Write(""[intC1]"");
    } 

    public void operator checked +=(int x)
    {
        System.Console.Write(""[intC1Checked]"");
    } 

    [OverloadResolutionPriority(1)]
    public void operator +=(long x)
    {
        System.Console.Write(""[longC1]"");
    } 
}

public class C2
{
    public static C2 operator +(C2 a, int x)
    {
        System.Console.Write(""[intC2]"");
        return a;
    } 

    public static C2 operator checked +(C2 a, int x)
    {
        System.Console.Write(""[intC2Checked]"");
        return a;
    } 

    [OverloadResolutionPriority(1)]
    public static C2 operator +(C2 a, long x)
    {
        System.Console.Write(""[longC2]"");
        return a;
    } 
}

public class Program
{
    static void Main()
    {
        checked
        {
            var x = new C1();
            x += 1;
            var y = new C2();
            y += 1;
        }
    } 
}
";

            var comp1 = CreateCompilation([source1, CompilerFeatureRequiredAttribute, OverloadResolutionPriorityAttributeDefinition], options: TestOptions.DebugExe);
            CompileAndVerify(comp1, expectedOutput: "[longC1][longC2]").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01470_InterpolatedStringHandlerArgument()
        {
            var code = @"
using System.Runtime.CompilerServices;

internal struct StructLogger
{
    public void operator +=([InterpolatedStringHandlerArgument("""")] DummyHandler handler)
    {
    }

    public static StructLogger operator -(StructLogger s, [InterpolatedStringHandlerArgument(""s"")] DummyHandler handler)
    {
        return s;
    }

    void Test1()
    {
        this+=$""log:{0}"";
    }
    void Test2()
    {
        this-=$""log:{0}"";
    }
}

[InterpolatedStringHandler]
internal ref struct DummyHandler
{
    public DummyHandler(int literalLength, int formattedCount, StructLogger structLogger)
    {
    }
    public string GetContent() => null;

    public void AppendLiteral(string s) {}
    public void AppendFormatted<T>(T t) {}
}
";

            var comp = CreateCompilation(new[] { code, CompilerFeatureRequiredAttribute, InterpolatedStringHandlerAttribute, InterpolatedStringHandlerArgumentAttribute });

            comp.VerifyDiagnostics(
                // 0.cs(17,15): error CS7036: There is no argument given that corresponds to the required parameter 'structLogger' of 'DummyHandler.DummyHandler(int, int, StructLogger)'
                //         this+=$"log:{0}";
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, @"$""log:{0}""").WithArguments("structLogger", "DummyHandler.DummyHandler(int, int, StructLogger)").WithLocation(17, 15),
                // 0.cs(17,15): error CS1615: Argument 3 may not be passed with the 'out' keyword
                //         this+=$"log:{0}";
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, @"$""log:{0}""").WithArguments("3", "out").WithLocation(17, 15),
                // 0.cs(21,15): error CS7036: There is no argument given that corresponds to the required parameter 'structLogger' of 'DummyHandler.DummyHandler(int, int, StructLogger)'
                //         this-=$"log:{0}";
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, @"$""log:{0}""").WithArguments("structLogger", "DummyHandler.DummyHandler(int, int, StructLogger)").WithLocation(21, 15),
                // 0.cs(21,15): error CS1615: Argument 3 may not be passed with the 'out' keyword
                //         this-=$"log:{0}";
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, @"$""log:{0}""").WithArguments("3", "out").WithLocation(21, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01480_GetOperatorKind([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            SyntaxKind kind = CompoundAssignmentOperatorTokenKind(op);

            string name = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(kind, isChecked: false);
            Assert.Equal(kind, SyntaxFacts.GetOperatorKind(name));

            if (CompoundAssignmentOperatorHasCheckedForm(op))
            {
                name = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(kind, isChecked: true);
                Assert.Equal(kind, SyntaxFacts.GetOperatorKind(name));
            }
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01490_ExpressionTree([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
using System.Linq.Expressions;

public class C1
{
    public void operator" + op + @"(int x)
    {
    } 
}

public class Program
{
    static void Main()
    {
        Expression<System.Action<C1>> x = (c1) => c1 " + op + @" 1;
    } 
}
";

            var comp2 = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp2.VerifyDiagnostics(
                // (15,51): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<System.Action<C1>> x = (c1) => c1 += 1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "c1 " + op + " 1").WithLocation(15, 51)
                );
        }

        [Theory]
        [CombinatorialData]
        public void CompoundAssignment_01500_Partial([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        {
            var source = @"
partial class C1
{
    public partial void operator" + op + @"(int x);

    public partial void M(int x)
    {
    } 

    public partial void M(int x);
}
";

            var comp2 = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp2.VerifyDiagnostics(
                // (4,20): error CS1519: Invalid token 'void' in a member declaration
                //     public partial void operator+=(int x);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "void").WithArguments("void").WithLocation(4, 20),
                // (4,33): error CS9308: User-defined operator 'C1.operator +=(int)' must be declared public
                //     public partial void operator+=(int x);
                Diagnostic(ErrorCode.ERR_OperatorsMustBePublic, op).WithArguments("C1.operator " + op + "(int)").WithLocation(4, 33),
                // (4,33): error CS0501: 'C1.operator +=(int)' must declare a body because it is not marked abstract, extern, or partial
                //     public partial void operator+=(int x);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, op).WithArguments("C1.operator " + op + "(int)").WithLocation(4, 33)
                );
        }

        [Fact]
        public void CompoundAssignment_01510_CopyModifiers()
        {
            /*
                public class C1
                {
                    public virtual void modopt(int64) operator +=(int x) {}
                }
            */
            var ilSource = @"
.class public auto ansi beforefieldinit C1
    extends System.Object
{
    .method public hidebysig specialname newslot virtual 
        instance void modopt(int64) op_AdditionAssignment (int32 x) cil managed 
    {
        // Method begins at RVA 0x2069
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: nop
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
public class C2 : C1
{
    public override void operator +=(int y)
    {
        System.Console.Write(""C2"");
    }

    static void Main()
    {
        C1 c1 = new C2();
        c1 += 1;
    }
}
";
            var compilation1 = CreateCompilationWithIL([source1, CompilerFeatureRequiredAttribute], ilSource, options: TestOptions.DebugExe);
            CompileAndVerify(compilation1, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "C2").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                AssertEx.Equal("void modopt(System.Int64) C2.op_AdditionAssignment(System.Int32 y)", m.GlobalNamespace.GetMember("C2.op_AdditionAssignment").ToTestDisplayString());
            }
        }

        [Fact]
        public void CompoundAssignment_01520_CopyModifiers()
        {
            /*
                public class C1
                {
                    public virtual void operator +=(int modopt(int64) x) {}
                }
            */
            var ilSource = @"
.class public auto ansi beforefieldinit C1
    extends System.Object
{
    .method public hidebysig specialname newslot virtual 
        instance void op_AdditionAssignment (int32 modopt(int64) x) cil managed 
    {
        // Method begins at RVA 0x2069
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: nop
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
public class C2 : C1
{
    public override void operator +=(int y)
    {
        System.Console.Write(""C2"");
    }

    static void Main()
    {
        C1 c1 = new C2();
        c1 += 1;
    }
}
";
            var compilation1 = CreateCompilationWithIL([source1, CompilerFeatureRequiredAttribute], ilSource, options: TestOptions.DebugExe);
            CompileAndVerify(compilation1, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "C2").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                AssertEx.Equal("void C2.op_AdditionAssignment(System.Int32 modopt(System.Int64) y)", m.GlobalNamespace.GetMember("C2.op_AdditionAssignment").ToTestDisplayString());
            }
        }

        [Fact]
        public void CompoundAssignment_01530_NullableMismatchOnOverride()
        {
            var source = @"
#nullable enable

abstract class C1
{
    public abstract void operator +=(string? x);
    public abstract void operator -=(string x);
}

class C2 : C1
{
    public override void operator +=(string x) {}
    public override void operator -=(string? x) {}
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (12,35): warning CS8765: Nullability of type of parameter 'x' doesn't match overridden member (possibly because of nullability attributes).
                //     public override void operator +=(string x) {}
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "+=").WithArguments("x").WithLocation(12, 35)
                );
        }

        [Fact]
        public void CompoundAssignment_01540_Consumption_InCatchFilter()
        {
            var source = @"
public struct C1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 
    public void operator -=(int x)
    {
        System.Console.Write(""-="");
        _F -= x;
    } 

    public static implicit operator bool (C1 x) => x._F % 2 == 0;
}

public class Program
{
    static void Main()
    {
        C1 x = new C1();
        
        try 
        {
            try 
            {
                throw null;
            }
            catch when (x += 3)
            {
                System.Console.Write(""!"");
            }
        }
        catch when (x -= 1)
        {
            System.Console.Write(x._F);
        }
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "+=-=2").VerifyDiagnostics();

            verifier.VerifyIL("Program.Main",
@"
{
  // Code size      104 (0x68)
  .maxstack  2
  .locals init (C1 V_0, //x
                bool V_1,
                C1 V_2,
                bool V_3)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    ""C1""
  .try
  {
    IL_0009:  nop
    .try
    {
      IL_000a:  nop
      IL_000b:  ldnull
      IL_000c:  throw
    }
    filter
    {
      IL_000d:  pop
      IL_000e:  ldloc.0
      IL_000f:  stloc.2
      IL_0010:  ldloca.s   V_2
      IL_0012:  ldc.i4.3
      IL_0013:  call       ""void C1.op_AdditionAssignment(int)""
      IL_0018:  nop
      IL_0019:  ldloc.2
      IL_001a:  stloc.0
      IL_001b:  ldloc.2
      IL_001c:  call       ""bool C1.op_Implicit(C1)""
      IL_0021:  stloc.1
      IL_0022:  ldloc.1
      IL_0023:  ldc.i4.0
      IL_0024:  cgt.un
      IL_0026:  endfilter
    }  // end filter
    {  // handler
      IL_0028:  pop
      IL_0029:  nop
      IL_002a:  ldstr      ""!""
      IL_002f:  call       ""void System.Console.Write(string)""
      IL_0034:  nop
      IL_0035:  nop
      IL_0036:  leave.s    IL_0038
    }
    IL_0038:  nop
    IL_0039:  leave.s    IL_0067
  }
  filter
  {
    IL_003b:  pop
    IL_003c:  ldloc.0
    IL_003d:  stloc.2
    IL_003e:  ldloca.s   V_2
    IL_0040:  ldc.i4.1
    IL_0041:  call       ""void C1.op_SubtractionAssignment(int)""
    IL_0046:  nop
    IL_0047:  ldloc.2
    IL_0048:  stloc.0
    IL_0049:  ldloc.2
    IL_004a:  call       ""bool C1.op_Implicit(C1)""
    IL_004f:  stloc.3
    IL_0050:  ldloc.3
    IL_0051:  ldc.i4.0
    IL_0052:  cgt.un
    IL_0054:  endfilter
  }  // end filter
  {  // handler
    IL_0056:  pop
    IL_0057:  nop
    IL_0058:  ldloc.0
    IL_0059:  ldfld      ""int C1._F""
    IL_005e:  call       ""void System.Console.Write(int)""
    IL_0063:  nop
    IL_0064:  nop
    IL_0065:  leave.s    IL_0067
  }
  IL_0067:  ret
}
");
        }

        [Fact]
        public void CompoundAssignment_01550_Consumption_ConditionalAccessTarget()
        {
            var source = @"
class C1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 
}

class C2
{
    public C1 _F;
}

class Program
{
    static void Main()
    {
        C2 x = new C2() { _F = new C1()};
        
        x?._F += 3; 
        System.Console.Write(x._F._F);

        var result = x?._F += 2;
        System.Console.Write(result._F);
        System.Console.Write(x._F._F);

        x = null;
        x?._F += 1; 
        result = x?._F += 2;
        System.Console.Write(result is null ? ""null"" : ""!"");
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "+=3+=55null").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01560_Consumption_ConditionalAccessTarget()
        {
            var source = @"
struct S1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 
}

class C2
{
    public S1 _F;
}

class Program
{
    static void Main()
    {
        C2 x = new C2();
        
        x?._F += 3; 
        System.Console.Write(x._F._F);

        var result = x?._F += 2;
        System.Console.Write(result.GetValueOrDefault()._F);
        System.Console.Write(x._F._F);

        x = null;
        x?._F += 1; 
        result = x?._F += 2;
        System.Console.Write(result is null ? ""null"" : ""!"");
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "+=3+=55null").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01570_Consumption_ConditionalAccessTarget()
        {
            var source = @"
interface I1
{
    public int F {get;}
    public void operator +=(int x);
}

struct S1 : I1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 

    public int F => _F;
}

struct C1 : I1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 

    public int F => _F;
}

class C2<T> where T : I1, new()
{
    public T _F;
}

class Program
{
    static void Main()
    {
        Test<S1>();
        Test<C1>();
    }

    static void Test<T>() where T : I1, new()
    {
        C2<T> x = new C2<T>() { _F = new T() };
        
        x?._F += 3; 
        System.Console.Write(x._F.F);

        x = null;
        x?._F += 1; 
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "+=3+=3").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01580_Consumption_ConditionalAccessTarget()
        {
            var source = @"
interface I1
{
    public void operator +=(int x);
}

class C2<T> where T : I1
{
    public T _F;
}

class Program
{
    static void Test<T>(C2<T> x) where T : I1
    {
        var result = x?._F += 2;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (16,24): error CS8978: 'T' cannot be made nullable.
                //         var result = x?._F += 2;
                Diagnostic(ErrorCode.ERR_CannotBeMadeNullable, "._F += 2").WithArguments("T").WithLocation(16, 24)
                );
        }

        [Fact]
        public void CompoundAssignment_01590_Consumption_ConditionalAccessTarget()
        {
            var source = @"
using System.Threading.Tasks;

struct S1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 
}

class C2
{
    public S1 _F;
}

class Program
{
    static async Task Main()
    {
        C2 x = new C2();
        
        x?._F += await GetInt(3); 
        System.Console.Write(x._F._F);

        var result = x?._F += await GetInt(2);
        System.Console.Write(result.GetValueOrDefault()._F);
        System.Console.Write(x._F._F);

        x = null;
        x?._F += await GetInt(1); 
        result = x?._F += await GetInt(2);
        System.Console.Write(result is null ? ""null"" : ""!"");
    } 

    static async Task<int> GetInt(int x)
    {
        await Task.Yield();
        return x;
    }
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "+=3+=55null").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01600_Consumption_ConditionalAccessTarget()
        {
            var source = @"
using System.Threading.Tasks;

struct S1
{
    public int _F;
    public void operator +=(int x)
    {
        System.Console.Write(""+="");
        _F += x;
    } 
}

class C2
{
    public S1 _F;
}

class C3
{
    public C2 _F;
}

class Program
{
    static async Task Main()
    {
        C3 x = new C3() { _F = new C2() };
        
        x?._F?._F += await GetInt(3); 
        System.Console.Write(x._F._F._F);

        var result = x?._F?._F += await GetInt(2);
        System.Console.Write(result.GetValueOrDefault()._F);
        System.Console.Write(x._F._F._F);

        x._F = null;
        x?._F?._F += await GetInt(1); 
        result = x?._F?._F += await GetInt(2);
        System.Console.Write(result is null ? ""null"" : ""!"");

        x = null;
        x?._F?._F += await GetInt(1); 
        result = x?._F?._F += await GetInt(2);
        System.Console.Write(result is null ? ""null"" : ""!"");
    } 

    static async Task<int> GetInt(int x)
    {
        await Task.Yield();
        return x;
    }
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute], options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "+=3+=55nullnull").VerifyDiagnostics();
        }

        [Fact]
        public void CompoundAssignment_01610_Consumption_RightIsImplicitObjectCreation()
        {
            var source = @"
struct S1
{
    public void operator +=(S1 y) {}
}

struct S2
{
    public static S2 operator +(S2 x, S2 y) => x;
}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 += new();

        var s2 = new S2();
        s2 += new();
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (17,9): error CS8310: Operator '+=' cannot be applied to operand 'new()'
                //         s1 += new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s1 += new()").WithArguments("+=", "new()").WithLocation(17, 9),
                // (20,9): error CS8310: Operator '+=' cannot be applied to operand 'new()'
                //         s2 += new();
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s2 += new()").WithArguments("+=", "new()").WithLocation(20, 9)
                );
        }

        [Fact]
        public void CompoundAssignment_01620_Consumption_RightIsDefault()
        {
            var source = @"
struct S1
{
    public void operator +=(S1 y) {}
}

struct S2
{
    public static S2 operator +(S2 x, S2 y) => x;
}

class Program
{
    static void Main()
    {
        var s1 = new S1();
        s1 += default;

        var s2 = new S2();
        s2 += default;
    } 
}
";

            var comp = CreateCompilation([source, CompilerFeatureRequiredAttribute]);
            comp.VerifyDiagnostics(
                // (17,9): error CS8310: Operator '+=' cannot be applied to operand 'default'
                //         s1 += default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s1 += default").WithArguments("+=", "default").WithLocation(17, 9),
                // (20,9): error CS8310: Operator '+=' cannot be applied to operand 'default'
                //         s2 += default;
                Diagnostic(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, "s2 += default").WithArguments("+=", "default").WithLocation(20, 9)
                );
        }
    }
}
