' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class GetExtendedSemanticInfoTests

        <Fact()>
        Public Sub Query1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q 'BIND6:"s"
                           Where s > 0 'BIND3:"s"
                           Where 10 > s 'BIND2:"s"
                           Where DirectCast(Function()
                                                System.Console.WriteLine(s) 'BIND1:"s"
                                                Return True
                                            End Function, Func(Of Boolean)).Invoke()

        Dim q2 As Object = From s In 'BIND7:"s"
                           (From s In q 'BIND8:"s"
                            Where s > 0) 'BIND5:"s"
                           Where 10 > s 'BIND4:"s"
    End Sub
End Module
    ]]></file>
</compilation>)

            'Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            '----------------------------
            Dim semanticModel1 = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node1, node2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)
            Assert.NotEqual(node1, node3)
            Assert.NotEqual(node2, node3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node6 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
            Dim s6 = semanticModel1.GetDeclaredSymbol(node6)
            Assert.Same(s1, s6)

            Assert.Same(s6, semanticModel1.GetDeclaredSymbol(DirectCast(node6, VisualBasicSyntaxNode)))
            Assert.Same(s6, semanticModel1.GetDeclaredSymbol(DirectCast(node6.Parent, CollectionRangeVariableSyntax)))
            Assert.Same(s6, semanticModel1.GetDeclaredSymbol(node6.Parent))

            Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            Dim s7 = DirectCast(semanticModel1.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.NotSame(s1, s7)
            Assert.Equal("s", s7.Name)
            Assert.Equal("System.Int32", s7.Type.ToTestDisplayString())

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s3 = semanticInfo.Symbol

            Assert.Same(s7, s3)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node5, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s4 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s4)
            Assert.NotSame(s3, s4)

            Assert.Equal("s", s4.Name)
            Assert.Equal("System.Int32", s4.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            Assert.Same(s4, semanticModel1.GetDeclaredSymbol(node8))

            '----------------------------
            Dim semanticModel2 = compilation.GetSemanticModel(tree)
            Assert.NotSame(semanticModel1, semanticModel2)

            Dim node_3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s2)

            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Int32", s2.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node_3, node_2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Assert.NotEqual(node_3, node_1)
            Assert.NotEqual(node_2, node_1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub Query2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim lambda As Action =
            Sub()
                Dim q1 As Object = From s In q 'BIND6:"s"
                                   Where s > 0 'BIND3:"s"
                                   Where 10 > s 'BIND2:"s"
                                   Where DirectCast(Function()
                                                        System.Console.WriteLine(s) 'BIND1:"s"
                                                        Return True
                                                    End Function, Func(Of Boolean)).Invoke()

                Dim q2 As Object = From s In 'BIND7:"s"
                                   (From s In q 'BIND8:"s"
                                    Where s > 0) 'BIND5:"s"
                                   Where 10 > s 'BIND4:"s"
            End Sub
    End Sub
End Module
    ]]></file>
</compilation>)

            'Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            '----------------------------
            Dim semanticModel1 = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node1, node2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)
            Assert.NotEqual(node1, node3)
            Assert.NotEqual(node2, node3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node6 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
            Dim s6 = semanticModel1.GetDeclaredSymbol(node6)
            Assert.Same(s1, s6)

            Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            Dim s7 = DirectCast(semanticModel1.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.NotSame(s1, s7)
            Assert.Equal("s", s7.Name)
            Assert.Equal("System.Int32", s7.Type.ToTestDisplayString())

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s3 = semanticInfo.Symbol

            Assert.Same(s7, s3)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node5, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s4 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s4)
            Assert.NotSame(s3, s4)

            Assert.Equal("s", s4.Name)
            Assert.Equal("System.Int32", s4.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            Assert.Same(s4, semanticModel1.GetDeclaredSymbol(node8))

            '----------------------------
            Dim semanticModel2 = compilation.GetSemanticModel(tree)
            Assert.NotSame(semanticModel1, semanticModel2)

            Dim node_3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s2)

            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Int32", s2.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node_3, node_2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Assert.NotEqual(node_3, node_1)
            Assert.NotEqual(node_2, node_1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub Query3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program

    Dim q As New QueryAble()
    Dim q1 As Object = From s In q 'BIND6:"s"
                        Where s > 0 'BIND3:"s"
                        Where 10 > s 'BIND2:"s"
                        Where DirectCast(Function()
                                            System.Console.WriteLine(s) 'BIND1:"s"
                                            Return True
                                        End Function, Func(Of Boolean)).Invoke()

    Dim q2 As Object = From s In 'BIND7:"s"
                        (From s In q 'BIND8:"s"
                        Where s > 0) 'BIND5:"s"
                        Where 10 > s 'BIND4:"s"

    Sub Main(args As String())
    End Sub
End Module
    ]]></file>
