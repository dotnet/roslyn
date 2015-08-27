// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    ///     Provides extension methods for <see cref="IVsHierarchy"/> instances.
    /// </summary>
    internal static class VsHierarchyExtensions
    {
        /// <summary>
        ///     Returns the GUID of the specified property.
        /// </summary>
        public static Guid GetGuidProperty(this IVsHierarchy hierarchy, VsHierarchyPropID property)
        {
            Requires.NotNull(hierarchy, nameof(hierarchy));

            Guid result;
            HResult hr = hierarchy.GetGuidProperty(HierarchyId.Root, (int)property, out result);
            if (hr.Failed)
                throw hr.Exception;

            return result;
        }
    }
}
