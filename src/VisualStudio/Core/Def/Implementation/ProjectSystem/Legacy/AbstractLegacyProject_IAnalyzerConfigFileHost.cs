// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal abstract partial class AbstractLegacyProject : IAnalyzerConfigFileHost
    {
        void IAnalyzerConfigFileHost.AddAnalyzerConfigFile(string filePath)
        {
            VisualStudioProject.AddAnalyzerConfigFile(filePath);
        }

        void IAnalyzerConfigFileHost.RemoveAnalyzerConfigFile(string filePath)
        {
            VisualStudioProject.RemoveAnalyzerConfigFile(filePath);
        }
    }
}
