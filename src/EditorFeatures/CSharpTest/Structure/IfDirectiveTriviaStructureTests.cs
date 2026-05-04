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
    public Task TestEnabledIfDisabledElifDisabledElse()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public Task TestDisabledIfEnabledElifDisabledElse()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public Task TestDisabledIfDisabledElifEnabledElse()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public Task TestEmptyEnabledRegion()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public Task TestMissingEndif1()
        => VerifyBlockSpansAsync("""
                #$$if true
                class C
                {
                }
                """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public Task TestMissingEndif2()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10426")]
    public Task TestMissingEndif3()
        => VerifyBlockSpansAsync("""
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
