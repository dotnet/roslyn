' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class LookupTests
        Inherits BasicTestBase

        Private Function GetContext(compilation As VisualBasicCompilation,
                                   treeName As String,
                                   textToFind As String) As Binder
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim position = CompilationUtils.FindPositionFromText(tree, textToFind)

            Return DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel).GetEnclosingBinder(position)
        End Function


        <Fact()>
        Public Sub TestLookupResult()
            Dim sym1 = New MockAssemblySymbol("hello") ' just a symbol to put in results
            Dim sym2 = New MockAssemblySymbol("goodbye") ' just a symbol to put in results
            Dim sym3 = New MockAssemblySymbol("world") ' just a symbol to put in results
            Dim sym4 = New MockAssemblySymbol("banana") ' just a symbol to put in results
            Dim sym5 = New MockAssemblySymbol("apple") ' just a symbol to put in results
            Dim meth1 = New MockMethodSymbol("foo") ' just a symbol to put in results
            Dim meth2 = New MockMethodSymbol("bag") ' just a symbol to put in results
            Dim meth3 = New MockMethodSymbol("baz") ' just a symbol to put in results

            Dim r1 = New LookupResult()
            r1.SetFrom(SingleLookupResult.Empty)
            Assert.False(r1.HasSymbol)
            Assert.False(r1.IsGood)
            Assert.False(r1.HasDiagnostic)
            Assert.False(r1.StopFurtherLookup)

            Dim r2 = SingleLookupResult.Good(sym1)
            Dim _r2 = New LookupResult()
            _r2.SetFrom(r2)
            Assert.True(_r2.HasSymbol)
            Assert.True(_r2.IsGood)
            Assert.Same(sym1, _r2.SingleSymbol)
            Assert.False(_r2.HasDiagnostic)
            Assert.True(_r2.StopFurtherLookup)

            Dim r3 = New LookupResult()
            r3.SetFrom(SingleLookupResult.Ambiguous(ImmutableArray.Create(Of Symbol)(sym1, sym2, sym3), AddressOf GenerateAmbiguity))
            Assert.True(r3.HasSymbol)
            Assert.False(r3.IsGood)
            Assert.Same(sym1, r3.SingleSymbol)
            Assert.True(r3.HasDiagnostic)
            Assert.True(r3.StopFurtherLookup)
            Dim diag3 = DirectCast(r3.Diagnostic, AmbiguousSymbolDiagnostic)
            Assert.Same(sym1, diag3.AmbiguousSymbols.Item(0))
            Assert.Same(sym2, diag3.AmbiguousSymbols.Item(1))
            Assert.Same(sym3, diag3.AmbiguousSymbols.Item(2))

            Dim r4 = New LookupResult()
            r4.SetFrom(SingleLookupResult.Inaccessible(sym2, New BadSymbolDiagnostic(sym2, ERRID.ERR_InaccessibleSymbol2, sym2)))
            Assert.True(r4.HasSymbol)
            Assert.False(r4.IsGood)
            Assert.Same(sym2, r4.SingleSymbol)
            Assert.True(r4.HasDiagnostic)
            Assert.False(r4.StopFurtherLookup)
            Dim diag4 = DirectCast(r4.Diagnostic, BadSymbolDiagnostic)
            Assert.Equal(ERRID.ERR_InaccessibleSymbol2, diag4.Code)
            Assert.Same(sym2, diag4.BadSymbol)

            Dim r5 = New LookupResult()
            r5.SetFrom(SingleLookupResult.WrongArity(sym3, ERRID.ERR_IndexedNotArrayOrProc))
            Assert.True(r5.HasSymbol)
            Assert.False(r5.IsGood)
            Assert.Same(sym3, r5.SingleSymbol)
            Assert.True(r5.HasDiagnostic)
            Assert.False(r5.StopFurtherLookup)
            Dim diag5 = r5.Diagnostic
            Assert.Equal(ERRID.ERR_IndexedNotArrayOrProc, diag5.Code)

            Dim r6 = New LookupResult()
            r6.MergePrioritized(r1)
            r6.MergePrioritized(r2)
            Assert.True(r6.HasSymbol)
            Assert.Same(sym1, r6.SingleSymbol)
            Assert.False(r6.HasDiagnostic)
            Assert.True(r6.StopFurtherLookup)
            r6.Free()

            Dim r7 = New LookupResult()
            r7.MergePrioritized(r2)
            r7.MergePrioritized(r1)
            Assert.True(r7.HasSymbol)
            Assert.Same(sym1, r7.SingleSymbol)
            Assert.False(r7.HasDiagnostic)
            Assert.True(r7.StopFurtherLookup)

            Dim r8 = New LookupResult()
            r8.SetFrom(SingleLookupResult.Good(sym4))
            Dim r9 = New LookupResult()
            r9.SetFrom(r2)
            r9.MergePrioritized(r8)
            Assert.True(r9.HasSymbol)
            Assert.Same(sym1, r9.SingleSymbol)
            Assert.False(r9.HasDiagnostic)
            Assert.True(r9.StopFurtherLookup)

            Dim r10 = New LookupResult()
            r10.SetFrom(r3)
            r10.MergePrioritized(r8)
            r10.MergePrioritized(r2)
            Assert.True(r10.HasSymbol)
            Assert.Same(sym1, r10.SingleSymbol)
            Assert.True(r10.HasDiagnostic)
            Assert.True(r10.StopFurtherLookup)
            Dim diag10 = DirectCast(r10.Diagnostic, AmbiguousSymbolDiagnostic)
            Assert.Same(sym1, diag10.AmbiguousSymbols.Item(0))
            Assert.Same(sym2, diag10.AmbiguousSymbols.Item(1))
            Assert.Same(sym3, diag10.AmbiguousSymbols.Item(2))

            Dim r11 = New LookupResult()
            r11.MergePrioritized(r1)
            r11.MergePrioritized(r5)
            r11.MergePrioritized(r3)
            r11.MergePrioritized(r8)
            r11.MergePrioritized(r2)
            Assert.True(r11.HasSymbol)
            Assert.Same(sym1, r11.SingleSymbol)
            Assert.True(r11.HasDiagnostic)
            Assert.True(r11.StopFurtherLookup)
            Dim diag11 = DirectCast(r11.Diagnostic, AmbiguousSymbolDiagnostic)
            Assert.Same(sym1, diag11.AmbiguousSymbols.Item(0))
            Assert.Same(sym2, diag11.AmbiguousSymbols.Item(1))
            Assert.Same(sym3, diag11.AmbiguousSymbols.Item(2))

            Dim r12 = New LookupResult()
            Dim r12Empty = New LookupResult()
            r12.MergePrioritized(r1)
            r12.MergePrioritized(r12Empty)
            Assert.False(r12.HasSymbol)
            Assert.False(r12.HasDiagnostic)
            Assert.False(r12.StopFurtherLookup)

            Dim r13 = New LookupResult()
            r13.MergePrioritized(r1)
            r13.MergePrioritized(r5)
            r13.MergePrioritized(r4)
            Assert.True(r13.HasSymbol)
            Assert.Same(sym2, r13.SingleSymbol)
            Assert.True(r13.HasDiagnostic)
            Assert.False(r13.StopFurtherLookup)
            Dim diag13 = DirectCast(r13.Diagnostic, BadSymbolDiagnostic)
            Assert.Equal(ERRID.ERR_InaccessibleSymbol2, diag13.Code)
            Assert.Same(sym2, diag13.BadSymbol)

            Dim r14 = New LookupResult()
            r14.MergeAmbiguous(r1, AddressOf GenerateAmbiguity)
            r14.MergeAmbiguous(r5, AddressOf GenerateAmbiguity)
            r14.MergeAmbiguous(r4, AddressOf GenerateAmbiguity)
            Assert.True(r14.HasSymbol)
            Assert.Same(sym2, r14.SingleSymbol)
            Assert.True(r14.HasDiagnostic)
            Assert.False(r14.StopFurtherLookup)
            Dim diag14 = DirectCast(r14.Diagnostic, BadSymbolDiagnostic)
            Assert.Equal(ERRID.ERR_InaccessibleSymbol2, diag14.Code)
            Assert.Same(sym2, diag14.BadSymbol)

            Dim r15 = New LookupResult()
            r15.MergeAmbiguous(r1, AddressOf GenerateAmbiguity)
            r15.MergeAmbiguous(r8, AddressOf GenerateAmbiguity)
            r15.MergeAmbiguous(r3, AddressOf GenerateAmbiguity)
            r15.MergeAmbiguous(r14, AddressOf GenerateAmbiguity)
            Assert.True(r15.HasSymbol)
            Assert.Same(sym4, r15.SingleSymbol)
            Assert.True(r15.HasDiagnostic)
            Assert.True(r15.StopFurtherLookup)
            Dim diag15 = DirectCast(r15.Diagnostic, AmbiguousSymbolDiagnostic)
            Assert.Same(sym4, diag15.AmbiguousSymbols.Item(0))
            Assert.Same(sym1, diag15.AmbiguousSymbols.Item(1))
            Assert.Same(sym2, diag15.AmbiguousSymbols.Item(2))
            Assert.Same(sym3, diag15.AmbiguousSymbols.Item(3))

            Dim r16 = SingleLookupResult.Good(meth1)

            Dim r17 = SingleLookupResult.Good(meth2)

            Dim r18 = SingleLookupResult.Good(meth3)

            Dim r19 = New LookupResult()
            r19.MergeMembersOfTheSameType(r16, False)
            Assert.True(r19.StopFurtherLookup)
            Assert.Equal(1, r19.Symbols.Count)
            Assert.False(r19.HasDiagnostic)
            r19.MergeMembersOfTheSameType(r17, False)
            Assert.True(r19.StopFurtherLookup)
            Assert.Equal(2, r19.Symbols.Count)
            Assert.False(r19.HasDiagnostic)
            r19.MergeMembersOfTheSameType(r18, False)
            Assert.True(r19.StopFurtherLookup)
            Assert.Equal(3, r19.Symbols.Count)
            Assert.Equal(r16.Symbol, r19.Symbols(0))
            Assert.Equal(r17.Symbol, r19.Symbols(1))
            Assert.Equal(r18.Symbol, r19.Symbols(2))
            Assert.False(r19.HasDiagnostic)
            r19.MergeAmbiguous(r2, AddressOf GenerateAmbiguity)
            Assert.True(r19.StopFurtherLookup)
            Assert.Equal(1, r19.Symbols.Count)
            Assert.Equal(r16.Symbol, r19.SingleSymbol)
            Assert.True(r19.HasDiagnostic)
            Dim diag19 = DirectCast(r19.Diagnostic, AmbiguousSymbolDiagnostic)
            Assert.Equal(4, diag19.AmbiguousSymbols.Length)
            Assert.Equal(r16.Symbol, diag19.AmbiguousSymbols(0))
            Assert.Equal(r17.Symbol, diag19.AmbiguousSymbols(1))
            Assert.Equal(r18.Symbol, diag19.AmbiguousSymbols(2))
            Assert.Equal(r2.Symbol, diag19.AmbiguousSymbols(3))

        End Sub

        Private Function GenerateAmbiguity(syms As ImmutableArray(Of Symbol)) As AmbiguousSymbolDiagnostic
            Return New AmbiguousSymbolDiagnostic(ERRID.ERR_AmbiguousInModules2, syms, New FormattedSymbolList(syms.AsEnumerable))
        End Function

        <Fact()>
        Public Sub MemberLookup1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Comp">
    <file name="a.vb">
