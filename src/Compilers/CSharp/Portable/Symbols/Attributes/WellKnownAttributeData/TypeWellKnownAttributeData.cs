// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a type.
    /// </summary>
    internal sealed class TypeWellKnownAttributeData : CommonTypeWellKnownAttributeData
    {
        #region CoClassAttribute

        private NamedTypeSymbol _comImportCoClass;
        public NamedTypeSymbol ComImportCoClass
        {
            get
            {
                return _comImportCoClass;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert((object)_comImportCoClass == null);
                Debug.Assert((object)value != null);
                _comImportCoClass = value;
                SetDataStored();
            }
        }

        #endregion

        #region NullableOptOutAttribute

        private bool? _nullableOptOut;
        public bool? NullableOptOut
        {
            get
            {
                VerifySealed(expected: true);
                return _nullableOptOut;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value.HasValue);
                _nullableOptOut = value;
                SetDataStored();
            }
        }

        #endregion
    }
}
