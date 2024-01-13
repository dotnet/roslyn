// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractObjectBrowserLibraryManager
    {
        internal bool TryFillDescription(ObjectListItem listItem, IVsObjectBrowserDescription3 description, _VSOBJDESCOPTIONS options)
        {
            var project = GetProject(listItem);
            if (project == null)
            {
                return false;
            }

            return CreateDescriptionBuilder(description, listItem, project).TryBuild(options);
        }
    }
}
