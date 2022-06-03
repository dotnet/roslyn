' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols

    Public Class MethodImplementationFlagsTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestInliningFlags()
            Dim src =
<compilation>
    <file name="C.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Public Class C
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Sub M_Aggressive()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Sub M_NoInlining()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                           Dim aggressiveInliningMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_Aggressive")
                                                           Assert.Equal(MethodImplAttributes.AggressiveInlining, aggressiveInliningMethod.MethodImplementationFlags)
                                                           Dim noInliningMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_NoInlining")
                                                           Assert.Equal(MethodImplAttributes.NoInlining, noInliningMethod.MethodImplementationFlags)
                                                       End Sub

            CompileAndVerify(src, sourceSymbolValidator:=validator, symbolValidator:=validator, useLatestFramework:=True)
        End Sub

        <Fact>
        Public Sub TestOptimizationFlags()
            Dim src =
<compilation>
    <file name="C.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Public Class C
    <MethodImpl(CType(512, MethodImplOptions))> ' Aggressive optimization
    Public Sub M_Aggressive()
    End Sub

    <MethodImpl(MethodImplOptions.NoOptimization)>
    Public Sub M_NoOptimization()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                           Dim aggressiveOptimizationMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_Aggressive")
                                                           Assert.Equal(CType(512, MethodImplAttributes), aggressiveOptimizationMethod.MethodImplementationFlags)
                                                           Dim noOptimizationMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_NoOptimization")
                                                           Assert.Equal(MethodImplAttributes.NoOptimization, noOptimizationMethod.MethodImplementationFlags)
                                                       End Sub

            CompileAndVerify(src, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        Public Sub TestMixingOptimizationWithInliningFlags()
            Dim src =
<compilation>
    <file name="C.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Public Class C
    <MethodImpl(CType(512, MethodImplOptions) Or MethodImplOptions.NoInlining)> ' aggressive optimization and no inlining
    Public Sub M_AggressiveOpt_NoInlining()
    End Sub

    <MethodImpl(MethodImplOptions.NoOptimization Or MethodImplOptions.NoInlining)>
    Public Sub M_NoOpt_NoInlining()
    End Sub

    <MethodImpl(CType(512, MethodImplOptions) Or MethodImplOptions.AggressiveInlining)> ' aggressive optimization and aggressive inlining
    Public Sub M_AggressiveOpt_AggressiveInlining()
    End Sub

    <MethodImpl(MethodImplOptions.NoOptimization Or MethodImplOptions.AggressiveInlining)>
    Public Sub M_NoOpt_AggressiveInlining()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                           Dim aggressiveOptNoInliningMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_AggressiveOpt_NoInlining")
                                                           Assert.Equal(CType(512, MethodImplAttributes) Or MethodImplAttributes.NoInlining, aggressiveOptNoInliningMethod.MethodImplementationFlags)
                                                           Dim noOptNoInliningMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_NoOpt_NoInlining")
                                                           Assert.Equal(MethodImplAttributes.NoOptimization Or MethodImplAttributes.NoInlining, noOptNoInliningMethod.MethodImplementationFlags)
                                                           Dim aggressiveOptAggressiveInliningMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_AggressiveOpt_AggressiveInlining")
                                                           Assert.Equal(CType(512, MethodImplAttributes) Or MethodImplAttributes.AggressiveInlining, aggressiveOptAggressiveInliningMethod.MethodImplementationFlags)
                                                           Dim noOptAggressiveInliningMethod As IMethodSymbol = c.GetMember(Of MethodSymbol)("M_NoOpt_AggressiveInlining")
                                                           Assert.Equal(MethodImplAttributes.NoOptimization Or MethodImplAttributes.AggressiveInlining, noOptAggressiveInliningMethod.MethodImplementationFlags)
                                                       End Sub

            CompileAndVerify(src, sourceSymbolValidator:=validator, symbolValidator:=validator, useLatestFramework:=True)
        End Sub

        <Fact>
        Public Sub TestPreserveSigAndRuntimeFlags()
            Dim src =
<compilation>
    <file name="C.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Public Class C
    <MethodImpl(MethodImplOptions.PreserveSig, MethodCodeType:=MethodCodeType.Runtime)>
    Public Sub M()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                           Dim method As IMethodSymbol = c.GetMember(Of MethodSymbol)("M")
                                                           Assert.Equal(MethodImplAttributes.PreserveSig Or MethodImplAttributes.Runtime, method.MethodImplementationFlags)
                                                       End Sub

            CompileAndVerify(src, sourceSymbolValidator:=validator, symbolValidator:=validator, verify:=Verification.Skipped)
        End Sub

        <Fact>
        Public Sub TestNativeFlag()
            Dim src =
<compilation>
    <file name="C.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Public Class C
    <MethodImpl(MethodCodeType:=MethodCodeType.Native)>
    Public Sub M()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim validator As Action(Of ModuleSymbol) = Sub([module])
                                                           Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                           Dim method As IMethodSymbol = c.GetMember(Of MethodSymbol)("M")
                                                           Assert.Equal(MethodImplAttributes.Native, method.MethodImplementationFlags)
                                                       End Sub

            CompileAndVerify(src, sourceSymbolValidator:=validator, symbolValidator:=validator, verify:=Verification.Skipped)
        End Sub
    End Class
End Namespace
