// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor;

/// <summary>
/// Represents a command that can be intercepted by the <see cref="RazorLSPTextViewConnectionListener"/> text view filter.
/// </summary>
internal interface IInterceptedCommand
{
    bool QueryStatus(Guid pguidCmdGroup, uint nCmdID);

    Task<ImmutableArray<TextChange>> ExecuteAsync(Solution solution, DocumentId documentId, uint nCmdID, CancellationToken cancellationToken);
}
