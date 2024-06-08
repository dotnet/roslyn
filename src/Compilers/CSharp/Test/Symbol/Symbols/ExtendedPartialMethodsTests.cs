// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExtendedPartialMethodsTests : CSharpTestBase
    {
        [Fact]
        public void NonVoidReturnType_LangVersion()
        {
            const string text = @"
partial class C
{
    internal partial int M1();
    internal partial int M1() => 1;
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,26): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal partial int M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(4, 26),
                // (5,26): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal partial int M1() => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(5, 26)
            );

            comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NonVoidReturnType_NoAccessibility()
        {
            const string text = @"
partial class C
{
    partial int M1();
    partial int M1() => 1;
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,17): error CS8794: Partial method 'C.M1()' must have accessibility modifiers because it has a non-void return type.
                //     partial int M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(4, 17),
                // (5,17): error CS8794: Partial method 'C.M1()' must have accessibility modifiers because it has a non-void return type.
                //     partial int M1() => 1;
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(5, 17)
            );
        }

        [Fact]
        public void NonVoidReturnType_NoImpl()
        {
            const string text = @"
partial class C
{
    private partial int M1();
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,25): error CS8793: Partial method 'C.M1()' must have an implementation part because it has accessibility modifiers.
                //     private partial int M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("C.M1()").WithLocation(4, 25)
            );
        }

        [Fact]
        public void NonVoidReturnType_NoImpl_NoAccessibility()
        {
            const string text = @"
partial class C
{
    partial int M1();
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,17): error CS8794: Partial method 'C.M1()' must have accessibility modifiers because it has a non-void return type.
                //     partial int M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(4, 17)
            );
        }

        [Fact]
        public void NonVoidReturnType_Semantics()
        {
            const string text1 = @"
partial class C
{
    private partial int M1();

    public static void Main()
    {
        System.Console.WriteLine(new C().M1());
    }
}";

            const string text2 = @"
partial class C
{
    private partial int M1() { return 42; }
}
";
            var verifier = CompileAndVerify(new[] { text1, text2 }, parseOptions: TestOptions.RegularWithExtendedPartialMethods, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void OutParam_LangVersion()
        {
            const string text = @"
partial class C
{
    private partial void M1(out int i);
    private partial void M1(out int i) { i = 0; }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,26): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     private partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(4, 26),
                // (5,26): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     private partial void M1(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(5, 26)
            );

            comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void OutParam_NoAccessibility()
        {
            const string text = @"
partial class C
{
    partial void M1(out int i);
    partial void M1(out int i) { i = 0; }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,18): error CS8795: Partial method 'C.M1(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M1").WithArguments("C.M1(out int)").WithLocation(4, 18),
                // (5,18): error CS8795: Partial method 'C.M1(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M1(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M1").WithArguments("C.M1(out int)").WithLocation(5, 18)
            );
        }

        [Fact]
        public void OutParam_NoImpl()
        {
            const string text = @"
partial class C
{
    private partial void M1(out int i);
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,26): error CS8793: Partial method 'C.M1(out int)' must have an implementation part because it has accessibility modifiers.
                //     private partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("C.M1(out int)").WithLocation(4, 26)
            );
        }

        [Fact]
        public void OutParam_NoImpl_NoAccessibility()
        {
            const string text = @"
partial class C
{
    partial void M1(out int i);
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,18): error CS8795: Partial method 'C.M1(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M1").WithArguments("C.M1(out int)").WithLocation(4, 18)
            );
        }

        [Fact]
        public void OutParam_Semantics()
        {
            const string text1 = @"
partial class C
{
    private partial void M1(out int value);

    public static void Main()
    {
        new C().M1(out var value);
        System.Console.WriteLine(value);
    }
}";

            const string text2 = @"
partial class C
{
    private partial void M1(out int value) { value = 42; }
}
";
            var verifier = CompileAndVerify(new[] { text1, text2 }, parseOptions: TestOptions.RegularWithExtendedPartialMethods, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void OutParam_DefiniteAssignment()
        {
            const string text1 = @"
partial class C
{
    private partial void M1(out int value);

    public static void Main()
    {
        int i;
        new C().M1(out i);
        System.Console.Write(i);
    }
}";

            const string text2 = @"
partial class C
{
    private partial void M1(out int value) { value = 42; }
}
";
            var verifier = CompileAndVerify(new[] { text1, text2 }, parseOptions: TestOptions.RegularWithExtendedPartialMethods, expectedOutput: "42");
            verifier.VerifyDiagnostics();
        }

        public static readonly object[][] s_accessMods = new[]
        {
            new[] { "public" },
            new[] { "internal" },
            new[] { "protected" },
            new[] { "private" },
            new[] { "protected internal" },
            new[] { "private protected" }
        };

        [Theory]
        [MemberData(nameof(s_accessMods))]
        public void AccessMod_LangVersion(string mod)
        {
            var text = $@"
partial class C
{{
    {mod} partial void M1();
    {mod} partial void M1() {{ }}
}}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,27): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0"),
                // (5,27): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0")
            );

            comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AccessMod_Mismatch_01()
        {
            var text = @"
partial class C
{
     public partial void M1();
     internal partial void M1() { }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,28): error CS8797: Both partial method declarations must have equivalent accessibility modifiers.
                //      internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "M1").WithLocation(5, 28)
            );
        }

        [Fact]
        public void AccessMod_Mismatch_02()
        {
            var text = @"
partial class C
{
     private partial void M1();
     partial void M1() { }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,19): error CS8797: Both partial member declarations must have identical accessibility modifiers.
                //      partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "M1").WithLocation(5, 19)
            );
        }

        [Fact]
        public void AccessMod_Mismatch_03()
        {
            var text = @"
partial class C
{
     partial void M1();
     private partial void M1() { }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,27): error CS8797: Both partial member declarations must have identical accessibility modifiers.
                //      private partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberAccessibilityDifference, "M1").WithLocation(5, 27)
            );
        }

        [Theory]
        [MemberData(nameof(s_accessMods))]
        public void AccessMod_NoImpl(string mod)
        {
            var text = $@"
partial class C
{{
    {mod} partial void M1();
}}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,27): error CS8795: Partial method C.M1() must have an implementation part because it has accessibility modifiers.
                //     {mod} partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("C.M1()")
            );
        }

        [Fact]
        public void Static_NoImpl()
        {
            const string text1 = @"
partial class C
{
    static partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Simple_NoImpl()
        {
            const string text1 = @"
partial class C
{
    partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Virtual_LangVersion()
        {
            const string text1 = @"
partial class C
{
    internal virtual partial int M1();
    internal virtual partial int M1() => 1;
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,34): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal virtual partial int M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(4, 34),
                // (5,34): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal virtual partial int M1() => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(5, 34)
            );

            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Virtual_NoAccessibility()
        {
            const string text1 = @"
partial class C
{
    virtual partial void M1();
    virtual partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,26): error CS8796: Partial method 'C.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     virtual partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(4, 26),
                // (5,26): error CS8796: Partial method 'C.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     virtual partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(5, 26)
            );
        }

        [Fact]
        public void Virtual_NoImpl()
        {
            const string text1 = @"
partial class C
{
    internal virtual partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,35): error CS8793: Partial method 'C.M1()' must have an implementation part because it has accessibility modifiers.
                //     internal virtual partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("C.M1()").WithLocation(4, 35)
            );
        }

        [Fact]
        public void Virtual_Mismatch_01()
        {
            const string text1 = @"
partial class C
{
    internal virtual partial void M1();
    internal partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,27): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(5, 27)
            );
        }

        [Fact]
        public void Virtual_Mismatch_02()
        {
            const string text1 = @"
partial class C
{
    internal partial void M1();
    internal virtual partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,35): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal virtual partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(5, 35)
            );
        }

        [Fact]
        public void Virtual_Semantics()
        {
            const string text1 = @"
using System;

public partial class C
{
    internal virtual partial int M1();
    internal virtual partial int M1() => 1;
}

public class D : C
{
    internal override int M1() => 2;

    static void UseC(C c)
    {
        Console.Write(c.M1());
    }

    public static void Main()
    {
        UseC(new C());
        UseC(new D());
    }
}";
            CompileAndVerify(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods, expectedOutput: "12").VerifyDiagnostics();
        }

        [Fact]
        public void Virtual_AllowNull_01()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial class C
{
    internal virtual partial void M1([AllowNull] string s1);
    internal virtual partial void M1(string s1) { }
}
class D : C
{
    internal override void M1(string s1) // 1
    {
    }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (12,28): warning CS8765: Nullability of type of parameter 's1' doesn't match overridden member (possibly because of nullability attributes).
                //     internal override void M1(string s1) // 1
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("s1").WithLocation(12, 28)
            );
        }

        [Fact]
        public void Virtual_AllowNull_02()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial class C
{
    internal virtual partial void M1(string s1);
    internal virtual partial void M1([AllowNull] string s1) { }
}
class D : C
{
    internal override void M1(string s1) // 1
    {
    }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (12,28): warning CS8765: Nullability of type of parameter 's1' doesn't match overridden member (possibly because of nullability attributes).
                //     internal override void M1(string s1) // 1
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("s1").WithLocation(12, 28)
            );
        }

        [Fact]
        public void Virtual_AllowNull_03()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial class C
{
    internal virtual partial void M1(string s1);
    internal virtual partial void M1([AllowNull] string s1) { }
}
class D : C
{
    internal override void M1([AllowNull] string s1)
    {
    }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Virtual_AllowNull_04()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial class C
{
    internal virtual partial void M1([AllowNull] string s1);
    internal virtual partial void M1(string s1) { }
}
class D : C
{
    internal override void M1([AllowNull] string s1)
    {
    }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Virtual_AllowNull_05()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial class C
{
    internal virtual partial void M1([AllowNull] string s1, string s2);
    internal virtual partial void M1(string s1, [AllowNull] string s2) { }
}
class D : C
{
    internal override void M1([AllowNull] string s1, [AllowNull] string s2)
    {
    }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Virtual_AllowNull_06()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial class C
{
    internal virtual partial void M1([AllowNull] string s1, string s2);
    internal virtual partial void M1(string s1, [AllowNull] string s2) { }
}
partial class D : C
{
    internal override partial void M1(string s1, [AllowNull] string s2);
    internal override partial void M1([AllowNull] string s1, string s2) { }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Override_LangVersion()
        {
            const string text1 = @"
partial class D
{
    public override partial string ToString();
    public override partial string ToString() => ""hello"";
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,36): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override partial string ToString();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "ToString").WithArguments("extended partial methods", "9.0").WithLocation(4, 36),
                // (5,36): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public override partial string ToString() => "hello";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "ToString").WithArguments("extended partial methods", "9.0").WithLocation(5, 36)
            );
            var method = comp.GetMember<MethodSymbol>("D.ToString");
            Assert.Equal("System.String System.Object.ToString()", method.OverriddenMethod.ToTestDisplayString());

            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
            method = comp.GetMember<MethodSymbol>("D.ToString");
            Assert.Equal("System.String System.Object.ToString()", method.OverriddenMethod.ToTestDisplayString());
        }

