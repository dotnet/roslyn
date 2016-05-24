// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Semantics;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
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
    static double M3(double d) { return d; }
}";

            SemanticModel model;
            InvocationExpressionSyntax[] nodes = GetInvocations(source, 3, out model);

            // M2(1, 2)

            IInvocationExpression invocation = CheckInvocation(nodes[0], model, "M2(1, 2)", "M2", SpecialType.System_Void);
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "C");
            
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "a", 1);

            // 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            CheckConstantArgument(invocation, argument, "b", 2);

            // local.M2(b: 2, a: 1)

            invocation = CheckInvocation(nodes[1], model, "local.M2(b: 2, a: 1)", "M2", SpecialType.System_Void);
            CheckLocalReference(invocation.Instance, "local", "C");

            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // a: 1

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[1]);
            CheckConstantArgument(invocation, argument, "a", 1);

            // b: 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "b", 2);

            // M3(x)

            invocation = CheckInvocation(nodes[2], model, "M3(x)", "M3", SpecialType.System_Double);
            Assert.Null(invocation.Instance);

            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 1);
            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 1);

            // x

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            IOperation argumentValue = CheckArgument(invocation, argument, "d");

            Assert.Equal(argumentValue.Kind, OperationKind.ConversionExpression);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Double);
            CheckLocalReference(((IConversionExpression)argumentValue).Operand, "x", "Int32");
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
        M2(1, new int[] { 2, 3 });
    }

    static void M2(int a, params int[] c) { }
}";

            SemanticModel model;
            InvocationExpressionSyntax[] nodes = GetInvocations(source, 4, out model);

            // M2(1, 2, 3)

            IInvocationExpression invocation = CheckInvocation(nodes[0], model, "M2(1, 2, 3)", "M2", SpecialType.System_Void);
            Assert.Null(invocation.Instance);

            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "a", 1);

            // 2, 3

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            IOperation argumentValue = CheckArgument(invocation, argument, "c");
            CheckArrayCreation(argumentValue, 2, 3);

            // M2(1)

            invocation = CheckInvocation(nodes[1], model, "M2(1)", "M2", SpecialType.System_Void);
            Assert.Null(invocation.Instance);

            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "a", 1);

            // ()

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            argumentValue = CheckArgument(invocation, argument, "c");
            CheckArrayCreation(argumentValue);

            // M2()

            invocation = CheckInvocation(nodes[2], model, "M2()", "M2", SpecialType.System_Void, isInvalid: true);
            Assert.Null(invocation.Instance);
            
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // ,

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            argumentValue = CheckArgument(invocation, argument, "a", isInvalid: true);
            Assert.Equal(argumentValue.Kind, OperationKind.InvalidExpression);
            Assert.True(argumentValue.IsInvalid);

            // ()

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            argumentValue = CheckArgument(invocation, argument, "c");
            CheckArrayCreation(argumentValue);

            // M2(1, new int[] { 2, 3 })

            invocation = CheckInvocation(nodes[3], model, "M2(1, new int[] { 2, 3 })", "M2", SpecialType.System_Void);
            Assert.Null(invocation.Instance);

            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "a", 1);

            // new int [] { 2, 3 }

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            argumentValue = CheckArgument(invocation, argument, "c");
            CheckArrayCreation(argumentValue, 2, 3);
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

            SemanticModel model;
            InvocationExpressionSyntax[] nodes = GetInvocations(source, 3, out model);

            // M2()

            IInvocationExpression invocation = CheckInvocation(nodes[0], model, "M2()", "M2", SpecialType.System_Void, isVirtual: true);
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "Derived");

            // this.M2()

            invocation = CheckInvocation(nodes[1], model, "this.M2()", "M2", SpecialType.System_Void, isVirtual: true);
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Explicit, "Derived");

            // base.M2()

            invocation = CheckInvocation(nodes[2], model, "base.M2()", "M2", SpecialType.System_Void, isVirtual: false);
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.BaseClass, "Base");
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

            SemanticModel model;
            InvocationExpressionSyntax[] nodes = GetInvocations(source, 2, out model);

            //  M2(1, c: 3)

            IInvocationExpression invocation = CheckInvocation(nodes[0], model, "M2(1, c: 3)", "M2", SpecialType.System_Void);
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "C");

            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 3);
            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 3);
            
            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "a", 1);

            // 20

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[2]);
            CheckConstantArgument(invocation, argument, "b", 20);

            // c: 3

            argument = arguments[2];
            Assert.True(argument == evaluationOrderArguments[1]);
            CheckConstantArgument(invocation, argument, "c", 3);

            //  M2(b: 2)

            invocation = CheckInvocation(nodes[1], model, "M2(b: 2)", "M2", SpecialType.System_Void);
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "C");

            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 3);
            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 3);

            // 10

            argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[1]);
            CheckConstantArgument(invocation, argument, "a", 10);

            // b: 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "b", 2);

            // 30

            argument = arguments[2];
            Assert.True(argument == evaluationOrderArguments[2]);
            CheckConstantArgument(invocation, argument, "c", 30);
        }

        [Fact]
        public void DelegateInvocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        System.Func<int, int, bool> f = null;
        bool b = f(1, 2);
    }
}";

            SemanticModel model;
            InvocationExpressionSyntax[] nodes = GetInvocations(source, 1, out model);

            //  f(1, 2)

            IInvocationExpression invocation = CheckInvocation(nodes[0], model, "f(1, 2)", "Invoke", SpecialType.System_Boolean, isVirtual: true);
           
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // 1

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            CheckConstantArgument(invocation, argument, "arg1", 1);

            // 2

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            CheckConstantArgument(invocation, argument, "arg2", 2);
        }


        [Fact]
        public void RefAndOutInvocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        int x = 10;
        int y;
        F(ref x, out y);
    }

    void F(ref int xx, out int yy) { yy = 12; }
}";

            SemanticModel model;
            InvocationExpressionSyntax[] nodes = GetInvocations(source, 1, out model);

            //  F(ref x, out y)

            IInvocationExpression invocation = CheckInvocation(nodes[0], model, "F(ref x, out y)", "F", SpecialType.System_Void);

            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            ImmutableArray<IArgument> evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder;
            Assert.Equal(evaluationOrderArguments.Length, 2);

            // ref x

            IArgument argument = arguments[0];
            Assert.True(argument == evaluationOrderArguments[0]);
            IOperation argumentValue = CheckArgument(invocation, argument, "xx");
            CheckLocalReference(argumentValue, "x", "Int32");

            // ref y

            argument = arguments[1];
            Assert.True(argument == evaluationOrderArguments[1]);
            argumentValue = CheckArgument(invocation, argument, "yy");
            CheckLocalReference(argumentValue, "y", "Int32");
        }

        private static InvocationExpressionSyntax[] GetInvocations(string source, int invocationsCount, out SemanticModel model)
        {
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = compilation.SyntaxTrees.Single();
            model = compilation.GetSemanticModel(tree);
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Equal(invocationsCount, nodes.Length);

            return nodes;
        }

        private static IInvocationExpression CheckInvocation(InvocationExpressionSyntax node, SemanticModel model, string expressionText, string methodName, SpecialType resultType, bool isInvalid = false, bool isVirtual = false)
        {
            Assert.Equal(expressionText, node.ToString());
            IOperation operation = model.GetOperation(node);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.Equal(isInvalid, operation.IsInvalid);
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.Equal(isVirtual, invocation.IsVirtual);
            Assert.Equal(methodName, invocation.TargetMethod.Name);
            Assert.Equal(resultType, invocation.Type.SpecialType);

            return invocation;
        }

        private static IOperation CheckArgument(IInvocationExpression invocation, IArgument argument, string parameterName, bool isInvalid = false)
        {
            Assert.Equal(isInvalid, argument.IsInvalid);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(parameterName, argument.Parameter.Name);
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) == argument);
            IOperation argumentValue = argument.Value;
            Assert.Equal(isInvalid, argumentValue.IsInvalid);

            return argumentValue;
        }

        private static void CheckConstantArgument(IInvocationExpression invocation, IArgument argument, string parameterName, int argumentConstantValue)
        {
            IOperation argumentValue = CheckArgument(invocation, argument, parameterName);
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentConstantValue, argumentValue.ConstantValue.Value);
        }

        private static void CheckInstanceReference(IOperation invocationInstance, InstanceReferenceKind instanceKind, string instanceType)
        {
            Assert.NotNull(invocationInstance);
            Assert.Equal(invocationInstance.Kind, OperationKind.InstanceReferenceExpression);
            IInstanceReferenceExpression instanceReference = (IInstanceReferenceExpression)invocationInstance;
            Assert.False(instanceReference.IsInvalid);
            Assert.Equal(instanceKind, instanceReference.InstanceReferenceKind);
            Assert.Equal(instanceType, instanceReference.Type.Name);
        }

        private static void CheckLocalReference(IOperation reference, string localName, string localType)
        {
            Assert.NotNull(reference);
            Assert.Equal(reference.Kind, OperationKind.LocalReferenceExpression);
            ILocalReferenceExpression localReference = (ILocalReferenceExpression)reference;
            Assert.False(localReference.IsInvalid);
            Assert.Equal(localName, localReference.Local.Name);
            Assert.Equal(localType, localReference.Type.Name);
        }

        private static void CheckArrayCreation(IOperation value, params int[] elements)
        {
            Assert.Equal(value.Kind, OperationKind.ArrayCreationExpression);
            Assert.Equal(value.Type.TypeKind, TypeKind.Array);
            Assert.Equal(((ArrayTypeSymbol)value.Type).ElementType.SpecialType, SpecialType.System_Int32);
            Assert.False(value.ConstantValue.HasValue);
            IArrayCreationExpression argumentArray = (IArrayCreationExpression)value;
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer);
            ImmutableArray<IOperation> elementValues = argumentArray.Initializer.ElementValues;
            Assert.Equal(elementValues.Length, elements.Length);

            for (int index = 0; index < elements.Length; index++)
            {
                IOperation elementValue = elementValues[index];
                Assert.Equal(elementValue.Kind, OperationKind.LiteralExpression);
                Assert.False(elementValue.IsInvalid);
                Assert.Equal(elementValue.Type.SpecialType, SpecialType.System_Int32);
                Assert.True(elementValue.ConstantValue.HasValue);
                Assert.Equal(elementValue.ConstantValue.Value, elements[index]);
            }
        }
    }
}