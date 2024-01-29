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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource;

[Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
public class TypeDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<TypeDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new TypeDeclarationStructureProvider();

    [Fact]
    public async Task NoCommentsOrAttributes()
    {
        var code = """
                {|hint:class $$C{|textspan:
                {
                    void M();
                }|}|}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task WithAttributes()
    {
        var code = """
                {|hint:{|textspan:[Bar]
                [Baz]
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task WithCommentsAndAttributes()
    {
        var code = """
                {|hint:{|textspan:// Summary:
                //     This is a doc comment.
                [Bar, Baz]
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47889")]
    public async Task RecordWithCommentsAndAttributes()
    {
        var code = """
                {|hint:{|textspan:// Summary:
                //     This is a doc comment.
                [Bar, Baz]
                |}{|#0:public record $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task RecordStructWithCommentsAndAttributes()
    {
        var code = """
                {|hint:{|textspan:// Summary:
                //     This is a doc comment.
                [Bar, Baz]
                |}{|#0:public record struct $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task WithDocComments()
    {
        var code = """
                {|hint:{|textspan:/// <summary>This is a doc comment.</summary>
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }

    [Fact]
    public async Task WithMultilineDocComments()
    {
        var code = """
                {|hint:{|textspan:/// <summary>This is a doc comment.</summary>
                /// <remarks>
                /// Comments are cool
                /// </remarks>
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """;

        await VerifyBlockSpansAsync(code,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
    }
}
