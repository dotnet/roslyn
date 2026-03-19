' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class TypeSubstitutionTests
        <Fact>
        Public Sub Substitution1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic        
Class G(Of T, U)
    Public a As T
    Public b as U()
    Public c As Dictionary(Of U, IEnumerable(Of T))
    Public d As G(Of T, U)
    Public Function e(ByRef p1 as T, p2 as U(), p3 as G(Of T, U)) As Dictionary(Of IEnumerable(Of U), T)
        Return Nothing
    End Function
End Class    
    </file>
    <file name="b.vb">
Class B
    Public x as G(Of Integer, String)
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim classB = DirectCast(globalNS.GetTypeMembers("B").First, NamedTypeSymbol)
            Dim fieldx = DirectCast(classB.GetMembers("x").First, FieldSymbol)
            Dim substType = fieldx.Type

            Dim substFieldA = DirectCast(substType.GetMembers("a").First, FieldSymbol)
            Assert.Equal("System.Int32", substFieldA.Type.ToTestDisplayString())

            Dim substFieldB = DirectCast(substType.GetMembers("b").First, FieldSymbol)
            Assert.Equal("System.String()", substFieldB.Type.ToTestDisplayString())

            Dim substFieldC = DirectCast(substType.GetMembers("c").First, FieldSymbol)
            Assert.Equal("System.Collections.Generic.Dictionary(Of System.String, System.Collections.Generic.IEnumerable(Of System.Int32))",
                         substFieldC.Type.ToTestDisplayString())

            Dim substFieldD = DirectCast(substType.GetMembers("d").First, FieldSymbol)
            Assert.Equal("G(Of System.Int32, System.String)", substFieldD.Type.ToTestDisplayString())
            Assert.Equal(substFieldD.ContainingType, substFieldD.Type)

            Dim substMethodE = DirectCast(substType.GetMembers("e").First, MethodSymbol)
            Assert.Equal("Function G(Of System.Int32, System.String).e(ByRef p1 As System.Int32, p2 As System.String(), p3 As G(Of System.Int32, System.String)) As System.Collections.Generic.Dictionary(Of System.Collections.Generic.IEnumerable(Of System.String), System.Int32)",
                         substMethodE.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub Substitution2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Namespace Goo
    Class B(Of R, S)
        Function Func(ByVal p1 As R) As S
        End Function
    End Class

    Class C(Of T)
        Class E(Of U)
            Inherits B(Of T, U)
        End Class

        Class D
            Inherits C(Of String)
            Class F
                Inherits E(Of K(Of T))
            End Class
        End Class
    End Class

    Class J
        Inherits C(Of Integer)
    End Class
End Namespace

Class K(Of W)
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.SourceModule.GlobalNamespace
            Dim gooNS = DirectCast(globalNS.GetMembers("Goo").First, NamespaceSymbol)

            Dim classDSymbol As TypeSymbol = DirectCast(DirectCast(GooNS.GetMembers("J").First(), NamedTypeSymbol).BaseType.GetMembers("D").First(), NamedTypeSymbol)
            Assert.Equal("Goo.C(Of System.Int32).D", classDSymbol.ToTestDisplayString())
            Dim classFSymbol = DirectCast(classDSymbol.GetMembers("F").First(), TypeSymbol)
            Assert.Equal("Goo.C(Of System.Int32).D.F", classFSymbol.ToTestDisplayString())
            Dim classFBaseType = classFSymbol.BaseType
            Dim classFBaseTypeSquared = classFBaseType.BaseType
            Assert.Equal("Goo.B(Of System.String, K(Of System.Int32))", classFBaseTypeSquared.ToTestDisplayString())

            Dim method = classFSymbol.BaseType.BaseType.GetMembers("Func").First()
            Assert.Equal("Function Goo.B(Of System.String, K(Of System.Int32)).Func(p1 As System.String) As K(Of System.Int32)", method.ToTestDisplayString())

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub
    End Class
End Namespace

