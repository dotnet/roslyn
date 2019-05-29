﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private bool _hasAssertsTrueAttribute;
        public bool HasAssertsTrueAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasAssertsTrueAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasAssertsTrueAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasAssertsFalseAttribute;
        public bool HasAssertsFalseAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasAssertsFalseAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasAssertsFalseAttribute = value;
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
    }
}
