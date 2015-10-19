// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeFunction2))]
    public partial class CodeFunction : AbstractCodeMember, ICodeElementContainer<CodeParameter>, ICodeElementContainer<CodeAttribute>, EnvDTE.CodeFunction, EnvDTE80.CodeFunction2, IMethodXML, IMethodXML2
    {
        internal static EnvDTE.CodeFunction Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeFunction(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeFunction CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeFunction(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);
        }

        internal CodeFunction(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        internal CodeFunction(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        EnvDTE.CodeElements ICodeElementContainer<CodeParameter>.GetCollection()
        {
            return this.Parameters;
        }

        EnvDTE.CodeElements ICodeElementContainer<CodeAttribute>.GetCollection()
        {
            return this.Attributes;
        }

        private IMethodSymbol MethodSymbol
        {
            get { return (IMethodSymbol)LookupSymbol(); }
        }

        internal override ImmutableArray<SyntaxNode> GetParameters()
        {
            return ImmutableArray.CreateRange(CodeModelService.GetParameterNodes(LookupNode()));
        }

        protected override object GetExtenderNames()
        {
            return CodeModelService.GetFunctionExtenderNames();
        }

        protected override object GetExtender(string name)
        {
            return CodeModelService.GetFunctionExtender(name, LookupNode(), LookupSymbol());
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementFunction; }
        }

        public bool CanOverride
        {
            get
            {
                return CodeModelService.GetCanOverride(LookupNode());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateCanOverride, value);
            }
        }

        public EnvDTE.vsCMFunction FunctionKind
        {
            get
            {
                var symbol = LookupSymbol() as IMethodSymbol;
                if (symbol == null)
                {
                    throw Exceptions.ThrowEUnexpected();
                }

                return CodeModelService.GetFunctionKind(symbol);
            }
        }

        public bool IsOverloaded
        {
            get
            {
                var symbol = (IMethodSymbol)LookupSymbol();

                // Only methods and constructors can be overloaded
                if (symbol.MethodKind != MethodKind.Ordinary &&
                    symbol.MethodKind != MethodKind.Constructor)
                {
                    return false;
                }

                var methodsOfName = symbol.ContainingType.GetMembers(symbol.Name)
                                                         .Where(m => m.Kind == SymbolKind.Method);

                return methodsOfName.Count() > 1;
            }
        }

        public EnvDTE.CodeElements Overloads
        {
            get
            {
                return OverloadsCollection.Create(this.FileCodeModel.State, this);
            }
        }

        public override EnvDTE.CodeElements Children
        {
            get { return UnionCollection.Create(this.State, this, (ICodeElements)this.Attributes, (ICodeElements)this.Parameters); }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                return CodeTypeRef.Create(this.State, this, GetProjectId(), MethodSymbol.ReturnType);
            }

            set
            {
                // The type is sometimes part of the node key, so we should be sure to reacquire
                // it after updating it. Note that we pass trackKinds: false because it's possible
                // that UpdateType might change the kind of a node (e.g. change a VB Sub to a Function).

                UpdateNodeAndReacquireNodeKey(FileCodeModel.UpdateType, value, trackKinds: false);
            }
        }
    }
}
