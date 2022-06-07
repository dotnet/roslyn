' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class NativeIntegerTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TypeDefinitions_FromMetadata()
            Dim source0 =
"public interface I
{
    public void F1(System.IntPtr x, nint y);
    public void F2(System.UIntPtr x, nuint y);
}"
            Dim comp As Compilation = CreateCSharpCompilation(source0, parseOptions:=New CSharp.CSharpParseOptions(CSharp.LanguageVersion.CSharp9))
            Dim ref0 = comp.EmitToImageReference()

            Dim type = comp.GlobalNamespace.GetTypeMembers("I").Single()
            Dim method = DirectCast(type.GetMembers("F1").Single(), IMethodSymbol)
            Assert.Equal("Sub I.F1(x As System.IntPtr, y As System.IntPtr)", SymbolDisplay.ToDisplayString(method, SymbolDisplayFormat.TestFormat))
            VerifyType(DirectCast(method.Parameters(0).Type, INamedTypeSymbol), signed:=True, isNativeInteger:=False)
            VerifyType(DirectCast(method.Parameters(1).Type, INamedTypeSymbol), signed:=True, isNativeInteger:=True)

            method = DirectCast(type.GetMembers("F2").Single(), IMethodSymbol)
            Assert.Equal("Sub I.F2(x As System.UIntPtr, y As System.UIntPtr)", SymbolDisplay.ToDisplayString(method, SymbolDisplayFormat.TestFormat))
            VerifyType(DirectCast(method.Parameters(0).Type, INamedTypeSymbol), signed:=False, isNativeInteger:=False)
            VerifyType(DirectCast(method.Parameters(1).Type, INamedTypeSymbol), signed:=False, isNativeInteger:=True)

            comp = CreateCompilation("", references:={ref0})
            comp.VerifyDiagnostics()

            type = comp.GlobalNamespace.GetTypeMembers("I").Single()
            method = DirectCast(type.GetMembers("F1").Single(), IMethodSymbol)
            Assert.Equal("Sub I.F1(x As System.IntPtr, y As System.IntPtr)", SymbolDisplay.ToDisplayString(method, SymbolDisplayFormat.TestFormat))
            VerifyType(DirectCast(method.Parameters(0).Type, INamedTypeSymbol), signed:=True, isNativeInteger:=False)
            VerifyType(DirectCast(method.Parameters(1).Type, INamedTypeSymbol), signed:=True, isNativeInteger:=False)

            method = DirectCast(type.GetMembers("F2").Single(), IMethodSymbol)
            Assert.Equal("Sub I.F2(x As System.UIntPtr, y As System.UIntPtr)", SymbolDisplay.ToDisplayString(method, SymbolDisplayFormat.TestFormat))
            VerifyType(DirectCast(method.Parameters(0).Type, INamedTypeSymbol), signed:=False, isNativeInteger:=False)
            VerifyType(DirectCast(method.Parameters(1).Type, INamedTypeSymbol), signed:=False, isNativeInteger:=False)
        End Sub

        Private Shared Sub VerifyType(type As INamedTypeSymbol, signed As Boolean, isNativeInteger As Boolean)
            Assert.Equal(If(signed, SpecialType.System_IntPtr, SpecialType.System_UIntPtr), type.SpecialType)
            Assert.Equal(SymbolKind.NamedType, type.Kind)
            Assert.Equal(TypeKind.Struct, type.TypeKind)
            Assert.Same(type, type.ConstructedFrom)
            Assert.Equal(isNativeInteger, type.IsNativeIntegerType)

            Dim underlyingType = type.NativeIntegerUnderlyingType
            If isNativeInteger Then
                Assert.NotNull(underlyingType)
                VerifyType(underlyingType, signed, isNativeInteger:=False)
            Else
                Assert.Null(underlyingType)
            End If
        End Sub

        <Fact>
        Public Sub CreateNativeIntegerTypeSymbol()
            Dim comp = VisualBasicCompilation.Create(assemblyName:=GetUniqueName(), references:=TargetFrameworkUtil.GetReferences(TargetFramework.DefaultVb), options:=TestOptions.ReleaseDll)
            comp.AssertNoDiagnostics()
            Assert.Throws(Of NotSupportedException)(Function() comp.CreateNativeIntegerTypeSymbol(signed:=True))
            Assert.Throws(Of NotSupportedException)(Function() comp.CreateNativeIntegerTypeSymbol(signed:=False))
        End Sub

    End Class

End Namespace
