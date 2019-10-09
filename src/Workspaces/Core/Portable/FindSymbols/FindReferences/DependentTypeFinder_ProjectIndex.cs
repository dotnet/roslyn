// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        private class ProjectIndex
        {
            private static readonly ConditionalWeakTable<Project, AsyncLazy<ProjectIndex>> s_projectToIndex =
                new ConditionalWeakTable<Project, AsyncLazy<ProjectIndex>>();

            public readonly MultiDictionary<Document, DeclaredSymbolInfo> ClassesThatMayDeriveFromSystemObject;
            public readonly MultiDictionary<Document, DeclaredSymbolInfo> ValueTypes;
            public readonly MultiDictionary<Document, DeclaredSymbolInfo> Enums;
            public readonly MultiDictionary<Document, DeclaredSymbolInfo> Delegates;
            public readonly MultiDictionary<string, (Document, DeclaredSymbolInfo)> NamedTypes;

            public ProjectIndex(MultiDictionary<Document, DeclaredSymbolInfo> classesThatMayDeriveFromSystemObject, MultiDictionary<Document, DeclaredSymbolInfo> valueTypes, MultiDictionary<Document, DeclaredSymbolInfo> enums, MultiDictionary<Document, DeclaredSymbolInfo> delegates, MultiDictionary<string, (Document, DeclaredSymbolInfo)> namedTypes)
            {
                ClassesThatMayDeriveFromSystemObject = classesThatMayDeriveFromSystemObject;
                ValueTypes = valueTypes;
                Enums = enums;
                Delegates = delegates;
                NamedTypes = namedTypes;
            }

            public static Task<ProjectIndex> GetIndexAsync(
                Project project, CancellationToken cancellationToken)
            {
                if (!s_projectToIndex.TryGetValue(project, out var lazyIndex))
                {
                    lazyIndex = s_projectToIndex.GetValue(
                        project, p => new AsyncLazy<ProjectIndex>(
                            c => ProjectIndex.CreateIndexAsync(p, c), cacheResult: true));
                }

                return lazyIndex.GetValueAsync(cancellationToken);
            }

            private static async Task<ProjectIndex> CreateIndexAsync(Project project, CancellationToken cancellationToken)
            {
                var classesThatMayDeriveFromSystemObject = new MultiDictionary<Document, DeclaredSymbolInfo>();
                var valueTypes = new MultiDictionary<Document, DeclaredSymbolInfo>();
                var enums = new MultiDictionary<Document, DeclaredSymbolInfo>();
                var delegates = new MultiDictionary<Document, DeclaredSymbolInfo>();

                var namedTypes = new MultiDictionary<string, (Document, DeclaredSymbolInfo)>(
                    project.LanguageServices.GetService<ISyntaxFactsService>().StringComparer);

                foreach (var document in project.Documents)
                {
                    var syntaxTreeIndex = await document.GetSyntaxTreeIndexAsync(loadOnly: false, cancellationToken).ConfigureAwait(false);
                    foreach (var info in syntaxTreeIndex.DeclaredSymbolInfos)
                    {
                        switch (info.Kind)
                        {
                            case DeclaredSymbolInfoKind.Class:
                                classesThatMayDeriveFromSystemObject.Add(document, info);
                                break;
                            case DeclaredSymbolInfoKind.Enum:
                                enums.Add(document, info);
                                break;
                            case DeclaredSymbolInfoKind.Struct:
                                valueTypes.Add(document, info);
                                break;
                            case DeclaredSymbolInfoKind.Delegate:
                                delegates.Add(document, info);
                                break;
                        }

                        foreach (var inheritanceName in info.InheritanceNames)
                        {
                            namedTypes.Add(inheritanceName, (document, info));
                        }
                    }
                }

                return new ProjectIndex(classesThatMayDeriveFromSystemObject, valueTypes, enums, delegates, namedTypes);
            }
        }
    }
}
