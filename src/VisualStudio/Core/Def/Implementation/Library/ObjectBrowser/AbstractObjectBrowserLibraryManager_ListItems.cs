// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using VSLangProj80;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractObjectBrowserLibraryManager
    {
        internal void CollectMemberListItems(IAssemblySymbol assemblySymbol, Compilation compilation, Project project, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
            => GetListItemFactory().CollectMemberListItems(assemblySymbol, compilation, project, builder, searchString);

        internal void CollectNamespaceListItems(IAssemblySymbol assemblySymbol, Project project, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
            => GetListItemFactory().CollectNamespaceListItems(assemblySymbol, project, builder, searchString);

        internal void CollectTypeListItems(IAssemblySymbol assemblySymbol, Compilation compilation, Project project, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
            => GetListItemFactory().CollectTypeListItems(assemblySymbol, compilation, project, builder, searchString);

        internal ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> GetAssemblySet(Solution solution, string languageName, CancellationToken cancellationToken)
            => GetListItemFactory().GetAssemblySet(solution, languageName, cancellationToken);

        internal ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> GetAssemblySet(Project project, bool lookInReferences, CancellationToken cancellationToken)
            => GetListItemFactory().GetAssemblySet(project, lookInReferences, cancellationToken);

        internal ImmutableArray<ObjectListItem> GetBaseTypeListItems(ObjectListItem parentListItem, Compilation compilation, Project project)
            => GetListItemFactory().GetBaseTypeListItems(parentListItem, compilation, project);

        internal ImmutableArray<ObjectListItem> GetFolderListItems(ObjectListItem parentListItem, Compilation compilation)
            => GetListItemFactory().GetFolderListItems(parentListItem, compilation);

        internal ImmutableArray<ObjectListItem> GetMemberListItems(ObjectListItem parentListItem, Compilation compilation, Project project)
            => GetListItemFactory().GetMemberListItems(parentListItem, compilation, project);

        internal ImmutableArray<ObjectListItem> GetNamespaceListItems(ObjectListItem parentListItem, Compilation compilation, Project project)
            => GetListItemFactory().GetNamespaceListItems(parentListItem, compilation, project);

        internal ImmutableArray<ObjectListItem> GetProjectListItems(Solution solution, string languageName, uint listFlags, CancellationToken cancellationToken)
            => GetListItemFactory().GetProjectListItems(solution, languageName, listFlags, cancellationToken);

        internal ImmutableArray<ObjectListItem> GetReferenceListItems(ObjectListItem parentListItem, Compilation compilation)
            => GetListItemFactory().GetReferenceListItems(parentListItem, compilation);

        internal ImmutableArray<ObjectListItem> GetTypeListItems(ObjectListItem parentListItem, Compilation compilation, Project project)
            => GetListItemFactory().GetTypeListItems(parentListItem, compilation, project);
    }
}
