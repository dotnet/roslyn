// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        /// <summary>
        /// Bind the switch statement, reporting in the process any switch labels that are subsumed by previous cases.
        /// </summary>
        internal override BoundStatement BindSwitchStatementCore(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // If it is a valid C# 6 switch statement, we use the old binder to bind it.
            if (!UseV8SwitchBinder) return base.BindSwitchStatementCore(node, originalBinder, diagnostics);

            Debug.Assert(SwitchSyntax.Equals(node));

            // Bind switch expression and set the switch governing type.
            BoundExpression boundSwitchGoverningExpression = SwitchGoverningExpression;
            diagnostics.AddRange(SwitchGoverningDiagnostics);

            ImmutableArray<BoundPatternSwitchSection> switchSections = BindPatternSwitchSections(originalBinder, diagnostics, out BoundPatternSwitchLabel defaultLabel);
            ImmutableArray<LocalSymbol> locals = GetDeclaredLocalsForScope(node);
            ImmutableArray<LocalFunctionSymbol> functions = GetDeclaredLocalFunctionsForScope(node);
            BoundDecisionDag decisionDag = DecisionDagBuilder.CreateDecisionDag(
                compilation: this.Compilation,
                syntax: node,
                switchGoverningExpression: boundSwitchGoverningExpression,
                switchSections: switchSections,
                // If there is no explicit default label, the default action is to break out of the switch
                defaultLabel: defaultLabel?.Label ?? BreakLabel,
                diagnostics);
            // Report subsumption errors, but ignore the input's constant value for that.
            bool hasErrors = CheckSwitchErrors(node, boundSwitchGoverningExpression, ref switchSections, decisionDag, diagnostics);

            if (boundSwitchGoverningExpression.ConstantValue != null)
            {
                // But when the input is constant, we use that to reshape the decision dag that is returned
                // so that flow analysis will see that some of the cases may be unreachable.
                decisionDag = SimplifyDecisionDagForConstantInput(decisionDag, boundSwitchGoverningExpression, diagnostics);
            }

            return new BoundPatternSwitchStatement(
                syntax: node,
                expression: boundSwitchGoverningExpression,
                innerLocals: locals,
                innerLocalFunctions: functions,
                switchSections: switchSections,
                defaultLabel: defaultLabel,
                breakLabel: this.BreakLabel,
                decisionDag: decisionDag,
                hasErrors: hasErrors);
        }

        private bool CheckSwitchErrors(
            SwitchStatementSyntax node,
            BoundExpression boundSwitchGoverningExpression,
            ref ImmutableArray<BoundPatternSwitchSection> switchSections,
            BoundDecisionDag decisionDag,
            DiagnosticBag diagnostics)
        {
            HashSet<LabelSymbol> reachableLabels = decisionDag.ReachableLabels;
            bool areAnySubsumed(ImmutableArray<BoundPatternSwitchSection> sections)
            {
                foreach (BoundPatternSwitchSection section in sections)
                {
                    foreach (BoundPatternSwitchLabel label in section.SwitchLabels)
                    {
                        if (!label.HasErrors && !reachableLabels.Contains(label.Label))
                        {
                            // we found a label that is subsumed
                            return true;
                        }
                    }
                }

                return false;
            }

            if (!areAnySubsumed(switchSections))
            {
                return false;
            }

            // We mark any subsumed sections as erroneous for the benefit of flow analysis
            var sectionBuilder = ArrayBuilder<BoundPatternSwitchSection>.GetInstance();
            foreach (var oldSection in switchSections)
            {
                var labelBuilder = ArrayBuilder<BoundPatternSwitchLabel>.GetInstance();
                foreach (var label in oldSection.SwitchLabels)
                {
                    var newLabel = label;
                    if (!label.HasErrors && !reachableLabels.Contains(label.Label) && label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        var syntax = label.Syntax;
                        switch (syntax)
                        {
                            case CasePatternSwitchLabelSyntax p:
                                syntax = p.Pattern;
                                break;
                            case CaseSwitchLabelSyntax p:
                                syntax = p.Value;
                                break;
                        }

                        diagnostics.Add(ErrorCode.ERR_SwitchCaseSubsumed, syntax.Location);
                        newLabel = new BoundPatternSwitchLabel(label.Syntax, label.Label, label.Pattern, label.Guard, hasErrors: true);
                    }

                    labelBuilder.Add(newLabel);
                }

                sectionBuilder.Add(oldSection.Update(oldSection.Locals, labelBuilder.ToImmutableAndFree(), oldSection.Statements));
            }

            switchSections = sectionBuilder.ToImmutableAndFree();
            return true;
        }

        /// <summary>
        /// Given a decision dag and a constant-valued input, produce a simplified decision dag that has removed all the
        /// tests that are unnecessary due to that constant value. This simplification affects flow analysis (reachability
        /// and definite assignment) and permits us to simplify the generated code.
        /// </summary>
        private BoundDecisionDag SimplifyDecisionDagForConstantInput(
            BoundDecisionDag decisionDag,
            BoundExpression input,
            DiagnosticBag diagnostics)
        {
            ConstantValue inputConstant = input.ConstantValue;
            Debug.Assert(inputConstant != null);

            // First, we topologically sort the nodes of the dag so that we can translate the nodes bottom-up.
            // This will avoid overflowing the compiler's runtime stack which would occur for a large switch
            // statement if we were using a recursive strategy.
            ImmutableArray<BoundDecisionDag> sortedNodes = decisionDag.TopologicallySortedNodes();

            // Cache simplified/translated replacement for each translated dag node
            var replacementCache = PooledDictionary<BoundDecisionDag, BoundDecisionDag>.GetInstance();

            // Loop backwards through the topologically sorted nodes to translate them, so that we always visit a node after its successors
            for (int i = sortedNodes.Length - 1; i >= 0; i--)
            {
                BoundDecisionDag node = sortedNodes[i];
                Debug.Assert(!replacementCache.ContainsKey(node));
                BoundDecisionDag newNode = makeReplacement(node);
                replacementCache.Add(node, newNode);
            }

            var result = replacement(decisionDag);
            replacementCache.Free();
            return result;

            // Return a cached replacement node. Since we always visit a node's succesors before the node,
            // the replacement should always be in the cache when we need it.
            BoundDecisionDag replacement(BoundDecisionDag dag)
            {
                if (replacementCache.TryGetValue(dag, out BoundDecisionDag knownReplacement))
                {
                    return knownReplacement;
                }

                throw ExceptionUtilities.Unreachable;
            }

            // Make a replacement for a given node, using the precomputed replacements for its successors.
            BoundDecisionDag makeReplacement(BoundDecisionDag dag)
            {
                switch (dag)
                {
                    case BoundEvaluationPoint p:
                        return p.Update(p.Evaluation, replacement(p.Next));
                    case BoundDecisionPoint p:
                        // This is the key to the optimization. The result of a top-level test might be known if the input is constant.
                        switch (knownResult(p.Decision))
                        {
                            case true:
                                return replacement(p.WhenTrue);
                            case false:
                                return replacement(p.WhenFalse);
                            default:
                                return p.Update(p.Decision, replacement(p.WhenTrue), replacement(p.WhenFalse));
                        }
                    case BoundWhenClause p:
                        if (p.WhenExpression == null || p.WhenExpression.ConstantValue == ConstantValue.True)
                        {
                            return p.Update(p.Bindings, p.WhenExpression, makeReplacement(p.WhenTrue), null);
                        }
                        else if (p.WhenExpression.ConstantValue == ConstantValue.False)
                        {
                            // It is possible in this case that we could eliminate some predecessor nodes, for example
                            // those that compute evaluations only needed to get to this decision. We do not bother,
                            // as that optimization would only be likely to affect test code.
                            return replacement(p.WhenFalse);
                        }
                        else
                        {
                            return p.Update(p.Bindings, p.WhenExpression, replacement(p.WhenTrue), replacement(p.WhenFalse));
                        }
                    case BoundDecision p:
                        return p;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(dag);
                }
            }

            // Is the decision 
            bool? knownResult(BoundDagDecision choice)
            {
                if (choice.Input.Source != null)
                {
                    // This is a test of something other than the main input; result unknown
                    return null;
                }

                switch (choice)
                {
                    case BoundNullValueDecision d:
                        return inputConstant.IsNull;
                    case BoundNonNullDecision d:
                        return !inputConstant.IsNull;
                    case BoundNonNullValueDecision d:
                        return d.Value == inputConstant;
                    case BoundTypeDecision d:
                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        bool? known = ExpressionOfTypeMatchesPatternType(Conversions, input.Type, d.Type, ref useSiteDiagnostics, out Conversion conversion, inputConstant, inputConstant.IsNull);
                        diagnostics.Add(d.Syntax, useSiteDiagnostics);
                        return known;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(choice);
                }
            }

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
        private ImmutableArray<BoundPatternSwitchSection> BindPatternSwitchSections(
            Binder originalBinder,
            DiagnosticBag diagnostics,
            out BoundPatternSwitchLabel defaultLabel)
        {
            // Bind match sections
            var boundPatternSwitchSectionsBuilder = ArrayBuilder<BoundPatternSwitchSection>.GetInstance(SwitchSyntax.Sections.Count);
            defaultLabel = null;
            foreach (SwitchSectionSyntax sectionSyntax in SwitchSyntax.Sections)
            {
                BoundPatternSwitchSection section = BindPatternSwitchSection(sectionSyntax, originalBinder, ref defaultLabel, diagnostics);
                boundPatternSwitchSectionsBuilder.Add(section);
            }

            return boundPatternSwitchSectionsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Bind the pattern switch section.
        /// </summary>
        private BoundPatternSwitchSection BindPatternSwitchSection(
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

            return new BoundPatternSwitchSection(node, sectionBinder.GetDeclaredLocalsForScope(node), boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
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
                        pattern.WasCompilerGenerated = true; // we don't have a pattern syntax here
                        bool hasErrors = pattern.HasErrors;
                        SyntaxNode innerValueSyntax = caseLabelSyntax.Value.SkipParens();
                        if (innerValueSyntax.Kind() == SyntaxKind.DefaultLiteralExpression)
                        {
                            diagnostics.Add(ErrorCode.ERR_DefaultInSwitch, innerValueSyntax.Location);
                            hasErrors = true;
                        }

                        var constantValue = pattern.ConstantValue;
                        if (!hasErrors &&
                            (object)constantValue != null &&
                            pattern.Value.Type == SwitchGoverningType &&
                            this.FindMatchingSwitchCaseLabel(constantValue, caseLabelSyntax) != label)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, pattern.ConstantValue.GetValueToDisplay() ?? label.Name);
                            hasErrors = true;
                        }

                        return new BoundPatternSwitchLabel(node, label, pattern, null, hasErrors);
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
                            matchLabelSyntax.Pattern, SwitchGoverningType, node.HasErrors, diagnostics);
                        return new BoundPatternSwitchLabel(node, label, pattern,
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null,
                            node.HasErrors);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }
    }
}
