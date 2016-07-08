using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKRootTemplateWizard : IWizard
{
    public void BeforeOpeningFile(ProjectItem projectItem) { }
    public void ProjectFinishedGenerating(Project project) { }
    public void RunFinished() { }
    public bool ShouldAddProjectItem(string filePath) { return true; }
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) { }
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        OnRunStarted(automationObject as DTE, replacementsDictionary, runKind, customParams);
    }
}
