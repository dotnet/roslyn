﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on an event.
    /// </summary>
    internal class CommonEventWellKnownAttributeData : WellKnownAttributeData
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
    }
}
