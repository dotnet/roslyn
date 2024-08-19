' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.[Text]
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Roslyn.Test.Utilities.TestMetadata
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Retargeting
#If Not Retargeting Then
    Public Class RetargetCustomModifiers
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim oldMsCorLib = Net40.References.mscorlib
            Dim c1 = VisualBasicCompilation.Create("C1", references:={oldMsCorLib, TestReferences.SymbolsTests.CustomModifiers.Modifiers.netmodule})

            Dim c1Assembly = c1.Assembly
            Dim newMsCorLib = NetFramework.mscorlib
            Dim c2 = VisualBasicCompilation.Create("C2", references:=New MetadataReference() {newMsCorLib, New VisualBasicCompilationReference(c1)})
            Dim mscorlibAssembly = c2.GetReferencedAssemblySymbol(newMsCorLib)
            Assert.NotSame(mscorlibAssembly, c1.GetReferencedAssemblySymbol(oldMsCorLib))
            Dim modifiers = c2.GlobalNamespace.GetTypeMembers("Modifiers").Single()
            Assert.IsType(Of PENamedTypeSymbol)(modifiers)
            Dim f0 As FieldSymbol = modifiers.GetMembers("F0").OfType(Of FieldSymbol)().Single()
            Assert.Equal(1, f0.CustomModifiers.Length)
            Dim f0Mod = f0.CustomModifiers(0)
            Assert.[True](f0Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", f0Mod.Modifier.ToTestDisplayString())
            Assert.Same(mscorlibAssembly, f0Mod.Modifier.ContainingAssembly)
            Dim m1 As MethodSymbol = modifiers.GetMembers("F1").OfType(Of MethodSymbol)().Single()
            Dim p1 As ParameterSymbol = m1.Parameters(0)
            Dim p2 As ParameterSymbol = modifiers.GetMembers("F2").OfType(Of MethodSymbol)().Single().Parameters(0)
            Dim m5 As MethodSymbol = modifiers.GetMembers("F5").OfType(Of MethodSymbol)().Single()
            Dim p5 As ParameterSymbol = m5.Parameters(0)
            Dim p6 As ParameterSymbol = modifiers.GetMembers("F6").OfType(Of MethodSymbol)().Single().Parameters(0)
            Dim m7 As MethodSymbol = modifiers.GetMembers("F7").OfType(Of MethodSymbol)().Single()
            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length)
            Assert.Equal(1, p1.CustomModifiers.Length)
            Dim p1Mod = p1.CustomModifiers(0)
            Assert.[True](p1Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString())
            Assert.Same(mscorlibAssembly, p1Mod.Modifier.ContainingAssembly)
            Assert.Equal(2, p2.CustomModifiers.Length)
            For Each p2Mod In p2.CustomModifiers
                Assert.[True](p2Mod.IsOptional)
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString())
                Assert.Same(mscorlibAssembly, p2Mod.Modifier.ContainingAssembly)
            Next
            Assert.[True](m5.IsSub)
            Assert.Equal(1, m5.ReturnTypeCustomModifiers.Length)
            Dim m5Mod = m5.ReturnTypeCustomModifiers(0)
            Assert.[True](m5Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString())
            Assert.Same(mscorlibAssembly, m5Mod.Modifier.ContainingAssembly)
            Assert.Equal(0, p5.CustomModifiers.Length)
            Dim p5Type As ArrayTypeSymbol = DirectCast(p5.[Type], ArrayTypeSymbol)
            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString())
            Assert.Equal(1, p5Type.CustomModifiers.Length)
            Dim p5TypeMod = p5Type.CustomModifiers(0)
            Assert.[True](p5TypeMod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString())
            Assert.Same(mscorlibAssembly, p5TypeMod.Modifier.ContainingAssembly)
            Assert.Equal(0, p6.CustomModifiers.Length)

            Assert.True(p6.[Type].IsErrorType())
            Assert.IsType(Of PointerTypeSymbol)(p6.Type)
            Assert.False(DirectCast(p6.Type, INamedTypeSymbol).IsSerializable)

            Assert.[False](m7.IsSub)
            Assert.Equal(1, m7.ReturnTypeCustomModifiers.Length)
            Dim m7Mod = m7.ReturnTypeCustomModifiers(0)
            Assert.[True](m7Mod.IsOptional)
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString())
            Assert.Same(mscorlibAssembly, m7Mod.Modifier.ContainingAssembly)
        End Sub

        <Fact>
        Public Sub Test2()
            Dim oldMsCorLib = Net40.References.mscorlib
            Dim source = "
