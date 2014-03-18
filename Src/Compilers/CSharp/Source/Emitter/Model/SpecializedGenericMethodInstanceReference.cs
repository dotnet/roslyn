// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.



using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a generic method of a generic type instantiation, closed over type parameters.
    /// e.g. 
    /// A{T}.M{S}()
    /// A.B{T}.C.M{S}()
    /// </summary>
    internal sealed class SpecializedGenericMethodInstanceReference : SpecializedMethodReference, Microsoft.Cci.IGenericMethodInstanceReference
    {
        private readonly SpecializedMethodReference genericMethod;

        public SpecializedGenericMethodInstanceReference(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
            Debug.Assert(PEModuleBuilder.IsGenericType(underlyingMethod.ContainingType) && underlyingMethod.ContainingType.IsDefinition);
            genericMethod = new SpecializedMethodReference(underlyingMethod);
        }

        System.Collections.Generic.IEnumerable<Microsoft.Cci.ITypeReference> Microsoft.Cci.IGenericMethodInstanceReference.GetGenericArguments(Microsoft.CodeAnalysis.Emit.Context context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (var arg in UnderlyingMethod.TypeArguments)
            {
                yield return moduleBeingBuilt.Translate(arg, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
            }
        }

        Microsoft.Cci.IMethodReference Microsoft.Cci.IGenericMethodInstanceReference.GetGenericMethod(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return genericMethod;
        }

        public override Microsoft.Cci.IGenericMethodInstanceReference AsGenericMethodInstanceReference
        {
            get
            {
                return this;
            }
        }

        public override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IGenericMethodInstanceReference)this);
        }

    }
}
