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
        /// For a given solution, maps from an assembly (source-project or metadata-assembly) to the set of projects referencing it.
        ///     Key: DefinitionProject, which contains the project-id for a source-project-assembly, or assembly-name for a metadata-assembly.
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
            Solution solution, ISymbol symbol, IImmutableSet<Project>? projects, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Namespace)
            {
                // namespaces are visible in all projects.
                return projects != null
                    ? projects.ToImmutableArray()
                    : solution.Projects.ToImmutableArray();
            }
            else
            {
                var dependentProjects = await GetDependentProjectsWorkerAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                return projects != null
                    ? dependentProjects.WhereAsArray(projects.Contains)
                    : dependentProjects;
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
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
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

            // 1) Compute all the dependent projects (submission + non-submission) and their InternalsVisibleTo semantics to the definition project.
            ImmutableArray<DependentProject> dependentProjects;

            var visibility = symbol.GetResultantVisibility();
            if (visibility == SymbolVisibility.Private)
            {
                dependentProjects = await ComputeDependentProjectsAsync(solution, symbol, sourceProject, visibility, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // We cache the dependent projects for non-private symbols to speed up future calls.
                var dependentProjectsMap = s_dependentProjectsCache.GetValue(solution, s_createDependentProjectsMapCallback);
                var definitionProject = new DefinitionProject(sourceProjectId: sourceProject?.Id, assemblyName: containingAssembly.Name.ToLower());

                var asyncLazy = dependentProjectsMap.GetOrAdd(
                    definitionProject,
                    _ => new AsyncLazy<ImmutableArray<DependentProject>>(c => ComputeDependentProjectsAsync(solution, symbol, sourceProject, visibility, c), cacheResult: true));
                dependentProjects = await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }

            // 2) Filter the above computed dependent projects based on symbol visibility.
            if (visibility == SymbolVisibility.Internal)
                dependentProjects = dependentProjects.WhereAsArray(dp => dp.HasInternalsAccess);

            return dependentProjects.SelectAsArray(dp => solution.GetRequiredProject(dp.ProjectId));
        }

        private static async Task<ImmutableArray<DependentProject>> ComputeDependentProjectsAsync(
            Solution solution,
            ISymbol symbol,
            Project? sourceProject,
            SymbolVisibility visibility,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dependentProjects = new HashSet<DependentProject>();

            // If a symbol was defined in source, then it is always visible to the project it
            // was defined in.
            if (sourceProject != null)
            {
                dependentProjects.Add(new DependentProject(sourceProject.Id, hasInternalsAccess: true));
            }

            // If it's not private, then we need to find possible references.
            if (visibility != SymbolVisibility.Private)
            {
                AddNonSubmissionDependentProjects(solution, symbol.ContainingAssembly, sourceProject, dependentProjects, cancellationToken);
            }

            // submission projects are special here. The fields generated inside the Script object
            // is private, but further submissions can bind to them.
            await AddSubmissionDependentProjectsAsync(solution, sourceProject, dependentProjects, cancellationToken).ConfigureAwait(false);

            return dependentProjects.ToImmutableArray();
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

        private static void AddNonSubmissionDependentProjects(
            Solution solution,
            IAssemblySymbol containingAssembly,
            Project? sourceProject,
            HashSet<DependentProject> dependentProjects,
            CancellationToken cancellationToken)
        {
            var isSubmission = sourceProject != null && sourceProject.IsSubmission;
            if (isSubmission)
                return;

            HashSet<string>? internalsVisibleToSet = null;
            foreach (var project in solution.Projects)
            {
                if (!project.SupportsCompilation ||
                    !HasReferenceTo(containingAssembly, sourceProject, project, cancellationToken))
                {
                    continue;
                }

                internalsVisibleToSet ??= GetInternalsVisibleToSet(containingAssembly);
                var internalsVisableTo = internalsVisibleToSet.Contains(project.AssemblyName);
                dependentProjects.Add(new DependentProject(project.Id, internalsVisableTo));
            }
        }

        private static HashSet<string> GetInternalsVisibleToSet(IAssemblySymbol assembly)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in assembly.GetAttributes().Where(IsInternalsVisibleToAttribute))
            {
                var typeNameConstant = attr.ConstructorArguments.FirstOrDefault();
                if (typeNameConstant.Type == null ||
                    typeNameConstant.Type.SpecialType != SpecialType.System_String ||
                    !(typeNameConstant.Value is string value))
                {
                    continue;
                }

                var commaIndex = value.IndexOf(',');
                var assemblyName = commaIndex >= 0 ? value.Substring(0, commaIndex).Trim() : value;

                set.Add(assemblyName);
            }

            return set;
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

        //private static async Task<(bool isDependent, ProjectId projectId, bool internalsVisibleTo)> ComputeDependentProjectAsync(
        //    IAssemblySymbol containingAssembly,
        //    SymbolKey containingAssemblySymbolKey,
        //    Project? sourceProject,
        //    Project project,
        //    AsyncLazy<HashSet<string>> sourceProjectInternalsVisibleToMap,
        //    CancellationToken cancellationToken)
        //{
        //    // Only examine real roslyn projects that have a direct P2P reference to the source project (if it exists),
        //    // or contain a metadata reference if we have a metadata-assembly.
        //    if (!project.SupportsCompilation ||
        //        !HasReferenceTo(containingAssembly, sourceProject, project, cancellationToken))
        //    {
        //        return default;
        //    }

        //    if (sourceProject == null)
        //    {
        //        // If it's a symbol from metadata, do the full check for every project to see if it has a reference to
        //        // this metadata dll.
        //        var internalsVisibleTo = await MetadataAssemblyHasInternalsAccessAsync(
        //            containingAssembly, containingAssemblySymbolKey, project, cancellationToken).ConfigureAwait(false);

        //        return (isDependent: true, project.Id, internalsVisibleTo);
        //    }
        //    else
        //    {
        //        // If it's a symbol from source, then load our fast cache of IVT information for that source-project and
        //        // see if this project is in that set.
        //        var ivtMap = await sourceProjectInternalsVisibleToMap.GetValueAsync(cancellationToken).ConfigureAwait(false);
        //        var internalsVisibleTo = ivtMap.Contains(project.AssemblyName);

        //        return (isDependent: true, project.Id, internalsVisibleTo);
        //    }
        //}

        ///// <summary>
        ///// Performs the full, expensive, compilation check if <paramref name="project"/> can see internal symbols from <paramref name="containingAssembly"/>.
        ///// </summary>
        //private static async Task<bool> MetadataAssemblyHasInternalsAccessAsync(
        //    IAssemblySymbol containingAssembly,
        //    SymbolKey containingAssemblySymbolKey,
        //    Project project,
        //    CancellationToken cancellationToken)
        //{
        //    Debug.Assert(project.SupportsCompilation);

        //    var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        //    var targetAssembly = compilation.Assembly;

        //    if (containingAssembly.Language != targetAssembly.Language)
        //    {
        //        var resolvedSymbol = containingAssemblySymbolKey.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
        //        if (resolvedSymbol is IAssemblySymbol sourceAssemblyInTargetCompilation)
        //        {
        //            return targetAssembly.IsSameAssemblyOrHasFriendAccessTo(sourceAssemblyInTargetCompilation);
        //        }
        //    }
        //    else
        //    {
        //        return targetAssembly.IsSameAssemblyOrHasFriendAccessTo(containingAssembly);
        //    }

        //    return false;
        //}

        /// <summary>
        /// This method creates an cheap InternalsVisibleTo map from the given <paramref name="sourceProject"/> to the
        /// assembly names that have friend access to this assembly.  It uses the information stored in our syntactic
        /// indices to avoid having to check with the compilation to get the assembly attributes in the project.
        /// </summary>
        /// <remarks>
        /// Importantly: this set is not 100% accurate.  It operates like a bloom-filter in that it can exactly tell you
        /// if an item is *not* in the set.  However, it may claim something is in the set in an innacurate fashion.
        /// Specifically, it just looks for `[InternalsVisibleTo("...")]` in files and trusts that if it finds any, that
        /// the assembly-name referenced in the string will have IVT to this project.  That may be innacurate given that
        /// there may be additional assembly-identity information that would make it so that the actual project would not have 
        /// IVT.  However, that should be:
        /// 
        /// 1) very rare/unlikely.
        /// 
        /// 2) something that doesn't impact quality of results.  i.e we'll just check more projects for results.
        /// However, we will still only look in those projects for documents that can match, so it shouldn't result in
        /// significant extra work to be done.
        /// </remarks>
        private static AsyncLazy<HashSet<string>> GetSourceProjectInternalsVisibleToMap(Project? sourceProject)
        {
            // If we're dealing with a metadata symbol, we have no source information we can look at to make this
            // determination.
            if (sourceProject == null)
                return new AsyncLazy<HashSet<string>>(new HashSet<string>());

            return new AsyncLazy<HashSet<string>>(
                async c =>
                {
                    // Load all the indices for all documents in the project and grab any potential IVT attributes.
                    var tasks = sourceProject.Documents.Select(async d =>
                    {
                        var index = await d.GetSyntaxTreeIndexAsync(c).ConfigureAwait(false);
                        return index.InternalsVisibleTo.SelectAsArray(ivt => GetAssemblyName(ivt));
                    }).ToArray();

                    var assemblyNameArrays = await Task.WhenAll(tasks).ConfigureAwait(false);

                    var allAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var array in assemblyNameArrays)
                        allAssemblyNames.AddRange(array);

                    return allAssemblyNames;
                }, cacheResult: true);
        }

        private static string GetAssemblyName(string ivtValue)
        {
            var commaIndex = ivtValue.IndexOf(',');
            var assemblyName = commaIndex >= 0 ? ivtValue.Substring(0, commaIndex).Trim() : ivtValue;
            return assemblyName;
        }

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
