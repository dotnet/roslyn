// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class NamespaceSymbol : Cci.INamespace
    {
        Cci.INamespace Cci.INamespace.ContainingNamespace => ContainingNamespace;
        string Cci.INamedEntity.Name => MetadataName;
    }
}
