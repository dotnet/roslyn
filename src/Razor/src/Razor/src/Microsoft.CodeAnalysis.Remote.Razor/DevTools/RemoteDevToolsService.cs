// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

using SyntaxNode = AspNetCore.Razor.Language.Syntax.SyntaxNode;

internal sealed class RemoteDevToolsService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteDevToolsService
{
    internal sealed class Factory : FactoryBase<IRemoteDevToolsService>
    {
        protected override IRemoteDevToolsService CreateService(in ServiceArgs args)
            => new RemoteDevToolsService(in args);
    }

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true
    };

    public ValueTask<string> GetCSharpDocumentTextAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            async context =>
            {
                var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
                return codeDocument.GetCSharpSourceText().ToString();
            },
            cancellationToken);

    public ValueTask<string> GetHtmlDocumentTextAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            async context =>
            {
                var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
                return codeDocument.GetHtmlSourceText(cancellationToken).ToString();
            },
            cancellationToken);

    public ValueTask<string> GetFormattingDocumentTextAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            async context =>
            {
                var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
                var csharpSyntaxTree = await context.Snapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var csharpSyntaxRoot = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618 // Type or member is obsolete
                return CSharpFormattingPass.GetFormattingDocumentContentsForSyntaxVisualizer(codeDocument, csharpSyntaxRoot, DocumentMappingService);
#pragma warning restore CS0618 // Type or member is obsolete
            },
            cancellationToken);

    public ValueTask<string> GetTagHelpersJsonAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        TagHelpersKind kind,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetTagHelpersJsonAsync(context, kind, cancellationToken),
            cancellationToken);

    private static async ValueTask<string> GetTagHelpersJsonAsync(RemoteDocumentContext documentContext, TagHelpersKind kind, CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var tagHelpers = kind switch
        {
            TagHelpersKind.All => codeDocument.GetTagHelpers(),
            TagHelpersKind.InScope => codeDocument.GetRequiredTagHelperContext().TagHelpers,
            TagHelpersKind.Referenced => codeDocument.GetReferencedTagHelpers(),
            _ => []
        };

        tagHelpers ??= [];

        // TagHelperCollection is self-referencial, so we need to create objects that System.Text.Json can handle.
        var toSerialize = tagHelpers.Select(th => new
        {
            th.Name,
            th.AssemblyName,
            th.DisplayName,
            th.Documentation,
            th.TagOutputHint,
            Kind = th.Kind.ToString(),
            th.CaseSensitive,
            TagMatchingRules = th.TagMatchingRules.Select(r => new
            {
                r.TagName,
                r.ParentTag,
                TagStructure = r.TagStructure.ToString(),
                r.CaseSensitive,
                Attributes = r.Attributes.Select(a => new
                {
                    a.Name,
                    NameComparison = a.NameComparison.ToString(),
                    a.Value,
                    ValueComparison = a.ValueComparison.ToString(),
                    a.DisplayName,
                    Diagnostics = a.Diagnostics.Select(d => new { d.Id, Message = d.GetMessage() })
                })
            }),
            BoundAttributes = th.BoundAttributes.Select(a => new
            {
                a.Name,
                a.TypeName,
                a.IsEnum,
                a.IsEditorRequired,
                a.IsStringProperty,
                a.IndexerNamePrefix,
                a.IndexerTypeName,
                a.Documentation,
                a.DisplayName,
                a.CaseSensitive,
                MetadataKind = a.Metadata.Kind.ToString(),
                Parameters = a.Parameters.Select(p => new
                {
                    p.Name,
                    p.TypeName,
                    p.IsEnum,
                    p.Documentation,
                    p.DisplayName,
                    p.CaseSensitive,
                    MetadataKind = a.Metadata.Kind.ToString(),
                    Diagnostics = p.Diagnostics.Select(d => new { d.Id, Message = d.GetMessage() })
                }),
                Diagnostics = a.Diagnostics.Select(d => new { d.Id, Message = d.GetMessage() })
            }),
            AllowedChildTags = th.AllowedChildTags.Select(c => new
            {
                c.Name,
                c.DisplayName,
                Diagnostics = c.Diagnostics.Select(d => new { d.Id, Message = d.GetMessage() })
            }),
            MetadataKind = th.Metadata.Kind.ToString(),
            Diagnostics = th.Diagnostics.Select(d => new { d.Id, Message = d.GetMessage() })
        });

        return JsonSerializer.Serialize(toSerialize, s_serializerOptions);
    }

    public ValueTask<SyntaxVisualizerTree?> GetRazorSyntaxTreeAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetRazorSyntaxTreeAsync(context, cancellationToken),
            cancellationToken);

    private static async ValueTask<SyntaxVisualizerTree?> GetRazorSyntaxTreeAsync(RemoteDocumentContext documentContext, CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var razorSyntaxTree = codeDocument.GetTagHelperRewrittenSyntaxTree();

        if (razorSyntaxTree?.Root == null)
            return null;

        return new SyntaxVisualizerTree
        {
            Root = ConvertSyntaxNode(razorSyntaxTree.Root)
        };
    }

    private static SyntaxVisualizerNode ConvertSyntaxNode(SyntaxNode node)
        => new SyntaxVisualizerNode
        {
            Kind = node.Kind.ToString(),
            SpanStart = node.SpanStart,
            SpanEnd = node.Span.End,
            Children = [.. node.ChildNodes().Select(ConvertSyntaxNode)]
        };
}
