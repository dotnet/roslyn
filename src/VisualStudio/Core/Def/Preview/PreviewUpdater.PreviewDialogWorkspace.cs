// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
