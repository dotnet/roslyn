// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        TypeParameterSymbolAdapter : SymbolAdapter,
#else
        TypeParameterSymbol :
#endif 
        Cci.IGenericParameterReference,
        Cci.IGenericMethodParameterReference,
        Cci.IGenericTypeParameterReference,
        Cci.IGenericParameter,
        Cci.IGenericMethodParameter,
        Cci.IGenericTypeParameter
    {
        public bool IsEncDeleted
            => false;

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

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
        {
            get { return Cci.PrimitiveTypeCode.NotPrimitive; }
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

                if (AdaptedTypeParameterSymbol.ContainingSymbol.Kind == SymbolKind.Method)
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
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition);

                if (AdaptedTypeParameterSymbol.ContainingSymbol.Kind == SymbolKind.Method)
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

                if (AdaptedTypeParameterSymbol.ContainingSymbol.Kind == SymbolKind.NamedType)
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
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition);

                if (AdaptedTypeParameterSymbol.ContainingSymbol.Kind == SymbolKind.NamedType)
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
            throw ExceptionUtilities.Unreachable();
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
            Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition);
            return null;
        }

        string Cci.INamedEntity.Name
        {
            get { return AdaptedTypeParameterSymbol.MetadataName; }
        }

        ushort Cci.IParameterListEntry.Index
        {
            get
            {
                return (ushort)AdaptedTypeParameterSymbol.Ordinal;
            }
        }

        Cci.IMethodReference Cci.IGenericMethodParameterReference.DefiningMethod
        {
            get
            {
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition);
                return ((MethodSymbol)AdaptedTypeParameterSymbol.ContainingSymbol).GetCciAdapter();
            }
        }

        Cci.ITypeReference Cci.IGenericTypeParameterReference.DefiningType
        {
            get
            {
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition);
                return ((NamedTypeSymbol)AdaptedTypeParameterSymbol.ContainingSymbol).GetCciAdapter();
            }
        }

        IEnumerable<Cci.TypeReferenceWithAttributes> Cci.IGenericParameter.GetConstraints(EmitContext context)
        {
            var moduleBeingBuilt = (PEModuleBuilder)context.Module;

            var seenValueType = false;
            if (AdaptedTypeParameterSymbol.HasUnmanagedTypeConstraint)
            {
                var typeRef = moduleBeingBuilt.GetSpecialType(
                    SpecialType.System_ValueType,
                    syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                    diagnostics: context.Diagnostics);

                var modifier = CSharpCustomModifier.CreateRequired(
                    moduleBeingBuilt.Compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_UnmanagedType));

                // emit "(class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType" pattern as "unmanaged"
                yield return new Cci.TypeReferenceWithAttributes(new Cci.ModifiedTypeReference(typeRef, ImmutableArray.Create<Cci.ICustomModifier>(modifier)));

                // do not emit another one for Dev11 similarities
                seenValueType = true;
            }

            foreach (var type in AdaptedTypeParameterSymbol.ConstraintTypesNoUseSiteDiagnostics)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_Object:
                        Debug.Assert(!type.NullableAnnotation.IsAnnotated());
                        break;
                    case SpecialType.System_ValueType:
                        seenValueType = true;
                        break;
                }
                var typeRef = moduleBeingBuilt.Translate(type.Type,
                                                            syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                            diagnostics: context.Diagnostics);

                yield return type.GetTypeRefWithAttributes(
                                                            moduleBeingBuilt,
                                                            declaringSymbol: AdaptedTypeParameterSymbol,
                                                            typeRef);
            }

            if (AdaptedTypeParameterSymbol.HasValueTypeConstraint && !seenValueType)
            {
                // Add System.ValueType constraint to comply with Dev11 output
                var typeRef = moduleBeingBuilt.GetSpecialType(SpecialType.System_ValueType,
                                                                syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                                diagnostics: context.Diagnostics);

                yield return new Cci.TypeReferenceWithAttributes(typeRef);
            }
        }

        bool Cci.IGenericParameter.MustBeReferenceType
        {
            get
            {
                return AdaptedTypeParameterSymbol.HasReferenceTypeConstraint;
            }
        }

        bool Cci.IGenericParameter.MustBeValueType
        {
            get
            {
                return AdaptedTypeParameterSymbol.HasValueTypeConstraint || AdaptedTypeParameterSymbol.HasUnmanagedTypeConstraint;
            }
        }

        bool Cci.IGenericParameter.MustHaveDefaultConstructor
        {
            get
            {
                //  add constructor constraint for value type constrained 
                //  type parameters to comply with Dev11 output
                //  do this for "unmanaged" constraint too
                return AdaptedTypeParameterSymbol.HasConstructorConstraint || AdaptedTypeParameterSymbol.HasValueTypeConstraint || AdaptedTypeParameterSymbol.HasUnmanagedTypeConstraint;
            }
        }

        Cci.TypeParameterVariance Cci.IGenericParameter.Variance
        {
            get
            {
                switch (AdaptedTypeParameterSymbol.Variance)
                {
                    case VarianceKind.None:
                        return Cci.TypeParameterVariance.NonVariant;
                    case VarianceKind.In:
                        return Cci.TypeParameterVariance.Contravariant;
                    case VarianceKind.Out:
                        return Cci.TypeParameterVariance.Covariant;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(AdaptedTypeParameterSymbol.Variance);
                }
            }
        }

        Cci.IMethodDefinition Cci.IGenericMethodParameter.DefiningMethod
        {
            get
            {
                CheckDefinitionInvariant();
                return ((MethodSymbol)AdaptedTypeParameterSymbol.ContainingSymbol).GetCciAdapter();
            }
        }

        Cci.ITypeDefinition Cci.IGenericTypeParameter.DefiningType
        {
            get
            {
                CheckDefinitionInvariant();
                return ((NamedTypeSymbol)AdaptedTypeParameterSymbol.ContainingSymbol).GetCciAdapter();
            }
        }
    }

    internal partial class TypeParameterSymbol
    {
#if DEBUG
        private TypeParameterSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new TypeParameterSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new TypeParameterSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal TypeParameterSymbol AdaptedTypeParameterSymbol => this;

        internal new TypeParameterSymbol GetCciAdapter()
        {
            return this;
        }
#endif
    }

#if DEBUG
    internal partial class TypeParameterSymbolAdapter
    {
        internal TypeParameterSymbolAdapter(TypeParameterSymbol underlyingTypeParameterSymbol)
        {
            AdaptedTypeParameterSymbol = underlyingTypeParameterSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedTypeParameterSymbol;
        internal TypeParameterSymbol AdaptedTypeParameterSymbol { get; }
    }
#endif
}
