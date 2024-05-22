// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

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
            => LookupNode().Ancestors().Where(CodeModelService.IsNamespace).FirstOrDefault();

        public override object Parent
        {
            get
            {
                var namespaceNode = GetNamespaceNode();

                return namespaceNode != null
                    ? FileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeNamespace>(namespaceNode)
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
                return CodeModelService.GetComment(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateComment, value);
            }
        }

        public string DocComment
        {
            get
            {
                return CodeModelService.GetDocComment(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateDocComment, value);
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

            codeElement ??= ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.Members.Item(element));

            if (codeElement == null)
            {
                throw new ArgumentException(ServicesVSResources.Element_is_not_valid, nameof(element));
            }

            codeElement.Delete();
        }
    }
}
