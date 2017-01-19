// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    /// <summary>
    /// An abstraction of a stream of values that can be written to.
    /// </summary>
    internal abstract class ObjectWriter
    {
        public abstract void WriteBoolean(bool value);
        public abstract void WriteByte(byte value);
        public abstract void WriteChar(char ch);
        public abstract void WriteDecimal(decimal value);
        public abstract void WriteDouble(double value);
        public abstract void WriteSingle(float value);
        public abstract void WriteInt32(int value);
        public abstract void WriteInt64(long value);
        public abstract void WriteSByte(sbyte value);
        public abstract void WriteInt16(short value);
        public abstract void WriteUInt32(uint value);
        public abstract void WriteUInt64(ulong value);
        public abstract void WriteUInt16(ushort value);
        public abstract void WriteString(string value);
        public abstract void WriteValue(object value);
    }
}
