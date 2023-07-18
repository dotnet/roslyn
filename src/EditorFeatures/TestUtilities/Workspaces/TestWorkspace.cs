// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
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

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public partial class TestWorkspace : Workspace, ILspWorkspace
    {
        public ExportProvider ExportProvider { get; }
        public TestComposition? Composition { get; }

        public bool CanApplyChangeDocument { get; set; }

        internal override bool CanChangeActiveContextDocument { get { return true; } }

        public IList<TestHostProject> Projects { get; }
        public IList<TestHostDocument> Documents { get; }
        public IList<TestHostDocument> AdditionalDocuments { get; }
        public IList<TestHostDocument> AnalyzerConfigDocuments { get; }
        public IList<TestHostDocument> ProjectionDocuments { get; }
        internal IGlobalOptionService GlobalOptions { get; }

        internal override bool IgnoreUnchangeableDocumentsWhenApplyingChanges { get; }

        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        private readonly Dictionary<string, ITextBuffer2> _createdTextBuffers = new();
        private readonly string _workspaceKind;
        private readonly bool _supportsLspMutation;

        internal TestWorkspace(
            TestComposition? composition = null,
            string? workspaceKind = WorkspaceKind.Host,
            Guid solutionTelemetryId = default,
            bool disablePartialSolutions = true,
            bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
            WorkspaceConfigurationOptions? configurationOptions = null,
            bool supportsLspMutation = false)
            : base(GetHostServices(ref composition, configurationOptions != null), workspaceKind ?? WorkspaceKind.Host)
        {
            this.Composition = composition;
            this.ExportProvider = composition.ExportProviderFactory.CreateExportProvider();

            var partialSolutionsTestHook = Services.GetRequiredService<IWorkpacePartialSolutionsTestHook>();
            partialSolutionsTestHook.IsPartialSolutionDisabled = disablePartialSolutions;

            // configure workspace before creating any solutions:
            if (configurationOptions != null)
            {
                var workspaceConfigurationService = GetService<TestWorkspaceConfigurationService>();
                workspaceConfigurationService.Options = configurationOptions.Value;
            }

            SetCurrentSolutionEx(CreateSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()).WithTelemetryId(solutionTelemetryId)));

            _workspaceKind = workspaceKind ?? WorkspaceKind.Host;
            this.Projects = new List<TestHostProject>();
            this.Documents = new List<TestHostDocument>();
            this.AdditionalDocuments = new List<TestHostDocument>();
            this.AnalyzerConfigDocuments = new List<TestHostDocument>();
            this.ProjectionDocuments = new List<TestHostDocument>();

            this.CanApplyChangeDocument = true;
            this.IgnoreUnchangeableDocumentsWhenApplyingChanges = ignoreUnchangeableDocumentsWhenApplyingChanges;
            _supportsLspMutation = supportsLspMutation;
            this.GlobalOptions = GetService<IGlobalOptionService>();

            if (Services.GetService<INotificationService>() is INotificationServiceCallback callback)
            {
                // Avoid showing dialogs in tests by default
                callback.NotificationCallback = (message, title, severity) =>
                {
                    var severityText = severity switch
                    {
                        NotificationSeverity.Information => "💡",
                        NotificationSeverity.Warning => "⚠",
                        _ => "❌"
                    };

                    var fullMessage = string.IsNullOrEmpty(title)
                        ? message
                        : $"{title}:{Environment.NewLine}{Environment.NewLine}{message}";

                    throw new InvalidOperationException($"{severityText} {fullMessage}");
                };
            }

            _metadataAsSourceFileService = ExportProvider.GetExportedValues<IMetadataAsSourceFileService>().FirstOrDefault();
        }

        private static HostServices GetHostServices([NotNull] ref TestComposition? composition, bool hasWorkspaceConfigurationOptions)
        {
            composition ??= EditorTestCompositions.EditorFeatures;

            if (hasWorkspaceConfigurationOptions)
            {
                composition = composition.AddParts(typeof(TestWorkspaceConfigurationService));
            }

            return composition.GetHostServices();
        }

        protected internal override bool PartialSemanticsEnabled => true;

        public TestHostDocument DocumentWithCursor
            => Documents.Single(d => d.CursorPosition.HasValue && !d.IsLinkFile);

        public new void RegisterText(SourceTextContainer text)
            => base.RegisterText(text);

        protected override void Dispose(bool finalize)
        {
            _metadataAsSourceFileService?.CleanupGeneratedFiles();

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

        internal void AddTestSolution(TestHostSolution solution)
            => this.OnSolutionAdded(SolutionInfo.Create(solution.Id, solution.Version, solution.FilePath, projects: solution.Projects.Select(p => p.ToProjectInfo())));

        public void AddTestProject(TestHostProject project)
        {
            if (!this.Projects.Contains(project))
            {
                this.Projects.Add(project);

                foreach (var doc in project.Documents)
                {
                    this.Documents.Add(doc);
                }

                foreach (var doc in project.AdditionalDocuments)
                {
                    this.AdditionalDocuments.Add(doc);
                }

                foreach (var doc in project.AnalyzerConfigDocuments)
                {
                    this.AnalyzerConfigDocuments.Add(doc);
                }
            }

            this.OnProjectAdded(project.ToProjectInfo());
        }

        public new void OnProjectRemoved(ProjectId projectId)
            => base.OnProjectRemoved(projectId);

        public new void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
            => base.OnProjectReferenceAdded(projectId, projectReference);

        public new void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
            => base.OnProjectReferenceRemoved(projectId, projectReference);

        public new void OnDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
            => base.OnDocumentOpened(documentId, textContainer, isCurrentContext);

        public new void OnParseOptionsChanged(ProjectId projectId, ParseOptions parseOptions)
            => base.OnParseOptionsChanged(projectId, parseOptions);

        public new void OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
            => base.OnAnalyzerReferenceAdded(projectId, analyzerReference);

        public void OnDocumentRemoved(DocumentId documentId, bool closeDocument = false)
        {
            if (closeDocument && this.IsDocumentOpen(documentId))
            {
                this.CloseDocument(documentId);
            }

            base.OnDocumentRemoved(documentId);
        }

        public new void OnDocumentSourceCodeKindChanged(DocumentId documentId, SourceCodeKind sourceCodeKind)
            => base.OnDocumentSourceCodeKindChanged(documentId, sourceCodeKind);

        public DocumentId? GetDocumentId(TestHostDocument hostDocument)
        {
            if (!Documents.Contains(hostDocument) &&
                !AdditionalDocuments.Contains(hostDocument) &&
                !AnalyzerConfigDocuments.Contains(hostDocument))
            {
                return null;
            }

            return hostDocument.Id;
        }

        public TestHostDocument GetTestDocument(DocumentId documentId)
            => this.Documents.FirstOrDefault(d => d.Id == documentId);

        public TestHostDocument GetTestAdditionalDocument(DocumentId documentId)
            => this.AdditionalDocuments.FirstOrDefault(d => d.Id == documentId);

        public TestHostDocument GetTestAnalyzerConfigDocument(DocumentId documentId)
            => this.AnalyzerConfigDocuments.FirstOrDefault(d => d.Id == documentId);

        public TestHostProject GetTestProject(DocumentId documentId)
            => GetTestProject(documentId.ProjectId);

        public TestHostProject GetTestProject(ProjectId projectId)
            => this.Projects.FirstOrDefault(p => p.Id == projectId);

        public TServiceInterface GetService<TServiceInterface>()
            => ExportProvider.GetExportedValue<TServiceInterface>();

        public TServiceInterface GetService<TServiceInterface>(string contentType)
        {
            var values = ExportProvider.GetExports<TServiceInterface, ContentTypeMetadata>();
            return values.Single(value => value.Metadata.ContentTypes.Contains(contentType)).Value;
        }

        public TServiceInterface GetService<TServiceInterface>(string contentType, string name)
        {
            var values = ExportProvider.GetExports<TServiceInterface, OrderableContentTypeMetadata>();
            return values.Single(value => value.Metadata.Name == name && value.Metadata.ContentTypes.Contains(contentType)).Value;
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                    return KindSupportsAddRemoveDocument();

                case ApplyChangesKind.AddAdditionalDocument:
                case ApplyChangesKind.RemoveAdditionalDocument:
                case ApplyChangesKind.AddAnalyzerConfigDocument:
                case ApplyChangesKind.RemoveAnalyzerConfigDocument:
                case ApplyChangesKind.AddAnalyzerReference:
                case ApplyChangesKind.RemoveAnalyzerReference:
                case ApplyChangesKind.AddSolutionAnalyzerReference:
                case ApplyChangesKind.RemoveSolutionAnalyzerReference:
                    return true;

                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.ChangeAdditionalDocument:
                case ApplyChangesKind.ChangeAnalyzerConfigDocument:
                case ApplyChangesKind.ChangeDocumentInfo:
                    return this.CanApplyChangeDocument;

                case ApplyChangesKind.AddProjectReference:
                case ApplyChangesKind.AddMetadataReference:
                    return true;

                default:
                    return false;
            }
        }

        private bool KindSupportsAddRemoveDocument()
            => _workspaceKind switch
            {
                WorkspaceKind.MiscellaneousFiles => false,
                WorkspaceKind.Interactive => false,
                _ => true
            };

        protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
        {
            var testDocument = this.GetTestDocument(document);
            testDocument.Update(newText);
        }

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            var hostDocument = new TestHostDocument(
                text.ToString(), info.Name, info.SourceCodeKind,
                info.Id, info.FilePath, info.Folders, ExportProvider,
                info.DocumentServiceProvider);
            hostProject.AddDocument(hostDocument);
            this.OnDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            var hostDocument = this.GetTestDocument(documentId);
            hostProject.RemoveDocument(hostDocument);
            this.OnDocumentRemoved(documentId, closeDocument: true);
        }

        protected override void ApplyAdditionalDocumentTextChanged(DocumentId document, SourceText newText)
        {
            var testDocument = this.GetTestAdditionalDocument(document);
            testDocument.Update(newText);
        }

        protected override void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            var hostDocument = new TestHostDocument(text.ToString(), info.Name, id: info.Id, exportProvider: ExportProvider);
            hostProject.AddAdditionalDocument(hostDocument);
            this.OnAdditionalDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            var hostDocument = this.GetTestAdditionalDocument(documentId);
            hostProject.RemoveAdditionalDocument(hostDocument);
            this.OnAdditionalDocumentRemoved(documentId);
        }

        protected override void ApplyAnalyzerConfigDocumentTextChanged(DocumentId document, SourceText newText)
        {
            var testDocument = this.GetTestAnalyzerConfigDocument(document);
            testDocument.Update(newText);
        }

        protected override void ApplyAnalyzerConfigDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            var hostDocument = new TestHostDocument(text.ToString(), info.Name, id: info.Id, filePath: info.FilePath, folders: info.Folders, exportProvider: ExportProvider);
            hostProject.AddAnalyzerConfigDocument(hostDocument);
            this.OnAnalyzerConfigDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyAnalyzerConfigDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            var hostDocument = this.GetTestAnalyzerConfigDocument(documentId);
            hostProject.RemoveAnalyzerConfigDocument(hostDocument);
            this.OnAnalyzerConfigDocumentRemoved(documentId);
        }

        protected override void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            if (projectChanges.OldProject.FilePath != projectChanges.NewProject.FilePath)
            {
                var hostProject = this.GetTestProject(projectChanges.NewProject.Id);
                hostProject.OnProjectFilePathChanged(projectChanges.NewProject.FilePath);
                base.OnProjectNameChanged(projectChanges.NewProject.Id, projectChanges.NewProject.Name, projectChanges.NewProject.FilePath);
            }

            base.ApplyProjectChanges(projectChanges);
        }

        internal override void SetDocumentContext(DocumentId documentId)
            => OnDocumentContextUpdated(documentId);

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
        public TestHostDocument CreateProjectionBufferDocument(
            string markup,
            IList<TestHostDocument> baseDocuments,
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

            var projectionDocument = new TestHostDocument(
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
            string markup, IList<TestHostDocument> baseDocuments,
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

        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            // Fetching the open SourceTextContainer implicitly opens the document.
            var testDocument = GetTestDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

            testDocument.GetOpenTextContainer();
        }

        /// <summary>
        /// Overriding base impl so that when we close a document it goes back to the initial state when the test
        /// workspace was loaded, throwing away any changes made to the open version.
        /// </summary>
        internal override ValueTask TryOnDocumentClosedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(this._supportsLspMutation);

            var testDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

            this.OnDocumentClosedEx(documentId, testDocument.Loader, requireDocumentPresentAndOpen: false);
            return ValueTaskFactory.CompletedTask;
        }

        public override void CloseDocument(DocumentId documentId)
        {
            var testDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            this.OnDocumentClosed(documentId, testDocument.Loader);
        }

        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            // Fetching the open SourceTextContainer implicitly opens the document.
            var testDocument = GetTestAdditionalDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

            testDocument.GetOpenTextContainer();
        }

        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            var testDocument = this.GetTestAdditionalDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            this.OnAdditionalDocumentClosed(documentId, testDocument.Loader);
        }

        public override void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
        {
            // Fetching the open SourceTextContainer implicitly opens the document.
            var testDocument = GetTestAnalyzerConfigDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);

            testDocument.GetOpenTextContainer();
        }

        public override void CloseAnalyzerConfigDocument(DocumentId documentId)
        {
            var testDocument = this.GetTestAnalyzerConfigDocument(documentId);
            Contract.ThrowIfTrue(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            this.OnAnalyzerConfigDocumentClosed(documentId, testDocument.Loader);
        }

        public void OpenSourceGeneratedDocument(DocumentId documentId)
        {
            // Fetching the open SourceTextContainer implicitly opens the document.
            var testDocument = GetTestDocument(documentId);
            Contract.ThrowIfFalse(testDocument.IsSourceGenerated);

            testDocument.GetOpenTextContainer();
        }

        public async Task CloseSourceGeneratedDocumentAsync(DocumentId documentId)
        {
            var testDocument = this.GetTestDocument(documentId);
            Contract.ThrowIfFalse(testDocument.IsSourceGenerated);
            Contract.ThrowIfFalse(IsDocumentOpen(documentId));

            var document = await CurrentSolution.GetSourceGeneratedDocumentAsync(documentId, CancellationToken.None);
            Contract.ThrowIfNull(document);
            OnSourceGeneratedDocumentClosed(document);
        }

        public Task ChangeDocumentAsync(DocumentId documentId, SourceText text)
        {
            return ChangeDocumentAsync(documentId, this.CurrentSolution.WithDocumentText(documentId, text));
        }

        public Task ChangeDocumentAsync(DocumentId documentId, Solution solution)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public Task AddDocumentAsync(DocumentInfo documentInfo)
        {
            var documentId = documentInfo.Id;

            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(this.CurrentSolution.AddDocument(documentInfo));

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentAdded, oldSolution, newSolution, documentId: documentId);
        }

        public void ChangeAdditionalDocument(DocumentId documentId, SourceText text)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(this.CurrentSolution.WithAdditionalDocumentText(documentId, text));

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public void ChangeAnalyzerConfigDocument(DocumentId documentId, SourceText text)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(this.CurrentSolution.WithAnalyzerConfigDocumentText(documentId, text));

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AnalyzerConfigDocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public Task ChangeProjectAsync(ProjectId projectId, Solution solution)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
        }

        public new void ClearSolution()
            => base.ClearSolution();

        public Task ChangeSolutionAsync(Solution solution)
        {
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            return this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
            => true;

        internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        {
            // VisualStudioWorkspace asserts the main thread for this call, so do the same thing here to catch tests
            // that fail to account for this possibility.
            var threadingContext = ExportProvider.GetExportedValue<IThreadingContext>();
            Contract.ThrowIfFalse(threadingContext.HasMainThread && threadingContext.JoinableTaskContext.IsOnMainThread);
            return true;
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
}
