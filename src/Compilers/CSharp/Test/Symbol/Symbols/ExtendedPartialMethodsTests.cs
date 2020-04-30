// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
                // (4,26): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal partial int M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(4, 26),
                // (5,26): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal partial int M1() => 1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(5, 26)
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
                // (4,17): error CS9051: Partial method 'C.M1()' must have accessibility modifiers because it has a non-void return type.
                //     partial int M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(4, 17),
                // (5,17): error CS9051: Partial method 'C.M1()' must have accessibility modifiers because it has a non-void return type.
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
                // (4,25): error CS9050: Partial method 'C.M1()' must have an implementation part because it has accessibility modifiers.
                //     private partial int M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("C.M1()").WithLocation(4, 25)
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
                // (4,26): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     private partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(4, 26),
                // (5,26): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     private partial void M1(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(5, 26)
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
                // (4,18): error CS9052: Partial method 'C.M1(out int)' must have accessibility modifiers because it has 'out' parameters.
                //     partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, "M1").WithArguments("C.M1(out int)").WithLocation(4, 18),
                // (5,18): error CS9052: Partial method 'C.M1(out int)' must have accessibility modifiers because it has 'out' parameters.
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
                // (4,26): error CS9050: Partial method 'C.M1(out int)' must have an implementation part because it has accessibility modifiers.
                //     private partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M1").WithArguments("C.M1(out int)").WithLocation(4, 26)
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
                // (4,28): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     {mod} void M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods"),
                // (5,28): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     {mod} void M1() { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods")
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
                // (5,28): error CS9055: Both partial method declarations must have equivalent accessibility modifiers.
                //      internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodAccessibilityDifference, "M1").WithLocation(5, 28)
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
                // (5,19): error CS9054: Both partial method declarations must have accessibility modifiers or neither may have accessibility modifiers.
                //      partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExplicitAccessibilityDifference, "M1").WithLocation(5, 19)
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
                // (5,27): error CS9054: Both partial method declarations must have accessibility modifiers or neither may have accessibility modifiers.
                //      private partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExplicitAccessibilityDifference, "M1").WithLocation(5, 27)
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
                // (4,27): error CS9052: Partial method C.M1() must have an implementation part because it has accessibility modifiers.
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
                // (4,34): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal virtual partial int M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(4, 34),
                // (5,34): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal virtual partial int M1() => 1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(5, 34)
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
                // (4,26): error CS9053: Partial method 'C.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
                //     virtual partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("C.M1()").WithLocation(4, 26),
                // (5,26): error CS9053: Partial method 'C.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
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
                // (4,35): error CS9050: Partial method 'C.M1()' must have an implementation part because it has accessibility modifiers.
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
                // (5,27): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(5, 27)
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
                // (5,35): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal virtual partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(5, 35)
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
        public void Override_LangVersion()
        {
            const string text1 = @"
partial class D
{
    internal override partial string ToString();
    internal override partial string ToString() => ""hello"";
}";
            var comp = CreateCompilation(text1);
            comp.VerifyDiagnostics(
                // (4,38): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal override partial string ToString();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ToString").WithArguments("extended partial methods").WithLocation(4, 38),
                // (5,38): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal override partial string ToString() => "hello";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ToString").WithArguments("extended partial methods").WithLocation(5, 38)
            );
            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
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
                // (9,27): error CS9053: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
                //     override partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(9, 27),
                // (10,27): error CS9053: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
                //     override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(10, 27)
            );
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
                // (9,27): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(9, 27)
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
                // (9,36): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(9, 36)
            );
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
    internal sealed override partial string ToString();
    internal sealed override partial string ToString() => ""hello"";
}";
            var comp = CreateCompilation(text1);
            comp.VerifyDiagnostics(
                // (8,45): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal sealed override partial string ToString();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ToString").WithArguments("extended partial methods").WithLocation(8, 45),
                // (9,45): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal sealed override partial string ToString() => "hello";
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ToString").WithArguments("extended partial methods").WithLocation(9, 45)
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
                // (8,34): error CS9053: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
                //     sealed override partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(8, 34),
                // (9,34): error CS9053: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
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
                // (9,36): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(9, 36)
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
                // (9,43): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal sealed override partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(9, 43)
            );
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
            var comp = CreateCompilation(text1);
            comp.VerifyDiagnostics(
                // (6,34): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal static partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(6, 34),
                // (9,41): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal static extern partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(9, 41));

            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
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
                // (9,32): error CS9053: Partial method 'C.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
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

                Assert.True(method.IsExtern);

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
            // PROTOTYPE: it feels like both IsAsync/IsExtern should be shared between
            // definition+implementation, or neither should.
            // Currently IsExtern is shared but IsAsync is not.
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
            // PROTOTYPE: it feels like both IsAsync/IsExtern should be shared between
            // definition+implementation, or neither should.
            // Currently IsExtern is shared but IsAsync is not.
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
            // PROTOTYPE: it feels like both IsAsync/IsExtern should be shared between
            // definition+implementation, or neither should.
            // Currently IsExtern is shared but IsAsync is not.
            Assert.False(method.IsAsync);
            Assert.True(method.PartialImplementationPart.IsAsync);
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
            var comp = CreateCompilation(text1);
            comp.VerifyDiagnostics(
                // (9,31): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal new partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(9, 31),
                // (10,31): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     internal new partial void M1() { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(10, 31));

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
                // (9,31): error CS9050: Partial method 'D.M1()' must have an implementation part because it has accessibility modifiers.
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
                // (9,22): error CS9053: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
                //     new partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, "M1").WithArguments("D.M1()").WithLocation(9, 22),
                // (10,22): error CS9053: Partial method 'D.M1()' must have accessibility modifiers because it has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
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
                // (10,27): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(10, 27)
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
                // (10,31): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     internal new partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(10, 31)
            );
        }
    }
}
