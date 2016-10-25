// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // We use a subclass of SwitchBinder for the pattern-matching switch statement until we have completed
    // a totally compatible implementation of switch that also accepts pattern-matching constructs.
    internal partial class PatternSwitchBinder : SwitchBinder
    {
        internal PatternSwitchBinder(Binder next, SwitchStatementSyntax switchSyntax) : base(next, switchSyntax)
        {
        }

        /// <summary>
        /// When pattern-matching is enabled, we use a completely different binder and binding
        /// strategy for switch statements. Once we have confirmed that it is totally upward
        /// compatible with the existing syntax and semantics, we will merge them completely.
        /// However, until we have edit-and-continue working, we continue using the old binder
        /// when we can.
        /// </summary>
        private bool UseV7SwitchBinder
        {
            get
            {
                    var parseOptions = SwitchSyntax?.SyntaxTree?.Options as CSharpParseOptions;
                    return
                        parseOptions?.Features.ContainsKey("testV7SwitchBinder") == true ||
                        HasPatternSwitchSyntax(SwitchSyntax) ||
                        !SwitchGoverningType.IsValidV6SwitchGoverningType();
            }
        }

        private static bool HasPatternSwitchSyntax(SwitchStatementSyntax switchSyntax)
        {
            foreach (var section in switchSyntax.Sections)
            {
                if (section.Labels.Any(SyntaxKind.CasePatternSwitchLabel))
                {
                    return true;
                }
            }

            return false;
        }

        internal override BoundStatement BindSwitchExpressionAndSections(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // If it is a valid C# 6 switch statement, we use the old binder to bind it.
            if (!UseV7SwitchBinder) return base.BindSwitchExpressionAndSections(node, originalBinder, diagnostics);

            Debug.Assert(SwitchSyntax.Equals(node));

            // Bind switch expression and set the switch governing type.
            var boundSwitchExpression = SwitchGoverningExpression;
            diagnostics.AddRange(SwitchGoverningDiagnostics);

            BoundPatternSwitchLabel defaultLabel;
            bool isComplete;
            ImmutableArray<BoundPatternSwitchSection> switchSections =
                BindPatternSwitchSections(boundSwitchExpression, node.Sections, originalBinder, out defaultLabel, out isComplete, diagnostics);
            var locals = GetDeclaredLocalsForScope(node);
            var functions = GetDeclaredLocalFunctionsForScope(node);
            return new BoundPatternSwitchStatement(
                node, boundSwitchExpression,
                locals, functions, switchSections, defaultLabel, this.BreakLabel, this, isComplete);
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
                boundSwitchExpression: SwitchGoverningExpression,
                node: node,
                label: LabelsByNode[node],
                defaultLabel: ref defaultLabel,
                diagnostics: diagnostics);
        }

        private ImmutableArray<BoundPatternSwitchSection> BindPatternSwitchSections(
            BoundExpression boundSwitchExpression,
            SyntaxList<SwitchSectionSyntax> sections,
            Binder originalBinder,
            out BoundPatternSwitchLabel defaultLabel,
            out bool isComplete,
            DiagnosticBag diagnostics)
        {
            defaultLabel = null;

            // true if we found a case label whose value is the same as the input expression's constant value
            bool someValueMatched = false;

            // Bind match sections
            var boundPatternSwitchSectionsBuilder = ArrayBuilder<BoundPatternSwitchSection>.GetInstance();
            SubsumptionDiagnosticBuilder subsumption = new SubsumptionDiagnosticBuilder(ContainingMemberOrLambda, this.Conversions, boundSwitchExpression);
            foreach (var sectionSyntax in sections)
            {
                boundPatternSwitchSectionsBuilder.Add(BindPatternSwitchSection(
                    boundSwitchExpression, sectionSyntax, originalBinder, ref defaultLabel, ref someValueMatched, subsumption, diagnostics));
            }

            isComplete = defaultLabel != null || subsumption.IsComplete || someValueMatched;
            return boundPatternSwitchSectionsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Bind the pattern switch section, producing subsumption diagnostics.
        /// </summary>
        /// <param name="boundSwitchExpression"/>
        /// <param name="node"/>
        /// <param name="originalBinder"/>
        /// <param name="defaultLabel">If a default label is found in this section, assigned that label</param>
        /// <param name="someValueMatched">If a constant label is found that matches the constant input, assigned that label</param>
        /// <param name="subsumption">A helper class that uses a decision tree to produce subsumption diagnostics.</param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private BoundPatternSwitchSection BindPatternSwitchSection(
            BoundExpression boundSwitchExpression,
            SwitchSectionSyntax node,
            Binder originalBinder,
            ref BoundPatternSwitchLabel defaultLabel,
            ref bool someValueMatched,
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
                BoundPatternSwitchLabel boundLabel = BindPatternSwitchSectionLabel(sectionBinder, boundSwitchExpression, labelSyntax, label, ref defaultLabel, diagnostics);
                bool valueMatched; // true if we find an unconditional constant label that matches the input constant's value
                bool isReachable = subsumption.AddLabel(boundLabel, diagnostics, out valueMatched);
                boundLabel = boundLabel.Update(boundLabel.Label, boundLabel.Pattern, boundLabel.Guard, isReachable && !someValueMatched);
                someValueMatched |= valueMatched;
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
            Binder sectionBinder, BoundExpression boundSwitchExpression, SwitchLabelSyntax node, LabelSymbol label, ref BoundPatternSwitchLabel defaultLabel, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        bool wasExpression;
                        var pattern = sectionBinder.BindConstantPattern(
                            node, boundSwitchExpression, boundSwitchExpression.Type, caseLabelSyntax.Value, node.HasErrors, diagnostics, out wasExpression, wasSwitchCase: true);
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

                        // Until we've determined whether or not the switch label is reachable, we assume it
                        // is. The caller updates isReachable after determining if the label is subsumed.
                        const bool isReachable = true;
                        return new BoundPatternSwitchLabel(node, label, pattern, null, isReachable, hasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var defaultLabelSyntax = (DefaultSwitchLabelSyntax)node;
                        var pattern = new BoundWildcardPattern(node);
                        bool hasErrors = pattern.HasErrors;
                        if (defaultLabel != null)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, label.Name);
                            hasErrors = true;
                        }

                        // We always treat the default label as reachable, even if the switch is complete.
                        const bool isReachable = true;

                        // Note that this is semantically last! The caller will place it in the decision tree
                        // in the final position.
                        defaultLabel = new BoundPatternSwitchLabel(node, label, pattern, null, isReachable, hasErrors);
                        return defaultLabel;
                    }

                case SyntaxKind.CasePatternSwitchLabel:
                    {
                        var matchLabelSyntax = (CasePatternSwitchLabelSyntax)node;
                        var pattern = sectionBinder.BindPattern(
                            matchLabelSyntax.Pattern, boundSwitchExpression, boundSwitchExpression.Type, node.HasErrors, diagnostics, wasSwitchCase: true);
                        return new BoundPatternSwitchLabel(node, label, pattern,
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null,
                            true, node.HasErrors);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }
    }
}
