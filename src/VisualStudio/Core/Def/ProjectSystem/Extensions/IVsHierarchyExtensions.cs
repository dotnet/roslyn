// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal static class IVsHierarchyExtensions
{
    extension(IVsHierarchy hierarchy)
    {
        public bool TryGetItemProperty<T>(uint itemId, int propertyId, [MaybeNullWhen(false)] out T value)
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

        public bool TryGetProperty<T>(int propertyId, [MaybeNullWhen(false)] out T value)
        {
            const uint root = VSConstants.VSITEMID_ROOT;
            return hierarchy.TryGetItemProperty(root, propertyId, out value);
        }

        public bool TryGetProperty<T>(__VSHPROPID propertyId, [MaybeNullWhen(false)] out T value)
            => hierarchy.TryGetProperty((int)propertyId, out value);

        public bool TryGetItemProperty<T>(uint itemId, __VSHPROPID propertyId, [MaybeNullWhen(false)] out T value)
            => hierarchy.TryGetItemProperty(itemId, (int)propertyId, out value);

        public bool TryGetGuidProperty(int propertyId, out Guid guid)
            => ErrorHandler.Succeeded(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, propertyId, out guid));

        public bool TryGetGuidProperty(__VSHPROPID propertyId, out Guid guid)
            => ErrorHandler.Succeeded(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)propertyId, out guid));

        public bool TryGetProject([NotNullWhen(returnValue: true)] out EnvDTE.Project? project)
            => hierarchy.TryGetProperty<EnvDTE.Project>(__VSHPROPID.VSHPROPID_ExtObject, out project);

        public bool TryGetName([NotNullWhen(returnValue: true)] out string? name)
            => hierarchy.TryGetProperty<string>(__VSHPROPID.VSHPROPID_Name, out name);

        public bool TryGetItemName(uint itemId, [NotNullWhen(returnValue: true)] out string? name)
            => hierarchy.TryGetItemProperty<string>(itemId, __VSHPROPID.VSHPROPID_Name, out name);

        public bool TryGetCanonicalName(uint itemId, [NotNullWhen(returnValue: true)] out string? name)
            => ErrorHandler.Succeeded(hierarchy.GetCanonicalName(itemId, out name));

        public bool TryGetParentHierarchy([NotNullWhen(returnValue: true)] out IVsHierarchy? parentHierarchy)
            => hierarchy.TryGetProperty<IVsHierarchy>(__VSHPROPID.VSHPROPID_ParentHierarchy, out parentHierarchy);

        public bool TryGetTypeGuid(out Guid typeGuid)
            => hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_TypeGuid, out typeGuid);

        public bool TryGetTargetFrameworkMoniker(uint itemId, [NotNullWhen(returnValue: true)] out string? targetFrameworkMoniker)
            => hierarchy.TryGetItemProperty<string>(itemId, (int)__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, out targetFrameworkMoniker);

        public uint TryGetItemId(string moniker)
        {
            if (ErrorHandler.Succeeded(hierarchy.ParseCanonicalName(moniker, out var itemid)))
            {
                return itemid;
            }

            return VSConstants.VSITEMID_NIL;
        }

        public string? TryGetProjectFilePath()
        {
            if (ErrorHandler.Succeeded(((IVsProject3)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out var projectFilePath)) && !string.IsNullOrEmpty(projectFilePath))
            {
                return projectFilePath;
            }

            return null;
        }
    }
}
