// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PasteTracking;
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
                    var document = solution.GetDocument(documentId);

                    var mefHostExportProvider = (IMefHostExportProvider)solution.Workspace.Services.HostServices;
                    var service = mefHostExportProvider.GetExports<ICodeRefactoringService>().Single().Value;
                    var pasteTrackingService = (RemotePasteTrackingService)mefHostExportProvider.GetExports<IPasteTrackingService>().SingleOrDefault()?.Value;

                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var container = sourceText.Container;

                    try
                    {
                        pasteTrackingService?.SetPastedTextSpan(container, pastedTextSpan);
                        return await service.HasRefactoringsAsync(document, textSpan, pastedTextSpan, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        pasteTrackingService?.ClearPastedTextSpan(container);
                    }
                }
            }, cancellationToken);
        }
    }
}
