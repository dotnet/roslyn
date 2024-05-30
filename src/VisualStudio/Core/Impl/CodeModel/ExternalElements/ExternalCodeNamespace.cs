// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeNamespace))]
    public sealed class ExternalCodeNamespace : AbstractExternalCodeElement, EnvDTE.CodeNamespace, EnvDTE.CodeElement
    {
        internal static EnvDTE.CodeNamespace Create(CodeModelState state, ProjectId projectId, INamespaceSymbol namespaceSymbol)
        {
            var newElement = new ExternalCodeNamespace(state, projectId, namespaceSymbol);
            return (EnvDTE.CodeNamespace)ComAggregate.CreateAggregatedObject(newElement);
        }

        private ExternalCodeNamespace(CodeModelState state, ProjectId projectId, INamespaceSymbol namespaceSymbol)
            : base(state, projectId, namespaceSymbol)
        {
        }

        private INamespaceSymbol NamespaceSymbol
        {
            get { return (INamespaceSymbol)LookupSymbol(); }
        }

        protected override string GetDocComment()
            => string.Empty;

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementNamespace; }
        }

        public EnvDTE.CodeElements Members
        {
            get { return ExternalNamespaceCollection.Create(State, this, ProjectId, NamespaceSymbol); }
        }

        public EnvDTE.CodeClass AddClass(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeDelegate AddDelegate(string name, object type, object position, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeEnum AddEnum(string name, object position, object bases, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeInterface AddInterface(string name, object position, object bases, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeNamespace AddNamespace(string name, object position)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeStruct AddStruct(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public void Remove(object element)
            => throw Exceptions.ThrowEFail();
    }
}
