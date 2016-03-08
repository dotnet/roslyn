// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class PreciseAbstractFlowPass<LocalState>
    {
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // visit switch header
            LocalState breakState = VisitSwitchHeader(node);
            SetUnreachable();

            // visit switch block
            VisitSwitchBlock(node);
            IntersectWith(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private LocalState VisitSwitchHeader(BoundSwitchStatement node)
        {
            // Initial value for the Break state for a switch statement is established as follows:
            //  Break state = UnreachableState if either of the following is true:
            //  (1) there is a default label, or
            //  (2) the switch expression is constant and there is a matching case label.
            //  Otherwise, the Break state = current state.

            // visit switch expression
            VisitRvalue(node.Expression);
            LocalState breakState = this.State;

            // For a switch statement, we simulate a possible jump to the switch labels to ensure that
            // the label is not treated as an unused label and a pending branch to the label is noted.

            // However, if switch expression is a constant, we must have determined the single target label
            // at bind time, i.e. node.ConstantTargetOpt, and we must simulate a jump only to this label.

            var constantTargetOpt = node.ConstantTargetOpt;
            if ((object)constantTargetOpt == null)
            {
                bool hasDefaultLabel = false;
                foreach (var section in node.SwitchSections)
                {
                    foreach (var boundSwitchLabel in section.SwitchLabels)
                    {
                        var label = boundSwitchLabel.Label;
                        hasDefaultLabel = hasDefaultLabel || label.IdentifierNodeOrToken.Kind() == SyntaxKind.DefaultSwitchLabel;
                        SetState(breakState.Clone());
                        var simulatedGoto = new BoundGotoStatement(node.Syntax, label);
                        VisitGotoStatement(simulatedGoto);
                    }
                }

                if (hasDefaultLabel)
                {
                    // Condition (1) for an unreachable break state is satisfied
                    breakState = UnreachableState();
                }
            }
            else if (!node.BreakLabel.Equals(constantTargetOpt))
            {
                SetState(breakState.Clone());
                var simulatedGoto = new BoundGotoStatement(node.Syntax, constantTargetOpt);
                VisitGotoStatement(simulatedGoto);

                // Condition (1) or (2) for an unreachable break state is satisfied
                breakState = UnreachableState();
            }

            return breakState;
        }

        private void VisitSwitchBlock(BoundSwitchStatement node)
        {
            var afterSwitchState = UnreachableState();
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);
            // visit switch sections
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitSwitchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                IntersectWith(ref afterSwitchState, ref this.State);
            }

            SetState(afterSwitchState);
        }

        public virtual BoundNode VisitSwitchSection(BoundSwitchSection node, bool lastSection)
        {
            return VisitSwitchSection(node);
        }

        public override BoundNode VisitSwitchSection(BoundSwitchSection node)
        {
            // visit switch section labels
            foreach (var boundSwitchLabel in node.SwitchLabels)
            {
                VisitRvalue(boundSwitchLabel.ExpressionOpt);
                VisitSwitchSectionLabel(boundSwitchLabel.Label, node);
            }

            // visit switch section body
            VisitStatementList(node);

            return null;
        }

        protected virtual void VisitSwitchSectionLabel(LabelSymbol label, BoundSwitchSection node)
        {
            VisitLabel(label, node);
        }

        // ===========================
        // Below here is the implementation for the pattern-matching variation of the switch statement.
        // ===========================

        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            // visit switch header
            LocalState breakState = VisitPatternSwitchHeader(node);

            // visit switch block
            VisitPatternSwitchBlock(node);
            IntersectWith(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private void VisitPatternSwitchBlock(BoundPatternSwitchStatement node)
        {
            var afterSwitchState = UnreachableState();
            var switchSections = node.PatternSwitchSections;
            var iLastSection = (switchSections.Length - 1);
            var dispatchState = this.State.Clone();

            // visit switch sections
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                SetState(dispatchState.Clone());
                VisitPatternSwitchSection(switchSections[iSection], node.Expression, iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                IntersectWith(ref afterSwitchState, ref this.State);
            }

            SetState(afterSwitchState);
        }

        /// <summary>
        /// Visit the switch expression, and return the initial break state.
        /// </summary>
        private LocalState VisitPatternSwitchHeader(BoundPatternSwitchStatement node)
        {
            // decide if the switch has the moral equivalent of a default label.
            bool hasDefaultLabel = false;
            foreach (var section in node.PatternSwitchSections)
            {
                foreach (var boundPatternSwitchLabel in section.PatternSwitchLabels)
                {
                    if (boundPatternSwitchLabel.Guard != null && !IsConstantTrue(boundPatternSwitchLabel.Guard))
                    {
                        continue;
                    }

                    if (boundPatternSwitchLabel.Pattern.Kind == BoundKind.WildcardPattern ||
                        boundPatternSwitchLabel.Pattern.Kind == BoundKind.DeclarationPattern && ((BoundDeclarationPattern)boundPatternSwitchLabel.Pattern).IsVar)
                    {
                        hasDefaultLabel = true;
                        goto foundDefaultLabel;
                    }
                }
            }
            foundDefaultLabel:;

            // visit switch expression
            VisitRvalue(node.Expression);

            // return the state to use if no pattern matches
            if (hasDefaultLabel)
            {
                return UnreachableState();
            }
            else
            {
                return this.State;
            }
        }

        protected virtual void VisitPatternSwitchSection(BoundPatternSwitchSection node, BoundExpression switchExpression, bool isLastSection)
        {
            // visit switch section labels
            var initialState = this.State;
            var afterGuardState = UnreachableState();
            foreach (var label in node.PatternSwitchLabels)
            {
                SetState(initialState.Clone());
                VisitPattern(switchExpression, label.Pattern);
                SetState(StateWhenTrue);
                if (label.Guard != null)
                {
                    VisitCondition(label.Guard);
                    SetState(StateWhenTrue);
                }
                IntersectWith(ref afterGuardState, ref this.State);
            }

            // visit switch section body
            SetState(afterGuardState);
            VisitStatementList(node);
        }

        public virtual void VisitPattern(BoundExpression expression, BoundPattern pattern)
        {
            Split();
            bool? knownMatch = CheckRefutations(expression, pattern);
            switch (knownMatch)
            {
                case true:
                    SetState(StateWhenTrue);
                    SetConditionalState(this.State, UnreachableState());
                    break;
                case false:
                    SetState(StateWhenFalse);
                    SetConditionalState(UnreachableState(), this.State);
                    break;
                case null:
                    break;
            }
        }

        public override BoundNode VisitPropertyPatternMember(BoundPropertyPatternMember node)
        {
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Check if the given expression is known to *always* match, or *always* fail against the given pattern.
        /// Return true for known match, false for known fail, and null otherwise.
        /// </summary>
        private bool? CheckRefutations(BoundExpression expression, BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.WildcardPattern:
                    return true;
                case BoundKind.DeclarationPattern:
                    {
                        var declPattern = (BoundDeclarationPattern)pattern;
                        if (declPattern.IsVar|| // var pattern always matches
                            declPattern.DeclaredType?.Type?.IsValueType == true && declPattern.DeclaredType.Type == (object)expression.Type) // exact match
                        {
                            return true;
                        }
                        // there are probably other cases to check. Note that reference types can, in general, fail because of null
                    }
                    break;
                case BoundKind.PropertyPattern:
                    {
                        var propPattern = (BoundPropertyPattern)pattern;
                        if (expression.Type?.IsValueType == true &&
                            (expression.Type == propPattern.Type || propPattern.Type?.Interfaces.Contains(expression.Type) == true))
                        {
                            // so far so good: the expression is known to match the *type* of the property pattern.
                            // Now check if each subpattern is irrefutable.
                            int n = propPattern.Subpatterns.Length;
                            foreach (var subpattern in propPattern.Subpatterns)
                            {
                                var prop = (subpattern.Member as BoundPropertyPatternMember)?.MemberSymbol;
                                var pat = subpattern.Pattern;
                                BoundExpression subExpr;
                                switch (prop?.Kind)
                                {
                                    case SymbolKind.Property:
                                        var propSymbol = (PropertySymbol)prop;
                                        subExpr = new BoundPropertyAccess(pat.Syntax, null, propSymbol, LookupResultKind.Viable, propSymbol.Type);
                                        break;
                                    case SymbolKind.Field:
                                        var fieldSymbol = (FieldSymbol)prop;
                                        subExpr = new BoundFieldAccess(pat.Syntax, null, fieldSymbol, null);
                                        break;
                                    // TODO: what about events?
                                    default:
                                        return false;
                                }
                                var subMatch = CheckRefutations(subExpr, pat);
                                if (subMatch != true) return subMatch;
                            }

                            return true;
                        }
                    }
                    break;
            }

            return null;
        }

        public override BoundNode VisitThrowExpression(BoundThrowExpression node)
        {
            VisitRvalue(node.Expression);
            SetUnreachable();
            return node;
        }

        public override BoundNode VisitMatchExpression(BoundMatchExpression node)
        {
            VisitRvalue(node.Left);
            var initialState = this.State;
            var endState = UnreachableState();
            foreach (var c in node.Cases)
            {
                SetState(initialState.Clone());
                VisitPattern(node.Left, c.Pattern);
                SetState(StateWhenTrue);
                if (c.Guard != null)
                {
                    VisitCondition(c.Guard);
                    SetState(StateWhenTrue);
                }

                VisitRvalue(c.Expression);
                IntersectWith(ref endState, ref this.State);
            }

            SetState(endState);
            return node;
        }
    }
}
