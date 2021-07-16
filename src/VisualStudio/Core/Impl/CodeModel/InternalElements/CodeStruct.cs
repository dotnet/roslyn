// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeStruct2))]
    public sealed class CodeStruct : AbstractCodeType, EnvDTE.CodeStruct, EnvDTE80.CodeStruct2
    {
        internal static EnvDTE.CodeStruct Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeStruct(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeStruct)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeStruct CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeStruct(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeStruct)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeStruct(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeStruct(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementStruct; }
        }

        public bool IsAbstract
        {
            get
            {
                // TODO: Fix -- C# uses the current node, not the symbol.
                var symbol = LookupSymbol();
                return symbol.IsAbstract;
            }

            set
            {
                // TODO: Fix -- C# will actually allow the user to set an abstract modifier on a struct. VB throws E_NOTIMPL
                throw new NotImplementedException();
            }
        }

        public EnvDTE.CodeElements Parts
        {
            get { return PartialTypeCollection.Create(State, this); }
        }

        public EnvDTE.CodeClass AddClass(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddClass(LookupNode(), name, position, bases, implementedInterfaces, access);
            });
        }

        public EnvDTE.CodeDelegate AddDelegate(string name, object type, object position, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddDelegate(LookupNode(), name, type, position, access);
            });
        }

        public EnvDTE.CodeEnum AddEnum(string name, object position, object bases, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddEnum(LookupNode(), name, position, bases, access);
            });
        }

        public EnvDTE80.CodeEvent AddEvent(string name, string fullDelegateName, bool createPropertyStyleEvent, object position, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddEvent(LookupNode(), name, fullDelegateName, createPropertyStyleEvent, position, access);
            });
        }

        public EnvDTE.CodeFunction AddFunction(string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access, object location)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddFunction(LookupNode(), name, kind, type, position, access);
            });
        }

        public EnvDTE.CodeProperty AddProperty(string getterName, string putterName, object type, object position, EnvDTE.vsCMAccess access, object location)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddProperty(LookupNode(), getterName, putterName, type, position, access);
            });
        }

        public EnvDTE.CodeStruct AddStruct(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddStruct(LookupNode(), name, position, bases, implementedInterfaces, access);
            });
        }

        public EnvDTE.CodeVariable AddVariable(string name, object type, object position, EnvDTE.vsCMAccess access, object location)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddVariable(LookupNode(), name, type, position, access);
            });
        }
    }
}
