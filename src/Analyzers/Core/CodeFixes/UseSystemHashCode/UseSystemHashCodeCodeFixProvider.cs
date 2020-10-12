﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class UseSystemHashCodeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseSystemHashCodeCodeFixProvider()
        {
        }

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
            var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            var declarationService = document.GetLanguageService<ISymbolDeclarationService>();
            if (declarationService == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (!Analyzer.TryGetAnalyzer(semanticModel.Compilation, out var analyzer))
            {
                Debug.Fail("Could not get analyzer");
                return;
            }

            foreach (var diagnostic in diagnostics)
            {
                var operationLocation = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var operation = semanticModel.GetOperation(operationLocation, cancellationToken);

                var methodDecl = diagnostic.AdditionalLocations[1].FindNode(cancellationToken);
                var method = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken);
                var methodBlock = declarationService.GetDeclarations(method)[0].GetSyntax(cancellationToken);

                var (accessesBase, members, _) = analyzer.GetHashedMembers(method, operation);
                if (accessesBase || !members.IsDefaultOrEmpty)
                {
                    // Produce the new statements for the GetHashCode method and replace the
                    // existing ones with them.

                    // Only if there was a base.GetHashCode() do we pass in the ContainingType
                    // so that we generate the same.
                    var containingType = accessesBase ? method!.ContainingType : null;
                    var components = generator.GetGetHashCodeComponents(
                        semanticModel.Compilation, containingType, members, justMemberReference: true);

                    var updatedDecl = generator.WithStatements(
                        methodBlock,
                        generator.CreateGetHashCodeStatementsUsingSystemHashCode(
                            generatorInternal, analyzer.SystemHashCodeType, components));
                    editor.ReplaceNode(methodBlock, updatedDecl);
                }
            }
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Use_System_HashCode, createChangedDocument, AnalyzersResources.Use_System_HashCode)
            {
            }
        }
    }
}
