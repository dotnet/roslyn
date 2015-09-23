// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal abstract partial class RemoveSuppressionCodeAction
        {
            private sealed class AttributeRemoveAction : RemoveSuppressionCodeAction
            {
                private readonly AttributeData _attribute;

                public static AttributeRemoveAction Create(
                    AttributeData attribute,
                    Document document,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer)
                {
                    return new AttributeRemoveAction(attribute, document, diagnostic, fixer);
                }

                private AttributeRemoveAction(
                    AttributeData attribute,
                    Document document,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer,
                    bool forFixMultipleContext = false)
                    : base(document, diagnostic, fixer, forFixMultipleContext)
                {
                    _attribute = attribute;
                }

                public override RemoveSuppressionCodeAction CloneForFixMultipleContext()
                {
                    return new AttributeRemoveAction(_attribute, _document, _diagnostic, Fixer, forFixMultipleContext: true);
                }

                public override SyntaxTree SyntaxTreeToModify => _attribute.ApplicationSyntaxReference.SyntaxTree;

                public async Task<SyntaxNode> GetAttributeToRemoveAsync(CancellationToken cancellationToken)
                {
                    var attributeNode = await _attribute.ApplicationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    return Fixer.IsSingleAttributeInAttributeList(attributeNode) ?
                        attributeNode.Parent :
                        attributeNode;
                }

                protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                {
                    var attributeNode = await GetAttributeToRemoveAsync(cancellationToken).ConfigureAwait(false);
                    var document = GetDocumentWithAttribute(attributeNode);
                    if (document == null)
                    {
                        return _document;
                    }

                    var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                    editor.RemoveNode(attributeNode);
                    var newProject = editor.GetChangedDocument().Project;
                    return newProject.GetDocument(_document.Id);
                }

                private Document GetDocumentWithAttribute(SyntaxNode attributeNode)
                {
                    var tree = attributeNode.SyntaxTree;
                    if (_document.FilePath == tree.FilePath)
                    {
                        return _document;
                    }

                    return _document.Project.GetDocument(tree);
                }
            }
        }
    }
}