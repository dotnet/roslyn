' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class MethodTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Methods1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Partial Class C
    Sub m1()
    End Sub
    Protected MustOverride Function m2$()
    Friend NotOverridable Overloads Function m3() As D
    End Function
    Protected Friend Overridable Shadows Sub m4()
    End Sub
    Protected Overrides Sub m5()
    End Sub
End Class
    </file>
    <file name="b.vb">
Option Strict Off        
Public Partial Class C
    Friend Shared Function m6()
    End Function
    Private Class D
    End Class
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Assert.Equal(8, membersOfC.Length)

            Dim classD = DirectCast(membersOfC(1), NamedTypeSymbol)
            Assert.Equal("D", classD.Name)
            Assert.Equal(TypeKind.Class, classD.TypeKind)

            Dim ctor = DirectCast(membersOfC(0), MethodSymbol)
            Assert.Same(classC, ctor.ContainingSymbol)
            Assert.Same(classC, ctor.ContainingType)
            Assert.Equal(".ctor", ctor.Name)
            Assert.Equal(MethodKind.Constructor, ctor.MethodKind)
            Assert.Equal(Accessibility.Public, ctor.DeclaredAccessibility)
            Assert.Equal(0, ctor.TypeParameters.Length)
            Assert.Equal(0, ctor.TypeArguments.Length)
            Assert.False(ctor.IsGenericMethod)
            Assert.False(ctor.IsMustOverride)
            Assert.False(ctor.IsNotOverridable)
            Assert.False(ctor.IsOverridable)
            Assert.False(ctor.IsOverrides)
            Assert.False(ctor.IsShared)
            Assert.False(ctor.IsOverloads)
            Assert.True(ctor.IsSub)
            Assert.Equal("System.Void", ctor.ReturnType.ToTestDisplayString())
            Assert.Equal(0, ctor.Parameters.Length)

            Dim m1 = DirectCast(membersOfC(2), MethodSymbol)
            Assert.Same(classC, m1.ContainingSymbol)
            Assert.Same(classC, m1.ContainingType)
            Assert.Equal("m1", m1.Name)
            Assert.Equal(MethodKind.Ordinary, m1.MethodKind)
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility)
            Assert.Equal(0, m1.TypeParameters.Length)
            Assert.Equal(0, m1.TypeArguments.Length)
            Assert.False(m1.IsGenericMethod)
            Assert.False(m1.IsMustOverride)
            Assert.False(m1.IsNotOverridable)
            Assert.False(m1.IsOverridable)
            Assert.False(m1.IsOverrides)
            Assert.False(m1.IsShared)
            Assert.False(m1.IsOverloads)
            Assert.True(m1.IsSub)
            Assert.Equal("System.Void", m1.ReturnType.ToTestDisplayString())
            Assert.False(m1.IsRuntimeImplemented()) ' test for default implementation

            Dim m2 = DirectCast(membersOfC(3), MethodSymbol)
            Assert.Equal(Accessibility.Protected, m2.DeclaredAccessibility)
            Assert.True(m2.IsMustOverride)
            Assert.False(m2.IsNotOverridable)
            Assert.False(m2.IsOverridable)
            Assert.False(m2.IsOverrides)
            Assert.False(m2.IsShared)
            Assert.False(m2.IsSub)
            Assert.False(m2.IsOverloads)
            Assert.Equal("System.String", m2.ReturnType.ToTestDisplayString())

            Dim m3 = DirectCast(membersOfC(4), MethodSymbol)
            Assert.Equal(Accessibility.Friend, m3.DeclaredAccessibility)
            Assert.False(m3.IsMustOverride)
            Assert.True(m3.IsNotOverridable)
            Assert.False(m3.IsOverridable)
            Assert.False(m3.IsOverrides)
            Assert.False(m3.IsShared)
            Assert.True(m3.IsOverloads)
            Assert.False(m3.IsSub)
            Assert.Equal("C.D", m3.ReturnType.ToTestDisplayString())

            Dim m4 = DirectCast(membersOfC(5), MethodSymbol)
            Assert.Equal(Accessibility.ProtectedOrFriend, m4.DeclaredAccessibility)
            Assert.False(m4.IsMustOverride)
            Assert.False(m4.IsNotOverridable)
            Assert.True(m4.IsOverridable)
            Assert.False(m4.IsOverrides)
            Assert.False(m4.IsShared)
            Assert.False(m4.IsOverloads)
            Assert.True(m4.IsSub)

            Dim m5 = DirectCast(membersOfC(6), MethodSymbol)
            Assert.Equal(Accessibility.Protected, m5.DeclaredAccessibility)
            Assert.False(m5.IsMustOverride)
            Assert.False(m5.IsNotOverridable)
            Assert.False(m5.IsOverridable)
            Assert.True(m5.IsOverrides)
            Assert.False(m5.IsShared)
            Assert.True(m5.IsOverloads)
            Assert.True(m5.IsSub)

            Dim m6 = DirectCast(membersOfC(7), MethodSymbol)
            Assert.Equal(Accessibility.Friend, m6.DeclaredAccessibility)
            Assert.False(m6.IsMustOverride)
            Assert.False(m6.IsNotOverridable)
            Assert.False(m6.IsOverridable)
            Assert.False(m6.IsOverrides)
            Assert.True(m6.IsShared)
            Assert.False(m6.IsSub)
            Assert.Equal("System.Object", m6.ReturnType.ToTestDisplayString())

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation,
                                                          <expected>
BC31411: 'C' must be declared 'MustInherit' because it contains methods declared 'MustOverride'.
Public Partial Class C
                     ~
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
    Friend NotOverridable Overloads Function m3() As D
                                             ~~
BC30508: 'm3' cannot expose type 'C.D' in namespace '&lt;Default&gt;' through class 'C'.
    Friend NotOverridable Overloads Function m3() As D
                                                     ~
BC30284: sub 'm5' cannot be declared 'Overrides' because it does not override a sub in a base class.
    Protected Overrides Sub m5()
                            ~~
                                                          </expected>)
        End Sub

        <Fact>
        Public Sub Methods2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Module M
    Sub m1()
    End Sub
