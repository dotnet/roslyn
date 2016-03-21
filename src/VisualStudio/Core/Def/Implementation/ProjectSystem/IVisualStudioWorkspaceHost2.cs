// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // TODO: Find a better name for this
    internal interface IVisualStudioWorkspaceHost2
    {
        void OnHasAllInformation(ProjectId projectId, bool hasAllInformation);
    }
}
