// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    // This part contains all the logic for hooking up the CodeFixProvider to the CodeStyleProvider.
    // All the code in this part is an implementation detail and is intentionally private so that
    // subclasses cannot change anything.  All code relevant to subclasses relating to fixing is
    // contained in AbstractCodeStyleProvider.cs

    internal abstract partial class AbstractCodeStyleProvider<TOptionKind, TCodeStyleProvider>
    {
        private async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics[0];
            var cancellationToken = context.CancellationToken;

            var codeFixes = await ComputeCodeActionsAsync(
                document, diagnostic, cancellationToken).ConfigureAwait(false);
            context.RegisterFixes(codeFixes, context.Diagnostics);
        }

        public abstract class CodeFixProvider : SyntaxEditorBasedCodeFixProvider
        {
            public readonly TCodeStyleProvider _codeStyleProvider = new();

            protected CodeFixProvider()
            {
                FixableDiagnosticIds = ImmutableArray.Create(_codeStyleProvider._descriptorId);
            }

            public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

            public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
                => _codeStyleProvider.RegisterCodeFixesAsync(context);

            protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
                => _codeStyleProvider.FixAllAsync(document, diagnostics, editor, cancellationToken);
        }
    }
}
