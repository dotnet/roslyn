// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        /// A GeneratorDriver instance that has been created for a project in the solution. Any time we create a GeneratorDriver the first time for
        /// a project, we'll hold it in this field. If other requests come in to get a GeneratorDriver for the same project (but from different Solution snapshots),
        /// well reuse this GeneratorDriver rather than creating a new one. This allows some first time initialization of a generator (like walking metadata references)
        /// to be shared rather than doing that initialization multiple times. In the case we are reusing a GeneratorDriver, we'll still always update the GeneratorDriver with
        /// the current state of the project, so the results are still correct.
        ///
        /// This object is held by the ProjectState, but the instance is expected to be shared across multiple Solution instances.
        ///
        /// When a generator run has happened for a Project, this is assigned an updated AsyncLazy holding the most recent GeneratorDriver from the run.
        /// The idea here is if a different fork of the Solution still needs a generator, we have a more recent one. It also ensures that generally the GeneratorDriver
        /// being held is "recent", so that way we're not holding onto generator state from a much older run of the solution.
        /// </summary>
        private AsyncLazy<GeneratorDriver>? _driverCache;

        public GeneratorDriverInitializationCache()
        {
        }

        public async Task<GeneratorDriver> CreateAndRunGeneratorDriverAsync(
            ProjectState projectState,
            Compilation compilation,
            Func<GeneratorFilterContext, bool> generatorFilter,
            CancellationToken cancellationToken)
        {
            // If we already have a cached entry setup, just use it; no reason to avoid creating an AsyncLazy we won't use if we can avoid it
            var existingDriverCache = _driverCache;
            if (existingDriverCache is not null)
            {
                return UpdateDriverAndRunGenerators(await existingDriverCache.GetValueAsync(cancellationToken).ConfigureAwait(false));
            }

            // The AsyncLazy we create here implicitly creates a GeneratorDriver that will run generators for the compilation passed to this method.
            // If the one that is set to _driverCache is the one we created, then it's ready to go. If the AsyncLazy is one created by some
            // other call, then we'll still need to run generators for the compilation passed.
            var createdAsyncLazy = AsyncLazy.Create(CreateGeneratorDriverAndRunGenerators);
            var asyncLazy = Interlocked.CompareExchange(ref _driverCache, createdAsyncLazy, comparand: null);

            if (asyncLazy == null)
            {
                // We want to ensure that the driver is always created and initialized at least once, so we'll ensure that runs even if we cancel the request here.
                // Otherwise the concern is we might keep starting and cancelling the work which is just wasteful to keep doing it over and over again. We do this
                // in a Task.Run() so if the underlying computation were to run on our thread, we're not blocking our caller from observing cancellation
                // if they request it.
                _ = Task.Run(() => createdAsyncLazy.GetValueAsync(CancellationToken.None));

                return await createdAsyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return UpdateDriverAndRunGenerators(await asyncLazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
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

            GeneratorDriver UpdateDriverAndRunGenerators(GeneratorDriver driver)
            {
                driver = UpdateGeneratorDriverToMatchState(driver, projectState);

                return driver.RunGenerators(compilation, generatorFilter, cancellationToken);
            }
        }

        public void UpdateCacheWithForGeneratorDriver(GeneratorDriver driver)
        {
            _driverCache = AsyncLazy.Create(driver);
        }
    }
}
