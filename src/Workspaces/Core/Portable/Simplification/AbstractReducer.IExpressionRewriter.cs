// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract partial class AbstractReducer
    {
        internal interface IReductionRewriter : IDisposable
        {
            void Initialize(ParseOptions parseOptions, OptionSet optionSet, CancellationToken cancellationToken);

            SyntaxNodeOrToken VisitNodeOrToken(SyntaxNodeOrToken nodeOrTokenToReduce, SemanticModel semanticModel, bool simplifyAllDescendants);

            bool HasMoreWork { get; }
        }
    }
}
