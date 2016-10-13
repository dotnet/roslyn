// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
    /// </summary>
    internal abstract class DecisionTreeBuilder
    {
        protected readonly Symbol _enclosingSymbol;
        protected readonly Conversions _conversions;
        protected HashSet<DiagnosticInfo> _useSiteDiagnostics = new HashSet<DiagnosticInfo>();

        protected DecisionTreeBuilder(
            Symbol enclosingSymbol,
            Conversions conversions)
        {
            this._enclosingSymbol = enclosingSymbol;
            this._conversions = conversions;
        }

        protected SyntaxNode Syntax { private get; set; }

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
                        return AddByValue(decisionTree, constantPattern,
                            (e, t) => new DecisionTree.Guarded(e, t, default(ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>>), sectionSyntax, guard, label));
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

        protected delegate DecisionTree DecisionMaker(
            BoundExpression expression,
            TypeSymbol type);

        private DecisionTree AddByValue(DecisionTree decision, BoundConstantPattern value, DecisionMaker makeDecision)
        {
            if (decision.MatchIsComplete)
            {
                return null;
            }

            // Even if value.ConstantValue == null, we proceed here for error recovery, so that the case label isn't
            // dropped on the floor. That is useful, for example to suppress unreachable code warnings on bad case labels.
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
                if (guarded.Default.MatchIsComplete)
                {
                    return null;
                }
            }
            else
            {
                guarded.Default = new DecisionTree.ByValue(guarded.Expression, guarded.Type, null);
            }

            return AddByValue(guarded.Default, value, makeDecision);
        }

        private DecisionTree AddByValue(DecisionTree.ByValue byValue, BoundConstantPattern value, DecisionMaker makeDecision)
        {
            Debug.Assert(value.Value.Type == byValue.Type);
            if (byValue.Default != null)
            {
                return AddByValue(byValue.Default, value, makeDecision);
            }

            // For error recovery, to avoid "unreachable code" diagnostics when there is a bad case
            // label, we use the case label itself as the value key.
            object valueKey = value.ConstantValue?.Value ?? value;
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
                        byType.MatchIsComplete = true;
                    }
                }
            }

            if (value.ConstantValue == ConstantValue.Null)
            {
                return byType.Expression.ConstantValue?.IsNull == false
                    ? null : AddByNull((DecisionTree)byType, makeDecision);
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
                            return null;
                        }

                        continue;
                    case false:
                    case null:
                        continue;
                }
            }

            DecisionTree forType = null;

            // Find an existing decision tree for the expression's type. Since this new test
            // should logically be last, we look for the last one we can piggy-back it onto.
            for (int i = byType.TypeAndDecision.Count - 1; i >= 0 && forType == null; i--)
            {
                var kvp = byType.TypeAndDecision[i];
                var matchedType = kvp.Key;
                var decision = kvp.Value;
                if (matchedType.TupleUnderlyingTypeOrSelf() == value.Value.Type.TupleUnderlyingTypeOrSelf())
                {
                    forType = decision;
                    break;
                }
                else if (ExpressionOfTypeMatchesPatternType(value.Value.Type, matchedType, ref _useSiteDiagnostics) != false)
                {
                    break;
                }
            }

            if (forType == null)
            {
                var type = value.Value.Type;
                var localSymbol = new SynthesizedLocal(_enclosingSymbol as MethodSymbol, type, SynthesizedLocalKind.PatternMatchingTemp, Syntax, false, RefKind.None);
                var narrowedExpression = new BoundLocal(Syntax, localSymbol, null, type);
                forType = new DecisionTree.ByValue(narrowedExpression, value.Value.Type.TupleUnderlyingTypeOrSelf(), localSymbol);
                byType.TypeAndDecision.Add(new KeyValuePair<TypeSymbol, DecisionTree>(value.Value.Type, forType));
            }

            return AddByValue(forType, value, makeDecision);
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
                        if (byValue.Default == null)
                        {
                            byValue.Default = makeDecision(byValue.Expression, byValue.Type);
                            if (byValue.Default.MatchIsComplete)
                            {
                                byValue.MatchIsComplete = true;
                            }

                            return byValue.Default;
                        }
                        else
                        {
                            Debug.Assert(byValue.Default.Type == type);
                            return Add(byValue.Default, makeDecision);
                        }
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
            foreach (var kvp in byType.TypeAndDecision)
            {
                var MatchedType = kvp.Key;
                var Decision = kvp.Value;
                // See if matching Type matches this value
                switch (ExpressionOfTypeMatchesPatternType(type, MatchedType, ref _useSiteDiagnostics))
                {
                    case true:
                        if (Decision.MatchIsComplete)
                        {
                            return null;
                        }

                        continue;
                    case false:
                        continue;
                    case null:
                        continue;
                }
            }

            var localSymbol = new SynthesizedLocal(_enclosingSymbol as MethodSymbol, type, SynthesizedLocalKind.PatternMatchingTemp, Syntax, false, RefKind.None);
            var expression = new BoundLocal(Syntax, localSymbol, null, type);
            var result = makeDecision(expression, type);
            Debug.Assert(result.Temp == null);
            result.Temp = localSymbol;
            byType.TypeAndDecision.Add(new KeyValuePair<TypeSymbol, DecisionTree>(type, result));
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
            if (decision.MatchIsComplete)
            {
                return null;
            }

            switch (decision.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    return AddByNull((DecisionTree.ByType)decision, makeDecision);
                case DecisionTree.DecisionKind.ByValue:
                    {
                        var byValue = (DecisionTree.ByValue)decision;
                        if (byValue.MatchIsComplete)
                        {
                            return null;
                        }

                        throw ExceptionUtilities.Unreachable;
                    }
                case DecisionTree.DecisionKind.Guarded:
                    return AddByNull((DecisionTree.Guarded)decision, makeDecision);
                default:
                    throw ExceptionUtilities.UnexpectedValue(decision.Kind);
            }
        }

        private DecisionTree AddByNull(DecisionTree.ByType byType, DecisionMaker makeDecision)
        {
            if (byType.WhenNull?.MatchIsComplete == true || byType.Default?.MatchIsComplete == true)
            {
                return null;
            }

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
            if (decision.MatchIsComplete)
            {
                return null;
            }

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
                if (guarded.Default.MatchIsComplete)
                {
                    return null;
                }

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

        /// <summary>
        /// Does an expression of type <paramref name="expressionType"/> "match" a pattern that looks for
        /// type <paramref name="patternType"/>?
        /// 'true' if the matched type catches all of them, 'false' if it catches none of them, and
        /// 'null' if it might catch some of them. For this test we assume the expression's value
        /// isn't null.
        /// </summary>
        protected bool? ExpressionOfTypeMatchesPatternType(
            TypeSymbol expressionType,
            TypeSymbol patternType,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (expressionType == patternType)
            {
                return true;
            }

            var conversion = _conversions.ClassifyBuiltInConversion(expressionType, patternType, ref useSiteDiagnostics);

            // This is for classification purposes only; we discard use-site diagnostics. Use-site diagnostics will
            // be given if a conversion is actually used.
            switch (conversion.Kind)
            {
                case ConversionKind.Boxing:             // a value of type int matches a pattern of type object
                case ConversionKind.Identity:           // a value of a given type matches a pattern of that type
                case ConversionKind.ImplicitReference:  // a value of type string matches a pattern of type object
                    return true;

                case ConversionKind.ImplicitNullable:   // a value of type int matches a pattern of type int?
                case ConversionKind.ExplicitNullable:   // a non-null value of type "int?" matches a pattern of type int
                    // but if the types differ (e.g. one of them is type byte and the other is type int?).. no match
                    return ConversionsBase.HasIdentityConversion(expressionType.StrippedType().TupleUnderlyingTypeOrSelf(), patternType.StrippedType().TupleUnderlyingTypeOrSelf());

                case ConversionKind.ExplicitEnumeration:// a value of enum type does not match a pattern of integral type
                case ConversionKind.ExplicitNumeric:    // a value of type long does not match a pattern of type int
                case ConversionKind.ImplicitNumeric:    // a value of type short does not match a pattern of type int
                case ConversionKind.ImplicitTuple:      // distinct tuple types don't match
                case ConversionKind.NoConversion:
                    return false;

                case ConversionKind.ExplicitDynamic:    // a value of type dynamic might not match a pattern of type other than object
                case ConversionKind.ExplicitReference:  // a narrowing reference conversion might or might not succeed
                case ConversionKind.Unboxing:           // a value of type object might match a pattern of type int
                    return null;

                default:
                    // other conversions don't apply (e.g. conversions from expression, user-defined) and should not arise
                    throw ExceptionUtilities.UnexpectedValue(conversion.Kind);
            }
        }
    }
}
