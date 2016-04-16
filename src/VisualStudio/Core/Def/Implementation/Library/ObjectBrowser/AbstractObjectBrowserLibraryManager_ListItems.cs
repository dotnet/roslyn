// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractObjectBrowserLibraryManager
    {
        internal void CollectMemberListItems(IAssemblySymbol assemblySymbol, Compilation compilation, ProjectId projectId, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
        {
            GetListItemFactory().CollectMemberListItems(assemblySymbol, compilation, projectId, builder, searchString);
        }

        internal void CollectNamespaceListItems(IAssemblySymbol assemblySymbol, ProjectId projectId, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
        {
            GetListItemFactory().CollectNamespaceListItems(assemblySymbol, projectId, builder, searchString);
        }

        internal void CollectTypeListItems(IAssemblySymbol assemblySymbol, Compilation compilation, ProjectId projectId, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
        {
            GetListItemFactory().CollectTypeListItems(assemblySymbol, compilation, projectId, builder, searchString);
        }

        internal ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> GetAssemblySet(Solution solution, string languageName, CancellationToken cancellationToken)
        {
            return GetListItemFactory().GetAssemblySet(solution, languageName, cancellationToken);
        }

        internal ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> GetAssemblySet(Project project, bool lookInReferences, CancellationToken cancellationToken)
        {
            return GetListItemFactory().GetAssemblySet(project, lookInReferences, cancellationToken);
        }

        internal ImmutableArray<ObjectListItem> GetBaseTypeListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            return GetListItemFactory().GetBaseTypeListItems(parentListItem, compilation);
        }

        internal ImmutableArray<ObjectListItem> GetFolderListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            return GetListItemFactory().GetFolderListItems(parentListItem, compilation);
        }

        internal ImmutableArray<ObjectListItem> GetMemberListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            return GetListItemFactory().GetMemberListItems(parentListItem, compilation);
        }

        internal ImmutableArray<ObjectListItem> GetNamespaceListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            return GetListItemFactory().GetNamespaceListItems(parentListItem, compilation);
        }

        internal ImmutableArray<ObjectListItem> GetProjectListItems(Solution solution, string languageName, uint listFlags, CancellationToken cancellationToken)
        {
            return GetListItemFactory().GetProjectListItems(solution, languageName, listFlags, cancellationToken);
        }

        internal ImmutableArray<ObjectListItem> GetReferenceListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            return GetListItemFactory().GetReferenceListItems(parentListItem, compilation);
        }

        internal ImmutableArray<ObjectListItem> GetTypeListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            return GetListItemFactory().GetTypeListItems(parentListItem, compilation);
        }
    }
}
