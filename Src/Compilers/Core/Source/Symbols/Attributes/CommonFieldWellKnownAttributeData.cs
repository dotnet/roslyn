// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a field.
    /// </summary>
    internal class CommonFieldWellKnownAttributeData : WellKnownAttributeData, IMarshalAsAttributeTarget
    {
        public CommonFieldWellKnownAttributeData()
        {
            offset = Uninitialized;
        }

        #region FieldOffsetAttribute
        private int offset;                    // may be Uninitialized
        private const int Uninitialized = -1;

        public void SetFieldOffset(int offset)
        {
            VerifySealed(expected: false);
            Debug.Assert(offset >= 0);
            this.offset = offset;
            SetDataStored();
        }

        public int? Offset
        {
            get
            {
                VerifySealed(expected: true);
                return offset != Uninitialized ? offset : (int?)null;
            }
        }
        #endregion

        #region DefaultParameterValue, DecimalConstant, DateTimeConstant
        private ConstantValue constValue = ConstantValue.Unset;

        public ConstantValue ConstValue
        {
            get
            {
                return this.constValue;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(constValue == ConstantValue.Unset);
                this.constValue = value;
                SetDataStored();
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

        #region NonSerializedAttribute
        private bool hasNonSerializedAttribute;
        public bool HasNonSerializedAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasNonSerializedAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasNonSerializedAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region MarshalAsAttribute
        private MarshalPseudoCustomAttributeData lazyMarshalAsData;

        MarshalPseudoCustomAttributeData IMarshalAsAttributeTarget.GetOrCreateData()
        {
            VerifySealed(expected: false);
            if (this.lazyMarshalAsData == null)
            {
                lazyMarshalAsData = new MarshalPseudoCustomAttributeData();
                SetDataStored();
            }

            return lazyMarshalAsData;
        }

        /// <summary>
        /// Returns marshalling data or null of MarshalAs attribute isn't applied on the field.
        /// </summary>
        public MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                VerifySealed(expected: true);
                return lazyMarshalAsData;
            }
        }
        #endregion
    }
}