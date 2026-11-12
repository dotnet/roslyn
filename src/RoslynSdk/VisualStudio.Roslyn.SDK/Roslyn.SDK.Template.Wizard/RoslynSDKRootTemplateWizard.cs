// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKRootTemplateWizard
{
    public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

    private void OnRunStarted(Dictionary<string, string> replacementsDictionary)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // add the root project name (the name the user passed in) to the global replacement dictionary
        GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];
        GlobalDictionary["$saferootidentifiername$"] = replacementsDictionary["$safeprojectname$"].Replace(".", "");
    }
}
