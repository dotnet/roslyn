// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeParameter2))]
    public sealed class CodeParameter : AbstractCodeElement, EnvDTE.CodeParameter, EnvDTE80.CodeParameter2
    {
        internal static EnvDTE.CodeParameter Create(
            CodeModelState state,
            AbstractCodeMember parent,
            string name)
        {
            var element = new CodeParameter(state, parent, name);
            return (EnvDTE.CodeParameter)ComAggregate.CreateAggregatedObject(element);
        }

        private readonly ParentHandle<AbstractCodeMember> _parentHandle;
        private readonly string _name;

        private CodeParameter(
            CodeModelState state,
            AbstractCodeMember parent,
            string name)
            : base(state, parent.FileCodeModel)
        {
            _parentHandle = new ParentHandle<AbstractCodeMember>(parent);
            _name = name;
        }

        private IParameterSymbol ParameterSymbol
        {
            get { return (IParameterSymbol)LookupSymbol(); }
        }

        private void UpdateNodeAndReacquireParentNodeKey<T>(Action<SyntaxNode, T> parameterUpdater, T value)
        {
            Action<SyntaxNode, T> updater = (n, v) =>
            {
                var parentNode = _parentHandle.Value.LookupNode();
                var parentNodePath = new SyntaxPath(parentNode);

                parameterUpdater(n, v);

                _parentHandle.Value.ReacquireNodeKey(parentNodePath, CancellationToken.None);
            };

            UpdateNode(updater, value);
        }

        protected override EnvDTE.CodeElements GetCollection()
        {
            return GetCollection<CodeParameter>(Parent);
        }

        protected override string GetName()
        {
            return _name;
        }

        protected override string GetFullName()
        {
            var node = LookupNode();
            if (node == null)
            {
                return string.Empty;
            }

            return CodeModelService.GetParameterFullName(node);
        }

        internal override SyntaxNode LookupNode()
        {
            var parentNode = _parentHandle.Value.LookupNode();
            if (parentNode == null)
            {
                throw Exceptions.ThrowEFail();
            }

            SyntaxNode parameterNode;
            if (!CodeModelService.TryGetParameterNode(parentNode, _name, out parameterNode))
            {
                throw Exceptions.ThrowEFail();
            }

            return parameterNode;
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementParameter; }
        }

        public override object Parent
        {
            get { return _parentHandle.Value; }
        }

        EnvDTE.CodeElement EnvDTE.CodeParameter.Parent
        {
            get { return (EnvDTE.CodeElement)Parent; }
        }

        EnvDTE.CodeElement EnvDTE80.CodeParameter2.Parent
        {
            get { return (EnvDTE.CodeElement)Parent; }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return this.Attributes; }
        }

        public EnvDTE.CodeElements Attributes
        {
            get { return AttributeCollection.Create(this.State, this); }
        }

        public string DocComment
        {
            get
            {
                return string.Empty;
            }

            set
            {
                throw Exceptions.ThrowENotImpl();
            }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                return CodeTypeRef.Create(this.State, this, GetProjectId(), ParameterSymbol.Type);
            }

            set
            {
                UpdateNodeAndReacquireParentNodeKey(FileCodeModel.UpdateType, value);
            }
        }

        public EnvDTE80.vsCMParameterKind ParameterKind
        {
            get
            {
                return CodeModelService.GetParameterKind(LookupNode());
            }

            set
            {
                UpdateNodeAndReacquireParentNodeKey(FileCodeModel.UpdateParameterKind, value);
            }
        }

        public string DefaultValue
        {
            get
            {
                return CodeModelService.GetInitExpression(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateInitExpression, value);
            }
        }

        public EnvDTE.CodeAttribute AddAttribute(string name, string value, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddAttribute(LookupNode(), name, value, position);
            });
        }
    }
}
