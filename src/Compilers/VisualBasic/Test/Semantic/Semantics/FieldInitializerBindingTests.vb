' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class FieldInitializerBindingTests
        Inherits BasicTestBase

        <Fact>
        Public Sub NoInitializers()
            Dim source =
<compilation name="NoInitializers">
    <file name="fi.vb">
Class C
    Shared s1 as String
    Dim i1 As Integer
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) = Nothing
            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) = Nothing
            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub ConstantInstanceInitializer()
            Dim source =
<compilation name="ConstantInstanceInitializer">
    <file name="fi.vb">
Class C
    Shared s1 as String
    Dim i1 As Integer = 1
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) = Nothing
            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("i1", "1", lineNumber:=2)}

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub ConstantStaticInitializer()
            Dim source =
<compilation name="ConstantStaticInitializer">
    <file name="fi.vb">
Class C
    Shared s1 as String = "1"
    Dim i1 As Integer 
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("s1", """1""", lineNumber:=1)}
            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) = Nothing

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub ExpressionInstanceInitializer()
            Dim source =
<compilation name="ExpressionInstanceInitializer">
    <file name="fi.vb">
Class C
    Shared s1 As String
    Dim i1 As Integer = 1 + Foo()
    Dim i2 As New C()

    Shared Function Foo() As Integer
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) = Nothing
            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("i1", "1 + Foo()", lineNumber:=2),
                                           New ExpectedInitializer("i2", "As New C()", lineNumber:=3)}

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub ExpressionStaticInitializer()
            Dim source =
<compilation name="ExpressionStaticInitializer">
    <file name="fi.vb">
Class C
    Shared s1 As Integer = 1 + Foo()
    Dim i1 As Integer

    Shared Function Foo() As Integer
        Return 1
    End Function
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("s1", "1 + Foo()", lineNumber:=1)}
            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) = Nothing

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub InitializerOrder()
            Dim source =
<compilation name="InitializerOrder">
    <file name="fi.vb">
Class C
    Shared s1 As Integer = 1
    Shared s2 As Integer = 2
    Shared s3 As Integer = 3
    Dim i1 As Integer = 1
    Dim i2 As Integer = 2
    Dim i3 As Integer = 3
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("s1", "1", lineNumber:=1),
                                            New ExpectedInitializer("s2", "2", lineNumber:=2),
                                            New ExpectedInitializer("s3", "3", lineNumber:=3)}

            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("i1", "1", lineNumber:=4),
                                            New ExpectedInitializer("i2", "2", lineNumber:=5),
                                            New ExpectedInitializer("i3", "3", lineNumber:=6)}

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub AllPartialClasses()
            Dim source =
<compilation name="AllPartialClasses">
    <file name="fi.vb">
Partial Class C
    Shared s1 As Integer = 1
    Dim i1 As Integer = 1
End Class
Partial Class C
    Shared s2 As Integer = 2
    Dim i2 As Integer = 2
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("s1", "1", lineNumber:=1),
                                            New ExpectedInitializer("s2", "2", lineNumber:=5)}

            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("i1", "1", lineNumber:=2),
                                            New ExpectedInitializer("i2", "2", lineNumber:=6)}

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub SomePartialClasses()
            Dim source =
<compilation name="SomePartialClasses">
    <file name="fi.vb">
Partial Class C
    Shared s1 As Integer = 1
    Dim i1 As Integer = 1
End Class
Partial Class C
    Shared s2 As Integer = 2
    Dim i2 As Integer = 2
End Class
Partial Class C
    Shared s3 As Integer
    Dim i3 As Integer
End Class
    </file>
</compilation>

            Dim expectedStaticInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("s1", "1", lineNumber:=1),
                                            New ExpectedInitializer("s2", "2", lineNumber:=5)}

            Dim expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer) =
                New ExpectedInitializer() {New ExpectedInitializer("i1", "1", lineNumber:=2),
                                            New ExpectedInitializer("i2", "2", lineNumber:=6)}

            CompileAndCheckInitializers(source, expectedInstanceInitializers, expectedStaticInitializers)
        End Sub

        <Fact>
        Public Sub NoStaticMembers()
            Dim source =
<compilation name="NoStaticMembers">
    <file name="fi.vb">
Class C
    Dim i1 As Integer
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
            Assert.False(IsStatic(GetMember(source, "i1")))
        End Sub

        <Fact>
        Public Sub NoStaticFields()
            Dim source =
<compilation name="NoStaticFields">
    <file name="fi.vb">
Class C
    Dim i1 As Integer

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
            Assert.False(IsStatic(GetMember(source, "i1")))
        End Sub

        <Fact>
        Public Sub NoStaticInitializers()
            Dim source =
<compilation name="NoStaticInitializers">
    <file name="fi.vb">
Class C
    Dim i1 As Integer
    Shared s1 As Integer

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
            Assert.False(IsStatic(GetMember(source, "i1")))
            Assert.True(IsStatic(GetMember(source, "s1")))
        End Sub

        <Fact>
        Public Sub StaticInitializers()
            Dim source =
<compilation name="StaticInitializers">
    <file name="fi.vb">
Class C
    Dim i1 As Integer
    Shared s1 As Integer = 1

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.True(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.True(IsBeforeFieldInit(typeSymbol))
            Assert.False(IsStatic(GetMember(source, "i1")))
            Assert.True(IsStatic(GetMember(source, "s1")))
        End Sub

        <Fact>
        Public Sub ConstantInitializers()
            Dim source =
<compilation name="ConstantInitializers">
    <file name="fi.vb">
Class C
    Dim i1 As Integer
    Const s1 As Integer = 1

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
            Assert.False(IsStatic(GetMember(source, "i1")))
            Assert.True(IsStatic(GetMember(source, "s1")))
        End Sub

        <Fact>
        Public Sub SourceStaticConstructorNoStaticMembers()
            Dim source =
<compilation name="SourceStaticConstructorNoStaticMembers">
    <file name="fi.vb">
Class C
    Shared Sub New()
    End Sub

    Dim i1 As Integer
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
        End Sub

        <Fact>
        Public Sub SourceStaticConstructorNoStaticFields()
            Dim source =
<compilation name="SourceStaticConstructorNoStaticFields">
    <file name="fi.vb">
Class C
    Shared Sub New()
    End Sub

    Dim i1 As Integer

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
        End Sub

        <Fact>
        Public Sub SourceStaticConstructorNoStaticInitializers()
            Dim source =
<compilation name="SourceStaticConstructorNoStaticInitializers">
    <file name="fi.vb">
Class C
    Shared Sub New()
    End Sub

    Dim i1 As Integer
    Shared s1 As Integer

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
        End Sub

        <Fact>
        Public Sub SourceStaticConstructorStaticInitializers()
            Dim source =
<compilation name="SourceStaticConstructorStaticInitializers">
    <file name="fi.vb">
Class C
    Shared Sub New()
    End Sub

    Dim i1 As Integer
    Shared s1 As Integer = 1

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
        End Sub

        <Fact>
        Public Sub SourceStaticConstructorConstantInitializers()
            Dim source =
<compilation name="SourceStaticConstructorConstantInitializers">
    <file name="fi.vb">
Class C
    Shared Sub New()
    End Sub

    Dim i1 As Integer
    Const s1 As Integer = 1

    Shared Sub Foo()
    End Sub
End Class
    </file>
</compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)
            Assert.False(HasSynthesizedStaticConstructor(typeSymbol))
            Assert.False(IsBeforeFieldInit(typeSymbol))
        End Sub

        <Fact>
        Public Sub SourceSingleDimensionArrayWithInitializers()
            Dim source =
                <compilation name="Array1D.vb">
                    <file name="a.cs">Imports System
