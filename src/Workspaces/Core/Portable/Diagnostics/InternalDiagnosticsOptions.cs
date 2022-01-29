// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class InternalDiagnosticsOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Diagnostics\";

        public static readonly Option2<bool> PutCustomTypeInBingSearch = new(nameof(InternalDiagnosticsOptions), nameof(PutCustomTypeInBingSearch), defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "PutCustomTypeInBingSearch"));

        public static readonly Option2<bool> CrashOnAnalyzerException = new(nameof(InternalDiagnosticsOptions), nameof(CrashOnAnalyzerException), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "CrashOnAnalyzerException"));

        public static readonly Option2<DiagnosticMode> NormalDiagnosticMode = new(nameof(InternalDiagnosticsOptions), nameof(NormalDiagnosticMode), defaultValue: DiagnosticMode.Default,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "NormalDiagnosticMode"));
    }
}
