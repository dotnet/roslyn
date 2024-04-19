// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    internal interface IHotReloadDiagnosticManager
    {
        /// <summary>
        /// Hot reload diagnostics for all sources.
        /// </summary>
        ImmutableArray<IHotReloadDiagnosticSource> Sources { get; }

        /// <summary>
        /// Registers source of hot reload diagnostics.
        /// </summary>
        void Register(IHotReloadDiagnosticSource source);

        /// <summary>
        /// Unregisters source of hot reload diagnostics.
        /// </summary>
        void Unregister(IHotReloadDiagnosticSource source);

        /// <summary>
        /// Requests refresh of hot reload diagnostics.
        /// </summary>
        void Refresh();
    }
}
