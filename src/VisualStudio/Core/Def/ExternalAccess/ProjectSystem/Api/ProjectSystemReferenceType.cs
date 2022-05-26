﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api
{
    internal enum ProjectSystemReferenceType
    {
        /// <summary>
        /// Unknown reference type
        /// </summary>
        Unknown,

        /// <summary>
        /// Individual assembly reference `&lt;Reference ... /&gt;`
        /// </summary>
        Assembly,

        /// <summary>
        /// NuGet package reference `&lt;PackageReference ... /&gt;`
        /// </summary>
        Package,

        /// <summary>
        /// Project reference `&lt;ProjectReference ... /&gt;`
        /// </summary>
        Project,

        /// <summary>
        /// SDK reference
        /// </summary>
        SDK,
    }
}
