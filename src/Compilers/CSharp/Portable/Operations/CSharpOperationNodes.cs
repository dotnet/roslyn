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

    internal sealed class CSharpLazyInterpolatedStringTextOperation : LazyInterpolatedStringTextOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundLiteral _text;

        internal CSharpLazyInterpolatedStringTextOperation(CSharpOperationFactory operationFactory, BoundLiteral text, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _text = text;
        }

        protected override IOperation CreateText()
        {
            return _operationFactory.CreateBoundLiteralOperation(_text, @implicit: true);
        }
    }

    internal sealed class CSharpLazyInterpolationOperation : LazyInterpolationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundStringInsert _stringInsert;

        internal CSharpLazyInterpolationOperation(CSharpOperationFactory operationFactory, BoundStringInsert stringInsert, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _stringInsert = stringInsert;
        }

        protected override IOperation CreateExpression()
        {
            return _operationFactory.Create(_stringInsert.Value);
        }

        protected override IOperation CreateAlignment()
        {
            return _operationFactory.Create(_stringInsert.Alignment);
        }

        protected override IOperation CreateFormatString()
        {
            return _operationFactory.Create(_stringInsert.Format);
        }
    }

    internal sealed class CSharpLazyInvalidOperation : LazyInvalidOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly IBoundInvalidNode _node;

        internal CSharpLazyInvalidOperation(CSharpOperationFactory operationFactory, IBoundInvalidNode node, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _node = node;
        }

        protected override ImmutableArray<IOperation> CreateChildren()
        {
            return _operationFactory.CreateFromArray<BoundNode, IOperation>(_node.InvalidNodeChildren);
        }
    }

    internal sealed class CSharpLazyDynamicMemberReferenceOperation : LazyDynamicMemberReferenceOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _instance;

        internal CSharpLazyDynamicMemberReferenceOperation(CSharpOperationFactory operationFactory, BoundNode instance, string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _instance = instance;
        }

        protected override IOperation CreateInstance()
        {
            return _operationFactory.Create(_instance);
        }
    }

    internal sealed class CSharpLazyDynamicObjectCreationOperation : LazyDynamicObjectCreationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundDynamicObjectCreationExpression _dynamicObjectCreationExpression;

        internal CSharpLazyDynamicObjectCreationOperation(CSharpOperationFactory operationFactory, BoundDynamicObjectCreationExpression dynamicObjectCreationExpression, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _dynamicObjectCreationExpression = dynamicObjectCreationExpression;
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_dynamicObjectCreationExpression.Arguments);
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
            return (IObjectOrCollectionInitializerOperation)_operationFactory.Create(_dynamicObjectCreationExpression.InitializerExpressionOpt);
        }
    }

    internal sealed class CSharpLazyDynamicInvocationOperation : LazyDynamicInvocationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundDynamicInvocableBase _dynamicInvocable;

        internal CSharpLazyDynamicInvocationOperation(CSharpOperationFactory operationFactory, BoundDynamicInvocableBase dynamicInvocable, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _dynamicInvocable = dynamicInvocable;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.CreateBoundDynamicInvocationExpressionReceiver(_dynamicInvocable.Expression);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_dynamicInvocable.Arguments);
        }
    }

    internal sealed class CSharpLazyDynamicIndexerAccessOperation : LazyDynamicIndexerAccessOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundExpression _indexer;

        internal CSharpLazyDynamicIndexerAccessOperation(CSharpOperationFactory operationFactory, BoundExpression indexer, ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _indexer = indexer;
        }

        protected override IOperation CreateOperation()
        {
            return _operationFactory.CreateBoundDynamicIndexerAccessExpressionReceiver(_indexer);
        }

        protected override ImmutableArray<IOperation> CreateArguments()
        {
            return _operationFactory.CreateBoundDynamicIndexerAccessArguments(_indexer);
        }
    }

    internal sealed class CSharpLazyVariableDeclaratorOperation : LazyVariableDeclaratorOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundLocalDeclaration _localDeclaration;

        internal CSharpLazyVariableDeclaratorOperation(CSharpOperationFactory operationFactory, BoundLocalDeclaration localDeclaration, ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localDeclaration = localDeclaration;
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return _operationFactory.CreateVariableDeclaratorInitializer(_localDeclaration, Syntax);
        }

        protected override ImmutableArray<IOperation> CreateIgnoredArguments()
        {
            return _operationFactory.CreateFromArray<BoundExpression, IOperation>(_localDeclaration.ArgumentsOpt);
        }
    }

    internal sealed class CSharpLazyVariableDeclarationOperation : LazyVariableDeclarationOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _localDeclaration;

        internal CSharpLazyVariableDeclarationOperation(CSharpOperationFactory operationFactory, BoundNode localDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit) :
            base(semanticModel, syntax, type, constantValue, isImplicit)
        {
            _operationFactory = operationFactory;
            _localDeclaration = localDeclaration;
        }

        protected override ImmutableArray<IVariableDeclaratorOperation> CreateDeclarators()
        {
            return _operationFactory.CreateVariableDeclarator(_localDeclaration, Syntax);
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
            return null;
        }

        protected override ImmutableArray<IOperation> CreateIgnoredDimensions()
        {
            return _operationFactory.CreateIgnoredDimensions(_localDeclaration, Syntax);
        }
    }

    internal sealed class CSharpLazyConstantPatternOperation : LazyConstantPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyConstantPatternOperation(ITypeSymbol inputType, ITypeSymbol narrowedType, CSharpOperationFactory operationFactory, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            base(inputType, narrowedType, semanticModel, syntax, type: null, constantValue: null, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    internal sealed class CSharpLazyRelationalPatternOperation : LazyRelationalPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNode _value;

        internal CSharpLazyRelationalPatternOperation(ITypeSymbol inputType, ITypeSymbol narrowedType, CSharpOperationFactory operationFactory, BinaryOperatorKind operatorKind, BoundNode value, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) :
            base(operatorKind, inputType, narrowedType, semanticModel, syntax, type: null, constantValue: null, isImplicit)
        {
            _operationFactory = operationFactory;
            _value = value;
        }

        protected override IOperation CreateValue()
        {
            return _operationFactory.Create(_value);
        }
    }

    /// <summary>
    /// Represents a C# negated pattern.
    /// </summary>
    internal sealed partial class CSharpLazyNegatedPatternOperation : LazyNegatedPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNegatedPattern _boundNegatedPattern;

        public CSharpLazyNegatedPatternOperation(
            CSharpOperationFactory operationFactory,
            BoundNegatedPattern boundNegatedPattern,
            SemanticModel semanticModel)
            : base(inputType: boundNegatedPattern.InputType.GetPublicSymbol(),
                   narrowedType: boundNegatedPattern.NarrowedType.GetPublicSymbol(),
                   semanticModel: semanticModel,
                   syntax: boundNegatedPattern.Syntax,
                   type: null,
                   constantValue: null,
                   isImplicit: boundNegatedPattern.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _boundNegatedPattern = boundNegatedPattern;
        }
        protected override IPatternOperation CreatePattern()
        {
            return (IPatternOperation)_operationFactory.Create(_boundNegatedPattern.Negated);
        }
    }
    /// <summary>
    /// Represents a C# binary pattern.
    /// </summary>
    internal sealed partial class CSharpLazyBinaryPatternOperation : LazyBinaryPatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundBinaryPattern _boundBinaryPattern;

        public CSharpLazyBinaryPatternOperation(
            CSharpOperationFactory operationFactory,
            BoundBinaryPattern boundBinaryPattern,
            SemanticModel semanticModel)
            : base(operatorKind: boundBinaryPattern.Disjunction ? BinaryOperatorKind.Or : BinaryOperatorKind.And,
                   inputType: boundBinaryPattern.InputType.GetPublicSymbol(),
                   narrowedType: boundBinaryPattern.NarrowedType.GetPublicSymbol(),
                   semanticModel: semanticModel,
                   syntax: boundBinaryPattern.Syntax,
                   type: null,
                   constantValue: null,
                   isImplicit: boundBinaryPattern.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _boundBinaryPattern = boundBinaryPattern;
        }
        protected override IPatternOperation CreateLeftPattern()
        {
            return (IPatternOperation)_operationFactory.Create(_boundBinaryPattern.Left);
        }
        protected override IPatternOperation CreateRightPattern()
        {
            return (IPatternOperation)_operationFactory.Create(_boundBinaryPattern.Right);
        }
    }

    /// <summary>
    /// Represents a C# recursive pattern.
    /// </summary>
    internal sealed partial class CSharpLazyRecursivePatternOperation : LazyRecursivePatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundRecursivePattern _boundRecursivePattern;

        public CSharpLazyRecursivePatternOperation(
            CSharpOperationFactory operationFactory,
            BoundRecursivePattern boundRecursivePattern,
            SemanticModel semanticModel)
            : base(inputType: boundRecursivePattern.InputType.GetPublicSymbol(),
                   narrowedType: boundRecursivePattern.NarrowedType.GetPublicSymbol(),
                   matchedType: (boundRecursivePattern.DeclaredType?.Type ?? boundRecursivePattern.InputType.StrippedType()).GetPublicSymbol(),
                   deconstructSymbol: boundRecursivePattern.DeconstructMethod.GetPublicSymbol(),
                   declaredSymbol: boundRecursivePattern.Variable.GetPublicSymbol(),
                   semanticModel: semanticModel,
                   syntax: boundRecursivePattern.Syntax,
                   type: null,
                   constantValue: null,
                   isImplicit: boundRecursivePattern.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _boundRecursivePattern = boundRecursivePattern;

        }
        protected override ImmutableArray<IPatternOperation> CreateDeconstructionSubpatterns()
        {
            return _boundRecursivePattern.Deconstruction.IsDefault ? ImmutableArray<IPatternOperation>.Empty :
                _boundRecursivePattern.Deconstruction.SelectAsArray<BoundSubpattern, CSharpOperationFactory, IPatternOperation>((p, fac) => (IPatternOperation)fac.Create(p.Pattern), _operationFactory);
        }
        protected override ImmutableArray<IPropertySubpatternOperation> CreatePropertySubpatterns()
        {
            return _boundRecursivePattern.Properties.IsDefault ? ImmutableArray<IPropertySubpatternOperation>.Empty :
                _boundRecursivePattern.Properties.SelectAsArray<BoundSubpattern, CSharpLazyRecursivePatternOperation, IPropertySubpatternOperation>((p, recursivePattern) => recursivePattern._operationFactory.CreatePropertySubpattern(p, recursivePattern.MatchedType), this);
        }
    }

    /// <summary>
    /// Represents a C# recursive pattern using ITuple.
    /// </summary>
    internal sealed partial class CSharpLazyITuplePatternOperation : LazyRecursivePatternOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundITuplePattern _boundITuplePattern;

        public CSharpLazyITuplePatternOperation(CSharpOperationFactory operationFactory, BoundITuplePattern boundITuplePattern, SemanticModel semanticModel)
            : base(inputType: boundITuplePattern.InputType.GetPublicSymbol(),
                   narrowedType: boundITuplePattern.NarrowedType.GetPublicSymbol(),
                   matchedType: boundITuplePattern.InputType.StrippedType().GetPublicSymbol(),
                   deconstructSymbol: boundITuplePattern.GetLengthMethod.ContainingType.GetPublicSymbol(),
                   declaredSymbol: null,
                   semanticModel: semanticModel,
                   syntax: boundITuplePattern.Syntax,
                   type: null,
                   constantValue: null,
                   isImplicit: boundITuplePattern.WasCompilerGenerated)
        {
            _operationFactory = operationFactory;
            _boundITuplePattern = boundITuplePattern;

        }
        protected override ImmutableArray<IPatternOperation> CreateDeconstructionSubpatterns()
        {
            return _boundITuplePattern.Subpatterns.IsDefault ? ImmutableArray<IPatternOperation>.Empty :
                _boundITuplePattern.Subpatterns.SelectAsArray<BoundSubpattern, CSharpOperationFactory, IPatternOperation>((p, fac) => (IPatternOperation)fac.Create(p.Pattern), _operationFactory);
        }
        protected override ImmutableArray<IPropertySubpatternOperation> CreatePropertySubpatterns()
        {
            return ImmutableArray<IPropertySubpatternOperation>.Empty;
        }
    }

    internal sealed class CSharpLazyMethodBodyOperation : LazyMethodBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundNonConstructorMethodBody _methodBody;

        internal CSharpLazyMethodBodyOperation(CSharpOperationFactory operationFactory, BoundNonConstructorMethodBody methodBody, SemanticModel semanticModel, SyntaxNode syntax) :
            base(semanticModel, syntax, type: null, constantValue: null, isImplicit: false)
        {
            _operationFactory = operationFactory;
            _methodBody = methodBody;
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)_operationFactory.Create(_methodBody.BlockBody);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)_operationFactory.Create(_methodBody.ExpressionBody);
        }
    }

    internal sealed class CSharpLazyConstructorBodyOperation : LazyConstructorBodyOperation
    {
        private readonly CSharpOperationFactory _operationFactory;
        private readonly BoundConstructorMethodBody _constructorMethodBody;

        internal CSharpLazyConstructorBodyOperation(CSharpOperationFactory operationFactory, BoundConstructorMethodBody constructorMethodBody, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax) :
            base(locals, semanticModel, syntax, type: null, constantValue: null, isImplicit: false)
        {
            _operationFactory = operationFactory;
            _constructorMethodBody = constructorMethodBody;
        }

        protected override IOperation CreateInitializer()
        {
            return _operationFactory.Create(_constructorMethodBody.Initializer);
        }

        protected override IBlockOperation CreateBlockBody()
        {
            return (IBlockOperation)_operationFactory.Create(_constructorMethodBody.BlockBody);
        }

        protected override IBlockOperation CreateExpressionBody()
        {
            return (IBlockOperation)_operationFactory.Create(_constructorMethodBody.ExpressionBody);
        }
    }
}
