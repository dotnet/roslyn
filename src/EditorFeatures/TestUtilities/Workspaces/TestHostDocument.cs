// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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

        private readonly ExportProvider _exportProvider;
        private HostLanguageServices _languageServiceProvider;
        private readonly Lazy<string> _initialText;
        private IWpfTextView _textView;

        private DocumentId _id;
        private TestHostProject _project;
        public ITextBuffer TextBuffer;
        public ITextSnapshot InitialTextSnapshot;

        private readonly string _name;
        private readonly SourceCodeKind _sourceCodeKind;
        private readonly string _filePath;
        private readonly IReadOnlyList<string> _folders;
        private readonly IDocumentServiceProvider _documentServiceProvider;
        private readonly ImmutableArray<string> _roles;

        public DocumentId Id
        {
            get
            {
                return _id;
            }
        }

        public TestHostProject Project
        {
            get
            {
                return _project;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public SourceCodeKind SourceCodeKind
        {
            get
            {
                return _sourceCodeKind;
            }
        }

        public string FilePath
        {
            get
            {
                return _filePath;
            }
        }

        public bool IsGenerated
        {
            get
            {
                return false;
            }
        }

        public TextLoader Loader { get; }
        public int? CursorPosition { get; }
        public IList<TextSpan> SelectedSpans { get; }
        public IDictionary<string, ImmutableArray<TextSpan>> AnnotatedSpans { get; }

        /// <summary>
        /// If a file exists in ProjectA and is added to ProjectB as a link, then this returns
        /// false for the document in ProjectA and true for the document in ProjectB.
        /// </summary>
        public bool IsLinkFile { get; internal set; }

        internal TestHostDocument(
            ExportProvider exportProvider,
            HostLanguageServices languageServiceProvider,
            ITextBuffer textBuffer,
            string filePath,
            int? cursorPosition,
            IDictionary<string, ImmutableArray<TextSpan>> spans,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            IReadOnlyList<string> folders = null,
            bool isLinkFile = false,
            IDocumentServiceProvider documentServiceProvider = null,
            ImmutableArray<string> roles = default)
        {
            Contract.ThrowIfNull(textBuffer);
            Contract.ThrowIfNull(filePath);

            _exportProvider = exportProvider;
            _languageServiceProvider = languageServiceProvider;
            this.TextBuffer = textBuffer;
            this.InitialTextSnapshot = textBuffer.CurrentSnapshot;
            _initialText = new Lazy<string>(() => this.InitialTextSnapshot.GetText());
            _filePath = filePath;
            _folders = folders;
            _name = filePath;
            this.CursorPosition = cursorPosition;
            _sourceCodeKind = sourceCodeKind;
            this.IsLinkFile = isLinkFile;
            _documentServiceProvider = documentServiceProvider;
            _roles = roles.IsDefault ? s_defaultRoles : roles;

            this.SelectedSpans = new List<TextSpan>();
            if (spans.ContainsKey(string.Empty))
            {
                this.SelectedSpans = spans[string.Empty];
            }

            this.AnnotatedSpans = new Dictionary<string, ImmutableArray<TextSpan>>();
            foreach (var namedSpanList in spans.Where(s => s.Key != string.Empty))
            {
                this.AnnotatedSpans.Add(namedSpanList);
            }

            Loader = new TestDocumentLoader(this);
        }

        public TestHostDocument(
            string text = "", string displayName = "",
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            DocumentId id = null, string filePath = null,
            IReadOnlyList<string> folders = null)
        {
            _exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
            _id = id;
            _initialText = new Lazy<string>(() => text);
            _name = displayName;
            _sourceCodeKind = sourceCodeKind;
            Loader = new TestDocumentLoader(this);
            _filePath = filePath;
            _folders = folders;
            _roles = s_defaultRoles;
        }

        internal void SetProject(TestHostProject project)
        {
            _project = project;

            if (this.Id == null)
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

            if (this.TextBuffer == null)
            {
                var contentTypeService = _languageServiceProvider.GetService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();
                this.TextBuffer = _exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(_initialText.Value, contentType);
                this.InitialTextSnapshot = this.TextBuffer.CurrentSnapshot;
            }
        }

        private class TestDocumentLoader : TextLoader
        {
            private readonly TestHostDocument _hostDocument;

            internal TestDocumentLoader(TestHostDocument hostDocument)
            {
                _hostDocument = hostDocument;
            }

            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                // Create a simple SourceText so that way we're not backing "closed" files by editors to best reflect
                // what closed files look like in reality.
                var text = SourceText.From(_hostDocument.GetTextBuffer().CurrentSnapshot.GetText());
                return Task.FromResult(TextAndVersion.Create(text, VersionStamp.Create(), _hostDocument.FilePath));
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
                _textView = factory.CreateTextView(this.TextBuffer, roles);
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
            return this.TextBuffer;
        }

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
