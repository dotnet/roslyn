// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IChecksummedPersistentStorageService : IPersistentStorageService2
    {
        new IChecksummedPersistentStorage GetStorage(Solution solution);
        new IChecksummedPersistentStorage GetStorage(Solution solution, bool checkBranchId);
    }
}
