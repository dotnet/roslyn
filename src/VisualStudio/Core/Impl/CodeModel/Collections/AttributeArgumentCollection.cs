// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return this.ParentAttribute.LookupNode();
        }

        protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
        {
            var node = LookupNode();

            var attributeArgumentNodes = CodeModelService.GetAttributeArgumentNodes(node);
            if (index >= 0 && index < attributeArgumentNodes.Count())
            {
                var child = attributeArgumentNodes.ElementAt(index);
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
