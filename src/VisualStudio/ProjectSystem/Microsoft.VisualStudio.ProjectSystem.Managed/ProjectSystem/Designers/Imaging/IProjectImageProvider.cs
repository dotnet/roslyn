// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    /// <summary>
    ///     Provides project images given a specific key.
    /// </summary>
    internal interface IProjectImageProvider
    {
        /// <summary>
        ///     Returns the <see cref="ProjectImageMoniker"/> for the specified key, returning <see langword="null"/>
        ///     if the provider does handle the specified key.
        /// </summary>
        ProjectImageMoniker GetProjectImage(string key);
    }
}
