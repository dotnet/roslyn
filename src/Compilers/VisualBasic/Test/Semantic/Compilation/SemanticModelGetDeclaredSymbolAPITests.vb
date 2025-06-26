' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class Binder_Statements_Tests
        Inherits SemanticModelTestBase

#Region "GetDeclaredSymbol Function"

        <Fact()>
        Public Sub LocalSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilation()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Dim x As Integer = 0
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)

            Dim model1 = comp.GetSemanticModel(tree)
            Dim model2 = comp.GetSemanticModel(tree)
            Assert.NotEqual(model1, model2)

            Dim vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(vardecl)
            Dim symbol2 = model2.GetDeclaredSymbol(vardecl)

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.Equal(symbol1, symbol2)
        End Sub

        <WorkItem(749753, "DevDiv2/DevDiv")>
        <Fact()>
        Public Sub TopLevelSub()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
Namespace MyNS2
End Namespace
Sub goo()
End Sub
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)
            Dim model1 = comp.GetSemanticModel(tree)

            Dim memberSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodStatementSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(memberSyntax)
            Assert.NotNull(symbol1)
            Assert.Equal("goo", symbol1.Name)
        End Sub

        <Fact()>
        Public Sub SubInNamespace()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
Namespace MyNS2
Sub goo()
End Sub
End Namespace
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)
            Dim model1 = comp.GetSemanticModel(tree)

            Dim memberSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodStatementSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(memberSyntax)
            Assert.Equal("goo", symbol1.Name)
        End Sub

        <Fact()>
        Public Sub SubInNamespaceWithRootNamespace()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
Namespace MyNS2
Sub goo()
End Sub
End Namespace
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml, options:=TestOptions.ReleaseDll.WithRootNamespace("Pavement"))
            Dim tree = comp.SyntaxTrees(0)
            Dim model1 = comp.GetSemanticModel(tree)

            Dim memberSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodStatementSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(memberSyntax)
            Assert.Equal("goo", symbol1.Name)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LocalSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilationStaticLocal()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Static x As Integer = 0
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)

            Dim model1 = comp.GetSemanticModel(tree)
            Dim model2 = comp.GetSemanticModel(tree)
            Assert.NotEqual(model1, model2)

            Dim vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(vardecl)
            Dim symbol2 = model2.GetDeclaredSymbol(vardecl)

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.Equal(symbol1, symbol2)
        End Sub

        <Fact()>
        Public Sub LocalSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilations()
            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Dim x As Integer = 0
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40(xml)

            Dim tree1 = comp1.SyntaxTrees(0)
            Dim tree2 = comp2.SyntaxTrees(0)

            Dim model1 = comp1.GetSemanticModel(tree1)
            Dim model2 = comp2.GetSemanticModel(tree2)
            Assert.NotEqual(model1, model2)

            Dim vardecl1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(vardecl1)

            Dim vardecl2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim symbol2 = model2.GetDeclaredSymbol(vardecl2)

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.NotEqual(symbol1, symbol2)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LocalSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilationsStaticLocal()
            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Static x As Integer = 0
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40(xml)

            Dim tree1 = comp1.SyntaxTrees(0)
            Dim tree2 = comp2.SyntaxTrees(0)

            Dim model1 = comp1.GetSemanticModel(tree1)
            Dim model2 = comp2.GetSemanticModel(tree2)
            Assert.NotEqual(model1, model2)

            Dim vardecl1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim symbol1 = model1.GetDeclaredSymbol(vardecl1)

            Dim vardecl2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().First()
            Dim symbol2 = model2.GetDeclaredSymbol(vardecl2)

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.NotEqual(symbol1, symbol2)
        End Sub

        <Fact()>
        Public Sub LambdaParameterSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilation()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Dim f As Func(Of Integer, Integer) = Function(p) p
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)

            Dim model1 = comp.GetSemanticModel(tree)
            Dim model2 = comp.GetSemanticModel(tree)
            Assert.NotEqual(model1, model2)

            Dim paramRef = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(ins) ins.ToString() = "p")
            Dim symbol1 = model1.GetSemanticInfoSummary(paramRef).Symbol
            Dim symbol2 = model2.GetSemanticInfoSummary(paramRef).Symbol

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.Equal(symbol1, symbol2)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LambdaParameterSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilationStaticLocal()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Static f As Func(Of Integer, Integer) = Function(p) p
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)

            Dim model1 = comp.GetSemanticModel(tree)
            Dim model2 = comp.GetSemanticModel(tree)
            Assert.NotEqual(model1, model2)

            Dim paramRef = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(ins) ins.ToString() = "p")
            Dim symbol1 = model1.GetSemanticInfoSummary(paramRef).Symbol
            Dim symbol2 = model2.GetSemanticInfoSummary(paramRef).Symbol

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.Equal(symbol1, symbol2)
        End Sub

        <Fact()>
        Public Sub LambdaParameterSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilations()

            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Dim f As Func(Of Integer, Integer) = Function(p) p
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree1 = comp1.SyntaxTrees(0)
            Dim tree2 = comp2.SyntaxTrees(0)

            Dim model1 = comp1.GetSemanticModel(tree1)
            Dim model2 = comp2.GetSemanticModel(tree2)
            Assert.NotEqual(model1, model2)

            Dim paramRef1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(ins) ins.ToString() = "p")
            Dim symbol1 = model1.GetSemanticInfoSummary(paramRef1).Symbol

            Dim paramRef2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(ins) ins.ToString() = "p")
            Dim symbol2 = model2.GetSemanticInfoSummary(paramRef2).Symbol

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.NotEqual(symbol1, symbol2)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LambdaParameterSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilationsStaticLocal()
            Dim xml =
                <compilation name="TST">
                    <file name="C.vb">
                        Public Class C
                           Public Sub M()
                              Static f As Func(Of Integer, Integer) = Function(p) p
                           End Sub
                        End Public
                    </file>
                </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree1 = comp1.SyntaxTrees(0)
            Dim tree2 = comp2.SyntaxTrees(0)

            Dim model1 = comp1.GetSemanticModel(tree1)
            Dim model2 = comp2.GetSemanticModel(tree2)
            Assert.NotEqual(model1, model2)

            Dim paramRef1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(ins) ins.ToString() = "p")
            Dim symbol1 = model1.GetSemanticInfoSummary(paramRef1).Symbol

            Dim paramRef2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(ins) ins.ToString() = "p")
            Dim symbol2 = model2.GetSemanticInfoSummary(paramRef2).Symbol

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.NotEqual(symbol1, symbol2)
        End Sub

        <WorkItem(539707, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539707")>
        <Fact()>
        Public Sub GetDeclaredSymbolSameForStatementOrBlock()

            Dim xml =
                <compilation>
                    <file name="a.vb">
                        Namespace N1

                            Interface I1
                                Sub M1()
                            End Interface 

                            Enum E1
                                m1
                                m2
                            End Enum

                            Structure S1
                                Public i as integer
                            End Structure

                            Class C1
                                Public Function F() as integer
                                    return 0
                                End Function

                                Public Sub S()
                                End Sub

                                Public ReadOnly Property P as string
                                    Get
                                        return nothing
                                    End Get
                                End Property

                                Public Sub New()
                                End Sub
                            End Class

                            Module Program  
                                Sub main()
                                End Sub 
                            End Module

                        End Namespace

                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(xml)
            Dim diag = comp.GetDiagnostics()
            Debug.Assert(diag.Length = 0)
            Dim tree = comp.SyntaxTrees(0)

            Dim model1 = comp.GetSemanticModel(tree)

            ' Test NamespaceBlockSyntax and NamespaceStatementSyntax
            Dim n1Syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of NamespaceBlockSyntax)().First()
            Dim sym1 = model1.GetDeclaredSymbol(DirectCast(n1Syntax, VisualBasicSyntaxNode))
            Assert.Equal("N1", sym1.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Dim sym2 = model1.GetDeclaredSymbol(DirectCast(n1Syntax.NamespaceStatement, VisualBasicSyntaxNode))
            Assert.Equal(sym1, sym2)

            ' Test TypeBlocks in the Namespace (Interface, Class, Structure, Module)
            Dim typeBlocks = n1Syntax.DescendantNodes().OfType(Of TypeBlockSyntax)()
            For Each tb In typeBlocks
                sym1 = model1.GetDeclaredSymbol(DirectCast(tb, VisualBasicSyntaxNode))
                sym2 = model1.GetDeclaredSymbol(DirectCast(tb.BlockStatement, VisualBasicSyntaxNode))
                Assert.Equal(sym1.ToDisplayString(SymbolDisplayFormat.TestFormat), sym2.ToDisplayString(SymbolDisplayFormat.TestFormat))
                Assert.Equal(sym1, sym2)
            Next

            ' Test EnumBlockSyntax and EnumStatementSyntax
            Dim e1Syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of EnumBlockSyntax)().First()
            sym1 = model1.GetDeclaredSymbol(DirectCast(e1Syntax, VisualBasicSyntaxNode))
            Assert.Equal("N1.E1", sym1.ToDisplayString(SymbolDisplayFormat.TestFormat))

            sym2 = model1.GetDeclaredSymbol(DirectCast(e1Syntax.EnumStatement, VisualBasicSyntaxNode))
            Assert.Equal(sym1, sym2)

            ' Test ClassBlockSyntax and ClassStatementSyntax
            Dim c1Syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            sym1 = model1.GetDeclaredSymbol(DirectCast(c1Syntax, VisualBasicSyntaxNode))
            Assert.Equal("N1.C1", sym1.ToDisplayString(SymbolDisplayFormat.TestFormat))

            sym2 = model1.GetDeclaredSymbol(DirectCast(c1Syntax.BlockStatement, VisualBasicSyntaxNode))
            Assert.Equal(sym1, sym2)

            ' Test MethodBlock Members of C1
            Dim methodBlocks = c1Syntax.DescendantNodes().OfType(Of MethodBlockSyntax)()
            For Each mb In methodBlocks
                sym1 = model1.GetDeclaredSymbol(DirectCast(mb, VisualBasicSyntaxNode))
                sym2 = model1.GetDeclaredSymbol(DirectCast(mb.BlockStatement, VisualBasicSyntaxNode))
                Assert.Equal(sym1.ToDisplayString(SymbolDisplayFormat.TestFormat), sym2.ToDisplayString(SymbolDisplayFormat.TestFormat))
                Assert.Equal(sym1, sym2)
            Next

            ' Test PropertyBlock Members of C1
            Dim propertyBlocks = c1Syntax.DescendantNodes().OfType(Of PropertyBlockSyntax)()
            For Each pb In propertyBlocks
                sym1 = model1.GetDeclaredSymbol(DirectCast(pb, VisualBasicSyntaxNode))
                sym2 = model1.GetDeclaredSymbol(DirectCast(pb.PropertyStatement, VisualBasicSyntaxNode))
                Assert.Equal(sym1.ToDisplayString(SymbolDisplayFormat.TestFormat), sym2.ToDisplayString(SymbolDisplayFormat.TestFormat))
                Assert.Equal(sym1, sym2)
            Next

        End Sub

        ' Ensure local symbols gotten from same Binding instance are same object.
        <Fact()>
        Public Sub GetSemanticInfoLocalVariableTwice()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Imports System   

Class B
    Public f1 as Integer
End Class

