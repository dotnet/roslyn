// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public sealed partial class LspTestWorkspace : TestWorkspace, ILspWorkspace
{
    private readonly bool _supportsLspMutation;

    internal LspTestWorkspace(
        ExportProvider exportProvider,
        string? workspaceKind = WorkspaceKind.Host,
        Guid solutionTelemetryId = default,
        bool disablePartialSolutions = true,
        bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
        WorkspaceConfigurationOptions? configurationOptions = null,
        bool supportsLspMutation = false)
        : base(exportProvider,
               workspaceKind,
               solutionTelemetryId,
               disablePartialSolutions,
               ignoreUnchangeableDocumentsWhenApplyingChanges,
               configurationOptions)
    {
        _supportsLspMutation = supportsLspMutation;
    }

    bool ILspWorkspace.SupportsMutation => _supportsLspMutation;

    async ValueTask ILspWorkspace.UpdateTextIfPresentAsync(DocumentId documentId, SourceText sourceText, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(_supportsLspMutation);
        OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity, requireDocumentPresent: false);
    }

    internal override ValueTask TryOnDocumentClosedAsync(DocumentId documentId, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(_supportsLspMutation);
        return base.TryOnDocumentClosedAsync(documentId, cancellationToken);
    }
}
