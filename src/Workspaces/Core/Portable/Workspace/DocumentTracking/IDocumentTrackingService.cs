// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Retrieves information about what documents are currently active or visible in the host workspace.  Note: this
/// information is fundamentally racy (it can change directly after it is requested), and on different threads than the
/// thread that asks for it.  As such, this information <em>must</em> only be used to provide a hint towards how a
/// feature should go about its work, it must not impact the final results that a feature produces.  For example, a
/// feature is allowed to use this information to decide what order to process documents in, to try to get more relevant
/// results to a client more quickly.  However, it is not allowed to use this information to decide what results to
/// return altogether.  Hosts are free to implement this service to do nothing at all, always returning empty/default
/// values for the members within.  As per the above, this should never affect correctness, but it may impede a
/// feature's ability to provide results in as timely a manner as possible for a client.
/// </summary>
internal interface IDocumentTrackingService : IWorkspaceService
{
    /// <summary>
    /// Get the <see cref="DocumentId"/> of the active document. May be null if there is no active document, the
    /// active document is not in the workspace, or if this functionality is not supported by a particular host.
    /// </summary>
    DocumentId? TryGetActiveDocument();

    /// <summary>
    /// Get a read only collection of the <see cref="DocumentId"/>s of all the visible documents in the workspace.  May
    /// be empty if there are no visible documents, or if this functionality is not supported by a particular host.
    /// </summary>
    ImmutableArray<DocumentId> GetVisibleDocuments();

    /// <summary>
    /// Fired when the active document changes.  A host is not required to support this event, even if it implements
    /// <see cref="TryGetActiveDocument"/>.
    /// </summary>
    event EventHandler<DocumentId?> ActiveDocumentChanged;
}
