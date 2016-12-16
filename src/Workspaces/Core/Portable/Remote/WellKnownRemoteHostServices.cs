// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal class WellKnownRemoteHostServices
    {
        public const string RemoteHostService = "remoteHostService";
        public const string RemoteHostService_Connect = "Connect";
        public const string RemoteHostService_SynchronizePrimaryWorkspaceAsync = "SynchronizePrimaryWorkspaceAsync";

        public const string RemoteHostService_PersistentStorageService_RegisterPrimarySolutionId = "PersistentStorageService_RegisterPrimarySolutionId";
        public const string RemoteHostService_PersistentStorageService_UnregisterPrimarySolutionId = "PersistentStorageService_UnregisterPrimarySolutionId";
        public const string RemoteHostService_PersistentStorageService_UpdateSolutionIdStorageLocation = "PersistentStorageService_UpdateSolutionIdStorageLocation";
    }
}