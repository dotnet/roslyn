// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeVariable2))]
    public sealed class CodeVariable : AbstractCodeMember, EnvDTE.CodeVariable, EnvDTE80.CodeVariable2
    {
        internal static EnvDTE.CodeVariable Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeVariable(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeVariable)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeVariable CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeVariable(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeVariable)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeVariable(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeVariable(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        private ITypeSymbol GetSymbolType()
        {
            var symbol = LookupSymbol();
            if (symbol != null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Field:
                        return ((IFieldSymbol)symbol).Type;
                    case SymbolKind.Property:
                        // Note: VB WithEvents fields are represented as properties
                        return ((IPropertySymbol)symbol).Type;
                }
            }

            return null;
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementVariable; }
        }

        public EnvDTE80.vsCMConstKind ConstKind
        {
            get
            {
                return CodeModelService.GetConstKind(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateConstKind, value);
            }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return Attributes; }
        }

        public object InitExpression
        {
            get
            {
                return CodeModelService.GetInitExpression(LookupNode());
            }

            set
            {
                if (value == null || value is string)
                {
                    UpdateNode(FileCodeModel.UpdateInitExpression, (string)value);
                    return;
                }

                // TODO(DustinCa): Legacy VB throws E_INVALIDARG if value is not a string.
                throw Exceptions.ThrowEFail();
            }
        }

        public bool IsConstant
        {
            get
            {
                return CodeModelService.GetIsConstant(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateIsConstant, value);
            }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                var type = GetSymbolType();
                if (type == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                return CodeTypeRef.Create(this.State, this, GetProjectId(), type);
            }

            set
            {
                // The type is sometimes part of the node key, so we should be sure to reacquire
                // it after updating it. Note that we pass trackKinds: false because it's possible
                // that UpdateType might change the kind of a node (e.g. change a VB Sub to a Function).

                UpdateNodeAndReacquireNodeKey(FileCodeModel.UpdateType, value, trackKinds: false);
            }
        }
    }
}
