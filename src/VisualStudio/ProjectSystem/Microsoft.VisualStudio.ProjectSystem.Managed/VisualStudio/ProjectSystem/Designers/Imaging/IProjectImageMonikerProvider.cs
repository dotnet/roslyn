// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    /// <summary>
    ///     Provides project images given a specific key.
    /// </summary>
    internal interface IProjectImageMonikerProvider
    {
        /// <summary>
        ///     Returns the <see cref="ProjectImageMoniker"/> for the specified key, returning <see langword="false"/>
        ///     if the provider does handle the specified key.
        /// </summary>
        bool TryGetProjectImageMoniker(string key, out ProjectImageMoniker result);
    }
}
