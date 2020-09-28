// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Represents a <see cref="ITemporaryStorageWithName"/> which is used to hold data for <see cref="SourceText"/>.
    /// </summary>
    internal interface ITemporaryTextStorageWithName : ITemporaryTextStorage, ITemporaryStorageWithName
    {
        /// <summary>
        /// Gets the value for the <see cref="SourceText.ChecksumAlgorithm"/> property for the <see cref="SourceText"/>
        /// represented by this temporary storage.
        /// </summary>
        SourceHashAlgorithm ChecksumAlgorithm { get; }

        /// <summary>
        /// Gets the value for the <see cref="SourceText.Encoding"/> property for the <see cref="SourceText"/>
        /// represented by this temporary storage.
        /// </summary>
        Encoding? Encoding { get; }

        /// <summary>
        /// Gets the checksum for the <see cref="SourceText"/> represented by this temporary storage. This is equivalent
        /// to calling <see cref="SourceText.GetChecksum"/>.
        /// </summary>
        ImmutableArray<byte> GetChecksum();
    }
}
