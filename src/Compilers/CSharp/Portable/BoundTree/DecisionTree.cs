// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundPatternSwitchStatement
    {
        private ImmutableArray<Diagnostic> _decisionTreeDiagnostics;
        private DecisionTree _decisionTree;
        private ImmutableArray<LocalSymbol> _temps;

        public ImmutableArray<Diagnostic> DecisionTreeDiagnostics
        {
            get
            {
                EnsureDecisionTree();
                Debug.Assert(_decisionTree != null);
                return _decisionTreeDiagnostics;
            }
        }

        public DecisionTree DecisionTree
        {
            get
            {
                EnsureDecisionTree();
                Debug.Assert(_decisionTree != null);
                return _decisionTree;
            }
        }

        public ImmutableArray<LocalSymbol> Temps
        {
            get
            {
                EnsureDecisionTree();
                if (_temps.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _temps, ComputeTemps(_decisionTree));
                }

                Debug.Assert(!_temps.IsDefault);
                return _temps;
            }
        }

        /// <summary>
        /// Compute the set of temps needed for the whole decision tree.
        /// </summary>
        private ImmutableArray<LocalSymbol> ComputeTemps(DecisionTree decisionTree)
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            AddTemps(decisionTree, builder);
            return builder.ToImmutableAndFree();
        }

        private void AddTemps(DecisionTree decisionTree, ArrayBuilder<LocalSymbol> builder)
        {
            if (decisionTree == null)
            {
                return;
            }

            switch (decisionTree.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    {
                        var byType = (DecisionTree.ByType)decisionTree;
                        AddTemps(byType.WhenNull, builder);
                        foreach (var td in byType.TypeAndDecision)
                        {
                            AddTemps(td.Value, builder);
                        }

                        AddTemps(byType.Default, builder);
                        return;
                    }
                case DecisionTree.DecisionKind.ByValue:
                    {
                        var byValue = (DecisionTree.ByValue)decisionTree;
                        foreach (var vd in byValue.ValueAndDecision)
                        {
                            AddTemps(vd.Value, builder);
                        }

                        AddTemps(byValue.Default, builder);
                        return;
                    }
                case DecisionTree.DecisionKind.Guarded:
                    {
                        var guarded = (DecisionTree.Guarded)decisionTree;
                        ComputeTemps(guarded.Default);
                        return;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
            }
        }

        private void EnsureDecisionTree()
        {
            if (_decisionTree == null)
            {
                // it is not a problem if this is done concurrently by multiple threads, as they
                // should all compute semantically equivalent diagnostics.
                var diagnostics = DiagnosticBag.GetInstance();
                var decisionTree = ComputeDecisionTree(diagnostics);
                this._decisionTreeDiagnostics = diagnostics.ToReadOnlyAndFree();
                Interlocked.CompareExchange(ref _decisionTree, decisionTree, null);
            }
        }

        private DecisionTree ComputeDecisionTree(DiagnosticBag diagnostics)
        {
            DecisionTreeComputer decisionTreeComputer = new DecisionTreeComputer(
                this.Binder.ContainingMemberOrLambda, diagnostics, this, this.Binder.Conversions);
            return decisionTreeComputer.ComputeDecisionTree();
        }
    }

    internal class DecisionTreeComputer
    {
        private readonly Symbol _enclosingSymbol;
        private readonly DiagnosticBag _diagnostics;
        private readonly BoundPatternSwitchStatement _switchStatement;
        private BoundPatternSwitchSection _section;
        private readonly Conversions _conversions;
        private SyntaxNode _syntax;

        public DecisionTreeComputer(
            Symbol enclosingSymbol,
            DiagnosticBag diagnostics,
            BoundPatternSwitchStatement switchStatement,
            Conversions conversions)
        {
            this._enclosingSymbol = enclosingSymbol;
            this._diagnostics = diagnostics;
            this._switchStatement = switchStatement;
            this._conversions = conversions;
        }

        internal DecisionTree ComputeDecisionTree()
        {
            Debug.Assert(_section == null);
            var expression = _switchStatement.Expression;
            if (expression.ConstantValue == null && expression.Kind != BoundKind.Local)
            {
                // unless the expression is simple enough, copy it into a local
                var localSymbol = new SynthesizedLocal(_enclosingSymbol as MethodSymbol, expression.Type, SynthesizedLocalKind.PatternMatchingTemp, _switchStatement.Syntax, false, RefKind.None);
                expression = new BoundLocal(expression.Syntax, localSymbol, null, expression.Type);
            }

            var result = DecisionTree.Create(_switchStatement.Expression, _switchStatement.Expression.Type, _enclosingSymbol);
            var subsumptionTree = DecisionTree.Create(_switchStatement.Expression, _switchStatement.Expression.Type, _enclosingSymbol);
            BoundPatternSwitchLabel defaultLabel = null;
            BoundPatternSwitchSection defaultSection = null;
            foreach (var section in _switchStatement.SwitchSections)
            {
                this._section = section;
                foreach (var label in section.SwitchLabels)
                {
                    if (label.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
                    {
                        if (defaultLabel != null)
                        {
                            // duplicate switch label will have been reported during initial binding.
                        }
                        else
                        {
                            defaultLabel = label;
                            defaultSection = section;
                        }
                    }
                    else
                    {
                        this._syntax = label.Syntax;
                        // For purposes of subsumption, we do not take into consideration the value
                        // of the input expression. Therefore we consider null possible if the type permits.
                        var subsumedErrorCode = CheckSubsumed(label.Pattern, subsumptionTree, inputCouldBeNull: true);
                        if (subsumedErrorCode != 0 && subsumedErrorCode != ErrorCode.ERR_NoImplicitConvCast)
                        {
                            if (!label.HasErrors)
                            {
                                _diagnostics.Add(subsumedErrorCode, label.Pattern.Syntax.Location);
                            }
                        }
                        else
                        {
                            AddToDecisionTree(result, label);
                            if (label.Guard == null || label.Guard.ConstantValue == ConstantValue.True)
                            {
                                // Only unconditional switch labels contribute to subsumption
                                AddToDecisionTree(subsumptionTree, label);
                            }
                        }
                    }
                }
            }

            if (defaultLabel != null)
            {
                Add(result, (e, t) => new DecisionTree.Guarded(_switchStatement.Expression, _switchStatement.Expression.Type, default(ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>>), defaultSection, null, defaultLabel));
            }

            return result;
        }

        private void AddToDecisionTree(DecisionTree decisionTree, BoundPatternSwitchLabel label)
        {
            var pattern = label.Pattern;
            var guard = label.Guard;
            if (guard?.ConstantValue == ConstantValue.False)
            {
                return;
            }

            switch (pattern.Kind)
            {
                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        AddByValue(decisionTree, constantPattern.Value, (e, t) => new DecisionTree.Guarded(e, t, default(ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>>), _section, guard, label));
                        break;
                    }
                case BoundKind.DeclarationPattern:
                    {
                        var declarationPattern = (BoundDeclarationPattern)pattern;
                        DecisionMaker maker = (e, t) => new DecisionTree.Guarded(e, t, ImmutableArray.Create(new KeyValuePair<BoundExpression, BoundExpression>(e, declarationPattern.VariableAccess)), _section, guard, label);
                        if (declarationPattern.IsVar)
                        {
                            Add(decisionTree, maker);
                        }
                        else
                        {
                            AddByType(decisionTree, declarationPattern.DeclaredType.Type, maker);
                        }
                        break;
                    }
                case BoundKind.WildcardPattern:
                // We do not yet support a wildcard pattern syntax. It is used exclusively
                // to model the "default:" case, which is handled specially in the caller.
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

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
                                            if (_conversions.ExpressionOfTypeMatchesPatternType(constantPattern.Value.Type, type) == true)
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
                                        if (_conversions.ExpressionOfTypeMatchesPatternType(declarationPattern.DeclaredType.Type.TupleUnderlyingTypeOrSelf(), type) == true)
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
                                        if (_conversions.ExpressionOfTypeMatchesPatternType(decisionTree.Type, type) == true)
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

        private delegate DecisionTree DecisionMaker(
            BoundExpression expression,
            TypeSymbol type);

        private DecisionTree AddByValue(DecisionTree decision, BoundExpression value, DecisionMaker makeDecision)
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

        private DecisionTree AddByValue(DecisionTree.Guarded guarded, BoundExpression value, DecisionMaker makeDecision)
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

        private DecisionTree AddByValue(DecisionTree.ByValue byValue, BoundExpression value, DecisionMaker makeDecision)
        {
            Debug.Assert(value.Type == byValue.Type);
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

        private DecisionTree AddByValue(DecisionTree.ByType byType, BoundExpression value, DecisionMaker makeDecision)
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
                switch (_conversions.ExpressionOfTypeMatchesPatternType(value.Type, matchedType))
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
                if (matchedType.TupleUnderlyingTypeOrSelf() == value.Type.TupleUnderlyingTypeOrSelf())
                {
                    forType = decision;
                    break;
                }
                else if (_conversions.ExpressionOfTypeMatchesPatternType(value.Type, matchedType) != false)
                {
                    break;
                }
            }

            if (forType == null)
            {
                var type = value.Type;
                var localSymbol = new SynthesizedLocal(_enclosingSymbol as MethodSymbol, type, SynthesizedLocalKind.PatternMatchingTemp, _syntax, false, RefKind.None);
                var narrowedExpression = new BoundLocal(_syntax, localSymbol, null, type);
                forType = new DecisionTree.ByValue(narrowedExpression, value.Type.TupleUnderlyingTypeOrSelf(), localSymbol);
                byType.TypeAndDecision.Add(new KeyValuePair<TypeSymbol, DecisionTree>(value.Type, forType));
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
                switch (_conversions.ExpressionOfTypeMatchesPatternType(type, MatchedType))
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

            var localSymbol = new SynthesizedLocal(_enclosingSymbol as MethodSymbol, type, SynthesizedLocalKind.PatternMatchingTemp, _syntax, false, RefKind.None);
            var expression = new BoundLocal(_syntax, localSymbol, null, type);
            var result = makeDecision(expression, type);
            Debug.Assert(result.Temp == null);
            result.Temp = localSymbol;
            byType.TypeAndDecision.Add(new KeyValuePair<TypeSymbol, DecisionTree>(type, result));
            if (_conversions.ExpressionOfTypeMatchesPatternType(byType.Type, type) == true &&
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
                if (_conversions.ExpressionOfTypeMatchesPatternType(inputType, type) == true &&
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

        private DecisionTree Add(DecisionTree decision, DecisionMaker makeDecision)
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
    }

    internal static class PatternConversionExtensions
    {
        /// <summary>
        /// Does an expression of type <paramref name="expressionType"/> "match" a pattern that looks for
        /// type <paramref name="patternType"/>?
        /// 'true' if the matched type catches all of them, 'false' if it catches none of them, and
        /// 'null' if it might catch some of them. For this test we assume the expression's value
        /// isn't null.
        /// </summary>
        public static bool? ExpressionOfTypeMatchesPatternType(this Conversions conversions, TypeSymbol expressionType, TypeSymbol patternType)
        {
            if (expressionType == patternType)
            {
                return true;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = conversions.ClassifyBuiltInConversion(expressionType, patternType, ref useSiteDiagnostics);

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

    internal abstract class DecisionTree
    {
        public readonly BoundExpression Expression;
        public readonly TypeSymbol Type;
        public LocalSymbol Temp;
        public bool MatchIsComplete;

        public enum DecisionKind { ByType, ByValue, Guarded }
        public abstract DecisionKind Kind { get; }

#if DEBUG
        internal string Dump()
        {
            var builder = new StringBuilder();
            DumpInternal(builder, "");
            return builder.ToString();
        }
        internal abstract void DumpInternal(StringBuilder builder, string indent);
#endif
        public DecisionTree(BoundExpression expression, TypeSymbol type, LocalSymbol temp)
        {
            this.Expression = expression;
            this.Type = type;
            this.Temp = temp;
            Debug.Assert(this.Expression != null);
            Debug.Assert(this.Type != null);
        }

        public static DecisionTree Create(BoundExpression expression, TypeSymbol type, Symbol enclosingSymbol)
        {
            Debug.Assert(expression.Type == type);
            LocalSymbol temp = null;
            if (expression.ConstantValue == null)
            {
                // Unless it is a constant, the decision tree acts on a copy of the input expression.
                // We create a temp to represent that copy. Lowering will assign into this temp.
                temp = new SynthesizedLocal(enclosingSymbol as MethodSymbol, type, SynthesizedLocalKind.PatternMatchingTemp, expression.Syntax, false, RefKind.None);
                expression = new BoundLocal(expression.Syntax, temp, null, type);
            }

            if (expression.Type.CanBeAssignedNull())
            {
                // We need the ByType decision tree to separate null from non-null values.
                // Note that, for the purpose of the decision tree (and subsumption), we
                // ignore the fact that the input may be a constant, and therefore always
                // or never null.
                return new ByType(expression, type, temp);
            }
            else
            {
                // If it is a (e.g. builtin) value type, we can switch on its (constant) values.
                // If it isn't a builtin, in practice we will only use the Default part of the
                // ByValue.
                return new ByValue(expression, type, temp);
            }
        }

        public class ByType : DecisionTree
        {
            public DecisionTree WhenNull;
            public readonly ArrayBuilder<KeyValuePair<TypeSymbol, DecisionTree>> TypeAndDecision =
                new ArrayBuilder<KeyValuePair<TypeSymbol, DecisionTree>>();
            public DecisionTree Default;
            public override DecisionKind Kind => DecisionKind.ByType;
            public ByType(BoundExpression expression, TypeSymbol type, LocalSymbol temp) : base(expression, type, temp) { }
#if DEBUG
            internal override void DumpInternal(StringBuilder builder, string indent)
            {
                builder.AppendLine($"{indent}ByType");
                if (WhenNull != null)
                {
                    builder.AppendLine($"{indent}  null");
                    WhenNull.DumpInternal(builder, indent + "    ");
                }

                foreach (var kv in TypeAndDecision)
                {
                    builder.AppendLine($"{indent}  {kv.Key}");
                    kv.Value.DumpInternal(builder, indent + "    ");
                }

                if (Default != null)
                {
                    builder.AppendLine($"{indent}  default");
                    Default.DumpInternal(builder, indent + "    ");
                }
            }
#endif
        }

        public class ByValue : DecisionTree
        {
            public readonly Dictionary<object, DecisionTree> ValueAndDecision =
                new Dictionary<object, DecisionTree>();
            public DecisionTree Default;
            public override DecisionKind Kind => DecisionKind.ByValue;
            public ByValue(BoundExpression expression, TypeSymbol type, LocalSymbol temp) : base(expression, type, temp) { }
#if DEBUG
            internal override void DumpInternal(StringBuilder builder, string indent)
            {
                builder.AppendLine($"{indent}ByValue");
                foreach (var kv in ValueAndDecision)
                {
                    builder.AppendLine($"{indent}  {kv.Key}");
                    kv.Value.DumpInternal(builder, indent + "    ");
                }

                if (Default != null)
                {
                    builder.AppendLine($"{indent}  default");
                    Default.DumpInternal(builder, indent + "    ");
                }
            }
#endif
        }

        public class Guarded : DecisionTree
        {
            // A sequence of bindings to be assigned before evaluation of the guard or jump to the label.
            // Each one contains the source of the assignment and the destination of the assignment, in that order.
            public readonly ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> Bindings;
            public readonly BoundPatternSwitchSection Section;
            public readonly BoundExpression Guard;
            public readonly BoundPatternSwitchLabel Label;
            public DecisionTree Default = null; // decision tree to use if the Guard is false
            public override DecisionKind Kind => DecisionKind.Guarded;
            public Guarded(
                BoundExpression expression,
                TypeSymbol type,
                ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> bindings,
                BoundPatternSwitchSection section,
                BoundExpression guard,
                BoundPatternSwitchLabel label)
                : base(expression, type, null)
            {
                this.Guard = guard;
                this.Label = label;
                this.Bindings = bindings;
                this.Section = section;
                Debug.Assert(guard?.ConstantValue != ConstantValue.False);
                base.MatchIsComplete =
                    (guard == null) || (guard.ConstantValue == ConstantValue.True);
            }
#if DEBUG
            internal override void DumpInternal(StringBuilder builder, string indent)
            {
                builder.Append($"{indent}Guarded");
                if (Guard != null)
                {
                    builder.Append($" guard={Guard.Syntax.ToString()}");
                }

                builder.AppendLine($" label={Label.Syntax.ToString()}");
            }
#endif
        }
    }
}
