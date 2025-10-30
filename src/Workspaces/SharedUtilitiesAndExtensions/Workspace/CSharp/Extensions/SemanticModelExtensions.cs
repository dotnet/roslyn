// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SemanticModelExtensions
    {
        public static INamespaceSymbol GetDeclaredSymbol(this SemanticModel semanticModel, BaseNamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default)
        {
            return declarationSyntax is NamespaceDeclarationSyntax namespaceDeclaration
                ? semanticModel.GetDeclaredSymbol(namespaceDeclaration, cancellationToken)
                : semanticModel.GetDeclaredSymbol((FileScopedNamespaceDeclarationSyntax)declarationSyntax, cancellationToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SemanticModelExtensions
    {
        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            ArgumentListSyntax argumentList,
            CancellationToken cancellationToken)
        {
            return semanticModel.GenerateParameterNames(
                argumentList.Arguments, reservedNames: null, cancellationToken: cancellationToken);
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            AttributeArgumentListSyntax argumentList,
            CancellationToken cancellationToken)
        {
            return semanticModel.GenerateParameterNames(
                argumentList.Arguments, reservedNames: null, cancellationToken: cancellationToken);
        }

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<ArgumentSyntax> arguments,
            IList<string> reservedNames,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameColon != null)).ToImmutableArray();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToImmutableArray();

            return GenerateNames(reservedNames, isFixed, parameterNames);
        }

        public static ImmutableArray<ParameterName> GenerateNames(IList<string> reservedNames, ImmutableArray<bool> isFixed, ImmutableArray<string> parameterNames)
            => [.. NameGenerator.EnsureUniqueness(parameterNames, isFixed)
                .Select((name, index) => new ParameterName(name, isFixed[index]))
                .Skip(reservedNames.Count)];

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<AttributeArgumentSyntax> arguments,
            IList<string> reservedNames,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameEquals != null)).ToImmutableArray();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToImmutableArray();

            return GenerateNames(reservedNames, isFixed, parameterNames);
        }

        /// <summary>
        /// Given an argument node, tries to generate an appropriate name that can be used for that
        /// argument.
        /// </summary>
        public static string GenerateNameForArgument(
            this SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            // If it named argument then we use the name provided.
            if (argument.NameColon != null)
            {
                return argument.NameColon.Name.Identifier.ValueText;
            }

            return semanticModel.GenerateNameForExpression(
                argument.Expression, capitalize: false, cancellationToken: cancellationToken);
        }

        public static string GenerateNameForArgument(
            this SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
        {
            // If it named argument then we use the name provided.
            if (argument.NameEquals != null)
            {
                return argument.NameEquals.Name.Identifier.ValueText;
            }

            return semanticModel.GenerateNameForExpression(
                argument.Expression, capitalize: false, cancellationToken: cancellationToken);
        }
    }
}
