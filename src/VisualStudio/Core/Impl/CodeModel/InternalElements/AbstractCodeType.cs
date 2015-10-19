// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    public abstract class AbstractCodeType : AbstractCodeMember, EnvDTE.CodeType
    {
        internal AbstractCodeType(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        internal AbstractCodeType(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        private SyntaxNode GetNamespaceOrTypeNode()
        {
            return LookupNode().Ancestors()
                .Where(n => CodeModelService.IsNamespace(n) || CodeModelService.IsType(n))
                .FirstOrDefault();
        }

        private SyntaxNode GetNamespaceNode()
        {
            return LookupNode().Ancestors()
                .Where(n => CodeModelService.IsNamespace(n))
                .FirstOrDefault();
        }

        internal INamedTypeSymbol LookupTypeSymbol()
        {
            return (INamedTypeSymbol)LookupSymbol();
        }

        protected override object GetExtenderNames()
        {
            return CodeModelService.GetTypeExtenderNames();
        }

        protected override object GetExtender(string name)
        {
            return CodeModelService.GetTypeExtender(name, this);
        }

        public override object Parent
        {
            get
            {
                var containingNamespaceOrType = GetNamespaceOrTypeNode();

                return containingNamespaceOrType != null
                    ? (object)FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(containingNamespaceOrType)
                    : this.FileCodeModel;
            }
        }

        public override EnvDTE.CodeElements Children
        {
            get
            {
                return UnionCollection.Create(this.State, this,
                    (ICodeElements)this.Attributes,
                    (ICodeElements)InheritsImplementsCollection.Create(this.State, this, this.FileCodeModel, this.NodeKey),
                    (ICodeElements)this.Members);
            }
        }

        public EnvDTE.CodeElements Bases
        {
            get
            {
                return BasesCollection.Create(this.State, this, this.FileCodeModel, this.NodeKey, interfaces: false);
            }
        }

        public EnvDTE80.vsCMDataTypeKind DataTypeKind
        {
            get
            {
                return CodeModelService.GetDataTypeKind(LookupNode(), (INamedTypeSymbol)LookupSymbol());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateDataTypeKind, value);
            }
        }

        public EnvDTE.CodeElements DerivedTypes
        {
            get { throw new NotImplementedException(); }
        }

        public EnvDTE.CodeElements ImplementedInterfaces
        {
            get
            {
                return BasesCollection.Create(this.State, this, this.FileCodeModel, this.NodeKey, interfaces: true);
            }
        }

        public EnvDTE.CodeElements Members
        {
            get
            {
                return TypeCollection.Create(this.State, this, this.FileCodeModel, this.NodeKey);
            }
        }

        public EnvDTE.CodeNamespace Namespace
        {
            get
            {
                var namespaceNode = GetNamespaceNode();

                return namespaceNode != null
                    ? FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeNamespace>(namespaceNode)
                    : null;
            }
        }

        /// <returns>True if the current type inherits from or equals the type described by the
        /// given full name.</returns>
        /// <remarks>Equality is included in the check as per Dev10 Bug #725630</remarks>
        public bool get_IsDerivedFrom(string fullName)
        {
            var currentType = LookupTypeSymbol();
            if (currentType == null)
            {
                return false;
            }

            var baseType = GetSemanticModel().Compilation.GetTypeByMetadataName(fullName);
            if (baseType == null)
            {
                return false;
            }

            return currentType.InheritsFromOrEquals(baseType);
        }

        public override bool IsCodeType
        {
            get { return true; }
        }

        public void RemoveMember(object element)
        {
            var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);

            if (codeElement == null)
            {
                codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.Members.Item(element));
            }

            if (codeElement == null)
            {
                throw new ArgumentException(ServicesVSResources.ElementIsNotValid, "element");
            }

            codeElement.Delete();
        }

        public EnvDTE.CodeElement AddBase(object @base, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                FileCodeModel.AddBase(LookupNode(), @base, position);

                var codeElements = this.Bases as ICodeElements;
                EnvDTE.CodeElement element;
                var hr = codeElements.Item(1, out element);

                if (ErrorHandler.Succeeded(hr))
                {
                    return element;
                }

                return null;
            });
        }

        public EnvDTE.CodeInterface AddImplementedInterface(object @base, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                var name = FileCodeModel.AddImplementedInterface(LookupNode(), @base, position);

                var codeElements = this.ImplementedInterfaces as ICodeElements;
                EnvDTE.CodeElement element;
                var hr = codeElements.Item(name, out element);

                if (ErrorHandler.Succeeded(hr))
                {
                    return element as EnvDTE.CodeInterface;
                }

                return null;
            });
        }

        public void RemoveBase(object element)
        {
            FileCodeModel.EnsureEditor(() =>
            {
                FileCodeModel.RemoveBase(LookupNode(), element);
            });
        }

        public void RemoveInterface(object element)
        {
            FileCodeModel.EnsureEditor(() =>
            {
                FileCodeModel.RemoveImplementedInterface(LookupNode(), element);
            });
        }
    }
}
