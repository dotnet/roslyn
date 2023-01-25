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
        public override BoundNode? VisitCollectionLiteralExpression(BoundCollectionLiteralExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });

            var collectionType = node.Type;
            var initializers = node.Initializers;
            if (collectionType is ArrayTypeSymbol) // PROTOTYPE: Span<T> and ReadOnlySpan<T> should also use this code path.
            {
                if (initializers.Any(e => e.Kind == BoundKind.CollectionLiteralSpreadOperator))
                {
                    // A collection literal with a spread operator cannot be lowered to an array initializer
                    // because the array size is not known at compile time. Instead, the array is created
                    // from an intermediate List<T> is created.
                    throw ExceptionUtilities.Unreachable(); // PROTOTYPE: ...
                }

                return VisitCollectionLiteralArrayExpression(node);
            }

            var syntax = node.Syntax;
            var constructor = node.Constructor;
            BoundExpression rewrittenReceiver = constructor.IsDefaultValueTypeConstructor()
                ? new BoundDefaultExpression(syntax, collectionType)
                : new BoundObjectCreationExpression(syntax, constructor);

            if (initializers.IsEmpty)
            {
                return rewrittenReceiver;
            }

            // Create a temp for the collection.
            BoundAssignmentOperator boundAssignmentToTemp;
            BoundLocal temp = _factory.StoreToTemp(rewrittenReceiver, out boundAssignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            rewrittenReceiver = temp;

            var placeholder = node.Placeholder;
            AddPlaceholderReplacement(placeholder, rewrittenReceiver);

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
                        rewrittenInitializer = MakeCollectionLiteralDictionaryElement(rewrittenReceiver, dictionaryElement);
                        break;
                    case BoundCollectionLiteralSpreadOperator spreadInitializer:
                        rewrittenInitializer = MakeCollectionLiteralSpreadOperator(spreadInitializer);
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

            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(1 + loweredInitializers.Count);
            sideEffects.Add(boundAssignmentToTemp);
            sideEffects.AddRange(loweredInitializers);
            loweredInitializers.Free();

            return new BoundSequence(
                syntax,
                ImmutableArray.Create(temp.LocalSymbol),
                sideEffects.ToImmutableAndFree(),
                temp,
                collectionType);
        }

        private BoundExpression VisitCollectionLiteralArrayExpression(BoundCollectionLiteralExpression node)
        {
            var arrayType = (ArrayTypeSymbol?)node.Type;
            Debug.Assert(arrayType is { });

            var elementType = arrayType.ElementType;
            var syntax = node.Syntax;
            var initializers = node.Initializers;

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

        private BoundExpression MakeCollectionLiteralSpreadOperator(BoundCollectionLiteralSpreadOperator initializer)
        {
            var enumeratorInfo = initializer.EnumeratorInfoOpt;
            Debug.Assert(enumeratorInfo is { });

            var syntax = initializer.Syntax;
            var iterationVariable = _factory.SynthesizedLocal(enumeratorInfo.ElementType, syntax);

            AddPlaceholderReplacement(initializer.AddElementPlaceholder, _factory.Local(iterationVariable));
            var rewrittenBody = _factory.ExpressionStatement(VisitExpression(initializer.AddMethodInvocation));
            RemovePlaceholderReplacement(initializer.AddElementPlaceholder);

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
