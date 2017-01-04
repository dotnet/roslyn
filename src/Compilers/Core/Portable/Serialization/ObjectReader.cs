// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    /// <summary>
    /// An abstract of a stream of values that can be read from.
    /// </summary>
    internal abstract class ObjectReader
    {
        public abstract bool ReadBoolean();
        public abstract byte ReadByte();
        public abstract char ReadChar();
        public abstract decimal ReadDecimal();
        public abstract double ReadDouble();
        public abstract float ReadSingle();
        public abstract int ReadInt32();
        public abstract long ReadInt64();
        public abstract sbyte ReadSByte();
        public abstract short ReadInt16();
        public abstract uint ReadUInt32();
        public abstract ulong ReadUInt64();
        public abstract ushort ReadUInt16();
        public abstract string ReadString();
        public abstract object ReadValue();
    }
}
