// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SQLite.v2;

namespace Microsoft.CodeAnalysis.Storage;

internal partial class AbstractPersistentStorageService
{
    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(AbstractPersistentStorageService service)
    {
        public void Shutdown()
        {
            (service._currentPersistentStorage as SQLitePersistentStorage)?.DatabaseOwnership.Dispose();
            service._currentPersistentStorage = null;
        }
    }
}
