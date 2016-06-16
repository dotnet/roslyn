// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShimWithServices : CSharpProjectShim, IProjectCodeModelProvider
    {
        public CSharpProjectShimWithServices(
            ICSharpProjectRoot projectRoot,
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
            : base(
                  projectRoot,
                  projectTracker,
                  reportExternalErrorCreatorOpt,
                  projectSystemName,
                  hierarchy,
                  serviceProvider,
                  visualStudioWorkspaceOpt,
                  hostDiagnosticUpdateSourceOpt)
        {
        }

        protected override bool CanUseTextBuffer(ITextBuffer textBuffer)
        {
            // In Web scenarios, the project system tells us about all files in the project, including ".aspx" and ".cshtml" files.
            // The impact of this is that we try to add a StandardTextDocument for the file, and parse it on disk, etc, which won't
            // end well.  We prevent that from happening by not allowing buffers that aren't of our content type to be used for
            // StandardTextDocuments.  In the web scenarios, we will instead end up creating a ContainedDocument that actually 
            // knows about the secondary buffer that contains valid code in our content type.
            return textBuffer.ContentType.IsOfType(ContentTypeNames.CSharpContentType);
        }
    }
}
