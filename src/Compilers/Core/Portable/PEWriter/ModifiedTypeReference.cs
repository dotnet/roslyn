// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        private readonly ITypeReference _modifiedType;
        private readonly ImmutableArray<ICustomModifier> _customModifiers;

        public ModifiedTypeReference(ITypeReference modifiedType, ImmutableArray<ICustomModifier> customModifiers)
        {
            RoslynDebug.Assert(modifiedType != null);
            Debug.Assert(!customModifiers.IsDefault);

            _modifiedType = modifiedType;
            _customModifiers = customModifiers;
        }

        ImmutableArray<ICustomModifier> IModifiedTypeReference.CustomModifiers
        {
            get
            {
                // TODO: Should we thread this through Module.Translate? For example, can we run into Pia type here? 
                return _customModifiers;
            }
        }

        ITypeReference IModifiedTypeReference.UnmodifiedType
        {
            get
            {
                return _modifiedType;
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

        PrimitiveTypeCode ITypeReference.TypeCode
        {
            get { return PrimitiveTypeCode.NotPrimitive; }
        }

        TypeDefinitionHandle ITypeReference.TypeDef
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

        IGenericMethodParameterReference? ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        IGenericTypeInstanceReference? ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return null;
            }
        }

        IGenericTypeParameterReference? ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        INamespaceTypeDefinition? ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        INamespaceTypeReference? ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return null;
            }
        }

        INestedTypeDefinition? ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        INestedTypeReference? ITypeReference.AsNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        ISpecializedNestedTypeReference? ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        ITypeDefinition? ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        IDefinition? IReference.AsDefinition(EmitContext context)
        {
            return null;
        }
    }
}
