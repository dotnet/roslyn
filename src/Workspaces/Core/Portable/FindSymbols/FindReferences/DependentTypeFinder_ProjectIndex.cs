// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                new();

            public readonly MultiDictionary<Document, DeclaredSymbolInfo> ClassesAndRecordsThatMayDeriveFromSystemObject;
            public readonly MultiDictionary<Document, DeclaredSymbolInfo> ValueTypes;
            public readonly MultiDictionary<Document, DeclaredSymbolInfo> Enums;
            public readonly MultiDictionary<Document, DeclaredSymbolInfo> Delegates;
            public readonly MultiDictionary<string, (Document, DeclaredSymbolInfo)> NamedTypes;

            public ProjectIndex(MultiDictionary<Document, DeclaredSymbolInfo> classesAndRecordsThatMayDeriveFromSystemObject, MultiDictionary<Document, DeclaredSymbolInfo> valueTypes, MultiDictionary<Document, DeclaredSymbolInfo> enums, MultiDictionary<Document, DeclaredSymbolInfo> delegates, MultiDictionary<string, (Document, DeclaredSymbolInfo)> namedTypes)
            {
                ClassesAndRecordsThatMayDeriveFromSystemObject = classesAndRecordsThatMayDeriveFromSystemObject;
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
                    project.LanguageServices.GetRequiredService<ISyntaxFactsService>().StringComparer);

                foreach (var document in project.Documents)
                {
                    var syntaxTreeIndex = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                    foreach (var info in syntaxTreeIndex.DeclaredSymbolInfos)
                    {
                        switch (info.Kind)
                        {
                            case DeclaredSymbolInfoKind.Class:
                            case DeclaredSymbolInfoKind.Record:
                                classesThatMayDeriveFromSystemObject.Add(document, info);
                                break;
                            case DeclaredSymbolInfoKind.Enum:
                                enums.Add(document, info);
                                break;
                            case DeclaredSymbolInfoKind.Struct:
                            case DeclaredSymbolInfoKind.RecordStruct:
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
