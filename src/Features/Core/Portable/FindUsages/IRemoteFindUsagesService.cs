// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindUsages;

internal interface IRemoteFindUsagesService
{
    internal interface ICallback : IRemoteOptionsCallback<ClassificationOptions>
    {
        ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
        ValueTask ItemsCompletedAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
        ValueTask ReportMessageAsync(RemoteServiceCallbackId callbackId, string message, CancellationToken cancellationToken);
        ValueTask ReportInformationalMessageAsync(RemoteServiceCallbackId callbackId, string message, CancellationToken cancellationToken);
        ValueTask SetSearchTitleAsync(RemoteServiceCallbackId callbackId, string title, CancellationToken cancellationToken);
        ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition, CancellationToken cancellationToken);
        ValueTask OnReferencesFoundAsync(RemoteServiceCallbackId callbackId, ImmutableArray<SerializableSourceReferenceItem> references, CancellationToken cancellationToken);
    }

    ValueTask FindReferencesAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        SerializableSymbolAndProjectId symbolAndProjectId,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken);

    ValueTask FindImplementationsAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        SerializableSymbolAndProjectId symbolAndProjectId,
        CancellationToken cancellationToken);
}

[ExportRemoteServiceCallbackDispatcher(typeof(IRemoteFindUsagesService)), Shared]
internal sealed class FindUsagesServerCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteFindUsagesService.ICallback
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FindUsagesServerCallbackDispatcher()
    {
    }

    private new FindUsagesServerCallback GetCallback(RemoteServiceCallbackId callbackId)
        => (FindUsagesServerCallback)base.GetCallback(callbackId);

    public ValueTask<ClassificationOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken)
        => GetCallback(callbackId).GetClassificationOptionsAsync(language, cancellationToken);

    public ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken)
        => GetCallback(callbackId).AddItemsAsync(count, cancellationToken);

    public ValueTask ItemsCompletedAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken)
        => GetCallback(callbackId).ItemsCompletedAsync(count, cancellationToken);

    public ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition, CancellationToken cancellationToken)
        => GetCallback(callbackId).OnDefinitionFoundAsync(definition, cancellationToken);

    public ValueTask OnReferencesFoundAsync(RemoteServiceCallbackId callbackId, ImmutableArray<SerializableSourceReferenceItem> references, CancellationToken cancellationToken)
        => GetCallback(callbackId).OnReferencesFoundAsync(references, cancellationToken);

    public ValueTask ReportMessageAsync(RemoteServiceCallbackId callbackId, string message, CancellationToken cancellationToken)
        => GetCallback(callbackId).ReportMessageAsync(message, cancellationToken);

    public ValueTask ReportInformationalMessageAsync(RemoteServiceCallbackId callbackId, string message, CancellationToken cancellationToken)
        => GetCallback(callbackId).ReportInformationalMessageAsync(message, cancellationToken);

    public ValueTask SetSearchTitleAsync(RemoteServiceCallbackId callbackId, string title, CancellationToken cancellationToken)
        => GetCallback(callbackId).SetSearchTitleAsync(title, cancellationToken);
}

internal sealed class FindUsagesServerCallback(Solution solution, IFindUsagesContext context, OptionsProvider<ClassificationOptions> classificationOptions)
{
    private readonly Solution _solution = solution;
    private readonly IFindUsagesContext _context = context;
    private readonly Dictionary<int, DefinitionItem> _idToDefinition = [];
    private readonly OptionsProvider<ClassificationOptions> _classificationOptions = classificationOptions;

    internal ValueTask<ClassificationOptions> GetClassificationOptionsAsync(string language, CancellationToken cancellationToken)
        => _classificationOptions.GetOptionsAsync(_solution.Services.GetLanguageServices(language), cancellationToken);

    public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
        => _context.ProgressTracker.AddItemsAsync(count, cancellationToken);

    public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
        => _context.ProgressTracker.ItemsCompletedAsync(count, cancellationToken);

