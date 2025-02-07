// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private readonly string _name;

        internal AbstractKeyedCodeElement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKind)
        {
            NodeKey = nodeKey;
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
            NodeKey = new SyntaxNodeKey(name, -1);
            _name = name;
        }

        internal SyntaxNodeKey NodeKey { get; private set; }

        internal bool IsUnknown
        {
            get { return NodeKey.Ordinal == -1; }
        }

        internal override SyntaxNode LookupNode()
            => CodeModelService.LookupNode(NodeKey, GetSyntaxTree());

        internal override bool TryLookupNode(out SyntaxNode node)
            => CodeModelService.TryLookupNode(NodeKey, GetSyntaxTree(), out node);

        /// <summary>
        /// This function re-acquires the key for this code element using the given syntax path.
        /// </summary>
        internal void ReacquireNodeKey(SyntaxPath syntaxPath, CancellationToken cancellationToken)
        {
            Debug.Assert(syntaxPath != null);
            if (!syntaxPath.TryResolve(GetSyntaxTree(), cancellationToken, out SyntaxNode node))
            {
                throw Exceptions.ThrowEFail();
            }

            var newNodeKey = CodeModelService.GetNodeKey(node);

            FileCodeModel.UpdateCodeElementNodeKey(this, NodeKey, newNodeKey);

            NodeKey = newNodeKey;
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

            FileCodeModel.OnCodeElementDeleted(NodeKey);

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
