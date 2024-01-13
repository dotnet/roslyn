// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class TestHostDocument
    {
        protected HostLanguageServices? LanguageServiceProvider;
        protected readonly string InitialText;
        protected readonly ExportProvider? ExportProvider;

        private DocumentId? _id;
        private AbstractTestHostProject? _project;
        private readonly IReadOnlyList<string>? _folders;
        private readonly IDocumentServiceProvider? _documentServiceProvider;
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
                    var workspace = LanguageServiceProvider!.WorkspaceServices.Workspace;
                    var project = workspace.CurrentSolution.GetRequiredProject(_project!.Id);
                    var sourceGeneratedDocuments = project.GetSourceGeneratedDocumentsAsync(CancellationToken.None).AsTask().Result;
                    _id = sourceGeneratedDocuments.Single(d => d.FilePath == this.FilePath).Id;
                }

                Contract.ThrowIfNull(_id);
                return _id;
            }
        }

        public AbstractTestHostProject Project
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
            ISourceGenerator? generator = null)
        {
            Contract.ThrowIfNull(filePath);

            ExportProvider = exportProvider;
            LanguageServiceProvider = languageServiceProvider;
            InitialText = code;
            Name = name;
            FilePath = filePath;
            _folders = folders;
            this.CursorPosition = cursorPosition;
            SourceCodeKind = sourceCodeKind;
            this.IsLinkFile = isLinkFile;
            Generator = generator;
            _documentServiceProvider = documentServiceProvider;

            if (spans.TryGetValue(string.Empty, out var textSpans))
            {
                this.SelectedSpans = textSpans;
            }

            foreach (var namedSpanList in spans.Where(s => s.Key != string.Empty))
            {
                this.AnnotatedSpans.Add(namedSpanList);
            }

            _loader = new TestDocumentLoader(this, InitialText);
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
            ExportProvider = exportProvider;
            _id = id;
            InitialText = text;
            Name = displayName;
            SourceCodeKind = sourceCodeKind;
            _loader = new TestDocumentLoader(this, text);
            FilePath = filePath;
            _folders = folders;
            _documentServiceProvider = documentServiceProvider;
        }

        public virtual void Open()
        {
        }

        internal void SetProject(AbstractTestHostProject project)
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

            LanguageServiceProvider ??= project.LanguageServiceProvider;
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

        public IReadOnlyList<string> Folders
        {
            get
            {
                return _folders ?? ImmutableArray.Create<string>();
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
