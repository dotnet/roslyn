// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a reference to a generic method instantiation, closed over type parameters,
    /// e.g. MyNamespace.Class.Method{T}()
    /// </summary>
    internal sealed class GenericMethodInstanceReference : MethodReference, Microsoft.Cci.IGenericMethodInstanceReference
    {
        public GenericMethodInstanceReference(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
        }

        public override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IGenericMethodInstanceReference)this);
        }

        IEnumerable<Microsoft.Cci.ITypeReference> Microsoft.Cci.IGenericMethodInstanceReference.GetGenericArguments(Microsoft.CodeAnalysis.Emit.Context context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            foreach (var arg in UnderlyingMethod.TypeArguments)
            {
                yield return moduleBeingBuilt.Translate(arg, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
            }
        }

        Microsoft.Cci.IMethodReference Microsoft.Cci.IGenericMethodInstanceReference.GetGenericMethod(Microsoft.CodeAnalysis.Emit.Context context)
        {
            // NoPia method might come through here.
            return ((PEModuleBuilder)context.Module).Translate(
                (MethodSymbol)UnderlyingMethod.OriginalDefinition,
                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                diagnostics: context.Diagnostics,
                needDeclaration: true);
        }

        public override Microsoft.Cci.IGenericMethodInstanceReference AsGenericMethodInstanceReference
        {
            get
            {
                return this;
            }
        }
    }
}
