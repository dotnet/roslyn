// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Structure;

internal abstract class BlockStructureServiceWithProviders : BlockStructureService
{
    private readonly SolutionServices _services;
    private readonly ImmutableArray<BlockStructureProvider> _providers;

    protected BlockStructureServiceWithProviders(SolutionServices services)
    {
        _services = services;
        _providers = GetBuiltInProviders().Concat(GetImportedProviders());
    }

    /// <summary>
    /// Returns the providers always available to the service.
    /// This does not included providers imported via MEF composition.
    /// </summary>
    protected virtual ImmutableArray<BlockStructureProvider> GetBuiltInProviders()
        => [];

    private ImmutableArray<BlockStructureProvider> GetImportedProviders()
    {
        var language = Language;
        var mefExporter = _services.ExportProvider;

        var providers = mefExporter.GetExports<BlockStructureProvider, LanguageMetadata>()
                                   .Where(lz => lz.Metadata.Language == language)
                                   .Select(lz => lz.Value);

        return providers.ToImmutableArray();
    }

    public override async Task<BlockStructure> GetBlockStructureAsync(
        Document document,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        using var context = CreateContext(syntaxTree, options, cancellationToken);
        return GetBlockStructure(context, _providers);
    }

    private static BlockStructureContext CreateContext(
        SyntaxTree syntaxTree,
        in BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        return new BlockStructureContext(syntaxTree, options, cancellationToken);
    }

    private static BlockStructure GetBlockStructure(
        in BlockStructureContext context,
        ImmutableArray<BlockStructureProvider> providers)
    {
        foreach (var provider in providers)
            provider.ProvideBlockStructure(context);

        return CreateBlockStructure(context);
    }

    private static BlockStructure CreateBlockStructure(in BlockStructureContext context)
    {
        for (var i = 0; i < context.Spans.Count; i++)
            context.Spans[i] = UpdateBlockSpan(context.Spans[i], context.Options);

        return new BlockStructure(context.Spans.ToImmutable());
    }

    private static BlockSpan UpdateBlockSpan(BlockSpan blockSpan, in BlockStructureOptions options)
    {
        var type = blockSpan.Type;

        var isTopLevel = BlockTypes.IsDeclarationLevelConstruct(type);
        var isMemberLevel = BlockTypes.IsCodeLevelConstruct(type);
        var isComment = BlockTypes.IsCommentOrPreprocessorRegion(type);

        if ((!options.ShowBlockStructureGuidesForDeclarationLevelConstructs && isTopLevel) ||
            (!options.ShowBlockStructureGuidesForCodeLevelConstructs && isMemberLevel) ||
            (!options.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions && isComment))
        {
            type = BlockTypes.Nonstructural;
        }

        var isCollapsible = blockSpan.IsCollapsible;
        if (isCollapsible)
        {
            if ((!options.ShowOutliningForDeclarationLevelConstructs && isTopLevel) ||
                (!options.ShowOutliningForCodeLevelConstructs && isMemberLevel) ||
                (!options.ShowOutliningForCommentsAndPreprocessorRegions && isComment))
            {
                isCollapsible = false;
            }
        }

        return blockSpan.With(type: type, isCollapsible: isCollapsible);
    }
}
