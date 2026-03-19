// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Operations
{
    internal class ForEachLoopOperationInfo
    {
        /// <summary>
        /// Element type of the collection
        /// </summary>
        public readonly ITypeSymbol ElementType;

        public readonly IMethodSymbol GetEnumeratorMethod;
        public readonly IPropertySymbol CurrentProperty;
        public readonly IMethodSymbol MoveNextMethod;

        public readonly bool IsAsynchronous;
        public readonly IConvertibleConversion? InlineArrayConversion;
        public readonly bool CollectionIsInlineArrayValue;
        public readonly bool NeedsDispose;
        public readonly bool KnownToImplementIDisposable;
        public readonly IMethodSymbol? PatternDisposeMethod;

        /// <summary>
        /// The conversion from the type of the <see cref="CurrentProperty"/> to the <see cref="ElementType"/>.
        /// </summary>
        public readonly IConvertibleConversion CurrentConversion;

        /// <summary>
        /// The conversion from the <see cref="ElementType"/> to the iteration variable type.
        /// </summary>
        public readonly IConvertibleConversion ElementConversion;

        public readonly ImmutableArray<IArgumentOperation> GetEnumeratorArguments;
        public readonly ImmutableArray<IArgumentOperation> MoveNextArguments;
        public readonly ImmutableArray<IArgumentOperation> CurrentArguments;
        public readonly ImmutableArray<IArgumentOperation> DisposeArguments;

        public ForEachLoopOperationInfo(
            ITypeSymbol elementType,
            IMethodSymbol getEnumeratorMethod,
            IPropertySymbol currentProperty,
            IMethodSymbol moveNextMethod,
            bool isAsynchronous,
            IConvertibleConversion? inlineArrayConversion,
            bool collectionIsInlineArrayValue,
            bool needsDispose,
            bool knownToImplementIDisposable,
            IMethodSymbol? patternDisposeMethod,
            IConvertibleConversion currentConversion,
            IConvertibleConversion elementConversion,
            ImmutableArray<IArgumentOperation> getEnumeratorArguments = default,
            ImmutableArray<IArgumentOperation> moveNextArguments = default,
            ImmutableArray<IArgumentOperation> currentArguments = default,
            ImmutableArray<IArgumentOperation> disposeArguments = default)
        {
            Debug.Assert(!collectionIsInlineArrayValue || inlineArrayConversion is { });

            ElementType = elementType;
            GetEnumeratorMethod = getEnumeratorMethod;
            CurrentProperty = currentProperty;
            MoveNextMethod = moveNextMethod;
            IsAsynchronous = isAsynchronous;
            InlineArrayConversion = inlineArrayConversion;
            CollectionIsInlineArrayValue = collectionIsInlineArrayValue;
            KnownToImplementIDisposable = knownToImplementIDisposable;
            NeedsDispose = needsDispose;
            PatternDisposeMethod = patternDisposeMethod;
            CurrentConversion = currentConversion;
            ElementConversion = elementConversion;
            GetEnumeratorArguments = getEnumeratorArguments;
            MoveNextArguments = moveNextArguments;
            CurrentArguments = currentArguments;
            DisposeArguments = disposeArguments;
        }
    }
}
