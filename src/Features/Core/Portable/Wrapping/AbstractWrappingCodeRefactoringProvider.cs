// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Wrapping
{
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

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            if (!span.IsEmpty)
            {
                return;
            }

            var position = span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            foreach (var node in token.Parent.AncestorsAndSelf())
            {
                // Make sure we don't have any syntax errors here.  Don't want to format if we don't
                // really understand what's going on.
                if (node.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    return;
                }

                // Check if any wrapper can handle this node.  If so, then we're done, otherwise
                // keep walking up.
                foreach (var wrapper in _wrappers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var computer = await wrapper.TryCreateComputerAsync(
                        document, position, node, cancellationToken).ConfigureAwait(false);

                    if (computer == null)
                    {
                        continue;
                    }

                    var actions = await computer.GetTopLevelCodeActionsAsync().ConfigureAwait(false);
                    if (actions.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    context.RegisterRefactorings(actions);
                    return;
                }
            }
        }
    }
}
