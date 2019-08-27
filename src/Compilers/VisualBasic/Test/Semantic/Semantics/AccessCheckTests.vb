' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class AccessCheckTests
        Inherits BasicTestBase

        ' Very simple test, just to make sure access checking works.
        <Fact>
        Public Sub SimpleAccess()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="SimpleAccess">
    <file name="a.vb">
Imports System.Collections.Generic
Class A
    Shared Protected prot As Integer
End Class

Class B
    Public Sub goo()
        dim i as Integer
        i = A.prot
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC30389: 'A.prot' is not accessible in this context because it is 'Protected'.
        i = A.prot
            ~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub AccessCheckOutsideToInner()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="OutsideToInner">
    <file name="a.vb">

Class C
    Public Shared c_pub As Integer
    Friend Shared c_int As Integer
    Protected Shared c_pro As Integer
    Protected Friend Shared c_intpro As Integer
    Private Shared c_priv As Integer

    Sub m()
        C.c_pub = 1
        C.c_int = 1
        C.c_pro = 1
        C.c_intpro = 1
        C.c_priv = 1
        N1.n1_pub = 1
        N1.n1_int = 1
        N1.n1_pro = 1
        N1.n1_intpro = 1
        N1.n1_priv = 1
        N1.N2.n2_pub = 1
        N1.N2.n2_int = 1
        N1.N2.n2_pro = 1
        N1.N2.n2_intpro = 1
        N1.N2.n2_priv = 1
        N1.N3.n3_pub = 1
        N1.N4.n4_pub = 1
        N1.N5.n5_pub = 1
        N1.N6.n6_pub = 1
    End Sub

    Private Class N1
        Public Shared n1_pub As Integer
        Friend Shared n1_int As Integer
        Protected Shared n1_pro As Integer
        Protected Friend Shared n1_intpro As Integer
        Private Shared n1_priv As Integer

        Public Class N2
            Public Shared n2_pub As Integer
            Friend Shared n2_int As Integer
            Protected Shared n2_pro As Integer
            Protected Friend Shared n2_intpro As Integer
            Private Shared n2_priv As Integer
        End Class

        Private Class N3
            Public Shared n3_pub As Integer
        End Class

        Protected Class N4
            Public Shared n4_pub As Integer
        End Class

        Friend Class N5
            Public Shared n5_pub As Integer
        End Class

        Protected Friend Class N6
            Public Shared n6_pub As Integer
        End Class
    End Class
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC30389: 'C.N1.n1_pro' is not accessible in this context because it is 'Protected'.
        N1.n1_pro = 1
        ~~~~~~~~~
BC30389: 'C.N1.n1_priv' is not accessible in this context because it is 'Private'.
        N1.n1_priv = 1
        ~~~~~~~~~~
BC30389: 'C.N1.N2.n2_pro' is not accessible in this context because it is 'Protected'.
        N1.N2.n2_pro = 1
        ~~~~~~~~~~~~
BC30389: 'C.N1.N2.n2_priv' is not accessible in this context because it is 'Private'.
        N1.N2.n2_priv = 1
        ~~~~~~~~~~~~~
BC30389: 'C.N1.N3' is not accessible in this context because it is 'Private'.
        N1.N3.n3_pub = 1
        ~~~~~
BC30389: 'C.N1.N4' is not accessible in this context because it is 'Protected'.
        N1.N4.n4_pub = 1
        ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub AccessCheckInnerToOuter()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="AccessCheckInnerToOuter">
    <file name="a.vb">
Class C
    Public Shared c_pub As Integer
    Friend Shared c_int As Integer
    Protected Shared c_pro As Integer
    Protected Friend Shared c_intpro As Integer
    Private Shared c_priv As Integer

    Private Class N1
        Public Shared n1_pub As Integer
        Friend Shared n1_int As Integer
        Protected Shared n1_pro As Integer
        Protected Friend Shared n1_intpro As Integer
        Private Shared n1_priv As Integer

        Public Class N2
            Public Shared n2_pub As Integer
            Friend Shared n2_int As Integer
            Protected Shared n2_pro As Integer
            Protected Friend Shared n2_intpro As Integer
            Private Shared n2_priv As Integer

            Sub m()
                c_pub = 1
                c_int = 1
                c_pro = 1
                c_intpro = 1
                c_priv = 1
                n1_pub = 1
                n1_int = 1
                n1_pro = 1
                n1_intpro = 1
                n1_priv = 1
                n2_pub = 1
                n2_int = 1
                n2_pro = 1
                n2_intpro = 1
                n2_priv = 1
                N3.n3_pub = 1
                N4.n4_pub = 1
                N5.n5_pub = 1
                N6.n6_pub = 1
            End Sub
        End Class

        Private Class N3
            Public Shared n3_pub As Integer
        End Class

        Protected Class N4
            Public Shared n4_pub As Integer
        End Class

        Friend Class N5
            Public Shared n5_pub As Integer
        End Class

        Protected Friend Class N6
            Public Shared n6_pub As Integer
        End Class
    End Class
End Class    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(c)
        End Sub

        <Fact>
        Public Sub AccessCheckDerived()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="AccessCheckDerived">
    <file name="a.vb">
Class C
    Public Shared c_pub As Integer
    Friend Shared c_int As Integer
    Protected Shared c_pro As Integer
    Protected Friend Shared c_intpro As Integer
    Private Shared c_priv As Integer

    Protected Class N3
        Public Shared n3_pub As Integer
        Friend Shared n3_int As Integer
        Protected Shared n3_pro As Integer
        Protected Friend Shared n3_intpro As Integer
        Private Shared n3_priv As Integer

    End Class

    Private Class N4
        Public Shared n4_pub As Integer
    End Class

    Friend Class N5
        Public Shared n5_pub As Integer
    End Class

    Protected Friend Class N6
        Public Shared n6_pub As Integer
    End Class
End Class

Class D
    Inherits C
    Public Shared n1_pub As Integer
    Friend Shared n1_int As Integer
    Protected Shared n1_pro As Integer
    Protected Friend Shared n1_intpro As Integer
    Private Shared n1_priv As Integer
End Class

Class E

    Inherits D
    Public Shared n2_pub As Integer
    Friend Shared n2_int As Integer
    Protected Shared n2_pro As Integer
    Protected Friend Shared n2_intpro As Integer
    Private Shared n2_priv As Integer

    Sub m()
        c_pub = 1
        c_int = 1
        c_pro = 1
        c_intpro = 1
        c_priv = 1
        n1_pub = 1
        n1_int = 1
        n1_pro = 1
        n1_intpro = 1
        n1_priv = 1
        n2_pub = 1
        n2_int = 1
        n2_pro = 1
        n2_intpro = 1
        n2_priv = 1
        N3.n3_pub = 1
        N3.n3_int = 1
        N3.n3_pro = 1
        N3.n3_intpro = 1
        N3.n3_priv = 1
        N4.n4_pub = 1
        N5.n5_pub = 1
        N6.n6_pub = 1
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC30389: 'C.c_priv' is not accessible in this context because it is 'Private'.
        c_priv = 1
        ~~~~~~
BC30389: 'D.n1_priv' is not accessible in this context because it is 'Private'.
        n1_priv = 1
        ~~~~~~~
BC30389: 'C.N3.n3_pro' is not accessible in this context because it is 'Protected'.
        N3.n3_pro = 1
        ~~~~~~~~~
BC30389: 'C.N3.n3_priv' is not accessible in this context because it is 'Private'.
        N3.n3_priv = 1
        ~~~~~~~~~~
BC30389: 'C.N4' is not accessible in this context because it is 'Private'.
        N4.n4_pub = 1
        ~~
</expected>)

        End Sub

        <Fact>
        Public Sub AccessCheckProtected()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="AccessCheckProtected">
    <file name="a.vb">
Public Class A
    Protected iField As Integer
    Protected Shared sField As Integer

End Class

Public Class B
    Inherits A

    Public Class N
        Inherits A

        Public Class NN
            Public Sub m(bb As B, cc As C, dd As D, ee As E)
                Dim x As Integer = bb.iField
                Dim y As Integer = B.sField
                Dim z As Integer = cc.iField
                Dim w As Integer = C.sField
                Dim q As Integer = dd.iField
                Dim r As Integer = D.sField
                Dim s As Integer = ee.iField
                Dim t As Integer = E.sField
            End Sub
        End Class

        Public Sub m(bb As B, cc As C, dd As D, ee As E)
            Dim u As Integer = cc.iField
            Dim v As Integer = dd.iField
            Dim w As Integer = ee.iField
        End Sub
    End Class

    Public Sub m(bb As B, cc As C, dd As D, ee As E)
        Dim u1 As Integer = cc.iField
        Dim v1 As Integer = dd.iField
        Dim w1 As Integer = ee.iField
    End Sub

