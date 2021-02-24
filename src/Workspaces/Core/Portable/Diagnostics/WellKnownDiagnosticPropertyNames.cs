// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class WellKnownDiagnosticPropertyNames
    {
        /// <summary>
        /// Predefined name of diagnostic property which shows in what compilation stage the diagnostic is created. 
        /// </summary>
        public const string Origin = nameof(Origin);
    }
}
