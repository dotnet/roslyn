#if false
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.CodeActions;

namespace Roslyn.Services.CodeActions
{
    internal partial class AbstractCodeActionsService
    {
        private class Walker : CommonSyntaxWalker
        {
            private readonly AbstractCodeActionsService codeActionsService;
            private readonly IDocument document;
            private readonly TextSpan textSpan;
            private readonly CancellationToken cancellationToken;
            private readonly List<CodeIssue> allIssues;
            private readonly IEnumerable<ICodeIssueProvider> issueProviders;

            public Walker(
                AbstractCodeActionsService codeActionsService,
                IDocument document,
                TextSpan textSpan,
                List<CodeIssue> allIssues,
                IEnumerable<ICodeIssueProvider> issueProviders,
                CancellationToken cancellationToken)
            {
                this.document = document;
                this.codeActionsService = codeActionsService;
                this.textSpan = textSpan;
                this.allIssues = allIssues;
                this.issueProviders = issueProviders;
                this.cancellationToken = cancellationToken;
            }

            public override void Visit(
                CommonSyntaxNode node)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (node.FullSpan.IntersectsWith(this.textSpan))
                {
                    this.codeActionsService.AddIssues(document, node, issueProviders, allIssues, cancellationToken);
                    base.Visit(node);
                }
            }

            protected override void VisitToken(
                CommonSyntaxToken token)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (token.FullSpan.IntersectsWith(this.textSpan))
                {
                    this.codeActionsService.AddIssues(document, token, issueProviders, allIssues, cancellationToken);
                    base.VisitToken(token);
                }
            }

#if false
            private void AddCodeIssues(IEnumerable<CodeIssue> issues)
            {
                if (issues != null)
                {
                    foreach (var codeIssue in issues)
                    {
                        if (IsValid(codeIssue))
                        {
                            allIssues.Add(codeIssue);
                        }
                    }
                }
            }

            private bool IsValid(CodeIssue issue)
            {
                if (issue == null)
                {
                    return false;
                }

                if (textSpan.Contains(issue.TextSpan.Start))
                {
                    return true;
                }

                // Special case.  If it's just the caret (i.e. an empty selection), then the fix is
                // viable if it appears on the caret itself.
                return textSpan.IsEmpty && textSpan.IntersectsWith(issue.TextSpan.Start);
            }
#endif

#if false
            protected override void VisitTrivia(
                CommonSyntaxTrivia trivia)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (visitTrivia && trivia.FullSpan.IntersectsWith(this.textSpan))
                {
                    var issues = this.codeActionService.GetIssues(document, trivia, cancellationToken);
                    AddCodeIssues(issues, allIssues);

                    base.VisitTrivia(trivia);
                }
            }
#endif
        }
    }
}
#endif