End Class

Public Class C
    Inherits B
End Class

Public Class D
    Inherits A
End Class

Public Class E
    Inherits B.N
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC30389: 'A.iField' is not accessible in this context because it is 'Protected'.
                Dim q As Integer = dd.iField
                                   ~~~~~~~~~
BC30389: 'A.iField' is not accessible in this context because it is 'Protected'.
            Dim v As Integer = dd.iField
                               ~~~~~~~~~
BC30389: 'A.iField' is not accessible in this context because it is 'Protected'.
        Dim v1 As Integer = dd.iField
                            ~~~~~~~~~
BC30389: 'A.iField' is not accessible in this context because it is 'Protected'.
        Dim w1 As Integer = ee.iField
                            ~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Bug4685_01()

            Dim compilationDef =
<compilation name="Bug4685">
    <file name="a.vb">
Class A
    Public Function Goo(x As Object) As String
        Return "ABC"
    End Function
 
    Protected Sub Goo(x As String)
    End Sub
End Class
 
Class B
    Inherits A

    Shared Sub Test()
        Dim x As C = New C()
        x.Bar(New D())
    End Sub

    Class C
        Sub Bar(y As D)
            Dim z As String = y.Goo("").ToLower()
            System.Console.WriteLine(z)
        End Sub
    End Class
End Class
 
Class D
    Inherits B
End Class

Module Module1
    Sub Main()
        B.Test()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
            Dim z As String = y.Goo("").ToLower()
                              ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Bug4685_02()

            Dim compilationDef =
<compilation name="Bug4685">
    <file name="a.vb">
Class A
    Public Function Goo(x As String) As String
        Return "ABC"
    End Function
 
    Protected Sub Goo(x As Object)
    End Sub
End Class
 
Class B
    Inherits A

    Shared Sub Test()
        Dim x As C = New C()
        x.Bar(New D())
    End Sub

    Class C
        Sub Bar(y As D)
            Dim z As String = y.Goo("").ToLower()
            System.Console.WriteLine(z)
        End Sub
    End Class
End Class
 
Class D
    Inherits B
End Class

Module Module1
    Sub Main()
        B.Test()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="abc")
        End Sub

        <Fact>
        Public Sub AccessCheckCrossAssembly()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="AccessCheckCrossAssembly1">
    <file name="a.vb">
Public Class C
    Public Shared c_pub As Integer
    Friend Shared c_int As Integer
    Protected Shared c_pro As Integer
    Protected Friend Shared c_intpro As Integer
    Private Shared c_priv As Integer
End Class

Friend Class D
    Public Shared d_pub As Integer
End Class
    </file>
</compilation>)

            CompilationUtils.AssertNoErrors(other)


            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssembly2">
    <file name="a.vb">
Public Class A
    Public Sub m()
        Dim aa As Integer = C.c_pub
        Dim bb As Integer = C.c_int
        Dim cc As Integer = C.c_pro
        Dim dd As Integer = C.c_intpro
        Dim ee As Integer = C.c_priv
        Dim ff As Integer = D.d_pub
    End Sub
End Class
    </file>
</compilation>,
                {New VisualBasicCompilationReference(other)})

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC30389: 'C.c_int' is not accessible in this context because it is 'Friend'.
        Dim bb As Integer = C.c_int
                            ~~~~~~~
BC30389: 'C.c_pro' is not accessible in this context because it is 'Protected'.
        Dim cc As Integer = C.c_pro
                            ~~~~~~~
BC30389: 'C.c_intpro' is not accessible in this context because it is 'Protected Friend'.
        Dim dd As Integer = C.c_intpro
                            ~~~~~~~~~~
BC30389: 'C.c_priv' is not accessible in this context because it is 'Private'.
        Dim ee As Integer = C.c_priv
                            ~~~~~~~~
BC30389: 'D' is not accessible in this context because it is 'Friend'.
        Dim ff As Integer = D.d_pub
                            ~
</expected>)
        End Sub

        <WorkItem(540036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540036")>
        <Fact>
        Public Sub AccessCheckCrossAssemblyParameterProtectedMethodP2P()
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyParameterProtectedMethod1">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AccessCheckCrossAssemblyParameterProtectedMethod2")>
Friend Class C
End Class
]]>
    </file>
</compilation>, {SystemCoreRef})

            other.VerifyDiagnostics()

            Dim c As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyParameterProtectedMethod2">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>,
                {New VisualBasicCompilationReference(other)})

            c.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub EnsureAccessCheckWithBadIVTDenies()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyParameterProtectedMethod1">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AccessCheckCrossAssemblyParameterProtectedMethod200000")>
Friend Class C
End Class
]]>
    </file>
</compilation>, {SystemCoreRef})
            CompilationUtils.AssertNoErrors(other)

            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyParameterProtectedMethod2">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>,
                {New VisualBasicCompilationReference(other)})
            CompilationUtils.AssertTheseDiagnostics(c,
<expected>                                               
BC30389: 'C' is not accessible in this context because it is 'Friend'.
        Protected Sub New(o As C)
                               ~
</expected>)
        End Sub

        <Fact>
        Public Sub AccessCheckCrossAssemblyParameterProtectedMethodMD()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyParameterProtectedMethod1">
    <file name="a.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AccessCheckCrossAssemblyParameterProtectedMethod2")>
Friend Class C
End Class
]]>
    </file>
</compilation>, {SystemCoreRef})

            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="AccessCheckCrossAssemblyParameterProtectedMethod2">
    <file name="a.vb"><![CDATA[
Public Class A
    Friend Class B
        Protected Sub New(o As C)
        End Sub
    End Class
End Class
]]>
    </file>
</compilation>,
                {MetadataReference.CreateFromImage(other.EmitToArray())})
            CompilationUtils.AssertNoErrors(c)
        End Sub

        <WorkItem(542206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542206")>
        <Fact>
        Public Sub AccessCheckInternalVisibleToAttributeVBModule()
            Dim other As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="FriendAsmNightly01-Class1">
    <file name="a1.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("FriendAsmNightly01a.1141284.1")>

Friend Module Module1
    Public Const Const1 As Integer = 3
End Module
]]>
    </file>
</compilation>)

            Dim otherImage = other.EmitToArray()

            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="FriendAsmNightly01a.1141284.1">
    <file name="a2.vb"><![CDATA[
    Friend Module FriendAsmNightly01amod
        Sub FriendAsmNightly01a()
            If  Const1 = 3 Then
                System.Console.Write("PASS")
            Else
                System.Console.Write("FAIL")
            End If
        End Sub
    End Module
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(otherImage)})

            CompilationUtils.AssertNoErrors(comp)
        End Sub

        <Fact>
        Public Sub AccessCheckApi1()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="AccessCheckApi1">
    <file name="a.vb">
Imports System.Collections.Generic
Imports AliasA = A

Class A
    Shared Private priv As Integer
    Shared Public pub As Integer
    Protected prot As Integer
    Shared Private unknowntype As Goo

    Private Class K 
    End Class

    Private karray As K()
    Private aarray As A() 
    Private kenum As IEnumerable(Of K)
    Private aenum As IEnumerable(Of A)
End Class

Class B
End Class

Class ADerived
    Inherits A
End Class

Class ADerived2
    Inherits A
End Class
    </file>
