// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.Remote
{
    internal static class RemoteHostClientFactoryOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        [ExportOption]
        public static readonly Option<bool> RemoteHost_InProc = new Option<bool>("InternalFeatureOnOffOptions", nameof(RemoteHost_InProc), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(RemoteHost_InProc)));
    }
}
