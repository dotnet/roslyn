// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ITypeInferenceServiceExtensions
    {
        public static INamedTypeSymbol InferDelegateType(
            this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            SyntaxNode expression,
            CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, expression, cancellationToken);
            var delegateTypes = types.Select(t => t.GetDelegateType(semanticModel.Compilation));
            return delegateTypes.WhereNotNull().FirstOrDefault();
        }

        public static ITypeSymbol InferType(this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            SyntaxNode expression,
            bool objectAsDefault,
            CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, expression, cancellationToken)
                                            .WhereNotNull();

            if (!types.Any())
            {
                return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
            }

            return types.FirstOrDefault();
        }

        public static ITypeSymbol InferType(this ITypeInferenceService typeInferenceService,
            SemanticModel semanticModel,
            int position,
            bool objectAsDefault,
            CancellationToken cancellationToken)
        {
            var types = typeInferenceService.InferTypes(semanticModel, position, cancellationToken)
                                            .WhereNotNull();

            if (!types.Any())
            {
                return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
            }

            return types.FirstOrDefault();
        }
    }
}
