// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return UnderlyingProperty.GetCustomAttributesToEmit(compilationState);
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

        protected override Cci.CallingConvention CallingConvention
        {
            get
            {
                return UnderlyingProperty.CallingConvention;
            }
        }

        protected override bool ReturnValueIsModified
        {
            get
            {
                return UnderlyingProperty.TypeCustomModifiers.Length != 0;
            }
        }

        protected override ImmutableArray<Cci.ICustomModifier> ReturnValueCustomModifiers
        {
            get
            {
                return UnderlyingProperty.TypeCustomModifiers.As<Cci.ICustomModifier>();
            }
        }

        protected override bool ReturnValueIsByRef
        {
            get
            {
                return UnderlyingProperty.RefKind == RefKind.Ref;
            }
        }

        protected override Cci.ITypeReference GetType(PEModuleBuilder moduleBuilder, CSharpSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return moduleBuilder.Translate(UnderlyingProperty.Type, syntaxNodeOpt, diagnostics);
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
