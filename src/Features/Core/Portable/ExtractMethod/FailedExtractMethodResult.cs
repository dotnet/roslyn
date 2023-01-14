// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class FailedExtractMethodResult : ExtractMethodResult
    {
        public FailedExtractMethodResult(OperationStatus status)
            : base(status.Flag, status.Reasons, null, ImmutableArray<AbstractFormattingRule>.Empty, default, null)
        {
        }
    }
}
