// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    internal sealed class GeneratorDriverCreationCache
    {
        // HACK: we probably shouldn't have this be a static instance, but this is easier than threading it through from the Workspace layer.
        internal static readonly GeneratorDriverCreationCache Instance = new();

        private ImmutableDictionary<ProjectId, TaskCompletionSource<GeneratorDriver>> _driverCache = ImmutableDictionary<ProjectId, TaskCompletionSource<GeneratorDriver>>.Empty;

        public async ValueTask<GeneratorDriver> CreateAndRunGeneratorDriverAsync(
            ProjectState projectState,
            Compilation compilation,
            Func<GeneratorFilterContext, bool> generatorFilter,
            CancellationToken cancellationToken)
        {
            // Create a TaskCompletionSource that will represent a generator driver created for this project that can be used by other
            // forks. If the cached TaskCompletionSource is the one we create here first, then that means we are the ones responsible for
            // running the generator the first time.
            var createdTaskCompletionSource = new TaskCompletionSource<GeneratorDriver>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcs = ImmutableInterlocked.GetOrAdd(ref _driverCache, projectState.Id, static (_, created) => created, createdTaskCompletionSource);

            if (tcs != createdTaskCompletionSource)
            {

                // There is already some other work in process to create a driver for this project; we'll want to await that work and then
                GeneratorDriver existingDriver;

                try
                {
                    // TODO: we don't have a WithCancellation() that could be used here, so we should use some alternate pattern to get the
                    // same effect.
                    existingDriver = await tcs.Task.ConfigureAwait(false);
                    existingDriver = UpdateGeneratorDriverToMatchState(existingDriver, projectState);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    // Something went wrong trying to update the driver that was recently created; in this case, let's just create a new one
                    existingDriver = CreateGeneratorDriver();
                }

                return existingDriver.RunGenerators(compilation, generatorFilter, cancellationToken);
            }

            // Otherwise, we're the lucky thread that needs to create a driver and prime it
            var generatorDriver = CreateGeneratorDriver();

            // We don't cancel this since we want it always progress to avoid re-initializing generators multiple times
            generatorDriver = generatorDriver.RunGenerators(compilation, generatorFilter, CancellationToken.None);

            createdTaskCompletionSource.SetResult(generatorDriver);
            return generatorDriver;

            GeneratorDriver CreateGeneratorDriver()
            {
                var generatedFilesBaseDirectory = projectState.CompilationOutputInfo.GetEffectiveGeneratedFilesOutputDirectory();
                var additionalTexts = projectState.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText);
                var compilationFactory = projectState.LanguageServices.GetRequiredService<ICompilationFactoryService>();

                return compilationFactory.CreateGeneratorDriver(
                    projectState.ParseOptions!,
                    GetSourceGenerators(projectState),
                    projectState.ProjectAnalyzerOptions.AnalyzerConfigOptionsProvider,
                    additionalTexts,
                    generatedFilesBaseDirectory);
            }
        }

        public void EmptyCacheForSolution(SolutionCompilationState state)
        {
            // If we don't have any cached drivers, then just return
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
