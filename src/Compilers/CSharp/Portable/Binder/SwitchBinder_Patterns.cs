// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // We use a subclass of SwitchBinder for the pattern-matching switch statement until we have completed
    // a totally compatible implementation of switch that also accepts pattern-matching constructs.
    internal partial class SwitchBinder : LocalScopeBinder
    {
        internal static SwitchBinder Create(Binder next, SwitchStatementSyntax switchSyntax)
        {
            return new SwitchBinder(next, switchSyntax);
        }

        /// <summary>
        /// Bind the switch statement, reporting in the process any switch labels that are subsumed by previous cases.
        /// </summary>
        internal override BoundStatement BindSwitchStatementCore(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            Debug.Assert(SwitchSyntax.Equals(node));

            if (node.Sections.Count == 0)
            {
                diagnostics.Add(ErrorCode.WRN_EmptySwitch, node.OpenBraceToken.GetLocation());
            }

            // Bind switch expression and set the switch governing type.
            BoundExpression boundSwitchGoverningExpression = SwitchGoverningExpression;
            diagnostics.AddRange(SwitchGoverningDiagnostics);

            ImmutableArray<BoundSwitchSection> switchSections = BindSwitchSections(originalBinder, diagnostics, out BoundPatternSwitchLabel defaultLabel);
            ImmutableArray<LocalSymbol> locals = GetDeclaredLocalsForScope(node);
            ImmutableArray<LocalFunctionSymbol> functions = GetDeclaredLocalFunctionsForScope(node);
            BoundDecisionDag decisionDag = DecisionDagBuilder.CreateDecisionDagForSwitchStatement(
                compilation: this.Compilation,
                syntax: node,
                switchGoverningExpression: boundSwitchGoverningExpression,
                switchSections: switchSections,
                // If there is no explicit default label, the default action is to break out of the switch
                defaultLabel: defaultLabel?.Label ?? BreakLabel,
                diagnostics);

            // Report subsumption errors, but ignore the input's constant value for that.
            CheckSwitchErrors(node, boundSwitchGoverningExpression, ref switchSections, decisionDag, diagnostics);

            // When the input is constant, we use that to reshape the decision dag that is returned
            // so that flow analysis will see that some of the cases may be unreachable.
            decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(boundSwitchGoverningExpression);

            return new BoundSwitchStatement(
                syntax: node,
                expression: boundSwitchGoverningExpression,
                innerLocals: locals,
                innerLocalFunctions: functions,
                switchSections: switchSections,
                defaultLabel: defaultLabel,
                breakLabel: this.BreakLabel,
                decisionDag: decisionDag);
        }

        private void CheckSwitchErrors(
            SwitchStatementSyntax node,
            BoundExpression boundSwitchGoverningExpression,
            ref ImmutableArray<BoundSwitchSection> switchSections,
            BoundDecisionDag decisionDag,
            DiagnosticBag diagnostics)
        {
            var reachableLabels = decisionDag.ReachableLabels;
            bool isSubsumed(BoundPatternSwitchLabel switchLabel)
            {
                return !reachableLabels.Contains(switchLabel.Label);
            }

            // If no switch sections are subsumed, just return
            if (!switchSections.Any(s => s.SwitchLabels.Any(l => isSubsumed(l))))
            {
                return;
            }

            var sectionBuilder = ArrayBuilder<BoundSwitchSection>.GetInstance(switchSections.Length);
            foreach (var oldSection in switchSections)
            {
                var labelBuilder = ArrayBuilder<BoundPatternSwitchLabel>.GetInstance(oldSection.SwitchLabels.Length);
                foreach (var label in oldSection.SwitchLabels)
                {
                    var newLabel = label;
                    if (!label.HasErrors && isSubsumed(label) && label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        var syntax = label.Syntax;
                        switch (syntax)
                        {
                            case CasePatternSwitchLabelSyntax p:
                                if (!p.Pattern.HasErrors)
                                {
                                    diagnostics.Add(ErrorCode.ERR_SwitchCaseSubsumed, p.Pattern.Location);
                                }
                                break;
                            case CaseSwitchLabelSyntax p:
                                if (label.Pattern is BoundConstantPattern cp && !cp.ConstantValue.IsBad && FindMatchingSwitchCaseLabel(cp.ConstantValue, p) != label.Label)
                                {
                                    // We use the traditional diagnostic when possible
                                    diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, syntax.Location, cp.ConstantValue.GetValueToDisplay());
                                }
                                else if (!label.Pattern.HasErrors)
                                {
                                    diagnostics.Add(ErrorCode.ERR_SwitchCaseSubsumed, p.Value.Location);
                                }
                                break;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                        }

                        // We mark any subsumed sections as erroneous for the benefit of flow analysis
                        newLabel = new BoundPatternSwitchLabel(label.Syntax, label.Label, label.Pattern, label.WhenClause, hasErrors: true);
                    }

                    labelBuilder.Add(newLabel);
                }

                sectionBuilder.Add(oldSection.Update(oldSection.Locals, labelBuilder.ToImmutableAndFree(), oldSection.Statements));
            }

            switchSections = sectionBuilder.ToImmutableAndFree();
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
        /// Bind the pattern switch labels.
        /// </summary>
        private ImmutableArray<BoundSwitchSection> BindSwitchSections(
            Binder originalBinder,
            DiagnosticBag diagnostics,
            out BoundPatternSwitchLabel defaultLabel)
        {
            // Bind match sections
            var boundPatternSwitchSectionsBuilder = ArrayBuilder<BoundSwitchSection>.GetInstance(SwitchSyntax.Sections.Count);
            defaultLabel = null;
            foreach (SwitchSectionSyntax sectionSyntax in SwitchSyntax.Sections)
            {
                BoundSwitchSection section = BindPatternSwitchSection(sectionSyntax, originalBinder, ref defaultLabel, diagnostics);
                boundPatternSwitchSectionsBuilder.Add(section);
            }

            return boundPatternSwitchSectionsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Bind the pattern switch section.
        /// </summary>
        private BoundSwitchSection BindPatternSwitchSection(
            SwitchSectionSyntax node,
            Binder originalBinder,
            ref BoundPatternSwitchLabel defaultLabel,
            DiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundPatternSwitchLabel>.GetInstance(node.Labels.Count);
            Binder sectionBinder = originalBinder.GetBinder(node); // this binder can bind pattern variables from the section.
            Debug.Assert(sectionBinder != null);
            Dictionary<SyntaxNode, LabelSymbol> labelsByNode = LabelsByNode;

            foreach (SwitchLabelSyntax labelSyntax in node.Labels)
            {
                LabelSymbol label = labelsByNode[labelSyntax];
                BoundPatternSwitchLabel boundLabel = BindPatternSwitchSectionLabel(sectionBinder, labelSyntax, label, ref defaultLabel, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(node.Statements.Count);
            foreach (StatementSyntax statement in node.Statements)
            {
                boundStatementsBuilder.Add(sectionBinder.BindStatement(statement, diagnostics));
            }

            return new BoundSwitchSection(node, sectionBinder.GetDeclaredLocalsForScope(node), boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        private BoundPatternSwitchLabel BindPatternSwitchSectionLabel(
            Binder sectionBinder,
            SwitchLabelSyntax node,
            LabelSymbol label,
            ref BoundPatternSwitchLabel defaultLabel,
            DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        BoundConstantPattern pattern = sectionBinder.BindConstantPattern(
                            node, SwitchGoverningType, caseLabelSyntax.Value, node.HasErrors, diagnostics, out bool wasExpression);
                        reportIfConstantNamedUnderscore(pattern, caseLabelSyntax.Value);
                        pattern.WasCompilerGenerated = true; // we don't have a pattern syntax here
                        bool hasErrors = pattern.HasErrors;
                        SyntaxNode innerValueSyntax = caseLabelSyntax.Value.SkipParens();
                        if (innerValueSyntax.Kind() == SyntaxKind.DefaultLiteralExpression)
                        {
                            diagnostics.Add(ErrorCode.ERR_DefaultInSwitch, innerValueSyntax.Location);
                            hasErrors = true;
                        }

                        return new BoundPatternSwitchLabel(node, label, pattern, null, hasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var defaultLabelSyntax = (DefaultSwitchLabelSyntax)node;
                        var pattern = new BoundDiscardPattern(node, SwitchGoverningType);
                        bool hasErrors = pattern.HasErrors;
                        if (defaultLabel != null)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, label.Name);
                            hasErrors = true;
                            return new BoundPatternSwitchLabel(node, label, pattern, null, hasErrors);
                        }
                        else
                        {
                            // Note that this is semantically last! The caller will place it in the decision dag
                            // in the final position.
                            return defaultLabel = new BoundPatternSwitchLabel(node, label, pattern, null, hasErrors);
                        }
                    }

                case SyntaxKind.CasePatternSwitchLabel:
                    {
                        var matchLabelSyntax = (CasePatternSwitchLabelSyntax)node;
                        BoundPattern pattern = sectionBinder.BindPattern(
                            matchLabelSyntax.Pattern, SwitchGoverningType, SwitchGoverningValEscape, node.HasErrors, diagnostics);
                        if (matchLabelSyntax.Pattern is ConstantPatternSyntax p)
                            reportIfConstantNamedUnderscore(pattern, p.Expression);

                        return new BoundPatternSwitchLabel(node, label, pattern,
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null,
                            node.HasErrors);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }

            void reportIfConstantNamedUnderscore(BoundPattern pattern, ExpressionSyntax expression)
            {
                if (!pattern.HasErrors &&
                    expression is IdentifierNameSyntax name && name.Identifier.ContextualKind() == SyntaxKind.UnderscoreToken)
                {
                    diagnostics.Add(ErrorCode.WRN_CaseConstantNamedUnderscore, expression.Location);
                }
            }
        }
    }
}
