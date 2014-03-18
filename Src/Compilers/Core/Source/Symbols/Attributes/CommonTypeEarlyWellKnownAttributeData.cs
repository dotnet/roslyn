// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private AttributeUsageInfo attributeUsageInfo = AttributeUsageInfo.Null;
        public AttributeUsageInfo AttributeUsageInfo
        {
            get
            {
                return this.attributeUsageInfo;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(attributeUsageInfo.IsNull);
                Debug.Assert(!value.IsNull);
                this.attributeUsageInfo = value;
                SetDataStored();
            }
        }
        #endregion

        #region ComImportAttribute
        private bool hasComImportAttribute;
        public bool HasComImportAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasComImportAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasComImportAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region ConditionalAttribute
        private ImmutableArray<string> lazyConditionalSymbols = ImmutableArray<string>.Empty;

        public void AddConditionalSymbol(string name)
        {
            VerifySealed(expected: false);
            lazyConditionalSymbols = lazyConditionalSymbols.Add(name);
            SetDataStored();

        }

        public ImmutableArray<string> ConditionalSymbols
        {
            get
            {
                VerifySealed(expected: true);
                return lazyConditionalSymbols;
            }
        }
        #endregion

        #region ObsoleteAttribute
        private ObsoleteAttributeData obsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
        public ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                VerifySealed(expected: true);
                return this.obsoleteAttributeData.IsUninitialized ? null : this.obsoleteAttributeData;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);
                Debug.Assert(!value.IsUninitialized);

                this.obsoleteAttributeData = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
