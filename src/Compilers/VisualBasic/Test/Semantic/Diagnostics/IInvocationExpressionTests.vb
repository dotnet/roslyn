' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

        <Fact>
        Public Sub ParamArrayInvocations()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub M1()
        M2(1, 2, 3)
        M2(1)
        M2()
        M2(1, New Integer() { 2, 3 })
    End Sub

    Shared Sub M2(a As Integer, ParamArray c As Integer())
    End Sub
End CLass
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(nodes.Length, 4)

            ' M2(1, 2, 3)

            Assert.Equal("M2(1, 2, 3)", nodes(0).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.Null(invocation.Instance)
            Dim arguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 2)

            Dim evaluationOrderArguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 2)

            ' 1

            Dim argument As IArgument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "a")
            Dim argumentValue As IOperation = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 1)

            ' 2, 3

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "c")
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.ArrayCreationExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.TypeKind, TypeKind.Array)
            Assert.Equal(DirectCast(argumentValue.Type, ArrayTypeSymbol).ElementType.SpecialType, SpecialType.System_Int32)
            Assert.False(argumentValue.ConstantValue.HasValue)
            Dim argumentArray As IArrayCreationExpression = DirectCast(argumentValue, IArrayCreationExpression)
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer)
            Assert.Equal(argumentArray.Initializer.ElementValues.Length, 2)

            ' 2

            argumentValue = argumentArray.Initializer.ElementValues(0)
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 2)

            ' 3

            argumentValue = argumentArray.Initializer.ElementValues(1)
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 3)

            ' M2(1)

            Assert.Equal("M2(1)", nodes(1).ToString())
            operation = model.GetOperation(nodes(1))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.Null(invocation.Instance)
            arguments = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 2)

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 2)

            ' 1

            argument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "a")
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 1)

            ' ()

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "c")
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.ArrayCreationExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.TypeKind, TypeKind.Array)
            Assert.Equal(DirectCast(argumentValue.Type, ArrayTypeSymbol).ElementType.SpecialType, SpecialType.System_Int32)
            Assert.False(argumentValue.ConstantValue.HasValue)
            argumentArray = DirectCast(argumentValue, IArrayCreationExpression)
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer)
            Assert.Equal(argumentArray.Initializer.ElementValues.Length, 0)

            ' M2()

            Assert.Equal("M2()", nodes(2).ToString())
            operation = model.GetOperation(nodes(2))
            ' The VB compiler does not treat this as invocation expression that is invalid--instead it's just an invalid expression.
            Assert.Equal(operation.Kind, OperationKind.InvalidExpression)
            Assert.True(operation.IsInvalid)
            Dim invalid As IInvalidExpression = DirectCast(operation, IInvalidExpression)
            Assert.Equal(invalid.Type.SpecialType, SpecialType.System_Void)

            ' M2(1, New Integer() { 2, 3 })

            Assert.Equal("M2(1, New Integer() { 2, 3 })", nodes(3).ToString())
            operation = model.GetOperation(nodes(3))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.Null(invocation.Instance)
            arguments = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 2)

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 2)

            ' 1

            argument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "a")
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 1)

            ' New Integer() { 2, 3 }

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "c")
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.ArrayCreationExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.TypeKind, TypeKind.Array)
            Assert.Equal(DirectCast(argumentValue.Type, ArrayTypeSymbol).ElementType.SpecialType, SpecialType.System_Int32)
            Assert.False(argumentValue.ConstantValue.HasValue)
            argumentArray = DirectCast(argumentValue, IArrayCreationExpression)
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer)
            Assert.Equal(argumentArray.Initializer.ElementValues.Length, 2)

            ' 2

            argumentValue = argumentArray.Initializer.ElementValues(0)
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 2)

            ' 3

            argumentValue = argumentArray.Initializer.ElementValues(1)
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 3)
        End Sub

        <Fact>
        Public Sub VirtualInvocations()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class Base
    Public Overridable Sub M2()
    End Sub
End Class

