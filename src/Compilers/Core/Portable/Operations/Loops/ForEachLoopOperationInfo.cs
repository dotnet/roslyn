// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

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
        public readonly bool NeedsDispose;
        public readonly bool KnownToImplementIDisposable;

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

        public ForEachLoopOperationInfo(
            ITypeSymbol elementType,
            IMethodSymbol getEnumeratorMethod,
            IPropertySymbol currentProperty,
            IMethodSymbol moveNextMethod,
            bool isAsynchronous,
            bool needsDispose,
            bool knownToImplementIDisposable,
            IConvertibleConversion currentConversion,
            IConvertibleConversion elementConversion,
            ImmutableArray<IArgumentOperation> getEnumeratorArguments = default,
            ImmutableArray<IArgumentOperation> moveNextArguments = default,
            ImmutableArray<IArgumentOperation> currentArguments = default)
        {
            ElementType = elementType;
            GetEnumeratorMethod = getEnumeratorMethod;
            CurrentProperty = currentProperty;
            MoveNextMethod = moveNextMethod;
            IsAsynchronous = isAsynchronous;
            NeedsDispose = needsDispose;
            KnownToImplementIDisposable = knownToImplementIDisposable;
            CurrentConversion = currentConversion;
            ElementConversion = elementConversion;
            GetEnumeratorArguments = getEnumeratorArguments;
            MoveNextArguments = moveNextArguments;
            CurrentArguments = currentArguments;
        }
    }
}
