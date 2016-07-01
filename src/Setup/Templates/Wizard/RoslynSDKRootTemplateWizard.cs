using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKRootTemplateWizard
{
    public static Dictionary<string, string> GlobalDictionary = new Dictionary<string, string>();

    private void OnRunStarted(DTE dTE, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        // add the root project name (the name the user passed in) to the global replacement dictionary
        GlobalDictionary["$saferootprojectname$"] = replacementsDictionary["$safeprojectname$"];
        GlobalDictionary["$saferootidentifiername$"] = replacementsDictionary["$safeprojectname$"].Replace(".","");
    }
}
