// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1813: Avoid unsealed attributes
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public class AvoidUnsealedAttributesFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AvoidUnsealedAttributesAnalyzer.RuleId); 
        
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            var declaration = editor.Generator.GetDeclaration(node);

            if (declaration != null)
            {
                // We cannot have multiple overlapping diagnostics of this id.
                var diagnostic = context.Diagnostics.Single();

                context.RegisterCodeFix(new MyCodeAction(SystemRuntimeAnalyzersResources.AvoidUnsealedAttributesCodeFix,
                    async ct => await MakeSealed(editor, declaration, ct).ConfigureAwait(false)),
                    diagnostic);
            }
        }

        private Task<Document> MakeSealed(DocumentEditor editor, SyntaxNode declaration, CancellationToken ct)
        {
            var modifiers = editor.Generator.GetModifiers(declaration);
            editor.SetModifiers(declaration, modifiers + DeclarationModifiers.Sealed);
            return Task.FromResult(editor.GetChangedDocument());
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
