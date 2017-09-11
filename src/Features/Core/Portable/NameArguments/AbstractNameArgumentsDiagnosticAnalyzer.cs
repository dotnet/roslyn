// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.NameArguments
{
    /// <summary>
    /// Reports arguments that should be named, based on style preferences. For example, on literals.
    /// </summary>
    internal abstract class AbstractNameArgumentsDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public AbstractNameArgumentsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.NameArgumentsDiagnosticId,
               new LocalizableResourceString(nameof(FeaturesResources.Name_literal_argument), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract bool LanguageSupportsNonTrailingNamedArguments(ParseOptions options);

        protected abstract void ReportDiagnosticIfNeeded(SyntaxNodeAnalysisContext context, OptionSet optionSet,
            ImmutableArray<IParameterSymbol> parameters);


        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.Node.SyntaxTree;
            if (!LanguageSupportsNonTrailingNamedArguments(syntaxTree.Options))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet is null)
            {
                return;
            }

            var preference = optionSet.GetOption(CodeStyleOptions.PreferNamedArguments, context.Compilation.Language).Value;
            if (preference == NamedArgumentsPreference.Never)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;
            if (symbol is null)
            {
                return;
            }

            var parameters = symbol.GetParameters();
            if (parameters.IsDefaultOrEmpty)
            {
                return;
            }

            ReportDiagnosticIfNeeded(context, optionSet, parameters);
        }

        protected void ReportDiagnostic(SyntaxNodeAnalysisContext context, OptionSet optionSet, SyntaxNode argument, string parameterName)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder["ParameterName"] = parameterName;

            context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(CodeStyleOptions.PreferNamedArguments, context.SemanticModel.Language).Notification.Value),
                    argument.GetLocation(), builder.ToImmutable()));
        }
    }
}
