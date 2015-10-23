// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a non source code file.
    /// </summary>
    public abstract class AdditionalText
    {
        /// <summary>
        /// Path to the text.
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// Retrieves a <see cref="SourceText"/> with the contents of this file.
        /// </summary>
        public abstract SourceText GetText(CancellationToken cancellationToken = default(CancellationToken));
    }
}
