// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2229: Implement serialization constructors.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "CA2229 CodeFix provider"), Shared]
    public sealed class ImplementSerializationConstructorsFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2229Id); 

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var symbol = model.GetDeclaredSymbol(node, context.CancellationToken);

            if (symbol == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.Single();

            // There was no constructor and so the diagnostic was on the type. Generate a serlialization ctor.
            if (symbol.Kind == SymbolKind.NamedType)
            {
                context.RegisterCodeFix(new MyCodeAction(FxCopFixersResources.ImplementSerializationConstructor,
                     async ct => await GenerateConstructor(context.Document, node, symbol, ct).ConfigureAwait(false)),
                diagnostic);
            }
            // There is a serialization constructor but with incorrect accessibility. Set that right.
            else if (symbol.Kind == SymbolKind.Method)
            {
                context.RegisterCodeFix(new MyCodeAction(FxCopFixersResources.ImplementSerializationConstructor,
                     async ct => await SetAccessibility(context.Document, node, symbol, ct).ConfigureAwait(false)),
                diagnostic);
            }

            return;
        }

        private async Task<Document> GenerateConstructor(Document document, SyntaxNode node, ISymbol symbol, CancellationToken cancellationToken)
        {
            var editor = SymbolEditor.Create(document);
            var typeSymbol = symbol as INamedTypeSymbol;

            await editor.EditOneDeclarationAsync(typeSymbol, node.GetLocation(), (docEditor, declaration) =>
            {
                var generator = docEditor.Generator;
                var throwStatement = generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException")));
                var ctorDecl = generator.ConstructorDeclaration(
                                    typeSymbol.Name,
                                    new[]
                                    {
                                            generator.ParameterDeclaration("serializationInfo", generator.TypeExpression(WellKnownTypes.SerializationInfo(docEditor.SemanticModel.Compilation))),
                                            generator.ParameterDeclaration("streamingContext", generator.TypeExpression(WellKnownTypes.StreamingContext(docEditor.SemanticModel.Compilation)))
                                    },
                                    typeSymbol.IsSealed ? Accessibility.Private : Accessibility.Protected,
                                    statements: new[] { throwStatement });

                docEditor.AddMember(declaration, ctorDecl);
            }, cancellationToken);

            return editor.GetChangedDocuments().First();
        }

        private async Task<Document> SetAccessibility(Document document, SyntaxNode node, ISymbol symbol, CancellationToken cancellationToken)
        {
            var editor = SymbolEditor.Create(document);
            var methodSymbol = symbol as IMethodSymbol;

            // This would be constructor and can have only one definition.
            Debug.Assert(methodSymbol.IsConstructor() && methodSymbol.DeclaringSyntaxReferences.Count() == 1);
            await editor.EditOneDeclarationAsync(methodSymbol, (docEditor, declaration) => 
            {
                var newAccessibility = methodSymbol.ContainingType.IsSealed ? Accessibility.Private : Accessibility.Protected;
                docEditor.SetAccessibility(declaration, newAccessibility);
            }, cancellationToken);

            return editor.GetChangedDocuments().First();
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
