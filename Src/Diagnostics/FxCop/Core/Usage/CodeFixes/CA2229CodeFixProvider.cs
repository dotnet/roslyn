// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [ExportCodeFixProvider("CA2229 CodeFix provider", LanguageNames.CSharp), Shared]
    public sealed class CA2229CodeFixProvider : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2229Id);
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return FxCopFixersResources.ImplementSerializationConstructor;
        }

        internal async override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var symbol = model.GetDeclaredSymbol(nodeToFix);
            var generator = SyntaxGenerator.GetGenerator(document);

            // There was no constructor and so the diagnostic was on the type. Generate a serlialization ctor.
            if (symbol.Kind == SymbolKind.NamedType)
            {
                var typeSymbol = symbol as INamedTypeSymbol;
                var throwStatement = generator.ThrowStatement(generator.ObjectCreationExpression(generator.DottedName("System.NotImplementedException")));

                var ctorDecl = generator.ConstructorDeclaration(
                    typeSymbol.Name,
                    new[]
                    {
                        generator.ParameterDeclaration("serializationInfo", generator.TypeExpression(WellKnownTypes.SerializationInfo(model.Compilation))),
                        generator.ParameterDeclaration("streamingContext", generator.TypeExpression(WellKnownTypes.StreamingContext(model.Compilation)))
                    },
                    typeSymbol.IsSealed ? Accessibility.Private : Accessibility.Protected,
                    statements: new[] { throwStatement });

                var editor = new SymbolEditor(document.Project.Solution);
                await editor.EditOneDeclarationAsync(typeSymbol, nodeToFix.GetLocation(), (d, g) => g.AddMembers(d, new[] { ctorDecl }), cancellationToken);
                return editor.GetChangedDocuments().First();
            }
            else if (symbol.Kind == SymbolKind.Method)
            {
                // There is a serialization constructor but with incorrect accessibility. Set that right.
                var methodSymbol = symbol as IMethodSymbol;

                // This would be constructor and can have only one definition.
                Debug.Assert(methodSymbol.IsConstructor() && methodSymbol.DeclaringSyntaxReferences.Count() == 1);
                var declaration = await methodSymbol.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);

                var newAccessibility = methodSymbol.ContainingType.IsSealed ? Accessibility.Private : Accessibility.Protected;
                var newDecl = generator.WithAccessibility(declaration, newAccessibility);
                return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDecl));
            }

            return document;
        }
    }
}
