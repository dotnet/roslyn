// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a type.
    /// </summary>
    internal class CommonTypeEarlyWellKnownAttributeData : EarlyWellKnownAttributeData
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

                _obsoleteAttributeData = value;
                SetDataStored();
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
