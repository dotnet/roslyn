// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    internal sealed class LspMutatingTestWorkspace : TestWorkspace, IMutatingLspWorkspace
    {
        public LspMutatingTestWorkspace(
            TestComposition? composition = null,
            string? workspaceKind = WorkspaceKind.Host,
            Guid solutionTelemetryId = default,
            bool disablePartialSolutions = true,
            bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
            WorkspaceConfigurationOptions? configurationOptions = null)
            : base(composition,
                  workspaceKind,
                  solutionTelemetryId,
                  disablePartialSolutions,
                  ignoreUnchangeableDocumentsWhenApplyingChanges,
                  configurationOptions)
        {
        }

        void IMutatingLspWorkspace.CloseIfPresent(DocumentId documentId, TextLoader textLoader)
            => OnDocumentClosed(documentId, textLoader, _: false, requireDocumentPresent: false);

        void IMutatingLspWorkspace.OpenIfPresent(DocumentId documentId, SourceTextContainer container)
            => OnDocumentOpened(documentId, container, isCurrentContext: false, requireDocumentPresent: false);

        void IMutatingLspWorkspace.UpdateTextIfPresent(DocumentId documentId, SourceText sourceText)
            => OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity, requireDocumentPresent: false);
    }
}
