// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertLinqMethodToLinqQueryProvider : AbstractConvertLinqProvider
    {
        protected abstract class Analyzer<TSource, TDestination> : AnalyzerBase<TSource, TDestination>
            where TSource : SyntaxNode
            where TDestination : SyntaxNode
        {
            public Analyzer(SemanticModel semanticModel, CancellationToken cancellationToken) 
                : base(semanticModel, cancellationToken) { }
        }
    }
}
