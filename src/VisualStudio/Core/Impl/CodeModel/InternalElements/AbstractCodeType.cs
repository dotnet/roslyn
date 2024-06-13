// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            => (INamedTypeSymbol)LookupSymbol();

        protected override object GetExtenderNames()
            => CodeModelService.GetTypeExtenderNames();

        protected override object GetExtender(string name)
            => CodeModelService.GetTypeExtender(name, this);

        public override object Parent
        {
            get
            {
                var containingNamespaceOrType = GetNamespaceOrTypeNode();

                return containingNamespaceOrType != null
                    ? FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(containingNamespaceOrType)
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
            // Is this an EnvDTE.CodeElement that we created? If so, try to get the underlying code element object.
            var abstractCodeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);

            if (abstractCodeElement == null)
            {
                if (element is EnvDTE.CodeElement codeElement)
                {
                    // Is at least an EnvDTE.CodeElement? If so, try to retrieve it from the Members collection by name.
                    // Note: This might throw an ArgumentException if the name isn't found in the collection.

                    abstractCodeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.Members.Item(codeElement.Name));
                }
                else if (element is string or int)
                {
                    // Is this a string or int? If so, try to retrieve it from the Members collection. Again, this will
                    // throw an ArgumentException if the name or index isn't found in the collection.

                    abstractCodeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.Members.Item(element));
                }
            }

            if (abstractCodeElement == null)
            {
                throw new ArgumentException(ServicesVSResources.Element_is_not_valid, nameof(element));
            }

            abstractCodeElement.Delete();
        }

        public EnvDTE.CodeElement AddBase(object @base, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                FileCodeModel.AddBase(LookupNode(), @base, position);

                var codeElements = this.Bases as ICodeElements;
                var hr = codeElements.Item(1, out var element);

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
                var hr = codeElements.Item(name, out var element);

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
