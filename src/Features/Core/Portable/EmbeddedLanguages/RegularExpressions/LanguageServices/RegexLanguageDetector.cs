// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.RQName.Nodes;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices
{

    /// <summary>
    /// Helper class to detect regex pattern tokens in a document efficiently.
    /// </summary>
    internal sealed class RegexLanguageDetector : AbstractLanguageDetector<RegexOptions, RegexTree>
    {
        private const string _patternName = "pattern";

        /// <summary>
        /// Cache so that we can reuse the same <see cref="RegexLanguageDetector"/> when analyzing a particular
        /// compilation model.  This saves the time from having to recreate this for every string literal that features
        /// examine for a particular compilation.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, RegexLanguageDetector> _modelToDetector = new();

        private readonly INamedTypeSymbol? _regexType;
        private readonly HashSet<string> _methodNamesOfInterest;

        private static readonly LanguageCommentDetector<RegexOptions> s_languageCommentDetector = new("regex", "regexp");

        public RegexLanguageDetector(
            EmbeddedLanguageInfo info,
            INamedTypeSymbol? regexType,
            HashSet<string> methodNamesOfInterest)
            : base("Regex", info, s_languageCommentDetector)
        {
            _regexType = regexType;
            _methodNamesOfInterest = methodNamesOfInterest;
        }

        public static RegexLanguageDetector GetOrCreate(
            Compilation compilation, EmbeddedLanguageInfo info)
        {
            // Do a quick non-allocating check first.
            if (_modelToDetector.TryGetValue(compilation, out var detector))
                return detector;

            return _modelToDetector.GetValue(compilation, _ => Create(compilation, info));
        }

        private static RegexLanguageDetector Create(
            Compilation compilation, EmbeddedLanguageInfo info)
        {
            var regexType = compilation.GetTypeByMetadataName(typeof(Regex).FullName!);
            var methodNamesOfInterest = GetMethodNamesOfInterest(regexType, info.SyntaxFacts);
            return new RegexLanguageDetector(info, regexType, methodNamesOfInterest);
        }

        /// <summary>
        /// Finds public, static methods in <see cref="Regex"/> that have a parameter called
        /// 'pattern'.  These are helpers (like <see cref="Regex.Replace(string, string, string)"/> 
        /// where at least one (but not necessarily more) of the parameters should be treated as a
        /// pattern.
        /// </summary>
        private static HashSet<string> GetMethodNamesOfInterest(INamedTypeSymbol? regexType, ISyntaxFacts syntaxFacts)
        {
            var result = syntaxFacts.IsCaseSensitive
                ? new HashSet<string>()
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (regexType != null)
            {
                result.AddRange(
                    from method in regexType.GetMembers().OfType<IMethodSymbol>()
                    where method.DeclaredAccessibility == Accessibility.Public
                    where method.IsStatic
                    where method.Parameters.Any(p => p.Name == _patternName)
                    select method.Name);
            }

            return result;
        }

        protected override bool IsArgumentToWellKnownAPI(
            SyntaxToken token,
            SyntaxNode argumentNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out RegexOptions options)
        {
            if (_regexType == null)
            {
                options = default;
                return false;
            }

            var syntaxFacts = Info.SyntaxFacts;
            var argumentList = argumentNode.GetRequiredParent();
            var invocationOrCreation = argumentList.Parent;
            if (syntaxFacts.IsInvocationExpression(invocationOrCreation))
            {
                var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
                var name = GetNameOfInvokedExpression(invokedExpression);
                if (name != null && _methodNamesOfInterest.Contains(name))
                {
                    // Is a string argument to a method that looks like it could be a Regex method.  
                    // Need to do deeper analysis.

                    // Note we do not use GetAllSymbols here because we don't want to incur the
                    // allocation.
                    var symbolInfo = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken);
                    var method = symbolInfo.Symbol;
                    if (TryAnalyzeInvocation(_regexType, argumentNode, semanticModel, method, cancellationToken, out options))
                        return true;

                    foreach (var candidate in symbolInfo.CandidateSymbols)
                    {
                        if (TryAnalyzeInvocation(_regexType, argumentNode, semanticModel, candidate, cancellationToken, out options))
                            return true;
                    }
                }
            }
            else if (syntaxFacts.IsObjectCreationExpression(invocationOrCreation))
            {
                var typeNode = syntaxFacts.GetTypeOfObjectCreationExpression(invocationOrCreation);
                var name = GetNameOfType(typeNode, syntaxFacts);
                if (name != null)
                {
                    if (syntaxFacts.StringComparer.Compare(nameof(Regex), name) == 0)
                    {
                        var constructor = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                        if (_regexType.Equals(constructor?.ContainingType))
                        {
                            // Argument to "new Regex".  Need to do deeper analysis
                            return AnalyzeStringLiteral(argumentNode, semanticModel, cancellationToken, out options);
                        }
                    }
                }
            }
            else if (syntaxFacts.IsImplicitObjectCreationExpression(invocationOrCreation))
            {
                var constructor = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                if (_regexType.Equals(constructor?.ContainingType))
                {
                    // Argument to "new Regex".  Need to do deeper analysis
                    return AnalyzeStringLiteral(argumentNode, semanticModel, cancellationToken, out options);
                }
            }

            options = default;
            return false;
        }

        private bool TryAnalyzeInvocation(
            INamedTypeSymbol regexType,
            SyntaxNode argumentNode,
            SemanticModel semanticModel,
            ISymbol? method,
            CancellationToken cancellationToken,
            out RegexOptions options)
        {
            if (method != null &&
                method.DeclaredAccessibility == Accessibility.Public &&
                method.IsStatic &&
                regexType.Equals(method.ContainingType))
            {
                return AnalyzeStringLiteral(argumentNode, semanticModel, cancellationToken, out options);
            }

            options = default;
            return false;
        }

        protected override RegexTree? TryParse(VirtualCharSequence chars, RegexOptions options)
            => RegexParser.TryParse(chars, options);

        private bool AnalyzeStringLiteral(
            SyntaxNode argumentNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out RegexOptions options)
        {
            options = default;

            var parameter = Info.SemanticFacts.FindParameterForArgument(semanticModel, argumentNode, cancellationToken);
            if (parameter?.Name != _patternName)
            {
                return false;
            }

            options = GetOptionsFromSiblingArgument(argumentNode, semanticModel, cancellationToken) ?? default;
            return true;
        }

        protected override bool TryGetOptions(
            SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out RegexOptions options)
        {
            if (exprType.Name == nameof(RegexOptions))
            {
                var constVal = semanticModel.GetConstantValue(expr, cancellationToken);
                if (constVal.Value is int intValue)
                {
                    options = (RegexOptions)intValue;
                    return true;
                }
            }

            options = default;
            return false;
        }

        internal static class TestAccessor
        {
            public static bool TryMatch(string text, out RegexOptions options)
                => s_languageCommentDetector.TryMatch(text, out options);
        }
    }
}