</compilation>)
            Dim compilation As Compilation = c
            Dim globalNS As NamespaceSymbol = c.GlobalNamespace
            Dim sourceAssem As AssemblySymbol = c.SourceModule.ContainingAssembly
            Dim mscorlibAssem As AssemblySymbol = c.GetReferencedAssemblySymbol(c.References(0))
            Dim classA As NamedTypeSymbol = TryCast(globalNS.GetMembers("A").[Single](), NamedTypeSymbol)
            Dim tree = c.SyntaxTrees(0)
            Dim model = c.GetSemanticModel(tree)
            Dim importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 2).AsNode(), SimpleImportsClauseSyntax)
            Dim aliasA = DirectCast(model.GetDeclaredSymbol(importsClause), AliasSymbol)
            Dim classADerived As NamedTypeSymbol = TryCast(globalNS.GetMembers("ADerived").[Single](), NamedTypeSymbol)
            Dim classADerived2 As NamedTypeSymbol = TryCast(globalNS.GetMembers("ADerived2").[Single](), NamedTypeSymbol)
            Dim classB As NamedTypeSymbol = TryCast(globalNS.GetMembers("B").[Single](), NamedTypeSymbol)
            Dim classK As NamedTypeSymbol = TryCast(classA.GetMembers("K").[Single](), NamedTypeSymbol)
            Dim privField As FieldSymbol = TryCast(classA.GetMembers("priv").[Single](), FieldSymbol)
            Dim pubField As FieldSymbol = TryCast(classA.GetMembers("pub").[Single](), FieldSymbol)
            Dim protField As FieldSymbol = TryCast(classA.GetMembers("prot").[Single](), FieldSymbol)
            Dim karrayType As TypeSymbol = (TryCast(classA.GetMembers("karray").[Single](), FieldSymbol)).[Type]
            Dim aarrayType As TypeSymbol = (TryCast(classA.GetMembers("aarray").[Single](), FieldSymbol)).[Type]
            Dim kenumType As TypeSymbol = (TryCast(classA.GetMembers("kenum").[Single](), FieldSymbol)).[Type]
            Dim aenumType As TypeSymbol = (TryCast(classA.GetMembers("aenum").[Single](), FieldSymbol)).[Type]
            Dim unknownType As TypeSymbol = (TryCast(classA.GetMembers("unknowntype").[Single](), FieldSymbol)).[Type]
            Dim semanticModel = c.GetSemanticModel(c.SyntaxTrees(0))
            Assert.True(Symbol.IsSymbolAccessible(classA, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(classA, classB))
            Assert.True(Symbol.IsSymbolAccessible(aliasA, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(aliasA, classB))
            Assert.True(Symbol.IsSymbolAccessible(pubField, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(pubField, classB))
            Assert.False(Symbol.IsSymbolAccessible(privField, classB))
            Assert.False(compilation.IsSymbolAccessibleWithin(privField, classB))
            Assert.False(Symbol.IsSymbolAccessible(karrayType, classB))
            Assert.False(compilation.IsSymbolAccessibleWithin(karrayType, classB))
            Assert.True(Symbol.IsSymbolAccessible(aarrayType, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(aarrayType, classB))
            Assert.False(Symbol.IsSymbolAccessible(kenumType, classB))
            Assert.False(compilation.IsSymbolAccessibleWithin(kenumType, classB))
            Assert.True(Symbol.IsSymbolAccessible(aenumType, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(aenumType, classB))
            Assert.True(Symbol.IsSymbolAccessible(unknownType, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(unknownType, classB))
            Assert.True(Symbol.IsSymbolAccessible(globalNS, classB))
            Assert.True(compilation.IsSymbolAccessibleWithin(globalNS, classB))
            Assert.True(Symbol.IsSymbolAccessible(protField, classA))
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classA))
            Assert.True(Symbol.IsSymbolAccessible(protField, classA, classADerived))
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classA, classADerived))
            Assert.False(Symbol.IsSymbolAccessible(protField, classB))
            Assert.False(compilation.IsSymbolAccessibleWithin(protField, classB))
            Assert.False(Symbol.IsSymbolAccessible(protField, classB, classADerived))
            Assert.False(compilation.IsSymbolAccessibleWithin(protField, classB, classADerived))
            Assert.True(Symbol.IsSymbolAccessible(protField, classA))
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classA))
            Assert.True(Symbol.IsSymbolAccessible(protField, classADerived, classADerived))
            Assert.True(compilation.IsSymbolAccessibleWithin(protField, classADerived, classADerived))
            Assert.False(Symbol.IsSymbolAccessible(protField, classADerived, classADerived2))
            Assert.False(compilation.IsSymbolAccessibleWithin(protField, classADerived, classADerived2))
            Assert.True(Symbol.IsSymbolAccessible(classA, sourceAssem))
            Assert.True(compilation.IsSymbolAccessibleWithin(classA, sourceAssem))
            Assert.True(Symbol.IsSymbolAccessible(aliasA, sourceAssem))
            Assert.True(compilation.IsSymbolAccessibleWithin(aliasA, sourceAssem))
            Assert.True(Symbol.IsSymbolAccessible(aarrayType, sourceAssem))
            Assert.True(compilation.IsSymbolAccessibleWithin(aarrayType, sourceAssem))
            Assert.False(Symbol.IsSymbolAccessible(karrayType, sourceAssem))
            Assert.False(compilation.IsSymbolAccessibleWithin(karrayType, sourceAssem))
            Assert.False(Symbol.IsSymbolAccessible(classA, mscorlibAssem))
            Assert.False(compilation.IsSymbolAccessibleWithin(classA, mscorlibAssem))
            Assert.False(Symbol.IsSymbolAccessible(aliasA, mscorlibAssem))
            Assert.False(compilation.IsSymbolAccessibleWithin(aliasA, mscorlibAssem))
            Assert.True(Symbol.IsSymbolAccessible(unknownType, sourceAssem))
            Assert.True(compilation.IsSymbolAccessibleWithin(unknownType, sourceAssem))
            Assert.True(Symbol.IsSymbolAccessible(mscorlibAssem, sourceAssem))
            Assert.True(compilation.IsSymbolAccessibleWithin(mscorlibAssem, sourceAssem))
            Assert.False(sourceAssem.IsInteractive)
            Assert.Equal(ImmutableArray.Create(Of SyntaxReference)(), sourceAssem.DeclaringSyntaxReferences)
            Assert.Equal(5, sourceAssem.TypeNames.Count)
            Assert.Equal(2, sourceAssem.NamespaceNames.Count)
            Assert.Equal(sourceAssem.GetSpecialType(SpecialType.System_Object), sourceAssem.ObjectType)
        End Sub

        <Fact>
        Public Sub InconsistentAccessibility01()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Friend Class A
End Class

Public Class B
    Inherits A
End Class

Partial Public Class B
    Inherits A
End Class

Public Class C
    Public F1 As A

    Public Function M (x As A) As A
        return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30910: 'B' cannot inherit from class 'A' because it expands the access of the base class outside the assembly.
    Inherits A
             ~
BC30909: 'F1' cannot expose type 'A' outside the project through class 'C'.
    Public F1 As A
                 ~
BC30909: 'x' cannot expose type 'A' outside the project through class 'C'.
    Public Function M (x As A) As A
                            ~
BC30909: 'M' cannot expose type 'A' outside the project through class 'C'.
    Public Function M (x As A) As A
                                  ~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility02()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Public Class C(Of T)
    Protected Class A
    End Class
End Class
Public Class E
    Protected Class D
        Inherits C(Of D)
        Public Class B
            Inherits A
        End Class

        Public F1 As A

        Public Function M (x As A) As A
            return Nothing
        End Function

        Public Class F
            Inherits System.Collections.Generic.List(Of A)
        End Class
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30509: 'B' cannot inherit from class 'C(Of E.D).A' because it expands the access of the base class to class 'E'.
            Inherits A
                     ~
BC30508: 'F1' cannot expose type 'C(Of E.D).A' in class 'E' through class 'D'.
        Public F1 As A
                     ~
BC30508: 'x' cannot expose type 'C(Of E.D).A' in class 'E' through class 'D'.
        Public Function M (x As A) As A
                                ~
BC30508: 'M' cannot expose type 'C(Of E.D).A' in class 'E' through class 'D'.
        Public Function M (x As A) As A
                                      ~
BC30921: 'F' cannot inherit from class 'List(Of C(Of E.D).A)' because it expands the access of type 'C(Of E.D).A' to class 'E'.
            Inherits System.Collections.Generic.List(Of A)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility03()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Imports System.Collections.Generic 

Namespace Ns
    Friend Interface A
    End Interface

    Public Class B
        Implements A
    End Class

    Public Interface C
        Inherits IEnumerable(Of A)
    End Interface
End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30922: 'C' cannot inherit from interface 'IEnumerable(Of A)' because it expands the access of type 'A' outside the assembly.
        Inherits IEnumerable(Of A)
                 ~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility04()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Friend Class A
End Class

Public Class B

    Sub Goo(x As A)
    End Sub
End Class    
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30909: 'x' cannot expose type 'A' outside the project through class 'B'.
    Sub Goo(x As A)
                 ~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility05()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Public Class A(Of T)
    Private Class B
        Inherits A(Of C)

        Private Class C

        End Class
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30921: 'B' cannot inherit from class 'A(Of A(Of T).B.C)' because it expands the access of type 'A(Of T).B.C' to class 'A(Of T)'.
        Inherits A(Of C)
                 ~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility06()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Public Class A(Of T)
    Private Class B
        Inherits C.D

    End Class
