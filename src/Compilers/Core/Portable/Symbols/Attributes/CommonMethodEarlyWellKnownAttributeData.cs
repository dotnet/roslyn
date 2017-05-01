// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a method.
    /// </summary>
    internal class CommonMethodEarlyWellKnownAttributeData : EarlyWellKnownAttributeData
    {
        #region ConditionalAttribute
        private ImmutableArray<string> _lazyConditionalSymbols = ImmutableArray<string>.Empty;

        public void AddConditionalSymbol(string name)
        {
            VerifySealed(expected: false);
            _lazyConditionalSymbols = _lazyConditionalSymbols.Add(name);
            SetDataStored();
        }

        public ImmutableArray<string> ConditionalSymbols
        {
            get
            {
                VerifySealed(expected: true);
                return _lazyConditionalSymbols;
            }
        }
        #endregion

        #region ObsoleteAttribute
        private ObsoleteAttributeData _obsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
        public ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                VerifySealed(expected: true);
                return _obsoleteAttributeData.IsUninitialized ? null : _obsoleteAttributeData;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);
                Debug.Assert(!value.IsUninitialized);

                _obsoleteAttributeData = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
