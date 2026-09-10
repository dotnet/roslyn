// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EnvDTE;
using Microsoft.VisualStudio.Shell;

public class RoslynSDKVsixTemplateWizardSecondProject : RoslynSDKTestTemplateWizard
{
    public override void OnProjectFinishedGenerating(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        base.OnProjectFinishedGenerating(project);

        // set the VSIX project to be the starting project
        var dte = project.DTE;
        if (dte.Solution.Projects.Count == 2)
        {
            dte.Solution.Properties.Item("StartupProject").Value = project.Name;
        }
    }
}
