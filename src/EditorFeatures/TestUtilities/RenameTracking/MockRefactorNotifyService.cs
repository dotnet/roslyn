// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking;

public sealed class MockRefactorNotifyService : IRefactorNotifyService
{
    private int _onAfterSymbolRenamedCount = 0;

    public int OnBeforeSymbolRenamedCount { get; private set; } = 0;
    public int OnAfterSymbolRenamedCount { get { return _onAfterSymbolRenamedCount; } }

    public bool OnBeforeSymbolRenamedReturnValue { get; set; }
    public bool OnAfterSymbolRenamedReturnValue { get; set; }

    public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
    {
        OnBeforeSymbolRenamedCount++;

        if (throwOnFailure && !OnBeforeSymbolRenamedReturnValue)
        {
            Marshal.ThrowExceptionForHR(unchecked((int)0x80004004)); // E_ABORT
        }

        return OnBeforeSymbolRenamedReturnValue;
    }

    public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
    {
        _onAfterSymbolRenamedCount++;

        if (throwOnFailure && !OnAfterSymbolRenamedReturnValue)
        {
            Marshal.ThrowExceptionForHR(unchecked((int)0x80004004)); // E_ABORT
        }

        return OnAfterSymbolRenamedReturnValue;
    }
}
