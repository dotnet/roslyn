// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on an event.
    /// </summary>
    internal class CommonEventWellKnownAttributeData : WellKnownAttributeData, ISkipLocalsInitAttributeTarget
    {
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

        private bool _hasExcludeFromCodeCoverageAttribute;
        public bool HasExcludeFromCodeCoverageAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasExcludeFromCodeCoverageAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasExcludeFromCodeCoverageAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasSkipLocalsInitAttribute;
        public bool HasSkipLocalsInitAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSkipLocalsInitAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSkipLocalsInitAttribute = value;
                SetDataStored();
            }
        }
    }
}
