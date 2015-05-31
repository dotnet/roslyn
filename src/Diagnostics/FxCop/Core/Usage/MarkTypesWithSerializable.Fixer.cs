// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2237: Mark ISerializable types with SerializableAttribute
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "CA2237 CodeFix provider"), Shared]
    public sealed class MarkTypesWithSerializableFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2237Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var generator = SyntaxGenerator.GetGenerator(context.Document);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            node = generator.GetDeclaration(node);
            if (node == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.Single();
            context.RegisterCodeFix(new MyCodeAction(FxCopFixersResources.AddSerializableAttribute,
                                        async ct => await AddSerializableAttribute(context.Document, node, ct).ConfigureAwait(false)),
                                    diagnostic);
        }

        private async Task<Document> AddSerializableAttribute(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var attr = editor.Generator.Attribute(editor.Generator.TypeExpression(WellKnownTypes.SerializableAttribute(editor.SemanticModel.Compilation)));
            editor.AddAttribute(node, attr);
            return editor.GetChangedDocument();
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
