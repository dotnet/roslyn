// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class MakeLocalFunctionStaticDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public MakeLocalFunctionStaticDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId,
                   EnforceOnBuildValues.MakeLocalFunctionStatic,
                   CSharpCodeStyleOptions.PreferStaticLocalFunction,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_local_function_static), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Local_function_can_be_made_static), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(context =>
            {
                if (MakeLocalFunctionStaticHelper.IsStaticLocalFunctionSupported(context.Compilation.LanguageVersion()))
                    context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.LocalFunctionStatement);
            });

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;
            if (localFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            var option = context.GetCSharpAnalyzerOptions().PreferStaticLocalFunction;
            if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            if (MakeLocalFunctionStaticHelper.CanMakeLocalFunctionStaticBecauseNoCaptures(localFunction, semanticModel))
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    localFunction.Identifier.GetLocation(),
                    option.Notification,
                    additionalLocations: ImmutableArray.Create(localFunction.GetLocation()),
                    properties: null));
            }
        }
    }
}
