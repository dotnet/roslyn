// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    [DataContract]
    internal readonly record struct SourceTextData
    {
        [DataMember]
        public required SourceHashAlgorithm ChecksumAlgorithm { get; init; }

        [DataMember]
        public required Encoding Encoding { get; init; }

        [DataMember]
        public required string StorageName { get; init; }

        [DataMember]
        public required long StorageOffset { get; init; }

        [DataMember]
        public required long StorageLength { get; init; }
    }
}
