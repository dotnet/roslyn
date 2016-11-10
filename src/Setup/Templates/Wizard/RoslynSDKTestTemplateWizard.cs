using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using VSLangProj;

public class RoslynSDKTestTemplateWizard : RoslynSDKChildTemplateWizard
{
    public override void OnProjectFinishedGenerating(Project project)
    {
        // There is no good way for the test project to reference the main project, so we will use the wizard.
        var vsProject = project.Object as VSProject;
        if (vsProject != null)
        {
            var referenceProject = vsProject.References.AddProject(RoslynSDKAnalyzerTemplateWizard.Project);
        }
    }
}
