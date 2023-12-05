// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal enum DiagnosticMode
    {
        /// <summary>
        /// Push diagnostics.  Roslyn solution crawler will compute diagnostics, and notify listeners when new
        /// diagnostics are computed.
        /// </summary>
        SolutionCrawlerPush,
        /// <summary>
        /// Lsp pull diagnostics.  Diagnostics are computed on demand when requested.
        /// </summary>
        LspPull,

        /// <summary>
        /// Default mode - when the option is set to default we use a feature flag to determine if we're
        /// is in <see cref="SolutionCrawlerPush"/> or <see cref="LspPull"/>
        /// </summary>
        Default,
    }
}
