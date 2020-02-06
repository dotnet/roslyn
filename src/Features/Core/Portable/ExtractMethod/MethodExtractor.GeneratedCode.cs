// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        internal class GeneratedCode
        {
            public GeneratedCode(
                OperationStatus status,
                SemanticDocument document,
                SyntaxAnnotation methodNameAnnotation,
                SyntaxAnnotation callsiteAnnotation,
                SyntaxAnnotation methodDefinitionAnnotation)
            {
                Contract.ThrowIfNull(document);
                Contract.ThrowIfNull(methodNameAnnotation);
                Contract.ThrowIfNull(callsiteAnnotation);
                Contract.ThrowIfNull(methodDefinitionAnnotation);

                Status = status;
                SemanticDocument = document;
                MethodNameAnnotation = methodNameAnnotation;
                CallSiteAnnotation = callsiteAnnotation;
                MethodDefinitionAnnotation = methodDefinitionAnnotation;
            }

            public OperationStatus Status { get; }
            public SemanticDocument SemanticDocument { get; }

            public SyntaxAnnotation MethodNameAnnotation { get; }
            public SyntaxAnnotation CallSiteAnnotation { get; }
            public SyntaxAnnotation MethodDefinitionAnnotation { get; }
        }
    }
}
