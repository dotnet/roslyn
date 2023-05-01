// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify;

internal interface IRemoteFullyQualifyService
{
    ValueTask<FullyQualifyFixData?> GetFixDataAsync(Checksum solutionChecksum, DocumentId documentId, TextSpan span, bool hideAdvancedMembers, CancellationToken cancellationToken);
}
