// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class ChangeSignatureAnalyzedContext
    {
    }

    internal sealed class ChangeSignatureAnalyzedSucceedContext : ChangeSignatureAnalyzedContext
    {
        public readonly Document Document;
        public readonly ISymbol Symbol;
        public readonly ParameterConfiguration ParameterConfiguration;
        public readonly int InsertPosition;

        public Solution Solution => Document.Project.Solution;

        public ChangeSignatureAnalyzedSucceedContext(
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
