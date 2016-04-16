// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum SessionReadOnlyReason
    {
        /// <summary>
        /// The project is editable.
        /// </summary>
        None,

        /// <summary>
        /// The program is running. No edits allowed.
        /// </summary>
        Running,

        /// <summary>
        /// The program is stopped at exception. No edits allowed.
        /// </summary>
        StoppedAtException,
    }
}
