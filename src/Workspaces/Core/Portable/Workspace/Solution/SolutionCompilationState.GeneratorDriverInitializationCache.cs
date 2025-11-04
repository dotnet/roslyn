// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    internal sealed class GeneratorDriverInitializationCache
    {
        /// <summary>
        /// A set of GeneratorDriver instances that have been created for the keyed project in the solution. Any time we create a GeneratorDriver the first time for
        /// a project, we'll put it into this map. If other requests come in to get a GeneratorDriver for the same project (but from different Solution snapshots),
        /// well reuse this GeneratorDriver rather than creating a new one. This allows some first time initialization of a generator (like walking metadata references)
        /// to be shared rather than doing that initialization multiple times. In the case we are reusing a GeneratorDriver, we'll still always update the GeneratorDriver with
        /// the current state of the project, so the results are still correct.
        /// 
        /// Since these entries are going to be holding onto non-trivial amounts of state, we get rid of the cached entries once there's a belief that we won't be
        /// creating further GeneratorDrivers for a given project. See uses of <see cref="EmptyCacheForProjectsThatHaveGeneratorDriversInSolution"/>
        /// for details.
        /// 
        /// Any additions/removals to this map must be done via ImmutableInterlocked methods.
        /// </summary>
        private ImmutableDictionary<ProjectId, AsyncLazy<GeneratorDriver>> _driverCache = ImmutableDictionary<ProjectId, AsyncLazy<GeneratorDriver>>.Empty;

        public async Task<GeneratorDriver> CreateAndRunGeneratorDriverAsync(
            ProjectState projectState,
            Compilation compilation,
            Func<GeneratorFilterContext, bool> generatorFilter,
            CancellationToken cancellationToken)
        {
            // The AsyncLazy we create here implicitly creates a GeneratorDriver that will run generators for the compilation passed to this method.
            // If the one that is added to _driverCache is the one we created, then it's ready to go. If the AsyncLazy is one created by some
            // other call, then we'll still need to run generators for the compilation passed.
            var createdAsyncLazy = AsyncLazy.Create(CreateGeneratorDriverAndRunGenerators);
            var asyncLazy = ImmutableInterlocked.GetOrAdd(ref _driverCache, projectState.Id, static (_, created) => created, createdAsyncLazy);

            if (asyncLazy == createdAsyncLazy)
            {
                // We want to ensure that the driver is always created and initialized at least once, so we'll ensure that runs even if we cancel the request here.
                // Otherwise the concern is we might keep starting and cancelling the work which is just wasteful to keep doing it over and over again. We do this
                // in a Task.Run() so if the underlying computation were to run on our thread, we're not blocking our caller from observing cancellation
                // if they request it.
                _ = Task.Run(() => asyncLazy.GetValueAsync(CancellationToken.None));

                return await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var driver = await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

                driver = UpdateGeneratorDriverToMatchState(driver, projectState);

                return driver.RunGenerators(compilation, generatorFilter, cancellationToken);
            }

            GeneratorDriver CreateGeneratorDriverAndRunGenerators(CancellationToken cancellationToken)
            {
                var generatedFilesBaseDirectory = projectState.CompilationOutputInfo.GetEffectiveGeneratedFilesOutputDirectory();
                var additionalTexts = projectState.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText);
                var compilationFactory = projectState.LanguageServices.GetRequiredService<ICompilationFactoryService>();

                var generatorDriver = compilationFactory.CreateGeneratorDriver(
                    projectState.ParseOptions!,
                    GetSourceGenerators(projectState),
                    projectState.ProjectAnalyzerOptions.AnalyzerConfigOptionsProvider,
                    additionalTexts,
                    generatedFilesBaseDirectory,
                    $"{projectState.Name} ({projectState.Id})");

                return generatorDriver.RunGenerators(compilation, generatorFilter, cancellationToken);
            }
        }

        public void EmptyCacheForProjectsThatHaveGeneratorDriversInSolution(SolutionCompilationState state)
        {
            // If we don't have any cached drivers, then just return before we loop through all the projects
            // in the solution. This is to ensure that once we hit a steady-state case of a Workspace's CurrentSolution
            // having generators for all projects, we won't need to keep anything further in our cache since the cache
            // will never be used -- any running of generators in the future will use the GeneratorDrivers already held by
            // the Solutions.
            //
            // This doesn't need to be synchronized against other mutations to _driverCache. If we see it as empty when
            // in reality something was just being added, we'll just do the cleanup the next time this method is called.
            if (_driverCache.IsEmpty)
                return;

            foreach (var (projectId, tracker) in state._projectIdToTrackerMap)
            {
                if (tracker.GeneratorDriver is not null)
                    EmptyCacheForProject(projectId);
            }
        }

        public void EmptyCacheForProject(ProjectId projectId)
        {
            ImmutableInterlocked.TryRemove(ref _driverCache, projectId, out _);
        }
    }
}