Class M
    Public Sub Main()
        Dim bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1) 'BIND:"bInstance"
    End Sub
    End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace
            Dim classB = DirectCast(globalNS.GetMembers("B").Single(), NamedTypeSymbol)
            Dim fieldF1 = DirectCast(classB.GetMembers("f1").Single(), FieldSymbol)

            Dim node As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim bindInfo1 As SemanticInfoSummary = semanticModel.GetSemanticInfoSummary(DirectCast(node, ExpressionSyntax))
            Dim bindInfo2 As SemanticInfoSummary = semanticModel.GetSemanticInfoSummary(DirectCast(node, ExpressionSyntax))
            Assert.Equal(SymbolKind.Local, bindInfo1.Symbol.Kind)
            Assert.Equal("bInstance", bindInfo1.Symbol.Name)
            Assert.Equal("B", bindInfo1.Type.ToTestDisplayString())
            Assert.Same(bindInfo1.Symbol, bindInfo2.Symbol)
            Assert.Same(bindInfo1.Type, bindInfo2.Type)

            Dim Syntax As ModifiedIdentifierSyntax = Nothing
            Dim varSymbol = GetVariableSymbol(compilation, semanticModel, "a.vb", "bInstance", Syntax)
            Assert.Same(bindInfo1.Symbol, varSymbol)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        ' Ensure local symbols gotten from same Binding instance are same object.
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub GetSemanticInfoLocalVariableTwiceStaticLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Imports System   

Class B
    Public f1 as Integer
End Class

Class M
    Public Sub Main()
        Static bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1) 'BIND:"bInstance"
    End Sub
    End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace
            Dim classB = DirectCast(globalNS.GetMembers("B").Single(), NamedTypeSymbol)
            Dim fieldF1 = DirectCast(classB.GetMembers("f1").Single(), FieldSymbol)

            Dim node As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim bindInfo1 As SemanticInfoSummary = semanticModel.GetSemanticInfoSummary(DirectCast(node, ExpressionSyntax))
            Dim bindInfo2 As SemanticInfoSummary = semanticModel.GetSemanticInfoSummary(DirectCast(node, ExpressionSyntax))
            Assert.Equal(SymbolKind.Local, bindInfo1.Symbol.Kind)
            Assert.Equal("bInstance", bindInfo1.Symbol.Name)
            Assert.Equal("B", bindInfo1.Type.ToTestDisplayString())
            Assert.Same(bindInfo1.Symbol, bindInfo2.Symbol)
            Assert.Same(bindInfo1.Type, bindInfo2.Type)

            Dim Syntax As ModifiedIdentifierSyntax = Nothing
            Dim varSymbol = GetVariableSymbol(compilation, semanticModel, "a.vb", "bInstance", Syntax)
            Assert.Same(bindInfo1.Symbol, varSymbol)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(540580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540580")>
        <Fact()>
        Public Sub GetSemanticInfoInsideType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Imports System
Class C
    Public Property P As Func(Of Func(Of A.B.String))
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim genericName = tree.FindNodeOrTokenByKind(SyntaxKind.GenericName)
            Dim info = model.GetSemanticInfoSummary(CType(genericName.AsNode(), ExpressionSyntax))

            genericName = tree.FindNodeOrTokenByKind(SyntaxKind.GenericName, 2)
            info = model.GetSemanticInfoSummary(CType(genericName.AsNode(), ExpressionSyntax))

            Dim qualifiedIdent = tree.FindNodeOrTokenByKind(SyntaxKind.QualifiedName)
            info = model.GetSemanticInfoSummary(CType(qualifiedIdent.AsNode(), ExpressionSyntax))
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromTypeDeclaration()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict On

        Imports System.Collections

        Namespace N1
            Class C1
            End Class

            Namespace N2
                Partial Class C2
                End Class

                public partial class Q'first
                end class
                public class Q'second
                end class
                public structure Q'third
                end structure
                public class Q(Of T)
                end class
 
            End Namespace
        End Namespace


    </file>
    <file name="b.vb">
        Option Strict Off

        Namespace N1.N2
            Partial Class C2
            End Class
            Public Interface Q
            end interface
        End Namespace

        Namespace Global.System
            Class C3  
            End Class
        End Namespace

        Class N1'class
            Namespace N2
                Class Wack
                End Class
                Enum Wack2
                End Enum
                Delegate Function Wack3(p as Integer) as Byte
            End Namespace
        End Class
    </file>
</compilation>, options:=options)

            Dim expectedErrors = <errors>
BC30179: class 'Q' and structure 'Q' conflict in namespace 'Goo.Bar.N1.N2'.
                public partial class Q'first
                                     ~
BC30179: class 'Q' and structure 'Q' conflict in namespace 'Goo.Bar.N1.N2'.
                public class Q'second
                             ~
BC30179: structure 'Q' and class 'Q' conflict in namespace 'Goo.Bar.N1.N2'.
                public structure Q'third
                                 ~
BC30179: interface 'Q' and class 'Q' conflict in namespace 'Goo.Bar.N1.N2'.
            Public Interface Q
                             ~
BC30481: 'Class' statement must end with a matching 'End Class'.
        Class N1'class
        ~~~~~~~~
BC30179: class 'N1' and namespace 'N1' conflict in namespace 'Goo.Bar'.
        Class N1'class
              ~~
BC30618: 'Namespace' statements can occur only at file or namespace level.
            Namespace N2
            ~~~~~~~~~~~~
BC30280: Enum 'Wack2' must contain at least one member.
                Enum Wack2
                     ~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
        End Class
        ~~~~~~~~~
</errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim typeSymbol, typeSymbol2, typeSymbol3, typeSymbol4, typeSymbol5, typeSymbol6 As INamedTypeSymbol

            typeSymbol = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "C1")
            Assert.NotNull(typeSymbol)
            Assert.Equal("Goo.Bar.N1.C1", typeSymbol.ToTestDisplayString())

            typeSymbol = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "C2")
            Assert.NotNull(typeSymbol)
            Assert.Equal("Goo.Bar.N1.N2.C2", typeSymbol.ToTestDisplayString())

            typeSymbol2 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "C2")
            Assert.NotNull(typeSymbol2)
            Assert.Equal("Goo.Bar.N1.N2.C2", typeSymbol2.ToTestDisplayString())
            Assert.Equal(typeSymbol, typeSymbol2)

            typeSymbol = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q'first")
            Assert.NotNull(typeSymbol)
            Assert.Equal("Goo.Bar.N1.N2.Q", typeSymbol.ToTestDisplayString())
            Assert.Equal(0, typeSymbol.Arity)
            Assert.Equal(TypeKind.Class, typeSymbol.TypeKind)

            typeSymbol2 = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q'second")
            Assert.NotNull(typeSymbol2)
            Assert.Equal("Goo.Bar.N1.N2.Q", typeSymbol2.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol2.TypeKind)
            Assert.Equal(0, typeSymbol2.Arity)
            Assert.Equal(typeSymbol, typeSymbol2)

            typeSymbol3 = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q'third")
            Assert.NotNull(typeSymbol3)
            Assert.Equal("Goo.Bar.N1.N2.Q", typeSymbol3.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, typeSymbol3.TypeKind)
            Assert.Equal(0, typeSymbol3.Arity)
            Assert.NotEqual(typeSymbol, typeSymbol3)
            Assert.NotEqual(typeSymbol2, typeSymbol3)

            typeSymbol4 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "Q")
            Assert.NotNull(typeSymbol4)
            Assert.Equal("Goo.Bar.N1.N2.Q", typeSymbol4.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, typeSymbol4.TypeKind)
            Assert.Equal(0, typeSymbol4.Arity)
            Assert.NotEqual(typeSymbol4, typeSymbol3)
            Assert.NotEqual(typeSymbol4, typeSymbol2)
            Assert.NotEqual(typeSymbol4, typeSymbol)

            typeSymbol5 = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q(Of T)")
            Assert.NotNull(typeSymbol5)
            Assert.Equal("Goo.Bar.N1.N2.Q(Of T)", typeSymbol5.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol5.TypeKind)
            Assert.Equal(1, typeSymbol5.Arity)
            Assert.NotEqual(typeSymbol5, typeSymbol4)
            Assert.NotEqual(typeSymbol5, typeSymbol3)
            Assert.NotEqual(typeSymbol5, typeSymbol2)
            Assert.NotEqual(typeSymbol5, typeSymbol)

            typeSymbol6 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "C3")
            Assert.NotNull(typeSymbol6)
            Assert.Equal("System.C3", typeSymbol6.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol6.TypeKind)
            Assert.Equal(0, typeSymbol6.Arity)

            Dim typeSymbol7 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "N1'class")
            Assert.NotNull(typeSymbol7)
            Assert.Equal("Goo.Bar.N1", typeSymbol7.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol7.TypeKind)

            Dim typeSymbol8 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "Wack")
            Assert.NotNull(typeSymbol8)
            Assert.Equal("Goo.Bar.N2.Wack", typeSymbol8.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol8.TypeKind)

            Dim typeSymbol9 = CompilationUtils.GetEnumSymbol(compilation, bindingsB, "b.vb", "Wack2")
            Assert.NotNull(typeSymbol9)
            Assert.Equal("Goo.Bar.N2.Wack2", typeSymbol9.ToTestDisplayString())
            Assert.Equal(TypeKind.Enum, typeSymbol9.TypeKind)

            Dim typeSymbol10 = CompilationUtils.GetDelegateSymbol(compilation, bindingsB, "b.vb", "Wack3")
            Assert.NotNull(typeSymbol10)
            Assert.Equal("Goo.Bar.N2.Wack3", typeSymbol10.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, typeSymbol10.TypeKind)

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        Private Function GetNamespaceSymbol(compilation As VisualBasicCompilation,
                                            semanticModel As SemanticModel,
                                            treeName As String,
                                            stringInDecl As String) As INamespaceSymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is NamespaceStatementSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            Return semanticModel.GetDeclaredSymbol(DirectCast(node, NamespaceStatementSyntax))
        End Function

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromNamespaceDeclaration()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict On

        Imports System.Collections

        Namespace N1
            Namespace N2.N3
            End Namespace
        End Namespace

        Class N4
        End Class

        Namespace N4'first
        End Namespace

        Namespace N4'second
        End Namespace
    </file>
    <file name="b.vb">
        Option Strict Off

        Namespace N1.N2
            Namespace N3
            End Namespace
        End Namespace

        Namespace N4
        End Namespace

        Namespace Global.N1
        End Namespace

        Namespace Global'lone global
            Namespace N7
            End Namespace
        End Namespace

        Class Outer
           Namespace N1'bad
           End Namespace
        End Class
    </file>
</compilation>, options:=options)

            Dim expectedErrors = <errors>
BC30179: class 'N4' and namespace 'N4' conflict in namespace 'Goo.Bar'.
        Class N4
              ~~
BC30481: 'Class' statement must end with a matching 'End Class'.
        Class Outer
        ~~~~~~~~~~~
