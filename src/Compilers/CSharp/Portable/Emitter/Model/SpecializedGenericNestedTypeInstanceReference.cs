// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to an instantiation of a generic type nested in an instantiation of another generic type.
    /// e.g. 
    /// A{int}.B{string}
    /// A.B{int}.C.D{string}
    /// </summary>
    internal sealed class SpecializedGenericNestedTypeInstanceReference : SpecializedNestedTypeReference, Cci.IGenericTypeInstanceReference
    {
        public SpecializedGenericNestedTypeInstanceReference(NamedTypeSymbol underlyingNamedType)
            : base(underlyingNamedType)
        {
            Debug.Assert(underlyingNamedType.IsDefinition);
            // Definition doesn't have custom modifiers on type arguments
            Debug.Assert(!underlyingNamedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Any(static a => a.CustomModifiers.Any()));
        }

        public sealed override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IGenericTypeInstanceReference)this);
        }

        ImmutableArray<Cci.ITypeReference> Cci.IGenericTypeInstanceReference.GetGenericArguments(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var builder = ArrayBuilder<Cci.ITypeReference>.GetInstance();
            foreach (TypeWithAnnotations type in UnderlyingNamedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
            {
                builder.Add(moduleBeingBuilt.Translate(type.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode, diagnostics: context.Diagnostics, eraseExtensions: false));
            }

            return builder.ToImmutableAndFree();
        }

        Cci.INamedTypeReference Cci.IGenericTypeInstanceReference.GetGenericType(EmitContext context)
        {
            Debug.Assert(UnderlyingNamedType.OriginalDefinition.IsDefinition);
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return (Cci.INamedTypeReference)moduleBeingBuilt.Translate(
                this.UnderlyingNamedType.OriginalDefinition, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                diagnostics: context.Diagnostics, ExtensionsEraseMode.None, needDeclaration: true);
        }

        public override Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return this; }
        }

        public override Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        public override Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return this; }
        }

        public override Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return null; }
        }
    }
}