End Module
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Dim moduleM = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfM = moduleM.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()

            Dim m1 = DirectCast(membersOfM(0), MethodSymbol)
            Assert.Same(moduleM, m1.ContainingSymbol)
            Assert.Same(moduleM, m1.ContainingType)
            Assert.Equal("m1", m1.Name)
            Assert.Equal(MethodKind.Ordinary, m1.MethodKind)
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility)
            Assert.Equal(0, m1.TypeParameters.Length)
            Assert.Equal(0, m1.TypeArguments.Length)
            Assert.False(m1.IsGenericMethod)
            Assert.False(m1.IsMustOverride)
            Assert.False(m1.IsNotOverridable)
            Assert.False(m1.IsOverridable)
            Assert.False(m1.IsOverrides)
            Assert.True(m1.IsShared)   ' methods in a module are implicitly Shared
            Assert.False(m1.IsOverloads)
            Assert.True(m1.IsSub)
            Assert.Equal("System.Void", m1.ReturnType.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub Constructors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Partial Class C
    Public Sub New()
    End Sub
End Class
    </file>
    <file name="b.vb">
Option Strict Off        
Public Partial Class C
    Friend Sub New(x as string, y as integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, MethodSymbol).Parameters.Length).ToArray()
            Assert.Equal(2, membersOfC.Length)

            Dim m1 = DirectCast(membersOfC(0), MethodSymbol)
            Assert.Same(classC, m1.ContainingSymbol)
            Assert.Same(classC, m1.ContainingType)
            Assert.Equal(".ctor", m1.Name)
            Assert.Equal(MethodKind.Constructor, m1.MethodKind)
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility)
            Assert.Equal(0, m1.TypeParameters.Length)
            Assert.Equal(0, m1.TypeArguments.Length)
            Assert.False(m1.IsGenericMethod)
            Assert.False(m1.IsMustOverride)
            Assert.False(m1.IsNotOverridable)
            Assert.False(m1.IsOverridable)
            Assert.False(m1.IsOverrides)
            Assert.False(m1.IsShared)
            Assert.False(m1.IsOverloads)
            Assert.True(m1.IsSub)
            Assert.Equal("System.Void", m1.ReturnType.ToTestDisplayString())
            Assert.Equal(0, m1.Parameters.Length)

            Dim m2 = DirectCast(membersOfC(1), MethodSymbol)
            Assert.Same(classC, m2.ContainingSymbol)
            Assert.Same(classC, m2.ContainingType)
            Assert.Equal(".ctor", m2.Name)
            Assert.Equal(MethodKind.Constructor, m2.MethodKind)
            Assert.Equal(Accessibility.Friend, m2.DeclaredAccessibility)
            Assert.Equal(0, m2.TypeParameters.Length)
            Assert.Equal(0, m2.TypeArguments.Length)
            Assert.False(m2.IsGenericMethod)
            Assert.False(m2.IsMustOverride)
            Assert.False(m2.IsNotOverridable)
            Assert.False(m2.IsOverridable)
            Assert.False(m2.IsOverrides)
            Assert.False(m2.IsShared)
            Assert.False(m2.IsOverloads)
            Assert.True(m2.IsSub)
            Assert.Equal("System.Void", m2.ReturnType.ToTestDisplayString())
            Assert.Equal(2, m2.Parameters.Length)
            Assert.Equal("x", m2.Parameters(0).Name)
            Assert.Equal("System.String", m2.Parameters(0).Type.ToTestDisplayString())
            Assert.Equal("y", m2.Parameters(1).Name)
            Assert.Equal("System.Int32", m2.Parameters(1).Type.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub SharedConstructors1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Partial Class C
    Shared Sub New()
    End Sub
End Class
    </file>
    <file name="b.vb">
Option Strict Off        
Module M
    Sub New()
    End Sub
End Module
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Dim moduleM = DirectCast(globalNSmembers(1), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, MethodSymbol).Parameters.Length).ToArray()

            Dim m1 = DirectCast(membersOfC(0), MethodSymbol)
            Assert.Same(classC, m1.ContainingSymbol)
            Assert.Same(classC, m1.ContainingType)
            Assert.Equal(".cctor", m1.Name)
            Assert.Equal(MethodKind.SharedConstructor, m1.MethodKind)
            Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility)
            Assert.Equal(0, m1.TypeParameters.Length)
            Assert.Equal(0, m1.TypeArguments.Length)
            Assert.False(m1.IsGenericMethod)
            Assert.False(m1.IsMustOverride)
            Assert.False(m1.IsNotOverridable)
            Assert.False(m1.IsOverridable)
            Assert.False(m1.IsOverrides)
            Assert.True(m1.IsShared)
            Assert.False(m1.IsOverloads)
            Assert.True(m1.IsSub)
            Assert.Equal("System.Void", m1.ReturnType.ToTestDisplayString())
            Assert.Equal(0, m1.Parameters.Length)

            Dim membersOfM = moduleM.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, MethodSymbol).Parameters.Length).ToArray()
            Dim m2 = DirectCast(membersOfM(0), MethodSymbol)
            Assert.Same(moduleM, m2.ContainingSymbol)
            Assert.Same(moduleM, m2.ContainingType)
            Assert.Equal(".cctor", m2.Name)
            Assert.Equal(MethodKind.SharedConstructor, m2.MethodKind)
            Assert.Equal(Accessibility.Private, m2.DeclaredAccessibility)
            Assert.Equal(0, m2.TypeParameters.Length)
            Assert.Equal(0, m2.TypeArguments.Length)
            Assert.False(m2.IsGenericMethod)
            Assert.False(m2.IsMustOverride)
            Assert.False(m2.IsNotOverridable)
            Assert.False(m2.IsOverridable)
            Assert.False(m2.IsOverrides)
            Assert.True(m2.IsShared)
            Assert.False(m2.IsOverloads)
            Assert.True(m2.IsSub)
            Assert.Equal("System.Void", m2.ReturnType.ToTestDisplayString())
            Assert.Equal(0, m2.Parameters.Length)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub DefaultConstructors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Class C
End Class
Public MustInherit Class D
End Class
    </file>
    <file name="b.vb">
Option Strict Off        
Public Structure S
End Structure
Public Module M 
End Module
Public Interface I 
End Interface
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Assert.Equal("C", classC.Name)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, MethodSymbol).Parameters.Length).ToArray()
            Assert.Equal(1, membersOfC.Length)

            Dim m1 = DirectCast(membersOfC(0), MethodSymbol)
            Assert.Same(classC, m1.ContainingSymbol)
            Assert.Same(classC, m1.ContainingType)
            Assert.Equal(".ctor", m1.Name)
            Assert.Equal(MethodKind.Constructor, m1.MethodKind)
            Assert.Equal(Accessibility.Public, m1.DeclaredAccessibility)
            Assert.Equal(0, m1.TypeParameters.Length)
            Assert.Equal(0, m1.TypeArguments.Length)
            Assert.False(m1.IsGenericMethod)
            Assert.False(m1.IsMustOverride)
            Assert.False(m1.IsNotOverridable)
            Assert.False(m1.IsOverridable)
            Assert.False(m1.IsOverrides)
            Assert.False(m1.IsShared)
            Assert.False(m1.IsOverloads)
            Assert.True(m1.IsSub)
            Assert.Equal("System.Void", m1.ReturnType.ToTestDisplayString())
            Assert.Equal(0, m1.Parameters.Length)

            Dim classD = DirectCast(globalNSmembers(1), NamedTypeSymbol)
            Assert.Equal("D", classD.Name)

            Dim membersOfD = classD.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ThenBy(Function(s) DirectCast(s, MethodSymbol).Parameters.Length).ToArray()
            Assert.Equal(1, membersOfD.Length)

            Dim m2 = DirectCast(membersOfD(0), MethodSymbol)
            Assert.Same(classD, m2.ContainingSymbol)
            Assert.Same(classD, m2.ContainingType)
            Assert.Equal(".ctor", m2.Name)
            Assert.Equal(MethodKind.Constructor, m2.MethodKind)
            Assert.Equal(Accessibility.Protected, m2.DeclaredAccessibility)
            Assert.Equal(0, m2.TypeParameters.Length)
            Assert.Equal(0, m2.TypeArguments.Length)
            Assert.False(m2.IsGenericMethod)
            Assert.False(m2.IsMustOverride)
            Assert.False(m2.IsNotOverridable)
            Assert.False(m2.IsOverridable)
            Assert.False(m2.IsOverrides)
            Assert.False(m2.IsShared)
            Assert.False(m2.IsOverloads)
            Assert.True(m2.IsSub)
            Assert.Equal("System.Void", m2.ReturnType.ToTestDisplayString())
            Assert.Equal(0, m2.Parameters.Length)

            Dim interfaceI = DirectCast(globalNSmembers(2), NamedTypeSymbol)
            Assert.Equal("I", interfaceI.Name)
            Assert.Equal(0, interfaceI.GetMembers().Length())

            Dim moduleM = DirectCast(globalNSmembers(3), NamedTypeSymbol)
            Assert.Equal("M", moduleM.Name)
            Assert.Equal(0, moduleM.GetMembers().Length())

            Dim structureS = DirectCast(globalNSmembers(4), NamedTypeSymbol)
            Assert.Equal("S", structureS.Name)
            Assert.Equal(1, structureS.GetMembers().Length()) ' Implicit parameterless constructor

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MethodParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Partial Class C
    Sub m1(x as D, ByRef y As Integer)
    End Sub
