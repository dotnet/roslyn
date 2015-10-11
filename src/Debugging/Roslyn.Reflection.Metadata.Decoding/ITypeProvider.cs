// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// NOTE: This is a temporary internal copy of code that will be cut from System.Reflection.Metadata v1.1 and
//       ship in System.Reflection.Metadata v1.2 (with breaking changes). Remove and use the public API when
//       a v1.2 prerelease is available and code flow is such that we can start to depend on it.

using System.Collections.Immutable;

namespace Roslyn.Reflection.Metadata.Decoding
{
    internal interface ITypeProvider<TType>
    {
        TType GetGenericInstance(TType genericType, ImmutableArray<TType> typeArguments);
        TType GetArrayType(TType elementType, ArrayShape shape);
        TType GetByReferenceType(TType elementType);
        TType GetSZArrayType(TType elementType);
        TType GetPointerType(TType elementType);
    }
}
