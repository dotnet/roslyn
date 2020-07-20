// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractClassificationService : IClassificationService
    {
        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
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

            var remoteSuccess = await TryAddSemanticClassificationsInRemoteProcessAsync(
                document, textSpan, result, cancellationToken).ConfigureAwait(false);
            if (remoteSuccess)
                return;

            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
            await AddSemanticClassificationsInCurrentProcessAsync(
                document, textSpan, temp, cancellationToken).ConfigureAwait(false);
            AddRange(temp, result);
        }

        /// <returns><see langword="true"/> if the remote call was made successfully and we should
        /// use the results of it. Otherwise, fall back to processing locally</returns>
        private static async Task<bool> TryAddSemanticClassificationsInRemoteProcessAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return false;

            var classifiedSpans = await client.RunRemoteAsync<SerializableClassifiedSpans>(
                WellKnownServiceHubService.CodeAnalysis,
                nameof(IRemoteSemanticClassificationService.GetSemanticClassificationsAsync),
                document.Project.Solution,
                new object[] { document.Id, textSpan },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            classifiedSpans.Rehydrate(result);
            return true;
        }

        public static async Task AddSemanticClassificationsInCurrentProcessAsync(Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetRequiredLanguageService<ISyntaxClassificationService>();

            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
            var classifiers = classificationService.GetDefaultSyntaxClassifiers();

            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds);

            await classificationService.AddSemanticClassificationsAsync(document, textSpan, getNodeClassifiers, getTokenClassifiers, result, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
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

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(syntaxTree);

            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
            classificationService.AddSyntacticClassifications(syntaxTree, textSpan, temp, cancellationToken);
            AddRange(temp, result);
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
    }
}
