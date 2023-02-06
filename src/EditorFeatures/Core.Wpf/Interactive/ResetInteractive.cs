// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// ResetInteractive class that implements base functionality for reset interactive command.
    /// Does not depend on VS classes to make it easier to stub out and test.
    /// </summary>
    internal abstract class ResetInteractive
    {
        private readonly Func<string, string> _createReference;

        private readonly Func<string, string> _createImport;

        private readonly EditorOptionsService _editorOptionsService;

        internal event EventHandler ExecutionCompleted;

        internal ResetInteractive(EditorOptionsService editorOptionsService, Func<string, string> createReference, Func<string, string> createImport)
        {
            _editorOptionsService = editorOptionsService;
            _createReference = createReference;
            _createImport = createImport;
        }

        internal Task ExecuteAsync(IInteractiveWindow interactiveWindow, string title)
        {
            if (GetProjectProperties(out var references, out var referenceSearchPaths, out var sourceSearchPaths, out var projectNamespaces, out var projectDirectory, out var platform))
            {
                // Now, we're going to do a bunch of async operations.  So create a wait
                // indicator so the user knows something is happening, and also so they cancel.
                var uiThreadOperationExecutor = GetUIThreadOperationExecutor();
                var context = uiThreadOperationExecutor.BeginExecute(title, EditorFeaturesWpfResources.Building_Project, allowCancellation: true, showProgress: false);

                var resetInteractiveTask = ResetInteractiveAsync(
                    interactiveWindow,
                    references,
                    referenceSearchPaths,
                    sourceSearchPaths,
                    projectNamespaces,
                    projectDirectory,
                    platform,
                    context);

                // Once we're done resetting, dismiss the wait indicator and focus the REPL window.
                return resetInteractiveTask.SafeContinueWith(
                    _ =>
                    {
                        context.Dispose();
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
            InteractiveHostPlatform? platform,
            IUIThreadOperationContext uiThreadOperationContext)
        {
            // First, open the repl window.
            var evaluator = (IResettableInteractiveEvaluator)interactiveWindow.Evaluator;

            // If the user hits the cancel button on the wait indicator, then we want to stop the
            // build.
            using (uiThreadOperationContext.UserCancellationToken.Register(() =>
                CancelBuildProject(), useSynchronizationContext: true))
            {
                // First, start a build.
                // If the build fails do not reset the REPL.
                var builtSuccessfully = await BuildProjectAsync().ConfigureAwait(true);
                if (!builtSuccessfully)
                {
                    return;
                }
            }

            // Then reset the REPL
            using var scope = uiThreadOperationContext.AddScope(allowCancellation: true, EditorFeaturesWpfResources.Resetting_Interactive);
            evaluator.ResetOptions = new InteractiveEvaluatorResetOptions(platform);
            await interactiveWindow.Operations.ResetAsync(initialize: true).ConfigureAwait(true);

            // TODO: load context from an rsp file.

            // Now send the reference paths we've collected to the repl.
            await evaluator.SetPathsAsync(referenceSearchPaths, sourceSearchPaths, projectDirectory).ConfigureAwait(true);

            var editorOptions = _editorOptionsService.Factory.GetOptions(interactiveWindow.CurrentLanguageBuffer);
            var importReferencesCommand = referencePaths.Select(_createReference);
            await interactiveWindow.SubmitAsync(importReferencesCommand).ConfigureAwait(true);

            // Project's default namespace might be different from namespace used within project.
            // Filter out namespace imports that do not exist in interactive compilation.
            var namespacesToImport = await GetNamespacesToImportAsync(projectNamespaces, interactiveWindow).ConfigureAwait(true);
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
            out string projectDirectory,
            out InteractiveHostPlatform? platform);

        /// <summary>
        /// A method that should trigger an async project build.
        /// </summary>
        /// <returns>Whether or not the build was successful.</returns>
        protected abstract Task<bool> BuildProjectAsync();

        /// <summary>
        /// A method that should trigger a project cancellation.
        /// </summary>
        protected abstract void CancelBuildProject();

        protected abstract IUIThreadOperationExecutor GetUIThreadOperationExecutor();
    }
}
