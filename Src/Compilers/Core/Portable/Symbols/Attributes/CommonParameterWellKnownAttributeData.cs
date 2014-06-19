// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private bool hasOutAttribute;
        public bool HasOutAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasOutAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasOutAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region InAttribute
        private bool hasInAttribute;
        public bool HasInAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasInAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasInAttribute = value;
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
        /// Returns marshalling data or null of MarshalAs attribute isn't applied on the parameter.
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

        #region IDispatchConstantAttribute
        private bool hasIDispatchConstantAttribute;
        public bool HasIDispatchConstantAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasIDispatchConstantAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasIDispatchConstantAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region IUnknownConstantAttribute
        private bool hasIUnknownConstantAttribute;
        public bool HasIUnknownConstantAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasIUnknownConstantAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasIUnknownConstantAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
