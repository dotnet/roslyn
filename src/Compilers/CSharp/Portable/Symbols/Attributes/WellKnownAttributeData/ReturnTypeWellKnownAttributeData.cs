// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ReturnTypeWellKnownAttributeData : CommonReturnTypeWellKnownAttributeData
    {
        private bool _hasMaybeNullAttribute;
        public bool HasMaybeNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasMaybeNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasMaybeNullAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasNotNullAttribute;
        public bool HasNotNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasNotNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasNotNullAttribute = value;
                SetDataStored();
            }
        }
    }
}
