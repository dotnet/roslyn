// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    /// <summary>
    /// Sequence points affected by an update on a specified file.
    /// </summary>
    /// <remarks>
    /// Creates a SequencePointsUpdate.
    /// </remarks>
    /// <param name="fileName">Name of the file which was modified.</param>
    /// <param name="lineUpdates">Collection of the file lines affected by the update.</param>
    [DataContract]
    internal readonly struct SequencePointUpdates(
        string fileName,
        ImmutableArray<SourceLineUpdate> lineUpdates)
    {

        /// <summary>
        /// Name of the modified file as stored in PDB.
        /// </summary>
        [DataMember(Name = "fileName")]
        public string FileName { get; } = fileName;

        /// <summary>
        /// Collection of the file lines affected by the update.
        /// </summary>
        [DataMember(Name = "lineUpdates")]
        public ImmutableArray<SourceLineUpdate> LineUpdates { get; } = lineUpdates;
    }
}
