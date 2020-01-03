// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal readonly struct CSharpMetadataContext
    {
        internal readonly CSharpCompilation Compilation;
        internal readonly EvaluationContext? EvaluationContext;

        internal CSharpMetadataContext(CSharpCompilation compilation, EvaluationContext? evaluationContext = null)
        {
            Compilation = compilation;
            EvaluationContext = evaluationContext;
        }
    }
}
