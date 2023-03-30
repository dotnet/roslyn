// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    public enum TestStage
    {
        /// <summary>
        /// no test is running
        /// </summary>
        Idle,
        /// <summary>
        /// running the diagnostic checks
        /// </summary>
        Diagnostic,
        /// <summary>
        /// running the generated code check
        /// </summary>
        GeneratedCode,
        /// <summary>
        /// running the suppression check
        /// </summary>
        Suppression
    }
}
