// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a parameter.
    /// </summary>
    internal class CommonParameterWellKnownAttributeData : WellKnownAttributeData, IMarshalAsAttributeTarget
    {
        #region OutAttribute
        private bool _hasOutAttribute;
        public bool HasOutAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasOutAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasOutAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region InAttribute
        private bool _hasInAttribute;
        public bool HasInAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasInAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasInAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        // PROTOTYPE: RefReadOnlyAttribute

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
        /// Returns marshalling data or null of MarshalAs attribute isn't applied on the parameter.
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

        #region IDispatchConstantAttribute
        private bool _hasIDispatchConstantAttribute;
        public bool HasIDispatchConstantAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasIDispatchConstantAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasIDispatchConstantAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region IUnknownConstantAttribute
        private bool _hasIUnknownConstantAttribute;
        public bool HasIUnknownConstantAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasIUnknownConstantAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasIUnknownConstantAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
