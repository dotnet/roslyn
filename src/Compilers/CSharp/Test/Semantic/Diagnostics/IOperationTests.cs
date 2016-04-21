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
    public class IOperationTests : CSharpTestBase
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
        M3(1, 2, 3);
    }

    void M2(int a, int b) { }
    static void M3(int a, params int[] c) { }
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

            // 1

            IArgument argument = arguments[0];
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

            // 2

            argument = arguments[1];
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
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

            // a: 1

            argument = arguments[0];
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);

            // b: 2

            argument = arguments[1];
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "b");
            argumentValue = argument.Value;
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression);
            Assert.False(argumentValue.IsInvalid);
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32);
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 2);

            // M3(1, 2, 3)

            Assert.Equal("M3(1, 2, 3)", nodes[2].ToString());
            operation = model.GetOperation(nodes[2]);
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression);
            Assert.False(operation.IsInvalid);
            invocation = (IInvocationExpression)operation;
            Assert.False(invocation.ConstantValue.HasValue);
            Assert.False(invocation.IsVirtual);
            Assert.Equal(invocation.TargetMethod.Name, "M3");
            Assert.Null(invocation.Instance);
            arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);

            // 1

            argument = arguments[0];
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

            // 2, 3

            argument = arguments[1];
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
        }
    }
}