Option Strict On
Option Explicit On

Class A
    Public Class M3(Of T)
    End Class

    Public Overloads Sub M4(ByVal x As Integer, ByVal y As Integer)
    End Sub

    Public Overloads Sub M5(ByVal x As Integer, ByVal y As Integer)
    End Sub
End Class

Class B
        Inherits A

        Public Shared Sub M1(Of T)()
        End Sub
        Public Shared Sub M2()
        End Sub
        Public Shadows M3 As Integer

        Public Overloads Sub M4(ByVal x As Integer, ByVal y As String)
        End Sub

        Public Shadows Sub M5(ByVal x As Integer, ByVal y As String)
        End Sub
End Class

Class C
    Inherits B

    Public Shadows Class M1
    End Class
    Public Shadows Class M2(Of T)
    End Class

    Public Overloads Sub M4()
    End Sub

    Public Overloads Sub M4(ByVal x As Integer)
    End Sub

    Public Overloads Sub M5()
    End Sub

    Public Overloads Sub M5(ByVal x As Integer)
    End Sub
End Class

Module Module1
    Sub Main()
    End Sub
End Module

    </file>
</compilation>, TestOptions.ReleaseExe)
            Dim context = GetContext(compilation, "a.vb", "Sub Main")
            Dim globalNS = compilation.GlobalNamespace

            Dim classA = DirectCast(globalNS.GetMembers("A").Single(), NamedTypeSymbol)
            Dim classB = DirectCast(globalNS.GetMembers("B").Single(), NamedTypeSymbol)
            Dim classC = DirectCast(globalNS.GetMembers("C").Single(), NamedTypeSymbol)

            Dim classA_M3 = DirectCast(classA.GetMembers("M3").Single(), NamedTypeSymbol)
            Dim methA_M4 = DirectCast(classA.GetMembers("M4").Single(), MethodSymbol)
            Dim methA_M5 = DirectCast(classA.GetMembers("M5").Single(), MethodSymbol)
            Dim methB_M1 = DirectCast(classB.GetMembers("M1").Single(), MethodSymbol)
            Dim methB_M2 = DirectCast(classB.GetMembers("M2").Single(), MethodSymbol)
            Dim methB_M4 = DirectCast(classB.GetMembers("M4").Single(), MethodSymbol)
            Dim methB_M5 = DirectCast(classB.GetMembers("M5").Single(), MethodSymbol)
            Dim fieldB_M3 = DirectCast(classB.GetMembers("M3").Single(), FieldSymbol)
            Dim classC_M1 = DirectCast(classC.GetMembers("M1").Single(), NamedTypeSymbol)
            Dim classC_M2 = DirectCast(classC.GetMembers("M2").Single(), NamedTypeSymbol)
            Dim methC_M4_0 = DirectCast(classC.GetMembers("M4")(0), MethodSymbol)
            Dim methC_M4_1 = DirectCast(classC.GetMembers("M4")(1), MethodSymbol)
            Dim methC_M5_0 = DirectCast(classC.GetMembers("M5")(0), MethodSymbol)
            Dim methC_M5_1 = DirectCast(classC.GetMembers("M5")(1), MethodSymbol)
            Dim lr As LookupResult

            ' nothing found
            lr = New LookupResult()
            context.LookupMember(lr, classC, "fizzle", 0, Nothing, Nothing)
            Assert.Equal(LookupResultKind.Empty, lr.Kind)

            ' non-generic class shadows with arity 0
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M1", 0, Nothing, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(classC_M1, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            ' method found with arity 1
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M1", 1, Nothing, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(methB_M1, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            ' generic class shadows with arity 1
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M2", 1, Nothing, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(classC_M2, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            ' method found with arity 0
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M2", 0, Nothing, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(methB_M2, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            ' field shadows with arity 1
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M3", 1, Nothing, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(fieldB_M3, lr.Symbols.Single())
            Assert.True(lr.HasDiagnostic)

            ' should collection all overloads of M4
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M4", 1, LookupOptions.AllMethodsOfAnyArity, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(4, lr.Symbols.Count)
            Assert.Contains(methA_M4, lr.Symbols)
            Assert.Contains(methB_M4, lr.Symbols)
            Assert.Contains(methC_M4_0, lr.Symbols)
            Assert.Contains(methC_M4_1, lr.Symbols)
            Assert.False(lr.HasDiagnostic)

            ' shouldn't get A.M5 because B.M5 is marked Shadows
            lr = New LookupResult()
            context.LookupMember(lr, classC, "M5", 1, LookupOptions.AllMethodsOfAnyArity, Nothing)
            Assert.True(lr.StopFurtherLookup)
            Assert.Equal(3, lr.Symbols.Count)
            Assert.DoesNotContain(methA_M5, lr.Symbols)
            Assert.Contains(methB_M5, lr.Symbols)
            Assert.Contains(methC_M5_0, lr.Symbols)
            Assert.Contains(methC_M5_1, lr.Symbols)
            Assert.False(lr.HasDiagnostic)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub


        <Fact()>
        Public Sub Bug3024()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug3024">
    <file name="a.vb">
Imports P
Imports R
Module C
    Dim x As Q
End Module
Namespace R
    Module M
        Class Q
        End Class
    End Module
End Namespace
Namespace P.Q
End Namespace
    </file>
</compilation>)


            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)

            Assert.Same(compilation.GetTypeByMetadataName("R.M+Q"), compilation.GetTypeByMetadataName("C").GetMembers("x").OfType(Of FieldSymbol)().Single().Type)
        End Sub

        <Fact()>
        Public Sub Bug3025()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug3025">
    <file name="a.vb">
Imports P
Imports R
Module C
    Dim x As Q
End Module
Namespace R
    Module M
        Class Q
        End Class
    End Module
End Namespace
Namespace P.Q
    Class Z
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30182: Type expected.
    Dim x As Q
             ~
</expected>)
        End Sub

        <Fact()>
        Public Sub Bug4099()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4099">
    <file name="a.vb">
Imports N
Imports K

Namespace N
    Module M
        Class C
        End Class
    End Module
End Namespace

Namespace K
    Class C
    End Class
End Namespace

Class A
    Inherits C
End Class
    </file>
</compilation>)


            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)

            Assert.Same(compilation.GetTypeByMetadataName("K.C"), compilation.GetTypeByMetadataName("A").BaseType)
        End Sub

        <Fact()>
        Public Sub Bug4100()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Bug4100">
    <file name="a.vb">
Imports N
Imports K

Namespace N
    Class C
    End Class
End Namespace

Namespace K
    Namespace C
        Class D
        End Class
    End Namespace
End Namespace

Class A
    Inherits C
End Class
    </file>
</compilation>)


            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)

            Assert.Same(compilation.GetTypeByMetadataName("N.C"), compilation.GetTypeByMetadataName("A").BaseType)

            compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Bug4100">
    <file name="a.vb">
