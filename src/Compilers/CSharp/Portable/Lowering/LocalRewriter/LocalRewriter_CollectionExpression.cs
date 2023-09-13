// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitCollectionExpression(BoundCollectionExpression node)
        {
            // BoundCollectionExpression should be handled in VisitConversion().
            throw ExceptionUtilities.Unreachable();
        }

        private BoundExpression RewriteCollectionExpressionConversion(Conversion conversion, BoundCollectionExpression node)
        {
            Debug.Assert(conversion.Kind == ConversionKind.CollectionExpression);
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_additionalLocals is { });
            Debug.Assert(node.Type is { });

            var previousSyntax = _factory.Syntax;
            _factory.Syntax = node.Syntax;
            try
            {
                var collectionTypeKind = conversion.GetCollectionExpressionTypeKind(out var elementType);
                switch (collectionTypeKind)
                {
                    case CollectionExpressionTypeKind.CollectionInitializer:
                        return VisitCollectionInitializerCollectionExpression(node, node.Type);
                    case CollectionExpressionTypeKind.Array:
                    case CollectionExpressionTypeKind.Span:
                    case CollectionExpressionTypeKind.ReadOnlySpan:
                        Debug.Assert(elementType is { });
                        return VisitArrayOrSpanCollectionExpression(node, collectionTypeKind, node.Type, TypeWithAnnotations.Create(elementType));
                    case CollectionExpressionTypeKind.CollectionBuilder:
                        return VisitCollectionBuilderCollectionExpression(node);
                    case CollectionExpressionTypeKind.ListInterface:
                        return VisitListInterfaceCollectionExpression(node);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(collectionTypeKind);
                }
            }
            finally
            {
                _factory.Syntax = previousSyntax;
            }
        }

        private BoundExpression VisitArrayOrSpanCollectionExpression(BoundCollectionExpression node, CollectionExpressionTypeKind collectionTypeKind, TypeSymbol collectionType, TypeWithAnnotations elementType)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_additionalLocals is { });

            var syntax = node.Syntax;
            MethodSymbol? spanConstructor = null;

            var arrayType = collectionType as ArrayTypeSymbol;
            if (arrayType is null)
            {
                // We're constructing a Span<T> or ReadOnlySpan<T> rather than T[].
                var spanType = (NamedTypeSymbol)collectionType;
                var elements = node.Elements;

                Debug.Assert(collectionTypeKind is CollectionExpressionTypeKind.Span or CollectionExpressionTypeKind.ReadOnlySpan);
                Debug.Assert(spanType.OriginalDefinition.Equals(_compilation.GetWellKnownType(
                    collectionTypeKind == CollectionExpressionTypeKind.Span ? WellKnownType.System_Span_T : WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions));
                Debug.Assert(elementType.Equals(spanType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0], TypeCompareKind.AllIgnoreOptions));

                if (collectionTypeKind == CollectionExpressionTypeKind.ReadOnlySpan &&
                    ShouldUseRuntimeHelpersCreateSpan(node, elementType.Type))
                {
                    var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Array)).AsMember(spanType);
                    return _factory.New(constructor, _factory.Array(elementType.Type, elements));
                }

                if (ShouldUseInlineArray(node) &&
                    _additionalLocals is { })
                {
                    return CreateAndPopulateSpanFromInlineArray(
                        syntax,
                        elementType,
                        elements,
                        asReadOnlySpan: collectionTypeKind == CollectionExpressionTypeKind.ReadOnlySpan,
                        _additionalLocals);
                }

                arrayType = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType);
                spanConstructor = ((MethodSymbol)_compilation.GetWellKnownTypeMember(
                    collectionTypeKind == CollectionExpressionTypeKind.Span ? WellKnownMember.System_Span_T__ctor_Array : WellKnownMember.System_ReadOnlySpan_T__ctor_Array)!).AsMember(spanType);
            }

            BoundExpression array;
            if (node.GetKnownLength(hasSpreadElements: out _) is null)
            {
                // The array initializer includes at least one spread element, so we'll create an intermediate List<T> instance.
                // https://github.com/dotnet/roslyn/issues/68785: Emit Enumerable.TryGetNonEnumeratedCount() and avoid intermediate List<T> at runtime.
                var listType = _compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(ImmutableArray.Create(elementType));
                var listToArray = ((MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__ToArray)!).AsMember(listType);
                var list = VisitCollectionInitializerCollectionExpression(node, listType);
                array = _factory.Call(list, listToArray);
            }
            else
            {
                array = CreateAndPopulateArray(node, arrayType);
            }

            if (spanConstructor is null)
            {
                return array;
            }

            Debug.Assert(TypeSymbol.Equals(array.Type, spanConstructor.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions));
            return new BoundObjectCreationExpression(syntax, spanConstructor, array);
        }

        private BoundExpression VisitCollectionInitializerCollectionExpression(BoundCollectionExpression node, TypeSymbol collectionType)
        {
            Debug.Assert(!_inExpressionLambda);

            var rewrittenReceiver = VisitExpression(node.CollectionCreation);
            Debug.Assert(rewrittenReceiver is { });

            // Create a temp for the collection.
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal temp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            var elements = node.Elements;
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(elements.Length + 1);
            sideEffects.Add(assignmentToTemp);

            var placeholder = node.Placeholder;
            Debug.Assert(placeholder is { });

            AddPlaceholderReplacement(placeholder, temp);

            foreach (var element in elements)
            {
                var rewrittenElement = element switch
                {
                    BoundCollectionElementInitializer collectionInitializer => MakeCollectionInitializer(temp, collectionInitializer),
                    BoundDynamicCollectionElementInitializer dynamicInitializer => MakeDynamicCollectionInitializer(temp, dynamicInitializer),
                    BoundCollectionExpressionSpreadElement spreadElement =>
                        MakeCollectionExpressionSpreadElement(spreadElement, VisitExpression(spreadElement.Expression), static (rewriter, iteratorBody) => rewriter.VisitStatement(iteratorBody)!),
                    _ => throw ExceptionUtilities.UnexpectedValue(element)
                };
                if (rewrittenElement != null)
                {
                    sideEffects.Add(rewrittenElement);
                }
            }

            RemovePlaceholderReplacement(placeholder);

            return new BoundSequence(
                node.Syntax,
                ImmutableArray.Create(temp.LocalSymbol),
                sideEffects.ToImmutableAndFree(),
                temp,
                collectionType);
        }

        private BoundExpression VisitListInterfaceCollectionExpression(BoundCollectionExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_factory.ModuleBuilderOpt is { });
            Debug.Assert(_diagnostics.DiagnosticBag is { });
            Debug.Assert(node.Type is NamedTypeSymbol);

            var syntax = node.Syntax;
            var collectionType = (NamedTypeSymbol)node.Type;
            BoundExpression arrayOrList;

            // Use Array.Empty<T>() rather than List<T> for an empty collection expression when
            // the target type is IEnumerable<T>, IReadOnlyCollection<T>, or IReadOnlyList<T>.
            if (collectionType is
                {
                    OriginalDefinition.SpecialType:
                        SpecialType.System_Collections_Generic_IEnumerable_T or
                        SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
                        SpecialType.System_Collections_Generic_IReadOnlyList_T,
                    TypeArgumentsWithAnnotationsNoUseSiteDiagnostics: [var elementType]
                })
            {
                int? lengthOpt = node.GetKnownLength(hasSpreadElements: out _);

                if (lengthOpt == 0)
                {
                    // arrayOrList = Array.Empty<ElementType>();
                    arrayOrList = CreateEmptyArray(syntax, ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType));
                }
                else
                {
                    bool hasKnownLength = lengthOpt.HasValue;
                    var typeArgs = ImmutableArray.Create(elementType);
                    var synthesizedType = _factory.ModuleBuilderOpt.EnsureReadOnlyListTypeExists(syntax, hasKnownLength: hasKnownLength, _diagnostics.DiagnosticBag).Construct(typeArgs);
                    if (synthesizedType.IsErrorType())
                    {
                        return BadExpression(node);
                    }

                    BoundExpression fieldValue;
                    if (hasKnownLength)
                    {
                        // fieldValue = new ElementType[] { e1, ..., eN };
                        var arrayType = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType);
                        fieldValue = CreateAndPopulateArray(node, arrayType);
                    }
                    else
                    {
                        // fieldValue = new List<ElementType> { e1, ..., eN };
                        fieldValue = VisitCollectionInitializerCollectionExpression(
                            node,
                            _factory.WellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(typeArgs));
                    }

                    // arrayOrList = new <>z__ReadOnlyList<ElementType>(fieldValue);
                    arrayOrList = new BoundObjectCreationExpression(syntax, synthesizedType.Constructors.Single(), fieldValue) { WasCompilerGenerated = true };
                }
            }
            else
            {
                arrayOrList = VisitCollectionInitializerCollectionExpression(node, collectionType);
            }

            return _factory.Convert(collectionType, arrayOrList);
        }

        private BoundExpression VisitCollectionBuilderCollectionExpression(BoundCollectionExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });
            Debug.Assert(node.CollectionBuilderMethod is { });
            Debug.Assert(node.CollectionBuilderInvocationPlaceholder is { });
            Debug.Assert(node.CollectionBuilderInvocationConversion is { });

            var constructMethod = node.CollectionBuilderMethod;

            var spanType = (NamedTypeSymbol)constructMethod.Parameters[0].Type;
            Debug.Assert(spanType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions));

            var elementType = spanType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
            BoundExpression span = VisitArrayOrSpanCollectionExpression(node, CollectionExpressionTypeKind.ReadOnlySpan, spanType, elementType);

            var invocation = new BoundCall(
                node.Syntax,
                receiverOpt: null,
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                method: constructMethod,
                arguments: ImmutableArray.Create(span),
                argumentNamesOpt: default,
                argumentRefKindsOpt: default,
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: default,
                defaultArguments: default,
                resultKind: LookupResultKind.Viable,
                type: constructMethod.ReturnType);

            var invocationPlaceholder = node.CollectionBuilderInvocationPlaceholder;
            AddPlaceholderReplacement(invocationPlaceholder, invocation);
            var result = VisitExpression(node.CollectionBuilderInvocationConversion);
            RemovePlaceholderReplacement(invocationPlaceholder);
            return result;
        }

        internal static bool ShouldUseRuntimeHelpersCreateSpan(BoundCollectionExpression node, TypeSymbol elementType)
        {
            Debug.Assert(node.CollectionTypeKind is
                CollectionExpressionTypeKind.ReadOnlySpan or
                CollectionExpressionTypeKind.CollectionBuilder);

            return node.GetKnownLength(out bool hasSpreadElements) is int length &&
                length > 0 &&
                !hasSpreadElements &&
                CodeGenerator.IsTypeAllowedInBlobWrapper(elementType.EnumUnderlyingTypeOrSelf().SpecialType) &&
                node.Elements.All(e => e.ConstantValueOpt is { });
        }

        private bool ShouldUseInlineArray(BoundCollectionExpression node)
        {
            Debug.Assert(node.CollectionTypeKind is
                CollectionExpressionTypeKind.ReadOnlySpan or
                CollectionExpressionTypeKind.Span or
                CollectionExpressionTypeKind.CollectionBuilder);

            return node.GetKnownLength(out bool hasSpreadElements) is int length &&
                length > 0 &&
                !hasSpreadElements &&
                _compilation.Assembly.RuntimeSupportsInlineArrayTypes;
        }

        private BoundExpression CreateAndPopulateSpanFromInlineArray(
            SyntaxNode syntax,
            TypeWithAnnotations elementType,
            ImmutableArray<BoundExpression> elements,
            bool asReadOnlySpan,
            ArrayBuilder<LocalSymbol> locals)
        {
            Debug.Assert(elements.Length > 0);
            Debug.Assert(_factory.ModuleBuilderOpt is { });
            Debug.Assert(_diagnostics.DiagnosticBag is { });
            Debug.Assert(_compilation.Assembly.RuntimeSupportsInlineArrayTypes);

            int arrayLength = elements.Length;
            var inlineArrayType = _factory.ModuleBuilderOpt.EnsureInlineArrayTypeExists(syntax, _factory, arrayLength, _diagnostics.DiagnosticBag).Construct(ImmutableArray.Create(elementType));
            Debug.Assert(inlineArrayType.HasInlineArrayAttribute(out int inlineArrayLength) && inlineArrayLength == arrayLength);

            var intType = _factory.SpecialType(SpecialType.System_Int32);
            MethodSymbol elementRef = _factory.ModuleBuilderOpt.EnsureInlineArrayElementRefExists(syntax, intType, _diagnostics.DiagnosticBag).
                Construct(ImmutableArray.Create(TypeWithAnnotations.Create(inlineArrayType), elementType));

            // Create an inline array and assign to a local.
            // var tmp = new <>y__InlineArrayN<ElementType>();
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal inlineArrayLocal = _factory.StoreToTemp(new BoundDefaultExpression(syntax, inlineArrayType), out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
            sideEffects.Add(assignmentToTemp);
            locals.Add(inlineArrayLocal.LocalSymbol);

            // Populate the inline array.
            // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, 0) = element0;
            // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, 1) = element1;
            // ...
            for (int i = 0; i < arrayLength; i++)
            {
                var element = VisitExpression(elements[i]);
                var call = _factory.Call(null, elementRef, inlineArrayLocal, _factory.Literal(i), useStrictArgumentRefKinds: true);
                var assignment = new BoundAssignmentOperator(syntax, call, element, type: call.Type) { WasCompilerGenerated = true };
                sideEffects.Add(assignment);
            }

            // Get a span to the inline array.
            // ... InlineArrayAsReadOnlySpan<<>y__InlineArrayN<ElementType>, ElementType>(in tmp, N)
            // or
            // ... InlineArrayAsSpan<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, N)
            MethodSymbol inlineArrayAsSpan = asReadOnlySpan ?
                _factory.ModuleBuilderOpt.EnsureInlineArrayAsReadOnlySpanExists(syntax, _factory.WellKnownType(WellKnownType.System_ReadOnlySpan_T), intType, _diagnostics.DiagnosticBag) :
                _factory.ModuleBuilderOpt.EnsureInlineArrayAsSpanExists(syntax, _factory.WellKnownType(WellKnownType.System_Span_T), intType, _diagnostics.DiagnosticBag);
            inlineArrayAsSpan = inlineArrayAsSpan.Construct(ImmutableArray.Create(TypeWithAnnotations.Create(inlineArrayType), elementType));
            var span = _factory.Call(
                receiver: null,
                inlineArrayAsSpan,
                inlineArrayLocal,
                _factory.Literal(arrayLength),
                useStrictArgumentRefKinds: true);

            Debug.Assert(span.Type is { });
            return new BoundSequence(
                syntax,
                locals: ImmutableArray<LocalSymbol>.Empty,
                sideEffects.ToImmutableAndFree(),
                span,
                span.Type);
        }

        /// <summary>
        /// Create and populate an array from a collection expression where the
        /// collection has a known length, although possibly including spreads.
        /// </summary>
        private BoundExpression CreateAndPopulateArray(BoundCollectionExpression node, ArrayTypeSymbol arrayType)
        {
            var syntax = node.Syntax;
            var elements = node.Elements;

            if (node.GetKnownLength(out bool hasSpreadElements) is not int knownLength)
            {
                // Should have been handled by the caller.
                throw ExceptionUtilities.UnexpectedValue(node);
            }

            if (knownLength == 0)
            {
                return CreateEmptyArray(syntax, arrayType);
            }

            if (!hasSpreadElements)
            {
                var initialization = new BoundArrayInitialization(
                        syntax,
                        isInferred: false,
                        elements.SelectAsArray(static (element, rewriter) => rewriter.VisitExpression(element), this));
                return new BoundArrayCreation(
                    syntax,
                    ImmutableArray.Create<BoundExpression>(
                        new BoundLiteral(
                            syntax,
                            ConstantValue.Create(knownLength),
                            _compilation.GetSpecialType(SpecialType.System_Int32))),
                    initialization,
                    arrayType)
                { WasCompilerGenerated = true };
            }

            NamedTypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            BoundAssignmentOperator assignmentToTemp;
            var localsBuilder = ArrayBuilder<BoundLocal>.GetInstance();
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();

            // Rewrite expressions into temporaries.
            foreach (var element in elements)
            {
                var expression = element is BoundCollectionExpressionSpreadElement spreadElement ?
                    spreadElement.Expression :
                    element;
                BoundLocal temp = _factory.StoreToTemp(VisitExpression(expression), out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
                localsBuilder.Add(temp);
                sideEffects.Add(assignmentToTemp);
            }

            // int length = N + s1.Length + ...;
            BoundLocal lengthTemp = _factory.StoreToTemp(GetKnownLengthExpression(elements, localsBuilder), out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            localsBuilder.Add(lengthTemp);
            sideEffects.Add(assignmentToTemp);

            // int index = 0;
            BoundLocal indexTemp = _factory.StoreToTemp(
                _factory.Literal(0),
                out assignmentToTemp,
                isKnownToReferToTempIfReferenceType: true);
            localsBuilder.Add(indexTemp);
            sideEffects.Add(assignmentToTemp);

            // ElementType[] array = new ElementType[length];
            BoundLocal arrayTemp = _factory.StoreToTemp(
                new BoundArrayCreation(syntax, ImmutableArray.Create<BoundExpression>(lengthTemp), initializerOpt: null, arrayType),
                out assignmentToTemp,
                isKnownToReferToTempIfReferenceType: true);
            localsBuilder.Add(arrayTemp);
            sideEffects.Add(assignmentToTemp);

            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                var rewrittenExpression = localsBuilder[i];

                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    var rewrittenElement = MakeCollectionExpressionSpreadElement(
                        spreadElement,
                        rewrittenExpression,
                        (_, iteratorBody) =>
                        {
                            var rewrittenValue = VisitExpression(((BoundExpressionStatement)iteratorBody).Expression);
                            return _factory.Block(
                                // array[index] = iterationLocal;
                                _factory.Assignment(
                                    _factory.ArrayAccess(arrayTemp, indexTemp),
                                    rewrittenValue),
                                // index = index + 1;
                                _factory.Assignment(
                                    indexTemp,
                                    _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, _factory.Literal(1))));
                        });
                    sideEffects.Add(rewrittenElement);
                }
                else
                {
                    // array[index] = element;
                    sideEffects.Add(
                        new BoundAssignmentOperator(
                            syntax,
                            _factory.ArrayAccess(arrayTemp, indexTemp),
                            rewrittenExpression,
                            isRef: false,
                            arrayType.ElementType));
                    // index = index + 1;
                    sideEffects.Add(
                        new BoundAssignmentOperator(
                            syntax,
                            indexTemp,
                            _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, _factory.Literal(1)),
                            isRef: false,
                            indexTemp.Type));
                }
            }

            var locals = localsBuilder.SelectAsArray(l => l.LocalSymbol);
            localsBuilder.Free();

            return new BoundSequence(
                node.Syntax,
                locals,
                sideEffects.ToImmutableAndFree(),
                arrayTemp,
                arrayType);
        }

        private BoundExpression GetKnownLengthExpression(ImmutableArray<BoundExpression> elements, ArrayBuilder<BoundLocal> rewrittenExpressions)
        {
            int initialLength = 0;
            BoundExpression? sum = null;

            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                var rewrittenExpression = rewrittenExpressions[i];

                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    var collectionPlaceholder = spreadElement.ExpressionPlaceholder;
                    Debug.Assert(collectionPlaceholder is { });
                    AddPlaceholderReplacement(collectionPlaceholder, rewrittenExpressions[i]);
                    var lengthAccess = VisitExpression(spreadElement.LengthOrCount);
                    RemovePlaceholderReplacement(collectionPlaceholder);

                    Debug.Assert(lengthAccess is { });
                    sum = add(sum, lengthAccess);
                }
                else
                {
                    initialLength++;
                }
            }

            Debug.Assert(sum is { });

            if (initialLength > 0)
            {
                sum = add(_factory.Literal(initialLength), sum);
            }

            return sum;

            BoundExpression add(BoundExpression? sum, BoundExpression value)
            {
                return sum is null ?
                    value :
                    _factory.Binary(BinaryOperatorKind.Addition, sum.Type!, sum, value);
            }
        }

        private BoundExpression MakeCollectionExpressionSpreadElement(
            BoundCollectionExpressionSpreadElement node,
            BoundExpression rewrittenExpression,
            Func<LocalRewriter, BoundStatement, BoundStatement> getRewrittenBody)
        {
            var enumeratorInfo = node.EnumeratorInfoOpt;
            var convertedExpression = (BoundConversion?)node.Conversion;
            var expressionPlaceholder = node.ExpressionPlaceholder;
            var elementPlaceholder = node.ElementPlaceholder;
            var iteratorBody = node.IteratorBody;

            Debug.Assert(enumeratorInfo is { });
            Debug.Assert(convertedExpression is { });
            Debug.Assert(expressionPlaceholder is { });
            Debug.Assert(elementPlaceholder is { });
            Debug.Assert(iteratorBody is { });

            AddPlaceholderReplacement(expressionPlaceholder, rewrittenExpression);

            var iterationVariable = _factory.SynthesizedLocal(enumeratorInfo.ElementType, node.Syntax);
            var iterationLocal = _factory.Local(iterationVariable);

            AddPlaceholderReplacement(elementPlaceholder, iterationLocal);
            var rewrittenBody = getRewrittenBody(this, iteratorBody);
            RemovePlaceholderReplacement(elementPlaceholder);

            var iterationVariables = ImmutableArray.Create(iterationVariable);
            var breakLabel = new GeneratedLabelSymbol("break");
            var continueLabel = new GeneratedLabelSymbol("continue");

            BoundStatement statement;
            if (convertedExpression.Operand.Type is ArrayTypeSymbol arrayType)
            {
                if (arrayType.IsSZArray)
                {
                    statement = RewriteSingleDimensionalArrayForEachEnumerator(
                        node,
                        convertedExpression.Operand,
                        elementPlaceholder: null,
                        elementConversion: null,
                        iterationVariables,
                        deconstruction: null,
                        breakLabel,
                        continueLabel,
                        rewrittenBody);
                }
                else
                {
                    statement = RewriteMultiDimensionalArrayForEachEnumerator(
                        node,
                        convertedExpression.Operand,
                        elementPlaceholder: null,
                        elementConversion: null,
                        iterationVariables,
                        deconstruction: null,
                        breakLabel,
                        continueLabel,
                        rewrittenBody);
                }
            }
            else
            {
                statement = RewriteForEachEnumerator(
                    node,
                    convertedExpression,
                    enumeratorInfo,
                    elementPlaceholder: null,
                    elementConversion: null,
                    iterationVariables,
                    deconstruction: null,
                    awaitableInfo: null,
                    breakLabel,
                    continueLabel,
                    rewrittenBody);
            }

            RemovePlaceholderReplacement(expressionPlaceholder);

            _needsSpilling = true;
            return _factory.SpillSequence(
                ImmutableArray<LocalSymbol>.Empty,
                ImmutableArray.Create(statement),
                result: _factory.Literal(0)); // result is unused
        }
    }
}
