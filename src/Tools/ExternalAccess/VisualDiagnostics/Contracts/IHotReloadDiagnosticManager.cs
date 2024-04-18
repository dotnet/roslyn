// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    internal interface IHotReloadDiagnosticManager
    {
        /// <summary>
        /// Hot reload errors.
        /// </summary>
        ImmutableArray<HotReloadDocumentDiagnostics> Errors { get; }

        /// <summary>
        /// Update the diagnostics for the given group name.
        /// </summary>
        /// <param name="errors">The diagnostics.</param>
        /// <param name="groupName">The group name.</param>
        void UpdateErrors(ImmutableArray<HotReloadDocumentDiagnostics> errors, string groupName);

        /// <summary>
        /// Clears all errors.
        /// </summary>
        void Clear();
    }
}
