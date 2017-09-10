// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.NameArguments;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.NameArguments
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpNameArgumentsDiagnosticAnalyzer : AbstractNameArgumentsDiagnosticAnalyzer
    {
        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression,
                SyntaxKind.ObjectCreationExpression, SyntaxKind.BaseConstructorInitializer,
                SyntaxKind.ThisConstructorInitializer, SyntaxKind.ElementAccessExpression,
                SyntaxKind.Attribute);

        internal override bool LanguageSupportsNonTrailingNamedArguments(ParseOptions options)
        {
            var parseOptions = (CSharpParseOptions)options;
            return parseOptions.LanguageVersion >= LanguageVersion.CSharp7_2;
        }

        internal override void ReportDiagnosticIfNeeded(SyntaxNodeAnalysisContext context, OptionSet optionSet, ImmutableArray<IParameterSymbol> parameters)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments;
            switch (context.Node)
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

                case AttributeSyntax attribute:
                    ReportDiagnosticIfNeeded(context, optionSet, parameters, attribute.ArgumentList.Arguments);
                    return;

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
                if (argument.NameColon != null ||
                    !argument.Expression.IsAnyLiteralExpression() ||
                    parameters[i].IsParams)
                {
                    continue;
                }

                ReportDiagnostic(context, optionSet, argument, parameters[i].Name);
            }
        }

        private void ReportDiagnosticIfNeeded(SyntaxNodeAnalysisContext context, OptionSet optionSet,
            ImmutableArray<IParameterSymbol> parameters, SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (argument.NameColon != null ||
                    argument.NameEquals != null ||
                    !argument.Expression.IsAnyLiteralExpression() ||
                    parameters[i].IsParams)
                {
                    continue;
                }

                ReportDiagnostic(context, optionSet, argument, parameters[i].Name);
            }
        }
    }
}