End Class
    </file>
    <file name="b.vb">
Option Strict Off        
Public Partial Class C
    Sub m2(a(), z, byref q, ParamArray w())
    End Sub
    Sub m3(x)
    End Sub
    Class D
    End Class
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()

            Dim classD = DirectCast(membersOfC(1), NamedTypeSymbol)
            Assert.Equal("D", classD.Name)
            Assert.Equal(TypeKind.Class, classD.TypeKind)

            Dim m1 = DirectCast(membersOfC(2), MethodSymbol)
            Assert.Same(classC, m1.ContainingSymbol)
            Assert.Equal("m1", m1.Name)
            Assert.Equal(2, m1.Parameters.Length)
            Dim m1p1 = m1.Parameters(0)
            Assert.Equal("x", m1p1.Name)
            Assert.Same(m1, m1p1.ContainingSymbol)
            Assert.False(m1p1.HasExplicitDefaultValue)
            Assert.False(m1p1.IsOptional)
            Assert.False(m1p1.IsParamArray)
            Assert.Same(classD, m1p1.Type)

            Dim m1p2 = m1.Parameters(1)
            Assert.Equal("y", m1p2.Name)
            Assert.Same(m1, m1p2.ContainingSymbol)
            Assert.False(m1p2.HasExplicitDefaultValue)
            Assert.False(m1p2.IsOptional)
            Assert.False(m1p2.IsParamArray)
            Assert.True(m1p2.IsByRef)
            Assert.Equal("System.Int32", m1p2.Type.ToTestDisplayString())
            Assert.Equal("ByRef y As System.Int32", m1p2.ToTestDisplayString())

            Dim m2 = DirectCast(membersOfC(3), MethodSymbol)
            Assert.Equal(4, m2.Parameters.Length)
            Dim m2p1 = m2.Parameters(0)
            Assert.Equal("a", m2p1.Name)
            Assert.Same(m2, m2p1.ContainingSymbol)
            Assert.False(m2p1.HasExplicitDefaultValue)
            Assert.False(m2p1.IsOptional)
            Assert.False(m2p1.IsParamArray)
            Assert.False(m2p1.IsByRef)
            Assert.Equal("System.Object()", m2p1.Type.ToTestDisplayString())

            Dim m2p2 = m2.Parameters(1)
            Assert.Equal("z", m2p2.Name)
            Assert.Same(m2, m2p2.ContainingSymbol)
            Assert.False(m2p2.HasExplicitDefaultValue)
            Assert.False(m2p2.IsOptional)
            Assert.False(m2p2.IsParamArray)
            Assert.Equal("System.Object", m2p2.Type.ToTestDisplayString())

            Dim m2p3 = m2.Parameters(2)
            Assert.Equal("q", m2p3.Name)
            Assert.Same(m2, m2p3.ContainingSymbol)
            Assert.False(m2p3.HasExplicitDefaultValue)
            Assert.False(m2p3.IsOptional)
            Assert.False(m2p3.IsParamArray)
            Assert.True(m2p3.IsByRef)
            Assert.Equal("System.Object", m2p3.Type.ToTestDisplayString())
            Assert.Equal("ByRef q As System.Object", m2p3.ToTestDisplayString())

            Dim m2p4 = m2.Parameters(3)
            Assert.Equal("w", m2p4.Name)
            Assert.Same(m2, m2p4.ContainingSymbol)
            Assert.False(m2p4.HasExplicitDefaultValue)
            Assert.False(m2p4.IsOptional)
            Assert.True(m2p4.IsParamArray)
            Assert.Equal(TypeKind.Array, m2p4.Type.TypeKind)
            Assert.Equal("System.Object", DirectCast(m2p4.Type, ArrayTypeSymbol).ElementType.ToTestDisplayString())
            Assert.Equal("System.Object()", m2p4.Type.ToTestDisplayString())

            Dim m3 = DirectCast(membersOfC(4), MethodSymbol)
            Assert.Equal(1, m3.Parameters.Length)
            Dim m3p1 = m3.Parameters(0)
            Assert.Equal("x", m3p1.Name)
            Assert.Same(m3, m3p1.ContainingSymbol)
            Assert.False(m3p1.HasExplicitDefaultValue)
            Assert.False(m3p1.IsOptional)
            Assert.False(m3p1.IsParamArray)
            Assert.Equal("System.Object", m3p1.Type.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MethodByRefParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Public Structure S
    Function M(Byref x as Object, ByRef y As Byte) As Long
       Return 0
    End Function
End Structure
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim typeS = DirectCast(globalNS.GetTypeMembers("S").Single(), NamedTypeSymbol)

            Dim m1 = DirectCast(typeS.GetMembers("M").Single(), MethodSymbol)
            Assert.Equal(2, m1.Parameters.Length)
            Dim m1p1 = m1.Parameters(0)
            Assert.Equal("x", m1p1.Name)
            Assert.Same(m1, m1p1.ContainingSymbol)
            Assert.False(m1p1.HasExplicitDefaultValue)
            Assert.False(m1p1.IsOptional)
            Assert.False(m1p1.IsParamArray)
            Assert.True(m1p1.IsByRef)

            Dim m1p2 = m1.Parameters(1)
            Assert.Equal("y", m1p2.Name)
            Assert.Same(m1, m1p2.ContainingSymbol)
            Assert.False(m1p2.HasExplicitDefaultValue)
            Assert.False(m1p2.IsOptional)
            Assert.False(m1p2.IsParamArray)
            Assert.True(m1p2.IsByRef)

            Assert.Equal("System.Byte", m1p2.Type.ToTestDisplayString())
            Assert.Equal("ByRef y As System.Byte", m1p2.ToTestDisplayString())
            Assert.Equal("ValueType", m1p2.Type.BaseType.Name)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub MethodTypeParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Imports System.Collections.Generic
Public Partial Class C
    Function m1(Of T, U)(x1 As T) As IEnumerable(Of U)
    End Function
End Class
    </file>
    <file name="b.vb">
Option Strict Off        
Public Partial Class C
    Private Class D
    End Class
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)

            Dim membersOfC = classC.GetMembers().AsEnumerable().OrderBy(Function(s) s.Name).ToArray()
            Assert.Equal(3, membersOfC.Length)

            Dim classD = DirectCast(membersOfC(1), NamedTypeSymbol)
            Assert.Equal("D", classD.Name)
            Assert.Equal(TypeKind.Class, classD.TypeKind)

            Dim m1 = DirectCast(membersOfC(2), MethodSymbol)
            Assert.Same(classC, m1.ContainingSymbol)
            Assert.Same(classC, m1.ContainingType)
            Assert.Equal("m1", m1.Name)
            Assert.True(m1.IsGenericMethod)
            Assert.Equal(2, m1.TypeParameters.Length)

            Dim tpT = m1.TypeParameters(0)
            Assert.Equal("T", tpT.Name)
            Assert.Same(m1, tpT.ContainingSymbol)
            Assert.Equal(VarianceKind.None, tpT.Variance)

            Dim tpU = m1.TypeParameters(1)
            Assert.Equal("U", tpU.Name)
            Assert.Same(m1, tpU.ContainingSymbol)
            Assert.Equal(VarianceKind.None, tpU.Variance)

            Dim paramX1 = m1.Parameters(0)
            Assert.Same(tpT, paramX1.Type)
            Assert.Equal("T", paramX1.Type.ToTestDisplayString())

            Assert.Same(tpU, DirectCast(m1.ReturnType, NamedTypeSymbol).TypeArguments(0))
            Assert.Equal("System.Collections.Generic.IEnumerable(Of U)", m1.ReturnType.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub ConstructGenericMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Imports System.Collections.Generic
Public Class C(Of W)
    Function m1(Of T, U)(p1 As T, p2 as W) As KeyValuePair(Of W, U))
    End Function

    Sub m2()
    End Sub

    Public Class D(Of X)
        Sub m3(Of T)(p1 As T)
        End Sub
    End Class
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim globalNSmembers = globalNS.GetMembers()
            Dim classC = DirectCast(globalNSmembers(0), NamedTypeSymbol)
            Dim constructedC = classC.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_Int32)}).AsImmutableOrNull())
            Dim m1 = DirectCast(constructedC.GetMembers("m1")(0), MethodSymbol)
            Dim m2 = DirectCast(constructedC.GetMembers("m2")(0), MethodSymbol)

            Assert.True(m1.CanConstruct)
            Assert.True(m1.OriginalDefinition.CanConstruct)
            Assert.Same(m1, m1.ConstructedFrom)
            Assert.Same(m1.TypeParameters(0), m1.TypeArguments(0))
            Assert.NotEqual(m1.OriginalDefinition.TypeParameters(0), m1.TypeParameters(0))
            Assert.Same(m1.OriginalDefinition.TypeParameters(0), m1.TypeParameters(0).OriginalDefinition)
            Assert.Same(m1, m1.TypeParameters(0).ContainingSymbol)
            Assert.Equal(2, m1.Arity)
            Assert.Same(m1, m1.Construct(m1.TypeParameters(0), m1.TypeParameters(1)))

            Dim m1_1 = DirectCast(constructedC.GetMembers("m1")(0), MethodSymbol)
            Assert.Equal(m1, m1_1)
            Assert.NotSame(m1, m1_1) ' Checks below need equal, but not identical symbols to test target scenarios!

            Assert.Same(m1, m1.Construct(m1_1.TypeParameters(0), m1_1.TypeParameters(1)))
            Dim alphaConstructedM1 = m1.Construct(m1_1.TypeParameters(1), m1_1.TypeParameters(0))

            Assert.Same(m1, alphaConstructedM1.ConstructedFrom)
            Assert.Same(alphaConstructedM1.TypeArguments(0), m1.TypeParameters(1))
            Assert.NotSame(alphaConstructedM1.TypeArguments(0), m1_1.TypeParameters(1))
            Assert.Same(alphaConstructedM1.TypeArguments(1), m1.TypeParameters(0))
            Assert.NotSame(alphaConstructedM1.TypeArguments(1), m1_1.TypeParameters(0))

            alphaConstructedM1 = m1.Construct(m1_1.TypeParameters(0), constructedC)

            Assert.Same(m1, alphaConstructedM1.ConstructedFrom)
            Assert.Same(alphaConstructedM1.TypeArguments(0), m1.TypeParameters(0))
            Assert.NotSame(alphaConstructedM1.TypeArguments(0), m1_1.TypeParameters(0))
            Assert.Same(alphaConstructedM1.TypeArguments(1), constructedC)

            alphaConstructedM1 = m1.Construct(constructedC, m1_1.TypeParameters(1))

            Assert.Same(m1, alphaConstructedM1.ConstructedFrom)
            Assert.Same(alphaConstructedM1.TypeArguments(0), constructedC)
            Assert.Same(alphaConstructedM1.TypeArguments(1), m1.TypeParameters(1))
            Assert.NotSame(alphaConstructedM1.TypeArguments(1), m1_1.TypeParameters(1))

            Assert.False(m2.CanConstruct)
            Assert.False(m2.OriginalDefinition.CanConstruct)
            Assert.Equal(0, m2.TypeParameters.Length)
            Assert.Equal(0, m2.TypeArguments.Length)

            Assert.Throws(Of InvalidOperationException)(Sub() m2.OriginalDefinition.Construct(classC))
            Assert.Throws(Of InvalidOperationException)(Sub() m2.Construct(classC))
            Assert.Throws(Of ArgumentException)(Sub() m1.OriginalDefinition.Construct(classC))
            Assert.Throws(Of ArgumentException)(Sub() m1.Construct(classC))

            Dim constructedC_d = constructedC.GetTypeMembers("D").Single()
            Dim m3 = DirectCast(constructedC_d.GetMembers("m3").Single(), MethodSymbol)

            Assert.Equal(1, m3.Arity)
            Assert.False(m3.CanConstruct)
            Assert.Throws(Of InvalidOperationException)(Sub() m3.Construct(classC))

            Dim d = classC.GetTypeMembers("D").Single()
            m3 = DirectCast(d.GetMembers("m3").Single(), MethodSymbol)
            Dim alphaConstructedM3 = m3.Construct(m1.TypeParameters(0))

            Assert.NotSame(m3, alphaConstructedM3)
            Assert.Same(m3, alphaConstructedM3.ConstructedFrom)
            Assert.Same(m1.TypeParameters(0), alphaConstructedM3.TypeArguments(0))

            Assert.Equal("T", m1.Parameters(0).Type.ToTestDisplayString())
            Assert.Equal("System.Int32", m1.Parameters(1).Type.ToTestDisplayString())
            Assert.Equal("System.Collections.Generic.KeyValuePair(Of System.Int32, U)", m1.ReturnType.ToTestDisplayString())
            Assert.Equal("T", m1.TypeParameters(0).ToTestDisplayString())
            Assert.Equal(m1.TypeParameters(0), m1.TypeArguments(0))
            Assert.Equal("U", m1.TypeParameters(1).ToTestDisplayString())
            Assert.Equal(m1.TypeParameters(1), m1.TypeArguments(1))
            Assert.Same(m1, m1.ConstructedFrom)

            Dim constructedM1 = m1.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Boolean)}).AsImmutableOrNull())
            Assert.Equal("System.String", constructedM1.Parameters(0).Type.ToTestDisplayString())
            Assert.Equal("System.Int32", constructedM1.Parameters(1).Type.ToTestDisplayString())
            Assert.Equal("System.Collections.Generic.KeyValuePair(Of System.Int32, System.Boolean)", constructedM1.ReturnType.ToTestDisplayString())
            Assert.Equal("T", constructedM1.TypeParameters(0).ToTestDisplayString())
            Assert.Equal("System.String", constructedM1.TypeArguments(0).ToTestDisplayString())
            Assert.Equal("U", constructedM1.TypeParameters(1).ToTestDisplayString())
            Assert.Equal("System.Boolean", constructedM1.TypeArguments(1).ToTestDisplayString())
            Assert.Same(m1, constructedM1.ConstructedFrom)
            Assert.Equal("Function C(Of System.Int32).m1(Of System.String, System.Boolean)(p1 As System.String, p2 As System.Int32) As System.Collections.Generic.KeyValuePair(Of System.Int32, System.Boolean)", constructedM1.ToTestDisplayString())
            Assert.False(constructedM1.CanConstruct)
            Assert.Throws(Of InvalidOperationException)(Sub() constructedM1.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Boolean)}).AsImmutableOrNull()))

            ' Try wrong arity.
            Assert.Throws(Of ArgumentException)(Sub()
                                                    Dim constructedM1WrongArity = m1.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_String)}).AsImmutableOrNull())
                                                End Sub)

            ' Try identity substitution.
            Dim identityM1 = m1.Construct(m1.OriginalDefinition.TypeParameters.As(Of TypeSymbol)())
            Assert.NotEqual(m1, identityM1)
            Assert.Same(m1, identityM1.ConstructedFrom)

            m1 = DirectCast(classC.GetMembers("m1").Single(), MethodSymbol)
            identityM1 = m1.Construct(m1.TypeParameters.As(Of TypeSymbol)())
            Assert.Same(m1, identityM1)

            constructedM1 = m1.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Boolean)}).AsImmutableOrNull())
            Assert.False(constructedM1.CanConstruct)
            Assert.Throws(Of InvalidOperationException)(Sub() constructedM1.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Boolean)}).AsImmutableOrNull()))
            Dim constructedM1_2 = m1.Construct(compilation.GetSpecialType(SpecialType.System_Byte), compilation.GetSpecialType(SpecialType.System_Boolean))
            Dim constructedM1_3 = m1.Construct((New TypeSymbol() {compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Boolean)}).AsImmutableOrNull())

            Assert.NotEqual(constructedM1, constructedM1_2)
            Assert.Equal(constructedM1, constructedM1_3)

            Dim p1 = constructedM1.Parameters(0)

            Assert.Equal(0, p1.Ordinal)
            Assert.Same(constructedM1, p1.ContainingSymbol)
            Assert.Equal(p1, p1)
            Assert.Equal(p1, constructedM1_3.Parameters(0))
            Assert.Equal(p1.GetHashCode(), constructedM1_3.Parameters(0).GetHashCode())
            Assert.NotEqual(m1.Parameters(0), p1)
            Assert.NotEqual(constructedM1_2.Parameters(0), p1)

            Dim constructedM3 = m3.Construct(compilation.GetSpecialType(SpecialType.System_String))
            Assert.NotEqual(constructedM3.Parameters(0), p1)
        End Sub

        <Fact>
        Public Sub InterfaceImplements01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Imports System.Collections.Generic

