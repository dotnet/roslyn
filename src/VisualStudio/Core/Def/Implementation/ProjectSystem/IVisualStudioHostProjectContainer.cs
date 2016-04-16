// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// An interface implemented by a workspace to get the set of host projects contained in the
    /// workspace.
    /// </summary>
    internal interface IVisualStudioHostProjectContainer
    {
        IEnumerable<IVisualStudioHostProject> GetProjects();

        void NotifyNonDocumentOpenedForProject(IVisualStudioHostProject project);
    }
}
