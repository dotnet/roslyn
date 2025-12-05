// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal static class CopilotAnalysisUtilities
{
    public static Task AnalyzeCopilotChangeAsync(
        Document document,
        bool accepted,
        string featureId,
        string proposalId,
        IEnumerable<TextChange> textChanges,
        CancellationToken cancellationToken)
    => CopilotChangeAnalysisUtilities.AnalyzeCopilotChangeAsync(
        document, accepted, featureId, proposalId, CodeAnalysis.Copilot.CopilotUtilities.TryNormalizeCopilotTextChanges(textChanges), cancellationToken);
}
