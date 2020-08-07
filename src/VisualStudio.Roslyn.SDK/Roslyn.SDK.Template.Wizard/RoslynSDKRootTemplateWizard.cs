// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKRootTemplateWizard
{
    private const string NuGetConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""roslyn"" value=""https://dotnet.myget.org/F/roslyn/api/v3/index.json"" />
    <add key=""roslyn-analyzers"" value=""https://dotnet.myget.org/F/roslyn-analyzers/api/v3/index.json"" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
</configuration>
";

    public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

    private void OnRunStarted(DTE dte, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // add the root project name (the name the user passed in) to the global replacement dictionary
        GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];
        GlobalDictionary["$saferootidentifiername$"] = replacementsDictionary["$safeprojectname$"].Replace(".","");

        if (!replacementsDictionary.TryGetValue("$solutiondirectory$", out var solutionFolder))
        {
            var solutionFile = dte.Solution.FullName;
            if (string.IsNullOrEmpty(solutionFile))
                return;

            solutionFolder = Path.GetDirectoryName(solutionFile);
        }

        if (Directory.Exists(solutionFolder))
        {
            File.WriteAllText(
                Path.Combine(solutionFolder, "NuGet.Config"),
                NuGetConfig,
                Encoding.UTF8);
        }
    }
}
