// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Serializable]
    internal struct MetadataDelta : ISerializable
    {
        public readonly byte[] Bytes;

        public MetadataDelta(byte[] bytes)
        {
            this.Bytes = bytes;
        }

        private MetadataDelta(SerializationInfo info, StreamingContext context)
        {
            this.Bytes = (byte[])info.GetValue("bytes", typeof(byte[]));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("bytes", Bytes, typeof(byte[]));
        }
    }
}
