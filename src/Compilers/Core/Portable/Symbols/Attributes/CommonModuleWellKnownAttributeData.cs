// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a module.
    /// </summary>
    internal class CommonModuleWellKnownAttributeData : WellKnownAttributeData
    {
        #region DebuggableAttribute
        private bool _hasDebuggableAttribute;
        public bool HasDebuggableAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDebuggableAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDebuggableAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region DefaultCharSetAttribute

        private byte _defaultCharacterSet;

        internal CharSet DefaultCharacterSet
        {
            get
            {
                VerifySealed(expected: true);
                Debug.Assert(HasDefaultCharSetAttribute);
                return (CharSet)_defaultCharacterSet;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(IsValidCharSet(value));
                _defaultCharacterSet = (byte)value;
                SetDataStored();
            }
        }

        internal bool HasDefaultCharSetAttribute
        {
            get { return _defaultCharacterSet != 0; }
        }

        internal static bool IsValidCharSet(CharSet value)
        {
            return value >= Cci.Constants.CharSet_None && value <= Cci.Constants.CharSet_Auto;
        }
        #endregion
    }
}
