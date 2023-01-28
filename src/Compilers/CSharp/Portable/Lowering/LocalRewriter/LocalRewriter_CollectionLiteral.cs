// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitCollectionLiteralExpression(BoundCollectionLiteralExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });

            var syntax = node.Syntax;
            var collectionType = node.CollectionType ?? node.Type;
            var initializers = node.Initializers;

            if (collectionType is ArrayTypeSymbol arrayType)
            {
                return MakeCollectionLiteralArrayExpression(syntax, arrayType, initializers);
            }

            BoundExpression collection;

            if (collectionType.IsSpanOrReadOnlySpanT(out var elementType))
            {
                collection = MakeCollectionLiteralArrayExpression(syntax, ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType), initializers);
            }
            else
            {
                Debug.Assert(node.Constructor is { });
                var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
                BoundLocal temp;
                MakeCollectionLiteralExpression(syntax, (NamedTypeSymbol)collectionType, node.Constructor, node.Placeholder, initializers, sideEffects, out temp);

                collection = new BoundSequence(
                    syntax,
                    ImmutableArray.Create(temp.LocalSymbol),
                    sideEffects.ToImmutableAndFree(),
                    temp,
                    collectionType);

                if (node.ToArray is { } toArrayMethod)
                {
                    Debug.Assert(TypeSymbol.Equals(collection.Type, toArrayMethod.ContainingType, TypeCompareKind.AllIgnoreOptions));
                    collection = _factory.Call(collection, toArrayMethod);
                }
            }

            var targetType = node.Type;
            if (node.SpanConstructor is { } spanConstructor)
            {
                Debug.Assert(TypeSymbol.Equals(collection.Type, spanConstructor.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions));
                Debug.Assert(TypeSymbol.Equals(targetType, spanConstructor.ContainingType, TypeCompareKind.AllIgnoreOptions));
                collection = new BoundObjectCreationExpression(syntax, spanConstructor, collection);
            }

            return collection;
        }

        private void MakeCollectionLiteralExpression(
            SyntaxNode syntax,
            NamedTypeSymbol collectionType,
            MethodSymbol constructor,
            BoundObjectOrCollectionValuePlaceholder placeholder,
            ImmutableArray<BoundExpression> initializers,
            ArrayBuilder<BoundExpression> sideEffects,
            out BoundLocal temp)
        {
            Debug.Assert(constructor is { });

            BoundExpression rewrittenReceiver = constructor.IsDefaultValueTypeConstructor()
                ? new BoundDefaultExpression(syntax, collectionType)
                : new BoundObjectCreationExpression(syntax, constructor);

            // Create a temp for the collection.
            BoundAssignmentOperator assignmentToTemp;
            temp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            sideEffects.Add(assignmentToTemp);

            AddPlaceholderReplacement(placeholder, temp);

            var loweredInitializers = ArrayBuilder<BoundExpression>.GetInstance(initializers.Length);

            foreach (var initializer in initializers)
            {
                BoundExpression? rewrittenInitializer;
                switch (initializer)
                {
                    case BoundCollectionLiteralElement element:
                        // PROTOTYPE: See MakeCollectionInitializer() which explicitly handles an extension method Add() with 'ref this' parameter.
                        rewrittenInitializer = MakeCollectionLiteralElement(element);
                        break;
                    case BoundCollectionLiteralDictionaryElement dictionaryElement:
                        rewrittenInitializer = MakeCollectionLiteralDictionaryElement(temp, dictionaryElement);
                        break;
                    case BoundCollectionLiteralSpreadElement spreadElement:
                        rewrittenInitializer = MakeCollectionLiteralSpreadElement(spreadElement);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
                }
                if (rewrittenInitializer != null)
                {
                    loweredInitializers.Add(rewrittenInitializer);
                }
            }

            RemovePlaceholderReplacement(placeholder);

            sideEffects.AddRange(loweredInitializers);
            loweredInitializers.Free();
        }

        private BoundExpression MakeCollectionLiteralArrayExpression(
            SyntaxNode syntax,
            ArrayTypeSymbol arrayType,
            ImmutableArray<BoundExpression> initializers)
        {
            int n = initializers.Length;
            BoundArrayInitialization? initialization = null;

            if (n > 0)
            {
                initialization = new BoundArrayInitialization(
                    syntax,
                    isInferred: false,
                    initializers.SelectAsArray(e => rewriteInitializer(e)));
            }

            return new BoundArrayCreation(
                syntax,
                ImmutableArray.Create<BoundExpression>(
                    new BoundLiteral(
                        syntax,
                        ConstantValue.Create(n),
                        _compilation.GetSpecialType(SpecialType.System_Int32))),
                initialization,
                arrayType)
            { WasCompilerGenerated = true };

            BoundExpression rewriteInitializer(BoundExpression initializer)
            {
                switch (initializer)
                {
                    case BoundCollectionLiteralElement element:
                        var rewrittenInitializer = VisitExpression(element.Expression);
                        Debug.Assert(rewrittenInitializer is { });
                        return rewrittenInitializer;

                    case BoundCollectionLiteralDictionaryElement dictionaryElement:
                        var rewrittenKey = VisitExpression(dictionaryElement.Key);
                        var rewrittenValue = VisitExpression(dictionaryElement.Value);
                        throw ExceptionUtilities.Unreachable(); // PROTOTYPE: Combine with new KeyValuePair<TKey, TValue(...).

                    default:
                        throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
                }
            }
        }

        private BoundExpression MakeCollectionLiteralElement(BoundCollectionLiteralElement initializer)
        {
            var rewrittenExpression = VisitExpression(initializer.Expression);
            Debug.Assert(rewrittenExpression is { });

            var addElementPlaceholder = initializer.AddElementPlaceholder;
            Debug.Assert(addElementPlaceholder is { });

            AddPlaceholderReplacement(addElementPlaceholder, rewrittenExpression);
            // PROTOTYPE: See MakeCollectionInitializer() which explicitly handles an extension method Add() with 'ref this' parameter.
            var rewrittenInitializer = VisitExpression(initializer.AddMethodInvocation);
            RemovePlaceholderReplacement(addElementPlaceholder);

            Debug.Assert(rewrittenInitializer is { });
            return rewrittenInitializer;
        }

        private BoundExpression MakeCollectionLiteralDictionaryElement(BoundExpression rewrittenReceiver, BoundCollectionLiteralDictionaryElement initializer)
        {
            var syntax = initializer.Syntax;
            var indexer = initializer.Indexer;

            var rewrittenKey = VisitExpression(initializer.Key);
            var rewrittenValue = VisitExpression(initializer.Value);

            var rewrittenLeft = MakeIndexerAccess(
                syntax,
                rewrittenReceiver,
                indexer,
                ImmutableArray.Create(rewrittenKey),
                argumentNamesOpt: default,
                argumentRefKindsOpt: default,
                expanded: false,
                argsToParamsOpt: default,
                defaultArguments: default,
                type: indexer.Type,
                oldNodeOpt: null,
                isLeftOfAssignment: true);

            return MakeAssignmentOperator(
                syntax,
                rewrittenLeft,
                rewrittenValue,
                type: _factory.SpecialType(SpecialType.System_Void),
                used: false,
                isChecked: false,
                isCompoundAssignment: false);
        }

        private BoundExpression MakeCollectionLiteralSpreadElement(BoundCollectionLiteralSpreadElement initializer)
        {
            var enumeratorInfo = initializer.EnumeratorInfo;
            var addElementPlaceholder = initializer.AddElementPlaceholder;

            Debug.Assert(enumeratorInfo is { });
            Debug.Assert(addElementPlaceholder is { });

            var syntax = initializer.Syntax;
            var iterationVariable = _factory.SynthesizedLocal(enumeratorInfo.ElementType, syntax);

            AddPlaceholderReplacement(addElementPlaceholder, _factory.Local(iterationVariable));
            var rewrittenAdd = VisitExpression(initializer.AddMethodInvocation);
            var rewrittenBody = _factory.ExpressionStatement(rewrittenAdd!);
            RemovePlaceholderReplacement(addElementPlaceholder);

            var statement = RewriteForEachEnumerator(
                initializer,
                (BoundConversion)initializer.Expression,
                enumeratorInfo,
                initializer.ElementPlaceholder,
                initializer.ElementConversion,
                iterationVariables: ImmutableArray.Create(iterationVariable),
                deconstruction: null,
                awaitableInfo: null,
                breakLabel: new GeneratedLabelSymbol("break"), // PROTOTYPE: Is this needed?
                continueLabel: new GeneratedLabelSymbol("continue"), // PROTOTYPE: Is this needed?
                rewrittenBody: rewrittenBody);

            _needsSpilling = true;
            return _factory.SpillSequence(
                ImmutableArray<LocalSymbol>.Empty,
                ImmutableArray.Create(statement),
                result: _factory.Literal(0)); // PROTOTYPE: The result is unused. Can we avoid creating it entirely?
        }
    }
}
