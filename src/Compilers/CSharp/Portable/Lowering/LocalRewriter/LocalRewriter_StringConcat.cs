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
        private static bool IsBinaryStringConcatenation([NotNullWhen(true)] BoundBinaryOperator? binaryOperator)
            => binaryOperator is { OperatorKind: var kind } && IsBinaryStringConcatenation(kind);

        private static bool IsBinaryStringConcatenation(BinaryOperatorKind binaryOperator)
            => binaryOperator is BinaryOperatorKind.StringConcatenation or BinaryOperatorKind.StringAndObjectConcatenation or BinaryOperatorKind.ObjectAndStringConcatenation;

        private BoundExpression VisitCompoundAssignmentStringConcatenation(BoundExpression left, BoundExpression unvisitedRight, BinaryOperatorKind operatorKind, TypeSymbol resultType, SyntaxNode syntax)
        {
            Debug.Assert(IsBinaryStringConcatenation(operatorKind));

            if (_inExpressionLambda)
            {
                return RewriteStringConcatInExpressionLambda(left, unvisitedRight, operatorKind, syntax, resultType);
            }

            ArrayBuilder<BoundExpression> arguments;
            if (unvisitedRight is BoundBinaryOperator { InterpolatedStringHandlerData: null } rightBinary && IsBinaryStringConcatenation(rightBinary))
            {
                CollectAndVisitConcatArguments(rightBinary, left, out arguments);
                Debug.Assert(ReferenceEquals(arguments[0], left));
            }
            else
            {
                arguments = ArrayBuilder<BoundExpression>.GetInstance();
                var concatMethods = new WellKnownConcatRelatedMethods(_compilation);
                AddConcatArgument(unvisitedRight, argumentAlreadyVisited: false, arguments, ref concatMethods);
                AddConcatArgument(left, argumentAlreadyVisited: true, arguments, ref concatMethods);
                arguments.ReverseContents();
            }

            return RewriteStringConcat(syntax, arguments);
        }

        private BoundExpression VisitStringConcatenation(BoundBinaryOperator originalOperator)
        {
            Debug.Assert(IsBinaryStringConcatenation(originalOperator));

            if (_inExpressionLambda)
            {
                // Case 1, expression trees
                return RewriteStringConcatInExpressionLambda(
                    originalOperator,
                    compoundAssignmentLeft: null,
                    compoundAssignmentOperatorKind: null,
                    compoundAssignmentSyntax: null,
                    compoundAssignmentType: null);
            }

            // We'll walk the children in a depth-first order, pull all the arguments out, and then visit them. We'll fold any constant arguments as
            // we go, pulling them all into a string literal.
            CollectAndVisitConcatArguments(originalOperator, visitedCompoundAssignmentLeftRead: null, out var arguments);

            return RewriteStringConcat(originalOperator.Syntax, arguments);
        }

        private BoundExpression RewriteStringConcat(SyntaxNode originalSyntax, ArrayBuilder<BoundExpression> visitedArguments)
        {
            // The strategy of this rewrite is to pull out all the individual arguments of the concat, visit them as appropriate,
            // and then construct the final concat all in one go. There are a few different lowering patterns that we take:
            //
            // 1. If we're in an expression tree, we have to follow strict binary operator rules. Nothing fancy to do, just do a recursive
            //    rewrite into nested `string.Concat` calls.
            // 2. If all the added expressions are constants, we can fold them all together and return a single constant.
            // 3. If all the added expressions are strings, then we want to use one of the `string.Concat(string)`-based overloads: if 4 or less,
            //    we'll use one of the hardcoded overloads. Otherwise, we'll use `string.Concat(string[])`.
            // 4. If all the added expressions are strings or chars, we can use the `string.Concat(ReadOnlySpan<char>)`-based overloads. If there are
            //    more than 4 arguments, we will instead fall back to `string.Concat(string[])`.
            // 5. If all the added expressions are strings or objects, we'll use the `string.Concat(string)`-based overloads, and call ToString on the
            //    arguments to avoid boxing structs by converthing them into objects. If there are more than 3, we'll use `string.Concat(string[])`.

            if (visitedArguments.Count == 0)
            {
                // All the arguments were null or the empty string. We can just return a constant empty string.
                visitedArguments.Free();
                return _factory.StringLiteral(string.Empty);
            }
            else if (visitedArguments is [{ ConstantValueOpt: { IsString: true } or { IsChar: true } } arg])
            {
                // We were able to fold a constant, so we can just return that constant.
                visitedArguments.Free();
                return arg as BoundLiteral ?? VisitExpression(arg);
            }

            var concatKind = StringConcatenationRewriteKind.AllStrings;

            foreach (var arg in visitedArguments)
            {
                var argumentType = arg.Type;
                // Null arguments should have been eliminated before now.
                Debug.Assert(argumentType is not null);
                switch (argumentType.SpecialType)
                {
                    case SpecialType.System_String:
                        break;

                    case SpecialType.System_Char:
                        // If we're concating a constant char, we can just treat it as if it's a one-character string, which is more preferable.
                        if (concatKind == StringConcatenationRewriteKind.AllStrings && arg.ConstantValueOpt is not { IsChar: true })
                        {
                            concatKind = StringConcatenationRewriteKind.AllStringsOrChars;
                        }
                        break;

                    default:
                        concatKind = StringConcatenationRewriteKind.AllStringsOrObjects;
                        break;
                }
            }
            switch (concatKind, visitedArguments.Count)
            {
                case (_, 0):
                    throw ExceptionUtilities.Unreachable();

                case (_, 1):
                    // Only 1 argument. We need to make sure that it's not null, but otherwise we don't need to call Concat and can just use ToString.
                    var arg = ConvertConcatExprToString(visitedArguments[0]);
                    visitedArguments.Free();
                    return _factory.Coalesce(arg, _factory.StringLiteral(string.Empty));

                case (StringConcatenationRewriteKind.AllStringsOrChars, <= 4):
                    // We can use one of the `string.Concat(ReadOnlySpan<char>)`-based overloads.
                    var concatMember = visitedArguments.Count switch
                    {
                        2 => SpecialMember.System_String__Concat_2ReadOnlySpans,
                        3 => SpecialMember.System_String__Concat_3ReadOnlySpans,
                        4 => SpecialMember.System_String__Concat_4ReadOnlySpans,
                        _ => throw ExceptionUtilities.Unreachable(),
                    };

                    bool needsImplicitConversionFromStringToSpan = visitedArguments.Any(arg => arg.Type is { SpecialType: SpecialType.System_String });
                    var charType = _compilation.GetSpecialType(SpecialType.System_Char);

                    if (!TryGetSpecialTypeMethod(originalSyntax, concatMember, out var spanConcat, isOptional: true)
                        || !TryGetNeededToSpanMembers(this, originalSyntax, needsImplicitConversionFromStringToSpan, charType, out var readOnlySpanCtorRefParamChar, out var stringImplicitConversionToReadOnlySpan))
                    {
                        goto fallbackStrings;
                    }

                    return RewriteStringConcatenationWithSpanBasedConcat(originalSyntax, _factory, spanConcat, stringImplicitConversionToReadOnlySpan, readOnlySpanCtorRefParamChar, visitedArguments);

                case (StringConcatenationRewriteKind.AllStrings, _):
                case (StringConcatenationRewriteKind.AllStringsOrChars, _):
                case (StringConcatenationRewriteKind.AllStringsOrObjects, _):
fallbackStrings:

#pragma warning disable IDE0055// Fix formatting
                    // All other cases can be handled here
#pragma warning restore IDE0055// Fix formatting
                    concatMember = visitedArguments.Count switch
                    {
                        2 => SpecialMember.System_String__ConcatStringString,
                        3 => SpecialMember.System_String__ConcatStringStringString,
                        4 => SpecialMember.System_String__ConcatStringStringStringString,
                        >= 5 => SpecialMember.System_String__ConcatStringArray,
                        _ => throw ExceptionUtilities.UnexpectedValue(visitedArguments.Count),
                    };

                    for (int i = 0; i < visitedArguments.Count; i++)
                    {
                        visitedArguments[i] = ConvertConcatExprToString(visitedArguments[i]);
                    }

                    var finalArguments = visitedArguments.ToImmutableAndFree();

                    if (finalArguments.Length > 4)
                    {
                        var array = _factory.ArrayOrEmpty(_factory.SpecialType(SpecialType.System_String), finalArguments);
                        finalArguments = [array];
                    }

                    var method = UnsafeGetSpecialTypeMethod(originalSyntax, concatMember);
                    Debug.Assert(method is not null);
                    return BoundCall.Synthesized(originalSyntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method, finalArguments);

                default:
                    throw ExceptionUtilities.UnexpectedValue(concatKind);
            }
        }

        private void CollectAndVisitConcatArguments(BoundBinaryOperator originalOperator, BoundExpression? visitedCompoundAssignmentLeftRead, out ArrayBuilder<BoundExpression> destinationArguments)
        {
            destinationArguments = ArrayBuilder<BoundExpression>.GetInstance();
            var concatMethods = new WellKnownConcatRelatedMethods(_compilation);
            pushArguments(this, originalOperator, destinationArguments, ref concatMethods);
            if (visitedCompoundAssignmentLeftRead is not null)
            {
                // We don't expect to be able to optimize anything about the compound assignment left read, so we just add it as-is. This assert should be kept in sync
                // with the cases that can be optimized by the addArgument local function below; if we ever find a case that can be optimized, we may need to consider
                // whether to do so. The visiting logic in the parent function here depends on only one argument being added for a compound assignment left read, so if
                // we ever do introduce optimizations here that result in more than one argument being added to destinationArguments, we'll need to adjust that logic.
                Debug.Assert(visitedCompoundAssignmentLeftRead is
                    not (BoundCall or BoundConversion { ConversionKind: ConversionKind.Boxing, Type.SpecialType: SpecialType.System_Object, Operand.Type.SpecialType: SpecialType.System_Char })
                    and { ConstantValueOpt: null });
                destinationArguments.Add(visitedCompoundAssignmentLeftRead);
            }
            destinationArguments.ReverseContents();

            // We push these in reverse order to take advantage of the left-recursive nature of the tree and avoid needing a second stack
            static void pushArguments(LocalRewriter self, BoundBinaryOperator binaryOperator, ArrayBuilder<BoundExpression> arguments, ref WellKnownConcatRelatedMethods concatMethods)
            {
                do
                {
                    if (shouldRecurse(binaryOperator.Right, out var right))
                    {
                        pushArguments(self, right, arguments, ref concatMethods);
                    }
                    else
                    {
                        self.AddConcatArgument(binaryOperator.Right, argumentAlreadyVisited: false, arguments, ref concatMethods);
                    }

                    if (shouldRecurse(binaryOperator.Left, out var left))
                    {
                        binaryOperator = left;
                    }
                    else
                    {
                        self.AddConcatArgument(binaryOperator.Left, argumentAlreadyVisited: false, arguments, ref concatMethods);
                        break;
                    }
                }
                while (true);

                static bool shouldRecurse(BoundExpression expr, [NotNullWhen(true)] out BoundBinaryOperator? binaryOperator)
                {
                    binaryOperator = expr as BoundBinaryOperator;
                    if (IsBinaryStringConcatenation(binaryOperator) && binaryOperator.InterpolatedStringHandlerData is null)
                    {
                        return true;
                    }
                    else
                    {
                        binaryOperator = null;
                        return false;
                    }
                }
            }
        }

        private void AddConcatArgument(BoundExpression argument, bool argumentAlreadyVisited, ArrayBuilder<BoundExpression> finalArguments, ref WellKnownConcatRelatedMethods wellKnownConcatOptimizationMethods)
        {
            if (!argumentAlreadyVisited)
            {
                argument = VisitExpression(argument);
            }

            if (argument is BoundConversion { ConversionKind: ConversionKind.Boxing, Type.SpecialType: SpecialType.System_Object, Operand: { Type.SpecialType: SpecialType.System_Char } operand })
            {
                argument = operand;
            }

            if (argument is BoundCall call)
            {
                if (wellKnownConcatOptimizationMethods.IsWellKnownConcatMethod(call, out var concatArguments))
                {
                    for (int i = concatArguments.Length - 1; i >= 0; i--)
                    {
                        AddConcatArgument(concatArguments[i], argumentAlreadyVisited: true, finalArguments, ref wellKnownConcatOptimizationMethods);
                    }

                    return;
                }
                else if (wellKnownConcatOptimizationMethods.IsCharToString(call, out var charExpression))
                {
                    argument = charExpression;
                }
            }

            // This is `strValue ?? ""`, possibly from a nested binary addition of an interpolated string. We can just directly use the left operand
            if (argument is BoundNullCoalescingOperator { LeftOperand: { Type.SpecialType: SpecialType.System_String } left, RightOperand: BoundLiteral { ConstantValueOpt: { IsString: true, RopeValue.IsEmpty: true } } })
            {
                argument = left;
            }

            switch (argument.ConstantValueOpt)
            {
                case { IsNull: true } or { IsString: true, RopeValue.IsEmpty: true }:
                    // If this is a null constant or an empty string, then we don't need to include it in the final arguments list
                    return;

                case { IsString: true } or { IsChar: true }:
                    // See if we can merge this argument with the previous one
                    if (finalArguments.Count > 0 && finalArguments[^1].ConstantValueOpt is { IsString: true } or { IsChar: true })
                    {
                        var constantValue = finalArguments[^1].ConstantValueOpt!;
                        var previous = getRope(constantValue);
                        var current = getRope(argument.ConstantValueOpt!);
                        // We're visiting arguments in reverse order, so we need to prepend this constant value, not append
                        finalArguments[^1] = _factory.StringLiteral(ConstantValue.CreateFromRope(Rope.Concat(current, previous)));
                        return;
                    }

                    break;
            }

            finalArguments.Add(argument);

            static Rope getRope(ConstantValue constantValue)
            {
                Debug.Assert(constantValue.IsString || constantValue.IsChar);
                if (constantValue.IsString)
                {
                    return constantValue.RopeValue!;
                }
                else
                {
                    return Rope.ForString(constantValue.CharValue.ToString());
                }
            }
        }

        private enum StringConcatenationRewriteKind
        {
            AllStrings,
            AllStringsOrChars,
            AllStringsOrObjects,
        }

        private struct WellKnownConcatRelatedMethods(CSharpCompilation compilation)
        {
            private readonly CSharpCompilation _compilation = compilation;

            private MethodSymbol? _concatStringString;
            private MethodSymbol? _concatStringStringString;
            private MethodSymbol? _concatStringStringStringString;
            private MethodSymbol? _concatStringArray;
            private MethodSymbol? _objectToString;

            public bool IsWellKnownConcatMethod(BoundCall call, out ImmutableArray<BoundExpression> arguments)
            {
                if (!call.ArgsToParamsOpt.IsDefault)
                {
                    // If the arguments were explicitly ordered, we don't want to try doing any optimizations, so just assume that
                    // it's not a well-known concat method.
                    arguments = default;
                    return false;
                }

                if (IsConcatNonArray(call, ref _concatStringString, SpecialMember.System_String__ConcatStringString, out arguments)
                    || IsConcatNonArray(call, ref _concatStringStringString, SpecialMember.System_String__ConcatStringStringString, out arguments)
                    || IsConcatNonArray(call, ref _concatStringStringStringString, SpecialMember.System_String__ConcatStringStringStringString, out arguments))
                {
                    return true;
                }

                InitializeField(ref _concatStringArray, SpecialMember.System_String__ConcatStringArray);
                if ((object)call.Method == _concatStringArray && call.Arguments[0] is BoundArrayCreation array)
                {
                    arguments = array.InitializerOpt?.Initializers ?? [];
                    return true;
                }

                arguments = default;
                return false;
            }

            public bool IsCharToString(BoundCall call, [NotNullWhen(true)] out BoundExpression? charExpression)
            {
                InitializeField(ref _objectToString, SpecialMember.System_Object__ToString);
                if (call is { Arguments: [], ReceiverOpt.Type: NamedTypeSymbol { SpecialType: SpecialType.System_Char } charType, Method: { Name: "ToString" } method }
                    && (object)method.GetLeastOverriddenMethod(charType) == _objectToString)
                {
                    charExpression = call.ReceiverOpt;
                    return true;
                }

                charExpression = null;
                return false;
            }

            private readonly void InitializeField(ref MethodSymbol? member, SpecialMember specialMember)
                => member ??= _compilation.GetSpecialTypeMember(specialMember) as MethodSymbol;

            private readonly bool IsConcatNonArray(BoundCall call, ref MethodSymbol? concatMethod, SpecialMember concatSpecialMember, out ImmutableArray<BoundExpression> arguments)
            {
                InitializeField(ref concatMethod, concatSpecialMember);

                if ((object)call.Method == concatMethod)
                {
                    arguments = call.Arguments;
                    return true;
                }

                arguments = default;
                return false;
            }
        }

        private static bool TryGetNeededToSpanMembers(
            LocalRewriter self,
            SyntaxNode syntax,
            bool needsImplicitConversionFromStringToSpan,
            NamedTypeSymbol charType,
            [NotNullWhen(true)] out MethodSymbol? readOnlySpanCtorRefParamChar,
            out MethodSymbol? stringImplicitConversionToReadOnlySpan)
        {
            readOnlySpanCtorRefParamChar = null;
            stringImplicitConversionToReadOnlySpan = null;

            if (self.TryGetSpecialTypeMethod(syntax, SpecialMember.System_ReadOnlySpan_T__ctor_Reference, out MethodSymbol? readOnlySpanCtorRefParamGeneric, isOptional: true) &&
                readOnlySpanCtorRefParamGeneric.Parameters[0].RefKind != RefKind.Out)
            {
                var readOnlySpanOfChar = readOnlySpanCtorRefParamGeneric.ContainingType.Construct(charType);
                readOnlySpanCtorRefParamChar = readOnlySpanCtorRefParamGeneric.AsMember(readOnlySpanOfChar);
            }
            else
            {
                return false;
            }

            if (needsImplicitConversionFromStringToSpan)
            {
                return self.TryGetSpecialTypeMethod(syntax, SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar, out stringImplicitConversionToReadOnlySpan, isOptional: true);
            }

            return true;
        }

        private static BoundExpression RewriteStringConcatenationWithSpanBasedConcat(
            SyntaxNode syntax,
            SyntheticBoundNodeFactory factory,
            MethodSymbol spanConcat,
            MethodSymbol? stringImplicitConversionToReadOnlySpan,
            MethodSymbol readOnlySpanCtorRefParamChar,
            ArrayBuilder<BoundExpression> args)
        {
            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();

            for (int i = 0; i < args.Count; i++)
            {
                BoundExpression? arg = args[i];
                Debug.Assert(arg.Type is not null);

                if (arg.Type.SpecialType == SpecialType.System_Char)
                {
                    var temp = factory.StoreToTemp(arg, out var tempAssignment);
                    localsBuilder.Add(temp.LocalSymbol);

                    Debug.Assert(readOnlySpanCtorRefParamChar.Parameters[0].RefKind != RefKind.Out);

                    var wrappedChar = new BoundObjectCreationExpression(
                        arg.Syntax,
                        readOnlySpanCtorRefParamChar,
                        [temp],
                        argumentNamesOpt: default,
                        argumentRefKindsOpt: [readOnlySpanCtorRefParamChar.Parameters[0].RefKind == RefKind.Ref ? RefKind.Ref : RefKindExtensions.StrictIn],
                        expanded: false,
                        argsToParamsOpt: default,
                        defaultArguments: default,
                        constantValueOpt: null,
                        initializerExpressionOpt: null,
                        type: readOnlySpanCtorRefParamChar.ContainingType);

                    args[i] = new BoundSequence(
                        arg.Syntax,
                        [],
                        [tempAssignment],
                        wrappedChar,
                        wrappedChar.Type);
                }
                else
                {
                    Debug.Assert(arg.HasAnyErrors || arg.Type.SpecialType == SpecialType.System_String);
                    Debug.Assert(stringImplicitConversionToReadOnlySpan is not null);
                    args[i] = BoundCall.Synthesized(arg.Syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, stringImplicitConversionToReadOnlySpan, arg);
                }
            }

            var concatCall = BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, spanConcat, args.ToImmutableAndFree());

            var oldSyntax = factory.Syntax;
            factory.Syntax = syntax;

            var sequence = factory.Sequence(
                localsBuilder.ToImmutableAndFree(),
                [],
                concatCall);

            factory.Syntax = oldSyntax;
            return sequence;
        }

        /// <summary>
        /// Most of the above optimizations are not applicable in expression trees as the operator
        /// must stay a binary operator. We cannot do much beyond constant folding which is done in binder.
        /// </summary>
        private BoundExpression RewriteStringConcatInExpressionLambda(
            BoundExpression original,
            BoundExpression? compoundAssignmentLeft,
            BinaryOperatorKind? compoundAssignmentOperatorKind,
            SyntaxNode? compoundAssignmentSyntax,
            TypeSymbol? compoundAssignmentType)
        {
            BoundBinaryOperator? current = original as BoundBinaryOperator;
            Debug.Assert(IsBinaryStringConcatenation(current) || (compoundAssignmentLeft is not null && compoundAssignmentOperatorKind is not null && compoundAssignmentSyntax is not null));
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();

            for (;
                 current is not null && IsBinaryStringConcatenation(current);
                 current = current.Left as BoundBinaryOperator)
            {
                stack.Push(current);
            }

            BoundExpression currentResult;

            if (stack.Count == 0)
            {
                currentResult = VisitExpression(original);
            }
            else
            {
                currentResult = stack.Peek().Left;
                currentResult = VisitExpression(currentResult);
            }

            while (stack.TryPop(out current))
            {
                var right = VisitExpression(current.Right);

                currentResult = createBinaryOperator(this, current.OperatorKind, currentResult, right, current.Syntax, current.Type);
            }

            if (compoundAssignmentLeft is not null)
            {
                currentResult = createBinaryOperator(this, compoundAssignmentOperatorKind!.Value, compoundAssignmentLeft, currentResult, compoundAssignmentSyntax!, compoundAssignmentType!);
            }

            stack.Free();
            return currentResult;

            static BoundExpression createBinaryOperator(LocalRewriter self, BinaryOperatorKind operatorKind, BoundExpression currentResult, BoundExpression right, SyntaxNode syntax, TypeSymbol type)
            {
                SpecialMember member = (operatorKind == BinaryOperatorKind.StringConcatenation) ?
                    SpecialMember.System_String__ConcatStringString :
                    SpecialMember.System_String__ConcatObjectObject;

                var method = self.UnsafeGetSpecialTypeMethod(syntax, member);
                Debug.Assert((object)method != null);

                currentResult = new BoundBinaryOperator(syntax, operatorKind, constantValueOpt: null, method, constrainedToTypeOpt: null, default(LookupResultKind), currentResult, right, type);
                return currentResult;
            }
        }

        /// <summary>
        /// Returns an expression which converts the given expression into a string (or null).
        /// If necessary, this invokes .ToString() on the expression, to avoid boxing value types.
        /// </summary>
        private BoundExpression ConvertConcatExprToString(BoundExpression expr)
        {
            var syntax = expr.Syntax;

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

            // If expression is of form `constantChar.ToString()` then unwrap it from a `ToString()` call so we can lower it to a constant later
            // NOTE: We get `object.ToString()` from a compilation because we just need to compare symbols and don't need all error recovery of `TryGetSpecialTypeMethod`
            if (expr is BoundCall { Type.SpecialType: SpecialType.System_String, Method: { Name: "ToString" } method, ReceiverOpt: { Type: NamedTypeSymbol { SpecialType: SpecialType.System_Char } charType, ConstantValueOpt.IsChar: true } } call &&
                method.GetLeastOverriddenMember(charType) == _compilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString))
            {
                expr = call.ReceiverOpt;
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

            Debug.Assert(expr.Type is not null);

            // If it's a string already, just return it
            if (expr.Type.IsStringType())
            {
                return expr;
            }

            // Evaluate toString at the last possible moment, to avoid spurious diagnostics if it's missing.
            // All code paths below here use it.
            var objectToStringMethod = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_Object__ToString);

            // If it's a struct which has overridden ToString, find that method. Note that we might fail to
            // find it, e.g. if object.ToString is missing
            MethodSymbol? structToStringMethod = null;
            if (expr.Type.IsValueType && !expr.Type.IsTypeParameter())
            {
                var type = (NamedTypeSymbol)expr.Type;
                var typeToStringMembers = type.GetMembers(objectToStringMethod.Name);
                foreach (var member in typeToStringMembers)
                {
                    if (member is MethodSymbol toStringMethod &&
                        toStringMethod.GetLeastOverriddenMethod(type) == (object)objectToStringMethod)
                    {
                        structToStringMethod = toStringMethod;
                        break;
                    }
                }
            }

            // If it's one of special value types in the given range (and not a field of a MarshalByRef object),
            // it should have its own ToString method (but we might fail to find it if object.ToString is missing).
            // Assume that this won't be removed, and emit a direct call rather than a constrained virtual call.
            // This logic can probably be applied to all special types,
            // but that would introduce a silent change every time a new special type is added,
            // and if at some point the assumption no longer holds, this would be a bug, which might not get noticed.
            // So to be extra safe we constrain the check to a fixed range of special types
            if (structToStringMethod != null &&
                expr.Type.SpecialType.CanOptimizeBehavior() &&
                !isFieldOfMarshalByRef(expr, _compilation))
            {
                return BoundCall.Synthesized(syntax, expr, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, structToStringMethod);
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
                    expr = new BoundPassByCopy(syntax, expr, expr.Type);
                }
                return BoundCall.Synthesized(syntax, expr, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, objectToStringMethod);
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
    }
}
