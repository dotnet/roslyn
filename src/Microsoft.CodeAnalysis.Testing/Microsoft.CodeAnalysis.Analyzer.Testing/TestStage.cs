// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    public enum TestStage
    {
        /// <summary>
        /// No test is running
        /// </summary>
        Idle,

        /// <summary>
        /// Running the diagnostic check
        /// </summary>
        Diagnostic,

        /// <summary>
        /// Running the generated code check
        /// </summary>
        GeneratedCode,

        /// <summary>
        /// Running the suppression check
        /// </summary>
        Suppression,
    }
}
