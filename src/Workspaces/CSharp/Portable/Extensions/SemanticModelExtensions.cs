// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

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

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<ArgumentSyntax> arguments,
            IList<string> reservedNames,
            NamingRule parameterNamingRule,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameColon != null)).ToImmutableArray();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToImmutableArray();

            return GenerateNames(reservedNames, isFixed, parameterNames, parameterNamingRule);
        }

        private static ImmutableArray<ParameterName> GenerateNames(IList<string> reservedNames, ImmutableArray<bool> isFixed, ImmutableArray<string> parameterNames)
            => NameGenerator.EnsureUniqueness(parameterNames, isFixed)
                .Select((name, index) => new ParameterName(name, isFixed[index]))
                .Skip(reservedNames.Count).ToImmutableArray();

        private static ImmutableArray<ParameterName> GenerateNames(IList<string> reservedNames, ImmutableArray<bool> isFixed, ImmutableArray<string> parameterNames, NamingRule parameterNamingRule)
            => NameGenerator.EnsureUniqueness(parameterNames, isFixed)
                .Select((name, index) => new ParameterName(name, isFixed[index], parameterNamingRule))
                .Skip(reservedNames.Count).ToImmutableArray();

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

        public static ImmutableArray<ParameterName> GenerateParameterNames(
            this SemanticModel semanticModel,
            IEnumerable<AttributeArgumentSyntax> arguments,
            IList<string> reservedNames,
            NamingRule parameterNamingRule,
            CancellationToken cancellationToken)
        {
            reservedNames ??= SpecializedCollections.EmptyList<string>();

            // We can't change the names of named parameters.  Any other names we're flexible on.
            var isFixed = reservedNames.Select(s => true).Concat(
                arguments.Select(a => a.NameEquals != null)).ToImmutableArray();

            var parameterNames = reservedNames.Concat(
                arguments.Select(a => semanticModel.GenerateNameForArgument(a, cancellationToken))).ToImmutableArray();

            return GenerateNames(reservedNames, isFixed, parameterNames, parameterNamingRule);
        }
    }
}
