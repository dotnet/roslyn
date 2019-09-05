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
    internal sealed class TypeWellKnownAttributeData : CommonTypeWellKnownAttributeData, ISkipLocalsInitAttributeTarget
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

        #region SkipLocalsInitAttribute
        private bool _hasSkipLocalsInitAttribute;
        public bool HasSkipLocalsInitAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSkipLocalsInitAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSkipLocalsInitAttribute = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
