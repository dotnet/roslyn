// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
