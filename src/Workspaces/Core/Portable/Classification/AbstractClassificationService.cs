// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ObsoleteSymbol;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal abstract class AbstractClassificationService(ISyntaxClassificationService syntaxClassificationService) : IClassificationService
{
    private readonly ISyntaxClassificationService _syntaxClassificationService = syntaxClassificationService;

    private Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>>? _getNodeClassifiers;
    private Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>>? _getTokenClassifiers;

    public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken);
    public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

    public Task AddSemanticClassificationsAsync(
        Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        return AddClassificationsAsync(document, textSpans, options, ClassificationType.Semantic, result, cancellationToken);
    }

    public Task AddEmbeddedLanguageClassificationsAsync(
        Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        return AddClassificationsAsync(document, textSpans, options, ClassificationType.EmbeddedLanguage, result, cancellationToken);
    }

    public async Task AddClassificationsAsync(
        Document document,
        ImmutableArray<TextSpan> textSpans,
        ClassificationOptions options,
        ClassificationType type,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        var classificationService = document.GetLanguageService<ISyntaxClassificationService>();
        if (classificationService == null)
        {
            // When renaming a file's extension through VS when it's opened in editor, 
            // the content type might change and the content type changed event can be 
            // raised before the renaming propagate through VS workspace. As a result, 
            // the document we got (based on the buffer) could still be the one in the workspace
            // before rename happened. This would cause us problem if the document is supported 
            // by workspace but not a roslyn language (e.g. xaml, F#, etc.), since none of the roslyn 
            // language services would be available.
            //
            // If this is the case, we will simply bail out. It's OK to ignore the request
            // because when the buffer eventually get associated with the correct document in roslyn
            // workspace, we will be invoked again.
            //
            // For example, if you open a xaml from from a WPF project in designer view,
            // and then rename file extension from .xaml to .cs, then the document we received
            // here would still belong to the special "-xaml" project.
            return;
        }

        var project = document.Project;
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            // We have an oop connection.  If we're not fully loaded, see if we can retrieve a previously cached set
            // of classifications from the server.  Note: this must be a separate call (instead of being part of
            // service.GetSemanticClassificationsAsync below) as we want to try to read in the cached
            // classifications without doing any syncing to the OOP process.
            var workspaceStatusService = document.Project.Solution.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoaded = await workspaceStatusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (await TryGetCachedClassificationsAsync(document, textSpans, type, client, isFullyLoaded, result, cancellationToken).ConfigureAwait(false))
                return;

            // Call the project overload.  Semantic classification only needs the current project's information
            // to classify properly.
            var classifiedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationService, SerializableClassifiedSpans>(
               project,
               (service, solutionInfo, cancellationToken) => service.GetClassificationsAsync(
                   solutionInfo, document.Id, textSpans, type, options, isFullyLoaded, cancellationToken),
               cancellationToken).ConfigureAwait(false);

            // if the remote call fails do nothing (error has already been reported)
            if (classifiedSpans.HasValue)
                classifiedSpans.Value.Rehydrate(result);
        }
        else
        {
            await AddClassificationsInCurrentProcessAsync(
                document, textSpans, type, options, result, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<bool> TryGetCachedClassificationsAsync(
        Document document,
        ImmutableArray<TextSpan> textSpans,
        ClassificationType type,
        RemoteHostClient client,
        bool isFullyLoaded,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        // Only try to get cached classifications if we're not fully loaded yet.
        if (isFullyLoaded)
            return false;

        var (documentKey, checksum) = await SemanticClassificationCacheUtilities.GetDocumentKeyAndChecksumAsync(
            document, cancellationToken).ConfigureAwait(false);

        var cachedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationService, SerializableClassifiedSpans?>(
           document.Project,
           (service, solutionInfo, cancellationToken) => service.GetCachedClassificationsAsync(
               documentKey, textSpans, type, checksum, cancellationToken),
           cancellationToken).ConfigureAwait(false);

        // if the remote call fails do nothing (error has already been reported)
        if (!cachedSpans.HasValue || cachedSpans.Value == null)
            return false;

        cachedSpans.Value.Rehydrate(result);
        return true;
    }

    private async Task AddClassificationsInCurrentProcessAsync(
        Document document,
        ImmutableArray<TextSpan> textSpans,
        ClassificationType type,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        if (type == ClassificationType.Semantic)
        {
            var classificationService = document.GetRequiredLanguageService<ISyntaxClassificationService>();

            var (getNodeClassifiers, getTokenClassifiers) = GetExtensionClassifiers(document, classificationService);

            await classificationService.AddSemanticClassificationsAsync(
                document, textSpans, options, getNodeClassifiers, getTokenClassifiers, result, cancellationToken).ConfigureAwait(false);

            if (options.ClassifyReassignedVariables)
            {
                var reassignedVariableService = document.GetRequiredLanguageService<IReassignedVariableService>();
                var reassignedVariableSpans = await reassignedVariableService.GetLocationsAsync(document, textSpans, cancellationToken).ConfigureAwait(false);
                foreach (var span in reassignedVariableSpans)
                    result.Add(new ClassifiedSpan(span, ClassificationTypeNames.ReassignedVariable));
            }

            if (options.ClassifyObsoleteSymbols)
            {
                var obsoleteSymbolService = document.GetRequiredLanguageService<IObsoleteSymbolService>();
                var obsoleteSymbolSpans = await obsoleteSymbolService.GetLocationsAsync(document, textSpans, cancellationToken).ConfigureAwait(false);
                foreach (var span in obsoleteSymbolSpans)
                    result.Add(new ClassifiedSpan(span, ClassificationTypeNames.ObsoleteSymbol));
            }
        }
        else if (type == ClassificationType.EmbeddedLanguage)
        {
            var embeddedLanguageService = document.GetLanguageService<IEmbeddedLanguageClassificationService>();
            if (embeddedLanguageService != null)
            {
                await embeddedLanguageService.AddEmbeddedLanguageClassificationsAsync(
                    document, textSpans, options, result, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(type);
        }

        return;

        (Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>>, Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>>) GetExtensionClassifiers(
            Document document, ISyntaxClassificationService classificationService)
        {
            if (_getNodeClassifiers == null || _getTokenClassifiers == null)
            {
                var extensionManager = document.Project.Solution.Services.GetRequiredService<IExtensionManager>();
                var classifiers = classificationService.GetDefaultSyntaxClassifiers();

                _getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, static c => c.SyntaxNodeTypes);
                _getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, static c => c.SyntaxTokenKinds);
            }

            return (_getNodeClassifiers, _getTokenClassifiers);
        }
    }

    public async Task AddSyntacticClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        AddSyntacticClassifications(document.Project.Solution.Services, root, textSpans, result, cancellationToken);
    }

    public void AddSyntacticClassifications(
        SolutionServices services, SyntaxNode? root, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        if (root is null)
            return;

        _syntaxClassificationService.AddSyntacticClassifications(root, textSpans, result, cancellationToken);
    }

    /// <summary>
    /// Helper to add all the values of <paramref name="temp"/> into <paramref name="result"/>
    /// without causing any allocations or boxing of enumerators.
    /// </summary>
    protected static void AddRange(ArrayBuilder<ClassifiedSpan> temp, List<ClassifiedSpan> result)
    {
        foreach (var span in temp)
        {
            result.Add(span);
        }
    }

    public ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(Document oldDocument, Document newDocument, TimeSpan timeout, CancellationToken cancellationToken)
        => default;

    public TextChangeRange? ComputeSyntacticChangeRange(SolutionServices services, SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var classificationService = services.GetLanguageServices(oldRoot.Language).GetService<ISyntaxClassificationService>();
        return classificationService?.ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, cancellationToken);
    }
}
