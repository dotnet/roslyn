// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitCollectionLiteralExpression(BoundCollectionLiteralExpression node)
        {
            Debug.Assert(node.Type is { });

            var collectionType = node.Type;
            var syntax = node.Syntax;
            var constructor = node.Constructor;

            BoundExpression collectionCreation = constructor.IsDefaultValueTypeConstructor()
                ? new BoundDefaultExpression(syntax, collectionType)
                : new BoundObjectCreationExpression(syntax, constructor);

            var initializer = node.InitializerExpressionOpt;
            return initializer is null
                ? collectionCreation
                : MakeExpressionWithInitializer(syntax, collectionCreation, initializer, collectionType);
        }
    }
}
