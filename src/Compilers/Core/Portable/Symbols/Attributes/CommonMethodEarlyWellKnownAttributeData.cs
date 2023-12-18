// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private ImmutableArray<string?> _lazyConditionalSymbols = ImmutableArray<string?>.Empty;

        public void AddConditionalSymbol(string? name)
        {
            VerifySealed(expected: false);
            _lazyConditionalSymbols = _lazyConditionalSymbols.Add(name);
            SetDataStored();
        }

        public ImmutableArray<string?> ConditionalSymbols
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
        public ObsoleteAttributeData? ObsoleteAttributeData
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

                if (PEModule.IsMoreImportantObsoleteKind(_obsoleteAttributeData.Kind, value.Kind))
                    return;

                _obsoleteAttributeData = value;
                SetDataStored();
            }
        }
        #endregion

        #region SetsRequiredMembers
        private bool _hasSetsRequiredMembers = false;
        public bool HasSetsRequiredMembersAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSetsRequiredMembers;
            }
            set
            {
                VerifySealed(false);
                _hasSetsRequiredMembers = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
