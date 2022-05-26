// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    internal class ObjectBrowserLibraryManager : AbstractObjectBrowserLibraryManager
    {
        public ObjectBrowserLibraryManager(
            IServiceProvider serviceProvider,
            IComponentModel componentModel,
            VisualStudioWorkspace workspace)
            : base(LanguageNames.CSharp, Guids.CSharpLibraryId, serviceProvider, componentModel, workspace)
        {
        }

        internal override AbstractDescriptionBuilder CreateDescriptionBuilder(
            IVsObjectBrowserDescription3 description,
            ObjectListItem listItem,
            Project project)
        {
            return new DescriptionBuilder(description, this, listItem, project);
        }

        internal override AbstractListItemFactory CreateListItemFactory()
            => new ListItemFactory();
    }
}