BC30618: 'Namespace' statements can occur only at file or namespace level.
           Namespace N1'bad
           ~~~~~~~~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
        End Class
        ~~~~~~~~~
    </errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim nsSymbol0 = GetNamespaceSymbol(compilation, bindingsA, "a.vb", "N1")
            Assert.NotNull(nsSymbol0)
            Assert.Equal("Goo.Bar.N1", nsSymbol0.ToTestDisplayString())

            Dim nsSymbol1 = GetNamespaceSymbol(compilation, bindingsA, "a.vb", "N2.N3")
            Assert.NotNull(nsSymbol1)
            Assert.Equal("Goo.Bar.N1.N2.N3", nsSymbol1.ToTestDisplayString())

            Dim nsSymbol2 = GetNamespaceSymbol(compilation, bindingsA, "a.vb", "N4'first")
            Assert.NotNull(nsSymbol2)
            Assert.Equal("Goo.Bar.N4", nsSymbol2.ToTestDisplayString())

            Dim nsSymbol3 = GetNamespaceSymbol(compilation, bindingsA, "a.vb", "N4'second")
            Assert.NotNull(nsSymbol3)
            Assert.Equal("Goo.Bar.N4", nsSymbol3.ToTestDisplayString())
            Assert.Equal(nsSymbol2, nsSymbol3)

            Dim nsSymbol4 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "N1.N2")
            Assert.NotNull(nsSymbol4)
            Assert.Equal("Goo.Bar.N1.N2", nsSymbol4.ToTestDisplayString())

            Dim nsSymbol5 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "N3")
            Assert.NotNull(nsSymbol5)
            Assert.Equal("Goo.Bar.N1.N2.N3", nsSymbol5.ToTestDisplayString())
            Assert.Equal(nsSymbol1, nsSymbol5)

            Dim nsSymbol6 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "N4")
            Assert.NotNull(nsSymbol6)
            Assert.Equal("Goo.Bar.N4", nsSymbol6.ToTestDisplayString())
            Assert.Equal(nsSymbol2, nsSymbol6)

            Dim nsSymbol7 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "N1'bad")
            Assert.NotNull(nsSymbol7)
            Assert.Equal("Goo.Bar.N1", nsSymbol7.ToTestDisplayString())

            Dim nsSymbol8 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "Global.N1")
            Assert.NotNull(nsSymbol8)
            Assert.Equal("N1", nsSymbol8.ToTestDisplayString())

            Dim nsSymbol9 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "N7")
            Assert.NotNull(nsSymbol9)
            Assert.Equal("N7", nsSymbol9.ToTestDisplayString())

            Dim nsSymbol10 = GetNamespaceSymbol(compilation, bindingsB, "b.vb", "Global'lone global")
            Assert.NotNull(nsSymbol10)
            Assert.Equal("Global", nsSymbol10.ToTestDisplayString())

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        Private Function GetMethodSymbol(compilation As VisualBasicCompilation,
                                            semanticModel As SemanticModel,
                                            treeName As String,
                                            stringInDecl As String,
                                            ByRef syntax As MethodBaseSyntax) As MethodSymbol
            Return DirectCast(GetMethodBaseSymbol(compilation, semanticModel, treeName, stringInDecl, syntax), MethodSymbol)
        End Function

        Private Function GetPropertySymbol(compilation As VisualBasicCompilation,
                                            semanticModel As SemanticModel,
                                            treeName As String,
                                            stringInDecl As String,
                                            ByRef syntax As MethodBaseSyntax) As PropertySymbol
            Return DirectCast(GetMethodBaseSymbol(compilation, semanticModel, treeName, stringInDecl, syntax), PropertySymbol)
        End Function

        Private Function GetMethodBaseSymbol(compilation As VisualBasicCompilation,
                                            semanticModel As SemanticModel,
                                            treeName As String,
                                            stringInDecl As String,
                                            ByRef syntax As MethodBaseSyntax) As ISymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is MethodBaseSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            syntax = DirectCast(node, MethodBaseSyntax)
            Return semanticModel.GetDeclaredSymbol(syntax)
        End Function

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromMethodDeclaration()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict On
        Imports System.Collections

        Namespace N1
            Namespace N2.N3
                Partial Class C1
                    Public Sub Goo(x as Integer)
                    End Sub

                    Public MustOverride Function Goo() As String

                    Private Function Goo(a as Integer, y As String) As Long
                    End Function

                    Public Sub New()
                    End Sub
                 End Class
            End Namespace
        End Namespace

    </file>
    <file name="b.vb">
        Option Strict Off

        Namespace N1.N2.N3
            Partial Class C1
                    Shared Sub New()
                    End Sub

                    Private Function Goo(b as Integer, y As String) As Long
                    End Function
            End Class

            Public Sub Bar()
            End Sub
        End Namespace

        
        Class Outer
           Namespace N1'bad
              Class Wack
                 Public Sub Wackadoodle()
                 End Sub
              End Class
           End Namespace
        End Class
    </file>
</compilation>, options:=options)

            Dim expectedDeclErrors =
<errors>
BC31411: 'C1' must be declared 'MustInherit' because it contains methods declared 'MustOverride'.
                Partial Class C1
                              ~~
BC30269: 'Private Function Goo(a As Integer, y As String) As Long' has multiple definitions with identical signatures.
                    Private Function Goo(a as Integer, y As String) As Long
                                     ~~~
BC30001: Statement is not valid in a namespace.
            Public Sub Bar()
            ~~~~~~~~~~~~~~~~
</errors>

            Dim expectedParseErrors =
<errors>
BC30481: 'Class' statement must end with a matching 'End Class'.
        Class Outer
        ~~~~~~~~~~~
BC30618: 'Namespace' statements can occur only at file or namespace level.
           Namespace N1'bad
           ~~~~~~~~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
        End Class
        ~~~~~~~~~
</errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim syntax As MethodBaseSyntax = Nothing

            Dim methSymbol1 = GetMethodSymbol(compilation, bindingsA, "a.vb", "Sub Goo(x as Integer)", syntax)
            Assert.NotNull(methSymbol1)
            Assert.Equal("Sub Goo.Bar.N1.N2.N3.C1.Goo(x As System.Int32)", methSymbol1.ToTestDisplayString())
            Assert.Equal(treeA.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol1.Locations.Single().GetLineSpan().StartLinePosition.Line)

            Dim methSymbol2 = GetMethodSymbol(compilation, bindingsA, "a.vb", "Function Goo() As String", syntax)
            Assert.NotNull(methSymbol2)
            Assert.Equal("Function Goo.Bar.N1.N2.N3.C1.Goo() As System.String", methSymbol2.ToTestDisplayString())
            Assert.Equal(treeA.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol2.Locations.Single().GetLineSpan().StartLinePosition.Line)

            Dim methSymbol3 = GetMethodSymbol(compilation, bindingsA, "a.vb", "Goo(a as Integer, y As String)", syntax)
            Assert.NotNull(methSymbol3)
            Assert.Equal("Function Goo.Bar.N1.N2.N3.C1.Goo(a As System.Int32, y As System.String) As System.Int64", methSymbol3.ToTestDisplayString())
            Assert.Equal(treeA.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol3.Locations.Single().GetLineSpan().StartLinePosition.Line)

            Dim methSymbol4 = GetMethodSymbol(compilation, bindingsA, "a.vb", "Sub New", syntax)
            Assert.NotNull(methSymbol4)
            Assert.Equal("Sub Goo.Bar.N1.N2.N3.C1..ctor()", methSymbol4.ToTestDisplayString())
            Assert.Equal(treeA.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol4.Locations.Single().GetLineSpan().StartLinePosition.Line)

            Dim methSymbol5 = GetMethodSymbol(compilation, bindingsB, "b.vb", "Sub New", syntax)
            Assert.NotNull(methSymbol5)
            Assert.Equal("Sub Goo.Bar.N1.N2.N3.C1..cctor()", methSymbol5.ToTestDisplayString())
            Assert.Equal(treeB.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol5.Locations.Single().GetLineSpan().StartLinePosition.Line)

            Dim methSymbol6 = GetMethodSymbol(compilation, bindingsB, "b.vb", "Goo(b as Integer, y As String)", syntax)
            Assert.NotNull(methSymbol6)
            Assert.Equal("Function Goo.Bar.N1.N2.N3.C1.Goo(b As System.Int32, y As System.String) As System.Int64", methSymbol6.ToTestDisplayString())
            Assert.Equal(treeB.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol6.Locations.Single().GetLineSpan().StartLinePosition.Line)

            Dim methSymbol7 = GetMethodSymbol(compilation, bindingsB, "b.vb", "Bar()", syntax)
            Assert.NotNull(methSymbol7)

            Dim methSymbol8 = GetMethodSymbol(compilation, bindingsB, "b.vb", "Wackadoodle()", syntax)
            Assert.NotNull(methSymbol8)
            Assert.Equal("Sub Goo.Bar.N1.Wack.Wackadoodle()", methSymbol8.ToTestDisplayString())
            Assert.Equal(treeB.GetLineSpan(syntax.Span).StartLinePosition.Line,
                         methSymbol8.Locations.Single().GetLineSpan().StartLinePosition.Line)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedDeclErrors)
            CompilationUtils.AssertTheseParseDiagnostics(compilation, expectedParseErrors)
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromPropertyDeclaration()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="c.vb">
Class C
    Property P As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
    Property Q As Object
    Property R(index As Integer)
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class
    </file>
</compilation>, options:=options)

            Dim tree = CompilationUtils.GetTree(compilation, "c.vb")
            Dim bindings = compilation.GetSemanticModel(tree)
            Dim propertySyntax As MethodBaseSyntax = Nothing
            Dim propertySymbol As IPropertySymbol
            Dim parameterSyntax As ParameterSyntax = Nothing
            Dim parameterSymbol As IParameterSymbol

            propertySymbol = GetPropertySymbol(compilation, bindings, "c.vb", "Property P As Integer", propertySyntax)
            Assert.NotNull(propertySymbol)

            propertySymbol = GetPropertySymbol(compilation, bindings, "c.vb", "Property Q As Object", propertySyntax)
            Assert.NotNull(propertySymbol)

            propertySymbol = GetPropertySymbol(compilation, bindings, "c.vb", "Property R(index As Integer)", propertySyntax)
            Assert.NotNull(propertySymbol)

            parameterSymbol = GetParameterSymbol(compilation, bindings, "c.vb", "index As Integer", parameterSyntax)
            Assert.NotNull(parameterSymbol)
            Assert.Equal(parameterSymbol.ContainingSymbol, propertySymbol)

            parameterSymbol = GetParameterSymbol(compilation, bindings, "c.vb", "value As Object", parameterSyntax)
            Assert.NotNull(parameterSymbol)
            Assert.Equal(parameterSymbol.ContainingSymbol, propertySymbol.SetMethod)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromOperatorDeclaration()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("NS")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict Off
Imports System

Enum E
    Zero
    One
    Two
End Enum

Class A
    Structure S
        Shared Narrowing Operator CType(x As S) As E?
            System.Console.WriteLine("Operator Conv")
            Return Nothing
        End Operator

        Shared Operator Mod(x As S?, y As E) As E
            System.Console.WriteLine("Operator Mod")
            Return y
        End Operator
    End Structure
End Class

    </file>
</compilation>, options:=options)

            Dim tree = CompilationUtils.GetTree(compilation, "a.vb")
            Dim model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of OperatorStatementSyntax)()
            Assert.Equal(2, nodes.Count)
            Dim sym1 = model.GetDeclaredSymbol(nodes.First())
            Dim sym2 = model.GetDeclaredSymbol(nodes.Last())
            Assert.Equal(MethodKind.Conversion, sym1.MethodKind)
            Assert.Equal(MethodKind.UserDefinedOperator, sym2.MethodKind)
            Assert.Equal("Public Shared Narrowing Operator CType(x As NS.A.S) As NS.E?", sym1.ToDisplayString())
            Assert.Equal("Public Shared Operator Mod(x As NS.A.S?, y As NS.E) As NS.E", sym2.ToDisplayString())

        End Sub

        Private Function GetParameterSymbol(compilation As VisualBasicCompilation,
                                            semanticModel As SemanticModel,
                                            treeName As String,
                                            stringInDecl As String,
                                            ByRef syntax As ParameterSyntax) As IParameterSymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is ParameterSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            syntax = DirectCast(node, ParameterSyntax)
            Return semanticModel.GetDeclaredSymbol(syntax)
        End Function

        Private Function GetLabelSymbol(compilation As VisualBasicCompilation,
                                    semanticModel As SemanticModel,
                                    treeName As String,
                                    stringInDecl As String,
                                    ByRef syntax As LabelStatementSyntax) As ILabelSymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is LabelStatementSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            syntax = DirectCast(node, LabelStatementSyntax)
            Return semanticModel.GetDeclaredSymbol(syntax, Nothing)
        End Function

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromParameter()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict On
        Imports System.Collections

        Namespace N1
            Partial Class C1
                Public Sub Goo(x as Integer, Optional yopt as String = "hi")
                End Sub

                Public MustOverride Function Goo(a as Long, a as integer) As String

                Public Sub New(c as string, d as string)
                End Sub
            End Class
        End Namespace

    </file>
    <file name="b.vb">
        Option Strict Off

        Namespace N1
            Partial Class C1
                    Shared Sub New()
                    End Sub
            End Class

            Public Sub Bar(aaa as integer)
            End Sub
        End Namespace

    </file>
