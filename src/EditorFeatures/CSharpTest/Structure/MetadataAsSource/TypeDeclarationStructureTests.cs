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
public sealed class TypeDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<TypeDeclarationSyntax>
{
    protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
    internal override AbstractSyntaxStructureProvider CreateProvider() => new TypeDeclarationStructureProvider();

    [Fact]
    public Task NoCommentsOrAttributes()
        => VerifyBlockSpansAsync("""
                {|hint:class $$C{|textspan:
                {
                    void M();
                }|}|}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task WithAttributes()
        => VerifyBlockSpansAsync("""
                {|hint:{|textspan:[Bar]
                [Baz]
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task WithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                {|hint:{|textspan:// Summary:
                //     This is a doc comment.
                [Bar, Baz]
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47889")]
    public Task RecordWithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                {|hint:{|textspan:// Summary:
                //     This is a doc comment.
                [Bar, Baz]
                |}{|#0:public record $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task RecordStructWithCommentsAndAttributes()
        => VerifyBlockSpansAsync("""
                {|hint:{|textspan:// Summary:
                //     This is a doc comment.
                [Bar, Baz]
                |}{|#0:public record struct $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task WithDocComments()
        => VerifyBlockSpansAsync("""
                {|hint:{|textspan:/// <summary>This is a doc comment.</summary>
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));

    [Fact]
    public Task WithMultilineDocComments()
        => VerifyBlockSpansAsync("""
                {|hint:{|textspan:/// <summary>This is a doc comment.</summary>
                /// <remarks>
                /// Comments are cool
                /// </remarks>
                |}{|#0:public class $$C|}{|textspan2:
                {
                    void M();
                }|}|#0}
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true),
            Region("textspan2", "#0", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
}
