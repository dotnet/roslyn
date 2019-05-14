// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Text;

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

            public void UnregisterTextContainer(SourceTextContainer container)
            {
            }

            public void CloseDocument(TextDocument document, SourceText text)
            {
                switch (document)
                {
                    case Document _:
                        OnDocumentClosed(document.Id, new PreviewTextLoader(text));
                        break;

                    case AnalyzerConfigDocument _:
                        OnAnalyzerConfigDocumentClosed(document.Id, new PreviewTextLoader(text));
                        break;

                    default:
                        OnAdditionalDocumentClosed(document.Id, new PreviewTextLoader(text));
                        break;
                }
            }

            public void OpenDocument(TextDocument document)
            {
                switch (document)
                {
                    case Document _:
                        OpenDocument(document.Id);
                        break;

                    case AnalyzerConfigDocument _:
                        OpenAnalyzerConfigDocument(document.Id);
                        break;

                    default:
                        OpenAdditionalDocument(document.Id);
                        break;
                }
            }

            protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
            {
                OnDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);
            }

            protected override void ApplyAdditionalDocumentTextChanged(DocumentId id, SourceText text)
            {
                OnAdditionalDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);
            }

            protected override void ApplyAnalyzerConfigDocumentTextChanged(DocumentId id, SourceText text)
            {
                OnAnalyzerConfigDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);
            }

            private class PreviewTextLoader : TextLoader
            {
                private readonly SourceText _text;

                internal PreviewTextLoader(SourceText documentText)
                {
                    _text = documentText;
                }

                public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
                {
                    return Task.FromResult(LoadTextAndVersionSynchronously(workspace, documentId, cancellationToken));
                }

                internal override TextAndVersion LoadTextAndVersionSynchronously(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
                {
                    return TextAndVersion.Create(_text, VersionStamp.Create());
                }
            }
        }
    }
}
