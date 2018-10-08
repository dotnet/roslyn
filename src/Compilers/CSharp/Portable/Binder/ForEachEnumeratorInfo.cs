﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Information to be deduced while binding a foreach loop so that the loop can be lowered
    /// to a while over an enumerator.  Not applicable to the array or string forms.
    /// </summary>
    internal sealed class ForEachEnumeratorInfo
    {
        // Types identified by the algorithm in the spec (8.8.4).
        public readonly TypeSymbol CollectionType;
        // public readonly TypeSymbol EnumeratorType; // redundant - return type of GetEnumeratorMethod
        public readonly TypeSymbolWithAnnotations ElementType;

        // Members required by the "pattern" based approach.  Also populated for other approaches.
        public readonly MethodSymbol GetEnumeratorMethod;
        public readonly MethodSymbol CurrentPropertyGetter;
        public readonly MethodSymbol MoveNextMethod;

        // Dispose method to be called on the enumerator (may be null).
        // Computed during initial binding so that we can expose it in the semantic model.
        public readonly bool NeedsDisposeMethod;

        // Conversions that will be required when the foreach is lowered.
        public readonly Conversion CollectionConversion; //collection expression to collection type
        public readonly Conversion CurrentConversion; // current to element type
        // public readonly Conversion ElementConversion; // element type to iteration var type - also required for arrays, so stored elsewhere
        public readonly Conversion EnumeratorConversion; // enumerator to object

        public readonly BinderFlags Location;

        private ForEachEnumeratorInfo(
            TypeSymbol collectionType,
            TypeSymbolWithAnnotations elementType,
            MethodSymbol getEnumeratorMethod,
            MethodSymbol currentPropertyGetter,
            MethodSymbol moveNextMethod,
            bool needsDisposeMethod,
            Conversion collectionConversion,
            Conversion currentConversion,
            Conversion enumeratorConversion,
            BinderFlags location)
        {
            Debug.Assert((object)collectionType != null, "Field 'collectionType' cannot be null");
            Debug.Assert(!elementType.IsNull, "Field 'elementType' cannot be null");
            Debug.Assert((object)getEnumeratorMethod != null, "Field 'getEnumeratorMethod' cannot be null");
            Debug.Assert((object)currentPropertyGetter != null, "Field 'currentPropertyGetter' cannot be null");
            Debug.Assert((object)moveNextMethod != null, "Field 'moveNextMethod' cannot be null");

            this.CollectionType = collectionType;
            this.ElementType = elementType;
            this.GetEnumeratorMethod = getEnumeratorMethod;
            this.CurrentPropertyGetter = currentPropertyGetter;
            this.MoveNextMethod = moveNextMethod;
            this.NeedsDisposeMethod = needsDisposeMethod;
            this.CollectionConversion = collectionConversion;
            this.CurrentConversion = currentConversion;
            this.EnumeratorConversion = enumeratorConversion;
            this.Location = location;
        }

        // Mutable version of ForEachEnumeratorInfo.  Convert to immutable using Build.
        internal struct Builder
        {
            public TypeSymbol CollectionType;
            public TypeSymbolWithAnnotations ElementType;

            public MethodSymbol GetEnumeratorMethod;
            public MethodSymbol CurrentPropertyGetter;
            public MethodSymbol MoveNextMethod;

            public bool NeedsDisposeMethod;

            public Conversion CollectionConversion;
            public Conversion CurrentConversion;
            public Conversion EnumeratorConversion;

            public ForEachEnumeratorInfo Build(BinderFlags location)
            {
                return new ForEachEnumeratorInfo(
                    CollectionType,
                    ElementType,
                    GetEnumeratorMethod,
                    CurrentPropertyGetter,
                    MoveNextMethod,
                    NeedsDisposeMethod,
                    CollectionConversion,
                    CurrentConversion,
                    EnumeratorConversion,
                    location);
            }
        }
    }
}
