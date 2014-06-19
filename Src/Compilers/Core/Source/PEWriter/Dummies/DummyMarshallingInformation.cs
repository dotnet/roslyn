using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyMarshallingInformation : IMarshallingInformation
    {
        #region IMarshallingInformation Members

        public ITypeReference CustomMarshaller
        {
            get { return Dummy.TypeReference; }
        }

        public string CustomMarshallerRuntimeArgument
        {
            get { return string.Empty; }
        }

        public uint ElementSize
        {
            get { return 0; }
        }

        public System.Runtime.InteropServices.UnmanagedType ElementType
        {
            get { return System.Runtime.InteropServices.UnmanagedType.Error; }
        }

        public uint IidParameterIndex
        {
            get { return 0; }
        }

        public System.Runtime.InteropServices.UnmanagedType UnmanagedType
        {
            get { return System.Runtime.InteropServices.UnmanagedType.Error; }
        }

        public uint NumberOfElements
        {
            get { return 0; }
        }

        public uint? ParamIndex
        {
            get { return 0; }
        }

        public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype
        {
            get { return System.Runtime.InteropServices.VarEnum.VT_VOID; }
        }

        public ITypeReference SafeArrayElementUserDefinedSubtype
        {
            get { return Dummy.TypeReference; }
        }

        public uint ElementSizeMultiplier
        {
            get { return 0; }
        }

        #endregion
    }
}