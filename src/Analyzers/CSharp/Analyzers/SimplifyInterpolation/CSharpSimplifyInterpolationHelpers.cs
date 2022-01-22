// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyInterpolation;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.SimplifyInterpolation
{
    internal sealed class CSharpSimplifyInterpolationHelpers : AbstractSimplifyInterpolationHelpers
    {
        public static CSharpSimplifyInterpolationHelpers Instance { get; } = new();

        private CSharpSimplifyInterpolationHelpers() { }

        protected override bool PermitNonLiteralAlignmentComponents => true;

        protected override SyntaxNode GetPreservedInterpolationExpressionSyntax(IOperation operation)
        {
            return operation.Syntax switch
            {
                ConditionalExpressionSyntax { Parent: ParenthesizedExpressionSyntax parent } => parent,
                var syntax => syntax,
            };
        }
    }
}
