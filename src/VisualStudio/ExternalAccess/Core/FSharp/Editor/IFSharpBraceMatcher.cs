// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal readonly struct FSharpBraceMatchingResult
{
    public TextSpan LeftSpan { get; }
    public TextSpan RightSpan { get; }

    public FSharpBraceMatchingResult(TextSpan leftSpan, TextSpan rightSpan)
        : this()
    {
        this.LeftSpan = leftSpan;
        this.RightSpan = rightSpan;
    }
}

internal interface IFSharpBraceMatcher
{
    Task<FSharpBraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken = default);
}
