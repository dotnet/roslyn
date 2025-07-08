// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class IfDirectiveTriviaStructureTests : AbstractCSharpSyntaxNodeStructureTests<IfDirectiveTriviaSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new IfDirectiveTriviaStructureProvider();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestEnabledIfDisabledElifDisabledElse()
    {
        await VerifyBlockSpansAsync("""
                #$$if true
                {|span:class C
                {
                }|}
                #elif false
                class D
                {
                }
                #else
                class E
                {
                }
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestDisabledIfEnabledElifDisabledElse()
    {
        await VerifyBlockSpansAsync("""
                #$$if false
                class C
                {
                }
                #elif true
                {|span:class D
                {
                }|}
                #else
                class E
                {
                }
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestDisabledIfDisabledElifEnabledElse()
    {
        await VerifyBlockSpansAsync("""
                #$$if false
                class C
                {
                }
                #elif false
                class D
                {
                }
                #else
                {|span:class E
                {
                }|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestEmptyEnabledRegion()
    {
        await VerifyBlockSpansAsync("""
                #$$if true
                #elif false
                class D
                {
                }
                #else
                class E
                {
                }
                #endif
                """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestMissingEndif1()
    {
        await VerifyBlockSpansAsync("""
                #$$if true
                class C
                {
                }
                """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestMissingEndif2()
    {
        await VerifyBlockSpansAsync("""
                #$$if true
                {|span:class C
                {
                }|}
                #elif false
                class D
                {
                }
                #else
                class E
                {
                }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public async Task TestMissingEndif3()
    {
        await VerifyBlockSpansAsync("""
                #$$if false
                class C
                {
                }
                #elif false
                class D
                {
                }
                #else
                class E
                {
                }
                """);
    }
}
