// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ITypeInferenceServiceExtensions
    {
        public static ImmutableArray<ITypeSymbol> InferTypes(this ITypeInferenceService service, SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
            => service.InferTypes(semanticModel, expression, nameOpt: null, cancellationToken: cancellationToken);

        public static ImmutableArray<ITypeSymbol> InferTypes(this ITypeInferenceService service, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => service.InferTypes(semanticModel, position, nameOpt: null, cancellationToken: cancellationToken);

        public static ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(this ITypeInferenceService service, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => service.GetTypeInferenceInfo(semanticModel, position, nameOpt: null, cancellationToken: cancellationToken);

        public static ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(this ITypeInferenceService service, SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
            => service.GetTypeInferenceInfo(semanticModel, expression, nameOpt: null, cancellationToken: cancellationToken);

        public static INamedTypeSymbol? InferDelegateType(
            this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            SyntaxNode expression,
            CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, expression, cancellationToken);
            return GetFirstDelegateType(semanticModel, types);
        }

        public static INamedTypeSymbol? InferDelegateType(
           this ITypeInferenceService typeInferenceService,
           SemanticModel semanticModel,
           int position,
           CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, position, cancellationToken);
            return GetFirstDelegateType(semanticModel, types);
        }

        private static INamedTypeSymbol? GetFirstDelegateType(SemanticModel semanticModel, ImmutableArray<ITypeSymbol> types)
        {
            var delegateTypes = types.Select(t => t.GetDelegateType(semanticModel.Compilation));
            return delegateTypes.WhereNotNull().FirstOrDefault();
        }

        public static ITypeSymbol? InferType(
            this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            SyntaxNode expression,
            bool objectAsDefault,
            CancellationToken cancellationToken)
        {
            return InferType(
                typeInferenceService, semanticModel, expression, objectAsDefault,
                name: null, cancellationToken: cancellationToken);
        }

        public static ITypeSymbol? InferType(
            this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            SyntaxNode expression,
            bool objectAsDefault,
            string? name,
            CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, expression, name, cancellationToken);

            if (types.Length == 0)
            {
                return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
            }

            return types.First();
        }

        public static ITypeSymbol? InferType(
            this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            int position,
            bool objectAsDefault,
            CancellationToken cancellationToken)
        {
            return InferType(
                typeInferenceService, semanticModel, position, objectAsDefault,
                name: null, cancellationToken: cancellationToken);
        }

        public static ITypeSymbol? InferType(
            this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            int position,
            bool objectAsDefault,
            string? name,
            CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, position, name, cancellationToken);

            if (types.Length == 0)
            {
                return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
            }

            return types.FirstOrDefault();
        }
    }
}
