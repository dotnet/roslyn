// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;

public class RoslynSDKAnalyzerTemplateWizard : RoslynSDKChildTemplateWizard
{
    public static Project? Project { get; private set; }

    public override void OnProjectFinishedGenerating(Project project)
    {
        Project = project;
    }
}
