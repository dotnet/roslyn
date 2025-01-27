// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

internal abstract partial class AbstractLegacyProject : IAnalyzerHost
{
    void IAnalyzerHost.AddAnalyzerReference(string analyzerAssemblyFullPath)
        => ProjectSystemProject.AddAnalyzerReference(analyzerAssemblyFullPath);

    void IAnalyzerHost.RemoveAnalyzerReference(string analyzerAssemblyFullPath)
        => ProjectSystemProject.RemoveAnalyzerReference(analyzerAssemblyFullPath);

    void IAnalyzerHost.SetRuleSetFile(string ruleSetFileFullPath)
    {
        // Sometimes the project system hands us paths with extra backslashes
        // and passing that to other parts of the shell causes issues
        // http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087250
        if (!string.IsNullOrEmpty(ruleSetFileFullPath))
        {
            ruleSetFileFullPath = Path.GetFullPath(ruleSetFileFullPath);
        }
        else
        {
            ruleSetFileFullPath = null;
        }

        ProjectSystemProjectOptionsProcessor.ExplicitRuleSetFilePath = ruleSetFileFullPath;
    }

    void IAnalyzerHost.AddAdditionalFile(string additionalFilePath)
        => ProjectSystemProject.AddAdditionalFile(additionalFilePath, folders: GetFolderNamesForDocument(additionalFilePath));

    void IAnalyzerHost.RemoveAdditionalFile(string additionalFilePath)
        => ProjectSystemProject.RemoveAdditionalFile(additionalFilePath);
}
