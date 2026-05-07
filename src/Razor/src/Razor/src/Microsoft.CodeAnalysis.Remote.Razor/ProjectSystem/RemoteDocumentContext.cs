// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentContext(Uri uri, RemoteDocumentSnapshot snapshot) : DocumentContext(uri, snapshot)
{
    public TextDocument TextDocument => Snapshot.TextDocument;

    public new RemoteDocumentSnapshot Snapshot => (RemoteDocumentSnapshot)base.Snapshot;

    public ISolutionQueryOperations GetSolutionQueryOperations()
        => Snapshot.ProjectSnapshot.SolutionSnapshot;
}
