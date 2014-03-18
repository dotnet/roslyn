// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to a type nested in an instantiation of a generic type.
    /// e.g. 
    /// A{int}.B
    /// A.B{int}.C.D
    /// </summary>
    internal class SpecializedNestedTypeReference : NamedTypeReference, Microsoft.Cci.ISpecializedNestedTypeReference
    {
        public SpecializedNestedTypeReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
        }

        Microsoft.Cci.INestedTypeReference Microsoft.Cci.ISpecializedNestedTypeReference.UnspecializedVersion
        {
            get
            {
                System.Diagnostics.Debug.Assert(UnderlyingNamedType.OriginalDefinition.IsDefinition);
                return this.UnderlyingNamedType.OriginalDefinition;
            }
        }

        public override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.ISpecializedNestedTypeReference)this);
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.ITypeMemberReference.GetContainingType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingNamedType.ContainingType, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
        }

        public override Microsoft.Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        public override Microsoft.Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        public override Microsoft.Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return this; }
        }

        public override Microsoft.Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return this; }
        }
    }
}
