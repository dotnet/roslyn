// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
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
    internal partial class Binder
    {
#nullable enable

        private BoundExpression BindCompoundAssignment(AssignmentExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            OperatorResolutionForReporting operatorResolutionForReporting = default;
            BoundExpression result = bindCompoundAssignment(node, ref operatorResolutionForReporting, diagnostics);
            operatorResolutionForReporting.Free();
            return result;

            BoundExpression bindCompoundAssignment(AssignmentExpressionSyntax node, ref OperatorResolutionForReporting operatorResolutionForReporting, BindingDiagnosticBag diagnostics)
            {
                node.Left.CheckDeconstructionCompatibleArgument(diagnostics);

                BoundExpression left = BindValue(node.Left, diagnostics, GetBinaryAssignmentKind(node.Kind()));
                ReportSuppressionIfNeeded(left, diagnostics);
                BoundExpression right = BindValue(node.Right, diagnostics, BindValueKind.RValue);
                BinaryOperatorKind kind = SyntaxKindToBinaryOperatorKind(node.Kind());

                // If either operand is bad, don't try to do binary operator overload resolution; that will just
                // make cascading errors.

                if (left.Kind == BoundKind.EventAccess)
                {
                    BinaryOperatorKind kindOperator = kind.Operator();
                    switch (kindOperator)
                    {
                        case BinaryOperatorKind.Addition:
                        case BinaryOperatorKind.Subtraction:
                            return BindEventAssignment(node, (BoundEventAccess)left, right, kindOperator, diagnostics);

                            // fall-through for other operators, if RHS is dynamic we produce dynamic operation, otherwise we'll report an error ...
                    }
                }

                if (left.HasAnyErrors || right.HasAnyErrors)
                {
                    // NOTE: no overload resolution candidates.
                    left = BindToTypeForErrorRecovery(left);
                    right = BindToTypeForErrorRecovery(right);
                    return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                        leftPlaceholder: null, leftConversion: null, finalPlaceholder: null, finalConversion: null, LookupResultKind.Empty, CreateErrorType(), hasErrors: true);
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                if (left.HasDynamicType() || right.HasDynamicType())
                {
                    if (IsLegalDynamicOperand(right) && IsLegalDynamicOperand(left) && kind != BinaryOperatorKind.UnsignedRightShift)
                    {
                        left = BindToNaturalType(left, diagnostics);
                        Debug.Assert(left.Type is { });

                        right = BindToNaturalType(right, diagnostics);
                        var placeholder = new BoundValuePlaceholder(right.Syntax, left.HasDynamicType() ? left.Type : right.Type).MakeCompilerGenerated();
                        var finalDynamicConversion = this.Compilation.Conversions.ClassifyConversionFromExpression(placeholder, left.Type, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                        diagnostics.Add(node, useSiteInfo);
                        var conversion = (BoundConversion)CreateConversion(node, placeholder, finalDynamicConversion, isCast: true, conversionGroupOpt: null, left.Type, diagnostics);

                        conversion = conversion.Update(conversion.Operand, conversion.Conversion, conversion.IsBaseConversion, conversion.Checked,
                                                       explicitCastInCode: true, conversion.ConstantValueOpt, conversion.ConversionGroupOpt, conversion.Type);

                        return new BoundCompoundAssignmentOperator(
                            node,
                            new BinaryOperatorSignature(
                                kind.WithType(BinaryOperatorKind.Dynamic).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                                left.Type,
                                right.Type,
                                Compilation.DynamicType),
                            left,
                            right,
                            leftPlaceholder: null, leftConversion: null,
                            finalPlaceholder: placeholder,
                            finalConversion: conversion,
                            LookupResultKind.Viable,
                            left.Type,
                            hasErrors: false);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, node.OperatorToken.Text, left.Display, right.Display);

                        // error: operator can't be applied on dynamic and a type that is not convertible to dynamic:
                        left = BindToTypeForErrorRecovery(left);
                        right = BindToTypeForErrorRecovery(right);
                        return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                            leftPlaceholder: null, leftConversion: null, finalPlaceholder: null, finalConversion: null, LookupResultKind.Empty, CreateErrorType(), hasErrors: true);
                    }
                }

                if (left.Kind == BoundKind.EventAccess && !CheckEventValueKind((BoundEventAccess)left, BindValueKind.Assignable, diagnostics))
                {
                    // If we're in a place where the event can be assigned, then continue so that we give errors
                    // about the types and operator not lining up.  Otherwise, just report that the event can't
                    // be used here.

                    // NOTE: no overload resolution candidates.
                    left = BindToTypeForErrorRecovery(left);
                    right = BindToTypeForErrorRecovery(right);
                    return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                        leftPlaceholder: null, leftConversion: null, finalPlaceholder: null, finalConversion: null, LookupResultKind.NotAVariable, CreateErrorType(), hasErrors: true);
                }

                if (!IsTypelessExpressionAllowedInBinaryOperator(kind, left, right))
                {
                    return createBadCompoundAssignmentOperator(node, kind, left, right, LookupResultKind.OverloadResolutionFailure, originalUserDefinedOperators: default, ref operatorResolutionForReporting, diagnostics);
                }

                bool checkOverflowAtRuntime = CheckOverflowAtRuntime;

                // Try an in-place user-defined operator
                bool tryInstance = shouldTryUserDefinedInstanceOperator(node, checkOverflowAtRuntime, left, out string? checkedInstanceOperatorName, out string? ordinaryInstanceOperatorName);

                if (tryInstance)
                {
                    Debug.Assert(ordinaryInstanceOperatorName is not null);

                    BoundCompoundAssignmentOperator? inPlaceResult = tryApplyUserDefinedInstanceOperator(node, kind, checkOverflowAtRuntime, checkedInstanceOperatorName, ordinaryInstanceOperatorName,
                                                                                                         left, right, ref operatorResolutionForReporting, diagnostics);
                    if (inPlaceResult is not null)
                    {
                        return inPlaceResult;
                    }
                }

                // A compound operator, say, x |= y, is bound as x = (X)( ((T)x) | ((T)y) ). We must determine
                // the binary operator kind, the type conversions from each side to the types expected by
                // the operator, and the type conversion from the return type of the operand to the left hand side.
                //
                // We can get away with binding the right-hand-side of the operand into its converted form early.
                // This is convenient because first, it is never rewritten into an access to a temporary before
                // the conversion, and second, because that is more convenient for the "d += lambda" case.
                // We want to have the converted (bound) lambda in the bound tree, not the unconverted unbound lambda.

                LookupResultKind resultKind;
                ImmutableArray<MethodSymbol> originalUserDefinedOperators;

                OverloadResolution.GetStaticUserDefinedBinaryOperatorMethodNames(kind, checkOverflowAtRuntime, out string staticOperatorName1, out string? staticOperatorName2Opt);
                BinaryOperatorAnalysisResult best = BinaryOperatorNonExtensionOverloadResolution(kind, isChecked: checkOverflowAtRuntime, staticOperatorName1, staticOperatorName2Opt, left, right,
                    node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);

                Debug.Assert(resultKind is LookupResultKind.Viable or LookupResultKind.Ambiguous or LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
                Debug.Assert(best.HasValue == (resultKind is LookupResultKind.Viable));
                Debug.Assert(resultKind is not (LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty) || originalUserDefinedOperators.IsEmpty);

                if (!best.HasValue && resultKind != LookupResultKind.Ambiguous)
                {
                    Debug.Assert(resultKind is LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
                    Debug.Assert(originalUserDefinedOperators.IsEmpty);

                    LookupResultKind staticExtensionResultKind;
                    ImmutableArray<MethodSymbol> staticExtensionOriginalUserDefinedOperators;
                    BinaryOperatorAnalysisResult? staticExtensionBest;
                    BoundCompoundAssignmentOperator? instanceExtensionResult = tryApplyUserDefinedExtensionOperator(
                        node, kind, tryInstance, checkOverflowAtRuntime,
                        staticOperatorName1, staticOperatorName2Opt,
                        checkedInstanceOperatorName, ordinaryInstanceOperatorName,
                        left, right, diagnostics,
                        ref operatorResolutionForReporting,
                        out staticExtensionBest, out staticExtensionResultKind, out staticExtensionOriginalUserDefinedOperators);

                    if (instanceExtensionResult is not null)
                    {
                        Debug.Assert(instanceExtensionResult.ResultKind is LookupResultKind.Viable || !instanceExtensionResult.OriginalUserDefinedOperatorsOpt.IsDefaultOrEmpty);
                        return instanceExtensionResult;
                    }

                    if (staticExtensionBest.HasValue)
                    {
                        best = staticExtensionBest.GetValueOrDefault();
                        resultKind = staticExtensionResultKind;
                        originalUserDefinedOperators = staticExtensionOriginalUserDefinedOperators;
                    }
                }

                if (!best.HasValue)
                {
                    return createBadCompoundAssignmentOperator(node, kind, left, right, resultKind, originalUserDefinedOperators, ref operatorResolutionForReporting, diagnostics);
                }

                if (best.Signature.Method is { } bestMethod)
                {
                    ReportObsoleteAndFeatureAvailabilityDiagnostics(bestMethod, node, diagnostics);
                    ReportUseSite(bestMethod, diagnostics, node);
                }

                // The rules in the spec for determining additional errors are bit confusing. In particular
                // this line is misleading:
                //
                // "for predefined operators ... x op= y is permitted if both x op y and x = y are permitted"
                //
                // That's not accurate in many cases. For example, "x += 1" is permitted if x is string or
                // any enum type, but x = 1 is not legal for strings or enums.
                //
                // The correct rules are spelled out in the spec:
                //
                // Spec §7.17.2:
                // An operation of the form x op= y is processed by applying binary operator overload
                // resolution (§7.3.4) as if the operation was written x op y.
                // Let R be the return type of the selected operator, and T the type of x. Then,
                //
                // * If an implicit conversion from an expression of type R to the type T exists,
                //   the operation is evaluated as x = (T)(x op y), except that x is evaluated only once.
                //   [no cast is inserted, unless the conversion is implicit dynamic]
                // * Otherwise, if
                //   (1) the selected operator is a predefined operator,
                //   (2) if R is explicitly convertible to T, and
                //   (3.1) if y is implicitly convertible to T or
                //   (3.2) the operator is a shift operator... [then cast the result to T]
                // * Otherwise ... a binding-time error occurs.

                // So let's tease that out. There are two possible errors: the conversion from the
                // operator result type to the left hand type could be bad, and the conversion
                // from the right hand side to the left hand type could be bad.
                //
                // We report the first error under the following circumstances:
                //
                // * The final conversion is bad, or
                // * The final conversion is explicit and the selected operator is not predefined
                //
                // We report the second error under the following circumstances:
                //
                // * The final conversion is explicit, and
                // * The selected operator is predefined, and
                // * the selected operator is not a shift, and
                // * the right-to-left conversion is not implicit

                bool hasError = false;

                BinaryOperatorSignature bestSignature = best.Signature;

                CheckNativeIntegerFeatureAvailability(bestSignature.Kind, node, diagnostics);
                CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, bestSignature.Method,
                    isUnsignedRightShift: bestSignature.Kind.Operator() == BinaryOperatorKind.UnsignedRightShift, bestSignature.ConstrainedToTypeOpt, diagnostics);

                if (checkOverflowAtRuntime)
                {
                    bestSignature = new BinaryOperatorSignature(
                        bestSignature.Kind.WithOverflowChecksIfApplicable(checkOverflowAtRuntime),
                        bestSignature.LeftType,
                        bestSignature.RightType,
                        bestSignature.ReturnType,
                        bestSignature.Method,
                        bestSignature.ConstrainedToTypeOpt);
                }

                BoundExpression rightConverted = CreateConversion(right, best.RightConversion, bestSignature.RightType, diagnostics);

                bool isPredefinedOperator = !bestSignature.Kind.IsUserDefined();

                var leftType = left.Type;
                Debug.Assert(leftType is { });

                var finalPlaceholder = new BoundValuePlaceholder(node, bestSignature.ReturnType);

                BoundExpression? finalConversion = GenerateConversionForAssignment(leftType, finalPlaceholder, diagnostics,
                                ConversionForAssignmentFlags.CompoundAssignment |
                                (isPredefinedOperator ? ConversionForAssignmentFlags.PredefinedOperator : ConversionForAssignmentFlags.None));

                if (finalConversion.HasErrors)
                {
                    hasError = true;
                }

                if (finalConversion is not BoundConversion final)
                {
                    Debug.Assert(finalConversion.HasErrors || (object)finalConversion == finalPlaceholder);
                    if ((object)finalConversion != finalPlaceholder)
                    {
                        finalPlaceholder = null;
                        finalConversion = null;
                    }
                }
                else if (final.Conversion.IsExplicit &&
                    isPredefinedOperator &&
                    !kind.IsShift())
                {
                    Conversion rightToLeftConversion = this.Conversions.ClassifyConversionFromExpression(right, leftType, isChecked: checkOverflowAtRuntime, ref useSiteInfo);
                    if (!rightToLeftConversion.IsImplicit || !rightToLeftConversion.IsValid)
                    {
                        hasError = true;
                        GenerateImplicitConversionError(diagnostics, node, rightToLeftConversion, right, leftType);
                    }
                }

                diagnostics.Add(node, useSiteInfo);

                if (!hasError && !bestSignature.Kind.IsUserDefined() && leftType.IsVoidPointer())
                {
                    Error(diagnostics, ErrorCode.ERR_VoidError, node);
                    hasError = true;
                }

                // Any events that weren't handled above (by BindEventAssignment) are bad - we just followed this
                // code path for the diagnostics.  Make sure we don't report success.
                Debug.Assert(left.Kind != BoundKind.EventAccess || hasError);

                var leftPlaceholder = new BoundValuePlaceholder(left.Syntax, leftType).MakeCompilerGenerated();
                var leftConversion = CreateConversion(node.Left, leftPlaceholder, best.LeftConversion, isCast: false, conversionGroupOpt: null, best.Signature.LeftType, diagnostics);

                return new BoundCompoundAssignmentOperator(node, bestSignature, left, rightConverted,
                    leftPlaceholder, leftConversion, finalPlaceholder, finalConversion, resultKind, originalUserDefinedOperators, leftType, hasError);
            }

            BoundCompoundAssignmentOperator createBadCompoundAssignmentOperator(
                AssignmentExpressionSyntax node,
                BinaryOperatorKind kind,
                BoundExpression left,
                BoundExpression right,
                LookupResultKind resultKind,
                ImmutableArray<MethodSymbol> originalUserDefinedOperators,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                ReportAssignmentOperatorError(node, kind, diagnostics, left, right, resultKind, ref operatorResolutionForReporting);
                left = BindToTypeForErrorRecovery(left);
                right = BindToTypeForErrorRecovery(right);
                return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                    leftPlaceholder: null, leftConversion: null, finalPlaceholder: null, finalConversion: null, resultKind, originalUserDefinedOperators, CreateErrorType(), hasErrors: true);
            }

            bool shouldTryUserDefinedInstanceOperator(AssignmentExpressionSyntax node, bool checkOverflowAtRuntime, BoundExpression left, out string? checkedName, out string? ordinaryName)
            {
                var leftType = left.Type;
                Debug.Assert(!left.HasDynamicType());

                checkedName = null;
                ordinaryName = null;

                if (leftType is null ||
                    !SyntaxFacts.IsOverloadableCompoundAssignmentOperator(node.OperatorToken.Kind()) ||
                    !node.IsFeatureEnabled(MessageID.IDS_FeatureUserDefinedCompoundAssignmentOperators))
                {
                    return false;
                }

                if (!CheckValueKind(node, left, BindValueKind.RefersToLocation | BindValueKind.Assignable, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                {
                    return false;
                }

                if (checkOverflowAtRuntime)
                {
                    checkedName = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(node.OperatorToken.Kind(), isChecked: true);
                    if (!SyntaxFacts.IsCheckedOperator(checkedName))
                    {
                        checkedName = null;
                    }
                }

                ordinaryName = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(node.OperatorToken.Kind(), isChecked: false);

                return true;
            }

            BoundCompoundAssignmentOperator? tryApplyUserDefinedInstanceOperator(
                AssignmentExpressionSyntax node,
                BinaryOperatorKind kind,
                bool checkOverflowAtRuntime,
                string? checkedName,
                string ordinaryName,
                BoundExpression left,
                BoundExpression right,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                var leftType = left.Type;
                Debug.Assert(leftType is not null);

                if (leftType.SpecialType.IsNumericType())
                {
                    return null;
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                ArrayBuilder<MethodSymbol>? methods = LookupUserDefinedInstanceOperators(
                    leftType,
                    checkedName: checkedName,
                    ordinaryName: ordinaryName,
                    parameterCount: 1,
                    ref useSiteInfo);

                diagnostics.Add(node, useSiteInfo);

                if (methods?.IsEmpty != false)
                {
                    methods?.Free();
                    return null;
                }

                AnalyzedArguments? analyzedArguments = null;

                BoundCompoundAssignmentOperator? inPlaceResult = tryInstanceOperatorOverloadResolutionAndFreeMethods(node, kind, checkOverflowAtRuntime, isExtension: false, left, right, ref analyzedArguments, methods, ref operatorResolutionForReporting, diagnostics);

                Debug.Assert(analyzedArguments is not null);
                analyzedArguments.Free();

                return inPlaceResult;
            }

            BoundCompoundAssignmentOperator? tryInstanceOperatorOverloadResolutionAndFreeMethods(
                AssignmentExpressionSyntax node,
                BinaryOperatorKind kind,
                bool checkOverflowAtRuntime,
                bool isExtension,
                BoundExpression left,
                BoundExpression right,
                ref AnalyzedArguments? analyzedArguments,
                ArrayBuilder<MethodSymbol> methods,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(!methods.IsEmpty);

                var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                var leftType = left.Type;
                Debug.Assert(leftType is not null);

                if (analyzedArguments == null)
                {
                    analyzedArguments = AnalyzedArguments.GetInstance();

                    if (isExtension)
                    {
                        // Create a set of arguments for overload resolution including the receiver.
                        CombineExtensionMethodArguments(left, originalArguments: null, analyzedArguments);

                        if (leftType.IsValueType)
                        {
                            Debug.Assert(analyzedArguments.RefKinds.Count == 0);
                            analyzedArguments.RefKinds.Add(RefKind.Ref);
                            analyzedArguments.RefKinds.Add(RefKind.None);
                        }
                    }

                    analyzedArguments.Arguments.Add(right);
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                OverloadResolution.MethodInvocationOverloadResolution(
                    methods,
                    typeArguments,
                    left,
                    analyzedArguments,
                    overloadResolutionResult,
                    ref useSiteInfo,
                    OverloadResolution.Options.DisallowExpandedForm | (isExtension ? OverloadResolution.Options.IsExtensionMethodResolution : OverloadResolution.Options.None));

                typeArguments.Free();
                diagnostics.Add(node, useSiteInfo);

                BoundCompoundAssignmentOperator? inPlaceResult;
                if (overloadResolutionResult.Succeeded)
                {
                    var method = overloadResolutionResult.ValidResult.Member;

                    BoundExpression rightConverted = CreateConversion(right, overloadResolutionResult.ValidResult.Result.ConversionForArg(isExtension ? 1 : 0), method.Parameters[0].Type, diagnostics);

                    ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver: false);
                    ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, method, node, isDelegateConversion: false);

                    BoundValuePlaceholder? leftPlaceholder = null;
                    BoundExpression? leftConversion = null;

                    if (isExtension)
                    {
                        Debug.Assert(method.ContainingType.ExtensionParameter is not null);

                        if (Compilation.SourceModule != method.ContainingModule)
                        {
                            // While this code path is reachable, its effect is not observable
                            // because instance operators are simply not considered when the target 
                            // version is C#13 or earlier. Coincidentally the following line
                            // would produce diagnostics only for C#13 or earlier.
                            CheckFeatureAvailability(node, MessageID.IDS_FeatureExtensions, diagnostics);
                        }

                        Conversion conversion = overloadResolutionResult.ValidResult.Result.ConversionForArg(0);

                        if (conversion.Kind is not ConversionKind.Identity)
                        {
                            Debug.Assert(conversion.Kind is ConversionKind.ImplicitReference);
                            Debug.Assert(leftType.IsReferenceType);

                            leftPlaceholder = new BoundValuePlaceholder(left.Syntax, leftType).MakeCompilerGenerated();
                            leftConversion = CreateConversion(node, leftPlaceholder, conversion, isCast: false, conversionGroupOpt: null, method.ContainingType.ExtensionParameter.Type, diagnostics);
                        }
                    }

                    inPlaceResult = new BoundCompoundAssignmentOperator(
                        node,
                        new BinaryOperatorSignature(
                            kind.WithOverflowChecksIfApplicable(checkOverflowAtRuntime),
                            leftType: leftType,
                            rightType: method.Parameters[0].Type,
                            returnType: leftType,
                            method: method,
                            constrainedToTypeOpt: null),
                        left: left,
                        right: rightConverted,
                        leftPlaceholder: leftPlaceholder, leftConversion: leftConversion, finalPlaceholder: null, finalConversion: null,
                        resultKind: LookupResultKind.Viable,
                        originalUserDefinedOperatorsOpt: ImmutableArray<MethodSymbol>.Empty,
                        getResultType(node, leftType, diagnostics));

                    methods.Free();
                }
                else if (overloadResolutionResult.HasAnyApplicableMember)
                {
                    ImmutableArray<MethodSymbol> methodsArray = methods.ToImmutableAndFree();

                    overloadResolutionResult.ReportDiagnostics(
                        binder: this, location: node.OperatorToken.GetLocation(), nodeOpt: node, diagnostics: diagnostics, name: node.OperatorToken.ValueText,
                        receiver: left, invokedExpression: node, arguments: analyzedArguments, memberGroup: methodsArray,
                        typeContainingConstructor: null, delegateTypeBeingInvoked: null);

                    inPlaceResult = new BoundCompoundAssignmentOperator(
                        node,
                        BinaryOperatorSignature.Error,
                        left,
                        BindToTypeForErrorRecovery(right),
                        leftPlaceholder: null, leftConversion: null, finalPlaceholder: null, finalConversion: null,
                        resultKind: LookupResultKind.OverloadResolutionFailure,
                        originalUserDefinedOperatorsOpt: methodsArray,
                        getResultType(node, leftType, diagnostics));
                }
                else
                {
                    inPlaceResult = null;
                    methods.Free();
                }

                if (!operatorResolutionForReporting.SaveResult(overloadResolutionResult, isExtension))
                {
                    overloadResolutionResult.Free();
                }

                return inPlaceResult;
            }

            TypeSymbol getResultType(ExpressionSyntax node, TypeSymbol leftType, BindingDiagnosticBag diagnostics)
            {
                return ResultIsUsed(node) ? leftType : GetSpecialType(SpecialType.System_Void, diagnostics, node);
            }

            // This method returns result in two ways:
            // - If it has a result due to instance extensions, it returns ready to use BoundCompoundAssignmentOperator 
            // - If it has static extensions result, it returns information via out parameters (staticBest, staticResultKind, staticOriginalUserDefinedOperators). 
            BoundCompoundAssignmentOperator? tryApplyUserDefinedExtensionOperator(
                AssignmentExpressionSyntax node,
                BinaryOperatorKind kind,
                bool tryInstance,
                bool checkOverflowAtRuntime,
                string staticOperatorName1,
                string? staticOperatorName2Opt,
                string? checkedInstanceOperatorName,
                string? ordinaryInstanceOperatorName,
                BoundExpression left,
                BoundExpression right,
                BindingDiagnosticBag diagnostics,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                out BinaryOperatorAnalysisResult? staticBest,
                out LookupResultKind staticResultKind,
                out ImmutableArray<MethodSymbol> staticOriginalUserDefinedOperators)
            {
                staticBest = null;
                staticResultKind = LookupResultKind.Empty;
                staticOriginalUserDefinedOperators = [];

                BinaryOperatorOverloadResolutionResult? result = BinaryOperatorOverloadResolutionResult.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var extensionCandidatesInSingleScope = ArrayBuilder<Symbol>.GetInstance();
                BoundCompoundAssignmentOperator? inPlaceResult = null;
                AnalyzedArguments? analyzedArguments = null;

                foreach (var scope in new ExtensionScopes(this))
                {
                    // Try an in-place user-defined operator
                    if (tryInstance)
                    {
                        Debug.Assert(ordinaryInstanceOperatorName is not null);

                        extensionCandidatesInSingleScope.Clear();
                        scope.Binder.GetCandidateExtensionMembersInSingleBinder(extensionCandidatesInSingleScope,
                            ordinaryInstanceOperatorName, checkedInstanceOperatorName, arity: 0,
                            LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustBeInstance, this);

                        inPlaceResult = tryApplyUserDefinedInstanceExtensionOperatorInSingleScope(
                            node, extensionCandidatesInSingleScope, kind, checkOverflowAtRuntime,
                            checkedInstanceOperatorName, ordinaryInstanceOperatorName,
                            left, right, ref analyzedArguments, ref operatorResolutionForReporting, diagnostics);
                        if (inPlaceResult is not null)
                        {
                            break;
                        }
                    }

                    extensionCandidatesInSingleScope.Clear();
                    scope.Binder.GetCandidateExtensionMembersInSingleBinder(extensionCandidatesInSingleScope,
                        staticOperatorName1, staticOperatorName2Opt, arity: 0,
                        LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustNotBeInstance, this);

                    if (this.OverloadResolution.BinaryOperatorExtensionOverloadResolutionInSingleScope(
                        extensionCandidatesInSingleScope, kind, checkOverflowAtRuntime,
                        staticOperatorName1, staticOperatorName2Opt,
                        left, right, result, ref useSiteInfo))
                    {
                        staticBest = BinaryOperatorAnalyzeOverloadResolutionResult(result, out staticResultKind, out staticOriginalUserDefinedOperators);
                        if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                        {
                            result = null;
                        }

                        break;
                    }

                    if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                    {
                        result = BinaryOperatorOverloadResolutionResult.GetInstance();
                    }
                }

                diagnostics.Add(node, useSiteInfo);

                analyzedArguments?.Free();
                extensionCandidatesInSingleScope.Free();
                result?.Free();
                return inPlaceResult;
            }

            BoundCompoundAssignmentOperator? tryApplyUserDefinedInstanceExtensionOperatorInSingleScope(
                AssignmentExpressionSyntax node,
                ArrayBuilder<Symbol> extensionCandidatesInSingleScope,
                BinaryOperatorKind kind,
                bool checkOverflowAtRuntime,
                string? checkedName,
                string ordinaryName,
                BoundExpression left,
                BoundExpression right,
                ref AnalyzedArguments? analyzedArguments,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                ArrayBuilder<MethodSymbol>? methods = LookupUserDefinedInstanceExtensionOperatorsInSingleScope(
                    extensionCandidatesInSingleScope,
                    checkedName: checkedName,
                    ordinaryName: ordinaryName,
                    parameterCount: 1);

                if (methods?.IsEmpty != false)
                {
                    methods?.Free();
                    return null;
                }

                return tryInstanceOperatorOverloadResolutionAndFreeMethods(node, kind, checkOverflowAtRuntime, isExtension: true, left, right, ref analyzedArguments, methods, ref operatorResolutionForReporting, diagnostics);
            }
        }

#nullable disable

        /// <summary>
        /// For "receiver.event += expr", produce "receiver.add_event(expr)".
        /// For "receiver.event -= expr", produce "receiver.remove_event(expr)".
        /// </summary>
        /// <remarks>
        /// Performs some validation of the accessor that couldn't be done in CheckEventValueKind, because
        /// the specific accessor wasn't known.
        /// </remarks>
        private BoundExpression BindEventAssignment(AssignmentExpressionSyntax node, BoundEventAccess left, BoundExpression right, BinaryOperatorKind opKind, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(opKind == BinaryOperatorKind.Addition || opKind == BinaryOperatorKind.Subtraction);

            bool hasErrors = false;

            EventSymbol eventSymbol = left.EventSymbol;
            BoundExpression receiverOpt = left.ReceiverOpt;

            TypeSymbol delegateType = left.Type;

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion argumentConversion = this.Conversions.ClassifyConversionFromExpression(right, delegateType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);

            if (!argumentConversion.IsImplicit || !argumentConversion.IsValid) // NOTE: dev10 appears to allow user-defined conversions here.
            {
                hasErrors = true;
                if (delegateType.IsDelegateType()) // Otherwise, suppress cascading.
                {
                    GenerateImplicitConversionError(diagnostics, node, argumentConversion, right, delegateType);
                }
            }

            BoundExpression argument = CreateConversion(right, argumentConversion, delegateType, diagnostics);

            bool isAddition = opKind == BinaryOperatorKind.Addition;
            MethodSymbol method = isAddition ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;

            TypeSymbol type;
            if ((object)method == null)
            {
                type = this.GetSpecialType(SpecialType.System_Void, diagnostics, node); //we know the return type would have been void

                // There will be a diagnostic on the declaration if it is from source.
                if (!eventSymbol.OriginalDefinition.IsFromCompilation(this.Compilation))
                {
                    // CONSIDER: better error code?  ERR_EventNeedsBothAccessors?
                    Error(diagnostics, ErrorCode.ERR_MissingPredefinedMember, node, delegateType, SourceEventSymbol.GetAccessorName(eventSymbol.Name, isAddition));
                }
            }
            else
            {
                CheckImplicitThisCopyInReadOnlyMember(receiverOpt, method, diagnostics);

                if (!this.IsAccessible(method, ref useSiteInfo, this.GetAccessThroughType(receiverOpt)))
                {
                    // CONSIDER: depending on the accessibility (e.g. if it's private), dev10 might just report the whole event bogus.
                    Error(diagnostics, ErrorCode.ERR_BadAccess, node, method);
                    hasErrors = true;
                }
                else if (IsBadBaseAccess(node, receiverOpt, method, diagnostics, eventSymbol))
                {
                    hasErrors = true;
                }
                else
                {
                    CheckReceiverAndRuntimeSupportForSymbolAccess(node, receiverOpt, method, diagnostics);
                }

                if (eventSymbol.IsWindowsRuntimeEvent)
                {
                    // Return type is actually void because this call will be later encapsulated in a call
                    // to WindowsRuntimeMarshal.AddEventHandler or RemoveEventHandler, which has the return
                    // type of void.
                    type = this.GetSpecialType(SpecialType.System_Void, diagnostics, node);
                }
                else
                {
                    type = method.ReturnType;
                }
            }

            diagnostics.Add(node, useSiteInfo);

            return new BoundEventAssignmentOperator(
                syntax: node,
                @event: eventSymbol,
                isAddition: isAddition,
                isDynamic: right.HasDynamicType(),
                receiverOpt: receiverOpt,
                argument: argument,
                type: type,
                hasErrors: hasErrors);
        }

        private static bool IsLegalDynamicOperand(BoundExpression operand)
        {
            Debug.Assert(operand != null);

            TypeSymbol type = operand.Type;

            // Literal null is a legal operand to a dynamic operation. The other typeless expressions --
            // method groups, lambdas, anonymous methods -- are not.

            // If the operand is of a class, interface, delegate, array, struct, enum, nullable
            // or type param types, it's legal to use in a dynamic expression. In short, the type
            // must be one that is convertible to object.

            if ((object)type == null)
            {
                return operand.IsLiteralNull();
            }

            // Pointer types and very special types are not convertible to object.

            return !type.IsPointerOrFunctionPointer() && !type.IsRestrictedType() && !type.IsVoidType();
        }

        private BoundExpression BindDynamicBinaryOperator(
            BinaryExpressionSyntax node,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            BindingDiagnosticBag diagnostics)
        {
            // This method binds binary * / % + - << >> < > <= >= == != & ! ^ && || operators where one or both
            // of the operands are dynamic.
            Debug.Assert((object)left.Type != null && left.Type.IsDynamic() || (object)right.Type != null && right.Type.IsDynamic());

            bool hasError = false;
            bool leftValidOperand = IsLegalDynamicOperand(left);
            bool rightValidOperand = IsLegalDynamicOperand(right);

            if (!leftValidOperand || !rightValidOperand || kind == BinaryOperatorKind.UnsignedRightShift)
            {
                // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
                Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, node.OperatorToken.Text, left.Display, right.Display);
                hasError = true;
            }

            MethodSymbol userDefinedOperator = null;

            if (kind.IsLogical() && leftValidOperand)
            {
                // We need to make sure left is either implicitly convertible to Boolean or has user defined truth operator.
                //   left && right is lowered to {op_False|op_Implicit}(left) ? left : And(left, right)
                //   left || right is lowered to {op_True|!op_Implicit}(left) ? left : Or(left, right)
                if (!IsValidDynamicCondition(left, isNegative: kind == BinaryOperatorKind.LogicalAnd, diagnostics, userDefinedOperator: out userDefinedOperator))
                {
                    // Dev11 reports ERR_MustHaveOpTF. The error was shared between this case and user-defined binary Boolean operators.
                    // We report two distinct more specific error messages.
                    Error(diagnostics, ErrorCode.ERR_InvalidDynamicCondition, node.Left, left.Type, kind == BinaryOperatorKind.LogicalAnd ? "false" : "true");

                    hasError = true;
                }
                else
                {
                    Debug.Assert(left.Type is not TypeParameterSymbol);
                    CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, userDefinedOperator, isUnsignedRightShift: false, constrainedToTypeOpt: null, diagnostics);
                }
            }

            return new BoundBinaryOperator(
                syntax: node,
                operatorKind: (hasError ? kind : kind.WithType(BinaryOperatorKind.Dynamic)).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                left: BindToNaturalType(left, diagnostics),
                right: BindToNaturalType(right, diagnostics),
                constantValueOpt: ConstantValue.NotAvailable,
                methodOpt: userDefinedOperator,
                constrainedToTypeOpt: null,
                resultKind: LookupResultKind.Viable,
                type: Compilation.DynamicType,
                hasErrors: hasError);
        }

        protected static bool IsSimpleBinaryOperator(SyntaxKind kind)
        {
            // We deliberately exclude &&, ||, ??, etc.
            switch (kind)
            {
                case SyntaxKind.AddExpression:
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.SubtractExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.BitwiseAndExpression:
                case SyntaxKind.BitwiseOrExpression:
                case SyntaxKind.ExclusiveOrExpression:
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                case SyntaxKind.UnsignedRightShiftExpression:
                    return true;
            }
            return false;
        }

        private BoundExpression BindSimpleBinaryOperator(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // The simple binary operators are left-associative, and expressions of the form
            // a + b + c + d .... are relatively common in machine-generated code. The parser can handle
            // creating a deep-on-the-left syntax tree no problem, and then we promptly blow the stack during
            // semantic analysis. Here we build an explicit stack to handle the left-hand recursion.

            Debug.Assert(IsSimpleBinaryOperator(node.Kind()));

            var syntaxNodes = ArrayBuilder<BinaryExpressionSyntax>.GetInstance();

            ExpressionSyntax current = node;
            while (IsSimpleBinaryOperator(current.Kind()))
            {
                var binOp = (BinaryExpressionSyntax)current;
                syntaxNodes.Push(binOp);
                current = binOp.Left;
            }

            BoundExpression result = BindExpression(current, diagnostics);

            if (current is ParenthesizedExpressionSyntax parenthesizedExpression
                && IsParenthesizedExpressionInPossibleBadNegCastContext(parenthesizedExpression))
            {
                if (result.Kind == BoundKind.TypeExpression
                    && !parenthesizedExpression.Expression.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    Error(diagnostics, ErrorCode.ERR_PossibleBadNegCast, node);
                }
                else if (result.Kind == BoundKind.BadExpression)
                {
                    if (parenthesizedExpression.Expression.IsKind(SyntaxKind.IdentifierName)
                        && ((IdentifierNameSyntax)parenthesizedExpression.Expression).Identifier.ValueText == "dynamic")
                    {
                        Error(diagnostics, ErrorCode.ERR_PossibleBadNegCast, node);
                    }
                }
            }

            while (syntaxNodes.Count > 0)
            {
                BinaryExpressionSyntax syntaxNode = syntaxNodes.Pop();
                BindValueKind bindValueKind = GetBinaryAssignmentKind(syntaxNode.Kind());
                BoundExpression left = CheckValue(result, bindValueKind, diagnostics);
                BoundExpression right = BindValue(syntaxNode.Right, diagnostics, BindValueKind.RValue);
                BoundExpression boundOp = BindSimpleBinaryOperator(syntaxNode, diagnostics, left, right, leaveUnconvertedIfInterpolatedString: true);
                result = boundOp;
            }

            syntaxNodes.Free();
            return result;
        }

        private BoundExpression BindSimpleBinaryOperator(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics,
            BoundExpression left, BoundExpression right, bool leaveUnconvertedIfInterpolatedString)
        {
            BinaryOperatorKind kind = SyntaxKindToBinaryOperatorKind(node.Kind());

            // If either operand is bad, don't try to do binary operator overload resolution; that would just
            // make cascading errors.

            if (left.HasAnyErrors || right.HasAnyErrors)
            {
                // NOTE: no user-defined conversion candidates
                left = BindToTypeForErrorRecovery(left);
                right = BindToTypeForErrorRecovery(right);
                return new BoundBinaryOperator(node, kind, ConstantValue.NotAvailable, methodOpt: null, constrainedToTypeOpt: null, LookupResultKind.Empty, left, right, GetBinaryOperatorErrorType(kind, diagnostics, node), true);
            }

            TypeSymbol leftType = left.Type;
            TypeSymbol rightType = right.Type;

            if ((object)leftType != null && leftType.IsDynamic() || (object)rightType != null && rightType.IsDynamic())
            {
                return BindDynamicBinaryOperator(node, kind, left, right, diagnostics);
            }

            // SPEC OMISSION: The C# 2.0 spec had a line in it that noted that the expressions "null == null"
            // SPEC OMISSION: and "null != null" were to be automatically treated as the appropriate constant;
            // SPEC OMISSION: overload resolution was to be skipped.  That's because a strict reading
            // SPEC OMISSION: of the overload resolution spec shows that overload resolution would give an
            // SPEC OMISSION: ambiguity error for this case; the expression is ambiguous between the int?,
            // SPEC OMISSION: bool? and string versions of equality.  This line was accidentally edited
            // SPEC OMISSION: out of the C# 3 specification; we should re-insert it.

            bool leftNull = left.IsLiteralNull();
            bool rightNull = right.IsLiteralNull();
            bool isEquality = kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual;
            if (isEquality && leftNull && rightNull)
            {
                return new BoundLiteral(node, ConstantValue.Create(kind == BinaryOperatorKind.Equal), GetSpecialType(SpecialType.System_Boolean, diagnostics, node));
            }

            if (IsTupleBinaryOperation(left, right) &&
                (kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual))
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureTupleEquality, diagnostics);
                return BindTupleBinaryOperator(node, kind, left, right, diagnostics);
            }

            if (leaveUnconvertedIfInterpolatedString
                && kind == BinaryOperatorKind.Addition
                && left is BoundUnconvertedInterpolatedString or BoundBinaryOperator { IsUnconvertedInterpolatedStringAddition: true }
                && right is BoundUnconvertedInterpolatedString or BoundBinaryOperator { IsUnconvertedInterpolatedStringAddition: true })
            {
                Debug.Assert(right.Type.SpecialType == SpecialType.System_String);
                var stringConstant = FoldBinaryOperator(node, BinaryOperatorKind.StringConcatenation, left, right, right.Type, diagnostics);
                return new BoundBinaryOperator(node, BinaryOperatorKind.StringConcatenation, BoundBinaryOperator.UncommonData.UnconvertedInterpolatedStringAddition(stringConstant), LookupResultKind.Empty, left, right, right.Type);
            }

            // SPEC: For an operation of one of the forms x == null, null == x, x != null, null != x,
            // SPEC: where x is an expression of nullable type, if operator overload resolution
            // SPEC: fails to find an applicable operator, the result is instead computed from
            // SPEC: the HasValue property of x.

            // Note that the spec says "fails to find an applicable operator", not "fails to
            // find a unique best applicable operator." For example:
            // struct X {
            //   public static bool operator ==(X? x, double? y) {...}
            //   public static bool operator ==(X? x, decimal? y) {...}
            //
            // The comparison "x == null" should produce an ambiguity error rather
            // that being bound as !x.HasValue.
            //

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            BinaryOperatorSignature signature;
            BinaryOperatorAnalysisResult best;
            OperatorResolutionForReporting operatorResolutionForReporting = default;

            bool foundOperator = BindSimpleBinaryOperatorParts(node, diagnostics, left, right, kind,
                ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators, out signature, out best);

            BinaryOperatorKind resultOperatorKind = signature.Kind;
            bool hasErrors = false;
            if (!foundOperator)
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind, ref operatorResolutionForReporting);
                resultOperatorKind &= ~BinaryOperatorKind.TypeMask;
                hasErrors = true;
            }

            operatorResolutionForReporting.Free();

            switch (node.Kind())
            {
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                    // Function pointer comparisons are defined on `void*` with implicit conversions to `void*` on both sides. So if this is a
                    // pointer comparison operation, and the underlying types of the left and right are both function pointers, then we need to
                    // warn about them because of JIT recompilation. If either side is explicitly cast to void*, that side's type will be void*,
                    // not delegate*, and we won't warn.
                    if ((resultOperatorKind & BinaryOperatorKind.Pointer) == BinaryOperatorKind.Pointer &&
                        leftType?.TypeKind == TypeKind.FunctionPointer && rightType?.TypeKind == TypeKind.FunctionPointer)
                    {
                        Debug.Assert(!resultOperatorKind.IsUserDefined());
                        // Comparison of function pointers might yield an unexpected result, since pointers to the same function may be distinct.
                        Error(diagnostics, ErrorCode.WRN_DoNotCompareFunctionPointers, node.OperatorToken);
                    }

                    break;
                default:
                    if (!resultOperatorKind.IsUserDefined() && (leftType.IsVoidPointer() || rightType.IsVoidPointer()))
                    {
                        // CONSIDER: dev10 cascades this, but roslyn doesn't have to.
                        Error(diagnostics, ErrorCode.ERR_VoidError, node);
                        hasErrors = true;
                    }
                    break;
            }

            if (foundOperator)
            {
                CheckNativeIntegerFeatureAvailability(resultOperatorKind, node, diagnostics);
                CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, signature.Method,
                    isUnsignedRightShift: resultOperatorKind.Operator() == BinaryOperatorKind.UnsignedRightShift, signature.ConstrainedToTypeOpt, diagnostics);
            }

            TypeSymbol resultType = signature.ReturnType;
            BoundExpression resultLeft = left;
            BoundExpression resultRight = right;
            ConstantValue resultConstant = null;

            if (foundOperator && (resultOperatorKind.OperandTypes() != BinaryOperatorKind.NullableNull))
            {
                Debug.Assert((object)signature.LeftType != null);
                Debug.Assert((object)signature.RightType != null);

                // If this is an object equality operator, we will suppress Lock conversion warnings.
                var needsFilterDiagnostics =
                    resultOperatorKind is BinaryOperatorKind.ObjectEqual or BinaryOperatorKind.ObjectNotEqual &&
                    diagnostics.AccumulatesDiagnostics;
                var conversionDiagnostics = needsFilterDiagnostics ? BindingDiagnosticBag.GetInstance(template: diagnostics) : diagnostics;

                resultLeft = CreateConversion(left, best.LeftConversion, signature.LeftType, conversionDiagnostics);
                resultRight = CreateConversion(right, best.RightConversion, signature.RightType, conversionDiagnostics);

                if (needsFilterDiagnostics)
                {
                    Debug.Assert(conversionDiagnostics != diagnostics);
                    diagnostics.AddDependencies(conversionDiagnostics);

                    var sourceBag = conversionDiagnostics.DiagnosticBag;
                    Debug.Assert(sourceBag is not null);

                    if (!sourceBag.IsEmptyWithoutResolution)
                    {
                        foreach (var diagnostic in sourceBag.AsEnumerableWithoutResolution())
                        {
                            var code = diagnostic is DiagnosticWithInfo { HasLazyInfo: true, LazyInfo.Code: var lazyCode } ? lazyCode : diagnostic.Code;
                            if ((ErrorCode)code is not ErrorCode.WRN_ConvertingLock)
                            {
                                diagnostics.Add(diagnostic);
                            }
                        }
                    }

                    conversionDiagnostics.Free();
                }

                resultConstant = FoldBinaryOperator(node, resultOperatorKind, resultLeft, resultRight, resultType, diagnostics);
            }
            else
            {
                // If we found an operator, we'll have given the `default` literal a type.
                // Otherwise, we'll have reported the problem in ReportBinaryOperatorError.
                resultLeft = BindToNaturalType(resultLeft, diagnostics, reportNoTargetType: false);
                resultRight = BindToNaturalType(resultRight, diagnostics, reportNoTargetType: false);
            }

            hasErrors = hasErrors || resultConstant != null && resultConstant.IsBad;

            return new BoundBinaryOperator(
                node,
                resultOperatorKind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                resultLeft,
                resultRight,
                resultConstant,
                signature.Method,
                signature.ConstrainedToTypeOpt,
                resultKind,
                originalUserDefinedOperators,
                resultType,
                hasErrors);
        }

        private bool BindSimpleBinaryOperatorParts(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics, BoundExpression left, BoundExpression right, BinaryOperatorKind kind,
            ref OperatorResolutionForReporting operatorResolutionForReporting, out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators, out BinaryOperatorSignature resultSignature, out BinaryOperatorAnalysisResult best)
        {
            if (!IsTypelessExpressionAllowedInBinaryOperator(kind, left, right))
            {
                resultKind = LookupResultKind.OverloadResolutionFailure;
                originalUserDefinedOperators = default(ImmutableArray<MethodSymbol>);
                best = default(BinaryOperatorAnalysisResult);
                resultSignature = new BinaryOperatorSignature(kind, leftType: null, rightType: null, CreateErrorType());
                return false;
            }

            bool isChecked = CheckOverflowAtRuntime;
            OverloadResolution.GetStaticUserDefinedBinaryOperatorMethodNames(kind, isChecked, out string name1, out string name2Opt);
            best = this.BinaryOperatorOverloadResolution(kind, isChecked, name1, name2Opt, left, right, node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);

            return bindSimpleBinaryOperatorPartsContinue(node, diagnostics, left, right, kind, ref resultKind, ref originalUserDefinedOperators, out resultSignature, ref best, isChecked, name1, name2Opt, ref operatorResolutionForReporting);

            bool bindSimpleBinaryOperatorPartsContinue(
                BinaryExpressionSyntax node,
                BindingDiagnosticBag diagnostics,
                BoundExpression left,
                BoundExpression right,
                BinaryOperatorKind kind,
                ref LookupResultKind resultKind,
                ref ImmutableArray<MethodSymbol> originalUserDefinedOperators,
                out BinaryOperatorSignature resultSignature,
                ref BinaryOperatorAnalysisResult best,
                bool isChecked,
                string name1,
                string name2Opt,
                ref OperatorResolutionForReporting operatorResolutionForReporting)
            {
                // However, as an implementation detail, we never "fail to find an applicable
                // operator" during overload resolution if we have x == null, x == default, etc. We always
                // find at least the reference conversion object == object; the overload resolution
                // code does not reject that.  Therefore what we should do is only bind
                // "x == null" as a nullable-to-null comparison if overload resolution chooses
                // the reference conversion.

                if (!best.HasValue)
                {
                    resultSignature = new BinaryOperatorSignature(kind, leftType: null, rightType: null, CreateErrorType());
                    return false;
                }

                bool foundOperator;
                var signature = best.Signature;

                if (signature.Method is { } bestMethod)
                {
                    ReportObsoleteAndFeatureAvailabilityDiagnostics(bestMethod, node, diagnostics);
                    ReportUseSite(bestMethod, diagnostics, node);
                }

                bool isObjectEquality = signature.Kind == BinaryOperatorKind.ObjectEqual || signature.Kind == BinaryOperatorKind.ObjectNotEqual;

                bool leftNull = left.IsLiteralNull();
                bool rightNull = right.IsLiteralNull();

                TypeSymbol leftType = left.Type;
                TypeSymbol rightType = right.Type;

                bool isNullableEquality = (object)signature.Method == null &&
                    (signature.Kind.Operator() == BinaryOperatorKind.Equal || signature.Kind.Operator() == BinaryOperatorKind.NotEqual) &&
                    (leftNull && (object)rightType != null && rightType.IsNullableType() ||
                        rightNull && (object)leftType != null && leftType.IsNullableType());

                if (isNullableEquality)
                {
                    resultSignature = new BinaryOperatorSignature(kind | BinaryOperatorKind.NullableNull, leftType: null, rightType: null,
                        GetSpecialType(SpecialType.System_Boolean, diagnostics, node));

                    foundOperator = true;
                }
                else
                {
                    resultSignature = signature;
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    bool leftDefault = left.IsLiteralDefault();
                    bool rightDefault = right.IsLiteralDefault();
                    foundOperator = !isObjectEquality || BuiltInOperators.IsValidObjectEquality(Conversions, leftType, leftNull, leftDefault, rightType, rightNull, rightDefault, ref useSiteInfo);
                    diagnostics.Add(node, useSiteInfo);

                    if (!foundOperator)
                    {
                        Debug.Assert(isObjectEquality);

                        // Try extension operators since predefined object equality was not applicable
                        LookupResultKind extensionResultKind;
                        ImmutableArray<MethodSymbol> extensionOriginalUserDefinedOperators;
                        BinaryOperatorAnalysisResult? extensionBest = BinaryOperatorExtensionOverloadResolution(kind, isChecked, name1, name2Opt, left, right, node, diagnostics,
                            ref operatorResolutionForReporting, out extensionResultKind, out extensionOriginalUserDefinedOperators);

                        if (extensionBest.HasValue)
                        {
                            best = extensionBest.GetValueOrDefault();
                            resultKind = extensionResultKind;
                            originalUserDefinedOperators = extensionOriginalUserDefinedOperators;
                            foundOperator = bindSimpleBinaryOperatorPartsContinue(node, diagnostics, left, right, kind, ref resultKind, ref originalUserDefinedOperators, out resultSignature, ref best, isChecked, name1, name2Opt, ref operatorResolutionForReporting);
                        }
                    }
                }

                return foundOperator;
            }
        }

