// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public interface ITypeDeclaration : INamespaceOrTypeDeclaration
    {
        TypeKind TypeKind { get; }

        Accessibility DeclaredAccessibility { get; }

        int Arity { get; }

        new ImmutableArray<ITypeDeclaration> Children { get; }
    }
}
