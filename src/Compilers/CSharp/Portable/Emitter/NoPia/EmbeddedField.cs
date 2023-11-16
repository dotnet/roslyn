// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGen;

#if !DEBUG
using FieldSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.FieldSymbol;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedField : EmbeddedTypesManager.CommonEmbeddedField
    {
        public EmbeddedField(EmbeddedType containingType, FieldSymbolAdapter underlyingField) :
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
            return UnderlyingField.AdaptedFieldSymbol.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override MetadataConstant GetCompileTimeValue(EmitContext context)
        {
            return UnderlyingField.GetMetadataConstantValue(context);
        }

        protected override bool IsCompileTimeConstant
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.IsMetadataConstant;
            }
        }

        protected override bool IsNotSerialized
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.IsNotSerialized;
            }
        }

        protected override bool IsReadOnly
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.IsReadOnly;
            }
        }

        protected override bool IsRuntimeSpecial
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.HasRuntimeSpecialName;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.HasSpecialName;
            }
        }

        protected override bool IsStatic
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.IsStatic;
            }
        }

        protected override bool IsMarshalledExplicitly
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.IsMarshalledExplicitly;
            }
        }

        protected override Cci.IMarshallingInformation MarshallingInformation
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.MarshallingInformation;
            }
        }

        protected override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.MarshallingDescriptor;
            }
        }

        protected override int? TypeLayoutOffset
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.TypeLayoutOffset;
            }
        }

        protected override Cci.TypeMemberVisibility Visibility
            => UnderlyingField.AdaptedFieldSymbol.MetadataVisibility;

        protected override string Name
        {
            get
            {
                return UnderlyingField.AdaptedFieldSymbol.MetadataName;
            }
        }
    }
}