#nullable enable
        private BoundExpression RebindSimpleBinaryOperatorAsConverted(BoundBinaryOperator unconvertedBinaryOperator, BindingDiagnosticBag diagnostics)
        {
            if (TryBindUnconvertedBinaryOperatorToDefaultInterpolatedStringHandler(unconvertedBinaryOperator, diagnostics, out var convertedBinaryOperator))
            {
                return convertedBinaryOperator;
            }

            var result = doRebind(diagnostics, unconvertedBinaryOperator);
            return result;

            BoundExpression doRebind(BindingDiagnosticBag diagnostics, BoundBinaryOperator? current)
            {
                var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
                while (current != null)
                {
                    Debug.Assert(current.IsUnconvertedInterpolatedStringAddition);
                    stack.Push(current);
                    current = current.Left as BoundBinaryOperator;
                }

                Debug.Assert(stack.Count > 0 && stack.Peek().Left is BoundUnconvertedInterpolatedString);

                BoundExpression? left = null;
                while (stack.TryPop(out current))
                {
                    var right = current.Right switch
                    {
                        BoundUnconvertedInterpolatedString s => s,
                        BoundBinaryOperator b => doRebind(diagnostics, b),
                        _ => throw ExceptionUtilities.UnexpectedValue(current.Right.Kind)
                    };

                    // https://github.com/dotnet/roslyn/issues/78965: Add test coverage for this code path

                    left = BindSimpleBinaryOperator((BinaryExpressionSyntax)current.Syntax, diagnostics, left ?? current.Left, right, leaveUnconvertedIfInterpolatedString: false);
                }

                Debug.Assert(left != null);
                Debug.Assert(stack.Count == 0);
                stack.Free();
                return left;
            }
        }

        private void ReportUnaryOperatorError(CSharpSyntaxNode node, BindingDiagnosticBag diagnostics, string operatorName, BoundExpression operand, LookupResultKind resultKind, ref OperatorResolutionForReporting operatorResolutionForReporting)
        {
            if (operand.IsLiteralDefault())
            {
                // We'll have reported an error for not being able to target-type `default` so we can avoid a cascading diagnostic
                return;
            }

            if (operatorResolutionForReporting.TryReportDiagnostics(node, this, operand.Display, null, diagnostics))
            {
                return;
            }

            ErrorCode errorCode = resultKind == LookupResultKind.Ambiguous ?
                ErrorCode.ERR_AmbigUnaryOp : // Operator '{0}' is ambiguous on an operand of type '{1}'
                ErrorCode.ERR_BadUnaryOp;    // Operator '{0}' cannot be applied to operand of type '{1}'

            Error(diagnostics, errorCode, node, operatorName, operand.Display);
        }

        private void ReportAssignmentOperatorError(AssignmentExpressionSyntax node, BinaryOperatorKind kind, BindingDiagnosticBag diagnostics, BoundExpression left, BoundExpression right,
            LookupResultKind resultKind, ref OperatorResolutionForReporting operatorResolutionForReporting)
        {
            if (IsTypelessExpressionAllowedInBinaryOperator(kind, left, right) &&
                node.OperatorToken.RawKind is (int)SyntaxKind.PlusEqualsToken or (int)SyntaxKind.MinusEqualsToken &&
                (object?)left.Type != null && left.Type.TypeKind == TypeKind.Delegate)
            {
                // Special diagnostic for delegate += and -= about wrong right-hand-side
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var conversion = this.Conversions.ClassifyConversionFromExpression(right, left.Type, isChecked: CheckOverflowAtRuntime, ref discardedUseSiteInfo);
                Debug.Assert(!conversion.IsImplicit);
                GenerateImplicitConversionError(diagnostics, right.Syntax, conversion, right, left.Type);
                // discard use-site diagnostics
            }
            else
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind, ref operatorResolutionForReporting);
            }
        }

        private void ReportBinaryOperatorError(ExpressionSyntax node, BindingDiagnosticBag diagnostics, SyntaxToken operatorToken, BoundExpression left, BoundExpression right,
            LookupResultKind resultKind, ref OperatorResolutionForReporting operatorResolutionForReporting)
        {
            if (operatorResolutionForReporting.TryReportDiagnostics(node, this, left.Display, right.Display, diagnostics))
            {
                return;
            }

            bool isEquality = operatorToken.Kind() == SyntaxKind.EqualsEqualsToken || operatorToken.Kind() == SyntaxKind.ExclamationEqualsToken;
            switch (left.Kind, right.Kind)
            {
                case (BoundKind.DefaultLiteral, _) when !isEquality:
                case (_, BoundKind.DefaultLiteral) when !isEquality:
                    // other than == and !=, binary operators are disallowed on `default` literal
                    Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, node, operatorToken.Text, "default");
                    return;
                case (BoundKind.DefaultLiteral, BoundKind.DefaultLiteral):
                    Error(diagnostics, ErrorCode.ERR_AmbigBinaryOpsOnDefault, node, operatorToken.Text, left.Display, right.Display);
                    return;
                case (BoundKind.DefaultLiteral, _) when right.Type is TypeParameterSymbol:
                    Debug.Assert(!right.Type.IsReferenceType);
                    Error(diagnostics, ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, node, operatorToken.Text, right.Type);
                    return;
                case (_, BoundKind.DefaultLiteral) when left.Type is TypeParameterSymbol:
                    Debug.Assert(!left.Type.IsReferenceType);
                    Error(diagnostics, ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault, node, operatorToken.Text, left.Type);
                    return;
                case (BoundKind.UnconvertedObjectCreationExpression, _):
                    Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, node, operatorToken.Text, left.Display);
                    return;
                case (_, BoundKind.UnconvertedObjectCreationExpression):
                    Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, node, operatorToken.Text, right.Display);
                    return;
            }

            ErrorCode errorCode;

            switch (resultKind)
            {
                case LookupResultKind.Ambiguous:
                    errorCode = ErrorCode.ERR_AmbigBinaryOps; // Operator '{0}' is ambiguous on operands of type '{1}' and '{2}'
                    break;

                case LookupResultKind.OverloadResolutionFailure when operatorToken.Kind() is SyntaxKind.PlusToken && isReadOnlySpanOfByte(left.Type) && isReadOnlySpanOfByte(right.Type):
                    errorCode = ErrorCode.ERR_BadBinaryReadOnlySpanConcatenation; // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}' that are not UTF-8 byte representations
                    break;

                default:
                    errorCode = ErrorCode.ERR_BadBinaryOps;    // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
                    break;
            }

            Error(diagnostics, errorCode, node, operatorToken.Text, left.Display, right.Display);

            bool isReadOnlySpanOfByte(TypeSymbol? type)
            {
                return type is NamedTypeSymbol namedType && Compilation.IsReadOnlySpanType(namedType) &&
                    namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single().Type.SpecialType is SpecialType.System_Byte;

            }
