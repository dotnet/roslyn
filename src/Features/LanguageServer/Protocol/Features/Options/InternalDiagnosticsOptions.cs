// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class InternalDiagnosticsOptions
{
    public static readonly Option2<DiagnosticMode> NormalDiagnosticMode = new("InternalDiagnosticsOptions", "NormalDiagnosticMode", defaultValue: DiagnosticMode.Default,
        storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Diagnostics\NormalDiagnosticMode"));
}
