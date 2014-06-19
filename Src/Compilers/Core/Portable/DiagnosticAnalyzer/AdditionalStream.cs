// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a stream of non source code text.
    /// </summary>
    public abstract class AdditionalStream
    {
        internal AdditionalStream()
        {
        }

        /// <summary>
        /// Path to the stream.
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// Opens a <see cref="Stream"/> that allows reading the content.
        /// </summary>
        public abstract Stream OpenRead();
    }
}
