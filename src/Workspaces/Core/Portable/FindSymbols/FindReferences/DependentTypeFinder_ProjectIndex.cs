// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        private sealed class ProjectIndex
        {
            private static readonly ConditionalWeakTable<ProjectState, AsyncLazy<ProjectIndex>> s_projectToIndex = new();

            public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> ClassesAndRecordsThatMayDeriveFromSystemObject;
            public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> ValueTypes;
            public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> Enums;
            public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> Delegates;
            public readonly MultiDictionary<string, (DocumentId, DeclaredSymbolInfo)> NamedTypes;

            public ProjectIndex(
                MultiDictionary<DocumentId, DeclaredSymbolInfo> classesAndRecordsThatMayDeriveFromSystemObject,
                MultiDictionary<DocumentId, DeclaredSymbolInfo> valueTypes,
                MultiDictionary<DocumentId, DeclaredSymbolInfo> enums,
                MultiDictionary<DocumentId, DeclaredSymbolInfo> delegates,
                MultiDictionary<string, (DocumentId, DeclaredSymbolInfo)> namedTypes)
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
                if (!s_projectToIndex.TryGetValue(project.State, out var lazyIndex))
                {
                    lazyIndex = s_projectToIndex.GetValue(
                        project.State, p => new AsyncLazy<ProjectIndex>(
                            c => CreateIndexAsync(project, c)));
                }

                return lazyIndex.GetValueAsync(cancellationToken);
            }

            private static async Task<ProjectIndex> CreateIndexAsync(Project project, CancellationToken cancellationToken)
            {
                var classesThatMayDeriveFromSystemObject = new MultiDictionary<DocumentId, DeclaredSymbolInfo>();
                var valueTypes = new MultiDictionary<DocumentId, DeclaredSymbolInfo>();
                var enums = new MultiDictionary<DocumentId, DeclaredSymbolInfo>();
                var delegates = new MultiDictionary<DocumentId, DeclaredSymbolInfo>();

                var namedTypes = new MultiDictionary<string, (DocumentId, DeclaredSymbolInfo)>(
                    project.Services.GetRequiredService<ISyntaxFactsService>().StringComparer);

                // Avoid realizing actual Document instances here.  We don't need them, and it can allocate a lot of
                // memory as we do background indexing.
                foreach (var (documentId, document) in project.State.DocumentStates.States)
                {
                    var syntaxTreeIndex = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(
                        SolutionKey.ToSolutionKey(project.Solution), project.State, document, cancellationToken).ConfigureAwait(false);
                    foreach (var info in syntaxTreeIndex.DeclaredSymbolInfos)
                    {
                        switch (info.Kind)
                        {
                            case DeclaredSymbolInfoKind.Class:
                            case DeclaredSymbolInfoKind.Record:
                                classesThatMayDeriveFromSystemObject.Add(documentId, info);
                                break;
                            case DeclaredSymbolInfoKind.Enum:
                                enums.Add(documentId, info);
                                break;
                            case DeclaredSymbolInfoKind.Struct:
                            case DeclaredSymbolInfoKind.RecordStruct:
                                valueTypes.Add(documentId, info);
                                break;
                            case DeclaredSymbolInfoKind.Delegate:
                                delegates.Add(documentId, info);
                                break;
                        }

                        foreach (var inheritanceName in info.InheritanceNames)
                            namedTypes.Add(inheritanceName, (documentId, info));
                    }
                }

                return new ProjectIndex(classesThatMayDeriveFromSystemObject, valueTypes, enums, delegates, namedTypes);
            }
        }
    }
}
