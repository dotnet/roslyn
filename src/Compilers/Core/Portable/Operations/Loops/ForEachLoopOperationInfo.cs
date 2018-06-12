// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal struct ForEachLoopOperationInfo
    {
        /// <summary>
        /// Element type of the collection
        /// </summary>
        public readonly ITypeSymbol ElementType;

        public readonly IMethodSymbol GetEnumeratorMethod;
        public readonly IPropertySymbol CurrentProperty;
        public readonly IMethodSymbol MoveNextMethod;

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

        public readonly Lazy<ImmutableArray<IArgumentOperation>> GetEnumeratorArguments;
        public readonly Lazy<ImmutableArray<IArgumentOperation>> MoveNextArguments;
        public readonly Lazy<ImmutableArray<IArgumentOperation>> CurrentArguments;

        public ForEachLoopOperationInfo(
            ITypeSymbol elementType,
            IMethodSymbol getEnumeratorMethod,
            IPropertySymbol currentProperty,
            IMethodSymbol moveNextMethod,
            bool needsDispose,
            bool knownToImplementIDisposable,
            IConvertibleConversion currentConversion,
            IConvertibleConversion elementConversion,
            Lazy<ImmutableArray<IArgumentOperation>> getEnumeratorArguments = default,
            Lazy<ImmutableArray<IArgumentOperation>> moveNextArguments = default,
            Lazy<ImmutableArray<IArgumentOperation>> currentArguments = default)
        {
            ElementType = elementType;
            GetEnumeratorMethod = getEnumeratorMethod;
            CurrentProperty = currentProperty;
            MoveNextMethod = moveNextMethod;
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
