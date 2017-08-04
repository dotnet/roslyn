// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// TemporaryStorage can be used to read and write text to a temporary storage location.
    /// </summary>
    internal interface ITemporaryStorageWithName
    {
        /// <summary>
        /// Get name of the temporary storage
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get offset of the temporary storage
        /// </summary>
        long Offset { get; }

        /// <summary>
        /// Get size of the temporary storage
        /// </summary>
        long Size { get; }
    }
}
