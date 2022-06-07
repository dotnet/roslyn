' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Public Class ConstantExpressionEvaluatorTests
    Inherits BasicTestBase

    <Fact>
    Public Sub ConstantValueGetDiscriminatorTest01()
        Assert.Equal(ConstantValueTypeDiscriminator.SByte, SpecialType.System_SByte.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Byte, SpecialType.System_Byte.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Int16, SpecialType.System_Int16.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.UInt16, SpecialType.System_UInt16.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Int32, SpecialType.System_Int32.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.UInt32, SpecialType.System_UInt32.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Int64, SpecialType.System_Int64.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.UInt64, SpecialType.System_UInt64.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Char, SpecialType.System_Char.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Boolean, SpecialType.System_Boolean.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Single, SpecialType.System_Single.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Double, SpecialType.System_Double.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.Decimal, SpecialType.System_Decimal.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.DateTime, SpecialType.System_DateTime.ToConstantValueDiscriminator())
        Assert.Equal(ConstantValueTypeDiscriminator.String, SpecialType.System_String.ToConstantValueDiscriminator())
    End Sub
End Class
