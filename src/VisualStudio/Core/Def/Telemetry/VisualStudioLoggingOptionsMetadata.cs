// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class VisualStudioLoggingOptionsMetadata
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Diagnostics\";

        public static readonly Option2<bool> EnableFileLoggingForDiagnostics = new("InternalDiagnosticsOptions", "EnableFileLoggingForDiagnostics", defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "EnableFileLoggingForDiagnostics"));
    }
}
