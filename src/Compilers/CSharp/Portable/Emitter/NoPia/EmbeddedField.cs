// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedField : EmbeddedTypesManager.CommonEmbeddedField
    {
        public EmbeddedField(EmbeddedType containingType, FieldSymbol underlyingField) :
            base(containingType, underlyingField)
        {
        }

        internal override EmbeddedTypesManager TypeManager
        {
            get
            {
                return ContainingType.TypeManager;
            }
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingField.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override MetadataConstant GetCompileTimeValue(EmitContext context)
        {
            return UnderlyingField.GetMetadataConstantValue(context);
        }

        protected override bool IsCompileTimeConstant
        {
            get
            {
                return UnderlyingField.IsMetadataConstant;
            }
        }

        protected override bool IsNotSerialized
        {
            get
            {
                return UnderlyingField.IsNotSerialized;
            }
        }

        protected override bool IsReadOnly
        {
            get
            {
                return UnderlyingField.IsReadOnly;
            }
        }

        protected override bool IsRuntimeSpecial
        {
            get
            {
                return UnderlyingField.HasRuntimeSpecialName;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingField.HasSpecialName;
            }
        }

        protected override bool IsStatic
        {
            get
            {
                return UnderlyingField.IsStatic;
            }
        }

        protected override bool IsMarshalledExplicitly
        {
            get
            {
                return UnderlyingField.IsMarshalledExplicitly;
            }
        }

        protected override Cci.IMarshallingInformation MarshallingInformation
        {
            get
            {
                return UnderlyingField.MarshallingInformation;
            }
        }

        protected override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return UnderlyingField.MarshallingDescriptor;
            }
        }

        protected override int? TypeLayoutOffset
        {
            get
            {
                return UnderlyingField.TypeLayoutOffset;
            }
        }

        protected override Cci.TypeMemberVisibility Visibility
        {
            get
            {
                return PEModuleBuilder.MemberVisibility(UnderlyingField);
            }
        }

        protected override string Name
        {
            get
            {
                return UnderlyingField.MetadataName;
            }
        }
    }
}