End Class

Public Class C
    Friend Class D
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility07()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Public Class A(Of T)
    Protected Class B
        Inherits D

    End Class

    Friend Class D
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30910: 'B' cannot inherit from class 'A(Of T).D' because it expands the access of the base class outside the assembly.
        Inherits D
                 ~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility08()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Public Class A
    Inherits C

    Protected Class B
        Inherits D

    End Class

End Class

Public Class C
    Friend Class D
    End Class
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30910: 'B' cannot inherit from class 'C.D' because it expands the access of the base class outside the assembly.
        Inherits D
                 ~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility09()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="Module1.vb">
Imports System

Namespace Project1
    Friend Module Module1
        'Test expanding access against inheritance using a class
        Private Class PrivateClass
        End Class

        Public Class PublicClass
            Inherits PrivateClass 'err expected - public can't inherit from private
        End Class

        'Test expanding access against inheritance using an interface

        Friend Interface I1
            Sub goo()
        End Interface
        Public Interface I2 'no err expected - I2 is really friend
            Inherits I1
        End Interface

        Sub main()
        End Sub

    End Module
End Namespace

Namespace Project1

    Friend Module FriendModule1
        Private class PC1
        end class

        Friend Class Cls1
        End Class

        Public Class Cls2
            Inherits Cls1  'this incorrectly give a compile error. It should not give a compile although a Public type exposes a friend type because the Public type itself is present in a friend type
        End Class

        Public Class Cls3
            Inherits PC1 'error expected
        end class

        ' --- Interfaces

        Friend Interface I1
        End Interface

        Public Interface I2
           inherits I1 'no error expected since I2 is really Friend
        End Interface

        Sub main()
        End Sub
    End Module

    Public Module PublicModule2
        public class C1
            inherits FriendModule1.Cls2 'err expected, Module1.Cls2 is really Friend
        end class
    End Module

    Public Class PublicClass1
        Friend Class c1 'Bug 241789
            Inherits c2 'Need a compile error
        End Class

        Protected Class c2
            Public i As Integer
        End Class
    End Class

    'My own pathological case for determining access inside inheritance relationships
     Public Class PathologicalC2
	    Friend Class c3
	        Public Enum bob
	            asdf
	        End Enum
	    End Class
     End Class

     Public Class PathologicalC4
	    Inherits PathologicalC2
	    Public Function y() As c3.bob
            Return Nothing
	    End Function
     End Class

End Namespace

'Imports System
'Imports Microsoft.VisualBasic

Namespace Project1

  public Class goo
    Protected Class goo2
        protected Class goo3
            friend class goo4
            end class
        End Class
        Public x As goo3.goo4 'Error because somebody who derives from Protected Goo2 could see our Friend
    End Class
  End Class

  public class outer
	friend class friendcls
    end class
	public class goo
	  inherits friendcls 'error because public visibility all the way out
    end class
  end class 

  friend class outer2
	friend class friendcls
    end class
	public class goo
	  inherits friendcls 'no error because constrained to be friend by outer2
    end class
  end class 

  Public Class Class1 'Bug #222843
    Friend Class cls1
    End Class
    Friend Delegate Sub scen4(ByVal x As cls1)
    Protected e4 As scen4 'err - exposing a type restricted to the project outside the project
  End Class

  Public Class Bug197195
    Private Enum E
	x
    End Enum
    Public Function MyDelegate(x as E) as E 'Error - exposing protected types via Public delegate - delegates have special error reporting code
            Return Nothing
    End Function
  End Class

  Friend Class Bug234168
    Protected x as Bug234168_Cls2 'this was giving an unexpected compile error
  End Class

  Friend Class Bug234168_Cls2
  End Class

  Public Class Bug237607_1
     Protected Class goo3   'can be seen in the Family only
        Friend Class goo4
            Public Function goo() As Integer	' can be seen when in both Family AND Assembly
                Return 100
            End Function
        End Class
     End Class
     Friend Exposed As goo3.goo4 'can be seen by everybody in the Assembly  - the problem guy
  End Class

  Public Class Bug237607_2
     Dim XX As New Bug237607_1()
     Public Sub goo()
         'BUGBUG Console.WriteLine(XX.Exposed.goo)
     End Sub
  End Class

  friend Class Bug237665
    Protected Class cls2
        Public Class clsP
            Public x As cls3    '----- no problem here as expected
        End Class
    Friend Class clsF
        Public x As cls3    '----- shouldn't be an error
    End Class
    End Class
    Protected Class cls3
    End Class
  End Class

    Class Bug238161_cls1
        Protected Class clsIn
        End Class
    End Class

    Class Bug238161_cls2
        Inherits Bug238161_cls1
        Public Function goo0() As clsIn '----- Error expected
            Return Nothing
        End Function
    End Class

    Public Class Bug243040
        Friend Interface I1
            Enum E1
                a
            End Enum
        End Interface
        Public Class cls1
            Friend x As I1.E1   '---- no problems expected
        End Class
        Protected Class cls2
            Friend x As I1.E1   '---- no problems expected - was incorrectly getting 'x' illegally exposes a Friend type outside of the Protected class 'cls2'
        End Class
    End Class

    Public Class Bug277352A
        Protected e As Bug277352B.goo '-- err expected
    End Class

    Public Class Bug277352B
        Protected Friend Delegate Sub goo(ByVal x As Integer)
    End Class

    Public Class Bug277358A
        Protected Friend e As Bug277358B.goo '--- error
    End Class

    Public Class Bug277358B
        Protected Friend Delegate Sub goo(ByVal x As Integer)
    End Class

    Class Bug301420
        Protected Class c2
        End Class
        Protected Class c3
            Inherits c2  'No error expected here because c2 and c3 are members of the same class
        End Class
    End Class

    Class Bug304084
        Protected Class c2
        End Class
        Protected Class c3
            Protected Class c4
                Inherits c2   'no error expected here
            End Class
        End Class
    End Class

    Class Bug305622
        Protected Enum e
            z
        End Enum

        Protected Sub Goo(ByVal arg As e) 'this gives an unexpected compile error that type "e" cannot be exposed
        End Sub
    End Class

End Namespace

'Option Explicit On 

