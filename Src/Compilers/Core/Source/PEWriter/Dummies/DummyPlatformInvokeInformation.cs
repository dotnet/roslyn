using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyPlatformInvokeInformation : IPlatformInvokeInformation
    {
        #region IPlatformInvokeInformation Members

        public string ImportName
        {
            get { return Dummy.Name; }
        }

        public IModuleReference ImportModule
        {
            get { return Dummy.ModuleReference; }
        }

        public StringFormatKind StringFormat
        {
            get { return StringFormatKind.Unspecified; }
        }

        public bool NoMangle
        {
            get { return false; }
        }

        public bool SupportsLastError
        {
            get { return false; }
        }

        public PInvokeCallingConvention PInvokeCallingConvention
        {
            get { return PInvokeCallingConvention.CDecl; }
        }

        public bool? UseBestFit
        {
            get { return null; }
        }

        public bool? ThrowExceptionForUnmappableChar
        {
            get { return null; }
        }

        #endregion
    }
}