// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Serializable]
    internal struct PdbDelta : ISerializable
    {
        // Tokens of updated methods. The debugger enumerates this list 
        // updated methods containing active statements.
        public readonly int[] UpdatedMethods;

        public readonly MemoryStream Stream;

        public PdbDelta(MemoryStream stream, int[] updatedMethods)
        {
            this.Stream = stream;
            this.UpdatedMethods = updatedMethods;
        }

        private PdbDelta(SerializationInfo info, StreamingContext context)
        {
            this.Stream = new MemoryStream((byte[])info.GetValue("bytes", typeof(byte[])));
            this.UpdatedMethods = (int[])info.GetValue("methods", typeof(int[]));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("bytes", Stream.GetBuffer(), typeof(byte[]));
            info.AddValue("methods", UpdatedMethods, typeof(int[]));
        }
    }
}
