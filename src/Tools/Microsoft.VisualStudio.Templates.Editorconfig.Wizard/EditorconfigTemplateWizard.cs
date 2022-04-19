// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard;
using Microsoft.VisualStudio.TemplateWizard;
using Templates.EditorConfig.FileGenerator;

public partial class EditorconfigTemplateWizard : IWizard
{

    public void BeforeOpeningFile(ProjectItem projectItem) { }
    public void ProjectFinishedGenerating(Project project) { }
    public void RunFinished() { }
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) { }

    public bool ShouldAddProjectItem(string filePath) => true;

    public void RunStarted(object automationObject,
                           Dictionary<string, string> replacementsDictionary,
                           WizardRunKind runKind,
                           object[] customParams)
    {
        if (automationObject is DTE2 dte)
        {
            if(!replacementsDictionary.TryGetValue("$type$", out var result))
            {
                return;
            }

            bool isDotnet = StringComparer.OrdinalIgnoreCase.Compare(result, "dotnet") == 0;
            (bool success, string fileName) = EditorConfigFileGenerator.TryAddFileToSolution(isDotnet);
            if (success)
            {
                VSHelpers.OpenFile(fileName);
            }
        }
    }
}