</compilation>, options:=options)

            Dim expectedErrors =
<errors>
BC31411: 'C1' must be declared 'MustInherit' because it contains methods declared 'MustOverride'.
            Partial Class C1
                          ~~
BC30237: Parameter already declared with name 'a'.
                Public MustOverride Function Goo(a as Long, a as integer) As String
                                                            ~
BC30001: Statement is not valid in a namespace.
            Public Sub Bar(aaa as integer)
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim syntax As ParameterSyntax = Nothing

            Dim paramSymbol1 = GetParameterSymbol(compilation, bindingsA, "a.vb", "x as Integer", syntax)
            Assert.NotNull(paramSymbol1)
            Assert.Equal("x", paramSymbol1.Name)
            Assert.Equal("System.Int32", paramSymbol1.Type.ToTestDisplayString())
            Assert.Equal("Sub Goo.Bar.N1.C1.Goo(x As System.Int32, [yopt As System.String = ""hi""])", paramSymbol1.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart,
                         paramSymbol1.Locations.Single().SourceSpan.Start)

            Dim paramSymbol2 = GetParameterSymbol(compilation, bindingsA, "a.vb", "yopt as String", syntax)
            Assert.NotNull(paramSymbol2)
            Assert.Equal("yopt", paramSymbol2.Name)
            Assert.Equal("System.String", paramSymbol2.Type.ToTestDisplayString())
            Assert.Equal("Sub Goo.Bar.N1.C1.Goo(x As System.Int32, [yopt As System.String = ""hi""])", paramSymbol2.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart,
                         paramSymbol2.Locations.Single().SourceSpan.Start - "Optional ".Length)

            Dim paramSymbol3 = GetParameterSymbol(compilation, bindingsA, "a.vb", "a as Long", syntax)
            Assert.NotNull(paramSymbol3)
            Assert.Equal("a", paramSymbol3.Name)
            Assert.Equal("System.Int64", paramSymbol3.Type.ToTestDisplayString())
            Assert.Equal("Function Goo.Bar.N1.C1.Goo(a As System.Int64, a As System.Int32) As System.String", paramSymbol3.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart,
                         paramSymbol3.Locations.Single().SourceSpan.Start)

            Dim paramSymbol4 = GetParameterSymbol(compilation, bindingsA, "a.vb", "a as integer", syntax)
            Assert.NotNull(paramSymbol4)
            Assert.Equal("a", paramSymbol4.Name)
            Assert.Equal("System.Int32", paramSymbol4.Type.ToTestDisplayString())
            Assert.Equal("Function Goo.Bar.N1.C1.Goo(a As System.Int64, a As System.Int32) As System.String", paramSymbol4.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart,
                         paramSymbol4.Locations.Single().SourceSpan.Start)

            Dim paramSymbol5 = GetParameterSymbol(compilation, bindingsA, "a.vb", "d as string", syntax)
            Assert.NotNull(paramSymbol5)
            Assert.Equal("d", paramSymbol5.Name)
            Assert.Equal("System.String", paramSymbol5.Type.ToTestDisplayString())
            Assert.Equal("Sub Goo.Bar.N1.C1..ctor(c As System.String, d As System.String)", paramSymbol5.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart,
                         paramSymbol5.Locations.Single().SourceSpan.Start)

            Dim paramSymbol6 = GetParameterSymbol(compilation, bindingsB, "b.vb", "aaa as integer", syntax)
            Assert.NotNull(paramSymbol6)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromEventParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TestGetDeclaredSymbolFromEventParameter">
    <file name="a.vb">
        Namespace N1
            Class Test
                Public Event Percent(ByVal Percent As Single)
                Public Shared Sub Main()
                End Sub
            End Class
        End Namespace
    </file>
</compilation>)

            Dim tree = CompilationUtils.GetTree(compilation, "a.vb")
            Dim model = compilation.GetSemanticModel(tree)

            Dim syntax As ParameterSyntax = Nothing

            Dim paramSymbol1 = GetParameterSymbol(compilation, model, "a.vb", "Percent As Single", syntax)
            Assert.NotNull(paramSymbol1)
            Assert.Equal("Percent", paramSymbol1.Name)
            Assert.Equal("System.Single", paramSymbol1.Type.ToTestDisplayString())
            Assert.Equal("Event N1.Test.Percent(Percent As System.Single)", paramSymbol1.ContainingType.AssociatedSymbol.ToTestDisplayString())
            Assert.Equal("Sub N1.Test.PercentEventHandler.Invoke(Percent As System.Single)", paramSymbol1.ContainingSymbol.ToTestDisplayString())

        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromParameterWithTypeChar()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="TypeChar.vb">
Imports System
Imports Microsoft.VisualBasic

Namespace VBN

    Module Program

        Function osGetWindowLong&amp;(ByVal h&amp;, ByVal ndx&amp;)
            Return h
        End Function

        Function F1!(ByRef p1!, p2%)
            Return p1 + p2
        End Function

        Function F2$(ByRef px$, ByVal py$, ByVal pz#, ByRef pw@)
            Return p2
        End Function

        Sub Main()
        End Sub

    End Module

End Namespace
    </file>
</compilation>)

            Const file = "TypeChar.vb"
            Dim treeA = CompilationUtils.GetTree(compilation, file)
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim syntax As ParameterSyntax = Nothing
            Dim paramSymbol1 = GetParameterSymbol(compilation, bindingsA, file, "ByVal ndx&", syntax)
            Assert.Equal("ndx", paramSymbol1.Name)
            Assert.Equal("System.Int64", paramSymbol1.Type.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart + 6, paramSymbol1.Locations.Single().SourceSpan.Start)

            Dim paramSymbol2 = GetParameterSymbol(compilation, bindingsA, file, "ByRef p1!", syntax)
            Assert.Equal("p1", paramSymbol2.Name)
            Assert.Equal("System.Single", paramSymbol2.Type.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart + 6, paramSymbol2.Locations.Single().SourceSpan.Start)

            Dim paramSymbol3 = GetParameterSymbol(compilation, bindingsA, file, "p2%", syntax)
            Assert.Equal("p2", paramSymbol3.Name)
            Assert.Equal("System.Int32", paramSymbol3.Type.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart, paramSymbol3.Locations.Single().SourceSpan.Start)

            Dim paramSymbol4 = GetParameterSymbol(compilation, bindingsA, file, "ByRef px$", syntax)
            Assert.Equal("px", paramSymbol4.Name)
            Assert.Equal("System.String", paramSymbol4.Type.ToTestDisplayString())
            Assert.Equal(syntax.SpanStart + 6, paramSymbol4.Locations.Single().SourceSpan.Start)

            Dim paramSymbol5 = GetParameterSymbol(compilation, bindingsA, file, "ByVal py$", syntax)
            Assert.Equal("py", paramSymbol5.Name)
            Assert.Equal("System.String", paramSymbol5.Type.ToTestDisplayString())

            Dim paramSymbol6 = GetParameterSymbol(compilation, bindingsA, file, "ByVal pz#", syntax)
            Assert.Equal("pz", paramSymbol6.Name)
            Assert.Equal("System.Double", paramSymbol6.Type.ToTestDisplayString())

            Dim paramSymbol7 = GetParameterSymbol(compilation, bindingsA, file, "ByRef pw@", syntax)
            Assert.Equal("pw", paramSymbol7.Name)
            Assert.Equal("System.Decimal", paramSymbol7.Type.ToTestDisplayString())

        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolLambdaParam()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam As Integer)
                                                  Return lambdaParam + 1
                                              End Function
    End Sub
End Module
    </file>
