// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        private BoundExpression BindIsPatternExpression(IsPatternExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeaturePatternMatching.CheckFeatureAvailability(diagnostics, node.IsKeyword);

            BoundExpression expression = BindRValueWithoutTargetType(node.Expression, diagnostics);
            bool hasErrors = IsOperandErrors(node, ref expression, diagnostics);
            TypeSymbol? expressionType = expression.Type;
            if (expressionType is null || expressionType.IsVoidType())
            {
                if (!hasErrors)
                {
                    // value expected
                    diagnostics.Add(ErrorCode.ERR_BadPatternExpression, node.Expression.Location, expression.Display);
                    hasErrors = true;
                }

                expression = BadExpression(expression.Syntax, expression);
            }

            Debug.Assert(expression.Type is { });
            BoundPattern pattern = BindPattern(node.Pattern, expression.Type, permitDesignations: true, hasErrors, diagnostics, underIsPattern: true);
            hasErrors |= pattern.HasErrors;
            return MakeIsPatternExpression(
                node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node),
                hasErrors, diagnostics);
        }

        private BoundExpression MakeIsPatternExpression(
            SyntaxNode node,
            BoundExpression expression,
            BoundPattern pattern,
            TypeSymbol boolType,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            // Note that these labels are for the convenience of the compilation of patterns, and are not necessarily emitted into the lowered code.
            LabelSymbol whenTrueLabel = new GeneratedLabelSymbol("isPatternSuccess");
            LabelSymbol whenFalseLabel = new GeneratedLabelSymbol("isPatternFailure");

            bool negated = pattern.IsNegated(out var innerPattern);
            BoundDecisionDag decisionDag = DecisionDagBuilder.CreateDecisionDagForIsPattern(
                this.Compilation, pattern.Syntax, expression, innerPattern, whenTrueLabel: whenTrueLabel, whenFalseLabel: whenFalseLabel, diagnostics);

            bool wasReported = false;
            if (!hasErrors && getConstantResult(decisionDag, negated, whenTrueLabel, whenFalseLabel) is { } constantResult)
            {
                if (!constantResult)
                {
                    Debug.Assert(expression.Type is object);
                    diagnostics.Add(ErrorCode.ERR_IsPatternImpossible, node.Location, expression.Type);
                    hasErrors = true;
                    wasReported = true;
                }
                else
                {
                    switch (pattern)
                    {
                        case BoundConstantPattern _:
                        case BoundITuplePattern _:
                            // these patterns can fail in practice
                            throw ExceptionUtilities.Unreachable();
                        case BoundRelationalPattern _:
                        case BoundTypePattern _:
                        case BoundNegatedPattern _:
                        case BoundBinaryPattern _:
                        case BoundListPattern:
                            Debug.Assert(expression.Type is object);
                            diagnostics.Add(ErrorCode.WRN_IsPatternAlways, node.Location, expression.Type);
                            wasReported = true;
                            break;
                        case BoundDiscardPattern _:
                            // we do not give a warning on this because it is an existing scenario, and it should
                            // have been obvious in source that it would always match.
                            break;
                        case BoundDeclarationPattern _:
                        case BoundRecursivePattern _:
                            // We do not give a warning on these because people do this to give a name to a value
                            break;
                    }
                }
            }
            else if (expression.ConstantValueOpt != null)
            {
                decisionDag = decisionDag.SimplifyDecisionDagIfConstantInput(expression);
                if (!hasErrors && getConstantResult(decisionDag, negated, whenTrueLabel, whenFalseLabel) is { } simplifiedResult)
                {
                    if (!simplifiedResult)
                    {
                        diagnostics.Add(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, node.Location);
                        wasReported = true;
                    }
                    else
                    {
                        switch (pattern)
                        {
                            case BoundConstantPattern _:
                                diagnostics.Add(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, node.Location);
                                wasReported = true;
                                break;
                            case BoundRelationalPattern _:
                            case BoundTypePattern _:
                            case BoundNegatedPattern _:
                            case BoundBinaryPattern _:
                            case BoundDiscardPattern _:
                                diagnostics.Add(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, node.Location);
                                wasReported = true;
                                break;
                        }
                    }
                }
            }

            if (!wasReported && diagnostics.AccumulatesDiagnostics && DecisionDagBuilder.EnableRedundantPatternsCheck(this.Compilation))
            {
                DecisionDagBuilder.CheckRedundantPatternsForIsPattern(this.Compilation, pattern.Syntax, expression, pattern, diagnostics);
            }

            // decisionDag, whenTrueLabel, and whenFalseLabel represent the decision DAG for the inner pattern,
            // after removing any outer 'not's, so consumers will need to compensate for negated patterns.
            return new BoundIsPatternExpression(
                node, expression, pattern, negated, decisionDag, whenTrueLabel: whenTrueLabel, whenFalseLabel: whenFalseLabel, boolType, hasErrors);

            static bool? getConstantResult(BoundDecisionDag decisionDag, bool negated, LabelSymbol whenTrueLabel, LabelSymbol whenFalseLabel)
            {
                if (!decisionDag.ReachableLabels.Contains(whenTrueLabel))
                {
                    return negated;
                }
                else if (!decisionDag.ReachableLabels.Contains(whenFalseLabel))
                {
                    return !negated;
                }
                return null;
            }
        }

        private BoundExpression BindSwitchExpression(SwitchExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            RoslynDebug.Assert(node is not null);

            MessageID.IDS_FeatureRecursivePatterns.CheckFeatureAvailability(diagnostics, node.SwitchKeyword);

            Binder? switchBinder = this.GetBinder(node);
            RoslynDebug.Assert(switchBinder is { });
            return switchBinder.BindSwitchExpressionCore(node, switchBinder, diagnostics);
        }

        internal virtual BoundExpression BindSwitchExpressionCore(
            SwitchExpressionSyntax node,
            Binder originalBinder,
            BindingDiagnosticBag diagnostics)
        {
            RoslynDebug.Assert(this.Next is { });
            return this.Next.BindSwitchExpressionCore(node, originalBinder, diagnostics);
        }

        internal BoundPattern BindPattern(
            PatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics,
            bool underIsPattern = false)
        {
            return node switch
            {
                DiscardPatternSyntax p => BindDiscardPattern(p, inputType, diagnostics),
                DeclarationPatternSyntax p => BindDeclarationPattern(p, inputType, permitDesignations, hasErrors, diagnostics),
                ConstantPatternSyntax p => BindConstantPatternWithFallbackToTypePattern(p, inputType, hasErrors, diagnostics),
                RecursivePatternSyntax p => BindRecursivePattern(p, inputType, permitDesignations, hasErrors, diagnostics),
                VarPatternSyntax p => BindVarPattern(p, inputType, permitDesignations, hasErrors, diagnostics),
                ParenthesizedPatternSyntax p => BindParenthesizedPattern(p, inputType, permitDesignations, hasErrors, diagnostics, underIsPattern),
                BinaryPatternSyntax p => BindBinaryPattern(p, inputType, permitDesignations, hasErrors, diagnostics),
                UnaryPatternSyntax p => BindUnaryPattern(p, inputType, hasErrors, diagnostics, underIsPattern),
                RelationalPatternSyntax p => BindRelationalPattern(p, inputType, hasErrors, diagnostics),
                TypePatternSyntax p => BindTypePattern(p, inputType, hasErrors, diagnostics),
                ListPatternSyntax p => BindListPattern(p, inputType, permitDesignations, hasErrors, diagnostics),
                SlicePatternSyntax p => BindSlicePattern(p, inputType, permitDesignations, ref hasErrors, misplaced: true, diagnostics),
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind()),
            };
        }

        private BoundPattern BindParenthesizedPattern(
            ParenthesizedPatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics,
            bool underIsPattern)
        {
            MessageID.IDS_FeatureParenthesizedPattern.CheckFeatureAvailability(diagnostics, node.OpenParenToken);
            return BindPattern(node.Pattern, inputType, permitDesignations, hasErrors, diagnostics, underIsPattern);
        }

        private BoundPattern BindSlicePattern(
            SlicePatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            ref bool hasErrors,
            bool misplaced,
            BindingDiagnosticBag diagnostics)
        {
            if (misplaced && !hasErrors)
            {
                diagnostics.Add(ErrorCode.ERR_MisplacedSlicePattern, node.Location);
                hasErrors = true;
            }

            BoundExpression? indexerAccess = null;
            BoundPattern? pattern = null;
            BoundSlicePatternReceiverPlaceholder? receiverPlaceholder = null;
            BoundSlicePatternRangePlaceholder? argumentPlaceholder = null;

            // We don't require the type to be sliceable if there's no subpattern.
            if (node.Pattern is not null)
            {
                receiverPlaceholder = new BoundSlicePatternReceiverPlaceholder(node, inputType) { WasCompilerGenerated = true };
                var systemRangeType = GetWellKnownType(WellKnownType.System_Range, diagnostics, node);
                argumentPlaceholder = new BoundSlicePatternRangePlaceholder(node, systemRangeType) { WasCompilerGenerated = true };

                TypeSymbol sliceType;
                if (inputType.IsErrorType())
                {
                    hasErrors = true;
                    sliceType = inputType;
                }
                else
                {
                    var analyzedArguments = AnalyzedArguments.GetInstance();
                    analyzedArguments.Arguments.Add(argumentPlaceholder);

                    indexerAccess = BindElementAccessCore(node, receiverPlaceholder, analyzedArguments, diagnostics).MakeCompilerGenerated();
                    indexerAccess = CheckValue(indexerAccess, BindValueKind.RValue, diagnostics);
                    Debug.Assert(indexerAccess is BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess or BoundBadExpression or BoundDynamicIndexerAccess);
                    analyzedArguments.Free();

                    if (!systemRangeType.HasUseSiteError)
                    {
                        _ = GetWellKnownTypeMember(WellKnownMember.System_Range__ctor, diagnostics, syntax: node);
                    }

                    Debug.Assert(indexerAccess.Type is not null);
                    sliceType = indexerAccess.Type;
                }

                pattern = BindPattern(node.Pattern, sliceType, permitDesignations, hasErrors, diagnostics);
            }

            return new BoundSlicePattern(node, pattern, indexerAccess, receiverPlaceholder, argumentPlaceholder, inputType: inputType, narrowedType: inputType, hasErrors);
        }

        private ImmutableArray<BoundPattern> BindListPatternSubpatterns(
            SeparatedSyntaxList<PatternSyntax> subpatterns,
            TypeSymbol inputType,
            TypeSymbol elementType,
            bool permitDesignations,
            ref bool hasErrors,
            out bool sawSlice,
            BindingDiagnosticBag diagnostics)
        {
            sawSlice = false;
            var builder = ArrayBuilder<BoundPattern>.GetInstance(subpatterns.Count);
            foreach (PatternSyntax pattern in subpatterns)
            {
                BoundPattern boundPattern;
                if (pattern is SlicePatternSyntax slice)
                {
                    boundPattern = BindSlicePattern(slice, inputType, permitDesignations, ref hasErrors, misplaced: sawSlice, diagnostics: diagnostics);
                    sawSlice = true;
                }
                else
                {
                    boundPattern = BindPattern(pattern, elementType, permitDesignations, hasErrors, diagnostics);
                }

                builder.Add(boundPattern);
            }

            return builder.ToImmutableAndFree();
        }

        private BoundListPattern BindListPattern(
            ListPatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            CheckFeatureAvailability(node, MessageID.IDS_FeatureListPattern, diagnostics);

            TypeSymbol elementType;
            BoundExpression? indexerAccess;
            BoundExpression? lengthAccess;
            TypeSymbol narrowedType = inputType.StrippedType();
            BoundListPatternReceiverPlaceholder? receiverPlaceholder;
            BoundListPatternIndexPlaceholder? argumentPlaceholder;

            if (inputType.IsDynamic())
            {
                Error(diagnostics, ErrorCode.ERR_UnsupportedTypeForListPattern, node, inputType);
            }

            if (inputType.IsErrorType() || inputType.IsDynamic())
            {
                hasErrors = true;
                elementType = inputType;
                indexerAccess = null;
                lengthAccess = null;
                receiverPlaceholder = null;
                argumentPlaceholder = null;
            }
            else
            {
                hasErrors |= !BindLengthAndIndexerForListPattern(node, narrowedType, diagnostics, out indexerAccess, out lengthAccess, out receiverPlaceholder, out argumentPlaceholder);

                Debug.Assert(indexerAccess!.Type is not null);
                elementType = indexerAccess.Type;
            }

            ImmutableArray<BoundPattern> subpatterns = BindListPatternSubpatterns(
                node.Patterns, inputType: narrowedType, elementType: elementType,
                permitDesignations, ref hasErrors, out bool sawSlice, diagnostics);

            BindPatternDesignation(
                node.Designation,
                declType: TypeWithAnnotations.Create(narrowedType, NullableAnnotation.NotAnnotated),
                permitDesignations, typeSyntax: null, diagnostics, ref hasErrors,
                out Symbol? variableSymbol, out BoundExpression? variableAccess);

            return new BoundListPattern(
                syntax: node, subpatterns: subpatterns, hasSlice: sawSlice, lengthAccess: lengthAccess,
                indexerAccess: indexerAccess, receiverPlaceholder, argumentPlaceholder, variable: variableSymbol,
                variableAccess: variableAccess, inputType: inputType, narrowedType: narrowedType, hasErrors);
        }

        /// <summary>
        /// Types which list-patterns can be used on (ie. countable and indexable ones) are assumed to have
        /// non-negative lengths.
        /// </summary>
        private bool IsCountableAndIndexable(SyntaxNode node, TypeSymbol inputType, out PropertySymbol? lengthProperty)
        {
            var success = BindLengthAndIndexerForListPattern(node, inputType, BindingDiagnosticBag.Discarded,
                indexerAccess: out _, out var lengthAccess, receiverPlaceholder: out _, argumentPlaceholder: out _);
            lengthProperty = success ? GetPropertySymbol(lengthAccess, out _, out _) : null;
            return success;
        }

        private bool BindLengthAndIndexerForListPattern(SyntaxNode node, TypeSymbol inputType, BindingDiagnosticBag diagnostics,
            out BoundExpression indexerAccess, out BoundExpression lengthAccess, out BoundListPatternReceiverPlaceholder? receiverPlaceholder, out BoundListPatternIndexPlaceholder argumentPlaceholder)
        {
            Debug.Assert(!inputType.IsDynamic());

            bool hasErrors = false;
            receiverPlaceholder = new BoundListPatternReceiverPlaceholder(node, inputType) { WasCompilerGenerated = true };
            if (inputType.IsSZArray())
            {
                hasErrors |= !TryGetSpecialTypeMember(Compilation, SpecialMember.System_Array__Length, node, diagnostics, out PropertySymbol lengthProperty);
                if (lengthProperty is not null)
                {
                    lengthAccess = new BoundPropertyAccess(node, receiverPlaceholder, initialBindingReceiverIsSubjectToCloning: ThreeState.False, lengthProperty, autoPropertyAccessorKind: AccessorKind.Unknown, LookupResultKind.Viable, lengthProperty.Type) { WasCompilerGenerated = true };
                }
                else
                {
                    lengthAccess = new BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray<Symbol?>.Empty, ImmutableArray<BoundExpression>.Empty, CreateErrorType(), hasErrors: true) { WasCompilerGenerated = true };
                }
            }
            else
            {
                if (!TryBindLengthOrCount(node, receiverPlaceholder, out lengthAccess, diagnostics))
                {
                    hasErrors = true;
                    Error(diagnostics, ErrorCode.ERR_ListPatternRequiresLength, node, inputType);
                }
            }

            var analyzedArguments = AnalyzedArguments.GetInstance();
            var systemIndexType = GetWellKnownType(WellKnownType.System_Index, diagnostics, node);
            argumentPlaceholder = new BoundListPatternIndexPlaceholder(node, systemIndexType) { WasCompilerGenerated = true };
            analyzedArguments.Arguments.Add(argumentPlaceholder);

            indexerAccess = BindElementAccessCore(node, receiverPlaceholder, analyzedArguments, diagnostics).MakeCompilerGenerated();
            indexerAccess = CheckValue(indexerAccess, BindValueKind.RValue, diagnostics);
            Debug.Assert(indexerAccess is BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess or BoundBadExpression or BoundDynamicIndexerAccess);
            analyzedArguments.Free();

            if (!systemIndexType.HasUseSiteError)
            {
                // Check required well-known member.
                _ = GetWellKnownTypeMember(WellKnownMember.System_Index__ctor, diagnostics, syntax: node);
            }

            return !hasErrors && !lengthAccess.HasErrors && !indexerAccess.HasErrors;
        }

        private static BoundPattern BindDiscardPattern(DiscardPatternSyntax node, TypeSymbol inputType, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureRecursivePatterns.CheckFeatureAvailability(diagnostics, node);
            return new BoundDiscardPattern(node, inputType: inputType, narrowedType: inputType);
        }

        private BoundPattern BindConstantPatternWithFallbackToTypePattern(
            ConstantPatternSyntax node,
            TypeSymbol inputType,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            return BindConstantPatternWithFallbackToTypePattern(node, node.Expression, inputType, hasErrors, diagnostics);
        }

        internal BoundPattern BindConstantPatternWithFallbackToTypePattern(
            SyntaxNode node,
            ExpressionSyntax expression,
            TypeSymbol inputType,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            ExpressionSyntax innerExpression = SkipParensAndNullSuppressions(expression, diagnostics, ref hasErrors);
            var convertedExpression = BindExpressionOrTypeForPattern(inputType, innerExpression, ref hasErrors, diagnostics, out var constantValueOpt, out bool wasExpression, out Conversion patternConversion);
            if (wasExpression)
            {
                var convertedType = convertedExpression.Type ?? inputType;
                if (convertedType.SpecialType == SpecialType.System_String && inputType.IsSpanOrReadOnlySpanChar())
                {
                    convertedType = inputType;
                }

                if ((constantValueOpt?.IsNumeric == true) && ShouldBlockINumberBaseConversion(patternConversion, inputType))
                {
                    // Cannot use a numeric constant or relational pattern on '{0}' because it inherits from or extends 'INumberBase&lt;T&gt;'. Consider using a type pattern to narrow to a specific numeric type.
                    diagnostics.Add(ErrorCode.ERR_CannotMatchOnINumberBase, node.Location, inputType);
                }

                return new BoundConstantPattern(
                    node, convertedExpression, constantValueOpt ?? ConstantValue.Bad, inputType, convertedType, hasErrors || constantValueOpt is null);
            }
            else
            {
                if (!hasErrors)
                    CheckFeatureAvailability(innerExpression, MessageID.IDS_FeatureTypePattern, diagnostics);

                var boundType = (BoundTypeExpression)convertedExpression;
                bool isExplicitNotNullTest = boundType.Type.SpecialType == SpecialType.System_Object;
                return new BoundTypePattern(node, boundType, isExplicitNotNullTest, inputType, boundType.Type, hasErrors);
            }
        }

        private bool ShouldBlockINumberBaseConversion(Conversion patternConversion, TypeSymbol inputType)
        {
            // We want to block constant and relation patterns that have an input type constrained to or inherited from INumberBase<T>, if we don't
            // know how to represent the constant being matched against in the input type. For example, `1.0 is 1` will work when written inline, but
            // will fail if the input type is `INumberBase<T>`. We block this now so that we can make make it work as expected in the future without
            // being a breaking change.

            if (patternConversion.IsIdentity || patternConversion.IsConstantExpression || patternConversion.IsNumeric)
            {
                return false;
            }

            var interfaces = inputType is TypeParameterSymbol typeParam ? typeParam.EffectiveInterfacesNoUseSiteDiagnostics : inputType.AllInterfacesNoUseSiteDiagnostics;
            return interfaces.Any(static (i, _) => i.IsWellKnownINumberBaseType(), 0) || inputType.IsWellKnownINumberBaseType();
        }

        private static ExpressionSyntax SkipParensAndNullSuppressions(ExpressionSyntax e, BindingDiagnosticBag diagnostics, ref bool hasErrors)
        {
            while (true)
            {
                switch (e.Kind())
                {
                    case SyntaxKind.DefaultLiteralExpression:
                        diagnostics.Add(ErrorCode.ERR_DefaultPattern, e.Location);
                        hasErrors = true;
                        return e;
                    case SyntaxKind.ParenthesizedExpression:
                        e = ((ParenthesizedExpressionSyntax)e).Expression;
                        continue;
                    case SyntaxKind.SuppressNullableWarningExpression:
                        diagnostics.Add(ErrorCode.ERR_IllegalSuppression, e.Location);
                        hasErrors = true;
                        e = ((PostfixUnaryExpressionSyntax)e).Operand;
                        continue;
                    default:
                        return e;
                }
            }
        }

        /// <summary>
        /// Binds the expression for a pattern.  Sets <paramref name="wasExpression"/> if it was a type rather than an expression,
        /// and in that case it returns a <see cref="BoundTypeExpression"/>.
        /// </summary>
        private BoundExpression BindExpressionOrTypeForPattern(
            TypeSymbol inputType,
            ExpressionSyntax patternExpression,
            ref bool hasErrors,
            BindingDiagnosticBag diagnostics,
            out ConstantValue? constantValueOpt,
            out bool wasExpression,
            out Conversion patternExpressionConversion)
        {
            constantValueOpt = null;
            BoundExpression expression = BindTypeOrRValue(patternExpression, diagnostics);
            wasExpression = expression.Kind != BoundKind.TypeExpression;
            if (wasExpression)
            {
                return BindExpressionForPatternContinued(expression, inputType, patternExpression, ref hasErrors, diagnostics, out constantValueOpt, out patternExpressionConversion);
            }
            else
            {
                Debug.Assert(expression is { Kind: BoundKind.TypeExpression, Type: { } });
                hasErrors |= CheckValidPatternType(patternExpression, inputType, expression.Type, diagnostics: diagnostics);
                patternExpressionConversion = Conversion.NoConversion;
                return expression;
            }
        }

        /// <summary>
        /// Binds the expression for an is-type right-hand-side, in case it does not bind as a type.
        /// </summary>
        private BoundExpression BindExpressionForPattern(
            TypeSymbol inputType,
            ExpressionSyntax patternExpression,
            ref bool hasErrors,
            BindingDiagnosticBag diagnostics,
            out ConstantValue? constantValueOpt,
            out bool wasExpression,
            out Conversion patternExpressionConversion)
        {
            constantValueOpt = null;
            var expression = BindExpression(patternExpression, diagnostics: diagnostics, invoked: false, indexed: false);
            expression = CheckValue(expression, BindValueKind.RValue, diagnostics);
            wasExpression = expression.Kind switch { BoundKind.BadExpression => false, BoundKind.TypeExpression => false, _ => true };
            patternExpressionConversion = Conversion.NoConversion;
            return wasExpression ? BindExpressionForPatternContinued(expression, inputType, patternExpression, ref hasErrors, diagnostics, out constantValueOpt, out patternExpressionConversion) : expression;
        }

        private BoundExpression BindExpressionForPatternContinued(
            BoundExpression expression,
            TypeSymbol inputType,
            ExpressionSyntax patternExpression,
            ref bool hasErrors,
            BindingDiagnosticBag diagnostics,
            out ConstantValue? constantValueOpt,
            out Conversion patternExpressionConversion)
        {
            BoundExpression convertedExpression = ConvertPatternExpression(
                inputType, patternExpression, expression, out constantValueOpt, hasErrors, diagnostics, out patternExpressionConversion);

            ConstantValueUtils.CheckLangVersionForConstantValue(convertedExpression, diagnostics);

            if (!convertedExpression.HasErrors && !hasErrors)
            {
                if (constantValueOpt == null)
                {
                    var strippedInputType = inputType.StrippedType();
                    if (strippedInputType.Kind is not SymbolKind.ErrorType and not SymbolKind.DynamicType and not SymbolKind.TypeParameter &&
                        strippedInputType.SpecialType is not SpecialType.System_Object and not SpecialType.System_ValueType)
                    {
                        diagnostics.Add(ErrorCode.ERR_ConstantValueOfTypeExpected, patternExpression.Location, strippedInputType);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_ConstantExpected, patternExpression.Location);
                    }
                    hasErrors = true;
                }
                else if (inputType.IsPointerType())
                {
                    CheckFeatureAvailability(patternExpression, MessageID.IDS_FeatureNullPointerConstantPattern, diagnostics);
                }
            }

            if (convertedExpression.Type is null && constantValueOpt != ConstantValue.Null)
            {
                Debug.Assert(hasErrors);
                convertedExpression = BindToTypeForErrorRecovery(convertedExpression);
            }

            return convertedExpression;
        }

        internal BoundExpression ConvertPatternExpression(
            TypeSymbol inputType,
            CSharpSyntaxNode node,
            BoundExpression expression,
            out ConstantValue? constantValue,
            bool hasErrors,
            BindingDiagnosticBag diagnostics,
            out Conversion patternExpressionConversion)
        {
            BoundExpression convertedExpression;

            // If we are pattern-matching against an open type, we do not convert the constant to the type of the input.
            // This permits us to match a value of type `IComparable<T>` with a pattern of type `int`.
            if (inputType.ContainsTypeParameter())
            {
                convertedExpression = expression;
                // If the expression does not have a constant value, an error will be reported in the caller
                if (!hasErrors && expression.ConstantValueOpt is object)
                {
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    if (expression.ConstantValueOpt == ConstantValue.Null)
                    {
                        // Pointers are value types, but they can be assigned null, so they can be matched against null.
                        if (inputType.IsNonNullableValueType() && !inputType.IsPointerOrFunctionPointer())
                        {
                            // We do not permit matching null against a struct type.
                            diagnostics.Add(ErrorCode.ERR_ValueCantBeNull, expression.Syntax.Location, inputType);
                            hasErrors = true;
                        }
                    }
                    else
                    {
                        RoslynDebug.Assert(expression.Type is { });
                        ConstantValue match = ExpressionOfTypeMatchesPatternType(Conversions, inputType, expression.Type, ref useSiteInfo, out _, operandConstantValue: null);
                        if (match == ConstantValue.False || match == ConstantValue.Bad)
                        {
                            diagnostics.Add(ErrorCode.ERR_PatternWrongType, expression.Syntax.Location, inputType, expression.Display);
                            hasErrors = true;
                        }
                    }

                    if (!hasErrors)
                    {
                        var requiredVersion = MessageID.IDS_FeatureRecursivePatterns.RequiredVersion();
                        patternExpressionConversion = this.Conversions.ClassifyConversionFromExpression(expression, inputType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                        if (Compilation.LanguageVersion < requiredVersion && !patternExpressionConversion.IsImplicit)
                        {
                            diagnostics.Add(ErrorCode.ERR_ConstantPatternVsOpenType,
                                expression.Syntax.Location, inputType, expression.Display, new CSharpRequiredLanguageVersion(requiredVersion));
                        }
                    }
                    else
                    {
                        patternExpressionConversion = Conversion.NoConversion;
                    }

                    diagnostics.Add(node, useSiteInfo);
                }
                else
                {
                    patternExpressionConversion = Conversion.NoConversion;
                }
            }
            else
            {
                if (expression.Type?.SpecialType == SpecialType.System_String && inputType.IsSpanOrReadOnlySpanChar())
                {
                    if (MessageID.IDS_FeatureSpanCharConstantPattern.CheckFeatureAvailability(diagnostics, Compilation, node.Location))
                    {
                        // report missing member and use site diagnostics
                        bool isReadOnlySpan = inputType.IsReadOnlySpanChar();
                        _ = GetWellKnownTypeMember(
                            isReadOnlySpan ? WellKnownMember.System_MemoryExtensions__SequenceEqual_ReadOnlySpan_T : WellKnownMember.System_MemoryExtensions__SequenceEqual_Span_T,
                            diagnostics,
                            syntax: node);
                        _ = GetWellKnownTypeMember(WellKnownMember.System_MemoryExtensions__AsSpan_String, diagnostics, syntax: node);
                        _ = GetWellKnownTypeMember(isReadOnlySpan ? WellKnownMember.System_ReadOnlySpan_T__get_Length : WellKnownMember.System_Span_T__get_Length,
                            diagnostics,
                            syntax: node);
                    }

                    convertedExpression = BindToNaturalType(expression, diagnostics);

                    constantValue = convertedExpression.ConstantValueOpt;
                    if (constantValue == ConstantValue.Null)
                    {
                        diagnostics.Add(ErrorCode.ERR_PatternSpanCharCannotBeStringNull, convertedExpression.Syntax.Location, inputType);
                    }

                    patternExpressionConversion = Conversion.NoConversion;

                    return convertedExpression;
                }

                // This will allow user-defined conversions, even though they're not permitted here.  This is acceptable
                // because the result of a user-defined conversion does not have a ConstantValue. A constant pattern
                // requires a constant value so we'll report a diagnostic to that effect later.
                convertedExpression = GenerateConversionForAssignment(inputType, expression, diagnostics, out patternExpressionConversion);

                if (convertedExpression.Kind == BoundKind.Conversion)
                {
                    var conversion = (BoundConversion)convertedExpression;
                    BoundExpression operand = conversion.Operand;
                    if (inputType.IsNullableType() && (convertedExpression.ConstantValueOpt == null || !convertedExpression.ConstantValueOpt.IsNull))
                    {
                        // Null is a special case here because we want to compare null to the Nullable<T> itself, not to the underlying type.
                        // We are not interested in the diagnostic that get created here
                        convertedExpression = CreateConversion(operand, inputType.GetNullableUnderlyingType(), BindingDiagnosticBag.Discarded);
                    }
                    else if ((conversion.ConversionKind == ConversionKind.Boxing || conversion.ConversionKind == ConversionKind.ImplicitReference)
                        && operand.ConstantValueOpt != null && convertedExpression.ConstantValueOpt == null)
                    {
                        // A boxed constant (or string converted to object) is a special case because we prefer
                        // to compare to the pre-converted value by casting the input value to the type of the constant
                        // (that is, unboxing or downcasting it) and then testing the resulting value using primitives.
                        // That is much more efficient than calling object.Equals(x, y), and we can share the downcasted
                        // input value among many constant tests.
                        convertedExpression = operand;
                    }
                    else if (conversion.ConversionKind == ConversionKind.ImplicitNullToPointer ||
                        (conversion.ConversionKind == ConversionKind.NoConversion && convertedExpression.Type?.IsErrorType() == true))
                    {
                        convertedExpression = operand;
                    }
                }
            }

            constantValue = convertedExpression.ConstantValueOpt;
            return convertedExpression;
        }

        /// <summary>
        /// Check that the pattern type is valid for the operand. Return true if an error was reported.
        /// </summary>
        private bool CheckValidPatternType(
            SyntaxNode typeSyntax,
            TypeSymbol inputType,
            TypeSymbol patternType,
            BindingDiagnosticBag diagnostics)
        {
            RoslynDebug.Assert((object)inputType != null);
            RoslynDebug.Assert((object)patternType != null);

            if (inputType.IsErrorType() || patternType.IsErrorType())
            {
                return false;
            }
            else if (inputType.IsPointerOrFunctionPointer() || patternType.IsPointerOrFunctionPointer())
            {
                // pattern-matching is not permitted for pointer types
                diagnostics.Add(ErrorCode.ERR_PointerTypeInPatternMatching, typeSyntax.Location);
                return true;
            }
            else if (patternType.IsNullableType())
            {
                // It is an error to use pattern-matching with a nullable type, because you'll never get null. Use the underlying type.
                Error(diagnostics, ErrorCode.ERR_PatternNullableType, typeSyntax, patternType.GetNullableUnderlyingType());
                return true;
            }
            else if (typeSyntax is NullableTypeSyntax)
            {
                Error(diagnostics, ErrorCode.ERR_PatternNullableType, typeSyntax, patternType);
                return true;
            }
            else if (patternType.IsStatic)
            {
                Error(diagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, patternType);
                return true;
            }
            else
            {
                if (patternType.IsDynamic())
                {
                    Error(diagnostics, ErrorCode.ERR_PatternDynamicType, typeSyntax);
                    return true;
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                ConstantValue matchPossible = ExpressionOfTypeMatchesPatternType(
                    Conversions, inputType, patternType, ref useSiteInfo, out Conversion conversion, operandConstantValue: null, operandCouldBeNull: true);
                diagnostics.Add(typeSyntax, useSiteInfo);
                if (matchPossible != ConstantValue.False && matchPossible != ConstantValue.Bad)
                {
                    if (!conversion.Exists && (inputType.ContainsTypeParameter() || patternType.ContainsTypeParameter()))
                    {
                        // permit pattern-matching when one of the types is an open type in C# 7.1.
                        LanguageVersion requiredVersion = MessageID.IDS_FeatureGenericPatternMatching.RequiredVersion();
                        if (requiredVersion > Compilation.LanguageVersion)
                        {
                            Error(diagnostics, ErrorCode.ERR_PatternWrongGenericTypeInVersion, typeSyntax,
                                inputType, patternType,
                                Compilation.LanguageVersion.ToDisplayString(),
                                new CSharpRequiredLanguageVersion(requiredVersion));
                            return true;
                        }
                    }
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_PatternWrongType, typeSyntax, inputType, patternType);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Does an expression of type <paramref name="expressionType"/> "match" a pattern that looks for
        /// type <paramref name="patternType"/>?
        ///  - <see cref="ConstantValue.True"/> if the matched type catches all of them
        ///  - <see cref="ConstantValue.False"/> if it catches none of them
        ///  - <see cref="ConstantValue.Bad"/> - compiler doesn't support the type check, i.e. cannot perform it, even at runtime
        ///  - 'null' if it might catch some of them.
        /// </summary>
        internal static ConstantValue ExpressionOfTypeMatchesPatternType(
            Conversions conversions,
            TypeSymbol expressionType,
            TypeSymbol patternType,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            out Conversion conversion,
            ConstantValue? operandConstantValue = null,
            bool operandCouldBeNull = false)
        {
            RoslynDebug.Assert((object)expressionType != null);

            // Short-circuit a common case.  This also improves recovery for some error
            // cases, e.g. when the type is void.
            if (expressionType.Equals(patternType, TypeCompareKind.AllIgnoreOptions))
            {
                conversion = Conversion.Identity;
                return ConstantValue.True;
            }

            if (expressionType.IsDynamic())
            {
                // if operand is the dynamic type, we do the same thing as though it were object
                expressionType = conversions.CorLibrary.GetSpecialType(SpecialType.System_Object);
            }

            conversion = conversions.ClassifyBuiltInConversion(expressionType, patternType, isChecked: false, ref useSiteInfo);
            ConstantValue result = Binder.GetIsOperatorConstantResult(expressionType, patternType, conversion.Kind, operandConstantValue, operandCouldBeNull);

            // Don't need to worry about checked user-defined operators
            Debug.Assert(!conversion.IsUserDefined || result == ConstantValue.False || result == ConstantValue.Bad);

            return result;
        }

        private BoundPattern BindDeclarationPattern(
            DeclarationPatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;
            BoundTypeExpression boundDeclType = BindTypeForPattern(typeSyntax, inputType, diagnostics, ref hasErrors);
            BindPatternDesignation(
                designation: node.Designation, declType: boundDeclType.TypeWithAnnotations, permitDesignations, typeSyntax, diagnostics,
                hasErrors: ref hasErrors, variableSymbol: out Symbol? variableSymbol, variableAccess: out BoundExpression? variableAccess);
            return new BoundDeclarationPattern(node, boundDeclType, isVar: false, variableSymbol, variableAccess, inputType: inputType, narrowedType: boundDeclType.Type, hasErrors);
        }

        private BoundTypeExpression BindTypeForPattern(
            TypeSyntax typeSyntax,
            TypeSymbol inputType,
            BindingDiagnosticBag diagnostics,
            ref bool hasErrors)
        {
            RoslynDebug.Assert(inputType is { });
            TypeWithAnnotations declType = BindType(typeSyntax, diagnostics, out AliasSymbol aliasOpt);
            Debug.Assert(declType.HasType);
            BoundTypeExpression boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, typeWithAnnotations: declType);
            hasErrors |= CheckValidPatternType(typeSyntax, inputType, declType.Type, diagnostics: diagnostics);
            return boundDeclType;
        }

        private void BindPatternDesignation(
            VariableDesignationSyntax? designation,
            TypeWithAnnotations declType,
            bool permitDesignations,
            TypeSyntax? typeSyntax,
            BindingDiagnosticBag diagnostics,
            ref bool hasErrors,
            out Symbol? variableSymbol,
            out BoundExpression? variableAccess)
        {
            switch (designation)
            {
                case SingleVariableDesignationSyntax singleVariableDesignation:
                    SyntaxToken identifier = singleVariableDesignation.Identifier;
                    SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

                    if (!permitDesignations && !identifier.IsMissing)
                        diagnostics.Add(ErrorCode.ERR_DesignatorBeneathPatternCombinator, identifier.GetLocation());

                    if (localSymbol is { })
                    {
                        RoslynDebug.Assert(ContainingMemberOrLambda is { });
                        if ((InConstructorInitializer || InFieldInitializer) && ContainingMemberOrLambda.ContainingSymbol.Kind == SymbolKind.NamedType)
                            CheckFeatureAvailability(designation, MessageID.IDS_FeatureExpressionVariablesInQueriesAndInitializers, diagnostics);

                        localSymbol.SetTypeWithAnnotations(declType);

                        // Check for variable declaration errors.
                        hasErrors |= localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                        if (!hasErrors)
                            CheckRestrictedTypeInAsyncMethod(this.ContainingMemberOrLambda, declType.Type, diagnostics, typeSyntax ?? (SyntaxNode)designation);

                        variableSymbol = localSymbol;
                        variableAccess = new BoundLocal(
                            syntax: designation, localSymbol: localSymbol, localSymbol.IsVar ? BoundLocalDeclarationKind.WithInferredType : BoundLocalDeclarationKind.WithExplicitType, constantValueOpt: null, isNullableUnknown: false, type: declType.Type);
                        return;
                    }
                    else
                    {
                        // We should have the right binder in the chain for a script or interactive, so we use the field for the pattern.
                        Debug.Assert(designation.SyntaxTree.Options.Kind != SourceCodeKind.Regular);
                        GlobalExpressionVariable expressionVariableField = LookupDeclaredField(singleVariableDesignation);
                        expressionVariableField.SetTypeWithAnnotations(declType, BindingDiagnosticBag.Discarded);
                        BoundExpression receiver = SynthesizeReceiver(designation, expressionVariableField, diagnostics);

                        variableSymbol = expressionVariableField;
                        variableAccess = new BoundFieldAccess(
                            syntax: designation, receiver: receiver, fieldSymbol: expressionVariableField, constantValueOpt: null, hasErrors: hasErrors);
                        return;
                    }
                case DiscardDesignationSyntax _:
                case null:
                    variableSymbol = null;
                    variableAccess = null;
                    return;
                default:
                    throw ExceptionUtilities.UnexpectedValue(designation.Kind());
            }
        }

        private TypeWithAnnotations BindRecursivePatternType(
            TypeSyntax? typeSyntax,
            TypeSymbol inputType,
            BindingDiagnosticBag diagnostics,
            ref bool hasErrors,
            out BoundTypeExpression? boundDeclType)
        {
            if (typeSyntax != null)
            {
                boundDeclType = BindTypeForPattern(typeSyntax, inputType, diagnostics, ref hasErrors);
                return boundDeclType.TypeWithAnnotations;
            }
            else
            {
                boundDeclType = null;
                // remove the nullable part of the input's type; e.g. a nullable int becomes an int in a recursive pattern
                return TypeWithAnnotations.Create(inputType.StrippedType(), NullableAnnotation.NotAnnotated);
            }
        }

        // Work around https://github.com/dotnet/roslyn/issues/20648: The compiler's internal APIs such as `declType.IsTupleType`
        // do not correctly treat the non-generic struct `System.ValueTuple` as a tuple type.  We explicitly perform the tests
        // required to identify it.  When that bug is fixed we should be able to remove this code and its callers.
        internal static bool IsZeroElementTupleType(TypeSymbol type)
        {
            return type.IsStructType() && type.Name == "ValueTuple" && type.GetArity() == 0 &&
                type.ContainingSymbol is var declContainer && declContainer.Kind == SymbolKind.Namespace && declContainer.Name == "System" &&
                (declContainer.ContainingSymbol as NamespaceSymbol)?.IsGlobalNamespace == true;
        }

        private BoundPattern BindRecursivePattern(
            RecursivePatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureRecursivePatterns.CheckFeatureAvailability(diagnostics, node);

            if (inputType.IsPointerOrFunctionPointer())
            {
                diagnostics.Add(ErrorCode.ERR_PointerTypeInPatternMatching, node.Location);
                hasErrors = true;
                inputType = CreateErrorType();
            }

            TypeSyntax? typeSyntax = node.Type;
            TypeWithAnnotations declTypeWithAnnotations = BindRecursivePatternType(typeSyntax, inputType, diagnostics, ref hasErrors, out BoundTypeExpression? boundDeclType);
            TypeSymbol declType = declTypeWithAnnotations.Type;

            MethodSymbol? deconstructMethod = null;
            ImmutableArray<BoundPositionalSubpattern> deconstructionSubpatterns = default;
            if (node.PositionalPatternClause != null)
            {
                PositionalPatternClauseSyntax positionalClause = node.PositionalPatternClause;
                var patternsBuilder = ArrayBuilder<BoundPositionalSubpattern>.GetInstance(positionalClause.Subpatterns.Count);
                if (IsZeroElementTupleType(declType))
                {
                    // Work around https://github.com/dotnet/roslyn/issues/20648: The compiler's internal APIs such as `declType.IsTupleType`
                    // do not correctly treat the non-generic struct `System.ValueTuple` as a tuple type.  We explicitly perform the tests
                    // required to identify it.  When that bug is fixed we should be able to remove this if statement.
                    BindValueTupleSubpatterns(
                        positionalClause, declType, ImmutableArray<TypeWithAnnotations>.Empty, permitDesignations, ref hasErrors, patternsBuilder, diagnostics);
                }
                else if (declType.IsTupleType)
                {
                    // It is a tuple type. Work according to its elements
                    BindValueTupleSubpatterns(positionalClause, declType, declType.TupleElementTypesWithAnnotations, permitDesignations, ref hasErrors, patternsBuilder, diagnostics);
                }
                else
                {
                    // It is not a tuple type. Seek an appropriate Deconstruct method.
                    var inputPlaceholder = new BoundImplicitReceiver(positionalClause, declType); // A fake receiver expression to permit us to reuse binding logic
                    var deconstructDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
                    BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                        positionalClause.Subpatterns.Count, inputPlaceholder, positionalClause,
                        deconstructDiagnostics, outPlaceholders: out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders,
                        out bool anyDeconstructCandidates);
                    if (!anyDeconstructCandidates &&
                        ShouldUseITupleForRecursivePattern(node, declType, diagnostics, out var iTupleType, out var iTupleGetLength, out var iTupleGetItem))
                    {
                        // There was no Deconstruct, but the constraints for the use of ITuple are satisfied.
                        // Use that and forget any errors from trying to bind Deconstruct.
                        deconstructDiagnostics.Free();
                        BindITupleSubpatterns(positionalClause, patternsBuilder, permitDesignations, diagnostics);
                        deconstructionSubpatterns = patternsBuilder.ToImmutableAndFree();
                        return new BoundITuplePattern(node, iTupleGetLength, iTupleGetItem, deconstructionSubpatterns, inputType, iTupleType, hasErrors);
                    }
                    else
                    {
                        diagnostics.AddRangeAndFree(deconstructDiagnostics);
                    }

                    deconstructMethod = BindDeconstructSubpatterns(
                        positionalClause, permitDesignations, deconstruct, outPlaceholders, patternsBuilder, ref hasErrors, diagnostics);
                }

                deconstructionSubpatterns = patternsBuilder.ToImmutableAndFree();
            }

            ImmutableArray<BoundPropertySubpattern> properties = default;
            if (node.PropertyPatternClause != null)
            {
                properties = BindPropertyPatternClause(node.PropertyPatternClause, declType, permitDesignations, diagnostics, ref hasErrors);
            }

            BindPatternDesignation(
                node.Designation, declTypeWithAnnotations, permitDesignations, typeSyntax, diagnostics,
                ref hasErrors, out Symbol? variableSymbol, out BoundExpression? variableAccess);
            bool isExplicitNotNullTest =
                node.Designation is null &&
                boundDeclType is null &&
                properties.IsDefaultOrEmpty &&
                deconstructMethod is null &&
                deconstructionSubpatterns.IsDefault;
            return new BoundRecursivePattern(
                syntax: node, declaredType: boundDeclType, deconstructMethod: deconstructMethod,
                deconstruction: deconstructionSubpatterns, properties: properties, isExplicitNotNullTest: isExplicitNotNullTest,
                variable: variableSymbol, variableAccess: variableAccess, inputType: inputType,
                narrowedType: boundDeclType?.Type ?? inputType.StrippedType(), hasErrors);
        }

        private MethodSymbol? BindDeconstructSubpatterns(
            PositionalPatternClauseSyntax node,
            bool permitDesignations,
            BoundExpression deconstruct,
            ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders,
            ArrayBuilder<BoundPositionalSubpattern> patterns,
            ref bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            var deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
            if (deconstructMethod is null)
                hasErrors = true;

            int skippedExtensionParameters = deconstructMethod?.IsExtensionMethod == true ? 1 : 0;
            for (int i = 0; i < node.Subpatterns.Count; i++)
            {
                var subPattern = node.Subpatterns[i];

                bool isError = hasErrors || outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                ParameterSymbol? parameter = null;
                if (!isError)
                {
                    int parameterIndex = i + skippedExtensionParameters;
                    if (parameterIndex < deconstructMethod!.ParameterCount)
                    {
                        parameter = deconstructMethod.Parameters[parameterIndex];
                    }
                    if (subPattern.NameColon != null)
                    {
                        // Check that the given name is the same as the corresponding parameter of the method.
                        if (parameter is { })
                        {
                            string name = subPattern.NameColon.Name.Identifier.ValueText;
                            string parameterName = parameter.Name;
                            if (name != parameterName)
                            {
                                diagnostics.Add(ErrorCode.ERR_DeconstructParameterNameMismatch, subPattern.NameColon.Name.Location, name, parameterName);
                            }
                        }
                    }
                    else if (subPattern.ExpressionColon != null)
                    {
                        MessageID.IDS_FeatureExtendedPropertyPatterns.CheckFeatureAvailability(diagnostics, subPattern.ExpressionColon.ColonToken);

                        diagnostics.Add(ErrorCode.ERR_IdentifierExpected, subPattern.ExpressionColon.Expression.Location);
                    }
                }

                var boundSubpattern = new BoundPositionalSubpattern(
                    subPattern,
                    parameter,
                    BindPattern(subPattern.Pattern, elementType, permitDesignations, isError, diagnostics)
                    );
                patterns.Add(boundSubpattern);
            }

            return deconstructMethod;
        }

        private void BindITupleSubpatterns(
            PositionalPatternClauseSyntax node,
            ArrayBuilder<BoundPositionalSubpattern> patterns,
            bool permitDesignations,
            BindingDiagnosticBag diagnostics)
        {
            var objectType = Compilation.GetSpecialType(SpecialType.System_Object);
            foreach (var subpatternSyntax in node.Subpatterns)
            {
                if (subpatternSyntax.NameColon != null)
                {
                    // error: name not permitted in ITuple deconstruction
                    diagnostics.Add(ErrorCode.ERR_ArgumentNameInITuplePattern, subpatternSyntax.NameColon.Location);
                }
                else if (subpatternSyntax.ExpressionColon != null)
                {
                    diagnostics.Add(ErrorCode.ERR_IdentifierExpected, subpatternSyntax.ExpressionColon.Expression.Location);
                }

                var boundSubpattern = new BoundPositionalSubpattern(
                    subpatternSyntax,
                    null,
                    BindPattern(subpatternSyntax.Pattern, objectType, permitDesignations, hasErrors: false, diagnostics));
                patterns.Add(boundSubpattern);
            }
        }

        private void BindITupleSubpatterns(
            ParenthesizedVariableDesignationSyntax node,
            ArrayBuilder<BoundPositionalSubpattern> patterns,
            bool permitDesignations,
            BindingDiagnosticBag diagnostics)
        {
            var objectType = Compilation.GetSpecialType(SpecialType.System_Object);
            foreach (var variable in node.Variables)
            {
                BoundPattern pattern = BindVarDesignation(variable, objectType, permitDesignations, hasErrors: false, diagnostics);
                var boundSubpattern = new BoundPositionalSubpattern(
                    variable,
                    null,
                    pattern);
                patterns.Add(boundSubpattern);
            }
        }

        private void BindValueTupleSubpatterns(
            PositionalPatternClauseSyntax node,
            TypeSymbol declType,
            ImmutableArray<TypeWithAnnotations> elementTypesWithAnnotations,
            bool permitDesignations,
            ref bool hasErrors,
            ArrayBuilder<BoundPositionalSubpattern> patterns,
            BindingDiagnosticBag diagnostics)
        {
            if (elementTypesWithAnnotations.Length != node.Subpatterns.Count && !hasErrors)
            {
                diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, node.Location, declType, elementTypesWithAnnotations.Length, node.Subpatterns.Count);
                hasErrors = true;
            }

            for (int i = 0; i < node.Subpatterns.Count; i++)
            {
                var subpatternSyntax = node.Subpatterns[i];
                bool isError = i >= elementTypesWithAnnotations.Length;
                TypeSymbol elementType = isError ? CreateErrorType() : elementTypesWithAnnotations[i].Type;
                FieldSymbol? foundField = null;
                if (!isError)
                {
                    if (subpatternSyntax.NameColon != null)
                    {
                        string name = subpatternSyntax.NameColon.Name.Identifier.ValueText;
                        foundField = CheckIsTupleElement(subpatternSyntax.NameColon.Name, (NamedTypeSymbol)declType, name, i, diagnostics);
                    }
                    else if (subpatternSyntax.ExpressionColon != null)
                    {
                        diagnostics.Add(ErrorCode.ERR_IdentifierExpected, subpatternSyntax.ExpressionColon.Expression.Location);
                    }
                }

                BoundPositionalSubpattern boundSubpattern = new BoundPositionalSubpattern(
                    subpatternSyntax,
                    foundField,
                    BindPattern(subpatternSyntax.Pattern, elementType, permitDesignations, isError, diagnostics));
                patterns.Add(boundSubpattern);
            }
        }

        private bool ShouldUseITupleForRecursivePattern(
            RecursivePatternSyntax node,
            TypeSymbol declType,
            BindingDiagnosticBag diagnostics,
            [NotNullWhen(true)] out NamedTypeSymbol? iTupleType,
            [NotNullWhen(true)] out MethodSymbol? iTupleGetLength,
            [NotNullWhen(true)] out MethodSymbol? iTupleGetItem)
        {
            iTupleType = null;
            iTupleGetLength = iTupleGetItem = null;
            if (node.Type != null)
            {
                // ITuple matching only applies if no type is given explicitly.
                return false;
            }

            if (node.PropertyPatternClause != null)
            {
                // ITuple matching only applies if there is no property pattern part.
                return false;
            }

            if (node.PositionalPatternClause == null)
            {
                // ITuple matching only applies if there is a positional pattern part.
                // This can only occur as a result of syntax error recovery, if at all.
                return false;
            }

            if (node.Designation?.Kind() == SyntaxKind.SingleVariableDesignation)
            {
                // ITuple matching only applies if there is no variable declared (what type would the variable be?)
                return false;
            }

            return ShouldUseITuple(node, declType, diagnostics, out iTupleType, out iTupleGetLength, out iTupleGetItem);
        }

        private bool ShouldUseITuple(
            SyntaxNode node,
            TypeSymbol declType,
            BindingDiagnosticBag diagnostics,
            [NotNullWhen(true)] out NamedTypeSymbol? iTupleType,
            [NotNullWhen(true)] out MethodSymbol? iTupleGetLength,
            [NotNullWhen(true)] out MethodSymbol? iTupleGetItem)
        {
            iTupleType = null;
            iTupleGetLength = iTupleGetItem = null;
            Debug.Assert(!declType.IsTupleType);
            Debug.Assert(!IsZeroElementTupleType(declType));

            if (Compilation.LanguageVersion < MessageID.IDS_FeatureRecursivePatterns.RequiredVersion())
            {
                return false;
            }

            iTupleType = Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ITuple);
            if (iTupleType.TypeKind != TypeKind.Interface)
            {
                // When compiling to a platform that lacks the interface ITuple (i.e. it is an error type), we simply do not match using it.
                return false;
            }

            // Resolution 2017-11-20 LDM: permit matching via ITuple only for `object`, `ITuple`, and types that are
            // declared to implement `ITuple`.
            if (declType != (object)Compilation.GetSpecialType(SpecialType.System_Object) &&
                declType != (object)Compilation.DynamicType &&
                declType != (object)iTupleType &&
                !hasBaseInterface(declType, iTupleType))
            {
                return false;
            }

            // Ensure ITuple has a Length and indexer
            iTupleGetLength = (MethodSymbol?)Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ITuple__get_Length);
            iTupleGetItem = (MethodSymbol?)Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ITuple__get_Item);
            if (iTupleGetLength is null || iTupleGetItem is null)
            {
                // This might not result in an ideal diagnostic
                return false;
            }

            // passed all the filters; permit using ITuple
            _ = diagnostics.ReportUseSite(iTupleType, node) ||
                diagnostics.ReportUseSite(iTupleGetLength, node) ||
                diagnostics.ReportUseSite(iTupleGetItem, node);

            return true;

            bool hasBaseInterface(TypeSymbol type, NamedTypeSymbol possibleBaseInterface)
            {
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var result = Compilation.Conversions.ClassifyBuiltInConversion(type, possibleBaseInterface, isChecked: CheckOverflowAtRuntime, ref useSiteInfo).IsImplicit;
                diagnostics.Add(node, useSiteInfo);
                return result;
            }
        }

        /// <summary>
        /// Check that the given name designates a tuple element at the given index, and return that element.
        /// </summary>
        private static FieldSymbol? CheckIsTupleElement(SyntaxNode node, NamedTypeSymbol tupleType, string name, int tupleIndex, BindingDiagnosticBag diagnostics)
        {
            FieldSymbol? foundElement = null;
            foreach (var symbol in tupleType.GetMembers(name))
            {
                if (symbol is FieldSymbol field && field.IsTupleElement())
                {
                    foundElement = field;
                    break;
                }
            }

            if (foundElement is null || foundElement.TupleElementIndex != tupleIndex)
            {
                diagnostics.Add(ErrorCode.ERR_TupleElementNameMismatch, node.Location, name, $"Item{tupleIndex + 1}");
            }

            return foundElement;
        }

        private BoundPattern BindVarPattern(
            VarPatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            if ((inputType.IsPointerOrFunctionPointer() && node.Designation.Kind() == SyntaxKind.ParenthesizedVariableDesignation)
                || (inputType.IsPointerType() && Compilation.LanguageVersion < MessageID.IDS_FeatureRecursivePatterns.RequiredVersion()))
            {
                diagnostics.Add(ErrorCode.ERR_PointerTypeInPatternMatching, node.Location);
                hasErrors = true;
                inputType = CreateErrorType();
            }

            Symbol foundSymbol = BindTypeOrAliasOrKeyword(node.VarKeyword, node, diagnostics, out bool isVar).Symbol;
            if (!isVar)
            {
                // Give an error if there is a bindable type "var" in scope
                diagnostics.Add(ErrorCode.ERR_VarMayNotBindToType, node.VarKeyword.GetLocation(), foundSymbol.ToDisplayString());
                hasErrors = true;
            }

            return BindVarDesignation(node.Designation, inputType, permitDesignations, hasErrors, diagnostics);
        }

        private BoundPattern BindVarDesignation(
            VariableDesignationSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DiscardDesignation:
                    {
                        return new BoundDiscardPattern(node, inputType: inputType, narrowedType: inputType);
                    }
                case SyntaxKind.SingleVariableDesignation:
                    {
                        var declType = TypeWithState.ForType(inputType).ToTypeWithAnnotations(Compilation);
                        BindPatternDesignation(
                            designation: node, declType: declType, permitDesignations: permitDesignations,
                            typeSyntax: null, diagnostics: diagnostics, hasErrors: ref hasErrors,
                            variableSymbol: out Symbol? variableSymbol, variableAccess: out BoundExpression? variableAccess);
                        var boundOperandType = new BoundTypeExpression(syntax: node, aliasOpt: null, typeWithAnnotations: declType); // fake a type expression for the variable's type
                        // We continue to use a BoundDeclarationPattern for the var pattern, as they have more in common.
                        Debug.Assert(node.Parent is { });
                        return new BoundDeclarationPattern(
                            node.Parent.Kind() == SyntaxKind.VarPattern ? node.Parent : node, // for `var x` use whole pattern, otherwise use designation for the syntax
                            boundOperandType, isVar: true, variableSymbol, variableAccess,
                            inputType: inputType, narrowedType: inputType, hasErrors);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        MessageID.IDS_FeatureRecursivePatterns.CheckFeatureAvailability(diagnostics, node);

                        var tupleDesignation = (ParenthesizedVariableDesignationSyntax)node;
                        var subPatterns = ArrayBuilder<BoundPositionalSubpattern>.GetInstance(tupleDesignation.Variables.Count);
                        MethodSymbol? deconstructMethod = null;
                        var strippedInputType = inputType.StrippedType();

                        if (IsZeroElementTupleType(strippedInputType))
                        {
                            // Work around https://github.com/dotnet/roslyn/issues/20648: The compiler's internal APIs such as `declType.IsTupleType`
                            // do not correctly treat the non-generic struct `System.ValueTuple` as a tuple type.  We explicitly perform the tests
                            // required to identify it.  When that bug is fixed we should be able to remove this if statement.
                            addSubpatternsForTuple(ImmutableArray<TypeWithAnnotations>.Empty);
                        }
                        else if (strippedInputType.IsTupleType)
                        {
                            // It is a tuple type. Work according to its elements
                            addSubpatternsForTuple(strippedInputType.TupleElementTypesWithAnnotations);
                        }
                        else
                        {
                            // It is not a tuple type. Seek an appropriate Deconstruct method.
                            var inputPlaceholder = new BoundImplicitReceiver(node, strippedInputType); // A fake receiver expression to permit us to reuse binding logic
                            var deconstructDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
                            BoundExpression deconstruct = MakeDeconstructInvocationExpression(
                                tupleDesignation.Variables.Count, inputPlaceholder, node, deconstructDiagnostics,
                                outPlaceholders: out ImmutableArray<BoundDeconstructValuePlaceholder> outPlaceholders,
                                out bool anyDeconstructCandidates);
                            if (!anyDeconstructCandidates &&
                                ShouldUseITuple(node, strippedInputType, diagnostics, out var iTupleType, out var iTupleGetLength, out var iTupleGetItem))
                            {
                                // There was no applicable candidate Deconstruct, and the constraints for the use of ITuple are satisfied.
                                // Use that and forget any errors from trying to bind Deconstruct.
                                deconstructDiagnostics.Free();
                                BindITupleSubpatterns(tupleDesignation, subPatterns, permitDesignations, diagnostics);
                                return new BoundITuplePattern(node, iTupleGetLength, iTupleGetItem, subPatterns.ToImmutableAndFree(), strippedInputType, iTupleType, hasErrors);
                            }
                            else
                            {
                                diagnostics.AddRangeAndFree(deconstructDiagnostics);
                            }

                            deconstructMethod = deconstruct.ExpressionSymbol as MethodSymbol;
                            if (!hasErrors)
                                hasErrors = outPlaceholders.IsDefault || tupleDesignation.Variables.Count != outPlaceholders.Length;

                            for (int i = 0; i < tupleDesignation.Variables.Count; i++)
                            {
                                var variable = tupleDesignation.Variables[i];
                                bool isError = outPlaceholders.IsDefaultOrEmpty || i >= outPlaceholders.Length;
                                TypeSymbol elementType = isError ? CreateErrorType() : outPlaceholders[i].Type;
                                BoundPattern pattern = BindVarDesignation(variable, elementType, permitDesignations, isError, diagnostics);
                                subPatterns.Add(new BoundPositionalSubpattern(variable, symbol: null, pattern));
                            }
                        }

                        return new BoundRecursivePattern(
                            syntax: node, declaredType: null, deconstructMethod: deconstructMethod,
                            deconstruction: subPatterns.ToImmutableAndFree(), properties: default, variable: null, variableAccess: null,
                            isExplicitNotNullTest: false, inputType: inputType, narrowedType: inputType.StrippedType(), hasErrors: hasErrors);

                        void addSubpatternsForTuple(ImmutableArray<TypeWithAnnotations> elementTypes)
                        {
                            if (elementTypes.Length != tupleDesignation.Variables.Count && !hasErrors)
                            {
                                diagnostics.Add(ErrorCode.ERR_WrongNumberOfSubpatterns, tupleDesignation.Location,
                                    strippedInputType, elementTypes.Length, tupleDesignation.Variables.Count);
                                hasErrors = true;
                            }
                            for (int i = 0; i < tupleDesignation.Variables.Count; i++)
                            {
                                var variable = tupleDesignation.Variables[i];
                                bool isError = i >= elementTypes.Length;
                                TypeSymbol elementType = isError ? CreateErrorType() : elementTypes[i].Type;
                                BoundPattern pattern = BindVarDesignation(variable, elementType, permitDesignations, isError, diagnostics);
                                subPatterns.Add(new BoundPositionalSubpattern(variable, symbol: null, pattern));
                            }
                        }
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(node.Kind());
                    }
            }
        }

        private ImmutableArray<BoundPropertySubpattern> BindPropertyPatternClause(
            PropertyPatternClauseSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            BindingDiagnosticBag diagnostics,
            ref bool hasErrors)
        {
            var builder = ArrayBuilder<BoundPropertySubpattern>.GetInstance(node.Subpatterns.Count);
            foreach (SubpatternSyntax p in node.Subpatterns)
            {
                if (p.ExpressionColon is ExpressionColonSyntax)
                    MessageID.IDS_FeatureExtendedPropertyPatterns.CheckFeatureAvailability(diagnostics, p.ExpressionColon.ColonToken);

                ExpressionSyntax? expr = p.ExpressionColon?.Expression;
                PatternSyntax pattern = p.Pattern;
                BoundPropertySubpatternMember? member;
                TypeSymbol memberType;
                bool isLengthOrCount = false;
                if (expr == null)
                {
                    if (!hasErrors)
                        diagnostics.Add(ErrorCode.ERR_PropertyPatternNameMissing, pattern.Location, pattern);

                    memberType = CreateErrorType();
                    member = null;
                    hasErrors = true;
                }
                else
                {
                    member = LookupMembersForPropertyPattern(inputType, expr, diagnostics, ref hasErrors);
                    memberType = member.Type;
                    // If we're dealing with the member that makes the type countable, and the type is also indexable, then it will be assumed to always return a non-negative value
                    if (memberType.SpecialType == SpecialType.System_Int32 &&
                        member.Symbol is { Name: WellKnownMemberNames.LengthPropertyName or WellKnownMemberNames.CountPropertyName, Kind: SymbolKind.Property } memberSymbol)
                    {
                        TypeSymbol receiverType = member.Receiver?.Type ?? inputType;
                        if (!receiverType.IsErrorType())
                        {
                            isLengthOrCount = IsCountableAndIndexable(node, receiverType, out PropertySymbol? lengthProperty) &&
                                memberSymbol.Equals(lengthProperty, TypeCompareKind.ConsiderEverything); // If Length and Count are both present, only the former is assumed to be non-negative.
                        }
                    }
                }

                BoundPattern boundPattern = BindPattern(pattern, memberType, permitDesignations, hasErrors, diagnostics);
                builder.Add(new BoundPropertySubpattern(p, member, isLengthOrCount, boundPattern));
            }

            return builder.ToImmutableAndFree();
        }

        private BoundPropertySubpatternMember LookupMembersForPropertyPattern(
            TypeSymbol inputType, ExpressionSyntax expr, BindingDiagnosticBag diagnostics, ref bool hasErrors)
        {
            BoundPropertySubpatternMember? receiver = null;
            Symbol? symbol = null;
            switch (expr)
            {
                case IdentifierNameSyntax name:
                    symbol = BindPropertyPatternMember(inputType, name, ref hasErrors, diagnostics);
                    break;
                case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } memberAccess when memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                    receiver = LookupMembersForPropertyPattern(inputType, memberAccess.Expression, diagnostics, ref hasErrors);
                    symbol = BindPropertyPatternMember(receiver.Type.StrippedType(), name, ref hasErrors, diagnostics);
                    break;
                default:
                    Error(diagnostics, ErrorCode.ERR_InvalidNameInSubpattern, expr);
                    hasErrors = true;
                    break;
            }

            TypeSymbol memberType = symbol switch
            {
                FieldSymbol field => field.Type,
                PropertySymbol property => property.Type,
                _ => CreateErrorType()
            };

            return new BoundPropertySubpatternMember(expr, receiver, symbol, type: memberType, hasErrors);
        }

        private Symbol? BindPropertyPatternMember(
            TypeSymbol inputType,
            IdentifierNameSyntax memberName,
            ref bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            // TODO: consider refactoring out common code with BindObjectInitializerMember
            BoundImplicitReceiver implicitReceiver = new BoundImplicitReceiver(memberName, inputType);
            string name = memberName.Identifier.ValueText;

            BoundExpression boundMember = BindInstanceMemberAccess(
                node: memberName,
                right: memberName,
                boundLeft: implicitReceiver,
                rightName: name,
                rightArity: 0,
                typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>),
                typeArgumentsWithAnnotations: default(ImmutableArray<TypeWithAnnotations>),
                invoked: false,
                indexed: false,
                diagnostics: diagnostics);

            if (boundMember.Kind == BoundKind.PropertyGroup)
            {
                boundMember = BindIndexedPropertyAccess(
                    (BoundPropertyGroup)boundMember, mustHaveAllOptionalParameters: true, diagnostics: diagnostics);
            }

            hasErrors |= boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

            switch (boundMember.Kind)
            {
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    break;

                case BoundKind.IndexerAccess:
                case BoundKind.DynamicIndexerAccess:
                case BoundKind.EventAccess:
                default:
                    if (!hasErrors)
                    {
                        switch (boundMember.ResultKind)
                        {
                            case LookupResultKind.Empty:
                                Error(diagnostics, ErrorCode.ERR_NoSuchMember, memberName, implicitReceiver.Type, name);
                                break;

                            case LookupResultKind.Inaccessible:
                                boundMember = CheckValue(boundMember, BindValueKind.RValue, diagnostics);
                                Debug.Assert(boundMember.HasAnyErrors);
                                break;

                            default:
                                Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, memberName, name);
                                break;
                        }
                        hasErrors = true;
                    }
                    break;
            }

            if (!hasErrors && !CheckValueKind(node: memberName.Parent, expr: boundMember, valueKind: BindValueKind.RValue,
                                              checkingReceiver: false, diagnostics: diagnostics))
            {
                hasErrors = true;
            }

            return boundMember.ExpressionSymbol;
        }

        private BoundPattern BindTypePattern(
            TypePatternSyntax node,
            TypeSymbol inputType,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureTypePattern.CheckFeatureAvailability(diagnostics, node);

            var patternType = BindTypeForPattern(node.Type, inputType, diagnostics, ref hasErrors);
            bool isExplicitNotNullTest = patternType.Type.SpecialType == SpecialType.System_Object;
            return new BoundTypePattern(node, patternType, isExplicitNotNullTest, inputType, patternType.Type, hasErrors);
        }

        private BoundPattern BindRelationalPattern(
            RelationalPatternSyntax node,
            TypeSymbol inputType,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureRelationalPattern.CheckFeatureAvailability(diagnostics, node.OperatorToken);

            BoundExpression value = BindExpressionForPattern(inputType, node.Expression, ref hasErrors, diagnostics, out var constantValueOpt, out _, out Conversion patternConversion);
            ExpressionSyntax innerExpression = SkipParensAndNullSuppressions(node.Expression, diagnostics, ref hasErrors);
            var type = value.Type ?? inputType;
            Debug.Assert(type is { });
            BinaryOperatorKind operation = tokenKindToBinaryOperatorKind(node.OperatorToken.Kind());
            if (operation == BinaryOperatorKind.Equal)
            {
                diagnostics.Add(ErrorCode.ERR_InvalidExprTerm, node.OperatorToken.GetLocation(), node.OperatorToken.Text);
                hasErrors = true;
            }

            BinaryOperatorKind opType = RelationalOperatorType(type.EnumUnderlyingTypeOrSelf());
            switch (opType)
            {
                case BinaryOperatorKind.Float:
                case BinaryOperatorKind.Double:
                    if (!hasErrors && constantValueOpt != null && !constantValueOpt.IsBad && double.IsNaN(constantValueOpt.DoubleValue))
                    {
                        diagnostics.Add(ErrorCode.ERR_RelationalPatternWithNaN, node.Expression.Location);
                        hasErrors = true;
                    }
                    break;
                case BinaryOperatorKind.String:
                case BinaryOperatorKind.Bool:
                case BinaryOperatorKind.Error:
                    if (!hasErrors)
                    {
                        diagnostics.Add(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, node.Location, type.ToDisplayString());
                        hasErrors = true;
                    }
                    break;
            }

            if (constantValueOpt is null)
            {
                hasErrors = true;
                constantValueOpt = ConstantValue.Bad;
            }

            if (!hasErrors && ShouldBlockINumberBaseConversion(patternConversion, inputType))
            {
                // Cannot use a numeric constant or relational pattern on '{0}' because it inherits from or extends 'INumberBase&lt;T&gt;'. Consider using a type pattern to narrow to a specific numeric type.
                diagnostics.Add(ErrorCode.ERR_CannotMatchOnINumberBase, node.Location, inputType);
                hasErrors = true;
            }

            return new BoundRelationalPattern(node, operation | opType, value, constantValueOpt, inputType, type, hasErrors);

            static BinaryOperatorKind tokenKindToBinaryOperatorKind(SyntaxKind kind) => kind switch
            {
                SyntaxKind.LessThanEqualsToken => BinaryOperatorKind.LessThanOrEqual,
                SyntaxKind.LessThanToken => BinaryOperatorKind.LessThan,
                SyntaxKind.GreaterThanToken => BinaryOperatorKind.GreaterThan,
                SyntaxKind.GreaterThanEqualsToken => BinaryOperatorKind.GreaterThanOrEqual,
                // The following occurs in error recovery scenarios
                _ => BinaryOperatorKind.Equal,
            };
        }

        /// <summary>
        /// Compute the type code for the comparison operator to be used.  When comparing `byte`s for example,
        /// the compiler actually uses the operator on the type `int` as there is no corresponding operator for
        /// the type `byte`.
        /// </summary>
        internal static BinaryOperatorKind RelationalOperatorType(TypeSymbol type) => type.SpecialType switch
        {
            SpecialType.System_Single => BinaryOperatorKind.Float,
            SpecialType.System_Double => BinaryOperatorKind.Double,
            SpecialType.System_Char => BinaryOperatorKind.Char,
            SpecialType.System_SByte => BinaryOperatorKind.Int, // operands are converted to int
            SpecialType.System_Byte => BinaryOperatorKind.Int, // operands are converted to int
            SpecialType.System_UInt16 => BinaryOperatorKind.Int, // operands are converted to int
            SpecialType.System_Int16 => BinaryOperatorKind.Int, // operands are converted to int
            SpecialType.System_Int32 => BinaryOperatorKind.Int,
            SpecialType.System_UInt32 => BinaryOperatorKind.UInt,
            SpecialType.System_Int64 => BinaryOperatorKind.Long,
            SpecialType.System_UInt64 => BinaryOperatorKind.ULong,
            SpecialType.System_Decimal => BinaryOperatorKind.Decimal,
            SpecialType.System_String => BinaryOperatorKind.String,
            SpecialType.System_Boolean => BinaryOperatorKind.Bool,
            SpecialType.System_IntPtr when type.IsNativeIntegerType => BinaryOperatorKind.NInt,
            SpecialType.System_UIntPtr when type.IsNativeIntegerType => BinaryOperatorKind.NUInt,
            _ => BinaryOperatorKind.Error,
        };

        private BoundPattern BindUnaryPattern(
            UnaryPatternSyntax node,
            TypeSymbol inputType,
            bool hasErrors,
            BindingDiagnosticBag diagnostics,
            bool underIsPattern)
        {
            MessageID.IDS_FeatureNotPattern.CheckFeatureAvailability(diagnostics, node.OperatorToken);

            bool permitDesignations = underIsPattern; // prevent designators under 'not' except under an is-pattern
            var subPattern = BindPattern(node.Pattern, inputType, permitDesignations, hasErrors, diagnostics, underIsPattern);
            return new BoundNegatedPattern(node, subPattern, inputType: inputType, narrowedType: inputType, hasErrors);
        }

        private BoundPattern BindBinaryPattern(
            BinaryPatternSyntax node,
            TypeSymbol inputType,
            bool permitDesignations,
            bool hasErrors,
            BindingDiagnosticBag diagnostics)
        {
            // Users (such as ourselves) can have many, many nested binary patterns. To avoid crashing, do left recursion manually.

            var binaryPatternStack = ArrayBuilder<(BinaryPatternSyntax pat, bool permitDesignations)>.GetInstance();
            BinaryPatternSyntax? currentNode = node;

            do
            {
                permitDesignations = permitDesignations && currentNode.IsKind(SyntaxKind.AndPattern);
                binaryPatternStack.Push((currentNode, permitDesignations));
                currentNode = currentNode.Left as BinaryPatternSyntax;
            } while (currentNode != null);

            Debug.Assert(binaryPatternStack.Count > 0);

            var binaryPatternAndPermitDesignations = binaryPatternStack.Pop();
            BoundPattern result = BindPattern(binaryPatternAndPermitDesignations.pat.Left, inputType, binaryPatternAndPermitDesignations.permitDesignations, hasErrors, diagnostics);
            var narrowedTypeCandidates = ArrayBuilder<TypeSymbol>.GetInstance(2);
            collectCandidates(result, narrowedTypeCandidates);

            do
            {
                result = bindBinaryPattern(
                    result,
                    this,
                    binaryPatternAndPermitDesignations.pat,
                    binaryPatternAndPermitDesignations.permitDesignations,
                    inputType,
                    narrowedTypeCandidates,
                    hasErrors,
                    diagnostics);
            } while (binaryPatternStack.TryPop(out binaryPatternAndPermitDesignations));

            binaryPatternStack.Free();
            narrowedTypeCandidates.Free();
            return result;

            static BoundPattern bindBinaryPattern(
                BoundPattern preboundLeft,
                Binder binder,
                BinaryPatternSyntax node,
                bool permitDesignations,
                TypeSymbol inputType,
                ArrayBuilder<TypeSymbol> narrowedTypeCandidates,
                bool hasErrors,
                BindingDiagnosticBag diagnostics)
            {
                bool isDisjunction = node.Kind() == SyntaxKind.OrPattern;
                if (isDisjunction)
                {
                    Debug.Assert(!permitDesignations);
                    MessageID.IDS_FeatureOrPattern.CheckFeatureAvailability(diagnostics, node.OperatorToken);

                    var right = binder.BindPattern(node.Right, inputType, permitDesignations, hasErrors, diagnostics);

                    // Compute the common type. This algorithm is quadratic, but disjunctive patterns are unlikely to be huge
                    collectCandidates(right, narrowedTypeCandidates);
                    var narrowedType = leastSpecificType(node, narrowedTypeCandidates, diagnostics) ?? inputType;

                    return new BoundBinaryPattern(node, disjunction: isDisjunction, preboundLeft, right, inputType: inputType, narrowedType: narrowedType, hasErrors);

                    TypeSymbol? leastSpecificType(SyntaxNode node, ArrayBuilder<TypeSymbol> candidates, BindingDiagnosticBag diagnostics)
                    {
                        Debug.Assert(candidates.Count >= 2);
                        CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
                        TypeSymbol? bestSoFar = candidates[0];
                        // first pass: select a candidate for which no other has been shown to be an improvement.
                        for (int i = 1, n = candidates.Count; i < n; i++)
                        {
                            TypeSymbol candidate = candidates[i];
                            bestSoFar = lessSpecificCandidate(bestSoFar, candidate, ref useSiteInfo) ?? bestSoFar;
                        }
                        // second pass: check that it is no more specific than any candidate.
                        for (int i = 0, n = candidates.Count; i < n; i++)
                        {
                            TypeSymbol candidate = candidates[i];
                            TypeSymbol? spoiler = lessSpecificCandidate(candidate, bestSoFar, ref useSiteInfo);
                            if (spoiler is null)
                            {
                                bestSoFar = null;
                                break;
                            }

                            // Our specificity criteria are transitive
                            Debug.Assert(spoiler.Equals(bestSoFar, TypeCompareKind.ConsiderEverything));
                        }

                        diagnostics.Add(node, useSiteInfo);
                        return bestSoFar;
                    }

                    // Given a candidate least specific type so far, attempt to refine it with a possibly less specific candidate.
                    TypeSymbol? lessSpecificCandidate(TypeSymbol bestSoFar, TypeSymbol possiblyLessSpecificCandidate, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
                    {
                        if (bestSoFar.Equals(possiblyLessSpecificCandidate, TypeCompareKind.AllIgnoreOptions))
                        {
                            // When the types are equivalent, merge them.
                            return bestSoFar.MergeEquivalentTypes(possiblyLessSpecificCandidate, VarianceKind.Out);
                        }
                        else if (binder.Conversions.HasImplicitReferenceConversion(bestSoFar, possiblyLessSpecificCandidate, ref useSiteInfo))
                        {
                            // When there is an implicit reference conversion from T to U, U is less specific
                            return possiblyLessSpecificCandidate;
                        }
                        else if (binder.Conversions.HasBoxingConversion(bestSoFar, possiblyLessSpecificCandidate, ref useSiteInfo))
                        {
                            // when there is a boxing conversion from T to U, U is less specific.
                            return possiblyLessSpecificCandidate;
                        }
                        else
                        {
                            // We have no improved candidate to offer.
                            return null;
                        }
                    }
                }
                else
                {
                    MessageID.IDS_FeatureAndPattern.CheckFeatureAvailability(diagnostics, node.OperatorToken);

                    var right = binder.BindPattern(node.Right, preboundLeft.NarrowedType, permitDesignations, hasErrors, diagnostics);
                    narrowedTypeCandidates.Clear();
                    narrowedTypeCandidates.Add(right.NarrowedType);
                    return new BoundBinaryPattern(node, disjunction: isDisjunction, preboundLeft, right, inputType: inputType, narrowedType: right.NarrowedType, hasErrors);
                }
            }

            static void collectCandidates(BoundPattern pat, ArrayBuilder<TypeSymbol> candidates)
            {
                if (pat is BoundBinaryPattern { Disjunction: true } p)
                {
                    collectCandidates(p.Left, candidates);
                    collectCandidates(p.Right, candidates);
                }
                else
                {
                    candidates.Add(pat.NarrowedType);
                }
            }

        }
    }
}
