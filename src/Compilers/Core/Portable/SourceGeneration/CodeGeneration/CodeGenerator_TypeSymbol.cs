// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        private abstract class TypeSymbol : NamespaceOrTypeSymbol, ITypeSymbol
        {
            public abstract TypeKind TypeKind { get; }

            #region default implementation

            ITypeSymbol ITypeSymbol.OriginalDefinition => throw new NotImplementedException();
            public virtual bool IsAnonymousType => throw new NotImplementedException();
            public virtual bool IsNativeIntegerType => throw new NotImplementedException();
            public virtual bool IsReadOnly => throw new NotImplementedException();
            public virtual bool IsReferenceType => throw new NotImplementedException();
            public virtual bool IsRefLikeType => throw new NotImplementedException();
            public virtual bool IsTupleType => throw new NotImplementedException();
            public virtual bool IsUnmanagedType => throw new NotImplementedException();
            public virtual bool IsValueType => throw new NotImplementedException();
            public virtual ImmutableArray<INamedTypeSymbol> AllInterfaces => throw new NotImplementedException();
            public virtual ImmutableArray<INamedTypeSymbol> Interfaces => throw new NotImplementedException();
            public virtual ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public virtual ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public virtual INamedTypeSymbol BaseType => throw new NotImplementedException();
            public virtual ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember) => throw new NotImplementedException();
            public virtual ITypeSymbol WithNullableAnnotation(NullableAnnotation nullableAnnotation) => throw new NotImplementedException();
            public virtual NullableAnnotation NullableAnnotation => throw new NotImplementedException();
            public virtual SpecialType SpecialType => throw new NotImplementedException();
            public virtual string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null) => throw new NotImplementedException();
            public virtual string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null) => throw new NotImplementedException();

            #endregion
        }
    }
}
