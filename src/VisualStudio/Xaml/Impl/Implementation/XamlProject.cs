// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal class XamlProject : AbstractLegacyProject
    {
        public XamlProject(VisualStudioProjectTracker projectTracker, IVsHierarchy hierarchy, IServiceProvider serviceProvider, VisualStudioWorkspaceImpl visualStudioWorkspace) :
            base(
                projectTracker,
                reportExternalErrorCreatorOpt: null,
                projectSystemName: $"{XamlProject.GetProjectName(hierarchy)}-{nameof(XamlProject)}",
                hierarchy: hierarchy,
                language: StringConstants.XamlLanguageName,
                serviceProvider: serviceProvider,
                visualStudioWorkspaceOpt: visualStudioWorkspace,
                hostDiagnosticUpdateSourceOpt: null)
        {
            projectTracker.AddProject(this);

            // Make sure the solution crawler is running in this workspace
            visualStudioWorkspace.StartSolutionCrawler();
        }

        private static string GetProjectName(IVsHierarchy hierarchy)
        {
            return hierarchy.TryGetName(out var name) ? name : null;
        }

        private string GetDebuggerDisplay()
        {
            return $"{this.DisplayName}";
        }
    }
}