</compilation>)

            'Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            '----------------------------
            Dim semanticModel1 = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node1, node2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)
            Assert.NotEqual(node1, node3)
            Assert.NotEqual(node2, node3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node6 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
            Dim s6 = semanticModel1.GetDeclaredSymbol(node6)
            Assert.Same(s1, s6)

            Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            Dim s7 = DirectCast(semanticModel1.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.NotSame(s1, s7)
            Assert.Equal("s", s7.Name)
            Assert.Equal("System.Int32", s7.Type.ToTestDisplayString())

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s3 = semanticInfo.Symbol

            Assert.Same(s7, s3)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node5, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s4 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s4)
            Assert.NotSame(s3, s4)

            Assert.Equal("s", s4.Name)
            Assert.Equal("System.Int32", s4.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            Assert.Same(s4, semanticModel1.GetDeclaredSymbol(node8))

            '----------------------------
            Dim semanticModel2 = compilation.GetSemanticModel(tree)
            Assert.NotSame(semanticModel1, semanticModel2)

            Dim node_3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s2)

            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Int32", s2.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node_3, node_2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Assert.NotEqual(node_3, node_1)
            Assert.NotEqual(node_2, node_1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub Query4()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function

    Public Function SkipWhile(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function

    Public Function Take(x As Integer) As QueryAble
        Return Me
    End Function

    Public Function Skip(x As Integer) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim lambda1 As Func(Of Object) =
            Function() From s In q 'BIND6:"s"
                       Where s > 0 'BIND3:"s"
                       Where 10 > s 'BIND2:"s"
                       Where DirectCast(Function() s > 1, Func(Of Boolean)).Invoke() 'BIND1:"s"

        Dim lambda2 As Func(Of Object) =
            Function() From s In 'BIND7:"s"
                       (From s In q 'BIND8:"s"
                        Where s > 0) 'BIND5:"s"
                       Where 10 > s 'BIND4:"s"
            
        Dim q2 As Object = From s In q Where CByte(s) 'BIND9:"Where CByte(s)"
            
        Dim q3 As Object = From s In q Take While s > 0 'BIND10:"Take While s > 0"
            
        Dim q4 As Object = From s In q Skip While s > 0 'BIND11:"Skip While s > 0"

        Dim q5 As Object = From s In q Take 1 'BIND12:"Take 1"

        Dim q6 As Object = From s In q Skip 1 'BIND13:"Skip 1"
    End Sub
End Module
    ]]></file>
</compilation>)

            'Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            '----------------------------
            Dim semanticModel1 = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node1, node2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)
            Assert.NotEqual(node1, node3)
            Assert.NotEqual(node2, node3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s1, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node6 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
            Dim s6 = semanticModel1.GetDeclaredSymbol(node6)
            Assert.Same(s1, s6)

            Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            Dim s7 = DirectCast(semanticModel1.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.NotSame(s1, s7)
            Assert.Equal("s", s7.Name)
            Assert.Equal("System.Int32", s7.Type.ToTestDisplayString())

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s3 = semanticInfo.Symbol

            Assert.Same(s7, s3)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel1, TryCast(node5, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s4 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s4)
            Assert.NotSame(s3, s4)

            Assert.Equal("s", s4.Name)
            Assert.Equal("System.Int32", s4.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            Assert.Same(s4, semanticModel1.GetDeclaredSymbol(node8))

            '----------------------------
            Dim semanticModel2 = compilation.GetSemanticModel(tree)
            Assert.NotSame(semanticModel1, semanticModel2)

            Dim node_3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_3, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.NotSame(s1, s2)

            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Int32", s2.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.NotEqual(node_3, node_2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_2, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node_1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Assert.NotEqual(node_3, node_1)
            Assert.NotEqual(node_2, node_1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel2, TryCast(node_1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Same(s2, semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node9 As WhereClauseSyntax = CompilationUtils.FindBindingText(Of WhereClauseSyntax)(compilation, "a.vb", 9)
            Dim symbolInfo = semanticModel2.GetSymbolInfo(node9)

            Assert.Equal("Function QueryAble.Where(x As System.Func(Of System.Int32, System.Byte)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node10 As PartitionWhileClauseSyntax = CompilationUtils.FindBindingText(Of PartitionWhileClauseSyntax)(compilation, "a.vb", 10)
            symbolInfo = semanticModel2.GetSymbolInfo(node10)

            Assert.Equal("Function QueryAble.TakeWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node11 As PartitionWhileClauseSyntax = CompilationUtils.FindBindingText(Of PartitionWhileClauseSyntax)(compilation, "a.vb", 11)
            symbolInfo = semanticModel2.GetSymbolInfo(node11)

            Assert.Equal("Function QueryAble.SkipWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node12 As PartitionClauseSyntax = CompilationUtils.FindBindingText(Of PartitionClauseSyntax)(compilation, "a.vb", 12)
            symbolInfo = semanticModel2.GetSymbolInfo(node12)

            Assert.Equal("Function QueryAble.Take(x As System.Int32) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node13 As PartitionClauseSyntax = CompilationUtils.FindBindingText(Of PartitionClauseSyntax)(compilation, "a.vb", 13)
            symbolInfo = semanticModel2.GetSymbolInfo(node13)

            Assert.Equal("Function QueryAble.Skip(x As System.Int32) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub Query5()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q 
                           Where s > 0 'BIND:"s > 0"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of BinaryExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Byte", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.NarrowingBoolean, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.Int32.op_GreaterThan(left As System.Int32, right As System.Int32) As System.Boolean",
                         semanticInfo.Symbol.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub Select1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function

    Public Function Join(inner As QueryAble, outer As Func(Of Integer, Integer), inner As Func(Of Integer, Integer), x as Func(Of Integer, Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Distinct() As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q 'BIND2:"s"
                           Select x = s+1 'BIND1:"s"
                           Select y = x 'BIND3:"y = x"
                           Where y > 0 'BIND4:"y"
                           Select y 'BIND5:"y"
                           Where y > 0 'BIND6:"y"
                           Select y = y 'BIND7:"y = y"
                           Where y > 0 'BIND8:"y"
                           Select y + 1 'BIND9:"y + 1"
                           Select z = 1 'BIND10:"z"

        q1 = From s In q Select s 'BIND11:"Select s"

        q1 = From s In q Select CStr(s) 'BIND12:"Select CStr(s)"

        q1 = From s In q Take 1 Select s + 1 'BIND13:"Select s + 1"

        q1 = From s1 In q, s2 In q Select s1 + s2 'BIND14:"Select s1 + s2"

        q1 = From s1 In q Join s2 In q On s1 Equals s2 'BIND15:"Join s2 In q On s1 Equals s2"
             Select s1 + s2 'BIND16:"Select s1 + s2"

        q1 = From s In q Distinct 'BIND17:"Distinct"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
            Assert.Same(s1, semanticModel.GetDeclaredSymbol(node2))

            Dim node3 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 3)
            Dim y1 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("y", y1.Name)
            Assert.Equal("System.Int32", y1.Type.ToTestDisplayString())

            Dim symbolInfo As SymbolInfo
            symbolInfo = semanticModel.GetSymbolInfo(node3)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node4, ExpressionSyntax))
            Assert.Same(y1, semanticInfo.Symbol)

            Dim node5_1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 5)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node5_1, ExpressionSyntax))
            Assert.Same(y1, semanticInfo.Symbol)

            Dim node5_2 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 5)
            Dim y2 = DirectCast(semanticModel.GetDeclaredSymbol(node5_2), RangeVariableSymbol)
            Assert.Equal("y", y2.Name)
            Assert.Equal("System.Int32", y2.Type.ToTestDisplayString())

            Assert.NotSame(y1, y2)

            Dim node6 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 6)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node6, ExpressionSyntax))
            Assert.Same(y2, semanticInfo.Symbol)

            Dim node7 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 7)
            Dim y3 = DirectCast(semanticModel.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.Equal("y", y3.Name)
            Assert.Equal("System.Int32", y3.Type.ToTestDisplayString())

            Assert.NotSame(y1, y3)
            Assert.NotSame(y2, y3)

            Assert.Same(y3, semanticModel.GetDeclaredSymbol(DirectCast(node7, VisualBasicSyntaxNode)))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, node7.Expression)
            Assert.Same(y2, semanticInfo.Symbol)

            Dim node8 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 8)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node8, ExpressionSyntax))
            Assert.Same(y3, semanticInfo.Symbol)

            Dim node9 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 9)
            Assert.Null(semanticModel.GetDeclaredSymbol(node9))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, node9.Expression)

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.Int32.op_CheckedAddition(left As System.Int32, right As System.Int32) As System.Int32",
                         semanticInfo.Symbol.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node10 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 10)
            Dim z = DirectCast(semanticModel.GetDeclaredSymbol(node10), RangeVariableSymbol)
            Assert.Equal("z", z.Name)
            Assert.Equal("System.Int32", z.Type.ToTestDisplayString())

            Assert.Same(z, semanticModel.GetDeclaredSymbol(DirectCast(node10, VisualBasicSyntaxNode)))

            Dim commonSymbolInfo As SymbolInfo

            Dim node11 As SelectClauseSyntax = CompilationUtils.FindBindingText(Of SelectClauseSyntax)(compilation, "a.vb", 11)
            symbolInfo = semanticModel.GetSymbolInfo(node11)

            Assert.Equal("Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            commonSymbolInfo = semanticModel.GetSymbolInfo(DirectCast(node11, SyntaxNode))

            Assert.Same(symbolInfo.Symbol, commonSymbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, commonSymbolInfo.CandidateReason)
            Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)

            Dim node12 As SelectClauseSyntax = CompilationUtils.FindBindingText(Of SelectClauseSyntax)(compilation, "a.vb", 12)
            symbolInfo = semanticModel.GetSymbolInfo(node12)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim node13 As SelectClauseSyntax = CompilationUtils.FindBindingText(Of SelectClauseSyntax)(compilation, "a.vb", 13)
            symbolInfo = semanticModel.GetSymbolInfo(node13)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node14 As SelectClauseSyntax = CompilationUtils.FindBindingText(Of SelectClauseSyntax)(compilation, "a.vb", 14)
            symbolInfo = semanticModel.GetSymbolInfo(node14)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node15 As SimpleJoinClauseSyntax = CompilationUtils.FindBindingText(Of SimpleJoinClauseSyntax)(compilation, "a.vb", 15)
            symbolInfo = semanticModel.GetSymbolInfo(node15)

            Assert.Equal("Function QueryAble.Join(inner As QueryAble, outer As System.Func(Of System.Int32, System.Int32), inner As System.Func(Of System.Int32, System.Int32), x As System.Func(Of System.Int32, System.Int32, System.Int32)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node16 As SelectClauseSyntax = CompilationUtils.FindBindingText(Of SelectClauseSyntax)(compilation, "a.vb", 16)
            symbolInfo = semanticModel.GetSymbolInfo(node16)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node17 As DistinctClauseSyntax = CompilationUtils.FindBindingText(Of DistinctClauseSyntax)(compilation, "a.vb", 17)
            symbolInfo = semanticModel.GetSymbolInfo(node17)

            Assert.Equal("Function QueryAble.Distinct() As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub ImplicitSelect1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    <System.Runtime.CompilerServices.Extension()>
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As Integer In q Where s > 1 'BIND2:"q"
        System.Console.WriteLine("------")
        Dim q2 As Object = From s As Long In q Where s > 1 'BIND1:"q"
        System.Console.WriteLine("------")
        Dim q3 As Object = From s In q 'BIND3:"From s In q"
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("QueryAble", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("QueryAble", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, LocalSymbol)
            Assert.Equal("q", s1.Name)
            Assert.Equal("QueryAble", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax))
            Assert.Same(s1, semanticInfo.Symbol)

            Dim node3 As QueryExpressionSyntax = CompilationUtils.FindBindingText(Of QueryExpressionSyntax)(compilation, "a.vb", 3)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node3, ExpressionSyntax))

            Assert.Equal("QueryAble", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub OrderBy1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From x In q
                           Order By x, 'BIND1:"x"
                                    x+1, 'BIND2:"x"
                                    x Descending 'BIND3:"x"
                           Order By x Descending 'BIND4:"x"
                           Select x 'BIND5:"x"

        Dim q2 As Object = From x In q
                           Order By x 'BIND6:"Order By x"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("x", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            For i As Integer = 2 To 5
                Dim node2to5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", i)

                semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2to5, ExpressionSyntax))

                Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
                Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
                Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
                Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

                Assert.Same(s1, semanticInfo.Symbol)

                Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
                Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

                Assert.Equal(0, semanticInfo.MemberGroup.Length)

                Assert.False(semanticInfo.ConstantValue.HasValue)
            Next

            Dim node6 As OrderByClauseSyntax = CompilationUtils.FindBindingText(Of OrderByClauseSyntax)(compilation, "a.vb", 6)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node6)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim orderBy = DirectCast(node1.Parent.Parent, OrderByClauseSyntax)

            For Each ordering In orderBy.Orderings
                symbolInfo = semanticModel.GetSymbolInfo(ordering)

                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Dim commonSymbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(ordering)
                Assert.Null(commonSymbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, commonSymbolInfo.CandidateReason)
                Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)
            Next
        End Sub

        <Fact()>
        Public Sub OrderBy2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From x In q
                           Order By x, 'BIND1:"x"
                                    x+1 'BIND2:"x+1"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As OrderingSyntax = CompilationUtils.FindBindingText(Of OrderingSyntax)(compilation, "a.vb", 1)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node1)

            Assert.Equal("Function QueryAble.OrderBy(x As System.Func(Of System.Int32, System.Int32)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim commonSymbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)
            Assert.Same(symbolInfo.Symbol, commonSymbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, commonSymbolInfo.CandidateReason)
            Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)

            Dim node2 As OrderingSyntax = CompilationUtils.FindBindingText(Of OrderingSyntax)(compilation, "a.vb", 2)
            symbolInfo = semanticModel.GetSymbolInfo(node2)

            Assert.Equal("Function QueryAble.ThenBy(x As System.Func(Of System.Int32, System.Int32)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            commonSymbolInfo = semanticModel.GetSymbolInfo(node2)
            Assert.Same(symbolInfo.Symbol, commonSymbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, commonSymbolInfo.CandidateReason)
            Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub Select2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q 
                           Select x = s, 'BIND1:"s"
                                  y = s, 'BIND2:"s"
                                  z = s, 'BIND3:"z = s"
                                  w = s, 'BIND4:"w"
                                  s,     'BIND5:"s"
                                  z = s  'BIND6:"z = s"
                           Where z > 0 'BIND7:"z"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Assert.Same(s1, CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax)).Symbol)

            Dim node3 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 3)
            Dim z1 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("z", z1.Name)
            Assert.Equal("System.Int32", z1.Type.ToTestDisplayString())

            Dim node4 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
            Dim w1 = DirectCast(semanticModel.GetDeclaredSymbol(node4), RangeVariableSymbol)
            Assert.Equal("w", w1.Name)
            Assert.Equal("System.Int32", w1.Type.ToTestDisplayString())

            Dim node5 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 5)
            Dim s2 = DirectCast(semanticModel.GetDeclaredSymbol(node5), RangeVariableSymbol)
            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Int32", s2.Type.ToTestDisplayString())
            Assert.NotSame(s1, s2)

            Dim node6 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 6)
            Dim z2 = DirectCast(semanticModel.GetDeclaredSymbol(node6), RangeVariableSymbol)
            Assert.Equal("z", z2.Name)
            Assert.Equal("System.Int32", z2.Type.ToTestDisplayString())

            Assert.NotSame(z1, z2)

            Dim node7 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 7)
            Assert.Same(z1, CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node7, ExpressionSyntax)).Symbol)

        End Sub

        <Fact()>
        Public Sub Let1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q 
                           Let x = s, 'BIND1:"s"
                               y = x+1, 'BIND2:"x"
                               x = s, 'BIND3:"x = s"
                               w = x  'BIND4:"x"

        Dim q2 As Object = From s In q 
                           Let x = s 'BIND5:"Let x = s"

        q2 = From s In q, s2 In q 
             Let x = s 'BIND6:"x = s"

        q2 = From s In q Join s2 In q On s Equals s2
             Let x = s 'BIND7:"x = s"

        q2 = From s In q Select s+1 
             Let x = 1 'BIND8:"x = 1"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node1.Parent), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("System.Int32", x1.Type.ToTestDisplayString())

            Assert.Same(x1, semanticModel.GetDeclaredSymbol(DirectCast(node1.Parent, ExpressionRangeVariableSyntax).NameEquals.Identifier))

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax))
            Assert.Same(x1, semanticInfo.Symbol)

            Dim node3 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 3)
            Dim x2 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("x", x2.Name)
            Assert.Equal("System.Int32", x1.Type.ToTestDisplayString())

            Assert.NotSame(x1, x2)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node3)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node4, ExpressionSyntax))
            Assert.Same(x1, semanticInfo.Symbol)

            Dim w1 = DirectCast(semanticModel.GetDeclaredSymbol(node4.Parent), RangeVariableSymbol)
            Assert.Equal("w", w1.Name)
            Assert.Equal("System.Int32", w1.Type.ToTestDisplayString())

            Assert.Same(w1, semanticModel.GetDeclaredSymbol(DirectCast(node4.Parent, ExpressionRangeVariableSyntax).NameEquals.Identifier))

            Dim node5 As LetClauseSyntax = CompilationUtils.FindBindingText(Of LetClauseSyntax)(compilation, "a.vb", 5)
            symbolInfo = semanticModel.GetSymbolInfo(node5)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node6 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 6)
            symbolInfo = semanticModel.GetSymbolInfo(node6)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim commonSymbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node6)
            Assert.Null(commonSymbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, commonSymbolInfo.CandidateReason)
            Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)

            Dim node7 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 7)
            symbolInfo = semanticModel.GetSymbolInfo(node7)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node8 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 8)
            symbolInfo = semanticModel.GetSymbolInfo(node8)

            Assert.Equal("Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            commonSymbolInfo = semanticModel.GetSymbolInfo(node8)
            Assert.Same(symbolInfo.Symbol, commonSymbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, commonSymbolInfo.CandidateReason)
            Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub Let2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q1 As Object = From s In qi 
                           Let x = s, 'BIND1:"x = s"
                               y = x+1, 'BIND2:"y = x+1"
                               x = s 'BIND3:"x = s"

        Dim q2 As Object = From s In qi Join s2 In qi On s Equals s2 'BIND7:"Join s2 In qi On s Equals s2"
                           Let x = s, 'BIND4:"x = s"
                               y = x+1, 'BIND5:"y = x+1"
                               x = s 'BIND6:"x = s"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 1)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node1)

            Assert.Equal("Function QueryAble(Of System.Int32).Select(Of <anonymous type: Key s As System.Int32, Key x As System.Int32>)(x As System.Func(Of System.Int32, <anonymous type: Key s As System.Int32, Key x As System.Int32>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key x As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node2 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 2)
            symbolInfo = semanticModel.GetSymbolInfo(node2)

            Assert.Equal("Function QueryAble(Of <anonymous type: Key s As System.Int32, Key x As System.Int32>).Select(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As System.Int32>, Key y As System.Int32>)(x As System.Func(Of <anonymous type: Key s As System.Int32, Key x As System.Int32>, <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As System.Int32>, Key y As System.Int32>)) As QueryAble(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As System.Int32>, Key y As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node3 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 3)
            symbolInfo = semanticModel.GetSymbolInfo(node3)

            Assert.Equal("Function QueryAble(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As System.Int32>, Key y As System.Int32>).Select(Of <anonymous type: Key s As System.Int32, Key x As System.Int32, Key y As System.Int32, Key $861 As System.Int32>)(x As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As System.Int32>, Key y As System.Int32>, <anonymous type: Key s As System.Int32, Key x As System.Int32, Key y As System.Int32, Key $861 As System.Int32>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key x As System.Int32, Key y As System.Int32, Key $861 As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node4 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 4)
            symbolInfo = semanticModel.GetSymbolInfo(node4)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node5 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 5)
            symbolInfo = semanticModel.GetSymbolInfo(node5)

            Assert.Equal("Function QueryAble(Of <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>).Select(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>, Key y As System.Int32>)(x As System.Func(Of <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>, <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>, Key y As System.Int32>)) As QueryAble(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>, Key y As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node6 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 6)
            symbolInfo = semanticModel.GetSymbolInfo(node6)

            Assert.Equal("Function QueryAble(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>, Key y As System.Int32>).Select(Of <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32, Key y As System.Int32, Key $1136 As System.Int32>)(x As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>, Key y As System.Int32>, <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32, Key y As System.Int32, Key $1136 As System.Int32>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32, Key y As System.Int32, Key $1136 As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node7 As SimpleJoinClauseSyntax = CompilationUtils.FindBindingText(Of SimpleJoinClauseSyntax)(compilation, "a.vb", 7)
            symbolInfo = semanticModel.GetSymbolInfo(node7)

            Assert.Equal("Function QueryAble(Of System.Int32).Join(Of System.Int32, System.Int32, <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>)(inner As QueryAble(Of System.Int32), outerKey As System.Func(Of System.Int32, System.Int32), innerKey As System.Func(Of System.Int32, System.Int32), x As System.Func(Of System.Int32, System.Int32, <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key s2 As System.Int32, Key x As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub BindingMemberAccessInsideQuery()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="BindingMemberAccessInsideQuery">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(x As Integer)
        Dim xxx = From i In New Integer() {1, 2, x.ToString().Length } Select y = x 'BIND1:"x.ToString"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node As MemberAccessExpressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Public Overrides Function ToString() As String", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub BindingMemberAccessInsideQuery2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="BindingMemberAccessInsideQuery2">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(x As String)
        Dim xxx = From i In New Integer() {1, 2, x.Length } Select y = x 'BIND1:"x.Length"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node As MemberAccessExpressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Public Overloads ReadOnly Property Length As Integer", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub BindingMemberAccessInsideQuery3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="BindingMemberAccessInsideQuery3">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(x As Integer)
        Dim xxx = From i In New Integer() {1, 2 } Select y = x.ToString.Length 'BIND1:"x.ToString"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node As MemberAccessExpressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Public Overrides Function ToString() As String", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub BindingMemberAccessInsideQuery4()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="BindingMemberAccessInsideQuery4">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(x As String)
        Dim xxx = From i In New Integer() {1, 2 } Select y = x.Length.ToString() 'BIND1:"x.Length"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node As MemberAccessExpressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Public Overloads ReadOnly Property Length As Integer", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub From1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New()
    End Sub

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Class QueryAble2(Of T)
    Public Function AsQueryable As QueryAble(Of T)
        Return New QueryAble(Of T)(1)
    End Function
End Class

Class QueryAble3(Of T)
    Public Function AsEnumerable As QueryAble(Of T)
        Return New QueryAble(Of T)(1)
    End Function
End Class

Class QueryAble4(Of T)
    Inherits QueryAble(Of T)
End Class

Class QueryAble5
    Public Function Cast(Of T)() As QueryAble4(Of T)
        Return New QueryAble4(Of T)()
    End Function
End Class

    Public Function AsEnumerable As QueryAble(Of T)
        Return New QueryAble(Of T)(1)
    End Function
End Class

Module Program
    Function Test(Of T)(x As T) As T
        return x
    End Function

    Sub Main(args As String())
        Dim qi As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q1 As Object = From s In qi 
                           From x In s, 'BIND1:"s"
                                y In Test(x), 'BIND2:"x"
                                x In s, 'BIND3:"x In s"
                                w In x  'BIND4:"x"

        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)

        Dim q2 As Object = From s1 In qb, s2 In qs 
                           Select s1+s2 'BIND5:"s2"

        Dim q3 As Object = From s1 In qb, s2 In qs 
                           Select s1+s2 'BIND6:"s1+s2"

        Dim q4 As Object = From s1 In qb, s2 In qs 
                           Select s3 = s1+s2 'BIND7:"s3"

        Dim q5 As Object = From s1 In qb, s2 In qs 
                           Let s3 = 'BIND8:"s3"
                                    CInt(s1+s2) 'BIND9:"s1"

        Dim q6 As Object = From s In qi 'BIND10:"From s In qi"
                           From x In s 'BIND11:"From x In s"

        Dim q6 As Object = From s In qi 'BIND12:"From s In qi"

        Dim q As Object

        q = From s In qi 'BIND13:"s In qi"

        q = From s In qi, s2 In s 'BIND14:"s2 In s"

        q = From s In qi, s2 In s, s5 As Integer In s2 'BIND15:"s5 As Integer In s2"

        Dim qii As New QueryAble(Of Integer)(0)

        q = From s3 As Integer In qii 'BIND16:"s3 As Integer In qii"

        q = From s4 As Long In qii 'BIND17:"s4 As Long In qii"

        q = From s In qi, s2 In s From s6 As Long In s2 'BIND18:"s6 As Long In s2"

        Dim qj As New QueryAble2(Of Integer)()

        q = From s7 In qj 'BIND19:"s7 In qj"

        Dim qjj As New QueryAble(Of QueryAble2(Of Integer))(0)

        q = From s in qjj, s8 In s 'BIND20:"s8 In s"

        q = From s9 As Integer In New QueryAble3(Of Integer)() 'BIND21:"s9 As Integer In New QueryAble3(Of Integer)()"

        q = From s10 As Long In qj 'BIND22:"s10 As Long In qj"

        q = From s in qjj, s11 As Integer In s 'BIND23:"s11 As Integer In s"

        q = From s in qjj, s12 As Long In s 'BIND24:"s12 As Long In s"

        q = From s In qi, s2 In s, s13 In s2 'BIND25:"s13 In s2"
            Select s13

        q = From s In qi, s2 In s, s14 In s2 'BIND26:"s14 In s2"
            Let s15 = 0

        q = From s16 In New QueryAble5() 'BIND27:"s16 In New QueryAble5()"
            Take 10
    End Sub

    <System.Runtime.CompilerServices.Extension()>
    Public Function Take(Of T)(this As QueryAble(Of T), x as Integer) As QueryAble(Of T)
        Return this
    End Function

End Module

Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("QueryAble(Of QueryAble(Of System.Int32))", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("QueryAble(Of QueryAble(Of System.Int32))", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("QueryAble(Of QueryAble(Of System.Int32))", s1.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node1.Parent), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("QueryAble(Of System.Int32)", x1.Type.ToTestDisplayString())

            Assert.Same(x1, semanticModel.GetDeclaredSymbol(DirectCast(node1.Parent, CollectionRangeVariableSyntax).Identifier))

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax))
            Assert.Same(x1, semanticInfo.Symbol)

            Dim node3 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 3)
            Dim x2 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("x", x2.Name)
            Assert.Equal("QueryAble(Of System.Int32)", x1.Type.ToTestDisplayString())

            Assert.NotSame(x1, x2)

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node4, ExpressionSyntax))
            Assert.Same(x1, semanticInfo.Symbol)

            Dim w1 = DirectCast(semanticModel.GetDeclaredSymbol(node4.Parent), RangeVariableSymbol)
            Assert.Equal("w", w1.Name)
            Assert.Equal("System.Int32", w1.Type.ToTestDisplayString())

            Assert.Same(w1, semanticModel.GetDeclaredSymbol(DirectCast(node4.Parent, CollectionRangeVariableSyntax).Identifier))

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node5, ExpressionSyntax))

            Assert.Equal("System.Int16", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int16", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s2", s2.Name)
            Assert.Equal("System.Int16", s2.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node6 As ExpressionRangeVariableSyntax = CompilationUtils.FindBindingText(Of ExpressionRangeVariableSyntax)(compilation, "a.vb", 6)
            Assert.Null(semanticModel.GetDeclaredSymbol(node6))

            Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            Dim s3 = DirectCast(semanticModel.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.Equal("s3", s3.Name)
            Assert.Equal("System.Int16", s3.Type.ToTestDisplayString())

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            s3 = DirectCast(semanticModel.GetDeclaredSymbol(node8), RangeVariableSymbol)
            Assert.Equal("s3", s3.Name)
            Assert.Equal("System.Int32", s3.Type.ToTestDisplayString())

            Dim node9 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 9)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node9, ExpressionSyntax))

            Assert.Equal("System.Byte", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int16", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            s2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s1", s2.Name)
            Assert.Equal("System.Byte", s2.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node10 As FromClauseSyntax = CompilationUtils.FindBindingText(Of FromClauseSyntax)(compilation, "a.vb", 10)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node10)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node11 As FromClauseSyntax = CompilationUtils.FindBindingText(Of FromClauseSyntax)(compilation, "a.vb", 11)
            symbolInfo = semanticModel.GetSymbolInfo(node11)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node12 As FromClauseSyntax = CompilationUtils.FindBindingText(Of FromClauseSyntax)(compilation, "a.vb", 12)
            symbolInfo = semanticModel.GetSymbolInfo(node12)

            Assert.Equal("Function QueryAble(Of QueryAble(Of QueryAble(Of System.Int32))).Select(Of QueryAble(Of QueryAble(Of System.Int32)))(x As System.Func(Of QueryAble(Of QueryAble(Of System.Int32)), QueryAble(Of QueryAble(Of System.Int32)))) As QueryAble(Of QueryAble(Of QueryAble(Of System.Int32)))", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim collectionInfo As CollectionRangeVariableSymbolInfo
            Dim node13 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 13)

            collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node13)

            Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)
            Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)
            Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

            Assert.Equal("s As QueryAble(Of QueryAble(Of System.Int32))", semanticModel.GetDeclaredSymbol(node13).ToTestDisplayString())

            If True Then
                Dim node14 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 14)

                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node14)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of QueryAble(Of QueryAble(Of System.Int32))).SelectMany(Of QueryAble(Of System.Int32), <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>)(m As System.Func(Of QueryAble(Of QueryAble(Of System.Int32)), QueryAble(Of QueryAble(Of System.Int32))), x As System.Func(Of QueryAble(Of QueryAble(Of System.Int32)), QueryAble(Of System.Int32), <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>)) As QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s2 As QueryAble(Of System.Int32)", semanticModel.GetDeclaredSymbol(node14).ToTestDisplayString())

                Dim node15 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 14)

                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node14)
            End If

            If True Then
                Dim node15 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 15)

                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node15)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>).SelectMany(Of System.Int32, <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s5 As System.Int32>)(m As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, QueryAble(Of System.Int32)), x As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, System.Int32, <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s5 As System.Int32>)) As QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s5 As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s5 As System.Int32", semanticModel.GetDeclaredSymbol(node15).ToTestDisplayString())
            End If

            If True Then
                Dim node16 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 16)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node16)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

                Assert.Equal("s3 As System.Int32", semanticModel.GetDeclaredSymbol(node16).ToTestDisplayString())
            End If

            If True Then
                Dim node17 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 17)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node17)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)

                symbolInfo = collectionInfo.AsClauseConversion
                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of System.Int64)(x As System.Func(Of System.Int32, System.Int64)) As QueryAble(Of System.Int64)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

                Assert.Equal("s4 As System.Int64", semanticModel.GetDeclaredSymbol(node17).ToTestDisplayString())
            End If

            If True Then
                Dim node18 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 18)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node18)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)

                symbolInfo = collectionInfo.AsClauseConversion
                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of System.Int64)(x As System.Func(Of System.Int32, System.Int64)) As QueryAble(Of System.Int64)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>).SelectMany(Of System.Int64, <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s6 As System.Int64>)(m As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, QueryAble(Of System.Int64)), x As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, System.Int64, <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s6 As System.Int64>)) As QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s6 As System.Int64>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s6 As System.Int64", semanticModel.GetDeclaredSymbol(node18).ToTestDisplayString())
            End If

            If True Then
                Dim node19 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 19)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node19)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble2(Of System.Int32).AsQueryable() As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

                Assert.Equal("s7 As System.Int32", semanticModel.GetDeclaredSymbol(node19).ToTestDisplayString())
            End If

            If True Then
                Dim node20 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 20)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node20)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble2(Of System.Int32).AsQueryable() As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of QueryAble2(Of System.Int32)).SelectMany(Of System.Int32, <anonymous type: Key s As QueryAble2(Of System.Int32), Key s8 As System.Int32>)(m As System.Func(Of QueryAble2(Of System.Int32), QueryAble(Of System.Int32)), x As System.Func(Of QueryAble2(Of System.Int32), System.Int32, <anonymous type: Key s As QueryAble2(Of System.Int32), Key s8 As System.Int32>)) As QueryAble(Of <anonymous type: Key s As QueryAble2(Of System.Int32), Key s8 As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s8 As System.Int32", semanticModel.GetDeclaredSymbol(node20).ToTestDisplayString())
            End If

            If True Then
                Dim node21 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 21)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node21)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble3(Of System.Int32).AsEnumerable() As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

                Assert.Equal("s9 As System.Int32", semanticModel.GetDeclaredSymbol(node21).ToTestDisplayString())
            End If

            If True Then
                Dim node22 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 22)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node22)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble2(Of System.Int32).AsQueryable() As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                symbolInfo = collectionInfo.AsClauseConversion
                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of System.Int64)(x As System.Func(Of System.Int32, System.Int64)) As QueryAble(Of System.Int64)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

                Assert.Equal("s10 As System.Int64", semanticModel.GetDeclaredSymbol(node22).ToTestDisplayString())
            End If

            If True Then
                Dim node23 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 23)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node23)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble2(Of System.Int32).AsQueryable() As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of QueryAble2(Of System.Int32)).SelectMany(Of System.Int32, <anonymous type: Key s As QueryAble2(Of System.Int32), Key s11 As System.Int32>)(m As System.Func(Of QueryAble2(Of System.Int32), QueryAble(Of System.Int32)), x As System.Func(Of QueryAble2(Of System.Int32), System.Int32, <anonymous type: Key s As QueryAble2(Of System.Int32), Key s11 As System.Int32>)) As QueryAble(Of <anonymous type: Key s As QueryAble2(Of System.Int32), Key s11 As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s11 As System.Int32", semanticModel.GetDeclaredSymbol(node23).ToTestDisplayString())
            End If

            If True Then
                Dim node24 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 24)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node24)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble2(Of System.Int32).AsQueryable() As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                symbolInfo = collectionInfo.AsClauseConversion
                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of System.Int64)(x As System.Func(Of System.Int32, System.Int64)) As QueryAble(Of System.Int64)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of QueryAble2(Of System.Int32)).SelectMany(Of System.Int64, <anonymous type: Key s As QueryAble2(Of System.Int32), Key s12 As System.Int64>)(m As System.Func(Of QueryAble2(Of System.Int32), QueryAble(Of System.Int64)), x As System.Func(Of QueryAble2(Of System.Int32), System.Int64, <anonymous type: Key s As QueryAble2(Of System.Int32), Key s12 As System.Int64>)) As QueryAble(Of <anonymous type: Key s As QueryAble2(Of System.Int32), Key s12 As System.Int64>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s12 As System.Int64", semanticModel.GetDeclaredSymbol(node24).ToTestDisplayString())
            End If

            If True Then
                Dim node25 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 25)

                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node25)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>).SelectMany(Of System.Int32, System.Int32)(m As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, QueryAble(Of System.Int32)), x As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, System.Int32, System.Int32)) As QueryAble(Of System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s13 As System.Int32", semanticModel.GetDeclaredSymbol(node25).ToTestDisplayString())
            End If

            If True Then
                Dim node26 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 26)

                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node26)

                Assert.Equal(CandidateReason.None, collectionInfo.ToQueryableCollectionConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)

                symbolInfo = collectionInfo.SelectMany
                Assert.Equal("Function QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>).SelectMany(Of System.Int32, <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s14 As System.Int32, Key s15 As System.Int32>)(m As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, QueryAble(Of System.Int32)), x As System.Func(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32)>, System.Int32, <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s14 As System.Int32, Key s15 As System.Int32>)) As QueryAble(Of <anonymous type: Key s As QueryAble(Of QueryAble(Of System.Int32)), Key s2 As QueryAble(Of System.Int32), Key s14 As System.Int32, Key s15 As System.Int32>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal("s14 As System.Int32", semanticModel.GetDeclaredSymbol(node26).ToTestDisplayString())
            End If

            If True Then
                Dim node27 As CollectionRangeVariableSyntax = CompilationUtils.FindBindingText(Of CollectionRangeVariableSyntax)(compilation, "a.vb", 27)
                collectionInfo = semanticModel.GetCollectionRangeVariableSymbolInfo(node27)

                symbolInfo = collectionInfo.ToQueryableCollectionConversion
                Assert.Equal("Function QueryAble5.Cast(Of System.Object)() As QueryAble4(Of System.Object)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Assert.Equal(CandidateReason.None, collectionInfo.AsClauseConversion.CandidateReason)
                Assert.Equal(CandidateReason.None, collectionInfo.SelectMany.CandidateReason)

                Assert.Equal("s16 As System.Object", semanticModel.GetDeclaredSymbol(node27).ToTestDisplayString())
            End If
        End Sub

        <Fact()>
        Public Sub Join1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s In qi 
                           Join x In qb 'BIND1:"x"
                           On s Equals x + 1 'BIND2:"x"
                           Join y In qs 'BIND3:"y"
                                Join x In qu 'BIND4:"x"
                                On y + 1 Equals 'BIND5:"y"
                                   x 'BIND6:"x"
                           On y Equals s
                           Join w In ql On w Equals y And w Equals x 'BIND7:"x"

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node1), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("System.Byte", x1.Type.ToTestDisplayString())

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax))

            Assert.Equal("System.Byte", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Dim x2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("x", x2.Name)
            Assert.Equal("System.Byte", x2.Type.ToTestDisplayString())
            Assert.Same(x1, x2)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node3 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
            Dim y1 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("y", y1.Name)
            Assert.Equal("System.Int16", y1.Type.ToTestDisplayString())

            Dim node4 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
            Dim x3 = DirectCast(semanticModel.GetDeclaredSymbol(node4), RangeVariableSymbol)
            Assert.Equal("x", x3.Name)
            Assert.Equal("System.UInt32", x3.Type.ToTestDisplayString())
            Assert.NotSame(x1, x3)

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node5, ExpressionSyntax))
            Assert.Same(y1, semanticInfo.Symbol)

            Dim node6 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 6)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node6, ExpressionSyntax))
            Assert.Null(semanticInfo.Symbol)

            Dim node7 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 7)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node7, ExpressionSyntax))
            Assert.Same(x1, semanticInfo.Symbol)

        End Sub

        <Fact()>
        Public Sub GroupBy1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

    Public Function Count() As Integer
        Return 0
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s In qi 
                           Join x In qb 
                           On s Equals x 
                           Join y In qs 
                                Join x In qu 
                                On y Equals 
                                   x 
                           On y Equals s
                           Join w In ql On w Equals y And w Equals x 
                           Group i1 = s+1, 'BIND1:"s"
                                 x, 'BIND2:"x"
                                 x  'BIND3:"x"
                           By k1 = y+1S, 'BIND4:"y"
                              w, 'BIND5:"w"
                              w  'BIND6:"w" 
                           Into Group, 'BIND7:"Group"
                                k1= Count(), 'BIND8:"k1"
                                k1= Count(), 'BIND9:"k1"
                                r2 = Count(i1+ 'BIND10:"i1"
                                               x) 'BIND11:"x"
                           Select w, 'BIND12:"w"
                                  k1 'BIND13:"k1"      

        Dim q2 As Object = From s In qi Group By s Into Group  'BIND14:"Group By s Into Group"      
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node1, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s.Name)
            Assert.Equal("System.Int32", s.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim i1 = DirectCast(semanticModel.GetDeclaredSymbol(node1.Parent.Parent), RangeVariableSymbol)
            Assert.Equal("i1", i1.Name)
            Assert.Equal("System.Int32", i1.Type.ToTestDisplayString())

            Assert.Same(i1, semanticModel.GetDeclaredSymbol(DirectCast(node1.Parent.Parent, ExpressionRangeVariableSyntax).NameEquals.Identifier))
            Assert.Same(i1, semanticModel.GetDeclaredSymbol(DirectCast(DirectCast(node1.Parent.Parent, ExpressionRangeVariableSyntax).NameEquals.Identifier, VisualBasicSyntaxNode)))

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax))
            Dim x1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("System.Byte", x1.Type.ToTestDisplayString())

            Dim x2 = DirectCast(semanticModel.GetDeclaredSymbol(node2.Parent), RangeVariableSymbol)
            Assert.Equal("x", x2.Name)
            Assert.Equal("System.Byte", x2.Type.ToTestDisplayString())
            Assert.NotSame(x1, x2)

            Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(node2.Parent, ExpressionRangeVariableSyntax))

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node3 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 3)
            Dim x3 = semanticModel.GetDeclaredSymbol(node3.Parent)
            Assert.NotSame(x1, x3)
            Assert.NotSame(x2, x3)

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int16", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int16", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim y = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("y", y.Name)
            Assert.Equal("System.Int16", y.Type.ToTestDisplayString())

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim k1 = DirectCast(semanticModel.GetDeclaredSymbol(node4.Parent.Parent), RangeVariableSymbol)
            Assert.Equal("k1", k1.Name)
            Assert.Equal("System.Int16", k1.Type.ToTestDisplayString())

            Assert.Same(k1, semanticModel.GetDeclaredSymbol(DirectCast(node4.Parent.Parent, ExpressionRangeVariableSyntax).NameEquals.Identifier))
            Assert.Same(k1, semanticModel.GetDeclaredSymbol(DirectCast(DirectCast(node4.Parent.Parent, ExpressionRangeVariableSyntax).NameEquals.Identifier, VisualBasicSyntaxNode)))

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node5, ExpressionSyntax))
            Dim w1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("w", w1.Name)
            Assert.Equal("System.Int64", w1.Type.ToTestDisplayString())

            Dim w2 = DirectCast(semanticModel.GetDeclaredSymbol(node5.Parent), RangeVariableSymbol)
            Assert.Equal("w", w2.Name)
            Assert.Equal("System.Int64", w2.Type.ToTestDisplayString())
            Assert.NotSame(w1, w2)

            symbolInfo = semanticModel.GetSymbolInfo(DirectCast(node5.Parent, ExpressionRangeVariableSyntax))

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node6 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 6)
            Dim w3 = semanticModel.GetDeclaredSymbol(node6.Parent)
            Assert.NotSame(w1, w3)
            Assert.NotSame(w2, w3)

            Dim node7 As AggregationRangeVariableSyntax = CompilationUtils.FindBindingText(Of AggregationRangeVariableSyntax)(compilation, "a.vb", 7)

            Dim gr = DirectCast(semanticModel.GetDeclaredSymbol(node7), RangeVariableSymbol)
            Assert.Equal("Group", gr.Name)
            Assert.Equal("QueryAble(Of <anonymous type: Key i1 As System.Int32, Key x As System.Byte, Key $2080 As System.Byte>)", gr.Type.ToTestDisplayString())

            Assert.Same(gr, semanticModel.GetDeclaredSymbol(DirectCast(node7, VisualBasicSyntaxNode)))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, node7.Aggregation)

            Assert.Same(gr.Type, semanticInfo.Type)
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Same(gr.Type, semanticInfo.ConvertedType)
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Null(semanticInfo.Alias)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)

            Dim k2 = DirectCast(semanticModel.GetDeclaredSymbol(node8.Parent.Parent), RangeVariableSymbol)
            Assert.Equal("k1", k2.Name)
            Assert.Equal("System.Int32", k2.Type.ToTestDisplayString())
            Assert.Same(k2, semanticModel.GetDeclaredSymbol(DirectCast(node8.Parent.Parent, AggregationRangeVariableSyntax)))
            Assert.Same(k2, semanticModel.GetDeclaredSymbol(DirectCast(node8, VisualBasicSyntaxNode)))
            Assert.Same(k2, semanticModel.GetDeclaredSymbol(node8))
            Assert.NotSame(k1, k2)

            Dim node9 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 9)
            Dim k3 = semanticModel.GetDeclaredSymbol(node9)
            Assert.NotNull(k3)
            Assert.NotSame(k1, k3)
            Assert.NotSame(k2, k3)

            Dim node10 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 10)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node10, ExpressionSyntax))
            Assert.Same(i1, semanticInfo.Symbol)

            Dim node11 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 11)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node11, ExpressionSyntax))
            Assert.Same(x2, semanticInfo.Symbol)

            Dim node12 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 12)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node12, ExpressionSyntax))
            Assert.Same(w2, semanticInfo.Symbol)

            Dim node13 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 13)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node13, ExpressionSyntax))
            Assert.Same(k1, semanticInfo.Symbol)

            Dim node14 As GroupByClauseSyntax = CompilationUtils.FindBindingText(Of GroupByClauseSyntax)(compilation, "a.vb", 14)
            symbolInfo = semanticModel.GetSymbolInfo(node14)

            Assert.Equal("Function QueryAble(Of System.Int32).GroupBy(Of System.Int32, <anonymous type: Key s As System.Int32, Key Group As QueryAble(Of System.Int32)>)(key As System.Func(Of System.Int32, System.Int32), into As System.Func(Of System.Int32, QueryAble(Of System.Int32), <anonymous type: Key s As System.Int32, Key Group As QueryAble(Of System.Int32)>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key Group As QueryAble(Of System.Int32)>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <Fact()>
        <CompilerTrait(CompilerFeature.IOperation)>
        Public Sub GroupJoin1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q1 As Object = From s In qi 
                           Group Join x In qb 'BIND1:"x"
                           On s Equals x + 1  'BIND2:"x"
                           Into x = Group 'BIND8:"x" 
                           Group Join y In qs 'BIND3:"y"
                                Group Join x In qu 'BIND4:"x"
                                On y + 1 Equals 'BIND5:"y"
                                   x 'BIND6:"x"
                                Into Group
                           On y Equals s
                           Into y = Group
                           Group Join w In ql On w Equals y And w Equals x 'BIND7:"x"
                           Into Group

        Dim q2 As Object = From s In qi 
                           Group Join x In qb On s Equals x Into Group 'BIND9:"Group Join x In qb On s Equals x Into Group" 
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node1), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("System.Byte", x1.Type.ToTestDisplayString())

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node2, ExpressionSyntax))

            Assert.Equal("System.Byte", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Dim x2 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("x", x2.Name)
            Assert.Equal("System.Byte", x2.Type.ToTestDisplayString())
            Assert.Same(x1, x2)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
            Dim x4 = DirectCast(semanticModel.GetDeclaredSymbol(node8), RangeVariableSymbol)
            Assert.Equal("x", x4.Name)
            Assert.Equal("QueryAble(Of System.Byte)", x4.Type.ToTestDisplayString())

            Assert.Same(x4, semanticModel.GetDeclaredSymbol(DirectCast(node8, VisualBasicSyntaxNode)))
            Assert.Same(x4, semanticModel.GetDeclaredSymbol(DirectCast(node8.Parent.Parent, AggregationRangeVariableSyntax)))
            Assert.Same(x4, semanticModel.GetDeclaredSymbol(node8.Parent.Parent))
            Assert.NotSame(x1, x4)
            Assert.NotSame(x2, x4)

            Dim node3 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
            Dim y1 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("y", y1.Name)
            Assert.Equal("System.Int16", y1.Type.ToTestDisplayString())

            Dim node4 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
            Dim x3 = DirectCast(semanticModel.GetDeclaredSymbol(node4), RangeVariableSymbol)
            Assert.Equal("x", x3.Name)
            Assert.Equal("System.UInt32", x3.Type.ToTestDisplayString())
            Assert.NotSame(x1, x3)
            Assert.NotSame(x2, x3)
            Assert.NotSame(x4, x3)

            Dim node5 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node5, ExpressionSyntax))
            Assert.Same(y1, semanticInfo.Symbol)

            Dim node6 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 6)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node6, ExpressionSyntax))
            Assert.Same(x3, semanticInfo.Symbol)

            Dim node7 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 7)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node7, ExpressionSyntax))
            Assert.Same(x4, semanticInfo.Symbol)

            Dim node9 As GroupJoinClauseSyntax = CompilationUtils.FindBindingText(Of GroupJoinClauseSyntax)(compilation, "a.vb", 9)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node9)

            Assert.Equal("Function QueryAble(Of System.Int32).GroupJoin(Of System.Byte, System.Int32, <anonymous type: Key s As System.Int32, Key Group As QueryAble(Of System.Byte)>)(inner As QueryAble(Of System.Byte), outerKey As System.Func(Of System.Int32, System.Int32), innerKey As System.Func(Of System.Byte, System.Int32), x As System.Func(Of System.Int32, QueryAble(Of System.Byte), <anonymous type: Key s As System.Int32, Key Group As QueryAble(Of System.Byte)>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key Group As QueryAble(Of System.Byte)>)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim node = tree.GetRoot().DescendantNodes().OfType(Of QueryExpressionSyntax)().First()

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s In q ... Into Group')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
      Children(5):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
            Children(1):
                IInvocationOperation ( Function QueryAble(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>).GroupJoin(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, System.Int32, <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>)(inner As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), outerKey As System.Func(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, System.Int32), innerKey As System.Func(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, System.Int32), x As System.Func(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>)) As QueryAble(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>)) (OperationKind.Invocation, Type: QueryAble(Of <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>), IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                  Instance Receiver: 
                    IInvocationOperation ( Function QueryAble(Of System.Int32).GroupJoin(Of System.Byte, System.Int32, <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>)(inner As QueryAble(Of System.Byte), outerKey As System.Func(Of System.Int32, System.Int32), innerKey As System.Func(Of System.Byte, System.Int32), x As System.Func(Of System.Int32, QueryAble(Of System.Byte), <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>)) As QueryAble(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>)) (OperationKind.Invocation, Type: QueryAble(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>), IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                      Instance Receiver: 
                        ILocalReferenceOperation: qi (OperationKind.LocalReference, Type: QueryAble(Of System.Int32)) (Syntax: 'qi')
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'qb')
                            ILocalReferenceOperation: qb (OperationKind.LocalReference, Type: QueryAble(Of System.Byte)) (Syntax: 'qb')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKey) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                      ReturnedValue: 
                                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKey) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x + 1')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Byte, System.Int32), IsImplicit) (Syntax: 'x + 1')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function (x As System.Byte) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x + 1')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x + 1')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x + 1')
                                      ReturnedValue: 
                                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + 1')
                                          Left: 
                                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              Operand: 
                                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'x')
                                          Right: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, QueryAble(Of System.Byte), <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>), IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32, $VB$ItAnonymous As QueryAble(Of System.Byte)) As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                      ReturnedValue: 
                                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                          Initializers(2):
                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's In qi')
                                                Left: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>.s As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's')
                                                    Instance Receiver: 
                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                                Right: 
                                                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's')
                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: QueryAble(Of System.Byte), IsInvalid, IsImplicit) (Syntax: 'From s In q ... Into Group')
                                                Left: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>.x As QueryAble(Of System.Byte) (OperationKind.PropertyReference, Type: QueryAble(Of System.Byte), IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                                    Instance Receiver: 
                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                                                Right: 
                                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: QueryAble(Of System.Byte), IsImplicit) (Syntax: 'Group Join  ... o x = Group')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(4):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                        IInvocationOperation ( Function QueryAble(Of System.Int16).GroupJoin(Of System.UInt32, System.Int64, <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)(inner As QueryAble(Of System.UInt32), outerKey As System.Func(Of System.Int16, System.Int64), innerKey As System.Func(Of System.UInt32, System.Int64), x As System.Func(Of System.Int16, QueryAble(Of System.UInt32), <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)) As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)) (OperationKind.Invocation, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'Group Join  ... Into Group')
                          Instance Receiver: 
                            ILocalReferenceOperation: qs (OperationKind.LocalReference, Type: QueryAble(Of System.Int16)) (Syntax: 'qs')
                          Arguments(4):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'qu')
                                ILocalReferenceOperation: qu (OperationKind.LocalReference, Type: QueryAble(Of System.UInt32)) (Syntax: 'qu')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKey) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y + 1')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int16, System.Int64), IsImplicit) (Syntax: 'y + 1')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (y As System.Int16) As System.Int64) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'y + 1')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'y + 1')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'y + 1')
                                          ReturnedValue: 
                                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'y + 1')
                                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              Operand: 
                                                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'y + 1')
                                                  Left: 
                                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                      Operand: 
                                                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int16) (Syntax: 'y')
                                                  Right: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKey) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.UInt32, System.Int64), IsImplicit) (Syntax: 'x')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (x As System.UInt32) As System.Int64) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                          ReturnedValue: 
                                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'x')
                                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              Operand: 
                                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.UInt32) (Syntax: 'x')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int16, QueryAble(Of System.UInt32), <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (y As System.Int16, $VB$ItAnonymous As QueryAble(Of System.UInt32)) As <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                          ReturnedValue: 
                                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                              Initializers(2):
                                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int16, IsImplicit) (Syntax: 'y In qs')
                                                    Left: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>.y As System.Int16 (OperationKind.PropertyReference, Type: System.Int16, IsImplicit) (Syntax: 'y')
                                                        Instance Receiver: 
                                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                                    Right: 
                                                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int16, IsImplicit) (Syntax: 'y')
                                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: QueryAble(Of System.UInt32), IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                                    Left: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>.Group As QueryAble(Of System.UInt32) (OperationKind.PropertyReference, Type: QueryAble(Of System.UInt32), IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                                        Instance Receiver: 
                                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                                    Right: 
                                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: QueryAble(Of System.UInt32), IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKey) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, System.Int32), IsImplicit) (Syntax: 'y')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'y')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'y')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'y')
                                  ReturnedValue: 
                                    IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>.s As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's')
                                      Instance Receiver: 
                                        IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsImplicit) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKey) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, System.Int32), IsImplicit) (Syntax: 's')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                  ReturnedValue: 
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Operand: 
                                        IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>.y As System.Int16 (OperationKind.PropertyReference, Type: System.Int16) (Syntax: 'y')
                                          Instance Receiver: 
                                            IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>, IsImplicit) (Syntax: 's')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>), IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, $VB$ItAnonymous As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)) As <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                  ReturnedValue: 
                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                      Initializers(2):
                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsInvalid, IsImplicit) (Syntax: 'From s In q ... Into Group')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.$VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)> (OperationKind.PropertyReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                                Instance Receiver: 
                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                            Right: 
                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsInvalid, IsImplicit) (Syntax: 'From s In q ... Into Group')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>) (OperationKind.PropertyReference, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                                Instance Receiver: 
                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                                            Right: 
                                              IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'Group Join  ... o y = Group')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ILocalReferenceOperation: ql (OperationKind.LocalReference, Type: QueryAble(Of System.Int64)) (Syntax: 'ql')
          IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>) As ?) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'w')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'w')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'w')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                    Children(2):
                        IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>) (OperationKind.PropertyReference, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsInvalid) (Syntax: 'y')
                          Instance Receiver: 
                            IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsInvalid, IsImplicit) (Syntax: 'w')
                        IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>.x As QueryAble(Of System.Byte) (OperationKind.PropertyReference, Type: QueryAble(Of System.Byte), IsInvalid) (Syntax: 'x')
                          Instance Receiver: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.$VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)> (OperationKind.PropertyReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsInvalid, IsImplicit) (Syntax: 'w')
                              Instance Receiver: 
                                IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsInvalid, IsImplicit) (Syntax: 'w')
          IAnonymousFunctionOperation (Symbol: Function (w As System.Int64) As ?) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'y')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'y')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'y')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                    Children(2):
                        IParameterReferenceOperation: w (OperationKind.ParameterReference, Type: System.Int64, IsInvalid) (Syntax: 'w')
                        IParameterReferenceOperation: w (OperationKind.ParameterReference, Type: System.Int64, IsInvalid) (Syntax: 'w')
          IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, $VB$ItAnonymous As ?) As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                ReturnedValue: 
                  IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                    Initializers(4):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's In qi')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>.s As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                          Right: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>.s As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's')
                              Instance Receiver: 
                                IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.$VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)> (OperationKind.PropertyReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                  Instance Receiver: 
                                    IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: QueryAble(Of System.Byte), IsImplicit) (Syntax: 'x =')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>.x As QueryAble(Of System.Byte) (OperationKind.PropertyReference, Type: QueryAble(Of System.Byte), IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                          Right: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>.x As QueryAble(Of System.Byte) (OperationKind.PropertyReference, Type: QueryAble(Of System.Byte), IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.$VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)> (OperationKind.PropertyReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                                  Instance Receiver: 
                                    IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'y =')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>.y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>) (OperationKind.PropertyReference, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'y')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                          Right: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>.y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>) (OperationKind.PropertyReference, Type: QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), IsImplicit) (Syntax: 'y')
                              Instance Receiver: 
                                IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte)>, Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>)>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'From s In q ... Into Group')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>.Group As ? (OperationKind.PropertyReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s As System.Int32, Key x As QueryAble(Of System.Byte), Key y As QueryAble(Of <anonymous type: Key y As System.Int16, Key Group As QueryAble(Of System.UInt32)>), Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
                          Right: 
                            IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ... Into Group')
]]>.Value)
        End Sub

        <Fact()>
        Public Sub Aggregate1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count() As Integer
    End Function
