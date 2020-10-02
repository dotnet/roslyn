// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;

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

        private ImmutableHashSet<string> _notNullIfParameterNotNull = ImmutableHashSet<string>.Empty;
        public ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get
            {
                VerifySealed(expected: true);
                return _notNullIfParameterNotNull;
            }
        }
        public void AddNotNullIfParameterNotNull(string parameterName)
        {
            VerifySealed(expected: false);
            // The common case is zero or one attribute
            _notNullIfParameterNotNull = _notNullIfParameterNotNull.Add(parameterName);
            SetDataStored();
        }
    }
}
