using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFileFlags
{
    internal static class SerializationType
    {
        internal const ushort CustomAttributeStart = 0x0001;
        internal const byte SecurityAttribute20Start = 0x2E;  // '.'
        internal const byte Undefined = 0x00;
        internal const byte Boolean = ElementType.Boolean;
        internal const byte Char = ElementType.Char;
        internal const byte Int8 = ElementType.Int8;
        internal const byte UInt8 = ElementType.UInt8;
        internal const byte Int16 = ElementType.Int16;
        internal const byte UInt16 = ElementType.UInt16;
        internal const byte Int32 = ElementType.Int32;
        internal const byte UInt32 = ElementType.UInt32;
        internal const byte Int64 = ElementType.Int64;
        internal const byte UInt64 = ElementType.UInt64;
        internal const byte Single = ElementType.Single;
        internal const byte Double = ElementType.Double;
        internal const byte String = ElementType.String;
        internal const byte SZArray = ElementType.SzArray;
        internal const byte Type = 0x50;
        internal const byte TaggedObject = 0x51;
        internal const byte Field = 0x53;
        internal const byte Property = 0x54;
        internal const byte Enum = 0x55;
    }
}