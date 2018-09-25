// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a method.
    /// </summary>
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

        private bool? _nullableWarnings;
        public bool? NullableWarnings
        {
            get
            {
                VerifySealed(expected: true);
                return _nullableWarnings;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value.HasValue);
                _nullableWarnings = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
