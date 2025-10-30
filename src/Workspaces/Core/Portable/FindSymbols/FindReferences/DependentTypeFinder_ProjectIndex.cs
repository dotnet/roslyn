// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
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
        /// <summary>
        /// We cache the project instance per <see cref="ProjectState"/>.  This allows us to reuse it over a wide set of
        /// changes (for example, changing completely unrelated projects that a particular project doesn't depend on).
        /// However, <see cref="ProjectState"/> doesn't change even when certain things change that will create a
        /// substantively different <see cref="Project"/>.  For example, if the <see
        /// cref="SourceGeneratorExecutionVersion"/> for the project changes, we'll still have the same project state.
        /// As such, we store the <see cref="Checksum"/> of the project as well, ensuring that if anything in it or its
        /// dependencies changes, we recompute the index.
        /// </summary>
        private static readonly ConditionalWeakTable<ProjectState, StrongBox<(Checksum checksum, AsyncLazy<ProjectIndex> lazyProjectIndex)>> s_projectToIndex = new();

        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> ClassesAndRecordsThatMayDeriveFromSystemObject = classesAndRecordsThatMayDeriveFromSystemObject;
        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> ValueTypes = valueTypes;
        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> Enums = enums;
        public readonly MultiDictionary<DocumentId, DeclaredSymbolInfo> Delegates = delegates;
        public readonly MultiDictionary<string, (DocumentId, DeclaredSymbolInfo)> NamedTypes = namedTypes;

        public static async Task<ProjectIndex> GetIndexAsync(
            Project project, CancellationToken cancellationToken)
        {
            // Use the checksum of the project.  That way if its state *or* SG info changes, we compute a new index with
            // accurate information in it.
            var checksum = await project.GetDiagnosticChecksumAsync(cancellationToken).ConfigureAwait(false);
            if (!s_projectToIndex.TryGetValue(project.State, out var tuple) ||
                tuple.Value.checksum != checksum)
            {
                tuple = new((checksum, AsyncLazy.Create(CreateIndexAsync, project)));

#if NET
                s_projectToIndex.AddOrUpdate(project.State, tuple);
#else
                // Best effort try to update the map with the new data. 
                s_projectToIndex.Remove(project.State);
                // Note: intentionally ignore the return value here.  We want to use the value we've computed even if
                // another thread beats us to adding things here. 
                _ = s_projectToIndex.GetValue(project.State, _ => tuple);
#endif
            }

            return await tuple.Value.lazyProjectIndex.GetValueAsync(cancellationToken).ConfigureAwait(false);
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
