// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class SimpleExtractMethodResult : ExtractMethodResult
    {
        public SimpleExtractMethodResult(
            OperationStatus status,
            Document document,
            SyntaxToken invocationNameToken,
            SyntaxNode methodDefinition)
            : base(status.Flag, status.Reasons, document, invocationNameToken, methodDefinition)
        {
        }
    }
}
