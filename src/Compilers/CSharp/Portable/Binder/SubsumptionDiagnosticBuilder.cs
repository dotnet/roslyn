// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{

    /// <summary>
    /// Helper class for binding the pattern switch statement. It helps compute which labels
    /// are subsumed and/or reachable.
    /// </summary>
    internal class SubsumptionDiagnosticBuilder : DecisionTreeBuilder
    {
        private readonly DecisionTree _subsumptionTree;

        internal SubsumptionDiagnosticBuilder(Symbol enclosingSymbol,
                                               Conversions conversions,
                                               BoundExpression expression)
            : base(enclosingSymbol, conversions)
        {
            _subsumptionTree = DecisionTree.Create(expression, expression.Type, enclosingSymbol);
        }

        /// <summary>
        /// Add the case label to the subsumption tree. Return true if the label is reachable
        /// given the expression and previously added labels. `valueMatched` is set to true
        /// if and only if the label is a reachable unconditional (no when clause) constant pattern
        /// whose value is the same as the input expression's constant value, and false otherwise.
        /// </summary>
        internal bool AddLabel(BoundPatternSwitchLabel label, DiagnosticBag diagnostics, out bool valueMatched)
        {
            // Use site diagnostics are reported (and cleared) by this method.
            // So they should be empty when we start.
            Debug.Assert(_useSiteDiagnostics.Count == 0);

            valueMatched = false;

            if (label.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
            {
                // the default case label is always considered reachable.
                return true;
            }

            try
            {
                // For purposes of subsumption, we do not take into consideration the value
                // of the input expression. Therefore we consider null possible if the type permits.
                Syntax = label.Syntax;
                var subsumedErrorCode = CheckSubsumed(label.Pattern, _subsumptionTree, inputCouldBeNull: true);
                if (subsumedErrorCode != 0 && subsumedErrorCode != ErrorCode.ERR_NoImplicitConvCast)
                {
                    if (!label.HasErrors)
                    {
                        diagnostics.Add(subsumedErrorCode, label.Pattern.Syntax.Location);
                    }

                    return false;
                }

                var guardAlwaysSatisfied = label.Guard == null || label.Guard.ConstantValue == ConstantValue.True;

                if (guardAlwaysSatisfied)
                {
                    // Only unconditional switch labels contribute to subsumption
                    if (AddToDecisionTree(_subsumptionTree, null, label) == null)
                    {
                        return false;
                    }
                }

                // For a constant switch, a constant label is only reachable if the value is equal.
                var patternConstant = (label.Pattern as BoundConstantPattern)?.ConstantValue;
                if (this._subsumptionTree.Expression.ConstantValue == null ||
                    patternConstant == null)
                {
                    // either the input or the pattern wasn't a constant, so they might match.
                    return true;
                }

                // If not subsumed, the label is considered reachable unless its constant value is
                // distinct from the constant value of the input expression.
                if (this._subsumptionTree.Expression.ConstantValue.Equals(patternConstant))
                {
                    valueMatched = guardAlwaysSatisfied;
                    return true;
                }

                return false;
            }
            finally
            {
                // report the use-site diagnostics
                diagnostics.Add(label.Syntax.Location, _useSiteDiagnostics);
                _useSiteDiagnostics.Clear();
            }
        }

        internal bool IsComplete => _subsumptionTree.MatchIsComplete;

        /// <summary>
        /// Check if the pattern is subsumed by the decisions in the decision tree, given that the input could
        /// (or could not) be null based on the parameter <paramref name="inputCouldBeNull"/>. If it is subsumed,
        /// returns an error code suitable for reporting the issue. If it is not subsumed, returns 0.
        /// </summary>
        private ErrorCode CheckSubsumed(BoundPattern pattern, DecisionTree decisionTree, bool inputCouldBeNull)
        {
            if (decisionTree.MatchIsComplete)
            {
                return ErrorCode.ERR_PatternIsSubsumed;
            }

            switch (pattern.Kind)
            {
                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        if (constantPattern.Value.HasErrors || constantPattern.Value.ConstantValue == null)
                        {
                            // since this will have been reported earlier, we use ErrorCode.ERR_NoImplicitConvCast
                            // as a flag to suppress errors in subsumption analysis.
                            return ErrorCode.ERR_NoImplicitConvCast;
                        }

                        bool isNull = constantPattern.Value.ConstantValue.IsNull;

                        // If null inputs have been handled by previous patterns, then
                        // the input can no longer be null. In that case a null pattern is subsumed.
                        if (isNull && !inputCouldBeNull)
                        {
                            return ErrorCode.ERR_PatternIsSubsumed;
                        }

                        switch (decisionTree.Kind)
                        {
                            case DecisionTree.DecisionKind.ByValue:
                                {
                                    var byValue = (DecisionTree.ByValue)decisionTree;
                                    if (isNull)
                                    {
                                        return 0; // null must be handled at a type test
                                    }

                                    DecisionTree decision;
                                    if (byValue.ValueAndDecision.TryGetValue(constantPattern.Value.ConstantValue.Value, out decision))
                                    {
                                        var error = CheckSubsumed(pattern, decision, inputCouldBeNull);
                                        if (error != 0)
                                        {
                                            return error;
                                        }
                                    }

                                    if (byValue.Default != null)
                                    {
                                        return CheckSubsumed(pattern, byValue.Default, inputCouldBeNull);
                                    }

                                    return 0;
                                }
                            case DecisionTree.DecisionKind.ByType:
                                {
                                    var byType = (DecisionTree.ByType)decisionTree;
                                    if (isNull)
                                    {
                                        if (byType.WhenNull != null)
                                        {
                                            var result = CheckSubsumed(pattern, byType.WhenNull, inputCouldBeNull);
                                            if (result != 0)
                                            {
                                                return result;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var td in byType.TypeAndDecision)
                                        {
                                            var type = td.Key;
                                            var decision = td.Value;
                                            if (ExpressionOfTypeMatchesPatternType(constantPattern.Value.Type, type, ref _useSiteDiagnostics) == true)
                                            {
                                                var error = CheckSubsumed(pattern, decision, false);
                                                if (error != 0)
                                                {
                                                    return error;
                                                }
                                            }
                                        }
                                    }
                                    return (byType.Default != null) ? CheckSubsumed(pattern, byType.Default, inputCouldBeNull) : 0;
                                }
                            case DecisionTree.DecisionKind.Guarded:
                                {
                                    var guarded = (DecisionTree.Guarded)decisionTree;
                                    return
                                        (guarded.Guard == null || guarded.Guard.ConstantValue == ConstantValue.True) ? ErrorCode.ERR_PatternIsSubsumed :
                                        guarded.Default == null ? 0 : CheckSubsumed(pattern, guarded.Default, inputCouldBeNull);
                                }
                            default:
                                throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
                        }
                    }
                case BoundKind.DeclarationPattern:
                    {
                        var declarationPattern = (BoundDeclarationPattern)pattern;
                        switch (decisionTree.Kind)
                        {
                            case DecisionTree.DecisionKind.ByValue:
                                {
                                    // A declaration pattern is only subsumed by a value pattern if all of the values are accounted for.
                                    // For example, when switching on a bool, do we handle both true and false?
                                    // For now, we do not handle this case. Also, this provides compatibility with previous compilers.
                                    if (inputCouldBeNull)
                                    {
                                        return 0; // null could never be handled by a value decision
                                    }

                                    var byValue = (DecisionTree.ByValue)decisionTree;
                                    if (byValue.Default != null)
                                    {
                                        return CheckSubsumed(pattern, byValue.Default, false);
                                    }

                                    return 0;
                                }
                            case DecisionTree.DecisionKind.ByType:
                                {
                                    var byType = (DecisionTree.ByType)decisionTree;
                                    if (declarationPattern.IsVar &&
                                        inputCouldBeNull &&
                                        (byType.WhenNull == null || CheckSubsumed(pattern, byType.WhenNull, inputCouldBeNull) == 0) &&
                                        (byType.Default == null || CheckSubsumed(pattern, byType.Default, inputCouldBeNull) == 0))
                                    {
                                        return 0; // new pattern catches null if not caught by existing WhenNull or Default
                                    }

                                    inputCouldBeNull = false;
                                    foreach (var td in byType.TypeAndDecision)
                                    {
                                        var type = td.Key;
                                        var decision = td.Value;
                                        if (ExpressionOfTypeMatchesPatternType(
                                                declarationPattern.DeclaredType.Type.TupleUnderlyingTypeOrSelf(), type, ref _useSiteDiagnostics) == true)
                                        {
                                            var error = CheckSubsumed(pattern, decision, inputCouldBeNull);
                                            if (error != 0)
                                            {
                                                return error;
                                            }
                                        }
                                    }

                                    if (byType.Default != null)
                                    {
                                        return CheckSubsumed(pattern, byType.Default, inputCouldBeNull);
                                    }

                                    return 0;
                                }
                            case DecisionTree.DecisionKind.Guarded:
                                {
                                    var guarded = (DecisionTree.Guarded)decisionTree;
                                    return (guarded.Guard == null || guarded.Guard.ConstantValue == ConstantValue.True) ? ErrorCode.ERR_PatternIsSubsumed :
                                        guarded.Default != null ? CheckSubsumed(pattern, guarded.Default, inputCouldBeNull) : 0;
                                }
                            default:
                                throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
                        }
                    }
                case BoundKind.WildcardPattern:
                    {
                        switch (decisionTree.Kind)
                        {
                            case DecisionTree.DecisionKind.ByValue:
                                return 0; // a value pattern is always considered incomplete (even bool true and false)
                            case DecisionTree.DecisionKind.ByType:
                                {
                                    var byType = (DecisionTree.ByType)decisionTree;
                                    if (inputCouldBeNull &&
                                        (byType.WhenNull == null || CheckSubsumed(pattern, byType.WhenNull, inputCouldBeNull) == 0) &&
                                        (byType.Default == null || CheckSubsumed(pattern, byType.Default, inputCouldBeNull) == 0))
                                    {
                                        return 0; // new pattern catches null if not caught by existing WhenNull or Default
                                    }

                                    inputCouldBeNull = false;
                                    foreach (var td in byType.TypeAndDecision)
                                    {
                                        var type = td.Key;
                                        var decision = td.Value;
                                        if (ExpressionOfTypeMatchesPatternType(decisionTree.Type, type, ref _useSiteDiagnostics) == true)
                                        {
                                            var error = CheckSubsumed(pattern, decision, inputCouldBeNull);
                                            if (error != 0)
                                            {
                                                return error;
                                            }
                                        }
                                    }

                                    if (byType.Default != null)
                                    {
                                        return CheckSubsumed(pattern, byType.Default, inputCouldBeNull);
                                    }

                                    return 0;
                                }
                            case DecisionTree.DecisionKind.Guarded:
                                {
                                    var guarded = (DecisionTree.Guarded)decisionTree;
                                    return (guarded.Guard == null || guarded.Guard.ConstantValue == ConstantValue.True) ? ErrorCode.ERR_PatternIsSubsumed :
                                        guarded.Default != null ? CheckSubsumed(pattern, guarded.Default, inputCouldBeNull) : 0;
                                }
                            default:
                                throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
                        }
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }
    }
}
