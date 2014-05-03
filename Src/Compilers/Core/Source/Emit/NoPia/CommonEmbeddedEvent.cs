// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly TEmbeddedMethod adder;
            private readonly TEmbeddedMethod remover;
            private readonly TEmbeddedMethod caller;

            private int isUsedForComAwareEventBinding;

            protected CommonEmbeddedEvent(TEventSymbol underlyingEvent, TEmbeddedMethod adder, TEmbeddedMethod remover, TEmbeddedMethod caller) :
                base(underlyingEvent)
            {
                Debug.Assert(adder != null || remover != null);

                this.adder = adder;
                this.remover = remover;
                this.caller = caller;
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
                if (this.isUsedForComAwareEventBinding == 0 &&
                    (!isUsedForComAwareEventBinding ||
                     Interlocked.CompareExchange(ref this.isUsedForComAwareEventBinding, 1, 0) == 0))
                {
                    Debug.Assert(!isUsedForComAwareEventBinding || this.isUsedForComAwareEventBinding != 0);

                    EmbedCorrespondingComEventInterfaceMethodInternal(syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding);
                }

                Debug.Assert(!isUsedForComAwareEventBinding || this.isUsedForComAwareEventBinding != 0);
            }

            Cci.IMethodReference Cci.IEventDefinition.Adder
            {
                get { return adder; }
            }

            Cci.IMethodReference Cci.IEventDefinition.Remover
            {
                get { return remover; }
            }

            Cci.IMethodReference Cci.IEventDefinition.Caller
            {
                get { return caller; }
            }

            IEnumerable<Cci.IMethodReference> Cci.IEventDefinition.Accessors
            {
                get
                {
                    if (adder != null)
                    {
                        yield return adder;
                    }

                    if (remover != null)
                    {
                        yield return remover;
                    }

                    if (caller != null)
                    {
                        yield return caller;
                    }
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
                return GetType((TPEModuleBuilder)context.Module, (TSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
            }

            protected TEmbeddedMethod AnAccessor
            {
                get
                {
                    return adder ?? remover;
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