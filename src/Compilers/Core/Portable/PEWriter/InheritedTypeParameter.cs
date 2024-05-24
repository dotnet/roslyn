// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal class InheritedTypeParameter : IGenericTypeParameter
    {
        private readonly ushort _index;
        private readonly ITypeDefinition _inheritingType;
        private readonly IGenericTypeParameter _parentParameter;

        internal InheritedTypeParameter(ushort index, ITypeDefinition inheritingType, IGenericTypeParameter parentParameter)
        {
            _index = index;
            _inheritingType = inheritingType;
            _parentParameter = parentParameter;
        }

        #region IGenericTypeParameter Members

        public ITypeDefinition DefiningType
        {
            get { return _inheritingType; }
        }

        #endregion

        #region IGenericParameter Members

        public IEnumerable<TypeReferenceWithAttributes> GetConstraints(EmitContext context)
        {
            return _parentParameter.GetConstraints(context);
        }

        public bool MustBeReferenceType
        {
            get { return _parentParameter.MustBeReferenceType; }
        }

        public bool MustBeValueType
        {
            get { return _parentParameter.MustBeValueType; }
        }

        public bool AllowsRefLikeType
        {
            get { return _parentParameter.AllowsRefLikeType; }
        }

        public bool MustHaveDefaultConstructor
        {
            get { return _parentParameter.MustHaveDefaultConstructor; }
        }

        public TypeParameterVariance Variance
        {
            get { return _inheritingType.IsInterface || _inheritingType.IsDelegate ? _parentParameter.Variance : TypeParameterVariance.NonVariant; }
        }

        #endregion

        #region ITypeDefinition Members

        public bool IsEncDeleted
            => false;

        public ushort Alignment
        {
            get { return 0; }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public bool IsEnum
        {
            get { return false; }
        }

        public IArrayTypeReference? AsArrayTypeReference
        {
            get
            {
                return this as IArrayTypeReference;
            }
        }

        public IGenericMethodParameter? AsGenericMethodParameter
        {
            get
            {
                return this as IGenericMethodParameter;
            }
        }

        public IGenericMethodParameterReference? AsGenericMethodParameterReference
        {
            get
            {
                return this as IGenericMethodParameterReference;
            }
        }

        public IGenericTypeInstanceReference? AsGenericTypeInstanceReference
        {
            get
            {
                return this as IGenericTypeInstanceReference;
            }
        }

        public IGenericTypeParameter? AsGenericTypeParameter
        {
            get
            {
                return this as IGenericTypeParameter;
            }
        }

        public IGenericTypeParameterReference? AsGenericTypeParameterReference
        {
            get
            {
                return this as IGenericTypeParameterReference;
            }
        }

        public INamespaceTypeDefinition? AsNamespaceTypeDefinition(EmitContext context)
        {
            return this as INamespaceTypeDefinition;
        }

        public INamespaceTypeReference? AsNamespaceTypeReference
        {
            get
            {
                return this as INamespaceTypeReference;
            }
        }

        public INestedTypeDefinition? AsNestedTypeDefinition(EmitContext context)
        {
            return this as INestedTypeDefinition;
        }

        public INestedTypeReference? AsNestedTypeReference
        {
            get
            {
                return this as INestedTypeReference;
            }
        }

        public ISpecializedNestedTypeReference? AsSpecializedNestedTypeReference
        {
            get
            {
                return this as ISpecializedNestedTypeReference;
            }
        }

        public IModifiedTypeReference? AsModifiedTypeReference
        {
            get
            {
                return this as IModifiedTypeReference;
            }
        }

        public IPointerTypeReference? AsPointerTypeReference
        {
            get
            {
                return this as IPointerTypeReference;
            }
        }

        public ITypeDefinition? AsTypeDefinition(EmitContext context)
        {
            return this as ITypeDefinition;
        }

        public IDefinition? AsDefinition(EmitContext context)
        {
            return this as IDefinition;
        }

        #endregion

        #region IReference Members

        CodeAnalysis.Symbols.ISymbolInternal? Cci.IReference.GetInternalSymbol() => null;

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _parentParameter.GetAttributes(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
        }

        #endregion

        #region ITypeReference Members

        public TypeDefinitionHandle TypeDef
        {
            get
            {
                return default(TypeDefinitionHandle);
            }
        }

        public bool IsAlias
        {
            get { return false; }
        }

        public bool IsValueType
        {
            get { return false; }
        }

        public ITypeDefinition GetResolvedType(EmitContext context)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public PrimitiveTypeCode TypeCode
        {
            get { return PrimitiveTypeCode.NotPrimitive; }
        }

        #endregion

        #region IParameterListEntry Members

        public ushort Index
        {
            get { return _index; }
        }

        #endregion

        #region INamedEntity Members

        public string? Name
        {
            get { return _parentParameter.Name; }
        }

        #endregion

        #region IGenericTypeParameterReference Members

        ITypeReference IGenericTypeParameterReference.DefiningType
        {
            get { return _inheritingType; }
        }

        #endregion

        #region INamedTypeReference Members

        public bool MangleName
        {
            get { return false; }
        }

        #endregion

        public bool IsNested
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public bool IsSpecializedNested
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public ITypeReference UnspecializedVersion
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public bool IsNamespaceTypeReference
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public bool IsGenericTypeInstance
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }
    }
}
