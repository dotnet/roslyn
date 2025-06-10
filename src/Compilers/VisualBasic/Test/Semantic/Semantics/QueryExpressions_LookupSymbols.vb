' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class GetExtendedSemanticInfoTests

        <Fact>
        Public Sub From_Lookup()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of QueryAble(Of QueryAble(Of Integer)))(0)

        Dim q As Object 

        q = From s In qi 'BIND1:"qi"

        q = From s1 In qi, : 'BIND2:":"

        q = From s1 In qi, s2 In : 'BIND3:":"

        q = From s1 In qi, s2 In s1 From : 'BIND4:":"

        q = From s1 In qi, s2 In s1 Select s2, s1  'BIND5:"Select"

        q = From s1 In qi, s2 In s1, : 'BIND6:":"

        q = From s1 In qi, s2 In s1, : 'BIND7:"qi"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.DoesNotContain("s", names1)

                Assert.Equal("qi As QueryAble(Of QueryAble(Of QueryAble(Of System.Int32)))", semanticModel.LookupSymbols(pos1, name:="qi").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 2) - 1

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("s1", names2)

                Assert.Equal("s1 As QueryAble(Of QueryAble(Of System.Int32))", semanticModel.LookupSymbols(pos2, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 3) - 1

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("s1", names3)
                Assert.DoesNotContain("s2", names3)

                Assert.Equal("s1 As QueryAble(Of QueryAble(Of System.Int32))", semanticModel.LookupSymbols(pos3, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos4 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 4) - 1

                Dim names4 = semanticModel.LookupNames(pos4)

                Assert.Contains("q", names4)
                Assert.Contains("qi", names4)
                Assert.Contains("s1", names4)
                Assert.Contains("s2", names4)

                Assert.Equal("s1 As QueryAble(Of QueryAble(Of System.Int32))", semanticModel.LookupSymbols(pos4, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos4, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 5) - 1

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("s1", names5)
                Assert.DoesNotContain("s2", names5)

                Assert.Equal("s1 As QueryAble(Of QueryAble(Of System.Int32))", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 6) - 1

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("s1", names6)
                Assert.Contains("s2", names6)

                Assert.Equal("s1 As QueryAble(Of QueryAble(Of System.Int32))", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos6, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos7 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 7)

                Dim names7 = semanticModel.LookupNames(pos7)

                Assert.Contains("q", names7)
                Assert.Contains("qi", names7)
                Assert.DoesNotContain("s1", names7)

                Assert.Equal("qi As QueryAble(Of QueryAble(Of QueryAble(Of System.Int32)))", semanticModel.LookupSymbols(pos7, name:="qi").Single.ToTestDisplayString())
            End If

        End Sub

        <Fact>
        Public Sub Select_Lookup()
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
        Dim qi As New QueryAble()

        Dim q As Object 

        q = From s1 In qi Select s1 'BIND1:"s1"

        q = From s1 In qi Select s2 = s1 'BIND2:"s1"

        q = From s1 In qi Select : 'BIND3:":"

        q = From s1 In qi Select s2 = s1,  : 'BIND4:":"

        q = From s1 In qi, s2 In qi Select s2  'BIND5:"s2"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("s1", names1)
                Assert.DoesNotContain("s2", names1)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 2)

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("s1", names2)
                Assert.DoesNotContain("s2", names2)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos2, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 3) - 1

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("s1", names3)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos4 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 4) - 1

                Dim names4 = semanticModel.LookupNames(pos4)

                Assert.Contains("q", names4)
                Assert.Contains("qi", names4)
                Assert.Contains("s1", names4)
                Assert.DoesNotContain("s2", names4)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos4, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 5)

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("s1", names5)
                Assert.Contains("s2", names5)

                Assert.Equal(1, Aggregate n In names5 Where n.Equals("s2") Into Count())

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s2").Single.ToTestDisplayString())
            End If

        End Sub

        <Fact>
        Public Sub Filter_Lookup()
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
        Dim qi As New QueryAble()

        Dim q As Object 

        q = From s1 In qi Where s1 > 0 'BIND1:"s1"

        q = From s1 In qi Take While : 'BIND2:":"

        q = From s1 In qi Skip While s1 > 0  : 'BIND3:":"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("s1", names1)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 2) - 1

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("s1", names2)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos2, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 3) - 1

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("s1", names3)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s1").Single.ToTestDisplayString())
            End If

        End Sub

        <Fact>
        Public Sub Let_Lookup()
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
        Dim qi As New QueryAble()

        Dim q As Object 

        q = From s1 In qi Let MaxValue=s1.MaxValue 'BIND1:"s1"

        q = From s1 In qi Let s2 = s1 'BIND2:"s1"

        q = From s1 In qi Let : 'BIND3:":"

        q = From s1 In qi Let s2 = s1, MaxValue = s1.MaxValue, : 'BIND4:":"

        q = From s1 In qi, s2 In qi Let s3 = s2  'BIND5:"s2"

        q = From s1 In qi Select s1 + 1 Let s3 = s1  'BIND6:"s1"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("s1", names1)
                Assert.DoesNotContain("MaxValue", names1)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 2)

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("s1", names2)
                Assert.DoesNotContain("s2", names2)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos2, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 3) - 1

                Dim names2 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("s1", names2)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s1").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos4 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 4) - 1

                Dim names4 = semanticModel.LookupNames(pos4)

                Assert.Contains("q", names4)
                Assert.Contains("qi", names4)
                Assert.Contains("s1", names4)
                Assert.Contains("s2", names4)
                Assert.Contains("MaxValue", names4)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos4, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos4, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("MaxValue As System.Int32", semanticModel.LookupSymbols(pos4, name:="MaxValue").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 5)

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("s1", names5)
                Assert.Contains("s2", names5)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 6)

                Dim names5 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.DoesNotContain("s1", names5)
            End If
        End Sub

        <Fact>
        Public Sub Partition_Lookup()
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
        Dim qi As New QueryAble()

        Dim q As Object 

        q = From s1 In qi Skip 1 'BIND1:"1"

        q = From s1 In qi Take : 'BIND2:":"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.DoesNotContain("s1", names1)

                Assert.Equal("qi As QueryAble", semanticModel.LookupSymbols(pos1, name:="qi").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 2) - 1

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.DoesNotContain("s1", names2)

                Assert.Equal("qi As QueryAble", semanticModel.LookupSymbols(pos2, name:="qi").Single.ToTestDisplayString())
            End If
        End Sub

        <Fact>
        <CompilerTrait(CompilerFeature.IOperation)>
        Public Sub GroupBy_Lookup1()
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
        Dim qi As New QueryAble()

        Dim q As Object 

        q = From s1 In qi, s2 in qi Group : 'BIND1:":"

        q = From s1 In qi, s2 in qi Group s1 : 'BIND2:":"

        q = From s1 In qi, s2 in qi Group s1, :  'BIND3:":"

        q = From s1 In qi, s2 in qi Group By :  'BIND4:":"

        q = From s1 In qi, s2 in qi Group By s1 :  'BIND5:":"

        q = From s1 In qi, s2 in qi Group By s1, :  'BIND6:":"

        q = From s1 In qi, s2 in qi Group s3 = s1 By s1 :  'BIND7:":"

        q = From s1 In qi, s2 in qi Group s3 = s1 By s1, :  'BIND8:":"

        q = From s1 In qi, s2 in qi Group By s1 Into Count( : 'BIND9:":"

        q = From s1 In qi, s2 in qi Group By s1 Into Count() : 'BIND10:")"

        q = From s1 In qi, s2 in qi Group By s1 Into Count(s1) : 'BIND11:"s1"

        q = From s1 In qi, s2 in qi Group s3 = s2 By s1 Into Count() : 'BIND12:")"

        q = From s1 In qi, s2 in qi Group s4 = s2 By s1 Into Count(), Avg() : 'BIND13:")"

        q = From s1 In qi, s2 in qi Group s5 = s2 By s1 Into Count(), Avg(s3) : 'BIND14:"s3"

        q = From s1 In qi, s2 in qi Group By s1 :  'BIND15:"Group"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            If True Then
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("s1", names1)
                Assert.Contains("s2", names1)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 2) - 1

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("s1", names2)
                Assert.Contains("s2", names2)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos2, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos2, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 3) - 1

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("s1", names3)
                Assert.Contains("s2", names3)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos4 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 4) - 1

                Dim names4 = semanticModel.LookupNames(pos4)

                Assert.Contains("q", names4)
                Assert.Contains("qi", names4)
                Assert.Contains("s1", names4)
                Assert.Contains("s2", names4)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos4, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos4, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 5) - 1

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("s1", names5)
                Assert.Contains("s2", names5)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 6) - 1

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("s1", names6)
                Assert.Contains("s2", names6)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos7 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 7) - 1

                Dim names7 = semanticModel.LookupNames(pos7)

                Assert.Contains("q", names7)
                Assert.Contains("qi", names7)
                Assert.Contains("s1", names7)
                Assert.Contains("s2", names7)
                Assert.DoesNotContain("s3", names7)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos7, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos7, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos8 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 8) - 1

                Dim names8 = semanticModel.LookupNames(pos8)

                Assert.Contains("q", names8)
                Assert.Contains("qi", names8)
                Assert.Contains("s1", names8)
                Assert.Contains("s2", names8)
                Assert.DoesNotContain("s3", names8)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos8, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos8, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos9 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 9) - 1

                Dim names9 = semanticModel.LookupNames(pos9)

                Assert.Contains("q", names9)
                Assert.Contains("qi", names9)
                Assert.Contains("s1", names9)
                Assert.Contains("s2", names9)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos9, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos9, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos10 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 10) - 1

                Dim names10 = semanticModel.LookupNames(pos10)

                Assert.Contains("q", names10)
                Assert.Contains("qi", names10)
                Assert.Contains("s1", names10)
                Assert.Contains("s2", names10)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos10, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos10, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos11 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 11)

                Dim names11 = semanticModel.LookupNames(pos11)

                Assert.Contains("q", names11)
                Assert.Contains("qi", names11)
                Assert.Contains("s1", names11)
                Assert.Contains("s2", names11)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos11, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos11, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos12 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 12) - 1

                Dim names12 = semanticModel.LookupNames(pos12)

                Assert.Contains("q", names12)
                Assert.Contains("qi", names12)
                Assert.DoesNotContain("s1", names12)
                Assert.DoesNotContain("s2", names12)
                Assert.Contains("s3", names12)

                Assert.Equal("s3 As System.Int32", semanticModel.LookupSymbols(pos12, name:="s3").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos13 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 13) - 1

                Dim names13 = semanticModel.LookupNames(pos13)

                Assert.Contains("q", names13)
                Assert.Contains("qi", names13)
                Assert.DoesNotContain("s1", names13)
                Assert.DoesNotContain("s2", names13)
                Assert.Contains("s4", names13)

                Assert.Equal("s4 As System.Int32", semanticModel.LookupSymbols(pos13, name:="s4").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos14 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 14) - 1

                Dim names14 = semanticModel.LookupNames(pos14)

                Assert.Contains("q", names14)
                Assert.Contains("qi", names14)
                Assert.DoesNotContain("s1", names14)
                Assert.DoesNotContain("s2", names14)
                Assert.Contains("s5", names14)

                Assert.Equal("s5 As System.Int32", semanticModel.LookupSymbols(pos14, name:="s5").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos15 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 15)

                Dim names15 = semanticModel.LookupNames(pos15)

                Assert.Contains("q", names15)
                Assert.Contains("qi", names15)
                Assert.Contains("s1", names15)
                Assert.Contains("s2", names15)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos15, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos15, name:="s2").Single.ToTestDisplayString())
            End If

            Dim node = tree.GetRoot().DescendantNodes().OfType(Of QueryExpressionSyntax)().First()

            Assert.Equal("From s1 In qi, s2 in qi Group ", node.ToString())

            compilation.VerifyOperationTree(node, expectedOperationTree:=
            <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s1 In  ... in qi Group')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group ')
      Children(4):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 's2 in qi')
            Children(3):
                ILocalReferenceOperation: qi (OperationKind.LocalReference, Type: QueryAble, IsInvalid) (Syntax: 'qi')
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'qi')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'qi')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'qi')
                      ReturnedValue: 
                        ILocalReferenceOperation: qi (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'qi')
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, s2 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'From s1 In qi, s2 in qi')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'From s1 In qi, s2 in qi')
                    IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'From s1 In qi, s2 in qi')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 in qi')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 's1 In qi')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 in qi')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 in qi')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 in qi')
                                Right: 
                                  IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
          IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) As ?) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: '')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
          IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) As ?) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: '')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
          IAnonymousFunctionOperation (Symbol: Function ($315 As ?, $VB$ItAnonymous As ?) As <anonymous type: Key $315 As ?, Key $315 As ?>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group ')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group ')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group ')
                ReturnedValue: 
                  IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key $315 As ?, Key $315 As ?>, IsInvalid, IsImplicit) (Syntax: 'Group ')
                    Initializers(2):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group ')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $315 As ?, Key $315 As ?>.$315 As ? (OperationKind.PropertyReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $315 As ?, Key $315 As ?>, IsInvalid, IsImplicit) (Syntax: 'Group ')
                          Right: 
                            IParameterReferenceOperation: $315 (OperationKind.ParameterReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: '')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $315 As ?, Key $315 As ?>.$315 As ? (OperationKind.PropertyReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $315 As ?, Key $315 As ?>, IsInvalid, IsImplicit) (Syntax: 'Group ')
                          Right: 
                            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                              Children(1):
                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group ')
]]>.Value)
        End Sub

        <Fact>
        Public Sub GroupBy_Lookup2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
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