Namespace NS
    Public Class Abc
    End Class

    Public Interface IGoo(Of T)
        Sub I1Sub1(ByRef p As T)
    End Interface

    Public Interface I1
        Sub I1Sub1(ByRef p As String)
        Function I1Func1(ByVal p1 As Short, ByVal ParamArray p2 As Object()) As Integer
    End Interface

    Public Interface I2
        Inherits I1
        Sub I2Sub1()
        Function I2Func1(ByRef p1 As aBC) As AbC
    End Interface
End Namespace
    </file>
    <file name="b.vb">
Imports System.Collections.Generic

Namespace NS.NS1
  Class Impl
        Implements I2, IGoo(Of String)

        Public Sub Sub1(ByRef p As String) Implements I1.I1Sub1, IGoo(Of String).I1Sub1

        End Sub

        Public Function I1Func1(ByVal p1 As Short, ByVal ParamArray p2() As Object) As Integer Implements I1.I1Func1
            Return p1
        End Function

        Public Function I2Func1(ByRef p1 As ABc) As ABC Implements I2.I2Func1
            Return Nothing
        End Function

        Public Sub I2Sub1() Implements I2.I2Sub1

        End Sub
    End Class

    Structure StructImpl(Of T)
        Implements IGoo(Of T)

        Public Sub Sub1(ByRef p As T) Implements IGoo(Of T).I1Sub1

        End Sub
    End Structure
