// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureAnalyzedContext
    {
        public readonly bool CanChangeSignature;
        public readonly Project Project;
        public readonly ISymbol Symbol;
        public readonly CannotChangeSignatureReason CannotChangeSignatureReason;
        public readonly ParameterConfiguration ParameterConfiguration;

        public Solution Solution => Project.Solution;

        public ChangeSignatureAnalyzedContext(
            Project project, ISymbol symbol, ParameterConfiguration parameterConfiguration)
        {
            CanChangeSignature = true;
            Project = project;
            Symbol = symbol;
            ParameterConfiguration = parameterConfiguration;
            CannotChangeSignatureReason = CannotChangeSignatureReason.None;
        }

        public ChangeSignatureAnalyzedContext(CannotChangeSignatureReason reason)
        {
            CanChangeSignature = false;
            CannotChangeSignatureReason = reason;
        }
    }
}
