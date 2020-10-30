// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{

    internal sealed class CSharpLazyNoneOperation : LazyNoneOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _boundNode;

        public CSharpLazyNoneOperation(CSharpOperationFactory operationFactory, BoundNode boundNode, SemanticModel semanticModel, SyntaxNode node, ConstantValue constantValue, bool isImplicit, ITypeSymbol type) :
            base(semanticModel, node, constantValue: constantValue, isImplicit: isImplicit, type)
        {
            _operationFactory = operationFactory;
            _boundNode = boundNode;
        }

        protected override ImmutableArray<IOperation> GetChildren() => _operationFactory.GetIOperationChildren(_boundNode);
    }


    internal sealed class CSharpLazyNonePatternOperation : LazyNoneOperation, IPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundPattern _boundNode;

        public CSharpLazyNonePatternOperation(CSharpOperationFactory operationFactory, BoundPattern boundNode, SemanticModel semanticModel, SyntaxNode node, bool isImplicit) :
            base(semanticModel, node, constantValue: null, isImplicit: isImplicit, type: null)
        {
            _operationFactory = operationFactory;
            _boundNode = boundNode;
        }

        public ITypeSymbol InputType => _boundNode.InputType.GetITypeSymbol(NullableAnnotation.None);

        public ITypeSymbol NarrowedType => _boundNode.NarrowedType.GetITypeSymbol(NullableAnnotation.None);

        protected override ImmutableArray<IOperation> GetChildren() => _operationFactory.GetIOperationChildren(_boundNode);
    }
}
