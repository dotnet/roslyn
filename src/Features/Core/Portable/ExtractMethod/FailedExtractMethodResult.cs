// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class FailedExtractMethodResult : ExtractMethodResult
    {
        public FailedExtractMethodResult(OperationStatus status)
            : base(status.Flag, status.Reasons, null, default(SyntaxToken), default(SyntaxNode))
        {
        }
    }
}
