// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitCollectionExpression(BoundCollectionExpression node)
        {
            // BoundCollectionExpression should be handled in VisitConversion().
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitUnconvertedCollectionExpression(BoundUnconvertedCollectionExpression node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private BoundExpression RewriteCollectionExpressionConversion(Conversion conversion, BoundCollectionExpression node, ParameterSymbol? passedAsParameter)
        {
            Debug.Assert(conversion.Kind == ConversionKind.CollectionExpression);
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_additionalLocals is { });
            Debug.Assert(node.Type is { });

            var previousSyntax = _factory.Syntax;
            _factory.Syntax = node.Syntax;
            try
            {
                var collectionTypeKind = conversion.GetCollectionExpressionTypeKind(out var elementType, out _, out _);
                switch (collectionTypeKind)
                {
                    case CollectionExpressionTypeKind.ImplementsIEnumerable:
                        if (ConversionsBase.IsSpanOrListType(_compilation, node.Type, WellKnownType.System_Collections_Generic_List_T, out var listElementType))
                        {
                            if (TryRewriteSingleElementSpreadToList(node, listElementType, out var result))
                            {
                                return result;
                            }

                            if (useListOptimization(_compilation, node))
                            {
                                return CreateAndPopulateList(node, listElementType, node.Elements.SelectAsArray(static (element, node) => unwrapListElement(node, element), node));
                            }
                        }
                        return VisitCollectionInitializerCollectionExpression(node, node.Type);
                    case CollectionExpressionTypeKind.Array:
                    case CollectionExpressionTypeKind.Span:
                    case CollectionExpressionTypeKind.ReadOnlySpan:
                        Debug.Assert(elementType is { });
                        return VisitArrayOrSpanCollectionExpression(node, node.Type, passedAsParameter);
                    case CollectionExpressionTypeKind.CollectionBuilder:
                        // A few special cases when a collection type is an ImmutableArray<T>
                        if ((object)node.Type.OriginalDefinition == _compilation.GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T))
                        {
                            return VisitArrayOrSpanCollectionExpression(node, node.Type, passedAsParameter);
                        }

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

            // If the collection type is List<T> and items are added using the expected List<T>.Add(T) method,
            // then construction can be optimized to use CollectionsMarshal methods.
            static bool useListOptimization(CSharpCompilation compilation, BoundCollectionExpression node)
            {
                var elements = node.Elements;
                if (elements.Length == 0)
                {
                    return true;
                }
                var addMethod = (MethodSymbol?)compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__Add);
                if (addMethod is null)
                {
                    return false;
                }
                return elements.All(canOptimizeListElement, addMethod);
            }

            static bool canOptimizeListElement(BoundNode element, MethodSymbol addMethod)
            {
                BoundExpression expr;
                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    Debug.Assert(spreadElement.IteratorBody is { });
                    expr = ((BoundExpressionStatement)spreadElement.IteratorBody).Expression;
                }
                else
                {
                    expr = (BoundExpression)element;
                }
                if (expr is BoundCollectionElementInitializer collectionInitializer)
                {
                    return addMethod.Equals(collectionInitializer.AddMethod.OriginalDefinition);
                }
                return false;
            }

            static BoundNode unwrapListElement(BoundCollectionExpression node, BoundNode element)
            {
                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    Debug.Assert(spreadElement.IteratorBody is { });
                    var iteratorBody = Binder.GetUnderlyingCollectionExpressionElement(node, ((BoundExpressionStatement)spreadElement.IteratorBody).Expression, throwOnErrors: true);
                    Debug.Assert(iteratorBody is { });
                    return spreadElement.Update(
                        spreadElement.Expression,
                        spreadElement.ExpressionPlaceholder,
                        spreadElement.Conversion,
                        spreadElement.EnumeratorInfoOpt,
                        spreadElement.LengthOrCount,
                        spreadElement.ElementPlaceholder,
                        new BoundExpressionStatement(iteratorBody.Syntax, iteratorBody));
                }
                else
                {
                    var result = Binder.GetUnderlyingCollectionExpressionElement(node, (BoundExpression)element, throwOnErrors: true);
                    Debug.Assert(result is { });
                    return result;
                }
            }
        }

        // If we have something like `List<int> l = [.. someEnumerable]`
        // try rewrite it using `Enumerable.ToList` member if possible
        private bool TryRewriteSingleElementSpreadToList(BoundCollectionExpression node, TypeWithAnnotations listElementType, [NotNullWhen(true)] out BoundExpression? result)
        {
            result = null;

            if (node.Elements is not [BoundCollectionExpressionSpreadElement singleSpread])
            {
                return false;
            }

            if (!TryGetWellKnownTypeMember(node.Syntax, WellKnownMember.System_Linq_Enumerable__ToList, out MethodSymbol? toListGeneric, isOptional: true))
            {
                return false;
            }

            var toListOfElementType = toListGeneric.Construct([listElementType]);

            Debug.Assert(singleSpread.Expression.Type is not null);

            if (!ShouldUseIEnumerableBulkAddMethod(singleSpread.Expression.Type, toListOfElementType.Parameters[0].Type, singleSpread.EnumeratorInfoOpt?.GetEnumeratorInfo.Method))
            {
                return false;
            }

            var rewrittenSpreadExpression = VisitExpression(singleSpread.Expression);
            result = _factory.Call(receiver: null, toListOfElementType, rewrittenSpreadExpression);
            return true;
        }

        /// <summary>
        /// Decides if a bulk-add method such as AddRange, ToList, ToArray, etc. is suitable for copying a spread value with type 'spreadType' to the destination collection.
        /// </summary>
        private bool ShouldUseIEnumerableBulkAddMethod(TypeSymbol spreadType, TypeSymbol targetEnumerableType, MethodSymbol? getEnumeratorMethod)
        {
            Debug.Assert(targetEnumerableType.OriginalDefinition == (object)_compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T));

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            Conversion conversion;

            // If collection has a struct enumerator but doesn't implement ICollection<T>
            // then manual `foreach` is always more efficient then using `ToList` or `AddRange` methods
            if (getEnumeratorMethod?.ReturnType.IsValueType == true)
            {
                var iCollectionOfTType = _compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T);
                var iCollectionOfElementType = iCollectionOfTType.Construct(((NamedTypeSymbol)targetEnumerableType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);

                conversion = _compilation.Conversions.ClassifyBuiltInConversion(spreadType, iCollectionOfElementType, isChecked: false, ref discardedUseSiteInfo);
                if (conversion.Kind is not (ConversionKind.Identity or ConversionKind.ImplicitReference))
                {
                    return false;
                }
            }

            conversion = _compilation.Conversions.ClassifyImplicitConversionFromType(spreadType, targetEnumerableType, ref discardedUseSiteInfo);
            return conversion.Kind is ConversionKind.Identity or ConversionKind.ImplicitReference;
        }

        private static bool CanOptimizeSingleSpreadAsCollectionBuilderArgument(BoundCollectionExpression node, [NotNullWhen(true)] out BoundExpression? spreadExpression)
        {
            spreadExpression = null;

            if (node is
                {
                    CollectionBuilderMethod: { } builder,
                    Elements: [BoundCollectionExpressionSpreadElement { Expression: { Type: NamedTypeSymbol spreadType } expr }],
                } &&
                ConversionsBase.HasIdentityConversion(builder.Parameters[0].Type, spreadType) &&
                (!builder.ReturnType.IsRefLikeType || builder.Parameters[0].EffectiveScope == ScopedKind.ScopedValue))
            {
                spreadExpression = expr;
            }

            return spreadExpression is not null;
        }

        /// <summary>
        /// Create a collection value which may involve creating an array.
        /// Handles types 'T[]', 'Span{T}', 'ReadOnlySpan{T}', 'ImmutableArray{T}'.
        /// Note that Span/ROS/ImmutableArray cases, may or may not involve us actually creating an array.
        /// Depending on the collection elements, available APIs, or other context, we may use some other strategy.
        /// </summary>
        private BoundExpression VisitArrayOrSpanCollectionExpression(BoundCollectionExpression node, TypeSymbol collectionType, ParameterSymbol? passedAsParameter)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_additionalLocals is { });
            Debug.Assert(node.CollectionCreation is null); // shouldn't have generated a constructor call
            Debug.Assert(node.Placeholder is null);
            Debug.Assert(collectionType is ArrayTypeSymbol or NamedTypeSymbol);

            if (collectionType is ArrayTypeSymbol arrayType)
            {
                return createArray(node, arrayType, targetsReadOnlyCollection: false);
            }

            var namedType = (NamedTypeSymbol)collectionType;

            if (namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T)))
            {
                return createImmutableArray(node, namedType);
            }

            Debug.Assert(namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Span_T)) ||
                         namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T)));

            return createSpan(node, namedType, isReadOnlySpan: namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T)));

            ArrayTypeSymbol getBackingArrayType(NamedTypeSymbol collectionType)
            {
                Debug.Assert(collectionType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T) ||
                    collectionType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_Span_T) ||
                    collectionType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T));

                var elementType = collectionType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                return ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType);
            }

            BoundExpression createImmutableArray(BoundCollectionExpression node, NamedTypeSymbol immutableArrayType)
            {
                if (node.Elements.IsEmpty &&
                    _factory.WellKnownMember(WellKnownMember.System_Collections_Immutable_ImmutableArray_T__Empty, isOptional: true) is FieldSymbol immutableArrayOfTEmpty)
                {
                    // ImmutableArray<T> value = [];
                    var immutableArrayOfTargetCollectionTypeEmpty = immutableArrayOfTEmpty.AsMember(immutableArrayType);
                    return _factory.Field(receiver: null, immutableArrayOfTargetCollectionTypeEmpty);
                }

                if (CanOptimizeSingleSpreadAsCollectionBuilderArgument(node, out _))
                {
                    // ImmutableArray<T> array = ImmutableArray.Create(singleSpreadSpan)
                    return VisitCollectionBuilderCollectionExpression(node);
                }

                if (_factory.WellKnownMethod(WellKnownMember.System_Runtime_InteropServices_ImmutableCollectionsMarshal__AsImmutableArray_T, isOptional: true) is MethodSymbol asImmutableArray)
                {
                    // T[] array = [elems];
                    // ImmutableArray<T> value = ImmutableCollectionsMarshal.AsImmutableArray<T>(array);
                    var arrayType = getBackingArrayType(immutableArrayType);
                    var arrayValue = createArray(node, arrayType, targetsReadOnlyCollection: true);
                    return _factory.StaticCall(asImmutableArray.Construct([arrayType.ElementTypeWithAnnotations]), [arrayValue]);
                }

                // ReadOnlySpan<T> span = [elems];
                // ImmutableArray<T> array = ImmutableArray.Create(span)
                return VisitCollectionBuilderCollectionExpression(node);
            }

            BoundExpression? tryCreateNonArrayBackedSpan(BoundCollectionExpression node, NamedTypeSymbol spanType, bool isReadOnlySpan)
            {
                Debug.Assert(isReadOnlySpan
                    ? spanType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T)
                    : spanType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_Span_T));

                var elementType = spanType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                var elements = node.Elements;

                if (elements.Length == 0)
                {
                    // `default(Span<T>)` is the best way to make empty Spans
                    return _factory.Default(spanType);
                }

                if (isReadOnlySpan &&
                    ShouldUseRuntimeHelpersCreateSpan(node, elementType.Type))
                {
                    // Assert that binding layer agrees with lowering layer about whether this collection-expr will allocate.
                    Debug.Assert(!IsAllocatingRefStructCollectionExpression(node, CollectionExpressionTypeKind.ReadOnlySpan, elementType.Type, _compilation));
                    var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Array)).AsMember(spanType);
                    var rewrittenElements = elements.SelectAsArray(static (element, rewriter) => rewriter.VisitExpression((BoundExpression)element), this);
                    // Use codegen which downstream layer will emit as a "readonly span into assembly data segment" instead of "readonly span into array".
                    return _factory.New(constructor, _factory.Array(elementType.Type, rewrittenElements));
                }

                if (ShouldUseInlineArray(node, _compilation) &&
                    _additionalLocals is { })
                {
                    Debug.Assert(!IsAllocatingRefStructCollectionExpression(node, isReadOnlySpan ? CollectionExpressionTypeKind.ReadOnlySpan : CollectionExpressionTypeKind.Span, elementType.Type, _compilation));

                    return CreateTransientArrayOrCreateAndPopulateSpanFromInlineArray(
                        node.Syntax,
                        elementType,
                        elements.SelectAsArray(static (element, @this) => @this.VisitExpression((BoundExpression)element), this),
                        asReadOnlySpan: isReadOnlySpan,
                        passedAsParameter);
                }

                return null;
            }

            BoundExpression createSpan(BoundCollectionExpression node, NamedTypeSymbol spanType, bool isReadOnlySpan)
            {
                Debug.Assert(isReadOnlySpan
                    ? spanType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T)
                    : spanType.OriginalDefinition == (object)_compilation.GetWellKnownType(WellKnownType.System_Span_T));

                if (tryCreateNonArrayBackedSpan(node, spanType, isReadOnlySpan) is { } spanValue)
                    return spanValue;

                var arrayType = getBackingArrayType(spanType);
                var arrayValue = createArray(node, arrayType, targetsReadOnlyCollection: isReadOnlySpan);

                var wellKnownMember = isReadOnlySpan ? WellKnownMember.System_ReadOnlySpan_T__ctor_Array : WellKnownMember.System_Span_T__ctor_Array;
                var spanConstructor = _factory.WellKnownMethod(wellKnownMember).AsMember(spanType);

                // We can either get the same array type as the target span type or an array of more derived type.
                // In the second case reference conversion would happen automatically since we still construct the span
                // of the base type, while usually such conversion requires stloc+ldloc with the local of the base type
                assertTypesAreCompatible(_compilation, arrayType, spanConstructor.Parameters[0].Type, isReadOnlySpan);
                return _factory.New(spanConstructor, arrayValue);

                [Conditional("DEBUG")]
                static void assertTypesAreCompatible(CSharpCompilation compilation, TypeSymbol arrayType, TypeSymbol constructorParameterType, bool isReadOnlySpan)
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    var conversionKind = compilation.Conversions.ClassifyConversionFromType(arrayType, constructorParameterType, isChecked: false, ref discardedUseSiteInfo).Kind;
                    Debug.Assert(conversionKind == ConversionKind.Identity || (isReadOnlySpan && conversionKind == ConversionKind.ImplicitReference));
                }
            }

            BoundExpression createArray(BoundCollectionExpression node, ArrayTypeSymbol arrayType, bool targetsReadOnlyCollection)
            {
                BoundExpression array;
                if (TryOptimizeSingleSpreadToArray_NoConversionApplied(node, targetsReadOnlyCollection, arrayType) is { } optimizedArray)
                {
                    array = optimizedArray;
                }
                else if (ShouldUseKnownLength(node, out _))
                {
                    array = CreateAndPopulateArray(node, arrayType);
                }
                else
                {
                    // The array initializer has an unknown length, so we'll create an intermediate List<T> instance.
                    // https://github.com/dotnet/roslyn/issues/68785: Emit Enumerable.TryGetNonEnumeratedCount() and avoid intermediate List<T> at runtime.
                    var list = CreateAndPopulateList(node, arrayType.ElementTypeWithAnnotations, node.Elements);

                    Debug.Assert(list.Type is { });
                    Debug.Assert(list.Type.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T), TypeCompareKind.AllIgnoreOptions));

                    var listToArray = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ToArray)).AsMember((NamedTypeSymbol)list.Type);
                    array = _factory.Call(list, listToArray);
                }

                return array;
            }
        }

        private BoundExpression VisitCollectionInitializerCollectionExpression(BoundCollectionExpression node, TypeSymbol collectionType)
        {
            Debug.Assert(!_inExpressionLambda);

            var elements = node.Elements;
            var rewrittenReceiver = VisitExpression(node.CollectionCreation);

            Debug.Assert(rewrittenReceiver is { });

            // Create a temp for the collection.
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal temp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(elements.Length + 1);
            sideEffects.Add(assignmentToTemp);

            var placeholder = node.Placeholder;
            Debug.Assert(placeholder is { });

            AddPlaceholderReplacement(placeholder, temp);

            foreach (var element in elements)
            {
                var rewrittenElement = element is BoundCollectionExpressionSpreadElement spreadElement ?
                    MakeCollectionExpressionSpreadElement(
                        spreadElement,
                        VisitExpression(spreadElement.Expression),
                        iteratorBody =>
                        {
                            var syntax = iteratorBody.Syntax;
                            var rewrittenValue = rewriteCollectionInitializer(temp, ((BoundExpressionStatement)iteratorBody).Expression);
                            // MakeCollectionInitializer() may return null if Add() is marked [Conditional].
                            return rewrittenValue is { } ?
                                new BoundExpressionStatement(syntax, rewrittenValue) :
                                new BoundNoOpStatement(syntax, NoOpStatementFlavor.Default);
                        }) :
                    rewriteCollectionInitializer(temp, (BoundExpression)element);
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

            BoundExpression? rewriteCollectionInitializer(BoundLocal rewrittenReceiver, BoundExpression expressionElement)
            {
                return expressionElement switch
                {
                    BoundCollectionElementInitializer collectionInitializer => MakeCollectionInitializer(collectionInitializer),
                    BoundDynamicCollectionElementInitializer dynamicInitializer => MakeDynamicCollectionInitializer(rewrittenReceiver, dynamicInitializer),
                    var e => throw ExceptionUtilities.UnexpectedValue(e)
                };
            }
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
            var elements = node.Elements;
            BoundExpression arrayOrList;

            if (collectionType.OriginalDefinition.SpecialType is
                SpecialType.System_Collections_Generic_IEnumerable_T or
                SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
                SpecialType.System_Collections_Generic_IReadOnlyList_T)
            {
                int numberIncludingLastSpread;
                bool useKnownLength = ShouldUseKnownLength(node, out numberIncludingLastSpread);

                if (elements.Length == 0)
                {
                    Debug.Assert(numberIncludingLastSpread == 0);
                    // arrayOrList = Array.Empty<ElementType>();
                    arrayOrList = CreateEmptyArray(syntax, ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType));
                }
                else
                {
                    var typeArgs = ImmutableArray.Create(elementType);
                    var kind = useKnownLength
                        ? numberIncludingLastSpread == 0 && elements.Length == 1 && SynthesizedReadOnlyListTypeSymbol.CanCreateSingleElement(_compilation)
                            ? SynthesizedReadOnlyListKind.SingleElement
                            : SynthesizedReadOnlyListKind.Array
                        : SynthesizedReadOnlyListKind.List;
                    var synthesizedType = _factory.ModuleBuilderOpt.EnsureReadOnlyListTypeExists(syntax, kind: kind, _diagnostics.DiagnosticBag).Construct(typeArgs);
                    if (synthesizedType.IsErrorType())
                    {
                        return BadExpression(node);
                    }

                    BoundExpression fieldValue = kind switch
                    {
                        // fieldValue = e1;
                        SynthesizedReadOnlyListKind.SingleElement => this.VisitExpression((BoundExpression)elements.Single()),
                        // fieldValue = new ElementType[] { e1, ..., eN };
                        SynthesizedReadOnlyListKind.Array => createArray(node, ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType)),
                        // fieldValue = new List<ElementType> { e1, ..., eN };
                        SynthesizedReadOnlyListKind.List => CreateAndPopulateList(node, elementType, elements),
                        var v => throw ExceptionUtilities.UnexpectedValue(v)
                    };

                    // arrayOrList = new <>z__ReadOnlyList<ElementType>(fieldValue);
                    arrayOrList = new BoundObjectCreationExpression(syntax, synthesizedType.Constructors.Single(), fieldValue) { WasCompilerGenerated = true };
                }
            }
            else
            {
                arrayOrList = CreateAndPopulateList(node, elementType, elements);
            }

            Conversion c = _factory.ClassifyEmitConversion(arrayOrList, collectionType);
            Debug.Assert(c.IsImplicit);
            Debug.Assert(c.IsReference || c.IsIdentity);
            return _factory.Convert(collectionType, arrayOrList, c);

            BoundExpression createArray(BoundCollectionExpression node, ArrayTypeSymbol arrayType)
            {
                Debug.Assert(node.Type.OriginalDefinition.SpecialType is
                    SpecialType.System_Collections_Generic_IEnumerable_T or
                    SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
                    SpecialType.System_Collections_Generic_IReadOnlyList_T);

                if (TryOptimizeSingleSpreadToArray_NoConversionApplied(node, targetsReadOnlyCollection: true, arrayType) is { } optimizedArray)
                    return optimizedArray;

                return CreateAndPopulateArray(node, arrayType);
            }
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

            // If collection expression is of form `[.. anotherReadOnlySpan]`
            // with `anotherReadOnlySpan` being a ReadOnlySpan of the same type as target collection type
            // and that span cannot be captured in a returned ref struct
            // we can directly use `anotherReadOnlySpan` as collection builder argument and skip the copying assignment.
            BoundExpression span = CanOptimizeSingleSpreadAsCollectionBuilderArgument(node, out var spreadExpression)
                ? VisitExpression(spreadExpression)
                : VisitArrayOrSpanCollectionExpression(node, spanType, passedAsParameter: null);

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

        internal static bool IsAllocatingRefStructCollectionExpression(BoundCollectionExpressionBase node, CollectionExpressionTypeKind collectionKind, TypeSymbol? elementType, CSharpCompilation compilation)
        {
            return collectionKind is CollectionExpressionTypeKind.Span or CollectionExpressionTypeKind.ReadOnlySpan
                && node.Elements.Length > 0
                && elementType is not null
                && !(collectionKind == CollectionExpressionTypeKind.ReadOnlySpan && ShouldUseRuntimeHelpersCreateSpan(node, elementType))
                && !ShouldUseInlineArray(node, compilation);
        }

        internal static bool ShouldUseRuntimeHelpersCreateSpan(BoundCollectionExpressionBase node, TypeSymbol elementType)
        {
            return !node.HasSpreadElements(out _, out _) &&
                node.Elements.Length > 0 &&
                CodeGenerator.IsTypeAllowedInBlobWrapper(elementType.EnumUnderlyingTypeOrSelf().SpecialType) &&
                node.Elements.All(e => ((BoundExpression)e).ConstantValueOpt is { });
        }

        private static bool ShouldUseInlineArray(BoundCollectionExpressionBase node, CSharpCompilation compilation)
        {
            return !node.HasSpreadElements(out _, out _) &&
                node.Elements.Length > 0 &&
                compilation.Assembly.RuntimeSupportsInlineArrayTypes;
        }

        private BoundExpression CreateTransientArrayOrCreateAndPopulateSpanFromInlineArray(
            SyntaxNode syntax,
            TypeWithAnnotations elementType,
            ImmutableArray<BoundExpression> elements,
            bool asReadOnlySpan,
            ParameterSymbol? passedAsParameter)
        {
            Debug.Assert(elements.Length > 0);

            // If the span is being passed to a scoped parameter, we can share inline array temps across invocations, we
            // know that the inline array won't escape the current call. This will be done by a pass after local rewriting,
            // as the array sizes won't be known until all of local rewriting is done.
            if (passedAsParameter is { EffectiveScope: ScopedKind.ScopedValue, RefKind: RefKind.None })
            {
                _transientInlineArrayAllocator.AllocateInlineArray(elementType.Type, elements.Length, isReadOnly: asReadOnlySpan);
                _allocatedTransientInlineArrays ??= ArrayBuilder<(TypeSymbol type, int length)>.GetInstance();
                _allocatedTransientInlineArrays.Add((elementType.Type, elements.Length));

                var spanType = _factory.WellKnownType(asReadOnlySpan ? WellKnownType.System_ReadOnlySpan_T : WellKnownType.System_Span_T).Construct([elementType]);

                return new BoundTransientSpanFromInlineArray(syntax, elements, elementType.Type, asReadOnlySpan, spanType);
            }

            Debug.Assert(_additionalLocals is { });
            return CreateCreateAndPopulateSpanFromInlineArray(syntax, elementType, elements, asReadOnlySpan, _factory, _compilation, _additionalLocals, _diagnostics);
        }

        internal static BoundExpression CreateCreateAndPopulateSpanFromInlineArray(
            SyntaxNode syntax,
            TypeWithAnnotations elementType,
            ImmutableArray<BoundExpression> elements,
            bool asReadOnlySpan,
            SyntheticBoundNodeFactory factory,
            CSharpCompilation compilation,
            ArrayBuilder<LocalSymbol> additionalLocals,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(factory.ModuleBuilderOpt is { });
            Debug.Assert(diagnostics.DiagnosticBag is { });
            Debug.Assert(compilation.Assembly.RuntimeSupportsInlineArrayTypes);

            int arrayLength = elements.Length;
            if (arrayLength == 1
                && factory.WellKnownMember(asReadOnlySpan
                    ? WellKnownMember.System_ReadOnlySpan_T__ctor_ref_readonly_T
                    : WellKnownMember.System_Span_T__ctor_ref_T, isOptional: true) is MethodSymbol spanRefConstructor)
            {
                // Special case: no need to create an InlineArray1 type. Just use a temp of the element type.
                var spanType = factory
                    .WellKnownType(asReadOnlySpan ? WellKnownType.System_ReadOnlySpan_T : WellKnownType.System_Span_T)
                    .Construct([elementType]);
                var constructor = spanRefConstructor.AsMember(spanType);
                var temp = factory.StoreToTemp(elements[0], out var assignment);
                additionalLocals.Add(temp.LocalSymbol);
                var call = factory.New(constructor, arguments: [temp], argumentRefKinds: [asReadOnlySpan ? RefKindExtensions.StrictIn : RefKind.Ref]);
                return factory.Sequence([assignment], call);
            }

            var inlineArrayType = factory.ModuleBuilderOpt.EnsureInlineArrayTypeExists(syntax, factory, arrayLength, diagnostics).Construct(ImmutableArray.Create(elementType));
            Debug.Assert(inlineArrayType.HasInlineArrayAttribute(out int inlineArrayLength) && inlineArrayLength == arrayLength);

            var intType = factory.SpecialType(SpecialType.System_Int32);
            MethodSymbol elementRef = factory.ModuleBuilderOpt.EnsureInlineArrayElementRefExists(syntax, intType, diagnostics.DiagnosticBag).
                Construct(ImmutableArray.Create(TypeWithAnnotations.Create(inlineArrayType), elementType));

            // Create an inline array and assign to a local.
            // var tmp = new <>y__InlineArrayN<ElementType>();
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal inlineArrayLocal = factory.StoreToTemp(new BoundDefaultExpression(syntax, inlineArrayType), out assignmentToTemp);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
            sideEffects.Add(assignmentToTemp);
            additionalLocals.Add(inlineArrayLocal.LocalSymbol);

            // Populate the inline array.
            // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, 0) = element0;
            // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, 1) = element1;
            // ...
            for (int i = 0; i < arrayLength; i++)
            {
                var call = factory.Call(null, elementRef, inlineArrayLocal, factory.Literal(i), useStrictArgumentRefKinds: true);
                var assignment = new BoundAssignmentOperator(syntax, call, elements[i], type: call.Type) { WasCompilerGenerated = true };
                sideEffects.Add(assignment);
            }

            // Get a span to the inline array.
            // ... InlineArrayAsReadOnlySpan<<>y__InlineArrayN<ElementType>, ElementType>(in tmp, N)
            // or
            // ... InlineArrayAsSpan<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, N)
            MethodSymbol inlineArrayAsSpan = asReadOnlySpan ?
                factory.ModuleBuilderOpt.EnsureInlineArrayAsReadOnlySpanExists(syntax, factory.WellKnownType(WellKnownType.System_ReadOnlySpan_T), intType, diagnostics.DiagnosticBag) :
                factory.ModuleBuilderOpt.EnsureInlineArrayAsSpanExists(syntax, factory.WellKnownType(WellKnownType.System_Span_T), intType, diagnostics.DiagnosticBag);
            inlineArrayAsSpan = inlineArrayAsSpan.Construct(ImmutableArray.Create(TypeWithAnnotations.Create(inlineArrayType), elementType));
            var span = factory.Call(
                receiver: null,
                inlineArrayAsSpan,
                inlineArrayLocal,
                factory.Literal(arrayLength),
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

        /// <summary>Attempt to optimize conversion of a single-spread collection expr to array, even if the spread length is not known.</summary>
        /// <remarks>
        /// The following optimizations are tried, in order:
        /// <list type="number">
        /// <item><c>List.ToArray</c> if the spread value is a list</item>
        /// <item><c>Enumerable.ToArray</c> if we can convert the spread value to IEnumerable and additional conditions are met</item>
        /// <item><c>Span/ReadOnlySpan.ToArray</c> if we can convert the spread value to Span or ReadOnlySpan</item>
        /// </list>
        /// Applying conversion to the resulting array is up to the caller
        /// </remarks>
        private BoundExpression? TryOptimizeSingleSpreadToArray_NoConversionApplied(BoundCollectionExpression node, bool targetsReadOnlyCollection, ArrayTypeSymbol arrayType)
        {
            // Collection-expr is of the form `[..spreadExpression]`.
            // Optimize to `spreadExpression.ToArray()` if possible.
            if (node is { Elements: [BoundCollectionExpressionSpreadElement { Expression: { } spreadExpression } spreadElement] }
                && spreadElement.IteratorBody is BoundExpressionStatement expressionStatement)
            {
                var spreadElementConversion = expressionStatement.Expression is BoundConversion { Conversion: var actualConversion } ? actualConversion : Conversion.Identity;
                // Allow implicit reference conversion only if we target readonly collection types like
                // ReadOnlySpan, IEnumerable, IReadOnlyList etc. Cause otherwise user may get an array with different
                // actual underlying array type which may lead to unexpected behavior, e.g. an exception
                // when trying to insert an element of the base type
                var spreadElementHasCompatibleConversion = targetsReadOnlyCollection
                    ? spreadElementConversion.Kind is ConversionKind.Identity or ConversionKind.ImplicitReference
                    : spreadElementConversion.Kind is ConversionKind.Identity;
                var spreadTypeOriginalDefinition = spreadExpression.Type!.OriginalDefinition;

                if (spreadElementHasCompatibleConversion
                    && tryGetToArrayMethod(spreadTypeOriginalDefinition, WellKnownType.System_Collections_Generic_List_T, WellKnownMember.System_Collections_Generic_List_T__ToArray, out MethodSymbol? listToArrayMethod))
                {
                    var rewrittenSpreadExpression = VisitExpression(spreadExpression);
                    return _factory.Call(rewrittenSpreadExpression, listToArrayMethod.AsMember((NamedTypeSymbol)spreadExpression.Type!));
                }

                // See if 'Enumerable.ToArray<T>(IEnumerable<T>)' will work, possibly due to a covariant conversion on the spread value.
                if (_factory.WellKnownMethod(WellKnownMember.System_Linq_Enumerable__ToArray, isOptional: true) is { } linqToArrayMethodGeneric)
                {
                    // Note that in general, we expect well-known collection types and methods to lack constraints on their type parameter(s).
                    // Because an array element type may not be a valid type argument for unconstrained type parameter, we still check constraints here regardless.
                    var linqToArrayMethod = linqToArrayMethodGeneric.Construct([arrayType.ElementTypeWithAnnotations]);
                    if (linqToArrayMethod.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(_compilation, _compilation.Conversions, Location.None, BindingDiagnosticBag.Discarded))
                        && ShouldUseIEnumerableBulkAddMethod(spreadExpression.Type!, linqToArrayMethod.Parameters[0].Type, spreadElement.EnumeratorInfoOpt?.GetEnumeratorInfo.Method))
                    {
                        return _factory.Call(receiver: null, linqToArrayMethod, VisitExpression(spreadExpression));
                    }
                }

                if (spreadElementHasCompatibleConversion
                    && TryGetSpanConversion(spreadExpression.Type, writableOnly: false, out var asSpanMethod))
                {
                    var spanType = CallAsSpanMethod(spreadExpression, asSpanMethod).Type!.OriginalDefinition;
                    if (tryGetToArrayMethod(spanType, WellKnownType.System_ReadOnlySpan_T, WellKnownMember.System_ReadOnlySpan_T__ToArray, out var toArrayMethod)
                        || tryGetToArrayMethod(spanType, WellKnownType.System_Span_T, WellKnownMember.System_Span_T__ToArray, out toArrayMethod))
                    {
                        var rewrittenSpreadExpression = CallAsSpanMethod(VisitExpression(spreadExpression), asSpanMethod);
                        return _factory.Call(rewrittenSpreadExpression, toArrayMethod.AsMember((NamedTypeSymbol)rewrittenSpreadExpression.Type!));
                    }
                }

                bool tryGetToArrayMethod(TypeSymbol spreadTypeOriginalDefinition, WellKnownType wellKnownType, WellKnownMember wellKnownMember, [NotNullWhen(true)] out MethodSymbol? toArrayMethod)
                {
                    if (TypeSymbol.Equals(spreadTypeOriginalDefinition, this._compilation.GetWellKnownType(wellKnownType), TypeCompareKind.AllIgnoreOptions))
                    {
                        toArrayMethod = _factory.WellKnownMethod(wellKnownMember, isOptional: true);
                        return toArrayMethod is { };
                    }

                    toArrayMethod = null;
                    return false;
                }
            }

            return null;
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

            // Shouldn't call this method if the single spread optimization would work.
            // Passing `targetsReadOnlyCollection` as false since it is more restrictive case.
            Debug.Assert(TryOptimizeSingleSpreadToArray_NoConversionApplied(node, targetsReadOnlyCollection: false, arrayType) is null);

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
                    elements.SelectAsArray(static (element, rewriter) => rewriter.VisitExpression((BoundExpression)element), this));
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

            // indexTemp is null when we can use constant compile-time indices.
            // indexTemp is non-null when we need a runtime-tracked index variable (for spread elements).
            BoundLocal? indexTemp = null;

            if (numberIncludingLastSpread != 0)
            {
                // int index = 0;
                indexTemp = _factory.StoreToTemp(
                    _factory.Literal(0),
                    out assignmentToTemp);
                localsBuilder.Add(indexTemp);
                sideEffects.Add(assignmentToTemp);
            }

            // ElementType[] array = new ElementType[N + s1.Length + ...];
            BoundLocal arrayTemp = _factory.StoreToTemp(
                new BoundArrayCreation(syntax,
                    ImmutableArray.Create(GetKnownLengthExpression(elements, numberIncludingLastSpread, localsBuilder)),
                    initializerOpt: null,
                    arrayType),
                out assignmentToTemp);
            localsBuilder.Add(arrayTemp);
            sideEffects.Add(assignmentToTemp);

            int currentElementIndex = 0;
            AddCollectionExpressionElements(
                elements,
                arrayTemp,
                localsBuilder,
                numberIncludingLastSpread,
                sideEffects,
                addElement: (ArrayBuilder<BoundExpression> expressions, BoundExpression arrayTemp, BoundExpression rewrittenValue, bool isLastElement) =>
                {
                    Debug.Assert(arrayTemp.Type is ArrayTypeSymbol);

                    var expressionSyntax = rewrittenValue.Syntax;
                    var elementType = ((ArrayTypeSymbol)arrayTemp.Type).ElementType;

                    if (indexTemp is null)
                    {
                        // array[0] = element; array[1] = element; etc.
                        expressions.Add(
                            new BoundAssignmentOperator(
                                expressionSyntax,
                                _factory.ArrayAccess(arrayTemp, _factory.Literal(currentElementIndex)),
                                rewrittenValue,
                                isRef: false,
                                elementType));
                        currentElementIndex++;
                    }
                    else
                    {
                        // array[index] = element;
                        expressions.Add(
                            new BoundAssignmentOperator(
                                expressionSyntax,
                                _factory.ArrayAccess(arrayTemp, indexTemp),
                                rewrittenValue,
                                isRef: false,
                                elementType));
                        if (!isLastElement)
                        {
                            // index = index + 1;
                            expressions.Add(
                                new BoundAssignmentOperator(
                                    expressionSyntax,
                                    indexTemp,
                                    _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, _factory.Literal(1)),
                                    isRef: false,
                                    indexTemp.Type));
                        }
                    }
                },
                tryOptimizeSpreadElement: (ArrayBuilder<BoundExpression> sideEffects, BoundExpression arrayTemp, BoundCollectionExpressionSpreadElement spreadElement, BoundExpression rewrittenSpreadOperand) =>
                {
                    // When we have spreads, we always need a runtime-tracked index variable.
                    Debug.Assert(indexTemp is not null);

                    if (PrepareCopyToOptimization(spreadElement, rewrittenSpreadOperand) is not var (spanSliceMethod, spreadElementAsSpan, getLengthMethod, copyToMethod))
                        return false;

                    // https://github.com/dotnet/roslyn/issues/71270
                    // Could save the targetSpan to temp in the enclosing scope, but need to make sure we are async-safe etc.
                    if (!TryConvertToSpan(arrayTemp, writableOnly: true, out var targetSpan))
                        return false;

                    PerformCopyToOptimization(sideEffects, localsBuilder, indexTemp, targetSpan, rewrittenSpreadOperand, spanSliceMethod, spreadElementAsSpan, getLengthMethod, copyToMethod);
                    return true;
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
        /// For the purpose of optimization, conversions to ReadOnlySpan and/or Span are known on the following types:
        /// System.Array, System.Span, System.ReadOnlySpan, System.Collections.Immutable.ImmutableArray, and System.Collections.Generic.List.
        /// </summary>
        /// <param name="asSpanMethod">Not-null if non-identity conversion was found.</param>
        /// <returns>
        /// If <paramref name="writableOnly"/> is 'true', will only return 'true' with a conversion to Span.
        /// If <paramref name="writableOnly"/> is 'false', may return either a conversion to ReadOnlySpan or to Span, depending on the source type.
        /// For System.Array and 'false' argument for <paramref name="writableOnly"/>, only a conversion to ReadOnlySpan may be returned.
        /// For System.Array and 'true' argument for <paramref name="writableOnly"/>, only a conversion to Span may be returned.
        /// For System.Span, only a conversion to System.Span is may be returned.
        /// For System.ReadOnlySpan, only a conversion to System.ReadOnlySpan may be returned.
        /// For System.Collections.Immutable.ImmutableArray, only a conversion to System.ReadOnlySpan may be returned.
        /// For System.Collections.Generic.List, only a conversion to System.Span may be returned.
        /// </returns>
        /// <remarks>We are assuming that the well-known types we are converting to/from do not have constraints on their type parameters.</remarks>
        private bool TryGetSpanConversion(TypeSymbol type, bool writableOnly, out MethodSymbol? asSpanMethod)
        {
            if (type is ArrayTypeSymbol { IsSZArray: true } arrayType
                && _factory.WellKnownMethod(writableOnly ? WellKnownMember.System_Span_T__ctor_Array : WellKnownMember.System_ReadOnlySpan_T__ctor_Array, isOptional: true) is { } spanCtorArray)
            {
                var spanOfElementType = spanCtorArray.ContainingType.Construct(arrayType.ElementType);
                if (spanOfElementType.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(_compilation, _compilation.Conversions, Location.None, BindingDiagnosticBag.Discarded)))
                {
                    asSpanMethod = spanCtorArray.AsMember(spanOfElementType);
                    return true;
                }
            }

            if (type is not NamedTypeSymbol namedType)
            {
                asSpanMethod = null;
                return false;
            }

            if ((!writableOnly && namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything))
                || namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Span_T), TypeCompareKind.ConsiderEverything))
            {
                asSpanMethod = null;
                return true;
            }

            if (!writableOnly
                && namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T), TypeCompareKind.ConsiderEverything)
                && _factory.WellKnownMethod(WellKnownMember.System_Collections_Immutable_ImmutableArray_T__AsSpan, isOptional: true) is { } immutableArrayAsSpanMethod)
            {
                asSpanMethod = immutableArrayAsSpanMethod.AsMember(namedType);
                return true;
            }

            if (namedType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T), TypeCompareKind.ConsiderEverything)
                && _factory.WellKnownMethod(WellKnownMember.System_Runtime_InteropServices_CollectionsMarshal__AsSpan_T, isOptional: true) is { } collectionsMarshalAsSpanMethod)
            {
                asSpanMethod = collectionsMarshalAsSpanMethod.Construct(namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type);
                return true;
            }

            asSpanMethod = null;
            return false;
        }

        private bool TryConvertToSpan(BoundExpression expression, bool writableOnly, [NotNullWhen(true)] out BoundExpression? span)
        {
            var type = expression.Type;
            Debug.Assert(type is not null);

            if (!TryGetSpanConversion(type, writableOnly, out var asSpanMethod))
            {
                span = null;
                return false;
            }

            span = CallAsSpanMethod(expression, asSpanMethod);
            return true;
        }

        private BoundExpression CallAsSpanMethod(BoundExpression spreadExpression, MethodSymbol? asSpanMethod)
        {
            if (asSpanMethod is null)
            {
                return spreadExpression;
            }
            if (asSpanMethod is MethodSymbol { MethodKind: MethodKind.Constructor } constructor)
            {
                return _factory.New(constructor, spreadExpression);
            }
            else if (asSpanMethod is MethodSymbol { IsStatic: true, ParameterCount: 1 })
            {
                return _factory.Call(receiver: null, asSpanMethod, spreadExpression);
            }
            else
            {
                return _factory.Call(spreadExpression, asSpanMethod);
            }
        }

        /// <summary>
        /// Verifies presence of methods necessary for the CopyTo optimization
        /// without performing mutating actions e.g. appending to side effects or locals builders.
        /// </summary>
        private (MethodSymbol spanSliceMethod, BoundExpression spreadElementAsSpan, MethodSymbol getLengthMethod, MethodSymbol copyToMethod)? PrepareCopyToOptimization(
            BoundCollectionExpressionSpreadElement spreadElement,
            BoundExpression rewrittenSpreadOperand)
        {
            // Cannot use CopyTo when spread element has non-identity conversion to target element type.
            // Could do a covariant conversion of ReadOnlySpan in future: https://github.com/dotnet/roslyn/issues/71106
            if (spreadElement.IteratorBody is not BoundExpressionStatement expressionStatement || expressionStatement.Expression is BoundConversion { ConversionKind: not ConversionKind.Identity })
                return null;

            if (_factory.WellKnownMethod(WellKnownMember.System_Span_T__Slice_Int_Int, isOptional: true) is not { } spanSliceMethod)
                return null;

            if (!TryConvertToSpan(rewrittenSpreadOperand, writableOnly: false, out var spreadOperandAsSpan))
                return null;

            if ((getSpanMethodsForSpread(WellKnownType.System_ReadOnlySpan_T, WellKnownMember.System_ReadOnlySpan_T__get_Length, WellKnownMember.System_ReadOnlySpan_T__CopyTo_Span_T)
                    ?? getSpanMethodsForSpread(WellKnownType.System_Span_T, WellKnownMember.System_Span_T__get_Length, WellKnownMember.System_Span_T__CopyTo_Span_T))
                is not (var getLengthMethod, var copyToMethod))
            {
                return null;
            }

            return (spanSliceMethod, spreadOperandAsSpan, getLengthMethod, copyToMethod);

            // gets either Span or ReadOnlySpan methods for operating on the source spread element.
            (MethodSymbol getLengthMethod, MethodSymbol copyToMethod)? getSpanMethodsForSpread(
                WellKnownType wellKnownSpanType,
                WellKnownMember getLengthMember,
                WellKnownMember copyToMember)
            {
                if (spreadOperandAsSpan.Type!.OriginalDefinition.Equals(this._compilation.GetWellKnownType(wellKnownSpanType))
                    && _factory.WellKnownMethod(getLengthMember, isOptional: true) is { } getLengthMethod
                    && _factory.WellKnownMethod(copyToMember, isOptional: true) is { } copyToMethod)
                {
                    return (getLengthMethod, copyToMethod);
                }

                return null;
            }
        }

        private void PerformCopyToOptimization(
            ArrayBuilder<BoundExpression> sideEffects,
            ArrayBuilder<BoundLocal> localsBuilder,
            BoundLocal indexTemp,
            BoundExpression spanTemp,
            BoundExpression rewrittenSpreadOperand,
            MethodSymbol spanSliceMethod,
            BoundExpression spreadOperandAsSpan,
            MethodSymbol getLengthMethod,
            MethodSymbol copyToMethod)
        {
            // before:
            // ..e1 // in [e0, ..e1]
            //
            // after (roughly):
            // var e1Span = e1.AsSpan();
            // e1Span.CopyTo(destinationSpan.Slice(indexTemp, e1Span.Length);
            // indexTemp += e1Span.Length;

            Debug.Assert((object)spreadOperandAsSpan != rewrittenSpreadOperand || spreadOperandAsSpan is BoundLocal { LocalSymbol.SynthesizedKind: SynthesizedLocalKind.LoweringTemp });
            if ((object)spreadOperandAsSpan != rewrittenSpreadOperand)
            {
                spreadOperandAsSpan = _factory.StoreToTemp(spreadOperandAsSpan, out var assignmentToTemp);
                sideEffects.Add(assignmentToTemp);
                localsBuilder.Add((BoundLocal)spreadOperandAsSpan);
            }

            // e1Span.CopyTo(destinationSpan.Slice(indexTemp, e1Span.Length);
            var spreadLength = _factory.Call(spreadOperandAsSpan, getLengthMethod.AsMember((NamedTypeSymbol)spreadOperandAsSpan.Type!));
            var targetSlice = _factory.Call(spanTemp, spanSliceMethod.AsMember((NamedTypeSymbol)spanTemp.Type!), indexTemp, spreadLength);
            sideEffects.Add(_factory.Call(spreadOperandAsSpan, copyToMethod.AsMember((NamedTypeSymbol)spreadOperandAsSpan.Type!), targetSlice));

            // indexTemp += e1Span.Length;
            sideEffects.Add(new BoundAssignmentOperator(rewrittenSpreadOperand.Syntax, indexTemp, _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, spreadLength), isRef: false, indexTemp.Type));
        }

        /// <summary>
        /// Create and populate an list from a collection expression.
        /// The collection may or may not have a known length.
        /// </summary>
        private BoundExpression CreateAndPopulateList(BoundCollectionExpression node, TypeWithAnnotations elementType, ImmutableArray<BoundNode> elements)
        {
            Debug.Assert(!_inExpressionLambda);

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

            // Create a temp for the knownLength
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal? knownLengthTemp = null;

            BoundObjectCreationExpression rewrittenReceiver;
            if (useKnownLength && elements.Length > 0)
            {
                var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ctorInt32)).AsMember(collectionType);
                var knownLengthExpression = GetKnownLengthExpression(elements, numberIncludingLastSpread, localsBuilder);

                if (useOptimizations)
                {
                    // If we use optimizations, we know the length of the resulting list, and we store it in a temp so we can pass it to List.ctor(int32) and to CollectionsMarshal.SetCount

                    // int knownLengthTemp = N + s1.Length + ...;
                    knownLengthTemp = _factory.StoreToTemp(knownLengthExpression, out assignmentToTemp);
                    localsBuilder.Add(knownLengthTemp);
                    sideEffects.Add(assignmentToTemp);

                    // List<ElementType> list = new(knownLengthTemp);
                    rewrittenReceiver = _factory.New(constructor, ImmutableArray.Create<BoundExpression>(knownLengthTemp));
                }
                else
                {
                    // List<ElementType> list = new(N + s1.Length + ...)
                    rewrittenReceiver = _factory.New(constructor, ImmutableArray.Create(knownLengthExpression));
                }
            }
            else
            {
                // List<ElementType> list = new();
                var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ctor)).AsMember(collectionType);
                rewrittenReceiver = _factory.New(constructor, ImmutableArray<BoundExpression>.Empty);
            }

            // Create a temp for the list.
            BoundLocal listTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp);
            localsBuilder.Add(listTemp);
            sideEffects.Add(assignmentToTemp);

            // Use Span<T> if CollectionsMarshal methods are available, otherwise use List<T>.Add().
            if (useOptimizations)
            {
                Debug.Assert(useKnownLength);
                Debug.Assert(setCount is { });
                Debug.Assert(asSpan is { });
                Debug.Assert(knownLengthTemp is { });

                // CollectionsMarshal.SetCount<ElementType>(list, knownLengthTemp);
                sideEffects.Add(_factory.Call(receiver: null, setCount, listTemp, knownLengthTemp));

                // var span = CollectionsMarshal.AsSpan<ElementType(list);
                BoundLocal spanTemp = _factory.StoreToTemp(_factory.Call(receiver: null, asSpan, listTemp), out assignmentToTemp);
                localsBuilder.Add(spanTemp);
                sideEffects.Add(assignmentToTemp);

                // Populate the span.
                var spanGetItem = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Span_T__get_Item)).AsMember((NamedTypeSymbol)spanTemp.Type);

                // indexTemp is null when we can use constant compile-time indices.
                // indexTemp is non-null when we need a runtime-tracked index variable (for spread elements).
                BoundLocal? indexTemp = null;

                if (numberIncludingLastSpread != 0)
                {
                    // int index = 0;
                    indexTemp = _factory.StoreToTemp(
                        _factory.Literal(0),
                        out assignmentToTemp);
                    localsBuilder.Add(indexTemp);
                    sideEffects.Add(assignmentToTemp);
                }

                int currentElementIndex = 0;
                AddCollectionExpressionElements(
                    elements,
                    spanTemp,
                    localsBuilder,
                    numberIncludingLastSpread,
                    sideEffects,
                    addElement: (ArrayBuilder<BoundExpression> expressions, BoundExpression spanTemp, BoundExpression rewrittenValue, bool isLastElement) =>
                    {
                        Debug.Assert(spanTemp.Type is NamedTypeSymbol);

                        var expressionSyntax = rewrittenValue.Syntax;
                        var elementType = ((NamedTypeSymbol)spanTemp.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;

                        if (indexTemp is null)
                        {
                            // span[0] = element; span[1] = element; etc.
                            expressions.Add(
                                new BoundAssignmentOperator(
                                    expressionSyntax,
                                    _factory.Call(spanTemp, spanGetItem, _factory.Literal(currentElementIndex)),
                                    rewrittenValue,
                                    isRef: false,
                                    elementType));
                            currentElementIndex++;
                        }
                        else
                        {
                            // span[index] = element;
                            expressions.Add(
                                new BoundAssignmentOperator(
                                    expressionSyntax,
                                    _factory.Call(spanTemp, spanGetItem, indexTemp),
                                    rewrittenValue,
                                    isRef: false,
                                    elementType));
                            if (!isLastElement)
                            {
                                // index = index + 1;
                                expressions.Add(
                                    new BoundAssignmentOperator(
                                        expressionSyntax,
                                        indexTemp,
                                        _factory.Binary(BinaryOperatorKind.Addition, indexTemp.Type, indexTemp, _factory.Literal(1)),
                                        isRef: false,
                                        indexTemp.Type));
                            }
                        }
                    },
                    tryOptimizeSpreadElement: (ArrayBuilder<BoundExpression> sideEffects, BoundExpression spanTemp, BoundCollectionExpressionSpreadElement spreadElement, BoundExpression rewrittenSpreadOperand) =>
                    {
                        // When we have spreads, we always need a runtime-tracked index variable.
                        Debug.Assert(indexTemp is not null);

                        if (PrepareCopyToOptimization(spreadElement, rewrittenSpreadOperand) is not var (spanSliceMethod, spreadElementAsSpan, getLengthMethod, copyToMethod))
                            return false;

                        PerformCopyToOptimization(sideEffects, localsBuilder, indexTemp, spanTemp, rewrittenSpreadOperand, spanSliceMethod, spreadElementAsSpan, getLengthMethod, copyToMethod);
                        return true;
                    });
            }
            else
            {
                var addMethod = _factory.WellKnownMethod(WellKnownMember.System_Collections_Generic_List_T__Add).AsMember(collectionType);
                var addRangeMethod = _factory.WellKnownMethod(WellKnownMember.System_Collections_Generic_List_T__AddRange, isOptional: true)?.AsMember(collectionType);
                AddCollectionExpressionElements(
                    elements,
                    listTemp,
                    localsBuilder,
                    numberIncludingLastSpread,
                    sideEffects,
                    addElement: (ArrayBuilder<BoundExpression> expressions, BoundExpression listTemp, BoundExpression rewrittenValue, bool isLastElement) =>
                    {
                        // list.Add(element);
                        expressions.Add(
                            _factory.Call(listTemp, addMethod, rewrittenValue));
                    },
                    tryOptimizeSpreadElement: (ArrayBuilder<BoundExpression> sideEffects, BoundExpression listTemp, BoundCollectionExpressionSpreadElement spreadElement, BoundExpression rewrittenSpreadOperand) =>
                    {
                        Debug.Assert(rewrittenSpreadOperand.Type is not null);

                        if (addRangeMethod is null)
                            return false;

                        if (!ShouldUseIEnumerableBulkAddMethod(rewrittenSpreadOperand.Type, addRangeMethod.Parameters[0].Type, spreadElement.EnumeratorInfoOpt?.GetEnumeratorInfo.Method))
                        {
                            return false;
                        }

                        sideEffects.Add(_factory.Call(listTemp, addRangeMethod, rewrittenSpreadOperand));
                        return true;
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

        private BoundExpression RewriteCollectionExpressionElementExpression(BoundNode element)
        {
            var expression = element is BoundCollectionExpressionSpreadElement spreadElement ?
                spreadElement.Expression :
                (BoundExpression)element;
            return VisitExpression(expression);
        }

        private void RewriteCollectionExpressionElementsIntoTemporaries(
            ImmutableArray<BoundNode> elements,
            int numberIncludingLastSpread,
            ArrayBuilder<BoundLocal> locals,
            ArrayBuilder<BoundExpression> sideEffects)
        {
            for (int i = 0; i < numberIncludingLastSpread; i++)
            {
                var rewrittenExpression = RewriteCollectionExpressionElementExpression(elements[i]);
                BoundAssignmentOperator assignmentToTemp;
                BoundLocal temp = _factory.StoreToTemp(rewrittenExpression, out assignmentToTemp);
                locals.Add(temp);
                sideEffects.Add(assignmentToTemp);
            }
        }

        private void AddCollectionExpressionElements(
            ImmutableArray<BoundNode> elements,
            BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundLocal> rewrittenExpressions,
            int numberIncludingLastSpread,
            ArrayBuilder<BoundExpression> sideEffects,
            Action<ArrayBuilder<BoundExpression>, BoundExpression, BoundExpression, bool> addElement,
            Func<ArrayBuilder<BoundExpression>, BoundExpression, BoundCollectionExpressionSpreadElement, BoundExpression, bool> tryOptimizeSpreadElement)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                var rewrittenExpression = i < numberIncludingLastSpread ?
                    rewrittenExpressions[i] :
                    RewriteCollectionExpressionElementExpression(element);

                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    if (tryOptimizeSpreadElement(sideEffects, rewrittenReceiver, spreadElement, rewrittenExpression))
                        continue;

                    var rewrittenElement = MakeCollectionExpressionSpreadElement(
                        spreadElement,
                        rewrittenExpression,
                        iteratorBody =>
                        {
                            var rewrittenValue = VisitExpression(((BoundExpressionStatement)iteratorBody).Expression);
                            var builder = ArrayBuilder<BoundExpression>.GetInstance();
                            addElement(builder, rewrittenReceiver, rewrittenValue, false);
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
                    var isLastElement = i == (elements.Length - 1);
                    addElement(sideEffects, rewrittenReceiver, rewrittenExpression, isLastElement);
                }
            }
        }

        private BoundExpression GetKnownLengthExpression(ImmutableArray<BoundNode> elements, int numberIncludingLastSpread, ArrayBuilder<BoundLocal> rewrittenExpressions)
        {
            Debug.Assert(rewrittenExpressions.Count >= numberIncludingLastSpread);

            int initialLength = 0;
            BoundExpression? sum = null;

            for (int i = 0; i < numberIncludingLastSpread; i++)
            {
                var element = elements[i];
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
            Func<BoundStatement, BoundStatement> rewriteBody)
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
            var rewrittenBody = rewriteBody(iteratorBody);
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
            else if (enumeratorInfo is { InlineArraySpanType: not WellKnownType.Unknown })
            {
                statement = RewriteForEachStatementAsFor(
                    node,
                    getPreamble: GetInlineArrayForEachStatementPreambleDelegate(),
                    getItem: GetInlineArrayForEachStatementGetItemDelegate(),
                    getLength: GetInlineArrayForEachStatementGetLengthDelegate(),
                    arg: null,
                    convertedExpression.Operand,
                    enumeratorInfo,
                    elementPlaceholder: null,
                    elementConversion: null,
                    iterationVariables,
                    deconstructionOpt: null,
                    breakLabel,
                    continueLabel,
                    rewrittenBody);
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
