// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeInterface2))]
    public sealed class CodeInterface : AbstractCodeType, EnvDTE.CodeInterface, EnvDTE80.CodeInterface2
    {
        internal static EnvDTE.CodeInterface Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeInterface(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeInterface)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeInterface CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeInterface(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeInterface)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeInterface(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeInterface(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementInterface; }
        }

        public EnvDTE.CodeFunction AddFunction(string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access)
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

        public EnvDTE80.CodeEvent AddEvent(string name, string fullDelegateName, bool createPropertyStyleEvent, object position, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                // Note: C# always creates field-like events in interfaces
                return FileCodeModel.AddEvent(LookupNode(), name, fullDelegateName, false, position, access);
            });
        }

        public EnvDTE.CodeElements Parts
        {
            get { return PartialTypeCollection.Create(State, this); }
        }
    }
}
