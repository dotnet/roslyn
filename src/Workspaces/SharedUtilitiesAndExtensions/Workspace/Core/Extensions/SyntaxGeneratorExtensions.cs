﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SyntaxGeneratorExtensions
    {
        private const string EqualsName = "Equals";
        private const string DefaultName = "Default";
        private const string ObjName = "obj";
        public const string OtherName = "other";

        public static SyntaxNode CreateThrowNotImplementedStatement(
            this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
        {
            return codeDefinitionFactory.ThrowStatement(
               CreateNotImplementedException(codeDefinitionFactory, compilation));
        }

        public static SyntaxNode CreateThrowNotImplementedExpression(
            this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
        {
            return codeDefinitionFactory.ThrowExpression(
               CreateNotImplementedException(codeDefinitionFactory, compilation));
        }

        private static SyntaxNode CreateNotImplementedException(SyntaxGenerator codeDefinitionFactory, Compilation compilation)
            => codeDefinitionFactory.ObjectCreationExpression(
                    codeDefinitionFactory.TypeExpression(compilation.NotImplementedExceptionType(), addImport: false),
                    SpecializedCollections.EmptyList<SyntaxNode>());

        public static ImmutableArray<SyntaxNode> CreateThrowNotImplementedStatementBlock(
            this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
            => ImmutableArray.Create(CreateThrowNotImplementedStatement(codeDefinitionFactory, compilation));

        public static ImmutableArray<SyntaxNode> CreateArguments(
            this SyntaxGenerator factory,
            ImmutableArray<IParameterSymbol> parameters)
        {
            return parameters.SelectAsArray(p => CreateArgument(factory, p));
        }

        private static SyntaxNode CreateArgument(
            this SyntaxGenerator factory,
            IParameterSymbol parameter)
        {
            return factory.Argument(parameter.RefKind, factory.IdentifierName(parameter.Name));
        }

        public static SyntaxNode GetDefaultEqualityComparer(
            this SyntaxGenerator factory,
            Compilation compilation,
            ITypeSymbol type)
        {
            var equalityComparerType = compilation.EqualityComparerOfTType();
            var typeExpression = equalityComparerType == null
                ? factory.GenericName(nameof(EqualityComparer<int>), type)
                : factory.TypeExpression(equalityComparerType.Construct(type));

            return factory.MemberAccessExpression(typeExpression, factory.IdentifierName(DefaultName));
        }

        private static ITypeSymbol GetType(Compilation compilation, ISymbol symbol)
            => symbol switch
            {
                IFieldSymbol field => field.Type,
                IPropertySymbol property => property.Type,
                _ => compilation.GetSpecialType(SpecialType.System_Object),
            };

        public static SyntaxNode IsPatternExpression(this SyntaxGeneratorInternal generator, SyntaxNode expression, SyntaxNode pattern)
            => generator.IsPatternExpression(expression, isToken: default, pattern);
    }
}
