// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class UseSystemHashCodeDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public UseSystemHashCodeDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseSystemHashCode,
                   CodeStyleOptions.PreferSystemHashCode,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_System_HashCode), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.GetHashCode_implementation_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(c =>
            {
                // var hashCodeType = c.Compilation.GetTypeByMetadataName("System.HashCode");
                if (Analyzer.TryGetAnalyzer(c.Compilation, out var analyzer))
                {
                    c.RegisterOperationBlockAction(ctx => AnalyzeOperationBlock(analyzer, ctx));
                }
            });
        }

        private void AnalyzeOperationBlock(Analyzer analyzer, OperationBlockAnalysisContext context)
        {
            if (context.OperationBlocks.Length != 1)
            {
                return;
            }

            var owningSymbol = context.OwningSymbol;
            var operation = context.OperationBlocks[0];
            var (accessesBase, hashedMembers) = analyzer.GetHashedMembers(owningSymbol, operation);
            if (!accessesBase && hashedMembers.IsDefaultOrEmpty)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(operation.Syntax.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferSystemHashCode, operation.Language);
            if (!option.Value)
            {
                return;
            }

            var operationLocation = operation.Syntax.GetLocation();
            var declarationLocation = context.OwningSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).GetLocation();
            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                owningSymbol.Locations[0],
                option.Notification.Severity,
                new[] { operationLocation, declarationLocation },
                ImmutableDictionary<string, string>.Empty));
        }
    }
}
