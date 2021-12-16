// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeDelegate2))]
    public sealed partial class CodeDelegate : AbstractCodeType, ICodeElementContainer<CodeParameter>, EnvDTE.CodeDelegate, EnvDTE80.CodeDelegate2
    {
        internal static EnvDTE.CodeDelegate Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeDelegate(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeDelegate)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeDelegate CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeDelegate(state, fileCodeModel, nodeKind, name);
            return (EnvDTE.CodeDelegate)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeDelegate(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeDelegate(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        EnvDTE.CodeElements ICodeElementContainer<CodeParameter>.GetCollection()
            => this.Parameters;

        private IMethodSymbol LookupInvokeMethod()
        {
            var typeSymbol = (INamedTypeSymbol)LookupSymbol();
            return typeSymbol.DelegateInvokeMethod;
        }

        internal override ImmutableArray<SyntaxNode> GetParameters()
            => ImmutableArray.CreateRange(CodeModelService.GetParameterNodes(LookupNode()));

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementDelegate; }
        }

        public EnvDTE.CodeClass BaseClass
        {
            get
            {
                return (EnvDTE.CodeClass)this.CodeModelService.CreateCodeType(
                    this.State,
                    this.FileCodeModel.GetProjectId(),
                    this.FileCodeModel.GetCompilation().GetSpecialType(SpecialType.System_Delegate));
            }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                return CodeTypeRef.Create(this.State, this, GetProjectId(), LookupInvokeMethod().ReturnType);
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
