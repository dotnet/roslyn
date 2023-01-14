// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// Indicates which features are enabled for a code cleanup operation.
    /// </summary>
    internal sealed record EnabledDiagnosticOptions(bool FormatDocument, bool RunThirdPartyFixers, ImmutableArray<DiagnosticSet> Diagnostics, OrganizeUsingsSet OrganizeUsings);
}
