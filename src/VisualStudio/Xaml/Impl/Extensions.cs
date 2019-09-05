// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.Xaml
{
    internal static class Extensions
    {
        public static Guid GetProjectGuid(this VisualStudioWorkspace workspace, ProjectId projectId)
            => workspace.GetProjectGuid(projectId);
    }
}