public class Modifiers

    public volatileFld As Integer

    Overloads Sub F1(p As System.DateTime)
    End Sub
End Class"

            Dim c1 = VisualBasicCompilation.Create("C1", {Parse(source)}, {oldMsCorLib})

            Dim c1Assembly = c1.Assembly
            Dim newMsCorLib = NetFramework.mscorlib

            Dim r1 = New VisualBasicCompilationReference(c1)
            Dim c2 As VisualBasicCompilation = VisualBasicCompilation.Create("C2", references:={newMsCorLib, r1})
            Dim c1AsmRef = c2.GetReferencedAssemblySymbol(r1)
            Assert.NotSame(c1Assembly, c1AsmRef)

            Dim mscorlibAssembly = c2.GetReferencedAssemblySymbol(newMsCorLib)
            Assert.NotSame(mscorlibAssembly, c1.GetReferencedAssemblySymbol(oldMsCorLib))
            Dim modifiers = c2.GlobalNamespace.GetTypeMembers("Modifiers").Single()
            Assert.IsType(Of RetargetingNamedTypeSymbol)(modifiers)
            Dim volatileFld As FieldSymbol = modifiers.GetMembers("volatileFld").OfType(Of FieldSymbol)().Single()
            Assert.Equal(0, volatileFld.CustomModifiers.Length)

            Assert.Equal(SpecialType.System_Int32, volatileFld.[Type].SpecialType)
            Assert.Equal("volatileFld", volatileFld.Name)
            Assert.Same(volatileFld, volatileFld.OriginalDefinition)
            Assert.Null(volatileFld.GetConstantValue(ConstantFieldsInProgress.Empty))
            Assert.Null(volatileFld.ConstantValue)
            Assert.Null(volatileFld.AssociatedSymbol)
            Assert.Same(c1AsmRef, volatileFld.ContainingAssembly)
            Assert.Same(c1AsmRef.Modules(0), volatileFld.ContainingModule)
            Assert.Same(modifiers, volatileFld.ContainingSymbol)
            Assert.Equal(Accessibility.[Public], volatileFld.DeclaredAccessibility)
            Assert.[False](volatileFld.IsConst)
            Assert.[False](volatileFld.IsReadOnly)
            Assert.[False](volatileFld.IsShared)
            Assert.Same(volatileFld.ContainingModule, (DirectCast(volatileFld, RetargetingFieldSymbol)).RetargetingModule)
            Assert.Same(c1Assembly, (DirectCast(volatileFld, RetargetingFieldSymbol)).UnderlyingField.ContainingAssembly)
            Dim m1 As MethodSymbol = modifiers.GetMembers("F1").OfType(Of MethodSymbol)().Single()
            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length)
            Assert.Equal(0, m1.ExplicitInterfaceImplementations.Length)
            Assert.True(m1.IsOverloads)
            Assert.False(m1.IsExtensionMethod)
            Assert.Equal((DirectCast(m1, RetargetingMethodSymbol)).UnderlyingMethod.CallingConvention, m1.CallingConvention)
            Assert.Null(m1.AssociatedSymbol)
            Assert.Same(c1AsmRef.Modules(0), m1.ContainingModule)
            Dim p1 As ParameterSymbol = m1.Parameters(0)
            Assert.Equal(0, p1.CustomModifiers.Length)
            Assert.Same(c1AsmRef.Modules(0), p1.ContainingModule)
            Assert.False(p1.HasExplicitDefaultValue)
            Assert.Equal(0, p1.Ordinal)
        End Sub
    End Class
#End If
End Namespace

