// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractClassificationService : IClassificationService
    {
        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public Task AddSemanticClassificationsAsync(
            Document document, TextSpan textSpan, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return AddClassificationsAsync(document, textSpan, options, ClassificationType.Semantic, result, cancellationToken);
        }

        public Task AddEmbeddedLanguageClassificationsAsync(
            Document document, TextSpan textSpan, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return AddClassificationsAsync(document, textSpan, options, ClassificationType.EmbeddedLanguage, result, cancellationToken);
        }

        private static async Task AddClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            ClassificationType type,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
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

            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // We have an oop connection.  If we're not fully loaded, see if we can retrieve a previously cached set
                // of classifications from the server.  Note: this must be a separate call (instead of being part of
                // service.GetSemanticClassificationsAsync below) as we want to try to read in the cached
                // classifications without doing any syncing to the OOP process.
                var isFullyLoaded = IsFullyLoaded(document, cancellationToken);
                if (await TryGetCachedClassificationsAsync(document, textSpan, type, client, isFullyLoaded, result, cancellationToken).ConfigureAwait(false))
                    return;

                // Call the project overload.  Semantic classification only needs the current project's information
                // to classify properly.
                var classifiedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationService, SerializableClassifiedSpans>(
                   document.Project,
                   (service, solutionInfo, cancellationToken) => service.GetClassificationsAsync(
                       solutionInfo, document.Id, textSpan, type, options, isFullyLoaded, cancellationToken),
                   cancellationToken).ConfigureAwait(false);

                // if the remote call fails do nothing (error has already been reported)
                if (classifiedSpans.HasValue)
                    classifiedSpans.Value.Rehydrate(result);
            }
            else
            {
                await AddClassificationsInCurrentProcessAsync(
                    document, textSpan, type, options, result, cancellationToken).ConfigureAwait(false);
            }
        }

        private static bool IsFullyLoaded(Document document, CancellationToken cancellationToken)
        {
            var workspaceStatusService = document.Project.Solution.Services.GetRequiredService<IWorkspaceStatusService>();

            // Importantly, we do not await/wait on the fullyLoadedStateTask.  We do not want to ever be waiting on work
            // that may end up touching the UI thread (As we can deadlock if GetTagsSynchronous waits on us).  Instead,
            // we only check if the Task is completed.  Prior to that we will assume we are still loading.  Once this
            // task is completed, we know that the WaitUntilFullyLoadedAsync call will have actually finished and we're
            // fully loaded.
            var isFullyLoadedTask = workspaceStatusService.IsFullyLoadedAsync(cancellationToken);
            var isFullyLoaded = isFullyLoadedTask.IsCompleted && isFullyLoadedTask.GetAwaiter().GetResult();
            return isFullyLoaded;
        }

        private static async Task<bool> TryGetCachedClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationType type,
            RemoteHostClient client,
            bool isFullyLoaded,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            // Only try to get cached classifications if we're not fully loaded yet.
            if (isFullyLoaded)
                return false;

            var (documentKey, checksum) = await SemanticClassificationCacheUtilities.GetDocumentKeyAndChecksumAsync(
                document, cancellationToken).ConfigureAwait(false);

            var cachedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationService, SerializableClassifiedSpans?>(
               document.Project,
               (service, solutionInfo, cancellationToken) => service.GetCachedClassificationsAsync(
                   documentKey, textSpan, type, checksum, cancellationToken),
               cancellationToken).ConfigureAwait(false);

            // if the remote call fails do nothing (error has already been reported)
            if (!cachedSpans.HasValue || cachedSpans.Value == null)
                return false;

            cachedSpans.Value.Rehydrate(result);
            return true;
        }

        public static async Task AddClassificationsInCurrentProcessAsync(
            Document document,
            TextSpan textSpan,
            ClassificationType type,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (type == ClassificationType.Semantic)
            {
                var classificationService = document.GetRequiredLanguageService<ISyntaxClassificationService>();
                var reassignedVariableService = document.GetRequiredLanguageService<IReassignedVariableService>();

                var extensionManager = document.Project.Solution.Services.GetRequiredService<IExtensionManager>();
                var classifiers = classificationService.GetDefaultSyntaxClassifiers();

                var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes);
                var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds);

                await classificationService.AddSemanticClassificationsAsync(
                    document, textSpan, options, getNodeClassifiers, getTokenClassifiers, result, cancellationToken).ConfigureAwait(false);

                if (options.ClassifyReassignedVariables)
                {
                    var reassignedVariableSpans = await reassignedVariableService.GetLocationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                    foreach (var span in reassignedVariableSpans)
                        result.Add(new ClassifiedSpan(span, ClassificationTypeNames.ReassignedVariable));
                }
            }
            else if (type == ClassificationType.EmbeddedLanguage)
            {
                var embeddedLanguageService = document.GetLanguageService<IEmbeddedLanguageClassificationService>();
                if (embeddedLanguageService != null)
                {
                    await embeddedLanguageService.AddEmbeddedLanguageClassificationsAsync(
                        document, textSpan, options, result, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(type);
            }
        }

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            AddSyntacticClassifications(document.Project.Solution.Services, root, textSpan, result, cancellationToken);
        }

        public void AddSyntacticClassifications(
            SolutionServices services, SyntaxNode? root, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (root == null)
                return;

            var classificationService = services.GetLanguageServices(root.Language).GetService<ISyntaxClassificationService>();
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

        public TextChangeRange? ComputeSyntacticChangeRange(SolutionServices services, SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var classificationService = services.GetLanguageServices(oldRoot.Language).GetService<ISyntaxClassificationService>();
            return classificationService?.ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, cancellationToken);
        }
    }
}
