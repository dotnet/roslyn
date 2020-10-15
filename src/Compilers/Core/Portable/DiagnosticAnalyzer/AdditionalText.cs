// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Returns a <see cref="SourceText"/> with the contents of this file, or <c>null</c> if
        /// there were errors reading the file.
        /// </summary>
        public abstract SourceText? GetText(CancellationToken cancellationToken = default);
    }
}