Namespace Project1
    Friend Class FriendCls

        '----------------------------- Define Types with various access levels

        Public Structure PublicType_FriendCls
            Public PublicType_m1 As Integer
        End Structure

        Friend Structure FriendType_FriendCls
            Friend FriendType_M1 As Integer
        End Structure

        Private Structure PrivateType_FriendCls
            Private PrivateType_M1 As Integer
        End Structure

        Private Enum PrivateEnum_FriendCls
            one
        End Enum

        '----------------------------- Interface tests

        Public Interface PublicInterface
            'use types from a Public class
            Function PublicFunc0(ByVal parameter As PublicCls.PublicType_PublicCls) As PublicCls.PublicType_PublicCls 'no errors expected
            Function PublicFunc1(ByVal parameter As PublicCls.FriendType_PublicCls) As PublicCls.FriendType_PublicCls 'no errors expected - we are in a friend class
            'use types from this Friend class
            Function PublicFunc2(ByVal parameter As PublicType_FriendCls) As PublicType_FriendCls 'no errors expected
            Function PublicFunc3(ByVal parameter As FriendType_FriendCls) As FriendType_FriendCls 'no errors expected - we are in a friend class
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'errors expected
        End Interface

        Friend Interface FriendInterface
            'use types from a Public class
            Function PublicFunc0(ByVal parameter As PublicCls.PublicType_PublicCls) As PublicCls.PublicType_PublicCls 'no errors expected
            Function PublicFunc1(ByVal parameter As PublicCls.FriendType_PublicCls) As PublicCls.FriendType_PublicCls 'no errors expected
            'use types from this Friend class
            Function PublicFunc2(ByVal parameter As PublicType_FriendCls) As PublicType_FriendCls 'no errors expected
            Function PublicFunc3(ByVal parameter As FriendType_FriendCls) As FriendType_FriendCls 'no errors expected
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'errors expected
        End Interface

        Private Interface PrivateInterface
            'use types from a public class
            Function PublicFunc0(ByVal parameter As PublicCls.PublicType_PublicCls) As PublicCls.PublicType_PublicCls 'no errors expected
            Function PublicFunc1(ByVal parameter As PublicCls.FriendType_PublicCls) As PublicCls.FriendType_PublicCls 'no errors expected
            'use types from this Friend class
            Function PublicFunc2(ByVal parameter As PublicType_FriendCls) As PublicType_FriendCls 'no errors expected
            Function PublicFunc3(ByVal parameter As FriendType_FriendCls) As FriendType_FriendCls 'no errors expected
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'no errors expected
        End Interface

        '----------------------------- Nested type tests

        Public Structure BadPublicType_FriendCls
            'use types from a public class
            Public PublicMember_PublicClass As PublicCls.PublicType_PublicCls 'No Error expected
            Public FriendMember_PublicClass As PublicCls.FriendType_PublicCls 'No Error expected since we are in a friend class
            'use types from this Friend class
            Public PublicMember As PublicType_FriendCls 'No Error expected
            Public FriendMember As FriendType_FriendCls 'No error expected since we are in a friend class
            Public PrivateMember As PrivateType_FriendCls 'Error expected
        End Structure

        Friend Structure BadFriendType_FriendCls
            'use types from a public class
            Public publicMember_PublicClass As PublicCls.PublicType_PublicCls 'No Error expected
            Public friendMember_PublicClass As PublicCls.FriendType_PublicCls 'No Error expected
            'use types from this Friend class
            Public publicMember As PublicType_FriendCls 'No Error expected
            Public friendMember As FriendType_FriendCls 'No Error expected
            Public PrivateMember As PrivateType_FriendCls 'Error expected
        End Structure

        Private Structure BadPrivateType_FriendCls
            'Use types from a public class
            Public publicMember_PublicClass As PublicCls.PublicType_PublicCls 'No Error expected
            Public friendMember_PublicClass As PublicCls.FriendType_PublicCls 'No Error expected
            'use types from this Friend class
            Public publicMember As PublicType_FriendCls 'No Error expected
            Public friendMember As FriendType_FriendCls 'No Error expected
            Public privateMember As PrivateType_FriendCls 'No Error expected
        End Structure

        '----------------------------- Class Exposure via Data Member Tests

        'use types from a public class
        Public PublicAsPublicType_PublicClass As PublicCls.PublicType_PublicCls 'No error expected
        Public PublicAsFriendType_PublicClass As PublicCls.FriendType_PublicCls 'No Error expected since we are in a friend class
        'use types from this Friend class
        Public PublicAsPublicType As PublicType_FriendCls 'No error expected
        Public PublicAsFriendType As FriendType_FriendCls 'No Error expected because this is a friend class
        Public PublicAsPrivateType As PrivateType_FriendCls 'Error expected
        Public PublicAsPrivateEnum As PrivateEnum_FriendCls 'Error expected ' test an enum for kicks

        'use types from a public class
        Friend FriendAsPublicType_PublicCls As PublicCls.PublicType_PublicCls 'No error expected
        Friend FriendAsFriendType_PublicCls As PublicCls.FriendType_PublicCls 'No Error expected
        'use types from this Friend class
        Friend FriendAsPublicType As PublicType_FriendCls 'No error expected
        Friend FriendAsFriendType As FriendType_FriendCls 'No Error expected
        Friend FriendAsPrivateType As PrivateType_FriendCls 'Error expected

        'use types from a public class
        Private PrivateAsPublicType As PublicCls.PublicType_PublicCls 'No error expected
        Private PrivateAsFriendType As PublicCls.FriendType_PublicCls 'No error expected
        'use types from this Friend class
        Private PrivateAsPublicType_FriendCls As PublicType_FriendCls 'No error expected
        Private PrivateAsFriendType_FriendCls As FriendType_FriendCls 'No error expected
        Private PrivateAsPrivateType_FriendCls As PrivateType_FriendCls 'No error expected

        '----------------------------- Class Exposure via Function parameters and return values

        'use types from a public class
        Public Function PublicFunc1(ByVal parameter As PublicCls.PublicType_PublicCls) As PublicCls.PublicType_PublicCls 'No error Expected
            Return Nothing
        End Function
        Public Function PublicFunc2(ByVal parameter As PublicCls.FriendType_PublicCls) As PublicCls.FriendType_PublicCls 'No Errors Expected - we are in a friend class
            Return Nothing
        End Function
        'use types from this Friend class
        Public Function PublicFunc3(ByVal parameter As PublicType_FriendCls) As PublicType_FriendCls 'No error Expected
            Return Nothing
        End Function
        Public Function PublicFunc4(ByVal parameter As FriendType_FriendCls) As FriendType_FriendCls 'No Error expected because this is a friend class
            Return Nothing
        End Function
        Public Function PublicFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'Errors Expected
            Return Nothing
        End Function

        'use types from a public class
        Friend Function FriendFunc1(ByVal parameter As PublicCls.PublicType_PublicCls) As PublicCls.PublicType_PublicCls 'No error Expected
            Return Nothing
        End Function
        Friend Function FriendFunc2(ByVal parameter As PublicCls.FriendType_PublicCls) As PublicCls.FriendType_PublicCls 'No Error Expected
            Return Nothing
        End Function
        'use types from this Friend class
        Friend Function FriendFunc3(ByVal parameter As PublicType_FriendCls) As PublicType_FriendCls 'No error Expected
            Return Nothing
        End Function
        Friend Function FriendFunc4(ByVal parameter As FriendType_FriendCls) As FriendType_FriendCls 'No Error Expected
            Return Nothing
        End Function
        Friend Function FriendFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'Errors Expected
            Return Nothing
        End Function

        'use types from a public class
        Private Function PrivateFunc1(ByVal parameter As PublicCls.PublicType_PublicCls) As PublicCls.PublicType_PublicCls 'No errors Expected
            Return Nothing
        End Function
        Private Function PrivateFunc2(ByVal parameter As PublicCls.FriendType_PublicCls) As PublicCls.FriendType_PublicCls 'No Errors Expected
            Return Nothing
        End Function
        'use types from this Friend class
        Private Function PrivateFunc3(ByVal parameter As PublicType_FriendCls) As PublicType_FriendCls 'No errors Expected
            Return Nothing
        End Function
        Private Function PrivateFunc4(ByVal parameter As FriendType_FriendCls) As FriendType_FriendCls 'No Errors Expected
            Return Nothing
        End Function
        Private Function PrivateFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'No Error Expected
            Return Nothing
        End Function

    End Class
End Namespace

'Option Explicit On 