Class Derived
    Inherits Base

    Sub M1()
        M2()
        Me.M2()
        MyClass.M2()
        MyBase.M2()
    End Sub

    Public Overrides Sub M2()
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(nodes.Length, 4)

            ' M2()

            Assert.Equal("M2()", nodes(0).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Assert.True(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.NotNull(invocation.Instance)
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression)
            Dim instanceReference As IInstanceReferenceExpression = DirectCast(invocation.Instance, IInstanceReferenceExpression)
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.Implicit)
            Assert.Equal(instanceReference.Type.Name, "Derived")

            ' Me.M2()

            Assert.Equal("Me.M2()", nodes(1).ToString())
            operation = model.GetOperation(nodes(1))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.True(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.NotNull(invocation.Instance)
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression)
            instanceReference = DirectCast(invocation.Instance, IInstanceReferenceExpression)
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.Explicit)
            Assert.Equal(instanceReference.Type.Name, "Derived")

            ' MyClass.M2()

            Assert.Equal("MyClass.M2()", nodes(2).ToString())
            operation = model.GetOperation(nodes(2))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.NotNull(invocation.Instance)
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression)
            instanceReference = DirectCast(invocation.Instance, IInstanceReferenceExpression)
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.ThisClass)
            Assert.Equal(instanceReference.Type.Name, "Derived")

            ' MyBase.M2()

            Assert.Equal("MyBase.M2()", nodes(3).ToString())
            operation = model.GetOperation(nodes(3))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Assert.NotNull(invocation.Instance)
            Assert.Equal(invocation.Instance.Kind, OperationKind.InstanceReferenceExpression)
            instanceReference = DirectCast(invocation.Instance, IInstanceReferenceExpression)
            Assert.Equal(instanceReference.InstanceReferenceKind, InstanceReferenceKind.BaseClass)
            Assert.Equal(instanceReference.Type.Name, "Base")
        End Sub

        <Fact>
        Public Sub DefaultArgumentInvocations()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub M1()
        M2(1, c:=3)
        M2(b:=2)
    End Sub

    Sub M2(Optional a As Integer= 10, Optional b As Integer = 20, Optional c As Integer = 30)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(nodes.Length, 2)

            '  M2(1, c:=3)

            Assert.Equal("M2(1, c:=3)", nodes(0).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            Dim arguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 3)

            Dim evaluationOrderArguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 3)

            ' 1

            Dim argument As IArgument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
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

            ' 20

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "b")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 20)

            ' c:=3

            argument = arguments(2)
            Assert.True(argument Is evaluationOrderArguments(2))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "c")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 3)

            '  M2(b:=2)

            Assert.Equal("M2(b:=2)", nodes(1).ToString())
            operation = model.GetOperation(nodes(1))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            invocation = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.False(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "M2")
            arguments = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 3)

            evaluationOrderArguments = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 3)

            ' 10

            argument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "a")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 10)

            ' b:=2

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
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

            ' 30

            argument = arguments(2)
            Assert.True(argument Is evaluationOrderArguments(2))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "c")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 30)
        End Sub

        <Fact>
        Public Sub DelegateInvocations()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub M1()
        Dim f As System.Func(Of Integer, Integer, Boolean) = Nothing
        Dim b As Boolean = f(1, 2)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(nodes.Length, 1)

            '  f(1, 2)

            Assert.Equal("f(1, 2)", nodes(0).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.True(invocation.IsVirtual)
            Assert.Equal(invocation.TargetMethod.Name, "Invoke")
            Assert.Equal(invocation.Type.SpecialType, SpecialType.System_Boolean)
            Dim arguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 2)

            Dim evaluationOrderArguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 2)

            ' 1

            Dim argument As IArgument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "arg1")
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
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "arg2")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentValue.ConstantValue.Value, 2)
        End Sub

        <Fact>
        Public Sub SimpleRefInvocations()

            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Sub M1()
        Dim x as Integer = 10
        F(x, y, y)
    End Sub

    Property y As Integer
        Get
            Return 10
        End Get
        Set
        End Set
    End Property

    Sub F(ByRef xx As Integer, ByRef yy As Integer, ByRef zz As Double)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(nodes.Length, 1)

            '  F(x, y, y)

            Assert.Equal("F(x, y, y)", nodes(0).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.False(operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Dim arguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInParameterOrder
            Assert.Equal(arguments.Length, 3)

            Dim evaluationOrderArguments As ImmutableArray(Of IArgument) = invocation.ArgumentsInEvaluationOrder
            Assert.Equal(evaluationOrderArguments.Length, 3)

            ' x

            Dim argument As IArgument = arguments(0)
            Assert.True(argument Is evaluationOrderArguments(0))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "xx")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            Dim argumentValue As IOperation = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.LocalReferenceExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.False(argumentValue.ConstantValue.HasValue)

            ' y

            argument = arguments(1)
            Assert.True(argument Is evaluationOrderArguments(1))
            Assert.False(argument.IsInvalid)
            Assert.Null(argument.InConversion)
            Assert.Null(argument.OutConversion)
            Assert.Equal(argument.Parameter.Name, "yy")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.PropertyReferenceExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.False(argumentValue.ConstantValue.HasValue)

            ' y

            argument = arguments(2)
            Assert.True(argument Is evaluationOrderArguments(2))
            Assert.False(argument.IsInvalid)
            Dim inConversion As IOperation = argument.InConversion
            Assert.NotNull(inConversion)
            Assert.Equal(inConversion.Kind, OperationKind.ConversionExpression)
            Dim conversion As IConversionExpression = DirectCast(inConversion, IConversionExpression)
            Assert.Equal(conversion.Type.SpecialType, SpecialType.System_Double)
            Assert.Equal(conversion.Operand.Type.SpecialType, SpecialType.System_Int32)
            Dim outConversion As IOperation = argument.OutConversion
            Assert.NotNull(outConversion)
            Assert.Equal(outConversion.Kind, OperationKind.ConversionExpression)
            conversion = DirectCast(outConversion, IConversionExpression)
            Assert.Equal(conversion.Type.SpecialType, SpecialType.System_Int32)
            Assert.Equal(conversion.Operand.Type.SpecialType, SpecialType.System_Double)
            Assert.Equal(argument.Parameter.Name, "zz")
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            argumentValue = argument.Value
            Assert.Equal(argumentValue.Kind, OperationKind.PropertyReferenceExpression)
            Assert.False(argumentValue.IsInvalid)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.False(argumentValue.ConstantValue.HasValue)
        End Sub
    End Class
End Namespace

