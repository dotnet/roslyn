// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractObjectBrowserLibraryManager
    {
        private static string GetSearchText(VSOBSEARCHCRITERIA2[] pobSrch)
        {
            if (pobSrch.Length == 0 ||
                pobSrch[0].szName == null)
            {
                return null;
            }

            var searchText = pobSrch[0].szName;

            var openParenIndex = searchText.IndexOf('(');
            if (openParenIndex != -1)
            {
                searchText = searchText[openParenIndex..];
            }

            return searchText;
        }

        public IVsSimpleObjectList2 GetSearchList(
            ObjectListKind listKind,
            uint flags,
            VSOBSEARCHCRITERIA2[] pobSrch,
            ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> projectAndAssemblySet)
        {
            var searchText = GetSearchText(pobSrch);
            if (searchText == null)
            {
                return null;
            }

            // TODO: Support wildcards (e.g. *xyz, *xyz* and xyz*) like the old language service did.

            switch (listKind)
            {
                case ObjectListKind.Namespaces:
                    {
                        var builder = ImmutableArray.CreateBuilder<ObjectListItem>();

                        foreach (var projectIdAndAssembly in projectAndAssemblySet)
                        {
                            var projectId = projectIdAndAssembly.Item1;
                            var assemblySymbol = projectIdAndAssembly.Item2;

                            CollectNamespaceListItems(assemblySymbol, projectId, builder, searchText);
                        }

                        return new ObjectList(ObjectListKind.Namespaces, flags, this, builder.ToImmutable());
                    }

                case ObjectListKind.Types:
                    {
                        var builder = ImmutableArray.CreateBuilder<ObjectListItem>();

                        foreach (var projectIdAndAssembly in projectAndAssemblySet)
                        {
                            var projectId = projectIdAndAssembly.Item1;
                            var assemblySymbol = projectIdAndAssembly.Item2;

                            var compilation = this.GetCompilation(projectId);
                            if (compilation == null)
                            {
                                return null;
                            }

                            CollectTypeListItems(assemblySymbol, compilation, projectId, builder, searchText);
                        }

                        return new ObjectList(ObjectListKind.Types, flags, this, builder.ToImmutable());
                    }

                case ObjectListKind.Members:
                    {
                        var builder = ImmutableArray.CreateBuilder<ObjectListItem>();

                        foreach (var projectIdAndAssembly in projectAndAssemblySet)
                        {
                            var projectId = projectIdAndAssembly.Item1;
                            var assemblySymbol = projectIdAndAssembly.Item2;

                            var compilation = this.GetCompilation(projectId);
                            if (compilation == null)
                            {
                                return null;
                            }

                            CollectMemberListItems(assemblySymbol, compilation, projectId, builder, searchText);
                        }

                        return new ObjectList(ObjectListKind.Types, flags, this, builder.ToImmutable());
                    }

                default:
                    return null;
            }
        }
    }
}
