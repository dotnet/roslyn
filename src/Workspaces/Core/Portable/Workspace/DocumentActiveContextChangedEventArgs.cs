// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public sealed class DocumentActiveContextChangedEventArgs : EventArgs
{
    public Solution Solution { get; }
    public SourceTextContainer SourceTextContainer { get; }
    public DocumentId OldActiveContextDocumentId { get; }
    public DocumentId NewActiveContextDocumentId { get; }

    public DocumentActiveContextChangedEventArgs(Solution solution, SourceTextContainer sourceTextContainer, DocumentId oldActiveContextDocumentId, DocumentId newActiveContextDocumentId)
    {
        Contract.ThrowIfNull(solution);
        Contract.ThrowIfNull(sourceTextContainer);
        Contract.ThrowIfNull(oldActiveContextDocumentId);
        Contract.ThrowIfNull(newActiveContextDocumentId);

        this.Solution = solution;
        this.SourceTextContainer = sourceTextContainer;
        this.OldActiveContextDocumentId = oldActiveContextDocumentId;
        this.NewActiveContextDocumentId = newActiveContextDocumentId;
    }
}