        [Fact]
        public void Override_WrongAccessibility()
        {
            const string text1 = @"
partial class D
{
    internal override partial string ToString();
    internal override partial string ToString() => ""hello"";
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,38): error CS0507: 'D.ToString()': cannot change access modifiers when overriding 'public' inherited member 'object.ToString()'
                //     internal override partial string ToString();
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "ToString").WithArguments("D.ToString()", "public", "object.ToString()").WithLocation(4, 38));
            var method = comp.GetMember<MethodSymbol>("D.ToString");
            Assert.Equal("System.String System.Object.ToString()", method.OverriddenMethod.ToTestDisplayString());
        }

        [Fact]
        public void Override_AbstractBase()
        {
            const string text1 = @"
abstract class C
{
    internal abstract void M1();
}

partial class D : C
{
    internal override partial void M1();
    internal override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            var method = comp.GetMember<MethodSymbol>("D.M1");
            Assert.Equal(comp.GetMember<MethodSymbol>("C.M1"), method.OverriddenMethod);
        }

        [Fact]
        public void Override_NoAccessibility()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}

partial class D : C
{
    override partial void M1();
    override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,27): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     override partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(9, 27),
                // (9,27): error CS0507: 'D.M1()': cannot change access modifiers when overriding 'internal' inherited member 'C.M1()'
                //     override partial void M1();
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "M1").WithArguments("D.M1()", "internal", "C.M1()").WithLocation(9, 27),
                // (10,27): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(10, 27)
            );
            var method = comp.GetMember<MethodSymbol>("D.M1");
            Assert.Equal(comp.GetMember<MethodSymbol>("C.M1"), method.OverriddenMethod);
        }

        [Fact]
        public void Override_NoImpl()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}

partial class D : C
{
    internal override partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,36): error CS8793: Partial method 'D.M1()' must have an implementation part because it has accessibility modifiers.
                //     internal override partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("D.M1()").WithLocation(9, 36)
            );
            var method = comp.GetMember<MethodSymbol>("D.M1");
            Assert.Equal(comp.GetMember<MethodSymbol>("C.M1"), method.OverriddenMethod);
        }

        [Fact]
        public void Override_NoImpl_NoAccessibility()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}

partial class D : C
{
    override partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,27): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     override partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(9, 27),
                // (9,27): error CS0507: 'D.M1()': cannot change access modifiers when overriding 'internal' inherited member 'C.M1()'
                //     override partial void M1();
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "M1").WithArguments("D.M1()", "internal", "C.M1()").WithLocation(9, 27)
            );
            var method = comp.GetMember<MethodSymbol>("D.M1");
            Assert.Equal(comp.GetMember<MethodSymbol>("C.M1"), method.OverriddenMethod);
        }

        [Fact]
        public void Override_Mismatch_01()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal override partial void M1();
    internal partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,27): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(9, 27)
            );
        }

        [Fact]
        public void Override_Mismatch_02()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal partial void M1();
    internal override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (8,27): warning CS0114: 'D.M1()' hides inherited member 'C.M1()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     internal partial void M1();
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "M1").WithArguments("D.M1()", "C.M1()").WithLocation(8, 27),
                // (9,36): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(9, 36)
            );
        }

        [Fact]
        public void Override_AllowNull_01()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