#nullable disable
        }

        private BoundExpression BindConditionalLogicalOperator(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.LogicalOrExpression || node.Kind() == SyntaxKind.LogicalAndExpression);

            // Do not blow the stack due to a deep recursion on the left.

            BinaryExpressionSyntax binary = node;
            ExpressionSyntax child;

            while (true)
            {
                child = binary.Left;
                var childAsBinary = child as BinaryExpressionSyntax;

                if (childAsBinary == null ||
                    (childAsBinary.Kind() != SyntaxKind.LogicalOrExpression && childAsBinary.Kind() != SyntaxKind.LogicalAndExpression))
                {
                    break;
                }

                binary = childAsBinary;
            }

            BoundExpression left = BindRValueWithoutTargetType(child, diagnostics);

            do
            {
                binary = (BinaryExpressionSyntax)child.Parent;
                BoundExpression right = BindRValueWithoutTargetType(binary.Right, diagnostics);
                left = BindConditionalLogicalOperator(binary, left, right, diagnostics);
                child = binary;
            }
            while ((object)child != node);

            return left;
        }

        private BoundExpression BindConditionalLogicalOperator(BinaryExpressionSyntax node, BoundExpression left, BoundExpression right, BindingDiagnosticBag diagnostics)
        {
            OperatorResolutionForReporting operatorResolutionForReporting = default;
            BoundExpression result = bindConditionalLogicalOperator(node, left, right, ref operatorResolutionForReporting, diagnostics);
            operatorResolutionForReporting.Free();
            return result;

            BoundExpression bindConditionalLogicalOperator(BinaryExpressionSyntax node, BoundExpression left, BoundExpression right, ref OperatorResolutionForReporting operatorResolutionForReporting, BindingDiagnosticBag diagnostics)
            {
                BinaryOperatorKind kind = SyntaxKindToBinaryOperatorKind(node.Kind());

                Debug.Assert(kind == BinaryOperatorKind.LogicalAnd || kind == BinaryOperatorKind.LogicalOr);

                // Let's take an easy out here. The vast majority of the time the operands will
                // both be bool. This is the only situation in which the expression can be a
                // constant expression, so do the folding now if we can.

                if ((object)left.Type != null && left.Type.SpecialType == SpecialType.System_Boolean &&
                    (object)right.Type != null && right.Type.SpecialType == SpecialType.System_Boolean)
                {
                    var constantValue = FoldBinaryOperator(node, kind | BinaryOperatorKind.Bool, left, right, left.Type, diagnostics);

                    // NOTE: no candidate user-defined operators.
                    return new BoundBinaryOperator(node, kind | BinaryOperatorKind.Bool, constantValue, methodOpt: null, constrainedToTypeOpt: null,
                        resultKind: LookupResultKind.Viable, left, right, type: left.Type, hasErrors: constantValue != null && constantValue.IsBad);
                }

                // If either operand is bad, don't try to do binary operator overload resolution; that will just
                // make cascading errors.

                if (left.HasAnyErrors || right.HasAnyErrors)
                {
                    // NOTE: no candidate user-defined operators.
                    return new BoundBinaryOperator(node, kind, ConstantValue.NotAvailable, methodOpt: null, constrainedToTypeOpt: null,
                        resultKind: LookupResultKind.Empty, left, right, type: GetBinaryOperatorErrorType(kind, diagnostics, node), hasErrors: true);
                }

                if (left.HasDynamicType() || right.HasDynamicType())
                {
                    left = BindToNaturalType(left, diagnostics);
                    right = BindToNaturalType(right, diagnostics);
                    return BindDynamicBinaryOperator(node, kind, left, right, diagnostics);
                }

                LookupResultKind lookupResult;
                ImmutableArray<MethodSymbol> originalUserDefinedOperators;
                BinaryOperatorAnalysisResult best;

                if (!IsTypelessExpressionAllowedInBinaryOperator(kind, left, right))
                {
                    lookupResult = LookupResultKind.OverloadResolutionFailure;
                    originalUserDefinedOperators = default(ImmutableArray<MethodSymbol>);
                    best = default(BinaryOperatorAnalysisResult);
                }
                else
                {
                    best = this.BinaryOperatorOverloadResolution(kind, isChecked: CheckOverflowAtRuntime, left, right, node, diagnostics, ref operatorResolutionForReporting, out lookupResult, out originalUserDefinedOperators);
                }

                // SPEC: If overload resolution fails to find a single best operator, or if overload
                // SPEC: resolution selects one of the predefined integer logical operators, a binding-
                // SPEC: time error occurs.
                //
                // SPEC OMISSION: We should probably clarify that the enum logical operators count as
                // SPEC OMISSION: integer logical operators. Basically the rule here should actually be:
                // SPEC OMISSION: if overload resolution selects something other than a user-defined
                // SPEC OMISSION: operator or the built in not-lifted operator on bool, an error occurs.
                //

                if (!best.HasValue)
                {
                    ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, lookupResult, ref operatorResolutionForReporting);
                }
                else
                {
                    // There are two non-error possibilities. Either both operands are implicitly convertible to
                    // bool, or we've got a valid user-defined operator.
                    BinaryOperatorSignature signature = best.Signature;

                    if (signature.Method is { } bestMethod)
                    {
                        ReportObsoleteAndFeatureAvailabilityDiagnostics(bestMethod, node, diagnostics);
                        ReportUseSite(bestMethod, diagnostics, node);
                    }

                    bool bothBool = signature.LeftType.SpecialType == SpecialType.System_Boolean &&
                            signature.RightType.SpecialType == SpecialType.System_Boolean;

                    UnaryOperatorAnalysisResult? trueOperator = null, falseOperator = null;

                    if (!bothBool && !signature.Kind.IsUserDefined())
                    {
                        ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, lookupResult, ref operatorResolutionForReporting);
                    }
                    else if (bothBool || IsValidUserDefinedConditionalLogicalOperator(node, signature, diagnostics, out trueOperator, out falseOperator))
                    {
                        var resultLeft = CreateConversion(left, best.LeftConversion, signature.LeftType, diagnostics);
                        var resultRight = CreateConversion(right, best.RightConversion, signature.RightType, diagnostics);
                        var resultKind = kind | signature.Kind.OperandTypes();
                        if (signature.Kind.IsLifted())
                        {
                            resultKind |= BinaryOperatorKind.Lifted;
                        }

                        if (resultKind.IsUserDefined())
                        {
                            Debug.Assert(trueOperator is { HasValue: true });
                            Debug.Assert(falseOperator is { HasValue: true });

                            UnaryOperatorAnalysisResult trueFalseOperator = (kind == BinaryOperatorKind.LogicalAnd ? falseOperator : trueOperator).GetValueOrDefault();

                            _ = CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, signature.Method, isUnsignedRightShift: false, signature.ConstrainedToTypeOpt, diagnostics) &&
                                CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, trueFalseOperator.Signature.Method,
                                    isUnsignedRightShift: false, signature.ConstrainedToTypeOpt, diagnostics);

                            Debug.Assert(resultLeft.Type.Equals(signature.LeftType, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                            var operandPlaceholder = new BoundValuePlaceholder(resultLeft.Syntax, resultLeft.Type).MakeCompilerGenerated();

                            BoundExpression operandConversion = CreateConversion(
                                resultLeft.Syntax,
                                operandPlaceholder,
                                trueFalseOperator.Conversion,
                                isCast: false,
                                conversionGroupOpt: null,
                                trueFalseOperator.Signature.OperandType,
                                diagnostics);

                            return new BoundUserDefinedConditionalLogicalOperator(
                                node,
                                resultKind,
                                signature.Method,
                                trueOperator.GetValueOrDefault().Signature.Method,
                                falseOperator.GetValueOrDefault().Signature.Method,
                                operandPlaceholder,
                                operandConversion,
                                signature.ConstrainedToTypeOpt,
                                lookupResult,
                                originalUserDefinedOperators,
                                resultLeft,
                                resultRight,
                                signature.ReturnType);
                        }
                        else
                        {
                            Debug.Assert(bothBool);
                            Debug.Assert(!(signature.Method?.ContainingType?.IsInterface ?? false));

                            return new BoundBinaryOperator(
                                node,
                                resultKind,
                                resultLeft,
                                resultRight,
                                ConstantValue.NotAvailable,
                                signature.Method,
                                signature.ConstrainedToTypeOpt,
                                lookupResult,
                                originalUserDefinedOperators,
                                signature.ReturnType);
                        }
                    }
                }

                // We've already reported the error.
                return new BoundBinaryOperator(node, kind, left, right, ConstantValue.NotAvailable, methodOpt: null, constrainedToTypeOpt: null, lookupResult, originalUserDefinedOperators, CreateErrorType(), true);
            }
        }

        private bool IsValidDynamicCondition(BoundExpression left, bool isNegative, BindingDiagnosticBag diagnostics, out MethodSymbol userDefinedOperator)
        {
            userDefinedOperator = null;

            var type = left.Type;
            if ((object)type == null)
            {
                return false;
            }

            if (type.IsDynamic())
            {
                return true;
            }

            var booleanType = Compilation.GetSpecialType(SpecialType.System_Boolean);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var implicitConversion = Conversions.ClassifyImplicitConversionFromExpression(left, booleanType, ref useSiteInfo);

            if (implicitConversion.Exists)
            {
                if (left.Type is not null)
                {
                    CreateConversion(left.Syntax, new BoundValuePlaceholder(left.Syntax, left.Type).MakeCompilerGenerated(), implicitConversion, isCast: false, conversionGroupOpt: null, booleanType, diagnostics);
                }
                else
                {
                    Debug.Assert(left.IsLiteralNull());
                }

                diagnostics.Add(left.Syntax, useSiteInfo);
                return true;
            }

            if (type is not NamedTypeSymbol { IsInterface: false } namedType || namedType.IsNullableType())
            {
                diagnostics.Add(left.Syntax, useSiteInfo);
                return false;
            }

            // Strictly speaking, we probably should be digging through nullable type here to look for an operator
            // declared by the underlying type. However, it doesn't look like dynamic binder is able to deal with
            // nullable types while evaluating logical binary operators. Exceptions that it throws look like:
            //
            // Microsoft.CSharp.RuntimeBinder.RuntimeBinderException : The type ('S1?') must contain declarations of operator true and operator false
            // Stack Trace:
            //     at CallSite.Target(Closure, CallSite, Object, Object)
            //     at System.Dynamic.UpdateDelegates.UpdateAndExecute2[T0,T1,TRet](CallSite site, T0 arg0, T1 arg1)
            //
            // Microsoft.CSharp.RuntimeBinder.RuntimeBinderException : Operator '&&' cannot be applied to operands of type 'S1' and 'S1?'
            // Stack Trace:
            //     at CallSite.Target(Closure, CallSite, Object, Nullable`1)
            //     at System.Dynamic.UpdateDelegates.UpdateAndExecute2[T0,T1,TRet](CallSite site, T0 arg0, T1 arg1)
            var operandPlaceholder = new BoundValuePlaceholder(left.Syntax, namedType).MakeCompilerGenerated();
            UnaryOperatorAnalysisResult result = operatorOverloadResolution(left.Syntax, operandPlaceholder, isNegative ? UnaryOperatorKind.False : UnaryOperatorKind.True, diagnostics);

            if (result.HasValue)
            {
                Debug.Assert(result.Conversion.IsImplicit);
                userDefinedOperator = result.Signature.Method;

                TypeSymbol parameterType = userDefinedOperator.Parameters[0].Type;
                CreateConversion(left.Syntax, operandPlaceholder, result.Conversion, isCast: false, conversionGroupOpt: null, parameterType, diagnostics);
                return true;
            }

            return false;

            UnaryOperatorAnalysisResult operatorOverloadResolution(SyntaxNode node, BoundExpression operand, UnaryOperatorKind kind, BindingDiagnosticBag diagnostics)
            {
                OverloadResolution.GetStaticUserDefinedUnaryOperatorMethodNames(kind, isChecked: false, out string staticOperatorName1, out string staticOperatorName2Opt);

                OperatorResolutionForReporting operatorResolutionForReporting = default;
                var result = this.UnaryOperatorNonExtensionOverloadResolution(
                    kind, isChecked: false, staticOperatorName1, staticOperatorName2Opt,
                    operand: operand, node, diagnostics, ref operatorResolutionForReporting, resultKind: out _, originalUserDefinedOperators: out _);
                operatorResolutionForReporting.Free();

                return result;
            }
        }

        private bool IsValidUserDefinedConditionalLogicalOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorSignature signature,
            BindingDiagnosticBag diagnostics,
            out UnaryOperatorAnalysisResult? trueOperator,
            out UnaryOperatorAnalysisResult? falseOperator)
        {
            Debug.Assert(signature.Kind.OperandTypes() == BinaryOperatorKind.UserDefined);
            Debug.Assert(signature.Method is not null);

            bool result;
            if (signature.Method.IsExtensionBlockMember())
            {
                result = isValidExtensionUserDefinedConditionalLogicalOperator(syntax, signature, diagnostics, out trueOperator, out falseOperator);
            }
            else
            {
                result = isValidNonExtensionUserDefinedConditionalLogicalOperator(syntax, signature, diagnostics, out trueOperator, out falseOperator);
            }

#if DEBUG
            if (result)
            {
                Debug.Assert(trueOperator is { HasValue: true });
                Debug.Assert(falseOperator is { HasValue: true });
                Debug.Assert(!trueOperator.GetValueOrDefault().Signature.Kind.IsLifted());
                Debug.Assert(!falseOperator.GetValueOrDefault().Signature.Kind.IsLifted());
            }
            else
            {
                Debug.Assert(trueOperator is null);
                Debug.Assert(falseOperator is null);
            }
#endif

            return result;

            bool isValidNonExtensionUserDefinedConditionalLogicalOperator(
                CSharpSyntaxNode syntax,
                BinaryOperatorSignature signature,
                BindingDiagnosticBag diagnostics,
                out UnaryOperatorAnalysisResult? trueOperator,
                out UnaryOperatorAnalysisResult? falseOperator)
            {
                // SPEC: When the operands of && or || are of types that declare an applicable
                // SPEC: user-defined operator & or |, both of the following must be true, where
                // SPEC: T is the type in which the selected operator is defined:

                // SPEC VIOLATION:
                //
                // The native compiler violates the specification, the native compiler allows:
                //
                // public static D? operator &(D? d1, D? d2) { ... }
                // public static bool operator true(D? d) { ... }
                // public static bool operator false(D? d) { ... }
                //
                // to be used as D? && D? or D? || D?. But if you do this:
                //
                // public static D operator &(D d1, D d2) { ... }
                // public static bool operator true(D? d) { ... }
                // public static bool operator false(D? d) { ... }
                //
                // And use the *lifted* form of the operator, this is disallowed.
                //
                // public static D? operator &(D? d1, D d2) { ... }
                // public static bool operator true(D? d) { ... }
                // public static bool operator false(D? d) { ... }
                //
                // Is not allowed because "the return type must be the same as the type of both operands"
                // which is not at all what the spec says.
                //
                // We ought not to break backwards compatibility with the native compiler. The spec
                // is plausibly in error; it is possible that this section of the specification was
                // never updated when nullable types and lifted operators were added to the language.
                // And it seems like the native compiler's behavior of allowing a nullable
                // version but not a lifted version is a bug that should be fixed.
                //
                // Therefore we will do the following in Roslyn:
                //
                // * The return and parameter types of the chosen operator, whether lifted or unlifted,
                //   must be the same.
                // * The return and parameter types must be either the enclosing type, or its corresponding
                //   nullable type.
                // * There must be an operator true/operator false that takes the left hand type of the operator.

                // Only classes and structs contain user-defined operators, so we know it is a named type symbol.
                NamedTypeSymbol t = (NamedTypeSymbol)signature.Method.ContainingType;

                // SPEC: The return type and the type of each parameter of the selected operator
                // SPEC: must be T.

                // As mentioned above, we relax this restriction. The types must all be the same.

                bool typesAreSame = TypeSymbol.Equals(signature.LeftType, signature.RightType, TypeCompareKind.ConsiderEverything2) && TypeSymbol.Equals(signature.LeftType, signature.ReturnType, TypeCompareKind.ConsiderEverything2);
                MethodSymbol definition;
                bool typeMatchesContainer = TypeSymbol.Equals(signature.ReturnType.StrippedType(), t, TypeCompareKind.ConsiderEverything2) ||
                                            (t.IsInterface && (signature.Method.IsAbstract || signature.Method.IsVirtual) &&
                                             SourceUserDefinedOperatorSymbol.IsSelfConstrainedTypeParameter((definition = signature.Method.OriginalDefinition).ReturnType.StrippedType(), definition.ContainingType));

                if (!typesAreSame || !typeMatchesContainer)
                {
                    // CS0217: In order to be applicable as a short circuit operator a user-defined logical
                    // operator ('{0}') must have the same return type and parameter types

                    Error(diagnostics, ErrorCode.ERR_BadBoolOp, syntax, signature.Method);

                    trueOperator = null;
                    falseOperator = null;
                    return false;
                }

                // SPEC: T must contain declarations of operator true and operator false.

                // As mentioned above, we need more than just op true and op false existing; we need
                // to know that the first operand can be passed to it.

                var leftPlaceholder = new BoundValuePlaceholder(syntax, signature.LeftType).MakeCompilerGenerated();
                var result = UnaryOperatorOverloadResolutionResult.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                UnaryOperatorAnalysisResult? bestTrue = nonExtensionUnaryOperatorOverloadResolution(syntax, t, result, UnaryOperatorKind.True, leftPlaceholder, ref useSiteInfo);
                UnaryOperatorAnalysisResult? bestFalse = null;

                if (bestTrue?.HasValue == true)
                {
                    result.Results.Clear();
                    bestFalse = nonExtensionUnaryOperatorOverloadResolution(syntax, t, result, UnaryOperatorKind.False, leftPlaceholder, ref useSiteInfo);
                }

                result.Free();
                diagnostics.Add(syntax, useSiteInfo);

                if (bestTrue?.HasValue != true || bestFalse?.HasValue != true)
                {
                    // I have changed the wording of this error message. The original wording was:

                    // CS0218: The type ('T') must contain declarations of operator true and operator false

                    // I have changed that to:

                    // CS0218: In order to be applicable as a short circuit operator, the declaring type
                    // '{1}' of user-defined operator '{0}' must declare operator true and operator false.

                    Error(diagnostics, ErrorCode.ERR_MustHaveOpTF, syntax, signature.Method, t);

                    trueOperator = null;
                    falseOperator = null;
                    return false;
                }

                trueOperator = bestTrue;
                falseOperator = bestFalse;

                // For the remainder of this method the comments WOLOG assume that we're analyzing an &&. The
                // exact same issues apply to ||.

                // Note that the mere *existence* of operator true and operator false is sufficient.  They
                // are already constrained to take either T or T?. Since we know that the applicable
                // T.& takes (T, T), we know that both sides of the && are implicitly convertible
                // to T, and therefore the left side is implicitly convertible to T or T?.

                // SPEC: The expression x && y is evaluated as T.false(x) ? x : T.&(x,y) ... except that
                // SPEC: x is only evaluated once.
                //
                // DELIBERATE SPEC VIOLATION: The native compiler does not actually evaluate x&&y in this
                // manner. Suppose X is of type X. The code above is equivalent to:
                //
                // X temp = x, then evaluate:
                // T.false(temp) ? temp : T.&(temp, y)
                //
                // What the native compiler actually evaluates is:
                //
                // T temp = x, then evaluate
                // T.false(temp) ? temp : T.&(temp, y)
                //
                // That is a small difference but it has an observable effect. For example:
                //
                // class V { public static implicit operator T(V v) { ... } }
                // class X : V { public static implicit operator T?(X x) { ... } }
                // struct T {
                //   public static operator false(T? t) { ... }
                //   public static operator true(T? t) { ... }
                //   public static T operator &(T t1, T t2) { ... }
                // }
                //
                // Under the spec'd interpretation, if we had x of type X and y of type T then x && y is
                //
                // X temp = x;
                // T.false(temp) ? temp : T.&(temp, y)
                //
                // which would then be analyzed as:
                //
                // T.false(X.op_Implicit_To_Nullable_T(temp)) ?
                //     V.op_Implicit_To_T(temp) :
                //     T.&(op_Implicit_To_T(temp), y)
                //
                // But the native compiler actually generates:
                //
                // T temp = V.Op_Implicit_To_T(x);
                // T.false(new T?(temp)) ? temp : T.&(temp, y)
                //
                // That is, the native compiler converts the temporary to the type of the declaring operator type
                // regardless of the fact that there is a better conversion for the T.false call.
                //
                // We choose to match the native compiler behavior here; we might consider fixing
                // the spec to match the compiler.
                //
                // With this decision we need not keep track of any extra information in the bound
                // binary operator node; we need to know the left hand side converted to T, the right
                // hand side converted to T, and the method symbol of the chosen T.&(T, T) method.
                // The rewriting pass has enough information to deduce which T.false is to be called,
                // and can convert the T to T? if necessary.

                return true;
            }

#nullable enable

            UnaryOperatorAnalysisResult? nonExtensionUnaryOperatorOverloadResolution(
                CSharpSyntaxNode syntax,
                NamedTypeSymbol declaringType,
                UnaryOperatorOverloadResolutionResult result,
                UnaryOperatorKind kind,
                BoundValuePlaceholder leftPlaceholder, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                Debug.Assert(result.Results.IsEmpty);

                UnaryOperatorAnalysisResult? possiblyBest = null;

                if (this.OverloadResolution.GetUserDefinedOperators(
                    declaringType, kind, isChecked: false,
                    OperatorFacts.UnaryOperatorNameFromOperatorKind(kind, isChecked: false),
                    name2Opt: null, leftPlaceholder, result.Results, ref useSiteInfo))
                {
                    this.OverloadResolution.UnaryOperatorOverloadResolution(leftPlaceholder, result, ref useSiteInfo);
                    possiblyBest = AnalyzeUnaryOperatorOverloadResolutionResult(result, kind, leftPlaceholder, syntax, diagnostics: BindingDiagnosticBag.Discarded, resultKind: out _, originalUserDefinedOperators: out _);
                }

                return possiblyBest;
            }

            bool isValidExtensionUserDefinedConditionalLogicalOperator(
                CSharpSyntaxNode syntax,
                BinaryOperatorSignature signature,
                BindingDiagnosticBag diagnostics,
                out UnaryOperatorAnalysisResult? trueOperator,
                out UnaryOperatorAnalysisResult? falseOperator)
            {
                // SPEC: The return type and the type of each parameter of the selected operator
                // SPEC: must be T.

                if (!TypeSymbol.Equals(signature.LeftType, signature.RightType, TypeCompareKind.AllIgnoreOptions) ||
                    !TypeSymbol.Equals(signature.LeftType, signature.ReturnType, TypeCompareKind.AllIgnoreOptions))
                {
                    // Note, isValidNonExtensionUserDefinedConditionalLogicalOperator also performs a check that the signature type
                    // matches the declaring type, but that is actually enforced at the point of declaration. We also don't have a 
                    // single test that observes the effect of that check in isValidNonExtensionUserDefinedConditionalLogicalOperator.
                    // This is somewhat expected, because in order to observe the effect, one should consume a type with operators
                    // that do not follow C# rules for their signature.
                    // It is probably not worth guarding against a situation like that, we are not doing this for regular binary operators,
                    // for example.

                    // CS0217: In order to be applicable as a short circuit operator a user-defined logical
                    // operator ('{0}') must have the same return type and parameter types

                    Error(diagnostics, ErrorCode.ERR_BadBoolOp, syntax, signature.Method);

                    trueOperator = null;
                    falseOperator = null;
                    return false;
                }

                // SPEC: T must contain declarations of operator true and operator false.

                var leftPlaceholder = new BoundValuePlaceholder(syntax, signature.LeftType).MakeCompilerGenerated();
                var result = UnaryOperatorOverloadResolutionResult.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var extensionCandidates = ArrayBuilder<Symbol>.GetInstance();
                NamedTypeSymbol extensionContainingType = signature.Method.OriginalDefinition.ContainingType.ContainingType;

                UnaryOperatorAnalysisResult? bestTrue = extensionUnaryOperatorOverloadResolution(syntax, extensionCandidates, result, extensionContainingType, UnaryOperatorKind.True, leftPlaceholder, ref useSiteInfo);
                UnaryOperatorAnalysisResult? bestFalse = null;

                if (bestTrue?.HasValue == true)
                {
                    extensionCandidates.Clear();
                    bestFalse = extensionUnaryOperatorOverloadResolution(syntax, extensionCandidates, result, extensionContainingType, UnaryOperatorKind.False, leftPlaceholder, ref useSiteInfo);
                }

                extensionCandidates.Free();
                result.Free();
                diagnostics.Add(syntax, useSiteInfo);

                if (bestTrue?.HasValue != true || bestFalse?.HasValue != true)
                {
                    Error(diagnostics, ErrorCode.ERR_MustHaveOpTF, syntax, signature.Method, extensionContainingType);

                    trueOperator = null;
                    falseOperator = null;
                    return false;
                }

                trueOperator = bestTrue;
                falseOperator = bestFalse;

                return true;
            }

            UnaryOperatorAnalysisResult? extensionUnaryOperatorOverloadResolution(
                CSharpSyntaxNode syntax,
                ArrayBuilder<Symbol> extensionCandidates,
                UnaryOperatorOverloadResolutionResult result,
                NamedTypeSymbol extensionContainingType,
                UnaryOperatorKind kind,
                BoundValuePlaceholder leftPlaceholder, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                UnaryOperatorAnalysisResult? possiblyBest = null;

                string name = OperatorFacts.UnaryOperatorNameFromOperatorKind(kind, isChecked: false);
                extensionContainingType.GetExtensionMembers(extensionCandidates,
                    name, alternativeName: null, arity: 0,
                    LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustNotBeInstance,
                    FieldsBeingBound);

                if (this.OverloadResolution.UnaryOperatorExtensionOverloadResolutionInSingleScope(
                    extensionCandidates,
                    kind,
                    isChecked: false,
                    name,
                    name2Opt: null,
                    leftPlaceholder,
                    result, ref useSiteInfo))
                {
                    possiblyBest = AnalyzeUnaryOperatorOverloadResolutionResult(result, kind, leftPlaceholder, syntax, diagnostics: BindingDiagnosticBag.Discarded, resultKind: out _, originalUserDefinedOperators: out _);
                }

                return possiblyBest;
            }

#nullable disable
        }

        private TypeSymbol GetBinaryOperatorErrorType(BinaryOperatorKind kind, BindingDiagnosticBag diagnostics, CSharpSyntaxNode node)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    return GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
                default:
                    return CreateErrorType();
            }
        }

        private BinaryOperatorAnalysisResult BinaryOperatorOverloadResolution(
            BinaryOperatorKind kind,
            bool isChecked,
            BoundExpression left,
            BoundExpression right,
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            OverloadResolution.GetStaticUserDefinedBinaryOperatorMethodNames(kind, isChecked, out string name1, out string name2Opt);

            return BinaryOperatorOverloadResolution(kind, isChecked, name1, name2Opt, left, right, node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);
        }

        private BinaryOperatorAnalysisResult BinaryOperatorOverloadResolution(
            BinaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression left,
            BoundExpression right,
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            BinaryOperatorAnalysisResult possiblyBest = BinaryOperatorNonExtensionOverloadResolution(kind, isChecked, name1, name2Opt, left, right, node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);

            Debug.Assert(resultKind is LookupResultKind.Viable or LookupResultKind.Ambiguous or LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
            Debug.Assert(possiblyBest.HasValue == (resultKind is LookupResultKind.Viable));
            Debug.Assert(resultKind is not (LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty) || originalUserDefinedOperators.IsEmpty);

            if (!possiblyBest.HasValue && resultKind != LookupResultKind.Ambiguous)
            {
                Debug.Assert(resultKind is LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
                Debug.Assert(originalUserDefinedOperators.IsEmpty);

                LookupResultKind extensionResultKind;
                ImmutableArray<MethodSymbol> extensionOriginalUserDefinedOperators;
                BinaryOperatorAnalysisResult? extensionBest = BinaryOperatorExtensionOverloadResolution(kind, isChecked, name1, name2Opt, left, right, node, diagnostics, ref operatorResolutionForReporting, out extensionResultKind, out extensionOriginalUserDefinedOperators);

                if (extensionBest.HasValue)
                {
                    possiblyBest = extensionBest.GetValueOrDefault();
                    resultKind = extensionResultKind;
                    originalUserDefinedOperators = extensionOriginalUserDefinedOperators;
                }
            }

            return possiblyBest;
        }

#nullable enable

        private BinaryOperatorAnalysisResult? BinaryOperatorExtensionOverloadResolution(
            BinaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression left,
            BoundExpression right,
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            resultKind = LookupResultKind.Empty;
            originalUserDefinedOperators = [];

            if (left.Type is null && right.Type is null)
            {
                return null;
            }

            BinaryOperatorOverloadResolutionResult? result = BinaryOperatorOverloadResolutionResult.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var extensionCandidatesInSingleScope = ArrayBuilder<Symbol>.GetInstance();
            BinaryOperatorAnalysisResult? possiblyBest = null;

            foreach (var scope in new ExtensionScopes(this))
            {
                extensionCandidatesInSingleScope.Clear();
                scope.Binder.GetCandidateExtensionMembersInSingleBinder(extensionCandidatesInSingleScope,
                    name1, name2Opt, arity: 0,
                    LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustNotBeInstance, this);

                if (this.OverloadResolution.BinaryOperatorExtensionOverloadResolutionInSingleScope(extensionCandidatesInSingleScope, kind, isChecked, name1, name2Opt, left, right, result, ref useSiteInfo))
                {
                    possiblyBest = BinaryOperatorAnalyzeOverloadResolutionResult(result, out resultKind, out originalUserDefinedOperators);

                    if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                    {
                        result = null;
                    }

                    break;
                }

                if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                {
                    result = BinaryOperatorOverloadResolutionResult.GetInstance();
                }
            }

            diagnostics.Add(node, useSiteInfo);

            extensionCandidatesInSingleScope.Free();
            result?.Free();
            return possiblyBest;
        }

#nullable disable

        private BinaryOperatorAnalysisResult BinaryOperatorNonExtensionOverloadResolution(
            BinaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression left,
            BoundExpression right,
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            var result = BinaryOperatorOverloadResolutionResult.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.OverloadResolution.BinaryOperatorOverloadResolution(kind, isChecked, name1, name2Opt, left, right, result, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

            var possiblyBest = BinaryOperatorAnalyzeOverloadResolutionResult(result, out resultKind, out originalUserDefinedOperators);
            if (!operatorResolutionForReporting.SaveResult(result, isExtension: false))
            {
                result.Free();
            }

            Debug.Assert(resultKind is LookupResultKind.Viable or LookupResultKind.Ambiguous or LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
            Debug.Assert(possiblyBest.HasValue == (resultKind is LookupResultKind.Viable));
            Debug.Assert(resultKind is not (LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty) || originalUserDefinedOperators.IsEmpty);

            return possiblyBest;
        }

        private static BinaryOperatorAnalysisResult BinaryOperatorAnalyzeOverloadResolutionResult(BinaryOperatorOverloadResolutionResult result, out LookupResultKind resultKind, out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            var possiblyBest = result.Best;

            if (result.Results.Any())
            {
                var builder = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var analysisResult in result.Results)
                {
                    MethodSymbol method = analysisResult.Signature.Method;
                    if ((object)method != null)
                    {
                        builder.Add(method);
                    }
                }
                originalUserDefinedOperators = builder.ToImmutableAndFree();

                if (possiblyBest.HasValue)
                {
                    resultKind = LookupResultKind.Viable;
                }
                else if (result.AnyValid())
                {
                    resultKind = LookupResultKind.Ambiguous;
                }
                else
                {
                    resultKind = LookupResultKind.OverloadResolutionFailure;
                }
            }
            else
            {
                originalUserDefinedOperators = ImmutableArray<MethodSymbol>.Empty;
                resultKind = possiblyBest.HasValue ? LookupResultKind.Viable : LookupResultKind.Empty;
            }

            return possiblyBest;
        }

        private void ReportObsoleteAndFeatureAvailabilityDiagnostics(MethodSymbol operatorMethod, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            if ((object)operatorMethod != null)
            {
                ReportDiagnosticsIfObsolete(diagnostics, operatorMethod, node, hasBaseReceiver: false);

                if (operatorMethod.ContainingType.IsInterface &&
                    operatorMethod.ContainingModule != Compilation.SourceModule)
                {
                    Binder.CheckFeatureAvailability(node, MessageID.IDS_DefaultInterfaceImplementation, diagnostics);
                }
            }
        }

        private bool IsTypelessExpressionAllowedInBinaryOperator(BinaryOperatorKind kind, BoundExpression left, BoundExpression right)
        {
            // The default literal is only allowed with equality operators and both operands cannot be typeless at the same time.
            // Note: we only need to restrict expressions that can be converted to *any* type, in which case the resolution could always succeed.

            if (left.IsImplicitObjectCreation() ||
                right.IsImplicitObjectCreation())
            {
                return false;
            }

            bool isEquality = kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual;
            if (isEquality)
            {
                return !left.IsLiteralDefault() || !right.IsLiteralDefault();
            }
            else
            {
                return !left.IsLiteralDefault() && !right.IsLiteralDefault();
            }
        }

        private UnaryOperatorAnalysisResult UnaryOperatorOverloadResolution(
            UnaryOperatorKind kind,
            BoundExpression operand,
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            bool isChecked = CheckOverflowAtRuntime;
            OverloadResolution.GetStaticUserDefinedUnaryOperatorMethodNames(kind, isChecked, out string name1, out string name2Opt);

            var best = UnaryOperatorNonExtensionOverloadResolution(kind, isChecked, name1, name2Opt, operand, node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);

            Debug.Assert(resultKind is LookupResultKind.Viable or LookupResultKind.Ambiguous or LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
            Debug.Assert(best.HasValue == (resultKind is LookupResultKind.Viable));
            Debug.Assert(resultKind is not (LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty) || originalUserDefinedOperators.IsEmpty);

            if (!best.HasValue && resultKind != LookupResultKind.Ambiguous)
            {
                Debug.Assert(resultKind is LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
                Debug.Assert(originalUserDefinedOperators.IsEmpty);

                LookupResultKind extensionResultKind;
                ImmutableArray<MethodSymbol> extensionOriginalUserDefinedOperators;
                UnaryOperatorAnalysisResult? extensionBest = this.UnaryOperatorExtensionOverloadResolution(kind, isChecked, name1, name2Opt, operand, node, diagnostics,
                    ref operatorResolutionForReporting, out extensionResultKind, out extensionOriginalUserDefinedOperators);

                if (extensionBest.HasValue)
                {
                    best = extensionBest.GetValueOrDefault();
                    resultKind = extensionResultKind;
                    originalUserDefinedOperators = extensionOriginalUserDefinedOperators;
                }
            }

            return best;
        }

        private UnaryOperatorAnalysisResult UnaryOperatorNonExtensionOverloadResolution(
            UnaryOperatorKind kind,
            bool isChecked,
            string name1,
            string name2Opt,
            BoundExpression operand,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            var result = UnaryOperatorOverloadResolutionResult.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.OverloadResolution.UnaryOperatorOverloadResolution(kind, isChecked, name1, name2Opt, operand, result, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

            UnaryOperatorAnalysisResult possiblyBest = AnalyzeUnaryOperatorOverloadResolutionResult(result, kind, operand, node, diagnostics, out resultKind, out originalUserDefinedOperators);

            if (!operatorResolutionForReporting.SaveResult(result, isExtension: false))
            {
                result.Free();
            }

            Debug.Assert(resultKind is LookupResultKind.Viable or LookupResultKind.Ambiguous or LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
            Debug.Assert(possiblyBest.HasValue == (resultKind is LookupResultKind.Viable));
            Debug.Assert(resultKind is not (LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty) || originalUserDefinedOperators.IsEmpty);

            return possiblyBest;
        }

        UnaryOperatorAnalysisResult AnalyzeUnaryOperatorOverloadResolutionResult(
            UnaryOperatorOverloadResolutionResult result,
            UnaryOperatorKind kind,
            BoundExpression operand,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            var possiblyBest = result.Best;

            if (result.Results.Any())
            {
                var builder = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var analysisResult in result.Results)
                {
                    MethodSymbol method = analysisResult.Signature.Method;
                    if ((object)method != null)
                    {
                        builder.Add(method);
                    }
                }
                originalUserDefinedOperators = builder.ToImmutableAndFree();

                if (possiblyBest.HasValue)
                {
                    resultKind = LookupResultKind.Viable;
                }
                else if (result.AnyValid())
                {
                    // Special case: If we have the unary minus operator applied to a ulong, technically that should be
                    // an ambiguity. The ulong could be implicitly converted to float, double or decimal, and then
                    // the unary minus operator could be applied to the result. But though float is better than double,
                    // float is neither better nor worse than decimal. However it seems odd to give an ambiguity error
                    // when trying to do something such as applying a unary minus operator to an unsigned long.
                    // The same issue applies to unary minus applied to nuint.

                    if (kind == UnaryOperatorKind.UnaryMinus &&
                        (object)operand.Type != null &&
                        (operand.Type.SpecialType == SpecialType.System_UInt64 || isNuint(operand.Type)))
                    {
                        resultKind = LookupResultKind.OverloadResolutionFailure;
                    }
                    else
                    {
                        resultKind = LookupResultKind.Ambiguous;
                    }
                }
                else
                {
                    resultKind = LookupResultKind.OverloadResolutionFailure;
                }
            }
            else
            {
                originalUserDefinedOperators = ImmutableArray<MethodSymbol>.Empty;
                resultKind = possiblyBest.HasValue ? LookupResultKind.Viable : LookupResultKind.Empty;
            }

            if (possiblyBest is { HasValue: true, Signature: { Method: { } bestMethod } })
            {
                ReportObsoleteAndFeatureAvailabilityDiagnostics(bestMethod, node, diagnostics);
                ReportUseSite(bestMethod, diagnostics, node);
            }

            return possiblyBest;

            static bool isNuint(TypeSymbol type)
            {
                return type.SpecialType == SpecialType.System_UIntPtr
                    && type.IsNativeIntegerType;
            }
        }

#nullable enable

        private UnaryOperatorAnalysisResult? UnaryOperatorExtensionOverloadResolution(
            UnaryOperatorKind kind,
            bool isChecked,
            string name1,
            string? name2Opt,
            BoundExpression operand,
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            ref OperatorResolutionForReporting operatorResolutionForReporting,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            resultKind = LookupResultKind.Empty;
            originalUserDefinedOperators = [];

            if (operand.IsLiteralDefault() || // Reported not being able to target-type `default` elsewhere, so we can avoid doing more work
                operand.Type is null) // GetUserDefinedOperators performs this check too, let's optimize early
            {
                return null;
            }

            UnaryOperatorOverloadResolutionResult? result = UnaryOperatorOverloadResolutionResult.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var extensionCandidatesInSingleScope = ArrayBuilder<Symbol>.GetInstance();
            UnaryOperatorAnalysisResult? possiblyBest = null;

            foreach (var scope in new ExtensionScopes(this))
            {
                extensionCandidatesInSingleScope.Clear();
                scope.Binder.GetCandidateExtensionMembersInSingleBinder(extensionCandidatesInSingleScope,
                    name1, name2Opt, arity: 0,
                    LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustNotBeInstance, this);

                if (this.OverloadResolution.UnaryOperatorExtensionOverloadResolutionInSingleScope(extensionCandidatesInSingleScope, kind, isChecked, name1, name2Opt, operand, result, ref useSiteInfo))
                {
                    possiblyBest = AnalyzeUnaryOperatorOverloadResolutionResult(result, kind, operand, node, diagnostics, out resultKind, out originalUserDefinedOperators);

                    if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                    {
                        result = null;
                    }

                    break;
                }

                if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                {
                    result = UnaryOperatorOverloadResolutionResult.GetInstance();
                }
            }

            diagnostics.Add(node, useSiteInfo);

            extensionCandidatesInSingleScope.Free();
            result?.Free();
            return possiblyBest;
        }

#nullable disable

        private static object FoldDecimalBinaryOperators(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            // Roslyn uses Decimal.operator+, operator-, etc. for both constant expressions and
            // non-constant expressions. Dev11 uses Decimal.operator+ etc. for non-constant
            // expressions only. This leads to different results between the two compilers
            // for certain constant expressions involving +/-0. (See bug #529730.) For instance,
            // +0 + -0 == +0 in Roslyn and == -0 in Dev11. Similarly, -0 - -0 == -0 in Roslyn, +0 in Dev11.
            // This is a breaking change from the native compiler but seems acceptable since
            // constant and non-constant expressions behave consistently in Roslyn.
            // (In Dev11, (+0 + -0) != (x + y) when x = +0, y = -0.)

            switch (kind)
            {
                case BinaryOperatorKind.DecimalAddition:
                    return valueLeft.DecimalValue + valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalSubtraction:
                    return valueLeft.DecimalValue - valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalMultiplication:
                    return valueLeft.DecimalValue * valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalDivision:
                    return valueLeft.DecimalValue / valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalRemainder:
                    return valueLeft.DecimalValue % valueRight.DecimalValue;
            }

            return null;
        }

        private static object FoldNativeIntegerOverflowingBinaryOperator(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            checked
            {
                switch (kind)
                {
                    case BinaryOperatorKind.NIntAddition:
                        return valueLeft.Int32Value + valueRight.Int32Value;
                    case BinaryOperatorKind.NUIntAddition:
                        return valueLeft.UInt32Value + valueRight.UInt32Value;
                    case BinaryOperatorKind.NIntSubtraction:
                        return valueLeft.Int32Value - valueRight.Int32Value;
                    case BinaryOperatorKind.NUIntSubtraction:
                        return valueLeft.UInt32Value - valueRight.UInt32Value;
                    case BinaryOperatorKind.NIntMultiplication:
                        return valueLeft.Int32Value * valueRight.Int32Value;
                    case BinaryOperatorKind.NUIntMultiplication:
                        return valueLeft.UInt32Value * valueRight.UInt32Value;
                    case BinaryOperatorKind.NIntDivision:
                        return valueLeft.Int32Value / valueRight.Int32Value;
                    case BinaryOperatorKind.NIntRemainder:
                        return valueLeft.Int32Value % valueRight.Int32Value;
                    case BinaryOperatorKind.NIntLeftShift:
                        {
                            var int32Value = valueLeft.Int32Value << valueRight.Int32Value;
                            var int64Value = valueLeft.Int64Value << valueRight.Int32Value;
                            return (int32Value == int64Value) ? int32Value : null;
                        }
                    case BinaryOperatorKind.NUIntLeftShift:
                        {
                            var uint32Value = valueLeft.UInt32Value << valueRight.Int32Value;
                            var uint64Value = valueLeft.UInt64Value << valueRight.Int32Value;
                            return (uint32Value == uint64Value) ? uint32Value : null;
                        }
                }

                return null;
            }
        }

        private static object FoldUncheckedIntegralBinaryOperator(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            unchecked
            {
                switch (kind)
                {
                    case BinaryOperatorKind.IntAddition:
                        return valueLeft.Int32Value + valueRight.Int32Value;
                    case BinaryOperatorKind.LongAddition:
                        return valueLeft.Int64Value + valueRight.Int64Value;
                    case BinaryOperatorKind.UIntAddition:
                        return valueLeft.UInt32Value + valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongAddition:
                        return valueLeft.UInt64Value + valueRight.UInt64Value;
                    case BinaryOperatorKind.IntSubtraction:
                        return valueLeft.Int32Value - valueRight.Int32Value;
                    case BinaryOperatorKind.LongSubtraction:
                        return valueLeft.Int64Value - valueRight.Int64Value;
                    case BinaryOperatorKind.UIntSubtraction:
                        return valueLeft.UInt32Value - valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongSubtraction:
                        return valueLeft.UInt64Value - valueRight.UInt64Value;
                    case BinaryOperatorKind.IntMultiplication:
                        return valueLeft.Int32Value * valueRight.Int32Value;
                    case BinaryOperatorKind.LongMultiplication:
                        return valueLeft.Int64Value * valueRight.Int64Value;
                    case BinaryOperatorKind.UIntMultiplication:
                        return valueLeft.UInt32Value * valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongMultiplication:
                        return valueLeft.UInt64Value * valueRight.UInt64Value;

                    // even in unchecked context division may overflow:
                    case BinaryOperatorKind.IntDivision:
                        if (valueLeft.Int32Value == int.MinValue && valueRight.Int32Value == -1)
                        {
                            return int.MinValue;
                        }

                        return valueLeft.Int32Value / valueRight.Int32Value;

                    case BinaryOperatorKind.LongDivision:
                        if (valueLeft.Int64Value == long.MinValue && valueRight.Int64Value == -1)
                        {
                            return long.MinValue;
                        }

                        return valueLeft.Int64Value / valueRight.Int64Value;
                }

                return null;
            }
        }

        private static object FoldCheckedIntegralBinaryOperator(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            checked
            {
                switch (kind)
                {
                    case BinaryOperatorKind.IntAddition:
                        return valueLeft.Int32Value + valueRight.Int32Value;
                    case BinaryOperatorKind.LongAddition:
                        return valueLeft.Int64Value + valueRight.Int64Value;
                    case BinaryOperatorKind.UIntAddition:
                        return valueLeft.UInt32Value + valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongAddition:
                        return valueLeft.UInt64Value + valueRight.UInt64Value;
                    case BinaryOperatorKind.IntSubtraction:
                        return valueLeft.Int32Value - valueRight.Int32Value;
                    case BinaryOperatorKind.LongSubtraction:
                        return valueLeft.Int64Value - valueRight.Int64Value;
                    case BinaryOperatorKind.UIntSubtraction:
                        return valueLeft.UInt32Value - valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongSubtraction:
                        return valueLeft.UInt64Value - valueRight.UInt64Value;
                    case BinaryOperatorKind.IntMultiplication:
                        return valueLeft.Int32Value * valueRight.Int32Value;
                    case BinaryOperatorKind.LongMultiplication:
                        return valueLeft.Int64Value * valueRight.Int64Value;
                    case BinaryOperatorKind.UIntMultiplication:
                        return valueLeft.UInt32Value * valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongMultiplication:
                        return valueLeft.UInt64Value * valueRight.UInt64Value;
                    case BinaryOperatorKind.IntDivision:
                        return valueLeft.Int32Value / valueRight.Int32Value;
                    case BinaryOperatorKind.LongDivision:
                        return valueLeft.Int64Value / valueRight.Int64Value;
                }

                return null;
            }
        }

        internal static TypeSymbol GetEnumType(BinaryOperatorKind kind, BoundExpression left, BoundExpression right)
        {
            switch (kind)
            {
                case BinaryOperatorKind.EnumAndUnderlyingAddition:
                case BinaryOperatorKind.EnumAndUnderlyingSubtraction:
                case BinaryOperatorKind.EnumAnd:
                case BinaryOperatorKind.EnumOr:
                case BinaryOperatorKind.EnumXor:
                case BinaryOperatorKind.EnumEqual:
                case BinaryOperatorKind.EnumGreaterThan:
                case BinaryOperatorKind.EnumGreaterThanOrEqual:
                case BinaryOperatorKind.EnumLessThan:
                case BinaryOperatorKind.EnumLessThanOrEqual:
                case BinaryOperatorKind.EnumNotEqual:
                case BinaryOperatorKind.EnumSubtraction:
                    return left.Type;
                case BinaryOperatorKind.UnderlyingAndEnumAddition:
                case BinaryOperatorKind.UnderlyingAndEnumSubtraction:
                    return right.Type;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static SpecialType GetEnumPromotedType(SpecialType underlyingType)
        {
            switch (underlyingType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    return SpecialType.System_Int32;

                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return underlyingType;

                default:
                    throw ExceptionUtilities.UnexpectedValue(underlyingType);
            }
        }

#nullable enable
        private ConstantValue? FoldEnumBinaryOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol resultTypeSymbol,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(kind.IsEnum());
            Debug.Assert(!kind.IsLifted());

            // A built-in binary operation on constant enum operands is evaluated into an operation on
            // constants of the underlying type U of the enum type E. Comparison operators are lowered as
            // simply computing U<U. All other operators are computed as (E)(U op U) or in the case of
            // E-E, (U)(U-U).

            TypeSymbol enumType = GetEnumType(kind, left, right);
            TypeSymbol underlyingType = enumType.GetEnumUnderlyingType()!;

            BoundExpression newLeftOperand = CreateConversion(left, underlyingType, diagnostics);
            BoundExpression newRightOperand = CreateConversion(right, underlyingType, diagnostics);

            // If the underlying type is byte, sbyte, short, ushort or nullables of those then we'll need
            // to convert it up to int or int? because there are no + - * & | ^ < > <= >= == != operators
            // on byte, sbyte, short or ushort. They all convert to int.

            SpecialType operandSpecialType = GetEnumPromotedType(underlyingType.SpecialType);
            TypeSymbol operandType = (operandSpecialType == underlyingType.SpecialType) ?
                underlyingType :
                GetSpecialType(operandSpecialType, diagnostics, syntax);

            newLeftOperand = CreateConversion(newLeftOperand, operandType, diagnostics);
            newRightOperand = CreateConversion(newRightOperand, operandType, diagnostics);

            BinaryOperatorKind newKind = kind.Operator().WithType(newLeftOperand.Type!.SpecialType);

            switch (newKind.Operator())
            {
                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    resultTypeSymbol = operandType;
                    break;

                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    Debug.Assert(resultTypeSymbol.SpecialType == SpecialType.System_Boolean);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(newKind.Operator());
            }

            var constantValue = FoldBinaryOperator(syntax, newKind, newLeftOperand, newRightOperand, resultTypeSymbol, diagnostics);

            if (resultTypeSymbol.SpecialType != SpecialType.System_Boolean && constantValue != null && !constantValue.IsBad)
            {
                TypeSymbol resultType = kind == BinaryOperatorKind.EnumSubtraction ? underlyingType : enumType;

                // We might need to convert back to the underlying type.
                return FoldConstantNumericConversion(syntax, constantValue, resultType, diagnostics);
            }

            return constantValue;
        }

        // Returns null if the operator can't be evaluated at compile time.
        private ConstantValue? FoldBinaryOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol resultTypeSymbol,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            if (left.HasAnyErrors || right.HasAnyErrors)
            {
                return null;
            }

            // SPEC VIOLATION: see method definition for details
            ConstantValue? nullableEqualityResult = TryFoldingNullableEquality(kind, left, right);
            if (nullableEqualityResult != null)
            {
                return nullableEqualityResult;
            }

            var valueLeft = left.ConstantValueOpt;
            var valueRight = right.ConstantValueOpt;
            if (valueLeft == null || valueRight == null)
            {
                return null;
            }

            if (valueLeft.IsBad || valueRight.IsBad)
            {
                return ConstantValue.Bad;
            }

            if (kind.IsEnum() && !kind.IsLifted())
            {
                return FoldEnumBinaryOperator(syntax, kind, left, right, resultTypeSymbol, diagnostics);
            }

            // Divisions by zero on integral types and decimal always fail even in an unchecked context.
            if (IsDivisionByZero(kind, valueRight))
            {
                Error(diagnostics, ErrorCode.ERR_IntDivByZero, syntax);
                return ConstantValue.Bad;
            }

            object? newValue = null;
            SpecialType resultType = resultTypeSymbol.SpecialType;

            // Certain binary operations never fail; bool & bool, for example. If we are in one of those
            // cases, simply fold the operation and return.
            //
            // Although remainder and division always overflow at runtime with arguments int.MinValue/long.MinValue and -1
            // (regardless of checked context) the constant folding behavior is different.
            // Remainder never overflows at compile time while division does.
            newValue = FoldNeverOverflowBinaryOperators(kind, valueLeft, valueRight);
            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            ConstantValue? concatResult = FoldStringConcatenation(kind, valueLeft, valueRight);
            if (concatResult != null)
            {
                if (concatResult.IsBad)
                {
                    Error(diagnostics, ErrorCode.ERR_ConstantStringTooLong, right.Syntax);
                }

                return concatResult;
            }

            // Certain binary operations always fail if they overflow even when in an unchecked context;
            // decimal + decimal, for example. If we are in one of those cases, make the attempt. If it
            // succeeds, return the result. If not, give a compile-time error regardless of context.
            try
            {
                newValue = FoldDecimalBinaryOperators(kind, valueLeft, valueRight);
            }
            catch (OverflowException)
            {
                Error(diagnostics, ErrorCode.ERR_DecConstError, syntax);
                return ConstantValue.Bad;
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            try
            {
                newValue = FoldNativeIntegerOverflowingBinaryOperator(kind, valueLeft, valueRight);
            }
            catch (OverflowException)
            {
                if (CheckOverflowAtCompileTime)
                {
                    Error(diagnostics, ErrorCode.WRN_CompileTimeCheckedOverflow, syntax, resultTypeSymbol);
                }

                return null;
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            if (CheckOverflowAtCompileTime)
            {
                try
                {
                    newValue = FoldCheckedIntegralBinaryOperator(kind, valueLeft, valueRight);
                }
                catch (OverflowException)
                {
                    Error(diagnostics, ErrorCode.ERR_CheckedOverflow, syntax);
                    return ConstantValue.Bad;
                }
            }
            else
            {
                newValue = FoldUncheckedIntegralBinaryOperator(kind, valueLeft, valueRight);
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            return null;
        }

        /// <summary>
        /// If one of the (unconverted) operands has constant value null and the other has
        /// a null constant value other than null, then they are definitely not equal
        /// and we can give a constant value for either == or !=.  This is a spec violation
        /// that we retain from Dev10.
        /// </summary>
        /// <param name="kind">The operator kind.  Nothing will happen if it is not a lifted equality operator.</param>
        /// <param name="left">The left-hand operand of the operation (possibly wrapped in a conversion).</param>
        /// <param name="right">The right-hand operand of the operation (possibly wrapped in a conversion).</param>
        /// <returns>
        /// If the operator represents lifted equality, then constant value true if both arguments have constant
        /// value null, constant value false if exactly one argument has constant value null, and null otherwise.
        /// If the operator represents lifted inequality, then constant value false if both arguments have constant
        /// value null, constant value true if exactly one argument has constant value null, and null otherwise.
        /// </returns>
        /// <remarks>
        /// SPEC VIOLATION: according to the spec (section 7.19) constant expressions cannot
        /// include implicit nullable conversions or nullable subexpressions.  However, Dev10
        /// specifically folds over lifted == and != (see ExpressionBinder::TryFoldingNullableEquality).
        /// Dev 10 does do compile-time evaluation of simple lifted operators, but it does so
        /// in a rewriting pass (see NullableRewriter) - they are not treated as constant values.
        /// </remarks>
        private static ConstantValue? TryFoldingNullableEquality(BinaryOperatorKind kind, BoundExpression left, BoundExpression right)
        {
            if (kind.IsLifted())
            {
                BinaryOperatorKind op = kind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    if (left.Kind == BoundKind.Conversion && right.Kind == BoundKind.Conversion)
                    {
                        BoundConversion leftConv = (BoundConversion)left;
                        BoundConversion rightConv = (BoundConversion)right;
                        ConstantValue? leftConstant = leftConv.Operand.ConstantValueOpt;
                        ConstantValue? rightConstant = rightConv.Operand.ConstantValueOpt;

                        if (leftConstant != null && rightConstant != null)
                        {
                            bool leftIsNull = leftConstant.IsNull;
                            bool rightIsNull = rightConstant.IsNull;
                            if (leftIsNull || rightIsNull)
                            {
                                // IMPL CHANGE: Dev10 raises WRN_NubExprIsConstBool in some cases, but that really doesn't
                                // make sense (why warn that a constant has a constant value?).
                                return (leftIsNull == rightIsNull) == (op == BinaryOperatorKind.Equal) ? ConstantValue.True : ConstantValue.False;
                            }
                        }
                    }
                }
            }

            return null;
        }

        // Some binary operators on constants never overflow, regardless of whether the context is checked or not.
        private static object? FoldNeverOverflowBinaryOperators(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            // Note that we *cannot* do folding on single-precision floats as doubles to preserve precision,
            // as that would cause incorrect rounding that would be impossible to correct afterwards.
            switch (kind)
            {
                case BinaryOperatorKind.ObjectEqual:
                    if (valueLeft.IsNull) return valueRight.IsNull;
                    if (valueRight.IsNull) return false;
                    break;
                case BinaryOperatorKind.ObjectNotEqual:
                    if (valueLeft.IsNull) return !valueRight.IsNull;
                    if (valueRight.IsNull) return true;
                    break;
                case BinaryOperatorKind.DoubleAddition:
                    return valueLeft.DoubleValue + valueRight.DoubleValue;
                case BinaryOperatorKind.FloatAddition:
                    return valueLeft.SingleValue + valueRight.SingleValue;
                case BinaryOperatorKind.DoubleSubtraction:
                    return valueLeft.DoubleValue - valueRight.DoubleValue;
                case BinaryOperatorKind.FloatSubtraction:
                    return valueLeft.SingleValue - valueRight.SingleValue;
                case BinaryOperatorKind.DoubleMultiplication:
                    return valueLeft.DoubleValue * valueRight.DoubleValue;
                case BinaryOperatorKind.FloatMultiplication:
                    return valueLeft.SingleValue * valueRight.SingleValue;
                case BinaryOperatorKind.DoubleDivision:
                    return valueLeft.DoubleValue / valueRight.DoubleValue;
                case BinaryOperatorKind.FloatDivision:
                    return valueLeft.SingleValue / valueRight.SingleValue;
                case BinaryOperatorKind.DoubleRemainder:
                    return valueLeft.DoubleValue % valueRight.DoubleValue;
                case BinaryOperatorKind.FloatRemainder:
                    return valueLeft.SingleValue % valueRight.SingleValue;
                case BinaryOperatorKind.IntLeftShift:
                    return valueLeft.Int32Value << valueRight.Int32Value;
                case BinaryOperatorKind.LongLeftShift:
                    return valueLeft.Int64Value << valueRight.Int32Value;
                case BinaryOperatorKind.UIntLeftShift:
                    return valueLeft.UInt32Value << valueRight.Int32Value;
                case BinaryOperatorKind.ULongLeftShift:
                    return valueLeft.UInt64Value << valueRight.Int32Value;
                case BinaryOperatorKind.IntRightShift:
                case BinaryOperatorKind.NIntRightShift:
                    return valueLeft.Int32Value >> valueRight.Int32Value;
                case BinaryOperatorKind.IntUnsignedRightShift:
                    return (int)(((uint)valueLeft.Int32Value) >> valueRight.Int32Value); // Switch to `valueLeft.Int32Value >>> valueRight.Int32Value` once >>> becomes available
                case BinaryOperatorKind.NIntUnsignedRightShift:
                    return (valueLeft.Int32Value >= 0) ? valueLeft.Int32Value >> valueRight.Int32Value : null;
                case BinaryOperatorKind.LongRightShift:
                    return valueLeft.Int64Value >> valueRight.Int32Value;
                case BinaryOperatorKind.LongUnsignedRightShift:
                    return (long)(((ulong)valueLeft.Int64Value) >> valueRight.Int32Value); // Switch to `valueLeft.Int64Value >>> valueRight.Int32Value` once >>> becomes available 
                case BinaryOperatorKind.UIntRightShift:
                case BinaryOperatorKind.NUIntRightShift:
                case BinaryOperatorKind.UIntUnsignedRightShift:
                case BinaryOperatorKind.NUIntUnsignedRightShift:
                    return valueLeft.UInt32Value >> valueRight.Int32Value;
                case BinaryOperatorKind.ULongRightShift:
                case BinaryOperatorKind.ULongUnsignedRightShift:
                    return valueLeft.UInt64Value >> valueRight.Int32Value;
                case BinaryOperatorKind.BoolAnd:
                    return valueLeft.BooleanValue & valueRight.BooleanValue;
                case BinaryOperatorKind.IntAnd:
                case BinaryOperatorKind.NIntAnd:
                    return valueLeft.Int32Value & valueRight.Int32Value;
                case BinaryOperatorKind.LongAnd:
                    return valueLeft.Int64Value & valueRight.Int64Value;
                case BinaryOperatorKind.UIntAnd:
                case BinaryOperatorKind.NUIntAnd:
                    return valueLeft.UInt32Value & valueRight.UInt32Value;
                case BinaryOperatorKind.ULongAnd:
                    return valueLeft.UInt64Value & valueRight.UInt64Value;
                case BinaryOperatorKind.BoolOr:
                    return valueLeft.BooleanValue | valueRight.BooleanValue;
                case BinaryOperatorKind.IntOr:
                case BinaryOperatorKind.NIntOr:
                    return valueLeft.Int32Value | valueRight.Int32Value;
                case BinaryOperatorKind.LongOr:
                    return valueLeft.Int64Value | valueRight.Int64Value;
                case BinaryOperatorKind.UIntOr:
                case BinaryOperatorKind.NUIntOr:
                    return valueLeft.UInt32Value | valueRight.UInt32Value;
                case BinaryOperatorKind.ULongOr:
                    return valueLeft.UInt64Value | valueRight.UInt64Value;
                case BinaryOperatorKind.BoolXor:
                    return valueLeft.BooleanValue ^ valueRight.BooleanValue;
                case BinaryOperatorKind.IntXor:
                case BinaryOperatorKind.NIntXor:
                    return valueLeft.Int32Value ^ valueRight.Int32Value;
                case BinaryOperatorKind.LongXor:
                    return valueLeft.Int64Value ^ valueRight.Int64Value;
                case BinaryOperatorKind.UIntXor:
                case BinaryOperatorKind.NUIntXor:
                    return valueLeft.UInt32Value ^ valueRight.UInt32Value;
                case BinaryOperatorKind.ULongXor:
                    return valueLeft.UInt64Value ^ valueRight.UInt64Value;
                case BinaryOperatorKind.LogicalBoolAnd:
                    return valueLeft.BooleanValue && valueRight.BooleanValue;
                case BinaryOperatorKind.LogicalBoolOr:
                    return valueLeft.BooleanValue || valueRight.BooleanValue;
                case BinaryOperatorKind.BoolEqual:
                    return valueLeft.BooleanValue == valueRight.BooleanValue;
                case BinaryOperatorKind.StringEqual:
                    return valueLeft.StringValue == valueRight.StringValue;
                case BinaryOperatorKind.DecimalEqual:
                    return valueLeft.DecimalValue == valueRight.DecimalValue;
                case BinaryOperatorKind.FloatEqual:
                    return valueLeft.SingleValue == valueRight.SingleValue;
                case BinaryOperatorKind.DoubleEqual:
                    return valueLeft.DoubleValue == valueRight.DoubleValue;
                case BinaryOperatorKind.IntEqual:
                case BinaryOperatorKind.NIntEqual:
                    return valueLeft.Int32Value == valueRight.Int32Value;
                case BinaryOperatorKind.LongEqual:
                    return valueLeft.Int64Value == valueRight.Int64Value;
                case BinaryOperatorKind.UIntEqual:
                case BinaryOperatorKind.NUIntEqual:
                    return valueLeft.UInt32Value == valueRight.UInt32Value;
                case BinaryOperatorKind.ULongEqual:
                    return valueLeft.UInt64Value == valueRight.UInt64Value;
                case BinaryOperatorKind.BoolNotEqual:
                    return valueLeft.BooleanValue != valueRight.BooleanValue;
                case BinaryOperatorKind.StringNotEqual:
                    return valueLeft.StringValue != valueRight.StringValue;
                case BinaryOperatorKind.DecimalNotEqual:
                    return valueLeft.DecimalValue != valueRight.DecimalValue;
                case BinaryOperatorKind.FloatNotEqual:
                    return valueLeft.SingleValue != valueRight.SingleValue;
                case BinaryOperatorKind.DoubleNotEqual:
                    return valueLeft.DoubleValue != valueRight.DoubleValue;
                case BinaryOperatorKind.IntNotEqual:
                case BinaryOperatorKind.NIntNotEqual:
                    return valueLeft.Int32Value != valueRight.Int32Value;
                case BinaryOperatorKind.LongNotEqual:
                    return valueLeft.Int64Value != valueRight.Int64Value;
                case BinaryOperatorKind.UIntNotEqual:
                case BinaryOperatorKind.NUIntNotEqual:
                    return valueLeft.UInt32Value != valueRight.UInt32Value;
                case BinaryOperatorKind.ULongNotEqual:
                    return valueLeft.UInt64Value != valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalLessThan:
                    return valueLeft.DecimalValue < valueRight.DecimalValue;
                case BinaryOperatorKind.FloatLessThan:
                    return valueLeft.SingleValue < valueRight.SingleValue;
                case BinaryOperatorKind.DoubleLessThan:
                    return valueLeft.DoubleValue < valueRight.DoubleValue;
                case BinaryOperatorKind.IntLessThan:
                case BinaryOperatorKind.NIntLessThan:
                    return valueLeft.Int32Value < valueRight.Int32Value;
                case BinaryOperatorKind.LongLessThan:
                    return valueLeft.Int64Value < valueRight.Int64Value;
                case BinaryOperatorKind.UIntLessThan:
                case BinaryOperatorKind.NUIntLessThan:
                    return valueLeft.UInt32Value < valueRight.UInt32Value;
                case BinaryOperatorKind.ULongLessThan:
                    return valueLeft.UInt64Value < valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalGreaterThan:
                    return valueLeft.DecimalValue > valueRight.DecimalValue;
                case BinaryOperatorKind.FloatGreaterThan:
                    return valueLeft.SingleValue > valueRight.SingleValue;
                case BinaryOperatorKind.DoubleGreaterThan:
                    return valueLeft.DoubleValue > valueRight.DoubleValue;
                case BinaryOperatorKind.IntGreaterThan:
                case BinaryOperatorKind.NIntGreaterThan:
                    return valueLeft.Int32Value > valueRight.Int32Value;
                case BinaryOperatorKind.LongGreaterThan:
                    return valueLeft.Int64Value > valueRight.Int64Value;
                case BinaryOperatorKind.UIntGreaterThan:
                case BinaryOperatorKind.NUIntGreaterThan:
                    return valueLeft.UInt32Value > valueRight.UInt32Value;
                case BinaryOperatorKind.ULongGreaterThan:
                    return valueLeft.UInt64Value > valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalLessThanOrEqual:
                    return valueLeft.DecimalValue <= valueRight.DecimalValue;
                case BinaryOperatorKind.FloatLessThanOrEqual:
                    return valueLeft.SingleValue <= valueRight.SingleValue;
                case BinaryOperatorKind.DoubleLessThanOrEqual:
                    return valueLeft.DoubleValue <= valueRight.DoubleValue;
                case BinaryOperatorKind.IntLessThanOrEqual:
                case BinaryOperatorKind.NIntLessThanOrEqual:
                    return valueLeft.Int32Value <= valueRight.Int32Value;
                case BinaryOperatorKind.LongLessThanOrEqual:
                    return valueLeft.Int64Value <= valueRight.Int64Value;
                case BinaryOperatorKind.UIntLessThanOrEqual:
                case BinaryOperatorKind.NUIntLessThanOrEqual:
                    return valueLeft.UInt32Value <= valueRight.UInt32Value;
                case BinaryOperatorKind.ULongLessThanOrEqual:
                    return valueLeft.UInt64Value <= valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalGreaterThanOrEqual:
                    return valueLeft.DecimalValue >= valueRight.DecimalValue;
                case BinaryOperatorKind.FloatGreaterThanOrEqual:
                    return valueLeft.SingleValue >= valueRight.SingleValue;
                case BinaryOperatorKind.DoubleGreaterThanOrEqual:
                    return valueLeft.DoubleValue >= valueRight.DoubleValue;
                case BinaryOperatorKind.IntGreaterThanOrEqual:
                case BinaryOperatorKind.NIntGreaterThanOrEqual:
                    return valueLeft.Int32Value >= valueRight.Int32Value;
                case BinaryOperatorKind.LongGreaterThanOrEqual:
                    return valueLeft.Int64Value >= valueRight.Int64Value;
                case BinaryOperatorKind.UIntGreaterThanOrEqual:
                case BinaryOperatorKind.NUIntGreaterThanOrEqual:
                    return valueLeft.UInt32Value >= valueRight.UInt32Value;
                case BinaryOperatorKind.ULongGreaterThanOrEqual:
                    return valueLeft.UInt64Value >= valueRight.UInt64Value;
                case BinaryOperatorKind.UIntDivision:
                case BinaryOperatorKind.NUIntDivision:
                    return valueLeft.UInt32Value / valueRight.UInt32Value;
                case BinaryOperatorKind.ULongDivision:
                    return valueLeft.UInt64Value / valueRight.UInt64Value;

                // MinValue % -1 always overflows at runtime but never at compile time
                case BinaryOperatorKind.IntRemainder:
                    return (valueRight.Int32Value != -1) ? valueLeft.Int32Value % valueRight.Int32Value : 0;
                case BinaryOperatorKind.LongRemainder:
                    return (valueRight.Int64Value != -1) ? valueLeft.Int64Value % valueRight.Int64Value : 0;
                case BinaryOperatorKind.UIntRemainder:
                case BinaryOperatorKind.NUIntRemainder:
                    return valueLeft.UInt32Value % valueRight.UInt32Value;
                case BinaryOperatorKind.ULongRemainder:
                    return valueLeft.UInt64Value % valueRight.UInt64Value;
            }

            return null;
        }

        /// <summary>
        /// Returns ConstantValue.Bad if, and only if, the resulting string length exceeds <see cref="int.MaxValue"/>.
        /// </summary>
        private static ConstantValue? FoldStringConcatenation(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            if (kind == BinaryOperatorKind.StringConcatenation)
            {
                Rope leftValue = valueLeft.RopeValue ?? Rope.Empty;
                Rope rightValue = valueRight.RopeValue ?? Rope.Empty;

                long newLength = (long)leftValue.Length + (long)rightValue.Length;
                return (newLength > int.MaxValue) ? ConstantValue.Bad : ConstantValue.CreateFromRope(Rope.Concat(leftValue, rightValue));
            }

            return null;
        }
#nullable disable

        public static BinaryOperatorKind SyntaxKindToBinaryOperatorKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.MultiplyExpression: return BinaryOperatorKind.Multiplication;
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.DivideExpression: return BinaryOperatorKind.Division;
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.ModuloExpression: return BinaryOperatorKind.Remainder;
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.AddExpression: return BinaryOperatorKind.Addition;
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.SubtractExpression: return BinaryOperatorKind.Subtraction;
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.RightShiftExpression: return BinaryOperatorKind.RightShift;
                case SyntaxKind.UnsignedRightShiftAssignmentExpression:
                case SyntaxKind.UnsignedRightShiftExpression: return BinaryOperatorKind.UnsignedRightShift;
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.LeftShiftExpression: return BinaryOperatorKind.LeftShift;
                case SyntaxKind.EqualsExpression: return BinaryOperatorKind.Equal;
                case SyntaxKind.NotEqualsExpression: return BinaryOperatorKind.NotEqual;
                case SyntaxKind.GreaterThanExpression: return BinaryOperatorKind.GreaterThan;
                case SyntaxKind.LessThanExpression: return BinaryOperatorKind.LessThan;
                case SyntaxKind.GreaterThanOrEqualExpression: return BinaryOperatorKind.GreaterThanOrEqual;
                case SyntaxKind.LessThanOrEqualExpression: return BinaryOperatorKind.LessThanOrEqual;
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.BitwiseAndExpression: return BinaryOperatorKind.And;
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.BitwiseOrExpression: return BinaryOperatorKind.Or;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.ExclusiveOrExpression: return BinaryOperatorKind.Xor;
                case SyntaxKind.LogicalAndExpression: return BinaryOperatorKind.LogicalAnd;
                case SyntaxKind.LogicalOrExpression: return BinaryOperatorKind.LogicalOr;
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

#nullable enable

        private enum InstanceUserDefinedIncrementUsageMode : byte
        {
            None,
            ResultIsNotUsed,
            ResultIsUsed
        }

        private BoundExpression BindIncrementOperator(ExpressionSyntax node, ExpressionSyntax operandSyntax, SyntaxToken operatorToken, BindingDiagnosticBag diagnostics)
        {
            OperatorResolutionForReporting operatorResolutionForReporting = default;
            BoundExpression result = bindIncrementOperator(node, operandSyntax, operatorToken, ref operatorResolutionForReporting, diagnostics);
            operatorResolutionForReporting.Free();
            return result;

            BoundExpression bindIncrementOperator(ExpressionSyntax node, ExpressionSyntax operandSyntax, SyntaxToken operatorToken, ref OperatorResolutionForReporting operatorResolutionForReporting, BindingDiagnosticBag diagnostics)
            {
                operandSyntax.CheckDeconstructionCompatibleArgument(diagnostics);

                BoundExpression operand = BindToNaturalType(BindValue(operandSyntax, diagnostics, BindValueKind.IncrementDecrement), diagnostics);
                UnaryOperatorKind kind = SyntaxKindToUnaryOperatorKind(node.Kind());

                // If the operand is bad, avoid generating cascading errors.
                if (operand.HasAnyErrors)
                {
                    // NOTE: no candidate user-defined operators.
                    return new BoundIncrementOperator(
                        node,
                        kind,
                        operand,
                        methodOpt: null,
                        constrainedToTypeOpt: null,
                        operandPlaceholder: null,
                        operandConversion: null,
                        resultPlaceholder: null,
                        resultConversion: null,
                        LookupResultKind.Empty,
                        CreateErrorType(),
                        hasErrors: true);
                }

                // The operand has to be a variable, property or indexer, so it must have a type.
                var operandType = operand.Type;
                Debug.Assert(operandType is not null);

                if (operandType.IsDynamic())
                {
                    return new BoundIncrementOperator(
                        node,
                        kind.WithType(UnaryOperatorKind.Dynamic).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                        operand,
                        methodOpt: null,
                        constrainedToTypeOpt: null,
                        operandPlaceholder: null,
                        operandConversion: null,
                        resultPlaceholder: null,
                        resultConversion: null,
                        resultKind: LookupResultKind.Viable,
                        originalUserDefinedOperatorsOpt: default(ImmutableArray<MethodSymbol>),
                        type: operandType,
                        hasErrors: false);
                }

                bool isChecked = CheckOverflowAtRuntime;

                // Try an in-place user-defined operator
                InstanceUserDefinedIncrementUsageMode mode = getInstanceUserDefinedIncrementUsageMode(node, kind, isChecked, operand, out string? checkedInstanceOperatorName, out string? ordinaryInstanceOperatorName);

                if (mode != InstanceUserDefinedIncrementUsageMode.None)
                {
                    Debug.Assert(ordinaryInstanceOperatorName is not null);

                    BoundIncrementOperator? inPlaceResult = tryApplyUserDefinedInstanceOperator(node, operatorToken, kind, mode, isChecked, checkedInstanceOperatorName, ordinaryInstanceOperatorName,
                        operand, ref operatorResolutionForReporting, diagnostics);

                    if (inPlaceResult is not null)
                    {
                        return inPlaceResult;
                    }
                }

                OverloadResolution.GetStaticUserDefinedUnaryOperatorMethodNames(kind, isChecked, out string staticOperatorName1, out string? staticOperatorName2Opt);

                LookupResultKind resultKind;
                ImmutableArray<MethodSymbol> originalUserDefinedOperators;
                var best = this.UnaryOperatorNonExtensionOverloadResolution(kind, isChecked, staticOperatorName1, staticOperatorName2Opt, operand, node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);

                Debug.Assert(resultKind is LookupResultKind.Viable or LookupResultKind.Ambiguous or LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
                Debug.Assert(best.HasValue == (resultKind is LookupResultKind.Viable));
                Debug.Assert(resultKind is not (LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty) || originalUserDefinedOperators.IsEmpty);

                if (!best.HasValue && resultKind != LookupResultKind.Ambiguous)
                {
                    Debug.Assert(resultKind is LookupResultKind.OverloadResolutionFailure or LookupResultKind.Empty);
                    Debug.Assert(originalUserDefinedOperators.IsEmpty);

                    // Check for extension operators
                    LookupResultKind staticExtensionResultKind;
                    ImmutableArray<MethodSymbol> staticExtensionOriginalUserDefinedOperators;
                    UnaryOperatorAnalysisResult? staticExtensionBest;
                    BoundIncrementOperator? instanceExtensionResult = tryApplyUserDefinedExtensionOperator(
                        node, kind, mode, isChecked,
                        staticOperatorName1, staticOperatorName2Opt,
                        checkedInstanceOperatorName, ordinaryInstanceOperatorName,
                        operand, diagnostics,
                        ref operatorResolutionForReporting,
                        out staticExtensionBest, out staticExtensionResultKind, out staticExtensionOriginalUserDefinedOperators);

                    if (instanceExtensionResult is not null)
                    {
                        Debug.Assert(instanceExtensionResult.ResultKind is LookupResultKind.Viable || !instanceExtensionResult.OriginalUserDefinedOperatorsOpt.IsDefaultOrEmpty);
                        return instanceExtensionResult;
                    }

                    if (staticExtensionBest.HasValue)
                    {
                        best = staticExtensionBest.GetValueOrDefault();
                        resultKind = staticExtensionResultKind;
                        originalUserDefinedOperators = staticExtensionOriginalUserDefinedOperators;
                    }
                }

                if (!best.HasValue)
                {
                    ReportUnaryOperatorError(node, diagnostics, operatorToken.Text, operand, resultKind, ref operatorResolutionForReporting);
                    return new BoundIncrementOperator(
                        node,
                        kind,
                        operand,
                        methodOpt: null,
                        constrainedToTypeOpt: null,
                        operandPlaceholder: null,
                        operandConversion: null,
                        resultPlaceholder: null,
                        resultConversion: null,
                        resultKind,
                        originalUserDefinedOperators,
                        CreateErrorType(),
                        hasErrors: true);
                }

                var signature = best.Signature;

                CheckNativeIntegerFeatureAvailability(signature.Kind, node, diagnostics);
                CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, signature.Method, isUnsignedRightShift: false, signature.ConstrainedToTypeOpt, diagnostics);

                var resultPlaceholder = new BoundValuePlaceholder(node, signature.ReturnType).MakeCompilerGenerated();

                BoundExpression? resultConversion = GenerateConversionForAssignment(operandType, resultPlaceholder, diagnostics, ConversionForAssignmentFlags.IncrementAssignment);

                bool hasErrors = resultConversion.HasErrors;

                if (resultConversion is not BoundConversion)
                {
                    Debug.Assert(hasErrors || (object)resultConversion == resultPlaceholder);
                    if ((object)resultConversion != resultPlaceholder)
                    {
                        resultPlaceholder = null;
                        resultConversion = null;
                    }
                }

                if (!hasErrors && operandType.IsVoidPointer())
                {
                    Debug.Assert(!signature.Kind.IsUserDefined());
                    Error(diagnostics, ErrorCode.ERR_VoidError, node);
                    hasErrors = true;
                }

                var operandPlaceholder = new BoundValuePlaceholder(operand.Syntax, operandType).MakeCompilerGenerated();
                var operandConversion = CreateConversion(node, operandPlaceholder, best.Conversion, isCast: false, conversionGroupOpt: null, best.Signature.OperandType, diagnostics);

                return new BoundIncrementOperator(
                    node,
                    signature.Kind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                    operand,
                    signature.Method,
                    signature.ConstrainedToTypeOpt,
                    operandPlaceholder,
                    operandConversion,
                    resultPlaceholder,
                    resultConversion,
                    resultKind,
                    originalUserDefinedOperators,
                    operandType,
                    hasErrors);
            }

            InstanceUserDefinedIncrementUsageMode getInstanceUserDefinedIncrementUsageMode(
                ExpressionSyntax node,
                UnaryOperatorKind kind,
                bool checkOverflowAtRuntime,
                BoundExpression operand,
                out string? checkedName,
                out string? ordinaryName)
            {
                var operandType = operand.Type;
                Debug.Assert(operandType is not null);
                Debug.Assert(!operandType.IsDynamic());
                Debug.Assert(kind is (UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PrefixDecrement or UnaryOperatorKind.PostfixIncrement or UnaryOperatorKind.PostfixDecrement));

                checkedName = null;
                ordinaryName = null;

                if (kind is not (UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PrefixDecrement or UnaryOperatorKind.PostfixIncrement or UnaryOperatorKind.PostfixDecrement) ||
                    operandType.SpecialType.IsNumericType() ||
                    !node.IsFeatureEnabled(MessageID.IDS_FeatureUserDefinedCompoundAssignmentOperators))
                {
                    return InstanceUserDefinedIncrementUsageMode.None;
                }

                bool resultIsUsed = ResultIsUsed(node);

                if ((kind is (UnaryOperatorKind.PostfixIncrement or UnaryOperatorKind.PostfixDecrement) && resultIsUsed) ||
                    !CheckValueKind(node, operand, BindValueKind.RefersToLocation | BindValueKind.Assignable, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                {
                    return InstanceUserDefinedIncrementUsageMode.None;
                }

                checkedName = checkOverflowAtRuntime ?
                                     (kind is UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PostfixIncrement ?
                                          WellKnownMemberNames.CheckedIncrementAssignmentOperatorName :
                                          WellKnownMemberNames.CheckedDecrementAssignmentOperatorName) :
                                     null;
                ordinaryName = kind is UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PostfixIncrement ?
                                      WellKnownMemberNames.IncrementAssignmentOperatorName :
                                      WellKnownMemberNames.DecrementAssignmentOperatorName;

                return resultIsUsed ? InstanceUserDefinedIncrementUsageMode.ResultIsUsed : InstanceUserDefinedIncrementUsageMode.ResultIsNotUsed;
            }

            BoundIncrementOperator? tryApplyUserDefinedInstanceOperator(
                ExpressionSyntax node,
                SyntaxToken operatorToken,
                UnaryOperatorKind kind,
                InstanceUserDefinedIncrementUsageMode mode,
                bool isChecked,
                string? checkedName,
                string ordinaryName,
                BoundExpression operand,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(operand.Type is not null);
                Debug.Assert(mode != InstanceUserDefinedIncrementUsageMode.None);

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                ArrayBuilder<MethodSymbol>? methods = LookupUserDefinedInstanceOperators(
                    operand.Type,
                    checkedName: checkedName,
                    ordinaryName: ordinaryName,
                    parameterCount: 0,
                    ref useSiteInfo);

                diagnostics.Add(node, useSiteInfo);

                if (methods?.IsEmpty != false)
                {
                    methods?.Free();
                    return null;
                }

                AnalyzedArguments? analyzedArguments = null;
                BoundIncrementOperator? inPlaceResult = tryInstanceOperatorOverloadResolutionAndFreeMethods(node, operatorToken, kind, mode, isChecked, isExtension: false, operand, ref analyzedArguments, methods, ref operatorResolutionForReporting, diagnostics);
                Debug.Assert(analyzedArguments is not null);
                analyzedArguments.Free();

                return inPlaceResult;
            }

            BoundIncrementOperator? tryInstanceOperatorOverloadResolutionAndFreeMethods(
                ExpressionSyntax node,
                SyntaxToken operatorToken,
                UnaryOperatorKind kind,
                InstanceUserDefinedIncrementUsageMode mode,
                bool checkOverflowAtRuntime,
                bool isExtension,
                BoundExpression operand,
                ref AnalyzedArguments? analyzedArguments,
                ArrayBuilder<MethodSymbol> methods,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(!methods.IsEmpty);

                var operandType = operand.Type;
                Debug.Assert(operandType is not null);

                var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();

                if (analyzedArguments == null)
                {
                    analyzedArguments = AnalyzedArguments.GetInstance();

                    if (isExtension)
                    {
                        // Create a set of arguments for overload resolution including the receiver.
                        CombineExtensionMethodArguments(operand, originalArguments: null, analyzedArguments);

                        if (operandType.IsValueType)
                        {
                            Debug.Assert(analyzedArguments.RefKinds.Count == 0);
                            analyzedArguments.RefKinds.Add(RefKind.Ref);
                        }
                    }
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                OverloadResolution.MethodInvocationOverloadResolution(
                    methods,
                    typeArguments,
                    operand,
                    analyzedArguments,
                    overloadResolutionResult,
                    ref useSiteInfo,
                    OverloadResolution.Options.DisallowExpandedForm | (isExtension ? OverloadResolution.Options.IsExtensionMethodResolution : OverloadResolution.Options.None));

                typeArguments.Free();
                diagnostics.Add(node, useSiteInfo);

                BoundIncrementOperator? inPlaceResult;

                if (overloadResolutionResult.Succeeded)
                {
                    var method = overloadResolutionResult.ValidResult.Member;

                    ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver: false);
                    ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, method, node, isDelegateConversion: false);

                    BoundValuePlaceholder? operandPlaceholder = null;
                    BoundExpression? operandConversion = null;

                    if (isExtension)
                    {
                        Debug.Assert(method.ContainingType.ExtensionParameter is not null);

                        if (Compilation.SourceModule != method.ContainingModule)
                        {
                            // While this code path is reachable, its effect is not observable
                            // because instance operators are simply not considered when the target 
                            // version is C#13 or earlier. Coincidentally the following line
                            // would produce diagnostics only for C#13 or earlier.
                            CheckFeatureAvailability(node, MessageID.IDS_FeatureExtensions, diagnostics);
                        }

                        Conversion conversion = overloadResolutionResult.ValidResult.Result.ConversionForArg(0);

                        if (conversion.Kind is not ConversionKind.Identity)
                        {
                            Debug.Assert(conversion.Kind is ConversionKind.ImplicitReference);
                            Debug.Assert(operandType.IsReferenceType);

                            operandPlaceholder = new BoundValuePlaceholder(operand.Syntax, operandType).MakeCompilerGenerated();
                            operandConversion = CreateConversion(node, operandPlaceholder, conversion, isCast: false, conversionGroupOpt: null, method.ContainingType.ExtensionParameter.Type, diagnostics);
                        }
                    }

                    inPlaceResult = new BoundIncrementOperator(
                        node,
                        (kind | UnaryOperatorKind.UserDefined).WithOverflowChecksIfApplicable(checkOverflowAtRuntime),
                        operand,
                        methodOpt: method,
                        constrainedToTypeOpt: null,
                        operandPlaceholder: operandPlaceholder,
                        operandConversion: operandConversion,
                        resultPlaceholder: null,
                        resultConversion: null,
                        LookupResultKind.Viable,
                        ImmutableArray<MethodSymbol>.Empty,
                        getResultType(node, operandType, mode, diagnostics));

                    methods.Free();
                }
                else if (overloadResolutionResult.HasAnyApplicableMember)
                {
                    ImmutableArray<MethodSymbol> methodsArray = methods.ToImmutableAndFree();

                    overloadResolutionResult.ReportDiagnostics(
                        binder: this, location: operatorToken.GetLocation(), nodeOpt: node, diagnostics: diagnostics, name: operatorToken.ValueText,
                        receiver: operand, invokedExpression: node, arguments: analyzedArguments, memberGroup: methodsArray,
                        typeContainingConstructor: null, delegateTypeBeingInvoked: null);

                    inPlaceResult = new BoundIncrementOperator(
                        node,
                        (kind | UnaryOperatorKind.UserDefined).WithOverflowChecksIfApplicable(checkOverflowAtRuntime),
                        operand,
                        methodOpt: null,
                        constrainedToTypeOpt: null,
                        operandPlaceholder: null,
                        operandConversion: null,
                        resultPlaceholder: null,
                        resultConversion: null,
                        LookupResultKind.OverloadResolutionFailure,
                        methodsArray,
                        getResultType(node, operandType, mode, diagnostics));
                }
                else
                {
                    inPlaceResult = null;
                    methods.Free();
                }

                if (!operatorResolutionForReporting.SaveResult(overloadResolutionResult, isExtension))
                {
                    overloadResolutionResult.Free();
                }

                return inPlaceResult;
            }

            TypeSymbol getResultType(ExpressionSyntax node, TypeSymbol operandType, InstanceUserDefinedIncrementUsageMode mode, BindingDiagnosticBag diagnostics)
            {
                return mode == InstanceUserDefinedIncrementUsageMode.ResultIsUsed ? operandType : GetSpecialType(SpecialType.System_Void, diagnostics, node);
            }

            // This method returns result in two ways:
            // - If it has a result due to instance extensions, it returns ready to use BoundIncrementOperator 
            // - If it has static extensions result, it returns information via out parameters (staticBest, staticResultKind, staticOriginalUserDefinedOperators). 
            BoundIncrementOperator? tryApplyUserDefinedExtensionOperator(
                ExpressionSyntax node,
                UnaryOperatorKind kind,
                InstanceUserDefinedIncrementUsageMode mode,
                bool isChecked,
                string staticOperatorName1,
                string? staticOperatorName2Opt,
                string? checkedInstanceOperatorName,
                string? ordinaryInstanceOperatorName,
                BoundExpression operand,
                BindingDiagnosticBag diagnostics,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                out UnaryOperatorAnalysisResult? staticBest,
                out LookupResultKind staticResultKind,
                out ImmutableArray<MethodSymbol> staticOriginalUserDefinedOperators)
            {
                Debug.Assert(operand.Type is not null);

                staticBest = null;
                staticResultKind = LookupResultKind.Empty;
                staticOriginalUserDefinedOperators = [];

                UnaryOperatorOverloadResolutionResult? result = UnaryOperatorOverloadResolutionResult.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var extensionCandidatesInSingleScope = ArrayBuilder<Symbol>.GetInstance();
                BoundIncrementOperator? inPlaceResult = null;
                AnalyzedArguments? analyzedArguments = null;

                foreach (var scope in new ExtensionScopes(this))
                {
                    // Try an in-place user-defined operator
                    if (mode != InstanceUserDefinedIncrementUsageMode.None)
                    {
                        Debug.Assert(ordinaryInstanceOperatorName is not null);

                        extensionCandidatesInSingleScope.Clear();
                        scope.Binder.GetCandidateExtensionMembersInSingleBinder(extensionCandidatesInSingleScope,
                            ordinaryInstanceOperatorName, checkedInstanceOperatorName, arity: 0,
                            LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustBeInstance, this);

                        inPlaceResult = tryApplyUserDefinedInstanceExtensionOperatorInSingleScope(
                            node, operatorToken, extensionCandidatesInSingleScope, kind, mode, isChecked,
                            checkedInstanceOperatorName, ordinaryInstanceOperatorName,
                            operand, ref analyzedArguments, ref operatorResolutionForReporting, diagnostics);
                        if (inPlaceResult is not null)
                        {
                            break;
                        }
                    }

                    extensionCandidatesInSingleScope.Clear();
                    scope.Binder.GetCandidateExtensionMembersInSingleBinder(extensionCandidatesInSingleScope,
                        staticOperatorName1, staticOperatorName2Opt, arity: 0,
                        LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeOperator | LookupOptions.MustNotBeInstance, this);

                    if (this.OverloadResolution.UnaryOperatorExtensionOverloadResolutionInSingleScope(
                        extensionCandidatesInSingleScope, kind, isChecked,
                        staticOperatorName1, staticOperatorName2Opt,
                        operand, result, ref useSiteInfo))
                    {
                        staticBest = AnalyzeUnaryOperatorOverloadResolutionResult(result, kind, operand, node, diagnostics, out staticResultKind, out staticOriginalUserDefinedOperators);

                        if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                        {
                            result = null;
                        }

                        break;
                    }

                    if (operatorResolutionForReporting.SaveResult(result, isExtension: true))
                    {
                        result = UnaryOperatorOverloadResolutionResult.GetInstance();
                    }
                }

                diagnostics.Add(node, useSiteInfo);

                analyzedArguments?.Free();
                extensionCandidatesInSingleScope.Free();
                result?.Free();
                return inPlaceResult;
            }

            BoundIncrementOperator? tryApplyUserDefinedInstanceExtensionOperatorInSingleScope(
                ExpressionSyntax node,
                SyntaxToken operatorToken,
                ArrayBuilder<Symbol> extensionCandidatesInSingleScope,
                UnaryOperatorKind kind,
                InstanceUserDefinedIncrementUsageMode mode,
                bool isChecked,
                string? checkedName,
                string ordinaryName,
                BoundExpression operand,
                ref AnalyzedArguments? analyzedArguments,
                ref OperatorResolutionForReporting operatorResolutionForReporting,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(mode != InstanceUserDefinedIncrementUsageMode.None);

                ArrayBuilder<MethodSymbol>? methods = LookupUserDefinedInstanceExtensionOperatorsInSingleScope(
                    extensionCandidatesInSingleScope,
                    checkedName: checkedName,
                    ordinaryName: ordinaryName,
                    parameterCount: 0);

                if (methods?.IsEmpty != false)
                {
                    methods?.Free();
                    return null;
                }

                return tryInstanceOperatorOverloadResolutionAndFreeMethods(node, operatorToken, kind, mode, isChecked, isExtension: true, operand, ref analyzedArguments, methods, ref operatorResolutionForReporting, diagnostics);
            }
        }

        private ArrayBuilder<MethodSymbol>? LookupUserDefinedInstanceOperators(TypeSymbol lookupInType, string? checkedName, string ordinaryName, int parameterCount, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(parameterCount is 0 or 1);

            var lookupResult = LookupResult.GetInstance();
            ArrayBuilder<MethodSymbol>? methods = null;
            if (checkedName is not null)
            {
                Debug.Assert(SyntaxFacts.IsCheckedOperator(checkedName));
                this.LookupMembersWithFallback(lookupResult, lookupInType, name: checkedName, arity: 0, ref useSiteInfo, basesBeingResolved: null, options: LookupOptions.MustBeInstance | LookupOptions.MustBeOperator);

                if (lookupResult.IsMultiViable)
                {
                    methods = ArrayBuilder<MethodSymbol>.GetInstance(lookupResult.Symbols.Count);
                    appendViableMethods(lookupResult, parameterCount, methods);
                }

                lookupResult.Clear();
            }

            this.LookupMembersWithFallback(lookupResult, lookupInType, name: ordinaryName, arity: 0, ref useSiteInfo, basesBeingResolved: null, options: LookupOptions.MustBeInstance | LookupOptions.MustBeOperator);

            if (lookupResult.IsMultiViable)
            {
                if (methods is null)
                {
                    methods = ArrayBuilder<MethodSymbol>.GetInstance(lookupResult.Symbols.Count);
                    appendViableMethods(lookupResult, parameterCount, methods);
                }
                else
                {
                    var existing = new HashSet<MethodSymbol>(PairedOperatorComparer.Instance);

                    foreach (var method in methods)
                    {
                        existing.Add(method.GetLeastOverriddenMethod(ContainingType));
                    }

                    foreach (MethodSymbol method in lookupResult.Symbols)
                    {
                        if (IsViableInstanceOperator(method, parameterCount) && !existing.Contains(method.GetLeastOverriddenMethod(ContainingType)))
                        {
                            methods.Add(method);
                        }
                    }
                }
            }

            lookupResult.Free();

            return methods;

            static void appendViableMethods(LookupResult lookupResult, int parameterCount, ArrayBuilder<MethodSymbol> methods)
            {
                foreach (MethodSymbol method in lookupResult.Symbols)
                {
                    if (IsViableInstanceOperator(method, parameterCount))
                    {
                        methods.Add(method);
                    }
                }
            }
        }

        private static bool IsViableInstanceOperator(MethodSymbol method, int parameterCount)
        {
            Debug.Assert(parameterCount is 0 or 1);
            return method.ParameterCount == parameterCount && method.ReturnsVoid && !method.IsVararg &&
                    (parameterCount == 0 || method.Parameters[0].RefKind is RefKind.None or RefKind.In);
        }

        private ArrayBuilder<MethodSymbol>? LookupUserDefinedInstanceExtensionOperatorsInSingleScope(
            ArrayBuilder<Symbol> extensionCandidatesInSingleScope,
            string? checkedName,
            string ordinaryName,
            int parameterCount)
        {
            Debug.Assert(parameterCount is 0 or 1);
            ArrayBuilder<MethodSymbol>? checkedMethods = null;

            if (checkedName is not null)
            {
                Debug.Assert(SyntaxFacts.IsCheckedOperator(checkedName));
                lookupUserDefinedInstanceExtensionOperatorsInSingleScope(extensionCandidatesInSingleScope, name: checkedName, parameterCount, ref checkedMethods);
            }

            ArrayBuilder<MethodSymbol>? ordinaryMethods = null;
            lookupUserDefinedInstanceExtensionOperatorsInSingleScope(extensionCandidatesInSingleScope, name: ordinaryName, parameterCount, ref ordinaryMethods);

            if (ordinaryMethods is not null)
            {
                if (checkedMethods is null)
                {
                    return ordinaryMethods;
                }
                else
                {
                    var existing = new HashSet<MethodSymbol>(OverloadResolution.PairedExtensionOperatorSignatureComparer.Instance);
                    existing.AddRange(checkedMethods);

                    foreach (MethodSymbol method in ordinaryMethods)
                    {
                        if (!existing.Contains(method))
                        {
                            checkedMethods.Add(method);
                        }
                    }
                }
            }

            return checkedMethods;

            static void lookupUserDefinedInstanceExtensionOperatorsInSingleScope(
                ArrayBuilder<Symbol> extensionCandidatesInSingleScope,
                string name,
                int parameterCount,
                ref ArrayBuilder<MethodSymbol>? methods)
            {
                if (extensionCandidatesInSingleScope.IsEmpty)
                {
                    return;
                }

                var typeOperators = ArrayBuilder<MethodSymbol>.GetInstance();
                NamedTypeSymbol.AddOperators(typeOperators, extensionCandidatesInSingleScope);

                foreach (MethodSymbol op in typeOperators)
                {
                    var extensionParameter = op.ContainingType.ExtensionParameter;
                    Debug.Assert(extensionParameter is not null);
                    if (!((extensionParameter.Type.IsValueType && extensionParameter.RefKind == RefKind.Ref) ||
                        (extensionParameter.Type.IsReferenceType && extensionParameter.RefKind == RefKind.None)))
                    {
                        continue;
                    }

                    if (op.Name != name)
                    {
                        continue;
                    }

                    Debug.Assert(!op.IsStatic);
                    // If we're in error recovery, we might have bad operators. Just ignore them.
                    if (!IsViableInstanceOperator(op, parameterCount))
                    {
                        continue;
                    }

                    methods ??= ArrayBuilder<MethodSymbol>.GetInstance();
                    methods.Add(op);
                }

                typeOperators.Free();
            }
        }
#nullable disable

        private class PairedOperatorComparer : IEqualityComparer<MethodSymbol>
        {
            public static readonly PairedOperatorComparer Instance = new PairedOperatorComparer();

            private PairedOperatorComparer() { }

            public bool Equals(MethodSymbol x, MethodSymbol y)
            {
                Debug.Assert(!x.IsOverride);
                Debug.Assert(!x.IsStatic);

                Debug.Assert(!y.IsOverride);
                Debug.Assert(!y.IsStatic);

                var typeComparer = Symbols.SymbolEqualityComparer.AllIgnoreOptions;
                return typeComparer.Equals(x.ContainingType, y.ContainingType) &&
                       SourceMemberContainerTypeSymbol.DoOperatorsPair(x, y);
            }

            public int GetHashCode([DisallowNull] MethodSymbol method)
            {
                Debug.Assert(!method.IsOverride);
                Debug.Assert(!method.IsStatic);

                var typeComparer = Symbols.SymbolEqualityComparer.AllIgnoreOptions;
                int result = typeComparer.GetHashCode(method.ContainingType);

                if (method.ParameterTypesWithAnnotations is [var typeWithAnnotations, ..])
                {
                    result = Hash.Combine(result, typeComparer.GetHashCode(typeWithAnnotations.Type));
                }

                return result;
            }
        }

#nullable enable
        /// <summary>
        /// Returns false if reported an error, true otherwise.
        /// </summary>
        private bool CheckConstraintLanguageVersionAndRuntimeSupportForOperator(SyntaxNode node, MethodSymbol? methodOpt, bool isUnsignedRightShift, TypeSymbol? constrainedToTypeOpt, BindingDiagnosticBag diagnostics)
        {
            bool result = true;

            if (methodOpt?.ContainingType?.IsInterface == true && methodOpt.IsStatic)
            {
                if (methodOpt.IsAbstract || methodOpt.IsVirtual)
                {
                    if (constrainedToTypeOpt is not TypeParameterSymbol)
                    {
                        Error(diagnostics, ErrorCode.ERR_BadAbstractStaticMemberAccess, node);
                        return false;
                    }

                    if (Compilation.SourceModule != methodOpt.ContainingModule)
                    {
                        result = CheckFeatureAvailability(node, MessageID.IDS_FeatureStaticAbstractMembersInInterfaces, diagnostics);

                        if (!Compilation.Assembly.RuntimeSupportsStaticAbstractMembersInInterfaces)
                        {
                            Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, node);
                            return false;
                        }
                    }
                }
                else if (methodOpt.Name is WellKnownMemberNames.EqualityOperatorName or WellKnownMemberNames.InequalityOperatorName)
                {
                    result = CheckFeatureAvailability(node, MessageID.IDS_FeatureStaticAbstractMembersInInterfaces, diagnostics);
                }
            }

            if (methodOpt is null)
            {
                if (isUnsignedRightShift)
                {
                    result &= CheckFeatureAvailability(node, MessageID.IDS_FeatureUnsignedRightShift, diagnostics);
                }
            }
            else
            {
                Debug.Assert((methodOpt.Name == WellKnownMemberNames.UnsignedRightShiftOperatorName) == isUnsignedRightShift);

                if (Compilation.SourceModule != methodOpt.ContainingModule)
                {
                    if (methodOpt.IsExtensionBlockMember())
                    {
                        result &= CheckFeatureAvailability(node, MessageID.IDS_FeatureExtensions, diagnostics);
                    }
                    else if (SyntaxFacts.IsCheckedOperator(methodOpt.Name))
                    {
                        result &= CheckFeatureAvailability(node, MessageID.IDS_FeatureCheckedUserDefinedOperators, diagnostics);
                    }
                    else if (isUnsignedRightShift)
                    {
                        result &= CheckFeatureAvailability(node, MessageID.IDS_FeatureUnsignedRightShift, diagnostics);
                    }
                }
            }

            return result;
        }
#nullable disable

        private BoundExpression BindSuppressNullableWarningExpression(PostfixUnaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureNullableReferenceTypes.CheckFeatureAvailability(diagnostics, node.OperatorToken);

            var expr = BindExpression(node.Operand, diagnostics);
            switch (expr.Kind)
            {
                case BoundKind.NamespaceExpression:
                case BoundKind.TypeExpression:
                    Error(diagnostics, ErrorCode.ERR_IllegalSuppression, expr.Syntax);
                    break;
                default:
                    if (expr.IsSuppressed)
                    {
                        Debug.Assert(node.Operand.SkipParens().GetLastToken().Kind() == SyntaxKind.ExclamationToken);
                        Error(diagnostics, ErrorCode.ERR_DuplicateNullSuppression, expr.Syntax);
                    }
                    break;
            }

            return expr.WithSuppression();
        }

        // Based on ExpressionBinder::bindPtrIndirection.
        private BoundExpression BindPointerIndirectionExpression(PrefixUnaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression operand = BindToNaturalType(BindValue(node.Operand, diagnostics, GetUnaryAssignmentKind(node.Kind())), diagnostics);

            TypeSymbol pointedAtType;
            bool hasErrors;
            BindPointerIndirectionExpressionInternal(node, operand, diagnostics, out pointedAtType, out hasErrors);

            return new BoundPointerIndirectionOperator(node, operand, refersToLocation: false, pointedAtType ?? CreateErrorType(), hasErrors);
        }

        private static void BindPointerIndirectionExpressionInternal(CSharpSyntaxNode node, BoundExpression operand, BindingDiagnosticBag diagnostics, out TypeSymbol pointedAtType, out bool hasErrors)
        {
            var operandType = operand.Type as PointerTypeSymbol;

            hasErrors = operand.HasAnyErrors; // This would propagate automatically, but by reading it explicitly we can reduce cascading.

            if ((object)operandType == null)
            {
                pointedAtType = null;

                if (!hasErrors)
                {
                    // NOTE: Dev10 actually reports ERR_BadUnaryOp if the operand has Type == null,
                    // but this seems clearer.
                    Error(diagnostics, ErrorCode.ERR_PtrExpected, node);
                    hasErrors = true;
                }
            }
            else
            {
                pointedAtType = operandType.PointedAtType;

                if (pointedAtType.IsVoidType())
                {
                    pointedAtType = null;

                    if (!hasErrors)
                    {
                        Error(diagnostics, ErrorCode.ERR_VoidError, node);
                        hasErrors = true;
                    }
                }
            }
        }

        // Based on ExpressionBinder::bindPtrAddr.
        private BoundExpression BindAddressOfExpression(PrefixUnaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression operand = BindToNaturalType(BindValue(node.Operand, diagnostics, BindValueKind.AddressOf), diagnostics);
            ReportSuppressionIfNeeded(operand, diagnostics);

            bool hasErrors = operand.HasAnyErrors; // This would propagate automatically, but by reading it explicitly we can reduce cascading.
            bool isFixedStatementAddressOfExpression = SyntaxFacts.IsFixedStatementExpression(node);

            switch (operand)
            {
                case BoundLambda _:
                case UnboundLambda _:
                    {
                        Debug.Assert(hasErrors);
                        return new BoundAddressOfOperator(node, operand, CreateErrorType(), hasErrors: true);
                    }

                case BoundMethodGroup methodGroup:
                    return new BoundUnconvertedAddressOfOperator(node, methodGroup, hasErrors);
            }

            TypeSymbol operandType = operand.Type;
            Debug.Assert((object)operandType != null, "BindValue should have caught a null operand type");

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            ManagedKind managedKind = operandType.GetManagedKind(ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

            if (!hasErrors)
            {
                hasErrors = CheckManagedAddr(Compilation, operandType, managedKind, node.Location, diagnostics);
            }

            bool allowManagedAddressOf = Flags.Includes(BinderFlags.AllowMoveableAddressOf);
            if (!hasErrors && !allowManagedAddressOf)
            {
                if (IsMoveableVariable(operand, accessedLocalOrParameterOpt: out _) != isFixedStatementAddressOfExpression)
                {
                    Error(diagnostics, isFixedStatementAddressOfExpression ? ErrorCode.ERR_FixedNotNeeded : ErrorCode.ERR_FixedNeeded, node);
                    hasErrors = true;
                }
            }

            TypeSymbol pointerType = new PointerTypeSymbol(TypeWithAnnotations.Create(operandType));
            return new BoundAddressOfOperator(node, operand, pointerType, hasErrors);
        }

        /// <summary>
        /// Checks to see whether an expression is a "moveable" variable according to the spec. Moveable
        /// variables have underlying memory which may be moved by the runtime. The spec defines anything
        /// not fixed as moveable and specifies the expressions which are fixed.
        /// </summary>
        internal bool IsMoveableVariable(BoundExpression expr, out Symbol accessedLocalOrParameterOpt)
        {
            accessedLocalOrParameterOpt = null;

            while (true)
            {
                BoundKind exprKind = expr.Kind;
                switch (exprKind)
                {
                    case BoundKind.FieldAccess:
                    case BoundKind.EventAccess:
                        {
                            FieldSymbol fieldSymbol;
                            BoundExpression receiver;
                            if (exprKind == BoundKind.FieldAccess)
                            {
                                BoundFieldAccess fieldAccess = (BoundFieldAccess)expr;
                                fieldSymbol = fieldAccess.FieldSymbol;
                                receiver = fieldAccess.ReceiverOpt;
                            }
                            else
                            {
                                BoundEventAccess eventAccess = (BoundEventAccess)expr;
                                if (!eventAccess.IsUsableAsField || eventAccess.EventSymbol.IsWindowsRuntimeEvent)
                                {
                                    return true;
                                }
                                EventSymbol eventSymbol = eventAccess.EventSymbol;
                                fieldSymbol = eventSymbol.AssociatedField;
                                receiver = eventAccess.ReceiverOpt;
                            }

                            if ((object)fieldSymbol == null || fieldSymbol.IsStatic || (object)receiver == null)
                            {
                                return true;
                            }

                            bool receiverIsLValue = CheckValueKind(receiver.Syntax, receiver, BindValueKind.AddressOf, checkingReceiver: false, diagnostics: BindingDiagnosticBag.Discarded);

                            if (!receiverIsLValue)
                            {
                                return true;
                            }

                            // NOTE: type parameters will already have been weeded out, since a
                            // variable of type parameter type has to be cast to an effective
                            // base or interface type before its fields can be accessed and a
                            // conversion isn't an lvalue.
                            if (receiver.Type.IsReferenceType)
                            {
                                return true;
                            }

                            expr = receiver;
                            continue;
                        }
                    case BoundKind.InlineArrayAccess:
                        {
                            var elementAccess = (BoundInlineArrayAccess)expr;

                            if (elementAccess.GetItemOrSliceHelper is WellKnownMember.System_Span_T__get_Item or WellKnownMember.System_ReadOnlySpan_T__get_Item)
                            {
                                expr = elementAccess.Expression;
                                continue;
                            }

                            goto default;
                        }
                    case BoundKind.RangeVariable:
                        {
                            // NOTE: there are cases where you can take the address of a range variable.
                            // e.g. from x in new int[3] select *(&x)
                            BoundRangeVariable variableAccess = (BoundRangeVariable)expr;
                            expr = variableAccess.Value; //Check the underlying expression.
                            continue;
                        }
                    case BoundKind.Parameter:
                        {
                            BoundParameter parameterAccess = (BoundParameter)expr;
                            ParameterSymbol parameterSymbol = parameterAccess.ParameterSymbol;
                            accessedLocalOrParameterOpt = parameterSymbol;

                            if (parameterSymbol.RefKind != RefKind.None)
                            {
                                return true;
                            }

                            if (parameterSymbol.ContainingSymbol is SynthesizedPrimaryConstructor primaryConstructor &&
                                primaryConstructor.GetCapturedParameters().ContainsKey(parameterSymbol))
                            {
                                // See 'case BoundKind.FieldAccess' above. Receiver in our case is 'this' parameter.
                                // If we are in a class, its type is reference type.
                                // If we are in a struct, 'this' RefKind is not None.
                                // Therefore, movable in either case. 
                                return true;
                            }

                            return false;
                        }
                    case BoundKind.ThisReference:
                    case BoundKind.BaseReference:
                        {
                            accessedLocalOrParameterOpt = this.ContainingMemberOrLambda.EnclosingThisSymbol();
                            return true;
                        }
                    case BoundKind.Local:
                        {
                            BoundLocal localAccess = (BoundLocal)expr;
                            LocalSymbol localSymbol = localAccess.LocalSymbol;
                            accessedLocalOrParameterOpt = localSymbol;
                            // NOTE: The spec says that this is moveable if it is captured by an anonymous function,
                            // but that will be reported separately and error-recovery is better if we say that
                            // such locals are not moveable.
                            return localSymbol.RefKind != RefKind.None;
                        }
                    case BoundKind.PointerIndirectionOperator: //Covers ->, since the receiver will be one of these.
                    case BoundKind.ConvertedStackAllocExpression:
                        {
                            return false;
                        }
                    case BoundKind.PointerElementAccess:
                        {
                            // C# 7.3:
                            // a variable resulting from a... pointer_element_access of the form P[E] [is fixed] if P
                            // is not a fixed size buffer expression, or if the expression is a fixed size buffer
                            // member_access of the form E.I and E is a fixed variable
                            BoundExpression underlyingExpr = ((BoundPointerElementAccess)expr).Expression;
                            if (underlyingExpr is BoundFieldAccess fieldAccess && fieldAccess.FieldSymbol.IsFixedSizeBuffer)
                            {
                                expr = fieldAccess.ReceiverOpt;
                                continue;
                            }

                            return false;
                        }
                    case BoundKind.PropertyAccess: // Never fixed
                    case BoundKind.IndexerAccess: // Never fixed
                    default:
                        {
                            return true;
                        }
                }
            }
        }

        private BoundExpression BindUnaryOperator(PrefixUnaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression operand = BindToNaturalType(BindValue(node.Operand, diagnostics, GetUnaryAssignmentKind(node.Kind())), diagnostics);
            BoundLiteral constant = BindIntegralMinValConstants(node, operand, diagnostics);
            return constant ?? BindUnaryOperatorCore(node, node.OperatorToken.Text, operand, diagnostics);
        }

        private void ReportSuppressionIfNeeded(BoundExpression expr, BindingDiagnosticBag diagnostics)
        {
            if (expr.IsSuppressed)
            {
                Error(diagnostics, ErrorCode.ERR_IllegalSuppression, expr.Syntax);
            }
        }

#nullable enable
        private BoundExpression BindUnaryOperatorCore(CSharpSyntaxNode node, string operatorText, BoundExpression operand, BindingDiagnosticBag diagnostics)
        {
            UnaryOperatorKind kind = SyntaxKindToUnaryOperatorKind(node.Kind());

            bool isOperandNullOrNew = operand.IsLiteralNull() || operand.IsImplicitObjectCreation();
            if (isOperandNullOrNew)
            {
                // Dev10 does not allow unary prefix operators to be applied to the null literal
                // (or other typeless expressions).
                Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, node, operatorText, operand.Display);
            }

            // If the operand is bad, avoid generating cascading errors.
            if (isOperandNullOrNew || operand.Type?.IsErrorType() == true)
            {
                // Note: no candidate user-defined operators.
                return new BoundUnaryOperator(node, kind, operand, ConstantValue.NotAvailable,
                    methodOpt: null,
                    constrainedToTypeOpt: null,
                    resultKind: LookupResultKind.Empty,
                    type: CreateErrorType(),
                    hasErrors: true);
            }

            // If the operand is dynamic then we do not attempt to do overload resolution at compile
            // time; we defer that until runtime. If we did overload resolution then the dynamic
            // operand would be implicitly convertible to the parameter type of each operator
            // signature, and therefore every operator would be an applicable candidate. Instead
            // of changing overload resolution to handle dynamic, we just handle it here and let
            // overload resolution implement the specification.

            if (operand.HasDynamicType())
            {
                return new BoundUnaryOperator(
                    syntax: node,
                    operatorKind: kind.WithType(UnaryOperatorKind.Dynamic).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                    operand: operand,
                    constantValueOpt: ConstantValue.NotAvailable,
                    methodOpt: null,
                    constrainedToTypeOpt: null,
                    resultKind: LookupResultKind.Viable,
                    type: operand.Type!);
            }

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            OperatorResolutionForReporting operatorResolutionForReporting = default;

            var best = this.UnaryOperatorOverloadResolution(kind, operand, node, diagnostics, ref operatorResolutionForReporting, out resultKind, out originalUserDefinedOperators);
            if (!best.HasValue)
            {
                ReportUnaryOperatorError(node, diagnostics, operatorText, operand, resultKind, ref operatorResolutionForReporting);
                operatorResolutionForReporting.Free();

                return new BoundUnaryOperator(
                    node,
                    kind,
                    operand,
                    ConstantValue.NotAvailable,
                    methodOpt: null,
                    constrainedToTypeOpt: null,
                    resultKind,
                    originalUserDefinedOperators,
                    CreateErrorType(),
                    hasErrors: true);
            }

            operatorResolutionForReporting.Free();
            var signature = best.Signature;

            var resultOperand = CreateConversion(operand.Syntax, operand, best.Conversion, isCast: false, conversionGroupOpt: null, signature.OperandType, diagnostics);
            var resultType = signature.ReturnType;
            UnaryOperatorKind resultOperatorKind = signature.Kind;
            var resultConstant = FoldUnaryOperator(node, resultOperatorKind, resultOperand, resultType, diagnostics);

            CheckNativeIntegerFeatureAvailability(resultOperatorKind, node, diagnostics);
            CheckConstraintLanguageVersionAndRuntimeSupportForOperator(node, signature.Method, isUnsignedRightShift: false, signature.ConstrainedToTypeOpt, diagnostics);

            return new BoundUnaryOperator(
                node,
                resultOperatorKind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                resultOperand,
                resultConstant,
                signature.Method,
                signature.ConstrainedToTypeOpt,
                resultKind,
                resultType);
        }

        private ConstantValue? FoldEnumUnaryOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind kind,
            BoundExpression operand,
            BindingDiagnosticBag diagnostics)
        {
            var underlyingType = operand.Type.GetEnumUnderlyingType()!;

            BoundExpression newOperand = CreateConversion(operand, underlyingType, diagnostics);

            // We may have to upconvert the type if it is a byte, sbyte, short, ushort
            // or nullable of those, because there is no ~ operator
            var upconvertSpecialType = GetEnumPromotedType(underlyingType.SpecialType);
            var upconvertType = upconvertSpecialType == underlyingType.SpecialType ?
                underlyingType :
                GetSpecialType(upconvertSpecialType, diagnostics, syntax);

            newOperand = CreateConversion(newOperand, upconvertType, diagnostics);

            UnaryOperatorKind newKind = kind.Operator().WithType(upconvertSpecialType);

            var constantValue = FoldUnaryOperator(syntax, newKind, operand, upconvertType, diagnostics);

            // Convert back to the underlying type
            if (constantValue != null && !constantValue.IsBad)
            {
                // Do an unchecked conversion if bitwise complement
                var binder = kind.Operator() == UnaryOperatorKind.BitwiseComplement ?
                    this.WithCheckedOrUncheckedRegion(@checked: false) : this;
                return binder.FoldConstantNumericConversion(syntax, constantValue, underlyingType, diagnostics);
            }

            return constantValue;
        }

        private ConstantValue? FoldUnaryOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind kind,
            BoundExpression operand,
            TypeSymbol resultTypeSymbol,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(operand != null);
            // UNDONE: report errors when in a checked context.

            if (operand.HasAnyErrors)
            {
                return null;
            }

            var value = operand.ConstantValueOpt;
            if (value == null || value.IsBad)
            {
                return value;
            }

            if (kind.IsEnum() && !kind.IsLifted())
            {
                return FoldEnumUnaryOperator(syntax, kind, operand, diagnostics);
            }

            SpecialType resultType = resultTypeSymbol.SpecialType;
            var newValue = FoldNeverOverflowUnaryOperator(kind, value);
            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            try
            {
                newValue = FoldNativeIntegerOverflowingUnaryOperator(kind, value);
            }
            catch (OverflowException)
            {
                if (CheckOverflowAtCompileTime)
                {
                    Error(diagnostics, ErrorCode.WRN_CompileTimeCheckedOverflow, syntax, resultTypeSymbol);
                }

                return null;
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            if (CheckOverflowAtCompileTime)
            {
                try
                {
                    newValue = FoldCheckedIntegralUnaryOperator(kind, value);
                }
                catch (OverflowException)
                {
                    Error(diagnostics, ErrorCode.ERR_CheckedOverflow, syntax);
                    return ConstantValue.Bad;
                }
            }
            else
            {
                newValue = FoldUncheckedIntegralUnaryOperator(kind, value);
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            return null;
        }

        private static object? FoldNeverOverflowUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            // Note that we do operations on single-precision floats as double-precision.
            switch (kind)
            {
                case UnaryOperatorKind.DecimalUnaryMinus:
                    return -value.DecimalValue;
                case UnaryOperatorKind.DoubleUnaryMinus:
                case UnaryOperatorKind.FloatUnaryMinus:
                    return -value.DoubleValue;
                case UnaryOperatorKind.DecimalUnaryPlus:
                    return +value.DecimalValue;
                case UnaryOperatorKind.FloatUnaryPlus:
                case UnaryOperatorKind.DoubleUnaryPlus:
                    return +value.DoubleValue;
                case UnaryOperatorKind.LongUnaryPlus:
                    return +value.Int64Value;
                case UnaryOperatorKind.ULongUnaryPlus:
                    return +value.UInt64Value;
                case UnaryOperatorKind.IntUnaryPlus:
                case UnaryOperatorKind.NIntUnaryPlus:
                    return +value.Int32Value;
                case UnaryOperatorKind.UIntUnaryPlus:
                case UnaryOperatorKind.NUIntUnaryPlus:
                    return +value.UInt32Value;
                case UnaryOperatorKind.BoolLogicalNegation:
                    return !value.BooleanValue;
                case UnaryOperatorKind.IntBitwiseComplement:
                    return ~value.Int32Value;
                case UnaryOperatorKind.LongBitwiseComplement:
                    return ~value.Int64Value;
                case UnaryOperatorKind.UIntBitwiseComplement:
                    return ~value.UInt32Value;
                case UnaryOperatorKind.ULongBitwiseComplement:
                    return ~value.UInt64Value;
            }

            return null;
        }

        private static object? FoldUncheckedIntegralUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            unchecked
            {
                switch (kind)
                {
                    case UnaryOperatorKind.LongUnaryMinus:
                        return -value.Int64Value;
                    case UnaryOperatorKind.IntUnaryMinus:
                        return -value.Int32Value;
                }
            }

            return null;
        }

        private static object? FoldCheckedIntegralUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            checked
            {
                switch (kind)
                {
                    case UnaryOperatorKind.LongUnaryMinus:
                        return -value.Int64Value;
                    case UnaryOperatorKind.IntUnaryMinus:
                        return -value.Int32Value;
                }
            }

            return null;
        }

        private static object? FoldNativeIntegerOverflowingUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            checked
            {
                switch (kind)
                {
                    case UnaryOperatorKind.NIntUnaryMinus:
                        return -value.Int32Value;
                    case UnaryOperatorKind.NIntBitwiseComplement:
                    case UnaryOperatorKind.NUIntBitwiseComplement:
                        return null;
                }
            }

            return null;
        }

        public static UnaryOperatorKind SyntaxKindToUnaryOperatorKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PreIncrementExpression: return UnaryOperatorKind.PrefixIncrement;
                case SyntaxKind.PostIncrementExpression: return UnaryOperatorKind.PostfixIncrement;
                case SyntaxKind.PreDecrementExpression: return UnaryOperatorKind.PrefixDecrement;
                case SyntaxKind.PostDecrementExpression: return UnaryOperatorKind.PostfixDecrement;
                case SyntaxKind.UnaryPlusExpression: return UnaryOperatorKind.UnaryPlus;
                case SyntaxKind.UnaryMinusExpression: return UnaryOperatorKind.UnaryMinus;
                case SyntaxKind.LogicalNotExpression: return UnaryOperatorKind.LogicalNegation;
                case SyntaxKind.BitwiseNotExpression: return UnaryOperatorKind.BitwiseComplement;
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static BindValueKind GetBinaryAssignmentKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    return BindValueKind.Assignable;
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.UnsignedRightShiftAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                    return BindValueKind.CompoundAssignment;
                default:
                    return BindValueKind.RValue;
            }
        }

        private static BindValueKind GetUnaryAssignmentKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PreDecrementExpression:
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PostDecrementExpression:
                case SyntaxKind.PostIncrementExpression:
                    return BindValueKind.IncrementDecrement;
                case SyntaxKind.AddressOfExpression:
                    Debug.Assert(false, "Should be handled separately.");
                    goto default;
                default:
                    return BindValueKind.RValue;
            }
        }
