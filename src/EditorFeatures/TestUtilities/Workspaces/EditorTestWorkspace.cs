// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public partial class EditorTestWorkspace : TestWorkspace<EditorTestHostDocument, EditorTestHostProject, EditorTestHostSolution>
{
    private readonly Dictionary<string, ITextBuffer2> _createdTextBuffers = [];

    internal EditorTestWorkspace(
        TestComposition? composition = null,
        string? workspaceKind = WorkspaceKind.Host,
        Guid solutionTelemetryId = default,
        bool disablePartialSolutions = true,
        bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
        WorkspaceConfigurationOptions? configurationOptions = null,
        bool supportsLspMutation = false)
        : base(composition ?? EditorTestCompositions.EditorFeatures,
               workspaceKind,
               solutionTelemetryId,
               disablePartialSolutions,
               ignoreUnchangeableDocumentsWhenApplyingChanges,
               configurationOptions,
               supportsLspMutation)
    {
    }

    private protected override EditorTestHostDocument CreateDocument(
            string text = "",
            string displayName = "",
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            DocumentId? id = null,
            string? filePath = null,
            IReadOnlyList<string>? folders = null,
            ExportProvider? exportProvider = null,
            IDocumentServiceProvider? documentServiceProvider = null)
            => new(text, displayName, sourceCodeKind, id, filePath, folders, exportProvider, documentServiceProvider);

    private protected override EditorTestHostDocument CreateDocument(
        ExportProvider exportProvider,
        HostLanguageServices? languageServiceProvider,
        string code,
        string name,
        string filePath,
        int? cursorPosition,
        IDictionary<string, ImmutableArray<TextSpan>> spans,
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
        IReadOnlyList<string>? folders = null,
        bool isLinkFile = false,
        IDocumentServiceProvider? documentServiceProvider = null,
        ISourceGenerator? generator = null)
        => new(exportProvider, languageServiceProvider, code, name, filePath, cursorPosition, spans,
            sourceCodeKind, folders, isLinkFile, documentServiceProvider, roles: default, textBuffer: null, generator);

    private protected override EditorTestHostProject CreateProject(
        HostLanguageServices languageServices,
        CompilationOptions? compilationOptions,
        ParseOptions? parseOptions,
        string assemblyName,
        string projectName,
        IList<MetadataReference>? references,
        IList<EditorTestHostDocument> documents,
        IList<EditorTestHostDocument>? additionalDocuments = null,
        IList<EditorTestHostDocument>? analyzerConfigDocuments = null,
        Type? hostObjectType = null,
        bool isSubmission = false,
        string? filePath = null,
        IList<AnalyzerReference>? analyzerReferences = null,
        string? defaultNamespace = null)
        => new(
            languageServices,
            compilationOptions,
            parseOptions,
            assemblyName,
            projectName,
            references,
            documents,
            additionalDocuments,
            analyzerConfigDocuments,
            hostObjectType,
            isSubmission,
            filePath,
            analyzerReferences,
            defaultNamespace);

    private protected override EditorTestHostSolution CreateSolution(EditorTestHostProject[] projects)
        => new(projects);

    protected override void Dispose(bool finalize)
    {
        foreach (var document in Documents)
        {
            document.CloseTextView();
        }

        foreach (var document in AdditionalDocuments)
        {
            document.CloseTextView();
        }

        foreach (var document in AnalyzerConfigDocuments)
        {
            document.CloseTextView();
        }

        foreach (var document in ProjectionDocuments)
        {
            document.CloseTextView();
        }

        base.Dispose(finalize);
    }

    protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
    {
        var testDocument = GetTestDocument(document);
        Contract.ThrowIfNull(testDocument);
        testDocument.Update(newText);
    }

    protected override void ApplyAdditionalDocumentTextChanged(DocumentId document, SourceText newText)
    {
        var testDocument = GetTestAdditionalDocument(document);
        Contract.ThrowIfNull(testDocument);
        testDocument.Update(newText);
    }

    protected override void ApplyAnalyzerConfigDocumentTextChanged(DocumentId document, SourceText newText)
    {
        var testDocument = this.GetTestAnalyzerConfigDocument(document);
        Contract.ThrowIfNull(testDocument);
        testDocument.Update(newText);
    }

    public override void OpenDocument(DocumentId documentId, bool activate = true)
    {
        // Fetching the open SourceTextContainer implicitly opens the document.
        var testDocument = GetTestDocument(documentId);
        Contract.ThrowIfNull(testDocument);
        Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

        testDocument.GetOpenTextContainer();
    }

    public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
    {
        // Fetching the open SourceTextContainer implicitly opens the document.
        var testDocument = GetTestAdditionalDocument(documentId);
        Contract.ThrowIfNull(testDocument);
        Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

        testDocument.GetOpenTextContainer();
    }

    public override void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
    {
        // Fetching the open SourceTextContainer implicitly opens the document.
        var testDocument = GetTestAnalyzerConfigDocument(documentId);
        Contract.ThrowIfNull(testDocument);
        Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

        testDocument.GetOpenTextContainer();
    }

    public void OpenSourceGeneratedDocument(DocumentId documentId)
    {
        // Fetching the open SourceTextContainer implicitly opens the document.
        var testDocument = GetTestDocument(documentId);
        Contract.ThrowIfNull(testDocument);
        Contract.ThrowIfFalse(testDocument.IsSourceGenerated);

        testDocument.GetOpenTextContainer();
    }

    /// <summary>
    /// Creates a TestHostDocument backed by a projection buffer. The surface buffer is 
    /// described by a markup string with {|name:|} style pointers to annotated spans that can
    /// be found in one of a set of provided documents. Unnamed spans in the documents (which
    /// must have both endpoints inside an annotated spans) and in the surface buffer markup are
    /// mapped and included in the resulting document.
    /// 
    /// If the markup string has the caret indicator "$$", then the caret will be placed at the
    /// corresponding position. If it does not, then the first span mapped into the projection
    /// buffer that contains the caret from its document is used.
    ///
    /// The result is a new TestHostDocument backed by a projection buffer including tracking
    /// spans from any number of documents and inert text from the markup itself.
    /// 
    /// As an example, consider surface buffer markup
    ///  ABC [|DEF|] [|GHI[|JKL|]|]{|S1:|} [|MNO{|S2:|}PQR S$$TU|] {|S4:|}{|S5:|}{|S3:|}
    ///  
    /// This contains 4 unnamed spans and references to 5 spans that should be found and
    /// included. Consider an included base document created from the following markup:
    /// 
    ///  public class C
    ///  {
    ///      public void M1()
    ///      {
    ///          {|S1:int [|abc[|d$$ef|]|] = goo;|}
    ///          int y = goo;
    ///          {|S2:int [|def|] = goo;|}
    ///          int z = {|S3:123|} + {|S4:456|} + {|S5:789|};
    ///      }
    ///  }
    /// 
    /// The resulting projection buffer (with unnamed span markup preserved) would look like:
    ///  ABC [|DEF|] [|GHI[|JKL|]|]int [|abc[|d$$ef|]|] = goo; [|MNOint [|def|] = goo;PQR S$$TU|] 456789123
    /// 
    /// The union of unnamed spans from the surface buffer markup and each of the projected 
    /// spans is sorted as it would have been sorted by MarkupTestFile had it parsed the entire
    /// projection buffer as one file, which it would do in a stack-based manner. In our example,
    /// the order of the unnamed spans would be as follows:
    /// 
    ///  ABC [|DEF|] [|GHI[|JKL|]|]int [|abc[|d$$ef|]|] = goo; [|MNOint [|def|] = goo;PQR S$$TU|] 456789123
    ///       -----1       -----2            -------4                    -----6
    ///               ------------3     --------------5         --------------------------------7
    /// </summary>
    /// <param name="markup">Describes the surface buffer, and contains a mix of inert text, 
    /// named spans and unnamed spans. Any named spans must contain only the name portion 
    /// (e.g. {|Span1:|} which must match the name of a span in one of the baseDocuments. 
    /// Annotated spans cannot be nested but they can be adjacent, in which case order will be
    /// preserved. The markup may also contain the caret indicator.</param>
    /// <param name="baseDocuments">The set of documents from which the projection buffer 
    /// document will be composed.</param>
    /// <returns></returns>
    public EditorTestHostDocument CreateProjectionBufferDocument(
        string markup,
        IList<EditorTestHostDocument> baseDocuments,
        string path = "projectionbufferdocumentpath",
        ProjectionBufferOptions options = ProjectionBufferOptions.None,
        IProjectionEditResolver? editResolver = null)
    {
        GetSpansAndCaretFromSurfaceBufferMarkup(markup, baseDocuments,
            out var projectionBufferSpans, out var mappedSpans, out var mappedCaretLocation);

        var projectionBufferFactory = this.GetService<IProjectionBufferFactoryService>();
        var projectionBuffer = projectionBufferFactory.CreateProjectionBuffer(editResolver, projectionBufferSpans, options);

        // Add in mapped spans from each of the base documents
        foreach (var document in baseDocuments)
        {
            mappedSpans[string.Empty] = mappedSpans.TryGetValue(string.Empty, out var emptyTextSpans)
                ? emptyTextSpans
                : ImmutableArray<TextSpan>.Empty;
            foreach (var span in document.SelectedSpans)
            {
                var snapshotSpan = span.ToSnapshotSpan(document.GetTextBuffer().CurrentSnapshot);
                var mappedSpan = projectionBuffer.CurrentSnapshot.MapFromSourceSnapshot(snapshotSpan).Single();
                mappedSpans[string.Empty] = mappedSpans[string.Empty].Add(mappedSpan.ToTextSpan());
            }

            // Order unnamed spans as they would be ordered by the normal span finding 
            // algorithm in MarkupTestFile
            mappedSpans[string.Empty] = mappedSpans[string.Empty].OrderBy(s => s.End).ThenBy(s => -s.Start).ToImmutableArray();

            foreach (var (key, spans) in document.AnnotatedSpans)
            {
                mappedSpans[key] = mappedSpans.TryGetValue(key, out var textSpans) ? textSpans : ImmutableArray<TextSpan>.Empty;

                foreach (var span in spans)
                {
                    var snapshotSpan = span.ToSnapshotSpan(document.GetTextBuffer().CurrentSnapshot);
                    var mappedSpan = projectionBuffer.CurrentSnapshot.MapFromSourceSnapshot(snapshotSpan).Cast<Span?>().SingleOrDefault();
                    if (mappedSpan == null)
                    {
                        // not all span on subject buffer needs to exist on surface buffer
                        continue;
                    }

                    // but if they do, it must be only 1
                    mappedSpans[key] = mappedSpans[key].Add(mappedSpan.Value.ToTextSpan());
                }
            }
        }

        var projectionDocument = new EditorTestHostDocument(
            ExportProvider,
            languageServiceProvider: null,
            projectionBuffer.CurrentSnapshot.GetText(),
            path,
            path,
            mappedCaretLocation,
            mappedSpans,
            textBuffer: (ITextBuffer2)projectionBuffer);

        this.ProjectionDocuments.Add(projectionDocument);
        return projectionDocument;
    }

    private static void GetSpansAndCaretFromSurfaceBufferMarkup(
        string markup, IList<EditorTestHostDocument> baseDocuments,
        out IList<object> projectionBufferSpans,
        out Dictionary<string, ImmutableArray<TextSpan>> mappedMarkupSpans, out int? mappedCaretLocation)
    {
        projectionBufferSpans = new List<object>();
        var projectionBufferSpanStartingPositions = new List<int>();
        mappedCaretLocation = null;

        MarkupTestFile.GetPositionAndSpans(markup,
            out var inertText, out int? markupCaretLocation, out var markupSpans);

        var namedSpans = markupSpans.Where(kvp => kvp.Key != string.Empty);
        var sortedAndNamedSpans = namedSpans.OrderBy(kvp => kvp.Value.Single().Start)
                                            .ThenBy(kvp => markup.IndexOf("{|" + kvp.Key + ":", StringComparison.Ordinal));

        var currentPositionInInertText = 0;
        var currentPositionInProjectionBuffer = 0;

        // If the markup points to k spans, these k spans divide the inert text into k + 1
        // possibly empty substrings. When handling each span, also handle the inert text that
        // immediately precedes it. At the end, handle the trailing inert text
        foreach (var spanNameToListMap in sortedAndNamedSpans)
        {
            var spanName = spanNameToListMap.Key;
            var spanLocation = spanNameToListMap.Value.Single().Start;

            // Get any inert text between this and the previous span
            if (currentPositionInInertText < spanLocation)
            {
                var textToAdd = inertText[currentPositionInInertText..spanLocation];
                projectionBufferSpans.Add(textToAdd);
                projectionBufferSpanStartingPositions.Add(currentPositionInProjectionBuffer);

                // If the caret is in the markup and in this substring, calculate the final
                // caret location
                if (mappedCaretLocation == null &&
                    markupCaretLocation != null &&
                    currentPositionInInertText + textToAdd.Length >= markupCaretLocation)
                {
                    var caretOffsetInCurrentText = markupCaretLocation.Value - currentPositionInInertText;
                    mappedCaretLocation = currentPositionInProjectionBuffer + caretOffsetInCurrentText;
                }

                currentPositionInInertText += textToAdd.Length;
                currentPositionInProjectionBuffer += textToAdd.Length;
            }

            // Find and insert the span from the corresponding document
            var documentWithSpan = baseDocuments.FirstOrDefault(d => d.AnnotatedSpans.ContainsKey(spanName));
            if (documentWithSpan == null)
            {
                continue;
            }

            markupSpans.Remove(spanName);

            var matchingSpan = documentWithSpan.AnnotatedSpans[spanName].Single();
            var span = new Span(matchingSpan.Start, matchingSpan.Length);
            var trackingSpan = documentWithSpan.GetTextBuffer().CurrentSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);

            projectionBufferSpans.Add(trackingSpan);
            projectionBufferSpanStartingPositions.Add(currentPositionInProjectionBuffer);

            // If the caret is not in markup but is in this span, then calculate the final
            // caret location.  Note - if we find the caret marker in this document, then
            // we DO want to map it up, even if it's at the end of the span for this document.
            // This is not ambiguous for us, since we have explicit delimiters between the buffer
            // so it's clear which document the caret is in.
            if (mappedCaretLocation == null &&
                markupCaretLocation == null &&
                documentWithSpan.CursorPosition.HasValue &&
                (matchingSpan.Contains(documentWithSpan.CursorPosition.Value) || matchingSpan.End == documentWithSpan.CursorPosition.Value))
            {
                var caretOffsetInSpan = documentWithSpan.CursorPosition.Value - matchingSpan.Start;
                mappedCaretLocation = currentPositionInProjectionBuffer + caretOffsetInSpan;
            }

            currentPositionInProjectionBuffer += matchingSpan.Length;
        }

        // Handle any inert text after the final projected span
        if (currentPositionInInertText < inertText.Length - 1)
        {
            projectionBufferSpans.Add(inertText[currentPositionInInertText..]);
            projectionBufferSpanStartingPositions.Add(currentPositionInProjectionBuffer);

            if (mappedCaretLocation == null && markupCaretLocation != null && markupCaretLocation >= currentPositionInInertText)
            {
                var caretOffsetInCurrentText = markupCaretLocation.Value - currentPositionInInertText;
                mappedCaretLocation = currentPositionInProjectionBuffer + caretOffsetInCurrentText;
            }
        }

        MapMarkupSpans(markupSpans, out mappedMarkupSpans, projectionBufferSpans, projectionBufferSpanStartingPositions);
    }

    private static void MapMarkupSpans(
        IDictionary<string, ImmutableArray<TextSpan>> markupSpans,
        out Dictionary<string, ImmutableArray<TextSpan>> mappedMarkupSpans,
        IList<object> projectionBufferSpans, IList<int> projectionBufferSpanStartingPositions)
    {
        var tempMappedMarkupSpans = new Dictionary<string, PooledObjects.ArrayBuilder<TextSpan>>();

        foreach (var key in markupSpans.Keys)
        {
            tempMappedMarkupSpans[key] = PooledObjects.ArrayBuilder<TextSpan>.GetInstance();
            foreach (var markupSpan in markupSpans[key])
            {
                var positionInMarkup = 0;
                var spanIndex = 0;
                var markupSpanStart = markupSpan.Start;
                var markupSpanEndExclusive = markupSpan.Start + markupSpan.Length;
                int? spanStartLocation = null;
                int? spanEndLocationExclusive = null;

                foreach (var projectionSpan in projectionBufferSpans)
                {
                    if (projectionSpan is string text)
                    {
                        // this currently has a bug where it can't distinguish a markup of {|ProjectionMarkup:|}{|Markup1:|} and {|Markup1:{|ProjectionMarkup:|}|}
                        // it always map markup1 span as the later one.
                        // tracking issue - {|ProjectionMarkup:|}{|Markup1:|} and {|Markup1:{|ProjectionMarkup:|}|}
                        if (spanStartLocation == null && positionInMarkup <= markupSpanStart && markupSpanStart <= positionInMarkup + text.Length)
                        {
                            var offsetInText = markupSpanStart - positionInMarkup;
                            spanStartLocation = projectionBufferSpanStartingPositions[spanIndex] + offsetInText;
                        }

                        if (spanEndLocationExclusive == null && positionInMarkup <= markupSpanEndExclusive && markupSpanEndExclusive <= positionInMarkup + text.Length)
                        {
                            var offsetInText = markupSpanEndExclusive - positionInMarkup;
                            spanEndLocationExclusive = projectionBufferSpanStartingPositions[spanIndex] + offsetInText;
                            break;
                        }

                        positionInMarkup += text.Length;
                    }

                    spanIndex++;
                }

                tempMappedMarkupSpans[key].Add(new TextSpan(spanStartLocation!.Value, spanEndLocationExclusive!.Value - spanStartLocation.Value));
            }
        }

        mappedMarkupSpans = tempMappedMarkupSpans.ToDictionary(
            kvp => kvp.Key, kvp => kvp.Value.ToImmutableAndFree());
    }

    internal ITextBuffer2 GetOrCreateBufferForPath(string? filePath, IContentType contentType, string languageName, string initialText)
    {
        // If we don't have a file path we'll just make something up for the purpose of this dictionary so all
        // buffers are still held onto. This isn't a file name used in the workspace itself so it's unobservable.
        if (RoslynString.IsNullOrEmpty(filePath))
        {
            filePath = Guid.NewGuid().ToString();
        }

        return _createdTextBuffers.GetOrAdd(filePath, _ =>
        {
            var textBuffer = EditorFactory.CreateBuffer(ExportProvider, contentType, initialText);

            // Ensure that the editor options on the text buffer matches that of the options that can be directly set in the workspace
            var editorOptions = ExportProvider.GetExportedValue<IEditorOptionsFactoryService>().GetOptions(textBuffer);
            var globalOptions = GlobalOptions;

            editorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !globalOptions.GetOption(FormattingOptions2.UseTabs, languageName));
            editorOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, globalOptions.GetOption(FormattingOptions2.TabSize, languageName));
            editorOptions.SetOptionValue(DefaultOptions.IndentSizeOptionId, globalOptions.GetOption(FormattingOptions2.IndentationSize, languageName));

            return textBuffer;
        });
    }
}
