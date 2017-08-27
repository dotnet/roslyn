using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKChildTemplateWizard : IWizard
{
    /// <summary>
    /// Only one wizard will be run by the project system.  
    /// This means our wizard needs to load and call the nuget wizard for every function.
    /// </summary>
    private IWizard nugetWizard { get { return lazyWizard.Value; } }

    private Lazy<IWizard> lazyWizard = new Lazy<IWizard>(() =>
    {
        var asm = Assembly.Load("NuGet.VisualStudio.Interop, Version=1.0.0.0, Culture=Neutral, PublicKeyToken=b03f5f7f11d50a3a");
        return (IWizard)asm.CreateInstance("NuGet.VisualStudio.TemplateWizard");
    });

    public void BeforeOpeningFile(ProjectItem projectItem) { nugetWizard.BeforeOpeningFile(projectItem); }
    public void ProjectFinishedGenerating(Project project) { nugetWizard.ProjectFinishedGenerating(project); OnProjectFinishedGenerating(project); }
    public void RunFinished() { nugetWizard.RunFinished(); }
    public bool ShouldAddProjectItem(string filePath) { return nugetWizard.ShouldAddProjectItem(filePath); }
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) { nugetWizard.ProjectItemFinishedGenerating(projectItem); }
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        nugetWizard.RunStarted(automationObject, replacementsDictionary, runKind, customParams);
        OnRunStarted(automationObject as DTE, replacementsDictionary, runKind, customParams);
    }
}
