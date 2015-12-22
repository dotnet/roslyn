// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeNamespace))]
    public sealed class CodeNamespace : AbstractKeyedCodeElement, EnvDTE.CodeNamespace
    {
        internal static EnvDTE.CodeNamespace Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeNamespace(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeNamespace)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeNamespace CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeNamespace(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeNamespace)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeNamespace(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeNamespace(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        private SyntaxNode GetNamespaceNode()
        {
            return LookupNode().Ancestors().Where(CodeModelService.IsNamespace).FirstOrDefault();
        }

        public override object Parent
        {
            get
            {
                var namespaceNode = GetNamespaceNode();

                return namespaceNode != null
                    ? (object)FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeNamespace>(namespaceNode)
                    : this.FileCodeModel;
            }
        }

        public override EnvDTE.CodeElements Children
        {
            get
            {
                // Children are the same as members for namespaces
                return Members;
            }
        }

        public string Comment
        {
            get
            {
                // Namespaces don't support comments
                throw Exceptions.ThrowENotImpl();
            }

            set
            {
                // Namespaces don't support comments
                throw Exceptions.ThrowENotImpl();
            }
        }

        public string DocComment
        {
            get
            {
                // Namespaces can't have doc comments
                return string.Empty;
            }

            set
            {
                // We don't allow you to set, since you can't set things that don't exist.
                throw Exceptions.ThrowENotImpl();
            }
        }

        public EnvDTE.CodeElements Members
        {
            get
            {
                return NamespaceCollection.Create(State, this, FileCodeModel, NodeKey);
            }
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementNamespace; }
        }

        public EnvDTE.CodeClass AddClass(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access = EnvDTE.vsCMAccess.vsCMAccessDefault)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddClass(LookupNode(), name, position, bases, implementedInterfaces, access);
            });
        }

        public EnvDTE.CodeDelegate AddDelegate(string name, object type, object position, EnvDTE.vsCMAccess access = EnvDTE.vsCMAccess.vsCMAccessDefault)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddDelegate(LookupNode(), name, type, position, access);
            });
        }

        public EnvDTE.CodeEnum AddEnum(string name, object position, object bases, EnvDTE.vsCMAccess access = EnvDTE.vsCMAccess.vsCMAccessDefault)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddEnum(LookupNode(), name, position, bases, access);
            });
        }

        public EnvDTE.CodeInterface AddInterface(string name, object position, object bases, EnvDTE.vsCMAccess access = EnvDTE.vsCMAccess.vsCMAccessDefault)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddInterface(LookupNode(), name, position, bases, access);
            });
        }

        public EnvDTE.CodeNamespace AddNamespace(string name, object position)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddNamespace(LookupNode(), name, position);
            });
        }

        public EnvDTE.CodeStruct AddStruct(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access = EnvDTE.vsCMAccess.vsCMAccessDefault)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddStruct(LookupNode(), name, position, bases, implementedInterfaces, access);
            });
        }

        public void Remove(object element)
        {
            var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);

            if (codeElement == null)
            {
                codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.Members.Item(element));
            }

            if (codeElement == null)
            {
                throw new ArgumentException(ServicesVSResources.ElementIsNotValid, nameof(element));
            }

            codeElement.Delete();
        }
    }
}
