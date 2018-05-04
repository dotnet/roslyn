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
            // warning CS8600: Converting null literal or possible null value to non-nullable type.
            get { return ImmutableArray.Create("CS8625", "CS8600"); }
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            var declarationTypeToFix = TryGetDeclarationTypeToFix(node);
            if (declarationTypeToFix == null)
            {
                return;
            }

            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);
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
            if (declarationTypeToFix != null)
            {
                var fixedDeclaration = SyntaxFactory.NullableType(declarationTypeToFix);
                editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
            }
        }

        private static TypeSyntax TryGetDeclarationTypeToFix(SyntaxNode node)
        {
            if (!node.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return null;
            }

            // return null;
            if (node.IsParentKind(SyntaxKind.ReturnStatement))
            {
                var methodDeclaration = node.GetAncestors<MethodDeclarationSyntax>().FirstOrDefault();
                if (methodDeclaration != null)
                {
                    return methodDeclaration.ReturnType;
                }
            }

            // string x = null;
            if (node.Parent?.Parent?.IsParentKind(SyntaxKind.VariableDeclaration) == true)
            {
                var variableDeclaration = (VariableDeclarationSyntax)node.Parent.Parent.Parent;
                if (variableDeclaration.Variables.Count != 1)
                {
                    // string x = null, y = null;
                    return null;
                }

                return variableDeclaration.Type;
            }

            // string x { get; set; } = null;
            if (node.Parent?.IsParentKind(SyntaxKind.PropertyDeclaration) == true)
            {
                var propertyDeclaration = (PropertyDeclarationSyntax)node.Parent.Parent;
                return propertyDeclaration.Type;
            }

            // void M(string x = null) { }
            if (node.Parent?.IsParentKind(SyntaxKind.Parameter) == true)
            {
                var parameter = (ParameterSyntax)node.Parent.Parent;
                return parameter.Type;
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
