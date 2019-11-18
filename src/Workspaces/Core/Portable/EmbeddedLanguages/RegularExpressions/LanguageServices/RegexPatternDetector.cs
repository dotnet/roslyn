// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    /// <summary>
    /// Helper class to detect regex pattern tokens in a document efficiently.
    /// </summary>
    internal sealed class RegexPatternDetector
    {
        private const string _patternName = "pattern";

        private static readonly ConditionalWeakTable<SemanticModel, RegexPatternDetector> _modelToDetector =
            new ConditionalWeakTable<SemanticModel, RegexPatternDetector>();

        private readonly EmbeddedLanguageInfo _info;
        private readonly SemanticModel _semanticModel;
        private readonly INamedTypeSymbol _regexType;
        private readonly HashSet<string> _methodNamesOfInterest;

        /// <summary>
        /// Helps match patterns of the form: language=regex,option1,option2,option3
        /// 
        /// All matching is case insensitive, with spaces allowed between the punctuation.
        /// 'regex' or 'regexp' are both allowed.  Option values will be or'ed together
        /// to produce final options value.  If an unknown option is encountered, processing
        /// will stop with whatever value has accumulated so far.
        /// 
        /// Option names are the values from the <see cref="RegexOptions"/> enum.
        /// </summary>
        private static readonly Regex s_languageCommentDetector =
            new Regex(@"\blang(uage)?\s*=\s*regex(p)?\b((\s*,\s*)(?<option>[a-zA-Z]+))*",
                RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Dictionary<string, RegexOptions> s_nameToOption =
            typeof(RegexOptions).GetTypeInfo().DeclaredFields
                .Where(f => f.FieldType == typeof(RegexOptions))
                .ToDictionary(f => f.Name, f => (RegexOptions)f.GetValue(null), StringComparer.OrdinalIgnoreCase);

        public RegexPatternDetector(
            SemanticModel semanticModel,
            EmbeddedLanguageInfo info,
            INamedTypeSymbol regexType,
            HashSet<string> methodNamesOfInterest)
        {
            _info = info;
            _semanticModel = semanticModel;
            _regexType = regexType;
            _methodNamesOfInterest = methodNamesOfInterest;
        }

        public static RegexPatternDetector TryGetOrCreate(
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

        private static RegexPatternDetector TryCreate(
            SemanticModel semanticModel, EmbeddedLanguageInfo info)
        {
            var regexType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Regex).FullName);
            if (regexType == null)
            {
                return null;
            }

            var methodNamesOfInterest = GetMethodNamesOfInterest(regexType, info.SyntaxFacts);
            return new RegexPatternDetector(
                semanticModel, info, regexType, methodNamesOfInterest);
        }

        public static bool IsDefinitelyNotPattern(SyntaxToken token, ISyntaxFactsService syntaxFacts)
        {
            if (!syntaxFacts.IsStringLiteral(token))
            {
                return true;
            }

            if (!IsMethodOrConstructorArgument(token, syntaxFacts) &&
                !HasRegexLanguageComment(token, syntaxFacts, out _))
            {
                return true;
            }

            return false;
        }

        private static bool HasRegexLanguageComment(
            SyntaxToken token, ISyntaxFactsService syntaxFacts, out RegexOptions options)
        {
            if (HasRegexLanguageComment(token.GetPreviousToken().TrailingTrivia, syntaxFacts, out options))
            {
                return true;
            }

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (HasRegexLanguageComment(node.GetLeadingTrivia(), syntaxFacts, out options))
                {
                    return true;
                }
            }

            options = default;
            return false;
        }

        private static bool HasRegexLanguageComment(
            SyntaxTriviaList list, ISyntaxFactsService syntaxFacts, out RegexOptions options)
        {
            foreach (var trivia in list)
            {
                if (HasRegexLanguageComment(trivia, syntaxFacts, out options))
                {
                    return true;
                }
            }

            options = default;
            return false;
        }

        private static bool HasRegexLanguageComment(
            SyntaxTrivia trivia, ISyntaxFactsService syntaxFacts, out RegexOptions options)
        {
            if (syntaxFacts.IsRegularComment(trivia))
            {
                // Note: ToString on SyntaxTrivia is non-allocating.  It will just return the
                // underlying text that the trivia is already pointing to.
                var text = trivia.ToString();
                var (matched, matchOptions) = TryMatch(text);
                if (matched)
                {
                    options = matchOptions;
                    return true;
                }
            }

            options = default;
            return false;
        }

        private static (bool success, RegexOptions options) TryMatch(string text)
        {
            var options = RegexOptions.None;
            var match = s_languageCommentDetector.Match(text);
            if (!match.Success)
            {
                return default;
            }

            var optionGroup = match.Groups["option"];
            foreach (Capture capture in optionGroup.Captures)
            {
                if (s_nameToOption.TryGetValue(capture.Value, out var specificOption))
                {
                    options |= specificOption;
                }
                else
                {
                    // hit something we don't understand.  bail out.  that will help ensure
                    // users don't have weird behavior just because they misspelled something.
                    // instead, they will know they need to fix it up.
                    return default;
                }
            }

            return (true, options);
        }

        private static bool IsMethodOrConstructorArgument(SyntaxToken token, ISyntaxFactsService syntaxFacts)
            => syntaxFacts.IsLiteralExpression(token.Parent) &&
               syntaxFacts.IsArgument(token.Parent.Parent);

        /// <summary>
        /// Finds public, static methods in <see cref="Regex"/> that have a parameter called
        /// 'pattern'.  These are helpers (like <see cref="Regex.Replace(string, string, string)"/> 
        /// where at least one (but not necessarily more) of the parameters should be treated as a
        /// pattern.
        /// </summary>
        private static HashSet<string> GetMethodNamesOfInterest(INamedTypeSymbol regexType, ISyntaxFactsService syntaxFacts)
        {
            var result = syntaxFacts.IsCaseSensitive
                ? new HashSet<string>()
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var methods = from method in regexType.GetMembers().OfType<IMethodSymbol>()
                          where method.DeclaredAccessibility == Accessibility.Public
                          where method.IsStatic
                          where method.Parameters.Any(p => p.Name == _patternName)
                          select method.Name;

            result.AddRange(methods);

            return result;
        }

        public bool IsRegexPattern(SyntaxToken token, CancellationToken cancellationToken, out RegexOptions options)
        {
            options = default;
            if (IsDefinitelyNotPattern(token, _info.SyntaxFacts))
            {
                return false;
            }

            var syntaxFacts = _info.SyntaxFacts;
            if (HasRegexLanguageComment(token, syntaxFacts, out options))
            {
                return true;
            }

            var stringLiteral = token;
            var literalNode = stringLiteral.Parent;
            var argumentNode = literalNode.Parent;
            Debug.Assert(syntaxFacts.IsArgument(argumentNode));

            var argumentList = argumentNode.Parent;
            var invocationOrCreation = argumentList.Parent;
            if (syntaxFacts.IsInvocationExpression(invocationOrCreation))
            {
                var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
                var name = GetNameOfInvokedExpression(invokedExpression);
                if (_methodNamesOfInterest.Contains(name))
                {
                    // Is a string argument to a method that looks like it could be a Regex method.  
                    // Need to do deeper analysis.

                    // Note we do not use GetAllSymbols here because we don't want to incur the
                    // allocation.
                    var symbolInfo = _semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken);
                    var method = symbolInfo.Symbol;
                    if (TryAnalyzeInvocation(stringLiteral, argumentNode, method, cancellationToken, out options))
                    {
                        return true;
                    }

                    foreach (var candidate in symbolInfo.CandidateSymbols)
                    {
                        if (TryAnalyzeInvocation(stringLiteral, argumentNode, candidate, cancellationToken, out options))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (syntaxFacts.IsObjectCreationExpression(invocationOrCreation))
            {
                var typeNode = syntaxFacts.GetObjectCreationType(invocationOrCreation);
                var name = GetNameOfType(typeNode, syntaxFacts);
                if (name != null)
                {
                    if (syntaxFacts.StringComparer.Compare(nameof(Regex), name) == 0)
                    {
                        var constructor = _semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                        if (_regexType.Equals(constructor?.ContainingType))
                        {
                            // Argument to "new Regex".  Need to do deeper analysis
                            return AnalyzeStringLiteral(
                                stringLiteral, argumentNode, cancellationToken, out options);
                        }
                    }
                }
            }

            return false;
        }

        private bool TryAnalyzeInvocation(
            SyntaxToken stringLiteral, SyntaxNode argumentNode, ISymbol method,
            CancellationToken cancellationToken, out RegexOptions options)
        {
            if (method is
            {
                DeclaredAccessibility: Accessibility.Public,
                IsStatic: true
            } && _regexType.Equals(method.ContainingType))
            {
                return AnalyzeStringLiteral(
                    stringLiteral, argumentNode, cancellationToken, out options);
            }

            options = default;
            return false;
        }

        public RegexTree TryParseRegexPattern(SyntaxToken token, CancellationToken cancellationToken)
        {
            if (!this.IsRegexPattern(token, cancellationToken, out var options))
            {
                return null;
            }

            var chars = _info.VirtualCharService.TryConvertToVirtualChars(token);
            return RegexParser.TryParse(chars, options);
        }

        private bool AnalyzeStringLiteral(
            SyntaxToken stringLiteral, SyntaxNode argumentNode,
            CancellationToken cancellationToken, out RegexOptions options)
        {
            options = default;

            var parameter = _info.SemanticFacts.FindParameterForArgument(_semanticModel, argumentNode, cancellationToken);
            if (parameter?.Name != _patternName)
            {
                return false;
            }

            options = GetRegexOptions(argumentNode, cancellationToken);
            return true;
        }

        private RegexOptions GetRegexOptions(SyntaxNode argumentNode, CancellationToken cancellationToken)
        {
            var syntaxFacts = _info.SyntaxFacts;
            var argumentList = argumentNode.Parent;
            var arguments = syntaxFacts.GetArgumentsOfArgumentList(argumentList);
            foreach (var siblingArg in arguments)
            {
                if (siblingArg != argumentNode)
                {
                    var expr = syntaxFacts.GetExpressionOfArgument(siblingArg);
                    if (expr != null)
                    {
                        var exprType = _semanticModel.GetTypeInfo(expr, cancellationToken);
                        if (exprType.Type?.Name == nameof(RegexOptions))
                        {
                            var constVal = _semanticModel.GetConstantValue(expr, cancellationToken);
                            if (constVal.HasValue)
                            {
                                return (RegexOptions)(int)constVal.Value;
                            }
                        }
                    }
                }
            }

            return RegexOptions.None;
        }

        private string GetNameOfType(SyntaxNode typeNode, ISyntaxFactsService syntaxFacts)
        {
            if (syntaxFacts.IsQualifiedName(typeNode))
            {
                return GetNameOfType(syntaxFacts.GetRightSideOfDot(typeNode), syntaxFacts);
            }
            else if (syntaxFacts.IsIdentifierName(typeNode))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(typeNode).ValueText;
            }

            return null;
        }

        private string GetNameOfInvokedExpression(SyntaxNode invokedExpression)
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

        internal static class TestAccessor
        {
            public static (bool success, RegexOptions options) TryMatch(string text)
                => RegexPatternDetector.TryMatch(text);
        }
    }
}
