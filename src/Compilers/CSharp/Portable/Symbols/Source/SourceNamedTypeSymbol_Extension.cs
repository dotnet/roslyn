// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyExtensionInfo;

        private class ExtensionInfo
        {
            public StrongBox<ParameterSymbol?>? LazyExtensionParameter;
            public ImmutableDictionary<MethodSymbol, MethodSymbol>? LazyImplementationMap;
        }

        internal override string ExtensionName
        {
            get
            {
                if (!IsExtension)
                {
                    throw ExceptionUtilities.Unreachable();
                }

                MergedNamespaceOrTypeDeclaration declaration;
                if (ContainingType is not null)
                {
                    declaration = ((SourceNamedTypeSymbol)this.ContainingType).declaration;
                }
                else
                {
                    declaration = ((SourceNamespaceSymbol)this.ContainingSymbol).MergedDeclaration;
                }

                var index = declaration.Children.IndexOf(this.declaration);
                return GeneratedNames.MakeExtensionName(index);
            }
        }

        internal sealed override ParameterSymbol? ExtensionParameter
        {
            get
            {
                if (!IsExtension)
                {
                    return null;
                }

                if (_lazyExtensionInfo is null)
                {
                    Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null);
                }

                if (_lazyExtensionInfo.LazyExtensionParameter == null)
                {
                    var extensionParameter = makeExtensionParameter(this);
                    Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyExtensionParameter, new StrongBox<ParameterSymbol?>(extensionParameter), null);
                }

                return _lazyExtensionInfo.LazyExtensionParameter.Value;

                static ParameterSymbol? makeExtensionParameter(SourceNamedTypeSymbol symbol)
                {
                    var markerMethod = symbol.GetMembers(WellKnownMemberNames.ExtensionMarkerMethodName).OfType<SynthesizedExtensionMarker>().SingleOrDefault();

                    if (markerMethod is not { Parameters: [var parameter, ..] })
                    {
                        return null;
                    }

                    return new ReceiverParameterSymbol(symbol, parameter);
                }
            }
        }

        public sealed override MethodSymbol? TryGetCorrespondingExtensionImplementationMethod(MethodSymbol method)
        {
            Debug.Assert(this.IsExtension);
            Debug.Assert(method.IsDefinition);
            Debug.Assert(method.ContainingType == (object)this);

            var containingType = this.ContainingType;

            if (containingType is null)
            {
                return null; // PROTOTYPE: Test this code path
            }

            if (_lazyExtensionInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null); // PROTOTYPE: Test this code path
            }

            if (_lazyExtensionInfo.LazyImplementationMap is null)
            {
                var builder = ImmutableDictionary.CreateBuilder<MethodSymbol, MethodSymbol>(Roslyn.Utilities.ReferenceEqualityComparer.Instance);

                builder.AddRange(
                    containingType.GetMembersUnordered().OfType<SourceExtensionImplementationMethodSymbol>().
                    Select(static m => new KeyValuePair<MethodSymbol, MethodSymbol>(m.UnderlyingMethod, m)));

                Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyImplementationMap, builder.ToImmutable(), null);
            }

            return _lazyExtensionInfo.LazyImplementationMap.GetValueOrDefault(method);
        }

        internal static Symbol? GetCompatibleSubstitutedMember(CSharpCompilation compilation, Symbol extensionMember, TypeSymbol receiverType)
        {
            Debug.Assert(extensionMember.GetIsNewExtensionMember());

            NamedTypeSymbol extension = extensionMember.ContainingType;
            if (extension.ExtensionParameter is null)
            {
                return null;
            }

            Symbol result;
            if (extensionMember.IsDefinition)
            {
                NamedTypeSymbol? constructedExtension = inferExtensionTypeArguments(extension, receiverType, compilation);
                if (constructedExtension is null)
                {
                    return null;
                }

                result = extensionMember.SymbolAsMember(constructedExtension);
            }
            else
            {
                result = extensionMember;
            }

            Debug.Assert(result.ContainingType.ExtensionParameter is not null);
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Conversion conversion = compilation.Conversions.ConvertExtensionMethodThisArg(parameterType: result.ContainingType.ExtensionParameter.Type, receiverType, ref discardedUseSiteInfo, isMethodGroupConversion: false);
            if (!conversion.Exists)
            {
                return null;
            }

            return result;

            static NamedTypeSymbol? inferExtensionTypeArguments(NamedTypeSymbol extension, TypeSymbol receiverType, CSharpCompilation compilation)
            {
                if (extension.Arity == 0)
                {
                    return extension;
                }

                TypeConversions conversions = extension.ContainingAssembly.CorLibrary.TypeConversions;

                // Note: we create a value for purpose of inferring type arguments even when the receiver type is static
                var syntax = (CSharpSyntaxNode)CSharpSyntaxTree.Dummy.GetRoot();
                var receiverValue = new BoundLiteral(syntax, ConstantValue.Bad, receiverType) { WasCompilerGenerated = true };

                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                ImmutableArray<TypeWithAnnotations> typeArguments = MethodTypeInferrer.InferTypeArgumentsFromReceiverType(extension, receiverValue, compilation, conversions, ref discardedUseSiteInfo);
                if (typeArguments.IsDefault || typeArguments.Any(t => !t.HasType))
                {
                    return null;
                }

                bool success = checkConstraints(extension, typeArguments, compilation, conversions);
                if (!success)
                {
                    return null;
                }

                return extension.Construct(typeArguments);
            }

            static bool checkConstraints(NamedTypeSymbol symbol, ImmutableArray<TypeWithAnnotations> typeArgs, CSharpCompilation compilation,
                TypeConversions conversions)
            {
                ImmutableArray<TypeParameterSymbol> typeParams = symbol.TypeParameters;
                var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                var substitution = new TypeMap(typeParams, typeArgs);
                ArrayBuilder<TypeParameterDiagnosticInfo>? useSiteDiagnosticsBuilder = null;

                bool success = symbol.CheckConstraints(
                    new ConstraintsHelper.CheckConstraintsArgs(compilation, conversions, includeNullability: false, NoLocation.Singleton, diagnostics: null, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded),
                    substitution, typeParams, typeArgs, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt: null,
                    ref useSiteDiagnosticsBuilder, ignoreTypeConstraintsDependentOnTypeParametersOpt: null);

                diagnosticsBuilder.Free();

                return success;
            }
        }
    }
}
