// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class TestHostDocument
    {
        private static readonly ImmutableArray<string> s_defaultRoles = ImmutableArray.Create<string>
            (PredefinedTextViewRoles.Analyzable,
            PredefinedTextViewRoles.Document,
            PredefinedTextViewRoles.Editable,
            PredefinedTextViewRoles.Interactive,
            PredefinedTextViewRoles.Zoomable);

        private readonly ExportProvider? _exportProvider;
        private HostLanguageServices? _languageServiceProvider;
        private readonly string _initialText;
        private IWpfTextView? _textView;

        private DocumentId? _id;
        private TestHostProject? _project;

        /// <summary>
        /// The <see cref="ITextBuffer2"/> for this document. Null if not yet created.
        /// </summary>
        private ITextBuffer2? _textBuffer;

        /// <summary>
        /// The <see cref="ITextSnapshot"/> when the buffer was first created, which can be used for tracking changes to the current buffer.
        /// </summary>
        private ITextSnapshot? _initialTextSnapshot;
        private readonly IReadOnlyList<string>? _folders;
        private readonly IDocumentServiceProvider? _documentServiceProvider;
        private readonly ImmutableArray<string> _roles;
        private readonly TestDocumentLoader _loader;

        public DocumentId Id
        {
            get
            {
                // For source generated documents, the workspace generates the ID. Thus we won't
                // know it until we have a workspace we can go and get the ID from. We of course could
                // duplicate the algorithm but this lets us keep this code oblivious to the internals
                // of the workspace implementation.
                if (IsSourceGenerated && _id is null)
                {
                    var workspace = _languageServiceProvider!.WorkspaceServices.Workspace;
                    var project = workspace.CurrentSolution.GetRequiredProject(_project!.Id);
                    var sourceGeneratedDocuments = project.GetSourceGeneratedDocumentsAsync(CancellationToken.None).AsTask().Result;
                    _id = sourceGeneratedDocuments.Single(d => d.FilePath == this.FilePath).Id;
                }

                Contract.ThrowIfNull(_id);
                return _id;
            }
        }

        public TestHostProject Project
        {
            get
            {
                Contract.ThrowIfNull(_project);
                return _project;
            }
        }

        public string Name { get; }
        public SourceCodeKind SourceCodeKind { get; }
        public string? FilePath { get; }
        public SourceHashAlgorithm ChecksumAlgorithm { get; } = SourceHashAlgorithms.Default;

        public int? CursorPosition { get; }
        public IList<TextSpan> SelectedSpans { get; } = new List<TextSpan>();
        public IDictionary<string, ImmutableArray<TextSpan>> AnnotatedSpans { get; } = new Dictionary<string, ImmutableArray<TextSpan>>();

        /// <summary>
        /// If a file exists in ProjectA and is added to ProjectB as a link, then this returns
        /// false for the document in ProjectA and true for the document in ProjectB.
        /// </summary>
        public bool IsLinkFile { get; }

        /// <summary>
        /// If this is a source generated file, the source generator that produced this document.
        /// </summary>
        public ISourceGenerator? Generator;

        /// <summary>
        /// Returns true if this will be a source generated file instead of a regular one.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Generator))]
        public bool IsSourceGenerated => Generator is not null;

        internal TestHostDocument(
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
            ImmutableArray<string> roles = default,
            ITextBuffer2? textBuffer = null,
            ISourceGenerator? generator = null)
        {
            Contract.ThrowIfNull(filePath);

            _exportProvider = exportProvider;
            _languageServiceProvider = languageServiceProvider;
            _initialText = code;
            Name = name;
            FilePath = filePath;
            _folders = folders;
            this.CursorPosition = cursorPosition;
            SourceCodeKind = sourceCodeKind;
            this.IsLinkFile = isLinkFile;
            Generator = generator;
            _documentServiceProvider = documentServiceProvider;
            _roles = roles.IsDefault ? s_defaultRoles : roles;

            if (spans.TryGetValue(string.Empty, out var textSpans))
            {
                this.SelectedSpans = textSpans;
            }

            foreach (var namedSpanList in spans.Where(s => s.Key != string.Empty))
            {
                this.AnnotatedSpans.Add(namedSpanList);
            }

            _loader = new TestDocumentLoader(this, _initialText);

            if (textBuffer != null)
            {
                _textBuffer = textBuffer;
                _initialTextSnapshot = textBuffer.CurrentSnapshot;
            }
        }

        internal TestHostDocument(
            string text = "",
            string displayName = "",
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            DocumentId? id = null,
            string? filePath = null,
            IReadOnlyList<string>? folders = null,
            ExportProvider? exportProvider = null,
            IDocumentServiceProvider? documentServiceProvider = null)
        {
            _exportProvider = exportProvider;
            _id = id;
            _initialText = text;
            Name = displayName;
            SourceCodeKind = sourceCodeKind;
            _loader = new TestDocumentLoader(this, text);
            FilePath = filePath;
            _folders = folders;
            _roles = s_defaultRoles;
            _documentServiceProvider = documentServiceProvider;
        }

        internal void SetProject(TestHostProject project)
        {
            _project = project;

            // For generated documents, we need to fetch the IDs from the workspace later
            if (!IsSourceGenerated)
            {
                if (_id == null)
                {
                    _id = DocumentId.CreateNewId(project.Id, this.Name);
                }
                else
                {
                    Contract.ThrowIfFalse(project.Id == this.Id.ProjectId);
                }
            }

            _languageServiceProvider ??= project.LanguageServiceProvider;
        }

        private sealed class TestDocumentLoader : TextLoader
        {
            private readonly TestHostDocument _hostDocument;
            private readonly string _text;

            internal TestDocumentLoader(TestHostDocument hostDocument, string text)
            {
                _hostDocument = hostDocument;
                _text = text;
            }

            internal override string? FilePath
                => _hostDocument.FilePath;

            public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
                => Task.FromResult(TextAndVersion.Create(SourceText.From(_text, encoding: null, options.ChecksumAlgorithm), VersionStamp.Create(), _hostDocument.FilePath));
        }

        public TextLoader Loader => _loader;

        public IWpfTextView GetTextView()
        {
            if (_textView == null)
            {
                Contract.ThrowIfNull(_exportProvider, $"Can only create text view for {nameof(TestHostDocument)} created with {nameof(ExportProvider)}");
                WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} through {nameof(TestHostDocument)}.{nameof(GetTextView)}");

                var factory = _exportProvider.GetExportedValue<ITextEditorFactoryService>();

                // Every default role but outlining. Starting in 15.2, the editor
                // OutliningManager imports JoinableTaskContext in a way that's 
                // difficult to satisfy in our unit tests. Since we don't directly
                // depend on it, just disable it
                var roles = factory.CreateTextViewRoleSet(_roles);
                _textView = factory.CreateTextView(this.GetTextBuffer(), roles);
                if (this.CursorPosition.HasValue)
                {
                    _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextSnapshot, CursorPosition.Value));
                }
                else if (this.SelectedSpans.IsSingle())
                {
                    var span = this.SelectedSpans.Single();
                    _textView.Selection.Select(new SnapshotSpan(_textView.TextSnapshot, new Span(span.Start, span.Length)), false);
                }
            }

            return _textView;
        }

        public ITextBuffer2 GetTextBuffer()
        {
            var workspace = (TestWorkspace?)_languageServiceProvider?.WorkspaceServices.Workspace;

            if (_textBuffer == null)
            {
                Contract.ThrowIfNull(_languageServiceProvider, $"To get a text buffer for a {nameof(TestHostDocument)}, it must have been parented in a project.");
                var contentType = _languageServiceProvider.GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType();

                _textBuffer = workspace!.GetOrCreateBufferForPath(FilePath, contentType, _languageServiceProvider.Language, _initialText);
                _initialTextSnapshot = _textBuffer.CurrentSnapshot;
            }

            if (workspace != null)
            {
                // Open (or reopen) any files that were closed in this call. We do this for all linked copies at once.
                foreach (var linkedId in workspace.CurrentSolution.GetDocumentIdsWithFilePath(FilePath).Concat(this.Id))
                {
                    if (workspace.IsDocumentOpen(linkedId))
                        continue;

                    if (workspace.GetTestDocument(linkedId) is { } testDocument)
                    {
                        if (testDocument.IsSourceGenerated)
                        {
                            var threadingContext = workspace.GetService<IThreadingContext>();
                            var document = threadingContext.JoinableTaskFactory.Run(() => workspace.CurrentSolution.GetSourceGeneratedDocumentAsync(testDocument.Id, CancellationToken.None).AsTask());
                            Contract.ThrowIfNull(document);

                            workspace.OnSourceGeneratedDocumentOpened(_textBuffer.AsTextContainer(), document);
                        }
                        else
                        {
                            // If there is a linked file, we'll start the non-linked one as being the primary context, which some tests depend on.
                            workspace.OnDocumentOpened(linkedId, _textBuffer.AsTextContainer(), isCurrentContext: !testDocument.IsLinkFile);
                        }
                    }
                    else if (workspace.GetTestAdditionalDocument(linkedId) is { } testAdditionalDocument)
                    {
                        workspace.OnAdditionalDocumentOpened(linkedId, _textBuffer.AsTextContainer());
                    }
                    else if (workspace.GetTestAnalyzerConfigDocument(linkedId) is { } testAnalyzerConfigDocument)
                    {
                        workspace.OnAnalyzerConfigDocumentOpened(linkedId, _textBuffer.AsTextContainer());
                    }
                }
            }

            return _textBuffer;
        }

        public SourceTextContainer GetOpenTextContainer()
            => this.GetTextBuffer().AsTextContainer();

        public IReadOnlyList<string> Folders
        {
            get
            {
                return _folders ?? ImmutableArray.Create<string>();
            }
        }

        // TODO: delete this
        public ITextSnapshot InitialTextSnapshot
        {
            get
            {
                Contract.ThrowIfNull(_initialTextSnapshot);
                return _initialTextSnapshot;
            }
        }

        internal void Update(SourceText newText)
        {
            var buffer = GetTextBuffer();
            using (var edit = buffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
            {
                var oldText = buffer.CurrentSnapshot.AsText();
                var changes = newText.GetTextChanges(oldText);

                foreach (var change in changes)
                {
                    edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                }

                edit.Apply();
            }
        }

        private void Update(string newText)
        {
            using (var edit = this.GetTextBuffer().CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
            {
                edit.Replace(new Span(0, this.GetTextBuffer().CurrentSnapshot.Length), newText);
                edit.Apply();
            }
        }

        internal void CloseTextView()
        {
            if (_textView != null && !_textView.IsClosed)
            {
                _textView.Close();
                _textView = null;
            }
        }

        public DocumentInfo ToDocumentInfo()
        {
            Contract.ThrowIfTrue(IsSourceGenerated, "We shouldn't be producing a DocumentInfo for a source generated document.");
            return DocumentInfo.Create(Id, Name, Folders, SourceCodeKind, Loader, FilePath, isGenerated: false)
                .WithDocumentServiceProvider(_documentServiceProvider);
        }
    }
}
