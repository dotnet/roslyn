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
        public override BoundNode? VisitArrayOrSpanCollectionLiteralExpression(BoundArrayOrSpanCollectionLiteralExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });

            var syntax = node.Syntax;
            var collectionType = node.Type;
            var spanConstructor = node.SpanConstructor;

            var arrayType = collectionType as ArrayTypeSymbol;
            if (arrayType is null)
            {
                Debug.Assert(spanConstructor is { });
                Debug.Assert(TypeSymbol.Equals(collectionType, spanConstructor.ContainingType, TypeCompareKind.AllIgnoreOptions));

                var elementType = ((NamedTypeSymbol)collectionType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                arrayType = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, elementType);
            }

            var initializers = node.Initializers;
            int arrayLength = initializers.Length;
            // PROTOTYPE: Should [] be emitted as Array.Empty<T>()?
            var initialization = (arrayLength == 0)
                ? null
                : new BoundArrayInitialization(
                    syntax,
                    isInferred: false,
                    initializers.SelectAsArray(e => VisitExpression(e)));
            var array = new BoundArrayCreation(
                syntax,
                ImmutableArray.Create<BoundExpression>(
                    new BoundLiteral(
                        syntax,
                        ConstantValue.Create(arrayLength),
                        _compilation.GetSpecialType(SpecialType.System_Int32))),
                initialization,
                arrayType)
            { WasCompilerGenerated = true };

            if (spanConstructor is null)
            {
                return array;
            }

            Debug.Assert(TypeSymbol.Equals(array.Type, spanConstructor.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions));
            return new BoundObjectCreationExpression(syntax, spanConstructor, array);
        }

        public override BoundNode? VisitCollectionInitializerCollectionLiteralExpression(BoundCollectionInitializerCollectionLiteralExpression node)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.Type is { });

            var syntax = node.Syntax;
            var collectionType = node.Type;

            BoundExpression rewrittenReceiver;
            if (collectionType is TypeParameterSymbol typeParameter)
            {
                // PROTOTYPE: If we support _inExpressionLambda, see VisitNewT()
                // which does not call MakeNewT() in that case.
                rewrittenReceiver = MakeNewT(syntax, typeParameter);
            }
            else
            {
                var collectionCreation = node.CollectionCreation;
                Debug.Assert(collectionCreation is { });
                rewrittenReceiver = VisitExpression(collectionCreation);
            }

            // Create a temp for the collection.
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal temp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, isKnownToReferToTempIfReferenceType: true);
            var initializers = node.Initializers;
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(initializers.Length + 1);
            sideEffects.Add(assignmentToTemp);

            AddPlaceholderReplacement(node.Placeholder, temp);

            foreach (var initializer in initializers)
            {
                BoundExpression? rewrittenInitializer;
                switch (initializer)
                {
                    case BoundCollectionElementInitializer element:
                        rewrittenInitializer = MakeCollectionInitializer(temp, element);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
                }
                if (rewrittenInitializer != null)
                {
                    sideEffects.Add(rewrittenInitializer);
                }
            }

            RemovePlaceholderReplacement(node.Placeholder);

            return new BoundSequence(
                syntax,
                ImmutableArray.Create(temp.LocalSymbol),
                sideEffects.ToImmutableAndFree(),
                temp,
                collectionType);
        }
    }
}
