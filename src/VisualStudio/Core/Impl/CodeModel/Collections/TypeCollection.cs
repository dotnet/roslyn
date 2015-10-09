// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class TypeCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey)
        {
            var collection = new TypeCollection(state, parent, fileCodeModel, nodeKey);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private ComHandle<EnvDTE.FileCodeModel, FileCodeModel> _fileCodeModel;
        private SyntaxNodeKey _nodeKey;

        private TypeCollection(
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

            var nodesBuilder = ImmutableArray.CreateBuilder<SyntaxNode>();
            nodesBuilder.AddRange(CodeModelService.GetLogicalSupportedMemberNodes(node));

            return new NodeSnapshot(this.State, _fileCodeModel, node, parentElement, nodesBuilder.ToImmutable());
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            var memberNodes = CodeModelService.GetLogicalSupportedMemberNodes(node);
            if (index >= 0 && index < memberNodes.Count())
            {
                var child = memberNodes.ElementAt(index);
                element = FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(child);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

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
                return CodeModelService.GetLogicalSupportedMemberNodes(node).Count();
            }
        }
    }
}
