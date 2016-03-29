// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    /// <summary>
    /// ResetInteractive class that implements base functionality for reset interactive command.
    /// Does not depend on VS classes to make it easier to stub out and test.
    /// </summary>
    internal abstract class ResetInteractive
    {
        private readonly Func<string, string> _createReference;

        private readonly Func<string, string> _createImport;

        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;

        internal event EventHandler ExecutionCompleted;

        internal ResetInteractive(IEditorOptionsFactoryService editorOptionsFactoryService, Func<string, string> createReference, Func<string, string> createImport)
        {
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _createReference = createReference;
            _createImport = createImport;
        }

        internal Task Execute(IInteractiveWindow interactiveWindow, string title)
        {
            ImmutableArray<string> references, referenceSearchPaths, sourceSearchPaths, projectNamespaces;
            string projectDirectory;

            if (GetProjectProperties(out references, out referenceSearchPaths, out sourceSearchPaths, out projectNamespaces, out projectDirectory))
            {
                // Now, we're going to do a bunch of async operations.  So create a wait
                // indicator so the user knows something is happening, and also so they cancel.
                var waitIndicator = GetWaitIndicator();
                var waitContext = waitIndicator.StartWait(title, InteractiveEditorFeaturesResources.BuildingProject, allowCancel: true);

                var resetInteractiveTask = ResetInteractiveAsync(
                    interactiveWindow,
                    references,
                    referenceSearchPaths,
                    sourceSearchPaths,
                    projectNamespaces,
                    projectDirectory,
                    waitContext);

                // Once we're done resetting, dismiss the wait indicator and focus the REPL window.
                return resetInteractiveTask.SafeContinueWith(
                    _ =>
                    {
                        waitContext.Dispose();
                        ExecutionCompleted?.Invoke(this, new EventArgs());
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }

            return Task.CompletedTask;
        }

        private async Task ResetInteractiveAsync(
            IInteractiveWindow interactiveWindow,
            ImmutableArray<string> referencePaths,
            ImmutableArray<string> referenceSearchPaths,
            ImmutableArray<string> sourceSearchPaths,
            ImmutableArray<string> projectNamespaces,
            string projectDirectory,
            IWaitContext waitContext)
        {
            // First, open the repl window.
            IInteractiveEvaluator evaluator = interactiveWindow.Evaluator;

            // If the user hits the cancel button on the wait indicator, then we want to stop the
            // build.
            using (waitContext.CancellationToken.Register(() =>
                CancelBuildProject(), useSynchronizationContext: true))
            {
                // First, start a build.
                // If the build fails do not reset the REPL.
                var builtSuccessfully = await BuildProject().ConfigureAwait(true);
                if (!builtSuccessfully)
                {
                    return;
                }
            }

            // Then reset the REPL
            waitContext.Message = InteractiveEditorFeaturesResources.ResettingInteractive;
            await interactiveWindow.Operations.ResetAsync(initialize: true).ConfigureAwait(true);

            // TODO: load context from an rsp file.

            // Now send the reference paths we've collected to the repl.
            // The SetPathsAsync method is not available through an Interface.
            // Execute the method only if the cast to a concrete InteractiveEvaluator succeeds.
            InteractiveEvaluator interactiveEvaluator = evaluator as InteractiveEvaluator;
            if (interactiveEvaluator != null)
            {
                await interactiveEvaluator.SetPathsAsync(referenceSearchPaths, sourceSearchPaths, projectDirectory).ConfigureAwait(true);
            }

            var editorOptions = _editorOptionsFactoryService.GetOptions(interactiveWindow.CurrentLanguageBuffer);
            var importReferencesCommand = referencePaths.Select(_createReference);
            await interactiveWindow.SubmitAsync(importReferencesCommand).ConfigureAwait(true);

            // Project's default namespace might be different from namespace used within project.
            // Filter out namespace imports that do not exist in interactive compilation.
            IEnumerable<string> namespacesToImport = await GetNamespacesToImportAsync(projectNamespaces, interactiveWindow).ConfigureAwait(true);
            var importNamespacesCommand = namespacesToImport.Select(_createImport).Join(editorOptions.GetNewLineCharacter());

            if (!string.IsNullOrWhiteSpace(importNamespacesCommand))
            {
                await interactiveWindow.SubmitAsync(new[] { importNamespacesCommand }).ConfigureAwait(true);
            }
        }

        protected abstract Task<IEnumerable<string>> GetNamespacesToImportAsync(IEnumerable<string> namespacesToImport, IInteractiveWindow interactiveWindow);

        /// <summary>
        /// Gets the properties of the currently selected projects necessary for reset.
        /// </summary>
        protected abstract bool GetProjectProperties(
            out ImmutableArray<string> references,
            out ImmutableArray<string> referenceSearchPaths,
            out ImmutableArray<string> sourceSearchPaths,
            out ImmutableArray<string> projectNamespaces,
            out string projectDirectory);

        /// <summary>
        /// A method that should trigger an async project build.
        /// </summary>
        /// <returns>Whether or not the build was successful.</returns>
        protected abstract Task<bool> BuildProject();

        /// <summary>
        /// A method that should trigger a project cancellation.
        /// </summary>
        protected abstract void CancelBuildProject();

        protected abstract IWaitIndicator GetWaitIndicator();
    }
}