End Namespace
    </file>
</compilation>)

            Dim ns = DirectCast(compilation.SourceModule.GlobalNamespace.GetMembers("NS").AsEnumerable().SingleOrDefault(), NamespaceSymbol)
            Dim ns1 = DirectCast(ns.GetMembers("NS1").Single(), NamespaceSymbol)
            Dim classImpl = DirectCast(ns1.GetTypeMembers("impl").Single(), NamedTypeSymbol)
            'direct interfaces
            Assert.Equal(2, classImpl.Interfaces.Length)
            Dim itfc = DirectCast(classImpl.Interfaces(0), NamedTypeSymbol)
            Assert.Equal(1, itfc.Interfaces.Length)
            itfc = DirectCast(itfc.Interfaces(0), NamedTypeSymbol)
            Assert.Equal("I1", itfc.Name)
            Dim mem1 = DirectCast(classImpl.GetMembers("sub1").Single(), MethodSymbol)
            ' not impl
            'Assert.Equal(2, mem1.ExplicitInterfaceImplementation.Count)
            mem1 = DirectCast(classImpl.GetMembers("i2Func1").Single(), MethodSymbol)
            ' not impl
            'Assert.Equal(1, mem1.ExplicitInterfaceImplementation.Count)
            Dim param = DirectCast(mem1.Parameters(0), ParameterSymbol)
            Assert.True(param.IsByRef)
            Assert.Equal("ByRef " & param.Name & " As NS.Abc", param.ToTestDisplayString()) ' use case of declare's name

            Dim structImpl = DirectCast(ns1.GetTypeMembers("structimpl").Single(), NamedTypeSymbol)
            Assert.Equal(1, structImpl.Interfaces.Length)
            Dim mem2 = DirectCast(structImpl.GetMembers("sub1").Single(), MethodSymbol)
            ' not impl
            'Assert.Equal(1, mem2.ExplicitInterfaceImplementation.Count)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <WorkItem(537444, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537444")>
        <Fact>
        Public Sub DeclareFunction01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Option Explicit

Imports System
Imports System.Runtime.InteropServices

Public Structure S1
    Public strVar As String
    Public x As Integer
End Structure

Namespace NS
    Friend Module MyMod
        Declare Ansi Function VerifyString2 Lib "AttrUsgOthM010DLL.dll" (ByRef Arg As S1, ByVal Op As Integer) As String
    End Module

    Class cls1
        Overloads Declare Sub Goo Lib "someLib" ()
        Overloads Sub goo(ByRef arg As Integer)
            '   ...
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            Dim nsNS = DirectCast(compilation.Assembly.GlobalNamespace.GetMembers("NS").Single(), NamespaceSymbol)
            Dim modOfNS = DirectCast(nsNS.GetMembers("MyMod").Single(), NamedTypeSymbol)
            Dim mem1 = DirectCast(modOfNS.GetMembers().First(), MethodSymbol)

            Assert.Equal("VerifyString2", mem1.Name)
            ' TODO: add more verification when this is working

        End Sub

        <Fact>
        Public Sub CodepageOptionUnicodeMembers01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Module Module2
    Sub ÛÊÛÄÁÍäá()                                         ' 1067
        Console.WriteLine (CStr(AscW("ÛÊÛÄÁÍäá")))
    End Sub
End Module

Class Class1
    Public Shared Sub ŒŸõ()
        Console.WriteLine(CStr(AscW("ŒŸõ")))    ' 26908
    End Sub

    Friend Function [Widening](ByVal Ÿü As Short) As à–¾ 'invalid char in Dev10
        Return New à–¾(Ÿü)
    End Function
End Class

Structure ÄµÁiÛE
    Const str1 As String = "ÄµÁiÛE"                  ' 1044
    Dim i As Integer
End Structure

Public Class [Narrowing] 'êàê èäåíòèôèêàòîð.
    Public ñëîâî As [CULng]
End Class

Public Structure [CULng]
    Public [UInteger] As Integer
End Structure
    </file>
