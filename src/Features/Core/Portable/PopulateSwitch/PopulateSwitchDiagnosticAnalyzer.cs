// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class PopulateSwitchDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Add_missing_cases), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.Populate_switch), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        public PopulateSwitchDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.PopulateSwitchDiagnosticId,
                   option: null,
                   s_localizableTitle, s_localizableMessage)
        {
        }

        #region Interface methods

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Switch);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var switchOperation = (ISwitchOperation)context.Operation;
            var switchBlock = switchOperation.Syntax;
            var tree = switchBlock.SyntaxTree;

            if (SwitchIsIncomplete(switchOperation, out var missingCases, out var missingDefaultCase) &&
                !tree.OverlapsHiddenPosition(switchBlock.Span, context.CancellationToken))
            {
                Debug.Assert(missingCases || missingDefaultCase);
                var properties = ImmutableDictionary<string, string>.Empty
                    .Add(PopulateSwitchHelpers.MissingCases, missingCases.ToString())
                    .Add(PopulateSwitchHelpers.MissingDefaultCase, missingDefaultCase.ToString());

                var diagnostic = Diagnostic.Create(
                    Descriptor,
                    switchBlock.GetFirstToken().GetLocation(),
                    properties: properties,
                    additionalLocations: new[] { switchBlock.GetLocation() });
                context.ReportDiagnostic(diagnostic);
            }
        }

        #endregion

        private bool SwitchIsIncomplete(
            ISwitchOperation switchStatement,
            out bool missingCases, out bool missingDefaultCase)
        {
            var missingEnumMembers = PopulateSwitchHelpers.GetMissingEnumMembers(switchStatement);

            missingCases = missingEnumMembers.Count > 0;
            missingDefaultCase = !PopulateSwitchHelpers.HasDefaultCase(switchStatement);

            // The switch is incomplete if we're missing any cases or we're missing a default case.
            return missingDefaultCase || missingCases;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
