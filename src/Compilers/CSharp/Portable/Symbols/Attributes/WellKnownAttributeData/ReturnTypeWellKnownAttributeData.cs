// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
