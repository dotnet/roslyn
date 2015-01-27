// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract partial class AbstractCodeRefactoringService : ICodeRefactoringService
    {
        private static readonly Func<CodeRefactoringProvider, TextSpan, string> codeRefactoringDescription =
            (p, s) => string.Format("{0} ({1})", p.ToString(), s);

        public abstract IEnumerable<CodeRefactoringProvider> GetDefaultCodeRefactoringProviders();

        public async Task<IEnumerable<CodeRefactoring>> GetRefactoringsAsync(
            Document document,
            TextSpan textSpan,
            IEnumerable<CodeRefactoringProvider> providers,
            CancellationToken cancellationToken)
        {
            providers = providers ?? this.GetDefaultCodeRefactoringProviders();

            var result = new List<CodeRefactoring>();
            var extensionManager = document.GetExtensionManager();
            var context = new CodeRefactoringContext(document, textSpan, cancellationToken);

            foreach (var provider in providers)
            {
                await AddRefactoringAsync(provider, result, extensionManager, context).ConfigureAwait(false);
            }

            return result;
        }

        private async Task AddRefactoringAsync(
            CodeRefactoringProvider provider,
            List<CodeRefactoring> allRefactorings,
            IExtensionManager extensionManager,
            CodeRefactoringContext context)
        {
            try
            {
                if (!extensionManager.IsDisabled(provider))
                {
                    using (Logger.LogBlock(FeatureId.CodeActions, FunctionId.CodeAction_AddRefactoring, codeRefactoringDescription, provider, context.Span, context.CancellationToken))
                    {
                        var actions = await provider.GetRefactoringsAsync(context).ConfigureAwait(false);
                        if (actions != null && actions.Count() > 0)
                        {
                            allRefactorings.Add(new CodeRefactoring(provider, actions));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                extensionManager.HandleException(provider, e);
            }
        }
    }
}