</compilation>, options:=options)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("lambdaParam", StringComparison.Ordinal)).Parent
            Dim symbol = bindingsA.GetDeclaredSymbolFromSyntaxNode(node)

            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Parameter, symbol.Kind)
            Assert.Equal("lambdaParam As Integer", symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromPropertyStatementSyntax()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="TypeChar.vb">
Class Program

    Default Overridable Property DefProp(p As Integer) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property

    Property AutoProp As String = "error"

    WriteOnly Property RegularProp As String
        Set(value As String)
        End Set
    End Property

End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim defPropSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.PropertyStatement, 1).AsNode(), PropertyStatementSyntax)
            Dim autoPropSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.PropertyStatement, 2).AsNode(), PropertyStatementSyntax)
            Dim regularPropSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.PropertyStatement, 3).AsNode(), PropertyStatementSyntax)

            Dim defPropSymbol = model.GetDeclaredSymbol(defPropSyntax)
            Assert.NotNull(defPropSymbol)
            Assert.Equal("Property Program.DefProp(p As System.Int32) As System.String", defPropSymbol.ToTestDisplayString())
            Dim autoPropSymbol = model.GetDeclaredSymbol(autoPropSyntax)
            Assert.NotNull(autoPropSymbol)
            Assert.Equal("Property Program.AutoProp As System.String", autoPropSymbol.ToTestDisplayString())
            Dim regularPropSymbol = model.GetDeclaredSymbol(regularPropSyntax)
            Assert.NotNull(regularPropSymbol)
            Assert.Equal("WriteOnly Property Program.RegularProp As System.String", regularPropSymbol.ToTestDisplayString())

            Dim defPropBlockSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.PropertyBlock, 1).AsNode(), PropertyBlockSyntax)
            Assert.Equal(defPropSymbol, model.GetDeclaredSymbol(defPropBlockSyntax))
            Dim regularPropBlockSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.PropertyBlock, 2).AsNode(), PropertyBlockSyntax)
            Assert.Equal(regularPropSymbol, model.GetDeclaredSymbol(regularPropBlockSyntax))

            Dim defPropGetBlockSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.GetAccessorBlock, 1).AsNode(), AccessorBlockSyntax)
            Assert.Equal("Public Overridable Property Get DefProp(p As Integer) As String", model.GetDeclaredSymbol(defPropGetBlockSyntax).ToString())
            Assert.True(TypeOf (model.GetDeclaredSymbol(defPropGetBlockSyntax)) Is MethodSymbol, "API should return a MethodSymbol")
            Dim defPropSetBlockSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SetAccessorBlock, 1).AsNode(), AccessorBlockSyntax)
            Assert.Equal("Public Overridable Property Set DefProp(p As Integer, value As String)", model.GetDeclaredSymbol(defPropSetBlockSyntax).ToString())
            Dim regularPropSetBlockSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SetAccessorBlock, 2).AsNode(), AccessorBlockSyntax)
            Assert.Equal("Public Property Set RegularProp(value As String)", model.GetDeclaredSymbol(regularPropSetBlockSyntax).ToString())

            Dim defPropGetSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.GetAccessorStatement, 1).AsNode(), MethodBaseSyntax)
            Assert.Equal("Public Overridable Property Get DefProp(p As Integer) As String", model.GetDeclaredSymbol(defPropGetSyntax).ToString())
            Dim defPropSetSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SetAccessorStatement, 1).AsNode(), MethodBaseSyntax)
            Assert.Equal("Public Overridable Property Set DefProp(p As Integer, value As String)", model.GetDeclaredSymbol(defPropSetSyntax).ToString())
            Dim regularPropSetSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SetAccessorStatement, 2).AsNode(), MethodBaseSyntax)
            Assert.Equal("Public Property Set RegularProp(value As String)", model.GetDeclaredSymbol(regularPropSetSyntax).ToString())

        End Sub

        <WorkItem(540877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540877")>
        <Fact()>
        Public Sub TestGetDeclaredSymbolFromAlias()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="TypeChar.vb">
Imports M=

Imports MS_ = Microsoft
Imports Sys = System.Collections,
        Sys_Collections = System,
        Sys_Collections_BitArray = System.Collections.BitArray
Imports MS_ = System.Collections
Imports M = System.Collections

Class Program

End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 1).AsNode(), SimpleImportsClauseSyntax)
            Dim aliasSymbol = DirectCast(model.GetDeclaredSymbol(importsClause), AliasSymbol)
            Assert.NotNull(aliasSymbol)
            Assert.Equal("M", aliasSymbol.Name)
            Assert.Equal(SymbolKind.ErrorType, aliasSymbol.Target.Kind)
            Assert.Equal("", aliasSymbol.Target.Name)
            Assert.False(aliasSymbol.IsNotOverridable)
            Assert.False(aliasSymbol.IsMustOverride)
            Assert.False(aliasSymbol.IsOverrides)
            Assert.False(aliasSymbol.IsOverridable)
            Assert.False(aliasSymbol.IsShared)
            Assert.Equal(1, aliasSymbol.DeclaringSyntaxReferences.Length)
            Assert.Equal(SyntaxKind.SimpleImportsClause, aliasSymbol.DeclaringSyntaxReferences.First.GetSyntax().Kind)
            Assert.Equal(Accessibility.NotApplicable, aliasSymbol.DeclaredAccessibility)
            Dim x8 As Symbol = aliasSymbol.ContainingSymbol
            Assert.Equal(aliasSymbol.Locations.Item(0).GetHashCode, aliasSymbol.GetHashCode)
            Assert.Equal(aliasSymbol, aliasSymbol)
            Assert.NotNull(aliasSymbol)
            importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 2).AsNode(), SimpleImportsClauseSyntax)
            Assert.Equal("MS_=Microsoft", model.GetDeclaredSymbol(importsClause).ToTestDisplayString())
            importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 3).AsNode(), SimpleImportsClauseSyntax)
            Assert.Equal("Sys=System.Collections", model.GetDeclaredSymbol(importsClause).ToTestDisplayString())
            importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 4).AsNode(), SimpleImportsClauseSyntax)
            Assert.Equal("Sys_Collections=System", model.GetDeclaredSymbol(importsClause).ToTestDisplayString())
            importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 5).AsNode(), SimpleImportsClauseSyntax)
            Assert.Equal("Sys_Collections_BitArray=System.Collections.BitArray", model.GetDeclaredSymbol(importsClause).ToTestDisplayString())
            importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 6).AsNode(), SimpleImportsClauseSyntax)
            Assert.Equal("MS_=System.Collections", model.GetDeclaredSymbol(importsClause).ToTestDisplayString())
            importsClause = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 7).AsNode(), SimpleImportsClauseSyntax)
            Assert.Equal("M=System.Collections", model.GetDeclaredSymbol(importsClause).ToTestDisplayString())

            Dim genericSyntax = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleImportsClause, 7).AsNode()
            Assert.Equal("M=System.Collections", model.GetDeclaredSymbolFromSyntaxNode(genericSyntax).ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolForOnErrorGotoLabels()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="TypeChar.vb">
Import System

Class Program
Shared Sub Main()
On Error Goto Goo

Exit Sub
Goo:
    Resume next
End Sub

Shared Sub ResumeNext()
On Error Resume Next
Exit Sub
Goo:
    Resume next
End Sub

Shared Sub Goto0()
On Error Goto 0
End Sub

Shared Sub Goto1()
On Error Goto -1
End Sub

End Class

    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim Labels = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.LabelStatement, 1).AsNode(), LabelStatementSyntax)
            Assert.Equal(2, Labels.SlotCount)

            Dim LabelSymbol = DirectCast(model.GetDeclaredSymbol(Labels), LabelSymbol)
            Assert.NotNull(LabelSymbol)
            Assert.Equal("Goo", LabelSymbol.Name)

            Dim OnErrorGoto = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.OnErrorGoToLabelStatement, 1).AsNode(), OnErrorGoToStatementSyntax)
            Assert.NotNull(OnErrorGoto)

            Dim OnErrorResumeNext = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.OnErrorResumeNextStatement, 1).AsNode(), OnErrorResumeNextStatementSyntax)
            Assert.NotNull(OnErrorResumeNext)

            Dim OnErrorGoto0 = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.OnErrorGoToZeroStatement, 1).AsNode(), OnErrorGoToStatementSyntax)
            Assert.NotNull(OnErrorGoto0)

            Dim OnErrorGoto1 = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.OnErrorGoToMinusOneStatement, 1).AsNode(), OnErrorGoToStatementSyntax)
            Assert.NotNull(OnErrorGoto1)
        End Sub

        <WorkItem(541238, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541238")>
        <Fact()>
        Public Sub TestGetDeclaredSymbolFromAliasDecl()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Imports VB6 = Microsoft.VisualBasic
    </file>
</compilation>, options:=options)

            compilation.AssertTheseDiagnostics(<expected>
BC40056: Namespace or type specified in the Imports 'Microsoft.VisualBasic' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports VB6 = Microsoft.VisualBasic
              ~~~~~~~~~~~~~~~~~~~~~
                                               </expected>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("VB6", StringComparison.Ordinal)).Parent.Parent
            Dim symbol = bindingsA.GetDeclaredSymbol(node)

            Assert.Equal(SyntaxKind.SimpleImportsClause, node.Kind)
            Assert.Equal("VB6=Microsoft.VisualBasic", symbol.ToTestDisplayString())

        End Sub

        <Fact(), WorkItem(544076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544076")>
        Public Sub TestGetDeclaredSymbolFromSubFunctionConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="TypeChar.vb">
Interface I1
    Function F() As String
End Interface

Class C1
    Implements I1

    Public Sub New()
    End Sub

    Public Sub S()
    End Sub

    Public Function F() As String Implements I1.F
        Return Nothing
    End Function

    Declare Sub PInvokeSub Lib "Bar" ()
    Declare Function PInvokeFun Lib "Baz" () As Integer

End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim fSyntax1 = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.FunctionStatement, 1).AsNode(), MethodStatementSyntax)
            Dim nSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SubNewStatement, 1).AsNode(), SubNewStatementSyntax)
            Dim sSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SubStatement, 1).AsNode(), MethodStatementSyntax)
            Dim fSyntax2 = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.FunctionStatement, 2).AsNode(), MethodStatementSyntax)
            Dim declareSubSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.DeclareSubStatement, 1).AsNode(), DeclareStatementSyntax)
            Dim declareFunSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.DeclareFunctionStatement, 1).AsNode(), DeclareStatementSyntax)

            Dim fSymbol1 = model.GetDeclaredSymbol(fSyntax1)
            Assert.NotNull(fSymbol1)
            Assert.Equal("Function I1.F() As System.String", fSymbol1.ToTestDisplayString())

            Dim nSymbol = model.GetDeclaredSymbol(nSyntax)
            Assert.NotNull(nSymbol)
            Assert.Equal("Sub C1..ctor()", nSymbol.ToTestDisplayString())

            Dim sSymbol = model.GetDeclaredSymbol(sSyntax)
            Assert.NotNull(sSymbol)
            Assert.Equal("Sub C1.S()", sSymbol.ToTestDisplayString())

            Dim fSymbol2 = model.GetDeclaredSymbol(fSyntax2)
            Assert.NotNull(fSymbol2)
            Assert.Equal("Function C1.F() As System.String", fSymbol2.ToTestDisplayString())

            Dim declareSubSymbol = model.GetDeclaredSymbol(declareSubSyntax)
            Assert.NotNull(declareSubSymbol)
            Assert.Equal("Declare Ansi Sub C1.PInvokeSub Lib ""Bar"" ()", declareSubSymbol.ToTestDisplayString())

            Dim declareFunSymbol = model.GetDeclaredSymbol(declareFunSyntax)
            Assert.NotNull(declareFunSymbol)
            Assert.Equal("Declare Ansi Function C1.PInvokeFun Lib ""Baz"" () As System.Int32", declareFunSymbol.ToTestDisplayString())

            Assert.Same(fSymbol2, model.GetDeclaredSymbol(DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.FunctionBlock, 1).AsNode(), MethodBlockSyntax)))
            Assert.Same(nSymbol, model.GetDeclaredSymbol(DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.ConstructorBlock, 1).AsNode(), ConstructorBlockSyntax)))
            Assert.Same(sSymbol, model.GetDeclaredSymbol(DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.SubBlock, 1).AsNode(), MethodBlockSyntax)))
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromTypes()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="TypeChar.vb">
Interface I1
End Interface

Namespace NS

    Class C1

        Enum E2
            None
        End Enum

        Class C2
        End Class

        Interface I2
        End Interface

    End Class

End Namespace
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim nsSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.NamespaceStatement, 1).AsNode(), NamespaceStatementSyntax)
            Dim i1Syntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.InterfaceStatement, 1).AsNode(), TypeStatementSyntax)
            Dim c1Syntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.ClassStatement, 1).AsNode(), TypeStatementSyntax)
            Dim e2Syntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.EnumStatement, 1).AsNode(), EnumStatementSyntax)
            Dim c2Syntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.ClassStatement, 2).AsNode(), TypeStatementSyntax)
            Dim i2Syntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.InterfaceStatement, 2).AsNode(), TypeStatementSyntax)
            Dim e2NoneSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.EnumMemberDeclaration, 1).AsNode(), EnumMemberDeclarationSyntax)

            Dim i1Symbol = model.GetDeclaredSymbol(i1Syntax)
            Assert.NotNull(i1Symbol)
            Assert.Equal("I1", i1Symbol.ToTestDisplayString())
            Dim nsSymbol = model.GetDeclaredSymbol(nsSyntax)
            Assert.NotNull(nsSymbol)
            Assert.Equal("NS", nsSymbol.ToTestDisplayString())
            Dim c1Symbol = model.GetDeclaredSymbol(c1Syntax)
            Assert.NotNull(c1Symbol)
            Assert.Equal("NS.C1", c1Symbol.ToTestDisplayString())
            Dim i2Symbol = model.GetDeclaredSymbol(i2Syntax)
            Assert.NotNull(i2Symbol)
            Assert.Equal("NS.C1.I2", i2Symbol.ToTestDisplayString())
            Dim c2Symbol = model.GetDeclaredSymbol(c2Syntax)
            Assert.NotNull(c2Symbol)
            Assert.Equal("NS.C1.C2", c2Symbol.ToTestDisplayString())
            Dim e2Symbol = model.GetDeclaredSymbol(e2Syntax)
            Assert.NotNull(e2Symbol)
            Assert.Equal("NS.C1.E2", e2Symbol.ToTestDisplayString())
            Dim e2NoneSymbol = model.GetDeclaredSymbol(e2NoneSyntax)
            Assert.NotNull(e2NoneSymbol)
            Assert.Equal("NS.C1.E2.None", e2NoneSymbol.ToTestDisplayString())

            Assert.Equal(i1Symbol, model.GetDeclaredSymbol(DirectCast(i1Syntax.Parent, TypeBlockSyntax)))
            Assert.Equal(i2Symbol, model.GetDeclaredSymbol(DirectCast(i2Syntax.Parent, TypeBlockSyntax)))
            Assert.Equal(c1Symbol, model.GetDeclaredSymbol(DirectCast(c1Syntax.Parent, TypeBlockSyntax)))
            Assert.Equal(c2Symbol, model.GetDeclaredSymbol(DirectCast(c2Syntax.Parent, TypeBlockSyntax)))
            Assert.Equal(e2Symbol, model.GetDeclaredSymbol(DirectCast(e2Syntax.Parent, EnumBlockSyntax)))
            Assert.Equal(nsSymbol, model.GetDeclaredSymbol(DirectCast(nsSyntax.Parent, NamespaceBlockSyntax)))
        End Sub

        Private Function GetTypeParameterSymbol(compilation As VisualBasicCompilation,
                                                semanticModel As SemanticModel,
                                                treeName As String,
                                                stringInDecl As String,
                                                ByRef syntax As TypeParameterSyntax) As ITypeParameterSymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is TypeParameterSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            syntax = DirectCast(node, TypeParameterSyntax)
            Return semanticModel.GetDeclaredSymbol(syntax)
        End Function

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromTypeParameter()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict On
        Imports System.Collections

        Namespace N1
            Partial Class C1(Of TTT, UUU)
                Sub K(Of VVV)(a as VVV)
                End Sub
            End Class
        End Namespace

    </file>
    <file name="b.vb">
        Option Strict Off

        Namespace N1
            Partial Class C1(Of TTT, UUU)
            End Class

            Sub Goofy(Of ZZZ)()
            End Sub
        End Namespace

    </file>
