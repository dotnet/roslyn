// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class ParameterSymbol :
        Cci.IParameterTypeInformation,
        Cci.IParameterDefinition
    {
        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
        {
            get
            {
                return this.TypeWithAnnotations.CustomModifiers.As<Cci.ICustomModifier>();
            }
        }

        bool Cci.IParameterTypeInformation.IsByReference
        {
            get
            {
                return this.RefKind != RefKind.None;
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.RefCustomModifiers
        {
            get
            {
                return this.RefCustomModifiers.As<Cci.ICustomModifier>();
            }
        }

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(this.Type,
                                                      syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                      diagnostics: context.Diagnostics);
        }

        ushort Cci.IParameterListEntry.Index
        {
            get
            {
                return (ushort)this.Ordinal;
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
            if (!HasMetadataConstantValue)
            {
                return null;
            }

            ConstantValue constant = this.ExplicitDefaultConstantValue;
            TypeSymbol type;
            if (constant.SpecialType != SpecialType.None)
            {
                // preserve the exact type of the constant for primitive types,
                // e.g. it should be Int16 for [DefaultParameterValue((short)1)]int x
                type = this.ContainingAssembly.GetSpecialType(constant.SpecialType);
            }
            else
            {
                // default(struct), enum
                type = this.Type;
            }

            return ((PEModuleBuilder)context.Module).CreateConstant(type, constant.Value,
                                                           syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                           diagnostics: context.Diagnostics);
        }

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

        bool Cci.IParameterDefinition.HasDefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return HasMetadataConstantValue;
            }
        }

        bool Cci.IParameterDefinition.IsOptional
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMetadataOptional;
            }
        }

        bool Cci.IParameterDefinition.IsIn
        {
            get
            {
                CheckDefinitionInvariant();
                return IsMetadataIn;
            }
        }

        bool Cci.IParameterDefinition.IsMarshalledExplicitly
        {
            get
            {
                CheckDefinitionInvariant();
                return this.IsMarshalledExplicitly;
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

        bool Cci.IParameterDefinition.IsOut
        {
            get
            {
                CheckDefinitionInvariant();
                return IsMetadataOut;
            }
        }

        Cci.IMarshallingInformation Cci.IParameterDefinition.MarshallingInformation
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MarshallingInformation;
            }
        }

        ImmutableArray<byte> Cci.IParameterDefinition.MarshallingDescriptor
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MarshallingDescriptor;
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

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
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

            if (this is { IsDefinition: true, ContainingModule: moduleBeingBuilt.SourceModule })
            {
                return this;
            }

            return null;
        }

        string Cci.INamedEntity.Name
        {
            get { return this.MetadataName; }
        }
    }
}
