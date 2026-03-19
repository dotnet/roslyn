' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SymbolId

    Partial Public Class SymbolIdTest
        Inherits SymbolKeyTestBase

#Region "No change to symbol"

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528864")>
        Public Sub C2CTypeSymbolUnchanged01()

            Dim src1 = <compilation name="C2CTypeSymbolUnchanged01">
                           <file name="a.vb">
Imports System
Public Delegate Sub DGoo(p1 As Integer, p2 As String)
Namespace N1.N2

    Public Interface IGoo
    End Interface

    Namespace N3
        Public Class CGoo
            Public Structure SGoo
                Public Enum EGoo
                    Zero
                    One
                End Enum
            End Structure
        End Class
    End Namespace
End Namespace
                         </file>
                       </compilation>

            Dim src2 = <compilation name="C2CTypeSymbolUnchanged01">
                           <file name="b.vb">
Public Delegate Sub DGoo(p1 As Integer, p2 As String)
Namespace N1.N2

    Public Interface IGoo
        Function GetClass() As N3.CGoo
    End Interface

    Namespace N3
        Public Class CGoo
            Public Structure SGoo
                ' Update member
                Public Enum EGoo
                    Zero
                    One
                    Two
                End Enum
            End Structure
            ' Add member
            Public Sub M(n As Integer)
                Console.WriteLine(n)
            End Sub
        End Class
    End Namespace
End Namespace
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            Dim comp2 = CreateCompilationWithMscorlib40(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType Or SymbolCategory.DeclaredNamespace)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType Or SymbolCategory.DeclaredNamespace)

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530198")>
        Public Sub C2CTypeSymbolCaseChangeOnly()

            Dim src1 = <compilation name="C2CTypeSymbolCaseChangeOnly">
                           <file name="a.vb">
Imports System
Namespace NGOO
    Public Interface IGOO
        Delegate Sub DGOO(p As Integer)
    End Interface
End Namespace
                         </file>
                       </compilation>

            Dim src2 = <compilation name="C2CTypeSymbolCaseChangeOnly">
                           <file name="b.vb">
Imports System
Namespace nGOO
    Public Interface IGoo ' case
        Delegate Sub DgOO(p As Integer)
    End Interface
End Namespace
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            Dim comp2 = CreateCompilationWithMscorlib40(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType Or SymbolCategory.DeclaredNamespace)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType Or SymbolCategory.DeclaredNamespace)
            ' case-insensitive
            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1)

            Dim origlist = originalSymbols.OrderBy(Function(s) s.Name).ToList()
            Dim newlist = newSymbols.OrderBy(Function(s) s.Name).ToList()
            ' case sensitive
            For i = 0 To newlist.Count - 1 Step 1
                Dim sym1 = origlist(i)
                Dim sym2 = newlist(i)

                AssertSymbolsIdsEqual(sym2, sym1, SymbolIdComparison.None, expectEqual:=False)
                Dim resolvedSymbol = ResolveSymbol(sym2, comp1, SymbolIdComparison.None) ' ignored
                Assert.NotNull(resolvedSymbol)
                Assert.Equal(sym1, resolvedSymbol)
            Next

        End Sub

#End Region

#Region "Change to symbol but same ID"

        <Fact>
        Public Sub C2CTypeSymbolChanged01()

            Dim src1 = <compilation name="C2CTypeSymbolChanged01">
                           <file name="a.vb">
Imports System
Public Delegate Sub DGoo(p As Integer)

Namespace N1.N2

    Public Interface IBase
    End Interface

    Public Interface IGoo
    End Interface

    Namespace N3
        Public Class CGoo
            Public Structure SGoo
                Public Enum EGoo
                    Zero
                    One
                End Enum
            End Structure
        End Class
    End Namespace
End Namespace
                         </file>
                       </compilation>

            Dim src2 = <compilation name="C2CTypeSymbolChanged01">
                           <file name="a.vb">
Public Delegate Sub DGoo(p1 As Integer, p2 As String) ' One more param

Namespace N1.N2

    Public Interface IBase
    End Interface

    Public Interface IGoo
        Inherits IBase ' base
    End Interface

    Namespace N3
        Public Class cGoo
            Implements Igoo ' impl

            Private Structure SGOO ' modifier

                Public Enum egoo As Long ' change base, case
                    Zero
                    ONE
                End Enum
            End Structure

        End Class
    End Namespace
End Namespace
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            Dim comp2 = CreateCompilationWithMscorlib40(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType)

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1)
        End Sub

        <Fact>
        Public Sub C2CAssemblySymbolChanged01()

            Dim src1 = <compilation name="C2CAssemblySymbolChanged01">
                           <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.2.3.4")>
Public Class C
End Class
]]>
                           </file>
                       </compilation>

            Dim src2 = <compilation name="C2CAssemblySymbolChanged02">
                           <file name="b.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("5.6.7.8")>
Public Class C
End Class
]]>
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            Dim comp2 = CreateCompilationWithMscorlib40(src2)

            Dim sym1 = comp1.SourceModule.GlobalNamespace.GetMembers("C").FirstOrDefault()
            Dim sym2 = comp2.SourceModule.GlobalNamespace.GetMembers("C").FirstOrDefault()

            AssertSymbolsIdsEqual(sym2, sym1, SymbolIdComparison.IgnoreCase, expectEqual:=False)
            Assert.Null(ResolveSymbol(sym2, comp1, SymbolIdComparison.IgnoreCase))
            ' ignore asm id 
            ResolveAndVerifySymbol(sym2, sym1, comp1, SymbolIdComparison.IgnoreCase Or SymbolIdComparison.IgnoreAssemblyIds)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530170")>
        Public Sub C2CAssemblySymbolChanged02()

            Dim src1 = <compilation name="C2CAssemblySymbolChanged01">
                           <file name="a.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.2.3.4")>
Public Class C
End Class
]]>
                           </file>
                       </compilation>

            Dim src2 = <compilation name="C2CAssemblySymbolChanged02">
                           <file name="b.vb"><![CDATA[
<Assembly: System.Reflection.AssemblyVersion("1.2.3.4")>
Public Class C
End Class
]]>
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            Dim comp2 = CreateCompilationWithMscorlib40(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType)
            Dim sym1 As ISymbol = comp1.Assembly
            Dim sym2 As ISymbol = comp2.Assembly

            AssertSymbolsIdsEqual(sym2, sym1, SymbolIdComparison.IgnoreCase, False)
            Assert.Null(ResolveSymbol(sym2, comp1, SymbolIdComparison.IgnoreCase))
            ' ignore asm id 
            ' Same ID
            AssertSymbolsIdsEqual(sym2, sym1, SymbolIdComparison.IgnoreAssemblyIds)
            ' but can NOT resolve
            Assert.Null(ResolveSymbol(sym2, comp1, SymbolIdComparison.IgnoreCase Or SymbolIdComparison.IgnoreAssemblyIds))

            sym1 = comp1.Assembly.Modules(0)
            sym2 = comp2.Assembly.Modules(0)

            AssertSymbolsIdsEqual(sym2, sym1, SymbolIdComparison.IgnoreCase, False)
            Assert.Null(ResolveSymbol(sym2, comp1, SymbolIdComparison.IgnoreCase))

            AssertSymbolsIdsEqual(sym2, sym1, SymbolIdComparison.IgnoreAssemblyIds)
            Assert.Null(ResolveSymbol(sym2, comp1, SymbolIdComparison.IgnoreAssemblyIds))
        End Sub

#End Region

    End Class

End Namespace