Imports K
Imports N

Namespace N
    Class C
    End Class
End Namespace

Namespace K
    Namespace C
        Class D
        End Class
    End Namespace
End Namespace

Class A
    Inherits C
End Class
    </file>
</compilation>)


            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)

            Assert.Same(compilation.GetTypeByMetadataName("N.C"), compilation.GetTypeByMetadataName("A").BaseType)
        End Sub


        <Fact()>
        Public Sub Bug3015()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug3015">
    <file name="a.vb">
Imports P
Imports R

Module Module1
    Sub Main()
        Dim x As Q = New Q()
        System.Console.WriteLine(x.GetType())
    End Sub
End Module

Namespace R
    Class Q
    End Class
End Namespace

Namespace P.Q
End Namespace
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
R.Q
]]>)
        End Sub


        <Fact()>
        Public Sub Bug3014()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug3014">
    <file name="a.vb">
Imports P = System
Imports R
Module C
    Sub Main()
        Dim x As P(Of Integer)
        x=Nothing
    End Sub
End Module
Namespace R
    Class P(Of T)
    End Class
End Namespace
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32045: 'System' has no type parameters and so cannot have type arguments.
        Dim x As P(Of Integer)
                 ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AmbiguityInImports()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AmbiguityInImports1">
    <file name="a.vb">
Namespace NS1

    Friend Class CT1
    End Class

    Friend Class CT2
    End Class

    Public Class CT3(Of T)
    End Class

    Public Class CT4
    End Class

    Public Module M1

        Friend Class CT1
        End Class

        Public Class CT2(Of T)
        End Class

        Public Class CT3
        End Class

    End Module

    Public Module M2
        Public Class CT3
        End Class
    End Module
End Namespace

Namespace NS2
    Public Class CT5
    End Class

    Public Module M3
        Public Class CT5
        End Class
    End Module
End Namespace

Namespace NS3
    Public Class CT5
    End Class
End Namespace

    </file>
</compilation>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="AmbiguityInImports3">
    <file name="a.vb">
Namespace NS1
    Namespace CT4
    End Namespace
End Namespace

Namespace NS2
    Namespace CT5
    End Namespace
End Namespace
    </file>
</compilation>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="AmbiguityInImports2">
    <file name="a.vb">
Imports NS2
Imports NS3

Namespace NS1

    Module Module1
        Sub Test()
            Dim x1 As CT1
            Dim x2 As CT2
            Dim x3 As CT3
            Dim x4 As CT4
            Dim x5 As CT5

            x1 = Nothing
            x2 = Nothing
            x3 = Nothing
            x4 = Nothing
            x5 = Nothing
        End Sub
    End Module

End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(compilation1),
                 New VisualBasicCompilationReference(compilation3)})

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30389: 'NS1.CT1' is not accessible in this context because it is 'Friend'.
            Dim x1 As CT1
                      ~~~
BC30389: 'NS1.CT2' is not accessible in this context because it is 'Friend'.
            Dim x2 As CT2
                      ~~~
BC30562: 'CT3' is ambiguous between declarations in Modules 'NS1.M1, NS1.M2'.
            Dim x3 As CT3
                      ~~~
BC30560: 'CT4' is ambiguous in the namespace 'NS1'.
            Dim x4 As CT4
                      ~~~
BC30560: 'CT5' is ambiguous in the namespace 'NS2'.
            Dim x5 As CT5
                      ~~~
</expected>)
        End Sub


        <Fact()>
        Public Sub TieBreakingInImports()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="TieBreakingInImports1">
    <file name="a.vb">
Namespace NS1
    Namespace Test1
        Public Class Test3
        End Class
    End Namespace

    Public Class Test2
    End Class

    Public Class Test5
    End Class
End Namespace

Namespace NS2
    Namespace Test1
        Public Class Test4
        End Class
    End Namespace

    Public Class Test5
    End Class
End Namespace
    </file>
</compilation>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="TieBreakingInImports2">
    <file name="a.vb">
Namespace NS3
    Class Test1
    End Class

    Class Test1(Of T)
    End Class

    Class Test5
    End Class
End Namespace
    </file>
</compilation>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="TieBreakingInImports3">
    <file name="a.vb">
Namespace NS2
    Class Test2(Of T)
    End Class
End Namespace
    </file>
</compilation>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="TieBreakingInImports4">
    <file name="a.vb">
            Imports NS1
            Imports NS2
            Imports NS3

            Module Test
                Sub Test()
                    Dim x1 As Test1 = Nothing
                    Dim x2 As Test1(Of Integer) = Nothing
                    Dim x3 As Test2(Of Integer) = Nothing
                    Dim x4 As Test5 = Nothing
                End Sub
            End Module
                </file>
</compilation>, {New VisualBasicCompilationReference(compilation1),
                 New VisualBasicCompilationReference(compilation2),
                 New VisualBasicCompilationReference(compilation3)})

            CompilationUtils.AssertTheseDiagnostics(compilation4,
<expected>
BC30182: Type expected.
                    Dim x1 As Test1 = Nothing
                              ~~~~~
BC30389: 'NS3.Test1(Of T)' is not accessible in this context because it is 'Friend'.
                    Dim x2 As Test1(Of Integer) = Nothing
                              ~~~~~~~~~~~~~~~~~
BC30389: 'NS2.Test2(Of T)' is not accessible in this context because it is 'Friend'.
                    Dim x3 As Test2(Of Integer) = Nothing
                              ~~~~~~~~~~~~~~~~~
BC30561: 'Test5' is ambiguous, imported from the namespaces or types 'NS1, NS2'.
                    Dim x4 As Test5 = Nothing
                              ~~~~~
</expected>)

            Dim compilation5 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="TieBreakingInImports4">
    <file name="a.vb">
            Imports NS2
            Imports NS3
            Imports NS1

            Module Test
                Sub Test()
                    Dim x1 As Test1 = Nothing
                    Dim x2 As Test1(Of Integer) = Nothing
                    Dim x3 As Test2(Of Integer) = Nothing
                    Dim x4 As Test5 = Nothing
                End Sub
            End Module
                </file>
</compilation>, {New VisualBasicCompilationReference(compilation1),
                 New VisualBasicCompilationReference(compilation2),
                 New VisualBasicCompilationReference(compilation3)})

            CompilationUtils.AssertTheseDiagnostics(compilation5,
<expected>
BC30182: Type expected.
                    Dim x1 As Test1 = Nothing
                              ~~~~~
BC30389: 'NS3.Test1(Of T)' is not accessible in this context because it is 'Friend'.
                    Dim x2 As Test1(Of Integer) = Nothing
                              ~~~~~~~~~~~~~~~~~
BC30389: 'NS2.Test2(Of T)' is not accessible in this context because it is 'Friend'.
                    Dim x3 As Test2(Of Integer) = Nothing
                              ~~~~~~~~~~~~~~~~~
BC30561: 'Test5' is ambiguous, imported from the namespaces or types 'NS2, NS1'.
                    Dim x4 As Test5 = Nothing
                              ~~~~~
            </expected>)

            Dim compilation6 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="TieBreakingInImports4">
    <file name="a.vb">
            Imports NS3
            Imports NS1
            Imports NS2

            Module Test
                Sub Test()
                    Dim x1 As Test1 = Nothing
                    Dim x2 As Test1(Of Integer) = Nothing
                    Dim x3 As Test2(Of Integer) = Nothing
                    Dim x4 As Test5 = Nothing
                End Sub
            End Module
    </file>
</compilation>, {New VisualBasicCompilationReference(compilation1),
                 New VisualBasicCompilationReference(compilation2),
                 New VisualBasicCompilationReference(compilation3)})

            CompilationUtils.AssertTheseDiagnostics(compilation6,
<expected>
BC30182: Type expected.
                    Dim x1 As Test1 = Nothing
                              ~~~~~
BC30389: 'NS3.Test1(Of T)' is not accessible in this context because it is 'Friend'.
                    Dim x2 As Test1(Of Integer) = Nothing
                              ~~~~~~~~~~~~~~~~~
BC30389: 'NS2.Test2(Of T)' is not accessible in this context because it is 'Friend'.
                    Dim x3 As Test2(Of Integer) = Nothing
                              ~~~~~~~~~~~~~~~~~
BC30561: 'Test5' is ambiguous, imported from the namespaces or types 'NS1, NS2'.
                    Dim x4 As Test5 = Nothing
                              ~~~~~
</expected>)

        End Sub


        <Fact()>
        Public Sub RecursiveCheckForAccessibleTypesWithinANamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="RecursiveCheckForAccessibleTypesWithinANamespace1">
    <file name="a.vb">
Imports P

Module Module1
    Sub Main()
        Dim x As Q.R.S = New Q.R.S()
        System.Console.WriteLine(x.GetType())
    End Sub
End Module

Namespace P
    Namespace Q
        Namespace R
            Public Class S
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
P.Q.R.S
]]>)


            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="RecursiveCheckForAccessibleTypesWithinANamespace2">
    <file name="a.vb">
Imports P

Module Module1
    Sub Main()
        Dim x As Q.R.S = New Q.R.S()
        System.Console.WriteLine(x.GetType())
    End Sub
