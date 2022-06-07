// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a field.
    /// </summary>
    internal sealed class FieldWellKnownAttributeData : CommonFieldWellKnownAttributeData
    {
        private bool _hasAllowNullAttribute;
        public bool HasAllowNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasAllowNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasAllowNullAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasDisallowNullAttribute;
        public bool HasDisallowNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDisallowNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDisallowNullAttribute = value;
                SetDataStored();
            }
        }

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

        private bool? _maybeNullWhenAttribute;
        public bool? MaybeNullWhenAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _maybeNullWhenAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _maybeNullWhenAttribute = value;
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
