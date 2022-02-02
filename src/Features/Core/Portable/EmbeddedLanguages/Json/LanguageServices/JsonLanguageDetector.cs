// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
        private const string JsonParameterName = "json";
        private const string ParseMethodName = "Parse";

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

        protected override bool IsArgumentToWellKnownAPI(
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
                if (syntaxFacts.StringComparer.Equals(name, ParseMethodName))
                {
                    // Is a string argument to a method that looks like it could be a json-parsing
                    // method. Need to do deeper analysis
                    var symbol = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                    if (symbol is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: true } &&
                        _typesOfInterest.Contains(symbol.ContainingType) &&
                        IsArgumentToSuitableParameter(semanticModel, argumentNode, cancellationToken))
                    {
                        options = symbol.ContainingType.Name == nameof(JsonDocument) ? JsonOptions.Strict : default;
                        options |= GetOptionsFromSiblingArgument(argumentNode, semanticModel, cancellationToken);
                        return true;
                    }
                }
            }

            options = default;
            return false;
        }

        protected override bool TryGetOptions(
            SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out JsonOptions options)
        {
            options = default;

            // look for an argument of the form `new JsonDocumentOptions { AllowTrailingCommas = ..., CommentHandling = ... }`

            if (exprType.Name != nameof(JsonDocumentOptions))
                return false;

            var syntaxFacts = Info.SyntaxFacts;
            expr = syntaxFacts.WalkDownParentheses(expr);
            if (syntaxFacts.IsObjectCreationExpression(expr) ||
                syntaxFacts.IsImplicitObjectCreationExpression(expr))
            {
                syntaxFacts.GetPartsOfBaseObjectCreationExpression(expr, out var argumentList, out var objectInitializer);
                if (objectInitializer != null)
                {
                    var initializers = syntaxFacts.GetMemberInitializersOfInitializer(objectInitializer);
                    foreach (var initializer in initializers)
                    {
                        if (syntaxFacts.IsNamedMemberInitializer(initializer))
                        {
                            syntaxFacts.GetPartsOfNamedMemberInitializer(initializer, out var name, out var initExpr);
                            var propName = syntaxFacts.GetIdentifierOfIdentifierName(name).ValueText;
                            if (syntaxFacts.StringComparer.Equals(propName, nameof(JsonDocumentOptions.AllowTrailingCommas)) &&
                                semanticModel.GetConstantValue(initExpr).Value is true)
                            {
                                options |= JsonOptions.TrailingCommas;
                            }
                            else if (syntaxFacts.StringComparer.Equals(propName, nameof(JsonDocumentOptions.CommentHandling)) &&
                                     semanticModel.GetConstantValue(initExpr).Value is (byte)JsonCommentHandling.Allow or (byte)JsonCommentHandling.Skip)
                            {
                                options |= JsonOptions.Comments;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool IsArgumentToSuitableParameter(
            SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
        {
            var parameter = Info.SemanticFacts.FindParameterForArgument(semanticModel, argumentNode, cancellationToken);
            return parameter?.Name == JsonParameterName;
        }

        internal static class TestAccessor
        {
            public static bool TryMatch(string text, out JsonOptions options)
                => s_languageCommentDetector.TryMatch(text, out options);
        }
    }
}
