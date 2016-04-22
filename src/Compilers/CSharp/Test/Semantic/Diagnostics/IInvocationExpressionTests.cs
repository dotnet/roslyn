// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IInvocationExpressionTests : CSharpTestBase
    {
        [Fact]
        public void SimpleInvocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        C local = this;
        M2(1, 2);
        local.M2(b: 2, a: 1);
        int x = 1;
        M3(x);
    }

    void M2(int a, int b) { }
    void M3(double d) { }
}";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(nodes.Length, 3);

            // M2(1, 2)

            Assert.Equal("M2(1, 2)", nodes[0].ToString());
            IOperation operation = model.GetOperation(nodes[0]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.NotNull(invocation.Instance);
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression);
            IInstanceReferenceExpression instanceReference = (IInstanceReferenceExpression)invocation.Instance;
            Assert.False(instanceReference.IsInvalid);
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.Implicit);
            Assert.Equal(instanceReference.Type.Name, "C");
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            IOperation argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 2);

            // local.M2(b: 2, a: 1)

            Assert.Equal("local.M2(b: 2, a: 1)", nodes[1].ToString());
            operation = model.GetOperation(nodes[1]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.NotNull(invocation.Instance);
            Assert.Equal(invocation.Instance.Kind, OperationKind.LocalReferenceExpression);
            ILocalReferenceExpression localReference = (ILocalReferenceExpression)invocation.Instance;
            Assert.False(localReference.IsInvalid);
            Assert.Equal(localReference.Local.Name, "local");
            Assert.Equal(localReference.Type.Name, "C");
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // a: 1

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // b: 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 2);

            // M3(x)

            Assert.Equal("M3(x)", nodes[2].ToString());
            operation = model.GetOperation(nodes[2]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.Equal(invocation.TargetMethod.Name, "M3");
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 1);

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 1);

            // x

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "d");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.ConversionExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Double);
            IConversionExpression conversion = (IConversionExpression)argumentValue;
            argumentValue = ((IConversionExpression)argumentValue).Operand;
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.Equal(argumentValue.Kind, OperationKind.LocalReferenceExpression);
            Assert.Equal(((ILocalReferenceExpression)argumentValue).Local.Name, "x");
        }

        [Fact]
        public void ParamArrayInvocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        M2(1, 2, 3);
        M2(1);
        M2();
    }

    static void M2(int a, params int[] c) { }
}";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(nodes.Length, 3);

            // M3(1, 2, 3)

            Assert.Equal("M2(1, 2, 3)", nodes[0].ToString());
            IOperation operation = model.GetOperation(nodes[0]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.Null(invocation.Instance);
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            IOperation argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // 2, 3

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.ParamArray);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "c");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.ArrayCreationExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.TypeKind, TypeKind.Array);
            Assert.Equal(((ArrayTypeSymbol)argumentValue.Type).ElementType.SpecialType, SpecialType.System_Int32);
            Assert.False(argumentValue.ConstantValue.HasValue);
            IArrayCreationExpression argumentArray = (IArrayCreationExpression)argumentValue;
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer);
            Assert.Equal(argumentArray.Initializer.ElementValues.Length, 2);

            // 2

            argumentValue = argumentArray.Initializer.ElementValues[0];
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 2);

            // 3

            argumentValue = argumentArray.Initializer.ElementValues[1];
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 3);

            // M3(1)

            Assert.Equal("M2(1)", nodes[1].ToString());
            operation = model.GetOperation(nodes[1]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.Null(invocation.Instance);
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // ()

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.ParamArray);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "c");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.ArrayCreationExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.TypeKind, TypeKind.Array);
            Assert.Equal(((ArrayTypeSymbol)argumentValue.Type).ElementType.SpecialType, SpecialType.System_Int32);
            Assert.False(argumentValue.ConstantValue.HasValue);
            argumentArray = (IArrayCreationExpression)argumentValue;
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer);
            Assert.Equal(argumentArray.Initializer.ElementValues.Length, 0);

            // M3()

            Assert.Equal("M2()", nodes[2].ToString());
            operation = model.GetOperation(nodes[2]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.True(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.Null(invocation.Instance);
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // ,

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.True(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.InvalidExpression);
            Assert.True(argumentValue.IsInvalid);

            // ()

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.ParamArray);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "c");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.ArrayCreationExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.TypeKind, TypeKind.Array);
            Assert.Equal(((ArrayTypeSymbol)argumentValue.Type).ElementType.SpecialType, SpecialType.System_Int32);
            Assert.False(argumentValue.ConstantValue.HasValue);
            argumentArray = (IArrayCreationExpression)argumentValue;
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer);
            Assert.Equal(argumentArray.Initializer.ElementValues.Length, 0);
        }

        [Fact]
        public void VirtualInvocations()
        {
            const string source = @"
class Base
{
    public virtual void M2() { }
}

class Derived : Base
{
    void M1()
    {
        M2();
        this.M2();
        base.M2();
    }

    public override void M2() { }
}";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(nodes.Length, 3);

            // M2()

            Assert.Equal("M2()", nodes[0].ToString());
            IOperation operation = model.GetOperation(nodes[0]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Assert.True(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.NotNull(invocation.Instance);
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression);
            IInstanceReferenceExpression instanceReference = (IInstanceReferenceExpression)invocation.Instance;
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.Implicit);
            Assert.Equal(instanceReference.Type.Name, "Derived");

            // this.M2()

            Assert.Equal("this.M2()", nodes[1].ToString());
            operation = model.GetOperation(nodes[1]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.True(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.NotNull(invocation.Instance);
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression);
            instanceReference = (IInstanceReferenceExpression)invocation.Instance;
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.Explicit);
            Assert.Equal(instanceReference.Type.Name, "Derived");

            // base.M2()

            Assert.Equal("base.M2()", nodes[2].ToString());
            operation = model.GetOperation(nodes[2]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            Assert.NotNull(invocation.Instance);
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression);
            instanceReference = (IInstanceReferenceExpression)invocation.Instance;
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.BaseClass);
            Assert.Equal(instanceReference.Type.Name, "Base");
        }

        [Fact]
        public void DefaultArgumentInvocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        M2(1, c: 3);
        M2(b: 2);
    }

    void M2(int a = 10, int b = 20, int c = 30) { }
}";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(nodes.Length, 2);

            //  M2(1, c: 3)

            Assert.Equal("M2(1, c: 3)", nodes[0].ToString());
            IOperation operation = model.GetOperation(nodes[0]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 3);

            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 3);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            IOperation argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // 20

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[2]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.DefaultValue);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 20);

            // c: 3

            argument = arguments[2];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "c");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 3);

            //  M2(b: 2)

            Assert.Equal("M2(b: 2)", nodes[1].ToString());
            operation = model.GetOperation(nodes[1]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M2");
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 3);

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 3);

            // 10

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.DefaultValue);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 10);

            // b: 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 2);

            // 30

            argument = arguments[2];
            Assert.True(argument == evaluationOrderArguments[2]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.DefaultValue);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "c");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 30);
        }

        [Fact]
        public void DelegateInvocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        System.Func<int, int, int> f = null;
        int x = f(1, 2);
    }
}";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(nodes.Length, 1);

            //  f(1, 2)

            Assert.Equal("f(1, 2)", nodes[0].ToString());
            IOperation operation = model.GetOperation(nodes[0]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.True(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "Invoke");
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            IOperation argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.DefaultValue);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 2);
        }


        [Fact]
        public void RefAndOutInvocations()
        {
            const string source = @"
class Base
{
    public virtual void M2() { }
}

class Derived : Base
{
    void M1()
    {
        M2();
        this.M2();
        base.M2();
    }

    public override void M2() { }
}";

            var comp = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(nodes.Length, 3);
        }
    }
}