' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SymbolId

    Partial Public Class SymbolIdTest
        Inherits SymbolKeyTestBase

#Region "No change to symbol"

        <WorkItem(528864)>
        <WpfFact>
        Public Sub C2CTypeSymbolUnchanged01()

            Dim src1 = <compilation name="C2CTypeSymbolUnchanged01">
                           <file name="a.vb">
Imports System
Public Delegate Sub DFoo(p1 As Integer, p2 As String)
Namespace N1.N2

    Public Interface IFoo
    End Interface

    Namespace N3
        Public Class CFoo
            Public Structure SFoo
                Public Enum EFoo
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
Public Delegate Sub DFoo(p1 As Integer, p2 As String)
Namespace N1.N2

    Public Interface IFoo
        Function GetClass() As N3.CFoo
    End Interface

    Namespace N3
        Public Class CFoo
            Public Structure SFoo
                ' Update member
                Public Enum EFoo
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

            Dim comp1 = CreateCompilationWithMscorlib(src1)
            Dim comp2 = CreateCompilationWithMscorlib(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType Or SymbolCategory.DeclaredNamespace)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType Or SymbolCategory.DeclaredNamespace)

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1)
        End Sub

        <WpfFact, WorkItem(530198)>
        Public Sub C2CTypeSymbolCaseChangeOnly()

            Dim src1 = <compilation name="C2CTypeSymbolCaseChangeOnly">
                           <file name="a.vb">
Imports System
Namespace NFOO
    Public Interface IFOO
        Delegate Sub DFOO(p As Integer)
    End Interface
End Namespace
                         </file>
                       </compilation>

            Dim src2 = <compilation name="C2CTypeSymbolCaseChangeOnly">
                           <file name="b.vb">
Imports System
Namespace nFOO
    Public Interface IFoo ' case
        Delegate Sub DfOO(p As Integer)
    End Interface
End Namespace
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib(src1)
            Dim comp2 = CreateCompilationWithMscorlib(src2)

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

                AssertSymbolsIdsEqual(sym2, comp2, sym1, comp1, SymbolIdComparison.CaseSensitive, expectEqual:=False)
                Dim resolvedSymbol = ResolveSymbol(sym2, comp2, comp1, SymbolIdComparison.CaseSensitive) ' ignored
                Assert.NotNull(resolvedSymbol)
                Assert.Equal(sym1, resolvedSymbol)
            Next

        End Sub

#End Region

#Region "Change to symbol but same ID"

        <WpfFact>
        Public Sub C2CTypeSymbolChanged01()

            Dim src1 = <compilation name="C2CTypeSymbolChanged01">
                           <file name="a.vb">
Imports System
Public Delegate Sub DFoo(p As Integer)

Namespace N1.N2

    Public Interface IBase
    End Interface

    Public Interface IFoo
    End Interface

    Namespace N3
        Public Class CFoo
            Public Structure SFoo
                Public Enum EFoo
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
Public Delegate Sub DFoo(p1 As Integer, p2 As String) ' One more param

Namespace N1.N2

    Public Interface IBase
    End Interface

    Public Interface IFoo
        Inherits IBase ' base
    End Interface

    Namespace N3
        Public Class cFoo
            Implements Ifoo ' impl

            Private Structure SFOO ' modifier

                Public Enum efoo As Long ' change base, case
                    Zero
                    ONE
                End Enum
            End Structure

        End Class
    End Namespace
End Namespace
                           </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib(src1)
            Dim comp2 = CreateCompilationWithMscorlib(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType)

            ResolveAndVerifySymbolList(newSymbols, comp2, originalSymbols, comp1)
        End Sub

        <WpfFact>
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

            Dim comp1 = CreateCompilationWithMscorlib(src1)
            Dim comp2 = CreateCompilationWithMscorlib(src2)

            Dim sym1 = comp1.SourceModule.GlobalNamespace.GetMembers("C").FirstOrDefault()
            Dim sym2 = comp2.SourceModule.GlobalNamespace.GetMembers("C").FirstOrDefault()

            AssertSymbolsIdsEqual(sym2, comp2, sym1, comp1, SymbolIdComparison.CaseInsensitive, expectEqual:=False)
            Assert.Null(ResolveSymbol(sym2, comp2, comp1, SymbolIdComparison.CaseInsensitive))
            ' ignore asm id 
            ResolveAndVerifySymbol(sym2, comp2, sym1, comp1, SymbolIdComparison.CaseInsensitive Or SymbolIdComparison.IgnoreAssemblyIds)
        End Sub

        <WpfFact, WorkItem(530170)>
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

            Dim comp1 = CreateCompilationWithMscorlib(src1)
            Dim comp2 = CreateCompilationWithMscorlib(src2)

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType)
            Dim newSymbols = GetSourceSymbols(comp2, SymbolCategory.DeclaredType)
            Dim sym1 As ISymbol = comp1.Assembly
            Dim sym2 As ISymbol = comp2.Assembly

            AssertSymbolsIdsEqual(sym2, comp2, sym1, comp1, SymbolIdComparison.CaseInsensitive, False)
            Assert.Null(ResolveSymbol(sym2, comp2, comp1, SymbolIdComparison.CaseInsensitive))
            ' ignore asm id 
            ' Same ID
            AssertSymbolsIdsEqual(sym2, comp2, sym1, comp1, SymbolIdComparison.IgnoreAssemblyIds)
            ' but can NOT resolve
            Assert.Null(ResolveSymbol(sym2, comp2, comp1, SymbolIdComparison.CaseInsensitive Or SymbolIdComparison.IgnoreAssemblyIds))

            sym1 = comp1.Assembly.Modules(0)
            sym2 = comp2.Assembly.Modules(0)

            AssertSymbolsIdsEqual(sym2, comp2, sym1, comp1, SymbolIdComparison.CaseInsensitive, False)
            Assert.Null(ResolveSymbol(sym2, comp2, comp1, SymbolIdComparison.CaseInsensitive))

            AssertSymbolsIdsEqual(sym2, comp2, sym1, comp1, SymbolIdComparison.IgnoreAssemblyIds)
            Assert.Null(ResolveSymbol(sym2, comp2, comp1, SymbolIdComparison.IgnoreAssemblyIds))
        End Sub

#End Region

    End Class

End Namespace