</compilation>, options:=options)

            Dim expectedErrors =
<errors>
BC30001: Statement is not valid in a namespace.
            Sub Goofy(Of ZZZ)()
            ~~~~~~~~~~~~~~~~~~~
</errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim syntax As TypeParameterSyntax = Nothing

            Dim tpSymbol1 = GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "TTT", syntax)
            Assert.NotNull(tpSymbol1)
            Assert.Equal("TTT", tpSymbol1.Name)
            Assert.Equal("Goo.Bar.N1.C1(Of TTT, UUU)", tpSymbol1.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(2, tpSymbol1.Locations.Length())
            Assert.True(syntax.SpanStart = tpSymbol1.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = tpSymbol1.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, tpSymbol1))

            Dim tpSymbol2 = GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "UUU", syntax)
            Assert.NotNull(tpSymbol2)
            Assert.Equal("UUU", tpSymbol2.Name)
            Assert.Equal("Goo.Bar.N1.C1(Of TTT, UUU)", tpSymbol2.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(2, tpSymbol2.Locations.Length())
            Assert.True(syntax.SpanStart = tpSymbol2.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = tpSymbol2.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, tpSymbol2))

            Dim tpSymbol3 = GetTypeParameterSymbol(compilation, bindingsB, "b.vb", "TTT", syntax)
            Assert.NotNull(tpSymbol3)
            Assert.Equal("TTT", tpSymbol3.Name)
            Assert.Equal("Goo.Bar.N1.C1(Of TTT, UUU)", tpSymbol3.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(2, tpSymbol3.Locations.Length())
            Assert.True(syntax.SpanStart = tpSymbol3.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = tpSymbol3.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, tpSymbol3))

            Dim tpSymbol4 = GetTypeParameterSymbol(compilation, bindingsB, "b.vb", "UUU", syntax)
            Assert.NotNull(tpSymbol4)
            Assert.Equal("UUU", tpSymbol4.Name)
            Assert.Equal("Goo.Bar.N1.C1(Of TTT, UUU)", tpSymbol4.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(2, tpSymbol4.Locations.Length())
            Assert.True(syntax.SpanStart = tpSymbol4.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = tpSymbol4.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, tpSymbol4))

            Dim tpSymbol5 = GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "VVV", syntax)
            Assert.NotNull(tpSymbol5)
            Assert.Equal("VVV", tpSymbol5.Name)
            Assert.Equal("Sub Goo.Bar.N1.C1(Of TTT, UUU).K(Of VVV)(a As VVV)", tpSymbol5.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(1, tpSymbol5.Locations.Length())
            Assert.Equal(syntax.SpanStart, tpSymbol5.Locations.Single().SourceSpan.Start)

            Dim tpSymbol6 = GetTypeParameterSymbol(compilation, bindingsB, "b.vb", "ZZZ", syntax)
            Assert.NotNull(tpSymbol6)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub

        Private Function GetVariableSymbol(compilation As VisualBasicCompilation,
                                           semanticModel As SemanticModel,
                                           treeName As String,
                                           stringInDecl As String,
                                           ByRef syntax As ModifiedIdentifierSyntax) As ISymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is ModifiedIdentifierSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            syntax = DirectCast(node, ModifiedIdentifierSyntax)
            Return semanticModel.GetDeclaredSymbol(syntax)
        End Function

        <Fact()>
        Public Sub TestGetDeclaredFromLabel()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Module Module1
    Sub Main()
Label1:
Label2:
    End Sub
End Module
    </file>
</compilation>, options:=options)

            Dim tree = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindings = compilation.GetSemanticModel(tree)
            Dim root As Object = tree.GetRoot(Nothing)

            Dim label1 = GetLabelSymbol(compilation, bindings, "a.vb", "Label1", Nothing)
            Assert.NotNull(label1)
            Assert.Equal("Label1", label1.Name)

            Dim label2 = GetLabelSymbol(compilation, bindings, "a.vb", "Label2", Nothing)
            Assert.NotNull(label2)
            Assert.Equal("Label2", label2.Name)
            Dim symLabel = DirectCast(label1, LabelSymbol)
            Assert.False(symLabel.IsOverloadable())
            Assert.False(symLabel.IsMustOverride)
            Assert.False(symLabel.IsOverrides)
            Assert.False(symLabel.IsOverridable)
            Assert.False(symLabel.IsShared)
            AssertEx.Equal(Of Accessibility)(Accessibility.NotApplicable, symLabel.DeclaredAccessibility)
            Assert.Equal(1, symLabel.Locations.Length)
            Assert.Equal("Public Sub Main()", symLabel.ContainingSymbol.ToString)
            Assert.Equal("Public Sub Main()", symLabel.ContainingMethod.ToString)
            Assert.Equal(1, symLabel.Locations.Length)
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolFromVariableName()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict Off
        Imports System.Collections

        Namespace N1
            Partial Class C1
                Private aa, b$, aa$, b()
            End Class
        End Namespace

    </file>
    <file name="b.vb">
        Option Strict Off

        Namespace N1
            Partial Class C1
                public aa As String

                Function f(xxx as integer, yyy as string) As integer
                    dim aaa, bbb, ccc as Integer, ccc$
                    return ccc
                End Function
            End Class
        End Namespace
    </file>
</compilation>, options:=options)

            Dim expectedErrors =
<errors>
BC30260: 'aa' is already declared as 'Private aa As Object' in this class.
                Private aa, b$, aa$, b()
                                ~~~
BC30260: 'b' is already declared as 'Private b As String' in this class.
                Private aa, b$, aa$, b()
                                     ~
BC30260: 'aa' is already declared as 'Private aa As Object' in this class.
                public aa As String
                       ~~
BC42024: Unused local variable: 'aaa'.
                    dim aaa, bbb, ccc as Integer, ccc$
                        ~~~
BC42024: Unused local variable: 'bbb'.
                    dim aaa, bbb, ccc as Integer, ccc$
                             ~~~
BC30288: Local variable 'ccc' is already declared in the current block.
                    dim aaa, bbb, ccc as Integer, ccc$
                                                  ~~~~
BC42024: Unused local variable: 'ccc'.
                    dim aaa, bbb, ccc as Integer, ccc$
                                                  ~~~~
</errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim syntax As ModifiedIdentifierSyntax = Nothing

            Dim varSymbol1 = GetVariableSymbol(compilation, bindingsA, "a.vb", "aa", syntax)
            Assert.NotNull(varSymbol1)
            Assert.Equal("aa", varSymbol1.Name)
            Assert.Equal(SymbolKind.Field, varSymbol1.Kind)
            Assert.Equal("System.Object", DirectCast(varSymbol1, FieldSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol1.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol1.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol1.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol1))

            Dim varSymbol2 = GetVariableSymbol(compilation, bindingsA, "a.vb", "aa$", syntax)
            Assert.NotNull(varSymbol2)
            Assert.Equal("aa", varSymbol2.Name)
            Assert.Equal(SymbolKind.Field, varSymbol2.Kind)
            Assert.Equal("System.String", DirectCast(varSymbol2, FieldSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol2.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol2.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol2.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol2))

            Dim varSymbol3 = GetVariableSymbol(compilation, bindingsA, "a.vb", "b$", syntax)
            Assert.NotNull(varSymbol3)
            Assert.Equal("b", varSymbol3.Name)
            Assert.Equal(SymbolKind.Field, varSymbol3.Kind)
            Assert.Equal("System.String", DirectCast(varSymbol3, FieldSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol3.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol3.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol3.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol3))

            Dim varSymbol4 = GetVariableSymbol(compilation, bindingsA, "a.vb", "b(", syntax)
            Assert.NotNull(varSymbol4)
            Assert.Equal("b", varSymbol4.Name)
            Assert.Equal(SymbolKind.Field, varSymbol4.Kind)
            Assert.Equal("System.Object()", DirectCast(varSymbol4, FieldSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol4.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol4.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol4.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol4))

            Dim varSymbol5 = GetVariableSymbol(compilation, bindingsB, "b.vb", "aa", syntax)
            Assert.NotNull(varSymbol5)
            Assert.Equal("aa", varSymbol5.Name)
            Assert.Equal(SymbolKind.Field, varSymbol5.Kind)
            Assert.Equal("System.String", DirectCast(varSymbol5, FieldSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol5.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol5.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol5.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol5))

            Dim varSymbol6 = GetVariableSymbol(compilation, bindingsB, "b.vb", "yyy", syntax)
            Assert.NotNull(varSymbol6)
            Assert.Equal("yyy", varSymbol6.Name)
            Assert.Equal(SymbolKind.Parameter, varSymbol6.Kind)
            Assert.Equal("System.String", DirectCast(varSymbol6, ParameterSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol6.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol6.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol6.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol6))

            Dim varSymbol7 = GetVariableSymbol(compilation, bindingsB, "b.vb", "ccc$", syntax)
            Assert.NotNull(varSymbol7)
            Assert.Equal("ccc", varSymbol7.Name)
            Assert.Equal(SymbolKind.Local, varSymbol7.Kind)
            Assert.Equal("System.String", DirectCast(varSymbol7, LocalSymbol).Type.ToTestDisplayString())
            Assert.False(DirectCast(varSymbol7, ILocalSymbol).IsFixed)
            Assert.Equal(1, varSymbol7.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol7.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol7.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol7))

            Dim varSymbol8 = GetVariableSymbol(compilation, bindingsB, "b.vb", "ccc as Integer", syntax)
            Assert.NotNull(varSymbol8)
            Assert.Equal("ccc", varSymbol8.Name)
            Assert.Equal(SymbolKind.Local, varSymbol8.Kind)
            Assert.Equal("System.Int32", DirectCast(varSymbol8, LocalSymbol).Type.ToTestDisplayString())
            Assert.Equal(1, varSymbol8.Locations.Length())
            Assert.True(syntax.SpanStart = varSymbol8.Locations.Item(0).SourceSpan.Start OrElse
                        syntax.SpanStart = varSymbol8.Locations.Item(1).SourceSpan.Start,
                        GetStartSpanErrorMessage(syntax, varSymbol8))

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact>
        Public Sub Locals()
            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim $32 = 45
        Dim $45 = 55
    End Sub
End Module
    </file>
