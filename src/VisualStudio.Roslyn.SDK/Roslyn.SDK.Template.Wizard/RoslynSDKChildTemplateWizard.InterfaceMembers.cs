// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKChildTemplateWizard : IWizard
{
    /// <summary>
    /// Only one wizard will be run by the project system.  
    /// This means our wizard needs to load and call the nuget wizard for every function.
    /// </summary>
    private IWizard NugetWizard => lazyWizard.Value;

    private Lazy<IWizard> lazyWizard = new Lazy<IWizard>(() =>
    {
        var asm = Assembly.Load("NuGet.VisualStudio.Interop, Version=1.0.0.0, Culture=Neutral, PublicKeyToken=b03f5f7f11d50a3a");
        return (IWizard)asm.CreateInstance("NuGet.VisualStudio.TemplateWizard");
    });

    public void BeforeOpeningFile(ProjectItem projectItem) => NugetWizard.BeforeOpeningFile(projectItem);
    public void ProjectFinishedGenerating(Project project)
    {
        NugetWizard.ProjectFinishedGenerating(project);
        OnProjectFinishedGenerating(project);
    }
    public void RunFinished() => NugetWizard.RunFinished();
    public bool ShouldAddProjectItem(string filePath) => NugetWizard.ShouldAddProjectItem(filePath);
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) => NugetWizard.ProjectItemFinishedGenerating(projectItem);
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        WriteOutBetaNugetSource("dotnet.myget.org roslyn", "https://dotnet.myget.org/F/roslyn/api/v3/index.json");
        NugetWizard.RunStarted(automationObject, replacementsDictionary, runKind, customParams);
        OnRunStarted(automationObject as DTE, replacementsDictionary, runKind, customParams);
    }

    private void WriteOutBetaNugetSource(string key, string value)
    {
        var appDataFolder = Environment.GetEnvironmentVariable("APPDATA");
        var nugetConfigPath = Path.Combine(appDataFolder, @"NuGet\NuGet.Config");
        var document = XDocument.Load(nugetConfigPath);
        var packageSources = document.Root.Descendants().Where(x => x.Name.LocalName == "packageSources");
        var sources = packageSources.Elements(XName.Get("add"));
        if (!sources.Where(x => x.Attribute(XName.Get("value")).Value == "https://dotnet.myget.org/F/roslyn/api/v3/index.json").Any())
        {
            var newSource = new XElement(XName.Get("add"));
            newSource.SetAttributeValue(XName.Get("key"), key);
            newSource.SetAttributeValue(XName.Get("value"), value);
            sources.Last().AddAfterSelf(newSource);
            document.Save(nugetConfigPath);
        }
    }
}
