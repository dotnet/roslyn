// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on an event.
    /// </summary>
    internal class CommonEventWellKnownAttributeData : WellKnownAttributeData, ISkipLocalsInitAttributeTarget
    {
        private bool _hasRequiresUnsafeAttribute;
        public bool HasRequiresUnsafeAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasRequiresUnsafeAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasRequiresUnsafeAttribute = value;
                SetDataStored();
            }
        }

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
