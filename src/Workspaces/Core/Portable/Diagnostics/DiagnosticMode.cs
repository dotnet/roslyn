// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal enum DiagnosticMode
    {
        /// <summary>
        /// Push diagnostics.  Roslyn/LSP is responsible for aggregating internal diagnostic notifications and pushing
        /// those out to either VS or the LSP push diagnostic system.
        /// </summary>
        Push,
        /// <summary>
        /// Pull diagnostics.  Roslyn/LSP is responsible for aggregating internal diagnostic notifications and
        /// responding to LSP pull requests for them.
        /// </summary>
        Pull,

        /// <summary>
        /// Default mode - when the option is set to default we use a feature flag to determine if we're
        /// is in <see cref="Push"/> or <see cref="Pull"/>
        /// </summary>
        Default,
    }
}
