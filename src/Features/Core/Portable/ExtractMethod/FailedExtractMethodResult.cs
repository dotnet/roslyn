// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class FailedExtractMethodResult : ExtractMethodResult
    {
        public FailedExtractMethodResult(OperationStatus status)
            : base(status.Flag, status.Reasons, null, default, null)
        {
        }
    }
}
