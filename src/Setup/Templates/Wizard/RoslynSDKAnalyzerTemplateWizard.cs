using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using VSLangProj;

public class RoslynSDKAnalyzerTemplateWizard : RoslynSDKChildTemplateWizard
{
    public static Project Project { get; private set; }

    public override void OnProjectFinishedGenerating(Project project)
    {
        // We don't want to copy roslyn binaries to the output folder because they will be 
        // included in the VSIX. The only way to solve this is to have th wizard mark the
        // assemblies as copy local false.
        Project = project;
        var vsProject = project.Object as VSProject;
        if (vsProject != null)
        {
            if (vsProject.References != null)
            {
                foreach (Reference reference in vsProject.References)
                {
                    if (reference.Name.Contains("Microsoft.CodeAnalysis") ||
                        reference.Name.Contains("System.Collections.Immutable") ||
                        reference.Name.Contains("System.Composition") ||
                        reference.Name.Contains("System.Reflection.Metadata"))
                    {
                        reference.CopyLocal = false;
                    }
                }
            }
        }
    }
}
