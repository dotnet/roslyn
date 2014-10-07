using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Host;
using Roslyn.Services.Shared.Collections;
using Roslyn.Services.Shared.Utilities;

namespace Roslyn.Services.CodeActions
{
    internal abstract class AbstractCodeActionsExtensionManager : ICodeActionsExtensionManager
    {
        private static readonly ConditionalWeakTable<CodeRefactoring, ICodeRefactoringProvider> refactoringToProviderMap =
            new ConditionalWeakTable<CodeRefactoring, ICodeRefactoringProvider>();

        private static readonly ConditionalWeakTable<CodeIssue, ICodeIssueProvider> issueToProviderMap =
            new ConditionalWeakTable<CodeIssue, ICodeIssueProvider>();

        private readonly ConcurrentSet<ICodeIssueProvider> disabledIssueProviders = new ConcurrentSet<ICodeIssueProvider>(IdentityEqualityComparer<ICodeIssueProvider>.Instance);
        private readonly ConcurrentSet<ICodeRefactoringProvider> disabledRefactoringProviders = new ConcurrentSet<ICodeRefactoringProvider>(IdentityEqualityComparer<ICodeRefactoringProvider>.Instance);

        protected AbstractCodeActionsExtensionManager()
        {
        }

        private void DisableProvider(ICodeIssueProvider provider)
        {
            disabledIssueProviders.Add(provider);
        }

        private void DisableProvider(ICodeRefactoringProvider provider)
        {
            disabledRefactoringProviders.Add(provider);
        }

        public bool IsDisabled(ICodeIssueProvider provider)
        {
            return disabledIssueProviders.Contains(provider);
        }

        public bool IsDisabled(ICodeRefactoringProvider provider)
        {
            return disabledRefactoringProviders.Contains(provider);
        }

        public virtual void HandleException(ICodeIssueProvider provider, Exception exception)
        {
            DisableProvider(provider);
#if false
            errorHandlers.Do(h => h.HandleError(provider, exception));
#endif
        }

        public virtual void HandleException(ICodeRefactoringProvider provider, Exception exception)
        {
            DisableProvider(provider);
#if false
            errorHandlers.Do(h => h.HandleError(provider, exception));
#endif
        }

        public ICodeRefactoringProvider GetProviderForRefactoring(CodeRefactoring refactoring)
        {
            ICodeRefactoringProvider provider;
            return refactoringToProviderMap.TryGetValue(refactoring, out provider)
                ? provider
                : null;
        }

        public ICodeIssueProvider GetProviderForIssue(CodeIssue issue)
        {
            ICodeIssueProvider provider;
            return issueToProviderMap.TryGetValue(issue, out provider)
                ? provider
                : null;
        }

        public CodeRefactoring GetRefactoring(
            ICodeRefactoringProvider provider,
            IDocument document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            if (!IsDisabled(provider))
            {
                try
                {
                    var refactoring = provider.GetRefactoring(document, textSpan, cancellationToken);
                    if (refactoring != null)
                    {
                        refactoringToProviderMap.Add(refactoring, provider);
                    }

                    return refactoring;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    HandleException(provider, e);
                }
            }

            return null;
        }

        public List<CodeIssue> GetIssues(
            ICodeIssueProvider provider,
            List<CodeIssue> allIssues,
            IDocument document,
            CommonSyntaxNode syntax,
            CancellationToken cancellationToken)
        {
            if (!IsDisabled(provider))
            {
                try
                {
                    var issues = provider.GetIssues(document, syntax, cancellationToken);
                    return AddItems(allIssues, issues, provider, issueToProviderMap);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    HandleException(provider, e);
                }
            }

            return allIssues;
        }

        public List<CodeIssue> GetIssues(
            ICodeIssueProvider provider,
            List<CodeIssue> allIssues,
            IDocument document,
            CommonSyntaxToken syntax,
            CancellationToken cancellationToken)
        {
            if (!IsDisabled(provider))
            {
                try
                {
                    var issues = provider.GetIssues(document, syntax, cancellationToken);
                    return AddItems(allIssues, issues, provider, issueToProviderMap);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    HandleException(provider, e);
                }
            }

            return allIssues;
        }

        private static List<TIssue> AddItems<TIssue, TProvider>(
            List<TIssue> allIssues, IEnumerable<TIssue> issues, TProvider provider, ConditionalWeakTable<TIssue, TProvider> table)
            where TIssue : class
            where TProvider : class
        {
            if (issues != null)
            {
                foreach (var issue in issues)
                {
                    allIssues = AddItem(allIssues, issue, provider, table);
                }
            }

            return allIssues;
        }

        private static List<TIssue> AddItem<TIssue, TProvider>(
            List<TIssue> result, TIssue issue, TProvider provider, ConditionalWeakTable<TIssue, TProvider> table)
            where TIssue : class
            where TProvider : class
        {
            if (issue != null)
            {
                result = result ?? new List<TIssue>();
                result.Add(issue);

                table.Add(issue, provider);
            }

            return result;
        }
    }
}