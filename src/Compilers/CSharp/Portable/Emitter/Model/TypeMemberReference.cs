// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class TypeMemberReference : Cci.ITypeMemberReference
    {
        protected abstract Symbol UnderlyingSymbol { get; }

        public virtual Cci.ITypeReference GetContainingType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;
            return moduleBeingBuilt.Translate(UnderlyingSymbol.ContainingType, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics);
        }

        string Cci.INamedEntity.Name
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

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public abstract void Dispatch(Cci.MetadataVisitor visitor);

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => UnderlyingSymbol;

        public sealed override bool Equals(object obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }
    }
}