</compilation>)

            Dim glbNS = compilation.Assembly.GlobalNamespace

            Dim type1 = DirectCast(glbNS.GetMembers("Module2").Single(), NamedTypeSymbol)
            Dim mem1 = DirectCast(type1.GetMembers().First(), MethodSymbol)
            Assert.Equal(Accessibility.Public, mem1.DeclaredAccessibility)
            Assert.True(mem1.IsSub)
            Assert.Equal("Sub Module2.ÛÊÛÄÁÍäá()", mem1.ToTestDisplayString())

            Dim type2 = DirectCast(glbNS.GetMembers("Class1").Single(), NamedTypeSymbol)
            Dim mem2 = DirectCast(type2.GetMembers().First(), MethodSymbol)
            Assert.Equal(Accessibility.Public, mem2.DeclaredAccessibility)
            'Assert.Equal("ŒŸõ", mem2.Name) - TODO: Code Page issue

            Dim type3 = DirectCast(glbNS.GetTypeMembers("ÄµÁiÛE").Single(), NamedTypeSymbol)
            Dim mem3 = DirectCast(type3.GetMembers("Str1").Single(), FieldSymbol)
            Assert.True(mem3.IsConst)
            Assert.Equal("ÄµÁiÛE.str1 As System.String", mem3.ToTestDisplayString())

            Dim type4 = DirectCast(glbNS.GetTypeMembers("Narrowing").Single(), NamedTypeSymbol)
            Dim mem4 = DirectCast(type4.GetMembers("ñëîâî").Single(), FieldSymbol)
            Assert.Equal(TypeKind.Structure, mem4.Type.TypeKind)
        End Sub

        <WorkItem(537466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537466")>
        <Fact>
        Public Sub DefaultAccessibility01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Imports System

Interface GI(Of T)
    Sub Goo(ByVal t As T)
    Function Bar() As T
End Interface

Class GC
    Dim X As Integer
    Sub Goo()
    End Sub
    Function Bar() As String
        Return String.Empty
    End Function
    Property Prop As Integer
    Structure InnerStructure
    End Structure
    Class Innerclass
    End Class
    Delegate Sub DelegateA()
    Event EventA As DelegateA
End Class

Structure GS
    Dim X As Integer
    Sub Goo()
    End Sub
    Function Bar() As String
        Return String.Empty
    End Function
    Property Prop As Integer
    Structure InnerStructure
    End Structure
    Class Innerclass
    End Class
    Delegate Sub DelegateA()
    Event EventA As DelegateA
End Structure

Namespace NS
    Interface NI(Of T)
        Sub Goo(ByVal t As T)
        Function Bar() As T
    End Interface

    Class NC
        Dim X As Integer
        Sub Goo()
        End Sub
        Function Bar() As String
            Return String.Empty
        End Function
    End Class

    Structure NS
        Dim X As Integer
        Sub Goo()
        End Sub
        Function Bar() As String
            Return String.Empty
        End Function
    End Structure
End Namespace
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            ' interface - public
            Dim typemem = DirectCast(globalNS.GetTypeMembers("GI").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, typemem.DeclaredAccessibility)
            Dim mem = typemem.GetMembers("Goo").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Bar").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)

            ' Class - field (private), other - public
            typemem = DirectCast(globalNS.GetTypeMembers("GC").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, typemem.DeclaredAccessibility)
            mem = typemem.GetMembers("X").Single()
            Assert.Equal(Accessibility.Private, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Goo").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Bar").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Prop").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("InnerStructure").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("InnerClass").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("DelegateA").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            'mem = typemem.GetMembers("EventA").Single()
            'Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)

            ' Struct - public
            typemem = DirectCast(globalNS.GetTypeMembers("GS").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, typemem.DeclaredAccessibility)
            mem = typemem.GetMembers("X").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility) ' private is better but Dev10 is public
            mem = typemem.GetMembers("Goo").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Bar").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Prop").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("InnerStructure").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("InnerClass").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("DelegateA").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            'mem = typemem.GetMembers("EventA").Single()
            'Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)

            Dim nsNS = DirectCast(globalNS.GetMembers("NS").Single(), NamespaceSymbol)
            typemem = DirectCast(nsNS.GetTypeMembers("NI").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, typemem.DeclaredAccessibility)
            mem = typemem.GetMembers("Goo").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Bar").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)

            typemem = DirectCast(nsNS.GetTypeMembers("NC").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, typemem.DeclaredAccessibility)
            mem = typemem.GetMembers("X").Single()
            Assert.Equal(Accessibility.Private, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Goo").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Bar").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)

            typemem = DirectCast(nsNS.GetTypeMembers("NS").Single(), NamedTypeSymbol)
            Assert.Equal(Accessibility.Friend, typemem.DeclaredAccessibility)
            mem = typemem.GetMembers("X").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility) ' private is better but Dev10 is public
            mem = typemem.GetMembers("Goo").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)
            mem = typemem.GetMembers("Bar").Single()
            Assert.Equal(Accessibility.Public, mem.DeclaredAccessibility)

        End Sub

        <Fact>
        Public Sub OverloadsAndOverrides01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Dim derive = New NS.C2()
        derive.Goo(1) ' call derived Goo
        derive.Bar(1) ' call derived Bar
        derive.Boo(1) ' call derived Boo
        derive.VGoo(1) ' call derived VGoo
        derive.VBar(1) ' call derived VBar
        derive.VBoo(1) ' call derived VBoo
        Console.WriteLine("-------------")
        Dim base As NS.C1 = New NS.C2()
        base.Goo(1) ' call base Goo
        base.Bar(1) ' call base Bar
        base.Boo(1) ' call base Boo
        base.VGoo(1) ' call base Goo
        base.VBar(1) ' call D
        base.VBoo(1) ' call D
    End Sub
End Module

Namespace NS
    Public Class C1
        Public Sub Goo(ByVal p As Integer)
            Console.WriteLine("Base - Goo")
        End Sub
        Public Sub Bar(ByVal p As Integer)
            Console.WriteLine("Base - Bar")
        End Sub
        Public Sub Boo(ByVal p As Integer)
            Console.WriteLine("Base - Boo")
        End Sub
        Public Overridable Sub VGoo(ByVal p As Integer)
            Console.WriteLine("Base - VGoo")
        End Sub
        Public Overridable Sub VBar(ByVal p As Integer)
            Console.WriteLine("Base - VBar")
        End Sub
        Public Overridable Sub VBoo(ByVal p As Integer)
            Console.WriteLine("Base - VBoo")
        End Sub
    End Class

    Public Class C2
        Inherits C1
        Public Shadows Sub Goo(Optional ByVal p As Integer = 0)
            Console.WriteLine("Derived - Shadows Goo")
        End Sub
        Public Overloads Sub Bar(Optional ByVal p As Integer = 1)
            Console.WriteLine("Derived - Overloads Bar")
        End Sub
        ' warning
        Public Sub Boo(Optional ByVal p As Integer = 2)
            Console.WriteLine("Derived - Boo")
        End Sub
        ' not virtual
        Public Shadows Sub VGoo(Optional ByVal p As Integer = 0)
            Console.WriteLine("Derived - Shadows VGoo")
        End Sub
        ' hidebysig and virtual
        Public Overloads Overrides Sub VBar(ByVal p As Integer)
            Console.WriteLine("Derived - Overloads Overrides VBar")
        End Sub
        ' virtual
        Public Overrides Sub VBoo(ByVal p As Integer)
            Console.WriteLine("Derived - Overrides VBoo")
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            Dim ns = DirectCast(compilation.SourceModule.GlobalNamespace.GetMembers("NS").Single(), NamespaceSymbol)
            Dim type1 = DirectCast(ns.GetTypeMembers("C1").Single(), NamedTypeSymbol)
            Dim mem = DirectCast(type1.GetMembers("Goo").Single(), MethodSymbol)
            Assert.False(mem.IsOverridable)
            mem = DirectCast(type1.GetMembers("VGoo").Single(), MethodSymbol)
            Assert.True(mem.IsOverridable)

            Dim type2 = DirectCast(ns.GetTypeMembers("C2").Single(), NamedTypeSymbol)
            mem = DirectCast(type2.GetMembers("Goo").Single(), MethodSymbol)
            Assert.False(mem.IsOverloads)
            mem = DirectCast(type2.GetMembers("Bar").Single(), MethodSymbol)
            Assert.True(mem.IsOverloads)
            mem = DirectCast(type2.GetMembers("Boo").Single(), MethodSymbol)
            Assert.False(mem.IsOverloads)
            ' overridable
            mem = DirectCast(type2.GetMembers("VGoo").Single(), MethodSymbol)
            Assert.False(mem.IsOverloads)
            Assert.False(mem.IsOverrides)
            Assert.False(mem.IsOverridable)
            mem = DirectCast(type2.GetMembers("VBar").Single(), MethodSymbol)
            Assert.True(mem.IsOverloads)
            Assert.True(mem.IsOverrides)
            mem = DirectCast(type2.GetMembers("VBoo").Single(), MethodSymbol)
            Assert.True(mem.IsOverloads)
            Assert.True(mem.IsOverrides)
        End Sub

        <Fact>
        Public Sub Bug2820()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Class Class1
    sub Test(x as T)
    End sub
