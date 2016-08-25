﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

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
        internal abstract class CommonEmbeddedMember
        {
            internal abstract TEmbeddedTypesManager TypeManager { get; }
        }

        internal abstract class CommonEmbeddedMember<TMember> : CommonEmbeddedMember, Cci.IReference
            where TMember : TSymbol, Cci.ITypeMemberReference
        {
            protected readonly TMember UnderlyingSymbol;
            private ImmutableArray<TAttributeData> _lazyAttributes;

            protected CommonEmbeddedMember(TMember underlyingSymbol)
            {
                this.UnderlyingSymbol = underlyingSymbol;
            }

            protected abstract IEnumerable<TAttributeData> GetCustomAttributesToEmit(TModuleCompilationState compilationState);

            protected virtual TAttributeData PortAttributeIfNeedTo(TAttributeData attrData, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                return null;
            }

            private ImmutableArray<TAttributeData> GetAttributes(TModuleCompilationState compilationState, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                var builder = ArrayBuilder<TAttributeData>.GetInstance();

                // Copy some of the attributes.

                // Note, when porting attributes, we are not using constructors from original symbol.
                // The constructors might be missing (for example, in metadata case) and doing lookup
                // will ensure that we report appropriate errors.

                foreach (var attrData in GetCustomAttributesToEmit(compilationState))
                {
                    if (TypeManager.IsTargetAttribute(UnderlyingSymbol, attrData, AttributeDescription.DispIdAttribute))
                    {
                        if (attrData.CommonConstructorArguments.Length == 1)
                        {
                            builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_DispIdAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                        }
                    }
                    else
                    {
                        builder.AddOptional(PortAttributeIfNeedTo(attrData, syntaxNodeOpt, diagnostics));
                    }
                }

                return builder.ToImmutableAndFree();
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
            {
                if (_lazyAttributes.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var attributes = GetAttributes((TModuleCompilationState)context.Module.CommonModuleCompilationState, (TSyntaxNode)context.SyntaxNodeOpt, diagnostics);

                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyAttributes, attributes))
                    {
                        // Save any diagnostics that we encountered.
                        context.Diagnostics.AddRange(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyAttributes;
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                throw ExceptionUtilities.Unreachable;
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
