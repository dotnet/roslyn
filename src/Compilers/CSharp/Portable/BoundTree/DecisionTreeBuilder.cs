// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Helper class for (1) finding reachable and unreachable switch cases in binding, and (2)
    /// building a decision tree for lowering. As switch labels are added to the decision tree
    /// being built, a data structure (decision tree) representing the sequence of operations
    /// required to select the applicable case branch is constructed. See <see cref="DecisionTree"/>
    /// for the kinds of decisions that can appear in a decision tree.
    /// 
    /// The strategy for building the decision tree is: the top node is a ByType if the input
    /// could possibly be null. Otherwise it is a ByValue. Then, based on the type of switch
    /// label, we navigate to the appropriate node of the existing decision tree and insert
    /// a new decision tree node representing the condition associated with the new switch case.
    /// </summary>
    internal abstract class DecisionTreeBuilder
    {
        protected readonly Symbol _enclosingSymbol;
        protected readonly Conversions _conversions;
        protected HashSet<DiagnosticInfo> _useSiteDiagnostics = new HashSet<DiagnosticInfo>();
        protected readonly SwitchStatementSyntax _switchSyntax;
        private Dictionary<TypeSymbol, LocalSymbol> localByType = new Dictionary<TypeSymbol, LocalSymbol>();

        protected DecisionTreeBuilder(
            Symbol enclosingSymbol,
            SwitchStatementSyntax switchSyntax,
            Conversions conversions)
        {
            _enclosingSymbol = enclosingSymbol;
            _switchSyntax = switchSyntax;
            _conversions = conversions;
        }

        private BoundLocal GetBoundPatternMatchingLocal(TypeSymbol type)
        {
            // All synthesized pattern matching locals are associated with the Switch statement syntax node.
            // Their ordinals are zero.
            // EnC local slot variable matching logic find the right slot based on the type of the local.

            if (!localByType.TryGetValue(type, out var localSymbol))
            {
                localSymbol = new SynthesizedLocal(_enclosingSymbol as MethodSymbol, TypeSymbolWithAnnotations.Create(type), SynthesizedLocalKind.SwitchCasePatternMatching, _switchSyntax);
                localByType.Add(type, localSymbol);
            }

            return new BoundLocal(_switchSyntax, localSymbol, null, type);
        }

        /// <summary>
        /// Create a fresh decision tree for the given input expression of the given type.
        /// </summary>
        protected DecisionTree CreateEmptyDecisionTree(BoundExpression expression)
        {
            var type = expression.Type;

            LocalSymbol localSymbol = null;
            if (expression.ConstantValue == null)
            {
                // Unless it is a constant, the decision tree acts on a copy of the input expression.
                // We create a temp to represent that copy. Lowering will assign into this temp.
                var local = GetBoundPatternMatchingLocal(type);
                expression = local;
                localSymbol = local.LocalSymbol;
            }

            if (type.CanContainNull() || type.SpecialType == SpecialType.None)
            {
                // We need the ByType decision tree to separate null from non-null values.
                // Note that, for the purpose of the decision tree (and subsumption), we
                // ignore the fact that the input may be a constant, and therefore always
                // or never null.
                return new DecisionTree.ByType(expression, type, localSymbol);
            }
            else
            {
                // If it is a (e.g. builtin) value type, we can switch on its (constant) values.
                return new DecisionTree.ByValue(expression, type, localSymbol);
            }
        }

        protected DecisionTree AddToDecisionTree(DecisionTree decisionTree, SyntaxNode sectionSyntax, BoundPatternSwitchLabel label)
        {
            var pattern = label.Pattern;
            var guard = label.Guard;
            if (guard?.ConstantValue == ConstantValue.False)
            {
                return null;
            }

            switch (pattern.Kind)
            {
                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        DecisionMaker makeDecision = (e, t) => new DecisionTree.Guarded(e, t, default(ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>>), sectionSyntax, guard, label);
                        if (constantPattern.ConstantValue == ConstantValue.Null)
                        {
                            return AddByNull(decisionTree, makeDecision);
                        }
                        else
                        {
                            return AddByValue(decisionTree, constantPattern, makeDecision);
                        }
                    }
                case BoundKind.DeclarationPattern:
                    {
                        var declarationPattern = (BoundDeclarationPattern)pattern;
                        DecisionMaker maker =
                            (e, t) => new DecisionTree.Guarded(e, t, ImmutableArray.Create(new KeyValuePair<BoundExpression, BoundExpression>(e, declarationPattern.VariableAccess)), sectionSyntax, guard, label);
                        if (declarationPattern.IsVar)
                        {
                            return Add(decisionTree, maker);
                        }
                        else
                        {
                            return AddByType(decisionTree, declarationPattern.DeclaredType.Type, maker);
                        }
                    }
                case BoundKind.WildcardPattern:
                // We do not yet support a wildcard pattern syntax. It is used exclusively
                // to model the "default:" case, which is handled specially in the caller.
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

        /// <summary>
        /// A delegate to create the final (guarded) decision in a path when adding to the decision tree.
        /// </summary>
        /// <param name="expression">The input expression, cast to the required type if needed</param>
        /// <param name="type">The type of the input expression</param>
        protected delegate DecisionTree.Guarded DecisionMaker(
            BoundExpression expression,
            TypeSymbol type);

        private DecisionTree AddByValue(DecisionTree decision, BoundConstantPattern value, DecisionMaker makeDecision)
        {
            Debug.Assert(!decision.MatchIsComplete); // otherwise we would have given a subsumption error
            if (value.ConstantValue == null)
            {
                // If value.ConstantValue == null, we have a bad expression in a case label.
                // The case label is considered unreachable.
                return null;
            }

            switch (decision.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    return AddByValue((DecisionTree.ByType)decision, value, makeDecision);
                case DecisionTree.DecisionKind.ByValue:
                    return AddByValue((DecisionTree.ByValue)decision, value, makeDecision);
                case DecisionTree.DecisionKind.Guarded:
                    return AddByValue((DecisionTree.Guarded)decision, value, makeDecision);
                default:
                    throw ExceptionUtilities.UnexpectedValue(decision.Kind);
            }
        }

        private DecisionTree AddByValue(DecisionTree.Guarded guarded, BoundConstantPattern value, DecisionMaker makeDecision)
        {
            if (guarded.Default != null)
            {
                Debug.Assert(!guarded.Default.MatchIsComplete); // otherwise we would have given a subsumption error
            }
            else
            {
                // There is no default at this branch of the decision tree, so we create one.
                // Before the decision tree can match by value, it needs to test if the input is of the required type.
                // So we create a ByType node to represent that test.
                guarded.Default = new DecisionTree.ByType(guarded.Expression, guarded.Type, null);
            }

            return AddByValue(guarded.Default, value, makeDecision);
        }

        private DecisionTree AddByValue(DecisionTree.ByValue byValue, BoundConstantPattern value, DecisionMaker makeDecision)
        {
            Debug.Assert(value.Value.Type.Equals(byValue.Type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            if (byValue.Default != null)
            {
                return AddByValue(byValue.Default, value, makeDecision);
            }

            Debug.Assert(value.ConstantValue != null);
            object valueKey = value.ConstantValue.Value;
            DecisionTree valueDecision;
            if (byValue.ValueAndDecision.TryGetValue(valueKey, out valueDecision))
            {
                valueDecision = Add(valueDecision, makeDecision);
            }
            else
            {
                valueDecision = makeDecision(byValue.Expression, byValue.Type);
                byValue.ValueAndDecision.Add(valueKey, valueDecision);
            }

            if (byValue.Type.SpecialType == SpecialType.System_Boolean &&
                byValue.ValueAndDecision.Count == 2 &&
                byValue.ValueAndDecision.Values.All(d => d.MatchIsComplete))
            {
                byValue.MatchIsComplete = true;
            }

            return valueDecision;
        }

        private DecisionTree AddByValue(DecisionTree.ByType byType, BoundConstantPattern value, DecisionMaker makeDecision)
        {
            if (byType.Default != null)
            {
                try
                {
                    return AddByValue(byType.Default, value, makeDecision);
                }
                finally
                {
                    if (byType.Default.MatchIsComplete)
                    {
                        // This code may be unreachable due to https://github.com/dotnet/roslyn/issues/16878
                        byType.MatchIsComplete = true;
                    }
                }
            }

            if (value.ConstantValue == ConstantValue.Null)
            {
                // This should not occur, as the caller will have invoked AddByNull instead.
                throw ExceptionUtilities.Unreachable;
            }

            if ((object)value.Value.Type == null || value.ConstantValue == null)
            {
                return null;
            }

            foreach (var kvp in byType.TypeAndDecision)
            {
                var matchedType = kvp.Key;
                var decision = kvp.Value;

                // See if the test is already subsumed
                switch (ExpressionOfTypeMatchesPatternType(value.Value.Type, matchedType, ref _useSiteDiagnostics))
                {
                    case true:
                        if (decision.MatchIsComplete)
                        {
                            // Subsumed case have been eliminated by semantic analysis.
                            Debug.Assert(false);
                            return null;
                        }

                        continue;
                    case false:
                    case null:
                        continue;
                }
            }

            DecisionTree forType = null;

            // This new type test should logically be last. However it might be the same type as the one that is already
            // last. In that case we can produce better code by piggy-backing our new case on to the last decision.
            // Also, the last one might be a non-overlapping type, in which case we can piggy-back onto the second-last
            // type test.
            for (int i = byType.TypeAndDecision.Count - 1; i >= 0; i--)
            {
                var kvp = byType.TypeAndDecision[i];
                var matchedType = kvp.Key;
                var decision = kvp.Value;
                if (matchedType.Equals(value.Value.Type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                {
                    forType = decision;
                    break;
                }
                switch (ExpressionOfTypeMatchesPatternType(value.Value.Type, matchedType, ref _useSiteDiagnostics))
                {
                    case true:
                        if (decision.MatchIsComplete)
                        {
                            // we should have reported this case as subsumed already.
                            Debug.Assert(false);
                            return null;
                        }
                        else
                        {
                            goto case null;
                        }
                    case false:
                        continue;
                    case null:
                        // because there is overlap, we cannot reuse some earlier entry
                        goto noReuse;
                }
            }
noReuse:;

            // if we did not piggy-back, then create a new decision tree node for the type.
            if (forType == null)
            {
                var type = value.Value.Type;
                if (byType.Type.Equals(type, TypeCompareKind.AllIgnoreOptions))
                {
                    // reuse the input expression when we have an equivalent type to reduce the number of generated temps
                    forType = new DecisionTree.ByValue(byType.Expression, type.TupleUnderlyingTypeOrSelf(), null);
                }
                else
                {
                    var narrowedExpression = GetBoundPatternMatchingLocal(type);
                    forType = new DecisionTree.ByValue(narrowedExpression, type.TupleUnderlyingTypeOrSelf(), narrowedExpression.LocalSymbol);
                }

                byType.TypeAndDecision.Add(new KeyValuePair<TypeSymbol, DecisionTree>(type, forType));
            }

            return AddByValue(forType, value, makeDecision);
        }

        /// <summary>
        /// Does an expression of type <paramref name="expressionType"/> "match" a pattern that looks for
        /// type <paramref name="patternType"/>?
        /// 'true' if the matched type catches all of them, 'false' if it catches none of them, and
        /// 'null' if it might catch some of them. For this test we assume the expression's value
        /// isn't null.
        /// </summary>
        internal bool? ExpressionOfTypeMatchesPatternType(TypeSymbol expressionType, TypeSymbol patternType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return Binder.ExpressionOfTypeMatchesPatternType(this._conversions, expressionType, patternType, ref _useSiteDiagnostics, out Conversion conversion, null, false);
        }

        private DecisionTree AddByType(DecisionTree decision, TypeSymbol type, DecisionMaker makeDecision)
        {
            if (decision.MatchIsComplete || decision.Expression.ConstantValue?.IsNull == true)
            {
                return null;
            }

            switch (decision.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    return AddByType((DecisionTree.ByType)decision, type, makeDecision);
                case DecisionTree.DecisionKind.ByValue:
                    {
                        var byValue = (DecisionTree.ByValue)decision;
                        DecisionTree result;
                        if (byValue.Default == null)
                        {
                            if (byValue.Type.Equals(type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                            {
                                result = byValue.Default = makeDecision(byValue.Expression, byValue.Type);
                            }
                            else
                            {
                                byValue.Default = new DecisionTree.ByType(byValue.Expression, byValue.Type, null);
                                result = AddByType(byValue.Default, type, makeDecision);
                            }
                        }
                        else
                        {
                            result = AddByType(byValue.Default, type, makeDecision);
                        }

                        if (byValue.Default.MatchIsComplete)
                        {
                            byValue.MatchIsComplete = true;
                        }

                        return result;
                    }
                case DecisionTree.DecisionKind.Guarded:
                    return AddByType((DecisionTree.Guarded)decision, type, makeDecision);
                default:
                    throw ExceptionUtilities.UnexpectedValue(decision.Kind);
            }
        }

        private DecisionTree AddByType(DecisionTree.Guarded guarded, TypeSymbol type, DecisionMaker makeDecision)
        {
            if (guarded.Default == null)
            {
                guarded.Default = new DecisionTree.ByType(guarded.Expression, guarded.Type, null);
            }

            var result = AddByType(guarded.Default, type, makeDecision);
            if (guarded.Default.MatchIsComplete)
            {
                guarded.MatchIsComplete = true;
            }

            return result;
        }

        private DecisionTree AddByType(DecisionTree.ByType byType, TypeSymbol type, DecisionMaker makeDecision)
        {
            if (byType.Default != null)
            {
                try
                {
                    return AddByType(byType.Default, type, makeDecision);
                }
                finally
                {
                    if (byType.Default.MatchIsComplete)
                    {
                        byType.MatchIsComplete = true;
                    }
                }
            }

            // if the last type is the type we need, add to it
            DecisionTree result = null;
            if (byType.TypeAndDecision.Count != 0)
            {
                var lastTypeAndDecision = byType.TypeAndDecision.Last();
                if (lastTypeAndDecision.Key.Equals(type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                {
                    result = Add(lastTypeAndDecision.Value, makeDecision);
                }
            }

            if (result == null)
            {
                var expression = GetBoundPatternMatchingLocal(type);
                result = makeDecision(expression, type);
                Debug.Assert(result.Temp == null);
                result.Temp = expression.LocalSymbol;
                byType.TypeAndDecision.Add(new KeyValuePair<TypeSymbol, DecisionTree>(type, result));
            }

            if (ExpressionOfTypeMatchesPatternType(byType.Type, type, ref _useSiteDiagnostics) == true &&
                result.MatchIsComplete &&
                byType.WhenNull?.MatchIsComplete == true)
            {
                byType.MatchIsComplete = true;
            }

            return result;
        }

        private DecisionTree AddByNull(DecisionTree decision, DecisionMaker makeDecision)
        {
            // the decision tree cannot be complete, as if that were so we would have considered this decision subsumed.
            Debug.Assert(!decision.MatchIsComplete);

            switch (decision.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    return AddByNull((DecisionTree.ByType)decision, makeDecision);
                case DecisionTree.DecisionKind.ByValue:
                    throw ExceptionUtilities.Unreachable;
                case DecisionTree.DecisionKind.Guarded:
                    return AddByNull((DecisionTree.Guarded)decision, makeDecision);
                default:
                    throw ExceptionUtilities.UnexpectedValue(decision.Kind);
            }
        }

        private DecisionTree AddByNull(DecisionTree.ByType byType, DecisionMaker makeDecision)
        {
            // these tree cannot be complete, as if that were so we would have considered this decision subsumed.
            Debug.Assert(byType.WhenNull?.MatchIsComplete != true);
            Debug.Assert(byType.Default?.MatchIsComplete != true);

            if (byType.Default != null)
            {
                try
                {
                    return AddByNull(byType.Default, makeDecision);
                }
                finally
                {
                    if (byType.Default.MatchIsComplete)
                    {
                        byType.MatchIsComplete = true;
                    }
                }
            }
            DecisionTree result;
            if (byType.WhenNull == null)
            {
                result = byType.WhenNull = makeDecision(byType.Expression, byType.Type);
            }
            else
            {
                result = Add(byType.WhenNull, makeDecision);
            }

            if (byType.WhenNull.MatchIsComplete && NonNullHandled(byType))
            {
                byType.MatchIsComplete = true;
            }

            return result;
        }

        private bool NonNullHandled(DecisionTree.ByType byType)
        {
            var inputType = byType.Type.StrippedType().TupleUnderlyingTypeOrSelf();
            foreach (var td in byType.TypeAndDecision)
            {
                var type = td.Key;
                var decision = td.Value;
                if (ExpressionOfTypeMatchesPatternType(inputType, type, ref _useSiteDiagnostics) == true &&
                    decision.MatchIsComplete)
                {
                    return true;
                }
            }

            return false;
        }

        private DecisionTree AddByNull(DecisionTree.Guarded guarded, DecisionMaker makeDecision)
        {
            if (guarded.Default == null)
            {
                guarded.Default = new DecisionTree.ByType(guarded.Expression, guarded.Type, null);
            }

            var result = AddByNull(guarded.Default, makeDecision);
            if (guarded.Default.MatchIsComplete)
            {
                guarded.MatchIsComplete = true;
            }

            return result;
        }

        protected DecisionTree Add(DecisionTree decision, DecisionMaker makeDecision)
        {
            // the decision tree cannot be complete, otherwise we would have given a subsumption error for this case.
            Debug.Assert(!decision.MatchIsComplete);

            switch (decision.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    return Add((DecisionTree.ByType)decision, makeDecision);
                case DecisionTree.DecisionKind.ByValue:
                    return Add((DecisionTree.ByValue)decision, makeDecision);
                case DecisionTree.DecisionKind.Guarded:
                    return Add((DecisionTree.Guarded)decision, makeDecision);
                default:
                    throw ExceptionUtilities.UnexpectedValue(decision.Kind);
            }
        }

        private DecisionTree Add(DecisionTree.Guarded guarded, DecisionMaker makeDecision)
        {
            if (guarded.Default != null)
            {
                Debug.Assert(!guarded.Default.MatchIsComplete); // otherwise we would have given a subsumption error
                var result = Add(guarded.Default, makeDecision);
                if (guarded.Default.MatchIsComplete)
                {
                    guarded.MatchIsComplete = true;
                }

                return result;
            }
            else
            {
                var result = guarded.Default = makeDecision(guarded.Expression, guarded.Type);
                if (guarded.Default.MatchIsComplete)
                {
                    guarded.MatchIsComplete = true;
                }

                return result;
            }
        }

        private DecisionTree Add(DecisionTree.ByValue byValue, DecisionMaker makeDecision)
        {
            DecisionTree result;
            if (byValue.Default != null)
            {
                result = Add(byValue.Default, makeDecision);
            }
            else
            {
                result = byValue.Default = makeDecision(byValue.Expression, byValue.Type);
            }
            if (byValue.Default.MatchIsComplete)
            {
                byValue.MatchIsComplete = true;
            }

            return result;
        }

        private DecisionTree Add(DecisionTree.ByType byType, DecisionMaker makeDecision)
        {
            try
            {
                if (byType.Default == null)
                {
                    byType.Default = makeDecision(byType.Expression, byType.Type);
                    return byType.Default;
                }
                else
                {
                    return Add(byType.Default, makeDecision);
                }
            }
            finally
            {
                if (byType.Default.MatchIsComplete)
                {
                    byType.MatchIsComplete = true;
                }
            }
        }
    }
}
