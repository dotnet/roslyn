// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            _offset = Uninitialized;
        }

        #region FieldOffsetAttribute
        private int _offset;                    // may be Uninitialized
        private const int Uninitialized = -1;

        public void SetFieldOffset(int offset)
        {
            VerifySealed(expected: false);
            Debug.Assert(offset >= 0);
            _offset = offset;
            SetDataStored();
        }

        public int? Offset
        {
            get
            {
                VerifySealed(expected: true);
                return _offset != Uninitialized ? _offset : (int?)null;
            }
        }
        #endregion

        #region DefaultParameterValue, DecimalConstant, DateTimeConstant
        private ConstantValue _constValue = ConstantValue.Unset;

        public ConstantValue ConstValue
        {
            get
            {
                return _constValue;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(_constValue == ConstantValue.Unset);
                _constValue = value;
                SetDataStored();
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

        #region NonSerializedAttribute
        private bool _hasNonSerializedAttribute;
        public bool HasNonSerializedAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasNonSerializedAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasNonSerializedAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region MarshalAsAttribute
        private MarshalPseudoCustomAttributeData _lazyMarshalAsData;

        MarshalPseudoCustomAttributeData IMarshalAsAttributeTarget.GetOrCreateData()
        {
            VerifySealed(expected: false);
            if (_lazyMarshalAsData == null)
            {
                _lazyMarshalAsData = new MarshalPseudoCustomAttributeData();
                SetDataStored();
            }

            return _lazyMarshalAsData;
        }

        /// <summary>
        /// Returns marshalling data or null of MarshalAs attribute isn't applied on the field.
        /// </summary>
        public MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                VerifySealed(expected: true);
                return _lazyMarshalAsData;
            }
        }
        #endregion
    }
}
