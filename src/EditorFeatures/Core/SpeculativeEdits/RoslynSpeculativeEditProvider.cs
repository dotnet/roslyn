// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Internal.Proposals;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.CodeAnalysis.SpeculativeEdits;

[Obsolete]
[Export(typeof(SpeculativeEditProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
internal sealed class RoslynSpeculativeEditProvider : SpeculativeEditProvider
{
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly ITextBufferCloneService _textBufferCloneService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynSpeculativeEditProvider(
        ITextBufferFactoryService3 textBufferFactoryService,
        ITextDocumentFactoryService textDocumentFactoryService,
        ITextBufferCloneService textBufferCloneService)
    {
        this.TextBufferFactoryService = textBufferFactoryService;
        _textDocumentFactoryService = textDocumentFactoryService;
        _textBufferCloneService = textBufferCloneService;
    }

    public override ISpeculativeEditSession? TryStartSpeculativeEditSession(SpeculativeEditOptions options)
    {
        var oldTextSnapshot = options.SourceSnapshot;
        var oldDocument = oldTextSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (oldDocument is null)
            return null;

        var oldSourceText = oldTextSnapshot.AsText();
        var newSourceText = oldSourceText;

        var newDocument = oldDocument.WithText(newSourceText);

        // Associate buffer with a text document with random file path to satisfy extensibility points expecting
        // absolute file path.  Ensure the new path preserves the same extension as before as that extension is used by
        // LSP to determine the language of the document.
        var textDocument = _textDocumentFactoryService.CreateTextDocument(
            _textBufferCloneService.Clone(newSourceText, options.DocumentContentType),
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), oldDocument.Name));

        var oldBuffer = oldTextSnapshot.TextBuffer;
        var newBuffer = textDocument.TextBuffer;

        var newSolution = newDocument.Project.Solution;
        newSolution = newSolution.WithDocumentText(
            newSolution.GetRelatedDocumentIds(newDocument.Id),
            newBuffer.AsTextContainer().CurrentText,
            PreservationMode.PreserveIdentity);

        var previewWorkspace = new PreviewWorkspace(newSolution);
        previewWorkspace.OpenDocument(newDocument.Id, newBuffer.AsTextContainer());

        return new RoslynSpeculativeEditSession();
    }

    private sealed class RoslynSpeculativeEditSession(
        SpeculativeEditOptions options) : ISpeculativeEditSession
    {
        public ITextSnapshot ClonedSnapshot => throw new NotImplementedException();

        public SpeculativeEditOptions CreationOptions { get; } options;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
