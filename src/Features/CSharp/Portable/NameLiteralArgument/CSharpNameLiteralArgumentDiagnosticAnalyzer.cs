// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression,
                SyntaxKind.ObjectCreationExpression, SyntaxKind.BaseConstructorInitializer,
                SyntaxKind.ThisConstructorInitializer, SyntaxKind.ElementAccessExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var syntaxTree = context.Node.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_2)
            {
                return;
            }

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet is null)
            {
                return;
            }
            if (!optionSet.GetOption(CSharpCodeStyleOptions.PreferNamingLiteralArguments).Value)
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node);
            if (symbolInfo.Symbol is null)
            {
                return;
            }

            var parameters = symbolInfo.Symbol.GetParameters();
            if (parameters.IsDefaultOrEmpty)
            {
                return;
            }

            var node = context.Node;
            SeparatedSyntaxList<ArgumentSyntax> arguments;
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    arguments = invocation.ArgumentList.Arguments;
                    break;

                case ObjectCreationExpressionSyntax creation:
                    arguments = creation.ArgumentList.Arguments;
                    break;

                case ConstructorInitializerSyntax creation:
                    arguments = creation.ArgumentList.Arguments;
                    break;

                case ElementAccessExpressionSyntax access:
                    arguments = access.ArgumentList.Arguments;
                    break;

                default:
                    return;
            }

            ReportDiagnosticIfNeeded(context, optionSet, parameters, arguments);
        }

        private void ReportDiagnosticIfNeeded(SyntaxNodeAnalysisContext context, OptionSet optionSet,
            ImmutableArray<IParameterSymbol> parameters, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (!IsPositionalArgument(argument))
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

                context.ReportDiagnostic(
                    Diagnostic.Create(GetDescriptorWithSeverity(
                        optionSet.GetOption(CSharpCodeStyleOptions.PreferNamingLiteralArguments).Notification.Value),
                        argument.GetLocation()));
            }
        }

        private bool IsPositionalArgument(ArgumentSyntax argument)
        {
            return argument.NameColon == null;
        }
    }
}
