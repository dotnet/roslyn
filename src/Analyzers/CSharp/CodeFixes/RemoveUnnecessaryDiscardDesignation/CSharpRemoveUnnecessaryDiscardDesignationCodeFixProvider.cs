﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation
{
    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = nameof(CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider)), Shared]
    internal partial class CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRemoveUnnecessaryDiscardDesignationCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryDiscardDesignationDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = editor.Generator;

            foreach (var diagnostic in diagnostics)
            {
                var discard = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                switch (discard.Parent)
                {
                    case DeclarationPatternSyntax declarationPattern:
                        if (declarationPattern.Parent is IsPatternExpressionSyntax isPattern)
                        {
                            editor.ReplaceNode(
                                isPattern,
                                (current, _) =>
                                {
                                    var currentIsPattern = (IsPatternExpressionSyntax)current;
                                    return SyntaxFactory.BinaryExpression(
                                        SyntaxKind.IsExpression,
                                        currentIsPattern.Expression,
                                        currentIsPattern.IsKeyword,
                                        ((DeclarationPatternSyntax)isPattern.Pattern).Type)
                                            .WithAdditionalAnnotations(Formatter.Annotation);
                                });
                        }
                        else
                        {
                            editor.ReplaceNode(
                                declarationPattern,
                                (current, _) =>
                                    SyntaxFactory.TypePattern(((DeclarationPatternSyntax)current).Type)
                                                 .WithAdditionalAnnotations(Formatter.Annotation));
                        }
                        break;
                    case RecursivePatternSyntax recursivePattern:
                        editor.ReplaceNode(
                            recursivePattern,
                            (current, _) =>
                                ((RecursivePatternSyntax)current).WithDesignation(null)
                                                                 .WithAdditionalAnnotations(Formatter.Annotation));
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Remove_unnessary_discard, createChangedDocument, CSharpAnalyzersResources.Remove_unnessary_discard)
            {
            }
        }
    }
}
