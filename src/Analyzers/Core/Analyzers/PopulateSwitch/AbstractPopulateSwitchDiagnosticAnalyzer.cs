// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(AnalyzersResources.Add_missing_cases), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(AnalyzersResources.Populate_switch), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        protected AbstractPopulateSwitchDiagnosticAnalyzer(string diagnosticId, EnforceOnBuild enforceOnBuild)
            : base(diagnosticId,
                   enforceOnBuild,
                   option: null,
                   s_localizableTitle, s_localizableMessage)
        {
        }

        #region Interface methods

        protected abstract OperationKind OperationKind { get; }

        protected abstract bool IsSwitchTypeUnknown(TSwitchOperation operation);
        protected abstract IOperation GetValueOfSwitchOperation(TSwitchOperation operation);

        protected abstract bool HasConstantCase(TSwitchOperation operation, object? value);
        protected abstract ICollection<ISymbol> GetMissingEnumMembers(TSwitchOperation operation);
        protected abstract bool HasDefaultCase(TSwitchOperation operation);
        protected abstract Location GetDiagnosticLocation(TSwitchSyntax switchBlock);

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            if (ShouldSkipAnalysis(context, notification: null))
                return;

            var switchOperation = (TSwitchOperation)context.Operation;
            if (switchOperation.Syntax is not TSwitchSyntax switchBlock || IsSwitchTypeUnknown(switchOperation))
                return;

            var tree = switchBlock.SyntaxTree;

            if (SwitchIsIncomplete(switchOperation, out var missingCases, out var missingDefaultCase) &&
                !tree.OverlapsHiddenPosition(switchBlock.Span, context.CancellationToken))
            {
                Debug.Assert(missingCases || missingDefaultCase);
                var properties = ImmutableDictionary<string, string?>.Empty
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
            if (!IsBooleanSwitch(operation, out missingCases, out missingDefaultCase))
            {
                var missingEnumMembers = GetMissingEnumMembers(operation);

                missingCases = missingEnumMembers.Count > 0;
                missingDefaultCase = !HasDefaultCase(operation);
            }

            // The switch is incomplete if we're missing any cases or we're missing a default case.
            return missingDefaultCase || missingCases;
        }

        private bool IsBooleanSwitch(TSwitchOperation operation, out bool missingCases, out bool missingDefaultCase)
        {
            missingCases = false;
            missingDefaultCase = false;

            var value = GetValueOfSwitchOperation(operation);
            var type = value.Type.RemoveNullableIfPresent();
            if (type is not { SpecialType: SpecialType.System_Boolean })
                return false;

            // If the switch already has a default case, then we don't have to offer the user anything.
            if (HasDefaultCase(operation))
            {
                missingDefaultCase = false;
            }
            else
            {
                // Doesn't have a default.  We don't want to offer that if they're already complete.
                var hasAllCases = HasConstantCase(operation, true) && HasConstantCase(operation, false);
                if (value.Type.IsNullable())
                    hasAllCases = hasAllCases && HasConstantCase(operation, null);

                missingDefaultCase = !hasAllCases;
            }

            return true;
        }

        protected static bool ConstantValueEquals(Optional<object?> constantValue, object? value)
            => constantValue.HasValue && Equals(constantValue.Value, value);
    }
}
