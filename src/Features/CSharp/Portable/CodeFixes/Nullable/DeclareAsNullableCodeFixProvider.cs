// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.DeclareAsNullable), Shared]
    internal partial class DeclareAsNullableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            // warning CS8625: Cannot convert null literal to non-nullable reference or unconstrained type parameter.
            get { return ImmutableArray.Create("CS8625"); }
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                MakeDeclarationNullable(document, editor, node);
            }

            return Task.CompletedTask;
        }

        internal static void MakeDeclarationNullable(Document document, SyntaxEditor editor, SyntaxNode node)
        {
            var declarationTypeToFix = TryGetDeclarationTypeToFix(node);
            var fixedDeclaration = SyntaxFactory.NullableType(declarationTypeToFix);
            editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
        }

        private static TypeSyntax TryGetDeclarationTypeToFix(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.NullLiteralExpression) &&
               node.IsParentKind(SyntaxKind.ReturnStatement))
            {
                var methodDeclaration = node.GetAncestors<MethodDeclarationSyntax>().FirstOrDefault();
                if (methodDeclaration != null)
                {
                    return methodDeclaration.ReturnType;
                }
            }

            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Declare_as_Nullable,
                     createChangedDocument,
                     CSharpFeaturesResources.Declare_as_Nullable)
            {
            }
        }
    }
}
