﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAnonymousObjectCreationExpression(BoundAnonymousObjectCreationExpression node)
        {
            // Rewrite the arguments.
            var rewrittenArguments = VisitList(node.Arguments);

            return new BoundObjectCreationExpression(
                syntax: node.Syntax,
                constructor: node.Constructor,
                arguments: rewrittenArguments,
                argumentNamesOpt: default(ImmutableArray<string>),
                argumentRefKindsOpt: default(ImmutableArray<RefKind>),
                expanded: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                constantValueOpt: null,
                initializerExpressionOpt: null,
                type: node.Type);
        }
    }
}
