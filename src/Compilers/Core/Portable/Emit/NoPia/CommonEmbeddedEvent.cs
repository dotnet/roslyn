// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.NoPia
{
    internal abstract partial class EmbeddedTypesManager<
        TPEModuleBuilder,
        TModuleCompilationState,
        TEmbeddedTypesManager,
        TSyntaxNode,
        TAttributeData,
        TSymbol,
        TAssemblySymbol,
        TNamedTypeSymbol,
        TFieldSymbol,
        TMethodSymbol,
        TEventSymbol,
        TPropertySymbol,
        TParameterSymbol,
        TTypeParameterSymbol,
        TEmbeddedType,
        TEmbeddedField,
        TEmbeddedMethod,
        TEmbeddedEvent,
        TEmbeddedProperty,
        TEmbeddedParameter,
        TEmbeddedTypeParameter>
    {
        internal abstract class CommonEmbeddedEvent : CommonEmbeddedMember<TEventSymbol>, Cci.IEventDefinition
        {
            private readonly TEmbeddedMethod _adder;
            private readonly TEmbeddedMethod _remover;
            private readonly TEmbeddedMethod _caller;

            private int _isUsedForComAwareEventBinding;

            protected CommonEmbeddedEvent(TEventSymbol underlyingEvent, TEmbeddedMethod adder, TEmbeddedMethod remover, TEmbeddedMethod caller) :
                base(underlyingEvent)
            {
                Debug.Assert(adder != null || remover != null);

                _adder = adder;
                _remover = remover;
                _caller = caller;
            }

            internal override TEmbeddedTypesManager TypeManager
            {
                get
                {
                    return AnAccessor.TypeManager;
                }
            }

            protected abstract bool IsRuntimeSpecial { get; }
            protected abstract bool IsSpecialName { get; }
            protected abstract Cci.ITypeReference GetType(TPEModuleBuilder moduleBuilder, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
            protected abstract TEmbeddedType ContainingType { get; }
            protected abstract Cci.TypeMemberVisibility Visibility { get; }
            protected abstract string Name { get; }

            public TEventSymbol UnderlyingEvent
            {
                get
                {
                    return this.UnderlyingSymbol;
                }
            }

            protected abstract void EmbedCorrespondingComEventInterfaceMethodInternal(TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics, bool isUsedForComAwareEventBinding);

            internal void EmbedCorrespondingComEventInterfaceMethod(TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics, bool isUsedForComAwareEventBinding)
            {
                if (_isUsedForComAwareEventBinding == 0 &&
                    (!isUsedForComAwareEventBinding ||
                     Interlocked.CompareExchange(ref _isUsedForComAwareEventBinding, 1, 0) == 0))
                {
                    Debug.Assert(!isUsedForComAwareEventBinding || _isUsedForComAwareEventBinding != 0);

                    EmbedCorrespondingComEventInterfaceMethodInternal(syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding);
                }

                Debug.Assert(!isUsedForComAwareEventBinding || _isUsedForComAwareEventBinding != 0);
            }

            Cci.IMethodReference Cci.IEventDefinition.Adder
            {
                get { return _adder; }
            }

            Cci.IMethodReference Cci.IEventDefinition.Remover
            {
                get { return _remover; }
            }

            Cci.IMethodReference Cci.IEventDefinition.Caller
            {
                get { return _caller; }
            }

            IEnumerable<Cci.IMethodReference> Cci.IEventDefinition.GetAccessors(EmitContext context)
            {
                if (_adder != null)
                {
                    yield return _adder;
                }

                if (_remover != null)
                {
                    yield return _remover;
                }

                if (_caller != null)
                {
                    yield return _caller;
                }
            }

            bool Cci.IEventDefinition.IsRuntimeSpecial
            {
                get
                {
                    return IsRuntimeSpecial;
                }
            }

            bool Cci.IEventDefinition.IsSpecialName
            {
                get
                {
                    return IsSpecialName;
                }
            }

            Cci.ITypeReference Cci.IEventDefinition.GetType(EmitContext context)
            {
                return GetType((TPEModuleBuilder)context.Module, (TSyntaxNode)context.SyntaxNode, context.Diagnostics);
            }

            protected TEmbeddedMethod AnAccessor
            {
                get
                {
                    return _adder ?? _remover;
                }
            }

            Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
            {
                get { return ContainingType; }
            }

            Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
            {
                get
                {
                    return Visibility;
                }
            }

            Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
            {
                return ContainingType;
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                visitor.Visit((Cci.IEventDefinition)this);
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            string Cci.INamedEntity.Name
            {
                get
                {
                    return Name;
                }
            }
        }
    }
}
