// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using EnvDTE;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    public sealed class CodeFunctionDeclareDecl : CodeFunction
    {
        internal static new EnvDTE.CodeFunction Create(
          CodeModelState state,
          FileCodeModel fileCodeModel,
          SyntaxNodeKey nodeKey,
          int? nodeKind)
        {
            var element = new CodeFunctionDeclareDecl(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static new EnvDTE.CodeFunction CreateUnknown(
           CodeModelState state,
           FileCodeModel fileCodeModel,
           int nodeKind,
           string name)
        {
            var element = new CodeFunctionDeclareDecl(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeFunctionDeclareDecl(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeFunctionDeclareDecl(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        public override vsCMElement Kind
        {
            get { return vsCMElement.vsCMElementDeclareDecl; }
        }
    }
}
