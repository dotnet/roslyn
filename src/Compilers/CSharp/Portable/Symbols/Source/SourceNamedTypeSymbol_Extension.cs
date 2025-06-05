// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyExtensionInfo;

        private class ExtensionInfo
        {
            public MethodSymbol? LazyExtensionMarker = ErrorMethodSymbol.UnknownMethod;
            public ParameterSymbol? LazyExtensionParameter;
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

                int index = 0;
                foreach (Declaration child in declaration.Children)
                {
                    if (child == this.declaration)
                    {
                        return GeneratedNames.MakeExtensionName(index);
                    }

                    if (child.Kind == DeclarationKind.Extension)
                    {
                        index++;
                    }
                }

                throw ExceptionUtilities.Unreachable();
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

                var markerMethod = TryGetOrCreateExtensionMarker();

                if (_lazyExtensionInfo.LazyExtensionParameter == null && markerMethod is { Parameters: [var parameter, ..] })
                {
                    Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyExtensionParameter, new ReceiverParameterSymbol(this, parameter), null);
                }

                return _lazyExtensionInfo.LazyExtensionParameter;
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
                return null;
            }

            if (_lazyExtensionInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null); // Tracked by https://github.com/dotnet/roslyn/issues/76130 : Test this code path TODO2
            }

            if (_lazyExtensionInfo.LazyImplementationMap is null)
            {
                var builder = ImmutableDictionary.CreateBuilder<MethodSymbol, MethodSymbol>(ReferenceEqualityComparer.Instance);

                builder.AddRange(
                    containingType.GetMembersUnordered().OfType<SourceExtensionImplementationMethodSymbol>().
                    Select(static m => new KeyValuePair<MethodSymbol, MethodSymbol>(m.UnderlyingMethod, m)));

                Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyImplementationMap, builder.ToImmutable(), null);
            }

            return _lazyExtensionInfo.LazyImplementationMap.GetValueOrDefault(method);
        }

        protected sealed override MethodSymbol? CreateSynthesizedExtensionMarker()
        {
            return TryGetOrCreateExtensionMarker();
        }

        [MemberNotNull(nameof(_lazyExtensionInfo))]
        private MethodSymbol? TryGetOrCreateExtensionMarker()
        {
            Debug.Assert(IsExtension);

            if (_lazyExtensionInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null);
            }

            if (_lazyExtensionInfo.LazyExtensionMarker == (object)ErrorMethodSymbol.UnknownMethod)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyExtensionMarker, tryCreateExtensionMarker(), ErrorMethodSymbol.UnknownMethod);
            }

            return _lazyExtensionInfo.LazyExtensionMarker;

            MethodSymbol? tryCreateExtensionMarker()
            {
                var syntax = (ExtensionBlockDeclarationSyntax)this.GetNonNullSyntaxNode();
                var parameterList = syntax.ParameterList;
                Debug.Assert(parameterList is not null);

                if (parameterList is null)
                {
                    return null;
                }

                int count = parameterList.Parameters.Count;
                Debug.Assert(count > 0);

                return new SynthesizedExtensionMarker(this, parameterList);
            }
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

                var result = extension.Construct(typeArguments);

                var constraintArgs = new ConstraintsHelper.CheckConstraintsArgs(compilation, conversions, includeNullability: false,
                    NoLocation.Singleton, diagnostics: BindingDiagnosticBag.Discarded, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded);

                bool success = result.CheckConstraints(constraintArgs);
                if (!success)
                {
                    return null;
                }

                return result;
            }
        }
    }
}
