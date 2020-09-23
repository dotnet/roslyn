// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeProperty2))]
    public sealed partial class CodeProperty : AbstractCodeMember, ICodeElementContainer<CodeParameter>, ICodeElementContainer<CodeAttribute>, EnvDTE.CodeProperty, EnvDTE80.CodeProperty2
    {
        internal static EnvDTE.CodeProperty Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeProperty(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeProperty)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeProperty CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeProperty(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeProperty)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeProperty(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeProperty(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        private IPropertySymbol PropertySymbol
        {
            get { return (IPropertySymbol)LookupSymbol(); }
        }

        EnvDTE.CodeElements ICodeElementContainer<CodeParameter>.GetCollection()
            => this.Parameters;

        EnvDTE.CodeElements ICodeElementContainer<CodeAttribute>.GetCollection()
            => this.Attributes;

        internal override ImmutableArray<SyntaxNode> GetParameters()
            => ImmutableArray.CreateRange(CodeModelService.GetParameterNodes(LookupNode()));

        protected override object GetExtenderNames()
            => CodeModelService.GetPropertyExtenderNames();

        protected override object GetExtender(string name)
            => CodeModelService.GetPropertyExtender(name, LookupNode(), LookupSymbol());

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementProperty; }
        }

        public override object Parent
        {
            get
            {
                EnvDTE80.CodeProperty2 codeProperty = this;
                return codeProperty.Parent2;
            }
        }

        public EnvDTE.CodeElement Parent2
        {
            get
            {
                var containingTypeNode = GetContainingTypeNode();
                if (containingTypeNode == null)
                {
                    throw Exceptions.ThrowEUnexpected();
                }

                return FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(containingTypeNode);
            }
        }

        EnvDTE.CodeClass EnvDTE.CodeProperty.Parent
        {
            get
            {
                if (this.Parent is EnvDTE.CodeClass parentClass)
                {
                    return parentClass;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        EnvDTE.CodeClass EnvDTE80.CodeProperty2.Parent
        {
            get
            {
                EnvDTE.CodeProperty property = this;
                return property.Parent;
            }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return this.Attributes; }
        }

        private bool HasAccessorNode(MethodKind methodKind)
            => CodeModelService.TryGetAccessorNode(LookupNode(), methodKind, out _);

        private bool IsExpressionBodiedProperty()
            => CodeModelService.IsExpressionBodiedProperty(LookupNode());

        public EnvDTE.CodeFunction Getter
        {
            get
            {
                if (!HasAccessorNode(MethodKind.PropertyGet) &&
                    !IsExpressionBodiedProperty())
                {
                    return null;
                }

                return CodeAccessorFunction.Create(this.State, this, MethodKind.PropertyGet);
            }

            set
            {
                throw Exceptions.ThrowENotImpl();
            }
        }

        public EnvDTE.CodeFunction Setter
        {
            get
            {
                if (!HasAccessorNode(MethodKind.PropertySet))
                {
                    return null;
                }

                return CodeAccessorFunction.Create(this.State, this, MethodKind.PropertySet);
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
                return CodeTypeRef.Create(this.State, this, GetProjectId(), PropertySymbol.Type);
            }

            set
            {
                // The type is sometimes part of the node key, so we should be sure to reacquire
                // it after updating it. Note that we pass trackKinds: false because it's possible
                // that UpdateType might change the kind of a node (e.g. change a VB Sub to a Function).

                UpdateNodeAndReacquireNodeKey(FileCodeModel.UpdateType, value, trackKinds: false);
            }
        }

        public bool IsDefault
        {
            get
            {
                return CodeModelService.GetIsDefault(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateIsDefault, value);
            }
        }

        public EnvDTE80.vsCMPropertyKind ReadWrite
        {
            get { return CodeModelService.GetReadWrite(LookupNode()); }
        }
    }
}
