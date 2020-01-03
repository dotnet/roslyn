// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchDiagnosticAnalyzer<TSwitchOperation, TSwitchSyntax> :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSwitchOperation : IOperation
        where TSwitchSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Add_missing_cases), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(WorkspacesResources.Populate_switch), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        protected AbstractPopulateSwitchDiagnosticAnalyzer(string diagnosticId)
            : base(diagnosticId,
                   option: null,
                   s_localizableTitle, s_localizableMessage)
        {
        }

        #region Interface methods

        protected abstract OperationKind OperationKind { get; }

        protected abstract ICollection<ISymbol> GetMissingEnumMembers(TSwitchOperation operation);
        protected abstract bool HasDefaultCase(TSwitchOperation operation);
        protected abstract Location GetDiagnosticLocation(TSwitchSyntax switchBlock);

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, this.OperationKind);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var switchOperation = (TSwitchOperation)context.Operation;
            if (!(switchOperation.Syntax is TSwitchSyntax switchBlock))
                return;

            var tree = switchBlock.SyntaxTree;

            if (SwitchIsIncomplete(switchOperation, out var missingCases, out var missingDefaultCase) &&
                !tree.OverlapsHiddenPosition(switchBlock.Span, context.CancellationToken))
            {
                Debug.Assert(missingCases || missingDefaultCase);
                var properties = ImmutableDictionary<string, string>.Empty
                    .Add(PopulateSwitchStatementHelpers.MissingCases, missingCases.ToString())
                    .Add(PopulateSwitchStatementHelpers.MissingDefaultCase, missingDefaultCase.ToString());

                var diagnostic = Diagnostic.Create(
                    Descriptor,
                    GetDiagnosticLocation(switchBlock),
                    properties: properties,
                    additionalLocations: new[] { switchBlock.GetLocation() });
                context.ReportDiagnostic(diagnostic);
            }
        }

        #endregion

        private bool SwitchIsIncomplete(
            TSwitchOperation operation,
            out bool missingCases, out bool missingDefaultCase)
        {
            var missingEnumMembers = GetMissingEnumMembers(operation);

            missingCases = missingEnumMembers.Count > 0;
            missingDefaultCase = !HasDefaultCase(operation);

            // The switch is incomplete if we're missing any cases or we're missing a default case.
            return missingDefaultCase || missingCases;
        }
    }
}
