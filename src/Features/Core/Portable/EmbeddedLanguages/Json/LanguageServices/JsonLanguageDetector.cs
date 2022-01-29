// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Helper class to detect json in string tokens in a document efficiently.
    /// </summary>
    internal class JsonLanguageDetector : AbstractLanguageDetector<JsonOptions, JsonTree>
    {
        private const string s_jsonParameterName = "json";
        private const string s_parseMethodName = "Parse";
        private static readonly HashSet<string> s_typeNamesOfInterest = new()
        {
            "Newtonsoft.Json.Linq.JToken",
            "Newtonsoft.Json.Linq.JObject",
            "Newtonsoft.Json.Linq.JArray",
            "System.Text.Json.JsonDocument",
        };

        private static readonly ConditionalWeakTable<Compilation, JsonLanguageDetector> s_compilationToDetector = new();

        private readonly ISet<INamedTypeSymbol> _typesOfInterest;

        /// <summary>
        /// Helps match patterns of the form: language=json
        /// 
        /// All matching is case insensitive, with spaces allowed between the punctuation.
        /// </summary>
        private static readonly LanguageCommentDetector<JsonOptions> s_languageCommentDetector = new("json");

        public JsonLanguageDetector(
            EmbeddedLanguageInfo info,
            ISet<INamedTypeSymbol> typesOfInterest)
            : base(info, s_languageCommentDetector)
        {
            _typesOfInterest = typesOfInterest;
        }

        public static JsonLanguageDetector GetOrCreate(
            Compilation compilation, EmbeddedLanguageInfo info)
        {
            // Do a quick non-allocating check first.
            if (s_compilationToDetector.TryGetValue(compilation, out var detector))
                return detector;

            return s_compilationToDetector.GetValue(compilation, _ => Create(compilation, info));
        }

        private static JsonLanguageDetector Create(
            Compilation compilation, EmbeddedLanguageInfo info)
        {
            var types = s_typeNamesOfInterest.Select(t => compilation.GetTypeByMetadataName(t)).WhereNotNull().ToSet();
            return new JsonLanguageDetector(info, types);
        }

        protected override JsonTree? TryParse(VirtualCharSequence chars, JsonOptions options)
            => JsonParser.TryParse(chars, options);

        protected override bool IsEmbeddedLanguageString(
            SyntaxToken token,
            SyntaxNode argumentNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out JsonOptions options)
        {
            var syntaxFacts = Info.SyntaxFacts;
            var argumentList = argumentNode.GetRequiredParent();
            var invocationOrCreation = argumentList.Parent;
            if (syntaxFacts.IsInvocationExpression(invocationOrCreation))
            {
                var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
                var name = GetNameOfInvokedExpression(invokedExpression);
                if (syntaxFacts.StringComparer.Equals(name, s_parseMethodName))
                {
                    // Is a string argument to a method that looks like it could be a json-parsing
                    // method. Need to do deeper analysis
                    var symbol = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                    if (symbol is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: true } &&
                        _typesOfInterest.Contains(symbol.ContainingType) &&
                        IsArgumentToParameterWithName(semanticModel, argumentNode, s_jsonParameterName, cancellationToken))
                    {
                        options = GetOptionsFromSiblingArgument(argumentNode, semanticModel, cancellationToken);
                        return true;
                    }
                }
            }

            options = default;
            return false;
        }

        protected override bool TryGetOptions(SemanticModel semanticModel, SyntaxNode expr, TypeInfo exprType, CancellationToken cancellationToken, out JsonOptions options)
        {
            options = default;
            return false;
        }

        //public JsonTree? TryParseJson(SyntaxToken token)
        //{
        //    var syntaxFacts = Info.SyntaxFacts;
        //    if (IsDefinitelyNotJson(token, syntaxFacts))
        //        return null;

        //    HasJsonLanguageComment(token, syntaxFacts, out var options);

        //    var chars = _info.VirtualCharService.TryConvertToVirtualChars(token);
        //    if (chars.IsDefaultOrEmpty)
        //        return null;

        //    return JsonParser.TryParse(chars, options);
        //}

        private bool IsArgumentToParameterWithName(
            SemanticModel semanticModel, SyntaxNode argumentNode, string name, CancellationToken cancellationToken)
        {
            var parameter = Info.SemanticFacts.FindParameterForArgument(semanticModel, argumentNode, cancellationToken);
            return parameter?.Name == name;
        }

        internal static class TestAccessor
        {
            public static bool TryMatch(string text, out JsonOptions options)
                => s_languageCommentDetector.TryMatch(text, out options);
        }
    }
}
