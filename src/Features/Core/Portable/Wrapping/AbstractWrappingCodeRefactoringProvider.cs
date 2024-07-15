// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Wrapping;

/// <summary>
/// Base type for the C# and VB wrapping refactorings.  The only responsibility of this type is
/// to walk up the tree at the position the user is at, seeing if any node above the user can be
/// wrapped by any provided <see cref="ISyntaxWrapper"/>s.
/// 
/// Once we get any wrapping actions, we stop looking further.  This keeps the refactorings
/// scoped as closely as possible to where the user is, as well as preventing overloading of the
/// lightbulb with too many actions.
/// </summary>
internal abstract class AbstractWrappingCodeRefactoringProvider : CodeRefactoringProvider
{
    private readonly ImmutableArray<ISyntaxWrapper> _wrappers;

    protected AbstractWrappingCodeRefactoringProvider(
        ImmutableArray<ISyntaxWrapper> wrappers)
    {
        _wrappers = wrappers;
    }

    protected abstract SyntaxWrappingOptions GetWrappingOptions(IOptionsReader options, CodeActionOptions ideOptions);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;
        if (!span.IsEmpty)
            return;

        var position = span.Start;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var options = GetWrappingOptions(configOptions, context.Options.GetOptions(document.Project.Services));

        foreach (var node in token.GetRequiredParent().AncestorsAndSelf())
        {
            var containsSyntaxError = node.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

            // Check if any wrapper can handle this node.  If so, then we're done, otherwise
            // keep walking up.
            foreach (var wrapper in _wrappers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var computer = await wrapper.TryCreateComputerAsync(
                    document, position, node, options, containsSyntaxError, cancellationToken).ConfigureAwait(false);

                if (computer == null)
                    continue;

                var actions = await computer.GetTopLevelCodeActionsAsync().ConfigureAwait(false);
                if (actions.IsDefaultOrEmpty)
                    continue;

                context.RegisterRefactorings(actions);
                return;
            }

            // if we hit a syntax error and the computer couldn't handle it, then bail out.  Don't want to format if
            // we don't really understand what's going on.
            if (containsSyntaxError)
                return;
        }
    }
}
