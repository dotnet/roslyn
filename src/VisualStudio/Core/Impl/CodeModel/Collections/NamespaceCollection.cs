// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class NamespaceCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey)
        {
            var collection = new NamespaceCollection(state, parent, fileCodeModel, nodeKey);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private readonly ComHandle<EnvDTE.FileCodeModel, FileCodeModel> _fileCodeModel;
        private readonly SyntaxNodeKey _nodeKey;

        private NamespaceCollection(
            CodeModelState state,
            object parent,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey)
            : base(state, parent)
        {
            Debug.Assert(fileCodeModel != null);

            _fileCodeModel = new ComHandle<EnvDTE.FileCodeModel, FileCodeModel>(fileCodeModel);
            _nodeKey = nodeKey;
        }

        private FileCodeModel FileCodeModel
        {
            get { return _fileCodeModel.Object; }
        }

        private bool IsRootNamespace
        {
            get { return _nodeKey == SyntaxNodeKey.Empty; }
        }

        private SyntaxNode LookupNode()
        {
            if (!IsRootNamespace)
            {
                return FileCodeModel.LookupNode(_nodeKey);
            }
            else
            {
                return FileCodeModel.GetSyntaxRoot();
            }
        }

        private EnvDTE.CodeElement CreateCodeOptionsStatement(SyntaxNode node, SyntaxNode parentNode)
        {
            CodeModelService.GetOptionNameAndOrdinal(parentNode, node, out var name, out var ordinal);

            return CodeOptionsStatement.Create(this.State, this.FileCodeModel, name, ordinal);
        }

        private EnvDTE.CodeElement CreateCodeImport(SyntaxNode node, AbstractCodeElement parentElement)
        {
            var name = CodeModelService.GetImportNamespaceOrType(node);

            return CodeImport.Create(this.State, this.FileCodeModel, parentElement, name);
        }

        private EnvDTE.CodeElement CreateCodeAttribute(SyntaxNode node, SyntaxNode parentNode, AbstractCodeElement parentElement)
        {
            CodeModelService.GetAttributeNameAndOrdinal(parentNode, node, out var name, out var ordinal);

            return (EnvDTE.CodeElement)CodeAttribute.Create(this.State, this.FileCodeModel, parentElement, name, ordinal);
        }

        internal override Snapshot CreateSnapshot()
        {
            var node = LookupNode();
            var parentElement = !this.IsRootNamespace
                ? (AbstractCodeElement)this.Parent
                : null;

            var nodesBuilder = ArrayBuilder<SyntaxNode>.GetInstance();
            nodesBuilder.AddRange(CodeModelService.GetOptionNodes(node));
            nodesBuilder.AddRange(CodeModelService.GetImportNodes(node));
            nodesBuilder.AddRange(CodeModelService.GetAttributeNodes(node));
            nodesBuilder.AddRange(CodeModelService.GetLogicalSupportedMemberNodes(node));

            return new NodeSnapshot(this.State, _fileCodeModel, node, parentElement,
                nodesBuilder.ToImmutableAndFree());
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();
            var parentElement = !this.IsRootNamespace
                ? (AbstractCodeElement)this.Parent
                : null;

            var currentIndex = 0;

            // Option statements
            var optionNodes = CodeModelService.GetOptionNodes(node);
            var optionNodeCount = optionNodes.Count();
            if (index < currentIndex + optionNodeCount)
            {
                var child = optionNodes.ElementAt(index - currentIndex);
                element = CreateCodeOptionsStatement(child, node);
                return true;
            }

            currentIndex += optionNodeCount;

            // Imports/using statements
            var importNodes = CodeModelService.GetImportNodes(node);
            var importNodeCount = importNodes.Count();
            if (index < currentIndex + importNodeCount)
            {
                var child = importNodes.ElementAt(index - currentIndex);
                element = CreateCodeImport(child, parentElement);
                return true;
            }

            currentIndex += importNodeCount;

            // Attributes
            var attributeNodes = CodeModelService.GetAttributeNodes(node);
            var attributeNodeCount = attributeNodes.Count();
            if (index < currentIndex + attributeNodeCount)
            {
                var child = attributeNodes.ElementAt(index - currentIndex);
                element = CreateCodeAttribute(child, node, parentElement);
                return true;
            }

            currentIndex += attributeNodeCount;

            // Members
            var memberNodes = CodeModelService.GetLogicalSupportedMemberNodes(node);
            var memberNodeCount = memberNodes.Count();
            if (index < currentIndex + memberNodeCount)
            {
                var child = memberNodes.ElementAt(index - currentIndex);
                element = FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(child);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();
            var parentElement = !IsRootNamespace
                ? (AbstractCodeElement)Parent
                : null;

            // Option statements
            foreach (var child in CodeModelService.GetOptionNodes(node))
            {
                CodeModelService.GetOptionNameAndOrdinal(node, child, out var childName, out var ordinal);
                if (childName == name)
                {
                    element = CodeOptionsStatement.Create(State, FileCodeModel, childName, ordinal);
                    return true;
                }
            }

            // Imports/using statements
            foreach (var child in CodeModelService.GetImportNodes(node))
            {
                var childName = CodeModelService.GetImportNamespaceOrType(child);
                if (childName == name)
                {
                    element = CodeImport.Create(State, FileCodeModel, parentElement, childName);
                    return true;
                }
            }

            // Attributes
            foreach (var child in CodeModelService.GetAttributeNodes(node))
            {
                CodeModelService.GetAttributeNameAndOrdinal(node, child, out var childName, out var ordinal);
                if (childName == name)
                {
                    element = (EnvDTE.CodeElement)CodeAttribute.Create(State, FileCodeModel, parentElement, childName, ordinal);
                    return true;
                }
            }

            // Members
            foreach (var child in CodeModelService.GetLogicalSupportedMemberNodes(node))
            {
                var childName = CodeModelService.GetName(child);
                if (childName == name)
                {
                    element = FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(child);
                    return true;
                }
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get
            {
                var node = LookupNode();
                return
                    CodeModelService.GetOptionNodes(node).Count() +
                    CodeModelService.GetImportNodes(node).Count() +
                    CodeModelService.GetAttributeNodes(node).Count() +
                    CodeModelService.GetLogicalSupportedMemberNodes(node).Count();
            }
        }
    }
}
