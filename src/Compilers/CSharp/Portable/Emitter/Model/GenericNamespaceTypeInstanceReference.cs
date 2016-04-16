// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to a generic type instantiation that is not nested.
    /// e.g. MyNamespace.A{int}
    /// </summary>
    internal sealed class GenericNamespaceTypeInstanceReference : GenericTypeInstanceReference
    {
        public GenericNamespaceTypeInstanceReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
        }

        public override Microsoft.Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return this; }
        }

        public override Microsoft.Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        public override Microsoft.Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return null; }
        }

        public override Microsoft.Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return null; }
        }
    }
}
