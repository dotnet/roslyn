// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class PropertyWellKnownAttributeData : CommonPropertyWellKnownAttributeData
    {
        #region NullableOptOutAttribute

        private bool? _nullableOptOut;
        public bool? NullableOptOut
        {
            get
            {
                VerifySealed(expected: true);
                return _nullableOptOut;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value.HasValue);
                _nullableOptOut = value;
                SetDataStored();
            }
        }

        #endregion
    }
}