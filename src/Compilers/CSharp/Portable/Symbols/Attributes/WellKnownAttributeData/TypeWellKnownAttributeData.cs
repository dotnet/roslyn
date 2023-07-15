// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
#nullable enable
    internal sealed class CollectionBuilderAttributeData
    {
        public static readonly CollectionBuilderAttributeData Uninitialized = new CollectionBuilderAttributeData(null, null);

        public CollectionBuilderAttributeData(TypeSymbol? builderType, string? methodName)
        {
            BuilderType = builderType;
            MethodName = methodName;
        }

        public readonly TypeSymbol? BuilderType;
        public readonly string? MethodName;
    }
#nullable disable

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

#nullable enable
        #region CollectionBuilderAttribute
        private CollectionBuilderAttributeData? _collectionBuilder;
        public CollectionBuilderAttributeData? CollectionBuilder
        {
            get
            {
                VerifySealed(expected: true);
                return _collectionBuilder;
            }
            set
            {
                VerifySealed(expected: false);
                _collectionBuilder ??= value;
                SetDataStored();
            }
        }
        #endregion
#nullable disable
    }
}
