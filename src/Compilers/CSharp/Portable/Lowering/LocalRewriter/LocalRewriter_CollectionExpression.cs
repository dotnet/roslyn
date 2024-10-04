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

        public override BoundNode? VisitUnconvertedCollectionExpression(BoundUnconvertedCollectionExpression node)
        {
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
                        return VisitArrayOrSpanCollectionExpression(node, collectionTypeKind, node.Type, TypeWithAnnotations.Create(elementType));
                    case CollectionExpressionTypeKind.CollectionBuilder:
                        // A few special cases when a collection type is an ImmutableArray<T>
                        if (ConversionsBase.IsSpanOrListType(_compilation, node.Type, WellKnownType.System_Collections_Immutable_ImmutableArray_T, out var arrayElementType))
                        {
                            // For `[]` try to use `ImmutableArray<T>.Empty` singleton if available
                            if (node.Elements.IsEmpty &&
                                _compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Immutable_ImmutableArray_T__Empty) is FieldSymbol immutableArrayOfTEmpty)
                            {
                                var immutableArrayOfTargetCollectionTypeEmpty = immutableArrayOfTEmpty.AsMember((NamedTypeSymbol)node.Type);
                                return _factory.Field(receiver: null, immutableArrayOfTargetCollectionTypeEmpty);
                            }

                            // Otherwise try to optimize construction using `ImmutableCollectionsMarshal.AsImmutableArray`.
                            // Note, that we skip that path if collection expression is just `[.. readOnlySpan]` of the same element type,
                            // in such cases it is more efficient to emit a direct call of `ImmutableArray.Create`
                            if (_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_ImmutableCollectionsMarshal__AsImmutableArray_T) is MethodSymbol asImmutableArray &&
                                !CanOptimizeSingleSpreadAsCollectionBuilderArgument(node, out _))
                            {
                                return VisitImmutableArrayCollectionExpression(node, arrayElementType, asImmutableArray);
                            }
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

            if (!ShouldUseAddRangeOrToListMethod(singleSpread.Expression.Type, toListOfElementType.Parameters[0].Type, singleSpread.EnumeratorInfoOpt?.GetEnumeratorInfo.Method))
            {
                return false;
            }

            var rewrittenSpreadExpression = VisitExpression(singleSpread.Expression);
            result = _factory.Call(receiver: null, toListOfElementType, rewrittenSpreadExpression);
            return true;
        }

        private bool ShouldUseAddRangeOrToListMethod(TypeSymbol spreadType, TypeSymbol targetEnumerableType, MethodSymbol? getEnumeratorMethod)
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

        private BoundExpression VisitImmutableArrayCollectionExpression(BoundCollectionExpression node, TypeWithAnnotations elementType, MethodSymbol asImmutableArray)
        {
            var arrayCreation = VisitArrayOrSpanCollectionExpression(
                node,
                CollectionExpressionTypeKind.Array,
                ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType),
                elementType);
            // ImmutableCollectionsMarshal.AsImmutableArray(arrayCreation)
            return _factory.StaticCall(asImmutableArray.Construct(ImmutableArray.Create(elementType)), ImmutableArray.Create(arrayCreation));
        }

        private BoundExpression VisitArrayOrSpanCollectionExpression(BoundCollectionExpression node, CollectionExpressionTypeKind collectionTypeKind, TypeSymbol collectionType, TypeWithAnnotations elementType)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(_additionalLocals is { });
            Debug.Assert(node.CollectionCreation is null); // shouldn't have generated a constructor call
            Debug.Assert(node.Placeholder is null);

            var syntax = node.Syntax;
            var elements = node.Elements;
            MethodSymbol? spanConstructor = null;

            var arrayType = collectionType as ArrayTypeSymbol;
            if (arrayType is null)
            {
                // We're constructing a Span<T> or ReadOnlySpan<T> rather than T[].
                var spanType = (NamedTypeSymbol)collectionType;

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
                    // Assert that binding layer agrees with lowering layer about whether this collection-expr will allocate.
                    Debug.Assert(!IsAllocatingRefStructCollectionExpression(node, collectionTypeKind, elementType.Type, _compilation));
                    var constructor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Array)).AsMember(spanType);
                    var rewrittenElements = elements.SelectAsArray(static (element, rewriter) => rewriter.VisitExpression((BoundExpression)element), this);
                    return _factory.New(constructor, _factory.Array(elementType.Type, rewrittenElements));
                }

                if (ShouldUseInlineArray(node, _compilation) &&
                    _additionalLocals is { })
                {
                    Debug.Assert(!IsAllocatingRefStructCollectionExpression(node, collectionTypeKind, elementType.Type, _compilation));
                    return CreateAndPopulateSpanFromInlineArray(
                        syntax,
                        elementType,
                        elements,
                        asReadOnlySpan: collectionTypeKind == CollectionExpressionTypeKind.ReadOnlySpan);
                }

                Debug.Assert(IsAllocatingRefStructCollectionExpression(node, collectionTypeKind, elementType.Type, _compilation));
                arrayType = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType);
                spanConstructor = ((MethodSymbol)_factory.WellKnownMember(
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
                var list = CreateAndPopulateList(node, elementType, elements);

                Debug.Assert(list.Type is { });
                Debug.Assert(list.Type.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T), TypeCompareKind.AllIgnoreOptions));

                var listToArray = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ToArray)).AsMember((NamedTypeSymbol)list.Type);
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
                    BoundCollectionElementInitializer collectionInitializer => MakeCollectionInitializer(rewrittenReceiver, collectionInitializer),
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
                        SynthesizedReadOnlyListKind.Array => CreateAndPopulateArray(node, ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType)),
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

            // If collection expression is of form `[.. anotherReadOnlySpan]`
            // with `anotherReadOnlySpan` being a ReadOnlySpan of the same type as target collection type
            // and that span cannot be captured in a returned ref struct
            // we can directly use `anotherReadOnlySpan` as collection builder argument and skip the copying assignment.
            BoundExpression span = CanOptimizeSingleSpreadAsCollectionBuilderArgument(node, out var spreadExpression)
                ? VisitExpression(spreadExpression)
                : VisitArrayOrSpanCollectionExpression(node, CollectionExpressionTypeKind.ReadOnlySpan, spanType, elementType);

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

        private BoundExpression CreateAndPopulateSpanFromInlineArray(
            SyntaxNode syntax,
            TypeWithAnnotations elementType,
            ImmutableArray<BoundNode> elements,
            bool asReadOnlySpan)
        {
            Debug.Assert(elements.Length > 0);
            Debug.Assert(elements.All(e => e is BoundExpression));
            Debug.Assert(_factory.ModuleBuilderOpt is { });
            Debug.Assert(_diagnostics.DiagnosticBag is { });
            Debug.Assert(_compilation.Assembly.RuntimeSupportsInlineArrayTypes);
            Debug.Assert(_additionalLocals is { });

            int arrayLength = elements.Length;
            if (arrayLength == 1
                && _factory.WellKnownMember(asReadOnlySpan
                    ? WellKnownMember.System_ReadOnlySpan_T__ctor_ref_readonly_T
                    : WellKnownMember.System_Span_T__ctor_ref_T, isOptional: true) is MethodSymbol spanRefConstructor)
            {
                // Special case: no need to create an InlineArray1 type. Just use a temp of the element type.
                var spanType = _factory
                    .WellKnownType(asReadOnlySpan ? WellKnownType.System_ReadOnlySpan_T : WellKnownType.System_Span_T)
                    .Construct([elementType]);
                var constructor = spanRefConstructor.AsMember(spanType);
                var element = VisitExpression((BoundExpression)elements[0]);
                var temp = _factory.StoreToTemp(element, out var assignment);
                _additionalLocals.Add(temp.LocalSymbol);
                var call = _factory.New(constructor, arguments: [temp], argumentRefKinds: [asReadOnlySpan ? RefKindExtensions.StrictIn : RefKind.Ref]);
                return _factory.Sequence([assignment], call);
            }

            var inlineArrayType = _factory.ModuleBuilderOpt.EnsureInlineArrayTypeExists(syntax, _factory, arrayLength, _diagnostics.DiagnosticBag).Construct(ImmutableArray.Create(elementType));
            Debug.Assert(inlineArrayType.HasInlineArrayAttribute(out int inlineArrayLength) && inlineArrayLength == arrayLength);

            var intType = _factory.SpecialType(SpecialType.System_Int32);
            MethodSymbol elementRef = _factory.ModuleBuilderOpt.EnsureInlineArrayElementRefExists(syntax, intType, _diagnostics.DiagnosticBag).
                Construct(ImmutableArray.Create(TypeWithAnnotations.Create(inlineArrayType), elementType));

            // Create an inline array and assign to a local.
            // var tmp = new <>y__InlineArrayN<ElementType>();
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal inlineArrayLocal = _factory.StoreToTemp(new BoundDefaultExpression(syntax, inlineArrayType), out assignmentToTemp);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
            sideEffects.Add(assignmentToTemp);
            _additionalLocals.Add(inlineArrayLocal.LocalSymbol);

            // Populate the inline array.
            // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, 0) = element0;
            // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, 1) = element1;
            // ...
            for (int i = 0; i < arrayLength; i++)
            {
                var element = VisitExpression((BoundExpression)elements[i]);
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

            // Collection-expr is of the form `[..spreadExpression]`, where 'spreadExpression' has same element type as the target collection.
            // Optimize to `spreadExpression.ToArray()` if possible.
            if (node is { Elements: [BoundCollectionExpressionSpreadElement { Expression: { } spreadExpression } spreadElement] }
                && spreadElement.IteratorBody is BoundExpressionStatement expressionStatement
                && expressionStatement.Expression is not BoundConversion)
            {
                var spreadTypeOriginalDefinition = spreadExpression.Type!.OriginalDefinition;
                if (tryGetToArrayMethod(spreadTypeOriginalDefinition, WellKnownType.System_Collections_Generic_List_T, WellKnownMember.System_Collections_Generic_List_T__ToArray, out MethodSymbol? listToArrayMethod))
                {
                    var rewrittenSpreadExpression = VisitExpression(spreadExpression);
                    return _factory.Call(rewrittenSpreadExpression, listToArrayMethod.AsMember((NamedTypeSymbol)spreadExpression.Type!));
                }

                if (TryGetSpanConversion(spreadExpression.Type, writableOnly: false, out var asSpanMethod))
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

            // int index = 0;
            BoundLocal indexTemp = _factory.StoreToTemp(
                _factory.Literal(0),
                out assignmentToTemp);
            localsBuilder.Add(indexTemp);
            sideEffects.Add(assignmentToTemp);

            // ElementType[] array = new ElementType[N + s1.Length + ...];
            BoundLocal arrayTemp = _factory.StoreToTemp(
                new BoundArrayCreation(syntax,
                    ImmutableArray.Create(GetKnownLengthExpression(elements, numberIncludingLastSpread, localsBuilder)),
                    initializerOpt: null,
                    arrayType),
                out assignmentToTemp);
            localsBuilder.Add(arrayTemp);
            sideEffects.Add(assignmentToTemp);

            AddCollectionExpressionElements(
                elements,
                arrayTemp,
                localsBuilder,
                numberIncludingLastSpread,
                sideEffects,
                addElement: (ArrayBuilder<BoundExpression> expressions, BoundExpression arrayTemp, BoundExpression rewrittenValue) =>
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
                },
                tryOptimizeSpreadElement: (ArrayBuilder<BoundExpression> sideEffects, BoundExpression arrayTemp, BoundCollectionExpressionSpreadElement spreadElement, BoundExpression rewrittenSpreadOperand) =>
                {
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
                // conversion to 'object' will fail if, for example, 'arrayType.ElementType' is a pointer.
                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                if (_compilation.Conversions.ClassifyConversionFromType(source: arrayType.ElementType, destination: _compilation.GetSpecialType(SpecialType.System_Object), isChecked: false, ref useSiteInfo).IsImplicit)
                {
                    asSpanMethod = spanCtorArray.AsMember(spanCtorArray.ContainingType.Construct(arrayType.ElementType));
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
            if (spreadElement.IteratorBody is not BoundExpressionStatement expressionStatement || expressionStatement.Expression is BoundConversion)
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

                // int index = 0;
                BoundLocal indexTemp = _factory.StoreToTemp(
                    _factory.Literal(0),
                    out assignmentToTemp);
                localsBuilder.Add(indexTemp);
                sideEffects.Add(assignmentToTemp);

                AddCollectionExpressionElements(
                    elements,
                    spanTemp,
                    localsBuilder,
                    numberIncludingLastSpread,
                    sideEffects,
                    addElement: (ArrayBuilder<BoundExpression> expressions, BoundExpression spanTemp, BoundExpression rewrittenValue) =>
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
                    },
                    tryOptimizeSpreadElement: (ArrayBuilder<BoundExpression> sideEffects, BoundExpression spanTemp, BoundCollectionExpressionSpreadElement spreadElement, BoundExpression rewrittenSpreadOperand) =>
                    {
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
                    addElement: (ArrayBuilder<BoundExpression> expressions, BoundExpression listTemp, BoundExpression rewrittenValue) =>
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

                        if (!ShouldUseAddRangeOrToListMethod(rewrittenSpreadOperand.Type, addRangeMethod.Parameters[0].Type, spreadElement.EnumeratorInfoOpt?.GetEnumeratorInfo.Method))
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
            Action<ArrayBuilder<BoundExpression>, BoundExpression, BoundExpression> addElement,
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
