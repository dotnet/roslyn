' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17591")>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_SByte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.SByte
        Dim i As System.SByte = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.SByte) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Byte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Byte
        Dim i As System.Byte = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Byte) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Byte) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Int16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int16
        Dim i As System.Int16 = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int16) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int16) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_UInt16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt16
        Dim i As System.UInt16 = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt16) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt16) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Int32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int32
        Dim i As System.Int32 = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_UInt32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt32
        Dim i As System.UInt32 = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt32) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Int64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int64
        Dim i As System.Int64 = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int64) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_UInt64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt64
        Dim i As System.UInt64 = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt64) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Decimal()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Decimal
        Dim i As System.Decimal = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Decimal) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Single()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Single
        Dim i As System.Single = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Double()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Double
        Dim i As System.Double = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_System_Object()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Object
        Dim i As System.Object = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_SByte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.SByte
        Dim i As System.SByte = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.SByte) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Int16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int16
        Dim i As System.Int16 = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int16) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int16) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Int32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int32
        Dim i As System.Int32 = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Int64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int64
        Dim i As System.Int64 = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int64) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Decimal()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Decimal
        Dim i As System.Decimal = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Decimal) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Single()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Single
        Dim i As System.Single = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Double()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Double
        Dim i As System.Double = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_System_Object()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Object
        Dim i As System.Object = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_SByte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.SByte
        Dim i As System.SByte = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.SByte) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_Byte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Byte
        Dim i As System.Byte = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Byte) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Byte) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_Int16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int16
        Dim i As System.Int16 = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int16) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int16) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_UInt16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt16
        Dim i As System.UInt16 = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt16) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt16) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_Int32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int32
        Dim i As System.Int32 = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_UInt32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt32
        Dim i As System.UInt32 = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt32) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_Int64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int64
        Dim i As System.Int64 = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int64) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_UInt64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt64
        Dim i As System.UInt64 = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.UInt64) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_Boolean()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Boolean
        Dim i As System.Boolean = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_System_Object()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Object
        Dim i As System.Object = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_SByte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.SByte
        Dim i As System.SByte = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.SByte) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.SByte) (OperationKind.InvocationExpression, Type: System.SByte) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Byte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Byte
        Dim i As System.Byte = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Byte) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Byte) (OperationKind.InvocationExpression, Type: System.Byte) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Int16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int16
        Dim i As System.Int16 = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int16) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int16) (OperationKind.InvocationExpression, Type: System.Int16) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_UInt16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt16
        Dim i As System.UInt16 = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt16) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.UInt16) (OperationKind.InvocationExpression, Type: System.UInt16) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Int32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int32
        Dim i As System.Int32 = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_UInt32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt32
        Dim i As System.UInt32 = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.UInt32) (OperationKind.InvocationExpression, Type: System.UInt32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Int64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int64
        Dim i As System.Int64 = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int64) (OperationKind.InvocationExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_UInt64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt64
        Dim i As System.UInt64 = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.UInt64) (OperationKind.InvocationExpression, Type: System.UInt64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Decimal()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Decimal
        Dim i As System.Decimal = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Decimal) (OperationKind.InvocationExpression, Type: System.Decimal) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Single()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Single
        Dim i As System.Single = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Single) (OperationKind.InvocationExpression, Type: System.Single) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Double()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Double
        Dim i As System.Double = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Double) (OperationKind.InvocationExpression, Type: System.Double) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_System_Object()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Object
        Dim i As System.Object = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Object) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_SByte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.SByte
        Dim i As System.SByte = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.SByte) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.SByte) (OperationKind.InvocationExpression, Type: System.SByte) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Int16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int16
        Dim i As System.Int16 = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int16) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int16) (OperationKind.InvocationExpression, Type: System.Int16) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Int32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int32
        Dim i As System.Int32 = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Int64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int64
        Dim i As System.Int64 = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int64) (OperationKind.InvocationExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Decimal()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Decimal
        Dim i As System.Decimal = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Decimal) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Decimal) (OperationKind.InvocationExpression, Type: System.Decimal) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Single()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Single
        Dim i As System.Single = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Single) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Single) (OperationKind.InvocationExpression, Type: System.Single) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Double()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Double
        Dim i As System.Double = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Double) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Double) (OperationKind.InvocationExpression, Type: System.Double) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_System_Object()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Object
        Dim i As System.Object = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Object) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_SByte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.SByte
        Dim i As System.SByte = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.SByte) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.SByte) (OperationKind.InvocationExpression, Type: System.SByte) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_Byte()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Byte
        Dim i As System.Byte = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Byte) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Byte) (OperationKind.InvocationExpression, Type: System.Byte) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_Int16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int16
        Dim i As System.Int16 = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int16) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int16) (OperationKind.InvocationExpression, Type: System.Int16) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_UInt16()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt16
        Dim i As System.UInt16 = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt16) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.UInt16) (OperationKind.InvocationExpression, Type: System.UInt16) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_Int32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int32
        Dim i As System.Int32 = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_UInt32()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt32
        Dim i As System.UInt32 = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt32) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.UInt32) (OperationKind.InvocationExpression, Type: System.UInt32) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_Int64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Int64
        Dim i As System.Int64 = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int64) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Int64) (OperationKind.InvocationExpression, Type: System.Int64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_UInt64()
            Dim source = <![CDATA[
Class A
    Function Method() As System.UInt64
        Dim i As System.UInt64 = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.UInt64) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.UInt64) (OperationKind.InvocationExpression, Type: System.UInt64) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_Boolean()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Boolean
        Dim i As System.Boolean = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_System_Object()
            Dim source = <![CDATA[
Class A
    Function Method() As System.Object
        Dim i As System.Object = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As System.Object) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_E()
            Dim source = <![CDATA[
Class A
    Function Method() As E
        Dim i As E = Nothing
        Return +i'BIND:"+i"
    End Function
End Class
Enum E
A
B
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E) (Syntax: '+i')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '+i')
      Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: E) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_E()
            Dim source = <![CDATA[
Class A
    Function Method() As E
        Dim i As E = Nothing
        Return -i'BIND:"-i"
    End Function
End Class
Enum E
A
B
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E) (Syntax: '-i')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-i')
      Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: E) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_E()
            Dim source = <![CDATA[
Class A
    Function Method() As E
        Dim i As E = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class
Enum E
A
B
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: E) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: E) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_E()
            Dim source = <![CDATA[
Class A
    Function Method() As E
        Dim i As E = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class
Enum E
A
B
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E) (Syntax: '+Method()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '+Method()')
      Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'Method()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IInvocationExpression ( Function A.Method() As E) (OperationKind.InvocationExpression, Type: E) (Syntax: 'Method()')
              Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
              Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_E()
            Dim source = <![CDATA[
Class A
    Function Method() As E
        Dim i As E = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class
Enum E
A
B
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: E) (Syntax: '-Method()')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-Method()')
      Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'Method()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IInvocationExpression ( Function A.Method() As E) (OperationKind.InvocationExpression, Type: E) (Syntax: 'Method()')
              Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
              Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_E()
            Dim source = <![CDATA[
Class A
    Function Method() As E
        Dim i As E = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class
Enum E
A
B
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: E) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As E) (OperationKind.InvocationExpression, Type: E) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Plus_CustomType()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

Public Class CustomType
    Public Shared Operator -(x As CustomType) As CustomType
        Return x
    End Operator

    Public Shared operator +(x As CustomType) As CustomType
        return x
    End Operator

    Public Shared operator Not(x As CustomType) As CustomType
        return x
    End Operator
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus) (OperatorMethod: Function CustomType.op_UnaryPlus(x As CustomType) As CustomType) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '+i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Minus_CustomType()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return -i'BIND:"-i"
    End Function
End Class

Public Class CustomType
    Public Shared Operator -(x As CustomType) As CustomType
        Return x
    End Operator

    Public Shared operator +(x As CustomType) As CustomType
        return x
    End Operator

    Public Shared operator Not(x As CustomType) As CustomType
        return x
    End Operator
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperatorMethod: Function CustomType.op_UnaryNegation(x As CustomType) As CustomType) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '-i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Type_Not_CustomType()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return Not i'BIND:"Not i"
    End Function
End Class

Public Class CustomType
    Public Shared Operator -(x As CustomType) As CustomType
        Return x
    End Operator

    Public Shared operator +(x As CustomType) As CustomType
        return x
    End Operator

    Public Shared operator Not(x As CustomType) As CustomType
        return x
    End Operator
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not) (OperatorMethod: Function CustomType.op_OnesComplement(x As CustomType) As CustomType) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: 'Not i')
  Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Plus_CustomType()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return +Method()'BIND:"+Method()"
    End Function
