// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTime.LanguageServices
{
    /// <summary>
    /// Helper class to detect <see cref="System.DateTime"/> and <see cref="DateTimeOffset"/> format
    /// strings in a document efficiently.
    /// </summary>
    internal sealed class DateAndTimePatternDetector
    {
        private const string FormatName = "format";

        /// <summary>
        /// Cache so that we can reuse the same <see cref="DateAndTimePatternDetector"/> when analyzing a particular
        /// semantic model.  This saves the time from having to recreate this for every string literal that features
        /// examine for a particular semantic model.
        /// </summary>
        private static readonly ConditionalWeakTable<SemanticModel, DateAndTimePatternDetector?> _modelToDetector =
            new ConditionalWeakTable<SemanticModel, DateAndTimePatternDetector?>();

        private readonly EmbeddedLanguageInfo _info;
        private readonly SemanticModel _semanticModel;
        private readonly INamedTypeSymbol _dateTimeType;
        private readonly INamedTypeSymbol _dateTimeOffsetType;

        public DateAndTimePatternDetector(
            SemanticModel semanticModel,
            EmbeddedLanguageInfo info,
            INamedTypeSymbol dateTimeType,
            INamedTypeSymbol dateTimeOffsetType)
        {
            _info = info;
            _semanticModel = semanticModel;
            _dateTimeType = dateTimeType;
            _dateTimeOffsetType = dateTimeOffsetType;
        }

        public static DateAndTimePatternDetector? TryGetOrCreate(
            SemanticModel semanticModel, EmbeddedLanguageInfo info)
        {
            // Do a quick non-allocating check first.
            if (_modelToDetector.TryGetValue(semanticModel, out var detector))
            {
                return detector;
            }

            return _modelToDetector.GetValue(
                semanticModel, _ => TryCreate(semanticModel, info));
        }

        private static DateAndTimePatternDetector? TryCreate(
            SemanticModel semanticModel, EmbeddedLanguageInfo info)
        {
            var dateTimeType = semanticModel.Compilation.GetTypeByMetadataName(typeof(System.DateTime).FullName!);
            var dateTimeOffsetType = semanticModel.Compilation.GetTypeByMetadataName(typeof(System.DateTimeOffset).FullName!);
            if (dateTimeType == null || dateTimeOffsetType == null)
                return null;

            return new DateAndTimePatternDetector(semanticModel, info, dateTimeType, dateTimeOffsetType);
        }

        /// <summary>
        /// Checks if this <paramref name="token"/> is possibly a string literal token that could contain a date or time
        /// format string passed into an method call.  If so, <paramref name="argumentNode"/> and <paramref
        /// name="invocationExpression"/> will be the argument and invocatoin the string literal was passed as.
        /// </summary>
        public static bool IsPossiblyDateAndTimeArgumentToken(
            SyntaxToken token, ISyntaxFacts syntaxFacts,
            [NotNullWhen(true)] out SyntaxNode? argumentNode,
            [NotNullWhen(true)] out SyntaxNode? invocationExpression)
        {
            // Has to be a string literal passed to a method.
            argumentNode = null;
            invocationExpression = null;

            if (!syntaxFacts.IsStringLiteral(token))
                return false;

            if (!IsMethodArgument(token, syntaxFacts))
                return false;

            if (!syntaxFacts.IsLiteralExpression(token.Parent))
                return false;

            if (!syntaxFacts.IsArgument(token.Parent.Parent))
                return false;

            var argumentList = token.Parent.Parent.Parent;
            var invocationOrCreation = argumentList?.Parent;
            if (!syntaxFacts.IsInvocationExpression(invocationOrCreation))
                return false;

            var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
            var name = GetNameOfInvokedExpression(syntaxFacts, invokedExpression);
            if (name != nameof(ToString) && name != nameof(System.DateTime.ParseExact) && name != nameof(System.DateTime.TryParseExact))
                return false;

            // We have a string literal passed to a method called ToString/ParseExact/TryParseExact.
            // Have to do a more expensive semantic check now.
            argumentNode = token.Parent.Parent;
            invocationExpression = invocationOrCreation;

            return true;
        }

        private static string? GetNameOfInvokedExpression(ISyntaxFacts syntaxFacts, SyntaxNode invokedExpression)
        {
            if (syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
                return syntaxFacts.GetIdentifierOfSimpleName(syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;

            if (syntaxFacts.IsIdentifierName(invokedExpression))
                return syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;

            return null;
        }

        private static bool IsMethodArgument(SyntaxToken token, ISyntaxFacts syntaxFacts)
            => syntaxFacts.IsLiteralExpression(token.Parent) &&
               syntaxFacts.IsArgument(token.Parent!.Parent);

        public bool IsDateAndTimeToken(SyntaxToken token, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            if (IsPossiblyDateAndTimeArgumentToken(
                    token, _info.SyntaxFacts,
                    out var argumentNode, out var invocationOrCreation))
            {
                // if we couldn't determine the arg name or arg index, can't proceed.
                var (argName, argIndex) = GetArgumentNameOrIndex(argumentNode);
                if (argName == null && argIndex == null)
                    return false;

                // If we had a specified arg name and it isn't 'format', then it's not a DateTime
                // 'format' param we care about.
                if (argName != null && argName != FormatName)
                    return false;

                var symbolInfo = _semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken);
                var method = symbolInfo.Symbol;
                if (TryAnalyzeInvocation(method, argName, argIndex))
                    return true;

                foreach (var candidate in symbolInfo.CandidateSymbols)
                {
                    if (TryAnalyzeInvocation(candidate, argName, argIndex))
                        return true;
                }
            }
            else if (token.RawKind == syntaxFacts.SyntaxKinds.InterpolatedStringTextToken)
            {
                var interpolationFormatClause = token.Parent!;
                var interpolation = interpolationFormatClause.Parent!;
                if (interpolation!.RawKind == syntaxFacts.SyntaxKinds.Interpolation)
                {
                    var expression = syntaxFacts.GetExpressionOfInterpolation(interpolation);
                    var type = _semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                    return IsDateTimeType(type);
                }
            }

            return false;
        }

        private (string? name, int? index) GetArgumentNameOrIndex(SyntaxNode argument)
        {
            var syntaxFacts = _info.SyntaxFacts;

            var argName = syntaxFacts.GetNameForArgument(argument);
            if (argName != "")
                return (argName, null);

            var arguments = syntaxFacts.GetArgumentsOfArgumentList(argument.Parent);
            var index = arguments.IndexOf(argument);
            if (index >= 0)
                return (null, index);

            return default;
        }

        private bool TryAnalyzeInvocation(ISymbol? symbol, string? argName, int? argIndex)
            => symbol is IMethodSymbol method &&
               method.DeclaredAccessibility == Accessibility.Public &&
               method.MethodKind == MethodKind.Ordinary &&
               IsDateTimeType(method.ContainingType) &&
               AnalyzeStringLiteral(method, argName, argIndex);

        private bool IsDateTimeType(ITypeSymbol? type)
            => _dateTimeType.Equals(type) || _dateTimeOffsetType.Equals(type);

        private static bool AnalyzeStringLiteral(IMethodSymbol method, string? argName, int? argIndex)
        {
            Debug.Assert(argName != null || argIndex != null);

            var parameters = method.Parameters;
            if (argName != null)
                return parameters.Any(p => p.Name == argName);

            var parameter = argIndex < parameters.Length ? parameters[argIndex.Value] : null;
            return parameter?.Name == FormatName;
        }
    }
}
