// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    [Serializable]
    internal sealed class SerializedLocation : Location, IEquatable<SerializedLocation>
    {
        private readonly LocationKind kind;
        private readonly TextSpan sourceSpan;
        private readonly FileLinePositionSpan fileSpan;
        private readonly FileLinePositionSpan mappedFileSpan;

        private SerializedLocation(SerializationInfo info, StreamingContext context)
        {
            sourceSpan = (TextSpan)info.GetValue("sourceSpan", typeof(TextSpan));
            fileSpan = (FileLinePositionSpan)info.GetValue("fileSpan", typeof(FileLinePositionSpan));
            mappedFileSpan = (FileLinePositionSpan)info.GetValue("mappedFileSpan", typeof(FileLinePositionSpan));
            kind = (LocationKind)info.GetByte("kind");
        }

        internal static void GetObjectData(Location location, SerializationInfo info)
        {
            var fileSpan = location.GetLineSpan();
            var mappedFileSpan = location.GetMappedLineSpan();
            info.AddValue("sourceSpan", location.SourceSpan, typeof(TextSpan));
            info.AddValue("fileSpan", fileSpan, typeof(FileLinePositionSpan));
            info.AddValue("mappedFileSpan", mappedFileSpan, typeof(FileLinePositionSpan));
            info.AddValue("kind", (byte)location.Kind);
        }

        public override LocationKind Kind
        {
            get { return kind; }
        }

        public override TextSpan SourceSpan
        {
            get { return sourceSpan; }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            return fileSpan;
        }

        public override FileLinePositionSpan GetMappedLineSpan()
        {
            return mappedFileSpan;
        }

        public override int GetHashCode()
        {
            return fileSpan.GetHashCode();
        }

        public bool Equals(SerializedLocation other)
        {
            return other != null && fileSpan.Equals(other.fileSpan);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SerializedLocation);
        }
    }
}
