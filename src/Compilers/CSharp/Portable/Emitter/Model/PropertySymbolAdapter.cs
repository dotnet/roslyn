// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        PropertySymbolAdapter : SymbolAdapter,
#else
        PropertySymbol :
#endif 
        IPropertyDefinition
    {
        public bool IsEncDeleted
            => false;

        #region IPropertyDefinition Members

        IEnumerable<IMethodReference> IPropertyDefinition.GetAccessors(EmitContext context)
        {
            CheckDefinitionInvariant();

            var getMethod = AdaptedPropertySymbol.GetMethod?.GetCciAdapter();
            if (getMethod != null && getMethod.ShouldInclude(context))
            {
                yield return getMethod;
            }

            var setMethod = AdaptedPropertySymbol.SetMethod?.GetCciAdapter();
            if (setMethod != null && setMethod.ShouldInclude(context))
            {
                yield return setMethod;
            }

            var sourceProperty = AdaptedPropertySymbol as SourcePropertySymbolBase;
            if ((object)sourceProperty != null && this.ShouldInclude(context))
            {
                SynthesizedSealedPropertyAccessor synthesizedAccessor = sourceProperty.SynthesizedSealedAccessorOpt;
                if ((object)synthesizedAccessor != null)
                {
                    yield return synthesizedAccessor.GetCciAdapter();
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
                MethodSymbol getMethod = AdaptedPropertySymbol.GetMethod;
                if ((object)getMethod != null || !AdaptedPropertySymbol.IsSealed)
                {
                    return getMethod?.GetCciAdapter();
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
                return AdaptedPropertySymbol.HasRuntimeSpecialName;
            }
        }

        bool IPropertyDefinition.IsSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedPropertySymbol.HasSpecialName;
            }
        }

        ImmutableArray<IParameterDefinition> IPropertyDefinition.Parameters
        {
            get
            {
                CheckDefinitionInvariant();
#if DEBUG
                return AdaptedPropertySymbol.Parameters.SelectAsArray<ParameterSymbol, IParameterDefinition>(p => p.GetCciAdapter());
#else
                return StaticCast<IParameterDefinition>.From(AdaptedPropertySymbol.Parameters);
#endif
            }
        }

        IMethodReference IPropertyDefinition.Setter
        {
            get
            {
                CheckDefinitionInvariant();
                MethodSymbol setMethod = AdaptedPropertySymbol.SetMethod;
                if ((object)setMethod != null || !AdaptedPropertySymbol.IsSealed)
                {
                    return setMethod?.GetCciAdapter();
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
            Debug.Assert(AdaptedPropertySymbol.IsDefinition);

            // must be declared in the module we are building
            Debug.Assert(AdaptedPropertySymbol.ContainingModule is SourceModuleSymbol || AdaptedPropertySymbol.ContainingAssembly.IsLinked);
        }

        CallingConvention ISignature.CallingConvention
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return AdaptedPropertySymbol.CallingConvention;
            }
        }

        ushort ISignature.ParameterCount
        {
            get
            {
                CheckDefinitionInvariant();
                return (ushort)AdaptedPropertySymbol.ParameterCount;
            }
        }

        ImmutableArray<IParameterTypeInformation> ISignature.GetParameters(EmitContext context)
        {
            CheckDefinitionInvariant();
#if DEBUG
            return AdaptedPropertySymbol.Parameters.SelectAsArray<ParameterSymbol, IParameterTypeInformation>(p => p.GetCciAdapter());
#else
            return StaticCast<IParameterTypeInformation>.From(AdaptedPropertySymbol.Parameters);
#endif
        }

        ImmutableArray<ICustomModifier> ISignature.ReturnValueCustomModifiers
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return AdaptedPropertySymbol.TypeWithAnnotations.CustomModifiers.As<ICustomModifier>();
            }
        }

        ImmutableArray<ICustomModifier> ISignature.RefCustomModifiers
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return AdaptedPropertySymbol.RefCustomModifiers.As<ICustomModifier>();
            }
        }

        bool ISignature.ReturnValueIsByRef
        {
            get
            {
                CheckDefinitionInvariantAllowEmbedded();
                return AdaptedPropertySymbol.RefKind.IsManagedReference();
            }
        }

        ITypeReference ISignature.GetType(EmitContext context)
        {
            CheckDefinitionInvariantAllowEmbedded();
            return ((PEModuleBuilder)context.Module).Translate(AdaptedPropertySymbol.Type,
                                                      syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNode,
                                                      diagnostics: context.Diagnostics,
                                                      eraseExtensions: true);
        }

        #endregion

        #region ITypeDefinitionMember Members

        ITypeDefinition ITypeDefinitionMember.ContainingTypeDefinition
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedPropertySymbol.ContainingType.GetCciAdapter();
            }
        }

        TypeMemberVisibility ITypeDefinitionMember.Visibility
        {
            get
            {
                CheckDefinitionInvariant();
                return AdaptedPropertySymbol.MetadataVisibility;
            }
        }

        #endregion

        #region ITypeMemberReference Members

        ITypeReference ITypeMemberReference.GetContainingType(EmitContext context)
        {
            CheckDefinitionInvariant();
            return AdaptedPropertySymbol.ContainingType.GetCciAdapter();
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
                return AdaptedPropertySymbol.MetadataName;
            }
        }

        #endregion

        private IMethodReference GetSynthesizedSealedAccessor(MethodKind targetMethodKind)
        {
            var sourceProperty = AdaptedPropertySymbol as SourcePropertySymbolBase;
            if ((object)sourceProperty != null)
            {
                SynthesizedSealedPropertyAccessor synthesized = sourceProperty.SynthesizedSealedAccessorOpt;
                return (object)synthesized != null && synthesized.MethodKind == targetMethodKind ? synthesized.GetCciAdapter() : null;
            }

            return null;
        }
    }

    internal partial class PropertySymbol
    {
#if DEBUG
        private PropertySymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();

        internal new PropertySymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new PropertySymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal PropertySymbol AdaptedPropertySymbol => this;

        internal new PropertySymbol GetCciAdapter()
        {
            return this;
        }
#endif 

        internal virtual bool HasRuntimeSpecialName
        {
            get
            {
                CheckDefinitionInvariant();
                return false;
            }
        }
    }

#if DEBUG
    internal partial class PropertySymbolAdapter
    {
        internal PropertySymbolAdapter(PropertySymbol underlyingPropertySymbol)
        {
            AdaptedPropertySymbol = underlyingPropertySymbol;

            if (underlyingPropertySymbol is NativeIntegerPropertySymbol)
            {
                // Emit should use underlying symbol only.
                throw ExceptionUtilities.Unreachable();
            }
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedPropertySymbol;
        internal PropertySymbol AdaptedPropertySymbol { get; }
    }
#endif
}
