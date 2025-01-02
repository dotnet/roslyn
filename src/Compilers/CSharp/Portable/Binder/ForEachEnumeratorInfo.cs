// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

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

        public readonly WellKnownType InlineArraySpanType;
        public readonly bool InlineArrayUsedAsValue;

        // public readonly TypeSymbol EnumeratorType; // redundant - return type of GetEnumeratorMethod
        public readonly TypeWithAnnotations ElementTypeWithAnnotations;
        public TypeSymbol ElementType => ElementTypeWithAnnotations.Type;

        // Members required by the "pattern" based approach.  Also populated for other approaches.
        public readonly MethodArgumentInfo GetEnumeratorInfo;
        public readonly MethodSymbol CurrentPropertyGetter;
        public readonly MethodArgumentInfo MoveNextInfo;

        // True if the enumerator needs disposal once used. 
        // Will be either IDisposable/IAsyncDisposable, or use DisposeMethod below if set
        // Computed during initial binding so that we can expose it in the semantic model.
        public readonly bool NeedsDisposal;

        public readonly bool IsAsync;

        // When async and needs disposal, this stores the information to await the DisposeAsync() invocation
        public readonly BoundAwaitableInfo? DisposeAwaitableInfo;

        // When using pattern-based Dispose, this stores the method to invoke to Dispose
        public readonly MethodArgumentInfo? PatternDisposeInfo;

        // Conversions that will be required when the foreach is lowered.
        public readonly BoundValuePlaceholder? CurrentPlaceholder;
        public readonly BoundExpression? CurrentConversion; // current to element type

        public readonly BinderFlags Location;

        private ForEachEnumeratorInfo(
            TypeSymbol collectionType,
            WellKnownType inlineArraySpanType,
            bool inlineArrayUsedAsValue,
            TypeWithAnnotations elementType,
            MethodArgumentInfo getEnumeratorInfo,
            MethodSymbol currentPropertyGetter,
            MethodArgumentInfo moveNextInfo,
            bool isAsync,
            bool needsDisposal,
            BoundAwaitableInfo? disposeAwaitableInfo,
            MethodArgumentInfo? patternDisposeInfo,
            BoundValuePlaceholder? currentPlaceholder,
            BoundExpression? currentConversion,
            BinderFlags location)
        {
            RoslynDebug.Assert((object)collectionType != null, $"Field '{nameof(collectionType)}' cannot be null");
            RoslynDebug.Assert(elementType.HasType, $"Field '{nameof(elementType)}' cannot be null");
            RoslynDebug.Assert((object)getEnumeratorInfo != null, $"Field '{nameof(getEnumeratorInfo)}' cannot be null");
            RoslynDebug.Assert((object)currentPropertyGetter != null, $"Field '{nameof(currentPropertyGetter)}' cannot be null");
            RoslynDebug.Assert((object)moveNextInfo != null, $"Field '{nameof(moveNextInfo)}' cannot be null");
            Debug.Assert(patternDisposeInfo == null || needsDisposal);
            Debug.Assert(inlineArraySpanType is WellKnownType.Unknown or WellKnownType.System_Span_T or WellKnownType.System_ReadOnlySpan_T);
            Debug.Assert(inlineArraySpanType == WellKnownType.Unknown ||
                         (collectionType.HasInlineArrayAttribute(out _) && collectionType.TryGetInlineArrayElementField() is FieldSymbol elementField && elementType.Equals(elementField.TypeWithAnnotations, TypeCompareKind.ConsiderEverything)));
            Debug.Assert(!inlineArrayUsedAsValue || inlineArraySpanType != WellKnownType.Unknown);

            this.CollectionType = collectionType;
            this.InlineArraySpanType = inlineArraySpanType;
            this.InlineArrayUsedAsValue = inlineArrayUsedAsValue;
            this.ElementTypeWithAnnotations = elementType;
            this.GetEnumeratorInfo = getEnumeratorInfo;
            this.CurrentPropertyGetter = currentPropertyGetter;
            this.MoveNextInfo = moveNextInfo;
            this.IsAsync = isAsync;
            this.NeedsDisposal = needsDisposal;
            this.DisposeAwaitableInfo = disposeAwaitableInfo;
            this.PatternDisposeInfo = patternDisposeInfo;
            this.CurrentPlaceholder = currentPlaceholder;
            this.CurrentConversion = currentConversion;
            this.Location = location;
        }

        // Mutable version of ForEachEnumeratorInfo.  Convert to immutable using Build.
        internal struct Builder
        {
            public TypeSymbol CollectionType;
            public bool ViaExtensionMethod;
            public WellKnownType InlineArraySpanType;
            public bool InlineArrayUsedAsValue;
            public TypeWithAnnotations ElementTypeWithAnnotations;
            public TypeSymbol ElementType => ElementTypeWithAnnotations.Type;

            public MethodArgumentInfo? GetEnumeratorInfo;
            public MethodSymbol CurrentPropertyGetter;
            public MethodArgumentInfo? MoveNextInfo;

            public bool IsAsync;
            public bool NeedsDisposal;
            public BoundAwaitableInfo? DisposeAwaitableInfo;
            public MethodArgumentInfo? PatternDisposeInfo;

            public BoundValuePlaceholder? CurrentPlaceholder;
            public BoundExpression? CurrentConversion;

            public ForEachEnumeratorInfo Build(BinderFlags location)
            {
                Debug.Assert((object)CollectionType != null, $"'{nameof(CollectionType)}' cannot be null");
                Debug.Assert((object)ElementType != null, $"'{nameof(ElementType)}' cannot be null");
                Debug.Assert(GetEnumeratorInfo != null, $"'{nameof(GetEnumeratorInfo)}' cannot be null");

                Debug.Assert(MoveNextInfo != null);
                Debug.Assert(CurrentPropertyGetter != null);

                return new ForEachEnumeratorInfo(
                    CollectionType,
                    InlineArraySpanType,
                    InlineArrayUsedAsValue,
                    ElementTypeWithAnnotations,
                    GetEnumeratorInfo,
                    CurrentPropertyGetter,
                    MoveNextInfo,
                    IsAsync,
                    NeedsDisposal,
                    DisposeAwaitableInfo,
                    PatternDisposeInfo,
                    CurrentPlaceholder,
                    CurrentConversion,
                    location);
            }

            public bool IsIncomplete
                => GetEnumeratorInfo is null || MoveNextInfo is null || CurrentPropertyGetter is null;
        }
    }
}
