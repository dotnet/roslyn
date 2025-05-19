// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal partial class SwitchBinder : LocalScopeBinder
    {
        internal static SwitchBinder Create(Binder next, SwitchStatementSyntax switchSyntax)
        {
            return new SwitchBinder(next, switchSyntax);
        }

        /// <summary>
        /// Bind the switch statement, reporting in the process any switch labels that are subsumed by previous cases.
        /// </summary>
        internal override BoundStatement BindSwitchStatementCore(SwitchStatementSyntax node, Binder originalBinder, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(SwitchSyntax.Equals(node));

            if (node.Sections.Count == 0)
            {
                diagnostics.Add(ErrorCode.WRN_EmptySwitch, node.OpenBraceToken.GetLocation());
            }

            // Bind switch expression and set the switch governing type.
            BoundExpression boundSwitchGoverningExpression = SwitchGoverningExpression;
            diagnostics.AddRange(SwitchGoverningDiagnostics, allowMismatchInDependencyAccumulation: true);

            ImmutableArray<BoundSwitchSection> switchSections = BindSwitchSections(originalBinder, diagnostics, out BoundSwitchLabel defaultLabel);
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
            CheckSwitchErrors(ref switchSections, decisionDag, diagnostics);

            // When the input is constant, we use that to reshape the decision dag that is returned
            // so that flow analysis will see that some of the cases may be unreachable.
            decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(boundSwitchGoverningExpression);

            return new BoundSwitchStatement(
                syntax: node,
                expression: boundSwitchGoverningExpression,
                innerLocals: locals,
                innerLocalFunctions: ImmutableArray<MethodSymbol>.CastUp(functions),
                switchSections: switchSections,
                defaultLabel: defaultLabel,
                breakLabel: this.BreakLabel,
                reachabilityDecisionDag: decisionDag);
        }

        private void CheckSwitchErrors(
            ref ImmutableArray<BoundSwitchSection> switchSections,
            BoundDecisionDag decisionDag,
            BindingDiagnosticBag diagnostics)
        {
            var reachableLabels = decisionDag.ReachableLabels;
            static bool isSubsumed(BoundSwitchLabel switchLabel, ImmutableHashSet<LabelSymbol> reachableLabels)
            {
                return !reachableLabels.Contains(switchLabel.Label);
            }

            // If no switch sections are subsumed, just return
            if (!switchSections.Any(static (s, reachableLabels) => s.SwitchLabels.Any(isSubsumed, reachableLabels), reachableLabels))
            {
                return;
            }

            var sectionBuilder = ArrayBuilder<BoundSwitchSection>.GetInstance(switchSections.Length);
            bool anyPreviousErrors = false;
            foreach (var oldSection in switchSections)
            {
                var labelBuilder = ArrayBuilder<BoundSwitchLabel>.GetInstance(oldSection.SwitchLabels.Length);
                foreach (var label in oldSection.SwitchLabels)
                {
                    var newLabel = label;
                    if (!label.HasErrors && isSubsumed(label, reachableLabels) && label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        var syntax = label.Syntax;
                        switch (syntax)
                        {
                            case CasePatternSwitchLabelSyntax p:
                                if (!p.Pattern.HasErrors && !anyPreviousErrors)
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
                                else if (!label.Pattern.HasErrors && !anyPreviousErrors)
                                {
                                    diagnostics.Add(ErrorCode.ERR_SwitchCaseSubsumed, p.Value.Location);
                                }
                                break;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                        }

                        // We mark any subsumed sections as erroneous for the benefit of flow analysis
                        newLabel = new BoundSwitchLabel(label.Syntax, label.Label, label.Pattern, label.WhenClause, hasErrors: true);
                    }

                    anyPreviousErrors |= label.HasErrors;
                    labelBuilder.Add(newLabel);
                }

                sectionBuilder.Add(oldSection.Update(oldSection.Locals, labelBuilder.ToImmutableAndFree(), oldSection.Statements));
            }

            switchSections = sectionBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Bind a pattern switch label in order to force inference of the type of pattern variables.
        /// </summary>
        internal override void BindPatternSwitchLabelForInference(CasePatternSwitchLabelSyntax node, BindingDiagnosticBag diagnostics)
        {
            // node should be a label of this switch statement.
            Debug.Assert(this.SwitchSyntax == node.Parent.Parent);

            // This simulates enough of the normal binding path of a switch statement to cause
            // the label's pattern variables to have their types inferred, if necessary.
            // It also binds the when clause, and therefore any pattern and out variables there.
            BoundSwitchLabel defaultLabel = null;
            BindSwitchSectionLabel(
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
            BindingDiagnosticBag diagnostics,
            out BoundSwitchLabel defaultLabel)
        {
            // Bind match sections
            var boundSwitchSectionsBuilder = ArrayBuilder<BoundSwitchSection>.GetInstance(SwitchSyntax.Sections.Count);
            defaultLabel = null;
            foreach (SwitchSectionSyntax sectionSyntax in SwitchSyntax.Sections)
            {
                BoundSwitchSection section = BindSwitchSection(sectionSyntax, originalBinder, ref defaultLabel, diagnostics);
                boundSwitchSectionsBuilder.Add(section);
            }

            return boundSwitchSectionsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Bind the pattern switch section.
        /// </summary>
        private BoundSwitchSection BindSwitchSection(
            SwitchSectionSyntax node,
            Binder originalBinder,
            ref BoundSwitchLabel defaultLabel,
            BindingDiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundSwitchLabel>.GetInstance(node.Labels.Count);
            Binder sectionBinder = originalBinder.GetBinder(node); // this binder can bind pattern variables from the section.
            Debug.Assert(sectionBinder != null);
            Dictionary<SyntaxNode, LabelSymbol> labelsByNode = LabelsByNode;

            foreach (SwitchLabelSyntax labelSyntax in node.Labels)
            {
                LabelSymbol label = labelsByNode[labelSyntax];
                BoundSwitchLabel boundLabel = BindSwitchSectionLabel(sectionBinder, labelSyntax, label, ref defaultLabel, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(node.Statements.Count);
            foreach (StatementSyntax statement in node.Statements)
            {
                var boundStatement = sectionBinder.BindStatement(statement, diagnostics);
                if (ContainsUsingVariable(boundStatement))
                {
                    diagnostics.Add(ErrorCode.ERR_UsingVarInSwitchCase, statement.Location);
                }
                boundStatementsBuilder.Add(boundStatement);
            }

            return new BoundSwitchSection(node, sectionBinder.GetDeclaredLocalsForScope(node), boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        internal static bool ContainsUsingVariable(BoundStatement boundStatement)
        {
            if (boundStatement is BoundLocalDeclaration boundLocal)
            {
                return boundLocal.LocalSymbol.IsUsing;
            }
            else if (boundStatement is BoundMultipleLocalDeclarationsBase boundMultiple && !boundMultiple.LocalDeclarations.IsDefaultOrEmpty)
            {
                return boundMultiple.LocalDeclarations[0].LocalSymbol.IsUsing;
            }
            return false;
        }

        private BoundSwitchLabel BindSwitchSectionLabel(
            Binder sectionBinder,
            SwitchLabelSyntax node,
            LabelSymbol label,
            ref BoundSwitchLabel defaultLabel,
            BindingDiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        bool hasErrors = node.HasErrors;
                        BoundPattern pattern = sectionBinder.BindConstantPatternWithFallbackToTypePattern(
                            caseLabelSyntax.Value, caseLabelSyntax.Value, SwitchGoverningType, hasErrors, diagnostics);
                        pattern.WasCompilerGenerated = true; // we don't have a pattern syntax here
                        reportIfConstantNamedUnderscore(pattern, caseLabelSyntax.Value);

                        return new BoundSwitchLabel(node, label, pattern, null, pattern.HasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var pattern = new BoundDiscardPattern(node, inputType: SwitchGoverningType, narrowedType: SwitchGoverningType);
                        bool hasErrors = pattern.HasErrors;
                        if (defaultLabel != null)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, label.Name);
                            hasErrors = true;
                            return new BoundSwitchLabel(node, label, pattern, null, hasErrors);
                        }
                        else
                        {
                            // Note that this is semantically last! The caller will place it in the decision dag
                            // in the final position.
                            return defaultLabel = new BoundSwitchLabel(node, label, pattern, null, hasErrors);
                        }
                    }

                case SyntaxKind.CasePatternSwitchLabel:
                    {
                        var matchLabelSyntax = (CasePatternSwitchLabelSyntax)node;

                        MessageID.IDS_FeaturePatternMatching.CheckFeatureAvailability(diagnostics, node.Keyword);

                        BoundPattern pattern = sectionBinder.BindPattern(
                            matchLabelSyntax.Pattern, SwitchGoverningType, permitDesignations: true, node.HasErrors, diagnostics);
                        if (matchLabelSyntax.Pattern is ConstantPatternSyntax p)
                            reportIfConstantNamedUnderscore(pattern, p.Expression);

                        return new BoundSwitchLabel(node, label, pattern,
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null,
                            node.HasErrors);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }

            void reportIfConstantNamedUnderscore(BoundPattern pattern, ExpressionSyntax expression)
            {
                if (pattern is BoundConstantPattern { HasErrors: false } && IsUnderscore(expression))
                {
                    diagnostics.Add(ErrorCode.WRN_CaseConstantNamedUnderscore, expression.Location);
                }
            }
        }
    }
}
