// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKChildTemplateWizard
{
    public virtual void OnProjectFinishedGenerating(Project project) { }

    private void OnRunStarted(Dictionary<string, string> replacementsDictionary)
    {
        // Add the root project name to the projects replacement dictionary
        if (RoslynSDKRootTemplateWizard.GlobalDictionary.TryGetValue("$saferootprojectname$", out var safeRootProjectName))
        {
            replacementsDictionary.Add("$saferootprojectname$", safeRootProjectName);
        }

        if (RoslynSDKRootTemplateWizard.GlobalDictionary.TryGetValue("$saferootidentifiername$", out var saferootidentifiername))
        {
            replacementsDictionary.Add("$saferootidentifiername$", saferootidentifiername);
        }
    }
}
