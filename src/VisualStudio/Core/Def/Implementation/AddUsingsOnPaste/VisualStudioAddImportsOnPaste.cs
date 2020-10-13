// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Utilities;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.AddMissingImports;
using Microsoft.CodeAnalysis.AddMissingImports;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.AddUsingsOnPaste
{
    [ExportWorkspaceService(typeof(IAutomaticallyAddMissingImportsService)), Shared]
    internal class VisualStudioAddImportsOnPaste : IAutomaticallyAddMissingImportsService
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioAddImportsOnPaste(
            IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public void AddMissingUsings(Document document, TextSpan textSpan, IUIThreadOperationContext operationContext)
        {
            using var _ = operationContext.AddScope(allowCancellation: true, ServicesVSResources.Adding_missing_import_statements);
            var cancellationToken = operationContext.UserCancellationToken;

            var updatedProject = _threadingContext.JoinableTaskFactory.Run(() => AddMissingUsingsAsync(document, textSpan, cancellationToken));

            if (updatedProject is null)
            {
                return;
            }

            // Silent failure is fine because it doesn't disrupt the user here
            updatedProject.Solution.Workspace.TryApplyChanges(updatedProject.Solution);
        }

        private async Task<Project?> AddMissingUsingsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            // Check pasted text span for missing imports
            var addMissingImportsService = document.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
            var hasMissingImports = await addMissingImportsService.HasMissingImportsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (!hasMissingImports)
            {
                return null;
            }

            return await addMissingImportsService.AddMissingImportsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        }
    }
}
