' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Public Class ConstantExpressionEvaluatorTests
    Inherits BasicTestBase

    <Fact>
    Public Sub ConstantValueGetDiscriminatorTest01()
        Assert.Equal(ConstantValueTypeDiscriminator.Null, CompileTimeCalculations.GetDiscriminator(TypeCode.DBNull))
        Assert.Equal(ConstantValueTypeDiscriminator.SByte, CompileTimeCalculations.GetDiscriminator(TypeCode.SByte))
        Assert.Equal(ConstantValueTypeDiscriminator.Byte, CompileTimeCalculations.GetDiscriminator(TypeCode.Byte))
        Assert.Equal(ConstantValueTypeDiscriminator.Int16, CompileTimeCalculations.GetDiscriminator(TypeCode.Int16))
        Assert.Equal(ConstantValueTypeDiscriminator.UInt16, CompileTimeCalculations.GetDiscriminator(TypeCode.UInt16))
        Assert.Equal(ConstantValueTypeDiscriminator.Int32, CompileTimeCalculations.GetDiscriminator(TypeCode.Int32))
        Assert.Equal(ConstantValueTypeDiscriminator.UInt32, CompileTimeCalculations.GetDiscriminator(TypeCode.UInt32))
        Assert.Equal(ConstantValueTypeDiscriminator.Int64, CompileTimeCalculations.GetDiscriminator(TypeCode.Int64))
        Assert.Equal(ConstantValueTypeDiscriminator.UInt64, CompileTimeCalculations.GetDiscriminator(TypeCode.UInt64))
        Assert.Equal(ConstantValueTypeDiscriminator.Char, CompileTimeCalculations.GetDiscriminator(TypeCode.Char))
        Assert.Equal(ConstantValueTypeDiscriminator.Boolean, CompileTimeCalculations.GetDiscriminator(TypeCode.Boolean))
        Assert.Equal(ConstantValueTypeDiscriminator.Single, CompileTimeCalculations.GetDiscriminator(TypeCode.Single))
        Assert.Equal(ConstantValueTypeDiscriminator.Double, CompileTimeCalculations.GetDiscriminator(TypeCode.Double))
        Assert.Equal(ConstantValueTypeDiscriminator.Decimal, CompileTimeCalculations.GetDiscriminator(TypeCode.Decimal))
        Assert.Equal(ConstantValueTypeDiscriminator.DateTime, CompileTimeCalculations.GetDiscriminator(TypeCode.DateTime))
        Assert.Equal(ConstantValueTypeDiscriminator.String, CompileTimeCalculations.GetDiscriminator(TypeCode.String))
        Assert.Equal(ConstantValueTypeDiscriminator.Bad, CompileTimeCalculations.GetDiscriminator(TypeCode.Empty))
        Assert.Equal(ConstantValueTypeDiscriminator.Bad, CompileTimeCalculations.GetDiscriminator(TypeCode.Object))
    End Sub
End Class
