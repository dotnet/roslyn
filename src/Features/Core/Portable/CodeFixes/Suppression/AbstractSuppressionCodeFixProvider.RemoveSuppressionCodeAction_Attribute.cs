// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal abstract partial class RemoveSuppressionCodeAction
        {
            /// <summary>
            /// Code action to remove suppress message attributes for remove suppression.
            /// </summary>
            private sealed class AttributeRemoveAction : RemoveSuppressionCodeAction
            {
                private readonly Project _project;
                private readonly AttributeData _attribute;

                public static AttributeRemoveAction Create(
                    AttributeData attribute,
                    Project project,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer)
                {
                    return new AttributeRemoveAction(attribute, project, diagnostic, fixer);
                }

                private AttributeRemoveAction(
                    AttributeData attribute,
                    Project project,
                    Diagnostic diagnostic,
                    AbstractSuppressionCodeFixProvider fixer,
                    bool forFixMultipleContext = false)
                    : base(diagnostic, fixer, forFixMultipleContext)
                {
                    _project = project;
                    _attribute = attribute;
                }

                public override RemoveSuppressionCodeAction CloneForFixMultipleContext()
                {
                    return new AttributeRemoveAction(_attribute, _project, _diagnostic, Fixer, forFixMultipleContext: true);
                }

                public override SyntaxTree SyntaxTreeToModify => _attribute.ApplicationSyntaxReference.SyntaxTree;

                public async Task<SyntaxNode> GetAttributeToRemoveAsync(CancellationToken cancellationToken)
                {
                    var attributeNode = await _attribute.ApplicationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    return Fixer.IsSingleAttributeInAttributeList(attributeNode) ?
                        attributeNode.Parent :
                        attributeNode;
                }

                protected async override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
                {
                    var attributeNode = await GetAttributeToRemoveAsync(cancellationToken).ConfigureAwait(false);
                    var documentWithAttribute = _project.GetDocument(attributeNode.SyntaxTree);
                    if (documentWithAttribute == null)
                    {
                        return _project.Solution;
                    }

                    var editor = await DocumentEditor.CreateAsync(documentWithAttribute, cancellationToken).ConfigureAwait(false);
                    editor.RemoveNode(attributeNode);
                    return editor.GetChangedDocument().Project.Solution;
                }
            }
        }
    }
}
