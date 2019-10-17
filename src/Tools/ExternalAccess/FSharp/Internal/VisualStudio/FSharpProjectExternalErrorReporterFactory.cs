// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.VisualStudio
{
    internal static class FSharpProjectExternalErrorReporterFactory
    {
        public static IVsLanguageServiceBuildErrorReporter2 Create(ProjectId projectId, string errorCodePrefix, IServiceProvider serviceProvider)
            => new ProjectExternalErrorReporter(projectId, errorCodePrefix, (VisualStudioWorkspaceImpl)serviceProvider.GetMefService<VisualStudioWorkspace>());
    }
}
