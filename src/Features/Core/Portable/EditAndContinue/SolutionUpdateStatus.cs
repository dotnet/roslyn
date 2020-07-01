// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum SolutionUpdateStatus
    {
        /// <summary>
        /// No updates have been made to the solution.
        /// </summary>
        None = 0,

        /// <summary>
        /// Solution udpate is ready to be applied.
        /// </summary>
        Ready = 1,

        /// <summary>
        /// Solution update is blocked. Edit can't be applied due to compiler errors or rude edits.
        /// </summary>
        Blocked = 2,
    }
}
