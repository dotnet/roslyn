// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.BraceMatching;

internal interface IBraceMatchingService
{
    Task<BraceMatchingResult?> GetMatchingBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken);
}

[DataContract]
internal readonly record struct BraceMatchingResult(
    [property: DataMember(Order = 0)] TextSpan LeftSpan,
    [property: DataMember(Order = 1)] TextSpan RightSpan);
