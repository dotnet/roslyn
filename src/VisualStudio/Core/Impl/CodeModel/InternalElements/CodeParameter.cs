// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeParameter2))]
    public sealed class CodeParameter : AbstractCodeElement, EnvDTE.CodeParameter, EnvDTE80.CodeParameter2, IParameterKind
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
            void updater(SyntaxNode n, T v)
            {
                var parentNode = _parentHandle.Value.LookupNode();
                var parentNodePath = new SyntaxPath(parentNode);

                parameterUpdater(n, v);

                _parentHandle.Value.ReacquireNodeKey(parentNodePath, CancellationToken.None);
            }

            UpdateNode(updater, value);
        }

        protected override EnvDTE.CodeElements GetCollection()
            => GetCollection<CodeParameter>(Parent);

        protected override string GetName()
            => _name;

        protected override string GetFullName()
        {
            var node = LookupNode();
            if (node == null)
            {
                return string.Empty;
            }

            return CodeModelService.GetParameterFullName(node);
        }

        internal override bool TryLookupNode(out SyntaxNode node)
        {
            node = null;

            var parentNode = _parentHandle.Value.LookupNode();
            if (parentNode == null)
            {
                return false;
            }

            if (!CodeModelService.TryGetParameterNode(parentNode, _name, out var parameterNode))
            {
                return false;
            }

            node = parameterNode;
            return node != null;
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

        void IParameterKind.SetParameterPassingMode(PARAMETER_PASSING_MODE passingMode)
            => this.ParameterKind = this.CodeModelService.UpdateParameterKind(ParameterKind, passingMode);

        void IParameterKind.SetParameterArrayDimensions(int dimensions)
        {
            var type = this.ParameterSymbol.Type;
            var compilation = this.FileCodeModel.GetCompilation();

            var elementType = type is IArrayTypeSymbol
                ? ((IArrayTypeSymbol)type).ElementType
                : type;

            // The original C# implementation had a weird behavior where it wold allow setting array dimensions
            // to 0 to create an array with a single rank.
            var rank = Math.Max(dimensions, 1);
            var newType = compilation.CreateArrayTypeSymbol(elementType, rank);

            this.Type = CodeTypeRef.Create(this.State, this, GetProjectId(), newType);
        }

        int IParameterKind.GetParameterArrayCount()
        {
            var arrayType = this.ParameterSymbol.Type as IArrayTypeSymbol;
            var count = 0;

            while (arrayType != null)
            {
                count++;
                arrayType = arrayType.ElementType as IArrayTypeSymbol;
            }

            return count;
        }

        int IParameterKind.GetParameterArrayDimensions(int index)
        {
            if (index < 0)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var arrayType = this.ParameterSymbol.Type as IArrayTypeSymbol;
            var count = 0;

            while (count < index && arrayType != null)
            {
                count++;
                arrayType = arrayType.ElementType as IArrayTypeSymbol;
            }

            if (arrayType == null)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            return arrayType.Rank;
        }

        PARAMETER_PASSING_MODE IParameterKind.GetParameterPassingMode()
        {
            var parameterKind = this.ParameterKind;

            if ((parameterKind & EnvDTE80.vsCMParameterKind.vsCMParameterKindRef) != 0)
            {
                return PARAMETER_PASSING_MODE.cmParameterTypeInOut;
            }
            else if ((parameterKind & EnvDTE80.vsCMParameterKind.vsCMParameterKindOut) != 0)
            {
                return PARAMETER_PASSING_MODE.cmParameterTypeOut;
            }
            else
            {
                return PARAMETER_PASSING_MODE.cmParameterTypeIn;
            }
        }
    }
}
