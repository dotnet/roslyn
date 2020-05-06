// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteCodeRefactoringService
    {
        public Task<bool> HasRefactoringsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, TextSpan? pastedTextSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetRequiredDocument(documentId);

                    var mefHostExportProvider = (IMefHostExportProvider)solution.Workspace.Services.HostServices;
                    var service = mefHostExportProvider.GetExports<ICodeRefactoringService>().Single().Value;

                    // Make sure the paste tracking service for this process has the correct text span
                    var pasteTrackingService = (RemotePasteTrackingService?)mefHostExportProvider.GetExports<IPasteTrackingService>().SingleOrDefault()?.Value;
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var container = sourceText.Container;
                    using var _ = pasteTrackingService?.SetPastedTextSpanForRemoteCall(container, pastedTextSpan);

                    return await service.HasRefactoringsAsync(document, textSpan, pastedTextSpan, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
