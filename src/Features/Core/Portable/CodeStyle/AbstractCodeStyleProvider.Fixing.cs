// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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
            public readonly TCodeStyleProvider _codeStyleProvider;

            protected CodeFixProvider()
            {
                _codeStyleProvider = new TCodeStyleProvider();
                FixableDiagnosticIds = ImmutableArray.Create(_codeStyleProvider._descriptorId);
            }

            internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

            public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

            public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
                => _codeStyleProvider.RegisterCodeFixesAsync(context);

            protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
                => _codeStyleProvider.FixAllAsync(document, diagnostics, editor, cancellationToken);
        }
    }
}
