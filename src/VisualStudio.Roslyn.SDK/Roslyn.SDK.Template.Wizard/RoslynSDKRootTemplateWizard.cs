// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKRootTemplateWizard
{
    public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

    private void OnRunStarted(DTE dte, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // add the root project name (the name the user passed in) to the global replacement dictionary
        GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];
        GlobalDictionary["$saferootidentifiername$"] = replacementsDictionary["$safeprojectname$"].Replace(".", "");
    }
}
