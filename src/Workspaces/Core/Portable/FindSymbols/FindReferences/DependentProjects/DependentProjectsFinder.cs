// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols.DependentProjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DependentProjectMap = ConcurrentDictionary<DefinitionProject, AsyncLazy<ImmutableArray<DependentProject>>>;

    /// <summary>
    /// Provides helper methods for finding dependent projects across a solution that a given symbol can be referenced within.
    /// </summary>
    internal static partial class DependentProjectsFinder
    {
        /// <summary>
        /// Dependent projects cache.
        /// For a given solution, maps from an assembly (source/metadata) to the set of projects referencing it.
        ///     Key: DefinitionProject, which contains the assembly name and a flag indicating whether assembly is source or metadata assembly.
        ///     Value: List of DependentProjects, where each DependentProject contains a dependent project ID and a flag indicating whether the dependent project has internals access to definition project.
        /// </summary>
        private static readonly ConditionalWeakTable<Solution, DependentProjectMap> s_dependentProjectsCache =
            new ConditionalWeakTable<Solution, DependentProjectMap>();

        /// <summary>
        /// Used to create a new concurrent dependent projects map for a given assembly when needed.
        /// </summary>
        private static readonly ConditionalWeakTable<Solution, DependentProjectMap>.CreateValueCallback s_createDependentProjectsMapCallback =
            _ => new DependentProjectMap();

        /// <summary>
        /// Returns the set of <see cref="Project"/>s in <paramref name="solution"/> that <paramref name="symbol"/>
        /// could be referenced in.  Note that results are not precise.  Specifically, if a project cannot reference
        /// <paramref name="symbol"/> it will not be included. However, some projects which cannot reference <paramref
        /// name="symbol"/> may be included.  In practice though, this should be extremely unlikely.
        /// </summary>
        public static async Task<ImmutableArray<Project>> GetDependentProjectsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project>? projects, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Namespace)
            {
                // namespaces are visible in all projects.
                if (projects != null)
                {
                    return projects.ToImmutableArray();
                }

                return solution.Projects.ToImmutableArray();
            }
            else
            {
                var dependentProjects = await GetDependentProjectsWorkerAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                if (projects != null)
                {
                    return dependentProjects.WhereAsArray(projects.Contains);
                }

                return dependentProjects;
            }
        }

        /// <summary>
        /// This method computes the dependent projects that need to be searched for references of the given <paramref name="symbol"/>.
        /// This computation depends on the given symbol's visibility:
        ///     1) Public: Dependent projects include the symbol definition project and all the referencing projects.
        ///     2) Internal: Dependent projects include the symbol definition project and all the referencing projects that have internals access to the definition project.
        ///     3) Private: Dependent projects include the symbol definition project and all the referencing submission projects (which are special and can reference private fields of the previous submission).
        /// 
        /// We perform this computation in two stages:
        ///     1) Compute all the dependent projects (submission + non-submission) and their InternalsVisibleTo semantics to the definition project.
        ///     2) Filter the above computed dependent projects based on symbol visibility.
        /// Dependent projects computed in stage (1) are cached to avoid recomputation.
        /// </summary>
        private static async Task<ImmutableArray<Project>> GetDependentProjectsWorkerAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Find the assembly that this symbol comes from.  (Could be a metadata or source
            // assembly).
            symbol = symbol.OriginalDefinition;
            var containingAssembly = symbol.ContainingAssembly;
            if (containingAssembly == null)
            {
                // currently we don't support finding references for a symbol that doesn't have containing assembly symbol
                return ImmutableArray<Project>.Empty;
            }

            // Find the projects that reference this assembly.

            // If this is a source symbol from a project, try to find that project.
            var sourceProject = solution.GetProject(containingAssembly, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 1) Compute all the dependent projects (submission + non-submission) and their InternalsVisibleTo semantics to the definition project.
            ImmutableArray<DependentProject> dependentProjects;

            var visibility = symbol.GetResultantVisibility();
            if (visibility == SymbolVisibility.Private)
            {
                dependentProjects = await GetDependentProjectsCoreAsync(symbol, solution, sourceProject, visibility, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // We cache the dependent projects for non-private symbols to speed up future calls.
                var dependentProjectsMap = s_dependentProjectsCache.GetValue(solution, s_createDependentProjectsMapCallback);

                var asyncLazy = dependentProjectsMap.GetOrAdd(
                    new DefinitionProject(sourceProjectId: sourceProject?.Id, assemblyName: containingAssembly.Name.ToLower()),
                    _ => new AsyncLazy<ImmutableArray<DependentProject>>(c => GetDependentProjectsCoreAsync(symbol, solution, sourceProject, visibility, c), cacheResult: true));
                dependentProjects = await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }

            // 2) Filter the above computed dependent projects based on symbol visibility.
            return FilterDependentProjectsByVisibility(solution, dependentProjects, visibility);
        }

        private static async Task<ImmutableArray<DependentProject>> GetDependentProjectsCoreAsync(
            ISymbol symbol,
            Solution solution,
            Project? sourceProject,
            SymbolVisibility visibility,
            CancellationToken cancellationToken)
        {
            var dependentProjects = new HashSet<DependentProject>();

            // If a symbol was defined in source, then it is always visible to the project it
            // was defined in.
            if (sourceProject != null)
            {
                dependentProjects.Add(new DependentProject(sourceProject.Id, hasInternalsAccess: true));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // If it's not private, then we need to find possible references.
            if (visibility != SymbolVisibility.Private)
            {
                await AddNonSubmissionDependentProjectsAsync(symbol.ContainingAssembly, solution, sourceProject, dependentProjects, cancellationToken).ConfigureAwait(false);
            }

            // submission projects are special here. The fields generated inside the Script object
            // is private, but further submissions can bind to them.
            await AddSubmissionDependentProjectsAsync(solution, sourceProject, dependentProjects, cancellationToken).ConfigureAwait(false);

            return dependentProjects.ToImmutableArray();
        }

        private static ImmutableArray<Project> FilterDependentProjectsByVisibility(
            Solution solution,
            ImmutableArray<DependentProject> dependentProjects,
            SymbolVisibility visibility)
        {
            // Filter out dependent projects based on symbol visibility.
            switch (visibility)
            {
                case SymbolVisibility.Internal:
                    // Retain dependent projects that have internals access.
                    dependentProjects = dependentProjects.WhereAsArray(dp => dp.HasInternalsAccess);
                    break;
            }

            var projectIds = dependentProjects.SelectAsArray(dp => dp.ProjectId);
            return projectIds.SelectAsArray(id => solution.GetRequiredProject(id));
        }

        private static async Task AddSubmissionDependentProjectsAsync(
            Solution solution, Project? sourceProject, HashSet<DependentProject> dependentProjects, CancellationToken cancellationToken)
        {
            var isSubmission = sourceProject != null && sourceProject.IsSubmission;
            if (!isSubmission)
            {
                return;
            }

            var projectIdsToReferencingSubmissionIds = new Dictionary<ProjectId, List<ProjectId>>();

            // search only submission project
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetRequiredProject(projectId);
                if (project.IsSubmission && project.SupportsCompilation)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If we are referencing another project, store the link in the other direction
                    // so we walk across it later
                    var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;

                    if (previous != null)
                    {
                        var referencedProject = solution.GetProject(previous.Assembly, cancellationToken);
                        if (referencedProject != null)
                        {
                            if (!projectIdsToReferencingSubmissionIds.TryGetValue(referencedProject.Id, out var referencingSubmissions))
                            {
                                referencingSubmissions = new List<ProjectId>();
                                projectIdsToReferencingSubmissionIds.Add(referencedProject.Id, referencingSubmissions);
                            }

                            referencingSubmissions.Add(project.Id);
                        }
                    }
                }
            }

            // Submission compilations are special. If we have submissions 0, 1 and 2 chained in
            // the natural way, and we have a symbol in submission 0, we need to search both 1
            // and 2, even though 2 doesn't have a direct reference to 1. Hence we need to take
            // our current set of projects and find the transitive closure over backwards
            // submission previous references.
            var projectIdsToProcess = new Stack<ProjectId>(dependentProjects.Select(dp => dp.ProjectId));

            while (projectIdsToProcess.Count > 0)
            {
                var toProcess = projectIdsToProcess.Pop();

                if (projectIdsToReferencingSubmissionIds.TryGetValue(toProcess, out var submissionIds))
                {
                    foreach (var pId in submissionIds)
                    {
                        if (!dependentProjects.Any(dp => dp.ProjectId == pId))
                        {
                            dependentProjects.Add(new DependentProject(pId, hasInternalsAccess: true));
                            projectIdsToProcess.Push(pId);
                        }
                    }
                }
            }
        }

        private static bool IsInternalsVisibleToAttribute(AttributeData attr)
        {
            var attrType = attr.AttributeClass;
            return attrType?.Name == nameof(InternalsVisibleToAttribute) &&
                   attrType.ContainingNamespace?.Name == nameof(System.Runtime.CompilerServices) &&
                   attrType.ContainingNamespace.ContainingNamespace?.Name == nameof(System.Runtime) &&
                   attrType.ContainingNamespace.ContainingNamespace.ContainingNamespace?.Name == nameof(System) &&
                   attrType.ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
        }

        private static async Task AddNonSubmissionDependentProjectsAsync(
            IAssemblySymbol containingAssembly,
            Solution solution,
            Project? sourceProject,
            HashSet<DependentProject> dependentProjects,
            CancellationToken cancellationToken)
        {
            var isSubmission = sourceProject != null && sourceProject.IsSubmission;
            if (isSubmission)
                return;

            // HashSet<string>? internalsVisibleToSet = null;// fastCheckInternalsVisibleToSourceAsync = GetFastCheckInternalsVisibleToSource(sourceProject);
            var fastCheckInternalsVisibleToCompilation = GetFastCheckInternalsVisibleToSource(sourceProject, cancellationToken);
            var containingAssemblySymbolKey = containingAssembly.GetSymbolKey(cancellationToken);

            var tasks = solution.Projects.Select(p => Task.Run(() => ComputeDependentProjectAsync(
                containingAssembly,
                containingAssemblySymbolKey,
                sourceProject,
                p,
                fastCheckInternalsVisibleToCompilation,
                cancellationToken))).ToArray();

            var dependentProjectResults = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var (isDependent, projectId, internalsVisibleTo) in dependentProjectResults)
            {
                if (isDependent)
                    dependentProjects.Add(new DependentProject(projectId, internalsVisibleTo));
            }
        }

        private static async Task<(bool isDependent, ProjectId projectId, bool internalsVisibleTo)> ComputeDependentProjectAsync(
            IAssemblySymbol containingAssembly,
            SymbolKey containingAssemblySymbolKey,
            Project? sourceProject,
            Project project,
            Func<string, Task<bool>> fastCheckInternalsVisibleToCompilation,
            CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation ||
                !HasReferenceTo(containingAssembly, sourceProject, project, cancellationToken))
            {
                return default;
            }

            var internalsVisibleTo = sourceProject != null
                ? await fastCheckInternalsVisibleToCompilation(project.AssemblyName).ConfigureAwait(false)
                : await MetadataAssemblyHasInternalsAccessAsync(
                    containingAssembly, containingAssemblySymbolKey, project, cancellationToken).ConfigureAwait(false);

            return (isDependent: true, project.Id, internalsVisibleTo);
        }

        /// <summary>
        /// Performs the full, expensive, compilation check if <paramref name="project"/> can see internal symbols from <paramref name="containingAssembly"/>.
        /// </summary>
        private static async Task<bool> MetadataAssemblyHasInternalsAccessAsync(
            IAssemblySymbol containingAssembly,
            SymbolKey containingAssemblySymbolKey,
            Project project,
            CancellationToken cancellationToken)
        {
            Debug.Assert(project.SupportsCompilation);

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var targetAssembly = compilation.Assembly;

            if (containingAssembly.Language != targetAssembly.Language)
            {
                var resolvedSymbol = containingAssemblySymbolKey.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
                if (resolvedSymbol is IAssemblySymbol sourceAssemblyInTargetCompilation)
                {
                    return targetAssembly.IsSameAssemblyOrHasFriendAccessTo(sourceAssemblyInTargetCompilation);
                }
            }
            else
            {
                return targetAssembly.IsSameAssemblyOrHasFriendAccessTo(containingAssembly);
            }

            return false;
        }

        /// <summary>
        /// This method creates an initial cheapish InternalsVisibleTo map from the given <paramref
        /// name="sourceProject"/> to the assembly names that have friend access to this assembly.  It uses the
        /// information stored in our syntactic indices to avoid having to check with the compilation to get the 
        /// assembly attributes in the project.
        /// </summary>
        private static Func<string, Task<bool>> GetFastCheckInternalsVisibleToSource(
            Project? sourceProject, CancellationToken cancellationToken)
        {
            // If we're dealing with a metadata symbol, we have no source information we can look at to make this
            // determination.  So just assume any other assembly has IVT to us.  We'll later do the more expensive
            // semantic check to know for sure.
            if (sourceProject == null)
                return _ => Task.FromResult(true);

            // Lazily initialize and compute the actual place where we store the information.
            Task<HashSet<string>>? internalsVisibleToMap = null;

            return async v =>
            {
                internalsVisibleToMap ??= ComputeInternalsVisibleToMap(sourceProject, cancellationToken);
                var map = await internalsVisibleToMap.ConfigureAwait(false);
                return map.Contains(v);
            };

            static async Task<HashSet<string>> ComputeInternalsVisibleToMap(Project sourceProject, CancellationToken cancellationToken)
            {
                var tasks = sourceProject.Documents.Select(async d =>
                {
                    var index = await d.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
                    return index.InternalsVisibleTo.SelectAsArray(ivt => GetAssemblyName(ivt));
                }).ToArray();

                var assemblyNameArrays = await Task.WhenAll(tasks).ConfigureAwait(false);
                var allAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var array in assemblyNameArrays)
                    allAssemblyNames.AddRange(array);

                return allAssemblyNames;
            }
        }

        private static string GetAssemblyName(string ivtValue)
        {
            var commaIndex = ivtValue.IndexOf(',');
            var assemblyName = commaIndex >= 0 ? ivtValue.Substring(0, commaIndex).Trim() : ivtValue;
            return assemblyName;
        }

        /// <summary>
        /// This method creates an initial cheapish InternalsVisibleTo map from the given <paramref name="assembly"/> to
        /// the assembly names that have friend access to this assembly.  It still requires parsing and binding of all
        /// assembly level attributes in the assembly.
        /// </summary>
        //private static Func<string, bool> GetFastCheckInternalsVisibleToCompilation(IAssemblySymbol assembly)
        //{
        //    // Lazily initialize and compute the actual place where we store the information.
        //    HashSet<string>? internalsVisibleToMap = null;

        //    return v =>
        //    {
        //        internalsVisibleToMap ??= ComputeInternalsVisibleToMap(assembly);
        //        return internalsVisibleToMap.Contains(v);
        //    };

        //    static HashSet<string> ComputeInternalsVisibleToMap(IAssemblySymbol assembly)
        //    {
        //        var map = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //        foreach (var attr in assembly.GetAttributes().Where(IsInternalsVisibleToAttribute))
        //        {
        //            var typeNameConstant = attr.ConstructorArguments.FirstOrDefault();
        //            if (typeNameConstant.Type == null ||
        //                typeNameConstant.Type.SpecialType != SpecialType.System_String ||
        //                !(typeNameConstant.Value is string value))
        //            {
        //                continue;
        //            }

        //            var commaIndex = value.IndexOf(',');
        //            var assemblyName = commaIndex >= 0 ? value.Substring(0, commaIndex).Trim() : value;

        //            map.Add(assemblyName);
        //        }

        //        return map;
        //    }
        //}

        private static bool HasReferenceTo(
            IAssemblySymbol containingAssembly,
            Project? sourceProject,
            Project project,
            CancellationToken cancellationToken)
        {
            if (containingAssembly == null)
            {
                throw new ArgumentNullException(nameof(containingAssembly));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (sourceProject != null)
            {
                // most of time, compilation should be already there
                return project.ProjectReferences.Any(p => p.ProjectId == sourceProject.Id);
            }

            return project.HasReferenceToAssembly(containingAssembly, cancellationToken);
        }

        public static bool HasReferenceToAssembly(this Project project, IAssemblySymbol assemblySymbol, CancellationToken cancellationToken)
            => project.HasReferenceToAssembly(assemblySymbol.Name, cancellationToken);

        public static bool HasReferenceToAssembly(this Project project, string assemblyName, CancellationToken cancellationToken)
        {
            // If the project we're looking at doesn't even support compilations, then there's no 
            // way for it to have an IAssemblySymbol.  And without that, there is no way for it
            // to have any sort of 'ReferenceTo' the provided 'containingAssembly' symbol.
            if (!project.SupportsCompilation)
                return false;

            if (!project.TryGetCompilation(out var compilation))
            {
                // WORKAROUND:
                // perf check metadata reference using newly created empty compilation with only metadata references.
                compilation = project.LanguageServices.CompilationFactory!.CreateCompilation(
                    project.AssemblyName, project.CompilationOptions!);

                compilation = compilation.AddReferences(project.MetadataReferences);
            }

            foreach (var reference in project.MetadataReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol symbol)
                {
                    if (symbol.Name == assemblyName)
                        return true;
                }
            }

            return false;
        }
    }
}
