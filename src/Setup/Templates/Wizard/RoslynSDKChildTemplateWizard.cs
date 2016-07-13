using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKChildTemplateWizard
{
    public virtual void OnProjectFinishedGenerating(Project project) { }

    private void OnRunStarted(DTE dTE, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        // Add the root project name to the projects replacement dictionary
        string safeRootProjectName;
        if (RoslynSDKRootTemplateWizard.GlobalDictionary.TryGetValue("$saferootprojectname$", out safeRootProjectName))
        {
            replacementsDictionary.Add("$saferootprojectname$", safeRootProjectName);
        }

        string saferootidentifiername;
        if (RoslynSDKRootTemplateWizard.GlobalDictionary.TryGetValue("$saferootidentifiername$", out saferootidentifiername))
        {
            replacementsDictionary.Add("$saferootidentifiername$", saferootidentifiername);
        }
    }
}
