// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    public sealed class AttributeCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            AbstractCodeElement parent)
        {
            var collection = new AttributeCollection(state, parent);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private AttributeCollection(
            CodeModelState state,
            AbstractCodeElement parent)
            : base(state, parent)
        {
        }

        private AbstractCodeElement ParentElement
        {
            get { return (AbstractCodeElement)Parent; }
        }

        private FileCodeModel FileCodeModel
        {
            get { return this.ParentElement.FileCodeModel; }
        }

        private SyntaxNode LookupNode()
            => this.ParentElement.LookupNode();

        private EnvDTE.CodeElement CreateCodeAttribute(SyntaxNode node, SyntaxNode parentNode)
        {
            CodeModelService.GetAttributeNameAndOrdinal(parentNode, node, out var name, out var ordinal);

            return (EnvDTE.CodeElement)CodeAttribute.Create(this.State, this.FileCodeModel, this.ParentElement, name, ordinal);
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            var attributeNodes = CodeModelService.GetAttributeNodes(node);
            if (index >= 0 && index < attributeNodes.Count())
            {
                var child = attributeNodes.ElementAt(index);

                element = CreateCodeAttribute(child, node);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            foreach (var child in CodeModelService.GetAttributeNodes(node))
            {
                CodeModelService.GetAttributeNameAndOrdinal(node, child, out var childName, out var ordinal);
                if (childName == name)
                {
                    element = (EnvDTE.CodeElement)CodeAttribute.Create(State, FileCodeModel, this.ParentElement, childName, ordinal);
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
                return CodeModelService.GetAttributeNodes(ParentElement.LookupNode()).Count();
            }
        }
    }
}
