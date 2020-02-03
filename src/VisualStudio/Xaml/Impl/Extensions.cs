// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
