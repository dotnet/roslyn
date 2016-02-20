// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // TODO: Find a better name for this
    internal interface IVisualStudioWorkspaceHost2
    {
        void OnIsComplete(ProjectId projectId, bool succeeded);
    }
}
