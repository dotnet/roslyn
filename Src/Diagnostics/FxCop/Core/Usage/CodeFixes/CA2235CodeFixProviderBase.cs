// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    public abstract class CA2235CodeFixProviderBase : MultipleCodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(SerializationRulesDiagnosticAnalyzer.RuleCA2235Id);
        }

        protected abstract SyntaxNode GetFieldDeclarationNode(SyntaxNode node);

        internal async override Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            IEnumerable<CodeAction> actions = null;

            // Fix 1: Add a NonSerialized attribute to the field
            var fieldNode = GetFieldDeclarationNode(nodeToFix);
            if (fieldNode != null)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var attr = generator.Attribute(generator.TypeExpression(WellKnownTypes.NonSerializedAttribute(model.Compilation)));
                var newNode = generator.AddAttributes(fieldNode, attr);
                var newDocument = document.WithSyntaxRoot(root.ReplaceNode(fieldNode, newNode));
                var codeAction = new MyDocumentCodeAction(FxCopFixersResources.AddNonSerializedAttribute, newDocument);
                actions = SpecializedCollections.SingletonEnumerable(codeAction);

                // Fix 2: If the type of the field is defined in source, then add the serializable attribute to the type.
                var fieldSymbol = model.GetDeclaredSymbol(nodeToFix) as IFieldSymbol;
                var type = fieldSymbol.Type;
                if (type.Locations.Any(l => l.IsInSource))
                {
                    var typeDeclNode = type.DeclaringSyntaxReferences.First().GetSyntax();

                    var serializableAttr = generator.Attribute(generator.TypeExpression(WellKnownTypes.SerializableAttribute(model.Compilation)));
                    var newTypeDeclNode = generator.AddAttributes(typeDeclNode, serializableAttr);
                    var documentContainingNode = document.Project.Solution.GetDocument(typeDeclNode.SyntaxTree);
                    var docRoot = await documentContainingNode.GetSyntaxRootAsync(cancellationToken);
                    var newDocumentContainingNode = documentContainingNode.WithSyntaxRoot(docRoot.ReplaceNode(typeDeclNode, newTypeDeclNode));
                    var typeCodeAction = new MySolutionCodeAction(FxCopFixersResources.AddSerializableAttribute, newDocumentContainingNode.Project.Solution);

                    actions = actions.Concat(typeCodeAction);
                }
            }

            return actions;
        }

        private class MyDocumentCodeAction : CodeAction.DocumentChangeAction
        {
            public MyDocumentCodeAction(string title, Document newDocument) :
                base(title, c => Task.FromResult(newDocument))
            {
            }
        }

        private class MySolutionCodeAction : CodeAction.SolutionChangeAction
        {
            public MySolutionCodeAction(string title, Solution newSolution) :
                base(title, c => Task.FromResult(newSolution))
            {
            }
        }
    }
}
