' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Partial Public Class GetExtendedSemanticInfoTests

        <Fact>
        Public Sub Lambda1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Dim y As System.Action(Of String) = Sub(z)
                                                z.Clone()'BIND:"z"
                                            End Sub
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Dim y As System.Action(Of String) = Sub(z) z.Clone()'BIND:"z"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Dim y As System.Func(Of String, Object) = Function(z)
                                                      Return z.Clone() 'BIND:"z"
    End Sub
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Dim y As System.Func(Of String, Object) = Function(z) z.Clone()'BIND:"z"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Dim y As System.Func(Of System.Action(Of String)) = Function() Sub(z) 
                                                                z.Clone() 'BIND:"z"
                                                            End Sub
    End Sub
End Module
    </file>
</compilation>)

            Dim node As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim semanticInfo As SemanticInfoSummary = CompilationUtils.GetSemanticInfoSummary(semanticModel, DirectCast(node, ExpressionSyntax))

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.NotNull(semanticInfo.Symbol)
            Dim paramSymbol = semanticInfo.Symbol
            Assert.Equal("z As System.String", paramSymbol.ToTestDisplayString())

            ' Get info again
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, DirectCast(node, ExpressionSyntax))
            Assert.Same(paramSymbol, semanticInfo.Symbol)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(semanticModel, DirectCast(node.Parent.Parent, ExpressionSyntax))

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub Lambda6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Test(Function() Sub(z)
                            z.Clone()'BIND:"z"
                        End Sub)
    End Sub

    Sub Test(v As System.Func(Of System.Action(Of String)))
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Test(Function() Sub(z)
                            z.Clone()'BIND:"z"
                        End Sub)
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.Object", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Test(Function() Function(z As String)
                            Return z.Clone()'BIND:"z"
                        End Function)
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda9()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x As String = Nothing

        Test(Function() As System.Func(Of String, Object)
                 Return Function(z)
                            Return z.Clone()'BIND:"z"
                        End Function
             End Function)
    End Sub

End Module

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda10()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Test(Sub(x As System.Func(Of String, Object))
                 x = Function(z)
                         Return z.Clone()'BIND:"z"
                     End Function
             End Sub)
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("z As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda11()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Test(Sub(x As System.Func(Of String, Object))
                 x = Function(z)
                         Dim y As System.Guid
                         Return y'BIND:"y"
                     End Function
             End Sub)
    End Sub

    'Sub Test(p As System.Action(Of System.Func(Of String, Object)))
    'End Sub

End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Guid", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("y As System.Guid", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda12()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Test(Sub(x As System.Func(Of String, Object))
                 x = Function(z)
                         If z Is Nothing
                             Dim y As System.Guid
                             Return y'BIND:"y"
                         End If

                         Return z
                     End Function
             End Sub)
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Guid", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("y As System.Guid", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub Lambda13()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As System.Func(Of String, Object) = Function(z) z  'BIND:"Function(z) z"
    End Sub

End Module
    </file>
