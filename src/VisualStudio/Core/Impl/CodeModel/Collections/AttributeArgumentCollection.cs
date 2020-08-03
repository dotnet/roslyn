// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public sealed class AttributeArgumentCollection : AbstractCodeElementCollection
    {
        internal static EnvDTE.CodeElements Create(
            CodeModelState state,
            CodeAttribute parent)
        {
            var collection = new AttributeArgumentCollection(state, parent);
            return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
        }

        private AttributeArgumentCollection(
            CodeModelState state,
            CodeAttribute parent)
            : base(state, parent)
        {
        }

        private CodeAttribute ParentAttribute
        {
            get { return (CodeAttribute)Parent; }
        }

        private SyntaxNode LookupNode()
            => this.ParentAttribute.LookupNode();

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            var attributeArgumentNodes = CodeModelService.GetAttributeArgumentNodes(node);
            if (index >= 0 && index < attributeArgumentNodes.Count())
            {
                element = (EnvDTE.CodeElement)CodeAttributeArgument.Create(this.State, this.ParentAttribute, index);
                return true;
            }

            element = null;
            return false;
        }

        protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();
            var currentIndex = 0;

            foreach (var child in CodeModelService.GetAttributeArgumentNodes(node))
            {
                var childName = CodeModelService.GetName(child);
                if (childName == name)
                {
                    element = (EnvDTE.CodeElement)CodeAttributeArgument.Create(this.State, this.ParentAttribute, currentIndex);
                    return true;
                }

                currentIndex++;
            }

            element = null;
            return false;
        }

        public override int Count
        {
            get
            {
                var node = LookupNode();
                return CodeModelService.GetAttributeArgumentNodes(node).Count();
            }
        }
    }
}