End Module

Namespace P
    Namespace Q
        Namespace R
            Friend Class S
            End Class

            Friend Class T
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
P.Q.R.S
]]>)
        End Sub

        <Fact()>
        Public Sub DoNotLoadTypesForAccessibilityOfMostAccessibleTypeWithinANamespace()
            ' We need to be careful about metadata references we use here.
            ' The test checks that fields of namespace symbols are initialized in certain order.
            ' If we used a shared Mscorlib reference then other tests might have already initialized it's shared AssemblySymbol.
            Dim nonSharedMscorlibReference = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib).GetReference(display:="mscorlib.v4_0_30319.dll")

            Dim c = VisualBasicCompilation.Create("DoNotLoadTypesForAccessibilityOfMostAccessibleTypeWithinANamespace",
                                                     syntaxTrees:={Parse(<text>
                                                                            Namespace P
                                                                            End Namespace
                                                                        </text>.Value)},
                                                     references:={nonSharedMscorlibReference})

            Dim system = c.Assembly.Modules(0).GetReferencedAssemblySymbols()(0).GlobalNamespace.GetMembers("System").OfType(Of PENamespaceSymbol)().Single()
            Dim deployment = system.GetMembers("Deployment").OfType(Of PENamespaceSymbol)().Single()
            Dim internal = deployment.GetMembers("Internal").OfType(Of PENamespaceSymbol)().Single()
            Dim isolation = internal.GetMembers("Isolation").OfType(Of PENamespaceSymbol)().Single()

            Assert.Equal(Accessibility.Private, system.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, deployment.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, internal.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, isolation.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.False(isolation.AreTypesLoaded)

            Assert.Equal(Accessibility.Friend, isolation.DeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.False(isolation.AreTypesLoaded)
            Assert.Equal(Accessibility.Friend, isolation.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, system.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, deployment.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, internal.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            isolation.GetTypeMembers()
            Assert.True(isolation.AreTypesLoaded)

            Dim io = system.GetMembers("IO").OfType(Of PENamespaceSymbol)().Single()
            Dim isolatedStorage = io.GetMembers("IsolatedStorage").OfType(Of PENamespaceSymbol)().Single()

            Assert.Equal(Accessibility.Private, system.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, io.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, isolatedStorage.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.False(isolatedStorage.AreTypesLoaded)

            Assert.Equal(Accessibility.Public, isolatedStorage.DeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.False(isolatedStorage.AreTypesLoaded)
            Assert.Equal(Accessibility.Public, isolatedStorage.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Public, system.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Public, io.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
        End Sub

        <Fact()>
        Public Sub TestMergedNamespaceContainsTypesAccessibleFrom()
            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C1">
    <file name="a.vb">
Namespace P
    Namespace Q
        Public Class R
        End Class
    End Namespace
End Namespace
    </file>
</compilation>)

            Dim c2 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C2">
    <file name="a.vb">
Namespace P
    Namespace Q
        Friend Class S
        End Class
    End Namespace
End Namespace
    </file>
</compilation>)

            Dim c3 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="C3">
    <file name="a.vb">
Namespace P
    Namespace Q
    End Namespace
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(c1), New VisualBasicCompilationReference(c2)})

            Dim p = c3.GlobalNamespace.GetMembers("P").OfType(Of MergedNamespaceSymbol)().Single()
            Dim q = p.GetMembers("Q").OfType(Of MergedNamespaceSymbol)().Single()

            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(0, p.RawContainsAccessibleTypes)
            Assert.Equal(0, q.RawContainsAccessibleTypes)

            Assert.Equal(Accessibility.Public, q.DeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Public, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Public, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(0, p.RawContainsAccessibleTypes)
            Assert.Equal(0, q.RawContainsAccessibleTypes)

            Assert.True(q.ContainsTypesAccessibleFrom(c3.Assembly))
            Assert.True(p.ContainsTypesAccessibleFrom(c3.Assembly))

            Assert.Equal(0, p.RawContainsAccessibleTypes)
            Assert.Equal(0, q.RawContainsAccessibleTypes)

            c3 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="C3">
    <file name="a.vb">
Namespace P
    Namespace Q
        Friend Class U
        End Class
    End Namespace
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(c2)})

            p = c3.GlobalNamespace.GetMembers("P").OfType(Of MergedNamespaceSymbol)().Single()
            q = p.GetMembers("Q").OfType(Of MergedNamespaceSymbol)().Single()

            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.Unknown, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.Unknown, q.RawContainsAccessibleTypes)

            Assert.True(q.ContainsTypesAccessibleFrom(c3.Assembly))

            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.True, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.True, q.RawContainsAccessibleTypes)

            Assert.True(p.ContainsTypesAccessibleFrom(c3.Assembly))
            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)

            Dim c4 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="C4">
    <file name="a.vb">
Namespace P
    Namespace Q
    End Namespace
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(c3), New VisualBasicCompilationReference(c2)})

            p = c4.GlobalNamespace.GetMembers("P").OfType(Of MergedNamespaceSymbol)().Single()
            q = p.GetMembers("Q").OfType(Of MergedNamespaceSymbol)().Single()

            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.Unknown, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.Unknown, q.RawContainsAccessibleTypes)

            Assert.False(q.ContainsTypesAccessibleFrom(c4.Assembly))

            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.Unknown, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.False, q.RawContainsAccessibleTypes)

            Assert.False(p.ContainsTypesAccessibleFrom(c4.Assembly))
            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.False, p.RawContainsAccessibleTypes)

            c4 = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="C4">
    <file name="a.vb">
Namespace P
    Namespace Q
    End Namespace
End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(c3), New VisualBasicCompilationReference(c2)})

            p = c4.GlobalNamespace.GetMembers("P").OfType(Of MergedNamespaceSymbol)().Single()
            q = p.GetMembers("Q").OfType(Of MergedNamespaceSymbol)().Single()

            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.Unknown, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.Unknown, q.RawContainsAccessibleTypes)

            Assert.Equal(Accessibility.Friend, q.DeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Private, p.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(Accessibility.Friend, q.RawLazyDeclaredAccessibilityOfMostAccessibleDescendantType)
            Assert.Equal(ThreeState.Unknown, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.Unknown, q.RawContainsAccessibleTypes)

            Assert.False(q.ContainsTypesAccessibleFrom(c4.Assembly))
            Assert.Equal(ThreeState.Unknown, p.RawContainsAccessibleTypes)
            Assert.Equal(ThreeState.False, q.RawContainsAccessibleTypes)

            Assert.False(p.ContainsTypesAccessibleFrom(c4.Assembly))
            Assert.Equal(ThreeState.False, p.RawContainsAccessibleTypes)
        End Sub

        <Fact()>
        Public Sub Bug4128()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4128">
    <file name="a.vb">
Imports A = C.B
Imports XXXXXXX = UNKNOWN.UNKNOWN(Of UNKNOWN)  'BUG #4115
Imports XXXXYYY = UNKNOWN(Of UNKNOWN)

Module X
    Class C
    End Class
End Module

Module Y
    Class C
    End Class
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30562: 'C' is ambiguous between declarations in Modules 'X, Y'.
Imports A = C.B
            ~
BC40056: Namespace or type specified in the Imports 'UNKNOWN.UNKNOWN' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports XXXXXXX = UNKNOWN.UNKNOWN(Of UNKNOWN)  'BUG #4115
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'UNKNOWN' is not defined.
Imports XXXXXXX = UNKNOWN.UNKNOWN(Of UNKNOWN)  'BUG #4115
                                     ~~~~~~~
BC40056: Namespace or type specified in the Imports 'UNKNOWN' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports XXXXYYY = UNKNOWN(Of UNKNOWN)
                  ~~~~~~~~~~~~~~~~~~~
BC30002: Type 'UNKNOWN' is not defined.
Imports XXXXYYY = UNKNOWN(Of UNKNOWN)
                             ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub Bug4220()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4220">
    <file name="a.vb">
Imports A
Imports A.B
Imports A.B

Namespace A
  Module B
  End Module
End Namespace

    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31051: Namespace or type 'B' has already been imported.
Imports A.B
        ~~~
</expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4220">
    <file name="a.vb">
Imports A
Imports A.B

Module Module1
  Sub Main()
    c()
  End Sub
End Module

Namespace A
  Module B
    Public Sub c()
      System.Console.WriteLine("Sub c()")
    End Sub
  End Module
End Namespace
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
Sub c()
]]>)
        End Sub

        <Fact()>
        Public Sub Bug4180()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Bug4180">
    <file name="a.vb">
Namespace System
    Class [Object]
    End Class
    Class C
        Inherits [Object]
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            Assert.Same(compilation.Assembly.GetTypeByMetadataName("System.Object"), compilation.GetTypeByMetadataName("System.C").BaseType)

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="C1">
    <file name="a.vb">
Namespace NS1
    Namespace NS2
        Public Class C1
        End Class
    End Namespace
End Namespace

Namespace NS5
    Public Module Module3
        Public Sub Test()
        End Sub
    End Module
End Namespace
    </file>
</compilation>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="C2">
    <file name="a.vb">
Namespace NS1
    Namespace NS2
        Namespace C1
            Public Class C2
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS5

    Public Module Module4
        Public Sub Test()
        End Sub
    End Module

