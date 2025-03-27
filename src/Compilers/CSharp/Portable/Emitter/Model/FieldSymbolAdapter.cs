// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        FieldSymbolAdapter : SymbolAdapter,
#else
        FieldSymbol :
#endif 
        Cci.IFieldReference,
        Cci.IFieldDefinition,
        Cci.ITypeMemberReference,
        Cci.ITypeDefinitionMember,
        Cci.ISpecializedFieldReference
    {
        public bool IsEncDeleted
            => false;

        Cci.ITypeReference Cci.IFieldReference.GetType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            TypeWithAnnotations fieldTypeWithAnnotations = AdaptedFieldSymbol.TypeWithAnnotations;
            var customModifiers = fieldTypeWithAnnotations.CustomModifiers;
            var isFixed = AdaptedFieldSymbol.IsFixedSizeBuffer;
            var implType = isFixed ? AdaptedFieldSymbol.FixedImplementationType(moduleBeingBuilt) : fieldTypeWithAnnotations.Type;
            var type = moduleBeingBuilt.Translate(implType,
                                                  syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                  diagnostics: context.Diagnostics);

            if (isFixed || customModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, ImmutableArray<Cci.ICustomModifier>.CastUp(customModifiers));
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IFieldReference.RefCustomModifiers =>
            ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedFieldSymbol.RefCustomModifiers);

        bool Cci.IFieldReference.IsByReference => AdaptedFieldSymbol.RefKind != RefKind.None;

        Cci.IFieldDefinition Cci.IFieldReference.GetResolvedField(EmitContext context)
        {
            return ResolvedFieldImpl((PEModuleBuilder)context.Module);
        }

        private Cci.IFieldDefinition ResolvedFieldImpl(PEModuleBuilder moduleBeingBuilt)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (AdaptedFieldSymbol.IsDefinition &&
                AdaptedFieldSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return this;
            }

            return null;
        }

        Cci.ISpecializedFieldReference Cci.IFieldReference.AsSpecializedFieldReference
        {
            get
            {
                Debug.Assert(this.IsDefinitionOrDistinct());

                if (!AdaptedFieldSymbol.IsDefinition)
                {
                    return this;
                }

                return null;
            }
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            Debug.Assert(this.IsDefinitionOrDistinct());

            return moduleBeingBuilt.Translate(AdaptedFieldSymbol.ContainingType,
                                              syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                              diagnostics: context.Diagnostics,
                                              needDeclaration: AdaptedFieldSymbol.IsDefinition);
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!AdaptedFieldSymbol.IsDefinition)
            {
                visitor.Visit((Cci.ISpecializedFieldReference)this);
            }
            else if (AdaptedFieldSymbol.ContainingModule == ((PEModuleBuilder)visitor.Context.Module).SourceModule)
            {
                visitor.Visit((Cci.IFieldDefinition)this);
            }
            else
            {
                visitor.Visit((Cci.IFieldReference)this);
            }
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            return ResolvedFieldImpl(moduleBeingBuilt);
        }

        string Cci.INamedEntity.Name
        {
            get
            {
                return AdaptedFieldSymbol.MetadataName;
            }
        }

        bool Cci.IFieldReference.IsContextualNamedEntity
        {
            get
            {
                return false;
            }
        }

        MetadataConstant Cci.IFieldDefinition.GetCompileTimeValue(EmitContext context)
        {
            CheckDefinitionInvariant();

            return GetMetadataConstantValue(context);
        }

        internal MetadataConstant GetMetadataConstantValue(EmitContext context)
        {
            // A constant field of type decimal is not treated as a compile time value in CLR,
            // so check if it is a metadata constant, not just a constant to exclude decimals.
            if (AdaptedFieldSymbol.IsMetadataConstant)
            {
                // NOTE: We would like to be able to assert that the constant value of this field
                // is not bad (i.e. ConstantValue.Bad) if it is being consumed by CCI, but we can't
                // because this method is called by the ReferenceIndexer in the metadata-only case
                // (and we specifically don't want to prevent metadata-only emit because of a bad
                // constant).  If the constant value is bad, we'll end up exposing null to CCI.
                return ((PEModuleBuilder)context.Module).CreateConstant(AdaptedFieldSymbol.Type, AdaptedFieldSymbol.ConstantValue,
                                                               syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                               diagnostics: context.Diagnostics);
            }

            return null;
        }

        ImmutableArray<byte> Cci.IFieldDefinition.MappedData
        {
            get
            {
                CheckDefinitionInvariant();
                return default(ImmutableArray<byte>);
            }
        }

        bool Cci.IFieldDefinition.IsCompileTimeConstant
        {
            get
            {
                CheckDefinitionInvariant();
                // A constant field of type decimal is not treated as a compile time value in CLR,
                // so check if it is a metadata constant, not just a constant to exclude decimals.
                return AdaptedFieldSymbol.IsMetadataConstant;
            }
        }

        bool Cci.IFieldDefinition.IsNotSerialized
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.IsNotSerialized;
            }
        }

        bool Cci.IFieldDefinition.IsReadOnly
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.IsReadOnly || (AdaptedFieldSymbol.IsConst && !AdaptedFieldSymbol.IsMetadataConstant);
            }
        }

        bool Cci.IFieldDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.HasRuntimeSpecialName;
            }
        }

        bool Cci.IFieldDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.HasSpecialName;
            }
        }

        bool Cci.IFieldDefinition.IsStatic
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.IsStatic;
            }
        }

        bool Cci.IFieldDefinition.IsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.IsMarshalledExplicitly;
            }
        }

        Cci.IMarshallingInformation Cci.IFieldDefinition.MarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.MarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IFieldDefinition.MarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.MarshallingDescriptor;
            }
        }

        int Cci.IFieldDefinition.Offset
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.TypeLayoutOffset ?? 0;
            }
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.ContainingType.GetCciAdapter();
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedFieldSymbol.MetadataVisibility;
            }
        }

        Cci.IFieldReference Cci.ISpecializedFieldReference.UnspecializedVersion
        {
            get
            {
                Debug.Assert(!AdaptedFieldSymbol.IsDefinition);
                return AdaptedFieldSymbol.OriginalDefinition.GetCciAdapter();
            }
        }
    }

    internal partial class FieldSymbol
    {
#if DEBUG
        private FieldSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new FieldSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new FieldSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal FieldSymbol AdaptedFieldSymbol => this;

        internal new FieldSymbol GetCciAdapter()
        {
            return this;
        }
#endif 

        internal virtual bool IsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MarshallingInformation != null;
            }
        }

        internal virtual ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return default(ImmutableArray<byte>);
            }
        }
    }

#if DEBUG
    internal partial class FieldSymbolAdapter
    {
        internal FieldSymbolAdapter(FieldSymbol underlyingFieldSymbol)
        {
            AdaptedFieldSymbol = underlyingFieldSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedFieldSymbol;
        internal FieldSymbol AdaptedFieldSymbol { get; }
    }
#endif
}
