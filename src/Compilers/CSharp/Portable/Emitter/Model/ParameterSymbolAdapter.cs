// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
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
        ParameterSymbolAdapter : SymbolAdapter,
#else
        ParameterSymbol :
#endif 
        Cci.IParameterTypeInformation,
        Cci.IParameterDefinition
    {
        bool Cci.IDefinition.IsEncDeleted
            => false;

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedParameterSymbol.TypeWithAnnotations.CustomModifiers);
            }
        }

        bool Cci.IParameterTypeInformation.IsByReference
        {
            get
            {
                return AdaptedParameterSymbol.RefKind != RefKind.None;
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.RefCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(AdaptedParameterSymbol.RefCustomModifiers);
            }
        }

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(AdaptedParameterSymbol.Type,
                                                      syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                      diagnostics: context.Diagnostics);
        }

        ushort Cci.IParameterListEntry.Index
        {
            get
            {
                return (ushort)AdaptedParameterSymbol.Ordinal;
            }
        }

        /// <summary>
        /// Gets constant value to be stored in metadata Constant table.
        /// </summary>
        MetadataConstant Cci.IParameterDefinition.GetDefaultValue(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this.GetMetadataConstantValue(context);
        }

        internal MetadataConstant GetMetadataConstantValue(EmitContext context)
        {
            if (!AdaptedParameterSymbol.HasMetadataConstantValue)
            {
                return null;
            }

            ConstantValue constant = AdaptedParameterSymbol.ExplicitDefaultConstantValue;
            TypeSymbol type;
            if (constant.SpecialType != SpecialType.None)
            {
                // preserve the exact type of the constant for primitive types,
                // e.g. it should be Int16 for [DefaultParameterValue((short)1)]int x
                type = AdaptedParameterSymbol.ContainingAssembly.GetSpecialType(constant.SpecialType);
            }
            else
            {
                // default(struct), enum
                type = AdaptedParameterSymbol.Type;
            }

            return ((PEModuleBuilder)context.Module).CreateConstant(type, constant.Value,
                                                           syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                           diagnostics: context.Diagnostics);
        }

        bool Cci.IParameterDefinition.HasDefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.HasMetadataConstantValue;
            }
        }

        bool Cci.IParameterDefinition.IsOptional
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.IsMetadataOptional;
            }
        }

        bool Cci.IParameterDefinition.IsIn
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.IsMetadataIn;
            }
        }

        bool Cci.IParameterDefinition.IsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.IsMarshalledExplicitly;
            }
        }

        bool Cci.IParameterDefinition.IsOut
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.IsMetadataOut;
            }
        }

        Cci.IMarshallingInformation Cci.IParameterDefinition.MarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.MarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IParameterDefinition.MarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedParameterSymbol.MarshallingDescriptor;
            }
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable();
            //At present we have no scenario that needs this method.
            //Should one arise, uncomment implementation and add a test.
#if false   
            Debug.Assert(this.IsDefinitionOrDistinct());

            if (!this.IsDefinition)
            {
                visitor.Visit((IParameterTypeInformation)this);
            }
            else if (this.ContainingModule == ((Module)visitor.Context).SourceModule)
            {
                visitor.Visit((IParameterDefinition)this);
            }
            else
            {
                visitor.Visit((IParameterTypeInformation)this);
            }
#endif
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            Debug.Assert(this.IsDefinitionOrDistinct());

            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            if (AdaptedParameterSymbol.IsDefinition &&
                AdaptedParameterSymbol.ContainingModule == moduleBeingBuilt.SourceModule)
            {
                return this;
            }

            return null;
        }

        string Cci.INamedEntity.Name
        {
            get { return AdaptedParameterSymbol.MetadataName; }
        }
    }

    internal partial class ParameterSymbol
    {
#if DEBUG
        private ParameterSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new ParameterSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new ParameterSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal ParameterSymbol AdaptedParameterSymbol => this;

        internal new ParameterSymbol GetCciAdapter()
        {
            return this;
        }
#endif

        internal virtual bool HasMetadataConstantValue
        {
            get
            {
                CheckDefinitionInvariant();
                // For a decimal value, DefaultValue won't be used directly, instead, DecimalConstantAttribute will be generated.
                // Similarly for DateTime. (C# does not directly support optional parameters with DateTime constants, but honors
                // the attributes if [Optional][DateTimeConstant(whatever)] are on the parameter.)
                return this.ExplicitDefaultConstantValue != null &&
                       this.ExplicitDefaultConstantValue.SpecialType != SpecialType.System_Decimal &&
                       this.ExplicitDefaultConstantValue.SpecialType != SpecialType.System_DateTime;
            }
        }

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
    internal partial class ParameterSymbolAdapter
    {
        internal ParameterSymbolAdapter(ParameterSymbol underlyingParameterSymbol)
        {
            AdaptedParameterSymbol = underlyingParameterSymbol;

            if (underlyingParameterSymbol is NativeIntegerParameterSymbol)
            {
                // Emit should use underlying symbol only.
                throw ExceptionUtilities.Unreachable();
            }
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedParameterSymbol;
        internal ParameterSymbol AdaptedParameterSymbol { get; }
    }
#endif
}