End Class

Module Program
    Function Test(Of T)(x as T) As T
        return x
    End Function 

    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)


        Dim q1 As Object = Aggregate s In qi, 'BIND1:"s"
                                     s In  'BIND2:"s" 
                                          Test(qb) 'BIND6:"qb" 
                           Into x =           'BIND3:"x" 
                                    Where(s > 0), 'BIND4:"s" 
                                x = Distinct      'BIND5:"x" 

        Dim q2 As Object = Aggregate s1 In New QueryAble(Of QueryAble(Of Integer))(0), 
                                     s2 In Test(s1) 'BIND7:"s1" 
                           Into x = Distinct 

        q2 = DirectCast(qi.Select(Function(i) i), Object) 'BIND8:"qi.Select(Function(i) i)"

        q2 = Aggregate i In qi Into [Select](i) 'BIND9:"[Select](i)"

        Dim qii As New QueryAble(Of QueryAble(Of Integer))(0)

        q2 = Aggregate ii In qii 
             Into [Select](From i In ii) 'BIND10:"ii"

        q2 = Aggregate i In qi Into Count(i) 'BIND11:"Count(i)"

        q2 = Aggregate i In qi Into Count() 'BIND12:"Aggregate i In qi Into Count()"

        q2 = Aggregate i In qi Into Count(), [Select](i) 'BIND13:"Aggregate i In qi Into Count(), [Select](i)"
     End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            Dim s1 = DirectCast(semanticModel.GetDeclaredSymbol(node1), RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Same(s1, semanticModel.GetDeclaredSymbol(DirectCast(node1, VisualBasicSyntaxNode)))
            Assert.Same(s1, semanticModel.GetDeclaredSymbol(node1.Parent))
            Assert.Same(s1, semanticModel.GetDeclaredSymbol(DirectCast(node1.Parent, CollectionRangeVariableSyntax)))

            Dim node2 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
            Dim s2 = DirectCast(semanticModel.GetDeclaredSymbol(node2), RangeVariableSymbol)
            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Byte", s2.Type.ToTestDisplayString())
            Assert.NotSame(s1, s2)

            Dim node3 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
            Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("?", x1.Type.ToTestDisplayString())

            Assert.Same(x1, semanticModel.GetDeclaredSymbol(DirectCast(node3, VisualBasicSyntaxNode)))
            Assert.Same(x1, semanticModel.GetDeclaredSymbol(DirectCast(node3.Parent.Parent, AggregationRangeVariableSyntax)))
            Assert.Same(x1, semanticModel.GetDeclaredSymbol(node3.Parent.Parent))

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s3 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s3.Name)
            Assert.Equal("System.Int32", s3.Type.ToTestDisplayString())
            Assert.Same(s1, s3)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node5 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 5)
            Dim x2 = DirectCast(semanticModel.GetDeclaredSymbol(node5), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("?", x1.Type.ToTestDisplayString())
            Assert.NotSame(x1, x2)

            Dim node6 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 6)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node6, ExpressionSyntax))
            Assert.Equal("qb As QueryAble(Of System.Byte)", semanticInfo.Symbol.ToTestDisplayString())

            Dim node7 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 7)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node7, ExpressionSyntax))
            Assert.Equal("s1 As QueryAble(Of System.Int32)", semanticInfo.Symbol.ToTestDisplayString())

            For i As Integer = 8 To 9
                Dim node8 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i)
                semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node8, ExpressionSyntax))

                Assert.Equal("QueryAble(Of System.Int32)", semanticInfo.Type.ToTestDisplayString())
                Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
                Assert.Equal("QueryAble(Of System.Int32)", semanticInfo.ConvertedType.ToTestDisplayString())
                Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of System.Int32)(x As System.Func(Of System.Int32, System.Int32)) As QueryAble(Of System.Int32)",
                             semanticInfo.Symbol.ToTestDisplayString())
                Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
                Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

                Assert.Null(semanticInfo.Alias)

                Assert.Equal(0, semanticInfo.MemberGroup.Length)

                Assert.False(semanticInfo.ConstantValue.HasValue)
            Next

            Dim node9 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 9)
            Dim symbolInfo1 = semanticModel.GetSymbolInfo(node9)
            Assert.Equal("Function QueryAble(Of System.Int32).Select(Of System.Int32)(x As System.Func(Of System.Int32, System.Int32)) As QueryAble(Of System.Int32)", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            Dim symbolInfo2 = semanticModel.GetSymbolInfo(DirectCast(node9, FunctionAggregationSyntax))
            Assert.Equal(symbolInfo1.CandidateReason, symbolInfo2.CandidateReason)
            Assert.Same(symbolInfo1.Symbol, symbolInfo2.Symbol)
            Assert.Equal(0, symbolInfo2.CandidateSymbols.Length)

            Dim commonSymbolInfo = semanticModel.GetSymbolInfo(DirectCast(node9, SyntaxNode))
            Assert.Equal(symbolInfo1.CandidateReason, commonSymbolInfo.CandidateReason)
            Assert.Same(symbolInfo1.Symbol, commonSymbolInfo.Symbol)
            Assert.Equal(0, commonSymbolInfo.CandidateSymbols.Length)

            Dim node10 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 10)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node10, ExpressionSyntax))
            Assert.Equal("ii As QueryAble(Of System.Int32)", semanticInfo.Symbol.ToTestDisplayString())

            Dim node11 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 11)
            symbolInfo1 = semanticModel.GetSymbolInfo(node11)
            Assert.Null(symbolInfo1.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo1.CandidateReason)
            Assert.Equal(1, symbolInfo1.CandidateSymbols.Length)
            Assert.Equal("Function QueryAble(Of System.Int32).Count() As System.Int32", symbolInfo1.CandidateSymbols(0).ToTestDisplayString())

            symbolInfo2 = semanticModel.GetSymbolInfo(DirectCast(node11, FunctionAggregationSyntax))
            Assert.Equal(symbolInfo1.CandidateReason, symbolInfo2.CandidateReason)
            Assert.Same(symbolInfo1.Symbol, symbolInfo2.Symbol)
            Assert.True(symbolInfo1.CandidateSymbols.SequenceEqual(symbolInfo2.CandidateSymbols))

            Dim node12 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 12)
            symbolInfo1 = semanticModel.GetSymbolInfo(node12)

            Assert.Null(symbolInfo1.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            Dim symbolInfo3 As AggregateClauseSymbolInfo = semanticModel.GetAggregateClauseSymbolInfo(node12)

            Assert.Null(symbolInfo3.Select1.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo3.Select1.CandidateReason)
            Assert.Null(symbolInfo3.Select2.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo3.Select2.CandidateReason)

            Dim node13 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 13)
            symbolInfo3 = semanticModel.GetAggregateClauseSymbolInfo(node13)

            Assert.Null(symbolInfo3.Select1.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo3.Select1.CandidateReason)
            Assert.Null(symbolInfo3.Select2.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo3.Select2.CandidateReason)
        End Sub

        <Fact()>
        Public Sub Aggregate2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Count() As Integer
    End Function