End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)

        Dim q As Object 

        q = From s1 In qi Group By s1 Into : 'BIND1:":"

        q = From s1 In qi Group By s1 Into Count : 'BIND2:":"

        q = From s1 In qi Group By s1 Into Count()  : 'BIND3:":"

        q = From s1 In qi Group By s1 Into Count(), : 'BIND4:":"

        q = From s1 In qi Group By s1 Into Count(),  : 'BIND9:":"

        q = From s1 In qi Group s2 = s1 By s1 Into : 'BIND5:":"

        q = From s1 In qi Group s2 = s1 By s1 Into Count : 'BIND6:":"

        q = From s1 In qi Group s2 = s1 By s1 Into Count()  : 'BIND7:":"

        q = From s1 In qi Group s2 = s1 By s1 Into Count(), : 'BIND8:":"

        q = From s1 In qi Group s2 = s1 By s1 Into Count(),  : 'BIND10:":"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For i As Integer = 1 To 10
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Equal("Count, Equals, GetHashCode, GetType, GroupBy, Select, ToString", String.Join(", ", names1.OrderBy(Function(n) n)))

                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of S)(x As System.Func(Of System.Int32, S)) As QueryAble(Of S)", semanticModel.LookupSymbols(pos1, name:="Select").Single.ToTestDisplayString())
                Assert.False(semanticModel.LookupSymbols(pos1, name:="v").Any)
                Assert.Equal("Count, Equals, GetHashCode, GetType, GroupBy, GroupBy, Select, ToString",
                             String.Join(", ", semanticModel.LookupSymbols(pos1).
                                               Select(Function(s) s.Name).OrderBy(Function(n) n)))
            Next

        End Sub

        <Fact>
        Public Sub GroupBy_Lookup3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim q As Object 

        q = From s1 In New Integer() {1,2,3} Group By s1 Into : 'BIND1:":"
    End Sub
