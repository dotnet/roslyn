// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Helper class for binding the pattern switch statement. It helps compute which labels
    /// are subsumed and/or reachable. The strategy, implemented in <see cref="PatternSwitchBinder"/>,
    /// is to start with an empty decision tree, and for each case
    /// we visit the decision tree to see if the case is subsumed. If it is, we report an error.
    /// If it is not subsumed and there is no guard expression, we then add it to the decision
    /// tree.
    /// </summary>
    internal sealed class SubsumptionDiagnosticBuilder : DecisionTreeBuilder
    {
        private readonly DecisionTree _subsumptionTree;

        internal SubsumptionDiagnosticBuilder(Symbol enclosingSymbol,
                                              SwitchStatementSyntax syntax,
                                              Conversions conversions,
                                              TypeSymbol switchGoverningType)
            : base(enclosingSymbol, syntax, conversions)
        {
            // For the purpose of computing subsumption, we ignore the input expression's constant
            // value. Therefore we create a fake expression here that doesn't contain the value.
            var placeholderExpression = new BoundDup(syntax, RefKind.None, switchGoverningType);
            _subsumptionTree = CreateEmptyDecisionTree(placeholderExpression);
        }

        /// <summary>
        /// Add the case label to the subsumption tree. Return true if the label is reachable
        /// (not subsumed) given the governing expression's type and previously added labels. 
        /// </summary>
        internal bool AddLabel(BoundPatternSwitchLabel label, DiagnosticBag diagnostics)
        {
            // Use site diagnostics are reported (and cleared) by this method.
            // So they should be empty when we start.
            Debug.Assert(_useSiteDiagnostics.Count == 0);

            if (label.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
            {
                // the default case label is always considered reachable.
                return true;
            }

            try
            {
                // For purposes of subsumption, we do not take into consideration the value
                // of the input expression. Therefore we consider null possible if the type permits.
                var inputCouldBeNull = _subsumptionTree.Type.CanContainNull();
                var subsumedErrorCode = CheckSubsumed(label.Pattern, _subsumptionTree, inputCouldBeNull: inputCouldBeNull);
                if (subsumedErrorCode != 0)
                {
                    if (!label.HasErrors && subsumedErrorCode != ErrorCode.ERR_NoImplicitConvCast)
                    {
                        diagnostics.Add(subsumedErrorCode, label.Pattern.Syntax.Location);
                    }

                    return false;
                }

                var guardAlwaysSatisfied = label.Guard == null || label.Guard.ConstantValue == ConstantValue.True;

                if (guardAlwaysSatisfied)
                {
                    // Only unconditional switch labels contribute to subsumption
                    if (AddToDecisionTree(_subsumptionTree, null, label) == null && !label.Pattern.HasErrors)
                    {
                        // Since the pattern was not subsumed, we should be able to add it to the decision tree
                        throw ExceptionUtilities.Unreachable;
                    }
                }

                return true;
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
                        if (constantPattern.Value.HasErrors || constantPattern.Value.ConstantValue == null || constantPattern.Value.ConstantValue.IsBad)
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
                            // Note: we do not have any test covering this. Is it reachable?
                            // Possibly not given the simple patterns types we support today.
                            return ErrorCode.ERR_PatternIsSubsumed;
                        }

                        switch (decisionTree.Kind)
                        {
                            case DecisionTree.DecisionKind.ByValue:
                                {
                                    var byValue = (DecisionTree.ByValue)decisionTree;
                                    if (isNull)
                                    {
                                        // This should not occur, as the decision tree should contain a handler for
                                        // null earlier, for example in a type test.
                                        throw ExceptionUtilities.Unreachable;
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
                                        // Note: we do not have any test covering this. Is it reachable?
                                        // Possibly not given the simple patterns types we support today.
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
                                    // This is unreachable because the subsumption version of the decision tree
                                    // never contains guarded decision trees that are not complete, or that have
                                    // any guard other than `true`.
                                    throw ExceptionUtilities.Unreachable;
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
                                        // Note: we do not have any test covering this. Is it reachable?
                                        // Possibly not given the simple patterns types we support today.
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
                                        // if the pattern's type is already handled by the previous pattern
                                        // or the previous pattern handles all of the (non-null) input data...
                                        if (ExpressionOfTypeMatchesPatternType(
                                                declarationPattern.DeclaredType.Type.TupleUnderlyingTypeOrSelf(), type, ref _useSiteDiagnostics) == true ||
                                            ExpressionOfTypeMatchesPatternType(byType.Type, type, ref _useSiteDiagnostics) == true)
                                        {
                                            // then we check if the pattern is subsumed by the previous decision
                                            var error = CheckSubsumed(pattern, decision, inputCouldBeNull);
                                            if (error != 0)
                                            {
                                                return error;
                                            }
                                        }
                                    }

                                    if (byType.Default != null)
                                    {
                                        // Note: we do not have any test covering this. Is it reachable?
                                        // Possibly not given the simple patterns types we support today.
                                        return CheckSubsumed(pattern, byType.Default, inputCouldBeNull);
                                    }

                                    return 0;
                                }
                            case DecisionTree.DecisionKind.Guarded:
                                {
                                    // Because all guarded decision trees in the subsumption tree are
                                    // complete, we should never get here.
                                    throw ExceptionUtilities.Unreachable;
                                }
                            default:
                                throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
                        }
                    }
                case BoundKind.WildcardPattern:
                    // because we always handle `default:` last, and that is the only way to get a wildcard pattern,
                    // we should never need to see if it subsumes something else.
                    throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }
    }
}
