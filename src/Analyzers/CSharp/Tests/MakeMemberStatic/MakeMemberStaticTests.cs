// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MakeMemberStatic;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeMemberStatic
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CSharpMakeMemberStaticCodeFixProvider>;

    public class MakeMemberStaticTests
    {
        private static async Task TestAsync(string testCode, string fixedCode, string? customModifierOrder = null)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                Options =
                {
                    { CSharpCodeStyleOptions.PreferredModifierOrder,
                        customModifierOrder ?? CSharpCodeStyleOptions.PreferredModifierOrder.DefaultValue.Value }
                }
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestField()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
public static class Foo
{
    int {|CS0708:i|};
}",
@"
public static class Foo
{
    static int i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        [WorkItem(54202, "https://github.com/dotnet/roslyn/issues/54202")]
        public async Task TestTrivia()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
public static class Foo
{
    // comment
    readonly int {|CS0708:i|};
}",
@"
public static class Foo
{
    // comment
    static readonly int i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
public static class Foo
{
    void {|CS0708:M|}() { }
}",
@"
public static class Foo
{
    static void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestProperty()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
public static class Foo
{
    object {|CS0708:P|} { get; set; }
}",
@"
public static class Foo
{
    static object P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestEventField()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
public static class Foo
{
    event System.Action {|CS0708:E|};
}",
@"
public static class Foo
{
    static event System.Action E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task FixAll()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"namespace NS
{
    public static class Foo
    {
        int {|CS0708:i|};
        void {|CS0708:M|}() { }
        object {|CS0708:P|} { get; set; }
        event System.Action {|CS0708:E|};
    }
}",
@"namespace NS
{
    public static class Foo
    {
        static int i;

        static void M() { }

        static object P { get; set; }

        static event System.Action E;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestModifierOrder_DefaultOrder()
        {
            var testCode = @"
public static class Foo
{
    public void {|CS0708:Test|}() { }
}
";

            var fixedCode = @"
public static class Foo
{
    public static void Test() { }
}
";

            await TestAsync(testCode, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestModifierOrder_CustomOrder1()
        {
            var testCode = @"
public static class Foo
{
    public void {|CS0708:Test|}() { }
}
";

            var fixedCode = @"
public static class Foo
{
    static public void Test() { }
}
";
            var customModifierOrder = "static, public, private, protected, internal, extern, new, virtual, " +
                "abstract, sealed, override, readonly, unsafe, volatile, async";

            await TestAsync(testCode, fixedCode, customModifierOrder);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestModifierOrder_CustomOrder2()
        {
            var testCode = @"
public static class Foo
{
    public async System.Threading.Tasks.Task {|CS0708:Test|}() { }
}
";

            var fixedCode = @"
public static class Foo
{
    public async static System.Threading.Tasks.Task Test() { }
}
";
            var customModifierOrder = "public, private, protected, internal, extern, new, virtual, abstract, " +
                "sealed, override, readonly, unsafe, volatile, async, static";

            await TestAsync(testCode, fixedCode, customModifierOrder);
        }
    }
}
