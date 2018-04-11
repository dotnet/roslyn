// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
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
            var generator = new EditorConfigFileGenerator(dte);
            (bool success, string fileName) = generator.TryGenerateFile();
            if (success)
            {
                generator.OpenFile(fileName);
            }
        }
    }
}
