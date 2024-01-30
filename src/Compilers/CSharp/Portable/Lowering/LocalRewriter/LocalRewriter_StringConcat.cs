// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// The strategy of this rewrite is to do rewrite "locally".
        /// We analyze arguments of the concat in a shallow fashion assuming that 
        /// lowering and optimizations (including this one) is already done for the arguments.
        /// Based on the arguments we select the most appropriate pattern for the current node.
        /// 
        /// NOTE: it is not guaranteed that the node that we chose will be the most optimal since we have only 
        ///       local information - i.e. we look at the arguments, but we do not know about siblings.
        ///       When we move to the parent, the node may be rewritten by this or some another optimization.
        ///       
        /// Example:
        ///     result = ( "abc" + "def" + null ?? expr1 + "moo" + "baz" ) + expr2
        /// 
        /// Will rewrite into:
        ///     result = Concat("abcdef", expr2)
        ///     
        /// However there will be transient nodes like  Concat(expr1 + "moo")  that will not be present in the
        /// resulting tree.
        ///
        /// </summary>
        private BoundExpression RewriteStringConcatenation(SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type)
        {
            Debug.Assert(
                operatorKind == BinaryOperatorKind.StringConcatenation ||
                operatorKind == BinaryOperatorKind.StringAndObjectConcatenation ||
                operatorKind == BinaryOperatorKind.ObjectAndStringConcatenation);

            if (_inExpressionLambda)
            {
                return RewriteStringConcatInExpressionLambda(syntax, operatorKind, loweredLeft, loweredRight, type);
            }

            // Convert both sides to a string (calling ToString if necessary)
            loweredLeft = ConvertConcatExprToString(syntax, loweredLeft);
            loweredRight = ConvertConcatExprToString(syntax, loweredRight);

            Debug.Assert(loweredLeft.Type is { } && (loweredLeft.Type.IsStringType() || loweredLeft.Type.IsErrorType()) || loweredLeft.ConstantValueOpt?.IsNull == true);
            Debug.Assert(loweredRight.Type is { } && (loweredRight.Type.IsStringType() || loweredRight.Type.IsErrorType()) || loweredRight.ConstantValueOpt?.IsNull == true);

            // try fold two args without flattening.
            var folded = TryFoldTwoConcatOperands(loweredLeft, loweredRight);
            if (folded != null)
            {
                return folded;
            }

            // flatten and merge -  ( expr1 + "A" ) + ("B" + expr2) ===> (expr1 + "AB" + expr2)
            ArrayBuilder<BoundExpression> leftFlattened = ArrayBuilder<BoundExpression>.GetInstance();
            ArrayBuilder<BoundExpression> rightFlattened = ArrayBuilder<BoundExpression>.GetInstance();

            FlattenConcatArg(loweredLeft, leftFlattened);
            FlattenConcatArg(loweredRight, rightFlattened);

            if (leftFlattened.Any() && rightFlattened.Any())
            {
                folded = TryFoldTwoConcatOperands(leftFlattened.Last(), rightFlattened.First());
                if (folded != null)
                {
                    rightFlattened[0] = folded;
                    leftFlattened.RemoveLast();
                }
            }

            leftFlattened.AddRange(rightFlattened);
            rightFlattened.Free();

            BoundExpression? result;

            switch (leftFlattened.Count)
            {
                case 0:
                    result = _factory.StringLiteral(string.Empty);
                    break;

                case 1:
                    // All code paths which reach here (through TryFoldTwoConcatOperands) have already called
                    // RewriteStringConcatenationOneExpr if necessary
                    result = leftFlattened[0];
                    break;

                case 2:
                    var left = leftFlattened[0];
                    var right = leftFlattened[1];

                    if (!TryRewriteStringConcatenationWithSpanBasedConcat(syntax, leftFlattened, out result))
                    {
                        result = RewriteStringConcatenationTwoExprs(syntax, left, right);
                    }
                    break;

                case 3:
                    {
                        var first = leftFlattened[0];
                        var second = leftFlattened[1];
                        var third = leftFlattened[2];

                        if (!TryRewriteStringConcatenationWithSpanBasedConcat(syntax, leftFlattened, out result))
                        {
                            result = RewriteStringConcatenationThreeExprs(syntax, first, second, third);
                        }
                    }
                    break;

                case 4:
                    {
                        var first = leftFlattened[0];
                        var second = leftFlattened[1];
                        var third = leftFlattened[2];
                        var fourth = leftFlattened[3];

                        if (!TryRewriteStringConcatenationWithSpanBasedConcat(syntax, leftFlattened, out result))
                        {
                            result = RewriteStringConcatenationFourExprs(syntax, first, second, third, fourth);
                        }
                    }
                    break;

                default:
                    result = RewriteStringConcatenationManyExprs(syntax, leftFlattened.ToImmutable());
                    break;
            }

            leftFlattened.Free();
            return result;
        }

        /// <summary>
        /// digs into known concat operators and unwraps their arguments
        /// otherwise returns the expression as-is
        /// 
        /// Generally we only need to recognize same node patterns that we create as a result of concatenation rewrite.
        /// </summary>
        private void FlattenConcatArg(BoundExpression lowered, ArrayBuilder<BoundExpression> flattened)
        {
            if (TryExtractStringConcatArgs(lowered, out var arguments))
            {
                flattened.AddRange(arguments);
            }
            else
            {
                // fallback - if nothing above worked, leave arg as-is
                flattened.Add(lowered);
            }
        }

        /// <summary>
        /// Determines whether an expression is a known string concat operator (with or without a subsequent ?? ""), and extracts
        /// its args if so.
        /// </summary>
        /// <returns>True if this is a call to a known string concat operator and its arguments are successfully extracted, false otherwise</returns>
        private bool TryExtractStringConcatArgs(BoundExpression lowered, out ImmutableArray<BoundExpression> arguments)
        {
            switch (lowered)
            {
                case BoundCall boundCall:
                    var method = boundCall.Method;
                    if (method.IsStatic && method.ContainingType.SpecialType == SpecialType.System_String)
                    {
                        if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringStringString))
                        {
                            arguments = boundCall.Arguments;
                            return true;
                        }

                        if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringArray))
                        {
                            var args = boundCall.Arguments[0] as BoundArrayCreation;
                            if (args != null)
                            {
                                var initializer = args.InitializerOpt;
                                if (initializer != null)
                                {
                                    arguments = initializer.Initializers;
                                    return true;
                                }
                            }
                        }

                        if ((object)method == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Concat_2ReadOnlySpans) ||
                            (object)method == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Concat_3ReadOnlySpans) ||
                            (object)method == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Concat_4ReadOnlySpans))
                        {
                            // Faced a span-based string.Concat call. Since we can produce such call on the previous iterations ourselves, we need to unwrap it.
                            // The key thing is that we need not to only extract arguments, but also unwrap them from being spans and for chars also wrap them into `ToString` calls.
                            // Another challenge is that we may have merged user-written span-based string.Concat with additional argument previously and we need to undo this change as well
                            // so that if at some point we exceed 3 or 4 span arguments we can undo all span-concat changes and use string.Concat(string[]) overload with the same arguments as the user provided.
                            // We do that by tracking which argument nodes have `WasCompilerGenerated` flag. If If a consecutive span of nodes is not compiler-generated, they are arguments from original user-written span.Concat call
                            var wrappedArgs = boundCall.Arguments;
                            var unwrappedArgsBuilder = ArrayBuilder<BoundExpression>.GetInstance(capacity: wrappedArgs.Length);

                            var previousConcatArgsBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                            var previousConcatIndex = -1;

                            for (var i = 0; i < wrappedArgs.Length; i++)
                            {
                                var wrappedArg = wrappedArgs[i];

                                if (!wrappedArg.WasCompilerGenerated)
                                {
                                    previousConcatArgsBuilder.Add(wrappedArg);
                                    if (previousConcatIndex == -1)
                                    {
                                        previousConcatIndex = i;
                                    }
                                    continue;
                                }

                                // Check whether a call is an implicit `string -> ReadOnlySpan<char>` conversion
                                if (wrappedArg is BoundCall { Method: var argMethod, Arguments: [var singleArgument] } &&
                                    (object)argMethod == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar))
                                {
                                    unwrappedArgsBuilder.Add(singleArgument);
                                }
                                // This complicated check is for a sequence, which wraps a span around single char.
                                // The sequence needs to have this shape: `{ locals: <none>, sideEffects: temp = <original char expression>, result: new ReadOnlySpan<char>(in temp) }`
                                else if (wrappedArg is BoundSequence { Locals.Length: 0, SideEffects: [BoundAssignmentOperator { Right: var assignmentRight }], Value: BoundObjectCreationExpression { Constructor: var objectCreationConstructor, Arguments: [BoundLocal] } } &&
                                         (object)objectCreationConstructor.OriginalDefinition == _compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference) &&
                                         objectCreationConstructor.ReceiverType.IsReadOnlySpanChar())
                                {
                                    Debug.Assert(assignmentRight.Type?.IsCharType() == true);
                                    var charToString = FindSpecificToStringOfStructType(assignmentRight.Type, UnsafeGetSpecialTypeMethod(wrappedArg.Syntax, SpecialMember.System_Object__ToString));
                                    if (charToString is null)
                                    {
                                        unwrappedArgsBuilder.Free();
                                        previousConcatArgsBuilder.Free();
                                        arguments = default;
                                        return false;
                                    }
                                    unwrappedArgsBuilder.Add(BoundCall.Synthesized(wrappedArg.Syntax, assignmentRight, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, charToString));
                                }
                                else
                                {
                                    unwrappedArgsBuilder.Free();
                                    previousConcatArgsBuilder.Free();
                                    arguments = default;
                                    return false;
                                }
                            }

                            if (previousConcatArgsBuilder.Count == boundCall.Arguments.Length)
                            {
                                unwrappedArgsBuilder.Free();
                                previousConcatArgsBuilder.Free();
                                arguments = default;
                                return false;
                            }

                            var previousConcatMember = GetSpanConcatMemberByArgumentsCount(previousConcatArgsBuilder.Count);

                            if (previousConcatMember.HasValue)
                            {
                                Debug.Assert(previousConcatIndex > -1);

                                if (TryGetWellKnownTypeMember(lowered.Syntax, previousConcatMember.Value, out MethodSymbol? previousConcatMethod, isOptional: true))
                                {
                                    var reconstructedPreviousConcat = BoundCall.Synthesized(lowered.Syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, previousConcatMethod, previousConcatArgsBuilder.ToImmutable());
                                    unwrappedArgsBuilder.Insert(previousConcatIndex, reconstructedPreviousConcat);
                                }
                                else
                                {
                                    unwrappedArgsBuilder.Free();
                                    previousConcatArgsBuilder.Free();
                                    arguments = default;
                                    return false;
                                }
                            }

                            arguments = unwrappedArgsBuilder.ToImmutableAndFree();
                            previousConcatArgsBuilder.Free();
                            return true;
                        }
                    }
                    break;

                case BoundNullCoalescingOperator boundCoalesce:
                    Debug.Assert(boundCoalesce.LeftPlaceholder is null);
                    Debug.Assert(boundCoalesce.LeftConversion is null);

                    // The RHS may be a constant value with an identity conversion to string even
                    // if it is not a string: in particular, the null literal behaves this way.
                    // To be safe, check that the constant value is actually a string before
                    // attempting to access its value as a string.

                    var rightConstant = boundCoalesce.RightOperand.ConstantValueOpt;
                    if (rightConstant != null && rightConstant.IsString && rightConstant.StringValue.Length == 0)
                    {
                        arguments = ImmutableArray.Create(boundCoalesce.LeftOperand);
                        return true;
                    }

                    break;

                case BoundSequence { SideEffects.Length: 0, Value: BoundCall sequenceCall }:
                    return TryExtractStringConcatArgs(sequenceCall, out arguments);
            }

            arguments = default;
            return false;
        }

        /// <summary>
        /// folds two concat operands into one expression if possible
        /// otherwise returns null
        /// </summary>
        private BoundExpression? TryFoldTwoConcatOperands(BoundExpression loweredLeft, BoundExpression loweredRight)
        {
            // both left and right are constants
            var leftConst = loweredLeft.ConstantValueOpt;
            var rightConst = loweredRight.ConstantValueOpt;

            if (leftConst != null && rightConst != null)
            {
                // const concat may fail to fold if strings are huge. 
                // This would be unusual.
                ConstantValue? concatenated = TryFoldTwoConcatConsts(leftConst, rightConst);
                if (concatenated != null)
                {
                    return _factory.StringLiteral(concatenated);
                }
            }

            // one or another is null. 
            if (IsNullOrEmptyStringConstant(loweredLeft))
            {
                if (IsNullOrEmptyStringConstant(loweredRight))
                {
                    return _factory.Literal(string.Empty);
                }

                return RewriteStringConcatenationOneExpr(loweredRight);
            }
            else if (IsNullOrEmptyStringConstant(loweredRight))
            {
                return RewriteStringConcatenationOneExpr(loweredLeft);
            }

            return null;
        }

        private static bool IsNullOrEmptyStringConstant(BoundExpression operand)
        {
            return (operand.ConstantValueOpt != null && string.IsNullOrEmpty(operand.ConstantValueOpt.StringValue)) ||
                    operand.IsDefaultValue();
        }

        /// <summary>
        /// folds two concat constants into one if possible
        /// otherwise returns null.
        /// It is generally always possible to concat constants, unless resulting string would be too large.
        /// </summary>
        private static ConstantValue? TryFoldTwoConcatConsts(ConstantValue leftConst, ConstantValue rightConst)
        {
            var leftVal = leftConst.StringValue;
            var rightVal = rightConst.StringValue;

            if (!leftConst.IsDefaultValue && !rightConst.IsDefaultValue)
            {
                Debug.Assert(leftVal is { } && rightVal is { });
                if (leftVal.Length + rightVal.Length < 0)
                {
                    return null;
                }
            }

            // TODO: if transient string allocations are an issue, consider introducing constants that contain builders.
            //       it may be not so easy to even get here though, since typical
            //       "A" + "B" + "C" + ... cases should be folded in the binder as spec requires so.
            //       we would be mostly picking here edge cases like "A" + (object)null + "B" + (object)null + ...
            return ConstantValue.Create(leftVal + rightVal);
        }

        /// <summary>
        /// Strangely enough there is such a thing as unary concatenation and it must be rewritten.
        /// </summary>
        private BoundExpression RewriteStringConcatenationOneExpr(BoundExpression loweredOperand)
        {
            // If it's a call to 'string.Concat' (or is something which ends in '?? ""', which this method also extracts),
            // we know the result cannot be null. Otherwise return loweredOperand ?? ""
            if (TryExtractStringConcatArgs(loweredOperand, out _))
            {
                return loweredOperand;
            }
            else
            {
                return _factory.Coalesce(loweredOperand, _factory.Literal(""));
            }
        }

        private BoundExpression RewriteStringConcatenationTwoExprs(SyntaxNode syntax, BoundExpression loweredLeft, BoundExpression loweredRight)
        {
            Debug.Assert(loweredLeft.HasAnyErrors || loweredLeft.Type is { } && loweredLeft.Type.IsStringType());
            Debug.Assert(loweredRight.HasAnyErrors || loweredRight.Type is { } && loweredRight.Type.IsStringType());

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringString);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method, loweredLeft, loweredRight);
        }

        private BoundExpression RewriteStringConcatenationThreeExprs(SyntaxNode syntax, BoundExpression loweredFirst, BoundExpression loweredSecond, BoundExpression loweredThird)
        {
            Debug.Assert(loweredFirst.HasAnyErrors || loweredFirst.Type is { } && loweredFirst.Type.IsStringType());
            Debug.Assert(loweredSecond.HasAnyErrors || loweredSecond.Type is { } && loweredSecond.Type.IsStringType());
            Debug.Assert(loweredThird.HasAnyErrors || loweredThird.Type is { } && loweredThird.Type.IsStringType());

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringStringString);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method, ImmutableArray.Create(loweredFirst, loweredSecond, loweredThird));
        }

        private BoundExpression RewriteStringConcatenationFourExprs(SyntaxNode syntax, BoundExpression loweredFirst, BoundExpression loweredSecond, BoundExpression loweredThird, BoundExpression loweredFourth)
        {
            Debug.Assert(loweredFirst.HasAnyErrors || loweredFirst.Type is { } && loweredFirst.Type.IsStringType());
            Debug.Assert(loweredSecond.HasAnyErrors || loweredSecond.Type is { } && loweredSecond.Type.IsStringType());
            Debug.Assert(loweredThird.HasAnyErrors || loweredThird.Type is { } && loweredThird.Type.IsStringType());
            Debug.Assert(loweredFourth.HasAnyErrors || loweredFourth.Type is { } && loweredFourth.Type.IsStringType());

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringStringStringString);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method, ImmutableArray.Create(loweredFirst, loweredSecond, loweredThird, loweredFourth));
        }

        private BoundExpression RewriteStringConcatenationManyExprs(SyntaxNode syntax, ImmutableArray<BoundExpression> loweredArgs)
        {
            Debug.Assert(loweredArgs.Length > 4);
            Debug.Assert(loweredArgs.All(a => a.HasErrors || a.Type is { } && a.Type.IsStringType()));

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringArray);
            Debug.Assert((object)method != null);

            var array = _factory.ArrayOrEmpty(_factory.SpecialType(SpecialType.System_String), loweredArgs);

            return BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method, array);
        }

        private bool TryRewriteStringConcatenationWithSpanBasedConcat(SyntaxNode syntax, ArrayBuilder<BoundExpression> args, [NotNullWhen(true)] out BoundExpression? result)
        {
            // As we scan arguments we might face span-based string.Concat. If it got to this stage it must be a user-provided one (since we weren't able to unwrap it back to strings previously)
            // We try our best to merge it with another argument to minimize amount of intermediate string allocations,
            // e.g. turn `string.Concat(span1, span2) + string3` into `string.Concat(span1, span2, string3ConvertedToSpan)`
            // instead of `string.Concat(string.Concat(span1, span2), string3)`. We do that by separately tracking arguments as is
            // and arguments if we unwrap all user-defined `string.Concat`s. If at the end amount of arguments with unwrapped user-defined `string.Concat`s
            // is < 5 and we have respective span-based concat member, we emit it, otherwise treat user-provided `string.Concat`s as ordinary arguments
            var preparedArgs = ArrayBuilder<BoundExpression>.GetInstance(capacity: args.Count);
            var preparedArgsIfUnwrapUserStringConcat = ArrayBuilder<BoundExpression>.GetInstance(capacity: args.Count);

            var needsSpanRefParamConstructor = false;
            var needsImplicitConversionFromStringToSpan = false;

            // When we get here we either 100% already queried for object.ToString in `ConvertConcatExprToString` (if at least 1 operand is not a string) or don't need it (if all operands are strings).
            // Thus we can pass `isOptional` flag to avoid duplicate or unintentional "missing member" diagnostic in case we are actually missing `object.ToString()`
            var objectToStringMethod = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_Object__ToString, isOptional: true);
            NamedTypeSymbol? charType = null;

            foreach (var arg in args)
            {
                Debug.Assert(arg.HasAnyErrors || arg.Type?.IsStringType() == true);

                if (arg is BoundCall { ReceiverOpt: { Type: NamedTypeSymbol { SpecialType: SpecialType.System_Char } receiverCharType } receiver } potentialToStringCall &&
                    (object)potentialToStringCall.Method.GetLeastOverriddenMethod(charType) == objectToStringMethod)
                {
                    needsSpanRefParamConstructor = true;
                    charType = receiverCharType;
                    preparedArgs.Add(receiver);
                    preparedArgsIfUnwrapUserStringConcat.Add(receiver);
                    continue;
                }

                preparedArgs.Add(arg);

                if (arg is BoundCall spanConcatCall &&
                    ((object)spanConcatCall.Method == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Concat_2ReadOnlySpans) ||
                    (object)spanConcatCall.Method == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Concat_3ReadOnlySpans) ||
                    (object)spanConcatCall.Method == _compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Concat_4ReadOnlySpans)))
                {
                    preparedArgsIfUnwrapUserStringConcat.AddRange(spanConcatCall.Arguments);
                    continue;
                }

                needsImplicitConversionFromStringToSpan = true;
                preparedArgsIfUnwrapUserStringConcat.Add(arg);
            }

            var concatMemberIfUnwrapUserSpanConcats = GetSpanConcatMemberByArgumentsCount(preparedArgsIfUnwrapUserStringConcat.Count);

            MethodSymbol? spanConcat;
            MethodSymbol? readOnlySpanCtorRefParamChar;
            MethodSymbol? stringImplicitConversionToReadOnlySpan;

            if (preparedArgsIfUnwrapUserStringConcat.Count > preparedArgs.Count && concatMemberIfUnwrapUserSpanConcats.HasValue)
            {
                if (TryGetWellKnownTypeMember(syntax, concatMemberIfUnwrapUserSpanConcats.Value, out spanConcat, isOptional: true) &&
                    tryGetNeededToSpanMembers(this, syntax, needsSpanRefParamConstructor, needsImplicitConversionFromStringToSpan, charType, out readOnlySpanCtorRefParamChar, out stringImplicitConversionToReadOnlySpan))
                {
                    result = rewriteStringConcatenationWithSpanBasedConcat(
                        syntax,
                        _factory,
                        spanConcat,
                        stringImplicitConversionToReadOnlySpan,
                        readOnlySpanCtorRefParamChar,
                        preparedArgsIfUnwrapUserStringConcat.ToImmutableAndFree());

                    preparedArgs.Free();
                    return true;
                }

                needsImplicitConversionFromStringToSpan = true;
            }

            var concatMember = GetSpanConcatMemberByArgumentsCount(preparedArgsIfUnwrapUserStringConcat.Count);

            // If we got here it only makes sense to lower using span-based concat if at least one operand is a char.
            // Because otherwise we will just unnecessarily wrap every string operand into span conversion and use span-based concat
            // which is unnecessary IL bloat. Thus we require `needsSpanRefParamConstructor` to be true
            if (needsSpanRefParamConstructor &&
                concatMember.HasValue &&
                TryGetWellKnownTypeMember(syntax, concatMember.Value, out spanConcat, isOptional: true) &&
                tryGetNeededToSpanMembers(this, syntax, needsSpanRefParamConstructor, needsImplicitConversionFromStringToSpan, charType, out readOnlySpanCtorRefParamChar, out stringImplicitConversionToReadOnlySpan))
            {
                result = rewriteStringConcatenationWithSpanBasedConcat(
                        syntax,
                        _factory,
                        spanConcat,
                        stringImplicitConversionToReadOnlySpan,
                        readOnlySpanCtorRefParamChar,
                        preparedArgs.ToImmutableAndFree());

                preparedArgsIfUnwrapUserStringConcat.Free();
                return true;
            }

            result = null;
            return false;

            static bool tryGetNeededToSpanMembers(LocalRewriter self, SyntaxNode syntax, bool needsSpanRefParamConstructor, bool needsImplicitConversionFromStringToSpan, NamedTypeSymbol? charType, out MethodSymbol? readOnlySpanCtorRefParamChar, out MethodSymbol? stringImplicitConversionToReadOnlySpan)
            {
                readOnlySpanCtorRefParamChar = null;
                stringImplicitConversionToReadOnlySpan = null;

                if (needsSpanRefParamConstructor)
                {
                    if (self.TryGetWellKnownTypeMember(syntax, WellKnownMember.System_ReadOnlySpan_T__ctor_Reference, out MethodSymbol? readOnlySpanCtorRefParamGeneric, isOptional: true))
                    {
                        Debug.Assert(charType is not null);
                        var readOnlySpanOfChar = readOnlySpanCtorRefParamGeneric.ContainingType.Construct(charType);
                        readOnlySpanCtorRefParamChar = readOnlySpanCtorRefParamGeneric.AsMember(readOnlySpanOfChar);
                    }
                    else
                    {
                        return false;
                    }
                }

                if (needsImplicitConversionFromStringToSpan)
                {
                    return self.TryGetWellKnownTypeMember(syntax, WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar, out stringImplicitConversionToReadOnlySpan, isOptional: true);
                }

                return true;
            }

            static BoundExpression rewriteStringConcatenationWithSpanBasedConcat(
                SyntaxNode syntax,
                SyntheticBoundNodeFactory factory,
                MethodSymbol spanConcat,
                MethodSymbol? stringImplicitConversionToReadOnlySpan,
                MethodSymbol? readOnlySpanCtorRefParamChar,
                ImmutableArray<BoundExpression> args)
            {
                var preparedArgsBuilder = ArrayBuilder<BoundExpression>.GetInstance(capacity: args.Length);
                var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();

                foreach (var arg in args)
                {
                    Debug.Assert(arg.Type is not null);

                    if (arg.Type.IsReadOnlySpanChar())
                    {
                        preparedArgsBuilder.Add(arg);
                    }
                    else if (arg.Type.SpecialType == SpecialType.System_Char)
                    {
                        Debug.Assert(readOnlySpanCtorRefParamChar is not null);

                        var temp = factory.StoreToTemp(arg, out var tempAssignment);
                        localsBuilder.Add(temp.LocalSymbol);

                        var wrappedChar = new BoundObjectCreationExpression(
                            arg.Syntax,
                            readOnlySpanCtorRefParamChar,
                            [temp],
                            argumentNamesOpt: default,
                            argumentRefKindsOpt: [RefKindExtensions.StrictIn],
                            expanded: false,
                            argsToParamsOpt: default,
                            defaultArguments: default,
                            constantValueOpt: null,
                            initializerExpressionOpt: null,
                            type: readOnlySpanCtorRefParamChar.ContainingType);

                        preparedArgsBuilder.Add(new BoundSequence(
                            arg.Syntax,
                            [],
                            [tempAssignment],
                            wrappedChar,
                            wrappedChar.Type)
                        { WasCompilerGenerated = true });
                    }
                    else
                    {
                        Debug.Assert(arg.HasAnyErrors || arg.Type.SpecialType == SpecialType.System_String);
                        Debug.Assert(stringImplicitConversionToReadOnlySpan is not null);
                        preparedArgsBuilder.Add(BoundCall.Synthesized(arg.Syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, stringImplicitConversionToReadOnlySpan, arg));
                    }
                }

                var concatCall = BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, spanConcat, preparedArgsBuilder.ToImmutableAndFree());

                var oldSyntax = factory.Syntax;
                factory.Syntax = syntax;

                var sequence = factory.Sequence(
                    localsBuilder.ToImmutableAndFree(),
                    [],
                    concatCall);

                factory.Syntax = oldSyntax;
                return sequence;
            }
        }

        private static WellKnownMember? GetSpanConcatMemberByArgumentsCount(int argumentCount) => argumentCount switch
        {
            2 => WellKnownMember.System_String__Concat_2ReadOnlySpans,
            3 => WellKnownMember.System_String__Concat_3ReadOnlySpans,
            4 => WellKnownMember.System_String__Concat_4ReadOnlySpans,
            _ => null,
        };

        /// <summary>
        /// Most of the above optimizations are not applicable in expression trees as the operator
        /// must stay a binary operator. We cannot do much beyond constant folding which is done in binder.
        /// </summary>
        private BoundExpression RewriteStringConcatInExpressionLambda(SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type)
        {
            SpecialMember member = (operatorKind == BinaryOperatorKind.StringConcatenation) ?
                SpecialMember.System_String__ConcatStringString :
                SpecialMember.System_String__ConcatObjectObject;

            var method = UnsafeGetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return new BoundBinaryOperator(syntax, operatorKind, constantValueOpt: null, method, constrainedToTypeOpt: null, default(LookupResultKind), loweredLeft, loweredRight, type);
        }

        /// <summary>
        /// Returns an expression which converts the given expression into a string (or null).
        /// If necessary, this invokes .ToString() on the expression, to avoid boxing value types.
        /// </summary>
        private BoundExpression ConvertConcatExprToString(SyntaxNode syntax, BoundExpression expr)
        {
            // If it's a value type, it'll have been boxed by the +(string, object) or +(object, string)
            // operator. Undo that.
            if (expr.Kind == BoundKind.Conversion)
            {
                BoundConversion conv = (BoundConversion)expr;
                if (conv.ConversionKind == ConversionKind.Boxing)
                {
                    expr = conv.Operand;
                }
            }

            // Is the expression a constant char?  If so, we can
            // simply make it a literal string instead and avoid any 
            // allocations for converting the char to a string at run time.
            // Similarly if it's a literal null, don't do anything special.
            if (expr is { ConstantValueOpt: { } cv })
            {
                if (cv.SpecialType == SpecialType.System_Char)
                {
                    return _factory.StringLiteral(cv.CharValue.ToString());
                }
                else if (cv.IsNull)
                {
                    return expr;
                }
            }

            // If expression is of form `constantChar.ToString()` then rewrite it here to a string literal so we can choose better options for lowering later (e.g. fold it with another constant instead of performing concatenation)
            // NOTE: We request `object.ToString()` as an optional member here to avoid reporting "missing member" diagnostic earlier than we should.
            // Because if in the end it turns out that we have a simple concatenation of n strings there is no need to report missing `object.ToString()` member
            if (TryGetSpecialTypeMethod(expr.Syntax, SpecialMember.System_Object__ToString, out MethodSymbol? optionalObjectToString, isOptional: true) &&
                expr is BoundCall { ReceiverOpt: { Type: NamedTypeSymbol { SpecialType: SpecialType.System_Char } charType, ConstantValueOpt: { IsChar: true } charConstant } } call &&
                call.Method.GetLeastOverriddenMember(charType) == optionalObjectToString)
            {
                var oldSyntax = _factory.Syntax;
                _factory.Syntax = expr.Syntax;
                expr = _factory.Literal(charConstant.CharValue.ToString());
                _factory.Syntax = oldSyntax;
            }

            Debug.Assert(expr.Type is not null);

            // If it's a string already, just return it
            if (expr.Type.IsStringType())
            {
                return expr;
            }

            // Evaluate toString at the last possible moment, to avoid spurious diagnostics if it's missing.
            // All code paths below here use it.
            var objectToStringMethod = UnsafeGetSpecialTypeMethod(expr.Syntax, SpecialMember.System_Object__ToString);

            // If it's a struct which has overridden ToString, find that method. Note that we might fail to
            // find it, e.g. if object.ToString is missing
            MethodSymbol? structToStringMethod = FindSpecificToStringOfStructType(expr.Type, objectToStringMethod);

            // If it's a special value type (and not a field of a MarshalByRef object), it should have its own ToString method (but we might fail to find
            // it if object.ToString is missing). Assume that this won't be removed, and emit a direct call rather
            // than a constrained virtual call. This keeps in the spirit of #7079, but expands the range of
            // types to all special value types.
            if (structToStringMethod != null && (expr.Type.SpecialType != SpecialType.None && !isFieldOfMarshalByRef(expr, _compilation)))
            {
                return BoundCall.Synthesized(expr.Syntax, expr, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, structToStringMethod);
            }

            // - It's a reference type (excluding unconstrained generics): no copy
            // - It's a constant: no copy
            // - The type definitely doesn't have its own ToString method (i.e. we're definitely calling 
            //   object.ToString on a struct type, not type parameter): no copy (yes this is a versioning issue,
            //   but that doesn't matter)
            // - We're calling the type's own ToString method, and it's effectively readonly (the method or the whole
            //   type is readonly): no copy
            // - Otherwise: copy
            // This is to mimic the old behaviour, where value types would be boxed before ToString was called on them,
            // but with optimizations for readonly methods.
            bool callWithoutCopy = expr.Type.IsReferenceType ||
                expr.ConstantValueOpt != null ||
                (structToStringMethod == null && !expr.Type.IsTypeParameter()) ||
                structToStringMethod?.IsEffectivelyReadOnly == true;

            // No need for a conditional access if it's a value type - we know it's not null.
            if (expr.Type.IsValueType)
            {
                if (!callWithoutCopy)
                {
                    expr = new BoundPassByCopy(expr.Syntax, expr, expr.Type);
                }
                return BoundCall.Synthesized(expr.Syntax, expr, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, objectToStringMethod);
            }

            if (callWithoutCopy)
            {
                return makeConditionalAccess(expr);
            }
            else
            {
                // If we do conditional access on a copy, we need a proper BoundLocal rather than a
                // BoundPassByCopy (as it's accessed multiple times). If we don't do this, and the
                // receiver is an unconstrained generic parameter, BoundLoweredConditionalAccess has
                // to generate a lot of code to ensure it only accesses the copy once (which is pointless).
                var temp = _factory.StoreToTemp(expr, out var store);
                return _factory.Sequence(
                    ImmutableArray.Create(temp.LocalSymbol),
                    ImmutableArray.Create<BoundExpression>(store),
                    makeConditionalAccess(temp));
            }

            BoundExpression makeConditionalAccess(BoundExpression receiver)
            {
                int currentConditionalAccessID = ++_currentConditionalAccessID;

                return new BoundLoweredConditionalAccess(
                    syntax,
                    receiver,
                    hasValueMethodOpt: null,
                    whenNotNull: BoundCall.Synthesized(
                        syntax,
                        new BoundConditionalReceiver(syntax, currentConditionalAccessID, expr.Type),
                        initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                        objectToStringMethod),
                    whenNullOpt: null,
                    id: currentConditionalAccessID,
                    forceCopyOfNullableValueType: false,
                    type: _compilation.GetSpecialType(SpecialType.System_String));
            }

            static bool isFieldOfMarshalByRef(BoundExpression expr, CSharpCompilation compilation)
            {
                Debug.Assert(!IsCapturedPrimaryConstructorParameter(expr));

                if (expr is BoundFieldAccess fieldAccess)
                {
                    return DiagnosticsPass.IsNonAgileFieldAccess(fieldAccess, compilation);
                }
                return false;
            }
        }

        private static MethodSymbol? FindSpecificToStringOfStructType(TypeSymbol exprType, MethodSymbol objectToStringMethod)
        {
            if (exprType.IsValueType && !exprType.IsTypeParameter())
            {
                var namedType = (NamedTypeSymbol)exprType;
                var typeToStringMembers = namedType.GetMembers(objectToStringMethod.Name);
                foreach (var member in typeToStringMembers)
                {
                    if (member is MethodSymbol toStringMethod &&
                        toStringMethod.GetLeastOverriddenMethod(namedType) == (object)objectToStringMethod)
                    {
                        return toStringMethod;
                    }
                }
            }

            return null;
        }
    }
}
