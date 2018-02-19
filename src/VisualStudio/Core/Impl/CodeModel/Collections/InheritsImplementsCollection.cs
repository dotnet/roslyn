﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public sealed class InheritsImplementsCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey)
        {
            var collection = new InheritsImplementsCollection(state, parent, fileCodeModel, nodeKey);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private ComHandle<EnvDTE.FileCodeModel, FileCodeModel> _fileCodeModel;
        private SyntaxNodeKey _nodeKey;

        private InheritsImplementsCollection(
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

        private SyntaxNode LookupNode()
        {
            return FileCodeModel.LookupNode(_nodeKey);
        }

        internal override Snapshot CreateSnapshot()
        {
            var node = LookupNode();
            var parentElement = (AbstractCodeElement)this.Parent;

            var nodesBuilder = ArrayBuilder<SyntaxNode>.GetInstance();
            nodesBuilder.AddRange(CodeModelService.GetInheritsNodes(node));
            nodesBuilder.AddRange(CodeModelService.GetImplementsNodes(node));

            return new NodeSnapshot(this.State, _fileCodeModel, node, parentElement,
                nodesBuilder.ToImmutableAndFree());
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            int currentIndex = 0;

            // Inherits statements
            var inheritsNodes = CodeModelService.GetInheritsNodes(node);
            var inheritsNodeCount = inheritsNodes.Count();
            if (index < currentIndex + inheritsNodeCount)
            {
                var child = inheritsNodes.ElementAt(index - currentIndex);
                element = FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(child);
                return true;
            }

            currentIndex += inheritsNodeCount;

            // Implements statements
            var implementsNodes = CodeModelService.GetImplementsNodes(node);
            var implementsNodeCount = implementsNodes.Count();
            if (index < currentIndex + implementsNodeCount)
            {
                var child = implementsNodes.ElementAt(index - currentIndex);
                element = FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(child);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            // Inherits statements
            foreach (var child in CodeModelService.GetInheritsNodes(node))
            {
                CodeModelService.GetInheritsNamespaceAndOrdinal(node, child, out var childName, out var ordinal);
                if (childName == name)
                {
                    element = FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(child);
                    return true;
                }
            }

            // Implements statements
            foreach (var child in CodeModelService.GetImplementsNodes(node))
            {
                CodeModelService.GetImplementsNamespaceAndOrdinal(node, child, out var childName, out var ordinal);
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
                    CodeModelService.GetInheritsNodes(node).Count() +
                    CodeModelService.GetImplementsNodes(node).Count();
            }
        }
    }
}
