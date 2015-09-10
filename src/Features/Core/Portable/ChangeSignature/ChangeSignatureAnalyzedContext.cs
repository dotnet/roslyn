// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureAnalyzedContext
    {
        public readonly bool CanChangeSignature;
        public readonly Solution Solution;
        public readonly ISymbol Symbol;
        public readonly CannotChangeSignatureReason CannotChangeSignatureReason;
        public readonly ParameterConfiguration ParameterConfiguration;

        public ChangeSignatureAnalyzedContext(Solution solution, ISymbol symbol, ParameterConfiguration parameterConfiguration)
        {
            this.CanChangeSignature = true;
            this.Solution = solution;
            this.Symbol = symbol;
            this.ParameterConfiguration = parameterConfiguration;
            this.CannotChangeSignatureReason = CannotChangeSignatureReason.None;
        }

        public ChangeSignatureAnalyzedContext(CannotChangeSignatureReason reason)
        {
            this.CanChangeSignature = false;
            this.CannotChangeSignatureReason = reason;
        }
    }
}
