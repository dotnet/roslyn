// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal readonly struct SequencePointUpdates
    {
        [DataMember(Order = 0)]
        public readonly string FileName;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<SourceLineUpdate> LineUpdates;

        public SequencePointUpdates(string fileName, ImmutableArray<SourceLineUpdate> lineUpdates)
        {
            FileName = fileName;
            LineUpdates = lineUpdates;
        }
    }
}
