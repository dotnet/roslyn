// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a method.
    /// </summary>
    internal sealed class MethodWellKnownAttributeData : CommonMethodWellKnownAttributeData
    {
        private bool _hasDoesNotReturnAttribute;
        public bool HasDoesNotReturnAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDoesNotReturnAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDoesNotReturnAttribute = value;
                SetDataStored();
            }
        }
    }
}
