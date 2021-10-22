// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a parameter.
    /// </summary>
    internal abstract class CommonParameterEarlyWellKnownAttributeData : EarlyWellKnownAttributeData
    {
        #region DefaultParameterValue, DecimalConstant, DateTimeConstant
        private ConstantValue _defaultParameterValue = ConstantValue.Unset;

        public ConstantValue DefaultParameterValue
        {
            get
            {
                return _defaultParameterValue;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(_defaultParameterValue == ConstantValue.Unset);
                _defaultParameterValue = value;
                SetDataStored();
            }
        }
        #endregion

        #region CallerInfoAttributes
        private bool _hasCallerLineNumberAttribute;
        public bool HasCallerLineNumberAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasCallerLineNumberAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasCallerLineNumberAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasCallerFilePathAttribute;
        public bool HasCallerFilePathAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasCallerFilePathAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasCallerFilePathAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasCallerMemberNameAttribute;
        public bool HasCallerMemberNameAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasCallerMemberNameAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasCallerMemberNameAttribute = value;
                SetDataStored();
            }
        }

        private int _argumentExpressionParameterIndex = -1;
        public int CallerArgumentExpressionParameterIndex
        {
            get
            {
                VerifySealed(expected: true);
                return _argumentExpressionParameterIndex;
            }
            set
            {
                VerifySealed(expected: false);
                _argumentExpressionParameterIndex = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
