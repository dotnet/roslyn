// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    [ComDefaultInterface(typeof(EnvDTE80.CodeImport))]
    public sealed class CodeImport : AbstractCodeElement, EnvDTE80.CodeImport
    {
        internal static EnvDTE80.CodeImport Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            AbstractCodeElement parent,
            string dottedName)
        {
            var element = new CodeImport(state, fileCodeModel, parent, dottedName);
            var result = (EnvDTE80.CodeImport)ComAggregate.CreateAggregatedObject(element);

            return result;
        }

        internal static EnvDTE80.CodeImport CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string dottedName)
        {
            var element = new CodeImport(state, fileCodeModel, nodeKind, dottedName);
            return (EnvDTE80.CodeImport)ComAggregate.CreateAggregatedObject(element);
        }

        private readonly ParentHandle<AbstractCodeElement> _parentHandle; // parent object -- if parent is FCM then NULL else ref'd
        private readonly string _dottedName;

        private CodeImport(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            AbstractCodeElement parent,
            string dottedName)
            : base(state, fileCodeModel)
        {
            _parentHandle = new ParentHandle<AbstractCodeElement>(parent);
            _dottedName = dottedName;
        }

        private CodeImport(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string dottedName)
            : base(state, fileCodeModel, nodeKind)
        {
            _dottedName = dottedName;
        }

        internal override bool TryLookupNode(out SyntaxNode node)
        {
            node = null;

            var parentNode = _parentHandle.Value != null
                ? _parentHandle.Value.LookupNode()
                : FileCodeModel.GetSyntaxRoot();

            if (parentNode == null)
            {
                return false;
            }

            if (!CodeModelService.TryGetImportNode(parentNode, _dottedName, out var importNode))
            {
                return false;
            }

            node = importNode;
            return node != null;
        }

        internal override ISymbol LookupSymbol()
        {
            Debug.Fail("CodeImports aren't backed by symbols");
            throw new InvalidOperationException();
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementImportStmt; }
        }

        public override object Parent
        {
            get
            {
                if (_parentHandle.Value != null)
                {
                    return _parentHandle.Value;
                }
                else
                {
                    return FileCodeModel;
                }
            }
        }

        public string Alias
        {
            get
            {
                return CodeModelService.GetImportAlias(LookupNode());
            }

            set
            {
                // TODO: Implement
                throw new NotImplementedException();
            }
        }

        public string Namespace
        {
            get
            {
                return CodeModelService.GetImportNamespaceOrType(LookupNode());
            }

            set
            {
                // TODO: Implement
                throw new NotImplementedException();
            }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return EmptyCollection.Create(this.State, this); }
        }

        protected override string GetName()
            => CodeModelService.GetName(LookupNode());

        protected override void SetName(string value)
            => throw Exceptions.ThrowEFail();

        protected override string GetFullName()
            => CodeModelService.GetFullName(LookupNode(), semanticModel: null);
    }
}
