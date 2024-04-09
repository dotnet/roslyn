// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    internal interface IHotReloadDiagnosticService
    {
        /// <summary>
        /// Update the diagnostics for the given group name.
        /// </summary>
        /// <param name="diagnostics">The diagnostics.</param>
        /// <param name="groupName">The group name.</param>
        void UpdateDiagnostics(IEnumerable<Diagnostic> diagnostics, string groupName);
    }
}
