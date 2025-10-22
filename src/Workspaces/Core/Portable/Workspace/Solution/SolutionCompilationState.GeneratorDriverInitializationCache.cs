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
                // Otherwise the concern is we might keep starting and cancelling the work which is just wasteful to keep doing it over and over again.
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
                    generatedFilesBaseDirectory);

                return generatorDriver.RunGenerators(compilation, generatorFilter, cancellationToken);
            }
        }

        public void EmptyCacheForProjectsThatHaveGeneratorDriversInSolution(SolutionCompilationState state)
        {
            // If we don't have any cached drivers, then just return before we loop through all the projects
            // in the solution.
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
