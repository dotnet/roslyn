// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public partial class TestWorkspace : Workspace
    {
        public const string WorkspaceName = "Test";

        public ExportProvider ExportProvider { get; }

        public bool CanApplyChangeDocument { get; set; }

        internal override bool CanChangeActiveContextDocument { get { return true; } }

        public IList<TestHostProject> Projects { get; }
        public IList<TestHostDocument> Documents { get; }
        public IList<TestHostDocument> AdditionalDocuments { get; }
        public IList<TestHostDocument> ProjectionDocuments { get; }

        private readonly BackgroundCompiler _backgroundCompiler;
        private readonly BackgroundParser _backgroundParser;

        public TestWorkspace()
            : this(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, WorkspaceName)
        {
        }

        public TestWorkspace(ExportProvider exportProvider, string workspaceKind = null, bool disablePartialSolutions = true)
            : base(MefV1HostServices.Create(exportProvider.AsExportProvider()), workspaceKind ?? WorkspaceName)
        {
            ResetThreadAffinity();

            this.TestHookPartialSolutionsDisabled = disablePartialSolutions;
            this.ExportProvider = exportProvider;
            this.Projects = new List<TestHostProject>();
            this.Documents = new List<TestHostDocument>();
            this.AdditionalDocuments = new List<TestHostDocument>();
            this.ProjectionDocuments = new List<TestHostDocument>();

            this.CanApplyChangeDocument = true;

            _backgroundCompiler = new BackgroundCompiler(this);
            _backgroundParser = new BackgroundParser(this);
            _backgroundParser.Start();
        }

        /// <summary>
        /// Reset the thread affinity, in particular the designated foreground thread, to the active 
        /// thread.  
        /// </summary>
        internal static void ResetThreadAffinity(ForegroundThreadData foregroundThreadData = null)
        {
            foregroundThreadData = foregroundThreadData ?? ForegroundThreadAffinitizedObject.CurrentForegroundThreadData;

            // HACK: When the platform team took over several of our components they created a copy
            // of ForegroundThreadAffinitizedObject.  This needs to be reset in the same way as our copy
            // does.  Reflection is the only choice at the moment. 
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Microsoft.VisualStudio.Language.Intellisense.Implementation.ForegroundThreadAffinitizedObject", throwOnError: false);
                if (type != null)
                {
                    type.GetField("foregroundThread", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, foregroundThreadData.Thread);
                    type.GetField("ForegroundTaskScheduler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, foregroundThreadData.TaskScheduler);

                    break;
                }
            }
        }

        protected internal override bool PartialSemanticsEnabled
        {
            get { return _backgroundCompiler != null; }
        }

        public TestHostDocument DocumentWithCursor { get { return Documents.Single(d => d.CursorPosition.HasValue && !d.IsLinkFile); } }

        protected override void OnDocumentTextChanged(Document document)
        {
            if (_backgroundParser != null)
            {
                _backgroundParser.Parse(document);
            }
        }

        protected override void OnDocumentClosing(DocumentId documentId)
        {
            if (_backgroundParser != null)
            {
                _backgroundParser.CancelParse(documentId);
            }
        }

        public new void RegisterText(SourceTextContainer text)
        {
            base.RegisterText(text);
        }

        protected override void Dispose(bool finalize)
        {
            var metadataAsSourceService = ExportProvider.GetExportedValues<IMetadataAsSourceFileService>().FirstOrDefault();
            if (metadataAsSourceService != null)
            {
                metadataAsSourceService.CleanupGeneratedFiles();
            }

            this.ClearSolutionData();

            foreach (var document in Documents)
            {
                document.CloseTextView();
            }

            foreach (var document in AdditionalDocuments)
            {
                document.CloseTextView();
            }

            foreach (var document in ProjectionDocuments)
            {
                document.CloseTextView();
            }

            var exceptions = ExportProvider.GetExportedValue<TestExtensionErrorHandler>().GetExceptions();

            if (exceptions.Count == 1)
            {
                throw exceptions.Single();
            }
            else if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }

            if (SynchronizationContext.Current != null)
            {
                Dispatcher.CurrentDispatcher.DoEvents();
            }

            if (_backgroundParser != null)
            {
                _backgroundParser.CancelAllParses();
            }

            base.Dispose(finalize);
        }

        internal void AddTestSolution(TestHostSolution solution)
        {
            this.OnSolutionAdded(SolutionInfo.Create(solution.Id, solution.Version, solution.FilePath, projects: solution.Projects.Select(p => p.ToProjectInfo())));
        }

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
            }

            this.OnProjectAdded(project.ToProjectInfo());
        }

        public new void OnProjectRemoved(ProjectId projectId)
        {
            base.OnProjectRemoved(projectId);
        }

        public new void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            base.OnProjectReferenceAdded(projectId, projectReference);
        }

        public new void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            base.OnProjectReferenceRemoved(projectId, projectReference);
        }

        public new void OnDocumentOpened(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext = true)
        {
            base.OnDocumentOpened(documentId, textContainer, isCurrentContext);
        }

        public void OnDocumentClosed(DocumentId documentId)
        {
            var testDocument = this.GetTestDocument(documentId);
            this.OnDocumentClosed(documentId, testDocument.Loader);
        }

        public new void OnParseOptionsChanged(ProjectId projectId, ParseOptions parseOptions)
        {
            base.OnParseOptionsChanged(projectId, parseOptions);
        }

        public new void OnDocumentRemoved(DocumentId documentId)
        {
            base.OnDocumentRemoved(documentId);
        }

        public new void OnDocumentSourceCodeKindChanged(DocumentId documentId, SourceCodeKind sourceCodeKind)
        {
            base.OnDocumentSourceCodeKindChanged(documentId, sourceCodeKind);
        }

        public DocumentId GetDocumentId(TestHostDocument hostDocument)
        {
            if (!Documents.Contains(hostDocument) && !AdditionalDocuments.Contains(hostDocument))
            {
                return null;
            }

            return hostDocument.Id;
        }

        public TestHostDocument GetTestDocument(DocumentId documentId)
        {
            return this.Documents.FirstOrDefault(d => d.Id == documentId);
        }

        public TestHostDocument GetTestAdditionalDocument(DocumentId documentId)
        {
            return this.AdditionalDocuments.FirstOrDefault(d => d.Id == documentId);
        }

        public TestHostProject GetTestProject(DocumentId documentId)
        {
            return GetTestProject(documentId.ProjectId);
        }

        public TestHostProject GetTestProject(ProjectId projectId)
        {
            return this.Projects.FirstOrDefault(p => p.Id == projectId);
        }

        public TServiceInterface GetService<TServiceInterface>()
        {
            return ExportProvider.GetExportedValue<TServiceInterface>();
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                case ApplyChangesKind.AddAdditionalDocument:
                case ApplyChangesKind.RemoveAdditionalDocument:
                    return true;

                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.ChangeAdditionalDocument:
                    return this.CanApplyChangeDocument;

                default:
                    return false;
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
        {
            var testDocument = this.GetTestDocument(document);
            testDocument.Update(newText);
        }

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            var hostDocument = new TestHostDocument(text.ToString(), info.Name, info.SourceCodeKind, info.Id);
            hostProject.AddDocument(hostDocument);
            this.OnDocumentAdded(hostDocument.ToDocumentInfo());
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            var hostProject = this.GetTestProject(documentId.ProjectId);
            var hostDocument = this.GetTestDocument(documentId);
            hostProject.RemoveDocument(hostDocument);
            this.OnDocumentRemoved(documentId);
        }

        protected override void ApplyAdditionalDocumentTextChanged(DocumentId document, SourceText newText)
        {
            var testDocument = this.GetTestAdditionalDocument(document);
            testDocument.Update(newText);
        }

        protected override void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
        {
            var hostProject = this.GetTestProject(info.Id.ProjectId);
            var hostDocument = new TestHostDocument(text.ToString(), info.Name, id: info.Id);
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

        internal override void SetDocumentContext(DocumentId documentId)
        {
            OnDocumentContextUpdated(documentId);
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
        ///          {|S1:int [|abc[|d$$ef|]|] = foo;|}
        ///          int y = foo;
        ///          {|S2:int [|def|] = foo;|}
        ///          int z = {|S3:123|} + {|S4:456|} + {|S5:789|};
        ///      }
        ///  }
        /// 
        /// The resulting projection buffer (with unnamed span markup preserved) would look like:
        ///  ABC [|DEF|] [|GHI[|JKL|]|]int [|abc[|d$$ef|]|] = foo; [|MNOint [|def|] = foo;PQR S$$TU|] 456789123
        /// 
        /// The union of unnamed spans from the surface buffer markup and each of the projected 
        /// spans is sorted as it would have been sorted by MarkupTestFile had it parsed the entire
        /// projection buffer as one file, which it would do in a stack-based manner. In our example,
        /// the order of the unnamed spans would be as follows:
        /// 
        ///  ABC [|DEF|] [|GHI[|JKL|]|]int [|abc[|d$$ef|]|] = foo; [|MNOint [|def|] = foo;PQR S$$TU|] 456789123
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
        public TestHostDocument CreateProjectionBufferDocument(string markup, IList<TestHostDocument> baseDocuments, string languageName, string path = "projectionbufferdocumentpath", ProjectionBufferOptions options = ProjectionBufferOptions.None, IProjectionEditResolver editResolver = null)
        {
            IList<object> projectionBufferSpans;
            Dictionary<string, IList<TextSpan>> mappedSpans;
            int? mappedCaretLocation;

            GetSpansAndCaretFromSurfaceBufferMarkup(markup, baseDocuments, out projectionBufferSpans, out mappedSpans, out mappedCaretLocation);

            var projectionBufferFactory = this.GetService<IProjectionBufferFactoryService>();
            var projectionBuffer = projectionBufferFactory.CreateProjectionBuffer(editResolver, projectionBufferSpans, options);

            // Add in mapped spans from each of the base documents
            foreach (var document in baseDocuments)
            {
                mappedSpans[string.Empty] = mappedSpans.ContainsKey(string.Empty) ? mappedSpans[string.Empty] : new List<TextSpan>();
                foreach (var span in document.SelectedSpans)
                {
                    var snapshotSpan = span.ToSnapshotSpan(document.TextBuffer.CurrentSnapshot);
                    var mappedSpan = projectionBuffer.CurrentSnapshot.MapFromSourceSnapshot(snapshotSpan).Single();
                    mappedSpans[string.Empty].Add(mappedSpan.ToTextSpan());
                }

                // Order unnamed spans as they would be ordered by the normal span finding 
                // algorithm in MarkupTestFile
                mappedSpans[string.Empty] = mappedSpans[string.Empty].OrderBy(s => s.End).ThenBy(s => -s.Start).ToList();

                foreach (var kvp in document.AnnotatedSpans)
                {
                    mappedSpans[kvp.Key] = mappedSpans.ContainsKey(kvp.Key) ? mappedSpans[kvp.Key] : new List<TextSpan>();

                    foreach (var span in kvp.Value)
                    {
                        var snapshotSpan = span.ToSnapshotSpan(document.TextBuffer.CurrentSnapshot);
                        var mappedSpan = projectionBuffer.CurrentSnapshot.MapFromSourceSnapshot(snapshotSpan).Single();
                        mappedSpans[kvp.Key].Add(mappedSpan.ToTextSpan());
                    }
                }
            }

            var languageServices = this.Services.GetLanguageServices(languageName);

            var projectionDocument = new TestHostDocument(
                TestExportProvider.ExportProviderWithCSharpAndVisualBasic,
                languageServices,
                projectionBuffer,
                path,
                mappedCaretLocation,
                mappedSpans);

            this.ProjectionDocuments.Add(projectionDocument);
            return projectionDocument;
        }

        private void GetSpansAndCaretFromSurfaceBufferMarkup(string markup, IList<TestHostDocument> baseDocuments, out IList<object> projectionBufferSpans, out Dictionary<string, IList<TextSpan>> mappedMarkupSpans, out int? mappedCaretLocation)
        {
            IDictionary<string, IList<TextSpan>> markupSpans;
            projectionBufferSpans = new List<object>();
            var projectionBufferSpanStartingPositions = new List<int>();
            mappedCaretLocation = null;
            string inertText;
            int? markupCaretLocation;

            MarkupTestFile.GetPositionAndSpans(markup, out inertText, out markupCaretLocation, out markupSpans);

            var namedSpans = markupSpans.Where(kvp => kvp.Key != string.Empty);
            var sortedAndNamedSpans = namedSpans.OrderBy(kvp => kvp.Value.Single().Start)
                                                .ThenBy(kvp => markup.IndexOf("{|" + kvp.Key + ":|}", StringComparison.Ordinal));

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
                    var textToAdd = inertText.Substring(currentPositionInInertText, spanLocation - currentPositionInInertText);
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
                var trackingSpan = documentWithSpan.TextBuffer.CurrentSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);

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
                projectionBufferSpans.Add(inertText.Substring(currentPositionInInertText));
                projectionBufferSpanStartingPositions.Add(currentPositionInProjectionBuffer);

                if (mappedCaretLocation == null && markupCaretLocation != null && markupCaretLocation >= currentPositionInInertText)
                {
                    var caretOffsetInCurrentText = markupCaretLocation.Value - currentPositionInInertText;
                    mappedCaretLocation = currentPositionInProjectionBuffer + caretOffsetInCurrentText;
                }
            }

            MapMarkupSpans(markupSpans, out mappedMarkupSpans, projectionBufferSpans, projectionBufferSpanStartingPositions);
        }

        private void MapMarkupSpans(IDictionary<string, IList<TextSpan>> markupSpans, out Dictionary<string, IList<TextSpan>> mappedMarkupSpans, IList<object> projectionBufferSpans, IList<int> projectionBufferSpanStartingPositions)
        {
            mappedMarkupSpans = new Dictionary<string, IList<TextSpan>>();

            foreach (string key in markupSpans.Keys)
            {
                mappedMarkupSpans[key] = new List<TextSpan>();
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
                        var text = projectionSpan as string;

                        if (text != null)
                        {
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

                    mappedMarkupSpans[key].Add(new TextSpan(spanStartLocation.Value, spanEndLocationExclusive.Value - spanStartLocation.Value));
                }
            }
        }

        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            OnDocumentOpened(documentId, this.CurrentSolution.GetDocument(documentId).GetTextAsync().Result.Container);
        }

        public override void CloseDocument(DocumentId documentId)
        {
            var currentDoc = this.CurrentSolution.GetDocument(documentId);

            OnDocumentClosed(documentId,
                TextLoader.From(TextAndVersion.Create(currentDoc.GetTextAsync().Result, currentDoc.GetTextVersionAsync().Result)));
        }

        public void ChangeDocument(DocumentId documentId, SourceText text)
        {
            var oldSolution = this.CurrentSolution;
            var newSolution = this.SetCurrentSolution(oldSolution.WithDocumentText(documentId, text));

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public void ChangeAdditionalDocument(DocumentId documentId, SourceText text)
        {
            var oldSolution = this.CurrentSolution;
            var newSolution = this.SetCurrentSolution(oldSolution.WithAdditionalDocumentText(documentId, text));

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentChanged, oldSolution, newSolution, documentId.ProjectId, documentId);
        }

        public void ChangeProject(ProjectId projectId, Solution solution)
        {
            var oldSolution = this.CurrentSolution;
            var newSolution = this.SetCurrentSolution(solution);

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
        }

        public new void ClearSolution()
        {
            base.ClearSolution();
        }

        public void ChangeSolution(Solution solution)
        {
            var oldSolution = this.CurrentSolution;
            var newSolution = this.SetCurrentSolution(solution);

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }
    }
}