End Class

Public Class CustomType
    Public Shared Operator -(x As CustomType) As CustomType
        Return x
    End Operator

    Public Shared operator +(x As CustomType) As CustomType
        return x
    End Operator

    Public Shared operator Not(x As CustomType) As CustomType
        return x
    End Operator
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus) (OperatorMethod: Function CustomType.op_UnaryPlus(x As CustomType) As CustomType) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '+Method()')
  Operand: IInvocationExpression ( Function A.Method() As CustomType) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Minus_CustomType()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return -Method()'BIND:"-Method()"
    End Function
End Class

Public Class CustomType
    Public Shared Operator -(x As CustomType) As CustomType
        Return x
    End Operator

    Public Shared operator +(x As CustomType) As CustomType
        return x
    End Operator

    Public Shared operator Not(x As CustomType) As CustomType
        return x
    End Operator
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperatorMethod: Function CustomType.op_UnaryNegation(x As CustomType) As CustomType) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: '-Method()')
  Operand: IInvocationExpression ( Function A.Method() As CustomType) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_Method_Not_CustomType()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return Not Method()'BIND:"Not Method()"
    End Function
End Class

Public Class CustomType
    Public Shared Operator -(x As CustomType) As CustomType
        Return x
    End Operator

    Public Shared operator +(x As CustomType) As CustomType
        return x
    End Operator

    Public Shared operator Not(x As CustomType) As CustomType
        return x
    End Operator
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Not) (OperatorMethod: Function CustomType.op_OnesComplement(x As CustomType) As CustomType) (OperationKind.UnaryOperatorExpression, Type: CustomType) (Syntax: 'Not Method()')
  Operand: IInvocationExpression ( Function A.Method() As CustomType) (OperationKind.InvocationExpression, Type: CustomType) (Syntax: 'Method()')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: A) (Syntax: 'Method')
      Arguments(0)
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(18135, "https://github.com/dotnet/roslyn/issues/18135")>
        Public Sub Test_UnaryOperatorExpression_Type_And_TrueFalse()
            Dim source = <![CDATA[

Public Class CustomType
    Public Shared Operator IsTrue(x As CustomType) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As CustomType) As Boolean
        Return False
    End Operator

    Public Shared Operator And(x As CustomType, y As CustomType) As CustomType
        Return x
    End Operator

    Public Shared Operator Or(x As CustomType, y As CustomType) As CustomType
        Return x
    End Operator
End Class

Class A
    Sub Method()
        Dim x As CustomType = New CustomType()
        Dim y As CustomType = New CustomType()
        If x AndAlso y Then'BIND:"If x AndAlso y Then"
        End If
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If x AndAls ... End If')
  Condition: IUnaryOperatorExpression (UnaryOperatorKind.True) (OperatorMethod: Function CustomType.op_True(x As CustomType) As System.Boolean) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'x AndAlso y')
      Operand: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalAnd) (OperatorMethod: Function CustomType.op_BitwiseAnd(x As CustomType, y As CustomType) As CustomType) (OperationKind.BinaryOperatorExpression, Type: CustomType) (Syntax: 'x AndAlso y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'x')
          Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'y')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If x AndAls ... End If')
  IfFalse: null
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(18135, "https://github.com/dotnet/roslyn/issues/18135")>
        Public Sub Test_UnaryOperatorExpression_Type_Or_TrueFalse()
            Dim source = <![CDATA[

Public Class CustomType
    Public Shared Operator IsTrue(x As CustomType) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As CustomType) As Boolean
        Return False
    End Operator

    Public Shared Operator And(x As CustomType, y As CustomType) As CustomType
        Return x
    End Operator

    Public Shared Operator Or(x As CustomType, y As CustomType) As CustomType
        Return x
    End Operator
End Class

Class A
    Sub Method()
        Dim x As CustomType = New CustomType()
        Dim y As CustomType = New CustomType()
        If x OrElse y Then'BIND:"If x OrElse y Then"
        End If
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If x OrElse ... End If')
  Condition: IUnaryOperatorExpression (UnaryOperatorKind.True) (OperatorMethod: Function CustomType.op_True(x As CustomType) As System.Boolean) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'x OrElse y')
      Operand: IBinaryOperatorExpression (BinaryOperatorKind.ConditionalAnd) (OperatorMethod: Function CustomType.op_BitwiseOr(x As CustomType, y As CustomType) As CustomType) (OperationKind.BinaryOperatorExpression, Type: CustomType) (Syntax: 'x OrElse y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'x')
          Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: CustomType) (Syntax: 'y')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If x OrElse ... End If')
  IfFalse: null
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_With_CustomType_NoRightOperator()
            Dim source = <![CDATA[
Class A
    Function Method() As CustomType
        Dim i As CustomType = Nothing
        Return +i'BIND:"+i"
    End Function
End Class

Public Class CustomType
End CLass

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: CustomType, IsInvalid) (Syntax: '+i')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: ?, IsInvalid) (Syntax: '+i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: CustomType, IsInvalid) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_With_CustomType_DerivedTypes()
            Dim source = <![CDATA[
Class A
    Function Method() As BaseType
        Dim i As DerivedType = Nothing
        Return +i 'BIND:"+i"
    End Function
End Class

Public Class BaseType
    Public Shared Operator +(x As BaseType) As BaseType
        Return x
    End Operator
End Class

Public Class DerivedType
    Inherits BaseType
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Plus) (OperatorMethod: Function BaseType.op_UnaryPlus(x As BaseType) As BaseType) (OperationKind.UnaryOperatorExpression, Type: BaseType) (Syntax: '+i')
  Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: BaseType) (Syntax: 'i')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: DerivedType) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_With_CustomType_ImplicitConversion()
            Dim source = <![CDATA[
Class A
    Function Method() As BaseType
        Dim i As DerivedType = Nothing
        Return +i 'BIND:"+i"
    End Function
End Class

Public Class BaseType
    Public Shared Operator +(x As BaseType) As BaseType
        Return x
    End Operator
End Class

Public Class DerivedType
    Public Shared Narrowing Operator CType(ByVal x As DerivedType) As BaseType
        Return New BaseType()
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: BaseType, IsInvalid) (Syntax: '+i')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: ?, IsInvalid) (Syntax: '+i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: DerivedType, IsInvalid) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_With_CustomType_ExplicitConversion()
            Dim source = <![CDATA[
Class A
    Function Method() As BaseType
        Dim i As DerivedType = Nothing
        Return +i 'BIND:"+i"
    End Function
End Class

Public Class BaseType
    Public Shared Operator +(x As BaseType) As BaseType
        Return x
    End Operator
End Class

Public Class DerivedType
    Public Shared Widening Operator CType(ByVal x As DerivedType) As BaseType
        Return New BaseType()
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: BaseType, IsInvalid) (Syntax: '+i')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: ?, IsInvalid) (Syntax: '+i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: DerivedType, IsInvalid) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub Test_UnaryOperatorExpression_With_CustomType_Malformed_Operator()
            Dim source = <![CDATA[
Class A
    Function Method() As BaseType
        Dim i As BaseType = Nothing
        Return +i 'BIND:"+i"
    End Function
End Class

Public Class BaseType
    Public Shared Operator +(x As Integer) As BaseType
        Return New BaseType()
    End Operator
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: BaseType, IsInvalid) (Syntax: '+i')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: ?, IsInvalid) (Syntax: '+i')
      Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: BaseType, IsInvalid) (Syntax: 'i')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyLiftedUnaryOperators1()
            Dim source = <![CDATA[
Class C
    Sub F(x as Integer?)
        dim y = -x 'BIND:"-x"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, IsLifted, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyNonLiftedUnaryOperators1()
            Dim source = <![CDATA[
Class C
    Sub F(x as Integer)
        dim y = -x 'BIND:"-x"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyUncheckedLiftedUnaryOperators1()
            Dim source = <![CDATA[
Class C
    Sub F(x as Integer?)
        dim y = -x 'BIND:"-x"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, IsLifted) (OperationKind.UnaryOperatorExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim references = DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef})
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, references:=references, options:=TestOptions.ReleaseDll.WithOverflowChecks(False))

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(compilation, fileName, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyUncheckedNonLiftedUnaryOperators1()
            Dim source = <![CDATA[
Class C
    Sub F(x as Integer)
        dim y = -x 'BIND:"-x"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim references = DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef})
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, references:=references, options:=TestOptions.ReleaseDll.WithOverflowChecks(False))

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(compilation, fileName, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyLiftedUserDefinedUnaryOperators1()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator -(c as C) as C
    End Operator

    Sub F(x as C?)
        dim y = -x 'BIND:"-x"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus, IsLifted) (OperatorMethod: Function C.op_UnaryNegation(c As C) As C) (OperationKind.UnaryOperatorExpression, Type: System.Nullable(Of C)) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of C)) (Syntax: 'x')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyNonLiftedUserDefinedUnaryOperators1()
            Dim source = <![CDATA[
Structure C
    Public Shared Operator -(c as C) as C
    End Operator

    Sub F(x as C)
        dim y = -x 'BIND:"-x"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperatorMethod: Function C.op_UnaryNegation(c As C) As C) (OperationKind.UnaryOperatorExpression, Type: C) (Syntax: '-x')
  Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
]]>.Value

            VerifyOperationTreeForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
