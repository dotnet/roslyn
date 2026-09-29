// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EnvDTE;

public class RoslynSDKAnalyzerTemplateWizard : RoslynSDKChildTemplateWizard
{
    public static Project? Project { get; private set; }

    public override void OnProjectFinishedGenerating(Project project)
    {
        Project = project;
    }
}
