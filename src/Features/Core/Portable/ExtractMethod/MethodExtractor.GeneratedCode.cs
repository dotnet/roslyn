// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class MethodExtractor<TSelectionResult, TStatementSyntax, TExpressionSyntax>
{
    internal sealed class GeneratedCode
    {
        public GeneratedCode(
            SemanticDocument document,
            SyntaxAnnotation methodNameAnnotation,
            SyntaxAnnotation callSiteAnnotation,
            SyntaxAnnotation methodDefinitionAnnotation)
        {
            Contract.ThrowIfNull(document);
            Contract.ThrowIfNull(methodNameAnnotation);
            Contract.ThrowIfNull(callSiteAnnotation);
            Contract.ThrowIfNull(methodDefinitionAnnotation);

            SemanticDocument = document;
            MethodNameAnnotation = methodNameAnnotation;
            CallSiteAnnotation = callSiteAnnotation;
            MethodDefinitionAnnotation = methodDefinitionAnnotation;
        }

        public SemanticDocument SemanticDocument { get; }

        public SyntaxAnnotation MethodNameAnnotation { get; }
        public SyntaxAnnotation CallSiteAnnotation { get; }
        public SyntaxAnnotation MethodDefinitionAnnotation { get; }
    }
}
