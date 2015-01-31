// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