End Class
    </file>
</compilation>)

            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim test = class1.GetMembers("Test").OfType(Of MethodSymbol)().Single()

            Assert.Equal("T", test.Parameters(0).Type.Name)

        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="b.vb">
Class Base
    Sub BANANa(x as string, y as integer)
    End Sub
End Class

Partial Class Class1
    Inherits Base
    Sub baNana()
    End Sub
    Sub Banana(x as integer)
    End Sub
End Class
    </file>
    <file name="a.vb">
Partial Class Class1
    Sub baNANa(xyz as String)
    End Sub
    Sub BANANA(x as Long)
    End Sub
End Class
    </file>
</compilation>)
            ' No "Overloads", so all methods should match first overloads in first source file
            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim allMethods = class1.GetMembers("baNana").OfType(Of MethodSymbol)()

            ' All methods in Class1 should have metadata name "baNana" (from first file supplied to compilation).
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                Assert.Equal("baNana", m.MetadataName)
                If m.Parameters.Any Then
                    Assert.NotEqual("baNana", m.Name)
                End If
            Next
            Assert.Equal(4, count)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="b.vb">
Class Base
    Sub BANANa(x as string, y as integer)
    End Sub
End Class

Partial Class Class1
    Inherits Base
    Overloads Sub baNana()
    End Sub
    Overloads Sub Banana(x as integer)
    End Sub
End Class
    </file>
    <file name="a.vb">
Partial Class Class1
    Overloads Sub baNANa(xyz as String)
    End Sub
    Overloads Sub BANANA(x as Long)
    End Sub
End Class
    </file>
</compilation>)
            ' "Overloads" specified, so all methods should match method in base
            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim allMethods = class1.GetMembers("baNANa").OfType(Of MethodSymbol)()

            ' All methods in Class1 should have metadata name "baNANa".
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                Assert.Equal("BANANa", m.MetadataName)
            Next
            Assert.Equal(4, count)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="b.vb">
Class Base
    Overridable Sub BANANa(x as string, y as integer)
    End Sub
End Class

Partial Class Class1
    Inherits Base
    Overloads Sub baNana()
    End Sub
    Overrides Sub baNANa(xyz as String, a as integer)
    End Sub
    Overloads Sub Banana(x as integer)
    End Sub
End Class
    </file>
    <file name="a.vb">
Partial Class Class1
    Overloads Sub BANANA(x as Long)
    End Sub
End Class
    </file>
</compilation>)
            ' "Overrides" specified, so all methods should match method in base
            Dim class1 = compilation.GetTypeByMetadataName("Class1")
            Dim allMethods = class1.GetMembers("baNANa").OfType(Of MethodSymbol)()

            ' All methods in Class1 should have metadata name "BANANa".
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                Assert.Equal("BANANa", m.MetadataName)
            Next
            Assert.Equal(4, count)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="b.vb">
Interface Base1
    Sub BANANa(x as string, y as integer)
End Interface

Interface Base2
    Sub BANANa(x as string, y as integer, z as Object)
End Interface

Interface Base3
    Inherits Base2
End Interface

Interface Interface1
    Inherits Base1, Base3
    Overloads Sub baNana()
    Overloads Sub baNANa(xyz as String, a as integer)
    Overloads Sub Banana(x as integer)
End Interface
    </file>
</compilation>)
            ' "Overloads" specified, so all methods should match methods in base
            Dim interface1 = compilation.GetTypeByMetadataName("Interface1")
            Dim allMethods = interface1.GetMembers("baNANa").OfType(Of MethodSymbol)()

            CompilationUtils.AssertNoErrors(compilation)

            ' All methods in Interface1 should have metadata name "BANANa".
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                Assert.Equal("BANANa", m.MetadataName)
            Next
            Assert.Equal(3, count)

        End Sub

        <Fact>
        Public Sub MultipleOverloadsMetadataName5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="b.vb">
Interface Base1
    Sub BAnANa(x as string, y as integer)
End Interface

Interface Base2
    Sub BANANa(x as string, y as integer, z as Object)
End Interface

Interface Base3
    Inherits Base2
End Interface

Interface Interface1
    Inherits Base1, Base3
    Overloads Sub baNana()
    Overloads Sub baNANa(xyz as String, a as integer)
    Overloads Sub Banana(x as integer)
End Interface
    </file>
</compilation>)
            ' "Overloads" specified, but base methods have multiple casing, so don't use it.
            Dim interface1 = compilation.GetTypeByMetadataName("Interface1")
            Dim allMethods = interface1.GetMembers("baNANa").OfType(Of MethodSymbol)()

            CompilationUtils.AssertNoErrors(compilation)

            ' All methods in Interface1 should have metadata name "baNana".
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                Assert.Equal("baNana", m.MetadataName)
            Next
            Assert.Equal(3, count)

        End Sub

        <Fact>
        Public Sub ProbableExtensionMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="C">
    <file name="a.vb">
Option Strict On
Class C1
    Sub X()
        goodext1()
        goodext2()
        goodext3()
        goodext4()
        goodext5()
        goodext6()
        goodext7()
        goodext8()

        badext1()
        badext2()
        badext3()
        badext4()
        badext5()
        badext6()
    End Sub
End Class

Namespace Blah
    Class ExtensionAttribute
        Inherits System.Attribute
    End Class