End Namespace
    </file>
</compilation>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="C3">
    <file name="a.vb">
Namespace NS1

    Module Module1
        Sub Main()
            Dim x As NS2.C1.C2 = New NS2.C1.C2()
            System.Console.WriteLine(x.GetType())
        End Sub
    End Module

    Namespace NS2
        Namespace C1

        End Namespace
    End Namespace

End Namespace
    </file>
</compilation>, {New VisualBasicCompilationReference(compilation1), New VisualBasicCompilationReference(compilation2)}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation3, <![CDATA[
NS1.NS2.C1.C2
]]>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C4">
    <file name="a.vb">
Namespace NS1
    Namespace NS2
        Namespace C1
            Public Class C3
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>)

            compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="C3">
    <file name="a.vb">
Imports NS1

Module Module1
    Sub Main()
        Dim x As NS2.C1.C2 = Nothing
    End Sub
End Module
    </file>
</compilation>, {New VisualBasicCompilationReference(compilation1), New VisualBasicCompilationReference(compilation2), New VisualBasicCompilationReference(compilation4)}, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation3,
<expected>
BC30560: 'C1' is ambiguous in the namespace 'NS1.NS2'.
        Dim x As NS2.C1.C2 = Nothing
                 ~~~~~~
</expected>)

            compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="C3">
    <file name="a.vb">
Namespace NS5

    Module Module1
        Sub Main()
            Test()
        End Sub
    End Module

End Namespace    
</file>
</compilation>, {New VisualBasicCompilationReference(compilation1), New VisualBasicCompilationReference(compilation2)}, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation3,
<expected>
BC30562: 'Test' is ambiguous between declarations in Modules 'NS5.Module3, NS5.Module4'.
            Test()
            ~~~~
</expected>)

            compilation3 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="C3">
    <file name="a.vb">
Namespace NS5

    Module Module1
        Sub Main()
            Test()
        End Sub
    End Module

    Module Module2
        Sub Test()
            System.Console.WriteLine("Module2.Test")
        End Sub
    End Module

End Namespace    </file>
</compilation>, {New VisualBasicCompilationReference(compilation1), New VisualBasicCompilationReference(compilation2)}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation3, <![CDATA[
Module2.Test
]]>)
        End Sub


        <Fact()>
        Public Sub Bug4817()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4817">
    <file name="a.vb">
Imports A
Imports B

Class A
    Shared Sub Foo()
        System.Console.WriteLine("A.Foo()")
    End Sub
End Class

Class B
    Inherits A
End Class

