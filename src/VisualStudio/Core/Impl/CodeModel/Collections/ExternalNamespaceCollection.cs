// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(ICodeElements))]
    public sealed class ExternalNamespaceCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            object parent,
            ProjectId projectId,
            INamespaceSymbol namespaceSymbol)
        {
            var collection = new ExternalNamespaceCollection(state, parent, projectId, namespaceSymbol);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private readonly ProjectId _projectId;
        private readonly SymbolKey _namespaceSymbolId;
        private ImmutableArray<EnvDTE.CodeElement> _children;

        internal ExternalNamespaceCollection(CodeModelState state, object parent, ProjectId projectId, INamespaceSymbol namespaceSymbol)
            : base(state, parent)
        {
            _projectId = projectId;
            _namespaceSymbolId = namespaceSymbol.GetSymbolKey();
        }

        private ImmutableArray<EnvDTE.CodeElement> GetChildren()
        {
            if (_children == null)
            {
                var childrenBuilder = ArrayBuilder<EnvDTE.CodeElement>.GetInstance();

                foreach (var child in ExternalNamespaceEnumerator.ChildrenOfNamespace(this.State, _projectId, _namespaceSymbolId))
                {
                    childrenBuilder.Add(child);
                }

                _children = childrenBuilder.ToImmutableAndFree();
            }

            return _children;
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var children = GetChildren();
            if (index < children.Length)
            {
                element = children[index];
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var children = GetChildren();
            var index = children.IndexOf(e => e.Name == name);

            if (index < children.Length)
            {
                element = children[index];
                return true;
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get { return GetChildren().Length; }
        }

        public override System.Collections.IEnumerator GetEnumerator()
            => ExternalNamespaceEnumerator.Create(this.State, _projectId, _namespaceSymbolId);
    }
}
