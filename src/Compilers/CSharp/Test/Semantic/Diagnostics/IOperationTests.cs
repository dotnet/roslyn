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
        public void Invocations()
        {
            const string source = @"
class C
{
    void M1()
    {
        M2(1, 2);
        M2(b: 2, a: 1);
        M3(1, 2, 3);
    }

    void M2(int a, int b) { }
    void M3(int a, params int[] c) { }
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
            IInvocationExpression invocation = (IInvocationExpression)operation;
            Optional<object> constant = invocation.ConstantValue;
            Assert.False(constant.HasValue);
            ImmutableArray<IArgument> arguments = invocation.ArgumentsInParameterOrder;
            Assert.Equal(arguments.Length, 2);
            IArgument argument = arguments[0];
            Assert.False(argument.IsInvalid);
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional);
            Assert.Null(argument.InConversion);
            Assert.Null(argument.OutConversion);
            Assert.Equal(argument.Parameter.Name, "a");
            IOperation argumentValue = argument.Value;
            Assert.True(argumentValue.ConstantValue.HasValue);
            Assert.Equal(argumentValue.ConstantValue.Value, 1);
        }
    }
}