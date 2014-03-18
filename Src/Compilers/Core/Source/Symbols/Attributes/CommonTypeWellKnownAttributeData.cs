// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        #region SerializableAttribute
        private bool hasSerializableAttribute;
        public bool HasSerializableAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasSerializableAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasSerializableAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region DefaultMemberAttribute
        private bool hasDefaultMemberAttribute;
        public bool HasDefaultMemberAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasDefaultMemberAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasDefaultMemberAttribute = value;
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

        #region WindowsRuntimeImportAttribute
        private bool hasWindowsRuntimeImportAttribute;
        public bool HasWindowsRuntimeImportAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasWindowsRuntimeImportAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasWindowsRuntimeImportAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region GuidAttribute
        // Decoded guid string from GuidAttribute
        private string guidString;

        public string GuidString
        {
            get
            {
                VerifySealed(expected: true);
                return this.guidString;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);
                this.guidString = value;
                SetDataStored();
            }
        }
        #endregion

        #region StructLayoutAttribute

        private TypeLayout layout;
        private CharSet charSet;

        // StructLayoutAttribute
        public void SetStructLayout(TypeLayout layout, CharSet charSet)
        {
            VerifySealed(expected: false);
            Debug.Assert(charSet == CharSet.Ansi || charSet == CharSet.Unicode || charSet == Cci.Constants.CharSet_Auto);
            this.layout = layout;
            this.charSet = charSet;
            SetDataStored();
        }

        public bool HasStructLayoutAttribute
        {
            get
            {
                VerifySealed(expected: true);
                // charSet is non-zero iff it was set by SetStructLayout called from StructLayoutAttribute decoder
                return charSet != 0;
            }
        }

        public TypeLayout Layout
        {
            get
            {
                VerifySealed(expected: true);
                Debug.Assert(HasStructLayoutAttribute);
                return layout;
            }
        }

        public CharSet MarshallingCharSet
        {
            get
            {
                VerifySealed(expected: true);
                Debug.Assert(HasStructLayoutAttribute);
                return charSet;
            }
        }

        #endregion

        #region SecurityCriticalAttribute and SecuritySafeCriticalAttribute
        private bool hasSecurityCriticalAttributes;
        public bool HasSecurityCriticalAttributes
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasSecurityCriticalAttributes;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasSecurityCriticalAttributes = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
