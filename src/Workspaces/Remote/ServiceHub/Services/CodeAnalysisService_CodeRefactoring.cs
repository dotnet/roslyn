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
        public Task<bool> HasRefactoringsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    using var pasteTrackingServiceRegistration = RemotePasteTrackingService.RegisterCallback(new PasteTrackingServiceCallback(EndPoint, cancellationToken));

                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var mefHostExportProvider = (IMefHostExportProvider)solution.Workspace.Services.HostServices;
                    var service = mefHostExportProvider.GetExports<ICodeRefactoringService>().Single().Value;

                    return await service.HasRefactoringsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private sealed class PasteTrackingServiceCallback : IPasteTrackingService
        {
            private readonly RemoteEndPoint _endPoint;
            private readonly CancellationToken _cancellationToken;

            public PasteTrackingServiceCallback(RemoteEndPoint endPoint, CancellationToken cancellationToken)
            {
                _endPoint = endPoint;
                _cancellationToken = cancellationToken;
            }

            public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
            {
                var result = _endPoint.InvokeAsync<TextSpan?>(nameof(TryGetPastedTextSpan), new object[] { sourceTextContainer }, _cancellationToken).Result;
                textSpan = result.GetValueOrDefault();
                return result.HasValue;
            }
        }
    }
}