End Class

Module Program
    Function Test(Of T)(x as T) As T
        return x
    End Function 

    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)


        Dim q1 As Object = From y In qs 
                           Aggregate s In qi, 'BIND1:"s"
                                     s In qb  'BIND2:"s" 
                           Into x =           'BIND3:"x" 
                                    Where(s > 'BIND4:"s" 
                                              y), 'BIND6:"y" 
                                x = Distinct      'BIND5:"x"
                           Select x 'BIND7:"x" 

        Dim q2 As Object = From s1 In New QueryAble(Of QueryAble(Of Integer))(0) 
                           Aggregate s2 In Test(s1) 'BIND8:"s1" 
                           Into x = Distinct 

        Dim q3 As Object = From s1 In New QueryAble(Of QueryAble(Of Integer))(0) 
                           Aggregate s2 In s1 Into Count() 'BIND9:"Aggregate s2 In s1 Into Count()" 

        Dim q4 As Object = From s1 In New QueryAble(Of QueryAble(Of Integer))(0) Where True
                           Aggregate s2 In s1 Into Count() 'BIND10:"Aggregate s2 In s1 Into Count()" 

        Dim q5 As Object = From s1 In New QueryAble(Of QueryAble(Of Integer))(0) 
                           Aggregate s2 In s1 Into Count(), [Select](s2) 'BIND11:"Aggregate s2 In s1 Into Count(), [Select](s2)" 

        Dim q6 As Object = From s1 In New QueryAble(Of QueryAble(Of Integer))(0) Where True
                           Aggregate s2 In s1 Into Count(), [Select](s2) 'BIND12:"Aggregate s2 In s1 Into Count(), [Select](s2)" 
     End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            Dim s1 = DirectCast(semanticModel.GetDeclaredSymbol(node1), RangeVariableSymbol)
            Assert.Equal("s", s1.Name)
            Assert.Equal("System.Int32", s1.Type.ToTestDisplayString())

            Assert.Same(s1, semanticModel.GetDeclaredSymbol(DirectCast(node1, VisualBasicSyntaxNode)))
            Assert.Same(s1, semanticModel.GetDeclaredSymbol(node1.Parent))
            Assert.Same(s1, semanticModel.GetDeclaredSymbol(DirectCast(node1.Parent, CollectionRangeVariableSyntax)))

            Dim node2 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
            Dim s2 = DirectCast(semanticModel.GetDeclaredSymbol(node2), RangeVariableSymbol)
            Assert.Equal("s", s2.Name)
            Assert.Equal("System.Byte", s2.Type.ToTestDisplayString())
            Assert.NotSame(s1, s2)

            Dim node3 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
            Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node3), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("?", x1.Type.ToTestDisplayString())

            Assert.Same(x1, semanticModel.GetDeclaredSymbol(DirectCast(node3, VisualBasicSyntaxNode)))
            Assert.Same(x1, semanticModel.GetDeclaredSymbol(DirectCast(node3.Parent.Parent, AggregationRangeVariableSyntax)))
            Assert.Same(x1, semanticModel.GetDeclaredSymbol(node3.Parent.Parent))

            Dim node4 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 4)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node4, ExpressionSyntax))

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim s3 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("s", s3.Name)
            Assert.Equal("System.Int32", s3.Type.ToTestDisplayString())
            Assert.Same(s1, s3)

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim node5 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 5)
            Dim x2 = DirectCast(semanticModel.GetDeclaredSymbol(node5), RangeVariableSymbol)
            Assert.Equal("x", x1.Name)
            Assert.Equal("?", x1.Type.ToTestDisplayString())
            Assert.NotSame(x1, x2)

            Dim node6 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 6)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node6, ExpressionSyntax))
            Dim y1 = DirectCast(semanticInfo.Symbol, RangeVariableSymbol)
            Assert.Equal("y", y1.Name)
            Assert.Equal("System.Int16", y1.Type.ToTestDisplayString())

            Dim node7 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 7)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node7, ExpressionSyntax))
            Assert.Same(x1, semanticInfo.Symbol)

            Dim node8 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 8)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, TryCast(node8, ExpressionSyntax))
            Assert.Equal("s1 As QueryAble(Of System.Int32)", semanticInfo.Symbol.ToTestDisplayString())

            Dim node9 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 9)
            Dim symbolInfo1 = semanticModel.GetSymbolInfo(node9)

            Assert.Null(symbolInfo1.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            Dim symbolInfo3 As AggregateClauseSymbolInfo

            symbolInfo3 = semanticModel.GetAggregateClauseSymbolInfo(node9)

            symbolInfo1 = symbolInfo3.Select1
            Assert.Equal("Function QueryAble(Of QueryAble(Of System.Int32)).Select(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key Count As System.Int32>)(x As System.Func(Of QueryAble(Of System.Int32), <anonymous type: Key s1 As QueryAble(Of System.Int32), Key Count As System.Int32>)) As QueryAble(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key Count As System.Int32>)", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            Assert.Null(symbolInfo3.Select2.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo3.Select2.CandidateReason)

            Dim node10 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 10)
            symbolInfo3 = semanticModel.GetAggregateClauseSymbolInfo(node10)

            symbolInfo1 = symbolInfo3.Select1
            Assert.Null(symbolInfo1.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            Assert.Null(symbolInfo3.Select2.Symbol)
            Assert.Equal(CandidateReason.None, symbolInfo3.Select2.CandidateReason)

            Dim node11 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 11)
            symbolInfo3 = semanticModel.GetAggregateClauseSymbolInfo(node11)

            symbolInfo1 = symbolInfo3.Select1
            Assert.Equal("Function QueryAble(Of QueryAble(Of System.Int32)).Select(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key $VB$Group As QueryAble(Of System.Int32)>)(x As System.Func(Of QueryAble(Of System.Int32), <anonymous type: Key s1 As QueryAble(Of System.Int32), Key $VB$Group As QueryAble(Of System.Int32)>)) As QueryAble(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key $VB$Group As QueryAble(Of System.Int32)>)", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            symbolInfo1 = symbolInfo3.Select2
            Assert.Equal("Function QueryAble(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key $VB$Group As QueryAble(Of System.Int32)>).Select(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key Count As System.Int32, Key Select As QueryAble(Of System.Int32)>)(x As System.Func(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key $VB$Group As QueryAble(Of System.Int32)>, <anonymous type: Key s1 As QueryAble(Of System.Int32), Key Count As System.Int32, Key Select As QueryAble(Of System.Int32)>)) As QueryAble(Of <anonymous type: Key s1 As QueryAble(Of System.Int32), Key Count As System.Int32, Key Select As QueryAble(Of System.Int32)>)", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            Dim node12 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 12)
            symbolInfo3 = semanticModel.GetAggregateClauseSymbolInfo(node12)

            symbolInfo1 = symbolInfo3.Select1
            Assert.Null(symbolInfo1.Symbol)
            Assert.Equal(True, symbolInfo1.IsEmpty)
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)

            symbolInfo1 = symbolInfo3.Select2
            Assert.Null(symbolInfo1.Symbol)
            Assert.Equal(True, symbolInfo1.IsEmpty)
            Assert.Equal(CandidateReason.None, symbolInfo1.CandidateReason)
            Assert.Equal(0, symbolInfo1.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub Aggregate3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    'Inherits Base

    'Public Shadows [Select] As Byte
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("SkipWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Skip(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Take(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Take {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1
    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)

        Dim q0 As Object

        q0 = From i In qi, b In qb
             Aggregate s In qs Into Where(True) 'BIND1:"Aggregate s In qs Into Where(True)"

        System.Console.WriteLine("------")
        q0 = From i In qi, b In qb
             Aggregate s In qs Into Where(True), Distinct() 'BIND2:"Aggregate s In qs Into Where(True), Distinct()"

        q0 = From i In qi Join b In qb On b Equals i
             Aggregate s In qs Into Where(True) 'BIND3:"Aggregate s In qs Into Where(True)"

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb On b Equals i
             Aggregate s In qs Into Where(True), Distinct() 'BIND4:"Aggregate s In qs Into Where(True), Distinct()"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticInfo As SemanticInfoSummary = Nothing

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo As SymbolInfo

            Dim aggregateInfo As AggregateClauseSymbolInfo

            For i As Integer = 0 To 1
                Dim node1 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 1 + 2 * i)
                aggregateInfo = semanticModel.GetAggregateClauseSymbolInfo(node1)

                symbolInfo = aggregateInfo.Select1
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                symbolInfo = aggregateInfo.Select2
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Dim node2 As AggregateClauseSyntax = CompilationUtils.FindBindingText(Of AggregateClauseSyntax)(compilation, "a.vb", 2 + 2 * i)
                aggregateInfo = semanticModel.GetAggregateClauseSymbolInfo(node2)

                symbolInfo = aggregateInfo.Select1
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                symbolInfo = aggregateInfo.Select2
                Assert.Equal("Function QueryAble(Of <anonymous type: Key i As System.Int32, Key b As System.Byte, Key $VB$Group As QueryAble(Of System.Int16)>).Select(Of <anonymous type: Key i As System.Int32, Key b As System.Byte, Key Where As QueryAble(Of System.Int16), Key Distinct As QueryAble(Of System.Int16)>)(x As System.Func(Of <anonymous type: Key i As System.Int32, Key b As System.Byte, Key $VB$Group As QueryAble(Of System.Int16)>, <anonymous type: Key i As System.Int32, Key b As System.Byte, Key Where As QueryAble(Of System.Int16), Key Distinct As QueryAble(Of System.Int16)>)) As QueryAble(Of <anonymous type: Key i As System.Int32, Key b As System.Byte, Key Where As QueryAble(Of System.Int16), Key Distinct As QueryAble(Of System.Int16)>)", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Next
        End Sub

        <WorkItem(546132, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546132")>
        <Fact()>
        Public Sub SymbolInfoForFunctionAgtAregationSyntax()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports Microsoft.VisualBasic

Module m1
    <System.Runtime.CompilerServices.Extension()>
    Sub aggr4(Of T)(ByVal this As T)
 
    End Sub
End Module
 
Class cls1
    Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
 
    Function GroupBy(Of K, R)(ByVal key As Func(Of Integer, K), ByVal result As Func(Of K, cls1, R)) As cls1
        Return Nothing
    End Function
 
    Sub aggr4(Of T)(ByVal this As T)
    End Sub
End Class
 
Module AggrArgsInvalidmod
    Sub AggrArgsInvalid()
        Dim colm As New cls1
        Dim q4 = From i In colm Group By vbCrLf Into aggr4(4)
    End Sub
End Module
    ]]></file>
</compilation>, references:={TestMetadata.Net40.SystemCore})

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_QueryOperatorNotFound, "aggr4").WithArguments("aggr4"))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel As SemanticModel = compilation.GetSemanticModel(tree)
            Dim node = DirectCast(tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToString().IndexOf("aggr4(4)", StringComparison.Ordinal)).Parent, FunctionAggregationSyntax)
            Dim info = semanticModel.GetSymbolInfo(node)

            Assert.NotNull(info)
            Assert.Null(info.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason)
            Assert.Equal(2, info.CandidateSymbols.Length)
        End Sub

        <WorkItem(542521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542521")>
        <Fact()>
        Public Sub AddressOfOperatorInQuery()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module CodCov004mod
    Function scen2() As Object
        Return Nothing
    End Function

    Sub CodCov004()
        Dim q = From a In AddressOf scen2
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim diagnostics = semanticModel.GetDiagnostics()

            Assert.NotEmpty(diagnostics)
        End Sub

        <WorkItem(542823, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542823")>
        <Fact()>
        Public Sub DefaultAggregateClauseInfo()
            Dim aggrClauseSymInfo = New AggregateClauseSymbolInfo()

            Assert.Null(aggrClauseSymInfo.Select1.Symbol)
            Assert.Equal(0, aggrClauseSymInfo.Select1.CandidateSymbols.Length)
            Assert.Null(aggrClauseSymInfo.Select2.Symbol)
            Assert.Equal(0, aggrClauseSymInfo.Select2.CandidateSymbols.Length)
        End Sub

        <WorkItem(543084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543084")>
        <Fact()>
        Public Sub MissingIdentifierNameSyntaxInIncompleteLetClause()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim numbers = New Integer() {4, 5}

        Dim q1 = From num In numbers Let n As 
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToString().IndexOf("n As", StringComparison.Ordinal)).Parent.Parent.DescendantNodes().OfType(Of IdentifierNameSyntax)().First()
            Dim info = semanticModel.GetTypeInfo(node)

            Assert.NotNull(info)
            Assert.Equal(TypeInfo.None, info)
        End Sub

        <WorkItem(542914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542914")>
        <Fact()>
        Public Sub Bug10356()
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim numbers = New Integer() {4, 5}

        Dim d = From z In New Integer() {1, 2, 3}
                        Let
                        Group By
    End Sub
End Module
    ]]></file>
</compilation>, {SystemCoreRef})

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node = tree.GetCompilationUnitRoot().FindToken(tree.GetCompilationUnitRoot().ToString().IndexOf("By", StringComparison.Ordinal)).Parent.Parent.DescendantNodes().OfType(Of IdentifierNameSyntax)().First()

            Dim containingSymbol = semanticModel.GetEnclosingSymbol(node.SpanStart)

            Assert.Equal("Function (z As System.Int32) As <anonymous type: Key z As System.Int32, Key Group As ?>", DirectCast(containingSymbol, Symbol).ToTestDisplayString())
        End Sub

        <WorkItem(543161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543161")>
        <Fact()>
        Public Sub InaccessibleQueryMethodOnCollectionType()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Private Function TakeWhile(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q0 = From s1 In qi Take While False'BIND:"Take While False"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of PartitionWhileClauseSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("Function QueryAble(Of System.Int32).TakeWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble(Of System.Int32)", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(546165, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546165")>
        <Fact()>
        Public Sub QueryInsideEnumMemberDecl()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Test
    Enum Enum1
        x = (From i In New Integer() {4, 5} Where True Select 1).First 'BIND:"True"
    End Enum
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Symbol)
            Assert.True(semanticSummary.ConstantValue.HasValue)
        End Sub

    End Class
End Namespace

