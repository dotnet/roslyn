// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateTime.LanguageServices
{
    /// <summary>
    /// Helper class to detect <see cref="System.DateTime"/> and <see cref="DateTimeOffset"/> format
    /// strings in a document efficiently.
    /// </summary>
    internal sealed class DateTimePatternDetector
    {
        private const string FormatName = "format";

        private static readonly ConditionalWeakTable<SemanticModel, DateTimePatternDetector?> _modelToDetector =
            new ConditionalWeakTable<global::Microsoft.CodeAnalysis.SemanticModel, DateTimePatternDetector?>();

        private readonly EmbeddedLanguageInfo _info;
        private readonly SemanticModel _semanticModel;
        private readonly INamedTypeSymbol _dateTimeType;
        private readonly INamedTypeSymbol _dateTimeOffsetType;

        public DateTimePatternDetector(
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

        public static DateTimePatternDetector? TryGetOrCreate(
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

        private static DateTimePatternDetector? TryCreate(
            SemanticModel semanticModel, EmbeddedLanguageInfo info)
        {
            var dateTimeType = semanticModel.Compilation.GetTypeByMetadataName(typeof(System.DateTime).FullName!);
            var dateTimeOffsetType = semanticModel.Compilation.GetTypeByMetadataName(typeof(System.DateTimeOffset).FullName!);
            if (dateTimeType == null || dateTimeOffsetType == null)
                return null;

            return new DateTimePatternDetector(semanticModel, info, dateTimeType, dateTimeOffsetType);
        }

        public static bool IsDefinitelyNotDateTimeStringToken(SyntaxToken token, ISyntaxFacts syntaxFacts)
        {
            if (!syntaxFacts.IsStringLiteral(token))
                return true;

            if (!IsMethodArgument(token, syntaxFacts))
                return true;

            return false;
        }

        private static bool IsMethodArgument(SyntaxToken token, ISyntaxFacts syntaxFacts)
            => syntaxFacts.IsLiteralExpression(token.Parent) &&
               syntaxFacts.IsArgument(token.Parent!.Parent);

        public bool IsDateTimeStringToken(SyntaxToken token, CancellationToken cancellationToken)
        {
            if (IsDefinitelyNotDateTimeStringToken(token, _info.SyntaxFacts))
                return false;

            var syntaxFacts = _info.SyntaxFacts;

            var stringLiteral = token;
            var literalNode = stringLiteral.Parent;
            var argumentNode = literalNode!.Parent;
            Debug.Assert(syntaxFacts.IsArgument(argumentNode));

            var argumentList = argumentNode!.Parent;
            var invocationOrCreation = argumentList!.Parent;
            if (!syntaxFacts.IsInvocationExpression(invocationOrCreation))
                return false;

            var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
            var name = GetNameOfInvokedExpression(invokedExpression);
            if (name != nameof(ToString) && name != nameof(System.DateTime.ParseExact) && name != nameof(System.DateTime.TryParseExact))
                return false;

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

        private bool IsDateTimeType(INamedTypeSymbol containingType)
            => _dateTimeType.Equals(containingType) || _dateTimeOffsetType.Equals(containingType);

        private bool AnalyzeStringLiteral(IMethodSymbol method, string? argName, int? argIndex)
        {
            Debug.Assert(argName != null || argIndex != null);

            var parameters = method.Parameters;
            if (argName != null)
                return parameters.Any(p => p.Name == argName);

            var parameter = argIndex!.Value < parameters.Length ? parameters[argIndex.Value] : null;
            return parameter?.Name == FormatName;
        }

        private IParameterSymbol? GetParameter(IMethodSymbol method, SyntaxNode argument)
        {
            var syntaxFacts = _info.SyntaxFacts;
            var parameters = method.Parameters;

            // Handle named argument

            var argName = syntaxFacts.GetNameForArgument(argument);
            if (argName != "")
                return parameters.FirstOrDefault(p => p.Name == argName);

            var arguments = syntaxFacts.GetArgumentsOfArgumentList(argument.Parent);
            var index = arguments.IndexOf(argument);
            if (index >= 0 && index < parameters.Length)
                return parameters[index];

            return null;
        }

        private string? GetNameOfInvokedExpression(SyntaxNode invokedExpression)
        {
            var syntaxFacts = _info.SyntaxFacts;
            if (syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;
            }
            else if (syntaxFacts.IsIdentifierName(invokedExpression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;
            }

            return null;
        }
    }
}
