// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class OperationTests : TestBase
    {
        private static void TestCore(Func<ImmutableArray<IOperation>, ImmutableArray<string>, ImmutableArray<RefKind>, HasDynamicArgumentsExpression> createDynamicExpression)
        {
            // Empty arguments and default argument names/refkinds
            ImmutableArray<IOperation> arguments = ImmutableArray<IOperation>.Empty;
            ImmutableArray<string> argumentNames = default;
            ImmutableArray<RefKind> argumentRefKinds = default;
            HasDynamicArgumentsExpression dynamicExpression = createDynamicExpression(arguments, argumentNames, argumentRefKinds);
            Assert.Throws<InvalidOperationException>(() => dynamicExpression.GetArgumentName(0));
            Assert.Throws<InvalidOperationException>(() => dynamicExpression.GetArgumentRefKind(0));

            // Non-empty arguments and default argument names/refkinds
            arguments = ImmutableArray.Create((IOperation)null);
            argumentNames = default;
            argumentRefKinds = default;
            dynamicExpression = createDynamicExpression(arguments, argumentNames, argumentRefKinds);
            Assert.Null(dynamicExpression.GetArgumentName(0));
            Assert.Null(dynamicExpression.GetArgumentRefKind(0));

            // Non-empty arguments and empty argument names/refkinds
            arguments = ImmutableArray.Create((IOperation)null);
            argumentNames = ImmutableArray<string>.Empty;
            argumentRefKinds = ImmutableArray<RefKind>.Empty;
            dynamicExpression = createDynamicExpression(arguments, argumentNames, argumentRefKinds);
            Assert.Null(dynamicExpression.GetArgumentName(0));
            Assert.Equal(RefKind.None, dynamicExpression.GetArgumentRefKind(0));

            // Non-empty arguments and non-empty argument names/refkinds
            string name = "first";
            RefKind refKind = RefKind.Ref;
            arguments = ImmutableArray.Create((IOperation)null);
            argumentNames = ImmutableArray.Create(name);
            argumentRefKinds = ImmutableArray.Create(refKind);
            dynamicExpression = createDynamicExpression(arguments, argumentNames, argumentRefKinds);
            Assert.Equal(name, dynamicExpression.GetArgumentName(0));
            Assert.Equal(refKind, dynamicExpression.GetArgumentRefKind(0));

            // Index out of range: Negative index
            Assert.Throws<ArgumentOutOfRangeException>(() => dynamicExpression.GetArgumentName(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => dynamicExpression.GetArgumentRefKind(-1));

            // Index out of range: Index > Length
            Assert.Throws<ArgumentOutOfRangeException>(() => dynamicExpression.GetArgumentName(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => dynamicExpression.GetArgumentRefKind(100));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicInvocationExpression_PublicExtensionMethodTests()
        {
            // Test null argument
            IDynamicInvocationOperation nullDynamicExpression = null;
            Assert.Throws<ArgumentNullException>(() => nullDynamicExpression.GetArgumentName(0));
            Assert.Throws<ArgumentNullException>(() => nullDynamicExpression.GetArgumentRefKind(0));

            Func<ImmutableArray<IOperation>, ImmutableArray<string>, ImmutableArray<RefKind>, HasDynamicArgumentsExpression> createDynamicExpression =
                (arguments, argumentNames, argumentRefKinds) => new DynamicInvocationOperation(null, arguments, argumentNames, argumentRefKinds, null, null, null, null, false);

            TestCore(createDynamicExpression);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicIndexerAccessExpression_PublicExtensionMethodTests()
        {
            // Test null argument
            IDynamicIndexerAccessOperation nullDynamicExpression = null;
            Assert.Throws<ArgumentNullException>(() => nullDynamicExpression.GetArgumentName(0));
            Assert.Throws<ArgumentNullException>(() => nullDynamicExpression.GetArgumentRefKind(0));

            Func<ImmutableArray<IOperation>, ImmutableArray<string>, ImmutableArray<RefKind>, HasDynamicArgumentsExpression> createDynamicExpression =
                (arguments, argumentNames, argumentRefKinds) => new DynamicIndexerAccessOperation(null, arguments, argumentNames, argumentRefKinds, null, null, null, null, false);

            TestCore(createDynamicExpression);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IDynamicObjectCreationExpression_PublicExtensionMethodTests()
        {
            // Test null argument
            IDynamicObjectCreationOperation nullDynamicExpression = null;
            Assert.Throws<ArgumentNullException>(() => nullDynamicExpression.GetArgumentName(0));
            Assert.Throws<ArgumentNullException>(() => nullDynamicExpression.GetArgumentRefKind(0));

            Func<ImmutableArray<IOperation>, ImmutableArray<string>, ImmutableArray<RefKind>, HasDynamicArgumentsExpression> createDynamicExpression =
                (arguments, argumentNames, argumentRefKinds) => new DynamicObjectCreationOperation(arguments, argumentNames, argumentRefKinds, null, null, null, null, null, false);

            TestCore(createDynamicExpression);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestGetFlowGraphNullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => ControlFlowGraph.Create((IBlockOperation)null));
            Assert.Throws<ArgumentNullException>(() => ControlFlowGraph.Create((IFieldInitializerOperation)null));
            Assert.Throws<ArgumentNullException>(() => ControlFlowGraph.Create((IPropertyInitializerOperation)null));
            Assert.Throws<ArgumentNullException>(() => ControlFlowGraph.Create((IParameterInitializerOperation)null));
            Assert.Throws<ArgumentNullException>(() => ControlFlowGraph.Create((IConstructorBodyOperation)null));
            Assert.Throws<ArgumentNullException>(() => ControlFlowGraph.Create((IMethodBodyOperation)null));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestGetFlowGraphInvalidArgumentWithNonNullParent()
        {
            IOperation parent = new BlockOperation(ImmutableArray<IOperation>.Empty, ImmutableArray<ILocalSymbol>.Empty,
                    semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);

            TestGetFlowGraphInvalidArgumentCore(argumentExceptionMessage: CodeAnalysisResources.NotARootOperation, parent);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestGetFlowGraphInvalidArgumentWithNullSemanticModel()
        {
            TestGetFlowGraphInvalidArgumentCore(argumentExceptionMessage: CodeAnalysisResources.OperationHasNullSemanticModel, parent: null);
        }

        private void TestGetFlowGraphInvalidArgumentCore(string argumentExceptionMessage, IOperation parent)
        {
            bool exceptionThrown = false;
            try
            {
                IBlockOperation block = new BlockOperation(
                    ImmutableArray<IOperation>.Empty, ImmutableArray<ILocalSymbol>.Empty,
                    semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
                block = Operation.SetParentOperation(block, parent);
                _ = ControlFlowGraph.Create(block);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "body").Message, ex.Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            try
            {
                IFieldInitializerOperation initializer = new FieldInitializerOperation(
                    ImmutableArray<IFieldSymbol>.Empty, ImmutableArray<ILocalSymbol>.Empty,
                    value: null, semanticModel: null,
                    syntax: null, type: null, constantValue: default, isImplicit: false);
                initializer = Operation.SetParentOperation(initializer, parent);
                _ = ControlFlowGraph.Create(initializer);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "initializer").Message, ex.Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            try
            {
                IPropertyInitializerOperation initializer = new PropertyInitializerOperation(
                    ImmutableArray<IPropertySymbol>.Empty, ImmutableArray<ILocalSymbol>.Empty,
                    value: null, semanticModel: null,
                    syntax: null, type: null, constantValue: default, isImplicit: false);
                initializer = Operation.SetParentOperation(initializer, parent);
                _ = ControlFlowGraph.Create(initializer);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "initializer").Message, ex.Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            try
            {
                IParameterInitializerOperation initializer = new ParameterInitializerOperation(
                                    parameter: null, locals: ImmutableArray<ILocalSymbol>.Empty,
                    value: null, semanticModel: null,
                    syntax: null, type: null, constantValue: default, isImplicit: false);
                initializer = Operation.SetParentOperation(initializer, parent);
                _ = ControlFlowGraph.Create(initializer);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "initializer").Message, ex.Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            try
            {
                IConstructorBodyOperation constructorBody = new ConstructorBodyOperation(
                                    ImmutableArray<ILocalSymbol>.Empty,
                                    initializer: null,
                                    blockBody: null,
                                    expressionBody: null,
                                    semanticModel: null, syntax: null);
                constructorBody = Operation.SetParentOperation(constructorBody, parent);
                _ = ControlFlowGraph.Create(constructorBody);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "constructorBody").Message, ex.Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            try
            {
                IMethodBodyOperation methodBody = new MethodBodyOperation(
                                                    blockBody: null,
                                                    expressionBody: null,
                                                    semanticModel: null, syntax: null);
                methodBody = Operation.SetParentOperation(methodBody, parent);
                _ = ControlFlowGraph.Create(methodBody);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "methodBody").Message, ex.Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestControlFlowGraphCreateFromSyntax()
        {
            var source = @"
class C
{
    void M(int x)
    {
        x = 0;
    }
}";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("c", new[] { tree });
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var methodBodySyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Last();

            // Verify ArgumentNullException
            bool exceptionThrown = false;
            try
            {
                _ = ControlFlowGraph.Create(node: null, model);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(ex.Message, new ArgumentNullException("node").Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            try
            {
                _ = ControlFlowGraph.Create(methodBodySyntax, semanticModel: null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(ex.Message, new ArgumentNullException("semanticModel").Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            // Verify argument exception on providing a syntax node in executable code which does not produce root operation.
            try
            {
                var literal = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
                _ = ControlFlowGraph.Create(literal, model);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(ex.Message, new ArgumentException(CodeAnalysisResources.NotARootOperation, "operation").Message);
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
            exceptionThrown = false;

            // Verify null return for non-executable code syntax node, which does not produce an operation.
            var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            Assert.Null(ControlFlowGraph.Create(classDecl, model));

            // Verify identical CFG from method body syntax and operation.
            var cfgFromSyntax = ControlFlowGraph.Create(methodBodySyntax, model);
            Assert.NotNull(cfgFromSyntax);

            var operation = (IMethodBodyOperation)model.GetOperation(methodBodySyntax);
            var cfgFromOperation = ControlFlowGraph.Create(operation);
            Assert.NotNull(cfgFromOperation);

            var expectedCfg = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[missing], IsInvalid) (Syntax: 'x = 0')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32[missing]) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32[missing], Constant: 0, IsInvalid) (Syntax: '0')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            ControlFlowGraphVerifier.VerifyGraph(compilation, expectedCfg, cfgFromSyntax);
            ControlFlowGraphVerifier.VerifyGraph(compilation, expectedCfg, cfgFromOperation);
        }
    }
}