    public ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken)
        => _context.ReportNoResultsAsync(message, cancellationToken);

    public ValueTask ReportInformationalMessageAsync(string message, CancellationToken cancellationToken)
        => _context.ReportMessageAsync(message, NotificationSeverity.Information, cancellationToken);

    public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
        => _context.SetSearchTitleAsync(title, cancellationToken);

    public async ValueTask OnDefinitionFoundAsync(SerializableDefinitionItem definition, CancellationToken cancellationToken)
    {
        try
        {
            var id = definition.Id;
            var rehydrated = await definition.RehydrateAsync(_solution, cancellationToken).ConfigureAwait(false);

            lock (_idToDefinition)
            {
                _idToDefinition.Add(id, rehydrated);
            }

            await _context.OnDefinitionFoundAsync(rehydrated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public async ValueTask OnReferencesFoundAsync(ImmutableArray<SerializableSourceReferenceItem> references, CancellationToken cancellationToken)
    {
        try
        {
            await _context.OnReferencesFoundAsync(ConvertAsync(references, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }

        async IAsyncEnumerable<SourceReferenceItem> ConvertAsync(
            ImmutableArray<SerializableSourceReferenceItem> references, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var reference in references)
                yield return await reference.RehydrateAsync(_solution, GetDefinition(reference.DefinitionId), cancellationToken).ConfigureAwait(false);
        }
    }

    private DefinitionItem GetDefinition(int definitionId)
    {
        lock (_idToDefinition)
        {
            Contract.ThrowIfFalse(_idToDefinition.ContainsKey(definitionId));
            return _idToDefinition[definitionId];
        }
    }
}

[DataContract]
internal readonly struct SerializableDocumentSpan(
    DocumentId documentId, TextSpan sourceSpan, bool isGeneratedCode)
{
    [DataMember(Order = 0)]
    public readonly DocumentId DocumentId = documentId;

    [DataMember(Order = 1)]
    public readonly TextSpan SourceSpan = sourceSpan;

    [DataMember(Order = 2)]
    public readonly bool IsGeneratedCode = isGeneratedCode;

    public static SerializableDocumentSpan Dehydrate(DocumentSpan documentSpan)
        => new(documentSpan.Document.Id, documentSpan.SourceSpan, documentSpan.IsGeneratedCode);

    public async ValueTask<DocumentSpan> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(DocumentId) ??
            await solution.GetSourceGeneratedDocumentAsync(DocumentId, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(document);
        return new DocumentSpan(document, SourceSpan, IsGeneratedCode);
    }
}

[DataContract]
internal readonly struct SerializableDefinitionItem(
    int id,
    ImmutableArray<string> tags,
    ImmutableArray<TaggedText> displayParts,
    ImmutableArray<TaggedText> nameDisplayParts,
    ImmutableArray<SerializableDocumentSpan> sourceSpans,
    ImmutableArray<AssemblyLocation> metadataLocations,
    ImmutableDictionary<string, string> properties,
    ImmutableArray<(string key, string value)> displayableProperties,
    bool displayIfNoReferences)
{
    [DataMember(Order = 0)]
    public readonly int Id = id;

    [DataMember(Order = 1)]
    public readonly ImmutableArray<string> Tags = tags;

    [DataMember(Order = 2)]
    public readonly ImmutableArray<TaggedText> DisplayParts = displayParts;

    [DataMember(Order = 3)]
    public readonly ImmutableArray<TaggedText> NameDisplayParts = nameDisplayParts;

    [DataMember(Order = 4)]
    public readonly ImmutableArray<SerializableDocumentSpan> SourceSpans = sourceSpans;

    [DataMember(Order = 5)]
    public readonly ImmutableArray<AssemblyLocation> MetadataLocations = metadataLocations;

    [DataMember(Order = 6)]
    public readonly ImmutableDictionary<string, string> Properties = properties;

    [DataMember(Order = 7)]
    public readonly ImmutableArray<(string key, string value)> DisplayableProperties = displayableProperties;

    [DataMember(Order = 8)]
    public readonly bool DisplayIfNoReferences = displayIfNoReferences;

    public static SerializableDefinitionItem Dehydrate(int id, DefinitionItem item)
        => new(id,
               item.Tags,
               item.DisplayParts,
               item.NameDisplayParts,
               item.SourceSpans.SelectAsArray(SerializableDocumentSpan.Dehydrate),
               item.MetadataLocations,
               item.Properties,
               item.DisplayableProperties,
               item.DisplayIfNoReferences);

    public async ValueTask<DefinitionItem.DefaultDefinitionItem> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
    {
        var sourceSpans = await SourceSpans.SelectAsArrayAsync(static (ss, solution, cancellationToken) => ss.RehydrateAsync(solution, cancellationToken), solution, cancellationToken).ConfigureAwait(false);

        return new DefinitionItem.DefaultDefinitionItem(
            Tags,
            DisplayParts,
            NameDisplayParts,
            sourceSpans,
            // todo: consider serializing this over.
            classifiedSpans: sourceSpans.SelectAsArray(ss => (ClassifiedSpansAndHighlightSpan?)null),
            MetadataLocations,
            Properties,
            DisplayableProperties,
            DisplayIfNoReferences);
    }
}

[DataContract]
internal readonly struct SerializableClassifiedSpansAndHighlightSpan(
    SerializableClassifiedSpans classifiedSpans, TextSpan highlightSpan)
{
    [DataMember(Order = 0)]
    public readonly SerializableClassifiedSpans ClassifiedSpans = classifiedSpans;

    [DataMember(Order = 1)]
    public readonly TextSpan HighlightSpan = highlightSpan;

    public static SerializableClassifiedSpansAndHighlightSpan Dehydrate(ClassifiedSpansAndHighlightSpan classifiedSpansAndHighlightSpan)
    {
        using var _ = Classifier.GetPooledList(out var temp);

        foreach (var span in classifiedSpansAndHighlightSpan.ClassifiedSpans)
            temp.Add(span);

        return new(SerializableClassifiedSpans.Dehydrate(temp), classifiedSpansAndHighlightSpan.HighlightSpan);
    }

    public ClassifiedSpansAndHighlightSpan Rehydrate()
        => new(this.ClassifiedSpans.Rehydrate(), this.HighlightSpan);
}

[DataContract]
internal readonly struct SerializableSourceReferenceItem(
    int definitionId,
    SerializableDocumentSpan sourceSpan,
    SerializableClassifiedSpansAndHighlightSpan classifiedSpans,
    SymbolUsageInfo symbolUsageInfo,
    ImmutableArray<(string key, string value)> additionalProperties)
{
    [DataMember(Order = 0)]
    public readonly int DefinitionId = definitionId;

    [DataMember(Order = 1)]
    public readonly SerializableDocumentSpan SourceSpan = sourceSpan;

    [DataMember(Order = 2)]
    public readonly SerializableClassifiedSpansAndHighlightSpan ClassifiedSpans = classifiedSpans;

    [DataMember(Order = 3)]
    public readonly SymbolUsageInfo SymbolUsageInfo = symbolUsageInfo;

    [DataMember(Order = 4)]
    public readonly ImmutableArray<(string key, string value)> AdditionalProperties = additionalProperties;

    public static SerializableSourceReferenceItem Dehydrate(int definitionId, SourceReferenceItem item)
        => new(definitionId,
               SerializableDocumentSpan.Dehydrate(item.SourceSpan),
               // We're always have classified spans for C#/VB, which are the only languages used in OOP find-references.
               SerializableClassifiedSpansAndHighlightSpan.Dehydrate(item.ClassifiedSpans!.Value),
               item.SymbolUsageInfo,
               item.AdditionalProperties);

    public async Task<SourceReferenceItem> RehydrateAsync(Solution solution, DefinitionItem definition, CancellationToken cancellationToken)
        => new(definition,
               await SourceSpan.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false),
               this.ClassifiedSpans.Rehydrate(),
               SymbolUsageInfo,
               AdditionalProperties);
}
