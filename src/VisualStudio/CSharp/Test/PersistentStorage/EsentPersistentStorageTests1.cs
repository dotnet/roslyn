// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Esent;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    /// <remarks>
    /// Tests are inherited from <see cref="AbstractPersistentStorageTests"/>.  That way we can
    /// write tests once and have them run against all <see cref="IPersistentStorageService"/>
    /// implementations.
    /// </remarks>
    public class EsentPersistentStorageTests : AbstractPersistentStorageTests
    {
        internal override IPersistentStorageService GetStorageService(IPersistentStorageFaultInjector faultInjector)
            => new EsentPersistentStorageService(_persistentEnabledOptionService, testing: true);
    }
}