#nullable disable

        private BoundLiteral BindIntegralMinValConstants(PrefixUnaryExpressionSyntax node, BoundExpression operand, BindingDiagnosticBag diagnostics)
        {
            // SPEC: To permit the smallest possible int and long values to be written as decimal integer
            // SPEC: literals, the following two rules exist:

            // SPEC: When a decimal-integer-literal with the value 2147483648 and no integer-type-suffix
            // SPEC: appears as the token immediately following a unary minus operator token, the result is a
            // SPEC: constant of type int with the value −2147483648.

            // SPEC: When a decimal-integer-literal with the value 9223372036854775808 and no integer-type-suffix
            // SPEC: or the integer-type-suffix L or l appears as the token immediately following a unary minus
            // SPEC: operator token, the result is a constant of type long with the value −9223372036854775808.

            if (node.Kind() != SyntaxKind.UnaryMinusExpression)
            {
                return null;
            }

            if (node.Operand != operand.Syntax || operand.Syntax.Kind() != SyntaxKind.NumericLiteralExpression)
            {
                return null;
            }

            var literal = (LiteralExpressionSyntax)operand.Syntax;
            var token = literal.Token;
            if (token.Value is uint)
            {
                uint value = (uint)token.Value;
                if (value != 2147483648U)
                {
                    return null;
                }

                if (token.Text.Contains("u") || token.Text.Contains("U") || token.Text.Contains("l") || token.Text.Contains("L"))
                {
                    return null;
                }

                return new BoundLiteral(node, ConstantValue.Create((int)-2147483648), GetSpecialType(SpecialType.System_Int32, diagnostics, node));
            }
            else if (token.Value is ulong)
            {
                var value = (ulong)token.Value;
                if (value != 9223372036854775808UL)
                {
                    return null;
                }

                if (token.Text.Contains("u") || token.Text.Contains("U"))
                {
                    return null;
                }

                return new BoundLiteral(node, ConstantValue.Create(-9223372036854775808), GetSpecialType(SpecialType.System_Int64, diagnostics, node));
            }

            return null;
        }

        private static bool IsDivisionByZero(BinaryOperatorKind kind, ConstantValue valueRight)
        {
            Debug.Assert(valueRight != null);

            switch (kind)
            {
                case BinaryOperatorKind.DecimalDivision:
                case BinaryOperatorKind.DecimalRemainder:
                    return valueRight.DecimalValue == 0.0m;
                case BinaryOperatorKind.IntDivision:
                case BinaryOperatorKind.IntRemainder:
                case BinaryOperatorKind.NIntDivision:
                case BinaryOperatorKind.NIntRemainder:
                    return valueRight.Int32Value == 0;
                case BinaryOperatorKind.LongDivision:
                case BinaryOperatorKind.LongRemainder:
                    return valueRight.Int64Value == 0;
                case BinaryOperatorKind.UIntDivision:
                case BinaryOperatorKind.UIntRemainder:
                case BinaryOperatorKind.NUIntDivision:
                case BinaryOperatorKind.NUIntRemainder:
                    return valueRight.UInt32Value == 0;
                case BinaryOperatorKind.ULongDivision:
                case BinaryOperatorKind.ULongRemainder:
                    return valueRight.UInt64Value == 0;
            }

            return false;
        }

        private bool IsOperandErrors(CSharpSyntaxNode node, ref BoundExpression operand, BindingDiagnosticBag diagnostics)
        {
            switch (operand.Kind)
            {
                case BoundKind.UnboundLambda:
                case BoundKind.Lambda:
                case BoundKind.MethodGroup:  // New in Roslyn - see DevDiv #864740.
                    // operand for an is or as expression cannot be a lambda expression or method group
                    if (!operand.HasAnyErrors)
                    {
                        Error(diagnostics, ErrorCode.ERR_LambdaInIsAs, node);
                    }

                    operand = BadExpression(node, operand).MakeCompilerGenerated();
                    return true;

                default:
                    if ((object)operand.Type == null && !operand.IsLiteralNull())
                    {
                        if (!operand.HasAnyErrors)
                        {
                            // Operator 'is' cannot be applied to operand of type '(int, <null>)'
                            Error(diagnostics, ErrorCode.ERR_BadUnaryOp, node, SyntaxFacts.GetText(SyntaxKind.IsKeyword), operand.Display);
                        }

                        operand = BadExpression(node, operand).MakeCompilerGenerated();
                        return true;
                    }

                    break;
            }

            return operand.HasAnyErrors;
        }

        private bool IsOperatorErrors(CSharpSyntaxNode node, TypeSymbol operandType, BoundTypeExpression typeExpression, BindingDiagnosticBag diagnostics)
        {
            var targetType = typeExpression.Type;

            // The native compiler allows "x is C" where C is a static class. This
            // is strictly illegal according to the specification (see the section
            // called "Referencing Static Class Types".) To retain compatibility we
            // allow it, but when /warn:5 or higher we break with the native
            // compiler and turn this into a warning.
            if (targetType.IsStatic)
            {
                Error(diagnostics, ErrorCode.WRN_StaticInAsOrIs, node, targetType);
            }

            if ((object)operandType != null && operandType.IsPointerOrFunctionPointer() || targetType.IsPointerOrFunctionPointer())
            {
                // operand for an is or as expression cannot be of pointer type
                Error(diagnostics, ErrorCode.ERR_PointerInAsOrIs, node);
                return true;
            }

            return targetType.TypeKind == TypeKind.Error;
        }

        protected static bool IsUnderscore(ExpressionSyntax node) =>
            node is IdentifierNameSyntax name && name.Identifier.IsUnderscoreToken();

        private BoundExpression BindIsOperator(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var resultType = (TypeSymbol)GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            var operand = BindRValueWithoutTargetType(node.Left, diagnostics);
            var operandHasErrors = IsOperandErrors(node, ref operand, diagnostics);

            TypeSymbol inputType = operand.Type;
            NamedTypeSymbol unionType;

            // try binding as a type, but back off to binding as an expression if that does not work.
            bool wasUnderscore = IsUnderscore(node.Right);
            if (!tryBindAsType(node.Right, diagnostics, out BindingDiagnosticBag isTypeDiagnostics, out BoundTypeExpression typeExpression) &&
                !wasUnderscore &&
                ((CSharpParseOptions)node.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeaturePatternMatching))
            {
                // it did not bind as a type; try binding as a constant expression pattern
                var isPatternDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
                if ((object)inputType == null)
                {
                    if (!operandHasErrors)
                    {
                        isPatternDiagnostics.Add(ErrorCode.ERR_BadPatternExpression, node.Left.Location, operand.Display);
                    }

                    operand = ToBadExpression(operand);
                    inputType = operand.Type;
                }

                unionType = PrepareForUnionMatchingIfAppropriateAndReturnUnionType(node, ref inputType, isPatternDiagnostics);

                bool hasErrors = node.Right.HasErrors;
                var convertedExpression = BindExpressionForPattern(unionType, inputType, node.Right, ref hasErrors, isPatternDiagnostics, out var constantValueOpt, out var wasExpression, out _);
                if (wasExpression)
                {
                    hasErrors |= constantValueOpt is null;
                    isTypeDiagnostics.Free();
                    diagnostics.AddRangeAndFree(isPatternDiagnostics);

                    var boundConstantPattern = new BoundConstantPattern(
                        node.Right, convertedExpression, constantValueOpt ?? ConstantValue.Bad, isUnionMatching: unionType is not null, inputType: unionType ?? inputType, convertedExpression.Type ?? inputType, hasErrors)
#pragma warning disable format
                        { WasCompilerGenerated = true };
#pragma warning restore format
                    return MakeIsPatternExpression(node, operand, boundConstantPattern, boundConstantPattern.IsUnionMatching, resultType, operandHasErrors, diagnostics);
                }

                isPatternDiagnostics.Free();
            }

            diagnostics.AddRangeAndFree(isTypeDiagnostics);
            var targetType = typeExpression.Type;

            unionType = PrepareForUnionMatchingIfAppropriateAndReturnUnionType(node, ref inputType, diagnostics);
            if (unionType is not null)
            {
                bool hasErrors = CheckValidPatternType(node.Right, unionType, inputType, targetType, diagnostics: diagnostics);
                // PROTOTYPE: Add test coverage for isExplicitNotNullTest
                var pattern = new BoundTypePattern(node, typeExpression, isExplicitNotNullTest: false, isUnionMatching: true, inputType: unionType, targetType, hasErrors);
                return MakeIsPatternExpression(node, operand, pattern.MakeCompilerGenerated(), hasUnionMatching: true, resultType, operandHasErrors, diagnostics);
            }

            var targetTypeWithAnnotations = typeExpression.TypeWithAnnotations;
            if (targetType.IsReferenceType && targetTypeWithAnnotations.NullableAnnotation.IsAnnotated())
            {
                Error(diagnostics, ErrorCode.ERR_IsNullableType, node.Right, targetType);
                operandHasErrors = true;
            }

            var targetTypeKind = targetType.TypeKind;
            if (operandHasErrors || IsOperatorErrors(node, inputType, typeExpression, diagnostics))
            {
                return new BoundIsOperator(node, operand, typeExpression, ConversionKind.NoConversion, resultType, hasErrors: true);
            }

            if (wasUnderscore && ((CSharpParseOptions)node.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeatureRecursivePatterns))
            {
                diagnostics.Add(ErrorCode.WRN_IsTypeNamedUnderscore, node.Right.Location, typeExpression.AliasOpt ?? (Symbol)targetType);
            }

            // Is and As operator should have null ConstantValue as they are not constant expressions.
            // However we perform analysis of is/as expressions at bind time to detect if the expression
            // will always evaluate to a constant to generate warnings (always true/false/null).
            // We also need this analysis result during rewrite to optimize away redundant isinst instructions.
            // We store the conversion from expression's operand type to target type to enable these
            // optimizations during is/as operator rewrite.

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            if (operand.ConstantValueOpt == ConstantValue.Null ||
                operand.Kind == BoundKind.MethodGroup ||
                inputType.IsVoidType())
            {
                // warning for cases where the result is always false:
                // (a) "null is TYPE" OR operand evaluates to null
                // (b) operand is a MethodGroup
                // (c) operand is of void type

                // NOTE:    Dev10 violates the SPEC for case (c) above and generates
                // NOTE:    an error ERR_NoExplicitBuiltinConv if the target type
                // NOTE:    is an open type. According to the specification, the result
                // NOTE:    is always false, but no compile time error occurs.
                // NOTE:    We follow the specification and generate WRN_IsAlwaysFalse
                // NOTE:    instead of an error.
                // NOTE:    See Test SyntaxBinderTests.TestIsOperatorWithTypeParameter

                Error(diagnostics, ErrorCode.WRN_IsAlwaysFalse, node, targetType);
                Conversion conv = Conversions.ClassifyConversionFromExpression(operand, targetType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);
                return new BoundIsOperator(node, operand, typeExpression, conv.Kind, resultType);
            }

            if (targetTypeKind == TypeKind.Dynamic)
            {
                // warning for dynamic target type
                Error(diagnostics, ErrorCode.WRN_IsDynamicIsConfusing,
                    node, node.OperatorToken.Text, targetType.Name,
                    GetSpecialType(SpecialType.System_Object, diagnostics, node).Name // a pretty way of getting the string "Object"
                    );
            }

            Debug.Assert((object)inputType != null);
            if (inputType.TypeKind == TypeKind.Dynamic)
            {
                // if operand has a dynamic type, we do the same thing as though it were an object
                inputType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
            }

            Conversion conversion = Conversions.ClassifyBuiltInConversion(inputType, targetType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);
            ReportIsOperatorDiagnostics(node, diagnostics, inputType, targetType, conversion.Kind, operand.ConstantValueOpt);
            return new BoundIsOperator(node, operand, typeExpression, conversion.Kind, resultType);

            bool tryBindAsType(
                ExpressionSyntax possibleType,
                BindingDiagnosticBag diagnostics,
                out BindingDiagnosticBag bindAsTypeDiagnostics,
                out BoundTypeExpression boundType)
            {
                bindAsTypeDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: diagnostics.AccumulatesDependencies);
                TypeWithAnnotations targetTypeWithAnnotations = BindType(possibleType, bindAsTypeDiagnostics, out AliasSymbol alias);
                TypeSymbol targetType = targetTypeWithAnnotations.Type;
                boundType = new BoundTypeExpression(possibleType, alias, targetTypeWithAnnotations);
                return !(targetType?.IsErrorType() == true && bindAsTypeDiagnostics.HasAnyResolvedErrors());
            }

        }

        private static void ReportIsOperatorDiagnostics(
            CSharpSyntaxNode syntax,
            BindingDiagnosticBag diagnostics,
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue)
        {
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions to generate warnings if the
            // NOTE:    expression will always be true/false/null.

            ConstantValue constantValue = GetIsOperatorConstantResult(operandType, targetType, conversionKind, operandConstantValue);
            if (constantValue != null)
            {
                if (constantValue.IsBad)
                {
                    Error(diagnostics, ErrorCode.ERR_BadBinaryOps, syntax, "is", operandType, targetType);
                }
                else
                {
                    Debug.Assert(constantValue == ConstantValue.True || constantValue == ConstantValue.False);

                    ErrorCode errorCode = constantValue == ConstantValue.True ? ErrorCode.WRN_IsAlwaysTrue : ErrorCode.WRN_IsAlwaysFalse;
                    Error(diagnostics, errorCode, syntax, targetType);
                }
            }
        }

        /// <summary>
        /// Possible return values:
        ///  - <see cref="ConstantValue.False"/>
        ///  - <see cref="ConstantValue.True"/>
        ///  - <see cref="ConstantValue.Bad"/> - compiler doesn't support the type check, i.e. cannot perform it, even at runtime
        ///  - 'null' value - result is not known at compile time    
        /// </summary>
        internal static ConstantValue GetIsOperatorConstantResult(
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue,
            bool operandCouldBeNull = true)
        {
            Debug.Assert((object)targetType != null);

            // SPEC:    The result of the operation depends on D and T as follows:
            // SPEC:    1)      If T is a reference type, the result is true if D and T are the same type, if D is a reference type and
            // SPEC:        an implicit reference conversion from D to T exists, or if D is a value type and a boxing conversion from D to T exists.
            // SPEC:    2)      If T is a nullable type, the result is true if D is the underlying type of T.
            // SPEC:    3)      If T is a non-nullable value type, the result is true if D and T are the same type.
            // SPEC:    4)      Otherwise, the result is false.

            // NOTE:    The language specification talks about the runtime evaluation of the is operation.
            // NOTE:    However, we are interested in computing the compile time constant value for the expression.
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions during binding to generate warnings
            // NOTE:    (always true/false/null) and during rewriting for optimized codegen.
            // NOTE:
            // NOTE:    Because the heuristic presented here is used to change codegen, it must be conservative. It is acceptable
            // NOTE:    for us to fail to report a warning in cases where humans could logically deduce that the operator will
            // NOTE:    always return false. It is not acceptable to inaccurately warn that the operator will always return false
            // NOTE:    if there are cases where it might succeed.
            // NOTE:
            // NOTE:    These same heuristics are also used in pattern-matching to determine if an expression of the form
            // NOTE:    `e is T x` is permitted. It is an error if `e` cannot be of type `T` according to this method
            // NOTE:    returning ConstantValue.False.
            // NOTE:    The heuristics are also used to determine if a `case T1 x1:` is subsumed by
            // NOTE:    some previous `case T2 x2:` in a switch statement. For that purpose operandType is T1, targetType is T2,
            // NOTE:    and operandCouldBeNull is false; the former subsumes the latter if this method returns ConstantValue.True.
            // NOTE:    Since the heuristic is now used to produce errors in pattern-matching, making it more accurate in the
            // NOTE:    future could be a breaking change.

            // To begin our heuristic: if the operand is literal null then we automatically return that the
            // result is false. You might think that we can simply check to see if the conversion is
            // ConversionKind.NullConversion, but "null is T" for a type parameter T is actually classified
            // as an implicit reference conversion if T is constrained to reference types. Rather
            // than deal with all those special cases we can simply bail out here.

            if (operandConstantValue == ConstantValue.Null)
            {
                return ConstantValue.False;
            }

            Debug.Assert((object)operandType != null);

            operandCouldBeNull =
                operandCouldBeNull &&
                operandType.CanContainNull() && // a non-nullable value type is never null
                (operandConstantValue == null || operandConstantValue == ConstantValue.Null); // a non-null constant is never null

            switch (conversionKind)
            {
                case ConversionKind.ImplicitSpan:
                case ConversionKind.ExplicitSpan:
                case ConversionKind.NoConversion:
                    // Oddly enough, "x is T" can be true even if there is no conversion from x to T!
                    //
                    // Scenario 1: Type parameter compared to System.Enum.
                    //
                    // bool M1<X>(X x) where X : struct { return x is Enum; }
                    //
                    // There is no conversion from X to Enum, not even an explicit conversion. But
                    // nevertheless, X could be constructed as an enumerated type.
                    // However, we can sometimes know that the result will be false.
                    //
                    // Scenario 2a: Constrained type parameter compared to reference type.
                    //
                    // bool M2a<X>(X x) where X : struct { return x is string; }
                    //
                    // We know that X, constrained to struct, will never be string.
                    //
                    // Scenario 2b: Reference type compared to constrained type parameter.
                    //
                    // bool M2b<X>(string x) where X : struct { return x is X; }
                    //
                    // We know that string will never be X, constrained to struct.
                    //
                    // Scenario 3: Value type compared to type parameter.
                    //
                    // bool M3<T>(int x) { return x is T; }
                    //
                    // There is no conversion from int to T, but T could nevertheless be int.
                    //
                    // Scenario 4: Constructed type compared to open type
                    //
                    // bool M4<T>(C<int> x) { return x is C<T>; }
                    //
                    // There is no conversion from C<int> to C<T>, but nevertheless, T might be int.
                    //
                    // Scenario 5: Open type compared to constructed type:
                    //
                    // bool M5<X>(C<X> x) { return x is C<int>);
                    //
                    // Again, X could be int.
                    //
                    // We could then go on to get more complicated. For example,
                    //
                    // bool M6<X>(C<X> x) where X : struct { return x is C<string>; }
                    //
                    // We know that C<X> is never convertible to C<string> no matter what
                    // X is. Or:
                    //
                    // bool M7<T>(Dictionary<int, int> x) { return x is List<T>; }
                    //
                    // We know that no matter what T is, the conversion will never succeed.
                    //
                    // As noted above, we must be conservative. We follow the lead of the native compiler,
                    // which uses the following algorithm:
                    //
                    // * If neither type is open and there is no conversion then the result is always false:

                    if (!operandType.ContainsTypeParameter() && !targetType.ContainsTypeParameter())
                    {
                        return ConstantValue.False;
                    }

                    // * Otherwise, at least one of them is of an open type. If the operand is of value type
                    //   and the target is a class type other than System.Enum, or vice versa, then we are
                    //   in scenario 2, not scenario 1, and can correctly deduce that the result is false.

                    if (operandType.IsValueType && targetType.IsClassType() && targetType.SpecialType != SpecialType.System_Enum ||
                        targetType.IsValueType && operandType.IsClassType() && operandType.SpecialType != SpecialType.System_Enum)
                    {
                        return ConstantValue.False;
                    }

                    // * If either type is a restricted type, the type check isn't supported for some scenarios because
                    //   a restricted type cannot be boxed or unboxed into.
                    if (targetType.IsRestrictedType() || operandType.IsRestrictedType())
                    {
                        if (targetType is TypeParameterSymbol { AllowsRefLikeType: true })
                        {
                            if (!operandType.IsErrorOrRefLikeOrAllowsRefLikeType())
                            {
                                return null;
                            }
                        }
                        else if (operandType is not TypeParameterSymbol { AllowsRefLikeType: true })
                        {
                            if (targetType.IsRefLikeType)
                            {
                                if (operandType is TypeParameterSymbol)
                                {
                                    Debug.Assert(operandType is TypeParameterSymbol { AllowsRefLikeType: false });
                                    return ConstantValue.False;
                                }
                            }
                            else if (operandType.IsRefLikeType)
                            {
                                if (targetType is TypeParameterSymbol)
                                {
                                    Debug.Assert(targetType is TypeParameterSymbol { AllowsRefLikeType: false });
                                    return ConstantValue.False;
                                }
                            }
                        }

                        return ConstantValue.Bad;
                    }

                    // * Otherwise, we give up. Though there are other situations in which we can deduce that
                    //   the result will always be false, such as scenarios 6 and 7, but we do not attempt
                    //   to deduce this.

                    // CONSIDER: we could use TypeUnification.CanUnify to do additional compile-time checking.

                    return null;

                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                case ConversionKind.ImplicitEnumeration:
                // case ConversionKind.ExplicitEnumeration: // Handled separately below.
                case ConversionKind.ImplicitConstant:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.IntPtr:
                case ConversionKind.ExplicitTuple:
                case ConversionKind.ImplicitTuple:

                    // Consider all the cases where we know that "x is T" must be false just from
                    // the conversion classification.
                    //
                    // If we have "x is T" and the conversion from x to T is numeric or enum then the result must be false.
                    //
                    // If we have "null is T" then obviously that must be false.
                    //
                    // If we have "1 is long" then that must be false. (If we have "1 is int" then it is an identity conversion,
                    // not an implicit constant conversion.
                    //
                    // User-defined and IntPtr conversions are always false for "is".

                    return ConstantValue.False;

                case ConversionKind.ExplicitEnumeration:
                    // Enum-to-enum conversions should be treated the same as unsuccessful struct-to-struct
                    // conversions (i.e. make allowances for type unification, etc)
                    if (operandType.IsEnumType() && targetType.IsEnumType())
                    {
                        goto case ConversionKind.NoConversion;
                    }

                    return ConstantValue.False;

                case ConversionKind.ExplicitNullable:

                    // An explicit nullable conversion is a conversion of one of the following forms:
                    //
                    // 1) X? --> Y?, where X --> Y is an explicit conversion.  (If X --> Y is an implicit
                    //    conversion then X? --> Y? is an implicit nullable conversion.) In this case we
                    //    know that "X? is Y?" must be false because either X? is null, or we have an
                    //    explicit conversion from struct type X to struct type Y, and so X is never of type Y.)
                    //
                    // 2) X --> Y?, where again, X --> Y is an explicit conversion. By the same reasoning
                    //    as in case 1, this must be false.

                    if (targetType.IsNullableType())
                    {
                        return ConstantValue.False;
                    }

                    Debug.Assert(operandType.IsNullableType());

                    // 3) X? --> X. In this case, this is just a different way of writing "x != null".
                    //    We only know what the result will be if the input is known not to be null.
                    if (Conversions.HasIdentityConversion(operandType.GetNullableUnderlyingType(), targetType))
                    {
                        return operandCouldBeNull ? null : ConstantValue.True;
                    }

                    // 4) X? --> Y where the conversion X --> Y is an implicit or explicit value type conversion.
                    //    "X? is Y" again must be false.

                    return ConstantValue.False;

                case ConversionKind.ImplicitReference:
                    return operandCouldBeNull ? null : ConstantValue.True;

                case ConversionKind.ExplicitReference:
                case ConversionKind.Unboxing:
                    // In these three cases, the expression type must be a reference type. Therefore,
                    // the result cannot be determined. The expression could be null or of the wrong type,
                    // resulting in false, or it could be a non-null reference to the appropriate type,
                    // resulting in true.
                    return null;

                case ConversionKind.Identity:
                    // The result of "x is T" can be statically determined to be true if x is an expression
                    // of non-nullable value type T. If x is of reference or nullable value type then
                    // we cannot know, because again, the expression value could be null or it could be good.
                    // If it is of pointer type then we have already given an error.
                    return operandCouldBeNull ? null : ConstantValue.True;

                case ConversionKind.Boxing:

                    // A boxing conversion might be a conversion:
                    //
                    // * From a non-nullable value type to a reference type
                    // * From a nullable value type to a reference type
                    // * From a type parameter that *could* be a value type under construction
                    //   to a reference type
                    //
                    // In the first case we know that the conversion will always succeed and that the
                    // operand is never null, and therefore "is" will always result in true.
                    //
                    // In the second two cases we do not know; either the nullable value type could be
                    // null, or the type parameter could be constructed with a reference type, and it
                    // could be null.
                    return operandCouldBeNull ? null : ConstantValue.True;

                case ConversionKind.ImplicitNullable:
                    // We have "x is T" in one of the following situations:
                    // 1) x is of type X and T is X?.  The value is always true.
                    // 2) x is of type X and T is Y? where X is convertible to Y via an implicit numeric conversion. Eg,
                    //    x is of type int and T is decimal?.  The value is always false.
                    // 3) x is of type X? and T is Y? where X is convertible to Y via an implicit numeric conversion.
                    //    The value is always false.

                    Debug.Assert(targetType.IsNullableType());
                    return operandType.Equals(targetType.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions)
                        ? ConstantValue.True : ConstantValue.False;

                default:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ExplicitDynamic:
                case ConversionKind.ExplicitPointerToInteger:
                case ConversionKind.ExplicitPointerToPointer:
                case ConversionKind.ImplicitPointerToVoid:
                case ConversionKind.ExplicitIntegerToPointer:
                case ConversionKind.ImplicitNullToPointer:
                case ConversionKind.AnonymousFunction:
                case ConversionKind.NullLiteral:
                case ConversionKind.DefaultLiteral:
                case ConversionKind.MethodGroup:
                case ConversionKind.Union:
                    // We've either replaced Dynamic with Object, or already bailed out with an error.
                    throw ExceptionUtilities.UnexpectedValue(conversionKind);
            }
        }

        private BoundExpression BindAsOperator(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var operand = BindRValueWithoutTargetType(node.Left, diagnostics);
            AliasSymbol alias;
            TypeWithAnnotations targetTypeWithAnnotations = BindType(node.Right, diagnostics, out alias);
            TypeSymbol targetType = targetTypeWithAnnotations.Type;
            var typeExpression = new BoundTypeExpression(node.Right, alias, targetTypeWithAnnotations);
            var targetTypeKind = targetType.TypeKind;
            var resultType = targetType;

            // Is and As operator should have null ConstantValue as they are not constant expressions.
            // However we perform analysis of is/as expressions at bind time to detect if the expression
            // will always evaluate to a constant to generate warnings (always true/false/null).
            // We also need this analysis result during rewrite to optimize away redundant isinst instructions.
            // We store the conversion kind from expression's operand type to target type to enable these
            // optimizations during is/as operator rewrite.

            switch (operand.Kind)
            {
                case BoundKind.UnboundLambda:
                case BoundKind.Lambda:
                case BoundKind.MethodGroup:  // New in Roslyn - see DevDiv #864740.
                    // operand for an is or as expression cannot be a lambda expression or method group
                    if (!operand.HasAnyErrors)
                    {
                        Error(diagnostics, ErrorCode.ERR_LambdaInIsAs, node);
                    }

                    return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder: null, operandConversion: null, resultType, hasErrors: true);

                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    if ((object)operand.Type == null)
                    {
                        Error(diagnostics, ErrorCode.ERR_TypelessTupleInAs, node);
                        return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder: null, operandConversion: null, resultType, hasErrors: true);
                    }
                    break;
            }

            if (operand.HasAnyErrors || targetTypeKind == TypeKind.Error)
            {
                // If either operand is bad or target type has errors, bail out preventing more cascading errors.
                return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder: null, operandConversion: null, resultType, hasErrors: true);
            }

            if (targetType.IsReferenceType && targetTypeWithAnnotations.NullableAnnotation.IsAnnotated())
            {
                Error(diagnostics, ErrorCode.ERR_AsNullableType, node.Right, targetType);

                return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder: null, operandConversion: null, resultType, hasErrors: true);
            }
            else if (!targetType.IsReferenceType && !targetType.IsNullableType())
            {
                // SPEC:    In an operation of the form E as T, E must be an expression and T must be a
                // SPEC:    reference type, a type parameter known to be a reference type, or a nullable type.
                if (targetTypeKind == TypeKind.TypeParameter)
                {
                    Error(diagnostics, ErrorCode.ERR_AsWithTypeVar, node, targetType);
                }
                else if (targetTypeKind == TypeKind.Pointer || targetTypeKind == TypeKind.FunctionPointer)
                {
                    Error(diagnostics, ErrorCode.ERR_PointerInAsOrIs, node);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_AsMustHaveReferenceType, node, targetType);
                }

                return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder: null, operandConversion: null, resultType, hasErrors: true);
            }

            // The C# specification states in the section called
            // "Referencing Static Class Types" that it is always
            // illegal to use "as" with a static type. The
            // native compiler actually allows "null as C" for
            // a static type C to be an expression of type C.
            // It also allows "someObject as C" if "someObject"
            // is of type object. To retain compatibility we
            // allow it, but when /warn:5 or higher we break with the native
            // compiler and turn this into a warning.
            if (targetType.IsStatic)
            {
                Error(diagnostics, ErrorCode.WRN_StaticInAsOrIs, node, targetType);
            }

            BoundValuePlaceholder operandPlaceholder;
            BoundExpression operandConversion;

            if (operand.IsLiteralNull())
            {
                // We do not want to warn for the case "null as TYPE" where the null
                // is a literal, because the user might be saying it to cause overload resolution
                // to pick a particular method
                Debug.Assert(operand.Type is null);
                operandPlaceholder = new BoundValuePlaceholder(operand.Syntax, operand.Type).MakeCompilerGenerated();
                operandConversion = CreateConversion(node, operandPlaceholder,
                                                     Conversion.NullLiteral,
                                                     isCast: false, conversionGroupOpt: null, resultType, diagnostics);

                return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder, operandConversion, resultType);
            }

            if (operand.IsLiteralDefault())
            {
                operand = new BoundDefaultExpression(operand.Syntax, targetType: null, constantValueOpt: ConstantValue.Null,
                    type: GetSpecialType(SpecialType.System_Object, diagnostics, node));
            }

            var operandType = operand.Type;
            Debug.Assert((object)operandType != null);
            var operandTypeKind = operandType.TypeKind;

            Debug.Assert(!targetType.IsPointerOrFunctionPointer(), "Should have been caught above");
            if (operandType.IsPointerOrFunctionPointer())
            {
                // operand for an is or as expression cannot be of pointer type
                Error(diagnostics, ErrorCode.ERR_PointerInAsOrIs, node);
                return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder: null, operandConversion: null, resultType, hasErrors: true);
            }

            if (operandTypeKind == TypeKind.Dynamic)
            {
                // if operand has a dynamic type, we do the same thing as though it were an object
                operandType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
                operandTypeKind = operandType.TypeKind;
            }

            if (targetTypeKind == TypeKind.Dynamic)
            {
                // for "as dynamic", we do the same thing as though it were an "as object"
                targetType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
                targetTypeKind = targetType.TypeKind;
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = Conversions.ClassifyBuiltInConversion(operandType, targetType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);
            bool hasErrors = ReportAsOperatorConversionDiagnostics(node, diagnostics, this.Compilation, operandType, targetType, conversion.Kind, operand.ConstantValueOpt);

            if (conversion.Exists)
            {
                operandPlaceholder = new BoundValuePlaceholder(operand.Syntax, operand.Type).MakeCompilerGenerated();
                operandConversion = CreateConversion(node, operandPlaceholder,
                                                     conversion,
                                                     isCast: false, conversionGroupOpt: null, resultType, diagnostics);
            }
            else
            {
                operandPlaceholder = null;
                operandConversion = null;
            }

            return new BoundAsOperator(node, operand, typeExpression, operandPlaceholder, operandConversion, resultType, hasErrors);
        }

        private static bool ReportAsOperatorConversionDiagnostics(
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            CSharpCompilation compilation,
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue)
        {
            // SPEC:    In an operation of the form E as T, E must be an expression and T must be a reference type,
            // SPEC:    a type parameter known to be a reference type, or a nullable type.
            // SPEC:    Furthermore, at least one of the following must be true, or otherwise a compile-time error occurs:
            // SPEC:    •	An identity (§6.1.1), implicit nullable (§6.1.4), implicit reference (§6.1.6), boxing (§6.1.7),
            // SPEC:        explicit nullable (§6.2.3), explicit reference (§6.2.4), or unboxing (§6.2.5) conversion exists
            // SPEC:        from E to T.
            // SPEC:    •	The type of E or T is an open type.
            // SPEC:    •	E is the null literal.

            // SPEC VIOLATION:  The specification contains an error in the list of legal conversions above.
            // SPEC VIOLATION:  If we have "class C<T, U> where T : U where U : class" then there is
            // SPEC VIOLATION:  an implicit conversion from T to U, but it is not an identity, reference or
            // SPEC VIOLATION:  boxing conversion. It will be one of those at runtime, but at compile time
            // SPEC VIOLATION:  we do not know which, and therefore cannot classify it as any of those.
            // SPEC VIOLATION:  See Microsoft.CodeAnalysis.CSharp.UnitTests.SyntaxBinderTests.TestAsOperator_SpecErrorCase() test for an example.

            // SPEC VIOLATION:  The specification also unintentionally allows the case where requirement 2 above:
            // SPEC VIOLATION:  "The type of E or T is an open type" is true, but type of E is void type, i.e. T is an open type.
            // SPEC VIOLATION:  Dev10 compiler correctly generates an error for this case and we will maintain compatibility.

            bool hasErrors = false;
            switch (conversionKind)
            {
                case ConversionKind.ImplicitReference:
                case ConversionKind.Boxing:
                case ConversionKind.ImplicitNullable:
                case ConversionKind.Identity:
                case ConversionKind.ExplicitNullable:
                case ConversionKind.ExplicitReference:
                case ConversionKind.Unboxing:
                    break;

                default:
                    // Generate an error if there is no possible legal conversion and both the operandType
                    // and the targetType are closed types OR operandType is void type, otherwise we need a runtime check
                    if (!operandType.ContainsTypeParameter() && !targetType.ContainsTypeParameter() ||
                        operandType.IsVoidType())
                    {
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, operandType, targetType);
                        Error(diagnostics, ErrorCode.ERR_NoExplicitBuiltinConv, node, distinguisher.First, distinguisher.Second);
                        hasErrors = true;
                    }

                    break;
            }

            if (!hasErrors)
            {
                ReportAsOperatorDiagnostics(node, diagnostics, operandType, targetType, conversionKind, operandConstantValue);
            }

            return hasErrors;
        }

        private static void ReportAsOperatorDiagnostics(
            CSharpSyntaxNode node,
            BindingDiagnosticBag diagnostics,
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue)
        {
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions to generate warnings if the
            // NOTE:    expression will always be true/false/null.

            ConstantValue constantValue = GetAsOperatorConstantResult(operandType, targetType, conversionKind, operandConstantValue);
            if (constantValue != null)
            {
                if (constantValue.IsBad)
                {
                    Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, "as", operandType, targetType);
                }
                else
                {
                    Debug.Assert(constantValue.IsNull);
                    Error(diagnostics, ErrorCode.WRN_AlwaysNull, node, targetType);
                }
            }
        }

        /// <summary>
        /// Possible return values:
        ///  - <see cref="ConstantValue.Null"/>
        ///  - <see cref="ConstantValue.Bad"/> - compiler doesn't support the type check, i.e. cannot perform it, even at runtime
        ///  - 'null' value - result is not known at compile time    
        /// </summary>
        internal static ConstantValue GetAsOperatorConstantResult(TypeSymbol operandType, TypeSymbol targetType, ConversionKind conversionKind, ConstantValue operandConstantValue)
        {
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions during binding to generate warnings (always true/false/null)
            // NOTE:    and during rewriting for optimized codegen.

            ConstantValue isOperatorConstantResult = GetIsOperatorConstantResult(operandType, targetType, conversionKind, operandConstantValue);
            if (isOperatorConstantResult != null)
            {
                if (isOperatorConstantResult.IsBad)
                {
                    return isOperatorConstantResult;
                }

                if (!isOperatorConstantResult.BooleanValue)
                {
                    if (operandType?.IsRefLikeType == true)
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Null;
                }
            }

            return null;
        }

        private BoundExpression GenerateNullCoalescingBadBinaryOpsError(BinaryExpressionSyntax node, BoundExpression leftOperand, BoundExpression rightOperand, BindingDiagnosticBag diagnostics)
        {
            Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, SyntaxFacts.GetText(node.OperatorToken.Kind()), leftOperand.Display, rightOperand.Display);

            leftOperand = BindToTypeForErrorRecovery(leftOperand);
            rightOperand = BindToTypeForErrorRecovery(rightOperand);
            return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                leftPlaceholder: null, leftConversion: null, BoundNullCoalescingOperatorResultKind.NoCommonType, @checked: CheckOverflowAtRuntime, CreateErrorType(), hasErrors: true);
        }

        private BoundExpression BindNullCoalescingOperator(BinaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var leftOperand = BindValue(node.Left, diagnostics, BindValueKind.RValue);
            leftOperand = BindToNaturalType(leftOperand, diagnostics);
            var rightOperand = BindValue(node.Right, diagnostics, BindValueKind.RValue);

            // If either operand is bad, bail out preventing more cascading errors
            if (leftOperand.HasAnyErrors || rightOperand.HasAnyErrors)
            {
                leftOperand = BindToTypeForErrorRecovery(leftOperand);
                rightOperand = BindToTypeForErrorRecovery(rightOperand);
                return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                    leftPlaceholder: null, leftConversion: null, BoundNullCoalescingOperatorResultKind.NoCommonType, @checked: CheckOverflowAtRuntime, CreateErrorType(), hasErrors: true);
            }

            // The specification does not permit the left hand side to be a default literal
            if (leftOperand.IsLiteralDefault())
            {
                Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, node, node.OperatorToken.Text, "default");

                return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                    leftPlaceholder: null, leftConversion: null, BoundNullCoalescingOperatorResultKind.NoCommonType, @checked: CheckOverflowAtRuntime, CreateErrorType(), hasErrors: true);
            }

            // SPEC: The type of the expression a ?? b depends on which implicit conversions are available
            // SPEC: between the types of the operands. In order of preference, the type of a ?? b is A0, A, or B,
            // SPEC: where A is the type of a, B is the type of b (provided that b has a type),
            // SPEC: and A0 is the underlying type of A if A is a nullable type, or A otherwise.

            TypeSymbol optLeftType = leftOperand.Type;   // "A"
            TypeSymbol optRightType = rightOperand.Type; // "B"
            bool isLeftNullable = (object)optLeftType != null && optLeftType.IsNullableType();
            TypeSymbol optLeftType0 = isLeftNullable ?  // "A0"
                optLeftType.GetNullableUnderlyingType() :
                optLeftType;

            // SPEC: The left hand side must be either the null literal or it must have a type. Lambdas and method groups do not have a type,
            // SPEC: so using one is an error.
            if (leftOperand.Kind == BoundKind.UnboundLambda || leftOperand.Kind == BoundKind.MethodGroup)
            {
                return GenerateNullCoalescingBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
            }

            // SPEC: Otherwise, if A exists and is a non-nullable value type, a compile-time error occurs. First we check for the pre-C# 8.0
            // SPEC: condition, to ensure that we don't allow previously illegal code in old language versions.
            if ((object)optLeftType != null && !optLeftType.IsReferenceType && !isLeftNullable)
            {
                // Prior to C# 8.0, the spec said that the left type must be either a reference type or a nullable value type. This was relaxed
                // with C# 8.0, so if the feature is not enabled then issue a diagnostic and return
                if (!optLeftType.IsValueType)
                {
                    CheckFeatureAvailability(node, MessageID.IDS_FeatureUnconstrainedTypeParameterInNullCoalescingOperator, diagnostics);
                }
                else
                {
                    return GenerateNullCoalescingBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
                }
            }

            // SPEC:    If b is a dynamic expression, the result is dynamic. At runtime, a is first
            // SPEC:    evaluated. If a is not null, a is converted to a dynamic type, and this becomes
            // SPEC:    the result. Otherwise, b is evaluated, and the outcome becomes the result.
            //
            // Note that there is no runtime dynamic dispatch since comparison with null is not a dynamic operation.
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            if ((object)optRightType != null && optRightType.IsDynamic())
            {
                var leftPlaceholder = new BoundValuePlaceholder(leftOperand.Syntax, optLeftType).MakeCompilerGenerated();
                var objectType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
                var leftConversion = CreateConversion(node, leftPlaceholder,
                                                      Conversions.ClassifyConversionFromExpression(leftOperand, objectType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo),
                                                      isCast: false, conversionGroupOpt: null, objectType, diagnostics);

                rightOperand = BindToNaturalType(rightOperand, diagnostics);
                diagnostics.Add(node, useSiteInfo);
                return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                    leftPlaceholder, leftConversion, BoundNullCoalescingOperatorResultKind.RightDynamicType, @checked: CheckOverflowAtRuntime, optRightType);
            }

            // SPEC:    Otherwise, if A exists and is a nullable type and an implicit conversion exists from b to A0,
            // SPEC:    the result type is A0. At run-time, a is first evaluated. If a is not null,
            // SPEC:    a is unwrapped to type A0, and this becomes the result.
            // SPEC:    Otherwise, b is evaluated and converted to type A0, and this becomes the result.

            if (isLeftNullable)
            {
                var rightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, optLeftType0, ref useSiteInfo);
                if (rightConversion.Exists)
                {
                    var leftPlaceholder = new BoundValuePlaceholder(leftOperand.Syntax, optLeftType0).MakeCompilerGenerated();
                    diagnostics.Add(node, useSiteInfo);
                    var convertedRightOperand = CreateConversion(rightOperand, rightConversion, optLeftType0, diagnostics);
                    // Note: we use an identity conversion for LHS and let lowering get 'a0' from 'a' with GetValueOrDefault
                    return new BoundNullCoalescingOperator(node, leftOperand, convertedRightOperand,
                        leftPlaceholder, leftConversion: leftPlaceholder, BoundNullCoalescingOperatorResultKind.LeftUnwrappedType, @checked: CheckOverflowAtRuntime, optLeftType0);
                }
            }

            // SPEC:    Otherwise, if A exists and an implicit conversion exists from b to A, the result type is A.
            // SPEC:    At run-time, a is first evaluated. If a is not null, a becomes the result.
            // SPEC:    Otherwise, b is evaluated and converted to type A, and this becomes the result.

            if ((object)optLeftType != null)
            {
                var rightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, optLeftType, ref useSiteInfo);
                if (rightConversion.Exists)
                {
                    var convertedRightOperand = CreateConversion(rightOperand, rightConversion, optLeftType, diagnostics);
                    var leftPlaceholder = new BoundValuePlaceholder(leftOperand.Syntax, optLeftType).MakeCompilerGenerated();
                    diagnostics.Add(node, useSiteInfo);
                    return new BoundNullCoalescingOperator(node, leftOperand, convertedRightOperand,
                        leftPlaceholder, leftConversion: leftPlaceholder, BoundNullCoalescingOperatorResultKind.LeftType, @checked: CheckOverflowAtRuntime, optLeftType);
                }
            }

            // SPEC:    Otherwise, if b has a type B and an implicit conversion exists from a to B,
            // SPEC:    the result type is B. At run-time, a is first evaluated. If a is not null,
            // SPEC:    a is unwrapped to type A0 (if A exists and is nullable) and converted to type B,
            // SPEC:    and this becomes the result. Otherwise, b is evaluated and becomes the result.

            // SPEC VIOLATION:  Native compiler violates the specification here and implements this part based on
            // SPEC VIOLATION:  whether A is a nullable type or not.
            // SPEC VIOLATION:  We will maintain compatibility with the native compiler and do the same.
            // SPEC VIOLATION:  Following SPEC PROPOSAL states the current implementations in both compilers:

            // SPEC PROPOSAL:    Otherwise, if A exists and is a nullable type and if b has a type B and
            // SPEC PROPOSAL:    an implicit conversion exists from A0 to B, the result type is B.
            // SPEC PROPOSAL:    At run-time, a is first evaluated. If a is not null, a is unwrapped to type A0
            // SPEC PROPOSAL:    and converted to type B, and this becomes the result.
            // SPEC PROPOSAL:    Otherwise, b is evaluated and becomes the result.

            // SPEC PROPOSAL:    Otherwise, if A does not exist or is a non-nullable type and if b has a type B and
            // SPEC PROPOSAL:    an implicit conversion exists from a to B, the result type is B.
            // SPEC PROPOSAL:    At run-time, a is first evaluated. If a is not null, a is converted to type B,
            // SPEC PROPOSAL:    and this becomes the result. Otherwise, b is evaluated and becomes the result.

            // See test CodeGenTests.TestNullCoalescingOperatorWithNullableConversions for an example.

            if ((object)optRightType != null)
            {
                rightOperand = BindToNaturalType(rightOperand, diagnostics);
                Conversion leftConversionClassification;
                BoundNullCoalescingOperatorResultKind resultKind;

                if (isLeftNullable)
                {
                    // This is the SPEC VIOLATION case.
                    // Note that at runtime we need two conversions on the left operand:
                    //      1) Explicit nullable conversion from leftOperand to optLeftType0 and
                    //      2) Implicit conversion from optLeftType0 to optRightType.
                    // We just store the second conversion in the bound node and insert the first conversion during rewriting
                    // the null coalescing operator. See method LocalRewriter.GetConvertedLeftForNullCoalescingOperator.

                    leftConversionClassification = Conversions.ClassifyImplicitConversionFromType(optLeftType0, optRightType, ref useSiteInfo);
                    resultKind = BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType;

                    if (leftConversionClassification.Exists)
                    {
                        var leftPlaceholder = new BoundValuePlaceholder(leftOperand.Syntax, optLeftType0).MakeCompilerGenerated();
                        var leftConversion = CreateConversion(node, leftPlaceholder, leftConversionClassification, isCast: false, conversionGroupOpt: null, optRightType, diagnostics);

                        diagnostics.Add(node, useSiteInfo);
                        return new BoundNullCoalescingOperator(node, leftOperand, rightOperand, leftPlaceholder, leftConversion, resultKind, @checked: CheckOverflowAtRuntime, optRightType);
                    }
                }
                else
                {
                    leftConversionClassification = Conversions.ClassifyImplicitConversionFromExpression(leftOperand, optRightType, ref useSiteInfo);
                    resultKind = BoundNullCoalescingOperatorResultKind.RightType;

                    if (leftConversionClassification.Exists)
                    {
                        var leftPlaceholder = new BoundValuePlaceholder(leftOperand.Syntax, optLeftType).MakeCompilerGenerated();
                        var leftConversion = CreateConversion(node, leftPlaceholder, leftConversionClassification, isCast: false, conversionGroupOpt: null, optRightType, diagnostics);

                        diagnostics.Add(node, useSiteInfo);
                        return new BoundNullCoalescingOperator(node, leftOperand, rightOperand, leftPlaceholder, leftConversion, resultKind, @checked: CheckOverflowAtRuntime, optRightType);
                    }
                }
            }

            // SPEC:    Otherwise, a and b are incompatible, and a compile-time error occurs.
            diagnostics.Add(node, useSiteInfo);
            return GenerateNullCoalescingBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
        }

        private BoundExpression BindNullCoalescingAssignmentOperator(AssignmentExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureCoalesceAssignmentExpression.CheckFeatureAvailability(diagnostics, node.OperatorToken);

            BoundExpression leftOperand = BindValue(node.Left, diagnostics, BindValueKind.CompoundAssignment);
            ReportSuppressionIfNeeded(leftOperand, diagnostics);
            BoundExpression rightOperand = BindValue(node.Right, diagnostics, BindValueKind.RValue);

            // Prevent more cascading errors if there are any on either operand
            if (leftOperand.HasAnyErrors || rightOperand.HasAnyErrors)
            {
                diagnostics = BindingDiagnosticBag.Discarded;
            }

            // Given a ??= b, the type of a is A, the type of B is b, and if A is a nullable value type, the underlying
            // non-nullable value type of A is A0.
            TypeSymbol leftType = leftOperand.Type;
            Debug.Assert((object)leftType != null);

            // If A is a non-nullable value type, a compile-time error occurs
            if (leftType.IsValueType && !leftType.IsNullableType())
            {
                return GenerateNullCoalescingAssignmentBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            // If A0 exists and B is implicitly convertible to A0, then the result type of this expression is A0, except if B is dynamic.
            // This differs from most assignments such that you cannot directly replace a with (a ??= b).
            // The exception for dynamic is called out in the spec, it's the same behavior that ?? has with respect to dynamic.
            if (leftType.IsNullableType())
            {
                var underlyingLeftType = leftType.GetNullableUnderlyingType();
                var underlyingRightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, underlyingLeftType, ref useSiteInfo);
                if (underlyingRightConversion.Exists && rightOperand.Type?.IsDynamic() != true)
                {
                    diagnostics.Add(node, useSiteInfo);
                    var convertedRightOperand = CreateConversion(rightOperand, underlyingRightConversion, underlyingLeftType, diagnostics);
                    return new BoundNullCoalescingAssignmentOperator(node, leftOperand, convertedRightOperand, underlyingLeftType);
                }
            }

            // If an implicit conversion exists from B to A, we store that conversion. At runtime, a is first evaluated. If
            // a is not null, b is not evaluated. If a is null, b is evaluated and converted to type A, and is stored in a.
            // Reset useSiteDiagnostics because they could have been used populated incorrectly from attempting to bind
            // as the nullable underlying value type case.
            useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(useSiteInfo);
            var rightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, leftType, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);
            if (rightConversion.Exists)
            {
                var convertedRightOperand = CreateConversion(rightOperand, rightConversion, leftType, diagnostics);
                return new BoundNullCoalescingAssignmentOperator(node, leftOperand, convertedRightOperand, leftType);
            }

            // a and b are incompatible and a compile-time error occurs
            return GenerateNullCoalescingAssignmentBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
        }

        private BoundExpression GenerateNullCoalescingAssignmentBadBinaryOpsError(AssignmentExpressionSyntax node, BoundExpression leftOperand, BoundExpression rightOperand, BindingDiagnosticBag diagnostics)
        {
            Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, SyntaxFacts.GetText(node.OperatorToken.Kind()), leftOperand.Display, rightOperand.Display);
            leftOperand = BindToTypeForErrorRecovery(leftOperand);
            rightOperand = BindToTypeForErrorRecovery(rightOperand);
            return new BoundNullCoalescingAssignmentOperator(node, leftOperand, rightOperand, CreateErrorType(), hasErrors: true);
        }

        /// <remarks>
        /// From ExpressionBinder::EnsureQMarkTypesCompatible:
        ///
        /// The v2.0 specification states that the types of the second and third operands T and S of a conditional operator
        /// must be TT and TS such that either (a) TT==TS, or (b), TT->TS or TS->TT but not both.
        ///
        /// Unfortunately that is not what we implemented in v2.0.  Instead, we implemented
        /// that either (a) TT=TS or (b) T->TS or S->TT but not both.  That is, we looked at the
        /// convertibility of the expressions, not the types.
        ///
        ///
        /// Changing that to the algorithm in the standard would be a breaking change.
        ///
        /// b ? (Func&lt;int&gt;)(delegate(){return 1;}) : (delegate(){return 2;})
        ///
        /// and
        ///
        /// b ? 0 : myenum
        ///
        /// would suddenly stop working.  (The first because o2 has no type, the second because 0 goes to
        /// any enum but enum doesn't go to int.)
        ///
        /// It gets worse.  We would like the 3.0 language features which require type inference to use
        /// a consistent algorithm, and that furthermore, the algorithm be smart about choosing the best
        /// of a set of types.  However, the language committee has decided that this algorithm will NOT
        /// consume information about the convertibility of expressions. Rather, it will gather up all
        /// the possible types and then pick the "largest" of them.
        ///
        /// To maintain backwards compatibility while still participating in the spirit of consistency,
        /// we implement an algorithm here which picks the type based on expression convertibility, but
        /// if there is a conflict, then it chooses the larger type rather than producing a type error.
        /// This means that b?0:myshort will have type int rather than producing an error (because 0->short,
        /// myshort->int).
        /// </remarks>
        private BoundExpression BindConditionalOperator(ConditionalExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var whenTrue = node.WhenTrue.CheckAndUnwrapRefExpression(diagnostics, out var whenTrueRefKind);
            var whenFalse = node.WhenFalse.CheckAndUnwrapRefExpression(diagnostics, out var whenFalseRefKind);

            var isRef = whenTrueRefKind == RefKind.Ref && whenFalseRefKind == RefKind.Ref;
            if (!isRef)
            {
                if (whenFalseRefKind == RefKind.Ref)
                {
                    diagnostics.Add(ErrorCode.ERR_RefConditionalNeedsTwoRefs, whenFalse.GetFirstToken().GetLocation());
                }

                if (whenTrueRefKind == RefKind.Ref)
                {
                    diagnostics.Add(ErrorCode.ERR_RefConditionalNeedsTwoRefs, whenTrue.GetFirstToken().GetLocation());
                }
            }
            else
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureRefConditional, diagnostics);
            }

            return isRef ? BindRefConditionalOperator(node, whenTrue, whenFalse, diagnostics) : BindValueConditionalOperator(node, whenTrue, whenFalse, diagnostics);
        }

