﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    using static ConvertProgramTransform;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToProgramMain), Shared]
    internal class ConvertToProgramMainCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToProgramMainCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseProgramMainId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);
            var priority = option.Notification.Severity == ReportDiagnostic.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c), priority),
                context.Diagnostics);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var fixedDocument = await ConvertToProgramMainAsync(document, cancellationToken).ConfigureAwait(false);
            var fixedRoot = await fixedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(editor.OriginalRoot, fixedRoot);
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, CodeActionPriority priority)
                : base(CSharpAnalyzersResources.Convert_to_Program_Main_style_program, createChangedDocument, nameof(ConvertToProgramMainCodeFixProvider))
            {
                this.Priority = priority;
            }
        }
    }
}
