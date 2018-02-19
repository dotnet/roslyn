// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
{
    public sealed class MockRefactorNotifyService : IRefactorNotifyService
    {
        private int _onBeforeSymbolRenamedCount = 0;
        private int _onAfterSymbolRenamedCount = 0;

        public int OnBeforeSymbolRenamedCount { get { return _onBeforeSymbolRenamedCount; } }
        public int OnAfterSymbolRenamedCount { get { return _onAfterSymbolRenamedCount; } }

        public bool OnBeforeSymbolRenamedReturnValue { get; set; }
        public bool OnAfterSymbolRenamedReturnValue { get; set; }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            _onBeforeSymbolRenamedCount++;

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
}
