// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Json
{
    /// <summary>
    /// Helper class to detect json in string tokens in a document efficiently.
    /// </summary>
    internal class JsonPatternDetector
    {
        private const string _jsonName = "json";
        private const string _methodNameOfInterest = "Parse";
        private static readonly HashSet<string> _typeNamesOfInterest = new HashSet<string>
        {
            "Newtonsoft.Json.Linq.JToken",
            "Newtonsoft.Json.Linq.JObject",
            "Newtonsoft.Json.Linq.JArray"
        };

        private static readonly ConditionalWeakTable<SemanticModel, JsonPatternDetector> _modelToDetector =
            new ConditionalWeakTable<SemanticModel, JsonPatternDetector>();

        private readonly SemanticModel _semanticModel;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly ISemanticFactsService _semanticFacts;
        private readonly IVirtualCharService _virtualCharService;
        private readonly ISet<INamedTypeSymbol> _typesOfInterest;

        /// <summary>
        /// Helps match patterns of the form: language=json
        /// 
        /// All matching is case insensitive, with spaces allowed between the punctuation.
        /// </summary>
        private static readonly Regex s_languageCommentDetector =
            new Regex(@"lang(uage)?\s*=\s*json((\s*,\s*)(?<option>[a-zA-Z]+))*",
                RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public JsonPatternDetector(
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService,
            ISet<INamedTypeSymbol> typesOfInterest)
        {
            _semanticModel = semanticModel;
            _syntaxFacts = syntaxFacts;
            _semanticFacts = semanticFacts;
            _virtualCharService = virtualCharService;
            _typesOfInterest = typesOfInterest;
        }

        public static JsonPatternDetector GetOrCreate(
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            // Do a quick non-allocating check first.
            if (_modelToDetector.TryGetValue(semanticModel, out var detector))
            {
                return detector;
            }

            return _modelToDetector.GetValue(
                semanticModel, _ => Create(semanticModel, syntaxFacts, semanticFacts, virtualCharService));
        }

        private static JsonPatternDetector Create(
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            var types = _typeNamesOfInterest.Select(t => semanticModel.Compilation.GetTypeByMetadataName(t)).WhereNotNull().ToSet();
            return new JsonPatternDetector(
                semanticModel, syntaxFacts, semanticFacts, virtualCharService, types);
        }

        public static bool IsDefinitelyNotJson(SyntaxToken token, ISyntaxFactsService syntaxFacts)
        {
            // We only support string literals passed in arguments to something.
            // In the future we could support any string literal, as long as it has
            // some marker (like a comment on it) stating it's a regex.
            if (!syntaxFacts.IsStringLiteral(token))
            {
                return true;
            }

            if (token.ValueText == "")
            {
                return true;
            }

            return false;
        }

        private static bool HasJsonLanguageComment(
            SyntaxToken token, ISyntaxFactsService syntaxFacts, out bool strict)
        {
            if (HasJsonLanguageComment(token.GetPreviousToken().TrailingTrivia, syntaxFacts, out strict))
            {
                return true;
            }

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (HasJsonLanguageComment(node.GetLeadingTrivia(), syntaxFacts, out strict))
                {
                    return true;
                }
            }

            strict = false;
            return false;
        }

        private static bool HasJsonLanguageComment(
            SyntaxTriviaList list, ISyntaxFactsService syntaxFacts, out bool strict)
        {
            foreach (var trivia in list)
            {
                if (HasJsonLanguageComment(trivia, syntaxFacts, out strict))
                {
                    return true;
                }
            }

            strict = false;
            return false;
        }

        private static bool HasJsonLanguageComment(
            SyntaxTrivia trivia, ISyntaxFactsService syntaxFacts, out bool strict)
        {
            strict = false;
            if (syntaxFacts.IsRegularComment(trivia))
            {
                var text = trivia.ToString();
                var match = s_languageCommentDetector.Match(text);
                if (match.Success)
                {
                    var optionGroup = match.Groups["option"];
                    foreach (Capture capture in optionGroup.Captures)
                    {
                        if (StringComparer.OrdinalIgnoreCase.Equals("strict", capture.Value))
                        {
                            strict = true;
                        }
                        else
                        {
                            break;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool IsMethodArgument(SyntaxToken token, ISyntaxFactsService syntaxFacts)
            => syntaxFacts.IsLiteralExpression(token.Parent) &&
               syntaxFacts.IsArgument(token.Parent.Parent) &&
               syntaxFacts.IsInvocationExpression(token.Parent.Parent.Parent.Parent);

        public bool IsDefinitelyJson(SyntaxToken token, CancellationToken cancellationToken)
        {
            if (IsDefinitelyNotJson(token, _syntaxFacts))
            {
                return false;
            }

            if (HasJsonLanguageComment(token, _syntaxFacts, out _))
            {
                return true;
            }

            if (!IsMethodArgument(token, _syntaxFacts))
            {
                return false;
            }

            var stringLiteral = token;
            var literalNode = stringLiteral.Parent;
            var argumentNode = literalNode.Parent;
            Debug.Assert(_syntaxFacts.IsArgument(argumentNode));

            var argumentList = argumentNode.Parent;
            var invocationOrCreation = argumentList.Parent;
            if (_syntaxFacts.IsInvocationExpression(invocationOrCreation))
            {
                var invokedExpression = _syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
                var name = GetNameOfInvokedExpression(invokedExpression);
                if (_syntaxFacts.StringComparer.Equals(name, _methodNameOfInterest))
                {
                    // Is a string argument to a method that looks like it could be a Regex method.  
                    // Need to do deeper analysis
                    var method = _semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken).GetAnySymbol();
                    if (method != null &&
                        method.DeclaredAccessibility == Accessibility.Public &&
                        method.IsStatic &&
                        _typesOfInterest.Contains(method.ContainingType))
                    {
                        return AnalyzeStringLiteral(
                            stringLiteral, argumentNode, cancellationToken);
                    }
                }
            }

            return false;
        }

        public JsonTree TryParseJson(SyntaxToken token, CancellationToken cancellationToken)
        {
            if (IsDefinitelyNotJson(token, _syntaxFacts))
            {
                return null;
            }

            HasJsonLanguageComment(token, _syntaxFacts, out var strict);

            var chars = _virtualCharService.TryConvertToVirtualChars(token);
            if (chars.IsDefaultOrEmpty)
            {
                return null;
            }

            return JsonParser.TryParse(chars, strict);
        }

        private bool AnalyzeStringLiteral(
            SyntaxToken stringLiteral, SyntaxNode argumentNode,
            CancellationToken cancellationToken)
        {
            var parameter = _semanticFacts.FindParameterForArgument(_semanticModel, argumentNode, cancellationToken);
            return parameter?.Name == _jsonName;
        }

        private string GetNameOfInvokedExpression(SyntaxNode invokedExpression)
        {
            if (_syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
            {
                return _syntaxFacts.GetIdentifierOfSimpleName(_syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;
            }
            else if (_syntaxFacts.IsIdentifierName(invokedExpression))
            {
                return _syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;
            }

            return null;
        }


        public bool IsProbablyJson(SyntaxToken token, CancellationToken cancellationToken)
        {
            if (IsDefinitelyNotJson(token, _syntaxFacts))
            {
                return false;
            }

            var tree = TryParseJson(token, cancellationToken);
            if (tree == null || !tree.Diagnostics.IsEmpty)
            {
                return false;
            }

            return ContainsProbableJsonObject(tree.Root);
        }

        private static bool ContainsProbableJsonObject(JsonNode node)
        {
            if (node.Kind == JsonKind.Object)
            {
                var objNode = (JsonObjectNode)node;
                if (objNode.Sequence.ChildCount >= 1)
                {
                    return true;
                }
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    if (ContainsProbableJsonObject(child.Node))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
