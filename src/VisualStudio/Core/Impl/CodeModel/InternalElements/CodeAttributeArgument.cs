// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeAttributeArgument))]
    public sealed class CodeAttributeArgument : AbstractCodeElement, EnvDTE80.CodeAttributeArgument
    {
        internal static EnvDTE80.CodeAttributeArgument Create(CodeModelState state, CodeAttribute parent, int index)
        {
            Debug.Assert(parent != null);
            Debug.Assert(index >= 0);

            var newElement = new CodeAttributeArgument(state, parent, index);
            return (EnvDTE80.CodeAttributeArgument)ComAggregate.CreateAggregatedObject(newElement);
        }

        private readonly ParentHandle<CodeAttribute> _parentHandle;
        private readonly int _index;

        private CodeAttributeArgument(CodeModelState state, CodeAttribute parent, int index)
            : base(state, parent.FileCodeModel)
        {
            _parentHandle = new ParentHandle<CodeAttribute>(parent);
            _index = index;
        }

        protected override EnvDTE.CodeElements GetCollection()
        {
            return GetCollection<CodeAttributeArgument>(this.Parent);
        }

        internal override SyntaxNode LookupNode()
        {
            if (!TryLookupNode(out var node))
            {
                throw Exceptions.ThrowEUnexpected();
            }

            return node;
        }

        internal override bool TryLookupNode(out SyntaxNode node)
        {
            node = null;

            var attributeNode = _parentHandle.Value.LookupNode();
            if (attributeNode == null)
            {
                return false;
            }

            if (!CodeModelService.TryGetAttributeArgumentNode(attributeNode, _index, out var attributeArgumentNode))
            {
                return false;
            }

            node = attributeArgumentNode;
            return node != null;
        }

        public override EnvDTE.vsCMElement Kind
        {
            get
            {
                // TODO: VB returns (EnvDTE.vsCMElement)EnvDTE80.vsCMElement2.vsCMElementAttributeArgument
                return EnvDTE.vsCMElement.vsCMElementOther;
            }
        }

        public override object Parent
        {
            get { return _parentHandle.Value; }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return EmptyCollection.Create(this.State, this); }
        }

        protected override string GetFullName()
        {
            // TODO: VB throws E_NOTIMPL
            throw Exceptions.ThrowEFail();
        }

        public string Value
        {
            get
            {
                return CodeModelService.GetAttributeArgumentValue(LookupNode());
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public new void Delete()
        {
            base.Delete();
        }
    }
}