</compilation>)

            c.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                Diagnostic(ERRID.ERR_IllegalChar, "$"),
                Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                Diagnostic(ERRID.ERR_IllegalChar, "$"))
        End Sub

        <Fact()>
        Public Sub TestGetDeclaredSymbolArrayField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public F(2) As Integer
End Class
    </file>
</compilation>)

            Dim tree = CompilationUtils.GetTree(compilation, "a.vb")
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToFullString().IndexOf("F(2)", StringComparison.Ordinal)).Parent
            Dim symbol = DirectCast(semanticModel.GetDeclaredSymbol(node), FieldSymbol)
            Assert.Equal("F", symbol.Name)
            Assert.Equal("System.Int32()", symbol.Type.ToTestDisplayString())
        End Sub

        <WorkItem(543476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543476")>
        <Fact()>
        Public Sub BindingLocalConstantValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BindingLocalConstantValue">
        <file name="a.vb">
Imports System            
Module M1
    Sub Main()
        const c1 as integer = 100                                   'BIND:"100"
        const c2 as string = "Hi There"                             'BIND1:""Hi There""
        const c3 As AttributeTargets = AttributeTargets.Property    'BIND2:"AttributeTargets.Property"
        const c5 As DateTime = #2/24/2012#                          'BIND3:"#2/24/2012#"
        const c6 As Decimal = 99.99D                                'BIND4:"99.99D"
        const c7 = nothing                                          'BIND5:"nothing"
        const c8 as string = nothing                                'BIND6:"nothing"

        dim s = sub()
                    const c1 as integer = 100                                   'BIND7:"100"
                    const c2 as string = "Hi There"                             'BIND8:""Hi There""
                    const c3 As AttributeTargets = AttributeTargets.Property    'BIND9:"AttributeTargets.Property"
                    const c5 As DateTime = #2/24/2012#                          'BIND10:"#2/24/2012#"
                    const c6 As Decimal = 99.99D                                'BIND11:"99.99D"
                    const c7 = nothing                                          'BIND12:"nothing"
                    const c8 as string = nothing                                'BIND13:"nothing"
                end sub
    End Sub
End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 0)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(100, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal("Hi There", semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 2)
            Assert.Equal("System.AttributeTargets", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(AttributeTargets.Property, CType(semanticInfo.ConstantValue.Value, AttributeTargets))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 3)
            Assert.Equal("System.DateTime", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(#2/24/2012#, CType(semanticInfo.ConstantValue.Value, DateTime))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 4)
            Assert.Equal("System.Decimal", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(99.99D, CType(semanticInfo.ConstantValue.Value, Decimal))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 5)
            Assert.Equal(Nothing, semanticInfo.Type)
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(Nothing, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 6)
            Assert.Equal(Nothing, semanticInfo.Type)
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(Nothing, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 7)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(100, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 8)
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal("Hi There", semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 9)
            Assert.Equal("System.AttributeTargets", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(AttributeTargets.Property, CType(semanticInfo.ConstantValue.Value, AttributeTargets))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 10)
            Assert.Equal("System.DateTime", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(#2/24/2012#, CType(semanticInfo.ConstantValue.Value, DateTime))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 11)
            Assert.Equal("System.Decimal", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(99.99D, CType(semanticInfo.ConstantValue.Value, Decimal))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 12)
            Assert.Equal(Nothing, semanticInfo.Type)
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(Nothing, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 13)
            Assert.Equal(Nothing, semanticInfo.Type)
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(Nothing, semanticInfo.ConstantValue.Value)
        End Sub

        <WorkItem(543476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543476")>
        <Fact()>
        Public Sub BindingLocalConstantVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BindingLocalConstantVariable">
        <file name="a.vb">
Imports System            
Module M1
    Sub Main()
        const c1 as integer = 100                                   'BIND:"c1"
        const c2 as string = "Hi There"                             'BIND1:"c2"
        const c3 As AttributeTargets = AttributeTargets.Property    'BIND2:"c3"
        const c4 As DateTime = #2/24/2012#                          'BIND3:"c4"
        const c5 As Decimal = 99.99D                                'BIND4:"c5"
        const c6 = nothing                                          'BIND5:"c6"
        const c7 as string = nothing                                'BIND6:"c7"

        dim s = sub()
                    const c1 as integer = 100                                   'BIND7:"c1"
                    const c2 as string = "Hi There"                             'BIND8:"c2"
                    const c3 As AttributeTargets = AttributeTargets.Property    'BIND9:"c3"
                    const c4 As DateTime = #2/24/2012#                          'BIND10:"c4"
                    const c5 As Decimal = 99.99D                                'BIND11:"c5"
                end sub
    End Sub
End Module
    </file>
    </compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 0)
            Dim local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.False(DirectCast(local, ILocalSymbol).IsFixed)
            Assert.True(local.HasConstantValue)
            Assert.Equal(100, CType(local.ConstantValue, Integer))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal("Hi There", CType(local.ConstantValue, String))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(AttributeTargets.Property, CType(local.ConstantValue, AttributeTargets))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(#2/24/2012#, CType(local.ConstantValue, DateTime))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(99.99D, CType(local.ConstantValue, Decimal))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 5)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(Nothing, local.ConstantValue)

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(Nothing, local.ConstantValue)

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(100, CType(local.ConstantValue, Integer))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal("Hi There", CType(local.ConstantValue, String))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 9)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(AttributeTargets.Property, CType(local.ConstantValue, AttributeTargets))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 10)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(#2/24/2012#, CType(local.ConstantValue, DateTime))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 11)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.True(local.HasConstantValue)
            Assert.Equal(99.99D, CType(local.ConstantValue, Decimal))
        End Sub

#End Region

#Region "Regression"

        <WorkItem(540374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540374")>
        <Fact()>
        Public Sub Bug6617()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Imports System
Class C
    Sub M()
        Dim i As Integer = New String(" "c, 10).Length
        Console.Write(i)
        Console.Write(New String(" "c, 10).Length)
    End Sub
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleMemberAccessExpression)
            Dim semanticInfo = model.GetSemanticInfoSummary(CType(node.AsNode(), ExpressionSyntax))
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.NotNull(semanticInfo.Symbol)
            Assert.Equal("ReadOnly Property System.String.Length As System.Int32", semanticInfo.Symbol.ToTestDisplayString())

            node = tree.FindNodeOrTokenByKind(SyntaxKind.SimpleMemberAccessExpression, 4)
            semanticInfo = model.GetSemanticInfoSummary(CType(node.AsNode(), ExpressionSyntax))
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.NotNull(semanticInfo.Symbol)
            Assert.Equal("ReadOnly Property System.String.Length As System.Int32", semanticInfo.Symbol.ToTestDisplayString())

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(540665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540665")>
        <Fact()>
        Public Sub GetSemanticInfoForPartiallyTypedMemberAccessNodes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfoForPartiallyTypedMemberAccessNodes">
    <file name="a.vb">
Imports System   

Namespace NS
    Class Dummy
    End Class

    Class Test
        Function F() As String
            Return Nothing
        End Function
        Property P() As Integer

        Public Sub Main()

            Call NS.

            Call Dummy.

            Call F.

            Call P.

        End Sub
    End Class

End Namespace
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node As SyntaxNode = Nothing
            Dim info As SemanticInfoSummary = Nothing

            '  CHECK: NS.
            node = tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, 3).AsNode()
            Assert.Equal("NS.", node.Parent.ToString().Trim())
            info = model.GetSemanticInfoSummary(CType(node, ExpressionSyntax))
            Assert.Null(info.Type)
            Assert.NotNull(info.Symbol)
            Assert.Equal("NS", info.Symbol.ToString())

            '  CHECK: Dummy.
            node = tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, 5).AsNode()
            Assert.Equal("Dummy.", node.Parent.ToString().Trim())
            info = model.GetSemanticInfoSummary(CType(node, ExpressionSyntax))
            Assert.Equal("NS.Dummy", info.Type.ToString())

            '  CHECK: F.
            node = tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, 7).AsNode()
            Assert.Equal("F.", node.Parent.ToString().Trim())
            info = model.GetSemanticInfoSummary(CType(node, ExpressionSyntax))
            Assert.Equal("String", info.Type.ToString())

            '  CHECK: P.
            node = tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, 9).AsNode()
            Assert.Equal("P.", node.Parent.ToString().Trim())
            info = model.GetSemanticInfoSummary(CType(node, ExpressionSyntax))
            Assert.Equal("Integer", info.Type.ToString())
        End Sub

        <WorkItem(541134, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541134")>
        <Fact()>
        Public Sub Bug7732()

            Dim xml =
                <compilation>
                    <file name="a.vb">
                        Module Program    
                            Delegate Sub D(x As D)    
                        End Module
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)

            Dim model1 = comp.GetSemanticModel(tree)

            Dim delegateDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of DelegateStatementSyntax)().First()
            Dim delegateSymbol = model1.GetDeclaredSymbol(delegateDecl)
            Assert.Equal("Program.D", delegateSymbol.ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Sub

        <WorkItem(541244, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541244")>
        <Fact()>
        Public Sub GetDeclaredSymbolDelegateStatementSyntax()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace Server
        Delegate Function FD(ByVal vStr As String) As String
        Delegate Sub FD2(ByVal vStr As String)
        Class C1
            Delegate Function FD3(ByVal vStr As String) As String
            Delegate Sub FD4(ByVal vStr As String)
        End Class
End Namespace
    </file>
</compilation>, options:=options)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("Delegate", StringComparison.Ordinal)).Parent
            Assert.Equal(SyntaxKind.DelegateFunctionStatement, node.Kind)
            Dim symbol = bindingsA.GetDeclaredSymbol(node)
            Assert.Equal("Goo.Bar.Server.FD", symbol.ToString())

            node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("Delegate", 30, StringComparison.Ordinal)).Parent
            Assert.Equal(SyntaxKind.DelegateSubStatement, node.Kind)
            symbol = bindingsA.GetDeclaredSymbol(node)
            Assert.Equal("Goo.Bar.Server.FD2", symbol.ToString())

            node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("Delegate", 140, StringComparison.Ordinal)).Parent
            Assert.Equal(SyntaxKind.DelegateFunctionStatement, node.Kind)
            symbol = bindingsA.GetDeclaredSymbol(node)
            Assert.Equal("Goo.Bar.Server.C1.FD3", symbol.ToString())

            node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("Delegate", 160, StringComparison.Ordinal)).Parent
            Assert.Equal(SyntaxKind.DelegateSubStatement, node.Kind)
            symbol = bindingsA.GetDeclaredSymbol(node)
            Assert.Equal("Goo.Bar.Server.C1.FD4", symbol.ToString())
        End Sub

        <WorkItem(541379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541379")>
        <Fact()>
        Public Sub GetDeclaredSymbolLambdaParamError()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Goo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
' Imports System  ' Func won't bind without this!
Module Module1
    Sub Main()
        Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam As Integer)
                                                  Return lambdaParam + 1
                                              End Function
    End Sub
End Module
    </file>
