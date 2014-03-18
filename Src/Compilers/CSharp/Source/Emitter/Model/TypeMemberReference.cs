// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class TypeMemberReference : Microsoft.Cci.ITypeMemberReference
    {
        protected abstract Symbol UnderlyingSymbol { get; }

        public virtual Microsoft.Cci.ITypeReference /*Microsoft.Cci.ITypeMemberReference*/ GetContainingType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(UnderlyingSymbol.ContainingType, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
        }

        string Microsoft.Cci.INamedEntity.Name
        {
            get
            {
                return UnderlyingSymbol.MetadataName;
            }
        }

        /// <remarks>
        /// Used only for testing.
        /// </remarks>
        public override string ToString()
        {
            return UnderlyingSymbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();

            // foreach (var a in GetAttributes()) yield return a; // this throws today.
        }

        public abstract void /*Microsoft.Cci.IReference*/ Dispatch(Microsoft.Cci.MetadataVisitor visitor);

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }
    }
}
