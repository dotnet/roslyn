// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        if (project.Object is VSProject vsProject)
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
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
