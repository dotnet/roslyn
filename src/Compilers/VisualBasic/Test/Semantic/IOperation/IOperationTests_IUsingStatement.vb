﻿Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        <WorkItem(19819, "https://github.com/dotnet/roslyn/issues/19819")>
        Public Sub UsingDeclarationSyntaxNotNull()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Module Module1
    Class C1
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub S1()
        Using D1 as New C1()
        End Using
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingStatement)

            Assert.NotNull(op.Declaration.Syntax)
            Assert.Same(node.UsingStatement, op.Declaration.Syntax)
        End Sub

        <Fact>
        <WorkItem(19887, "https://github.com/dotnet/roslyn/issues/19887")>
        Public Sub UsingDeclarationIncompleteUsingNullDeclaration()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Module Module1
    Class C1
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub S1()
        Using
        End Using
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            CompilationUtils.AssertTheseDiagnostics(comp,
                                        <expected>
BC30201: Expression expected.
        Using
             ~
                                        </expected>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingStatement)

            Assert.Null(op.Declaration)
        End Sub

        <Fact>
        <WorkItem(19887, "https://github.com/dotnet/roslyn/issues/19887")>
        Public Sub UsingDeclarationExistingVariableNullDeclaration()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Imports System
Module Module1
    Class C1
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub S1()
        Dim x = New C1()
        Using x
        End Using
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            CompilationUtils.AssertNoDiagnostics(comp)

            Dim tree = comp.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of UsingBlockSyntax).Single()
            Dim op = DirectCast(comp.GetSemanticModel(tree).GetOperationInternal(node), IUsingStatement)

            Assert.Null(op.Declaration)
        End Sub
    End Class
End Namespace
