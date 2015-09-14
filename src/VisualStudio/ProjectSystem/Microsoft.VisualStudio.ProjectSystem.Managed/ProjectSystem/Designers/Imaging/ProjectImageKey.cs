// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    /// <summary>
    ///     Provides common well-known <see cref="ProjectImageMoniker"/> keys.
    /// </summary>
    internal static class ProjectImageKey
    {
        /// <summary>
        ///     Represents the image key for the root of a project hierarchy.
        /// </summary>
        public const string ProjectRoot = nameof(ProjectRoot);

        /// <summary>
        ///     Represents the image key for the AppDesigner folder (called "Properties" in C# and "My Project" in VB).
        /// </summary>
        public const string AppDesignerFolder = nameof(AppDesignerFolder);
    }
}
