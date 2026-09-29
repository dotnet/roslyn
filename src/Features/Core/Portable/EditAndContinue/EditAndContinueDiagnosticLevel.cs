// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal enum EditAndContinueDiagnosticLevel
{
    /// <summary>
    /// No extra validation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Adds extra validation that is normally not performed due to its impact on performance
    /// and should only be used when diagnosing issues with EnC.
    /// </summary>
    Debug = 1,
}
