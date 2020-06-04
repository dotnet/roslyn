// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using VSLangProj;

public class RoslynSDKCodeFixTemplateWizard : RoslynSDKChildTemplateWizard
{
    public static Project Project { get; private set; }

    public override void OnProjectFinishedGenerating(Project project)
    {
        Project = project;

        // There is no good way for the test project to reference the main project, so we will use the wizard.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        if (project.Object is VSProject vsProject)
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        {
            var referenceProject = vsProject.References.AddProject(RoslynSDKAnalyzerTemplateWizard.Project);
        }
    }
}
