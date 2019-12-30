// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public Task<bool> HasRefactoringsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    using var pasteTrackingServiceRegistration = RemotePasteTrackingService.RegisterCallback(new PasteTrackingServiceCallback(this, cancellationToken));

                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var mefHostExportProvider = (IMefHostExportProvider)solution.Workspace.Services.HostServices;
                    var service = mefHostExportProvider.GetExports<ICodeRefactoringService>().Single().Value;

                    return await service.HasRefactoringsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private sealed class PasteTrackingServiceCallback : IPasteTrackingService
        {
            private readonly CodeAnalysisService _codeAnalysisService;
            private readonly CancellationToken _cancellationToken;

            public PasteTrackingServiceCallback(CodeAnalysisService codeAnalysisService, CancellationToken cancellationToken)
            {
                _codeAnalysisService = codeAnalysisService;
                _cancellationToken = cancellationToken;
            }

            public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
            {
                var result = _codeAnalysisService.InvokeAsync<TextSpan?>(nameof(TryGetPastedTextSpan), new object[] { sourceTextContainer }, _cancellationToken).Result;
                textSpan = result.GetValueOrDefault();
                return result.HasValue;
            }
        }
    }
}