class C
{
    internal virtual void M1([AllowNull] string s) { }
}
partial class D : C
{
    internal override partial void M1(string s1); // 1
    internal override partial void M1(string s1) { }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (11,36): warning CS8765: Nullability of type of parameter 's1' doesn't match overridden member (possibly because of nullability attributes).
                //     internal override partial void M1(string s1); // 1
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("s1").WithLocation(11, 36)
            );
        }

        [Fact]
        public void Override_AllowNull_02()
        {
            const string text1 = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

class C
{
    internal virtual void M1([AllowNull] string s1, [AllowNull] string s2) { }
}
partial class D : C
{
    internal override partial void M1([AllowNull] string s1, string s2);
    internal override partial void M1(string s1, [AllowNull] string s2)
    {
        s1.ToString(); // 1
        s2.ToString(); // 2
    }
}";
            var comp = CreateCompilation(new[] { text1, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         s1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(14, 9),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         s2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s2").WithLocation(15, 9));
        }

        [Fact]
        public void Override_GenericBase()
        {
            const string text1 = @"
#nullable enable

class C<T>
{
    internal virtual void M1(T t1) { }
}
partial class D : C<string>
{
    internal override partial void M1(string t1);
    internal override partial void M1(string t1)
    {
    }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Sealed_LangVersion()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal sealed override partial void M1();
    internal sealed override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,43): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal sealed override partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(8, 43),
                // (9,43): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal sealed override partial void M1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(9, 43)
            );
            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Sealed_NoAccessibility()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    sealed override partial void M1();
    sealed override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (8,34): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     sealed override partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(8, 34),
                // (8,34): error CS0507: 'D.M1()': cannot change access modifiers when overriding 'internal' inherited member 'C.M1()'
                //     sealed override partial void M1();
                Diagnostic(ErrorCode.ERR_CantChangeAccessOnOverride, "M1").WithArguments("D.M1()", "internal", "C.M1()").WithLocation(8, 34),
                // (9,34): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     sealed override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(9, 34)
            );
        }

        [Fact]
        public void Sealed_Mismatch_01()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal sealed override partial void M1();
    internal override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,36): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(9, 36)
            );
        }

        [Fact]
        public void Sealed_Mismatch_02()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal override partial void M1();
    internal sealed override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,43): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal sealed override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(9, 43)
            );
        }

        [Fact]
        public void SealedOverride_Reordered()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal override sealed partial void M1();
    internal sealed override partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NewVirtual_Reordered()
        {
            const string text1 = @"
class C
{
    internal virtual void M1() { }
}
partial class D : C
{
    internal new virtual partial void M1();
    internal virtual new partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Extern_LangVersion()
        {
            const string text1 = @"
using System.Runtime.InteropServices;

partial class C
{
    internal static partial void M1();

    [DllImport(""something.dll"")]
    internal static extern partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,34): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal static partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(6, 34),
                // (9,41): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal static extern partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(9, 41));

            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Extern_DuplicateMod()
        {
            const string text1 = @"
using System.Runtime.InteropServices;

partial class C
{
    internal static extern partial void M1();

    [DllImport(""something.dll"")]
    internal static extern partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,41): error CS0759: No defining declaration found for implementing declaration of partial method 'C.M1()'
                //     internal static extern partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "M1").WithArguments("C.M1()").WithLocation(6, 41),
                // (9,41): error CS0757: A partial method may not have multiple implementing declarations
                //     internal static extern partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyOneActual, "M1").WithLocation(9, 41),
                // (9,41): error CS0111: Type 'C' already defines a member called 'M1' with the same parameter types
                //     internal static extern partial void M1();
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M1").WithArguments("M1", "C").WithLocation(9, 41));
        }

        [Fact]
        public void Extern_NoAccessibility()
        {
            const string text1 = @"
using System.Runtime.InteropServices;

partial class C
{
    static partial void M1();

    [DllImport(""something.dll"")]
    static extern partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,32): error CS8796: Partial method 'C.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     static extern partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(9, 32)
            );
        }

        [Fact]
        public void Extern_Symbols()
        {
            const string text1 = @"
using System.Runtime.InteropServices;

public partial class C
{
    public static partial void M1();

    [DllImport(""something.dll"")]
    public static extern partial void M1();

    public static void M2() { M1(); }
}";

            const string text2 = @"
using System.Runtime.InteropServices;

public partial class C
{
    [DllImport(""something.dll"")]
    public static partial void M1();

    public static extern partial void M1();

    public static void M2() { M1(); }
}";
            const string expectedIL = @"
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       ""void C.M1()""
  IL_0005:  ret
}";

            var verifier = CompileAndVerify(
                text1,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M2", expectedIL);

            verifier = CompileAndVerify(
                text2,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M2", expectedIL);

            static void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = type.GetMember<MethodSymbol>("M1");

                // note: we're opting to give the same answer for 'IsExtern' on both partial method declarations 
                // because 'IsExtern' is true when the method is round tripped from metadata.
                Assert.True(method.IsExtern);
                if (method.PartialImplementationPart is MethodSymbol implementation)
                {
                    Assert.True(method.IsPartialDefinition());
                    Assert.True(implementation.IsExtern);
                }

                var importData = method.GetDllImportData();
                Assert.NotNull(importData);
                Assert.Equal("something.dll", importData.ModuleName);
                Assert.Equal("M1", importData.EntryPointName);
                Assert.Equal(CharSet.None, importData.CharacterSet);
                Assert.False(importData.SetLastError);
                Assert.False(importData.ExactSpelling);
                Assert.Equal(MethodImplAttributes.PreserveSig, method.ImplementationAttributes);
                Assert.Equal(CallingConvention.Winapi, importData.CallingConvention);
                Assert.Null(importData.BestFitMapping);
                Assert.Null(importData.ThrowOnUnmappableCharacter);
            }
        }

        [Fact]
        public void Extern_Symbols_NoDllImport()
        {
            const string text1 = @"
public partial class C
{
    public static partial void M1();

    public static extern partial void M1();

    public static void M2() { M1(); }
}";

            const string text2 = @"
public partial class C
{
    public static partial void M1();

    public static extern partial void M1();

    public static void M2() { M1(); }
}";
            const string expectedIL = @"
{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       ""void C.M1()""
  IL_0005:  ret
}";

            var verifier = CompileAndVerify(
                text1,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                sourceSymbolValidator: module => validator(module, isSource: true),
                symbolValidator: module => validator(module, isSource: false),
                // PEVerify fails when extern methods lack an implementation
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M2", expectedIL);

            verifier = CompileAndVerify(
                text2,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                sourceSymbolValidator: module => validator(module, isSource: true),
                symbolValidator: module => validator(module, isSource: false),
                // PEVerify fails when extern methods lack an implementation
                verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M2", expectedIL);

            static void validator(ModuleSymbol module, bool isSource)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                var method = type.GetMember<MethodSymbol>("M1");

                // 'IsExtern' is not round tripped when DllImport is missing
                Assert.Equal(isSource, method.IsExtern);
                if (method.PartialImplementationPart is MethodSymbol implementation)
                {
                    Assert.True(method.IsPartialDefinition());
                    Assert.Equal(isSource, implementation.IsExtern);
                }

                var importData = method.GetDllImportData();
                Assert.Null(importData);
            }
        }

        [Fact]
        public void Async_01()
        {
            const string text = @"
partial class C
{
    partial void M1();
    async partial void M1() { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,24): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async partial void M1() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M1").WithLocation(5, 24));

            var method = (MethodSymbol)comp.GetMembers("C.M1")[0];
            Assert.True(method.IsPartialDefinition());
            Assert.False(method.IsAsync);
            Assert.True(method.PartialImplementationPart.IsAsync);
        }

        [Fact]
        public void Async_02()
        {
            const string text = @"
using System.Threading.Tasks;

partial class C
{
    private partial Task M1();
    private async partial Task M1() { }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (7,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     private async partial Task M1() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M1").WithLocation(7, 32));

            var method = (MethodSymbol)comp.GetMembers("C.M1")[0];
            Assert.True(method.IsPartialDefinition());
            Assert.False(method.IsAsync);
            Assert.True(method.PartialImplementationPart.IsAsync);
        }

        [Fact]
        public void Async_03()
        {
            const string text = @"
using System.Threading.Tasks;
using System;

partial class C
{
    private static async Task CompletedTask() { }

    private static partial Task<int> M1();
    private static async partial Task<int> M1() { await CompletedTask(); return 1; }

    public static async Task Main()
    {
        Console.Write(await M1());
    }
}
";
            var verifier = CompileAndVerify(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods, expectedOutput: "1");
            verifier.VerifyDiagnostics(
                // (7,31): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     private static async Task CompletedTask() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "CompletedTask").WithLocation(7, 31));

            var method = (MethodSymbol)verifier.Compilation.GetMembers("C.M1")[0];
            Assert.True(method.IsPartialDefinition());
            Assert.False(method.IsAsync);
            Assert.True(method.PartialImplementationPart.IsAsync);
        }

        [Fact]
        public void Async_04()
        {
            const string text = @"
using System.Threading.Tasks;

partial class C
{
    internal async partial Task M();
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,33): error CS8793: Partial method 'C.M()' must have an implementation part because it has accessibility modifiers.
                //     internal async partial Task M();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("C.M()").WithLocation(6, 33),
                // (6,33): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     internal async partial Task M();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M").WithLocation(6, 33));
        }

        [Fact]
        public void Async_05()
        {
            const string text = @"
using System.Threading.Tasks;

partial class C
{
    internal async partial Task M();
    internal async partial Task M()
    {
        await Task.Yield();
    }
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,33): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     internal async partial Task M();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M").WithLocation(6, 33));
        }

        [Fact]
        public void Async_06()
        {
            const string text = @"
using System.Threading.Tasks;

partial class C
{
    internal async partial Task M();
    internal partial Task M()
    {
        await Task.Yield();
    }
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,33): error CS1994: The 'async' modifier can only be used in methods that have a body.
                //     internal async partial Task M();
                Diagnostic(ErrorCode.ERR_BadAsyncLacksBody, "M").WithLocation(6, 33),
                // (7,27): error CS0161: 'C.M()': not all code paths return a value
                //     internal partial Task M()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(7, 27),
                // (9,9): error CS4032: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task<Task>'.
                //         await Task.Yield();
                Diagnostic(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, "await Task.Yield()").WithArguments("System.Threading.Tasks.Task").WithLocation(9, 9));
        }

        [Fact]
        public void Async_07()
        {
            const string text = @"
using System.Threading.Tasks;

partial class C
{
    partial Task M();
    async partial Task M()
    {
        await Task.Yield();
    }
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,18): error CS8794: Partial method 'C.M()' must have accessibility modifiers because it has a non-void return type.
                //     partial Task M();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("C.M()").WithLocation(6, 18),
                // (7,24): error CS8794: Partial method 'C.M()' must have accessibility modifiers because it has a non-void return type.
                //     async partial Task M()
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("C.M()").WithLocation(7, 24));
        }

        [Fact]
        public void New_LangVersion()
        {
            const string text1 = @"
class C
{
    internal void M1() { }
}

partial class D : C
{
    internal new partial void M1();
    internal new partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,31): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal new partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(9, 31),
                // (10,31): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     internal new partial void M1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M1").WithArguments("extended partial methods", "9.0").WithLocation(10, 31));

            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void New_NoImpl()
        {
            const string text1 = @"
class C
{
    internal void M1() { }
}

partial class D : C
{
    internal new partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,31): error CS8793: Partial method 'D.M1()' must have an implementation part because it has accessibility modifiers.
                //     internal new partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("D.M1()").WithLocation(9, 31)
            );
        }

        [Fact]
        public void New_NoAccessibility()
        {
            const string text1 = @"
class C
{
    internal void M1() { }
}

partial class D : C
{
    new partial void M1();
    new partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,22): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     new partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(9, 22),
                // (10,22): error CS8796: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     new partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(10, 22)
            );
        }

        [Fact]
        public void New_Mismatch_01()
        {
            const string text1 = @"
class C
{
    internal void M1() { }
}

partial class D : C
{
    internal new partial void M1();
    internal partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (10,27): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(10, 27)
            );
        }

        [Fact]
        public void New_Mismatch_02()
        {
            const string text1 = @"
class C
{
    internal void M1() { }
}

partial class D : C
{
    internal partial void M1();
    internal new partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,27): warning CS0108: 'D.M1()' hides inherited member 'C.M1()'. Use the new keyword if hiding was intended.
                //     internal partial void M1();
                Diagnostic(ErrorCode.WRN_NewRequired, "M1").WithArguments("D.M1()", "C.M1()").WithLocation(9, 27),
                // (10,31): error CS8798: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal new partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMemberExtendedModDifference, "M1").WithLocation(10, 31)
            );
        }

        [Fact]
        public void InterfaceImpl_01()
        {
            const string text = @"
interface I
{
    void M();
}

partial class C
{
    public partial void M();
}

partial class C : I
{
    public partial void M()
    {
        System.Console.Write(1);
    }

    static void Main()
    {
        I i = new C();
        i.M();
    }
}
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                options: TestOptions.DebugExe,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void InterfaceImpl_02()
        {
            const string text = @"
interface I
{
    void M();
}

partial class C : I
{
    public partial void M();
}

partial class C
{
    public partial void M()
    {
        System.Console.Write(1);
    }

    static void Main()
    {
        I i = new C();
        i.M();
    }
}
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                options: TestOptions.DebugExe,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void InterfaceImpl_03()
        {
            const string text = @"
#nullable enable

using System.Diagnostics.CodeAnalysis;

interface I
{
    void M([AllowNull] string s1);
}

partial class C : I
{
    public partial void M(string s1); // 1
}

partial class C
{
    public partial void M(string s1)
    {
        s1.ToString();
    }
}
";
            var comp = CreateCompilation(new[] { text, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (13,25): warning CS8767: Nullability of reference types in type of parameter 's1' of 'void C.M(string s1)' doesn't match implicitly implemented member 'void I.M(string s1)' (possibly because of nullability attributes).
                //     public partial void M(string s1);
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation, "M").WithArguments("s1", "void C.M(string s1)", "void I.M(string s1)").WithLocation(13, 25));
        }

        [Fact]
        public void InterfaceImpl_04()
        {
            const string text = @"
#nullable enable

using System.Diagnostics.CodeAnalysis;

interface I
{
    void M([AllowNull] string s1, [AllowNull] string s2);
}

partial class C : I
{
    public partial void M([AllowNull] string s1, string s2);
}

partial class C
{
    public partial void M(string s1, [AllowNull] string s2)
    {
        s1.ToString(); // 1
        s2.ToString(); // 2
    }
}
";
            var comp = CreateCompilation(new[] { text, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (20,9): warning CS8602: Dereference of a possibly null reference.
                //         s1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(20, 9),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         s2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s2").WithLocation(21, 9));
        }

        [Fact]
        public void InterfaceImpl_05()
        {
            const string text = @"
#nullable enable

using System.Diagnostics.CodeAnalysis;

interface I
{
    void M([AllowNull] string s1);
}

partial class C : I
{
    public partial void M([AllowNull] string s1);
}

partial class C
{
    public partial void M([AllowNull] string s1) // 1
    {
        s1.ToString(); // 2
    }
}
";
            var comp = CreateCompilation(new[] { text, AllowNullAttributeDefinition }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (13,28): error CS0579: Duplicate 'AllowNull' attribute
                //     public partial void M([AllowNull] string s1);
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "AllowNull").WithArguments("AllowNull").WithLocation(13, 28),
                // (20,9): warning CS8602: Dereference of a possibly null reference.
                //         s1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(20, 9));
        }

        [Fact]
        public void DefaultInterfaceImpl_NoImpl()
        {
            const string text = @"
#nullable enable

partial interface I
{
    internal partial void M(string s1);
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,27): error CS8793: Partial method 'I.M(string)' must have an implementation part because it has accessibility modifiers.
                //     internal partial void M(string s1);
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("I.M(string)").WithLocation(6, 27)
            );
        }

        [Fact]
        public void DefaultInterfaceImpl_01()
        {
            const string text = @"
#nullable enable

partial interface I
{
    internal partial void M(string s1);
    internal partial void M(string s1) { }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
            );
        }

        [Fact]
        public void DefaultInterfaceImpl_AllowNull_01()
        {
            const string text = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial interface I
{
    public partial void M([AllowNull] string s1);
    public partial void M(string s1)
    {
        _ = s1.ToString(); // 1
    }
}

class C : I
{
    public void M(string s1) { }
}
";
            // note that I.M is not virtual so C.M does not implement it
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(10, 13)
            );
        }

        [Fact]
        public void DefaultInterfaceImpl_AllowNull_02()
        {
            const string text = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial interface I
{
    public virtual partial void M([AllowNull] string s1, string s2);
    public virtual partial void M(string s1, [AllowNull] string s2)
    {
        _ = s1.ToString(); // 1
        _ = s2.ToString(); // 2
    }
}

class C : I
{
    void I.M(string s1, string s2) { } // 3, 4
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(10, 13),
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s2").WithLocation(11, 13),
                // (17,12): warning CS8769: Nullability of reference types in type of parameter 's1' doesn't match implemented member 'void I.M(string s1, string s2)' (possibly because of nullability attributes).
                //     void I.M(string s1, string s2) { } // 3, 4
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation, "M").WithArguments("s1", "void I.M(string s1, string s2)").WithLocation(17, 12),
                // (17,12): warning CS8769: Nullability of reference types in type of parameter 's2' doesn't match implemented member 'void I.M(string s1, string s2)' (possibly because of nullability attributes).
                //     void I.M(string s1, string s2) { } // 3, 4
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation, "M").WithArguments("s2", "void I.M(string s1, string s2)").WithLocation(17, 12)
            );
        }

        [Fact]
        public void DefaultInterfaceImpl_AllowNull_03()
        {
            const string text = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

partial interface I
{
    public virtual partial void M([AllowNull] string s1, string s2);
    public virtual partial void M(string s1, [AllowNull] string s2)
    {
        _ = s1.ToString(); // 1
        _ = s2.ToString(); // 2
    }
}

partial class C : I
{
    public partial void M(string s1, [AllowNull] string s2);
    public partial void M([AllowNull] string s1, string s2)
    {
        _ = s1.ToString(); // 3
        _ = s2.ToString(); // 4
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s1.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(10, 13),
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s2").WithLocation(11, 13),
                // (20,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s1.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(20, 13),
                // (21,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = s2.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s2").WithLocation(21, 13)
            );
        }

        [Fact]
        public void ExplicitInterfaceImpl_01()
        {
            const string text = @"
interface I
{
    void M();
}

partial class C : I
{
    partial void I.M(); // 1
    partial void I.M() { } // 2
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (9,20): error CS0754: A partial member may not explicitly implement an interface member
                //     partial void I.M(); // 1
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "M").WithLocation(9, 20),
                // (10,20): error CS0754: A partial member may not explicitly implement an interface member
                //     partial void I.M() { } // 2
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "M").WithLocation(10, 20));
        }

        [Fact]
        public void ExplicitInterfaceImpl_02()
        {
            const string text = @"
interface I
{
    int M();
}

partial class C : I
{
    partial int I.M();
    partial int I.M() => 42;
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,19): error CS0754: A partial member may not explicitly implement an interface member
                //     partial int I.M();
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "M").WithLocation(9, 19),
                // (9,19): error CS8794: Partial method 'C.I.M()' must have accessibility modifiers because it has a non-void return type.
                //     partial int I.M();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("C.I.M()").WithLocation(9, 19),
                // (10,19): error CS0754: A partial member may not explicitly implement an interface member
                //     partial int I.M() => 42;
                Diagnostic(ErrorCode.ERR_PartialMemberNotExplicit, "M").WithLocation(10, 19),
                // (10,19): error CS8794: Partial method 'C.I.M()' must have accessibility modifiers because it has a non-void return type.
                //     partial int I.M() => 42;
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("C.I.M()").WithLocation(10, 19));
        }

        [Fact]
        public void InsideStruct()
        {
            const string text = @"
partial struct S
{
    public partial int M();
    public partial int M() => 42;
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,24): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public partial int M();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("extended partial methods", "9.0").WithLocation(4, 24),
                // (5,24): error CS8400: Feature 'extended partial methods' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     public partial int M() => 42;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "M").WithArguments("extended partial methods", "9.0").WithLocation(5, 24));

            comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReturnAttribute_01()
        {
            const string text = @"
class Attr1 : System.Attribute { }
class Attr2 : System.Attribute { }

public partial class C
{
    [return: Attr1]
    public partial string M();

    [return: Attr2]
    public partial string M() => ""hello"";
}
";
            var expectedAttributeNames = new[] { "Attr1", "Attr2" };

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, symbolValidator: validator);

            var definitionPart = comp.GetMember<MethodSymbol>("C.M");
            Assert.True(definitionPart.IsPartialDefinition());
            Assert.Equal(expectedAttributeNames, GetAttributeNames(definitionPart.GetReturnTypeAttributes()));

            var implementationPart = definitionPart.PartialImplementationPart;
            Assert.NotNull(implementationPart);
            Assert.True(implementationPart.IsPartialImplementation());
            Assert.Equal(expectedAttributeNames, GetAttributeNames(implementationPart.GetReturnTypeAttributes()));

            void validator(ModuleSymbol module)
            {
                var method = module.ContainingAssembly
                    .GetTypeByMetadataName("C")
                    .GetMember<MethodSymbol>("M");

                Assert.Equal(expectedAttributeNames, GetAttributeNames(method.GetReturnTypeAttributes()));
            }
        }

        [Fact]
        public void ConflictingOverloads_RefOut_01()
        {
            const string text = @"
partial class Program
{
    internal static partial void M(ref object o);
    internal static partial void M(out object o);
    internal static partial void M(out object o) { o = null; }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,34): error CS8793: Partial method 'Program.M(ref object)' must have an implementation part because it has accessibility modifiers.
                //     internal static partial void M(ref object o);
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("Program.M(ref object)").WithLocation(4, 34),
                // (5,34): error CS0663: 'Program' cannot define an overloaded method that differs only on parameter modifiers 'out' and 'ref'
                //     internal static partial void M(out object o);
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("Program", "method", "out", "ref").WithLocation(5, 34));
        }

        [Fact]
        public void ConflictingOverloads_RefOut_02()
        {
            const string text = @"
partial class Program
{
    internal static partial void M(ref object o);
    internal static partial void M(ref object o) { }
    internal static partial void M(out object o);
    internal static partial void M(out object o) { o = null; }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,34): error CS0663: 'Program' cannot define an overloaded method that differs only on parameter modifiers 'out' and 'ref'
                //     internal static partial void M(out object o);
                Diagnostic(ErrorCode.ERR_OverloadRefKind, "M").WithArguments("Program", "method", "out", "ref").WithLocation(6, 34));
        }

        [Fact]
        public void RefReturn()
        {
            const string text = @"
using System;

partial class Program
{
    static int i = 1;
    private static partial ref int M();
    private static partial ref int M() => ref i;

    static void Main()
    {
        ref int local = ref M();
        Console.Write(local);
        i++;
        Console.Write(local);
    }
}
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void PrivateProtectedAccessibility_01()
        {
            const string text1 = @"
using System;

public partial class Base
{
    private protected static partial void M1();
    private protected static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Derived : Base
{
    static void Main()
    {
        M1();
    }
}";
            var verifier = CompileAndVerify(
                new[] { text1, text2 },
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var comp1 = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference reference)
            {
                var comp2 = CreateCompilation(
                    text2,
                    references: new[] { reference },
                    parseOptions: TestOptions.RegularWithExtendedPartialMethods);
                comp2.VerifyDiagnostics(
                    // (6,9): error CS0122: 'Base.M1()' is inaccessible due to its protection level
                    //         M1();
                    Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("Base.M1()").WithLocation(6, 9));
            }
        }

        [Fact]
        public void PrivateProtectedAccessibility_02()
        {
            const string text1 = @"
using System;

partial class C
{
    private protected static partial void M1();
    private protected static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Program
{
    static void Main()
    {
        C.M1(); // 1
    }
}";
            var comp = CreateCompilation(new[] { text1, text2 }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,11): error CS0122: 'C.M1()' is inaccessible due to its protection level
                //         C.M1(); // 1
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("C.M1()").WithLocation(6, 11));
        }

        [Fact]
        public void ProtectedAccessibility_01()
        {
            const string text1 = @"
using System;

partial class Base
{
    protected static partial void M1();
    protected static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Derived : Base
{
    static void Main()
    {
        M1();
    }
}";
            var verifier = CompileAndVerify(
                new[] { text1, text2 },
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ProtectedAccessibility_02()
        {
            const string text1 = @"
using System;

partial class C
{
    protected static partial void M1();
    protected static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Program
{
    static void Main()
    {
        C.M1(); // 1
    }
}";
            var comp = CreateCompilation(new[] { text1, text2 }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,11): error CS0122: 'C.M1()' is inaccessible due to its protection level
                //         C.M1(); // 1
                Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("C.M1()").WithLocation(6, 11));
        }

        [Fact]
        public void InternalAccessibility_01()
        {
            const string text1 = @"
using System;

internal partial class C
{
    internal static partial void M1();
    internal static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Program
{
    static void Main()
    {
        C.M1();
    }
}";
            var verifier = CompileAndVerify(
                new[] { text1, text2 },
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void InternalAccessibility_02()
        {
            const string text1 = @"
using System;

internal partial class C
{
    internal static partial void M1();
    internal static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Program
{
    static void Main()
    {
        C.M1(); // 1
    }
}";
            var comp1 = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp1.VerifyDiagnostics();
            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference reference)
            {
                var comp2 = CreateCompilation(
                    text2,
                    references: new[] { comp1.ToMetadataReference() },
                    parseOptions: TestOptions.RegularWithExtendedPartialMethods);
                comp2.VerifyDiagnostics(
                    // (6,9): error CS0122: 'C' is inaccessible due to its protection level
                    //         C.M1(); // 1
                    Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("C").WithLocation(6, 9));
            }
        }

        [Fact]
        public void ProtectedInternalAccessibility_01()
        {
            const string text1 = @"
using System;

public partial class C
{
    protected internal static partial void M1();
    protected internal static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Program
{
    static void Main()
    {
        C.M1();
    }
}";
            var verifier = CompileAndVerify(
                new[] { text1, text2 },
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var comp1 = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference reference)
            {
                var comp2 = CreateCompilation(
                    text2,
                    references: new[] { reference },
                    parseOptions: TestOptions.RegularWithExtendedPartialMethods);
                comp2.VerifyDiagnostics(
                    // (6,11): error CS0122: 'C.M1()' is inaccessible due to its protection level
                    //         C.M1();
                    Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("C.M1()").WithLocation(6, 11));
            }
        }

        [Fact]
        public void ProtectedInternalAccessibility_02()
        {
            const string text1 = @"
using System;

public partial class Base
{
    protected internal static partial void M1();
    protected internal static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Derived : Base
{
    static void Main()
    {
        M1();
    }
}";
            var verifier = CompileAndVerify(
                new[] { text1, text2 },
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var comp1 = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference reference)
            {
                var verifier = CompileAndVerify(
                    text2,
                    references: new[] { reference },
                    parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                    expectedOutput: "1");
                verifier.VerifyDiagnostics();
            }
        }

        [Fact]
        public void PublicAccessibility()
        {
            const string text1 = @"
using System;

public partial class C
{
    public static partial void M1();
    public static partial void M1() { Console.Write(1); }
}";

            const string text2 = @"
class Program
{
    static void Main()
    {
        C.M1();
    }
}";
            var verifier = CompileAndVerify(
                new[] { text1, text2 },
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var comp1 = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference reference)
            {
                var verifier = CompileAndVerify(
                    text2,
                    references: new[] { reference },
                    parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                    expectedOutput: "1");
                verifier.VerifyDiagnostics();
            }
        }

        [Fact]
        public void EntryPoint_01()
        {
            const string text1 = @"
using System.Threading.Tasks;

public partial class C
{
    public static partial Task Main();
}";
            var comp1 = CreateCompilation(
                text1,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                options: TestOptions.DebugExe);
            comp1.VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (6,32): error CS8793: Partial method 'C.Main()' must have an implementation part because it has accessibility modifiers.
                //     public static partial Task Main();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "Main").WithArguments("C.Main()").WithLocation(6, 32));
        }

        [Fact]
        public void EntryPoint_02()
        {
            const string text1 = @"
using System.Threading.Tasks;
using System;

public partial class C
{
    public static partial Task Main();
    public static partial async Task Main()
    {
        Console.Write(1);
    }
}";
            var verifier = CompileAndVerify(
                text1,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                options: TestOptions.DebugExe,
                expectedOutput: "1");
            verifier.VerifyDiagnostics(
                // (8,38): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static partial async Task Main()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(8, 38));
        }

        [Fact]
        public void EntryPoint_03()
        {
            const string text1 = @"
using System.Threading.Tasks;

public partial class C
{
    public static partial Task<int> Main();
}";
            var comp1 = CreateCompilation(
                text1,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                options: TestOptions.DebugExe);
            comp1.VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (6,37): error CS8793: Partial method 'C.Main()' must have an implementation part because it has accessibility modifiers.
                //     public static partial Task<int> Main();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "Main").WithArguments("C.Main()").WithLocation(6, 37));
        }

        [Fact]
        public void EntryPoint_04()
        {
            const string text1 = @"
using System.Threading.Tasks;
using System;

public partial class C
{
    public static partial Task<int> Main();
    public static partial async Task<int> Main()
    {
        Console.Write(1);
        return 1;
    }
}";
            var verifier = CompileAndVerify(
                text1,
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                options: TestOptions.DebugExe,
                expectedOutput: "1");
            verifier.VerifyDiagnostics(
                // (8,43): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static partial async Task<int> Main()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Main").WithLocation(8, 43));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Override_CustomModifiers()
        {
            var source1 = @"
public class C
{
    public virtual void M(in object x) { }
}";
            var comp1 = CreateCompilation(source1, assemblyName: "C");
            comp1.VerifyDiagnostics();
            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference reference)
            {

                var source2 = @"
public partial class D : C
{
    public override partial void M(in object x);
    public override partial void M(in object x) { x.ToString(); }
}
";
                var verifier = CompileAndVerify(source2, references: new[] { reference }, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
                verifier.VerifyTypeIL("D", @"
.class public auto ansi beforefieldinit D
	extends [C]C
{
	// Methods
	.method public hidebysig virtual 
		instance void M (
			[in] object& modreq([netstandard]System.Runtime.InteropServices.InAttribute) x
		) cil managed 
	{
		.param [1]
			.custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
				01 00 00 00
			)
		// Method begins at RVA 0x2067
		// Code size 9 (0x9)
		.maxstack 8

		IL_0000: ldarg.1
		IL_0001: ldind.ref
		IL_0002: callvirt instance string [netstandard]System.Object::ToString()
		IL_0007: pop
		IL_0008: ret
	} // end of method D::M

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2071
		// Code size 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [C]C::.ctor()
		IL_0006: ret
	} // end of method D::.ctor

} // end of class D
");
            }
        }

        [Fact]
        public void InterfaceImpl_InThunk()
        {
            var source = @"
public interface I
{
    void M(in object obj);
}

public partial class C : I
{
    public partial void M(in object obj);
    public partial void M(in object obj) { }
}
";
            var verifier = CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: TestOptions.RegularWithExtendedPartialMethods,
                symbolValidator: verify);
            verifier.VerifyDiagnostics();

            void verify(ModuleSymbol module)
            {
                Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                var cType = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.NotNull(cType.GetMethod("M"));
                Assert.NotNull(cType.GetMethod("I.M"));
            }
        }

        [Fact]
        public void ConsumeExtendedPartialFromVB()
        {
            var csharp = @"
public partial class C
{
    public virtual partial int M1();
    public virtual partial int M1() => 42;
}";
            var comp = CreateCompilation(csharp, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            var vb = @"
Public Class D
    Inherits C

    Public Overrides Function M1() As Integer
        Return 123
    End Function
End Class
";

            var vbComp = CreateVisualBasicCompilation(vb, referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Standard).Add(comp.EmitToImageReference()));
            vbComp.VerifyDiagnostics();
        }

        [Fact]
        public void Override_SingleDimensionArraySizesInMetadata()
        {

            var il = @"
.class public auto ansi abstract beforefieldinit Base
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig newslot abstract virtual
        void M1 (int32[0...] param) cil managed 
    {
    }
    .method public hidebysig newslot abstract virtual
        int32[0...] M2 () cil managed 
    {
    }
    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}";

            var source = @"
public partial class Derived : Base
{
    public override partial void M1(int[] param);
    public override partial void M1(int[] param) { }

    public override partial int[] M2();
    public override partial int[] M2() => new int[0];
}";

            var comp = CreateCompilationWithIL(source, il, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (2,22): error CS0534: 'Derived' does not implement inherited abstract member 'Base.M1(int[*])'
                // public partial class Derived : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base.M1(int[*])").WithLocation(2, 22),
                // (4,34): error CS0115: 'Derived.M1(int[])': no suitable method found to override
                //     public override partial void M1(int[] param);
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M1").WithArguments("Derived.M1(int[])").WithLocation(4, 34),
                // (7,35): error CS0508: 'Derived.M2()': return type must be 'int[*]' to match overridden member 'Base.M2()'
                //     public override partial int[] M2();
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M2").WithArguments("Derived.M2()", "Base.M2()", "int[*]").WithLocation(7, 35)
            );
        }

        [Fact]
        public void Override_ArraySizesInMetadata()
        {
            var il = @"
.class public auto ansi abstract beforefieldinit Base
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig newslot abstract virtual
        void M1 (int32[5...5,2...4] param) cil managed 
    {
    }
    .method public hidebysig newslot abstract virtual
        int32[5...5,2...4] M2 () cil managed 
    {
    }
    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}";

            var source = @"
using System;
partial class Derived : Base
{
    public override partial void M1(int[,] param);
    public override partial int[,] M2();

    public override partial void M1(int[,] param)
    {
        Console.Write(1);
    }
    public override partial int[,] M2()
    {
        Console.Write(2);
        return null;
    }

    public static void Main()
    {
        var d = new Derived();
        d.M1(null);
        _ = d.M2();
    }
}";

            var comp = CreateCompilationWithIL(source, il, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            CompileAndVerify(comp, expectedOutput: "12");

            var m1 = comp.GetMember<MethodSymbol>("Derived.M1");
            verifyArray(m1.Parameters[0].Type);

            var m2 = comp.GetMember<MethodSymbol>("Derived.M2");
            verifyArray(m2.ReturnType);

            static void verifyArray(TypeSymbol type)
            {
                var array = (ArrayTypeSymbol)type;
                Assert.False(array.IsSZArray);
                Assert.Equal(2, array.Rank);
                Assert.Equal(5, array.LowerBounds[0]);
                Assert.Equal(1, array.Sizes[0]);
                Assert.Equal(2, array.LowerBounds[1]);
                Assert.Equal(3, array.Sizes[1]);
            }
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_01()
        {
            var source = @"
partial class C
{
    public partial int M();
    public partial long M() => 42; // 1
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,25): error CS8817: Both partial method declarations must have the same return type.
                //     public partial long M() => 42; // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M").WithLocation(5, 25));

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,25): error CS8817: Both partial method declarations must have the same return type.
                //     public partial long M() => 42; // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M").WithLocation(5, 25));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_02()
        {
            var source = @"
partial class C
{
    public partial long M();
    public partial int M() => 42; // 1
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,24): error CS8817: Both partial method declarations must have the same return type.
                //     public partial int M() => 42; // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M").WithLocation(5, 24));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_03()
        {
            var source = @"
using System.Collections.Generic;
#nullable enable

partial class C
{
    public partial string? M1();
    public partial string M1() => ""hello"";

    public partial IEnumerable<string?> M2();
    public partial IEnumerable<string> M2() => null!;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (8,27): warning CS8825: Partial method declarations 'string? C.M1()' and 'string C.M1()' must have identical nullability for parameter types and return types.
                //     public partial string M1() => "hello";
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("string? C.M1()", "string C.M1()").WithLocation(8, 27),
                // (11,40): warning CS8825: Partial method declarations 'IEnumerable<string?> C.M2()' and 'IEnumerable<string> C.M2()' must have identical nullability for parameter types and return types.
                //     public partial IEnumerable<string> M2() => null!;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M2").WithArguments("IEnumerable<string?> C.M2()", "IEnumerable<string> C.M2()").WithLocation(11, 40));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_04()
        {
            var source = @"
using System.Collections.Generic;
#nullable enable

partial class C
{
    public partial string M1();
    public partial string? M1() => null; // 1

    public partial IEnumerable<string> M2();
    public partial IEnumerable<string?> M2() => null!; // 2
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (8,28): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     public partial string? M1() => null; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M1").WithLocation(8, 28),
                // (11,41): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     public partial IEnumerable<string?> M2() => null!; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M2").WithLocation(11, 41));

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (8,28): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     public partial string? M1() => null; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M1").WithLocation(8, 28),
                // (11,41): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     public partial IEnumerable<string?> M2() => null!; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M2").WithLocation(11, 41));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_05()
        {
            var source = @"
using System.Collections.Generic;

class Base { }
class Derived : Base { }

partial class C
{
    public partial Base M1();
    public partial Derived M1() => new Derived(); // 1

    public partial IEnumerable<Base> M2();
    public partial IEnumerable<Derived> M2() => null!; // 2
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (10,28): error CS8817: Both partial method declarations must have the same return type.
                //     public partial Derived M1() => new Derived(); // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M1").WithLocation(10, 28),
                // (13,41): error CS8817: Both partial method declarations must have the same return type.
                //     public partial IEnumerable<Derived> M2() => null!; // 2
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M2").WithLocation(13, 41));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_06()
        {
            var source = @"
using System.Collections.Generic;

class Base { }
class Derived : Base { }

partial class C
{
    public partial Derived M1();
    public partial Base M1() => new Base(); // 1

    public partial IEnumerable<Derived> M2();
    public partial IEnumerable<Base> M2() => null!; // 2
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (10,25): error CS8817: Both partial method declarations must have the same return type.
                //     public partial Base M1() => new Base(); // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M1").WithLocation(10, 25),
                // (13,38): error CS8817: Both partial method declarations must have the same return type.
                //     public partial IEnumerable<Base> M2() => null!; // 2
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M2").WithLocation(13, 38));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_07()
        {
            var source = @"
using System.Collections.Generic;

partial class C
{
    public partial U M1<U>();
    public partial V M1<V>() => default;

    public partial IEnumerable<U> M2<U>();
    public partial IEnumerable<U> M2<U>() => default;

    public partial U M3<U>();
    public partial V M3<V>() => default;

    public partial IEnumerable<U> M4<U>();
    public partial IEnumerable<V> M4<V>() => default;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (7,22): warning CS8826: Partial method declarations 'U C.M1<U>()' and 'V C.M1<V>()' have signature differences.
                //     public partial V M1<V>() => default;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("U C.M1<U>()", "V C.M1<V>()").WithLocation(7, 22),
                // (13,22): warning CS8826: Partial method declarations 'U C.M3<U>()' and 'V C.M3<V>()' have signature differences.
                //     public partial V M3<V>() => default;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M3").WithArguments("U C.M3<U>()", "V C.M3<V>()").WithLocation(13, 22),
                // (16,35): warning CS8826: Partial method declarations 'IEnumerable<U> C.M4<U>()' and 'IEnumerable<V> C.M4<V>()' have signature differences.
                //     public partial IEnumerable<V> M4<V>() => default;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M4").WithArguments("IEnumerable<U> C.M4<U>()", "IEnumerable<V> C.M4<V>()").WithLocation(16, 35)
            );
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_08()
        {
            var source = @"
using System.Collections.Generic;

partial class C
{
    public partial int M1();
    public partial ERROR M1() => throw null!; // 1

    public partial IEnumerable<int> M2();
    public partial IEnumerable<ERROR> M2() => throw null!; // 2

    public partial int M3();
    public partial IEnumerable<ERROR> M3() => throw null!; // 3

    public partial ERROR M4(); // 4
    public partial int M4() => throw null!;

    public partial IEnumerable<ERROR> M5(); // 5
    public partial IEnumerable<int> M5() => throw null!;

    public partial IEnumerable<ERROR> M6(); // 6
    public partial int M6() => throw null!;

    public partial ERROR M7(); // 7
    public partial ERROR M7() => throw null!; // 8

    public partial IEnumerable<ERROR> M8(); // 9
    public partial IEnumerable<ERROR> M8() => throw null!; // 10
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (7,20): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial ERROR M1() => throw null!; // 1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(7, 20),
                // (7,26): error CS8817: Both partial method declarations must have the same return type.
                //     public partial ERROR M1() => throw null!; // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M1").WithLocation(7, 26),
                // (10,32): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial IEnumerable<ERROR> M2() => throw null!; // 2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(10, 32),
                // (10,39): error CS8817: Both partial method declarations must have the same return type.
                //     public partial IEnumerable<ERROR> M2() => throw null!; // 2
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M2").WithLocation(10, 39),
                // (13,32): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial IEnumerable<ERROR> M3() => throw null!; // 3
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(13, 32),
                // (13,39): error CS8817: Both partial method declarations must have the same return type.
                //     public partial IEnumerable<ERROR> M3() => throw null!; // 3
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M3").WithLocation(13, 39),
                // (15,20): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial ERROR M4(); // 4
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(15, 20),
                // (16,24): error CS8817: Both partial method declarations must have the same return type.
                //     public partial int M4() => throw null!;
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M4").WithLocation(16, 24),
                // (18,32): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial IEnumerable<ERROR> M5(); // 5
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(18, 32),
                // (19,37): error CS8817: Both partial method declarations must have the same return type.
                //     public partial IEnumerable<int> M5() => throw null!;
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M5").WithLocation(19, 37),
                // (21,32): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial IEnumerable<ERROR> M6(); // 6
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(21, 32),
                // (22,24): error CS8817: Both partial method declarations must have the same return type.
                //     public partial int M6() => throw null!;
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M6").WithLocation(22, 24),
                // (24,20): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial ERROR M7(); // 7
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(24, 20),
                // (25,20): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial ERROR M7() => throw null!; // 8
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(25, 20),
                // (27,32): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial IEnumerable<ERROR> M8(); // 9
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(27, 32),
                // (28,32): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public partial IEnumerable<ERROR> M8() => throw null!; // 10
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(28, 32));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_09()
        {
            var source = @"
partial class C
{
    public partial ref int M1();
    public partial int M1() => throw null!; // 1

    public partial int M2();
    public partial ref int M2() => throw null!; // 2
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,24): error CS8818: Partial member declarations must have matching ref return values.
                //     public partial int M1() => throw null!; // 1
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "M1").WithLocation(5, 24),
                // (5,24): warning CS8826: Partial method declarations 'ref int C.M1()' and 'int C.M1()' have signature differences.
                //     public partial int M1() => throw null!; // 1
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("ref int C.M1()", "int C.M1()").WithLocation(5, 24),
                // (8,28): error CS8818: Partial member declarations must have matching ref return values.
                //     public partial ref int M2() => throw null!; // 2
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "M2").WithLocation(8, 28),
                // (8,28): warning CS8826: Partial method declarations 'int C.M2()' and 'ref int C.M2()' have signature differences.
                //     public partial ref int M2() => throw null!; // 2
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M2").WithArguments("int C.M2()", "ref int C.M2()").WithLocation(8, 28));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_10()
        {
            var source = @"
partial class C
{
    public partial U M1<U>();
    public partial U M1<U>() where U : struct => throw null!; // 1

    public partial U M2<U>() where U : struct;
    public partial U M2<U>() => throw null!; // 2
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,22): error CS0761: Partial method declarations of 'C.M1<U>()' have inconsistent constraints for type parameter 'U'
                //     public partial U M1<U>() where U : struct => throw null!; // 1
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M1").WithArguments("C.M1<U>()", "U").WithLocation(5, 22),
                // (8,22): error CS0761: Partial method declarations of 'C.M2<U>()' have inconsistent constraints for type parameter 'U'
                //     public partial U M2<U>() => throw null!; // 2
                Diagnostic(ErrorCode.ERR_PartialMethodInconsistentConstraints, "M2").WithArguments("C.M2<U>()", "U").WithLocation(8, 22));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_11()
        {
            var source = @"
partial class C<T>
{
    public partial T M1();
    public partial T M2();
}

partial class C<T>
{
    public partial T M1() => throw null!;
    public partial T M2() => throw null!;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_12()
        {
            var source = @"
partial class C
{
    public partial (int x, int y) M1();
    public partial (int x1, int y1) M1() => default; // 1
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,37): error CS8142: Both partial member declarations, 'C.M1()' and 'C.M1()', must use the same tuple element names.
                //     public partial (int x1, int y1) M1() => default; // 1
                Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "M1").WithArguments("C.M1()", "C.M1()").WithLocation(5, 37));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_13()
        {
            var source = @"
partial class C
{
    public partial (int x, long y) M1();
    public partial (long x, int y) M1() => default; // 1
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,36): error CS8817: Both partial method declarations must have the same return type.
                //     public partial (long x, int y) M1() => default; // 1
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "M1").WithLocation(5, 36));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_14()
        {
            var source = @"
partial class C
{
    public partial object M1();
    public partial dynamic M1() => null;
    
    public partial dynamic M2();
    public partial object M2() => null;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,28): warning CS8826: Partial method declarations 'object C.M1()' and 'dynamic C.M1()' have signature differences.
                //     public partial dynamic M1() => null;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("object C.M1()", "dynamic C.M1()").WithLocation(5, 28),
                // (8,27): warning CS8826: Partial method declarations 'dynamic C.M2()' and 'object C.M2()' have signature differences.
                //     public partial object M2() => null;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M2").WithArguments("dynamic C.M2()", "object C.M2()").WithLocation(8, 27));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_15()
        {
            var source = @"
using System;

partial class C
{
    public partial IntPtr M1();
    public partial nint M1() => 0;
    
    public partial nint M2();
    public partial IntPtr M2() => default;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (7,25): warning CS8826: Partial method declarations 'IntPtr C.M1()' and 'nint C.M1()' have signature differences.
                //     public partial nint M1() => 0;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("IntPtr C.M1()", "nint C.M1()").WithLocation(7, 25),
                // (10,27): warning CS8826: Partial method declarations 'nint C.M2()' and 'IntPtr C.M2()' have signature differences.
                //     public partial IntPtr M2() => default;
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M2").WithArguments("nint C.M2()", "IntPtr C.M2()").WithLocation(10, 27));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_16()
        {
            var source = @"
partial class C
{
    public partial ref int M1();
    public partial ref readonly int M1() => throw null!; // 1

    public partial ref readonly int M2();
    public partial ref int M2() => throw null!; // 2
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (5,37): error CS8818: Partial member declarations must have matching ref return values.
                //     public partial ref readonly int M1() => throw null!; // 1
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "M1").WithLocation(5, 37),
                // (5,37): warning CS8826: Partial method declarations 'ref int C.M1()' and 'ref readonly int C.M1()' have signature differences.
                //     public partial ref readonly int M1() => throw null!; // 1
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("ref int C.M1()", "ref readonly int C.M1()").WithLocation(5, 37),
                // (8,28): error CS8818: Partial member declarations must have matching ref return values.
                //     public partial ref int M2() => throw null!; // 2
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "M2").WithLocation(8, 28),
                // (8,28): warning CS8826: Partial method declarations 'ref readonly int C.M2()' and 'ref int C.M2()' have signature differences.
                //     public partial ref int M2() => throw null!; // 2
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M2").WithArguments("ref readonly int C.M2()", "ref int C.M2()").WithLocation(8, 28));
        }

        [Fact, WorkItem(44930, "https://github.com/dotnet/roslyn/issues/44930")]
        public void DifferentReturnTypes_17()
        {
            var source = @"
partial class C
{
#nullable enable
    public partial string M1();
    public partial string? M1() => null; // 1
    
    public partial string? M2();
    public partial string M2() => ""hello""; // 2
    
#nullable disable
    public partial string M3();
    public partial string? M3() => null; // 3
    
    public partial string? M4(); // 4
    public partial string M4() => ""hello"";

#nullable enable
    public partial string M5();
    public partial string M6() => null!;
    public partial string M7();
    public partial string M8() => null!; // 5
    public partial string? M9();
    public partial string? M10() => null;
    public partial string? M11();
    public partial string? M12() => null;

#nullable disable
    public partial string M5() => null;
    public partial string M6();
    public partial string? M7() => null; // 6
    public partial string? M8(); // 7
    public partial string M9() => null;
    public partial string M10();
    public partial string? M11() => null; // 8
    public partial string? M12(); // 9
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,28): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     public partial string? M1() => null; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M1").WithLocation(6, 28),
                // (13,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M3() => null; // 3
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 26),
                // (15,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M4(); // 4
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(15, 26),
                // (31,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M7() => null; // 6
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(31, 26),
                // (32,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M8(); // 7
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(32, 26),
                // (35,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M11() => null; // 8
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(35, 26),
                // (36,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M12(); // 9
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(36, 26));

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (6,28): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     public partial string? M1() => null; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "M1").WithLocation(6, 28),
                // (9,27): warning CS8826: Partial method declarations 'string? C.M2()' and 'string C.M2()' have signature differences.
                //     public partial string M2() => "hello"; // 2
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M2").WithArguments("string? C.M2()", "string C.M2()").WithLocation(9, 27),
                // (13,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M3() => null; // 3
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(13, 26),
                // (15,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M4(); // 4
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(15, 26),
                // (22,27): warning CS8826: Partial method declarations 'string? C.M8()' and 'string C.M8()' have signature differences.
                //     public partial string M8() => null!; // 5
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M8").WithArguments("string? C.M8()", "string C.M8()").WithLocation(22, 27),
                // (31,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M7() => null; // 6
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(31, 26),
                // (32,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M8(); // 7
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(32, 26),
                // (35,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M11() => null; // 8
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(35, 26),
                // (36,26): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                //     public partial string? M12(); // 9
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(36, 26));
        }

        [Fact]
        public void DifferentReturnTypes_18()
        {
            var source =
@"partial class C
{
    public partial ref int F1();
    public partial string F1() => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,27): error CS8817: Both partial method declarations must have the same return type.
                //     public partial string F1() => null;
                Diagnostic(ErrorCode.ERR_PartialMethodReturnTypeDifference, "F1").WithLocation(4, 27),
                // (4,27): error CS8818: Partial member declarations must have matching ref return values.
                //     public partial string F1() => null;
                Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "F1").WithLocation(4, 27));
        }

        [Fact]
        public void DifferentReturnTypes_19()
        {
            var source =
@"partial class C
{
    public partial ref (int x, int y) F1();
    public partial (int, int) F1() => default;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                    // (4,31): error CS8818: Partial member declarations must have matching ref return values.
                    //     public partial (int, int) F1() => default;
                    Diagnostic(ErrorCode.ERR_PartialMemberRefReturnDifference, "F1").WithLocation(4, 31),
                    // (4,31): warning CS8826: Partial method declarations 'ref (int x, int y) C.F1()' and '(int, int) C.F1()' have signature differences.
                    //     public partial (int, int) F1() => default;
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F1").WithArguments("ref (int x, int y) C.F1()", "(int, int) C.F1()").WithLocation(4, 31));
        }

        [Fact]
        [WorkItem(45519, "https://github.com/dotnet/roslyn/issues/45519")]
        public void DifferentSignatures_Dynamic()
        {
            var source =
@"partial class C
{
    partial void F1(object o);
    partial void F1(dynamic o) { } // 1
    internal partial void F2(object o);
    internal partial void F2(dynamic o) { } // 2
    internal partial dynamic F3();
    internal partial object F3() => null; // 3
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5));
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6));
            comp.VerifyDiagnostics(
                // (4,18): warning CS8826: Partial method declarations 'void C.F1(object o)' and 'void C.F1(dynamic o)' have signature differences.
                //     partial void F1(dynamic o) { } // 1
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F1", isSuppressed: false).WithArguments("void C.F1(object o)", "void C.F1(dynamic o)").WithLocation(4, 18),
                // (6,27): warning CS8826: Partial method declarations 'void C.F2(object o)' and 'void C.F2(dynamic o)' have signature differences.
                //     internal partial void F2(dynamic o) { } // 2
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F2", isSuppressed: false).WithArguments("void C.F2(object o)", "void C.F2(dynamic o)").WithLocation(6, 27),
                // (8,29): warning CS8826: Partial method declarations 'dynamic C.F3()' and 'object C.F3()' have signature differences.
                //     internal partial object F3() => null; // 3
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F3", isSuppressed: false).WithArguments("dynamic C.F3()", "object C.F3()").WithLocation(8, 29));
        }

        [Fact]
        [WorkItem(45519, "https://github.com/dotnet/roslyn/issues/45519")]
        public void DifferentSignatures_Nullable()
        {
            var source =
@"using System.Collections.Generic;
#nullable enable
partial class C
{
    partial void F1(string? s);
    partial void F1(string s) { } // 1
    partial void F2(IEnumerable<string?> s);
    partial void F2(IEnumerable<string> s) { } // 2
    internal partial void F3(string? s);
    internal partial void F3(string s) { } // 3
    internal partial void F4(IEnumerable<string?> s);
    internal partial void F4(IEnumerable<string> s) { } // 4
    internal partial string? F5();
    internal partial string F5() => null!; // 5
    internal partial IEnumerable<string> F6();
    internal partial IEnumerable<string?> F6() => null!; // 6
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5));
            comp.VerifyDiagnostics(
                // (6,18): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     partial void F1(string s) { } // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F1").WithArguments("s").WithLocation(6, 18),
                // (8,18): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     partial void F2(IEnumerable<string> s) { } // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F2").WithArguments("s").WithLocation(8, 18),
                // (10,27): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     internal partial void F3(string s) { } // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F3").WithArguments("s").WithLocation(10, 27),
                // (12,27): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     internal partial void F4(IEnumerable<string> s) { } // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F4").WithArguments("s").WithLocation(12, 27),
                // (16,43): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     internal partial IEnumerable<string?> F6() => null!; // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "F6").WithLocation(16, 43));

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6));
            comp.VerifyDiagnostics(
                // (6,18): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     partial void F1(string s) { } // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F1").WithArguments("s").WithLocation(6, 18),
                // (8,18): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     partial void F2(IEnumerable<string> s) { } // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F2").WithArguments("s").WithLocation(8, 18),
                // (10,27): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     internal partial void F3(string s) { } // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F3").WithArguments("s").WithLocation(10, 27),
                // (12,27): warning CS8611: Nullability of reference types in type of parameter 's' doesn't match partial method declaration.
                //     internal partial void F4(IEnumerable<string> s) { } // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, "F4").WithArguments("s").WithLocation(12, 27),
                // (14,29): warning CS8826: Partial method declarations 'string? C.F5()' and 'string C.F5()' have signature differences.
                //     internal partial string F5() => null!; // 5
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F5").WithArguments("string? C.F5()", "string C.F5()").WithLocation(14, 29),
                // (16,43): warning CS8819: Nullability of reference types in return type doesn't match partial method declaration.
                //     internal partial IEnumerable<string?> F6() => null!; // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, "F6").WithLocation(16, 43));
        }

        // Errors reported for all differences.
        [Fact]
        [WorkItem(45519, "https://github.com/dotnet/roslyn/issues/45519")]
        public void DifferentSignatures_Tuples()
        {
            var source =
@"partial class C
{
    partial void F1<T, U>((T x, U y) t) { }
    partial void F1<T, U>((T x, U y) t);
    partial void F2<T, U>((T x, U y) t) { } // 1
    partial void F2<T, U>((T, U) t);
    partial void F3((dynamic, object) t);
    partial void F3((object x, dynamic y) t) { } // 2
    internal partial void F4<T, U>((T x, U y) t);
    internal partial void F4<T, U>((T, U) t) { } // 3
    internal partial (T, U) F5<T, U>() => default;
    internal partial (T, U) F5<T, U>();
    internal partial (T, U) F6<T, U>() => default; // 4
    internal partial (T x, U y) F6<T, U>();
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5));
            verifyDiagnostics(comp);

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6));
            verifyDiagnostics(comp);

            static void verifyDiagnostics(CSharpCompilation comp)
            {
                comp.VerifyDiagnostics(
                    // (5,18): error CS8142: Both partial member declarations, 'C.F2<T, U>((T, U))' and 'C.F2<T, U>((T x, U y))', must use the same tuple element names.
                    //     partial void F2<T, U>((T x, U y) t) { } // 1
                    Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "F2").WithArguments("C.F2<T, U>((T, U))", "C.F2<T, U>((T x, U y))").WithLocation(5, 18),
                    // (8,18): error CS8142: Both partial member declarations, 'C.F3((dynamic, object))' and 'C.F3((object x, dynamic y))', must use the same tuple element names.
                    //     partial void F3((object x, dynamic y) t) { } // 2
                    Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "F3").WithArguments("C.F3((dynamic, object))", "C.F3((object x, dynamic y))").WithLocation(8, 18),
                    // (10,27): error CS8142: Both partial member declarations, 'C.F4<T, U>((T x, U y))' and 'C.F4<T, U>((T, U))', must use the same tuple element names.
                    //     internal partial void F4<T, U>((T, U) t) { } // 3
                    Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "F4").WithArguments("C.F4<T, U>((T x, U y))", "C.F4<T, U>((T, U))").WithLocation(10, 27),
                    // (13,29): error CS8142: Both partial member declarations, 'C.F6<T, U>()' and 'C.F6<T, U>()', must use the same tuple element names.
                    //     internal partial (T, U) F6<T, U>() => default; // 4
                    Diagnostic(ErrorCode.ERR_PartialMemberInconsistentTupleNames, "F6").WithArguments("C.F6<T, U>()", "C.F6<T, U>()").WithLocation(13, 29));
            }
        }

        [Fact]
        [WorkItem(45519, "https://github.com/dotnet/roslyn/issues/45519")]
        public void DifferentSignatures_NativeIntegers()
        {
            var source =
@"using System.Collections.Generic;
partial class C
{
    partial void F1(nint i);
    partial void F1(System.IntPtr i) { } // 1
    partial void F2(dynamic x, nint y);
    partial void F2(object x, System.IntPtr y) { } // 2
    internal partial void F3(nint i);
    internal partial void F3(System.IntPtr i) { } // 3
    internal partial IEnumerable<System.IntPtr> F4();
    internal partial IEnumerable<nint> F4() => null; // 4
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(5));
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6));
            comp.VerifyDiagnostics(
                // (5,18): warning CS8826: Partial method declarations 'void C.F1(nint i)' and 'void C.F1(IntPtr i)' have signature differences.
                //     partial void F1(System.IntPtr i) { } // 1
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F1").WithArguments("void C.F1(nint i)", "void C.F1(IntPtr i)").WithLocation(5, 18),
                // (7,18): warning CS8826: Partial method declarations 'void C.F2(dynamic x, nint y)' and 'void C.F2(object x, IntPtr y)' have signature differences.
                //     partial void F2(object x, System.IntPtr y) { } // 2
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F2").WithArguments("void C.F2(dynamic x, nint y)", "void C.F2(object x, IntPtr y)").WithLocation(7, 18),
                // (9,27): warning CS8826: Partial method declarations 'void C.F3(nint i)' and 'void C.F3(IntPtr i)' have signature differences.
                //     internal partial void F3(System.IntPtr i) { } // 3
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F3").WithArguments("void C.F3(nint i)", "void C.F3(IntPtr i)").WithLocation(9, 27),
                // (11,40): warning CS8826: Partial method declarations 'IEnumerable<IntPtr> C.F4()' and 'IEnumerable<nint> C.F4()' have signature differences.
                //     internal partial IEnumerable<nint> F4() => null; // 4
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F4").WithArguments("IEnumerable<IntPtr> C.F4()", "IEnumerable<nint> C.F4()").WithLocation(11, 40));
        }

        [Fact]
        public void PublicAPI()
        {
            var source = @"
#nullable enable

partial class C
{
    public partial string M1();
    public partial string M1() => ""a"";

    partial void M2();
    partial void M2() { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();

            verifyPublicAPI(comp.GetMember<MethodSymbol>("C.M1").GetPublicSymbol());
            verifyPublicAPI(comp.GetMember<MethodSymbol>("C.M2").GetPublicSymbol());

            void verifyPublicAPI(IMethodSymbol defSymbol)
            {
                var implSymbol = defSymbol.PartialImplementationPart;
                Assert.NotNull(implSymbol);
                Assert.NotEqual(implSymbol, defSymbol);

                Assert.Null(defSymbol.PartialDefinitionPart);
                Assert.Null(implSymbol.PartialImplementationPart);

                Assert.Equal(implSymbol.PartialDefinitionPart, defSymbol);
                Assert.Equal(implSymbol.ToTestDisplayString(includeNonNullable: false), defSymbol.ToTestDisplayString(includeNonNullable: false));
            }
        }
    }
}
