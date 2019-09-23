// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
