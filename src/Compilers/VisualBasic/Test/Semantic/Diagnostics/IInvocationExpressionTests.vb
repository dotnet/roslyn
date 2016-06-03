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
    Shared Function M3(d As Double) As Double
        Return d
    End Function
End Class
]]>
                             </file>
                         </compilation>


            Dim model As SemanticModel = Nothing
            Dim nodes As InvocationExpressionSyntax() = GetInvocations(source, 3, model)

            ' M2(1, 2)

            Dim invocation As IInvocationExpression = CheckInvocation(nodes(0), model, "M2(1, 2)", "M2", 2, SpecialType.System_Void)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "C")

            ' 1

            Dim argument As IArgument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 1)

            ' 2

            argument = GetArgument(invocation, 1)
            CheckConstantArgument(invocation, argument, "b", 2)

            ' local.M2(b:=2, a:=1)

            invocation = CheckInvocation(nodes(1), model, "local.M2(b:=2, a:=1)", "M2", 2, SpecialType.System_Void)
            CheckLocalReference(invocation.Instance, "local", "C")

            ' a:=1

            argument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 1)

            ' b:=2

            argument = GetArgument(invocation, 1)
            CheckConstantArgument(invocation, argument, "b", 2)

            ' M3(x)

            invocation = CheckInvocation(nodes(2), model, "M3(x)", "M3", 1, SpecialType.System_Double)
            Assert.Null(invocation.Instance)

            ' x

            argument = GetArgument(invocation, 0)
            Dim argumentValue As IOperation = CheckArgument(invocation, argument, "d")

            Assert.Equal(argumentValue.Kind, OperationKind.ConversionExpression)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Double)
            CheckLocalReference(DirectCast(argumentValue, IConversionExpression).Operand, "x", "Int32")
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
        M2(c:=New Integer() { 2, 3 }, a:=1)
    End Sub

    Shared Sub M2(a As Integer, ParamArray c As Integer())
    End Sub
