// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;
#endif

internal interface IExternalCSharpCopilotGenerateDocumentationService
{
    Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentAsync(CopilotDocumentationCommentProposalWrapper proposal, CancellationToken cancellationToken);
}
