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
    internal interface ICodeActionsExtensionManager : IWorkspaceService
    {
        ICodeRefactoringProvider GetProviderForRefactoring(CodeRefactoring refactoring);

        ICodeIssueProvider GetProviderForIssue(CodeIssue issue);

        CodeRefactoring GetRefactoring(
            ICodeRefactoringProvider provider,
            IDocument document,
            TextSpan textSpan,
            CancellationToken cancellationToken);

        void AddIssues(
            ICodeIssueProvider provider,
            List<CodeIssue> allIssues,
            IDocument document,
            CommonSyntaxNode syntax,
            CancellationToken cancellationToken);

        void AddIssues(
            ICodeIssueProvider provider,
            List<CodeIssue> allIssues,
            IDocument document,
            CommonSyntaxToken syntax,
            CancellationToken cancellationToken);
    }
}
#endif