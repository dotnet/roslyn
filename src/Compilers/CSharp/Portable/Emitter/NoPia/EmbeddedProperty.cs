// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedProperty : EmbeddedTypesManager.CommonEmbeddedProperty
    {
        public EmbeddedProperty(PropertySymbol underlyingProperty, EmbeddedMethod getter, EmbeddedMethod setter) :
            base(underlyingProperty, getter, setter)
        {
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingProperty.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override ImmutableArray<EmbeddedParameter> GetParameters()
        {
            return EmbeddedTypesManager.EmbedParameters(this, UnderlyingProperty.Parameters);
        }

        protected override bool IsRuntimeSpecial
        {
            get { return UnderlyingProperty.HasRuntimeSpecialName; }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingProperty.HasSpecialName;
            }
        }

        protected override Cci.ISignature UnderlyingPropertySignature
        {
            get
            {
                return (Cci.ISignature)UnderlyingProperty;
            }
        }

        protected override EmbeddedType ContainingType
        {
            get { return AnAccessor.ContainingType; }
        }

        protected override Cci.TypeMemberVisibility Visibility
        {
            get
            {
                return PEModuleBuilder.MemberVisibility(UnderlyingProperty);
            }
        }

        protected override string Name
        {
            get
            {
                return UnderlyingProperty.MetadataName;
            }
        }
    }
}
