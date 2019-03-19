// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public interface INamespaceOrTypeDeclaration
    {
        string Name { get; }

        ImmutableArray<INamespaceOrTypeDeclaration> Children { get; }

        bool IsNamespace { get; }

        bool IsType { get; }
    }
}
