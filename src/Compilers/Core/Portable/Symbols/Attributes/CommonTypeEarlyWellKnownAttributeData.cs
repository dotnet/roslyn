// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a type.
    /// </summary>
    internal abstract class CommonTypeEarlyWellKnownAttributeData : EarlyWellKnownAttributeData
    {
        #region AttributeUsageAttribute
        private AttributeUsageInfo _attributeUsageInfo = AttributeUsageInfo.Null;
        public AttributeUsageInfo AttributeUsageInfo
        {
            get
            {
                return _attributeUsageInfo;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(_attributeUsageInfo.IsNull);
                Debug.Assert(!value.IsNull);
                _attributeUsageInfo = value;
                SetDataStored();
            }
        }
        #endregion

        #region ComImportAttribute
        private bool _hasComImportAttribute;
        public bool HasComImportAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasComImportAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasComImportAttribute = value;
                SetDataStored();
            }
        }
        #endregion

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

                if (PEModule.IsMoreImportantObsoleteKind(_obsoleteAttributeData.Kind, value.Kind))
                    return;

                _obsoleteAttributeData = value;
                SetDataStored();
                return;
            }
        }
        #endregion

        #region CodeAnalysisEmbeddedAttribute
        private bool _hasCodeAnalysisEmbeddedAttribute;
        public bool HasCodeAnalysisEmbeddedAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasCodeAnalysisEmbeddedAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasCodeAnalysisEmbeddedAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