Class Test
    Friend Shared ary01 As Short() = {+1, -2, 0}, ary02() As Single = {Math.Sqrt(2.0), 1.234!}
    ReadOnly ary03() = {"1", ary01(0).ToString(), Nothing, ""}, ary04 = {1, F(Nothing)}

    Function F(o As Object) As String
        If (o Is Nothing) Then
            Return Nothing
        End If
        Return o.ToString()
    End Function
End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            Dim tree = compilation.SyntaxTrees(0)

            Dim typeSymbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single(), SourceNamedTypeSymbol)

            Dim ary = DirectCast(typeSymbol.GetMembers("ary01").FirstOrDefault(), FieldSymbol)
            Assert.True(ary.IsShared)
            Assert.Equal(TypeKind.Array, ary.Type.TypeKind)
            Assert.Equal("System.Int16()", ary.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ary = DirectCast(typeSymbol.GetMembers("ary02").FirstOrDefault(), FieldSymbol)
            Assert.True(ary.IsShared)
            Assert.Equal(TypeKind.Array, ary.Type.TypeKind)
            Assert.Equal("System.Single()", ary.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ary = DirectCast(typeSymbol.GetMembers("ary03").FirstOrDefault(), FieldSymbol)
            Assert.True(ary.IsReadOnly)
            Assert.Equal(TypeKind.Array, ary.Type.TypeKind)
            Assert.Equal("System.Object()", ary.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ary = DirectCast(typeSymbol.GetMembers("ary04").FirstOrDefault(), FieldSymbol)
            Assert.True(ary.IsReadOnly)
            Assert.Equal(TypeKind.Class, ary.Type.TypeKind)
            Assert.Equal("System.Object", ary.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            '''' NYI: collection initializer
            'Dim expectedInitializers As IEnumerable(Of ExpectedInitializer) = _
            'New ExpectedInitializer() {New ExpectedInitializer("ary01", "{+1, -2, 0}", lineNumber:=2),
            '                           New ExpectedInitializer("ary02", "{Math.Sqrt(2.0), 1.234!}", lineNumber:=2)}

            'Dim boundInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.StaticInitializers)
            'CheckBoundInitializers(expectedInitializers, tree, boundInitializers, isStatic:=True)

            'expectedInitializers = New ExpectedInitializer() {
            '    New ExpectedInitializer("ary03", "{""1"", ary01(0).ToString(), Nothing, """"}", lineNumber:=3),
            '    New ExpectedInitializer("ary04", "{1, F(Nothing)}", lineNumber:=3)}

            'boundInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.InstanceInitializers)
            'CheckBoundInitializers(expectedInitializers, tree, boundInitializers, isStatic:=False)

        End Sub

        <Fact>
        Public Sub SourceFieldInitializers007()
            Dim source =
                <compilation name="FieldInit.dll">
                    <file name="aaa.cs.vb">Imports System
Class Test
    Friend Shared field01 As New Double()
    Const field02 = -2147483648 - 1, field03 = True + True ' -2
    ReadOnly field04 = F(Nothing), field05 As New Func(Of String, ULong)(Function(s) s.Length)

    Function F(o As Object) As String
        Return Nothing
    End Function
End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)
            Dim tree = compilation.SyntaxTrees(0)

            Dim typeSymbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetTypeMembers("Test").Single(), SourceNamedTypeSymbol)

            Dim field = DirectCast(typeSymbol.GetMembers("field01").FirstOrDefault(), FieldSymbol)
            Assert.True(field.IsShared)
            Assert.Equal(TypeKind.Structure, field.Type.TypeKind)
            Assert.Equal("System.Double", field.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            field = DirectCast(typeSymbol.GetMembers("field02").FirstOrDefault(), FieldSymbol)
            Assert.True(field.IsConst)
            Assert.Equal(-2147483649, field.ConstantValue)
            Assert.Equal(TypeKind.Structure, field.Type.TypeKind)
            Assert.Equal("System.Int64", field.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            field = DirectCast(typeSymbol.GetMembers("field03").FirstOrDefault(), FieldSymbol)
            Assert.True(field.IsConst)
            Assert.Equal(CShort(-2), field.ConstantValue)
            Assert.Equal(TypeKind.Structure, field.Type.TypeKind)
            Assert.Equal("System.Int16", field.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            field = DirectCast(typeSymbol.GetMembers("field04").FirstOrDefault(), FieldSymbol)
            Assert.True(field.IsReadOnly)
            Assert.Equal(TypeKind.Class, field.Type.TypeKind)
            Assert.Equal("System.Object", field.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            field = DirectCast(typeSymbol.GetMembers("field05").FirstOrDefault(), FieldSymbol)
            Assert.True(field.IsReadOnly)
            Assert.Equal(TypeKind.Delegate, field.Type.TypeKind)
            Assert.Equal("System.Func(Of System.String, System.UInt64)", field.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            Dim expectedInitializers As IEnumerable(Of ExpectedInitializer) =
            New ExpectedInitializer() {New ExpectedInitializer("field01", "New Double()", lineNumber:=2)}

            Dim boundInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.StaticInitializers)
            CheckBoundInitializers(expectedInitializers, tree, boundInitializers, isStatic:=True)

            expectedInitializers = New ExpectedInitializer() {
                           New ExpectedInitializer("field04", "F(Nothing)", lineNumber:=4),
                           New ExpectedInitializer("field05", "New Func(Of String, ULong)(Function(s) s.Length)", lineNumber:=4)}

            boundInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.InstanceInitializers)
            CheckBoundInitializers(expectedInitializers, tree, boundInitializers, isStatic:=False)

        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact>
        Public Sub Bug5181()
            Dim source =
                <compilation name="Bug5181_1">
                    <file name="a.b">
                        Class Class1
                            Public Shared A As Integer = 10
                            Public B As Integer = 10 + A + Me.F()
                            Public C As Func(Of Integer, Integer) = Function(p) 10 + A + p + Me.F()

                            Public Function F() As Integer
                                Return 10 + A + Me.F()
                            End Function
                        End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim firstMeF = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleMemberAccessExpression, 1)
            Dim secondMeF = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleMemberAccessExpression, 2)
            Dim thirdMeF = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleMemberAccessExpression, 3)

            Dim firstMeFSymbol = model.GetSemanticInfoSummary(CType(firstMeF.AsNode(), ExpressionSyntax)).Symbol
            Dim secondMeFSymbol = model.GetSemanticInfoSummary(CType(secondMeF.AsNode(), ExpressionSyntax)).Symbol
            Dim thirdMeFSymbol = model.GetSemanticInfoSummary(CType(thirdMeF.AsNode(), ExpressionSyntax)).Symbol

            Assert.NotNull(firstMeFSymbol)
            Assert.NotNull(secondMeFSymbol)
            Assert.NotNull(thirdMeFSymbol)
            Assert.Equal(firstMeFSymbol, secondMeFSymbol)
            Assert.Equal(firstMeFSymbol, thirdMeFSymbol)

            Dim firstMe = tree.FindNodeOrTokenByKind(SyntaxKind.MeExpression, 1)
            Dim secondMe = tree.FindNodeOrTokenByKind(SyntaxKind.MeExpression, 2)
            Dim thirdMe = tree.FindNodeOrTokenByKind(SyntaxKind.MeExpression, 3)

            Dim firstMeSymbol = model.GetSemanticInfoSummary(CType(firstMe.AsNode(), ExpressionSyntax)).Symbol
            Dim secondMeSymbol = model.GetSemanticInfoSummary(CType(secondMe.AsNode(), ExpressionSyntax)).Symbol
            Dim thirdMeSymbol = model.GetSemanticInfoSummary(CType(thirdMe.AsNode(), ExpressionSyntax)).Symbol

            'Assert.Equal(1, firstMeSymbols.Count)   returned 0 symbols
            'Assert.Equal(1, secondMeSymbols.Count)
            'Assert.Equal(1, thirdMeSymbols.Count)
        End Sub

        <Fact>
        Public Sub Bug6935()
            Dim source =
                <compilation name="Bug6935">
                    <file name="a.b">
                        Class C2
                        End C2

                        Class Class1
                            Public C2 As New C2()

                            Public Function F() As Boolean
                                Return Me.C2 IsNot Nothing
                            End Function
                        End Class
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim firstMeF = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleMemberAccessExpression, 1)

            Dim firstMeFSymbol = model.GetSemanticInfoSummary(CType(firstMeF.AsNode(), ExpressionSyntax)).Symbol

            Assert.NotNull(firstMeFSymbol)
            Assert.Equal(firstMeFSymbol.Name, "C2")
        End Sub

        <WorkItem(542375, "DevDiv")>
        <Fact>
        Public Sub ConstFieldNonConstValueAsArrayBoundary()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation>
        <file name="a.vb">
            <![CDATA[ 
Imports Microsoft.VisualBasic

Module Module2
    Const x As Integer = AscW(y)
    Const y As String = ChrW(z)
    Dim z As Integer = 123

    Const Scen1 As Integer = z

    Sub Cnst100()
        Dim ArrScen9(Scen1) As Double
    End Sub
End Module
        ]]>
        </file>
    </compilation>)

            'BC30059: Constant expression is required.
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredConstExpr, "AscW(y)"),
                                            Diagnostic(ERRID.ERR_RequiredConstExpr, "ChrW(z)"),
                                            Diagnostic(ERRID.ERR_RequiredConstExpr, "z"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub StaticLocalFields()
            'As we can't easily get at fields which are non callable by user code such as
            'the $STATIC$Foo$001$a  we can simply determine that the count of items which
            'we expect is present
            Dim source = <compilation name="StaticLocals">
                             <file name="a.vb">
        Class C
            Private _Field as integer = 1

            Shared Sub Foo()
                static a as integer = 1
            End Sub
        End Class
            </file>
                         </compilation>

            Dim typeSymbol = CompileAndExtractTypeSymbol(source)

            'Perhaps we should verify that the members does not include the fields
            Assert.Equal(3, typeSymbol.GetMembers.Length)

            Dim Lst_members As New List(Of String)
            For Each i In typeSymbol.GetMembers
                Lst_members.Add(i.ToString)
            Next

            Assert.Contains("Private _Field As Integer", Lst_members)
            Assert.Contains("Public Sub New()", Lst_members)
            Assert.Contains("Public Shared Sub Foo()", Lst_members)
        End Sub

        <Fact>
        Public Sub VbConstantFields_Error()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
   <compilation>
       <file name="a.vb">
Interface I : End Interface
Interface II : Inherits I : End Interface
Interface III : Inherits II : End Interface
Interface IV : Inherits III : End Interface
Structure SomeStructure : Implements I : End Structure
Class SomeClass : Implements I : End Class
Class SomeClass2 : Inherits SomeClass : End Class

Class Clazz
    Public Const F = CType(CType(Nothing, SomeStructure), I)
    Public Const F2 = CType(CType(Nothing, Integer), Object)
End Class

Class Clazz(Of T As I)
    Public Const F = CType(CType(Nothing, T), I)
End Class

Class Clazz3(Of T As {Structure, I})
    Public Const F = CType(CType(Nothing, T), I)
End Class

Class Clazz6(Of T As {Structure})
    Public Const F2 = CType(CType(CType(CType(Nothing, T), Object), Object), Object) ' Dev11 does not generate error, but it should 
    Public Const F6 = CType(CType(CType(CType(Nothing, T), T), T), T) ' Dev11 does not generate error, but it should 
End Class

Class Clazz7(Of T)
    Public Const F = CType(CType(CType(CType(Nothing, T), Object), Object), Object) ' Dev11 does not generate error, but it should 
End Class

Class Clazz9(Of U As {Class, I}, V As U)
    Public Const F = CType(CType(CType(Nothing, U), V), I)
End Class

Class Clazz4
    Public Const F4 = CType(CType(CType(Nothing, SomeStructure), SomeStructure), SomeStructure)
End Class

Class ClazzDateTimeDecimal
    Public Const F2 = CType(#12:00:00 AM#, Object)
    Public Const F4 = CType(CType(Nothing, Date), Object)

    Public Const F6 = CType(1.2345D, Object)
    Public Const F8 = CType(CType(Nothing, Decimal), Object)
End Class

Class ClazzNullable
    Public Const F1 = CType(Nothing, Integer?)
    Public Const F2 = CType(CType(Nothing, Object), Integer?)
    Public Const F3 = CType(CType(1, Integer), Integer?)
    Public Const F4 As Integer? = Nothing
End Class

Class ClazzNullable(Of T As Structure)
    Public Const F1 = CType(Nothing, T?)
    Public Const F2 = CType(CType(Nothing, Object), T?)
    Public Const F4 As T? = Nothing
End Class

Enum EI : AI : BI : End Enum
Enum EB As Byte : AB : BB : End Enum

Class ClazzWithEnums
    Public Const F1 = CType(CType(CType(CType(Nothing, EI), Object), Object), Object)
    Public Const F3 = CType(CType(CType(CType(Nothing, EB), Object), Object), Object)
End Class

Class StringConstants
    Const a As Object = "1"
    Const b As System.Object = "1"
    Const c = "1"
End Class
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30060: Conversion from 'Object' to 'SomeStructure' cannot occur in a constant expression.
    Public Const F = CType(CType(Nothing, SomeStructure), I)
                                 ~~~~~~~
BC30060: Conversion from 'Integer' to 'Object' cannot occur in a constant expression.
    Public Const F2 = CType(CType(Nothing, Integer), Object)
                            ~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Object' to 'T' cannot occur in a constant expression.
    Public Const F = CType(CType(Nothing, T), I)
                                 ~~~~~~~
BC30060: Conversion from 'Object' to 'T' cannot occur in a constant expression.
    Public Const F = CType(CType(Nothing, T), I)
                                 ~~~~~~~
BC30060: Conversion from 'Object' to 'T' cannot occur in a constant expression.
    Public Const F2 = CType(CType(CType(CType(Nothing, T), Object), Object), Object) ' Dev11 does not generate error, but it should 
                                              ~~~~~~~
BC30060: Conversion from 'Object' to 'T' cannot occur in a constant expression.
    Public Const F6 = CType(CType(CType(CType(Nothing, T), T), T), T) ' Dev11 does not generate error, but it should 
                                              ~~~~~~~
BC30060: Conversion from 'Object' to 'T' cannot occur in a constant expression.
    Public Const F = CType(CType(CType(CType(Nothing, T), Object), Object), Object) ' Dev11 does not generate error, but it should 
                                             ~~~~~~~
BC30060: Conversion from 'U' to 'V' cannot occur in a constant expression.
    Public Const F = CType(CType(CType(Nothing, U), V), I)
                                 ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Object' to 'SomeStructure' cannot occur in a constant expression.
    Public Const F4 = CType(CType(CType(Nothing, SomeStructure), SomeStructure), SomeStructure)
                                        ~~~~~~~
BC30060: Conversion from 'Date' to 'Object' cannot occur in a constant expression.
    Public Const F2 = CType(#12:00:00 AM#, Object)
                            ~~~~~~~~~~~~~
BC30060: Conversion from 'Date' to 'Object' cannot occur in a constant expression.
    Public Const F4 = CType(CType(Nothing, Date), Object)
                            ~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Decimal' to 'Object' cannot occur in a constant expression.
    Public Const F6 = CType(1.2345D, Object)
                            ~~~~~~~
BC30060: Conversion from 'Decimal' to 'Object' cannot occur in a constant expression.
    Public Const F8 = CType(CType(Nothing, Decimal), Object)
                            ~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Object' to 'Integer?' cannot occur in a constant expression.
    Public Const F1 = CType(Nothing, Integer?)
                            ~~~~~~~
BC30060: Conversion from 'Object' to 'Integer?' cannot occur in a constant expression.
    Public Const F2 = CType(CType(Nothing, Object), Integer?)
                            ~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Integer' to 'Integer?' cannot occur in a constant expression.
    Public Const F3 = CType(CType(1, Integer), Integer?)
                            ~~~~~~~~~~~~~~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const F4 As Integer? = Nothing
                       ~~~~~~~~
BC30060: Conversion from 'Object' to 'T?' cannot occur in a constant expression.
    Public Const F1 = CType(Nothing, T?)
                            ~~~~~~~
BC30060: Conversion from 'Object' to 'T?' cannot occur in a constant expression.
    Public Const F2 = CType(CType(Nothing, Object), T?)
                            ~~~~~~~~~~~~~~~~~~~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public Const F4 As T? = Nothing
                       ~~
BC30060: Conversion from 'EI' to 'Object' cannot occur in a constant expression.
    Public Const F1 = CType(CType(CType(CType(Nothing, EI), Object), Object), Object)
                                        ~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'EB' to 'Object' cannot occur in a constant expression.
    Public Const F3 = CType(CType(CType(CType(Nothing, EB), Object), Object), Object)
                                        ~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'String' to 'Object' cannot occur in a constant expression.
    Const b As System.Object = "1"
                               ~~~
</expected>)
        End Sub

        Private Const s_ELEMENT_TYPE_U1 = 5
        Private Const s_ELEMENT_TYPE_I4 = 8
        Private Const s_ELEMENT_TYPE_VALUETYPE = 17
        Private Const s_ELEMENT_TYPE_CLASS = 18
        Private Const s_ELEMENT_TYPE_OBJECT = 28

        Private Const s_FIELD_SIGNATURE_CALLING_CONVENTION = 6

        Private ReadOnly _ZERO4 As Byte() = New Byte() {0, 0, 0, 0}
        Private ReadOnly _ONE4 As Byte() = New Byte() {1, 0, 0, 0}

        Private ReadOnly _ZERO1 As Byte() = New Byte() {0}
        Private ReadOnly _ONE1 As Byte() = New Byte() {1}

        <Fact>
        Public Sub VbConstantFields_NoError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
   <compilation>
       <file name="a.vb">
Interface I : End Interface
Interface II : Inherits I : End Interface
Interface III : Inherits II : End Interface
Interface IV : Inherits III : End Interface
Structure SomeStructure : Implements I : End Structure
Class SomeClass : Implements I : End Class
Class SomeClass2 : Inherits SomeClass : End Class

Class Clazz9(Of T As IV)
    Public Const F9 = CType(CType(CType(CType(Nothing, IV), III), II), I)
End Class

Class Clazz3
    Public Const F3 = CType(CType(CType(Nothing, SomeClass2), SomeClass), I)
    Public Const F33 = CType(CType(CType((((Nothing))), SomeClass2), SomeClass), I)
End Class

Class Clazz2(Of T As {Class, I})
    Public Const F22 = CType(CType(Nothing, T), I)  'Dev11 - error, Roslyn - OK
End Class

Class Clazz7(Of T As {Class})
    Public Const F7 = CType(CType(CType(CType(Nothing, T), Object), Object), Object)
End Class

Class Clazz8(Of T As {Class, IV})
    Public Const F8 = CType(CType(CType(CType(CType(Nothing, T), IV), III), II), I) 'Dev11 - error, Roslyn - OK
End Class
        </file>
   </compilation>, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim bytes = compilation.EmitToArray()
            Using md = ModuleMetadata.CreateFromImage(bytes)
                Dim reader = md.MetadataReader

                Assert.Equal(6, reader.GetTableRowCount(TableIndex.Constant))
                Assert.Equal(6, reader.FieldDefinitions.Count)

                For Each handle In reader.GetConstants()
                    Dim constant = reader.GetConstant(handle)
                    Dim field = reader.GetFieldDefinition(CType(constant.Parent, FieldDefinitionHandle))
                    Dim name = reader.GetString(field.Name)

                    Dim actual = reader.GetBlobBytes(constant.Value)
                    AssertEx.Equal(_ZERO4, actual)

                    Dim constType = constant.TypeCode

                    Select Case name
                        Case "F9", "F3", "F33", "F22", "F7", "F8"
                            Assert.Equal(s_ELEMENT_TYPE_CLASS, constType)

                        Case Else
                            Assert.True(False)
                    End Select

                Next
            End Using
        End Sub

        <Fact>
        Public Sub VbConstantFields_NoError_DateDecimal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
   <compilation>
       <file name="a.vb">
Class ClazzDateTimeDecimal
    Public Const F1 = #12:00:00 AM#
    Public Const F3 = CType(CType(#12:00:00 AM#, Date), Date)

    Public Const F5 = 1.2345D
    Public Const F7 = CType(CType(CType(1.2345D, Decimal), Decimal), Decimal)
End Class
        </file>
   </compilation>, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact>
        Public Sub VbConstantFields_Enum()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
   <compilation>
       <file name="a.vb">
Enum EI : AI : BI : End Enum
Enum EB As Byte : AB : BB : End Enum

Class Clazz
    Public Const F2 = CType(Nothing, EI)
    Public Const F4 = CType(Nothing, EB)
    Public Const F5 = EI.BI
    Public Const F6 = EB.BB
    Public Const F7 As EI = Nothing
    Public Const F8 As EB = Nothing
End Class
        </file>
   </compilation>, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim bytes = compilation.EmitToArray()

            Using md = ModuleMetadata.CreateFromImage(bytes)
                Dim reader = md.MetadataReader

                Assert.Equal(10, reader.GetTableRowCount(TableIndex.Constant))
                Assert.Equal(10 + 2, reader.FieldDefinitions.Count)

                For Each handle In reader.GetConstants()
                    Dim constant = reader.GetConstant(handle)
                    Dim field = reader.GetFieldDefinition(CType(constant.Parent, FieldDefinitionHandle))
                    Dim name = reader.GetString(field.Name)

                    Select Case name
                        Case "F1"
                            ' Constant: int32(0)
                            Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                            AssertEx.Equal(_ZERO4, reader.GetBlobBytes(constant.Value))
                            ' Field type: System.Object
                            AssertEx.Equal(New Byte() {s_FIELD_SIGNATURE_CALLING_CONVENTION, s_ELEMENT_TYPE_OBJECT},
                                           reader.GetBlobBytes(field.Signature))

                        Case "F2"
                            ' Constant: int32(0)
                            Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                            AssertEx.Equal(_ZERO4, reader.GetBlobBytes(constant.Value))
                            ' Field type: int32
                            AssertEx.Equal(New Byte() {s_FIELD_SIGNATURE_CALLING_CONVENTION, s_ELEMENT_TYPE_I4},
                                           reader.GetBlobBytes(field.Signature))

                        Case "F3"
                            ' Constant: uint8(0)
                            Assert.Equal(s_ELEMENT_TYPE_U1, constant.TypeCode)
                            AssertEx.Equal(_ZERO1, reader.GetBlobBytes(constant.Value))
                            ' Field type: System.Object
                            AssertEx.Equal(New Byte() {s_FIELD_SIGNATURE_CALLING_CONVENTION, s_ELEMENT_TYPE_OBJECT},
                                           reader.GetBlobBytes(field.Signature))

                        Case "F4"
                            ' Constant: uint8(0)
                            Assert.Equal(s_ELEMENT_TYPE_U1, constant.TypeCode)
                            AssertEx.Equal(_ZERO1, reader.GetBlobBytes(constant.Value))
                            ' Field type: uint8
                            AssertEx.Equal(New Byte() {s_FIELD_SIGNATURE_CALLING_CONVENTION, s_ELEMENT_TYPE_U1},
                                           reader.GetBlobBytes(field.Signature))

                        Case "F5"
                            ' Constant: int32(0)
                            Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                            AssertEx.Equal(_ONE4, reader.GetBlobBytes(constant.Value))
                            ' Field type: int32
                            AssertEx.Equal(New Byte() {s_FIELD_SIGNATURE_CALLING_CONVENTION, s_ELEMENT_TYPE_I4},
                                           reader.GetBlobBytes(field.Signature))

                        Case "F6"
                            ' Constant: uint8(0)
                            Assert.Equal(s_ELEMENT_TYPE_U1, constant.TypeCode)
                            AssertEx.Equal(_ONE1, reader.GetBlobBytes(constant.Value))
                            ' Field type: uint8
                            AssertEx.Equal(New Byte() {s_FIELD_SIGNATURE_CALLING_CONVENTION, s_ELEMENT_TYPE_U1},
                                           reader.GetBlobBytes(field.Signature))

                        Case "F7"
                            ' Constant: int32(0)
                            Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                            AssertEx.Equal(_ZERO4, reader.GetBlobBytes(constant.Value))
                            ' Field type: EI (valuetype)
                            AssertAnyValueType(reader.GetBlobBytes(field.Signature))

                        Case "F8"
                            ' Constant: uint8(0)
                            Assert.Equal(s_ELEMENT_TYPE_U1, constant.TypeCode)
                            AssertEx.Equal(_ZERO1, reader.GetBlobBytes(constant.Value))
                            ' Field type: EB (valuetype)
                            AssertAnyValueType(reader.GetBlobBytes(field.Signature))

                        Case "AI"
                            ' Constant: int32(0)
                            Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                            AssertEx.Equal(_ZERO4, reader.GetBlobBytes(constant.Value))
                            ' Field type: EI (valuetype)
                            AssertAnyValueType(reader.GetBlobBytes(field.Signature))

                        Case "BI"
                            ' Constant: int32(0)
                            Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                            AssertEx.Equal(_ONE4, reader.GetBlobBytes(constant.Value))
                            ' Field type: EI (valuetype)
                            AssertAnyValueType(reader.GetBlobBytes(field.Signature))

                        Case "AB"
                            ' Constant: uint8(0)
                            Assert.Equal(s_ELEMENT_TYPE_U1, constant.TypeCode)
                            AssertEx.Equal(_ZERO1, reader.GetBlobBytes(constant.Value))
                            ' Field type: EB (valuetype)
                            AssertAnyValueType(reader.GetBlobBytes(field.Signature))

                        Case "BB"
                            ' Constant: uint8(0)
                            Assert.Equal(s_ELEMENT_TYPE_U1, constant.TypeCode)
                            AssertEx.Equal(_ONE1, reader.GetBlobBytes(constant.Value))
                            ' Field type: EB (valuetype)
                            AssertAnyValueType(reader.GetBlobBytes(field.Signature))

                        Case Else
                            Assert.True(False)
                    End Select
                Next
            End Using
        End Sub

        Private Sub AssertAnyValueType(actual As Byte())
            Assert.Equal(3, actual.Length)
            Assert.Equal(s_FIELD_SIGNATURE_CALLING_CONVENTION, actual(0))
            Assert.Equal(s_ELEMENT_TYPE_VALUETYPE, actual(1))
        End Sub

        <Fact>
        Public Sub VbParameterDefaults_Error()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
   <compilation>
       <file name="a.vb">
Interface I : End Interface
Interface II : Inherits I : End Interface
Interface III : Inherits II : End Interface
Interface IV : Inherits III : End Interface
Structure SomeStructure : Implements I : End Structure
Class SomeClass : Implements I : End Class
Class SomeClass2 : Inherits SomeClass : End Class

Interface Clazz
    Sub s1(Optional F As Object = CType(CType(Nothing, SomeStructure), I))
    Sub s2(Optional F As I = CType(CType(Nothing, SomeStructure), I))
    Sub s3(Optional F As Object = CType(CType(CType(Nothing, SomeStructure), SomeStructure), SomeStructure))
End Interface

Interface Clazz(Of T As I)
    Sub s1(Optional F As Object = CType(CType(Nothing, T), I))
    Sub s2(Optional F As I = CType(CType(Nothing, T), I))
End Interface

Interface Clazz3(Of T As {Structure, I})
    Sub s1(Optional F As Object = CType(CType(Nothing, T), I))
    Sub s2(Optional F As I = CType(CType(Nothing, T), I))
End Interface

Interface Clazz6(Of T As Structure)
    Sub s1(Optional F As Object = CType(CType(CType(CType(Nothing, T), Object), Object), Object)) ' Dev11 - OK, Roslyn - ERROR
    Sub s2(Optional F As Object = CType(CType(CType(CType(Nothing, T), T), T), T)) ' Dev11 - OK, Roslyn - ERROR
End Interface

Interface Clazz7(Of T)
    Sub s1(Optional F As Object = CType(CType(CType(CType(Nothing, T), Object), Object), Object)) ' Dev11 - OK, Roslyn - ERROR
    Sub s2(Optional F As Object = CType(CType(CType(CType(Nothing, T), T), T), T)) ' Dev11 - OK, Roslyn - ERROR
End Interface

Interface Clazz9(Of U As {Class, I}, V As U)
    Sub s1(Optional F As Object = CType(CType(CType(Nothing, U), V), I))
    Sub s2(Optional F As I = CType(CType(CType(Nothing, U), V), I))
    Sub s3(Optional F As Object = CType(CType(CType(Nothing, V), U), I))
    Sub s4(Optional F As I = CType(CType(CType(Nothing, V), U), I))
End Interface

Interface ClazzMisc
    Sub s5(Optional F As Object = CType(Nothing, Integer?))
    Sub s8(Optional F As Object = CType(CType(Nothing, Object), Integer?))
    Sub s10(Optional F As Object = CType(CType(1, Integer), Integer?))
End Interface

Interface ClazzMisc(Of T As Structure)
    Sub s5(Optional F As Object = CType(Nothing, T?))
    Sub s8(Optional F As Object = CType(CType(Nothing, Object), T?))
End Interface

Enum EI : AI : BI : End Enum
Interface ClazzWithEnums
    Sub s7(Optional p7 As Object = CType(EI.BI, EI?))
End Interface
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30060: Conversion from 'SomeStructure' to 'I' cannot occur in a constant expression.
    Sub s1(Optional F As Object = CType(CType(Nothing, SomeStructure), I))
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'SomeStructure' to 'I' cannot occur in a constant expression.
    Sub s2(Optional F As I = CType(CType(Nothing, SomeStructure), I))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'SomeStructure' to 'Object' cannot occur in a constant expression.
    Sub s3(Optional F As Object = CType(CType(CType(Nothing, SomeStructure), SomeStructure), SomeStructure))
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'I' cannot occur in a constant expression.
    Sub s1(Optional F As Object = CType(CType(Nothing, T), I))
                                        ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'I' cannot occur in a constant expression.
    Sub s2(Optional F As I = CType(CType(Nothing, T), I))
                                   ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'I' cannot occur in a constant expression.
    Sub s1(Optional F As Object = CType(CType(Nothing, T), I))
                                        ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'I' cannot occur in a constant expression.
    Sub s2(Optional F As I = CType(CType(Nothing, T), I))
                                   ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'Object' cannot occur in a constant expression.
    Sub s1(Optional F As Object = CType(CType(CType(CType(Nothing, T), Object), Object), Object)) ' Dev11 - OK, Roslyn - ERROR
                                                    ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'Object' cannot occur in a constant expression.
    Sub s2(Optional F As Object = CType(CType(CType(CType(Nothing, T), T), T), T)) ' Dev11 - OK, Roslyn - ERROR
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'Object' cannot occur in a constant expression.
    Sub s1(Optional F As Object = CType(CType(CType(CType(Nothing, T), Object), Object), Object)) ' Dev11 - OK, Roslyn - ERROR
                                                    ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T' to 'Object' cannot occur in a constant expression.
    Sub s2(Optional F As Object = CType(CType(CType(CType(Nothing, T), T), T), T)) ' Dev11 - OK, Roslyn - ERROR
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'V' to 'I' cannot occur in a constant expression.
    Sub s1(Optional F As Object = CType(CType(CType(Nothing, U), V), I))
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'V' to 'I' cannot occur in a constant expression.
    Sub s2(Optional F As I = CType(CType(CType(Nothing, U), V), I))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'V' to 'U' cannot occur in a constant expression.
    Sub s3(Optional F As Object = CType(CType(CType(Nothing, V), U), I))
                                              ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'V' to 'U' cannot occur in a constant expression.
    Sub s4(Optional F As I = CType(CType(CType(Nothing, V), U), I))
                                         ~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Integer?' to 'Object' cannot occur in a constant expression.
    Sub s5(Optional F As Object = CType(Nothing, Integer?))
                                  ~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Integer?' to 'Object' cannot occur in a constant expression.
    Sub s8(Optional F As Object = CType(CType(Nothing, Object), Integer?))
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'Integer?' to 'Object' cannot occur in a constant expression.
    Sub s10(Optional F As Object = CType(CType(1, Integer), Integer?))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T?' to 'Object' cannot occur in a constant expression.
    Sub s5(Optional F As Object = CType(Nothing, T?))
                                  ~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'T?' to 'Object' cannot occur in a constant expression.
    Sub s8(Optional F As Object = CType(CType(Nothing, Object), T?))
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'EI?' to 'Object' cannot occur in a constant expression.
    Sub s7(Optional p7 As Object = CType(EI.BI, EI?))
                                   ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub VbParameterDefaults_NoError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
   <compilation>
       <file name="a.vb">
Interface I : End Interface
Interface II : Inherits I : End Interface
Interface III : Inherits II : End Interface
Interface IV : Inherits III : End Interface
Structure SomeStructure : Implements I : End Structure
Class SomeClass : Implements I : End Class
Class SomeClass2 : Inherits SomeClass : End Class

Interface Clazz
    Sub s1(Optional F1 As SomeStructure = CType(CType(CType(Nothing, SomeStructure), SomeStructure), SomeStructure)) ' Dev11 - ERROR, Roslyn - OK
    Sub s2(Optional F2 As Object = CType(CType(Nothing, Integer), Object)) ' Dev11 - ERROR, Roslyn - OK
    Sub s3(Optional F3 As Object = CType(CType(CType(Nothing, SomeClass2), SomeClass), I))
    Sub s4(Optional F4 As I = CType(CType(CType(Nothing, SomeClass2), SomeClass), I))
    Sub s5(Optional F5 As Object = CType(CType(CType((((Nothing))), SomeClass2), SomeClass), I))
End Interface

Interface Clazz2(Of T As {Class, I})
    Sub s1(Optional F6 As Object = CType(CType(Nothing, T), I)) ' Dev11 - ERROR, Roslyn - OK
    Sub s2(Optional F7 As I = CType(CType(Nothing, T), I)) ' Dev11 - ERROR, Roslyn - OK
    Sub s3(Optional F8 As Object = CType(CType(CType(CType(Nothing, T), Object), Object), Object))
End Interface

Interface Clazz2x(Of T As {Class, Iv})
    Sub s1(Optional F9 As Object = CType(CType(CType(CType(CType(Nothing, T), IV), III), II), I)) ' Dev11 - ERROR, Roslyn - OK
    Sub s2(Optional F10 As I = CType(CType(CType(CType(CType(Nothing, T), IV), III), II), I)) ' Dev11 - ERROR, Roslyn - OK
End Interface

Interface Clazz6(Of T As Structure)
    Sub s3(Optional F11 As T = CType(CType(CType(CType(Nothing, T), T), T), T))
End Interface

Interface Clazz7(Of T)
    Sub s3(Optional F12 As T = CType(CType(CType(CType(Nothing, T), T), T), T))
End Interface

Interface Clazz9(Of T As IV)
    Sub s1(Optional F13 As Object = CType(CType(CType(CType(Nothing, IV), III), II), I))
    Sub s2(Optional F14 As I = CType(CType(CType(CType(Nothing, IV), III), II), I))
End Interface

Interface ClazzMisc
    Sub s1(Optional F15 As Object = CType(#12:00:00 AM#, Object)) ' Dev11 - ERROR, Roslyn - OK
    Sub s2(Optional F16 As Object = CType(CType(Nothing, Date), Object)) ' Dev11 - ERROR, Roslyn - OK

    Sub s3(Optional F17 As Object = CType(1.2345D, Object)) ' Dev11 - ERROR, Roslyn - OK
    Sub s4(Optional F18 As Object = CType(CType(Nothing, Decimal), Object)) ' Dev11 - ERROR, Roslyn - OK

    Sub s6(Optional F19 As Integer? = CType(Nothing, Integer?)) ' Dev11 - ERROR, Roslyn - OK
    Sub s7(Optional F20 As Integer? = Nothing)
    Sub s9(Optional F21 As Integer? = CType(CType(Nothing, Object), Integer?)) ' Dev11 - ERROR, Roslyn - OK
    Sub s11(Optional F22 As Integer? = CType(CType(1, Integer), Integer?)) ' Dev11 - ERROR, Roslyn - OK
    Sub s12(Optional F23 As Integer? = 1)
    Sub s13(Optional F24 As Integer? = CType(1, Integer?)) ' Dev11 - ERROR, Roslyn - OK
    Sub s14(Optional F25 As Integer? = CType(1, Integer))
End Interface

Interface ClazzMisc(Of T As Structure)
    Sub s6(Optional F26 As T? = CType(Nothing, T?)) ' Dev11 - ERROR, Roslyn - OK
    Sub s7(Optional F27 As T? = Nothing)
    Sub s9(Optional F28 As T? = CType(CType(Nothing, Object), T?)) ' Dev11 - ERROR, Roslyn - OK
End Interface

Enum EI : AI : BI : End Enum

Interface ClazzWithEnums
    Sub s1(Optional F30 As Object = CType(CType(CType(CType(Nothing, EI), Object), Object), Object)) 'Dev11 - ERROR, Roslyn - OK, Int constant!!!
    Sub s2(Optional F31 As Object = CType(Nothing, EI)) ' Int constant!!!
    Sub s3(Optional F32 As EI = CType(Nothing, EI))
    Sub s4(Optional F33 As EI? = CType(Nothing, EI))
    Sub s5(Optional F34 As EI? = CType(Nothing, EI?)) 'Dev11 - ERROR, Roslyn - OK
    Sub s6(Optional F35 As Object = EI.BI) 'Int constant!!!
    Sub s8(Optional F36 As Object = CType(EI.BI, Object)) 'Int constant!!!
    Sub s9(Optional F37 As EI? = CType(EI.BI, EI?)) 'Dev11 - ERROR, Roslyn - OK
    Sub s10(Optional F38 As EI? = EI.BI)
    Sub s11(Optional F39 As EI? = Nothing)
    Sub s12(Optional F40 As EI? = CType(CType(CType(Nothing, Integer), Byte), EI))
    Sub s13(Optional F41 As EI? = CType(CType(CType(EI.BI, Integer), Byte), EI))
End Interface
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim bytes = compilation.EmitToArray()
            Using md = ModuleMetadata.CreateFromImage(bytes)
                Dim reader = md.MetadataReader

                Const FIELD_COUNT = 40
                Const ATTR_CONST_COUNT = 4
                Const ENUM_CONST_COUNT = 2

                Assert.Equal(FIELD_COUNT - ATTR_CONST_COUNT + ENUM_CONST_COUNT, reader.GetTableRowCount(TableIndex.Constant))
                Assert.Equal(FIELD_COUNT, reader.GetTableRowCount(TableIndex.Param))

                For Each handle In reader.GetConstants()
                    Dim constant = reader.GetConstant(handle)

                    If constant.Parent.Kind = HandleKind.Parameter Then
                        Dim paramRow = reader.GetParameter(CType(constant.Parent, ParameterHandle))
                        Dim name = reader.GetString(paramRow.Name)

                        Select Case name
                            Case "F1", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                                 "F13", "F14", "F19", "F20", "F21", "F26", "F27", "F28", "F34", "F39"
                                ' Constant: nullref
                                Assert.Equal(s_ELEMENT_TYPE_CLASS, constant.TypeCode)
                                AssertEx.Equal(_ZERO4, reader.GetBlobBytes(constant.Value))

                            Case "F2", "F30", "F31", "F32", "F33", "F40"
                                ' Constant: int32(0)
                                Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                                AssertEx.Equal(_ZERO4, reader.GetBlobBytes(constant.Value))

                            Case "F22", "F23", "F24", "F25", "F35", "F36", "F37", "F38", "F41"
                                ' Constant: int32(1)
                                Assert.Equal(s_ELEMENT_TYPE_I4, constant.TypeCode)
                                AssertEx.Equal(_ONE4, reader.GetBlobBytes(constant.Value))

                            Case Else
                                Assert.True(False, "Unknown field: " + name)
                        End Select
                    End If
                Next

                For Each paramDef In reader.GetParameters()
                    Dim name = reader.GetString(reader.GetParameter(paramDef).Name)
                    ' Just make sure we have attributes on F15, F16, F17, F18
                    Select Case name
                        Case "F15", "F16", "F17", "F18"
                            Assert.True(HasAnyCustomAttribute(reader, paramDef))
                    End Select
                Next

            End Using
        End Sub

        Private Function HasAnyCustomAttribute(reader As MetadataReader, parent As EntityHandle) As Boolean
            For Each ca In reader.CustomAttributes
                If reader.GetCustomAttribute(ca).Parent = parent Then
                    Return True
                End If
            Next
            Return False
        End Function

        <Fact()>
        Public Sub ConstantOfWrongType()
            Dim ilSource = <![CDATA[
.class public auto ansi Clazz
       extends [mscorlib]System.Object
{
  .field public static literal object a = int32(0x00000001)
  .field public static literal object b = "abc"
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method Clazz::.ctor
}
]]>

            Dim vbSource =
<compilation>
    <file name="a.vb">
Imports System
Module C
  Sub S()
    Console.WriteLine(Clazz.a.ToString())
    Console.WriteLine(Clazz.b.ToString())
  End Sub
End Module
    </file>
</compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource.Value)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30799: Field 'Clazz.a' has an invalid constant value.
    Console.WriteLine(Clazz.a.ToString())
                      ~~~~~~~
BC30799: Field 'Clazz.b' has an invalid constant value.
    Console.WriteLine(Clazz.b.ToString())
                      ~~~~~~~
</errors>)
        End Sub

        <Fact, WorkItem(1028, "https://github.com/dotnet/roslyn/issues/1028")>
        Public Sub WriteOfReadonlySharedMemberOfAnotherInstantiation01()
            Dim source = <compilation>
                             <file name="a.vb">
Class Foo(Of T)
    Shared Sub New()
        Foo(Of Integer).X = 12
        Foo(Of Integer).Y = 12
        Foo(Of T).X = 12
        Foo(Of T).Y = 12
    End Sub

    Public Shared ReadOnly X As Integer
    Public Shared ReadOnly Property Y As Integer = 0
End Class
                             </file>
                         </compilation>

            Dim standardCompilation = CompilationUtils.CreateCompilationWithMscorlib(source, TestOptions.ReleaseDll)
            Dim strictCompilation = CompilationUtils.CreateCompilationWithMscorlib(source, TestOptions.ReleaseDll,
                                                                                   parseOptions:=TestOptions.Regular.WithStrictFeature())

            CompilationUtils.AssertTheseDiagnostics(standardCompilation, <expected>
BC30526: Property 'Y' is 'ReadOnly'.
        Foo(Of Integer).Y = 12
        ~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(strictCompilation, <expected>
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        Foo(Of Integer).X = 12
        ~~~~~~~~~~~~~~~~~
BC30526: Property 'Y' is 'ReadOnly'.
        Foo(Of Integer).Y = 12
        ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(1028, "https://github.com/dotnet/roslyn/issues/1028")>
        Public Sub WriteOfReadonlySharedMemberOfAnotherInstantiation02()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Sub Main()
        Console.WriteLine(Foo(Of Long).X)
        Console.WriteLine(Foo(Of Integer).X)
        Console.WriteLine(Foo(Of String).X)
        Console.WriteLine(Foo(Of Integer).X)
    End Sub
End Module

Public Class Foo(Of T)
    Shared Sub New()
        Console.WriteLine("Initializing for {0}", GetType(T))
        Foo(Of Integer).X = GetType(T).Name
    End Sub

    Public Shared ReadOnly X As String
End Class
    </file>
</compilation>,
verify:=False,
expectedOutput:=<![CDATA[Initializing for System.Int64
Initializing for System.Int32

Int64
Initializing for System.String

String
]]>)
        End Sub

#Region "Helpers"
        Private Shared Function CompileAndExtractTypeSymbol(sources As Xml.Linq.XElement, Optional typeName As String = "C") As SourceNamedTypeSymbol
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(sources)
            Dim typeSymbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetTypeMembers(typeName).Single(), SourceNamedTypeSymbol)
            Return typeSymbol
        End Function

        Private Shared Function GetMember(sources As Xml.Linq.XElement, fieldName As String, Optional typeName As String = "C") As Symbol
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(sources)
            Dim symbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetTypeMembers(typeName).Single.GetMembers(fieldName).Single(), Symbol)
            Return symbol
        End Function

        Private Shared Function HasSynthesizedStaticConstructor(typeSymbol As NamedTypeSymbol) As Boolean
            For Each member In typeSymbol.GetMembers(WellKnownMemberNames.StaticConstructorName)
                If member.IsImplicitlyDeclared Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Shared Function IsBeforeFieldInit(typeSymbol As NamedTypeSymbol) As Boolean
            Return (DirectCast(typeSymbol, Microsoft.Cci.ITypeDefinition)).IsBeforeFieldInit
        End Function

        Private Shared Function IsStatic(symbol As Symbol) As Boolean
            Return (DirectCast(symbol, Microsoft.Cci.IFieldDefinition)).IsStatic
        End Function

        Private Shared Sub CompileAndCheckInitializers(sources As Xml.Linq.XElement, expectedInstanceInitializers As IEnumerable(Of ExpectedInitializer), expectedStaticInitializers As IEnumerable(Of ExpectedInitializer))
            '
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(sources)
            Dim typeSymbol = DirectCast(compilation.SourceModule.GlobalNamespace.GetTypeMembers("C").Single(), SourceNamedTypeSymbol)
            Dim syntaxTree = compilation.SyntaxTrees.First()
            Dim boundInstanceInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.InstanceInitializers)
            CheckBoundInitializers(expectedInstanceInitializers, syntaxTree, boundInstanceInitializers, isStatic:=False)

            Dim boundStaticInitializers = BindInitializersWithoutDiagnostics(typeSymbol, typeSymbol.StaticInitializers)
            CheckBoundInitializers(expectedStaticInitializers, syntaxTree, boundStaticInitializers, isStatic:=True)
        End Sub

        Private Shared Sub CheckBoundInitializers(expectedInitializers As IEnumerable(Of ExpectedInitializer), syntaxTree As SyntaxTree, boundInitializers As ImmutableArray(Of BoundInitializer), isStatic As Boolean)
            If expectedInitializers Is Nothing Then
                Assert.[True](boundInitializers.IsEmpty)
            Else
                Assert.[True](Not boundInitializers.IsDefault)
                Dim numInitializers As Integer = expectedInitializers.Count()
                Assert.Equal(numInitializers, boundInitializers.Length)
                Dim i As Integer = 0
                For Each expectedInitializer In expectedInitializers
                    Dim boundInit = boundInitializers(i)
                    i += 1
                    Assert.[True](boundInit.Kind = BoundKind.FieldInitializer OrElse boundInit.Kind = BoundKind.PropertyInitializer)
                    Dim boundFieldInit = DirectCast(boundInit, BoundFieldOrPropertyInitializer)
                    Dim initValueSyntax = boundFieldInit.InitialValue.Syntax
                    If boundInit.Syntax.Kind <> SyntaxKind.AsNewClause Then
                        Assert.Same(initValueSyntax.Parent, boundInit.Syntax)
                        Assert.Equal(expectedInitializer.InitialValue, initValueSyntax.ToString())
                    End If
                    Dim initValueLineNumber = syntaxTree.GetLineSpan(initValueSyntax.Span).StartLinePosition.Line
                    Assert.Equal(expectedInitializer.LineNumber, initValueLineNumber)
                    Dim fieldSymbol As Symbol
                    If boundInit.Kind = BoundKind.FieldInitializer Then
                        fieldSymbol = DirectCast(boundFieldInit, BoundFieldInitializer).InitializedFields.First
                    Else
                        fieldSymbol = DirectCast(boundFieldInit, BoundPropertyInitializer).InitializedProperties.First
                    End If
                    Assert.Equal(expectedInitializer.FieldName, fieldSymbol.Name)

                    Dim boundReceiver As BoundExpression
                    Select Case boundFieldInit.MemberAccessExpressionOpt.Kind
                        Case BoundKind.PropertyAccess
                            boundReceiver = DirectCast(boundFieldInit.MemberAccessExpressionOpt, BoundPropertyAccess).ReceiverOpt
                        Case BoundKind.FieldAccess
                            boundReceiver = DirectCast(boundFieldInit.MemberAccessExpressionOpt, BoundFieldAccess).ReceiverOpt
                        Case Else
                            Throw TestExceptionUtilities.UnexpectedValue(boundFieldInit.MemberAccessExpressionOpt.Kind)
                    End Select

                    Assert.Equal(BoundKind.FieldAccess, boundFieldInit.MemberAccessExpressionOpt.Kind)

                    If isStatic Then
                        Assert.Null(boundReceiver)
                    Else
                        Assert.Equal(BoundKind.MeReference, boundReceiver.Kind)
                    End If
                Next
            End If
        End Sub

        Private Shared Function BindInitializersWithoutDiagnostics(typeSymbol As SourceNamedTypeSymbol, initializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer))) As ImmutableArray(Of BoundInitializer)
            Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()
            Dim processedFieldInitializers = Binder.BindFieldAndPropertyInitializers(typeSymbol, initializers, Nothing, diagnostics)
            Dim sealedDiagnostics = diagnostics.ToReadOnlyAndFree()
            For Each d In sealedDiagnostics
                Console.WriteLine(d)
            Next
            Assert.False(sealedDiagnostics.Any())
            Return processedFieldInitializers
        End Function

        Public Class ExpectedInitializer

            Public Property FieldName As String

            Public Property InitialValue As String

            Public Property LineNumber As Integer

            Public Sub New(fieldName As String, initialValue As String, lineNumber As Integer)
                Me.FieldName = fieldName
                Me.InitialValue = initialValue
                Me.LineNumber = lineNumber
            End Sub

        End Class

#End Region

    End Class
End Namespace
