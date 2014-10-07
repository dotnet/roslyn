#if false
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
    internal partial class ServicesLayerCodeActionsExtensionManager
    {
        private class CodeActionsExtensionManager : ICodeActionsExtensionManager
        {
            private static readonly ConditionalWeakTable<CodeRefactoring, ICodeRefactoringProvider> refactoringToProviderMap =
                new ConditionalWeakTable<CodeRefactoring, ICodeRefactoringProvider>();

            private static readonly ConditionalWeakTable<CodeIssue, ICodeIssueProvider> issueToProviderMap =
                new ConditionalWeakTable<CodeIssue, ICodeIssueProvider>();

            private readonly IExtensionManager extensionManager;

            public CodeActionsExtensionManager(IExtensionManager extensionManager)
            {
                this.extensionManager = extensionManager;
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
                if (!extensionManager.IsDisabled(provider))
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
                        extensionManager.HandleException(provider, e);
                    }
                }

                return null;
            }

            public void AddIssues(
                ICodeIssueProvider provider,
                List<CodeIssue> allIssues,
                IDocument document,
                CommonSyntaxNode syntax,
                CancellationToken cancellationToken)
            {
                if (!extensionManager.IsDisabled(provider))
                {
                    try
                    {
                        var issues = provider.GetIssues(document, syntax, cancellationToken);
                        AddItems(allIssues, issues, provider, issueToProviderMap);
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

            public void AddIssues(
                ICodeIssueProvider provider,
                List<CodeIssue> allIssues,
                IDocument document,
                CommonSyntaxToken syntax,
                CancellationToken cancellationToken)
            {
                if (!extensionManager.IsDisabled(provider))
                {
                    try
                    {
                        var issues = provider.GetIssues(document, syntax, cancellationToken);
                        AddItems(allIssues, issues, provider, issueToProviderMap);
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

            private static void AddItems<TIssue, TProvider>(
                List<TIssue> allIssues, IEnumerable<TIssue> issues, TProvider provider, ConditionalWeakTable<TIssue, TProvider> table)
                where TIssue : class
                where TProvider : class
            {
                if (issues != null)
                {
                    foreach (var issue in issues)
                    {
                        AddItem(allIssues, issue, provider, table);
                    }
                }
            }

            private static void AddItem<TIssue, TProvider>(
                List<TIssue> result, TIssue issue, TProvider provider, ConditionalWeakTable<TIssue, TProvider> table)
                where TIssue : class
                where TProvider : class
            {
                if (issue != null)
                {
                    result.Add(issue);
                    table.Add(issue, provider);
                }
            }
        }
    }
}
#endif