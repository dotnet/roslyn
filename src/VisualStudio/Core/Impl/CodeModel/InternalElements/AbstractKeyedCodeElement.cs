// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    /// <summary>
    /// This is the base class of all code elements identified by a SyntaxNodeKey.
    /// </summary>
    public abstract class AbstractKeyedCodeElement : AbstractCodeElement
    {
        private SyntaxNodeKey _nodeKey;
        private readonly string _name;

        internal AbstractKeyedCodeElement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKind)
        {
            _nodeKey = nodeKey;
            _name = null;
        }

        // This constructor is called for "unknown" code elements.
        internal AbstractKeyedCodeElement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind)
        {
            _nodeKey = new SyntaxNodeKey(name, -1);
            _name = name;
        }

        internal SyntaxNodeKey NodeKey
        {
            get { return _nodeKey; }
        }

        internal bool IsUnknown
        {
            get { return _nodeKey.Ordinal == -1; }
        }

        internal override SyntaxNode LookupNode()
        {
            return CodeModelService.LookupNode(_nodeKey, GetSyntaxTree());
        }

        internal override bool TryLookupNode(out SyntaxNode node)
        {
            return CodeModelService.TryLookupNode(_nodeKey, GetSyntaxTree(), out node);
        }

        /// <summary>
        /// This function re-acquires the key for this code element using the given syntax path.
        /// </summary>
        internal void ReacquireNodeKey(SyntaxPath syntaxPath, CancellationToken cancellationToken)
        {
            Debug.Assert(syntaxPath != null);

            SyntaxNode node;
            if (!syntaxPath.TryResolve(GetSyntaxTree(), cancellationToken, out node))
            {
                throw Exceptions.ThrowEFail();
            }

            var newNodeKey = CodeModelService.GetNodeKey(node);

            FileCodeModel.UpdateCodeElementNodeKey(this, _nodeKey, newNodeKey);

            _nodeKey = newNodeKey;
        }

        protected void UpdateNodeAndReacquireNodeKey<T>(Action<SyntaxNode, T> updater, T value, bool trackKinds = true)
        {
            FileCodeModel.EnsureEditor(() =>
            {
                // Sometimes, changing an element can result in needing to update its node key.

                var node = LookupNode();
                var nodePath = new SyntaxPath(node, trackKinds);

                updater(node, value);

                ReacquireNodeKey(nodePath, CancellationToken.None);
            });
        }

        protected override Document DeleteCore(Document document)
        {
            var result = base.DeleteCore(document);

            FileCodeModel.OnCodeElementDeleted(_nodeKey);

            return result;
        }

        protected override string GetName()
        {
            if (IsUnknown)
            {
                return _name;
            }

            return base.GetName();
        }

        protected override void SetName(string value)
        {
            FileCodeModel.EnsureEditor(() =>
            {
                var nodeKeyValidation = new NodeKeyValidation();
                nodeKeyValidation.AddFileCodeModel(this.FileCodeModel);

                var node = LookupNode();

                FileCodeModel.UpdateName(node, value);

                nodeKeyValidation.RestoreKeys();
            });
        }
    }
}