Namespace Project1
    Public Class PublicCls

        '----------------------------- Define Types with various access levels

        Public Structure PublicType_PublicCls
            Public PublicType_m1 As Integer
        End Structure

        Friend Structure FriendType_PublicCls
            Public FriendType_M1 As Integer
        End Structure

        Private Structure PrivateType_PublicCls
            Public PrivateType_M1 As Integer
        End Structure

        '----------------------------- Interface tests

        Public Interface PublicInterface
            'use types from this Public class
            Function PublicFunc0(ByVal parameter As PublicType_PublicCls) As PublicType_PublicCls 'no errors expected
            Function PublicFunc1(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'errors expected
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'errors expected
            'use types from a Friend Class
            Function PublicFunc3(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'errors expected - type is from a friend class
            Function PublicFunc4(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'errors expected - type is from a friend class
        End Interface

        Friend Interface FriendInterface
            'use types from this Public class
            Function PublicFunc0(ByVal parameter As PublicType_PublicCls) As PublicType_PublicCls 'no errors expected
            Function PublicFunc1(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'no errors expected
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'errors expected
            'use types from a Friend Class
            Function PublicFunc6(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'no errors expected
            Function PublicFunc7(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'no errors expected
        End Interface

        Private Interface PrivateInterface
            'use types from a public class
            Function PublicFunc0(ByVal parameter As PublicType_PublicCls) As PublicType_PublicCls 'no errors expected
            Function PublicFunc1(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'no errors expected
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'no errors expected
            'use types from a friend class
            Function PublicFunc6(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'no errors expected
            Function PublicFunc7(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'no errors expected
        End Interface

        '----------------------------- Nested type tests

        Public Structure BadPublicType_PublicCls
            'use types from this public class
            Public PublicMember_PublicClass As PublicType_PublicCls 'No Error expected
            Public FriendMember_PublicClass As FriendType_PublicCls 'Error expected
            Public PrivateMember_PublicClass As PrivateType_PublicCls 'Error expected
            'use types from a friend class
            Public PublicMember_FriendClass As FriendCls.PublicType_FriendCls 'Error expected - can't publicly expose something defined in a friend class
            Public FriendMember_FriendClass As FriendCls.FriendType_FriendCls 'Error expected
        End Structure

        Friend Structure BadFriendType_PublicCls
            'use types from this public class
            Public publicMember_PublicClass As PublicType_PublicCls 'No Error expected
            Public friendMember_PublicClass As FriendType_PublicCls 'No Error expected
            Public PrivateMember_PublicClass As PrivateType_PublicCls 'Error expected
            'use types from a friend class
            Public publicMember_FriendClass As FriendCls.PublicType_FriendCls 'No Error expected
            Public friendMember_FriendClass As FriendCls.FriendType_FriendCls 'No Error expected
        End Structure

        Private Structure BadPrivateType_PublicCls
            'use types from this public class
            Public publicMember_PublicClass As PublicType_PublicCls 'No Error expected
            Public friendMember_PublicClass As FriendType_PublicCls 'No Error expected
            Public privateMember_PublicClass As PrivateType_PublicCls 'No Error expected
            'Use types from a friend class
            Public publicMember_FriendClass As FriendCls.PublicType_FriendCls 'No Error expected
            Public friendMember_FriendClass As FriendCls.FriendType_FriendCls 'No Error expected
        End Structure

        '----------------------------- Class Exposure via Data Member Tests

        'use types from this public class
        Public PublicAsPublicType_PublicClass As PublicType_PublicCls 'No error expected
        Public PublicAsFriendType_PublicClass As FriendType_PublicCls 'Error expected
        Public PublicAsPrivateType_PublicClass As PrivateType_PublicCls 'Error expected
        'use types from a friend class
        Public PublicAsPublicType_FriendClass As FriendCls.PublicType_FriendCls 'error expected - can't publicly expose something defined in a friend class
        Public PublicAsFriendType_FriendClass As FriendCls.FriendType_FriendCls 'Error expected

        'use types from this public class
        Friend FriendAsPublicType As PublicType_PublicCls 'No error expected
        Friend FriendAsFriendType As FriendType_PublicCls 'No Error expected
        Friend FriendAsPrivateType As PrivateType_PublicCls 'Error expected
        'use types from a friend class
        Friend FriendAsPublicType_FriendClass As FriendCls.PublicType_FriendCls 'No error expected
        Friend FriendAsFriendType_FriendClass As FriendCls.FriendType_FriendCls 'No Error expected

        'use types from this public class
        Private PrivateAsPublicType As PublicType_PublicCls 'No error expected
        Private PrivateAsFriendType As FriendType_PublicCls 'No error expected
        Private PrivateAsPrivateType As PrivateType_PublicCls 'No error expected
        'use types from a friend class
        Private PrivateAsPublicType_FriendCls As FriendCls.PublicType_FriendCls 'No error expected
        Private PrivateAsFriendType_FriendCls As FriendCls.FriendType_FriendCls 'No error expected

        '----------------------------- Class Exposure via Function parameters and return values

        'use types from this public class
        Public Function PublicFunc1(ByVal parameter As PublicType_PublicCls) As PublicType_PublicCls 'No error Expected
            Return Nothing
        End Function
        Public Function PublicFunc3(parameter As FriendType_PublicCls) As FriendType_PublicCls  'Errors Expected
            Return Nothing
        End Function
        Public Function PublicFunc4(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'Errors Expected
            Return Nothing
        End Function
        'use types from a friend class
        Public Function PublicFunc5(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'error Expected - type from a friend class
            Return Nothing
        End Function
        Public Function PublicFunc6(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'Errors Expected
            Return Nothing
        End Function

        'use types from this public class
        Friend Function FriendFunc1(ByVal parameter As PublicType_PublicCls) As PublicType_PublicCls 'No error Expected
            Return Nothing
        End Function
        Friend Function FriendFunc3(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'No Error Expected
            Return Nothing
        End Function
        Friend Function FriendFunc4(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'Errors Expected
            Return Nothing
        End Function
        'use types from a friend class
        Friend Function FriendFunc5(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'No error Expected
            Return Nothing
        End Function
        Friend Function FriendFunc6(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'No Error Expected
            Return Nothing
        End Function

        Private Function PrivateFunc1(ByVal parameter As PublicType_PublicCls) As PublicType_PublicCls 'No errors Expected
            Return Nothing
        End Function
        Private Function PrivateFunc2(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'No Errors Expected
            Return Nothing
        End Function
        Private Function PrivateFunc3(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'No Error Expected
            Return Nothing
        End Function
        'use types from a friend class
        Private Function PrivateFunc4(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'No errors Expected
            Return Nothing
        End Function
        Private Function PrivateFunc5(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'No Errors Expected
            Return Nothing
        End Function

    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30509: 'PublicClass' cannot inherit from class 'Module1.PrivateClass' because it expands the access of the base class to namespace 'Project1'.
            Inherits PrivateClass 'err expected - public can't inherit from private
                     ~~~~~~~~~~~~
BC30509: 'Cls3' cannot inherit from class 'FriendModule1.PC1' because it expands the access of the base class to namespace 'Project1'.
            Inherits PC1 'error expected
                     ~~~
BC30910: 'C1' cannot inherit from class 'FriendModule1.Cls2' because it expands the access of the base class outside the assembly.
            inherits FriendModule1.Cls2 'err expected, Module1.Cls2 is really Friend
                     ~~~~~~~~~~~~~~~~~~
BC30509: 'c1' cannot inherit from class 'PublicClass1.c2' because it expands the access of the base class to namespace 'Project1'.
            Inherits c2 'Need a compile error
                     ~~
BC30909: 'y' cannot expose type 'PathologicalC2.c3.bob' outside the project through class 'PathologicalC4'.
	    Public Function y() As c3.bob
                            ~~~~~~
BC30508: 'x' cannot expose type 'goo.goo2.goo3.goo4' in class 'goo' through class 'goo2'.
        Public x As goo3.goo4 'Error because somebody who derives from Protected Goo2 could see our Friend
                    ~~~~~~~~~
BC30910: 'goo' cannot inherit from class 'outer.friendcls' because it expands the access of the base class outside the assembly.
	  inherits friendcls 'error because public visibility all the way out
            ~~~~~~~~~
BC30909: 'e4' cannot expose type 'Class1.scen4' outside the project through class 'Class1'.
    Protected e4 As scen4 'err - exposing a type restricted to the project outside the project
                    ~~~~~
BC30508: 'x' cannot expose type 'Bug197195.E' in namespace 'Project1' through class 'Bug197195'.
    Public Function MyDelegate(x as E) as E 'Error - exposing protected types via Public delegate - delegates have special error reporting code
                                    ~
BC30508: 'MyDelegate' cannot expose type 'Bug197195.E' in namespace 'Project1' through class 'Bug197195'.
    Public Function MyDelegate(x as E) as E 'Error - exposing protected types via Public delegate - delegates have special error reporting code
                                          ~
BC30508: 'Exposed' cannot expose type 'Bug237607_1.goo3.goo4' in namespace 'Project1' through class 'Bug237607_1'.
     Friend Exposed As goo3.goo4 'can be seen by everybody in the Assembly  - the problem guy
                       ~~~~~~~~~
BC30508: 'goo0' cannot expose type 'Bug238161_cls1.clsIn' in namespace 'Project1' through class 'Bug238161_cls2'.
        Public Function goo0() As clsIn '----- Error expected
                                  ~~~~~
BC30909: 'e' cannot expose type 'Bug277352B.goo' outside the project through class 'Bug277352A'.
        Protected e As Bug277352B.goo '-- err expected
                       ~~~~~~~~~~~~~~
BC30909: 'e' cannot expose type 'Bug277358B.goo' outside the project through class 'Bug277358A'.
        Protected Friend e As Bug277358B.goo '--- error
                              ~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through interface 'PublicInterface'.
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'errors expected
                                                    ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicFunc4' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through interface 'PublicInterface'.
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'errors expected
                                                                              ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through interface 'FriendInterface'.
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'errors expected
                                                    ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicFunc4' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through interface 'FriendInterface'.
            Function PublicFunc4(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'errors expected
                                                                              ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PrivateMember' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through structure 'BadPublicType_FriendCls'.
            Public PrivateMember As PrivateType_FriendCls 'Error expected
                                    ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PrivateMember' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through structure 'BadFriendType_FriendCls'.
            Public PrivateMember As PrivateType_FriendCls 'Error expected
                                    ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicAsPrivateType' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Public PublicAsPrivateType As PrivateType_FriendCls 'Error expected
                                      ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicAsPrivateEnum' cannot expose type 'FriendCls.PrivateEnum_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Public PublicAsPrivateEnum As PrivateEnum_FriendCls 'Error expected ' test an enum for kicks
                                      ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'FriendAsPrivateType' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Friend FriendAsPrivateType As PrivateType_FriendCls 'Error expected
                                      ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Public Function PublicFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'Errors Expected
                                                       ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicFunc5' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Public Function PublicFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'Errors Expected
                                                                                 ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Friend Function FriendFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'Errors Expected
                                                       ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'FriendFunc5' cannot expose type 'FriendCls.PrivateType_FriendCls' in namespace 'Project1' through class 'FriendCls'.
        Friend Function FriendFunc5(ByVal parameter As PrivateType_FriendCls) As PrivateType_FriendCls 'Errors Expected
                                                                                 ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'parameter' cannot expose type 'PublicCls.FriendType_PublicCls' outside the project through interface 'PublicInterface'.
            Function PublicFunc1(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'errors expected
                                                    ~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicFunc1' cannot expose type 'PublicCls.FriendType_PublicCls' outside the project through interface 'PublicInterface'.
            Function PublicFunc1(ByVal parameter As FriendType_PublicCls) As FriendType_PublicCls 'errors expected
                                                                             ~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through interface 'PublicInterface'.
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'errors expected
                                                    ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicFunc2' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through interface 'PublicInterface'.
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'errors expected
                                                                              ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'parameter' cannot expose type 'FriendCls.PublicType_FriendCls' outside the project through interface 'PublicInterface'.
            Function PublicFunc3(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'errors expected - type is from a friend class
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicFunc3' cannot expose type 'FriendCls.PublicType_FriendCls' outside the project through interface 'PublicInterface'.
            Function PublicFunc3(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'errors expected - type is from a friend class
                                                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'parameter' cannot expose type 'FriendCls.FriendType_FriendCls' outside the project through interface 'PublicInterface'.
            Function PublicFunc4(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'errors expected - type is from a friend class
                                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicFunc4' cannot expose type 'FriendCls.FriendType_FriendCls' outside the project through interface 'PublicInterface'.
            Function PublicFunc4(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'errors expected - type is from a friend class
                                                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through interface 'FriendInterface'.
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'errors expected
                                                    ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicFunc2' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through interface 'FriendInterface'.
            Function PublicFunc2(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'errors expected
                                                                              ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'FriendMember_PublicClass' cannot expose type 'PublicCls.FriendType_PublicCls' outside the project through structure 'BadPublicType_PublicCls'.
            Public FriendMember_PublicClass As FriendType_PublicCls 'Error expected
                                               ~~~~~~~~~~~~~~~~~~~~
BC30508: 'PrivateMember_PublicClass' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through structure 'BadPublicType_PublicCls'.
            Public PrivateMember_PublicClass As PrivateType_PublicCls 'Error expected
                                                ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicMember_FriendClass' cannot expose type 'FriendCls.PublicType_FriendCls' outside the project through structure 'BadPublicType_PublicCls'.
            Public PublicMember_FriendClass As FriendCls.PublicType_FriendCls 'Error expected - can't publicly expose something defined in a friend class
                                               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'FriendMember_FriendClass' cannot expose type 'FriendCls.FriendType_FriendCls' outside the project through structure 'BadPublicType_PublicCls'.
            Public FriendMember_FriendClass As FriendCls.FriendType_FriendCls 'Error expected
                                               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PrivateMember_PublicClass' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through structure 'BadFriendType_PublicCls'.
            Public PrivateMember_PublicClass As PrivateType_PublicCls 'Error expected
                                                ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicAsFriendType_PublicClass' cannot expose type 'PublicCls.FriendType_PublicCls' outside the project through class 'PublicCls'.
        Public PublicAsFriendType_PublicClass As FriendType_PublicCls 'Error expected
                                                 ~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicAsPrivateType_PublicClass' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through class 'PublicCls'.
        Public PublicAsPrivateType_PublicClass As PrivateType_PublicCls 'Error expected
                                                  ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicAsPublicType_FriendClass' cannot expose type 'FriendCls.PublicType_FriendCls' outside the project through class 'PublicCls'.
        Public PublicAsPublicType_FriendClass As FriendCls.PublicType_FriendCls 'error expected - can't publicly expose something defined in a friend class
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicAsFriendType_FriendClass' cannot expose type 'FriendCls.FriendType_FriendCls' outside the project through class 'PublicCls'.
        Public PublicAsFriendType_FriendClass As FriendCls.FriendType_FriendCls 'Error expected
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30508: 'FriendAsPrivateType' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through class 'PublicCls'.
        Friend FriendAsPrivateType As PrivateType_PublicCls 'Error expected
                                      ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'parameter' cannot expose type 'PublicCls.FriendType_PublicCls' outside the project through class 'PublicCls'.
        Public Function PublicFunc3(parameter As FriendType_PublicCls) As FriendType_PublicCls  'Errors Expected
                                                 ~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicFunc3' cannot expose type 'PublicCls.FriendType_PublicCls' outside the project through class 'PublicCls'.
        Public Function PublicFunc3(parameter As FriendType_PublicCls) As FriendType_PublicCls  'Errors Expected
                                                                          ~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through class 'PublicCls'.
        Public Function PublicFunc4(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'Errors Expected
                                                       ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'PublicFunc4' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through class 'PublicCls'.
        Public Function PublicFunc4(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'Errors Expected
                                                                                 ~~~~~~~~~~~~~~~~~~~~~
BC30909: 'parameter' cannot expose type 'FriendCls.PublicType_FriendCls' outside the project through class 'PublicCls'.
        Public Function PublicFunc5(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'error Expected - type from a friend class
                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicFunc5' cannot expose type 'FriendCls.PublicType_FriendCls' outside the project through class 'PublicCls'.
        Public Function PublicFunc5(ByVal parameter As FriendCls.PublicType_FriendCls) As FriendCls.PublicType_FriendCls 'error Expected - type from a friend class
                                                                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'parameter' cannot expose type 'FriendCls.FriendType_FriendCls' outside the project through class 'PublicCls'.
        Public Function PublicFunc6(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'Errors Expected
                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30909: 'PublicFunc6' cannot expose type 'FriendCls.FriendType_FriendCls' outside the project through class 'PublicCls'.
        Public Function PublicFunc6(ByVal parameter As FriendCls.FriendType_FriendCls) As FriendCls.FriendType_FriendCls 'Errors Expected
                                                                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30508: 'parameter' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through class 'PublicCls'.
        Friend Function FriendFunc4(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'Errors Expected
                                                       ~~~~~~~~~~~~~~~~~~~~~
BC30508: 'FriendFunc4' cannot expose type 'PublicCls.PrivateType_PublicCls' in namespace 'Project1' through class 'PublicCls'.
        Friend Function FriendFunc4(ByVal parameter As PrivateType_PublicCls) As PrivateType_PublicCls 'Errors Expected
                                                                                 ~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility10()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Public Interface A
    Inherits C

    Protected Interface B
        Inherits D

    End Interface

End Interface

Public Interface C
    Friend Interface D
    End Interface
End Interface
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31209: Interface in an interface cannot be declared 'Protected'.
    Protected Interface B
    ~~~~~~~~~
BC30910: 'B' cannot inherit from interface 'C.D' because it expands the access of the base interface outside the assembly.
        Inherits D
                 ~
</expected>)

        End Sub

        <Fact>
        Public Sub InconsistentAccessibility11()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Namespace Project1

    Public Class A

        Public C As B 

        Protected Friend Structure B
        End Structure
    End Class
End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30909: 'C' cannot expose type 'A.B' outside the project through class 'A'.
        Public C As B 
                    ~
</expected>)

        End Sub

        <WorkItem(543576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543576")>
        <Fact()>
        Public Sub InconsistentAccessibilityOfGenericConstraint()

            Dim compilationDef =
<compilation name="Bug4038">
    <file name="a.vb">
Imports System
Public Class A
    Private Class B(Of T As B(Of T).C)
        Private Class C
        End Class

        Public Sub D(Of S As C)()
        End Sub
    End Class

    Sub Main()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30508: 'B' cannot expose type 'A.B(Of T).C' in class 'A' through class 'B'.
    Private Class B(Of T As B(Of T).C)
                            ~~~~~~~~~
BC30508: 'D' cannot expose type 'A.B(Of T).C' in class 'A' through class 'B'.
        Public Sub D(Of S As C)()
                             ~
</expected>)
        End Sub

        <Fact(), WorkItem(545722, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545722")>
        Public Sub AccessCheckInaccessibleReturnType()
            Dim assem1 As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Assem1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

<Assembly: AssemblyTitle("TestFile_Class1")> 
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assem2")> 
Friend Class Class1
    Public ReadOnly Property Value() As String
        Get
            Return 42
        End Get
    End Property
End Class
Module M1
 Sub Main()
  Console.WriteLine("Class1")
 End Sub
End Module
Friend Delegate Sub Deleg1()
]]>
    </file>
</compilation>)
            Dim assem1Bytes = assem1.EmitToArray()

            Dim assem2 As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="Assem2">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Runtime.InteropServices
<Assembly: AssemblyTitle("TestFile_Class2")> 
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assem3")> 
Friend Class Class2
    Friend FldClass1 As Class1
    Friend Property PropClass1 As Class1
    Friend Event EvntDeleg1 As Deleg1

    Friend Function GetClass1() As Class1
        Return New Class1
    End Function
    Public Sub SetClass1(ByVal c1 As Class1)
    End Sub

    Public Function GetGenericClass1() As List(Of Class1)
        Return New List(Of Class1)()
    End Function
End Class

Module M1
    Sub Main()
        Console.WriteLine("Class2")
    End Sub
End Module
]]>
    </file>
</compilation>, {MetadataReference.CreateFromImage(assem1Bytes)})
            Dim assem2Bytes = assem2.EmitToArray()

            Dim assem3 As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="Assem3">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices
Module Module1
    Sub Main()
        Dim c2 As New Class2
        Dim x = c2.GetClass1()
        Dim y = c2.FldClass1
        Dim z = c2.PropClass1
        AddHandler c2.EvntDeleg1, Sub() Console.WriteLine()

        Dim u As Func(Of Object) = AddressOf c2.GetClass1
    End Sub

End Module

]]>
    </file>
</compilation>,
                {MetadataReference.CreateFromImage(assem1Bytes), MetadataReference.CreateFromImage(assem2Bytes)})


            CompilationUtils.AssertTheseDiagnostics(assem3,
<expected>
BC36666: 'Friend Function Class2.GetClass1() As Class1' is not accessible in this context because the return type is not accessible.
        Dim x = c2.GetClass1()
                ~~~~~~~~~~~~~~
BC36666: 'Friend Class2.FldClass1 As Class1' is not accessible in this context because the return type is not accessible.
        Dim y = c2.FldClass1
                ~~~~~~~~~~~~
BC36666: 'Friend Property Class2.PropClass1 As Class1' is not accessible in this context because the return type is not accessible.
        Dim z = c2.PropClass1
                ~~~~~~~~~~~~~
BC36666: 'Friend Function Class2.GetClass1() As Class1' is not accessible in this context because the return type is not accessible.
        Dim u As Func(Of Object) = AddressOf c2.GetClass1
                                             ~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(546209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546209")>
        <Fact()>
        Public Sub OverriddenMemberFromInternalType()
            Dim vbSource1 =
                <compilation name="A">
                    <file name="a.vb"><![CDATA[
Option Strict On
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("B")> 
Friend MustInherit Class A
    Public MustOverride Sub M()
    Public MustOverride ReadOnly Property P As Object
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(vbSource1)
            compilation1.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation1)
            Dim reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData)
            Dim vbSource2 =
                <compilation name="B">
                    <file name="b.vb"><![CDATA[
Option Strict On
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("C")> 
Friend MustInherit Class B
    Inherits A
    Public MustOverride Overrides Sub M()
    Public MustOverride Overrides ReadOnly Property P As Object
End Class
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(vbSource2, {reference1})
            compilation2.AssertNoErrors()
            compilationVerifier = CompileAndVerify(compilation2)
            Dim reference2 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData)
            Dim vbSource3 =
                <compilation name="C">
                    <file name="c.vb"><![CDATA[
Module M
    Function M(o As B) As Object
        o.M()
        Return o.P
    End Function
End Module
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(vbSource3, {reference1, reference2})
            compilation3.AssertNoErrors()
        End Sub

        <WorkItem(546209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546209")>
        <Fact()>
        Public Sub InternalOverriddenMember()
            Dim vbSource1 =
                <compilation name="A">
                    <file name="a.vb"><![CDATA[
Option Strict On
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("B")> 
Public MustInherit Class A
    Friend MustOverride Sub M()
    Friend MustOverride ReadOnly Property P As Object
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(vbSource1)
            compilation1.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation1)
            Dim reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData)
            Dim vbSource2 =
                <compilation name="B">
                    <file name="b.vb"><![CDATA[
Option Strict On
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("C")> 
Public MustInherit Class B
    Inherits A
    Friend MustOverride Overrides Sub M()
    Friend MustOverride Overrides ReadOnly Property P As Object
End Class
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(vbSource2, {reference1})
            compilation2.AssertNoErrors()
            compilationVerifier = CompileAndVerify(compilation2)
            Dim reference2 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData)
            Dim vbSource3 =
                <compilation name="C">
                    <file name="c.vb"><![CDATA[
Module M
    Function M(o As B) As Object
        o.M()
        Return o.P
    End Function
End Module
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(vbSource3, {reference1, reference2})
            compilation3.AssertNoErrors()
        End Sub

        <Fact, WorkItem(531415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531415")>
        Public Sub Bug18091()
            Dim c As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C1
    Default Protected Property D(i As Integer)
        Get
            Return Nothing
        End Get
        Set(value)
 
        End Set
    End Property
End Class
Class C2
    Inherits C1
    Sub tests()
        Dim a = New C2
        Dim x = a(1) 
    End Sub
    Sub tests2()
        Dim d As C1 = New C2
        Dim x = d(1) 
    End Sub
    Sub tests3(Of T As C2)(e as T)
        Dim x = e(1) 
    End Sub
    Sub tests4(Of T As C1)(f as T)
        Dim x = f(1) 
    End Sub
End Class
Class C3
    Sub tests()
        Dim b = New C2
        Dim x = b(1) 
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(c,
<expected>
BC30389: 'C1.D(i As Integer)' is not accessible in this context because it is 'Protected'.
        Dim x = d(1) 
                ~
BC30389: 'C1.D(i As Integer)' is not accessible in this context because it is 'Protected'.
        Dim x = f(1) 
                ~
BC30389: 'C1.D(i As Integer)' is not accessible in this context because it is 'Protected'.
        Dim x = b(1) 
                ~
</expected>)

        End Sub

        <Fact>
        Public Sub InaccessibleToUnnamedExe_01()
            Dim sourceA =
"Class A
End Class"
            Dim comp = CreateCompilation(sourceA)
            Dim refA = comp.EmitToImageReference()

            Dim sourceB =
"Class B
    Shared Sub Main()
        Dim a = New A()
    End Sub
End Class"
            ' Unnamed assembly (the default from the command-line compiler).
            comp = CreateCompilation(sourceB, references:={refA}, options:=TestOptions.ReleaseExe, assemblyName:=Nothing)
            comp.AssertTheseDiagnostics(
<expected>
BC30389: 'A' is not accessible in this context because it is 'Friend'.
        Dim a = New A()
                    ~
</expected>)

            ' Named assembly.
            comp = CreateCompilation(sourceB, references:={refA}, options:=TestOptions.ReleaseExe, assemblyName:="B")
            comp.AssertTheseDiagnostics(
<expected>
BC30389: 'A' is not accessible in this context because it is 'Friend'.
        Dim a = New A()
                    ~
</expected>)
        End Sub

        <Fact>
        Public Sub InaccessibleToUnnamedExe_02()
            Dim sourceA =
"<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")>
Class A
End Class"
            Dim comp = CreateCompilation(sourceA)
            Dim refA = comp.EmitToImageReference()

            Dim sourceB =
"Class B
    Shared Sub Main()
        Dim a = New A()
    End Sub
End Class"
            ' Unnamed assembly (the default from the command-line compiler).
            comp = CreateCompilation(sourceB, references:={refA}, options:=TestOptions.ReleaseExe, assemblyName:=Nothing)
            comp.AssertTheseDiagnostics(
<expected>
BC30389: 'A' is not accessible in this context because it is 'Friend'.
        Dim a = New A()
                    ~
</expected>)

            ' Named assembly.
            comp = CreateCompilation(sourceB, references:={refA}, options:=TestOptions.ReleaseExe, assemblyName:="B")
            comp.AssertTheseDiagnostics()

            ' Named assembly (distinct).
            comp = CreateCompilation(sourceB, references:={refA}, options:=TestOptions.ReleaseExe, assemblyName:="B2")
            comp.AssertTheseDiagnostics(
<expected>
BC30389: 'A' is not accessible in this context because it is 'Friend'.
        Dim a = New A()
                    ~
</expected>)
        End Sub

    End Class
End Namespace
