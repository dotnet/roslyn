// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // We use a subclass of SwitchBinder for the pattern-matching switch statement until we have completed
    // a totally compatible implementation of switch that also accepts pattern-matching constructs.
    internal partial class PatternSwitchBinder2 : SwitchBinder
    {
        internal PatternSwitchBinder2(Binder next, SwitchStatementSyntax switchSyntax) : base(next, switchSyntax)
        {
        }

        /// <summary>
        /// When pattern-matching is enabled, we use a completely different binder and binding
        /// strategy for switch statements. Once we have confirmed that it is totally upward
        /// compatible with the existing syntax and semantics, we will merge them completely.
        /// However, until we have edit-and-continue working, we continue using the old binder
        /// when we can.
        /// </summary>
        private bool UseV8SwitchBinder
        {
            get
            {
                    var parseOptions = SwitchSyntax?.SyntaxTree?.Options as CSharpParseOptions;
                    return
                        parseOptions?.Features.ContainsKey("testV8SwitchBinder") == true ||
                        HasPatternSwitchSyntax(SwitchSyntax) ||
                        !SwitchGoverningType.IsValidV6SwitchGoverningType();
            }
        }

        internal override BoundStatement BindSwitchExpressionAndSections(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // If it is a valid C# 6 switch statement, we use the old binder to bind it.
            if (!UseV8SwitchBinder) return base.BindSwitchExpressionAndSections(node, originalBinder, diagnostics);

            Debug.Assert(SwitchSyntax.Equals(node));

            // Bind switch expression and set the switch governing type.
            var boundSwitchExpression = SwitchGoverningExpression;
            diagnostics.AddRange(SwitchGoverningDiagnostics);

            BoundPatternSwitchLabel defaultLabel;
            bool isComplete;
            ImmutableArray<BoundPatternSwitchSection> switchSections =
                BindPatternSwitchSections(originalBinder, out defaultLabel, out isComplete, out var someCaseMatches, diagnostics);
            var locals = GetDeclaredLocalsForScope(node);
            var functions = GetDeclaredLocalFunctionsForScope(node);
            BoundDecisionDag decisionDag = null; // we'll compute it later
            return new BoundPatternSwitchStatement(
                syntax: node,
                expression: boundSwitchExpression,
                someLabelAlwaysMatches: someCaseMatches,
                innerLocals: locals,
                innerLocalFunctions: functions,
                switchSections: switchSections,
                defaultLabel: defaultLabel,
                breakLabel: this.BreakLabel,
                decisionDag: decisionDag,
                isComplete: isComplete);
        }

        /// <summary>
        /// Bind a pattern switch label in order to force inference of the type of pattern variables.
        /// </summary>
        internal override void BindPatternSwitchLabelForInference(CasePatternSwitchLabelSyntax node, DiagnosticBag diagnostics)
        {
            // node should be a label of this switch statement.
            Debug.Assert(this.SwitchSyntax == node.Parent.Parent);

            // This simulates enough of the normal binding path of a switch statement to cause
            // the label's pattern variables to have their types inferred, if necessary.
            // It also binds the when clause, and therefore any pattern and out variables there.
            BoundPatternSwitchLabel defaultLabel = null;
            BindPatternSwitchSectionLabel(
                sectionBinder: GetBinder(node.Parent),
                node: node,
                label: LabelsByNode[node],
                defaultLabel: ref defaultLabel,
                diagnostics: diagnostics);
        }

        /// <summary>
        /// Bind the pattern switch labels, reporting in the process which cases are subsumed. The strategy
        /// implemented with the help of <see cref="SubsumptionDiagnosticBuilder"/>, is to start with an empty
        /// decision tree, and for each case we visit the decision tree to see if the case is subsumed. If it
        /// is, we report an error. If it is not subsumed and there is no guard expression, we then add it to
        /// the decision tree.
        /// </summary>
        private ImmutableArray<BoundPatternSwitchSection> BindPatternSwitchSections(
            Binder originalBinder,
            out BoundPatternSwitchLabel defaultLabel,
            out bool isComplete,
            out bool someCaseMatches,
            DiagnosticBag diagnostics)
        {
            defaultLabel = null;

            // someCaseMatches will be set to true if some single case label would handle all inputs
            someCaseMatches = false;

            // Bind match sections
            var boundPatternSwitchSectionsBuilder = ArrayBuilder<BoundPatternSwitchSection>.GetInstance();
            SubsumptionDiagnosticBuilder subsumption = new SubsumptionDiagnosticBuilder(ContainingMemberOrLambda, SwitchSyntax, this.Conversions, SwitchGoverningType);
            foreach (var sectionSyntax in SwitchSyntax.Sections)
            {
                var section = BindPatternSwitchSection(sectionSyntax, originalBinder, ref defaultLabel, ref someCaseMatches, subsumption, diagnostics);
                boundPatternSwitchSectionsBuilder.Add(section);
            }

            isComplete = defaultLabel != null || subsumption.IsComplete || someCaseMatches;
            return boundPatternSwitchSectionsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Bind the pattern switch section, producing subsumption diagnostics.
        /// </summary>
        /// <param name="node"/>
        /// <param name="originalBinder"/>
        /// <param name="defaultLabel">If a default label is found in this section, assigned that label</param>
        /// <param name="someCaseMatches">If a case is found that would always match the input, set to true</param>
        /// <param name="subsumption">A helper class that uses a decision tree to produce subsumption diagnostics.</param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private BoundPatternSwitchSection BindPatternSwitchSection(
            SwitchSectionSyntax node,
            Binder originalBinder,
            ref BoundPatternSwitchLabel defaultLabel,
            ref bool someCaseMatches,
            SubsumptionDiagnosticBuilder subsumption,
            DiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundPatternSwitchLabel>.GetInstance();
            var sectionBinder = originalBinder.GetBinder(node); // this binder can bind pattern variables from the section.
            Debug.Assert(sectionBinder != null);
            var labelsByNode = LabelsByNode;

            foreach (var labelSyntax in node.Labels)
            {
                LabelSymbol label = labelsByNode[labelSyntax];
                BoundPatternSwitchLabel boundLabel = BindPatternSwitchSectionLabel(sectionBinder, labelSyntax, label, ref defaultLabel, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (var statement in node.Statements)
            {
                boundStatementsBuilder.Add(sectionBinder.BindStatement(statement, diagnostics));
            }

            return new BoundPatternSwitchSection(node, sectionBinder.GetDeclaredLocalsForScope(node), boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        private BoundPatternSwitchLabel BindPatternSwitchSectionLabel(
            Binder sectionBinder,
            SwitchLabelSyntax node,
            LabelSymbol label,
            ref BoundPatternSwitchLabel defaultLabel,
            DiagnosticBag diagnostics)
        {
            // Until we've determined whether or not the switch label is reachable, we assume it
            // is. The caller updates isReachable after determining if the label is subsumed.
            const bool isReachable = true;

            switch (node.Kind())
            {
                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        bool wasExpression;
                        var pattern = sectionBinder.BindConstantPattern(
                            node, SwitchGoverningType, caseLabelSyntax.Value, node.HasErrors, diagnostics, out wasExpression);
                        bool hasErrors = pattern.HasErrors;
                        var constantValue = pattern.ConstantValue;
                        if (!hasErrors &&
                            (object)constantValue != null &&
                            pattern.Value.Type == SwitchGoverningType &&
                            this.FindMatchingSwitchCaseLabel(constantValue, caseLabelSyntax) != label)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, pattern.ConstantValue.GetValueToDisplay() ?? label.Name);
                            hasErrors = true;
                        }

                        if (caseLabelSyntax.Value.Kind() == SyntaxKind.DefaultLiteralExpression)
                        {
                            diagnostics.Add(ErrorCode.WRN_DefaultInSwitch, caseLabelSyntax.Value.Location);
                        }

                        return new BoundPatternSwitchLabel(node, label, pattern, null, isReachable, hasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var defaultLabelSyntax = (DefaultSwitchLabelSyntax)node;
                        var pattern = new BoundDiscardPattern(node);
                        bool hasErrors = pattern.HasErrors;
                        if (defaultLabel != null)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, label.Name);
                            hasErrors = true;
                        }

                        // Note that this is semantically last! The caller will place it in the decision tree
                        // in the final position.
                        defaultLabel = new BoundPatternSwitchLabel(node, label, pattern, null, isReachable, hasErrors);
                        return defaultLabel;
                    }

                case SyntaxKind.CasePatternSwitchLabel:
                    {
                        var matchLabelSyntax = (CasePatternSwitchLabelSyntax)node;
                        var pattern = sectionBinder.BindPattern(
                            matchLabelSyntax.Pattern, SwitchGoverningType, node.HasErrors, diagnostics);
                        return new BoundPatternSwitchLabel(node, label, pattern,
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null,
                            isReachable, node.HasErrors);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }
    }
}
