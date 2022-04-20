// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Generator;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Utilities;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;

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
        using var _ = LogUserAction(UserTask.CreateFromTemplate, new TemplateInfo(runKind, replacementsDictionary));
        Assert(automationObject is DTE2, "Automation Object is wrong kind");
        if (automationObject is DTE2 dte)
        {
            var replacementDictionaryLookup = replacementsDictionary.TryGetValue("$type$", out var result);
            Assert(replacementDictionaryLookup, "Invalid replacements dictionary in template file");
            if (!replacementDictionaryLookup)
            {
                return;
            }

            bool isDotnet = StringComparer.OrdinalIgnoreCase.Compare(result, "dotnet") == 0;
            (bool success, string fileName) = EditorConfigFileGenerator.TryAddFileToSolution(isDotnet);
            Assert(success, "Unable to add the editorconfig file to the solution");

            if (success)
            {
                VSHelpers.OpenFile(fileName);
            }
        }
    }
}
