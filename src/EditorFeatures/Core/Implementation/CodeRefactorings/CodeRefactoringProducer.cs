// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringProducer))]
    internal class CodeRefactoringProducer : ICodeRefactoringProducer
    {
        [ImportingConstructor]
        public CodeRefactoringProducer()
        {
        }

        private IEnumerable<CodeRefactoringProvider> GetProviders(Document document)
        {
            return CodeRefactoringService.GetDefaultCodeRefactoringProviders(document);
        }

        public async Task<IEnumerable<CodeRefactoring>> GetCodeRefactoringsAsync(
            Document document,
            TextSpan state,
            CancellationToken cancellationToken)
        {
            var optionService = WorkspaceServices.WorkspaceService.GetService<IOptionService>(document.Project.Solution.Workspace);
            if (!optionService.GetOption(EditorComponentOnOffOptions.CodeRefactorings))
            {
                return SpecializedCollections.EmptyEnumerable<CodeRefactoring>();
            }

            using (Logger.LogBlock(FeatureId.CodeActions, FunctionId.CodeActions_RefactoringProducer_AddNewItemsWorker, cancellationToken))
            {
                var extensionManager = document.GetExtensionManager();
                var tasks = new List<Task<CodeRefactoring>>();
                var context = new CodeRefactoringContext(document, state, cancellationToken);

                foreach (var provider in this.GetProviders(document))
                {
                    tasks.Add(Task.Run(() => GetRefactoringFromProvider(
                        provider,
                        extensionManager,
                        context)));
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.WhereNotNull();
            }
        }

        private async Task<CodeRefactoring> GetRefactoringFromProvider(
            CodeRefactoringProvider provider,
            IExtensionManager extensionManager,
            CodeRefactoringContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (extensionManager.IsDisabled(provider))
            {
                return null;
            }

            try
            {
                var actions = await provider.GetRefactoringsAsync(context).ConfigureAwait(false);
                if (actions != null && actions.Count() > 0)
                {
                    return new CodeRefactoring(provider, actions);
                }
            }
            catch (OperationCanceledException)
            {
                // We don't want to catch operation canceled exceptions in the catch block 
                // below. So catch is here and rethrow it.
                throw;
            }
            catch (Exception e)
            {
                extensionManager.HandleException(provider, e);
            }

            return null;
        }
    }
}
