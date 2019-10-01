// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class UseSystemHashCodeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.UseSystemHashCode);

        internal override CodeFixCategory CodeFixCategory { get; }
            = CodeFixCategory.CodeQuality;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics[0];

            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(document, diagnostic, c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var analyzer = new Analyzer(semanticModel.Compilation);
            Debug.Assert(analyzer.CanAnalyze());

            foreach (var diagnostic in diagnostics)
            {
                var operationLocation = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var operation = semanticModel.GetOperation(operationLocation, cancellationToken);

                var methodDecl = diagnostic.AdditionalLocations[1].FindNode(cancellationToken);
                var method = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken);

                var members = analyzer.GetHashedMembers(method, operation);
                if (!members.IsDefaultOrEmpty)
                {
                    // Produce the new statements for the GetHashCode method and replace the
                    // existing ones with them.
                    var components = generator.GetGetHashCodeComponents(
                        semanticModel.Compilation, method.ContainingType, members, justMemberReference: true);

                    var updatedDecl = generator.WithStatements(
                        methodDecl,
                        generator.CreateGetHashCodeStatementsUsingSystemHashCode(
                            analyzer.SystemHashCodeType, components));
                    editor.ReplaceNode(methodDecl, updatedDecl);
                }
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_System_HashCode, createChangedDocument, FeaturesResources.Use_System_HashCode)
            {
            }
        }
    }
}
