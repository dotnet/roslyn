// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public class EditorTestHostDocument : TestHostDocument
{
    private static readonly ImmutableArray<string> s_defaultRoles = ImmutableArray.Create<string>
        (PredefinedTextViewRoles.Analyzable,
        PredefinedTextViewRoles.Document,
        PredefinedTextViewRoles.Editable,
        PredefinedTextViewRoles.Interactive,
        PredefinedTextViewRoles.Zoomable);

    private readonly ImmutableArray<string> _roles;

    private IWpfTextView? _textView;

    /// <summary>
    /// The <see cref="ITextBuffer2"/> for this document. Null if not yet created.
    /// </summary>
    private ITextBuffer2? _textBuffer;

    /// <summary>
    /// The <see cref="ITextSnapshot"/> when the buffer was first created, which can be used for tracking changes to the current buffer.
    /// </summary>
    private ITextSnapshot? _initialTextSnapshot;

    internal EditorTestHostDocument(
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
        : base(
            exportProvider,
            languageServiceProvider,
            code,
            name,
            filePath,
            cursorPosition,
            spans,
            sourceCodeKind,
            folders,
            isLinkFile,
            documentServiceProvider,
            generator)
    {
        _roles = roles.IsDefault ? s_defaultRoles : roles;

        if (textBuffer != null)
        {
            _textBuffer = textBuffer;
            _initialTextSnapshot = textBuffer.CurrentSnapshot;
        }
    }

    internal EditorTestHostDocument(
        string text = "",
        string displayName = "",
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
        DocumentId? id = null,
        string? filePath = null,
        IReadOnlyList<string>? folders = null,
        ExportProvider? exportProvider = null,
        IDocumentServiceProvider? documentServiceProvider = null)
        : base(
            text,
            displayName,
            sourceCodeKind,
            id,
            filePath,
            folders,
            exportProvider,
            documentServiceProvider)
    {
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

    public IWpfTextView GetTextView()
    {
        if (_textView == null)
        {
            Contract.ThrowIfNull(ExportProvider, $"Can only create text view for {nameof(TestHostDocument)} created with {nameof(ExportProvider)}");
            WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} through {nameof(TestHostDocument)}.{nameof(GetTextView)}");

            var factory = ExportProvider.GetExportedValue<ITextEditorFactoryService>();

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
        var workspace = (EditorTestWorkspace?)LanguageServiceProvider?.WorkspaceServices.Workspace;

        if (_textBuffer == null)
        {
            Contract.ThrowIfNull(LanguageServiceProvider, $"To get a text buffer for a {nameof(TestHostDocument)}, it must have been parented in a project.");
            var contentType = LanguageServiceProvider.GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType();

            _textBuffer = workspace!.GetOrCreateBufferForPath(FilePath, contentType, LanguageServiceProvider.Language, InitialText);
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

    public override void Open()
        => GetOpenTextContainer();

    public SourceTextContainer GetOpenTextContainer()
        => this.GetTextBuffer().AsTextContainer();

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
}
