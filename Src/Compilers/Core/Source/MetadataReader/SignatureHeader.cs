using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFileFlags
{
    internal static class SignatureHeader
    {
        internal const byte DefaultCall = 0x00;
        internal const byte CCall = 0x01;
        internal const byte StdCall = 0x02;
        internal const byte ThisCall = 0x03;
        internal const byte FastCall = 0x04;
        internal const byte VarArgCall = 0x05;
        internal const byte Field = 0x06;
        internal const byte LocalVar = 0x07;
        internal const byte Property = 0x08;

        // internal const byte UnManaged = 0x09;  // Not used as of now in CLR
        internal const byte GenericInstance = 0x0A;

        // internal const byte NativeVarArg = 0x0B;  // Not used as of now in CLR
        internal const byte Max = 0x0C;
        internal const byte CallingConventionMask = 0x0F;

        internal const byte HasThis = 0x20;
        internal const byte ExplicitThis = 0x40;
        internal const byte Generic = 0x10;

        internal static bool IsMethodSignature(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.CallingConventionMask) <= SignatureHeader.VarArgCall;
        }

        internal static bool IsVarArgCallSignature(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.CallingConventionMask) == SignatureHeader.VarArgCall;
        }

        internal static bool IsFieldSignature(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.CallingConventionMask) == SignatureHeader.Field;
        }

        internal static bool IsLocalVarSignature(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.CallingConventionMask) == SignatureHeader.LocalVar;
        }

        internal static bool IsPropertySignature(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.CallingConventionMask) == SignatureHeader.Property;
        }

        internal static bool IsGenericInstanceSignature(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.CallingConventionMask) == SignatureHeader.GenericInstance;
        }

        internal static bool IsExplicitThis(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.ExplicitThis) == SignatureHeader.ExplicitThis;
        }

        internal static bool IsGeneric(
          byte signatureHeader)
        {
            return (signatureHeader & SignatureHeader.Generic) == SignatureHeader.Generic;
        }
    }
}