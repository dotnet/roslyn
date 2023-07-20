// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitCollectionExpression(BoundCollectionExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });

            var collectionTypeKind = ConversionsBase.GetCollectionExpressionTypeKind(_compilation, node.Type, out var elementType);
            switch (collectionTypeKind)
            {
                case CollectionExpressionTypeKind.CollectionInitializer:
                    return VisitCollectionInitializerCollectionExpression(node, node.Type);
                case CollectionExpressionTypeKind.Array:
                case CollectionExpressionTypeKind.Span:
                case CollectionExpressionTypeKind.ReadOnlySpan:
                    Debug.Assert(elementType is { });
                    return VisitArrayOrSpanCollectionExpression(node, node.Type, elementType);
                case CollectionExpressionTypeKind.CollectionBuilder:
                    return VisitCollectionBuilderCollectionExpression(node);
                case CollectionExpressionTypeKind.ListInterface:
                    return VisitListInterfaceCollectionExpression(node);
                default:
                    throw ExceptionUtilities.UnexpectedValue(collectionTypeKind);
            }
        }

        private BoundExpression VisitArrayOrSpanCollectionExpression(BoundCollectionExpression node, TypeSymbol collectionType, TypeSymbol elementType)
        {
            Debug.Assert(!_inExpressionLambda);

            var syntax = node.Syntax;
            MethodSymbol? spanConstructor = null;

            var arrayType = collectionType as ArrayTypeSymbol;
            if (arrayType is null)
            {
                Debug.Assert(collectionType.Name is "Span" or "ReadOnlySpan");
                // We're constructing a Span<T> or ReadOnlySpan<T> rather than T[].
                var spanType = (NamedTypeSymbol)collectionType;
                Debug.Assert(elementType.Equals(spanType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type, TypeCompareKind.AllIgnoreOptions));
                arrayType = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, TypeWithAnnotations.Create(elementType));
                spanConstructor = ((MethodSymbol)_compilation.GetWellKnownTypeMember(
                    collectionType.Name == "Span" ? WellKnownMember.System_Span_T__ctor_Array : WellKnownMember.System_ReadOnlySpan_T__ctor_Array)!).AsMember(spanType);
            }

            var elements = node.Elements;
            BoundExpression array;

            if (elements.Any(i => i is BoundCollectionExpressionSpreadElement))
            {
                // The array initializer includes at least one spread element, so we'll create an intermediate List<T> instance.
                // https://github.com/dotnet/roslyn/issues/68785: Avoid intermediate List<T> if all spread elements have Length property.
                // https://github.com/dotnet/roslyn/issues/68785: Emit Enumerable.TryGetNonEnumeratedCount() and avoid intermediate List<T> at runtime.
                var listType = _compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(elementType);
                var listToArray = ((MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__ToArray)!).AsMember(listType);
                var list = VisitCollectionInitializerCollectionExpression(node, collectionType);
                array = _factory.Call(list, listToArray);
            }
            else
            {
                int arrayLength = elements.Length;
                // https://github.com/dotnet/roslyn/issues/68785: Emit [] as Array.Empty<T>() rather than a List<T>.
                var initialization = (arrayLength == 0)
                    ? null
                    : new BoundArrayInitialization(
                        syntax,
                        isInferred: false,
                        elements.SelectAsArray(e => VisitExpression(e)));
                array = new BoundArrayCreation(
                    syntax,
                    ImmutableArray.Create<BoundExpression>(
                        new BoundLiteral(
                            syntax,
                            ConstantValue.Create(arrayLength),
                            _compilation.GetSpecialType(SpecialType.System_Int32))),
                    initialization,
                    arrayType)
                { WasCompilerGenerated = true };
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
                    BoundCollectionExpressionSpreadElement spreadElement => MakeCollectionExpressionSpreadElement(spreadElement),
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
            Debug.Assert(node.Type is { });

            // https://github.com/dotnet/roslyn/issues/68785: Emit [] as Array.Empty<T>() rather than a List<T>.
            var list = VisitCollectionInitializerCollectionExpression(node, node.Type);
            return _factory.Convert(node.Type, list);
        }

        private BoundExpression VisitCollectionBuilderCollectionExpression(BoundCollectionExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });

            var constructMethod = node.CollectionBuilderMethod;
            Debug.Assert(constructMethod is { });
            Debug.Assert(constructMethod.ReturnType.Equals(node.Type, TypeCompareKind.AllIgnoreOptions));

            var spanType = (NamedTypeSymbol)constructMethod.Parameters[0].Type;
            Debug.Assert(spanType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions));

            var span = VisitArrayOrSpanCollectionExpression(node, spanType, spanType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type);
            return new BoundCall(
                node.Syntax,
                receiverOpt: null,
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
        }

        private BoundExpression MakeCollectionExpressionSpreadElement(BoundCollectionExpressionSpreadElement initializer)
        {
            var enumeratorInfo = initializer.EnumeratorInfoOpt;
            var addElementPlaceholder = initializer.AddElementPlaceholder;

            Debug.Assert(enumeratorInfo is { });
            Debug.Assert(addElementPlaceholder is { });
            Debug.Assert(addElementPlaceholder.Type is { });

            var syntax = (CSharpSyntaxNode)initializer.Syntax;
            var iterationVariable = _factory.SynthesizedLocal(addElementPlaceholder.Type, syntax);
            var convertedExpression = (BoundConversion)initializer.Expression;

            AddPlaceholderReplacement(addElementPlaceholder, _factory.Local(iterationVariable));

            var rewrittenBody = VisitStatement(initializer.AddMethodInvocation);
            Debug.Assert(rewrittenBody is { });

            RemovePlaceholderReplacement(addElementPlaceholder);

            var elementPlaceholder = initializer.ElementPlaceholder;
            var iterationVariables = ImmutableArray.Create(iterationVariable);
            var breakLabel = new GeneratedLabelSymbol("break");
            var continueLabel = new GeneratedLabelSymbol("continue");

            BoundStatement statement;
            if (convertedExpression.Operand.Type is ArrayTypeSymbol arrayType)
            {
                if (arrayType.IsSZArray)
                {
                    statement = RewriteSingleDimensionalArrayForEachEnumerator(
                        initializer,
                        convertedExpression.Operand,
                        elementPlaceholder,
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
                        initializer,
                        convertedExpression.Operand,
                        elementPlaceholder,
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
                    initializer,
                    convertedExpression,
                    enumeratorInfo,
                    elementPlaceholder,
                    elementConversion: null,
                    iterationVariables,
                    deconstruction: null,
                    awaitableInfo: null,
                    breakLabel,
                    continueLabel,
                    rewrittenBody);
            }

            _needsSpilling = true;
            return _factory.SpillSequence(
                ImmutableArray<LocalSymbol>.Empty,
                ImmutableArray.Create(statement),
                result: _factory.Literal(0)); // result is unused
        }
    }
}
