// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MakeMemberStatic;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeMemberStatic;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpMakeMemberStaticCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
public class MakeMemberStaticTests
{
    [Fact]
    public async Task TestField()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public static class Goo
            {
                int {|CS0708:i|};
            }
            """,
            """
            public static class Goo
            {
                static int i;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54202")]
    public async Task TestTrivia()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public static class Goo
            {
                // comment
                readonly int {|CS0708:i|};
            }
            """,
            """
            public static class Goo
            {
                // comment
                static readonly int i;
            }
            """);
    }

    [Fact]
    public async Task TestMethod()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public static class Goo
            {
                void {|CS0708:M|}() { }
            }
            """,
            """
            public static class Goo
            {
                static void M() { }
            }
            """);
    }

    [Fact]
    public async Task TestProperty()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public static class Goo
            {
                object {|CS0708:P|} { get; set; }
            }
            """,
            """
            public static class Goo
            {
                static object P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestEventField()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public static class Goo
            {
                event System.Action {|CS0708:E|};
            }
            """,
            """
            public static class Goo
            {
                static event System.Action E;
            }
            """);
    }

    [Fact]
    public async Task FixAll()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            namespace NS
            {
                public static class Goo
                {
                    int {|CS0708:i|};
                    void {|CS0708:M|}() { }
                    object {|CS0708:P|} { get; set; }
                    event System.Action {|CS0708:E|};
                }
            }
            """,
            """
            namespace NS
            {
                public static class Goo
                {
                    static int i;
                    static void M() { }
                    static object P { get; set; }
                    static event System.Action E;
                }
            }
            """);
    }
}
