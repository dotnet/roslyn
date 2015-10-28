// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class TypeParameterSymbol :
        Cci.IGenericParameterReference,
        Cci.IGenericMethodParameterReference,
        Cci.IGenericTypeParameterReference,
        Cci.IGenericParameter,
        Cci.IGenericMethodParameter,
        Cci.IGenericTypeParameter
    {
        bool Cci.ITypeReference.IsEnum
        {
            get { return false; }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get { return false; }
        }

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            return null;
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode(EmitContext context)
        {
            return Cci.PrimitiveTypeCode.NotPrimitive;
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get { return default(TypeDefinitionHandle); }
        }

        Cci.IGenericMethodParameter Cci.IGenericParameter.AsGenericMethodParameter
        {
            get
            {
                CheckDefinitionInvariant();

                if (this.ContainingSymbol.Kind == SymbolKind.Method)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                Debug.Assert(this.IsDefinition);

                if (this.ContainingSymbol.Kind == SymbolKind.Method)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        Cci.IGenericTypeParameter Cci.IGenericParameter.AsGenericTypeParameter
        {
            get
            {
                CheckDefinitionInvariant();

                if (this.ContainingSymbol.Kind == SymbolKind.NamedType)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                Debug.Assert(this.IsDefinition);

                if (this.ContainingSymbol.Kind == SymbolKind.NamedType)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
        {
            get { return null; }
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
        {
            get { return null; }
        }

        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
            //We've not yet discovered a scenario in which we need this.
            //If you're hitting this exception, uncomment the code below
            //and add a unit test.
#if false
            Debug.Assert(this.IsDefinition);

            SymbolKind kind = this.ContainingSymbol.Kind;

            if (((Module)visitor.Context).SourceModule == this.ContainingModule)
            {
                if (kind == SymbolKind.NamedType)
                {
                    visitor.Visit((IGenericTypeParameter)this);
                }
                else if (kind == SymbolKind.Method)
                {
                    visitor.Visit((IGenericMethodParameter)this);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                if (kind == SymbolKind.NamedType)
                {
                    visitor.Visit((IGenericTypeParameterReference)this);
                }
                else if (kind == SymbolKind.Method)
                {
                    visitor.Visit((IGenericMethodParameterReference)this);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
#endif
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            Debug.Assert(this.IsDefinition);
            return null;
        }

        string Cci.INamedEntity.Name
        {
            get { return this.MetadataName; }
        }

        ushort Cci.IParameterListEntry.Index
        {
            get
            {
                return (ushort)this.Ordinal;
            }
        }

        Cci.IMethodReference Cci.IGenericMethodParameterReference.DefiningMethod
        {
            get
            {
                Debug.Assert(this.IsDefinition);
                return (MethodSymbol)this.ContainingSymbol;
            }
        }

        Cci.ITypeReference Cci.IGenericTypeParameterReference.DefiningType
        {
            get
            {
                Debug.Assert(this.IsDefinition);
                return (NamedTypeSymbol)this.ContainingSymbol;
            }
        }

        IEnumerable<Cci.ITypeReference> Cci.IGenericParameter.GetConstraints(EmitContext context)
        {
            var moduleBeingBuilt = (PEModuleBuilder)context.Module;
            var seenValueType = false;
            foreach (var type in this.ConstraintTypesNoUseSiteDiagnostics)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_Object:
                        // Avoid emitting unnecessary object constraint.
                        continue;
                    case SpecialType.System_ValueType:
                        seenValueType = true;
                        break;
                }
                yield return moduleBeingBuilt.Translate(type.TypeSymbol,
                                                        syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                        diagnostics: context.Diagnostics);
            }
            if (this.HasValueTypeConstraint && !seenValueType)
            {
                // Add System.ValueType constraint to comply with Dev11 output
                yield return moduleBeingBuilt.GetSpecialType(SpecialType.System_ValueType,
                                                             syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                             diagnostics: context.Diagnostics);
            }
        }

        bool Cci.IGenericParameter.MustBeReferenceType
        {
            get
            {
                return this.HasReferenceTypeConstraint;
            }
        }

        bool Cci.IGenericParameter.MustBeValueType
        {
            get
            {
                return this.HasValueTypeConstraint;
            }
        }

        bool Cci.IGenericParameter.MustHaveDefaultConstructor
        {
            get
            {
                //  add constructor constraint for value type constrained 
                //  type parameters to comply with Dev11 output
                return this.HasConstructorConstraint || this.HasValueTypeConstraint;
            }
        }

        Cci.TypeParameterVariance Cci.IGenericParameter.Variance
        {
            get
            {
                switch (this.Variance)
                {
                    case VarianceKind.None:
                        return Cci.TypeParameterVariance.NonVariant;
                    case VarianceKind.In:
                        return Cci.TypeParameterVariance.Contravariant;
                    case VarianceKind.Out:
                        return Cci.TypeParameterVariance.Covariant;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.Variance);
                }
            }
        }

        Cci.IMethodDefinition Cci.IGenericMethodParameter.DefiningMethod
        {
            get
            {
                CheckDefinitionInvariant();
                return (MethodSymbol)this.ContainingSymbol;
            }
        }

        Cci.ITypeDefinition Cci.IGenericTypeParameter.DefiningType
        {
            get
            {
                CheckDefinitionInvariant();
                return (NamedTypeSymbol)this.ContainingSymbol;
            }
        }
    }
}
