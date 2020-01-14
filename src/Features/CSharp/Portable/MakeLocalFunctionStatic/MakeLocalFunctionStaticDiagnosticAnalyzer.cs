// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MakeLocalFunctionStaticDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public MakeLocalFunctionStaticDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId,
                   CSharpCodeStyleOptions.PreferStaticLocalFunction,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(FeaturesResources.Make_local_function_static), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Local_function_can_be_made_static), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.LocalFunctionStatement);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;
            if (localFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            var syntaxTree = context.Node.SyntaxTree;
            if (!MakeLocalFunctionStaticHelper.IsStaticLocalFunctionSupported(syntaxTree))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferStaticLocalFunction);
            if (!option.Value)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            if (MakeLocalFunctionStaticHelper.TryGetCaputuredSymbols(localFunction, semanticModel, out var captures) && captures.Length == 0)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    localFunction.Identifier.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: ImmutableArray.Create(localFunction.GetLocation()),
                    properties: null));
            }
        }
    }
}
