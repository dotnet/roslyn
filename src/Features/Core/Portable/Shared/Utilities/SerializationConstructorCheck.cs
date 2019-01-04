// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal readonly struct SerializationConstructorCheck
    {
        private readonly INamedTypeSymbol _iSerializableType, _serializationInfoType, _streamingContextType;

        public SerializationConstructorCheck(Compilation compilation)
        {
            _iSerializableType = compilation.ISerializableType();
            _serializationInfoType = compilation.SerializationInfoType();
            _streamingContextType = compilation.StreamingContextType();
        }

        // True if the method is a constructor adhereing to the pattern used for custom
        // deserialisation by types that implement System.Runtime.Serialization.ISerializable
        public bool IsISerializableConstructor(IMethodSymbol methodSymbol)
            => _iSerializableType != null &&
               methodSymbol.MethodKind == MethodKind.Constructor &&
               methodSymbol.Parameters.Length == 2 &&
               methodSymbol.Parameters[0].Type.Equals(_serializationInfoType) &&
               methodSymbol.Parameters[1].Type.Equals(_streamingContextType) &&
               methodSymbol.ContainingType.AllInterfaces.Contains(_iSerializableType);
    }
}
