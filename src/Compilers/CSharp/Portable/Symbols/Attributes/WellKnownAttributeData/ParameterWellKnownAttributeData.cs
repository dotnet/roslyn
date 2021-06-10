// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a parameter.
    /// </summary>
    internal sealed class ParameterWellKnownAttributeData : CommonParameterWellKnownAttributeData
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

        private bool? _notNullWhenAttribute;
        public bool? NotNullWhenAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _notNullWhenAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _notNullWhenAttribute = value;
                SetDataStored();
            }
        }

        private bool? _doesNotReturnIfAttribute;
        public bool? DoesNotReturnIfAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _doesNotReturnIfAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _doesNotReturnIfAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasEnumeratorCancellationAttribute;
        public bool HasEnumeratorCancellationAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasEnumeratorCancellationAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasEnumeratorCancellationAttribute = value;
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

        private ImmutableArray<int> _interpolatedStringHandlerArguments = ImmutableArray<int>.Empty;
        public ImmutableArray<int> InterpolatedStringHandlerArguments
        {
            get
            {
                VerifySealed(expected: true);
                return _interpolatedStringHandlerArguments;
            }
            set
            {
                VerifySealed(expected: false);
                _interpolatedStringHandlerArguments = value;
                SetDataStored();
            }
        }
    }
}
