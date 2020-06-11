// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
