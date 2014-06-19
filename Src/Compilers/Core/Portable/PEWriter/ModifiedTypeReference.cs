// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal sealed class ModifiedTypeReference : IModifiedTypeReference
    {
        private readonly ITypeReference modifiedType;
        private readonly ImmutableArray<ICustomModifier> customModifiers;

        public ModifiedTypeReference(ITypeReference modifiedType, ImmutableArray<ICustomModifier> customModifiers)
        {
            Debug.Assert(modifiedType != null);
            Debug.Assert(!customModifiers.IsDefault);

            this.modifiedType = modifiedType;
            this.customModifiers = customModifiers;
        }

        ImmutableArray<ICustomModifier> IModifiedTypeReference.CustomModifiers
        {
            get
            {
                // TODO: Should we thread this through Module.Translate? For example, can we run into Pia type here? 
                return customModifiers;
            }
        }

        ITypeReference IModifiedTypeReference.UnmodifiedType
        {
            get
            {
                return modifiedType;
            }
        }

        bool ITypeReference.IsEnum
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        bool ITypeReference.IsValueType
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        ITypeDefinition ITypeReference.GetResolvedType(EmitContext context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        PrimitiveTypeCode ITypeReference.TypeCode(EmitContext context)
        {
            return PrimitiveTypeCode.NotPrimitive;
        }

        TypeHandle ITypeReference.TypeDef
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        }

        void IReference.Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit((IModifiedTypeReference)this);
        }

        IGenericMethodParameterReference ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        IGenericTypeInstanceReference ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return null;
            }
        }

        IGenericTypeParameterReference ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        INamespaceTypeDefinition ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        INamespaceTypeReference ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return null;
            }
        }

        INestedTypeDefinition ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        INestedTypeReference ITypeReference.AsNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        ISpecializedNestedTypeReference ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        ITypeDefinition ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        IDefinition IReference.AsDefinition(EmitContext context)
        {
            return null;
        }
    }
}