End CLass
]]>
                             </file>
                         </compilation>

            Dim model As SemanticModel = Nothing
            Dim nodes As InvocationExpressionSyntax() = GetInvocations(source, 5, model)

            ' M2(1, 2, 3)

            Dim invocation As IInvocationExpression = CheckInvocation(nodes(0), model, "M2(1, 2, 3)", "M2", 2, SpecialType.System_Void)
            Assert.Null(invocation.Instance())

            ' 1

            Dim argument As IArgument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 1)

            ' 2, 3

            argument = GetArgument(invocation, 1)
            Dim argumentValue As IOperation = CheckArgument(invocation, argument, "c")
            CheckArrayCreation(argumentValue, 2, 3)

            ' M2(1)

            invocation = CheckInvocation(nodes(1), model, "M2(1)", "M2", 2, SpecialType.System_Void)
            Assert.Null(invocation.Instance)

            ' 1

            argument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 1)

            ' ()

            argument = GetArgument(invocation, 1)
            argumentValue = CheckArgument(invocation, argument, "c")
            CheckArrayCreation(argumentValue)

            ' M2()

            Assert.Equal("M2()", nodes(2).ToString())
            Dim operation As IOperation = model.GetOperation(nodes(2))
            ' The VB compiler does not treat this as invocation expression that is invalid--instead it's just an invalid expression.
            Assert.Equal(operation.Kind, OperationKind.InvalidExpression)
            Assert.True(operation.IsInvalid)
            Dim invalid As IInvalidExpression = DirectCast(operation, IInvalidExpression)
            Assert.Equal(invalid.Type.SpecialType, SpecialType.System_Void)

            ' M2(1, New Integer() { 2, 3 })

            invocation = CheckInvocation(nodes(3), model, "M2(1, New Integer() { 2, 3 })", "M2", 2, SpecialType.System_Void)
            Assert.Null(invocation.Instance)

            ' 1

            argument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 1)

            ' New Integer() { 2, 3 }

            argument = GetArgument(invocation, 1)
            argumentValue = CheckArgument(invocation, argument, "c")
            CheckArrayCreation(argumentValue, 2, 3)

            ' M2(c:=New Integer() { 2, 3 }, a:=1)

            Assert.Equal("M2(c:=New Integer() { 2, 3 }, a:=1)", nodes(4).ToString())
            operation = model.GetOperation(nodes(4))
            ' VB does not allow using a named argument to match a ParamArray parameter.
            Assert.Equal(operation.Kind, OperationKind.InvalidExpression)
            Assert.True(operation.IsInvalid)
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

            Dim model As SemanticModel = Nothing
            Dim nodes As InvocationExpressionSyntax() = GetInvocations(source, 4, model)

            ' M2()

            Dim invocation As IInvocationExpression = CheckInvocation(nodes(0), model, "M2()", "M2", 0, SpecialType.System_Void, IsVirtual:=True)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "Derived")

            ' Me.M2()

            invocation = CheckInvocation(nodes(1), model, "Me.M2()", "M2", 0, SpecialType.System_Void, IsVirtual:=True)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Explicit, "Derived")

            ' MyClass.M2()

            invocation = CheckInvocation(nodes(2), model, "MyClass.M2()", "M2", 0, SpecialType.System_Void, IsVirtual:=False)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.ThisClass, "Derived")

            ' MyBase.M2()

            invocation = CheckInvocation(nodes(3), model, "MyBase.M2()", "M2", 0, SpecialType.System_Void, IsVirtual:=False)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.BaseClass, "Base")
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

            Dim model As SemanticModel = Nothing
            Dim nodes As InvocationExpressionSyntax() = GetInvocations(source, 2, model)

            '  M2(1, c:=3)

            Dim invocation As IInvocationExpression = CheckInvocation(nodes(0), model, "M2(1, c:=3)", "M2", 3, SpecialType.System_Void)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "C")

            ' 1

            Dim argument As IArgument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 1)

            ' 20

            argument = GetArgument(invocation, 1)
            CheckConstantArgument(invocation, argument, "b", 20)

            ' c:=3

            argument = GetArgument(invocation, 2)
            CheckConstantArgument(invocation, argument, "c", 3)

            '  M2(b:=2)

            invocation = CheckInvocation(nodes(1), model, "M2(b:=2)", "M2", 3, SpecialType.System_Void)
            CheckInstanceReference(invocation.Instance, InstanceReferenceKind.Implicit, "C")

            ' 10

            argument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "a", 10)

            ' b:=2

            argument = GetArgument(invocation, 1)
            CheckConstantArgument(invocation, argument, "b", 2)

            ' 30

            argument = GetArgument(invocation, 2)
            CheckConstantArgument(invocation, argument, "c", 30)
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
        b = f(arg2:=2, arg1:=1)
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim model As SemanticModel = Nothing
            Dim nodes As InvocationExpressionSyntax() = GetInvocations(source, 2, model)

            '  f(1, 2)

            Dim invocation As IInvocationExpression = CheckInvocation(nodes(0), model, "f(1, 2)", "Invoke", 2, SpecialType.System_Boolean, IsVirtual:=True)

            ' 1

            Dim argument As IArgument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "arg1", 1)

            ' 2

            argument = GetArgument(invocation, 1)
            CheckConstantArgument(invocation, argument, "arg2", 2)

            '  f(arg2:=2, arg1:=1)

            invocation = CheckInvocation(nodes(1), model, "f(arg2:=2, arg1:=1)", "Invoke", 2, SpecialType.System_Boolean, IsVirtual:=True)

            ' arg1:=1

            argument = GetArgument(invocation, 0)
            CheckConstantArgument(invocation, argument, "arg1", 1)

            ' arg2:=2

            argument = GetArgument(invocation, 1)
            CheckConstantArgument(invocation, argument, "arg2", 2)
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
        F(zz:=y, yy:=y, xx:=x)
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

            Dim model As SemanticModel = Nothing
            Dim nodes As InvocationExpressionSyntax() = GetInvocations(source, 2, model)

            '  F(x, y, y)

            Dim invocation As IInvocationExpression = CheckInvocation(nodes(0), model, "F(x, y, y)", "F", 3, SpecialType.System_Void)

            ' x

            Dim argument As IArgument = GetArgument(invocation, 0)
            Dim argumentValue As IOperation = CheckArgument(invocation, argument, "xx")
            CheckLocalReference(argumentValue, "x", "Int32")

            ' y

            argument = GetArgument(invocation, 1)
            argumentValue = CheckArgument(invocation, argument, "yy")
            CheckPropertyReference(argumentValue, "y", "Int32")

            ' y

            argument = GetArgument(invocation, 2)
            argumentValue = CheckArgument(invocation, argument, "zz", allowConversions:=True)
            CheckPropertyReference(argumentValue, "y", "Int32")
            CheckArgumentConversions(argument, SpecialType.System_Int32, SpecialType.System_Double)

            '  F(zz:=y, yy:=y, xx:=x)

            invocation = CheckInvocation(nodes(1), model, "F(zz:=y, yy:=y, xx:=x)", "F", 3, SpecialType.System_Void)

            ' xx:=x

            argument = GetArgument(invocation, 0)
            argumentValue = CheckArgument(invocation, argument, "xx")
            CheckLocalReference(argumentValue, "x", "Int32")

            ' yy:=y

            argument = GetArgument(invocation, 1)
            argumentValue = CheckArgument(invocation, argument, "yy")
            CheckPropertyReference(argumentValue, "y", "Int32")

            ' zz:=y

            argument = GetArgument(invocation, 2)
            argumentValue = CheckArgument(invocation, argument, "zz", allowConversions:=True)
            CheckPropertyReference(argumentValue, "y", "Int32")
            CheckArgumentConversions(argument, SpecialType.System_Int32, SpecialType.System_Double)
        End Sub

        Private Shared Function GetInvocations(source As Xml.Linq.XElement, invocationsCount As Integer, ByRef model As SemanticModel) As InvocationExpressionSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.Regular)
            Dim tree = compilation.SyntaxTrees.Single()
            model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax).ToArray()
            Assert.Equal(invocationsCount, nodes.Length)

            Return nodes
        End Function

        Private Shared Function CheckInvocation(node As InvocationExpressionSyntax, model As SemanticModel, expressionText As String, methodName As String, argumentCount As Integer, resultType As SpecialType, Optional isInvalid As Boolean = False, Optional IsVirtual As Boolean = False) As IInvocationExpression
            Assert.Equal(expressionText, node.ToString())
            Dim operation As IOperation = model.GetOperation(node)
            Assert.Equal(operation.Kind, OperationKind.InvocationExpression)
            Assert.Equal(isInvalid, operation.IsInvalid)
            Dim invocation As IInvocationExpression = DirectCast(operation, IInvocationExpression)
            Assert.False(invocation.ConstantValue.HasValue)
            Assert.Equal(IsVirtual, invocation.IsVirtual)
            Assert.Equal(methodName, invocation.TargetMethod.Name)
            Assert.Equal(argumentCount, invocation.ArgumentsInParameterOrder.Length)
            Assert.Equal(argumentCount, invocation.ArgumentsInEvaluationOrder.Length)
            Assert.Equal(resultType, invocation.Type.SpecialType)

            Return invocation
        End Function


        Private Shared Function GetArgument(invocation As IInvocationExpression, index As Integer) As IArgument
            Dim argument As IArgument = invocation.ArgumentsInParameterOrder(index)
            Assert.True(argument Is invocation.ArgumentsInEvaluationOrder(index))
            Return argument
        End Function

        Private Shared Function CheckArgument(invocation As IInvocationExpression, argument As IArgument, parameterName As String, Optional isInvalid As Boolean = False, Optional allowConversions As Boolean = False) As IOperation
            Assert.Equal(isInvalid, argument.IsInvalid)
            If Not allowConversions Then
                Assert.Null(argument.InConversion)
                Assert.Null(argument.OutConversion)
            End If
            Assert.Equal(parameterName, argument.Parameter.Name)
            Assert.True(invocation.GetArgumentMatchingParameter(argument.Parameter) Is argument)
            Dim argumentValue As IOperation = argument.Value
            Assert.Equal(isInvalid, argumentValue.IsInvalid)

            Return argumentValue
        End Function

        Private Shared Sub CheckConstantArgument(invocation As IInvocationExpression, argument As IArgument, parameterName As String, argumentConstantValue As Integer)
            Dim argumentValue As IOperation = CheckArgument(invocation, argument, parameterName)
            Assert.Equal(argumentValue.Kind, OperationKind.LiteralExpression)
            Assert.Equal(argumentValue.Type.SpecialType, SpecialType.System_Int32)
            Assert.True(argumentValue.ConstantValue.HasValue)
            Assert.Equal(argumentConstantValue, argumentValue.ConstantValue.Value)
        End Sub

        Private Shared Sub CheckArgumentConversions(argument As IArgument, argumentType As SpecialType, parameterType As SpecialType)
            Dim inConversion As IOperation = argument.InConversion
            Assert.NotNull(inConversion)
            Assert.Equal(inConversion.Kind, OperationKind.ConversionExpression)
            Dim conversion As IConversionExpression = DirectCast(inConversion, IConversionExpression)
            Assert.Equal(conversion.Type.SpecialType, parameterType)
            Assert.Equal(conversion.Operand.Type.SpecialType, argumentType)
            Dim outConversion As IOperation = argument.OutConversion
            Assert.NotNull(outConversion)
            Assert.Equal(outConversion.Kind, OperationKind.ConversionExpression)
            conversion = DirectCast(outConversion, IConversionExpression)
            Assert.Equal(conversion.Type.SpecialType, argumentType)
            Assert.Equal(conversion.Operand.Type.SpecialType, parameterType)
        End Sub

        Private Shared Sub CheckInstanceReference(invocationInstance As IOperation, instanceKind As InstanceReferenceKind, instanceType As String)
            Assert.NotNull(invocationInstance)
            Assert.Equal(invocationInstance.Kind, OperationKind.InstanceReferenceExpression)
            Dim instanceReference As IInstanceReferenceExpression = DirectCast(invocationInstance, IInstanceReferenceExpression)
            Assert.False(instanceReference.IsInvalid)
            Assert.Equal(instanceKind, instanceReference.InstanceReferenceKind)
            Assert.Equal(instanceType, instanceReference.Type.Name)
        End Sub

        Private Shared Sub CheckLocalReference(reference As IOperation, localName As String, localType As String)
            Assert.NotNull(reference)
            Assert.Equal(reference.Kind, OperationKind.LocalReferenceExpression)
            Dim localReference As ILocalReferenceExpression = DirectCast(reference, ILocalReferenceExpression)
            Assert.False(localReference.IsInvalid)
            Assert.Equal(localName, localReference.Local.Name)
            Assert.Equal(localType, localReference.Type.Name)
            Assert.False(localReference.ConstantValue.HasValue)
        End Sub

        Private Shared Sub CheckPropertyReference(reference As IOperation, propertyName As String, propertyType As String)
            Assert.Equal(reference.Kind, OperationKind.PropertyReferenceExpression)
            Dim propertyReference As IPropertyReferenceExpression = DirectCast(reference, IPropertyReferenceExpression)
            Assert.False(propertyReference.IsInvalid)
            Assert.Equal(propertyName, propertyReference.Property.Name)
            Assert.Equal(propertyType, propertyReference.Type.Name)
            Assert.False(propertyReference.ConstantValue.HasValue)
        End Sub

        Private Shared Sub CheckArrayCreation(value As IOperation, ParamArray elements As Integer())
            Assert.Equal(value.Kind, OperationKind.ArrayCreationExpression)
            Assert.Equal(value.Type.TypeKind, TypeKind.Array)
            Assert.Equal(DirectCast(value.Type, ArrayTypeSymbol).ElementType.SpecialType, SpecialType.System_Int32)
            Assert.False(value.ConstantValue.HasValue)
            Dim argumentArray As IArrayCreationExpression = DirectCast(value, IArrayCreationExpression)
            Assert.Equal(argumentArray.Initializer.Kind, OperationKind.ArrayInitializer)
            Dim elementValues As ImmutableArray(Of IOperation) = argumentArray.Initializer.ElementValues
            Assert.Equal(elementValues.Length, elements.Length)

            For index As Integer = 0 To elements.Length - 1
                Dim elementValue As IOperation = elementValues(index)
                Assert.Equal(elementValue.Kind, OperationKind.LiteralExpression)
                Assert.False(elementValue.IsInvalid)
                Assert.Equal(elementValue.Type.SpecialType, SpecialType.System_Int32)
                Assert.True(elementValue.ConstantValue.HasValue)
                Assert.Equal(elementValue.ConstantValue.Value, elements(index))
            Next
        End Sub
    End Class
End Namespace

