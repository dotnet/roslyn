// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a type.
    /// </summary>
    internal class CommonTypeWellKnownAttributeData : WellKnownAttributeData, ISecurityAttributeTarget
    {
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

        #region SerializableAttribute
        private bool _hasSerializableAttribute;
        public bool HasSerializableAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSerializableAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSerializableAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region DefaultMemberAttribute
        private bool _hasDefaultMemberAttribute;
        public bool HasDefaultMemberAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDefaultMemberAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDefaultMemberAttribute = value;
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
        private SecurityWellKnownAttributeData _lazySecurityAttributeData;

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
        public SecurityWellKnownAttributeData SecurityInformation
        {
            get
            {
                VerifySealed(expected: true);
                return _lazySecurityAttributeData;
            }
        }
        #endregion

        #region WindowsRuntimeImportAttribute
        private bool _hasWindowsRuntimeImportAttribute;
        public bool HasWindowsRuntimeImportAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasWindowsRuntimeImportAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasWindowsRuntimeImportAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region GuidAttribute
        // Decoded guid string from GuidAttribute
        private string _guidString;

        public string GuidString
        {
            get
            {
                VerifySealed(expected: true);
                return _guidString;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);
                _guidString = value;
                SetDataStored();
            }
        }
        #endregion

        #region StructLayoutAttribute

        private TypeLayout _layout;
        private CharSet _charSet;

        // StructLayoutAttribute
        public void SetStructLayout(TypeLayout layout, CharSet charSet)
        {
            VerifySealed(expected: false);
            Debug.Assert(charSet == CharSet.Ansi || charSet == CharSet.Unicode || charSet == Cci.Constants.CharSet_Auto);
            _layout = layout;
            _charSet = charSet;
            SetDataStored();
        }

        public bool HasStructLayoutAttribute
        {
            get
            {
                VerifySealed(expected: true);
                // charSet is non-zero iff it was set by SetStructLayout called from StructLayoutAttribute decoder
                return _charSet != 0;
            }
        }

        public TypeLayout Layout
        {
            get
            {
                VerifySealed(expected: true);
                Debug.Assert(HasStructLayoutAttribute);
                return _layout;
            }
        }

        public CharSet MarshallingCharSet
        {
            get
            {
                VerifySealed(expected: true);
                Debug.Assert(HasStructLayoutAttribute);
                return _charSet;
            }
        }

        #endregion

        #region SecurityCriticalAttribute and SecuritySafeCriticalAttribute
        private bool _hasSecurityCriticalAttributes;
        public bool HasSecurityCriticalAttributes
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSecurityCriticalAttributes;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSecurityCriticalAttributes = value;
                SetDataStored();
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
