// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValidateRegexString
{
    internal class RegexPatternDetector
    {
        private const string _patternName = "pattern";

        private readonly SemanticModel _semanticModel;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly ISemanticFactsService _semanticFacts;
        private readonly INamedTypeSymbol _regexType;
        private readonly HashSet<string> _methodNamesOfInterest;
        private readonly CancellationToken _cancellationToken;

        public RegexPatternDetector(
            SemanticModel semanticModel, 
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            INamedTypeSymbol regexType, 
            HashSet<string> methodNamesOfInterest,
            CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _syntaxFacts = syntaxFacts;
            _semanticFacts = semanticFacts;
            _regexType = regexType;
            _methodNamesOfInterest = methodNamesOfInterest;
            _cancellationToken = cancellationToken;
        }

        public static RegexPatternDetector TryCreate(
            SemanticModel semanticModel, 
            ISyntaxFactsService syntaxFacts, 
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken)
        {
            var regexType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Regex).FullName);
            if (regexType == null)
            {
                return null;
            }

            var methodNamesOfInterest = GetMethodNamesOfInterest(regexType, syntaxFacts);
            return new RegexPatternDetector(
                semanticModel, syntaxFacts, semanticFacts,
                regexType, methodNamesOfInterest, cancellationToken);
        }

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

        public bool IsRegexPattern(SyntaxToken token, out RegexOptions options)
        {
            options = default;
            if (!_syntaxFacts.IsStringLiteral(token))
            {
                return false;
            }

            return Analyze(token, out options);
        }

        private bool Analyze(SyntaxToken stringLiteral, out RegexOptions options)
        {
            options = default;

            var literalNode = stringLiteral.Parent;
            var argumentNode = literalNode.Parent;
            if (!_syntaxFacts.IsArgument(argumentNode))
            {
                return false;
            }

            var argumentList = argumentNode.Parent;
            var invocationOrCreation = argumentList.Parent;
            if (_syntaxFacts.IsInvocationExpression(invocationOrCreation))
            {
                var invokedExpression = _syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
                var name = GetNameOfInvokedExpression(invokedExpression);
                if (_methodNamesOfInterest.Contains(name))
                {
                    // Is a string argument to a method that looks like it could be a Regex method.  
                    // Need to do deeper analysis
                    var method = _semanticModel.GetSymbolInfo(invocationOrCreation, _cancellationToken).GetAnySymbol();
                    if (method?.ContainingType == _regexType)
                    {
                        return AnalyzeStringLiteral(stringLiteral, argumentNode, out options);
                    }
                }
            }
            else if (_syntaxFacts.IsObjectCreationExpression(invocationOrCreation))
            {
                var typeNode = _syntaxFacts.GetObjectCreationType(invocationOrCreation);
                var name = GetNameOfType(typeNode, _syntaxFacts);
                if (name != null)
                {
                    if (_syntaxFacts.StringComparer.Compare(nameof(Regex), name) == 0)
                    {
                        // Argument to "new Regex".  Need to do deeper analysis
                        return AnalyzeStringLiteral(stringLiteral, argumentNode, out options);
                    }
                }
            }

            return false;
        }

        private bool AnalyzeStringLiteral(
            SyntaxToken stringLiteral, SyntaxNode argumentNode, out RegexOptions options)
        {
            options = default;

            var parameter = _semanticFacts.FindParameterForArgument(_semanticModel, argumentNode, _cancellationToken);
            if (parameter?.Name != _patternName)
            {
                return false;
            }

            options = GetRegexOptions(argumentNode);
            return true;
        }

        private RegexOptions GetRegexOptions(SyntaxNode argumentNode)
        {
            var argumentList = argumentNode.Parent;
            var arguments = _syntaxFacts.GetArgumentsOfArgumentList(argumentList);
            foreach (var siblingArg in arguments)
            {
                if (siblingArg != argumentNode)
                {
                    var expr = _syntaxFacts.GetExpressionOfArgument(siblingArg);
                    if (expr != null)
                    {
                        var exprType = _semanticModel.GetTypeInfo(expr, _cancellationToken);
                        if (exprType.Type?.Name == nameof(RegexOptions))
                        {
                            var constVal = _semanticModel.GetConstantValue(expr, _cancellationToken);
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
    }
}