</compilation>, options:=options)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim node = treeA.GetCompilationUnitRoot().FindToken(treeA.GetCompilationUnitRoot().ToFullString().IndexOf("lambdaParam", StringComparison.Ordinal)).Parent
            Dim symbol = bindingsA.GetDeclaredSymbolFromSyntaxNode(node)

            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Parameter, symbol.Kind)
            Assert.Equal("lambdaParam As Integer", symbol.ToDisplayString())
        End Sub

        <WorkItem(541425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541425")>
        <Fact()>
        Public Sub NoParenthesisSub()
            Dim xml =
                <compilation>
                    <file name="a.vb">
Class Class
    Sub Bob
    End Sub

    Sub New
    End Sub
End Class

                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(xml)
            Dim tree = comp.SyntaxTrees(0)
            Dim model1 = comp.GetSemanticModel(tree)

            Dim memberSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodStatementSyntax)().First()
            Dim memberSymbol = model1.GetDeclaredSymbol(memberSyntax)
            Assert.Equal("Sub [Class].Bob()", memberSymbol.ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Sub

        <WorkItem(542342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542342")>
        <Fact()>
        Public Sub SourceNamespaceSymbolMergeWithMetadata()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Namespace System
    Partial Public Class PartialClass
        Public Property Prop As Integer
    End Class
End Namespace
    </file>
    <file name="b.vb">
Namespace System
    Partial Public Class PartialClass
        Default Public Property Item(i As Integer) As Integer
            Get
                Return i
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
    </file>
</compilation>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim rootA = treeA.GetCompilationUnitRoot()
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim rootB = treeB.GetCompilationUnitRoot()
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim nsA = DirectCast(rootA.Members(0), NamespaceBlockSyntax)
            Dim nsB = DirectCast(rootB.Members(0), NamespaceBlockSyntax)

            Dim nsSymbolA = bindingsA.GetDeclaredSymbol(nsA)
            Assert.NotNull(nsSymbolA)
            Assert.Equal("System", nsSymbolA.ToTestDisplayString())
            Assert.Equal(3, nsSymbolA.Locations.Length)
            Assert.Equal(GetType(MergedNamespaceSymbol).FullName & "+CompilationMergedNamespaceSymbol", nsSymbolA.GetType().ToString())
            Assert.Equal(NamespaceKind.Compilation, nsSymbolA.NamespaceKind)
            Assert.Equal(2, nsSymbolA.ConstituentNamespaces.Length)
            Assert.True(nsSymbolA.ConstituentNamespaces.Contains(DirectCast(compilation.SourceAssembly.GlobalNamespace.GetMembers("System").First(), NamespaceSymbol)))
            Assert.True(nsSymbolA.ConstituentNamespaces.Contains(DirectCast(compilation.GetReferencedAssemblySymbol(compilation.References(0)).GlobalNamespace.GetMembers("System").First(), NamespaceSymbol)))

            Dim nsSymbolB = bindingsB.GetDeclaredSymbol(nsB)
            Assert.NotNull(nsSymbolB)
            Assert.Equal(nsSymbolA, nsSymbolB)

            Dim memSymbol = compilation.GlobalNamespace.GetMembers("System").Single()
            Assert.Equal(nsSymbolA.Locations.Length, memSymbol.Locations.Length)
            AssertEx.Equal(Of ISymbol)(nsSymbolA, memSymbol)
        End Sub

        <WorkItem(542595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542595")>
        <Fact()>
        Public Sub Bug9881Test()

            Dim xml =
                <compilation name="Bug9881Test">
                    <file name="C.vb">
                        Module Program
                            Sub Main(args As String())
                                Dim a = Function(x) x + 1
                            End Sub
                        End Module
                    </file>
                </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(xml)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)

            Dim binaryOp = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of BinaryExpressionSyntax)().First()
            Assert.NotNull(binaryOp)
            Dim info1 = model.GetSemanticInfoSummary(binaryOp)
        End Sub

        <WorkItem(542702, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542702")>
        <Fact>
        Public Sub Bug10037()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module M
    Sub M()
        Try
        Catch ex As Exception When (Function(function(Function(functio
        End Try
    End Sub
End Module
]]></file>
</compilation>, {SystemCoreRef})
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim text = "Function(function(Function(func"
            Dim position = FindPositionFromText(tree, text) + text.Length - 1
            Dim node = tree.GetCompilationUnitRoot().FindToken(position).Parent
            Dim identifier = DirectCast(node, ModifiedIdentifierSyntax)
            Dim symbol = model.GetDeclaredSymbol(identifier)
            CheckSymbol(symbol, "functio As Object")
        End Sub

        <WorkItem(543605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543605")>
        <Fact()>
        Public Sub TestGetDeclaredSymbolFromParameterInLambdaExprOfAddHandlerStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TestGetDeclaredSymbolFromEventParameter">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
	    AddHandler Function(ByVal x) x
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30677: 'AddHandler' or 'RemoveHandler' statement event operand must be a dot-qualified expression or a simple name.
	    AddHandler Function(ByVal x) x
                ~~~~~~~~~~~~~~~~~~~
BC30196: Comma expected.
	    AddHandler Function(ByVal x) x
                                   ~
BC30201: Expression expected.
	    AddHandler Function(ByVal x) x
                                   ~
]]></expected>)

            Dim tree = CompilationUtils.GetTree(compilation, "a.vb")
            Dim model = compilation.GetSemanticModel(tree)
            Dim syntax As ParameterSyntax = Nothing
            Dim paramSymbol1 = GetParameterSymbol(compilation, model, "a.vb", "ByVal x", syntax)

            Assert.NotNull(paramSymbol1)
            Assert.Equal("x As System.Object", paramSymbol1.ToTestDisplayString())
        End Sub

        <WorkItem(543666, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543666")>
        <Fact()>
        Public Sub BindingLocalConstantObjectVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BindingLocalConstantObjectVariable">
        <file name="a.vb">
Option Strict On
Imports System

Module M1
    Class C
        Function F() As Object
            Const c1 As Integer = 100                             'BIND:"c1"
            Const c2 As Object = "Hi" &amp; " There"              'BIND1:"c2"
            Const c3 As Object = AttributeTargets.Property        'BIND2:"c3"
            Const c4 As Object = #12/12/2012 1:23:45AM #          'BIND3:"c4"
            Const c5 As Object = 99.99D                           'BIND4:"c5"
            Const c6 As Object = Nothing                          'BIND5:"c6"
            Dim an = New With {.p1 = c1, Key .p2 = c2, .p3 = c3, .p4 = c4, Key .p5 = c5, .p6 = c6}  
            Return an
        End Function
    End Class

    Sub Main()
        Dim s = Function() As Object
                    Const c1 As Object = -100                             'BIND6:"c1"
                    Const c2 As Object = "q"c                             'BIND7:"c2"
                    Const c3 As Object = True                             'BIND8:"c3"
                    Const c4 As Object = 12345!                           'BIND9:"c4"
                    Const c5 As Object = CByte(255)                       'BIND10:"c5"
                    Const c6 As Object = Nothing + Nothing                'BIND11:"c6"
                    Dim an = New With {.p1 = c1, Key .p2 = c2, .p3 = c3, .p4 = c4, Key .p5 = c5, .p6 = c6}
                    Return an
                End Function
    End Sub
End Module
    </file>
    </compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 0)
            Dim local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Int32), local.Type)
            Assert.Equal(100, CType(local.ConstantValue, Integer))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_String), local.Type)
            Assert.Equal("Hi There", CType(local.ConstantValue, String))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(TypeKind.Enum, local.Type.TypeKind)
            Assert.Equal(AttributeTargets.Property, CType(local.ConstantValue, AttributeTargets))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_DateTime), local.Type)
            Assert.Equal(#12/12/2012 1:23:45 AM#, CType(local.ConstantValue, DateTime))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Decimal), local.Type)
            Assert.Equal(99.99D, CType(local.ConstantValue, Decimal))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 5)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Object), local.Type)
            Assert.Equal(Nothing, local.ConstantValue)

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Int32), local.Type)
            Assert.Equal(-100, CType(local.ConstantValue, Integer))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Char), local.Type)
            Assert.Equal("q"c, CType(local.ConstantValue, Char))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Boolean), local.Type)
            Assert.Equal(True, CType(local.ConstantValue, Boolean))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 9)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Single), local.Type)
            Assert.Equal(12345.0!, CType(local.ConstantValue, Single))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 10)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Byte), local.Type)
            Assert.Equal(255, CType(local.ConstantValue, Byte))

            expressionSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 11)
            local = DirectCast(model.GetDeclaredSymbol(expressionSyntax), LocalSymbol)
            Assert.True(local.IsConst)
            Assert.Equal(compilation.GetSpecialType(System_Int32), local.Type)
            Assert.Equal(0, CType(local.ConstantValue, Integer))
        End Sub

        <Fact(), WorkItem(545106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545106")>
        Public Sub GetDeclaredSymbolForDeclaredControlVariable()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Friend Module M
    Sub Main(args As String())
        For x? As SByte = Nothing To Nothing
            Console.Write("Hi")
        Next

        For Each y As String In args

        Next
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of ModifiedIdentifierSyntax)()
            Assert.Equal(3, nodes.Count)
            ' x?
            Dim node = nodes(1)
            Assert.Equal("x?", node.ToString())
            Dim sym = semanticModel.GetDeclaredSymbol(node)
            Assert.NotNull(sym)
            Assert.Equal(SymbolKind.Local, sym.Kind)
            Assert.False(DirectCast(sym, ILocalSymbol).IsForEach)
            ' y
            node = nodes.Last()
            Assert.Equal("y", node.ToString())
            sym = semanticModel.GetDeclaredSymbol(node)
            Assert.NotNull(sym)
            Assert.Equal(SymbolKind.Local, sym.Kind)
            Assert.True(DirectCast(sym, ILocalSymbol).IsForEach)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(611537, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611537")>
        <Fact()>
        Public Sub Bug611537()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class 
Namespace
Property 'BIND1:"Property"
    </file>
    </compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of PropertyStatementSyntax)()
            Assert.Null(semanticModel.GetDeclaredSymbol(nodes(0)))
        End Sub

        <Fact(), WorkItem(747446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747446")>
        Public Sub TestGetDeclaredSymbolWithChangedRootNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Interface I1
    Function F() As String
End Interface
    </file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim fSyntax1 = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.FunctionStatement, 1).AsNode(), MethodStatementSyntax)
            Dim fSymbol1 = model.GetDeclaredSymbol(fSyntax1)
            Assert.NotNull(fSymbol1)
            Assert.Equal("Function I1.F() As System.String", fSymbol1.ToTestDisplayString())

            ' Create a cloned compilation with a different root namespace and perform the same semantic queries.
            compilation = compilation.WithOptions(compilation.Options.WithRootNamespace("NewNs"))

            Dim newTree = compilation.SyntaxTrees(0)
            Dim newModel = compilation.GetSemanticModel(newTree)

            Dim fSyntax2 = DirectCast(newTree.FindNodeOrTokenByKind(SyntaxKind.FunctionStatement, 1).AsNode(), MethodStatementSyntax)
            Dim fSymbol2 = newModel.GetDeclaredSymbol(fSyntax2)
            Assert.NotNull(fSymbol2)
            Assert.Equal("Function NewNs.I1.F() As System.String", fSymbol2.ToTestDisplayString())

            ' Create a cloned compilation without a root namespace and perform the same semantic queries.
            compilation = compilation.WithOptions(compilation.Options.WithRootNamespace(Nothing))

            newTree = compilation.SyntaxTrees(0)
            newModel = compilation.GetSemanticModel(newTree)

            fSyntax2 = DirectCast(newTree.FindNodeOrTokenByKind(SyntaxKind.FunctionStatement, 1).AsNode(), MethodStatementSyntax)
            fSymbol2 = newModel.GetDeclaredSymbol(fSyntax2)
            Assert.NotNull(fSymbol2)
            Assert.Equal("Function I1.F() As System.String", fSymbol2.ToTestDisplayString())
        End Sub

        <Fact(), WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        Public Sub TestGetDeclaredSymbolWithIncompleteDeclaration()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compilation">
    <file name="a.vb">
Class C0
End Class

Class 

Class C1
End Class
    </file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim syntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.ClassStatement, 2).AsNode(), ClassStatementSyntax)
            Dim symbol = model.GetDeclaredSymbol(syntax)
            Assert.NotNull(symbol)
            Assert.Equal("?", symbol.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, symbol.TypeKind)
        End Sub
#End Region

    End Class

End Namespace
