// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    /// <summary>
    /// Represents a type referenced from a deleted member (as distinct from a type that has been deleted). This is also
    /// why it doesn't inherit from <see cref="DeletedSourceDefinition{T}"/>
    /// </summary>
    internal sealed class DeletedSourceTypeDefinition : DeletedSourceDefinition<ITypeDefinition>, ITypeDefinition
    {
        public DeletedSourceTypeDefinition(ITypeDefinition oldDefinition, Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> typesUsedByDeletedMembers)
            : base(oldDefinition, typesUsedByDeletedMembers)
        {
        }

        public override void Dispatch(MetadataVisitor visitor)
            => visitor.Visit(this);

        public ushort Alignment => OldDefinition.Alignment;

        public IEnumerable<IGenericTypeParameter> GenericParameters => OldDefinition.GenericParameters;

        public ushort GenericParameterCount => OldDefinition.GenericParameterCount;

        public bool HasDeclarativeSecurity => OldDefinition.HasDeclarativeSecurity;

        public bool IsAbstract => OldDefinition.IsAbstract;

        public bool IsBeforeFieldInit => OldDefinition.IsBeforeFieldInit;

        public bool IsComObject => OldDefinition.IsComObject;

        public bool IsGeneric => OldDefinition.IsGeneric;

        public bool IsInterface => OldDefinition.IsInterface;

        public bool IsDelegate => OldDefinition.IsDelegate;

        public bool IsRuntimeSpecial => OldDefinition.IsRuntimeSpecial;

        public bool IsSerializable => OldDefinition.IsSerializable;

        public bool IsSpecialName => OldDefinition.IsSpecialName;

        public bool IsWindowsRuntimeImport => OldDefinition.IsWindowsRuntimeImport;

        public bool IsSealed => OldDefinition.IsSealed;

        public LayoutKind Layout => OldDefinition.Layout;

        public IEnumerable<SecurityAttribute> SecurityAttributes
            => throw ExceptionUtilities.Unreachable();

        public uint SizeOf => OldDefinition.SizeOf;

        public CharSet StringFormat => OldDefinition.StringFormat;

        public bool IsEnum => OldDefinition.IsEnum;

        public bool IsValueType => OldDefinition.IsValueType;

        public Cci.PrimitiveTypeCode TypeCode => OldDefinition.TypeCode;

        public TypeDefinitionHandle TypeDef => OldDefinition.TypeDef;

        public IGenericMethodParameterReference? AsGenericMethodParameterReference => OldDefinition.AsGenericMethodParameterReference;

        public IGenericTypeInstanceReference? AsGenericTypeInstanceReference => OldDefinition.AsGenericTypeInstanceReference;

        public IGenericTypeParameterReference? AsGenericTypeParameterReference => OldDefinition.AsGenericTypeParameterReference;

        public INamespaceTypeReference? AsNamespaceTypeReference => OldDefinition.AsNamespaceTypeReference;

        public INestedTypeReference? AsNestedTypeReference => OldDefinition.AsNestedTypeReference;

        public ISpecializedNestedTypeReference? AsSpecializedNestedTypeReference => OldDefinition.AsSpecializedNestedTypeReference;

        public INamespaceTypeDefinition? AsNamespaceTypeDefinition(EmitContext context)
        {
            return OldDefinition.AsNamespaceTypeDefinition(context);
        }

        public INestedTypeDefinition? AsNestedTypeDefinition(EmitContext context)
        {
            return OldDefinition.AsNestedTypeDefinition(context);
        }

        public ITypeDefinition? AsTypeDefinition(EmitContext context)
        {
            return this;
        }

        public ITypeDefinition? GetResolvedType(EmitContext context)
        {
            return OldDefinition.GetResolvedType(context);
        }

        public ITypeReference? GetBaseClass(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<IEventDefinition> GetEvents(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<Cci.MethodImplementation> GetExplicitImplementationOverrides(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<IFieldDefinition> GetFields(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<IMethodDefinition> GetMethods(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<INestedTypeDefinition> GetNestedTypes(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<IPropertyDefinition> GetProperties(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<TypeReferenceWithAttributes> Interfaces(EmitContext context)
            => throw ExceptionUtilities.Unreachable();
    }
}
