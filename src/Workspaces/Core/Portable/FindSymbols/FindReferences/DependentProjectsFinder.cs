// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Provides helper methods for finding dependent projects across a solution that a given symbol can be referenced within.
    /// </summary>
    internal static class DependentProjectsFinder
    {
        /// <summary>
        /// A helper struct used for keying in <see cref="s_dependentProjectsCache"/>.
        /// </summary>
        private struct DefinitionProject
        {
            private readonly ProjectId _sourceProjectId;
            private readonly string _assemblyName;

            public DefinitionProject(ProjectId sourceProjectId, string assemblyName)
            {
                _sourceProjectId = sourceProjectId;
                _assemblyName = assemblyName;
            }
        }

        private struct DependentProject : IEquatable<DependentProject>
        {
            public readonly ProjectId ProjectId;
            public readonly bool HasInternalsAccess;

            public DependentProject(ProjectId dependentProjectId, bool hasInternalsAccess)
            {
                this.ProjectId = dependentProjectId;
                this.HasInternalsAccess = hasInternalsAccess;
            }

            public override bool Equals(object obj)
            {
                return obj is DependentProject && this.Equals((DependentProject)obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(HasInternalsAccess, ProjectId.GetHashCode());
            }

            public bool Equals(DependentProject other)
            {
                return HasInternalsAccess == other.HasInternalsAccess && ProjectId.Equals(other.ProjectId);
            }
        }

        /// <summary>
        /// Dependent projects cache.
        /// For a given solution, maps from an assembly (source/metadata) to the set of projects referencing it.
        ///     Key: DefinitionProject, which contains the assembly name and a flag indicating whether assembly is source or metadata assembly.
        ///     Value: List of DependentProjects, where each DependentProject contains a dependent project ID and a flag indicating whether the dependent project has internals access to definition project.
        /// </summary>
        private static readonly ConditionalWeakTable<Solution, ConcurrentDictionary<DefinitionProject, ImmutableArray<DependentProject>>> s_dependentProjectsCache =
            new ConditionalWeakTable<Solution, ConcurrentDictionary<DefinitionProject, ImmutableArray<DependentProject>>>();

        /// <summary>
        /// Used to create a new concurrent dependent projects map for a given assembly when needed.
        /// </summary>
        private static readonly ConditionalWeakTable<Solution, ConcurrentDictionary<DefinitionProject, ImmutableArray<DependentProject>>>.CreateValueCallback s_createDependentProjectsMapCallback =
            _ => new ConcurrentDictionary<DefinitionProject, ImmutableArray<DependentProject>>(concurrencyLevel: 2, capacity: 20);

        public static async Task<ImmutableArray<Project>> GetDependentProjectsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            if (symbol.Kind == SymbolKind.Namespace)
            {
                // namespaces are visible in all projects.
                if (projects != null)
                {
                    return projects.ToImmutableArray();
                }

                return GetAllProjects(solution);
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

        private static ImmutableArray<Project> GetAllProjects(Solution solution)
            => solution.Projects.ToImmutableArray();

        private static ImmutableArray<Project> GetProjects(Solution solution, ImmutableArray<ProjectId> projectIds)
            => projectIds.SelectAsArray(id => solution.GetProject(id));

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
            this ISymbol symbol,
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
                // We cache the dependent projects for non-private symbols, check in the cache first.
                var dependentProjectsMap = s_dependentProjectsCache.GetValue(solution, s_createDependentProjectsMapCallback);
                var key = new DefinitionProject(sourceProjectId: sourceProject?.Id, assemblyName: containingAssembly.Name.ToLower());

                if (!dependentProjectsMap.TryGetValue(key, out dependentProjects))
                {
                    dependentProjects = await GetDependentProjectsCoreAsync(symbol, solution, sourceProject, visibility, cancellationToken).ConfigureAwait(false);
                    dependentProjectsMap.TryAdd(key, dependentProjects);
                }
            }

            // 2) Filter the above computed dependent projects based on symbol visibility.
            return FilterDependentProjectsByVisibility(solution, dependentProjects, visibility);
        }

        private static async Task<ImmutableArray<DependentProject>> GetDependentProjectsCoreAsync(
            ISymbol symbol,
            Solution solution,
            Project sourceProject,
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
            return GetProjects(solution, projectIds);
        }

        private static async Task AddSubmissionDependentProjectsAsync(Solution solution, Project sourceProject, HashSet<DependentProject> dependentProjects, CancellationToken cancellationToken)
        {
            var isSubmission = sourceProject is { IsSubmission: true };
            if (!isSubmission)
            {
                return;
            }

            var projectIdsToReferencingSubmissionIds = new Dictionary<ProjectId, List<ProjectId>>();

            // search only submission project
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (project.IsSubmission && project.SupportsCompilation)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If we are referencing another project, store the link in the other direction
                    // so we walk across it later
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var previous = compilation.ScriptCompilationInfo.PreviousScriptCompilation;

                    if (previous != null)
                    {
                        var referencedProject = solution.GetProject(previous.Assembly, cancellationToken);
                        if (!projectIdsToReferencingSubmissionIds.TryGetValue(referencedProject.Id, out var referencingSubmissions))
                        {
                            referencingSubmissions = new List<ProjectId>();
                            projectIdsToReferencingSubmissionIds.Add(referencedProject.Id, referencingSubmissions);
                        }

                        referencingSubmissions.Add(project.Id);
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
            if (attrType == null)
            {
                return false;
            }

            var attributeName = attr.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
            return attributeName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute";
        }

        private static async Task AddNonSubmissionDependentProjectsAsync(IAssemblySymbol sourceAssembly, Solution solution, Project sourceProject, HashSet<DependentProject> dependentProjects, CancellationToken cancellationToken)
        {
            var isSubmission = sourceProject != null && sourceProject.IsSubmission;
            if (isSubmission)
            {
                return;
            }

            var internalsVisibleToMap = CreateInternalsVisibleToMap(sourceAssembly);

            var sourceAssemblySymbolKey = sourceAssembly.GetSymbolKey();

            // TODO(cyrusn): What about error tolerance situations.  Do we maybe want to search
            // transitive dependencies as well?  Even if the code wouldn't compile, they may be
            // things we want to find.
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);

                cancellationToken.ThrowIfCancellationRequested();

                if (project.SupportsCompilation && HasReferenceTo(sourceAssembly, sourceProject, project, cancellationToken))
                {
                    var hasInternalsAccess = await HasInternalsAccessAsync(
                        sourceAssembly, internalsVisibleToMap,
                        sourceAssemblySymbolKey, project, cancellationToken).ConfigureAwait(false);

                    dependentProjects.Add(new DependentProject(project.Id, hasInternalsAccess));
                }
            }
        }

        private static async Task<bool> HasInternalsAccessAsync(
            IAssemblySymbol sourceAssembly, Lazy<HashSet<string>> internalsVisibleToMap,
            SymbolKey sourceAssemblySymbolKey, Project project, CancellationToken cancellationToken)
        {
            if (internalsVisibleToMap.Value.Contains(project.AssemblyName) &&
                project.SupportsCompilation)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var targetAssembly = compilation.Assembly;
                if (sourceAssembly.Language != targetAssembly.Language)
                {
                    var resolvedSymbol = sourceAssemblySymbolKey.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
                    if (resolvedSymbol is IAssemblySymbol sourceAssemblyInTargetCompilation)
                    {
                        return targetAssembly.IsSameAssemblyOrHasFriendAccessTo(sourceAssemblyInTargetCompilation);
                    }
                }
                else
                {
                    return targetAssembly.IsSameAssemblyOrHasFriendAccessTo(sourceAssembly);
                }
            }

            return false;
        }

        /// <summary>
        /// This method creates an initial cheap InternalsVisibleTo map from the given <paramref name="assembly"/> to the assembly names that have friend access to this assembly.
        /// This map is a superset of the actual InternalsVisibleTo map and is used for performance reasons only.
        /// While identifying depend projects that can reference a given symbol (see method <see cref="AddNonSubmissionDependentProjectsAsync"/>), we need to know a symbol's
        /// accessibility from referencing projects. This requires us to create a compilation for the referencing project just to check accessibility and can be performance intensive.
        /// Instead, we crack the assembly attributes just for the symbol's containing assembly here to enable cheap checks for friend assemblies in <see cref="AddNonSubmissionDependentProjectsAsync"/>.
        /// </summary>
        private static Lazy<HashSet<string>> CreateInternalsVisibleToMap(IAssemblySymbol assembly)
        {
            var internalsVisibleToMap = new Lazy<HashSet<string>>(() =>
            {
                var map = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attr in assembly.GetAttributes().Where(IsInternalsVisibleToAttribute))
                {
                    var typeNameConstant = attr.ConstructorArguments.FirstOrDefault();
                    if (typeNameConstant.Type == null || typeNameConstant.Type.SpecialType != SpecialType.System_String)
                    {
                        continue;
                    }

                    var value = (string)typeNameConstant.Value;
                    if (value == null)
                    {
                        continue;
                    }

                    var commaIndex = value.IndexOf(',');
                    var assemblyName = commaIndex >= 0 ? value.Substring(0, commaIndex).Trim() : value;

                    map.Add(assemblyName);
                }

                return map;
            }, isThreadSafe: true);
            return internalsVisibleToMap;
        }

        private static bool HasReferenceTo(IAssemblySymbol containingAssembly, Project sourceProject, Project project, CancellationToken cancellationToken)
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
        {
            return project.HasReferenceToAssembly(assemblySymbol.Name, cancellationToken);
        }

        public static bool HasReferenceToAssembly(this Project project, string assemblyName, CancellationToken cancellationToken)
        {
            var hasMatch = project.GetAssemblyReferenceType(
                a => a.Name == assemblyName ? true : (bool?)null,
                cancellationToken);

            return hasMatch == true;
        }

        /// <summary>
        /// Determines if this project has a reference to an assembly matching a passed
        /// in predicate.  The predicate returns 'null' to indicate no match, and non-null
        /// to indicate a match of some kind.  If any match is found, that value is returned
        /// as the value of this function.  Otherwise 'null' is returned.
        /// </summary>
        private static T? GetAssemblyReferenceType<T>(
            this Project project,
            Func<IAssemblySymbol, T?> predicate,
            CancellationToken cancellationToken) where T : struct
        {
            // If the project we're looking at doesn't even support compilations, then there's no 
            // way for it to have an IAssemblySymbol.  And without that, there is no way for it
            // to have any sort of 'ReferenceTo' the provided 'containingAssembly' symbol.
            if (!project.SupportsCompilation)
            {
                return null;
            }

            // WORKAROUND:
            // perf check metadata reference using newly created empty compilation with only metadata references.
            //
            // TODO(cyrusn): Why don't we call project.TryGetCompilation first?  
            // wouldn't we want to use that compilation if it's available?
            var compilation = project.LanguageServices.CompilationFactory.CreateCompilation(
                project.AssemblyName, project.CompilationOptions);

            compilation = compilation.AddReferences(project.MetadataReferences);

            foreach (var reference in project.MetadataReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol symbol)
                {
                    var result = predicate(symbol);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
