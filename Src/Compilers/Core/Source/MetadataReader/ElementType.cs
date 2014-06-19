using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFileFlags
{
    internal static class ElementType
    {
        internal const byte End = 0x00;
        internal const byte Void = 0x01;
        internal const byte Boolean = 0x02;
        internal const byte Char = 0x03;
        internal const byte Int8 = 0x04;
        internal const byte UInt8 = 0x05;
        internal const byte Int16 = 0x06;
        internal const byte UInt16 = 0x07;
        internal const byte Int32 = 0x08;
        internal const byte UInt32 = 0x09;
        internal const byte Int64 = 0x0a;
        internal const byte UInt64 = 0x0b;
        internal const byte Single = 0x0c;
        internal const byte Double = 0x0d;
        internal const byte String = 0x0e;

        internal const byte Pointer = 0x0f;
        internal const byte ByReference = 0x10;

        internal const byte ValueType = 0x11;
        internal const byte Class = 0x12;
        internal const byte GenericTypeParameter = 0x13;
        internal const byte Array = 0x14;
        internal const byte GenericTypeInstance = 0x15;
        internal const byte TypedReference = 0x16;

        internal const byte IntPtr = 0x18;
        internal const byte UIntPtr = 0x19;
        internal const byte FunctionPointer = 0x1b;
        internal const byte Object = 0x1c;
        internal const byte SzArray = 0x1d;

        internal const byte GenericMethodParameter = 0x1e;

        internal const byte RequiredModifier = 0x1f;
        internal const byte OptionalModifier = 0x20;

        internal const byte Internal = 0x21;

        internal const byte Max = 0x22;

        internal const byte Modifier = 0x40;
        internal const byte Sentinel = 0x41;
        internal const byte Pinned = 0x45;
        internal const byte SingleHFA = 0x54; // What is this?
        internal const byte DoubleHFA = 0x55; // What is this?
    }
}