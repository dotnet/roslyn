// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeAttribute2))]
    public sealed class CodeAttribute : AbstractCodeElement, ICodeElementContainer<CodeAttributeArgument>, EnvDTE.CodeAttribute, EnvDTE80.CodeAttribute2
    {
        internal static EnvDTE.CodeAttribute Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            AbstractCodeElement parent,
            string name,
            int ordinal)
        {
            var newElement = new CodeAttribute(state, fileCodeModel, parent, name, ordinal);
            return (EnvDTE.CodeAttribute)ComAggregate.CreateAggregatedObject(newElement);
        }

        // CodeAttributes are tracked by name+ordinal within their parent node, or FileCodeModel if parent is null.
        private readonly AbstractCodeElement _parent; // NOTE: Ref'd
        private string _name;
        private readonly int _ordinal;

        private CodeAttribute(CodeModelState state, FileCodeModel fileCodeModel, AbstractCodeElement parent, string name, int ordinal)
            : base(state, fileCodeModel)
        {
            _parent = parent;
            _name = name;
            _ordinal = ordinal;
        }

        EnvDTE.CodeElements ICodeElementContainer<CodeAttributeArgument>.GetCollection()
        {
            return this.Arguments;
        }

        protected override EnvDTE.CodeElements GetCollection()
        {
            return GetCollection<CodeAttribute>(Parent);
        }

        internal override SyntaxNode LookupNode()
        {
            var parentNode = _parent != null
                ? _parent.LookupNode()
                : FileCodeModel.GetSyntaxRoot();

            if (parentNode == null)
            {
                throw Exceptions.ThrowEFail();
            }

            SyntaxNode attributeNode;
            if (!CodeModelService.TryGetAttributeNode(parentNode, _name, _ordinal, out attributeNode))
            {
                throw Exceptions.ThrowEFail();
            }

            return attributeNode;
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementAttribute; }
        }

        public override object Parent
        {
            get
            {
                return _parent != null
                    ? _parent
                    : (object)this.FileCodeModel;
            }
        }

        public EnvDTE.CodeElements Arguments
        {
            get { return AttributeArgumentCollection.Create(this.State, this); }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return Arguments; }
        }

        protected override void SetName(string value)
        {
            // Defer to the base type to do the heavy lifting...
            base.SetName(value);

            // ...and then update the name we use to track this attribute.
            _name = value;
        }

        public string Target
        {
            get
            {
                return CodeModelService.GetAttributeTarget(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateAttributeTarget, value);
            }
        }

        public string Value
        {
            get
            {
                return CodeModelService.GetAttributeValue(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateAttributeValue, value);
            }
        }

        public EnvDTE80.CodeAttributeArgument AddArgument(string value, object nameObj, object position)
        {
            string name;

            if (nameObj == Type.Missing ||
                nameObj == null)
            {
                name = null;
            }
            else if (nameObj is string)
            {
                name = (string)nameObj;
            }
            else
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddAttributeArgument(LookupNode(), name, value, position);
            });
        }

        public new void Delete()
        {
            base.Delete();
        }
    }
}
