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

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it.
        /// </summary>
        public static T GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T defaultValue)
        {
            return GetProperty(hierarchy, HierarchyId.Root, property, defaultValue);
        }

        /// <summary>
        ///     Gets the value of the specified property if the hierarchy supports it.
        /// </summary>
        public static T GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T defaultValue)
        {
            Requires.NotNull(hierarchy, nameof(hierarchy));

            if (item.IsNilOrEmpty || item.IsSelection)
                throw new ArgumentException(null, nameof(item));

            object resultObject;
            HResult hr = hierarchy.GetProperty(item, (int)property, out resultObject);
            if (hr == VSConstants.DISP_E_MEMBERNOTFOUND)
                return defaultValue;

            if (hr.Failed)
                throw hr.Exception;

            // NOTE: We consider it a bug in the underlying project system or the caller if this cast fails
            return (T)resultObject;
        }
    }
}
