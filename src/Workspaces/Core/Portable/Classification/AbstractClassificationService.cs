﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractClassificationService : IClassificationService
    {
        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
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
                var classifiedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationService, SerializableClassifiedSpans>(
                   document.Project.Solution,
                   (service, solutionInfo, cancellationToken) => service.GetSemanticClassificationsAsync(solutionInfo, document.Id, textSpan, cancellationToken),
                   cancellationToken).ConfigureAwait(false);

                // if the remote call fails do nothing (error has already been reported)
                if (classifiedSpans.HasValue)
                    classifiedSpans.Value.Rehydrate(result);
            }
            else
            {
                await AddSemanticClassificationsInCurrentProcessAsync(
                    document, textSpan, result, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task AddSemanticClassificationsInCurrentProcessAsync(
            Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetRequiredLanguageService<ISyntaxClassificationService>();
            var reassignedVariableService = document.GetRequiredLanguageService<IReassignedVariableService>();

            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
            var classifiers = classificationService.GetDefaultSyntaxClassifiers();

            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds);

            await classificationService.AddSemanticClassificationsAsync(document, textSpan, getNodeClassifiers, getTokenClassifiers, result, cancellationToken).ConfigureAwait(false);

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var classifyReassignedVariables = options.GetOption(ClassificationOptions.ClassifyReassignedVariables);
            if (classifyReassignedVariables)
            {
                var reassignedVariableSpans = await reassignedVariableService.GetLocationsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                foreach (var span in reassignedVariableSpans)
                    result.Add(new ClassifiedSpan(span, ClassificationTypeNames.ReassignedVariable));
            }
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
