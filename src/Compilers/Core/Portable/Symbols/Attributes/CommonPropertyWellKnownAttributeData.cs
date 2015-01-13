// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a property.
    /// </summary>
    internal class CommonPropertyWellKnownAttributeData : WellKnownAttributeData
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
    }
}