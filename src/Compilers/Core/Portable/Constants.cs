// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal static class Constants
    {
        internal const string UseIncrementalGenerator = $"Non-incremental source generators should not be used, use {nameof(IIncrementalGenerator)} instead.";
        internal const string UseIncrementalGeneratorDiagnosticId = "ROSLYN0001";
    }
}
