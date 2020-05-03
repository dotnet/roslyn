// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public readonly int InsertPosition;

        public Solution Solution => Document.Project.Solution;

        public ChangeSignatureAnalysisSucceededContext(
            Document document, int insertPosition, ISymbol symbol, ParameterConfiguration parameterConfiguration)
        {
            Document = document;
            Symbol = symbol;
            ParameterConfiguration = parameterConfiguration;
            InsertPosition = insertPosition;
        }
    }

    internal sealed class CannotChangeSignatureAnalyzedContext : ChangeSignatureAnalyzedContext
    {
        public readonly CannotChangeSignatureReason CannotChangeSignatureReason;

        public CannotChangeSignatureAnalyzedContext(CannotChangeSignatureReason reason)
        {
            CannotChangeSignatureReason = reason;
        }
    }
}
