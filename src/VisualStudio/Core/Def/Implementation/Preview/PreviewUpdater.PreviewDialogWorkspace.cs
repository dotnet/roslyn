// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal partial class PreviewUpdater
    {
        private class PreviewDialogWorkspace : PreviewWorkspace
        {
            public PreviewDialogWorkspace(Solution solution) : base(solution)
            {
            }

            public void UnregisterTextContainer(SourceTextContainer container)
            {
            }

            public void CloseDocument(DocumentId id, SourceText text)
            {
                OnDocumentClosed(id, new PreviewTextLoader(text));
            }

            protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
            {
                OnDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);
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
                    return Task.FromResult(TextAndVersion.Create(_text, VersionStamp.Create()));
                }
            }
        }
    }
}
