// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a method.
    /// </summary>
    internal class CommonMethodWellKnownAttributeData : WellKnownAttributeData, ISecurityAttributeTarget
    {
        public CommonMethodWellKnownAttributeData(bool preserveSigFirstWriteWins)
        {
            this.preserveSigFirstWriteWins = preserveSigFirstWriteWins;
            dllImportIndex = methodImplIndex = preserveSigIndex = -1;
        }

        public CommonMethodWellKnownAttributeData()
            : this(false)
        {
        }

        #region DllImportAttribute, MethodImplAttribute, PreserveSigAttribute

        // PreserveSig flag can be set by multiple attributes (DllImport, MethodImpl and PreserveSig).
        // True if the value of PreserveSig flag is determined by the first attribute that sets it (VB). 
        // Otherwise it's the last attribute's value (C#).
        private readonly bool preserveSigFirstWriteWins;

        // data from DllImportAttribute
        private DllImportData platformInvokeInfo;
        private bool dllImportPreserveSig;
        private int dllImportIndex;               // -1 .. not specified

        // data from MethodImplAttribute
        private int methodImplIndex;              // -1 .. not specified
        private MethodImplAttributes attributes;  // includes preserveSig

        // data from PreserveSigAttribute
        private int preserveSigIndex;             // -1 .. not specified

        // used by PreserveSigAttribute
        public void SetPreserveSignature(int attributeIndex)
        {
            VerifySealed(expected: false);
            Debug.Assert(attributeIndex >= 0);
            preserveSigIndex = attributeIndex;
            SetDataStored();
        }

        // used by MethodImplAttribute
        public void SetMethodImplementation(int attributeIndex, MethodImplAttributes attributes)
        {
            VerifySealed(expected: false);
            Debug.Assert(attributeIndex >= 0);
            this.attributes = attributes;
            this.methodImplIndex = attributeIndex;
            SetDataStored();
        }

        // used by DllImportAttribute
        public void SetDllImport(int attributeIndex, string moduleName, string entryPointName, Cci.PInvokeAttributes flags, bool preserveSig)
        {
            VerifySealed(expected: false);
            Debug.Assert(attributeIndex >= 0);
            platformInvokeInfo = new DllImportData(moduleName, entryPointName, flags);
            this.dllImportIndex = attributeIndex;
            this.dllImportPreserveSig = preserveSig;
            SetDataStored();
        }

        public DllImportData DllImportPlatformInvokeData
        {
            get
            {
                VerifySealed(expected: true);
                return platformInvokeInfo;
            }
        }

        public MethodImplAttributes MethodImplAttributes
        {
            get
            {
                VerifySealed(expected: true);
                var result = this.attributes;

                if (dllImportPreserveSig || preserveSigIndex >= 0)
                {
                    result |= MethodImplAttributes.PreserveSig;
                }

                if (dllImportIndex >= 0 && !dllImportPreserveSig)
                {
                    if (preserveSigFirstWriteWins)
                    {
                        // VB:
                        // only DllImport(PreserveSig := false) can unset preserveSig if it is the first attribute applied.
                        if ((preserveSigIndex == -1 || dllImportIndex < preserveSigIndex) &&
                            (methodImplIndex == -1 || (this.attributes & MethodImplAttributes.PreserveSig) == 0 || dllImportIndex < methodImplIndex))
                        {
                            result &= ~MethodImplAttributes.PreserveSig;
                        }
                    }
                    else
                    {
                        // C#:
                        // Last setter of PreserveSig flag wins. It is false only if the last one was DllImport(PreserveSig = false)
                        if (dllImportIndex > preserveSigIndex && (dllImportIndex > methodImplIndex || (this.attributes & MethodImplAttributes.PreserveSig) == 0))
                        {
                            result &= ~MethodImplAttributes.PreserveSig;
                        }
                    }
                }

                return result;
            }
        }
        #endregion

        #region SpecialNameAttribute
        private bool hasSpecialNameAttribute;
        public bool HasSpecialNameAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasSpecialNameAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasSpecialNameAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region DynamicSecurityMethodAttribute
        private bool hasDynamicSecurityMethodAttribute;
        public bool HasDynamicSecurityMethodAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasDynamicSecurityMethodAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasDynamicSecurityMethodAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region SuppressUnmanagedCodeSecurityAttribute
        private bool hasSuppressUnmanagedCodeSecurityAttribute;
        public bool HasSuppressUnmanagedCodeSecurityAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasSuppressUnmanagedCodeSecurityAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasSuppressUnmanagedCodeSecurityAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region Security Attributes
        private SecurityWellKnownAttributeData lazySecurityAttributeData;

        SecurityWellKnownAttributeData ISecurityAttributeTarget.GetOrCreateData()
        {
            VerifySealed(expected: false);

            if (lazySecurityAttributeData == null)
            {
                lazySecurityAttributeData = new SecurityWellKnownAttributeData();
                SetDataStored();
            }

            return lazySecurityAttributeData;
        }

        internal bool HasDeclarativeSecurity
        {
            get
            {
                VerifySealed(expected: true);
                return this.lazySecurityAttributeData != null || this.HasSuppressUnmanagedCodeSecurityAttribute;
            }
        }

        /// <summary>
        /// Returns data decoded from security attributes or null if there are no security attributes.
        /// </summary>
        public SecurityWellKnownAttributeData SecurityInformation
        {
            get
            {
                VerifySealed(expected: true);
                return lazySecurityAttributeData;
            }
        }
        #endregion
    }
}
