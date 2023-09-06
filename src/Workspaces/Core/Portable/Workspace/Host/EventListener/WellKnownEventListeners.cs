// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// list of well known <see cref="IEventListener"/> types
    /// </summary>
    internal static class WellKnownEventListeners
    {
        public const string Workspace = nameof(Workspace);
        public const string DiagnosticService = nameof(DiagnosticService);
    }
}
