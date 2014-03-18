// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class ModifiedTypeReference : IModifiedTypeReference
    {
        private readonly ITypeReference modifiedType;
        private readonly IEnumerable<ICustomModifier> customModifiers;

        public ModifiedTypeReference(ITypeReference modifiedType, IEnumerable<ICustomModifier> customModifiers)
        {
            Debug.Assert(modifiedType != null);
            Debug.Assert(customModifiers != null);

            this.modifiedType = modifiedType;
            this.customModifiers = customModifiers;
        }

        IEnumerable<ICustomModifier> IModifiedTypeReference.CustomModifiers
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

        ITypeDefinition ITypeReference.GetResolvedType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        PrimitiveTypeCode ITypeReference.TypeCode(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return PrimitiveTypeCode.NotPrimitive;
        }

        TypeHandle ITypeReference.TypeDef
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        IEnumerable<ICustomAttribute> IReference.GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
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

        INamespaceTypeDefinition ITypeReference.AsNamespaceTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
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

        INestedTypeDefinition ITypeReference.AsNestedTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
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

        ITypeDefinition ITypeReference.AsTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        IDefinition IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }
    }
}
