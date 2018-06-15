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
            Assert.Equal(null, dynamicExpression.GetArgumentName(0));
            Assert.Equal(null, dynamicExpression.GetArgumentRefKind(0));

            // Non-empty arguments and empty argument names/refkinds
            arguments = ImmutableArray.Create((IOperation)null);
            argumentNames = ImmutableArray<string>.Empty;
            argumentRefKinds = ImmutableArray<RefKind>.Empty;
            dynamicExpression = createDynamicExpression(arguments, argumentNames, argumentRefKinds);
            Assert.Equal(null, dynamicExpression.GetArgumentName(0));
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
                (arguments, argumentNames, argumentRefKinds) => new DynamicInvocationExpression(null, arguments, argumentNames, argumentRefKinds, null, null, null, null, false);

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
                (arguments, argumentNames, argumentRefKinds) => new DynamicIndexerAccessExpression(null, arguments, argumentNames, argumentRefKinds, null, null, null, null, false);

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
                (arguments, argumentNames, argumentRefKinds) => new DynamicObjectCreationExpression(arguments, argumentNames, argumentRefKinds, null, null, null, null, null, false);

            TestCore(createDynamicExpression);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestFlowAnalysisFeatureFlag()
        {
            var source = @"
class C
{
    void M()
    {
    }
}";
            var tree = CSharpSyntaxTree.ParseText(source);            

            void testFlowAnalysisFeatureFlagCore(bool expectException)
            {
                var compilation = CSharpCompilation.Create("c", new[] { tree });
                var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
                var methodBodySyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Last();
                var operation = (IMethodBodyOperation)model.GetOperation(methodBodySyntax);

                if (expectException)
                {
                    Assert.Throws<InvalidOperationException>(() => SemanticModel.GetControlFlowGraph(operation));
                }
                else
                {
                    ControlFlowGraph graph = SemanticModel.GetControlFlowGraph(operation);
                    Assert.NotNull(graph);
                    Assert.NotEmpty(graph.Blocks);
                }
            }

            // Test without feature flag.
            testFlowAnalysisFeatureFlagCore(expectException: true);

            // Test with feature flag.
            tree = CSharpSyntaxTree.ParseText(source);
            var options = tree.Options.WithFeatures(new[] { new KeyValuePair<string, string>("flow-analysis", "true") });
            tree = tree.WithRootAndOptions(tree.GetRoot(), options);
            testFlowAnalysisFeatureFlagCore(expectException: false);

            // Test with feature flag, case-insensitive.
            tree = CSharpSyntaxTree.ParseText(source);
            options = tree.Options.WithFeatures(new[] { new KeyValuePair<string, string>("Flow-Analysis", "true") });
            tree = tree.WithRootAndOptions(tree.GetRoot(), options);
            testFlowAnalysisFeatureFlagCore(expectException: false);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestGetFlowGraphNullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => SemanticModel.GetControlFlowGraph((IBlockOperation)null));
            Assert.Throws<ArgumentNullException>(() => SemanticModel.GetControlFlowGraph((IFieldInitializerOperation)null));
            Assert.Throws<ArgumentNullException>(() => SemanticModel.GetControlFlowGraph((IPropertyInitializerOperation)null));
            Assert.Throws<ArgumentNullException>(() => SemanticModel.GetControlFlowGraph((IParameterInitializerOperation)null));
            Assert.Throws<ArgumentNullException>(() => SemanticModel.GetControlFlowGraph((IConstructorBodyOperation)null));
            Assert.Throws<ArgumentNullException>(() => SemanticModel.GetControlFlowGraph((IMethodBodyOperation)null));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestGetFlowGraphInvalidArgumentWithNonNullParent()
        {
            IOperation parent = new BlockStatement(ImmutableArray<IOperation>.Empty, ImmutableArray<ILocalSymbol>.Empty,
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
            try
            {
                IBlockOperation block = new BlockStatement(
                    ImmutableArray<IOperation>.Empty, ImmutableArray<ILocalSymbol>.Empty,
                    semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
                block = Operation.SetParentOperation(block, parent);
                _ = SemanticModel.GetControlFlowGraph(block);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "body").Message, ex.Message);
            }

            try
            {
                IFieldInitializerOperation initializer = new FieldInitializer(
                    ImmutableArray<ILocalSymbol>.Empty, ImmutableArray<IFieldSymbol>.Empty,
                    value: null, kind: OperationKind.FieldInitializer,
                    semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
                initializer = Operation.SetParentOperation(initializer, parent);
                _ = SemanticModel.GetControlFlowGraph(initializer);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "initializer").Message, ex.Message);
            }

            try
            {
                IPropertyInitializerOperation initializer = new PropertyInitializer(
                    ImmutableArray<ILocalSymbol>.Empty, ImmutableArray<IPropertySymbol>.Empty,
                    value: null, kind: OperationKind.PropertyInitializer,
                    semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
                initializer = Operation.SetParentOperation(initializer, parent);
                _ = SemanticModel.GetControlFlowGraph(initializer);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "initializer").Message, ex.Message);
            }

            try
            {
                IParameterInitializerOperation initializer = new ParameterInitializer(
                                    ImmutableArray<ILocalSymbol>.Empty, parameter: null,
                                    value: null, kind: OperationKind.ParameterInitializer,
                                    semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
                initializer = Operation.SetParentOperation(initializer, parent);
                _ = SemanticModel.GetControlFlowGraph(initializer);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "initializer").Message, ex.Message);
            }

            try
            {
                IConstructorBodyOperation constructorBody = new ConstructorBodyOperation(
                                    ImmutableArray<ILocalSymbol>.Empty,
                                    initializer: null,
                                    blockBody: null,
                                    expressionBody: null,
                                    semanticModel: null, syntax: null);
                constructorBody = Operation.SetParentOperation(constructorBody, parent);
                _ = SemanticModel.GetControlFlowGraph(constructorBody);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "constructorBody").Message, ex.Message);
            }

            try
            {
                IMethodBodyOperation methodBody = new MethodBodyOperation(
                                                    blockBody: null,
                                                    expressionBody: null,
                                                    semanticModel: null, syntax: null);
                methodBody = Operation.SetParentOperation(methodBody, parent);
                _ = SemanticModel.GetControlFlowGraph(methodBody);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(new ArgumentException(argumentExceptionMessage, "methodBody").Message, ex.Message);
            }
        }
    }
}
