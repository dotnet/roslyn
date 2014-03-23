// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to an instantiation of a generic type nested in an instantiation of another generic type.
    /// e.g. 
    /// A{int}.B{string}
    /// A.B{int}.C.D{string}
    /// </summary>
    internal sealed class SpecializedGenericNestedTypeInstanceReference : SpecializedNestedTypeReference, Microsoft.Cci.IGenericTypeInstanceReference
    {
        public SpecializedGenericNestedTypeInstanceReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
        }

        public sealed override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IGenericTypeInstanceReference)this);
        }

        ImmutableArray<Microsoft.Cci.ITypeReference> Microsoft.Cci.IGenericTypeInstanceReference.GetGenericArguments(Microsoft.CodeAnalysis.Emit.Context context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var builder = ArrayBuilder<Microsoft.Cci.ITypeReference>.GetInstance();
            foreach (TypeSymbol type in UnderlyingNamedType.TypeArgumentsNoUseSiteDiagnostics)
            {
                builder.Add(moduleBeingBuilt.Translate(type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics));
            }

            return builder.ToImmutableAndFree();

        }

        Microsoft.Cci.INamedTypeReference Microsoft.Cci.IGenericTypeInstanceReference.GenericType
        {
            get
            {
                System.Diagnostics.Debug.Assert(UnderlyingNamedType.OriginalDefinition.IsDefinition);
                return this.UnderlyingNamedType.OriginalDefinition;
            }
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
            get { return this; }
        }

        public override Microsoft.Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return null; }
        }
    }
}
