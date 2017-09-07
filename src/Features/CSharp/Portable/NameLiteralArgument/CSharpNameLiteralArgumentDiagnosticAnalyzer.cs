// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.NameLiteralArgument
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpNameLiteralArgumentDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpNameLiteralArgumentDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.NameLiteralArgumentDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Name_literal_argument), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var syntaxTree = context.Node.SyntaxTree;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            ReportDiagnosticsIfNeeded((InvocationExpressionSyntax)context.Node, context, optionSet, syntaxTree);
        }

        private void ReportDiagnosticsIfNeeded(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, OptionSet optionSet, SyntaxTree syntaxTree)
        {
            if (!optionSet.GetOption(CSharpCodeStyleOptions.PreferNamingLiteralArguments).Value)
            {
                return;
            }

            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_2)
            {
                return;
            }

            var method = context.SemanticModel.GetSymbolInfo(invocation);
            if (method.Symbol is null)
            {
                return;
            }

            var symbol = method.Symbol;
            if (symbol.Kind != SymbolKind.Method)
            {
                return;
            }

            var methodSymbol = (IMethodSymbol)symbol;
            var arguments = invocation.ArgumentList.Arguments;
            var parameters = methodSymbol.Parameters;
            for (int i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (argument.NameColon != null)
                {
                    continue;
                }

                if (!argument.Expression.IsAnyLiteralExpression())
                {
                    continue;
                }

                if (parameters[i].IsParams)
                {
                    continue;
                }

                // Create a normal diagnostic
                context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(CSharpCodeStyleOptions.PreferNamingLiteralArguments).Notification.Value),
                    argument.GetLocation()));
            }
        }
    }
}
