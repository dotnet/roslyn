// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class ChangeSignatureAnalyzedContext
    {
    }

    internal sealed class ChangeSignatureAnalysisSucceededContext : ChangeSignatureAnalyzedContext
    {
        public readonly Document Document;
        public readonly ISymbol Symbol;
        public readonly ParameterConfiguration ParameterConfiguration;
        public readonly int PositionForTypeBinding;
        public readonly CodeCleanupOptionsProvider FallbackOptions;

        public ChangeSignatureAnalysisSucceededContext(
            Document document, int positionForTypeBinding, ISymbol symbol, ParameterConfiguration parameterConfiguration, CodeCleanupOptionsProvider fallbackOptions)
        {
            Document = document;
            Symbol = symbol;
            ParameterConfiguration = parameterConfiguration;
            PositionForTypeBinding = positionForTypeBinding;
            FallbackOptions = fallbackOptions;
        }

        public Solution Solution => Document.Project.Solution;
    }

    internal sealed class CannotChangeSignatureAnalyzedContext : ChangeSignatureAnalyzedContext
    {
        public readonly ChangeSignatureFailureKind CannotChangeSignatureReason;

        public CannotChangeSignatureAnalyzedContext(ChangeSignatureFailureKind reason)
        {
            CannotChangeSignatureReason = reason;
        }
    }
}
