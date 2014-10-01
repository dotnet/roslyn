// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
    [ExportCodeFixProvider("CA2229 CodeFix provider", LanguageNames.CSharp)]
    public sealed class CA2229CodeFixProvider : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2229Id);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.ImplementSerializationConstructor;
        }

        internal async override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            var syntaxFactoryService = document.GetLanguageService<SyntaxGenerator>();

            var symbol = model.GetDeclaredSymbol(nodeToFix);

            // There was no constructor and so the diagnostic was on the type. Generate a serlialization ctor.
            if (symbol.Kind == SymbolKind.NamedType)
            {
                var typeSymbol = symbol as INamedTypeSymbol;
                var throwStatement = syntaxFactoryService.CreateThrowNotImplementedStatementBlock(model.Compilation);

                var parameters = ImmutableArray.Create(
                    CodeGenerationSymbolFactory.CreateParameterSymbol(WellKnownTypes.SerializationInfo(model.Compilation), "serializationInfo"),
                    CodeGenerationSymbolFactory.CreateParameterSymbol(WellKnownTypes.StreamingContext(model.Compilation), "streamingContext"));

                var ctorSymbol = CodeGenerationSymbolFactory.CreateConstructorSymbol(null,
                                                                                     typeSymbol.IsSealed ? Accessibility.Private : Accessibility.Protected,
                                                                                     new SymbolModifiers(),
                                                                                     typeSymbol.Name,
                                                                                     parameters,
                                                                                     throwStatement);

                return await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution, typeSymbol, ctorSymbol).ConfigureAwait(false);
            }
            else if (symbol.Kind == SymbolKind.Method)
            {
                // There is a serialization constructor but with incorrect accessibility. Set that right.
                var methodSymbol = symbol as IMethodSymbol;

                // This would be constructor and can have only one definition.
                Debug.Assert(methodSymbol.IsConstructor() && methodSymbol.DeclaringSyntaxReferences.Count() == 1);
                var declaration = await methodSymbol.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken);

                var newAccessibility = methodSymbol.ContainingType.IsSealed ? Accessibility.Private : Accessibility.Protected;
                var newDecl = CodeGenerator.UpdateDeclarationAccessibility(declaration, document.Project.Solution.Workspace, newAccessibility, cancellationToken: cancellationToken);
                return document.WithSyntaxRoot(root.ReplaceNode(declaration, newDecl));
            }

            return document;
        }
    }
}
