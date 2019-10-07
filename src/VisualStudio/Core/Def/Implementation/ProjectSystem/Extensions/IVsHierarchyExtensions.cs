// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal static class IVsHierarchyExtensions
    {
        public static bool TryGetItemProperty<T>(this IVsHierarchy hierarchy, uint itemId, int propertyId, out T value)
        {
            if (ErrorHandler.Failed(hierarchy.GetProperty(itemId, propertyId, out var property)) ||
                !(property is T))
            {
                value = default;
                return false;
            }

            value = (T)property;

            return true;
        }

        public static bool TryGetProperty<T>(this IVsHierarchy hierarchy, int propertyId, out T value)
        {
            const uint root = VSConstants.VSITEMID_ROOT;
            return hierarchy.TryGetItemProperty(root, propertyId, out value);
        }

        public static bool TryGetProperty<T>(this IVsHierarchy hierarchy, __VSHPROPID propertyId, out T value)
        {
            return hierarchy.TryGetProperty((int)propertyId, out value);
        }

        public static bool TryGetItemProperty<T>(this IVsHierarchy hierarchy, uint itemId, __VSHPROPID propertyId, out T value)
        {
            return hierarchy.TryGetItemProperty(itemId, (int)propertyId, out value);
        }

        public static bool TryGetGuidProperty(this IVsHierarchy hierarchy, int propertyId, out Guid guid)
        {
            return ErrorHandler.Succeeded(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, propertyId, out guid));
        }

        public static bool TryGetGuidProperty(this IVsHierarchy hierarchy, __VSHPROPID propertyId, out Guid guid)
        {
            return ErrorHandler.Succeeded(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)propertyId, out guid));
        }

        public static bool TryGetProject(this IVsHierarchy hierarchy, out EnvDTE.Project project)
        {
            return hierarchy.TryGetProperty(__VSHPROPID.VSHPROPID_ExtObject, out project);
        }

        public static bool TryGetName(this IVsHierarchy hierarchy, out string name)
        {
            return hierarchy.TryGetProperty(__VSHPROPID.VSHPROPID_Name, out name);
        }

        public static bool TryGetItemName(this IVsHierarchy hierarchy, uint itemId, out string name)
        {
            return hierarchy.TryGetItemProperty(itemId, __VSHPROPID.VSHPROPID_Name, out name);
        }

        public static bool TryGetCanonicalName(this IVsHierarchy hierarchy, uint itemId, out string name)
        {
            return ErrorHandler.Succeeded(hierarchy.GetCanonicalName(itemId, out name));
        }

        public static bool TryGetParentHierarchy(this IVsHierarchy hierarchy, out IVsHierarchy parentHierarchy)
        {
            return hierarchy.TryGetProperty(__VSHPROPID.VSHPROPID_ParentHierarchy, out parentHierarchy);
        }

        public static bool TryGetTypeGuid(this IVsHierarchy hierarchy, out Guid typeGuid)
        {
            return hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_TypeGuid, out typeGuid);
        }

        public static bool TryGetTargetFrameworkMoniker(this IVsHierarchy hierarchy, uint itemId, out string targetFrameworkMoniker)
        {
            return hierarchy.TryGetItemProperty(itemId, (int)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, out targetFrameworkMoniker);
        }

        public static uint TryGetItemId(this IVsHierarchy hierarchy, string moniker)
        {
            if (ErrorHandler.Succeeded(hierarchy.ParseCanonicalName(moniker, out var itemid)))
            {
                return itemid;
            }

            return VSConstants.VSITEMID_NIL;
        }

        public static string TryGetProjectFilePath(this IVsHierarchy hierarchy)
        {
            if (ErrorHandler.Succeeded(((IVsProject3)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out var projectFilePath)) && !string.IsNullOrEmpty(projectFilePath))
            {
                return projectFilePath;
            }

            return null;
        }
    }
}
