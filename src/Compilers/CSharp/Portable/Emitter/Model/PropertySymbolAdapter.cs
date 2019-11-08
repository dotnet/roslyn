// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class PropertySymbol :
        IPropertyDefinition
    {
        #region IPropertyDefinition Members

        IEnumerable<IMethodReference> IPropertyDefinition.GetAccessors(EmitContext context)
        {
            CheckDefinitionInvariant();

            MethodSymbol getMethod = this.GetMethod;
            if (getMethod != null && getMethod.ShouldInclude(context))
            {
                yield return getMethod;
            }

            MethodSymbol setMethod = this.SetMethod;
            if (setMethod != null && setMethod.ShouldInclude(context))
            {
                yield return setMethod;
            }

            SourcePropertySymbol sourceProperty = this as SourcePropertySymbol;
            if ((object)sourceProperty != null && sourceProperty.ShouldInclude(context))
            {
                SynthesizedSealedPropertyAccessor synthesizedAccessor = sourceProperty.SynthesizedSealedAccessorOpt;
                if ((object)synthesizedAccessor != null)
                {
                    yield return synthesizedAccessor;
                }
            }
        }

        MetadataConstant IPropertyDefinition.DefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return null;
            }
        }

        IMethodReference IPropertyDefinition.Getter
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol getMethod = this.GetMethod;
                if ((object)getMethod != null || !this.IsSealed)
                {
                    return getMethod;
                }

                return GetSynthesizedSealedAccessor(MethodKind.PropertyGet);
            }
        }

        bool IPropertyDefinition.HasDefaultValue
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool IPropertyDefinition.IsRuntimeSpecial
        {
            get
            {
                CheckDefinitionInvariant();
                return HasRuntimeSpecialName;
            }
        }

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }

        bool IPropertyDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return this.HasSpecialName;
            }
        }

        ImmutableArray<IParameterDefinition> IPropertyDefinition.Parameters
        {
            get
            {
                CheckDefinitionInvariant();
                return StaticCast<IParameterDefinition>.From(this.Parameters);
            }
        }

        IMethodReference IPropertyDefinition.Setter
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol setMethod = this.SetMethod;
                if ((object)setMethod != null || !this.IsSealed)
                {
                    return setMethod;
                }

                return GetSynthesizedSealedAccessor(MethodKind.PropertySet);
            }
        }

        #endregion

        #region ISignature Members

        [Conditional("DEBUG")]
        private void CheckDefinitionInvariantAllowEmbedded()
        {
            // can't be generic instantiation
            Debug.Assert(this.IsDefinition);

            // must be declared in the module we are building
            Debug.Assert(this.ContainingModule is SourceModuleSymbol || this.ContainingAssembly.IsLinked);
        }

        CallingConvention ISignature.CallingConvention
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return this.CallingConvention;
            }
        }

        ushort ISignature.ParameterCount
        {
            get
            {
                CheckDefinitionInvariant();
                return (ushort)this.ParameterCount;
            }
        }

        ImmutableArray<IParameterTypeInformation> ISignature.GetParameters(EmitContext context)
        {
            CheckDefinitionInvariant();
            return StaticCast<IParameterTypeInformation>.From(this.Parameters);
        }

        ImmutableArray<ICustomModifier> ISignature.ReturnValueCustomModifiers
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return this.TypeWithAnnotations.CustomModifiers.As<ICustomModifier>();
            }
        }

        ImmutableArray<ICustomModifier> ISignature.RefCustomModifiers
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return this.RefCustomModifiers.As<ICustomModifier>();
            }
        }

        bool ISignature.ReturnValueIsByRef
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return this.RefKind.IsManagedReference();
            }
        }

        ITypeReference ISignature.GetType(EmitContext context)
        {
            CheckDefinitionInvariantAllowEmbedded();
            return ((PEModuleBuilder)context.Module).Translate(this.Type,
                                                      syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                      diagnostics: context.Diagnostics);
        }

        #endregion

        #region ITypeDefinitionMember Members

        ITypeDefinition ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();
                return this.ContainingType;
            }
        }

        TypeMemberVisibility ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return PEModuleBuilder.MemberVisibility(this);
            }
        }

        #endregion

        #region ITypeMemberReference Members

        ITypeReference ITypeMemberReference.GetContainingType(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this.ContainingType;
        }

        #endregion

        #region IReference Members

        void IReference.Dispatch(MetadataVisitor visitor)
        {
            CheckDefinitionInvariant();
            visitor.Visit((IPropertyDefinition)this);
        }

        IDefinition IReference.AsDefinition(EmitContext context)
        {
            CheckDefinitionInvariant();
            return this;
        }

        #endregion

        #region INamedEntity Members

        string INamedEntity.Name
        {
            get
            {
                CheckDefinitionInvariant();
                return this.MetadataName;
            }
        }

        #endregion

        private IMethodReference GetSynthesizedSealedAccessor(MethodKind targetMethodKind)
        {
            SourcePropertySymbol sourceProperty = this as SourcePropertySymbol;
            if ((object)sourceProperty != null)
            {
                SynthesizedSealedPropertyAccessor synthesized = sourceProperty.SynthesizedSealedAccessorOpt;
                return synthesized is object { MethodKind: targetMethodKind } ? synthesized : null;
            }

            return null;
        }
    }
}
