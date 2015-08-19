// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectTree
{
    internal static class ProjectTreeExtensions
    {
        public static bool IsProjectRoot(this IProjectTree tree)
        {
            return tree.HasCapability(ProjectTreeCapabilities.ProjectRoot);
        }

        public static bool HasCapability(this IProjectTree tree, string capability)
        {
            return tree.Capabilities.Contains(capability); 
        }
    }
}
