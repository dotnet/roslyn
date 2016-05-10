' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class IInvocationExpressionTests
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleInvocations()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C

    Sub M1()
        Dim local As C = Me
        M2(1, 2)
        local.M2(b:=2, a:=1)
        Dim x As Integer = 1
        M3(x)
    End Sub

    Sub M2(a As Integer, b As Integer)
    End Sub
    Function M3(d As Double) As Double
        Return d
    End Function
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(nodes.Length, 3)

            ' M2(1, 2)

            Assert.Equal("M2(1, 2)", nodes(0).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.Equal(invocation.Type.SpecialType, SpecialType.System_Void)
            Assert.NotNull(invocation.Instance)
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression)
            Dim instanceReference As IInstanceReferenceExpression = DirectCast(invocation.Instance, IInstanceReferenceExpression)
            Assert.False(instanceReference.IsInvalid)
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.Implicit)
            Assert.Equal(instanceReference.Type.Name, "C")
            Dim arguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 2)

            Dim evaluationOrderArguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 2)

            ' 1

            Dim argument As IArgument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "a")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            Dim argumentValue As IOperation = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 1)

            ' 2

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "b")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 2)

            ' local.M2(b:=2, a:=1)

            Assert.Equal("local.M2(b:=2, a:=1)", nodes(1).ToString())
            operation = model.GetOperation(nodes(1))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.NotNull(invocation.Instance)
            Assert.Equal(invocation.Instance.Kind, OperationKind.LocalReferenceExpression)
            Dim localReference As ILocalReferenceExpression = DirectCast(invocation.Instance, ILocalReferenceExpression)
            Assert.False(localReference.IsInvalid)
            Assert.Equal(localReference.Local.Name, "local")
            Assert.Equal(localReference.Type.Name, "C")
            arguments = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 2)

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 2)

            ' a:=1

            argument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "a")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 1)

            ' b:=2

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Named)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "b")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 2)

            ' M3(x)

            Assert.Equal("M3(x)", nodes(2).ToString())
            operation = model.GetOperation(nodes(2))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.Equal(invocation.Type.SpecialType, SpecialType.System_Double)
            Assert.Equal(invocation.TargetMethod.Name, "M3")
            arguments = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 1)

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 1)

            ' x

            argument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Equal(argument.ArgumentKind, ArgumentKind.Positional)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "d")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.ConversionExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Double)
            Dim conversion As IConversionExpression = DirectCast(argumentValue, IConversionExpression)
            argumentValue = DirectCast(argumentValue, IConversionExpression).Operand
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.Equal(argumentValue.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(argumentValue, ILocalReferenceExpression).Local.Name, "x")
        End Sub
    End Class
End Namespace

