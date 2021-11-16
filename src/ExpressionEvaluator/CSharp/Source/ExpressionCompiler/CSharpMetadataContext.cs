// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
