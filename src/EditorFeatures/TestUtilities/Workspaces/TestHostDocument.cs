// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.EditorUtilities;
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

        private readonly ExportProvider _exportProvider;
        private HostLanguageServices? _languageServiceProvider;
        private readonly string _initialText;
        private IWpfTextView? _textView;

        private DocumentId? _id;
        private TestHostProject? _project;

        /// <summary>
        /// The <see cref="ITextBuffer"/> for this document. Null if not yet created.
        /// </summary>
        private ITextBuffer? _textBuffer;

        /// <summary>
        /// The <see cref="ITextSnapshot"/> when the buffer was first created, which can be used for tracking changes to the current buffer.
        /// </summary>
        private ITextSnapshot? _initialTextSnapshot;
        private readonly IReadOnlyList<string>? _folders;
        private readonly IDocumentServiceProvider? _documentServiceProvider;
        private readonly ImmutableArray<string> _roles;

        public DocumentId Id
        {
            get
            {
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

        public bool IsGenerated
        {
            get
            {
                return false;
            }
        }

        public TextLoader Loader { get; }
        public int? CursorPosition { get; }
        public IList<TextSpan> SelectedSpans { get; } = new List<TextSpan>();
        public IDictionary<string, ImmutableArray<TextSpan>> AnnotatedSpans { get; } = new Dictionary<string, ImmutableArray<TextSpan>>();

        /// <summary>
        /// If a file exists in ProjectA and is added to ProjectB as a link, then this returns
        /// false for the document in ProjectA and true for the document in ProjectB.
        /// </summary>
        public bool IsLinkFile { get; internal set; }

        internal TestHostDocument(
            ExportProvider exportProvider,
            HostLanguageServices? languageServiceProvider,
            string code,
            string filePath,
            int? cursorPosition,
            IDictionary<string, ImmutableArray<TextSpan>> spans,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            IReadOnlyList<string>? folders = null,
            bool isLinkFile = false,
            IDocumentServiceProvider? documentServiceProvider = null,
            ImmutableArray<string> roles = default,
            ITextBuffer? textBuffer = null)
        {
            Contract.ThrowIfNull(filePath);

            _exportProvider = exportProvider;
            _languageServiceProvider = languageServiceProvider;
            _initialText = code;
            FilePath = filePath;
            _folders = folders;
            Name = filePath;
            this.CursorPosition = cursorPosition;
            SourceCodeKind = sourceCodeKind;
            this.IsLinkFile = isLinkFile;
            _documentServiceProvider = documentServiceProvider;
            _roles = roles.IsDefault ? s_defaultRoles : roles;

            if (spans.ContainsKey(string.Empty))
            {
                this.SelectedSpans = spans[string.Empty];
            }

            foreach (var namedSpanList in spans.Where(s => s.Key != string.Empty))
            {
                this.AnnotatedSpans.Add(namedSpanList);
            }

            Loader = new TestDocumentLoader(this, _initialText);

            if (textBuffer != null)
            {
                _textBuffer = textBuffer;
                _initialTextSnapshot = textBuffer.CurrentSnapshot;
            }
        }

        public TestHostDocument(
            string text = "", string displayName = "",
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            DocumentId? id = null, string? filePath = null,
            IReadOnlyList<string>? folders = null)
        {
            _exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
            _id = id;
            _initialText = text;
            Name = displayName;
            SourceCodeKind = sourceCodeKind;
            Loader = new TestDocumentLoader(this, text);
            FilePath = filePath;
            _folders = folders;
            _roles = s_defaultRoles;
        }

        internal void SetProject(TestHostProject project)
        {
            _project = project;

            if (_id == null)
            {
                _id = DocumentId.CreateNewId(project.Id, this.Name);
            }
            else
            {
                Contract.ThrowIfFalse(project.Id == this.Id.ProjectId);
            }

            if (_languageServiceProvider == null)
            {
                _languageServiceProvider = project.LanguageServiceProvider;
            }
        }

        private class TestDocumentLoader : TextLoader
        {
            private readonly TestHostDocument _hostDocument;
            private readonly string _text;

            internal TestDocumentLoader(TestHostDocument hostDocument, string text)
            {
                _hostDocument = hostDocument;
                _text = text;
            }

            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                return Task.FromResult(TextAndVersion.Create(SourceText.From(_text), VersionStamp.Create(), _hostDocument.FilePath));
            }
        }

        public IWpfTextView GetTextView()
        {
            if (_textView == null)
            {
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

        public ITextBuffer GetTextBuffer()
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
                    var testDocument = workspace.GetTestDocument(linkedId);

                    if (testDocument != null)
                    {
                        if (!workspace.IsDocumentOpen(linkedId))
                        {
                            // If there is a linked file, we'll start the non-linked one as being the primary context, which some tests depend on.
                            workspace.OnDocumentOpened(linkedId, _textBuffer.AsTextContainer(), isCurrentContext: !testDocument.IsLinkFile);
                        }
                    };
                }
            }

            return _textBuffer;
        }

        // TODO: delete this and move all callers to it
        public ITextBuffer TextBuffer => GetTextBuffer();

        public SourceTextContainer GetOpenTextContainer()
        {
            return this.GetTextBuffer().AsTextContainer();
        }

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
            return DocumentInfo.Create(this.Id, this.Name, this.Folders, this.SourceCodeKind, loader: this.Loader, filePath: this.FilePath, isGenerated: this.IsGenerated, _documentServiceProvider);
        }
    }
}
