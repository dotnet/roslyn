// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    partial int M1();
    partial int M1() => 1;
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,17): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     partial int M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(4, 17),
                // (5,17): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     partial int M1() => return 1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(5, 17)
            );

            comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NonVoidReturnType_NoImpl()
        {
            const string text = @"
partial class C
{
    partial int M1();
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,17): error CS9050: Partial method C.M1() must have an implementation part because it has a non-void return type.
                //     partial int M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveImplementation, "M1").WithArguments("C.M1()").WithLocation(4, 17)
            );
        }

        [Fact]
        public void NonVoidReturnType_Semantics()
        {
            const string text1 = @"
partial class C
{
    partial int M1();

    public static void Main()
    {
        System.Console.WriteLine(new C().M1());
    }
}";

            const string text2 = @"
partial class C
{
    partial int M1() { return 42; }
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
    partial void M1(out int i);
    partial void M1(out int i) { i = 0; }
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,18): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(4, 18),
                // (5,18): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     partial void M1(out int i) { i = 0; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(5, 18)
            );

            comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void OutParam_NoImpl()
        {
            const string text = @"
partial class C
{
    partial void M1(out int i);
}";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,18): error CS9051: Partial method C.M1(out int) must have an implementation part because it has 'out' parameters.
                //     partial void M1(out int i);
                Diagnostic(ErrorCode.ERR_PartialMethodWithOutParamMustHaveImplementation, "M1").WithArguments("C.M1(out int)").WithLocation(4, 18)
            );
        }

        [Fact]
        public void OutParam_Semantics()
        {
            const string text1 = @"
partial class C
{
    partial void M1(out int value);

    public static void Main()
    {
        new C().M1(out var value);
        System.Console.WriteLine(value);
    }
}";

            const string text2 = @"
partial class C
{
    partial void M1(out int value) { value = 42; }
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
        public void Virtual_NoImpl()
        {
            const string text1 = @"
partial class C
{
    virtual partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (4,26): error CS0621: 'C.M1()': virtual or abstract members cannot be private
                //     virtual partial void M1();
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M1").WithArguments("C.M1()").WithLocation(4, 26),
                // (4,26): error CS9053: Partial method C.M1() must have an implementation part because it is has a 'virtual', 'override', 'sealed', 'new', or 'extern' modifier.
                //     virtual partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveImplementation, "M1").WithArguments("C.M1()").WithLocation(4, 26)
            );
        }

        [Fact]
        public void Virtual_Mismatch()
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
        public void Override_Mismatch()
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
        public void Sealed_Mismatch()
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
        public void Extern_LangVersion()
        {
            const string text1 = @"
using System.Runtime.InteropServices;

partial class C
{
    static partial void M1();

    [DllImportAttribute(""something.dll"")]
    static extern partial void M1();
}";
            var comp = CreateCompilation(text1);
            comp.VerifyDiagnostics(
                // (9,32): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static extern partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(9, 32));

            comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics();
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
    new partial void M1();
    new partial void M1() { }
}";
            var comp = CreateCompilation(text1);
            comp.VerifyDiagnostics(
                // (9,22): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     new partial void M1();
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(9, 22),
                // (10,22): error CS8652: The feature 'extended partial methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     new partial void M1() { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M1").WithArguments("extended partial methods").WithLocation(10, 22));

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
    new partial void M1();
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,22): error CS9053: Partial method D.M1() must have an implementation part because it is has a 'virtual', 'override', 'sealed', or 'new', or 'extern' modifier.
                //     new partial void M1();
                Diagnostic(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveImplementation, "M1").WithArguments("D.M1()").WithLocation(9, 22)
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
    new partial void M1();
    partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (10,18): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(10, 18)
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
    partial void M1();
    new partial void M1() { }
}";
            var comp = CreateCompilation(text1, parseOptions: TestOptions.RegularWithExtendedPartialMethods);
            comp.VerifyDiagnostics(
                // (9,18): warning CS0108: 'D.M1()' hides inherited member 'C.M1()'. Use the new keyword if hiding was intended.
                //     partial void M1();
                Diagnostic(ErrorCode.WRN_NewRequired, "M1").WithArguments("D.M1()", "C.M1()").WithLocation(9, 18),
                // (10,22): error CS9056: Both partial method declarations must have equal combinations of 'virtual', 'override', 'sealed', or 'new' modifiers.
                //     new partial void M1() { }
                Diagnostic(ErrorCode.ERR_PartialMethodExtendedModDifference, "M1").WithLocation(10, 22)
            );
        }
    }
}