End Namespace

    </file>
    <file name="b.vb"><![CDATA[
Option Strict On       
Imports System
Imports System.Runtime.CompilerServices
Imports ExAttribute = System.Runtime.CompilerServices.ExtensionAttribute
Imports Ex2 = System.Runtime.CompilerServices.ExtensionAttribute

Module M1
    <[Extension]>
    Public Sub goodext1(this As C1)
    End Sub

    <ExtensionAttribute>
    Public Sub goodext2(this As C1)
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Sub goodext3(this As C1)
    End Sub

    <System.Runtime.CompilerServices.ExtensionAttribute>
    Public Sub goodext4(this As C1)
    End Sub

    <[ExAttribute]>
    Public Sub goodext5(this As C1)
    End Sub

    <Ex>
    Public Sub goodext6(this As C1)
    End Sub

    <Ex2>
    Public Sub goodext7(this As C1)
    End Sub

    <AnExt>
    Public Sub goodext8(this As C1)
    End Sub

    <AnExt>
    Declare Sub goodext9 Lib "goo" (this As C1)

    <Blah.Extension>
    Public Sub badext1(this As C1)
    End Sub

    <Blah.ExtensionAttribute>
    Public Sub badext2(this As C1)
    End Sub

    <Extension>
    Public Sub badext3()
    End Sub


End Module
]]></file>
    <file name="c.vb"><![CDATA[
Option Strict On       
Imports System
Imports Blah

Module M2
    <Extension>
    Public Sub badext4(this As C1)
    End Sub

    <ExtensionAttribute>
    Public Sub badext5(this As C1)
    End Sub

    <Extension>
    Declare Sub badext6 Lib "goo" (this As C1)

End Module
]]></file>
</compilation>, references:={Net40.References.SystemCore, Net40.References.System}, options:=TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse("AnExt=System.Runtime.CompilerServices.ExtensionAttribute")))

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim sourceMod = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim modM1 = DirectCast(globalNS.GetMembers("M1").Single(), NamedTypeSymbol)
            Dim modM2 = DirectCast(globalNS.GetMembers("M2").Single(), NamedTypeSymbol)

            Dim goodext1 = DirectCast(modM1.GetMembers("goodext1").Single(), MethodSymbol)
            Assert.True(goodext1.IsExtensionMethod)
            Assert.True(goodext1.MayBeReducibleExtensionMethod)

            Dim goodext2 = DirectCast(modM1.GetMembers("goodext2").Single(), MethodSymbol)
            Assert.True(goodext2.IsExtensionMethod)
            Assert.True(goodext2.MayBeReducibleExtensionMethod)

            Dim goodext3 = DirectCast(modM1.GetMembers("goodext3").Single(), MethodSymbol)
            Assert.True(goodext3.IsExtensionMethod)
            Assert.True(goodext3.MayBeReducibleExtensionMethod)

            Dim goodext4 = DirectCast(modM1.GetMembers("goodext4").Single(), MethodSymbol)
            Assert.True(goodext4.IsExtensionMethod)
            Assert.True(goodext4.MayBeReducibleExtensionMethod)

            Dim goodext5 = DirectCast(modM1.GetMembers("goodext5").Single(), MethodSymbol)
            Assert.True(goodext5.IsExtensionMethod)
            Assert.True(goodext5.MayBeReducibleExtensionMethod)

            Dim goodext6 = DirectCast(modM1.GetMembers("goodext6").Single(), MethodSymbol)
            Assert.True(goodext6.IsExtensionMethod)
            Assert.True(goodext6.MayBeReducibleExtensionMethod)

            Dim goodext7 = DirectCast(modM1.GetMembers("goodext7").Single(), MethodSymbol)
            Assert.True(goodext7.IsExtensionMethod)
            Assert.True(goodext7.MayBeReducibleExtensionMethod)

            Dim goodext8 = DirectCast(modM1.GetMembers("goodext8").Single(), MethodSymbol)
            Assert.True(goodext8.IsExtensionMethod)
            Assert.True(goodext8.MayBeReducibleExtensionMethod)

            Dim goodext9 = DirectCast(modM1.GetMembers("goodext9").Single(), MethodSymbol)
            Assert.True(goodext9.IsExtensionMethod)
            Assert.True(goodext9.MayBeReducibleExtensionMethod)

            Dim badext1 = DirectCast(modM1.GetMembers("badext1").Single(), MethodSymbol)
            Assert.False(badext1.IsExtensionMethod)
            Assert.True(badext1.MayBeReducibleExtensionMethod)

            Dim badext2 = DirectCast(modM1.GetMembers("badext2").Single(), MethodSymbol)
            Assert.False(badext2.IsExtensionMethod)
            Assert.True(badext2.MayBeReducibleExtensionMethod)

            Dim badext3 = DirectCast(modM1.GetMembers("badext3").Single(), MethodSymbol)
            Assert.False(badext3.IsExtensionMethod)
            Assert.True(badext3.MayBeReducibleExtensionMethod)

            Dim badext4 = DirectCast(modM2.GetMembers("badext4").Single(), MethodSymbol)
            Assert.False(badext4.IsExtensionMethod)
            Assert.True(badext4.MayBeReducibleExtensionMethod)

            Dim badext5 = DirectCast(modM2.GetMembers("badext5").Single(), MethodSymbol)
            Assert.False(badext5.IsExtensionMethod)
            Assert.True(badext5.MayBeReducibleExtensionMethod)

            Dim badext6 = DirectCast(modM2.GetMembers("badext6").Single(), MethodSymbol)
            Assert.False(badext6.IsExtensionMethod)
            Assert.True(badext6.MayBeReducibleExtensionMethod)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                               <expected>
BC30455: Argument not specified for parameter 'this' of 'Public Sub badext1(this As C1)'.
        badext1()
        ~~~~~~~
BC30455: Argument not specified for parameter 'this' of 'Public Sub badext2(this As C1)'.
        badext2()
        ~~~~~~~
BC30455: Argument not specified for parameter 'this' of 'Public Sub badext4(this As C1)'.
        badext4()
        ~~~~~~~
BC30455: Argument not specified for parameter 'this' of 'Public Sub badext5(this As C1)'.
        badext5()
        ~~~~~~~
BC30455: Argument not specified for parameter 'this' of 'Public Declare Ansi Sub badext6 Lib "goo" (this As C1)'.
        badext6()
        ~~~~~~~
BC36552: Extension methods must declare at least one parameter. The first parameter specifies which type to extend.
    Public Sub badext3()
               ~~~~~~~
                                               </expected>)
        End Sub

        <WorkItem(779441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/779441")>
        <Fact>
        Public Sub UserDefinedOperatorLocation()
            Dim source = <![CDATA[
Public Class C
    Public Shared Operator +(c As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim operatorPos = source.IndexOf("+"c)
            Dim parenPos = source.IndexOf("("c)

            Dim comp = CreateCompilationWithMscorlib40({Parse(source)})
            Dim Symbol = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMembers(WellKnownMemberNames.UnaryPlusOperatorName).Single()
            Dim span = Symbol.Locations.Single().SourceSpan
            Assert.Equal(operatorPos, span.Start)
            Assert.Equal(parenPos, span.End)
        End Sub

        <WorkItem(901815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/901815")>
        <Fact>
        Public Sub UserDefinedConversionLocation()
            Dim source = <![CDATA[
Public Class C
    Public Shared Operator +(Of T)
        Return Nothing
    End Operator
End Class
]]>.Value

            ' Used to raise an exception.
            Dim comp = CreateCompilationWithMscorlib40({Parse(source)}, options:=TestOptions.ReleaseDll)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC33016: Operator '+' must have either one or two parameters.
    Public Shared Operator +(Of T)
                           ~
BC30198: ')' expected.
    Public Shared Operator +(Of T)
                            ~
BC30199: '(' expected.
    Public Shared Operator +(Of T)
                            ~
BC32065: Type parameters cannot be specified on this declaration.
    Public Shared Operator +(Of T)
                            ~~~~~~
]]></errors>)
        End Sub

        <Fact, WorkItem(51082, "https://github.com/dotnet/roslyn/issues/51082")>
        Public Sub IsPartialDefinitionOnNonPartial()
            Dim source = <![CDATA[
Public Class C
    Sub M()
    End Sub
End Class
]]>.Value

            Dim comp = CreateCompilation(source)
            comp.AssertTheseDiagnostics()
            Dim m As IMethodSymbol = comp.GetMember(Of MethodSymbol)("C.M")
            Assert.False(m.IsPartialDefinition)
        End Sub

        <Fact, WorkItem(51082, "https://github.com/dotnet/roslyn/issues/51082")>
        Public Sub IsPartialDefinitionOnPartialDefinitionOnly()
            Dim source = <![CDATA[
Public Class C
    Private Partial Sub M()
    End Sub
End Class
]]>.Value

            Dim comp = CreateCompilation(source)
            comp.AssertTheseDiagnostics()
            Dim m As IMethodSymbol = comp.GetMember(Of MethodSymbol)("C.M")
            Assert.True(m.IsPartialDefinition)
            Assert.Null(m.PartialDefinitionPart)
            Assert.Null(m.PartialImplementationPart)
        End Sub

        <Fact, WorkItem(51082, "https://github.com/dotnet/roslyn/issues/51082")>
        Public Sub IsPartialDefinitionWithPartialImplementation()
            Dim source = <![CDATA[
Public Class C
    Private Partial Sub M()
    End Sub

    Private Sub M()
    End Sub
End Class
]]>.Value

            Dim comp = CreateCompilation(source)
            comp.AssertTheseDiagnostics()
            Dim m As IMethodSymbol = comp.GetMember(Of MethodSymbol)("C.M")
            Assert.True(m.IsPartialDefinition)
            Assert.Null(m.PartialDefinitionPart)
            Assert.False(m.PartialImplementationPart.IsPartialDefinition)
        End Sub
    End Class
End Namespace
