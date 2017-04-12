// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal abstract partial class AbstractLegacyProject : AbstractProject, IAnalyzerHost
    {
        public void AddAdditionalFile(string additionalFilePath)
        {
            AddAdditionalFile(additionalFilePath, getIsInCurrentContext: document => LinkedFileUtilities.IsCurrentContextHierarchy(document, RunningDocumentTable));
        }
    }
}
