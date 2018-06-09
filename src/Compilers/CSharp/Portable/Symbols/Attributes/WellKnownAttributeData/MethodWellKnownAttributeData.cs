// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class MethodWellKnownAttributeData : CommonMethodWellKnownAttributeData
    {
        #region NonNullTypesAttribute

        private bool? _nonNullTypes;
        public bool? NonNullTypes
        {
            get
            {
                VerifySealed(expected: true);
                return _nonNullTypes;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value.HasValue);
                _nonNullTypes = value;
                SetDataStored();
            }
        }

        #endregion
    }
}
