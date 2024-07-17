// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal static partial class DependentTypeFinder
{
    private sealed class ProjectIndex(
        MultiDictionary<DocumentId, DeclaredSymbolInfo> classesAndRecordsThatMayDeriveFromSystemObject,
        MultiDictionary<DocumentId, DeclaredSymbolInfo> valueTypes,
        MultiDictionary<DocumentId, DeclaredSymbolInfo> enums,
        MultiDictionary<DocumentId, DeclaredSymbolInfo> delegates,
        MultiDictionary<string, (DocumentId, DeclaredSymbolInfo)> namedTypes)
    {
        private static readonly ConditionalWeakTable<ProjectState, AsyncLazy<ProjectIndex>> s_projectToIndex = new();

        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> ClassesAndRecordsThatMayDeriveFromSystemObject = classesAndRecordsThatMayDeriveFromSystemObject;
        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> ValueTypes = valueTypes;
        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> Enums = enums;
        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> Delegates = delegates;
        public readonly MultiDictionary<string, (DocumentId, DeclaredSymbolInfo)> NamedTypes = namedTypes;

        public static Task<ProjectIndex> GetIndexAsync(
            Project project, CancellationToken cancellationToken)
        {
            if (!s_projectToIndex.TryGetValue(project.State, out var lazyIndex))
            {
                lazyIndex = s_projectToIndex.GetValue(
                    project.State, p => AsyncLazy.Create(
                        asynchronousComputeFunction: static (project, c) => CreateIndexAsync(project, c),
                        arg: project));
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

            var solutionKey = SolutionKey.ToSolutionKey(project.Solution);

            var regularDocumentStates = project.State.DocumentStates;
            var sourceGeneratorDocumentStates = await project.Solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);

            var allStates =
                regularDocumentStates.States.Select(kvp => (kvp.Key, kvp.Value)).Concat(
                sourceGeneratorDocumentStates.States.Select(kvp => (kvp.Key, (DocumentState)kvp.Value)));

            // Avoid realizing actual Document instances here.  We don't need them, and it can allocate a lot of
            // memory as we do background indexing.
            foreach (var (documentId, document) in allStates)
            {
                var syntaxTreeIndex = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(
                    solutionKey, project.State, document, cancellationToken).ConfigureAwait(false);
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