Module C
    Sub Main()
        Foo()
    End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseExe)


            CompileAndVerify(compilation, <![CDATA[
A.Foo()
]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4817">
    <file name="a.vb">
Imports A
Imports B

Class A
    Shared Sub Foo()
        System.Console.WriteLine("A.Foo()")
    End Sub
End Class

Class B
    Inherits A

    Overloads Shared Sub Foo(x As Integer)
        System.Console.WriteLine("B.Foo()")
    End Sub
End Class

Module C
    Sub Main()
        Foo()
    End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30561: 'Foo' is ambiguous, imported from the namespaces or types 'A, B, A'.
        Foo()
        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub LookupOptionMustBeInstance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On
Option Explicit On

Interface I
    Sub FooInstance()
End Interface

Class A
    Public Shared Sub FooShared()
    End Sub

    Public Sub FooInstance()
    End Sub
End Class

Module Module1
    Sub Main()
    End Sub
End Module

    </file>
</compilation>, TestOptions.ReleaseExe)
            Dim context = GetContext(compilation, "a.vb", "Sub Main")
            Dim globalNS = compilation.GlobalNamespace

            Dim classA = DirectCast(globalNS.GetMembers("A").Single(), NamedTypeSymbol)

            Dim fooShared = DirectCast(classA.GetMembers("FooShared").Single(), MethodSymbol)
            Dim fooInstance = DirectCast(classA.GetMembers("FooInstance").Single(), MethodSymbol)

            Dim lr As LookupResult

            ' Find Shared member
            lr = New LookupResult()
            context.LookupMember(lr, classA, "FooShared", 0, LookupOptions.MustNotBeInstance, Nothing)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(fooShared, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            lr = New LookupResult()
            context.LookupMember(lr, classA, "FooInstance", 0, LookupOptions.MustNotBeInstance, Nothing)
            Assert.Equal(LookupResultKind.MustNotBeInstance, lr.Kind)
            Assert.True(lr.HasDiagnostic) 'error BC30469: Reference to a non-shared member requires an object reference.

            lr = New LookupResult()
            context.LookupMember(lr, classA, "FooInstance", 0, LookupOptions.MustBeInstance, Nothing)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(fooInstance, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            lr = New LookupResult()
            context.LookupMember(lr, classA, "FooShared", 0, LookupOptions.MustBeInstance, Nothing)
            Assert.Equal(LookupResultKind.MustBeInstance, lr.Kind)
            Assert.False(lr.HasDiagnostic)


            Dim interfaceI = DirectCast(globalNS.GetMembers("I").Single(), NamedTypeSymbol)

            Dim ifooInstance = DirectCast(interfaceI.GetMembers("FooInstance").Single(), MethodSymbol)
            lr = New LookupResult()
            context.LookupMember(lr, interfaceI, "FooInstance", 0, LookupOptions.MustBeInstance, Nothing)
            Assert.Equal(1, lr.Symbols.Count)
            Assert.Equal(ifooInstance, lr.Symbols.Single())
            Assert.False(lr.HasDiagnostic)

            CompilationUtils.AssertNoDeclarationDiagnostics(compilation)
        End Sub

        <Fact>
        <WorkItem(545575, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545575")>
        Public Sub Bug14079()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Option Strict On

Interface I
    Class Foo
        Shared Sub Boo()
        End Sub
    End Class
End Interface
Class D
    Sub Foo()
    End Sub
    Interface I2
        Inherits I
        Shadows Class Foo(Of T)
        End Class
        Class C
            Sub Bar()
                Foo.Boo()
            End Sub
        End Class
    End Interface
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoDiagnostics(compilation)
        End Sub

        <Fact(), WorkItem(531293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531293")>
        Public Sub Bug17900()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Bug4817">
    <file name="a.vb">
Imports Undefined
Module Program
    Event E
 Sub Main()
 End Sub
End Module

    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC40056: Namespace or type specified in the Imports 'Undefined' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports Undefined
        ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Windows.Forms

Module Module1
    Sub Main()
        Dim x As ComponentModel.INotifyPropertyChanged =  Nothing 'BIND1:"ComponentModel"
    End Sub
End Module
    </file>
</compilation>, {SystemWindowsFormsRef})

            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Dim info = semanticModel.GetSymbolInfo(node)

            Dim ns = DirectCast(info.Symbol, NamespaceSymbol)

            Assert.Equal(NamespaceKind.Compilation, ns.NamespaceKind)
            Assert.Equal("System.ComponentModel", ns.ToTestDisplayString())

            Assert.Equal({"System.ComponentModel", "System.Windows.Forms.ComponentModel"},
                         semanticModel.LookupNamespacesAndTypes(node.Position, name:="ComponentModel").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

            Assert.Equal({"System.ComponentModel", "System.Windows.Forms.ComponentModel"},
                         semanticModel.LookupSymbols(node.Position, name:="ComponentModel").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports Windows.Foundation

Module Module1
    Sub Main()
        Diagnostics.Debug.WriteLine("") 'BIND1:"Diagnostics"

        Dim x = Diagnostics

        Diagnostics
    End Sub
End Module
    </file>
</compilation>, WinRtRefs)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30112: 'Diagnostics' is a namespace and cannot be used as an expression.
        Dim x = Diagnostics
                ~~~~~~~~~~~
BC30112: 'Diagnostics' is a namespace and cannot be used as an expression.
        Diagnostics
        ~~~~~~~~~~~
                                                                 </expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim node As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Dim info = semanticModel.GetSymbolInfo(node)

            Dim ns = DirectCast(info.Symbol, NamespaceSymbol)

            Assert.Equal(NamespaceKind.Compilation, ns.NamespaceKind)
            Assert.Equal("System.Diagnostics", ns.ToTestDisplayString())

            Assert.Equal({"System.Diagnostics", "Windows.Foundation.Diagnostics"},
                         semanticModel.LookupNamespacesAndTypes(node.Position, name:="Diagnostics").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

            Assert.Equal({"System.Diagnostics", "Windows.Foundation.Diagnostics"},
                         semanticModel.LookupSymbols(node.Position, name:="Diagnostics").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Imports NS1
Imports NS2

Module Module1
    Sub Main()
        NS3.            'BIND1:"NS3"
            NS4.T1.M1() 'BIND2:"NS4"
    End Sub
End Module

Namespace NS1
    Namespace NS3
        Namespace NS4
            Class T1
                Shared Sub M1()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS3
        Namespace NS4
            Class T2
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Dim info2 = semanticModel.GetSymbolInfo(node2)

            Dim ns2 = DirectCast(info2.Symbol, NamespaceSymbol)

            Assert.Equal(NamespaceKind.Module, ns2.NamespaceKind)
            Assert.Equal("NS1.NS3.NS4", ns2.ToTestDisplayString())

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Dim info1 = semanticModel.GetSymbolInfo(node1)

            Dim ns1 = DirectCast(info1.Symbol, NamespaceSymbol)

            Assert.Equal(NamespaceKind.Module, ns1.NamespaceKind)
            Assert.Equal("NS1.NS3", ns1.ToTestDisplayString())

            Assert.Equal({"NS1.NS3", "NS2.NS3"},
                         semanticModel.LookupNamespacesAndTypes(node1.Position, name:="NS3").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

            Assert.Equal({"NS1.NS3", "NS2.NS3"},
                         semanticModel.LookupSymbols(node1.Position, name:="NS3").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1
Imports NS2
Imports NS3
Imports NS4
Imports NS5
Imports NS9

Module Module1
    Sub Main()
        Dim x = GetType(NS6. 'BIND1:"NS6"
                        NS7. 'BIND2:"NS7"
                        T1)  'BIND3:"T1"
    End Sub

    Class Test1
        Inherits NS6. 'BIND4:"NS6"
                    NS7. 'BIND5:"NS7"
                        T1 'BIND6:"T1"
    End Class

    Sub Main2()
        Dim x = NS6. 'BIND7:"NS6"
                NS7. 'BIND8:"NS7"
                T1.  'BIND9:"T1"
                M1()
    End Sub

    Sub Main3()
        Dim x = GetType(NS6) 'BIND10:"NS6"
        Dim y = GetType(NS6. 'BIND11:"NS6"
                        NS7) 'BIND12:"NS7"
    End Sub

    Class Test2
        Inherits NS6 'BIND13:"NS6"
    End Class

    Class Test3
        Inherits NS6. 'BIND14:"NS6"
                    NS7 'BIND15:"NS7"
    End Class

    Sub Main4()
        NS6 'BIND16:"NS6"

        NS6. 'BIND17:"NS6"
                NS7 'BIND18:"NS7"
    End Sub
    
    <NS6> 'BIND19:"NS6"
    <NS6. 'BIND20:"NS6"
        NS7> 'BIND21:"NS7"
    <NS6. 'BIND22:"NS6"
        NS7. 'BIND23:"NS7"
            T1> 'BIND24:"T1"
    Class Test4
    End Class
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Class T1
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Class T1
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Class T1
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS4
    Namespace NS6
        Namespace NS7
            Namespace T1
                Class T2
                End Class
            End Namespace
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Class T1
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                                    <expected><![CDATA[
BC37229: 'T1' is ambiguous between declarations in namespaces 'NS1.NS6.NS7, NS2.NS6.NS7, NS4.NS6.NS7, NS5.NS6.NS7'.
        Dim x = GetType(NS6. 'BIND1:"NS6"
                        ~~~~~~~~~~~~~~~~~~
BC37229: 'T1' is ambiguous between declarations in namespaces 'NS1.NS6.NS7, NS2.NS6.NS7, NS4.NS6.NS7, NS5.NS6.NS7'.
        Inherits NS6. 'BIND4:"NS6"
                 ~~~~~~~~~~~~~~~~~~
BC37229: 'T1' is ambiguous between declarations in namespaces 'NS1.NS6.NS7, NS2.NS6.NS7, NS4.NS6.NS7, NS5.NS6.NS7'.
        Dim x = NS6. 'BIND7:"NS6"
                ~~~~~~~~~~~~~~~~~~
BC30182: Type expected.
        Dim x = GetType(NS6) 'BIND10:"NS6"
                        ~~~
BC30182: Type expected.
        Dim y = GetType(NS6. 'BIND11:"NS6"
                        ~~~~~~~~~~~~~~~~~~~
BC30182: Type expected.
        Inherits NS6 'BIND13:"NS6"
                 ~~~
BC30182: Type expected.
        Inherits NS6. 'BIND14:"NS6"
                 ~~~~~~~~~~~~~~~~~~~
BC30112: 'NS6' is a namespace and cannot be used as an expression.
        NS6 'BIND16:"NS6"
        ~~~
BC30112: 'NS6.NS7' is a namespace and cannot be used as an expression.
        NS6. 'BIND17:"NS6"
        ~~~~~~~~~~~~~~~~~~~
BC30182: Type expected.
    <NS6> 'BIND19:"NS6"
     ~~~
BC30182: Type expected.
    <NS6. 'BIND20:"NS6"
     ~~~~~~~~~~~~~~~~~~~
BC37229: 'T1' is ambiguous between declarations in namespaces 'NS1.NS6.NS7, NS2.NS6.NS7, NS4.NS6.NS7, NS5.NS6.NS7'.
    <NS6. 'BIND22:"NS6"
     ~~~~~~~~~~~~~~~~~~~
                                                                ]]></expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim nodes(24) As IdentifierNameSyntax

            For i As Integer = 1 To nodes.Length - 1
                nodes(i) = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", i)
            Next

            For Each i In {3, 6, 9, 24}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))

                Assert.Null(info3.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info3.CandidateReason)
                ' Content of this list should determine content of lists below !!!
                Assert.Equal({"NS1.NS6.NS7.T1", "NS2.NS6.NS7.T1", "NS4.NS6.NS7.T1", "NS5.NS6.NS7.T1"}, info3.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {2, 5, 8, 23}
                Dim info2 = semanticModel.GetSymbolInfo(nodes(i))

                Assert.Null(info2.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info2.CandidateReason)
                Assert.Equal({"NS1.NS6.NS7", "NS2.NS6.NS7", "NS4.NS6.NS7", "NS5.NS6.NS7"}, info2.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {1, 4, 7, 22}
                Dim info1 = semanticModel.GetSymbolInfo(nodes(i))

                Assert.Null(info1.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info1.CandidateReason)
                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS4.NS6", "NS5.NS6"}, info1.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS4.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupNamespacesAndTypes(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS4.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupSymbols(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {10, 13, 16, 19}
                Dim info2 = semanticModel.GetSymbolInfo(nodes(i))

                Assert.Null(info2.Symbol)
                Assert.Equal(If(i = 16, CandidateReason.Ambiguous, If(i = 19, CandidateReason.NotAnAttributeType, CandidateReason.NotATypeOrNamespace)), info2.CandidateReason)
                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS4.NS6", "NS5.NS6", "NS9.NS6"}, info2.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {12, 15, 18, 21}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))

                Assert.Null(info3.Symbol)
                Assert.Equal(If(i = 18, CandidateReason.Ambiguous, If(i = 21, CandidateReason.NotAnAttributeType, CandidateReason.NotATypeOrNamespace)), info3.CandidateReason)
                ' Content of this list should determine content of lists below !!!
                Assert.Equal({"NS1.NS6.NS7", "NS2.NS6.NS7", "NS4.NS6.NS7", "NS5.NS6.NS7", "NS9.NS6.NS7"}, info3.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {11, 14, 17, 20}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))

                Assert.Null(info3.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info3.CandidateReason)
                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS4.NS6", "NS5.NS6", "NS9.NS6"}, info3.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_05()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1
Imports NS2
Imports NS3
Imports NS4
Imports NS5
Imports NS9

Module Module1
    Sub Main()
        Dim x = GetType(NS6. 'BIND1:"NS6"
                        NS7. 'BIND2:"NS7"
                        T1)  'BIND3:"T1"
    End Sub

    Class Test1
        Inherits NS6. 'BIND4:"NS6"
                    NS7. 'BIND5:"NS7"
                        T1 'BIND6:"T1"
    End Class

    Sub Main2()
        NS6. 'BIND7:"NS6"
                NS7. 'BIND8:"NS7"
                T1.  'BIND9:"T1"
                M1()
    End Sub

    <NS6. 'BIND10:"NS6"
                    NS7. 'BIND11:"NS7"
                        T1> 'BIND12:"T1"
    Class Test2
    End Class
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Class T1
                Inherits System.Attribute
                Shared Sub M1()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Class T2
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Class T1
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS4
    Namespace NS6
        Namespace NS7
            Namespace T4
                Class T2
                End Class
            End Namespace
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim nodes(12) As IdentifierNameSyntax

            For i As Integer = 1 To nodes.Length - 1
                nodes(i) = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", i)
            Next

            For Each i In {3, 6, 9}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Equal("NS1.NS6.NS7.T1", info3.Symbol.ToTestDisplayString())
            Next

            Dim info12 = semanticModel.GetSymbolInfo(nodes(12))
            Assert.Equal("Sub NS1.NS6.NS7.T1..ctor()", info12.Symbol.ToTestDisplayString())

            For Each i In {2, 5, 8, 11}
                Dim info2 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Equal("NS1.NS6.NS7", info2.Symbol.ToTestDisplayString())
                Assert.Equal(NamespaceKind.Module, DirectCast(info2.Symbol, NamespaceSymbol).NamespaceKind)
            Next

            For Each i In {1, 4, 7, 10}
                Dim info1 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Equal("NS1.NS6", info1.Symbol.ToTestDisplayString())
                Assert.Equal(NamespaceKind.Module, DirectCast(info1.Symbol, NamespaceSymbol).NamespaceKind)


                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS4.NS6", "NS5.NS6", "NS9.NS6"},
                             semanticModel.LookupNamespacesAndTypes(nodes(i).Position, name:="NS6").AsEnumerable().
                                Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS4.NS6", "NS5.NS6", "NS9.NS6"},
                             semanticModel.LookupSymbols(nodes(i).Position, name:="NS6").AsEnumerable().
                                Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_06()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1
Imports NS2
Imports NS3
Imports NS5
Imports NS9

Module Module1
    Sub Main()
        Dim x = NS6. 'BIND1:"NS6"
                NS7. 'BIND2:"NS7"
                M1()  'BIND3:"M1"

        Dim y = GetType(NS6. 'BIND4:"NS6"
                            NS7. 'BIND5:"NS7"
                            M1)  'BIND6:"M1"
    End Sub

    <NS6. 'BIND7:"NS6"
                NS7. 'BIND8:"NS7"
                M1>  'BIND9:"M1"
    Class Test1
        Inherits NS6. 'BIND10:"NS6"
                NS7. 'BIND11:"NS7"
                M1  'BIND12:"M1"
    End Class
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Module T1
                Sub M1()
                End Sub
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Module T1
                Sub M1()
                End Sub
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Module T1
                Sub M1()
                End Sub
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Module T1
                Sub M1()
                End Sub
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                                    <expected><![CDATA[
BC30562: 'M1' is ambiguous between declarations in Modules 'NS1.NS6.NS7.T1, NS2.NS6.NS7.T1, NS5.NS6.NS7.T1'.
        Dim x = NS6. 'BIND1:"NS6"
                ~~~~~~~~~~~~~~~~~~
BC30002: Type 'NS6.NS7.M1' is not defined.
        Dim y = GetType(NS6. 'BIND4:"NS6"
                        ~~~~~~~~~~~~~~~~~~
BC30002: Type 'NS6.NS7.M1' is not defined.
    <NS6. 'BIND7:"NS6"
     ~~~~~~~~~~~~~~~~~~
BC30002: Type 'NS6.NS7.M1' is not defined.
        Inherits NS6. 'BIND10:"NS6"
                 ~~~~~~~~~~~~~~~~~~~
                                                    ]]></expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim nodes(12) As IdentifierNameSyntax

            For i As Integer = 1 To nodes.Length - 1
                nodes(i) = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", i)
            Next

            For Each i In {3, 6, 9, 12}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Null(info3.Symbol)
                Assert.Equal(If(i = 3, CandidateReason.Ambiguous, CandidateReason.NotATypeOrNamespace), info3.CandidateReason)
                ' Content of this list should determine content of lists below !!!
                Assert.Equal({"Sub NS1.NS6.NS7.T1.M1()", "Sub NS2.NS6.NS7.T1.M1()", "Sub NS5.NS6.NS7.T1.M1()"}, info3.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {2, 5, 8, 11}
                Dim info2 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Null(info2.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info2.CandidateReason)
                Assert.Equal({"NS1.NS6.NS7", "NS2.NS6.NS7", "NS5.NS6.NS7"}, info2.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {1, 4, 7, 10}
                Dim info1 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Null(info1.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info1.CandidateReason)
                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS5.NS6"}, info1.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupNamespacesAndTypes(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupSymbols(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_07()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Imports NS1
Imports NS2
Imports NS3
Imports NS5
Imports NS9

Module Module1
    Sub Main()
        Dim x = NS6. 'BIND1:"NS6"
                NS7. 'BIND2:"NS7"
                M1()  'BIND3:"M1"
    End Sub
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Module T1
                Sub M1(x as Integer)
                End Sub
                Sub M1(x as Long)
                End Sub
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Module T1
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Module T1
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Module T1
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                                    <expected>
BC30516: Overload resolution failed because no accessible 'M1' accepts this number of arguments.
                M1()  'BIND3:"M1"
                ~~
                                                    </expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)

            Dim info3 = semanticModel.GetSymbolInfo(node3)

            Assert.Null(info3.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info3.CandidateReason)
            Assert.Equal({"Sub NS1.NS6.NS7.T1.M1(x As System.Int32)", "Sub NS1.NS6.NS7.T1.M1(x As System.Int64)"}, info3.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Dim info2 = semanticModel.GetSymbolInfo(node2)
            Assert.Equal("NS1.NS6.NS7", info2.Symbol.ToTestDisplayString())
            Assert.Equal(NamespaceKind.Module, DirectCast(info2.Symbol, NamespaceSymbol).NamespaceKind)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Dim info1 = semanticModel.GetSymbolInfo(node1)
            Assert.Equal("NS1.NS6", info1.Symbol.ToTestDisplayString())
            Assert.Equal(NamespaceKind.Module, DirectCast(info1.Symbol, NamespaceSymbol).NamespaceKind)

            Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupNamespacesAndTypes(node1.Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

            Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupSymbols(node1.Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_08()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Imports NS1
Imports NS2
Imports NS3
Imports NS5
Imports NS9

Module Module1
    Sub Main()
        NS6. 'BIND1:"NS6"
                NS7. 'BIND2:"NS7"
                M1()  'BIND3:"M1"
    End Sub
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Module T1
                Sub M1()
                End Sub
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Module T1
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Module T1
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Module T1
            End Module
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)

            Dim info3 = semanticModel.GetSymbolInfo(node3)

            Assert.Equal("Sub NS1.NS6.NS7.T1.M1()", info3.Symbol.ToTestDisplayString())

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Dim info2 = semanticModel.GetSymbolInfo(node2)
            Assert.Equal("NS1.NS6.NS7", info2.Symbol.ToTestDisplayString())
            Assert.Equal(NamespaceKind.Module, DirectCast(info2.Symbol, NamespaceSymbol).NamespaceKind)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Dim info1 = semanticModel.GetSymbolInfo(node1)
            Assert.Equal("NS1.NS6", info1.Symbol.ToTestDisplayString())
            Assert.Equal(NamespaceKind.Module, DirectCast(info1.Symbol, NamespaceSymbol).NamespaceKind)

            Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupNamespacesAndTypes(node1.Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

            Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupSymbols(node1.Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_09()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1
Imports NS2
Imports NS3
Imports NS5
Imports NS9

Module Module1
    Class Test1
        Inherits NS6. 'BIND1:"NS6"
                    NS7. 'BIND2:"NS7"
                        T1 'BIND3:"T1"
    End Class

    Sub Main()
        NS6. 'BIND4:"NS6"
                    NS7. 'BIND5:"NS7"
                        T1 'BIND6:"T1"

        Dim x = GetType(NS6. 'BIND7:"NS6"
                        NS7. 'BIND8:"NS7"
                        T1) 'BIND9:"T1"
    End Sub

    <NS6. 'BIND10:"NS6"
                    NS7. 'BIND11:"NS7"
                        T1> 'BIND12:"T1"
    Class Test2
    End Class
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Module Module1
                Class T1
                End Class
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Module Module1
                Class T1
                End Class
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Module Module1
                Class T1
                End Class
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Module Module1
                Class T1
                End Class
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                                    <expected><![CDATA[
BC30562: 'T1' is ambiguous between declarations in Modules 'NS1.NS6.NS7.Module1, NS2.NS6.NS7.Module1, NS5.NS6.NS7.Module1'.
        Inherits NS6. 'BIND1:"NS6"
                 ~~~~~~~~~~~~~~~~~~
BC30562: 'T1' is ambiguous between declarations in Modules 'NS1.NS6.NS7.Module1, NS2.NS6.NS7.Module1, NS5.NS6.NS7.Module1'.
        NS6. 'BIND4:"NS6"
        ~~~~~~~~~~~~~~~~~~
BC30562: 'T1' is ambiguous between declarations in Modules 'NS1.NS6.NS7.Module1, NS2.NS6.NS7.Module1, NS5.NS6.NS7.Module1'.
        Dim x = GetType(NS6. 'BIND7:"NS6"
                        ~~~~~~~~~~~~~~~~~~
BC30562: 'T1' is ambiguous between declarations in Modules 'NS1.NS6.NS7.Module1, NS2.NS6.NS7.Module1, NS5.NS6.NS7.Module1'.
    <NS6. 'BIND10:"NS6"
     ~~~~~~~~~~~~~~~~~~~
                                                    ]]></expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim nodes(12) As IdentifierNameSyntax

            For i As Integer = 1 To nodes.Length - 1
                nodes(i) = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", i)
            Next

            For Each i In {3, 6, 9, 12}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Null(info3.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info3.CandidateReason)
                ' Content of this list should determine content of lists below !!!
                Assert.Equal({"NS1.NS6.NS7.Module1.T1", "NS2.NS6.NS7.Module1.T1", "NS5.NS6.NS7.Module1.T1"}, info3.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {2, 5, 8, 11}
                Dim info2 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Null(info2.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info2.CandidateReason)
                Assert.Equal({"NS1.NS6.NS7", "NS2.NS6.NS7", "NS5.NS6.NS7"}, info2.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next

            For Each i In {1, 4, 7, 10}
                Dim info1 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Null(info1.Symbol)
                Assert.Equal(CandidateReason.Ambiguous, info1.CandidateReason)
                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS5.NS6"}, info1.CandidateSymbols.AsEnumerable().Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupNamespacesAndTypes(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupSymbols(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next
        End Sub

        <Fact()>
        Public Sub AmbiguousNamespaces_10()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1
Imports NS2
Imports NS3
Imports NS5
Imports NS9

Module Module1
    Class Test1
        Inherits NS6. 'BIND1:"NS6"
                    NS7. 'BIND2:"NS7"
                        T1 'BIND3:"T1"
    End Class

    Sub Main()
        NS6. 'BIND4:"NS6"
                    NS7. 'BIND5:"NS7"
                        T1 'BIND6:"T1"

        Dim x = GetType(NS6. 'BIND7:"NS6"
                        NS7. 'BIND8:"NS7"
                        T1) 'BIND9:"T1"
    End Sub

    <NS6. 'BIND10:"NS6"
                    NS7. 'BIND11:"NS7"
                        T1> 'BIND12:"T1"
    Class Test2
    End Class
End Module


Namespace NS1
    Namespace NS6
        Namespace NS7
            Module Module1
                Class T1
                    Inherits System.Attribute
                End Class
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS2
    Namespace NS6
        Namespace NS7
            Module Module1
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS3
    Namespace NS6
        Namespace NS8
            Module Module1
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS5
    Namespace NS6
        Namespace NS7
            Module Module1
            End Module 
        End Namespace
    End Namespace
End Namespace

Namespace NS9
    Namespace NS6
        Namespace NS7
            Class T3
            End Class
        End Namespace
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                                    <expected><![CDATA[
BC30109: 'Module1.T1' is a class type and cannot be used as an expression.
        NS6. 'BIND4:"NS6"
        ~~~~~~~~~~~~~~~~~~
                                                    ]]></expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = DirectCast(compilation.GetSemanticModel(tree), VBSemanticModel)

            Dim nodes(12) As IdentifierNameSyntax

            For i As Integer = 1 To nodes.Length - 1
                nodes(i) = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", i)
            Next

            For Each i In {3, 6, 9}
                Dim info3 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Equal("NS1.NS6.NS7.Module1.T1", info3.Symbol.ToTestDisplayString())
            Next

            Dim info12 = semanticModel.GetSymbolInfo(nodes(12))
            Assert.Equal("Sub NS1.NS6.NS7.Module1.T1..ctor()", info12.Symbol.ToTestDisplayString())

            For Each i In {2, 5, 8, 11}
                Dim info2 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Equal("NS1.NS6.NS7", info2.Symbol.ToTestDisplayString())
                Assert.Equal(NamespaceKind.Module, DirectCast(info2.Symbol, NamespaceSymbol).NamespaceKind)
            Next

            For Each i In {1, 4, 7, 10}
                Dim info1 = semanticModel.GetSymbolInfo(nodes(i))
                Assert.Equal("NS1.NS6", info1.Symbol.ToTestDisplayString())
                Assert.Equal(NamespaceKind.Module, DirectCast(info1.Symbol, NamespaceSymbol).NamespaceKind)

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupNamespacesAndTypes(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())

                Assert.Equal({"NS1.NS6", "NS2.NS6", "NS3.NS6", "NS5.NS6", "NS9.NS6"},
                         semanticModel.LookupSymbols(nodes(i).Position, name:="NS6").AsEnumerable().
                            Select(Function(s) s.ToTestDisplayString()).OrderBy(Function(s) s).ToArray())
            Next
        End Sub

        <Fact()> <WorkItem(842056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/842056")>
        Public Sub AmbiguousNamespaces_11()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports A
Imports B
 
Namespace A.X
    Class C
    End Class
End Namespace
 
Namespace B.X
    Class C
    End Class
End Namespace
 
Module M
    Dim c As X.C
End Module
    ]]></file>
</compilation>, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation,
                                                    <expected><![CDATA[
BC37229: 'C' is ambiguous between declarations in namespaces 'A.X, B.X'.
    Dim c As X.C
             ~~~
                                                    ]]></expected>)
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants01()
            Dim csCompilation = CreateCSharpCompilation("CSEnum",
            <![CDATA[
public enum Color
{
Red,
Green,
DateTime,
[System.Obsolete] Datetime = DateTime,
Blue,
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.DateTime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            vbCompilation.VerifyDiagnostics() ' no obsolete diagnostic - we select the first one of the given name
            CompileAndVerify(vbCompilation, expectedOutput:="2")
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants01b()
            Dim csCompilation = CreateCSharpCompilation("CSEnum",
            <![CDATA[
public enum Color
{
Red,
Green,
DateTime,
[System.Obsolete] Datetime = DateTime,
DATETIME = DateTime,
Blue,
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.Datetime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            vbCompilation.VerifyDiagnostics() ' no obsolete diagnostic - we select the first one of the given name
            CompileAndVerify(vbCompilation, expectedOutput:="2")
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants02()
            Dim csCompilation = CreateCSharpCompilation("CSEnum",
            <![CDATA[
public enum Color
{
Red,
Green,
DateTime,
[System.Obsolete] Datetime,
Blue,
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.DateTime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            CompilationUtils.AssertTheseDiagnostics(vbCompilation,
                                                    <expected><![CDATA[
BC31429: 'DateTime' is ambiguous because multiple kinds of members with this name exist in enum 'Color'.
        System.Console.WriteLine(CInt(Color.DateTime))
                                      ~~~~~~~~~~~~~~
                                                    ]]></expected>)
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants02b()
            Dim csCompilation = CreateCSharpCompilation("CSEnum",
            <![CDATA[
public enum Color
{
Red,
Green,
DateTime,
[System.Obsolete] Datetime,
DATETIME,
Blue,
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.DateTime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            CompilationUtils.AssertTheseDiagnostics(vbCompilation,
                                                    <expected><![CDATA[
BC31429: 'DateTime' is ambiguous because multiple kinds of members with this name exist in enum 'Color'.
        System.Console.WriteLine(CInt(Color.DateTime))
                                      ~~~~~~~~~~~~~~
                                                    ]]></expected>)
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants02c()
            Dim csCompilation = CreateCSharpCompilation("CSEnum",
            <![CDATA[
public enum Color
{
Red,
Green,
DateTime,
[System.Obsolete] Datetime = DateTime,
[System.Obsolete] DATETIME,
Blue,
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.DateTime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            CompilationUtils.AssertTheseDiagnostics(vbCompilation,
                                                    <expected><![CDATA[
BC31429: 'DateTime' is ambiguous because multiple kinds of members with this name exist in enum 'Color'.
        System.Console.WriteLine(CInt(Color.DateTime))
                                      ~~~~~~~~~~~~~~
                                                    ]]></expected>)
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants02d()
            Dim vbCompilation1 = CreateVisualBasicCompilation("VBEnum",
            <![CDATA[
Public Enum Color
    Red
    Green
    DateTime
    <System.Obsolete> Datetime = DateTime
    DATETIME
    Blue
End Enum
]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            CompilationUtils.AssertTheseDiagnostics(vbCompilation1,
                                                    <expected><![CDATA[
BC31421: 'Datetime' is already declared in this enum.
    <System.Obsolete> Datetime = DateTime
                      ~~~~~~~~
BC31429: 'DateTime' is ambiguous because multiple kinds of members with this name exist in enum 'Color'.
    <System.Obsolete> Datetime = DateTime
                                 ~~~~~~~~
BC31421: 'DATETIME' is already declared in this enum.
    DATETIME
    ~~~~~~~~
                                                    ]]></expected>)
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.DateTime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedAssemblies:={New VisualBasicCompilationReference(vbCompilation1), MscorlibRef, MsvbRef})
            CompilationUtils.AssertTheseDiagnostics(vbCompilation,
                                                    <expected><![CDATA[
BC31429: 'DateTime' is ambiguous because multiple kinds of members with this name exist in enum 'Color'.
        System.Console.WriteLine(CInt(Color.DateTime))
                                      ~~~~~~~~~~~~~~
                                                    ]]></expected>)
        End Sub

        <Fact, WorkItem(2909, "https://github.com/dotnet/roslyn/issues/2909")>
        Public Sub AmbiguousEnumConstants02e()
            Dim vbCompilation1 = CreateVisualBasicCompilation("VBEnum",
            <![CDATA[
Public Enum Color
    Red
    Green
    DateTime
    <System.Obsolete> Datetime = 2
    Blue
End Enum
]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            CompilationUtils.AssertTheseDiagnostics(vbCompilation1,
                                                    <expected><![CDATA[
BC31421: 'Datetime' is already declared in this enum.
    <System.Obsolete> Datetime = 2
                      ~~~~~~~~
                                                    ]]></expected>)
            Dim vbCompilation = CreateVisualBasicCompilation("VBEnumClient",
            <![CDATA[
Public Module Program
    Sub Main()
        System.Console.WriteLine(CInt(Color.DateTime))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedAssemblies:={New VisualBasicCompilationReference(vbCompilation1), MscorlibRef, MsvbRef})
            CompilationUtils.AssertTheseDiagnostics(vbCompilation,
                                                    <expected><![CDATA[
                                                    ]]></expected>)
            CompileAndVerify(vbCompilation, expectedOutput:="2")
        End Sub

    End Class
End Namespace
