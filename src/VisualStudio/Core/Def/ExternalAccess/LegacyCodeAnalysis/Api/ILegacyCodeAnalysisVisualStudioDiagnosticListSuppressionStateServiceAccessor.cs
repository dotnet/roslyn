// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;

internal interface ILegacyCodeAnalysisVisualStudioDiagnosticListSuppressionStateServiceAccessor
{
    bool CanSuppressSelectedEntries { get; }
    bool CanSuppressSelectedEntriesInSource { get; }
    bool CanSuppressSelectedEntriesInSuppressionFiles { get; }
    bool CanRemoveSuppressionsSelectedEntries { get; }
}
