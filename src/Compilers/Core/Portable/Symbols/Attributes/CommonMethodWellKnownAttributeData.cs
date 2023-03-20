// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a method.
    /// </summary>
    internal class CommonMethodWellKnownAttributeData : WellKnownAttributeData, ISecurityAttributeTarget
    {
        public CommonMethodWellKnownAttributeData(bool preserveSigFirstWriteWins)
        {
            _preserveSigFirstWriteWins = preserveSigFirstWriteWins;
            _dllImportIndex = _methodImplIndex = _preserveSigIndex = -1;
        }

        public CommonMethodWellKnownAttributeData()
            : this(false)
        {
        }

        #region DllImportAttribute, MethodImplAttribute, PreserveSigAttribute

        // PreserveSig flag can be set by multiple attributes (DllImport, MethodImpl and PreserveSig).
        // True if the value of PreserveSig flag is determined by the first attribute that sets it (VB). 
        // Otherwise it's the last attribute's value (C#).
        private readonly bool _preserveSigFirstWriteWins;

        // data from DllImportAttribute
        private DllImportData? _platformInvokeInfo;
        private bool _dllImportPreserveSig;
        private int _dllImportIndex;               // -1 .. not specified

        // data from MethodImplAttribute
        private int _methodImplIndex;              // -1 .. not specified
        private MethodImplAttributes _attributes;  // includes preserveSig

        // data from PreserveSigAttribute
        private int _preserveSigIndex;             // -1 .. not specified

        // used by PreserveSigAttribute
        public void SetPreserveSignature(int attributeIndex)
        {
            VerifySealed(expected: false);
            Debug.Assert(attributeIndex >= 0);
            _preserveSigIndex = attributeIndex;
            SetDataStored();
        }

        // used by MethodImplAttribute
        public void SetMethodImplementation(int attributeIndex, MethodImplAttributes attributes)
        {
            VerifySealed(expected: false);
            Debug.Assert(attributeIndex >= 0);
            _attributes = attributes;
            _methodImplIndex = attributeIndex;
            SetDataStored();
        }

        // used by DllImportAttribute
        public void SetDllImport(int attributeIndex, string? moduleName, string? entryPointName, MethodImportAttributes flags, bool preserveSig)
        {
            VerifySealed(expected: false);
            Debug.Assert(attributeIndex >= 0);
            _platformInvokeInfo = new DllImportData(moduleName, entryPointName, flags);
            _dllImportIndex = attributeIndex;
            _dllImportPreserveSig = preserveSig;
            SetDataStored();
        }

        public DllImportData? DllImportPlatformInvokeData
        {
            get
            {
                VerifySealed(expected: true);
                return _platformInvokeInfo;
            }
        }

        public MethodImplAttributes MethodImplAttributes
        {
            get
            {
                VerifySealed(expected: true);
                var result = _attributes;

                if (_dllImportPreserveSig || _preserveSigIndex >= 0)
                {
                    result |= MethodImplAttributes.PreserveSig;
                }

                if (_dllImportIndex >= 0 && !_dllImportPreserveSig)
                {
                    if (_preserveSigFirstWriteWins)
                    {
                        // VB:
                        // only DllImport(PreserveSig := false) can unset preserveSig if it is the first attribute applied.
                        if ((_preserveSigIndex == -1 || _dllImportIndex < _preserveSigIndex) &&
                            (_methodImplIndex == -1 || (_attributes & MethodImplAttributes.PreserveSig) == 0 || _dllImportIndex < _methodImplIndex))
                        {
                            result &= ~MethodImplAttributes.PreserveSig;
                        }
                    }
                    else
                    {
                        // C#:
                        // Last setter of PreserveSig flag wins. It is false only if the last one was DllImport(PreserveSig = false)
                        if (_dllImportIndex > _preserveSigIndex && (_dllImportIndex > _methodImplIndex || (_attributes & MethodImplAttributes.PreserveSig) == 0))
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
        private bool _hasSpecialNameAttribute;
        public bool HasSpecialNameAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSpecialNameAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSpecialNameAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region DynamicSecurityMethodAttribute
        private bool _hasDynamicSecurityMethodAttribute;
        public bool HasDynamicSecurityMethodAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDynamicSecurityMethodAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDynamicSecurityMethodAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region SuppressUnmanagedCodeSecurityAttribute
        private bool _hasSuppressUnmanagedCodeSecurityAttribute;
        public bool HasSuppressUnmanagedCodeSecurityAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSuppressUnmanagedCodeSecurityAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSuppressUnmanagedCodeSecurityAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region Security Attributes
        private SecurityWellKnownAttributeData? _lazySecurityAttributeData;

        SecurityWellKnownAttributeData ISecurityAttributeTarget.GetOrCreateData()
        {
            VerifySealed(expected: false);

            if (_lazySecurityAttributeData == null)
            {
                _lazySecurityAttributeData = new SecurityWellKnownAttributeData();
                SetDataStored();
            }

            return _lazySecurityAttributeData;
        }

        internal bool HasDeclarativeSecurity
        {
            get
            {
                VerifySealed(expected: true);
                return _lazySecurityAttributeData != null || this.HasSuppressUnmanagedCodeSecurityAttribute;
            }
        }

        /// <summary>
        /// Returns data decoded from security attributes or null if there are no security attributes.
        /// </summary>
        public SecurityWellKnownAttributeData? SecurityInformation
        {
            get
            {
                VerifySealed(expected: true);
                return _lazySecurityAttributeData;
            }
        }
        #endregion

        #region ExcludeFromCodeCoverageAttribute

        private bool _hasExcludeFromCodeCoverageAttribute;
        public bool HasExcludeFromCodeCoverageAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasExcludeFromCodeCoverageAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasExcludeFromCodeCoverageAttribute = value;
                SetDataStored();
            }
        }

        #endregion
    }
}