</compilation>)

            Dim node As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, DirectCast(node, ExpressionSyntax))
            Assert.Null(semanticInfo1.Type)
            Assert.NotNull(semanticInfo1.Symbol)
            Assert.IsAssignableFrom(Of LambdaSymbol)(semanticInfo1.Symbol)
            Assert.Equal("System.Func(Of System.String, System.Object)", semanticInfo1.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelWidening, semanticInfo1.ImplicitConversion.Kind)

            Dim semanticInfo2 = CompilationUtils.GetSemanticInfoSummary(semanticModel, DirectCast(node, ExpressionSyntax))
            Assert.Equal("System.Func(Of System.String, System.Object)", semanticInfo2.ConvertedType.ToTestDisplayString())
            Assert.NotNull(semanticInfo2.Symbol)
            Assert.IsAssignableFrom(Of LambdaSymbol)(semanticInfo2.Symbol)

            Assert.Same(semanticInfo1.Symbol, semanticInfo2.Symbol)

            Assert.Equal("Function (z As System.String) As System.Object", semanticInfo2.Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Lambda14()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Me.GetNameToMembersMap().
            Where(Function(kvp) kvp.Value.Any(Function(v) v.Kind = SymbolKind.NamedType)). 'BIND1:"kvp"
            ToDictionary(
                Function(kvp) kvp.Key, 'BIND2:"kvp"
                Function(kvp) kvp.Value.OfType(Of NamedTypeSymbol)().AsReadOnly(),
                IdentifierComparison.Comparer)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)

            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node1)

            Dim node2 As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)

            Dim semanticInfo2 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node2)
        End Sub

        <Fact>
        Public Sub Bug8643()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Try
 
        Catch ex As Exception When (Function(e 'BIND1:"e"
 
        End Try
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ModifiedIdentifierSyntax = FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)

            Dim e = semanticModel.GetDeclaredSymbol(node1)

            Assert.Equal(SymbolKind.Parameter, e.Kind)
            Assert.Equal("e", e.Name)

            Assert.Same(e, semanticModel.GetDeclaredSymbol(DirectCast(node1, VisualBasicSyntaxNode)))
            Assert.Same(e, semanticModel.GetDeclaredSymbol(node1.Parent))
            Assert.Same(e, semanticModel.GetDeclaredSymbol(DirectCast(node1.Parent, ParameterSyntax)))

        End Sub

        <Fact>
        Public Sub Bug8522()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Sub New(x As Action)
    End Sub
 
    Public Const X As Integer = 0
End Class
 
Class C
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class
 
<C(New A(Sub() M.Main).X)> 'BIND1:"Main"
Module M
    Friend Const main As Object=Main

    Sub Main
    End Sub
End Module

    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)

            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node1)

            Assert.Equal(2, semanticInfo1.AllSymbols.Length)
        End Sub

        <WorkItem(9805, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub LambdaInWhileStatement()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Test
    Sub Sub1()
        While Function() True 'BIND1:"Function() True"
        End While
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node1)

            Assert.NotNull(semanticInfo1.Symbol)
        End Sub

        <Fact>
        Public Sub SingleLineLambdaWithDimStatement()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Test
    Sub Sub1()
        ' Even though it is an error to have a dim as the statement in the single line sub
        ' verify that GetDeclaredSymbol returns y with the correct type.
        dim x = Sub dim y as integer = 1 'BIND1:"y"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node1 As ModifiedIdentifierSyntax = FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            Dim symbol = DirectCast(semanticModel.GetDeclaredSymbol(node1), LocalSymbol)

            Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(544647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544647")>
        Public Sub InvokeGenericOverloadedMethod()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Class C
     Public Shared Sub Check(Of T)(p1 As IEnumerable(Of T), p2 As IEnumerable(Of T), p3 As IEqualityComparer(Of T))
     End Sub
     Public Shared Sub Check(Of T)(p1 As T, p2 As T, p3 As IEqualityComparer(Of T))
     End Sub
End Class

Module M

    Sub Main()
        Dim list = {"AA", "BB"}
        C.Check(list.Select(Function(r) r), {"aa", "Bb"}, StringComparer.OrdinalIgnoreCase)
    End Sub

End Module
    </file>
    </compilation>, references:={SystemCoreRef})

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().First()
            Assert.Equal("C.Check(list.Select(Function(r) r), {""aa"", ""Bb""}, StringComparer.OrdinalIgnoreCase)", node.ToString())

            Dim info = model.GetSymbolInfo(node)
            Assert.Equal(CandidateReason.None, info.CandidateReason)
            Assert.NotNull(info.Symbol)
        End Sub

        <Fact, WorkItem(566495, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566495")>
        Public Sub Bug566495()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
        <compilation>
            <file name="a.vb"><![CDATA[
        Imports System.Linq
        Imports System.Collections
        Imports System.Collections.Generic

        Module Program
            Sub Main()
                Dim L As New List(Of Goo)

                For i As Integer = 1 To 10
                    Dim F As New Goo
                    F.Id = i
                    F.Name = "some text"
                    L.Add(F)
                Next

        Dim L2 = L.Zip(L, Function(x, y) 
                              Return x. 'BIND:"x"
                          End Function).ToList 
            End Sub

            Public Class Goo
                Public Property Name As String
                Public Property Id As Integer
            End Class
        End Module
    ]]></file>
        </compilation>, {SystemCoreRef})

            AssertTheseDiagnostics(compilation,
<expected>
BC36646: Data type(s) of the type parameter(s) in extension method 'Public Function Zip(Of TSecond, TResult)(second As IEnumerable(Of TSecond), resultSelector As Func(Of Program.Goo, TSecond, TResult)) As IEnumerable(Of TResult)' defined in 'Enumerable' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Dim L2 = L.Zip(L, Function(x, y) 
                   ~~~
BC30203: Identifier expected.
                              Return x. 'BIND:"x"
                                        ~
</expected>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Program.Goo", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("Program.Goo", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("x As Program.Goo", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact, WorkItem(960755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960755")>
        Public Sub Bug960755_01()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System.Collections.Generic
 
Class C
    Sub M(c As IList(Of C))
        Dim tmp = New C()
        tmp.M(Function(a, b) AddressOf c.Add) 'BIND:"c.Add"
    End Sub
End Class
    </file>
    </compilation>)

            Dim node = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim semanticModel = compilation.GetSemanticModel(node.SyntaxTree)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal("Sub System.Collections.Generic.ICollection(Of C).Add(item As C)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString())
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)
        End Sub

        <Fact, WorkItem(960755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960755")>
        Public Sub Bug960755_02()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System.Collections.Generic
 
Class C
    Sub M(c As IList(Of C))
        Dim tmp As Integer = AddressOf c.Add 'BIND:"c.Add"
    End Sub
End Class
    </file>
    </compilation>)

            Dim node = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim semanticModel = compilation.GetSemanticModel(node.SyntaxTree)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal("Sub System.Collections.Generic.ICollection(Of C).Add(item As C)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString())
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)
        End Sub

        <Fact, WorkItem(960755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960755")>
        Public Sub Bug960755_03()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System.Collections.Generic
 
Class C
    Sub M(c As IList(Of C))
        Dim tmp = New C()
        tmp.M(Function(a, b) AddressOf c.Add) 'BIND:"c.Add"
    End Sub

    Sub M(x as System.Func(Of Integer, Integer, System.Action(Of C)))
    End Sub
End Class
    </file>
    </compilation>)

            Dim node = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim semanticModel = compilation.GetSemanticModel(node.SyntaxTree)

            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("Sub System.Collections.Generic.ICollection(Of C).Add(item As C)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

        <Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")>
        Public Sub ParameterReference_01()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System

Class Program
    Shared Sub Main(args As String())
    End Sub

    Function stuff() As Func(Of Program, String)
        Return Function(a) a.
    End Function

End Class
    </file>
    </compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30203: Identifier expected.
        Return Function(a) a.
                             ~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "a").Single()

            Assert.Equal("a.", node.Parent.ToString())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("a As Program", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")>
        Public Sub ParameterReference_02()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System

Class Program
    Shared Sub Main(args As String())
    End Sub

    Sub stuff()
        M1(Function(a) a.)
    End Sub

    Sub M1(l as Func(Of Program, String))
    End Sub
End Class
    </file>
    </compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30203: Identifier expected.
        M1(Function(a) a.)
                         ~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "a").Single()

            Assert.Equal("a.", node.Parent.ToString())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("a As Program", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")>
        Public Sub ParameterReference_03()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System

Class Program
    Shared Sub Main(args As String())
    End Sub

    Sub stuff()
        Dim l as Func(Of Program, String) = Function(a) a.
    End Sub
End Class
    </file>
    </compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30203: Identifier expected.
        Dim l as Func(Of Program, String) = Function(a) a.
                                                          ~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "a").Single()

            Assert.Equal("a.", node.Parent.ToString().Trim())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("a As Program", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")>
        Public Sub ParameterReference_04()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Imports System

Class Program
    Shared Sub Main(args As String())
    End Sub

    Sub stuff()
        Dim l = CType(Function(a) a. , Func(Of Program, String))
    End Sub
End Class
    </file>
    </compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30203: Identifier expected.
        Dim l = CType(Function(a) a. , Func(Of Program, String))
                                     ~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "a").Single()

            Assert.Equal("a.", node.Parent.ToString().Trim())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)

            Assert.Equal("a As Program", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <WorkItem(35096, "https://github.com/dotnet/roslyn/issues/35096")>
        <Fact()>
        Public Sub ThenIncludeSecondArgumentAndMultiArgumentLambdaWithNoLambdaOverlap()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ThenIncludeIntellisenseBugNamespace">
    <file name="a.vb"><![CDATA[
Imports System                       
Imports System.Collections.Generic
Imports System.Linq
Imports System.Linq.Expressions

Namespace ThenIncludeIntellisenseBug

    Class Program
        Shared Sub Main(args As String())
            Dim registrations = New List(Of Registration)().AsQueryable()
            Dim reg = registrations.Include(Function(r) r.Activities).ThenInclude(0, Function(a, b, c) c.F)
        End Sub
    End Class

    Friend Class Registration
        Public Property Activities As ICollection(Of Activity)
    End Class

    Public Class Activity
        Public Property Task As Task
    End Class

    Public Class Task
        Public Property Name As String
    End Class

    Public Interface IIncludableQueryable(Of Out TEntity, Out TProperty)
        Inherits IQueryable(Of TEntity)
    End Interface

    Public Module EntityFrameworkQueryableExtensions
        <System.Runtime.CompilerServices.Extension>
        Public Function Include(Of TEntity, TProperty)(
                source As IQueryable(Of TEntity), 
                navigationPropertyPath As Expression(Of Func(Of TEntity, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, ICollection(Of TPreviousProperty)),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of string, TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension>
        Public Function ThenInclude(Of TEntity, TPreviousProperty, TProperty)(
                source As IIncludableQueryable(Of TEntity, TPreviousProperty),
                a as Integer,
                navigationPropertyPath As Expression(Of Func(Of string, string, TPreviousProperty, TProperty))) As IIncludableQueryable(Of TEntity, TProperty)
            Return Nothing
        End Function

    End Module
End Namespace"
    ]]></file>
</compilation>, references:={SystemCoreRef})

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().ToArray()(1)
            Assert.Equal("registrations.Include(Function(r) r.Activities).ThenInclude(0, Function(a, b, c) c.F)", node.ToString())

            Dim info = model.GetSymbolInfo(node)
            Assert.Equal(2, info.CandidateSymbols.Length)
            Assert.Equal("Public Function ThenInclude(Of TProperty)(a As Integer, navigationPropertyPath As " +
                         "System.Linq.Expressions.Expression(Of System.Func(Of String, ThenIncludeIntellisenseBug.Activity, TProperty))) As " +
                         "ThenIncludeIntellisenseBug.IIncludableQueryable(Of ThenIncludeIntellisenseBug.Registration, TProperty)", info.CandidateSymbols(0).ToString())
            Assert.Equal("Public Function ThenInclude(Of TProperty)(a As Integer, navigationPropertyPath As " +
                         "System.Linq.Expressions.Expression(Of System.Func(Of String, String, System.Collections.Generic.ICollection(Of ThenIncludeIntellisenseBug.Activity), TProperty))) As " +
                         "ThenIncludeIntellisenseBug.IIncludableQueryable(Of ThenIncludeIntellisenseBug.Registration, TProperty)", info.CandidateSymbols(1).ToString())
        End Sub

    End Class

End Namespace
