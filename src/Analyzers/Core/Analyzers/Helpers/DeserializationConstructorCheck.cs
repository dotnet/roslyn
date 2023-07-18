// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal readonly struct DeserializationConstructorCheck(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _iSerializableType = compilation.ISerializableType();
        private readonly INamedTypeSymbol? _serializationInfoType = compilation.SerializationInfoType();
        private readonly INamedTypeSymbol? _streamingContextType = compilation.StreamingContextType();

        // True if the method is a constructor adhering to the pattern used for custom
        // deserialization by types that implement System.Runtime.Serialization.ISerializable
        public bool IsDeserializationConstructor(IMethodSymbol methodSymbol)
            => _iSerializableType != null &&
               methodSymbol.MethodKind == MethodKind.Constructor &&
               methodSymbol.Parameters.Length == 2 &&
               methodSymbol.Parameters[0].Type.Equals(_serializationInfoType) &&
               methodSymbol.Parameters[1].Type.Equals(_streamingContextType) &&
               methodSymbol.ContainingType.AllInterfaces.Contains(_iSerializableType);
    }
}
