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
                    case CollectionExpressionTypeKind.ImplementsIEnumerableT:
                    case CollectionExpressionTypeKind.ImplementsIEnumerable:
                        return VisitCollectionInitializerCollectionExpression(node, node.Type);
                    case CollectionExpressionTypeKind.Array:
                    case CollectionExpressionTypeKind.Span:
                    case CollectionExpressionTypeKind.ReadOnlySpan:
                        Debug.Assert(elementType is { });
                        return VisitArrayOrSpanCollectionExpression(node, collectionTypeKind, node.Type, TypeWithAnnotations.Create(elementType));
                    case CollectionExpressionTypeKind.ImmutableArray:
                        Debug.Assert(elementType is { });
                        return VisitImmutableArrayCollectionExpression(node, elementType);
                    case CollectionExpressionTypeKind.List:
                        return CreateAndPopulateList(node, TypeWithAnnotations.Create(elementType));
                    case CollectionExpressionTypeKind.CollectionBuilder:
                        return VisitCollectionBuilderCollectionExpression(node);
                    case CollectionExpressionTypeKind.ArrayInterface:
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

        private BoundExpression VisitImmutableArrayCollectionExpression(BoundCollectionExpression node, TypeSymbol elementType)
        {
            var elementTypeWithAnnotations = TypeWithAnnotations.Create(elementType);
            var arrayCreation = VisitArrayOrSpanCollectionExpression(
                node,
                CollectionExpressionTypeKind.Array,
                ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementTypeWithAnnotations),
                elementTypeWithAnnotations);
            var asImmutableArray = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_ImmutableCollectionsMarshal__AsImmutableArray_T)!;
            // ImmutableCollectionsMarshal.AsImmutableArray(arrayCreation)
            return _factory.StaticCall(asImmutableArray.Construct(elementType), ImmutableArray.Create(arrayCreation));
        }

        private BoundExpression VisitArrayOrSpanCollectionExpression(BoundCollectionExpression node, CollectionExpressionTypeKind collectionTypeKind, TypeSymbol collectionType, TypeWithAnnotations elementType)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_additionalLocals is { });
            Debug.Assert(node.CollectionCreation is null); // shouldn't have generated a constructor call
            Debug.Assert(node.Placeholder is null);

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

                if (elements.Length == 0)
                {
                    // `default(Span<T>)` is the best way to make empty Spans
                    return _factory.Default(collectionType);
                }

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
            if (ShouldUseKnownLength(node, out _))
            {
                array = CreateAndPopulateArray(node, arrayType);
            }
            else
            {
                // The array initializer has an unknown length, so we'll create an intermediate List<T> instance.
                // https://github.com/dotnet/roslyn/issues/68785: Emit Enumerable.TryGetNonEnumeratedCount() and avoid intermediate List<T> at runtime.
                var list = CreateAndPopulateList(node, elementType);

                Debug.Assert(list.Type is { });
                Debug.Assert(list.Type.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T), TypeCompareKind.AllIgnoreOptions));

                var listToArray = ((MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__ToArray)!).AsMember((NamedTypeSymbol)list.Type);
                array = _factory.Call(list, listToArray);
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

            var elements = node.Elements;
            var rewrittenReceiver = VisitExpression(node.CollectionCreation);

            Debug.Assert(rewrittenReceiver is { });

            // Create a temp for the collection.
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal temp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
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
                        MakeCollectionExpressionSpreadElement(
                            spreadElement,
                            VisitExpression(spreadElement.Expression),
                            static (rewriter, iteratorBody) => rewriter.VisitStatement(iteratorBody)!),
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
            Debug.Assert(node.CollectionCreation is null);
            Debug.Assert(node.Placeholder is null);

            var syntax = node.Syntax;
            var collectionType = (NamedTypeSymbol)node.Type;
            var elementType = collectionType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single();
            BoundExpression arrayOrList;

            if (collectionType.OriginalDefinition.SpecialType is
                SpecialType.System_Collections_Generic_IEnumerable_T or
                SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
                SpecialType.System_Collections_Generic_IReadOnlyList_T)
            {
                int numberIncludingLastSpread;
                bool useKnownLength = ShouldUseKnownLength(node, out numberIncludingLastSpread);

                if (numberIncludingLastSpread == 0 && node.Elements.Length == 0)
                {
                    // arrayOrList = Array.Empty<ElementType>();
                    arrayOrList = CreateEmptyArray(syntax, ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType));
                }
                else
                {
                    var typeArgs = ImmutableArray.Create(elementType);
                    var synthesizedType = _factory.ModuleBuilderOpt.EnsureReadOnlyListTypeExists(syntax, hasKnownLength: useKnownLength, _diagnostics.DiagnosticBag).Construct(typeArgs);
                    if (synthesizedType.IsErrorType())
                    {
                        return BadExpression(node);
                    }

                    BoundExpression fieldValue;
                    if (useKnownLength)
                    {
                        // fieldValue = new ElementType[] { e1, ..., eN };
                        var arrayType = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType);
                        fieldValue = CreateAndPopulateArray(node, arrayType);
                    }
                    else
                    {
                        // fieldValue = new List<ElementType> { e1, ..., eN };
                        fieldValue = CreateAndPopulateList(node, elementType);
                    }

                    // arrayOrList = new <>z__ReadOnlyList<ElementType>(fieldValue);
                    arrayOrList = new BoundObjectCreationExpression(syntax, synthesizedType.Constructors.Single(), fieldValue) { WasCompilerGenerated = true };
                }
            }
            else
            {
                arrayOrList = CreateAndPopulateList(node, elementType);
            }

            return _factory.Convert(collectionType, arrayOrList);
        }

        private BoundExpression VisitCollectionBuilderCollectionExpression(BoundCollectionExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });
            Debug.Assert(node.CollectionCreation is null);
            Debug.Assert(node.Placeholder is null);
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

            return !node.HasSpreadElements(out _, out _) &&
                node.Elements.Length > 0 &&
                CodeGenerator.IsTypeAllowedInBlobWrapper(elementType.EnumUnderlyingTypeOrSelf().SpecialType) &&
                node.Elements.All(e => e.ConstantValueOpt is { });
        }

        private bool ShouldUseInlineArray(BoundCollectionExpression node)
        {
            Debug.Assert(node.CollectionTypeKind is
                CollectionExpressionTypeKind.ReadOnlySpan or
                CollectionExpressionTypeKind.Span or
                CollectionExpressionTypeKind.CollectionBuilder);

            return !node.HasSpreadElements(out _, out _) &&
                node.Elements.Length > 0 &&
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
        /// Returns true if the collection expression has a known length and that length should be used
        /// in the lowered code to avoid resizing the collection instance, or allocating intermediate storage,
        /// during construction. If the collection expression includes spreads, the spreads must be countable.
        /// The caller will need to delay adding elements and iterating spreads until the last spread has been
        /// evaluated, to determine the overall length of the collection. Therefore, this method only returns
        /// true if the number of preceding elements is below a maximum.
        /// </summary>
        private static bool ShouldUseKnownLength(BoundCollectionExpression node, out int numberIncludingLastSpread)
        {
            // The maximum number of collection expression elements that will be rewritten into temporaries.
            // The value is arbitrary but small to avoid significant stack size for the containing method
            // while also allowing using the known length for common cases. In particular, this allows
            // using the known length for simple concatenation of two elements [e, ..y] or [..x, ..y].
            // Temporaries are only needed up to the last spread, so this also allows [..x, e1, e2, ...].
            const int maxTemporaries = 3;
            int n;
            bool hasKnownLength;
            node.HasSpreadElements(out n, out hasKnownLength);
            if (hasKnownLength && n <= maxTemporaries)
            {
                numberIncludingLastSpread = n;
                return true;
            }
            numberIncludingLastSpread = 0;
            return false;
        }

        /// <summary>
        /// Create and populate an array from a collection expression where the
        /// collection has a known length, although possibly including spreads.
        /// </summary>
        private BoundExpression CreateAndPopulateArray(BoundCollectionExpression node, ArrayTypeSymbol arrayType)
        {
            var syntax = node.Syntax;
            var elements = node.Elements;

            int numberIncludingLastSpread;
            if (!ShouldUseKnownLength(node, out numberIncludingLastSpread))
            {
                // Should have been handled by the caller.
                throw ExceptionUtilities.UnexpectedValue(node);
            }

            if (numberIncludingLastSpread == 0)
            {
                int knownLength = elements.Length;
                if (knownLength == 0)
                {
                    return CreateEmptyArray(syntax, arrayType);
                }

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

            BoundAssignmentOperator assignmentToTemp;
            var localsBuilder = ArrayBuilder<BoundLocal>.GetInstance();
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();

            RewriteCollectionExpressionElementsIntoTemporaries(elements, numberIncludingLastSpread, localsBuilder, sideEffects);

            // int index = 0;
            BoundLocal indexTemp = _factory.StoreToTemp(
                _factory.Literal(0),
                out assignmentToTemp,
                isKnownToReferToTempIfReferenceType: true);
            localsBuilder.Add(indexTemp);
            sideEffects.Add(assignmentToTemp);

            // ElementType[] array = new ElementType[N + s1.Length + ...];
            BoundLocal arrayTemp = _factory.StoreToTemp(
                new BoundArrayCreation(syntax,
                    ImmutableArray.Create(GetKnownLengthExpression(elements, numberIncludingLastSpread, localsBuilder)),
                    initializerOpt: null,
                    arrayType),
                out assignmentToTemp,
                isKnownToReferToTempIfReferenceType: true);
            localsBuilder.Add(arrayTemp);
            sideEffects.Add(assignmentToTemp);

            AddCollectionExpressionElements(
                elements,
                arrayTemp,
                localsBuilder,
                numberIncludingLastSpread,
                sideEffects,
                (ArrayBuilder<BoundExpression> expressions, BoundExpression arrayTemp, BoundExpression rewrittenValue) =>
                {
                    Debug.Assert(arrayTemp.Type is ArrayTypeSymbol);
                    Debug.Assert(indexTemp.Type is { SpecialType: SpecialType.System_Int32 });

                    var expressionSyntax = rewrittenValue.Syntax;
                    var elementType = ((ArrayTypeSymbol)arrayTemp.Type).ElementType;

                    // array[index] = element;
                    expressions.Add(
                        new BoundAssignmentOperator(
                            expressionSyntax,
                            _factory.ArrayAccess(arrayTemp, indexTemp),
                            rewrittenValue,
                            isRef: false,
                            elementType));
                    // index = index + 1;
                    expressions.Add(
                        new BoundAssignmentOperator(
                            expressionSyntax,
                            indexTemp,
                            _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, _factory.Literal(1)),
                            isRef: false,
                            indexTemp.Type));
                });

            var locals = localsBuilder.SelectAsArray(l => l.LocalSymbol);
            localsBuilder.Free();

            return new BoundSequence(
                syntax,
                locals,
                sideEffects.ToImmutableAndFree(),
                arrayTemp,
                arrayType);
        }

        /// <summary>
        /// Create and populate an list from a collection expression.
        /// The collection may or may not have a known length.
        /// </summary>
        private BoundExpression CreateAndPopulateList(BoundCollectionExpression node, TypeWithAnnotations elementType)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.CollectionCreation is null);
            Debug.Assert(node.Placeholder is null);

            var elements = node.Elements;
            var typeArguments = ImmutableArray.Create(elementType);
            var collectionType = _factory.WellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(typeArguments);

            var localsBuilder = ArrayBuilder<BoundLocal>.GetInstance();
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(elements.Length + 1);

            int numberIncludingLastSpread;
            bool useKnownLength = ShouldUseKnownLength(node, out numberIncludingLastSpread);
            RewriteCollectionExpressionElementsIntoTemporaries(elements, numberIncludingLastSpread, localsBuilder, sideEffects);

            bool useOptimizations = false;
            MethodSymbol? setCount = null;
            MethodSymbol? asSpan = null;

            // Do not use optimizations in async method since the optimizations require Span<T>.
            if (useKnownLength && elements.Length > 0 && _factory.CurrentFunction?.IsAsync == false)
            {
                setCount = ((MethodSymbol?)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_CollectionsMarshal__SetCount_T))?.Construct(typeArguments);
                asSpan = ((MethodSymbol?)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_CollectionsMarshal__AsSpan_T))?.Construct(typeArguments);

                if (setCount is { } && asSpan is { })
                {
                    useOptimizations = true;
                }
            }

            BoundObjectCreationExpression rewrittenReceiver;
            if (useKnownLength && elements.Length > 0 && !useOptimizations)
            {
                // List<ElementType> list = new(N + s1.Length + ...);
                var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ctorInt32)).AsMember(collectionType);
                rewrittenReceiver = _factory.New(constructor, ImmutableArray.Create(GetKnownLengthExpression(elements, numberIncludingLastSpread, localsBuilder)));
            }
            else
            {
                // List<ElementType> list = new();
                var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ctor)).AsMember(collectionType);
                rewrittenReceiver = _factory.New(constructor, ImmutableArray<BoundExpression>.Empty);
            }

            // Create a temp for the list.
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal listTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            localsBuilder.Add(listTemp);
            sideEffects.Add(assignmentToTemp);

            // Use Span<T> if CollectionsMarshal methods are available, otherwise use List<T>.Add().
            if (useOptimizations)
            {
                Debug.Assert(useKnownLength);
                Debug.Assert(setCount is { });
                Debug.Assert(asSpan is { });

                // CollectionsMarshal.SetCount<ElementType>(list, N + s1.Length + ...);
                sideEffects.Add(_factory.Call(receiver: null, setCount, listTemp, GetKnownLengthExpression(elements, numberIncludingLastSpread, localsBuilder)));

                // var span = CollectionsMarshal.AsSpan<ElementType(list);
                BoundLocal spanTemp = _factory.StoreToTemp(_factory.Call(receiver: null, asSpan, listTemp), out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
                localsBuilder.Add(spanTemp);
                sideEffects.Add(assignmentToTemp);

                // Populate the span.
                var spanGetItem = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Span_T__get_Item)).AsMember((NamedTypeSymbol)spanTemp.Type);

                // int index = 0;
                BoundLocal indexTemp = _factory.StoreToTemp(
                    _factory.Literal(0),
                    out assignmentToTemp,
                    isKnownToReferToTempIfReferenceType: true);
                localsBuilder.Add(indexTemp);
                sideEffects.Add(assignmentToTemp);

                AddCollectionExpressionElements(
                    elements,
                    spanTemp,
                    localsBuilder,
                    numberIncludingLastSpread,
                    sideEffects,
                    (ArrayBuilder<BoundExpression> expressions, BoundExpression spanTemp, BoundExpression rewrittenValue) =>
                    {
                        Debug.Assert(spanTemp.Type is NamedTypeSymbol);
                        Debug.Assert(indexTemp.Type is { SpecialType: SpecialType.System_Int32 });

                        var expressionSyntax = rewrittenValue.Syntax;
                        var elementType = ((NamedTypeSymbol)spanTemp.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;

                        // span[index] = element;
                        expressions.Add(
                            new BoundAssignmentOperator(
                                expressionSyntax,
                                _factory.Call(spanTemp, spanGetItem, indexTemp),
                                rewrittenValue,
                                isRef: false,
                                elementType));
                        // index = index + 1;
                        expressions.Add(
                            new BoundAssignmentOperator(
                                expressionSyntax,
                                indexTemp,
                                _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, _factory.Literal(1)),
                                isRef: false,
                                indexTemp.Type));
                    });
            }
            else
            {
                var addMethod = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__Add)).AsMember(collectionType);
                AddCollectionExpressionElements(
                    elements,
                    listTemp,
                    localsBuilder,
                    numberIncludingLastSpread,
                    sideEffects,
                    (ArrayBuilder<BoundExpression> expressions, BoundExpression listTemp, BoundExpression rewrittenValue) =>
                    {
                        // list.Add(element);
                        expressions.Add(
                            _factory.Call(listTemp, addMethod, rewrittenValue));
                    });
            }

            var locals = localsBuilder.SelectAsArray(l => l.LocalSymbol);
            localsBuilder.Free();

            return new BoundSequence(
                node.Syntax,
                locals,
                sideEffects.ToImmutableAndFree(),
                listTemp,
                collectionType);
        }

        private BoundExpression RewriteCollectionExpressionElementExpression(BoundExpression element)
        {
            var expression = element is BoundCollectionExpressionSpreadElement spreadElement ?
                spreadElement.Expression :
                element;
            return VisitExpression(expression);
        }

        private void RewriteCollectionExpressionElementsIntoTemporaries(
            ImmutableArray<BoundExpression> elements,
            int numberIncludingLastSpread,
            ArrayBuilder<BoundLocal> locals,
            ArrayBuilder<BoundExpression> sideEffects)
        {
            for (int i = 0; i < numberIncludingLastSpread; i++)
            {
                var rewrittenExpression = RewriteCollectionExpressionElementExpression(elements[i]);
                BoundAssignmentOperator assignmentToTemp;
                BoundLocal temp = _factory.StoreToTemp(rewrittenExpression, out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
                locals.Add(temp);
                sideEffects.Add(assignmentToTemp);
            }
        }

        private void AddCollectionExpressionElements(
            ImmutableArray<BoundExpression> elements,
            BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundLocal> rewrittenExpressions,
            int numberIncludingLastSpread,
            ArrayBuilder<BoundExpression> sideEffects,
            Action<ArrayBuilder<BoundExpression>, BoundExpression, BoundExpression> addElement)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                var rewrittenExpression = i < numberIncludingLastSpread ?
                    rewrittenExpressions[i] :
                    RewriteCollectionExpressionElementExpression(element);

                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    var rewrittenElement = MakeCollectionExpressionSpreadElement(
                        spreadElement,
                        rewrittenExpression,
                        (_, iteratorBody) =>
                        {
                            var rewrittenValue = VisitExpression(((BoundExpressionStatement)iteratorBody).Expression);
                            var builder = ArrayBuilder<BoundExpression>.GetInstance();
                            addElement(builder, rewrittenReceiver, rewrittenValue);
                            var statements = builder.SelectAsArray(expr => (BoundStatement)new BoundExpressionStatement(expr.Syntax, expr));
                            builder.Free();
                            Debug.Assert(statements.Length > 0);
                            return statements.Length == 1 ?
                                statements[0] :
                                new BoundBlock(iteratorBody.Syntax, locals: ImmutableArray<LocalSymbol>.Empty, statements);
                        });
                    sideEffects.Add(rewrittenElement);
                }
                else
                {
                    addElement(sideEffects, rewrittenReceiver, rewrittenExpression);
                }
            }
        }

        private BoundExpression GetKnownLengthExpression(ImmutableArray<BoundExpression> elements, int numberIncludingLastSpread, ArrayBuilder<BoundLocal> rewrittenExpressions)
        {
            Debug.Assert(rewrittenExpressions.Count >= numberIncludingLastSpread);

            int initialLength = 0;
            BoundExpression? sum = null;

            for (int i = 0; i < numberIncludingLastSpread; i++)
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

            initialLength += elements.Length - numberIncludingLastSpread;

            if (initialLength > 0)
            {
                var otherElements = _factory.Literal(initialLength);
                sum = sum is null ?
                    otherElements :
                    add(otherElements, sum);
            }

            Debug.Assert(sum is { });
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
