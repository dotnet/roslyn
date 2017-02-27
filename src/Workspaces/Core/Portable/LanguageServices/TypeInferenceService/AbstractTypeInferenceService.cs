﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageServices.TypeInferenceService
{
    internal abstract partial class AbstractTypeInferenceService<TExpressionSyntax> : ITypeInferenceService
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken);

        private ImmutableArray<ITypeSymbol> InferTypeBasedOnNameIfEmpty(
            SemanticModel semanticModel, ImmutableArray<ITypeSymbol> result, string nameOpt)
        {
            if (result.IsEmpty && nameOpt != null)
            {
                return InferTypeBasedOnName(semanticModel, nameOpt);
            }

            return result;
        }

        private ImmutableArray<TypeInferenceInfo> InferTypeBasedOnNameIfEmpty(
            SemanticModel semanticModel, ImmutableArray<TypeInferenceInfo> result, string nameOpt)
        {
            if (result.IsEmpty && nameOpt != null)
            {
                var types = InferTypeBasedOnName(semanticModel, nameOpt);
                return types.SelectAsArray(t => new TypeInferenceInfo(t));
            }

            return result;
        }

        private static readonly ImmutableArray<string> s_booleanPrefixes =
            ImmutableArray.Create("Is", "Has", "Contains", "Supports"); 

        private ImmutableArray<ITypeSymbol> InferTypeBasedOnName(
            SemanticModel semanticModel, string name)
        {
            var matchesBoolean = MatchesBoolean(name);
            return matchesBoolean
                ? ImmutableArray.Create<ITypeSymbol>(semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean))
                : ImmutableArray<ITypeSymbol>.Empty;
        }

        private static bool MatchesBoolean(string name)
        {
            foreach (var prefix in s_booleanPrefixes)
            {
                if (Matches(name, prefix))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Matches(string name, string prefix)
        {
            if (name.StartsWith(prefix))
            {
                if (name.Length == prefix.Length)
                {
                    return true;
                }

                var nextChar = name[prefix.Length];
                return !char.IsLower(nextChar);
            }

            return false;
        }

        public ImmutableArray<ITypeSymbol> InferTypes(
            SemanticModel semanticModel, int position, 
            string nameOpt, CancellationToken cancellationToken)
        {
            var result = CreateTypeInferrer(semanticModel, cancellationToken)
                .InferTypes(position)
                .Select(t => t.InferredType)
                .ToImmutableArray();

            return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
        }


        public ImmutableArray<ITypeSymbol> InferTypes(
            SemanticModel semanticModel, SyntaxNode expression, 
            string nameOpt, CancellationToken cancellationToken)
        {
            var result = CreateTypeInferrer(semanticModel, cancellationToken)
                .InferTypes(expression as TExpressionSyntax)
                .Select(info => info.InferredType)
                .ToImmutableArray();

            return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
        }

        public ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(
            SemanticModel semanticModel, int position, 
            string nameOpt, CancellationToken cancellationToken)
        {
            var result = CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(position);
            return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
        }

        public ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(
            SemanticModel semanticModel, SyntaxNode expression, 
            string nameOpt, CancellationToken cancellationToken)
        {
            var result = CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(expression as TExpressionSyntax);
            return InferTypeBasedOnNameIfEmpty(semanticModel, result, nameOpt);
        }
    }
}
