// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    internal class NoOpPersistentStorageService : IPersistentStorageService
    {
        public static readonly IPersistentStorageService Instance = new NoOpPersistentStorageService();

        private NoOpPersistentStorageService()
        {
        }

        public IPersistentStorage GetStorage(Solution solution)
            => NoOpPersistentStorage.Instance;
    }
}