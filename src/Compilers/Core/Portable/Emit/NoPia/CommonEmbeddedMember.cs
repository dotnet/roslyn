// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
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
        internal abstract class CommonEmbeddedMember : Cci.IEmbeddedDefinition
        {
            internal abstract TEmbeddedTypesManager TypeManager { get; }

            public bool IsEncDeleted
                => false;
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

            protected abstract IEnumerable<TAttributeData> GetCustomAttributesToEmit(TPEModuleBuilder moduleBuilder);

            protected virtual TAttributeData PortAttributeIfNeedTo(TAttributeData attrData, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                return null;
            }

            private ImmutableArray<TAttributeData> GetAttributes(TPEModuleBuilder moduleBuilder, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                var builder = ArrayBuilder<TAttributeData>.GetInstance();

                // Copy some of the attributes.

                // Note, when porting attributes, we are not using constructors from original symbol.
                // The constructors might be missing (for example, in metadata case) and doing lookup
                // will ensure that we report appropriate errors.

                foreach (var attrData in GetCustomAttributesToEmit(moduleBuilder))
                {
                    if (TypeManager.IsTargetAttribute(attrData, AttributeDescription.DispIdAttribute, out int signatureIndex))
                    {
                        if (signatureIndex == 0 && TypeManager.TryGetAttributeArguments(attrData, out var constructorArguments, out var namedArguments, syntaxNodeOpt, diagnostics))
                        {
                            builder.AddIfNotNull(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_DispIdAttribute__ctor, constructorArguments, namedArguments, syntaxNodeOpt, diagnostics));
                        }
                    }
                    else
                    {
                        builder.AddIfNotNull(PortAttributeIfNeedTo(attrData, syntaxNodeOpt, diagnostics));
                    }
                }

                return builder.ToImmutableAndFree();
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
            {
                if (_lazyAttributes.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var attributes = GetAttributes((TPEModuleBuilder)context.Module, (TSyntaxNode)context.SyntaxNode, diagnostics);

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
                throw ExceptionUtilities.Unreachable();
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                throw ExceptionUtilities.Unreachable();
            }

            Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

            public sealed override bool Equals(object obj)
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw ExceptionUtilities.Unreachable();
            }

            public sealed override int GetHashCode()
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
