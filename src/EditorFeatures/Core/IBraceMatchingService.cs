// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IBraceMatchingService
    {
        Task<BraceMatchingResult?> GetMatchingBracesAsync(Document document, int position, CancellationToken cancellationToken = default);
    }

    internal struct BraceMatchingResult
    {
        public TextSpan LeftSpan { get; }
        public TextSpan RightSpan { get; }

        public BraceMatchingResult(TextSpan leftSpan, TextSpan rightSpan)
            : this()
        {
            this.LeftSpan = leftSpan;
            this.RightSpan = rightSpan;
        }
    }
}
