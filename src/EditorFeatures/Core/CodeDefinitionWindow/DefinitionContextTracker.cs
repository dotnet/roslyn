// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeDefinitionWindow;

/// <summary>
/// A type that tracks caret movements, and when you've been on an identifier for awhile, pushes the new
/// code definition window context to the <see cref="ICodeDefinitionWindowService"/>.
/// </summary>
[Export(typeof(ITextViewConnectionListener))]
[Export(typeof(DefinitionContextTracker))]
[ContentType(ContentTypeNames.RoslynContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal class DefinitionContextTracker : ITextViewConnectionListener
{
    private readonly HashSet<ITextView> _subscribedViews = [];
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly ICodeDefinitionWindowService _codeDefinitionWindowService;
    private readonly IThreadingContext _threadingContext;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly IGlobalOptionService _globalOptions;

    private readonly AsyncBatchingWorkQueue<SnapshotPoint> _workQueue;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefinitionContextTracker(
        IMetadataAsSourceFileService metadataAsSourceFileService,
        ICodeDefinitionWindowService codeDefinitionWindowService,
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _codeDefinitionWindowService = codeDefinitionWindowService;
        _threadingContext = threadingContext;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.CodeDefinitionWindow);
        _globalOptions = globalOptions;

        _workQueue = new AsyncBatchingWorkQueue<SnapshotPoint>(
            DelayTimeSpan.Short,
            ProcessWorkAsync,
            _asyncListener,
            _threadingContext.DisposalToken);
    }

    void ITextViewConnectionListener.SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        // We won't listen to caret changes in the code definition window itself, since navigations there would cause it to
        // keep refreshing itself.
        if (!_subscribedViews.Contains(textView) && !textView.Roles.Contains(PredefinedTextViewRoles.CodeDefinitionView))
        {
            _subscribedViews.Add(textView);
            textView.Caret.PositionChanged += OnTextViewCaretPositionChanged;
            QueueUpdateForCaretPosition(textView.Caret.Position);
        }
    }

    void ITextViewConnectionListener.SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        if (reason == ConnectionReason.TextViewLifetime ||
            textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)).Count == 0)
        {
            if (_subscribedViews.Remove(textView))
                textView.Caret.PositionChanged -= OnTextViewCaretPositionChanged;
        }
    }

    private void OnTextViewCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        QueueUpdateForCaretPosition(e.NewPosition);
    }

    private void QueueUpdateForCaretPosition(CaretPosition caretPosition)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        // See if we moved somewhere else in a projection that we care about
        var pointInRoslynSnapshot = caretPosition.Point.GetPoint(tb => tb.ContentType.IsOfType(ContentTypeNames.RoslynContentType), caretPosition.Affinity);
        if (pointInRoslynSnapshot == null)
            return;

        _workQueue.AddWork(pointInRoslynSnapshot.Value, cancelExistingWork: true);
    }

    private async ValueTask ProcessWorkAsync(ImmutableSegmentedList<SnapshotPoint> points, CancellationToken cancellationToken)
    {
        var lastPoint = points.Last();

        // If it's not open, don't do anything, since if we are going to show locations in metadata that might
        // be expensive. This doesn't cause a functional issue, since opening the window clears whatever was previously there
        // so the user won't notice we weren't doing anything when it was open.
        if (!await _codeDefinitionWindowService.IsWindowOpenAsync(cancellationToken).ConfigureAwait(false))
            return;

        var snapshot = lastPoint.Snapshot;
        var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document is null)
            return;

        var locations = await GetContextFromPointAsync(document, lastPoint, cancellationToken).ConfigureAwait(true);
        await _codeDefinitionWindowService.SetContextAsync(locations, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal for testing purposes.
    /// </summary>
    internal async Task<ImmutableArray<CodeDefinitionWindowLocation>> GetContextFromPointAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        using var locations = TemporaryArray<CodeDefinitionWindowLocation>.Empty;

        var workspace = document.Project.Solution.Workspace;
        var navigableItems = await GetNavigableItemsAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (navigableItems.Length > 0)
        {
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

            foreach (var item in navigableItems)
            {
                if (await navigationService.CanNavigateToSpanAsync(workspace, item.Document.Id, item.SourceSpan, cancellationToken).ConfigureAwait(false))
                {
                    var text = await item.Document.GetTextAsync(document.Project.Solution, cancellationToken).ConfigureAwait(false);
                    var linePositionSpan = text.Lines.GetLinePositionSpan(item.SourceSpan);

                    if (item.Document.FilePath != null)
                        locations.Add(new CodeDefinitionWindowLocation(item.DisplayTaggedParts.JoinText(), item.Document.FilePath, linePositionSpan.Start));
                }
            }
        }
        else
        {
            // We didn't have regular source references, but possibly:
            // 1. Another language (like XAML) will take over via ISymbolNavigationService
            // 2. There are no locations from source, so we'll try to generate a metadata as source file and use that
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
                document, position, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (symbol != null)
            {
                var symbolNavigationService = workspace.Services.GetRequiredService<ISymbolNavigationService>();
                var definitionItem = symbol.ToNonClassifiedDefinitionItem(document.Project.Solution, includeHiddenLocations: false);
                var result = await symbolNavigationService.GetExternalNavigationSymbolLocationAsync(definitionItem, cancellationToken).ConfigureAwait(false);

                if (result != null)
                {
                    locations.Add(new CodeDefinitionWindowLocation(
                        symbol.ToDisplayString(), result.Value.filePath, result.Value.linePosition));
                }
                else if (_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
                {
                    var options = _globalOptions.GetMetadataAsSourceOptions(document.Project.Services);
                    var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(workspace, document.Project, symbol, signaturesOnly: false, options, cancellationToken).ConfigureAwait(false);
                    var identifierSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    locations.Add(new CodeDefinitionWindowLocation(
                        symbol.ToDisplayString(), declarationFile.FilePath, identifierSpan.Start));
                }
            }
        }

        return locations.ToImmutableAndClear();
    }

    private static async Task<ImmutableArray<INavigableItem>> GetNavigableItemsAsync(Document document, int position, CancellationToken cancellationToken)
    {
        // Try IFindDefinitionService first. Until partners implement this, it could fail to find a service, so fall back if it's null.
        var findDefinitionService = document.GetLanguageService<INavigableItemsService>();
        return findDefinitionService != null
            ? await findDefinitionService.GetNavigableItemsAsync(document, position, cancellationToken).ConfigureAwait(false)
            : [];
    }
}
