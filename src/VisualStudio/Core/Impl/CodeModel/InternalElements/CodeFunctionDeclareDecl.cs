// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

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
