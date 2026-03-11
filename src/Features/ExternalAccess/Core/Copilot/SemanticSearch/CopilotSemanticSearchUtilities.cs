// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal static class CopilotSemanticSearchUtilities
{
    private static readonly FindReferencesSearchOptions s_options = new()
    {
        AssociatePropertyReferencesWithSpecificAccessor = false,
        Cascade = false,
        DisplayAllDefinitions = false,
        Explicit = true,
        UnidirectionalHierarchyCascade = false
    };

    public static SyntaxTree CreateSyntaxTree(SolutionServices services, string language, string? filePath, ParseOptions options, SourceText? text, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, SyntaxNode root)
        => services.GetRequiredLanguageService<ISyntaxTreeFactoryService>(language).CreateSyntaxTree(filePath, options, text, encoding, checksumAlgorithm, root);

    public static SyntaxTree ParseSyntaxTree(SolutionServices services, string language, string? filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken)
        => services.GetRequiredLanguageService<ISyntaxTreeFactoryService>(language).ParseSyntaxTree(filePath, options, text, cancellationToken);

    public static PortableExecutableReference GetMetadataReference(SolutionServices services, string resolvedPath, MetadataReferenceProperties properties)
        => services.GetRequiredService<IMetadataService>().GetReference(resolvedPath, properties);

    public static ImmutableArray<TaggedText> ToTaggedText(this IEnumerable<SymbolDisplayPart>? displayParts)
        => TaggedTextExtensions.ToTaggedText(displayParts);

    public static SyntaxNode FindNode(this Location location, bool findInsideTrivia, bool getInnermostNodeForTie, CancellationToken cancellationToken)
        => location.SourceTree!.GetRoot(cancellationToken).FindNode(location.SourceSpan, findInsideTrivia, getInnermostNodeForTie);

    public static Task FindReferencesAsync(Solution solution, ISymbol symbol, Action<ReferenceLocation> callback, CancellationToken cancellationToken)
        => SymbolFinder.FindReferencesAsync(
            symbol, solution, new Progress(callback), documents: null, s_options, cancellationToken);

    public static bool CanApplyChange(TextDocument document)
        => document.CanApplyChange();

    private sealed class Progress(Action<ReferenceLocation> callback) : IStreamingFindReferencesProgress
    {
        public ValueTask OnStartedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask OnCompletedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask OnReferencesFoundAsync(ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references, CancellationToken cancellationToken)
        {
            foreach (var (_, _, location) in references)
                callback(location);

            return ValueTask.CompletedTask;
        }

        public IStreamingProgressTracker ProgressTracker
            => NoOpStreamingFindReferencesProgress.Instance.ProgressTracker;
    }
}
