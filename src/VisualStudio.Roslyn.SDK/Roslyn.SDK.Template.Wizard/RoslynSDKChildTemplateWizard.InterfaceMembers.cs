// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKChildTemplateWizard : IWizard
{
    public void BeforeOpeningFile(ProjectItem projectItem) { }
    public void ProjectFinishedGenerating(Project project)
    {
        OnProjectFinishedGenerating(project);
    }
    public void RunFinished() { }
    public bool ShouldAddProjectItem(string filePath) => true;
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) { }
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        WriteOutBetaNugetSource("dotnet.myget.org roslyn", "https://dotnet.myget.org/F/roslyn/api/v3/index.json");
        WriteOutBetaNugetSource("dotnet.myget.org roslyn-analyzers", "https://dotnet.myget.org/F/roslyn-analyzers/api/v3/index.json");
        OnRunStarted(automationObject as DTE, replacementsDictionary, runKind, customParams);
    }

    private void WriteOutBetaNugetSource(string key, string value)
    {
        var appDataFolder = Environment.GetEnvironmentVariable("APPDATA");
        var nugetConfigPath = Path.Combine(appDataFolder, @"NuGet\NuGet.Config");
        var document = XDocument.Load(nugetConfigPath);
        var packageSources = document.Root.Descendants().Where(x => x.Name.LocalName == "packageSources");
        var sources = packageSources.Elements(XName.Get("add"));
        if (!sources.Where(x => x.Attribute(XName.Get("value")).Value == value).Any())
        {
            var newSource = new XElement(XName.Get("add"));
            newSource.SetAttributeValue(XName.Get("key"), key);
            newSource.SetAttributeValue(XName.Get("value"), value);
            sources.Last().AddAfterSelf(newSource);
            document.Save(nugetConfigPath);
        }
    }
}