#nullable enable
        private BoundExpression BindValueConditionalOperator(ConditionalExpressionSyntax node, ExpressionSyntax whenTrue, ExpressionSyntax whenFalse, BindingDiagnosticBag diagnostics)
        {
            BoundExpression condition = BindBooleanExpression(node.Condition, diagnostics);
            BoundExpression trueExpr = BindValue(whenTrue, diagnostics, BindValueKind.RValue);
            BoundExpression falseExpr = BindValue(whenFalse, diagnostics, BindValueKind.RValue);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            ConstantValue? constantValue = null;
            TypeSymbol? bestType = BestTypeInferrer.InferBestTypeForConditionalOperator(trueExpr, falseExpr, this.Conversions, out bool hadMultipleCandidates, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

            if (bestType is null)
            {
                ErrorCode noCommonTypeError = hadMultipleCandidates ? ErrorCode.ERR_AmbigQM : ErrorCode.ERR_InvalidQM;
                constantValue = FoldConditionalOperator(condition, trueExpr, falseExpr);
                return new BoundUnconvertedConditionalOperator(node, condition, trueExpr, falseExpr, constantValue, noCommonTypeError, hasErrors: constantValue?.IsBad == true);
            }

            bool hasErrors;
            if (bestType.IsErrorType())
            {
                trueExpr = BindToNaturalType(trueExpr, diagnostics, reportNoTargetType: false);
                falseExpr = BindToNaturalType(falseExpr, diagnostics, reportNoTargetType: false);
                hasErrors = true;
            }
            else
            {
                trueExpr = GenerateConversionForAssignment(bestType, trueExpr, diagnostics);
                falseExpr = GenerateConversionForAssignment(bestType, falseExpr, diagnostics);
                hasErrors = trueExpr.HasAnyErrors || falseExpr.HasAnyErrors;
            }

            if (!hasErrors)
            {
                constantValue = FoldConditionalOperator(condition, trueExpr, falseExpr);
                hasErrors = constantValue != null && constantValue.IsBad;
            }

            return new BoundConditionalOperator(node, isRef: false, condition, trueExpr, falseExpr, constantValue, naturalTypeOpt: bestType, wasTargetTyped: false, bestType, hasErrors);
        }
#nullable disable

        private BoundExpression BindRefConditionalOperator(ConditionalExpressionSyntax node, ExpressionSyntax whenTrue, ExpressionSyntax whenFalse, BindingDiagnosticBag diagnostics)
        {
            BoundExpression condition = BindBooleanExpression(node.Condition, diagnostics);
            BoundExpression trueExpr = BindValue(whenTrue, diagnostics, BindValueKind.RValue | BindValueKind.RefersToLocation);
            BoundExpression falseExpr = BindValue(whenFalse, diagnostics, BindValueKind.RValue | BindValueKind.RefersToLocation);
            bool hasErrors = trueExpr.HasErrors | falseExpr.HasErrors;
            TypeSymbol trueType = trueExpr.Type;
            TypeSymbol falseType = falseExpr.Type;

            TypeSymbol type;
            if (!Conversions.HasIdentityConversion(trueType, falseType))
            {
                if (!hasErrors)
                    diagnostics.Add(ErrorCode.ERR_RefConditionalDifferentTypes, falseExpr.Syntax.Location, trueType);

                type = CreateErrorType();
                hasErrors = true;
            }
            else
            {
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                type = BestTypeInferrer.InferBestTypeForConditionalOperator(trueExpr, falseExpr, this.Conversions, hadMultipleCandidates: out _, ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);

                Debug.Assert(type is { });
                Debug.Assert(Conversions.HasIdentityConversion(trueType, type));
                Debug.Assert(Conversions.HasIdentityConversion(falseType, type));
            }

            trueExpr = BindToNaturalType(trueExpr, diagnostics, reportNoTargetType: false);
            falseExpr = BindToNaturalType(falseExpr, diagnostics, reportNoTargetType: false);
            return new BoundConditionalOperator(node, isRef: true, condition, trueExpr, falseExpr, constantValueOpt: null, type, wasTargetTyped: false, type, hasErrors);
        }
    }

    partial class RefSafetyAnalysis
    {
        private void ValidateRefConditionalOperator(SyntaxNode node, BoundExpression trueExpr, BoundExpression falseExpr, BindingDiagnosticBag diagnostics)
        {
            // val-escape must agree on both branches.
            SafeContext whenTrueEscape = GetValEscape(trueExpr);
            SafeContext whenFalseEscape = GetValEscape(falseExpr);

            if (whenTrueEscape != whenFalseEscape)
            {
                // ask the one with narrower escape, for the wider - hopefully the errors will make the violation easier to fix.
                if (!whenFalseEscape.IsConvertibleTo(whenTrueEscape))
                    CheckValEscape(falseExpr.Syntax, falseExpr, whenTrueEscape, checkingReceiver: false, diagnostics: diagnostics);
                else
                    CheckValEscape(trueExpr.Syntax, trueExpr, whenFalseEscape, checkingReceiver: false, diagnostics: diagnostics);

                diagnostics.Add(_inUnsafeRegion ? ErrorCode.WRN_MismatchedRefEscapeInTernary : ErrorCode.ERR_MismatchedRefEscapeInTernary, node.Location);
            }
        }
    }

    partial class Binder
    {
        /// <summary>
        /// Constant folding for conditional (aka ternary) operators.
        /// </summary>
        private static ConstantValue FoldConditionalOperator(BoundExpression condition, BoundExpression trueExpr, BoundExpression falseExpr)
        {
            ConstantValue trueValue = trueExpr.ConstantValueOpt;
            if (trueValue == null || trueValue.IsBad)
            {
                return trueValue;
            }

            ConstantValue falseValue = falseExpr.ConstantValueOpt;
            if (falseValue == null || falseValue.IsBad)
            {
                return falseValue;
            }

            ConstantValue conditionValue = condition.ConstantValueOpt;
            if (conditionValue == null || conditionValue.IsBad)
            {
                return conditionValue;
            }
            else if (conditionValue == ConstantValue.True)
            {
                return trueValue;
            }
            else if (conditionValue == ConstantValue.False)
            {
                return falseValue;
            }
            else
            {
                return ConstantValue.Bad;
            }
        }

        private void CheckNativeIntegerFeatureAvailability(BinaryOperatorKind operatorKind, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            if (Compilation.Assembly.RuntimeSupportsNumericIntPtr)
            {
                return;
            }

            switch (operatorKind & BinaryOperatorKind.TypeMask)
            {
                case BinaryOperatorKind.NInt:
                case BinaryOperatorKind.NUInt:
                    CheckFeatureAvailability(syntax, MessageID.IDS_FeatureNativeInt, diagnostics);
                    break;
            }
        }

        private void CheckNativeIntegerFeatureAvailability(UnaryOperatorKind operatorKind, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            if (Compilation.Assembly.RuntimeSupportsNumericIntPtr)
            {
                return;
            }

            switch (operatorKind & UnaryOperatorKind.TypeMask)
            {
                case UnaryOperatorKind.NInt:
                case UnaryOperatorKind.NUInt:
                    CheckFeatureAvailability(syntax, MessageID.IDS_FeatureNativeInt, diagnostics);
                    break;
            }
        }
    }
}
