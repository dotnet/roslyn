// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeElement2))]
    public sealed class CodeOptionsStatement : AbstractCodeElement
    {
        internal static EnvDTE80.CodeElement2 Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            string name,
            int ordinal)
        {
            var element = new CodeOptionsStatement(state, fileCodeModel, name, ordinal);
            var result = (EnvDTE80.CodeElement2)ComAggregate.CreateAggregatedObject(element);

            return result;
        }

        internal static EnvDTE80.CodeElement2 CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeOptionsStatement(state, fileCodeModel, nodeKind, name);
            return (EnvDTE80.CodeElement2)ComAggregate.CreateAggregatedObject(element);
        }

        private readonly string _name;
        private readonly int _ordinal;

        private CodeOptionsStatement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            string name,
            int ordinal)
            : base(state, fileCodeModel)
        {
            _name = name;
            _ordinal = ordinal;
        }

        private CodeOptionsStatement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind)
        {
            _name = name;
        }

        internal override SyntaxNode LookupNode()
        {
            SyntaxNode node;
            if (!TryLookupNode(out node))
            {
                throw Exceptions.ThrowEFail();
            }

            return node;
        }

        internal override bool TryLookupNode(out SyntaxNode node)
        {
            node = null;

            var parentNode = this.FileCodeModel.GetSyntaxRoot();
            if (parentNode == null)
            {
                return false;
            }

            SyntaxNode optionNode;
            if (!CodeModelService.TryGetOptionNode(parentNode, _name, _ordinal, out optionNode))
            {
                return false;
            }

            node = optionNode;
            return node != null;
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementOptionStmt; }
        }

        public override object Parent
        {
            get { return this.FileCodeModel; }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return EmptyCollection.Create(this.State, this); }
        }

        protected override void SetName(string value)
        {
            throw Exceptions.ThrowENotImpl();
        }

        public override void RenameSymbol(string newName)
        {
            throw Exceptions.ThrowENotImpl();
        }
    }
}
