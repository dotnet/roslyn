using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

public class RoslynSDKVsixTemplateWizardThirdProject : RoslynSDKTestTemplateWizard
{
    public override void OnProjectFinishedGenerating(Project project)
    {
        base.OnProjectFinishedGenerating(project);

        // set the VSIX project to be the starting project
        var dte = project.DTE;
        if (dte.Solution.Projects.Count == 3)
        {
            dte.Solution.Properties.Item("StartupProject").Value = project.Name;
        }
    }
}
