// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    internal class NodeSnapshot : Snapshot
    {
        private readonly CodeModelState _state;
        private readonly ComHandle<EnvDTE.FileCodeModel, FileCodeModel> _fileCodeModel;
        private readonly SyntaxNode _parentNode;
        private readonly AbstractCodeElement _parentElement;
        private readonly ImmutableArray<SyntaxNode> _nodes;

        public NodeSnapshot(
            CodeModelState state,
            ComHandle<EnvDTE.FileCodeModel, FileCodeModel> fileCodeModel,
            SyntaxNode parentNode,
            AbstractCodeElement parentElement,
            ImmutableArray<SyntaxNode> nodes)
        {
            _state = state;
            _fileCodeModel = fileCodeModel;
            _parentNode = parentNode;
            _parentElement = parentElement;
            _nodes = nodes;
        }

        private ICodeModelService CodeModelService
        {
            get { return _state.CodeModelService; }
        }

        private FileCodeModel FileCodeModel
        {
            get { return _fileCodeModel.Object; }
        }

        private EnvDTE.CodeElement CreateCodeOptionsStatement(SyntaxNode node)
        {
            this.CodeModelService.GetOptionNameAndOrdinal(_parentNode, node, out var name, out var ordinal);

            return CodeOptionsStatement.Create(_state, this.FileCodeModel, name, ordinal);
        }

        private EnvDTE.CodeElement CreateCodeImport(SyntaxNode node)
        {
            var name = this.CodeModelService.GetImportNamespaceOrType(node);

            return CodeImport.Create(_state, this.FileCodeModel, _parentElement, name);
        }

        private EnvDTE.CodeElement CreateCodeAttribute(SyntaxNode node)
        {
            this.CodeModelService.GetAttributeNameAndOrdinal(_parentNode, node, out var name, out var ordinal);

            return (EnvDTE.CodeElement)CodeAttribute.Create(_state, this.FileCodeModel, _parentElement, name, ordinal);
        }

        private EnvDTE.CodeElement CreateCodeParameter(SyntaxNode node)
        {
            Debug.Assert(_parentElement is AbstractCodeMember, "Parameters should always have an associated member!");

            var name = this.CodeModelService.GetParameterName(node);

            return (EnvDTE.CodeElement)CodeParameter.Create(_state, (AbstractCodeMember)_parentElement, name);
        }

        public override int Count
        {
            get { return _nodes.Length; }
        }

        public override EnvDTE.CodeElement this[int index]
        {
            get
            {
                if (index < 0 || index >= _nodes.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var node = _nodes[index];

                if (this.CodeModelService.IsOptionNode(node))
                {
                    return CreateCodeOptionsStatement(node);
                }
                else if (this.CodeModelService.IsImportNode(node))
                {
                    return CreateCodeImport(node);
                }
                else if (this.CodeModelService.IsAttributeNode(node))
                {
                    return CreateCodeAttribute(node);
                }
                else if (this.CodeModelService.IsParameterNode(node))
                {
                    return CreateCodeParameter(node);
                }

                // The node must be something that the FileCodeModel can create.
                return this.FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(node);
            }
        }
    }
}
