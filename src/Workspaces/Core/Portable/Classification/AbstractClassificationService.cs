// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractClassificationService : IClassificationService
    {
        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<ISyntaxClassificationService>();
            if (classificationService == null)
            {
                // When renaming a file's extension through VS when it's opened in editor, 
                // the content type might change and the content type changed event can be 
                // raised before the renaming propagate through VS workspace. As a result, 
                // the document we got (based on the buffer) could still be the one in the workspace
                // before rename happened. This would cause us problem if the document is supported 
                // by workspace but not a roslyn language (e.g. xaml, F#, etc.), since none of the roslyn 
                // language services would be available.
                //
                // If this is the case, we will simply bail out. It's OK to ignore the request
                // because when the buffer eventually get associated with the correct document in roslyn
                // workspace, we will be invoked again.
                //
                // For example, if you open a xaml from from a WPF project in designer view,
                // and then rename file extension from .xaml to .cs, then the document we received
                // here would still belong to the special "-xaml" project.
                return;
            }

            var workspaceStatusService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();

            // Importantly, we do not await/wait on the fullyLoadedStateTask.  We do not want to ever be waiting on work
            // that may end up touching the UI thread (As we can deadlock if GetTagsSynchronous waits on us).  Instead,
            // we only check if the Task is completed.  Prior to that we will assume we are still loading.  Once this
            // task is completed, we know that the WaitUntilFullyLoadedAsync call will have actually finished and we're
            // fully loaded.
            var isFullyLoadedTask = workspaceStatusService.IsFullyLoadedAsync(cancellationToken);
            var isFullyLoaded = isFullyLoadedTask.IsCompleted && isFullyLoadedTask.GetAwaiter().GetResult();

            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // Call the project overload.  Semantic classification only needs the current project's information
                // to classify properly.
                var classifiedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationService, SerializableClassifiedSpans>(
                   document.Project,
                   (service, solutionInfo, cancellationToken) => service.GetSemanticClassificationsAsync(solutionInfo, document.Id, textSpan, options, isFullyLoaded, cancellationToken),
                   cancellationToken).ConfigureAwait(false);

                // if the remote call fails do nothing (error has already been reported)
                if (classifiedSpans.HasValue)
                    classifiedSpans.Value.Rehydrate(result);
            }
            else
            {
                await AddSemanticClassificationsInCurrentProcessAsync(
                    document, textSpan, options, isFullyLoaded, result, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task AddSemanticClassificationsInCurrentProcessAsync(
            Document document, TextSpan textSpan, ClassificationOptions options, bool isFullyLoaded, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            // If we're not fully loaded try to read from the cache instead so that classifications appear up to date.
            // New code will not be semantically classified, but will eventually when the project fully loads.
            if (await TryAddSemanticClassificationsFromCacheAsync(document, textSpan, isFullyLoaded, result, cancellationToken).ConfigureAwait(false))
                return;

            var classificationService = document.GetRequiredLanguageService<ISyntaxClassificationService>();
            var reassignedVariableService = document.GetRequiredLanguageService<IReassignedVariableService>();

            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
            var classifiers = classificationService.GetDefaultSyntaxClassifiers();

            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds);

            await classificationService.AddSemanticClassificationsAsync(document, textSpan, options, getNodeClassifiers, getTokenClassifiers, result, cancellationToken).ConfigureAwait(false);

            if (options.ClassifyReassignedVariables)
            {
                var reassignedVariableSpans = await reassignedVariableService.GetLocationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                foreach (var span in reassignedVariableSpans)
                    result.Add(new ClassifiedSpan(span, ClassificationTypeNames.ReassignedVariable));
            }
        }

        private static async Task<bool> TryAddSemanticClassificationsFromCacheAsync(
            Document document,
            TextSpan textSpan,
            bool isFullyLoaded,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            CancellationToken cancellationToken)
        {
            // Don't use the cache if we're fully loaded.  We should just compute values normally.
            if (isFullyLoaded)
                return false;

            var semanticCacheService = document.Project.Solution.Workspace.Services.GetService<ISemanticClassificationCacheService>();
            if (semanticCacheService == null)
                return false;

            var result = await semanticCacheService.GetCachedSemanticClassificationsAsync(
                document, textSpan, cancellationToken).ConfigureAwait(false);
            if (result.IsDefault)
                return false;

            classifiedSpans.AddRange(result);
            return true;
        }

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            AddSyntacticClassifications(document.Project.Solution.Workspace, root, textSpan, result, cancellationToken);
        }

        public void AddSyntacticClassifications(
            Workspace workspace, SyntaxNode? root, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (root == null)
                return;

            var classificationService = workspace.Services.GetLanguageServices(root.Language).GetService<ISyntaxClassificationService>();
            if (classificationService == null)
                return;

            classificationService.AddSyntacticClassifications(root, textSpan, result, cancellationToken);
        }

        /// <summary>
        /// Helper to add all the values of <paramref name="temp"/> into <paramref name="result"/>
        /// without causing any allocations or boxing of enumerators.
        /// </summary>
        protected static void AddRange(ArrayBuilder<ClassifiedSpan> temp, List<ClassifiedSpan> result)
        {
            foreach (var span in temp)
            {
                result.Add(span);
            }
        }

        public ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(Document oldDocument, Document newDocument, TimeSpan timeout, CancellationToken cancellationToken)
            => default;

        public TextChangeRange? ComputeSyntacticChangeRange(Workspace workspace, SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var classificationService = workspace.Services.GetLanguageServices(oldRoot.Language).GetService<ISyntaxClassificationService>();
            return classificationService?.ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, cancellationToken);
        }
    }
}
