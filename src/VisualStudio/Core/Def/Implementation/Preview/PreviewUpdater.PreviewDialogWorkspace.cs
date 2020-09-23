// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal partial class PreviewUpdater
    {
        // internal for testing
        internal class PreviewDialogWorkspace : PreviewWorkspace
        {
            public PreviewDialogWorkspace(Solution solution) : base(solution)
            {
            }

            public void CloseDocument(TextDocument document, SourceText text)
            {
                switch (document.Kind)
                {
                    case TextDocumentKind.Document:
                        OnDocumentClosed(document.Id, new PreviewTextLoader(text));
                        break;

                    case TextDocumentKind.AnalyzerConfigDocument:
                        OnAnalyzerConfigDocumentClosed(document.Id, new PreviewTextLoader(text));
                        break;

                    case TextDocumentKind.AdditionalDocument:
                        OnAdditionalDocumentClosed(document.Id, new PreviewTextLoader(text));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(document.Kind);
                }
            }

            public void OpenDocument(TextDocument document)
            {
                switch (document.Kind)
                {
                    case TextDocumentKind.Document:
                        OpenDocument(document.Id);
                        break;

                    case TextDocumentKind.AnalyzerConfigDocument:
                        OpenAnalyzerConfigDocument(document.Id);
                        break;

                    case TextDocumentKind.AdditionalDocument:
                        OpenAdditionalDocument(document.Id);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(document.Kind);
                }
            }

            protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
                => OnDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);

            protected override void ApplyAdditionalDocumentTextChanged(DocumentId id, SourceText text)
                => OnAdditionalDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);

            protected override void ApplyAnalyzerConfigDocumentTextChanged(DocumentId id, SourceText text)
                => OnAnalyzerConfigDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);

            private class PreviewTextLoader : TextLoader
            {
                private readonly SourceText _text;

                internal PreviewTextLoader(SourceText documentText)
                    => _text = documentText;

                public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
                    => Task.FromResult(LoadTextAndVersionSynchronously(workspace, documentId, cancellationToken));

                internal override TextAndVersion LoadTextAndVersionSynchronously(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
                    => TextAndVersion.Create(_text, VersionStamp.Create());
            }
        }
    }
}
