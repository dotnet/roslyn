// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal abstract class AbstractWrappingCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly ImmutableArray<IWrapper> _wrappers;

        protected AbstractWrappingCodeRefactoringProvider(
            ImmutableArray<IWrapper> wrappers)
        {
            _wrappers = wrappers;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var span = context.Span;
            if (!span.IsEmpty)
            {
                return;
            }

            var position = span.Start;
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

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

                foreach (var wrapper in _wrappers)
                {
                    var actions = await wrapper.ComputeRefactoringsAsync(
                        document, position, node, cancellationToken).ConfigureAwait(false);

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
