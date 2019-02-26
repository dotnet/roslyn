// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a parameter.
    /// </summary>
    internal sealed class ParameterWellKnownAttributeData : CommonParameterWellKnownAttributeData
    {
        private bool _hasNotNullWhenTrueAttribute;
        public bool HasNotNullWhenTrueAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasNotNullWhenTrueAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasNotNullWhenTrueAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasNotNullWhenFalseAttribute;
        public bool HasNotNullWhenFalseAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasNotNullWhenFalseAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasNotNullWhenFalseAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasEnsuresNotNullAttribute;
        public bool HasEnsuresNotNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasEnsuresNotNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasEnsuresNotNullAttribute = value;
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
    }
}
