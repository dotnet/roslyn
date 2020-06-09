// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class NamespaceSymbol : Cci.INamespace
    {
        Cci.INamespace Cci.INamespace.ContainingNamespace => ContainingNamespace;
        string Cci.INamedEntity.Name => MetadataName;
    }
}
