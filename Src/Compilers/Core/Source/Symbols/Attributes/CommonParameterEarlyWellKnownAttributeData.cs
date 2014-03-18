// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a parameter.
    /// </summary>
    internal abstract class CommonParameterEarlyWellKnownAttributeData : EarlyWellKnownAttributeData
    {
        #region DefaultParameterValue, DecimalConstant, DateTimeConstant
        private ConstantValue defaultParameterValue = ConstantValue.Unset;

        public ConstantValue DefaultParameterValue
        {
            get
            {
                return this.defaultParameterValue;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(defaultParameterValue == ConstantValue.Unset);
                this.defaultParameterValue = value;
                SetDataStored();
            }
        }
        #endregion

        #region CallerInfoAttributes
        private bool hasCallerLineNumberAttribute;
        public bool HasCallerLineNumberAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasCallerLineNumberAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasCallerLineNumberAttribute = value;
                SetDataStored();
            }
        }

        private bool hasCallerFilePathAttribute;
        public bool HasCallerFilePathAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasCallerFilePathAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasCallerFilePathAttribute = value;
                SetDataStored();
            }
        }

        private bool hasCallerMemberNameAttribute;
        public bool HasCallerMemberNameAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasCallerMemberNameAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasCallerMemberNameAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
