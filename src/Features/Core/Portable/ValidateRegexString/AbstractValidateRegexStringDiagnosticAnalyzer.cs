// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValidateRegexString
{
    internal abstract class AbstractValidateRegexStringDiagnosticAnalyzer<TSyntaxKind>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        private const string _patternName = "pattern";
        private readonly int _stringLiteralKind;

        protected AbstractValidateRegexStringDiagnosticAnalyzer(int stringLiteralKind)
            : base(IDEDiagnosticIds.RegexPatternDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Regex_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _stringLiteralKind = stringLiteralKind;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract IVirtualCharService GetVirtualCharService();

        protected abstract IParameterSymbol DetermineParameter(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(ValidateRegexStringOption.ReportInvalidRegexPatterns, syntaxTree.Options.Language);
            if (!option)
            {
                return;
            }

            var regexType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Regex).FullName);
            if (regexType == null)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();
            var methodNamesOfInterest = GetMethodNamesOfInterest(regexType, syntaxFacts);

            var root = syntaxTree.GetRoot(cancellationToken);

            var analyzer = new Analyzer(this, context, regexType, methodNamesOfInterest);
            analyzer.Analyze(root);
        }

        private HashSet<string> GetMethodNamesOfInterest(INamedTypeSymbol regexType, ISyntaxFactsService syntaxFacts)
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

        private struct Analyzer
        {
            private readonly AbstractValidateRegexStringDiagnosticAnalyzer<TSyntaxKind> _analyzer;
            private readonly SemanticModelAnalysisContext _context;
            private readonly SemanticModel _semanticModel;
            private readonly ISyntaxFactsService _syntaxFacts;
            private readonly INamedTypeSymbol _regexType;
            private readonly HashSet<string> _methodNamesOfInterest;
            private readonly CancellationToken _cancellationToken;

            public Analyzer(
                AbstractValidateRegexStringDiagnosticAnalyzer<TSyntaxKind> analyzer, 
                SemanticModelAnalysisContext context, INamedTypeSymbol regexType, 
                HashSet<string> methodNamesOfInterest)
            {
                _analyzer = analyzer;
                _context = context;
                _semanticModel = context.SemanticModel;
                _syntaxFacts = analyzer.GetSyntaxFactsService();
                _regexType = regexType;
                _methodNamesOfInterest = methodNamesOfInterest;
                _cancellationToken = context.CancellationToken;
            }

            public void Analyze(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                    {
                        Analyze(child.AsNode());
                    }
                    else
                    {
                        var token = child.AsToken();
                        if (token.RawKind == _analyzer._stringLiteralKind)
                        {
                            AnalyzeStringLiteral(token);
                        }
                    }
                }
            }

            private void AnalyzeStringLiteral(SyntaxToken stringLiteral)
            {
                var literalNode = stringLiteral.Parent;
                var argumentNode = literalNode.Parent;
                if (!_syntaxFacts.IsArgument(argumentNode))
                {
                    return;
                }

                var argumentList = argumentNode.Parent;
                var invocationOrCreation = argumentList.Parent;
                if (_syntaxFacts.IsInvocationExpression(invocationOrCreation))
                {
                    var invokedExpression = _syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
                    var name = GetNameOfInvokedExpression(invokedExpression);
                    if (!_methodNamesOfInterest.Contains(name))
                    {
                        return;
                    }

                    // Is a string argument to a method that looks like it could be a Regex method.  
                    // Need to do deeper analysis
                    var method = _semanticModel.GetSymbolInfo(invocationOrCreation, _cancellationToken).GetAnySymbol();
                    if (method?.ContainingType != _regexType)
                    {
                        return;
                    }

                    AnalyzeStringLiteral(stringLiteral, argumentNode);
                }
                else if (_syntaxFacts.IsObjectCreationExpression(invocationOrCreation))
                {
                    var typeNode = _syntaxFacts.GetObjectCreationType(invocationOrCreation);
                    var name = GetNameOfType(typeNode, _syntaxFacts);
                    if (name == null)
                    {
                        return;
                    }

                    if (_syntaxFacts.StringComparer.Compare(nameof(Regex), name) != 0)
                    {
                        return;
                    }

                    // Argument to "new Regex".  Need to do deeper analysis
                    AnalyzeStringLiteral(stringLiteral, argumentNode);
                }
                else
                {
                    return;
                }
            }

            private void AnalyzeStringLiteral(SyntaxToken stringLiteral, SyntaxNode argumentNode)
            {
                var parameter = _analyzer.DetermineParameter(_semanticModel, argumentNode, _cancellationToken);
                if (parameter?.Name != _patternName)
                {
                    return;
                }

                var options = GetRegexOptions(argumentNode);

                var service = _analyzer.GetVirtualCharService();
                if (service == null)
                {
                    return;
                }

                var virtualChars = service.TryConvertToVirtualChars(stringLiteral);
                if (virtualChars.IsDefaultOrEmpty)
                {
                    return;
                }

                var tree = RegexParser.Parse(virtualChars, options);
                foreach (var diag in tree.Diagnostics)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(
                        _analyzer.GetDescriptorWithSeverity(DiagnosticSeverity.Warning),
                        Location.Create(_semanticModel.SyntaxTree, diag.Span),
                        diag.Message));
                }
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
}
