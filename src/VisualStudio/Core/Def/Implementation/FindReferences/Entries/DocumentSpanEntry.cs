// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Entry to show for a particular source location.  The row will show the classified
        /// contents of that line, and hovering will reveal a tooltip showing that line along
        /// with a few lines above/below it.
        /// </summary>
        private sealed class DocumentSpanEntry : AbstractDocumentSpanEntry, ISupportsNavigation
        {
            private readonly HighlightSpanKind _spanKind;
            private readonly ExcerptResult _excerptResult;
            private readonly SymbolReferenceKinds _symbolReferenceKinds;
            private readonly ImmutableDictionary<string, string> _customColumnsData;

            private readonly string _rawProjectName;
            private readonly List<string> _projectFlavors = new();

            private string? _cachedProjectName;

            private DocumentSpanEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                string rawProjectName,
                string? projectFlavor,
                Guid projectGuid,
                HighlightSpanKind spanKind,
                MappedSpanResult mappedSpanResult,
                ExcerptResult excerptResult,
                SourceText lineText,
                SymbolUsageInfo symbolUsageInfo,
                ImmutableDictionary<string, string> customColumnsData)
                : base(context, definitionBucket, projectGuid, lineText, mappedSpanResult)
            {
                _spanKind = spanKind;
                _excerptResult = excerptResult;
                _symbolReferenceKinds = symbolUsageInfo.ToSymbolReferenceKinds();
                _customColumnsData = customColumnsData;

                _rawProjectName = rawProjectName;
                this.AddFlavor(projectFlavor);
            }

            protected override string GetProjectName()
            {
                // Check if we have any flavors.  If we have at least 2, combine with the project name
                // so the user can know htat in the UI.
                lock (_projectFlavors)
                {
                    if (_cachedProjectName == null)
                    {
                        _cachedProjectName = _projectFlavors.Count < 2
                            ? _rawProjectName
                            : $"{_rawProjectName} ({string.Join(", ", _projectFlavors)})";
                    }

                    return _cachedProjectName;
                }
            }

            private void AddFlavor(string? projectFlavor)
            {
                if (projectFlavor == null)
                    return;

                lock (_projectFlavors)
                {
                    if (_projectFlavors.Contains(projectFlavor))
                        return;

                    _projectFlavors.Add(projectFlavor);
                    _projectFlavors.Sort();
                    _cachedProjectName = null;
                }
            }

            public static DocumentSpanEntry? TryCreate(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                Guid guid,
                string projectName,
                string? projectFlavor,
                string? filePath,
                TextSpan sourceSpan,
                HighlightSpanKind spanKind,
                MappedSpanResult mappedSpanResult,
                ExcerptResult excerptResult,
                SourceText lineText,
                SymbolUsageInfo symbolUsageInfo,
                ImmutableDictionary<string, string> customColumnsData)
            {
                var entry = new DocumentSpanEntry(
                    context, definitionBucket,
                    projectName, projectFlavor, guid,
                    spanKind, mappedSpanResult, excerptResult,
                    lineText, symbolUsageInfo, customColumnsData);

                // Because of things like linked files, we may have a reference up in multiple
                // different locations that are effectively at the exact same navigation location
                // for the user. i.e. they're the same file/span.  Showing multiple entries for these
                // is just noisy and gets worse and worse with shared projects and whatnot.  So, we
                // collapse things down to only show a single entry for each unique file/span pair.
                var winningEntry = definitionBucket.GetOrAddEntry(filePath, sourceSpan, entry);

                // If we were the one that successfully added this entry to the bucket, then pass us
                // back out to be put in the ui.
                if (winningEntry == entry)
                    return entry;

                // We were not the winner.  Add our flavor to the entry that already exists, but throw
                // away the item we created as we do not want to add it to the ui.
                winningEntry.AddFlavor(projectFlavor);
                return null;
            }

            protected override IList<System.Windows.Documents.Inline> CreateLineTextInlines()
            {
                var propertyId = _spanKind == HighlightSpanKind.Definition
                    ? DefinitionHighlightTag.TagId
                    : _spanKind == HighlightSpanKind.WrittenReference
                        ? WrittenReferenceHighlightTag.TagId
                        : ReferenceHighlightTag.TagId;

                var properties = Presenter.FormatMapService
                                          .GetEditorFormatMap("text")
                                          .GetProperties(propertyId);

                // Remove additive classified spans before creating classified text.
                // Otherwise the text will be repeated since there are two classifications
                // for the same span. Additive classifications should not change the foreground
                // color, so the resulting classified text will retain the proper look.
                var classifiedSpans = _excerptResult.ClassifiedSpans.WhereAsArray(
                    cs => !ClassificationTypeNames.AdditiveTypeNames.Contains(cs.ClassificationType));
                var classifiedTexts = classifiedSpans.SelectAsArray(
                    cs => new ClassifiedText(cs.ClassificationType, _excerptResult.Content.ToString(cs.TextSpan)));

                var inlines = classifiedTexts.ToInlines(
                    Presenter.ClassificationFormatMap,
                    Presenter.TypeMap,
                    runCallback: (run, classifiedText, position) =>
                    {
                        if (properties["Background"] is Brush highlightBrush)
                        {
                            if (position == _excerptResult.MappedSpan.Start)
                            {
                                run.SetValue(
                                    System.Windows.Documents.TextElement.BackgroundProperty,
                                    highlightBrush);
                            }
                        }
                    });

                return inlines;
            }

            public override bool TryCreateColumnContent(string columnName, [NotNullWhen(true)] out FrameworkElement? content)
            {
                if (base.TryCreateColumnContent(columnName, out content))
                {
                    // this lazy tooltip causes whole solution to be kept in memory until find all reference result gets cleared.
                    // solution is never supposed to be kept alive for long time, meaning there is bunch of conditional weaktable or weak reference
                    // keyed by solution/project/document or corresponding states. this will cause all those to be kept alive in memory as well.
                    // probably we need to dig in to see how expensvie it is to support this
                    var controlService = _excerptResult.Document.Project.Solution.Workspace.Services.GetRequiredService<IContentControlService>();
                    controlService.AttachToolTipToControl(content, () =>
                        CreateDisposableToolTip(_excerptResult.Document, _excerptResult.Span));

                    return true;
                }

                return false;
            }

            protected override object? GetValueWorker(string keyName)
            {
                if (keyName == StandardTableKeyNames2.SymbolKind)
                {
                    return _symbolReferenceKinds;
                }

                if (_customColumnsData.TryGetValue(keyName, out var value))
                {
                    return value;
                }

                return base.GetValueWorker(keyName);
            }

            private DisposableToolTip CreateDisposableToolTip(Document document, TextSpan sourceSpan)
            {
                Presenter.AssertIsForeground();

                var controlService = document.Project.Solution.Workspace.Services.GetRequiredService<IContentControlService>();
                var sourceText = document.GetTextSynchronously(CancellationToken.None);

                var excerptService = document.Services.GetService<IDocumentExcerptService>();
                if (excerptService != null)
                {
                    var classificationOptions = Presenter._globalOptions.GetClassificationOptions(document.Project.Language);
                    var excerpt = Presenter.ThreadingContext.JoinableTaskFactory.Run(() => excerptService.TryExcerptAsync(document, sourceSpan, ExcerptMode.Tooltip, classificationOptions, CancellationToken.None));
                    if (excerpt != null)
                    {
                        // get tooltip from excerpt service
                        var clonedBuffer = excerpt.Value.Content.CreateTextBufferWithRoslynContentType(document.Project.Solution.Workspace);
                        SetHighlightSpan(_spanKind, clonedBuffer, excerpt.Value.MappedSpan);
                        SetStaticClassifications(clonedBuffer, excerpt.Value.ClassifiedSpans);

                        return controlService.CreateDisposableToolTip(clonedBuffer, EnvironmentColors.ToolWindowBackgroundBrushKey);
                    }
                }

                // get default behavior
                var textBuffer = document.CloneTextBuffer(sourceText);
                SetHighlightSpan(_spanKind, textBuffer, sourceSpan);

                var contentSpan = GetRegionSpanForReference(sourceText, sourceSpan);
                return controlService.CreateDisposableToolTip(document, textBuffer, contentSpan, EnvironmentColors.ToolWindowBackgroundBrushKey);
            }

            private static void SetStaticClassifications(ITextBuffer textBuffer, ImmutableArray<ClassifiedSpan> classifiedSpans)
            {
                var key = PredefinedPreviewTaggerKeys.StaticClassificationSpansKey;
                textBuffer.Properties.RemoveProperty(key);
                textBuffer.Properties.AddProperty(key, classifiedSpans);
            }

            private static void SetHighlightSpan(HighlightSpanKind spanKind, ITextBuffer textBuffer, TextSpan span)
            {
                // Create an appropriate highlight span on that buffer for the reference.
                var key = spanKind == HighlightSpanKind.Definition
                    ? PredefinedPreviewTaggerKeys.DefinitionHighlightingSpansKey
                    : spanKind == HighlightSpanKind.WrittenReference
                        ? PredefinedPreviewTaggerKeys.WrittenReferenceHighlightingSpansKey
                        : PredefinedPreviewTaggerKeys.ReferenceHighlightingSpansKey;

                textBuffer.Properties.RemoveProperty(key);
                textBuffer.Properties.AddProperty(key, new NormalizedSnapshotSpanCollection(span.ToSnapshotSpan(textBuffer.CurrentSnapshot)));
            }

            private static Span GetRegionSpanForReference(SourceText sourceText, TextSpan sourceSpan)
            {
                const int AdditionalLineCountPerSide = 3;

                var referenceSpan = sourceSpan;
                var lineNumber = sourceText.Lines.GetLineFromPosition(referenceSpan.Start).LineNumber;
                var firstLineNumber = Math.Max(0, lineNumber - AdditionalLineCountPerSide);
                var lastLineNumber = Math.Min(sourceText.Lines.Count - 1, lineNumber + AdditionalLineCountPerSide);

                return Span.FromBounds(
                    sourceText.Lines[firstLineNumber].Start,
                    sourceText.Lines[lastLineNumber].End);
            }

            public bool CanNavigateTo()
            {
                if (_excerptResult.Document is SourceGeneratedDocument)
                {
                    var workspace = _excerptResult.Document.Project.Solution.Workspace;
                    var documentNavigationService = workspace.Services.GetService<IDocumentNavigationService>();

                    return documentNavigationService != null;
                }

                return false;
            }

            public Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(CanNavigateTo());

                // If the document is a source generated document, we need to do the navigation ourselves;
                // this is because the file path given to the table control isn't a real file path to a file
                // on disk.

                var workspace = _excerptResult.Document.Project.Solution.Workspace;
                var documentNavigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

                return documentNavigationService.TryNavigateToSpanAsync(
                    workspace,
                    _excerptResult.Document.Id,
                    _excerptResult.Span,
                    options,
                    cancellationToken);
            }
        }
    }
}
