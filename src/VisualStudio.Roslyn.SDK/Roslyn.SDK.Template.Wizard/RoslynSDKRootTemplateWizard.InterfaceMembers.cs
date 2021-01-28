// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKRootTemplateWizard : IWizard
{
    public void BeforeOpeningFile(ProjectItem projectItem) { }
    public void ProjectFinishedGenerating(Project project) { }
    public void RunFinished() { }
    public bool ShouldAddProjectItem(string filePath) => true;
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) { }
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (automationObject is DTE dte)
        {
            OnRunStarted(dte, replacementsDictionary, runKind, customParams);
        }
    }
}
