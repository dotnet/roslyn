// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.VisualStudio
{
    internal static class FSharpProjectExternalErrorReporterFactory
    {
        public static IVsLanguageServiceBuildErrorReporter2 Create(ProjectId projectId, string errorCodePrefix, IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var workspace = (VisualStudioWorkspaceImpl)serviceProvider.GetMefService<VisualStudioWorkspace>();
            workspace.SubscribeExternalErrorDiagnosticUpdateSourceToSolutionBuildEvents();
            return new ProjectExternalErrorReporter(projectId, errorCodePrefix, LanguageNames.FSharp, workspace);
        }
    }
}
