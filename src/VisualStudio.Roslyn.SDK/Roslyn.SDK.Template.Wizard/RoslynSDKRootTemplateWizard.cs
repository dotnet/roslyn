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
    <add key=""dotnet-public"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"" />
    <add key=""dotnet-tools"" value=""https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"" />
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
                Path.Combine(solutionFolder, "NuGet.config"),
                NuGetConfig,
                Encoding.UTF8);
        }
    }
}
