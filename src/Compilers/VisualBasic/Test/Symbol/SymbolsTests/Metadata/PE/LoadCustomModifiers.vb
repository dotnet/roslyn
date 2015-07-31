' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities


Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadCustomModifiers : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()

            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                             {
                                TestResources.SymbolsTests.CustomModifiers.Modifiers,
                                TestResources.NetFX.v4_0_21006.mscorlib
                             })

            Dim modifiersModule = assemblies(0).Modules(0)


            Dim modifiers = modifiersModule.GlobalNamespace.GetTypeMembers("Modifiers").Single()

            Dim f0 = modifiers.GetMembers("F0").OfType(Of FieldSymbol)().Single()

            Assert.Equal(1, f0.CustomModifiers.Length)

            Dim f0Mod = f0.CustomModifiers(0)

            Assert.True(f0Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", f0Mod.Modifier.ToTestDisplayString())

            Dim m1 As MethodSymbol = modifiers.GetMembers("F1").OfType(Of MethodSymbol)().Single()
            Dim p1 As ParameterSymbol = m1.Parameters(0)
            Dim p2 As ParameterSymbol = modifiers.GetMembers("F2").OfType(Of MethodSymbol)().Single().Parameters(0)

            Dim p4 As ParameterSymbol = modifiers.GetMembers("F4").OfType(Of MethodSymbol)().Single().Parameters(0)

            Dim m5 As MethodSymbol = modifiers.GetMembers("F5").OfType(Of MethodSymbol)().Single()
            Dim p5 As ParameterSymbol = m5.Parameters(0)

            Dim p6 As ParameterSymbol = modifiers.GetMembers("F6").OfType(Of MethodSymbol)().Single().Parameters(0)

            Dim m7 As MethodSymbol = modifiers.GetMembers("F7").OfType(Of MethodSymbol)().Single()

            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length)

            Assert.Equal(1, p1.CustomModifiers.Length)

            Dim p1Mod = p1.CustomModifiers(0)

            Assert.True(p1Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString())

            Assert.Equal(2, p2.CustomModifiers.Length)

            For Each p2Mod In p2.CustomModifiers
                Assert.True(p2Mod.IsOptional)
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString())
            Next

            Assert.Equal(SymbolKind.ErrorType, p4.Type.Kind)

            Assert.True(m5.IsSub)
            Assert.Equal(1, m5.ReturnTypeCustomModifiers.Length)

            Dim m5Mod = m5.ReturnTypeCustomModifiers(0)
            Assert.True(m5Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString())

            Assert.Equal(0, p5.CustomModifiers.Length)

            Dim p5Type As ArrayTypeSymbol = DirectCast(p5.Type, ArrayTypeSymbol)

            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString())

            Assert.Equal(1, p5Type.CustomModifiers.Length)
            Dim p5TypeMod = p5Type.CustomModifiers(0)

            Assert.True(p5TypeMod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString())

            Assert.Equal(0, p6.CustomModifiers.Length)

            Dim p6Type As TypeSymbol = p6.Type

            Assert.IsType(Of PointerTypeSymbol)(p6Type)
            Assert.Equal(ERRID.ERR_UnsupportedType1, p6Type.GetUseSiteErrorInfo().Code)

            Assert.False(m7.IsSub)
            Assert.Equal(1, m7.ReturnTypeCustomModifiers.Length)

            Dim m7Mod = m7.ReturnTypeCustomModifiers(0)
            Assert.True(m7Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString())
        End Sub

    End Class

End Namespace
