// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    public abstract class CA2235CodeFixProviderBase : MultipleCodeFixProviderBase
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2235Id); }
        }

        protected abstract SyntaxNode GetFieldDeclarationNode(SyntaxNode node);

        internal override Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var actions = ImmutableArray.CreateBuilder<CodeAction>();

            // Fix 1: Add a NonSerialized attribute to the field
            var fieldNode = GetFieldDeclarationNode(nodeToFix);
            if (fieldNode != null)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var codeAction = new MyDocumentCodeAction(FxCopFixersResources.AddNonSerializedAttribute,
                                                          async ct => await AddNonSerializedAttribute(document, model, root, fieldNode, generator).ConfigureAwait(false));
                actions.Add(codeAction);

                // Fix 2: If the type of the field is defined in source, then add the serializable attribute to the type.
                var fieldSymbol = model.GetDeclaredSymbol(nodeToFix, cancellationToken) as IFieldSymbol;
                var type = fieldSymbol.Type;
                if (type.Locations.Any(l => l.IsInSource))
                {
                    var typeCodeAction = new MySolutionCodeAction(FxCopFixersResources.AddSerializableAttribute,
                                                                  async ct => await AddSerializableAttributeToType(document, model, generator, type, cancellationToken).ConfigureAwait(false));

                    actions.Add(typeCodeAction);
                }
            }

            return Task.FromResult<IEnumerable<CodeAction>>(actions.ToImmutable());
        }

        private static async Task<Solution> AddSerializableAttributeToType(Document document, SemanticModel model, SyntaxGenerator generator, ITypeSymbol type, CancellationToken cancellationToken)
        {
            var typeDeclNode = type.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);

            var serializableAttr = generator.Attribute(generator.TypeExpression(WellKnownTypes.SerializableAttribute(model.Compilation)));
            var newTypeDeclNode = generator.AddAttributes(typeDeclNode, serializableAttr);
            var documentContainingNode = document.Project.Solution.GetDocument(typeDeclNode.SyntaxTree);
            var docRoot = await documentContainingNode.GetSyntaxRootAsync(cancellationToken);
            var newDocumentContainingNode = documentContainingNode.WithSyntaxRoot(docRoot.ReplaceNode(typeDeclNode, newTypeDeclNode));
            return newDocumentContainingNode.Project.Solution;
        }

        private Task<Document> AddNonSerializedAttribute(Document document, SemanticModel model, SyntaxNode root, SyntaxNode fieldNode, SyntaxGenerator generator)
        {
            var attr = generator.Attribute(generator.TypeExpression(WellKnownTypes.NonSerializedAttribute(model.Compilation)));
            var newNode = generator.AddAttributes(fieldNode, attr);
            var newDocument = document.WithSyntaxRoot(root.ReplaceNode(fieldNode, newNode));
            return Task.FromResult(newDocument);
        }

        private class MyDocumentCodeAction : DocumentChangeAction
        {
            public MyDocumentCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }

        private class MySolutionCodeAction : SolutionChangeAction
        {
            public MySolutionCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution) :
                base(title, createChangedSolution)
            {
            }
        }
    }
}
