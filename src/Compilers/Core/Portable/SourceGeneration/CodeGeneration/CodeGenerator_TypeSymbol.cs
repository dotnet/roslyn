// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal static partial class CodeGenerator
    {
        private class TypeSymbol : Symbol, ITypeSymbol
        {
            public virtual TypeKind TypeKind => throw new System.NotImplementedException();

            public virtual INamedTypeSymbol BaseType => throw new System.NotImplementedException();

            public virtual ImmutableArray<INamedTypeSymbol> Interfaces => throw new System.NotImplementedException();

            public virtual ImmutableArray<INamedTypeSymbol> AllInterfaces => throw new System.NotImplementedException();

            public virtual bool IsReferenceType => throw new System.NotImplementedException();

            public virtual bool IsValueType => throw new System.NotImplementedException();

            public virtual bool IsAnonymousType => throw new System.NotImplementedException();

            public virtual bool IsTupleType => throw new System.NotImplementedException();

            public virtual bool IsNativeIntegerType => throw new System.NotImplementedException();

            public virtual SpecialType SpecialType => throw new System.NotImplementedException();

            public virtual bool IsRefLikeType => throw new System.NotImplementedException();

            public virtual bool IsUnmanagedType => throw new System.NotImplementedException();

            public virtual bool IsReadOnly => throw new System.NotImplementedException();

            public virtual NullableAnnotation NullableAnnotation => throw new System.NotImplementedException();

            public virtual bool IsNamespace => throw new System.NotImplementedException();

            public virtual bool IsType => throw new System.NotImplementedException();

            ITypeSymbol ITypeSymbol.OriginalDefinition => throw new System.NotImplementedException();

            public virtual ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<ISymbol> GetMembers()
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<ISymbol> GetMembers(string name)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<INamedTypeSymbol> GetTypeMembers()
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            {
                throw new System.NotImplementedException();
            }

            public virtual ITypeSymbol WithNullableAnnotation(NullableAnnotation nullableAnnotation)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
