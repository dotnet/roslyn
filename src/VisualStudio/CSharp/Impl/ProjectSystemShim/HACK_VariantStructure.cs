// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    /// <summary>
    /// This is a terrible, terrible hack around the C# project system in
    /// CCscMSBuildHostObject::SetDelaySign. To indicate a value of "unset"
    /// for boolean options, they create variant of type VT_BOOL with the boolean
    /// field being a value of "4". The CLR, if it marshals this variant, marshals
    /// it as a "true" which is indistinguishable from a real VARIANT_TRUE. So
    /// instead we define this structure of the same layout, and marshal the variant
    /// as this structure. We can then pick out this broken pattern, and convert
    /// it to null instead of true.
    /// </summary>
    internal struct HACK_VariantStructure
    {
        private readonly short _type;

        private readonly short _padding1;
        private readonly short _padding2;
        private readonly short _padding3;

        private readonly short _booleanValue;
        private readonly IntPtr _padding4; // this will be aligned to the IntPtr-sized address

        public unsafe object ConvertToObject()
        {
            if (_type == (short)VarEnum.VT_BOOL && _booleanValue == 4)
            {
                return null;
            }

            // Can't take an address of this since it might move, so....
            var localCopy = this;
            return Marshal.GetObjectForNativeVariant((IntPtr)(&localCopy));
        }
    }
}
