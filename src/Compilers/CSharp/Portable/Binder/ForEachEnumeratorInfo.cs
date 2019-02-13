﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

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

        // True if the enumerator needs disposal once used. 
        // Will be either IDisposable/IAsyncDisposable, or use DisposeMethod below if set
        // Computed during initial binding so that we can expose it in the semantic model.
        public readonly bool NeedsDisposal;

        // When async and needs disposal, this stores the information to await the DisposeAsync() invocation
        public AwaitableInfo DisposeAwaitableInfo;

        // When using pattern-based Dispose, this stores the method to invoke to Dispose
        public readonly MethodSymbol DisposeMethod;

        // Conversions that will be required when the foreach is lowered.
        public readonly Conversion CollectionConversion; //collection expression to collection type
        public readonly Conversion CurrentConversion; // current to element type
        // public readonly Conversion ElementConversion; // element type to iteration var type - also required for arrays, so stored elsewhere
        public readonly Conversion EnumeratorConversion; // enumerator to object

        public readonly BinderFlags Location;

        internal bool IsAsync
            => DisposeAwaitableInfo != null;

        private ForEachEnumeratorInfo(
            TypeSymbol collectionType,
            TypeSymbolWithAnnotations elementType,
            MethodSymbol getEnumeratorMethod,
            MethodSymbol currentPropertyGetter,
            MethodSymbol moveNextMethod,
            bool needsDisposal,
            AwaitableInfo disposeAwaitableInfo,
            MethodSymbol disposeMethod,
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
            this.NeedsDisposal = needsDisposal;
            this.DisposeAwaitableInfo = disposeAwaitableInfo;
            this.DisposeMethod = disposeMethod;
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

            public bool NeedsDisposal;
            public AwaitableInfo DisposeAwaitableInfo;
            public MethodSymbol DisposeMethod;

            public Conversion CollectionConversion;
            public Conversion CurrentConversion;
            public Conversion EnumeratorConversion;

            public ForEachEnumeratorInfo Build(BinderFlags location)
            {
                Debug.Assert((object)CollectionType != null, "'CollectionType' cannot be null");
                Debug.Assert((object)ElementType != null, "'ElementType' cannot be null");
                Debug.Assert((object)GetEnumeratorMethod != null, "'GetEnumeratorMethod' cannot be null");

                Debug.Assert(MoveNextMethod != null);
                Debug.Assert(CurrentPropertyGetter != null);

                return new ForEachEnumeratorInfo(
                    CollectionType,
                    ElementType,
                    GetEnumeratorMethod,
                    CurrentPropertyGetter,
                    MoveNextMethod,
                    NeedsDisposal,
                    DisposeAwaitableInfo,
                    DisposeMethod,
                    CollectionConversion,
                    CurrentConversion,
                    EnumeratorConversion,
                    location);
            }

            public bool IsIncomplete
                => GetEnumeratorMethod is null || MoveNextMethod is null || CurrentPropertyGetter is null;
        }
    }
}
