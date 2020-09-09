// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Humanizer;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

        /// <summary>
        /// Given an expression node, tries to generate an appropriate name that can be used for
        /// that expression. 
        /// </summary>
        public static string GenerateNameForExpression(
            this SemanticModel semanticModel, ExpressionSyntax expression,
            bool capitalize, CancellationToken cancellationToken)
        {
            // Try to find a usable name node that we can use to name the
            // parameter.  If we have an expression that has a name as part of it
            // then we try to use that part.
            var current = expression;
            while (true)
            {
                current = current.WalkDownParentheses();

                if (current is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.ValueText.ToCamelCase();
                }
                else if (current is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.ValueText.ToCamelCase();
                }
                else if (current is MemberBindingExpressionSyntax memberBinding)
                {
                    return memberBinding.Name.Identifier.ValueText.ToCamelCase();
                }
                else if (current is ConditionalAccessExpressionSyntax conditionalAccess)
                {
                    current = conditionalAccess.WhenNotNull;
                }
                else if (current is CastExpressionSyntax castExpression)
                {
                    current = castExpression.Expression;
                }
                else if (current is DeclarationExpressionSyntax decl)
                {
                    if (!(decl.Designation is SingleVariableDesignationSyntax name))
                    {
                        break;
                    }

                    return name.Identifier.ValueText.ToCamelCase();
                }
                else if (current.Parent is ForEachStatementSyntax foreachStatement &&
                         foreachStatement.Expression == expression)
                {
                    return foreachStatement.Identifier.ValueText.ToCamelCase().Pluralize();
                }
                else
                {
                    break;
                }
            }

            // there was nothing in the expression to signify a name.  If we're in an argument
            // location, then try to choose a name based on the argument name.
            var argumentName = TryGenerateNameForArgumentExpression(
                semanticModel, expression, cancellationToken);
            if (argumentName != null)
            {
                return capitalize ? argumentName.ToPascalCase() : argumentName.ToCamelCase();
            }

            // Otherwise, figure out the type of the expression and generate a name from that
            // instead.
            var info = semanticModel.GetTypeInfo(expression, cancellationToken);

            // If we can't determine the type, then fallback to some placeholders.
            var type = info.Type;
            var pluralize = Pluralize(semanticModel, type);

            var parameterName = type.CreateParameterName(capitalize);
            return pluralize ? parameterName.Pluralize() : parameterName;
        }

        private static bool Pluralize(SemanticModel semanticModel, ITypeSymbol type)
        {
            if (type == null)
                return false;

            if (type.SpecialType == SpecialType.System_String)
                return false;

            var enumerableType = semanticModel.Compilation.IEnumerableOfTType();
            return type.AllInterfaces.Any(i => i.OriginalDefinition.Equals(enumerableType));
        }

        private static string TryGenerateNameForArgumentExpression(
            SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var topExpression = expression.WalkUpParentheses();
            if (topExpression.IsParentKind(SyntaxKind.Argument, out ArgumentSyntax argument))
            {
                if (argument.NameColon != null)
                {
                    return argument.NameColon.Name.Identifier.ValueText;
                }

                if (argument.Parent is BaseArgumentListSyntax argumentList)
                {
                    var index = argumentList.Arguments.IndexOf(argument);
                    if (semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken).Symbol is IMethodSymbol member && index < member.Parameters.Length)
                    {
                        var parameter = member.Parameters[index];
                        if (parameter.Type.OriginalDefinition.TypeKind != TypeKind.TypeParameter)
                        {
                            return parameter.Name;
                        }
                    }
                }
            }

            return null;
        }
    }
}