End Module
    ]]></file>
</compilation>, references:={SystemCoreRef})

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 1) - 1
            Assert.True(semanticModel.LookupSymbols(pos1, name:="Count", includeReducedExtensionMethods:=True).Any)

            Dim target = {"Select", "SelectMany", "Where", "Count", "Sum", "Distinct", "Average", "GroupBy"}

            Dim lookupResult = New HashSet(Of String)(semanticModel.LookupSymbols(pos1, includeReducedExtensionMethods:=True).Select(Function(s) s.Name))
            Assert.Equal(0, Aggregate name In target Where Not lookupResult.Contains(name) Into Count())
        End Sub

        <Fact>
        Public Sub OrderBy_Lookup()
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
        Dim qi As New QueryAble()

        Dim q As Object 

        q = From s1 In qi, s2 In qi Let s3 = s2 Order : 'BIND1:":"

        q = From s1 In qi, s2 In qi Let s3 = s2 Order By : 'BIND2:":"

        q = From s1 In qi, s2 In qi Let s3 = s2 Order By : 'BIND3:"By"

        q = From s1 In qi, s2 In qi Let s3 = s2 Order By : 'BIND4:"Order"

        q = From s1 In qi, s2 In qi Let s3 = s2 Order By s1 'BIND5:"s1"

        q = From s1 In qi, s2 In qi Let s3 = s2 Order By s1, : 'BIND6:":"

        q = From s1 In qi, s2 In qi Let s3 = s2 Order By s1, s2 'BIND7:"s2"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For i As Integer = 1 To 2
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("s1", names1)
                Assert.Contains("s2", names1)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos1, name:="s2").Single.ToTestDisplayString())
            Next

            If True Then
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 3)

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("s1", names3)
                Assert.Contains("s2", names3)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos3, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos4 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 4)

                Dim names4 = semanticModel.LookupNames(pos4)

                Assert.Contains("q", names4)
                Assert.Contains("qi", names4)
                Assert.DoesNotContain("s1", names4)
                Assert.DoesNotContain("s2", names4)
            End If

            If True Then
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 5)

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("s1", names5)
                Assert.Contains("s2", names5)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 6) - 1

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("s1", names6)
                Assert.Contains("s2", names6)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s2").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos7 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 7)

                Dim names7 = semanticModel.LookupNames(pos7)

                Assert.Contains("q", names7)
                Assert.Contains("qi", names7)
                Assert.Contains("s1", names7)
                Assert.Contains("s2", names7)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos7, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Int32", semanticModel.LookupSymbols(pos7, name:="s2").Single.ToTestDisplayString())
            End If

        End Sub

        <Fact>
        Public Sub Join_Lookup()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q As Object 

        q = From s In qi Join :'BIND1:":"

        q = From s1 In qi Join s2 In qb : 'BIND2:"qb"

        q = From s1 In qi Join s2 In : 'BIND3:"s2"

        q = From s1 In qi Join s2 In : 'BIND4:":"

        q = From s1 In qi Join s2 In qb On : 'BIND5:":"

        q = From s1 In qi Join s2 In qb On  'BIND6:"On"

        q = From s1 In qi Join s2 In qb On : 'BIND7:":"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 : 'BIND8:":"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 : 'BIND9:"s1"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 : 'BIND10:"s2"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 And : 'BIND11:":"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 And s2 Equals s1 : 'BIND12:":"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 And s2 Equals s1 : 'BIND13:"s2 Equals"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 And s2 Equals s1 : 'BIND14:"s1 :"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 Select s2, s1  'BIND15:"Select"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 Select s2, s1  'BIND16:"s2, s1"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 Select s2, s1 : 'BIND17:"s1 :"

        q = From s1 In qi Join s2 In qb On s1 Equals s2 Select s2, s1 : 'BIND18:":"

        Dim qs As New QueryAble(Of Short)(0)
        Dim ql As New QueryAble(Of Long)(0)

        q = From s1 In qi 
                Join s2 In qb  
                    Join s3 in qs 'BIND19:"qs"
                On s2 Equals s3 'BIND20:"s2"
                    Join s4 in ql 'BIND21:"ql"
                On s4 Equals s3 'BIND22:"s3"
            On s1 Equals s2 'BIND23:"s1"

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For Each i In {1, 4}
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("qb", names1)
                Assert.DoesNotContain("s1", names1)
                Assert.DoesNotContain("s2", names1)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos1, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {2, 3}
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("qb", names2)
                Assert.DoesNotContain("s1", names2)
                Assert.DoesNotContain("s2", names2)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos2, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {5, 7, 8, 11, 12, 18}
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("qb", names5)
                Assert.Contains("s1", names5)
                Assert.Contains("s2", names5)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos5, name:="s2").Single.ToTestDisplayString())
            Next

            For Each i In {6, 9, 10, 13, 14, 15, 16, 17}
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("qb", names6)
                Assert.Contains("s1", names6)
                Assert.Contains("s2", names6)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos6, name:="s2").Single.ToTestDisplayString())
            Next

            For Each i In {19, 21}
                Dim pos19 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names19 = semanticModel.LookupNames(pos19)

                Assert.Contains("q", names19)
                Assert.Contains("qi", names19)
                Assert.Contains("qb", names19)
                Assert.DoesNotContain("s1", names19)
                Assert.DoesNotContain("s2", names19)
                Assert.DoesNotContain("s3", names19)
                Assert.DoesNotContain("s4", names19)
            Next

            If True Then
                Dim pos20 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 20)

                Dim names20 = semanticModel.LookupNames(pos20)

                Assert.Contains("q", names20)
                Assert.Contains("qs", names20)
                Assert.Contains("ql", names20)
                Assert.DoesNotContain("s1", names20)
                Assert.Contains("s2", names20)
                Assert.Contains("s3", names20)
                Assert.DoesNotContain("s4", names20)

                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos20, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("s3 As System.Int16", semanticModel.LookupSymbols(pos20, name:="s3").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos22 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 22)

                Dim names22 = semanticModel.LookupNames(pos22)

                Assert.Contains("q", names22)
                Assert.Contains("qs", names22)
                Assert.Contains("ql", names22)
                Assert.DoesNotContain("s1", names22)
                Assert.Contains("s2", names22)
                Assert.Contains("s3", names22)
                Assert.Contains("s4", names22)

                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos22, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("s3 As System.Int16", semanticModel.LookupSymbols(pos22, name:="s3").Single.ToTestDisplayString())
                Assert.Equal("s4 As System.Int64", semanticModel.LookupSymbols(pos22, name:="s4").Single.ToTestDisplayString())
            End If

            If True Then
                Dim pos23 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 23)

                Dim names23 = semanticModel.LookupNames(pos23)

                Assert.Contains("q", names23)
                Assert.Contains("qs", names23)
                Assert.Contains("ql", names23)
                Assert.Contains("s1", names23)
                Assert.Contains("s2", names23)
                Assert.Contains("s3", names23)
                Assert.Contains("s4", names23)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos23, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos23, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("s3 As System.Int16", semanticModel.LookupSymbols(pos23, name:="s3").Single.ToTestDisplayString())
                Assert.Equal("s4 As System.Int64", semanticModel.LookupSymbols(pos23, name:="s4").Single.ToTestDisplayString())
            End If

        End Sub

        <Fact>
        Public Sub GroupJoin_Lookup1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q As Object 

        q = From s In qi Group Join :'BIND1:":"

        q = From s1 In qi Group Join s2 In qb : 'BIND2:"qb"

        q = From s1 In qi Group Join s2 In : 'BIND3:"s2"

        q = From s1 In qi Group Join s2 In : 'BIND4:":"

        q = From s1 In qi Group Join s2 In qb On : 'BIND5:":"

        q = From s1 In qi Group Join s2 In qb On  'BIND6:"On"

        q = From s1 In qi Group Join s2 In qb On : 'BIND7:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 : 'BIND8:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 : 'BIND9:"s1"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 : 'BIND10:"s2"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 And : 'BIND11:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 And s2 Equals s1 : 'BIND12:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 And s2 Equals s1 : 'BIND13:"s2 Equals"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 And s2 Equals s1 : 'BIND14:"s1 :"

        Dim qs As New QueryAble(Of Short)(0)
        Dim ql As New QueryAble(Of Long)(0)

        q = From s1 In qi 
                Group Join s2 In qb  
                    Group Join s3 in qs 'BIND19:"qs"
                On s2 Equals s3 'BIND20:"s2"
                Into s5 = Group
                    Group Join s4 in ql 'BIND21:"ql"
                On s4 Equals s5 'BIND22:"s5"
                Into s6 = Count() 'BIND24:"("
            On s1 Equals s2 'BIND23:"s1"
            Into s7 = Group, Count() 'BIND25:"("

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For Each i In {1, 4}
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("qb", names1)
                Assert.DoesNotContain("s1", names1)
                Assert.DoesNotContain("s2", names1)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos1, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {2, 3}
                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Contains("q", names2)
                Assert.Contains("qi", names2)
                Assert.Contains("qb", names2)
                Assert.DoesNotContain("s1", names2)
                Assert.DoesNotContain("s2", names2)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos2, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {5, 7, 8, 11, 12}
                Dim pos5 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names5 = semanticModel.LookupNames(pos5)

                Assert.Contains("q", names5)
                Assert.Contains("qi", names5)
                Assert.Contains("qb", names5)
                Assert.Contains("s1", names5)
                Assert.Contains("s2", names5)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos5, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos5, name:="s2").Single.ToTestDisplayString())
            Next

            For Each i In {6, 9, 10, 13, 14}
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("qb", names6)
                Assert.Contains("s1", names6)
                Assert.Contains("s2", names6)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos6, name:="s2").Single.ToTestDisplayString())
            Next

            For Each i In {19, 21}
                Dim pos19 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names19 = semanticModel.LookupNames(pos19)

                Assert.Contains("q", names19)
                Assert.Contains("qi", names19)
                Assert.Contains("qb", names19)
                Assert.DoesNotContain("s1", names19)
                Assert.DoesNotContain("s2", names19)
                Assert.DoesNotContain("s3", names19)
                Assert.DoesNotContain("s4", names19)
                Assert.DoesNotContain("s5", names19)
                Assert.DoesNotContain("s6", names19)
                Assert.DoesNotContain("s7", names19)
            Next

            If True Then
                Dim pos20 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, 20)

                Dim names20 = semanticModel.LookupNames(pos20)

                Assert.Contains("q", names20)
                Assert.Contains("qs", names20)
                Assert.Contains("ql", names20)
                Assert.DoesNotContain("s1", names20)
                Assert.Contains("s2", names20)
                Assert.Contains("s3", names20)
                Assert.DoesNotContain("s4", names20)
                Assert.DoesNotContain("s5", names20)
                Assert.DoesNotContain("s6", names20)
                Assert.DoesNotContain("s7", names20)

                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos20, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("s3 As System.Int16", semanticModel.LookupSymbols(pos20, name:="s3").Single.ToTestDisplayString())
            End If

            For Each i In {22, 24}
                Dim pos22 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names22 = semanticModel.LookupNames(pos22)

                Assert.Contains("q", names22)
                Assert.Contains("qs", names22)
                Assert.Contains("ql", names22)
                Assert.DoesNotContain("s1", names22)
                Assert.Contains("s2", names22)
                Assert.DoesNotContain("s3", names22)
                Assert.Contains("s4", names22)
                Assert.Contains("s5", names22)
                Assert.DoesNotContain("s6", names22)
                Assert.DoesNotContain("s7", names22)

                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos22, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("s4 As System.Int64", semanticModel.LookupSymbols(pos22, name:="s4").Single.ToTestDisplayString())
                Assert.Equal("s5 As ?", semanticModel.LookupSymbols(pos22, name:="s5").Single.ToTestDisplayString())
            Next

            For Each i In {23, 25}
                Dim pos23 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names23 = semanticModel.LookupNames(pos23)

                Assert.Contains("q", names23)
                Assert.Contains("qs", names23)
                Assert.Contains("ql", names23)
                Assert.Contains("s1", names23)
                Assert.Contains("s2", names23)
                Assert.DoesNotContain("s3", names23)
                Assert.DoesNotContain("s4", names23)
                Assert.Contains("s5", names23)
                Assert.Contains("s6", names23)
                Assert.DoesNotContain("s7", names23)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos23, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos23, name:="s2").Single.ToTestDisplayString())
                Assert.Equal("s5 As ?", semanticModel.LookupSymbols(pos23, name:="s5").Single.ToTestDisplayString())
                Assert.Equal("s6 As ?", semanticModel.LookupSymbols(pos23, name:="s6").Single.ToTestDisplayString())
            Next

        End Sub

        <Fact>
        Public Sub GroupJoin_Lookup2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q As Object 

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into : 'BIND1:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Count : 'BIND2:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Count() : 'BIND3:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Count(), : 'BIND4:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Count()  : 'BIND5:":"

        q = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Count(),  : 'BIND6:":"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For i As Integer = 1 To 6
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Equal("Count, Equals, GetHashCode, GetType, GroupJoin, Select, ToString", String.Join(", ", names1.OrderBy(Function(n) n)))

                Assert.Equal("Function QueryAble(Of System.Byte).Select(Of S)(x As System.Func(Of System.Byte, S)) As QueryAble(Of S)", semanticModel.LookupSymbols(pos1, name:="Select").Single.ToTestDisplayString())
                Assert.False(semanticModel.LookupSymbols(pos1, name:="v").Any)
                Assert.Equal("Count, Equals, GetHashCode, GetType, GroupJoin, Select, ToString",
                             String.Join(", ", semanticModel.LookupSymbols(pos1).
                                               Select(Function(s) s.Name).OrderBy(Function(n) n)))
            Next

        End Sub

        <Fact>
        Public Sub Aggregate_Lookup1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q As Object 

        q = Aggregate s1 In :'BIND1:":"

        q = Aggregate s1 In qi :'BIND2:":"

        q = Aggregate s1 In qi :'BIND3:"qi"

        q = Aggregate s1 In qi Into Count() 'BIND4:"qi"

        q = Aggregate s1 In qi Into Count, Avg() 'BIND5:"qi"

        q = Aggregate s1 In qi, :'BIND6:":"

        q = Aggregate s1 In qi, s2 In qb :'BIND7:":"

        q = Aggregate s1 In qi, s2 In qb Into Count() :'BIND8:"qb"

        q = Aggregate s1 In qi, s2 In qb Into Count(), Avg() :'BIND9:"qb"

        q = Aggregate s1 In qi From 'BIND10:"From"

        q = Aggregate s1 In qi From s2 In qb Into Count() 'BIND11:"From"

        q = Aggregate s1 In qi From s2 In qb Into Count(), Avg() 'BIND12:"From"

        q = Aggregate s1 In qi Join 'BIND13:"Join"

        q = Aggregate s1 In qi Join s2 In qb On s1 Equals s2 Into Count() 'BIND14:"Join"

        q = Aggregate s1 In qi Join s2 In qb On s1 Equals s2 'BIND15:"Equals"
            Into Count(s1) 'BIND18:"s1" 

        q = Aggregate s1 In qi Join s2 In qb On s1 Equals s2 Into Count(), Avg() 'BIND16:"Join"

        q = Aggregate s1 In qi Join s2 In qb On s1 Equals s2 'BIND17:"Equals"
            Into Count(s1), 'BIND19:"s1" 
                 Avg(s2) 'BIND20:"s2" 
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For Each i In {1, 2}
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("qb", names1)
                Assert.DoesNotContain("s1", names1)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos1, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {3, 4, 5}
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("qb", names3)
                Assert.DoesNotContain("s1", names3)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos3, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {6, 7}
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("qb", names6)
                Assert.Contains("s1", names6)
                Assert.DoesNotContain("s2", names6)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
            Next

            For Each i In {8, 9, 10, 11, 12}
                Dim pos8 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names8 = semanticModel.LookupNames(pos8)

                Assert.Contains("q", names8)
                Assert.Contains("qi", names8)
                Assert.Contains("qb", names8)
                Assert.Contains("s1", names8)
                Assert.DoesNotContain("s2", names8)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos8, name:="s1").Single.ToTestDisplayString())
            Next

            For Each i In {13, 14, 16}
                Dim pos13 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names13 = semanticModel.LookupNames(pos13)

                Assert.Contains("q", names13)
                Assert.Contains("qi", names13)
                Assert.Contains("qb", names13)
                Assert.DoesNotContain("s1", names13)
                Assert.DoesNotContain("s2", names13)

                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos13, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {15, 17, 18, 19, 20}
                Dim pos15 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names15 = semanticModel.LookupNames(pos15)

                Assert.Contains("q", names15)
                Assert.Contains("qi", names15)
                Assert.Contains("qb", names15)
                Assert.Contains("s1", names15)
                Assert.Contains("s2", names15)

                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos15, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos15, name:="s2").Single.ToTestDisplayString())
            Next
        End Sub

        <Fact>
        Public Sub Aggregate_Lookup2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)

        Dim q As Object 

        q = From s0 In qs Aggregate s1 In :'BIND1:":"

        q = From s0 In qs Aggregate s1 In qi :'BIND2:":"

        q = From s0 In qs Aggregate s1 In qi :'BIND3:"qi"

        q = From s0 In qs Aggregate s1 In qi Into Count() 'BIND4:"qi"

        q = From s0 In qs Aggregate s1 In qi Into Count, Avg() 'BIND5:"qi"

        q = From s0 In qs Aggregate s1 In qi, :'BIND6:":"

        q = From s0 In qs Aggregate s1 In qi, s2 In qb :'BIND7:":"

        q = From s0 In qs Aggregate s1 In qi, s2 In qb Into Count() :'BIND8:"qb"

        q = From s0 In qs Aggregate s1 In qi, s2 In qb Into Count(), Avg() :'BIND9:"qb"

        q = From s0 In qs Aggregate s1 In qi From 'BIND10:"From"

        q = From s0 In qs Aggregate s1 In qi From s2 In qb Into Count() 'BIND11:"From"

        q = From s0 In qs Aggregate s1 In qi From s2 In qb Into Count(), Avg() 'BIND12:"From"

        q = From s0 In qs Aggregate s1 In qi Join 'BIND13:"Join"

        q = From s0 In qs Aggregate s1 In qi Join s2 In qb On s1 Equals s2 Into Count() 'BIND14:"Join"

        q = From s0 In qs Aggregate s1 In qi Join s2 In qb On s1 Equals s2 'BIND15:"Equals"
            Into Count(s1) 'BIND18:"s1" 

        q = From s0 In qs Aggregate s1 In qi Join s2 In qb On s1 Equals s2 Into Count(), Avg() 'BIND16:"Join"

        q = From s0 In qs Aggregate s1 In qi Join s2 In qb On s1 Equals s2 'BIND17:"Equals"
            Into Count(s1), 'BIND19:"s1" 
                 Avg(s2) 'BIND20:"s2" 
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For Each i In {1, 2}
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("qb", names1)
                Assert.Contains("s0", names1)
                Assert.DoesNotContain("s1", names1)

                Assert.Equal("s0 As System.Int16", semanticModel.LookupSymbols(pos1, name:="s0").Single.ToTestDisplayString())
                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos1, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {3, 4, 5}
                Dim pos3 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names3 = semanticModel.LookupNames(pos3)

                Assert.Contains("q", names3)
                Assert.Contains("qi", names3)
                Assert.Contains("qb", names3)
                Assert.Contains("s0", names3)
                Assert.DoesNotContain("s1", names3)

                Assert.Equal("s0 As System.Int16", semanticModel.LookupSymbols(pos3, name:="s0").Single.ToTestDisplayString())
                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos3, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {6, 7}
                Dim pos6 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i) - 1

                Dim names6 = semanticModel.LookupNames(pos6)

                Assert.Contains("q", names6)
                Assert.Contains("qi", names6)
                Assert.Contains("qb", names6)
                Assert.Contains("s0", names6)
                Assert.Contains("s1", names6)
                Assert.DoesNotContain("s2", names6)

                Assert.Equal("s0 As System.Int16", semanticModel.LookupSymbols(pos6, name:="s0").Single.ToTestDisplayString())
                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos6, name:="s1").Single.ToTestDisplayString())
            Next

            For Each i In {8, 9, 10, 11, 12}
                Dim pos8 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names8 = semanticModel.LookupNames(pos8)

                Assert.Contains("q", names8)
                Assert.Contains("qi", names8)
                Assert.Contains("qb", names8)
                Assert.Contains("s0", names8)
                Assert.Contains("s1", names8)
                Assert.DoesNotContain("s2", names8)

                Assert.Equal("s0 As System.Int16", semanticModel.LookupSymbols(pos8, name:="s0").Single.ToTestDisplayString())
                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos8, name:="s1").Single.ToTestDisplayString())
            Next

            For Each i In {13, 14, 16}
                Dim pos13 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names13 = semanticModel.LookupNames(pos13)

                Assert.Contains("q", names13)
                Assert.Contains("qi", names13)
                Assert.Contains("qb", names13)
                Assert.Contains("s0", names13)
                Assert.DoesNotContain("s1", names13)
                Assert.DoesNotContain("s2", names13)

                Assert.Equal("s0 As System.Int16", semanticModel.LookupSymbols(pos13, name:="s0").Single.ToTestDisplayString())
                Assert.Equal("qi As QueryAble(Of System.Int32)", semanticModel.LookupSymbols(pos13, name:="qi").Single.ToTestDisplayString())
            Next

            For Each i In {15, 17, 18, 19, 20}
                Dim pos15 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names15 = semanticModel.LookupNames(pos15)

                Assert.Contains("q", names15)
                Assert.Contains("qi", names15)
                Assert.Contains("qb", names15)
                Assert.Contains("s0", names15)
                Assert.Contains("s1", names15)
                Assert.Contains("s2", names15)

                Assert.Equal("s0 As System.Int16", semanticModel.LookupSymbols(pos15, name:="s0").Single.ToTestDisplayString())
                Assert.Equal("s1 As System.Int32", semanticModel.LookupSymbols(pos15, name:="s1").Single.ToTestDisplayString())
                Assert.Equal("s2 As System.Byte", semanticModel.LookupSymbols(pos15, name:="s2").Single.ToTestDisplayString())
            Next
        End Sub

        <Fact>
        Public Sub Aggregate_Lookup3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Count(Of S)(x As Func(Of T, S)) As Integer
        Return 0
    End Function

