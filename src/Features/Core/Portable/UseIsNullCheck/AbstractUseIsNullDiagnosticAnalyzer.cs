// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseIsNullCheck
{
    internal abstract class AbstractUseIsNullCheckDiagnosticAnalyzer<
        TLanguageKindEnum>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
    {
        protected AbstractUseIsNullCheckDiagnosticAnalyzer(LocalizableString title)
            : base(IDEDiagnosticIds.UseIsNullCheckDiagnosticId,
                   title,
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(compilationContext => {
                var objectType = compilationContext.Compilation.GetSpecialType(SpecialType.System_Object);
                if (objectType != null)
                {
                    var referenceEqualsMethod = objectType.GetMembers(nameof(ReferenceEquals))
                                                          .OfType<IMethodSymbol>()
                                                          .FirstOrDefault(m => m.DeclaredAccessibility == Accessibility.Public &&
                                                                               m.Parameters.Length == 2);
                    if (referenceEqualsMethod != null)
                    {
                        context.RegisterSyntaxNodeAction(c => AnalyzeSyntax(c, referenceEqualsMethod), GetInvocationExpressionKind());
                    }
                }
            });

        protected abstract bool IsLanguageVersionSupported(ParseOptions options);
        protected abstract TLanguageKindEnum GetInvocationExpressionKind();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, IMethodSymbol referenceEqualsMethod)
        {
            var cancellationToken = context.CancellationToken;
            
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            if (!IsLanguageVersionSupported(syntaxTree.Options))
            {
                return;
            }

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, semanticModel.Language);
            if (!option.Value)
            {
                return;
            }

            var invocation = context.Node;
            var syntaxFacts = GetSyntaxFactsService();

            var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            var nameNode = syntaxFacts.IsIdentifierName(expression)
                ? expression
                : syntaxFacts.IsSimpleMemberAccessExpression(expression)
                    ? syntaxFacts.GetNameOfMemberAccessExpression(expression)
                    : null;

            if (!syntaxFacts.IsIdentifierName(nameNode))
            {
                return;
            }

            syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out _);
            if (!syntaxFacts.StringComparer.Equals(name, nameof(ReferenceEquals)))
            {
                return;
            }

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            if (arguments.Count != 2)
            {
                return;
            }

            if (!MatchesPattern(syntaxFacts, arguments[0], arguments[1]) &&
                !MatchesPattern(syntaxFacts, arguments[1], arguments[0]))
            {
                return;
            }

            var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
            if (!referenceEqualsMethod.Equals(symbol))
            {
                return;
            }


            var additionalLocations = ImmutableArray.Create(invocation.GetLocation());
            var properties = ImmutableDictionary<string, string>.Empty;

            var negated = syntaxFacts.IsLogicalNotExpression(invocation.Parent);
            if (negated)
            {
                properties = properties.Add(AbstractUseIsNullCheckCodeFixProvider.Negated, "");
            }

            var severity = option.Notification.Value;
            context.ReportDiagnostic(
                Diagnostic.Create(
                    GetDescriptorWithSeverity(severity), nameNode.GetLocation(),
                    additionalLocations, properties));
        }

        private bool MatchesPattern(ISyntaxFactsService syntaxFacts, SyntaxNode node1, SyntaxNode node2)
            => syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(node1)) &&
               !syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(node2));
    }
}
