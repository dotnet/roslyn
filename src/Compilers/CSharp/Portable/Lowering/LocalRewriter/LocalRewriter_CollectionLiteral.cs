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
            var collectionType = node.Type;
            var constructor = node.Constructor;

            BoundExpression rewrittenReceiver = constructor.IsDefaultValueTypeConstructor()
                ? new BoundDefaultExpression(syntax, collectionType)
                : new BoundObjectCreationExpression(syntax, constructor);

            var initializers = node.Initializers;
            if (initializers.IsEmpty)
            {
                return rewrittenReceiver;
            }

            // Create a temp for the collection.
            BoundAssignmentOperator boundAssignmentToTemp;
            BoundLocal temp = _factory.StoreToTemp(rewrittenReceiver, out boundAssignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            rewrittenReceiver = temp;

            var loweredInitializers = ArrayBuilder<BoundExpression>.GetInstance(initializers.Length);

            var placeholder = node.Placeholder;
            AddPlaceholderReplacement(placeholder, rewrittenReceiver);

            // Rewrite initializer expressions.
            foreach (var initializer in initializers)
            {
                BoundExpression? rewrittenInitializer;
                switch (initializer)
                {
                    case BoundCollectionElementInitializer collectionElementInitializer:
                        rewrittenInitializer = MakeCollectionInitializer(rewrittenReceiver, collectionElementInitializer);
                        break;
                    case BoundDictionaryElementInitializer dictionaryElement:
                        rewrittenInitializer = MakeCollectionLiteralDictionaryElement(rewrittenReceiver, dictionaryElement);
                        break;
                    case BoundSpreadInitializer spreadInitializer:
                        rewrittenInitializer = MakeCollectionLiteralSpreadInitializer(rewrittenReceiver, spreadInitializer);
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

        private BoundExpression MakeCollectionLiteralDictionaryElement(BoundExpression rewrittenReceiver, BoundDictionaryElementInitializer initializer)
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

        private BoundExpression MakeCollectionLiteralSpreadInitializer(BoundExpression rewrittenReceiver, BoundSpreadInitializer initializer)
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
