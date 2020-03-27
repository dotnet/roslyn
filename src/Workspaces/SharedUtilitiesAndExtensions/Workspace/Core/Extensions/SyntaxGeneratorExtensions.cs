// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SyntaxGeneratorExtensions
    {
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
    }
}