End Class

Module Program
    Sub Main(args As String())
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)

        Dim q As Object 

        q = Aggregate s1 In qi Into  'BIND1:"Into"

        q = Aggregate s1 In qi Into Count : 'BIND2:"Count"

        q = Aggregate s1 In qi Into Count() : 'BIND3:"Count"

        q = Aggregate s1 In qi Into Count(), Avg() : 'BIND4:"Avg"

        q = From s0 In qb Aggregate s1 In qi Into  'BIND5:"Into"

        q = From s0 In qb Aggregate s1 In qi Into Count : 'BIND6:"Count"

        q = From s0 In qb Aggregate s1 In qi Into Count() : 'BIND7:"Count"

        q = From s0 In qb Aggregate s1 In qi Into Count(), Avg() : 'BIND8:"Avg"

        q = Aggregate s1 In qi Into Count()  : 'BIND9:"Count"

        q = Aggregate s1 In qi Into Count(), Avg()  : 'BIND10:"Avg"

        q = From s0 In qb Aggregate s1 In qi Into Count()  : 'BIND11:"Count"

        q = From s0 In qb Aggregate s1 In qi Into Count(), Avg()  : 'BIND12:"Avg"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For i As Integer = 1 To 12
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Equal("Count, Equals, GetHashCode, GetType, GroupJoin, Select, ToString", String.Join(", ", names1.OrderBy(Function(n) n)))

                Assert.Equal("Function QueryAble(Of System.Int32).Select(Of S)(x As System.Func(Of System.Int32, S)) As QueryAble(Of S)", semanticModel.LookupSymbols(pos1, name:="Select").Single.ToTestDisplayString())
                Assert.False(semanticModel.LookupSymbols(pos1, name:="v").Any)
                Assert.Equal("Count, Equals, GetHashCode, GetType, GroupJoin, Select, ToString",
                             String.Join(", ", semanticModel.LookupSymbols(pos1).
                                               Select(Function(s) s.Name).OrderBy(Function(n) n)))
            Next
        End Sub

        <Fact>
        Public Sub Aggregate_Lookup4()
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
             Aggregate s In qs 'BIND1:"qs"
             Into Where(True) 'BIND2:"Where"

        System.Console.WriteLine("------")
        q0 = From i In qi, b In qb
             Aggregate s In qs 'BIND3:"qs"
             Into Where(True), Distinct() 'BIND4:"Where"

        q0 = From i In qi Join b In qb On b Equals i
             Aggregate s In qs 'BIND5:"qs"
             Into Where(True) 'BIND6:"Where"

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb On b Equals i
             Aggregate s In qs 'BIND7:"qs"
             Into Where(True), Distinct() 'BIND8:"Where"

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            For i As Integer = 1 To 7 Step 2
                Dim pos1 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i)

                Dim names1 = semanticModel.LookupNames(pos1)

                Assert.Contains("q0", names1)
                Assert.Contains("qi", names1)
                Assert.Contains("qb", names1)
                Assert.Contains("qs", names1)
                Assert.Contains("i", names1)
                Assert.Contains("b", names1)

                Dim pos2 As Integer = CompilationUtils.FindBindingTextPosition(compilation, "a.vb", Nothing, i + 1)

                Dim names2 = semanticModel.LookupNames(pos2)

                Assert.Equal("Distinct, Equals, GetHashCode, GetType, GroupBy, GroupJoin, Join, OrderBy, Select, SelectMany, Skip, SkipWhile, Take, TakeWhile, ToString, Where", String.Join(", ", names2.OrderBy(Function(n) n)))
            Next
        End Sub

    End Class
End Namespace
